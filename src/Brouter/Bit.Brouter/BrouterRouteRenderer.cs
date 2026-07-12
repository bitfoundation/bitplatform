using System.Reflection;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Brouter;

internal class BrouterRouteRenderer
{
    private readonly Broute _route;

    // Cache the last merged BrouterRouteParameters and the (inherited, local) reference pair
    // it was derived from. RenderRoute runs on every render of every route in the matched
    // chain; each one used to allocate a fresh dictionary + BrouterRouteParameters even when
    // nothing changed. Cache hit -> zero allocations on the hot render path. Cache miss
    // (parameters changed because a new match committed fresh dictionaries onto the route
    // and/or the cascading inherited value got a new instance) -> rebuild and store.
    private BrouterRouteParameters? _cachedRouteParams;
    private BrouterRouteParameters? _cachedInheritedRef;
    private IReadOnlyDictionary<string, object?>? _cachedLocalRef;

    // Same idea for the RouteData / RouteMeta wrappers: rebuild only when the underlying
    // reference changes. Reusing the wrapper instance also keeps CascadingValue's change
    // detection quiet on renders where the payload didn't change (a fresh wrapper every
    // render would re-notify every subscriber), matching the old raw-object? behavior
    // where an unchanged reference meant "no cascade update".
    private BrouterRouteData? _cachedRouteData;
    private object? _cachedLoadedDataRef;
    private BrouterRouteMeta? _cachedRouteMeta;
    private object? _cachedMetaRef;

    // Keep-alive lifecycle context (see BrouterKeepAliveContext) for the singleton mode
    // (EffectiveKeepAliveMax <= 1). A fresh instance is handed out only when the active/hidden state
    // flips, so consumers reading IsActive in OnParametersSet are notified on every transition while
    // unchanged renders stay allocation-free and quiet.
    private bool _keepAliveActive;
    private BrouterKeepAliveContext? _keepAliveContext;

    // Set by DropKeptContent (IBrouter.ClearKeepAlive) to stop rendering this route's kept-but-hidden
    // content, so its component is disposed and its retained state released. Reset the moment the
    // route matches again, so a later visit rebuilds it fresh.
    private bool _keptDropped;

    // Per-parameter retention (EffectiveKeepAliveMax > 1) for the inline (non-outlet) render path:
    // one entry per recently-visited parameter set, LRU-ordered with the most recently active at the
    // end. Each entry owns a keyed subtree whose parameters/data are frozen while hidden, so a
    // hidden /item/1 instance keeps seeing id=1 while the route's current match is /item/2.
    private readonly List<KeptEntry> _keptEntries = [];

    private sealed class KeptEntry
    {
        public string Key { get; }
        public BrouterRouteParameters Parameters { get; set; } = BrouterRouteParameters.Empty;
        public BrouterRouteData Data { get; set; } = BrouterRouteData.Empty;

        // Per-entry lifecycle context, minted only on an active/hidden flip (same contract as the
        // renderer-level singleton context above).
        private bool _active;
        private BrouterKeepAliveContext? _context;

        public KeptEntry(string key) => Key = key;

        public BrouterKeepAliveContext GetKeepAliveContext(bool active)
        {
            if (_context is null || _active != active)
            {
                _context = new BrouterKeepAliveContext(active);
                _active = active;
            }
            return _context;
        }
    }

    public BrouterRouteRenderer(Broute route)
    {
        _route = route;
    }

    // Drops this route's retained (hidden) keep-alive content on the next render, keeping only the
    // currently active instance (when the route is matched). Backs IBrouter.ClearKeepAlive.
    public void DropKeptContent(bool routeIsMatched)
    {
        // Per-parameter mode: the active entry (list tail, see RenderKeptEntries' LRU ordering)
        // survives when the route is matched; every hidden sibling is dropped so its subtree is
        // disposed on the next render.
        if (_keptEntries.Count > 0)
        {
            var keep = routeIsMatched ? _keptEntries[^1] : null;
            _keptEntries.Clear();
            if (keep is not null) _keptEntries.Add(keep);
        }

        // Singleton mode: only kept-but-hidden content is dropped; an active route stays rendered.
        if (routeIsMatched is false) _keptDropped = true;
    }

    private BrouterKeepAliveContext GetKeepAliveContext(bool active)
    {
        if (_keepAliveContext is null || _keepAliveActive != active)
        {
            _keepAliveContext = new BrouterKeepAliveContext(active);
            _keepAliveActive = active;
        }
        return _keepAliveContext;
    }

    public void BuildRenderTree(RenderTreeBuilder builder, bool matched)
    {
        // A fresh match clears any prior "dropped" state so the route renders again.
        if (matched) _keptDropped = false;

        builder.OpenComponent<CascadingValue<Broute>>(0);
        builder.AddAttribute(1, "Name", "ParentRoute");
        builder.AddAttribute(2, "Value", _route);
        builder.AddAttribute(3, "ChildContent", (RenderFragment)(b =>
        {
            b.AddContent(0, _route.ChildContent ?? _route.Routes);
            // A KeepAlive route that has been shown at least once keeps rendering while unmatched -
            // hidden - so its component state survives until it matches again (unless ClearKeepAlive
            // dropped it, in which case it re-renders only once it matches again).
            if (matched || (_route.KeepAlive && _route.HasEverMatched && _keptDropped is false))
            {
                // RenderRoute restarts its own sequence numbers from 0; wrap it in a region
                // so its frames live in an independent sequence-number space and don't collide
                // with the AddContent above.
                b.OpenRegion(1);
                RenderRoute(b, matched);
                b.CloseRegion();
            }
        }));
        builder.CloseComponent();
    }

    private void RenderRoute(RenderTreeBuilder builder, bool matched)
    {
        var inherited = _route.InheritedParameters;
        var local = _route.Parameters;

        // Reuse the previously-built BrouterRouteParameters when neither the inherited
        // cascading instance nor the local match dictionary has been replaced. Both refs
        // get a fresh instance whenever a navigation actually changes the routing state
        // (Brouter.ProcessNavigationAsync commits fresh dictionaries on a match commit, and
        // the cascading inherited reference is reissued by the parent renderer), so this
        // is conservative: equal refs mean nothing user-visible has changed.
        BrouterRouteParameters routeParams;
        if (_cachedRouteParams is not null
            && ReferenceEquals(_cachedInheritedRef, inherited)
            && ReferenceEquals(_cachedLocalRef, local))
        {
            routeParams = _cachedRouteParams;
        }
        else
        {
            var merged = MergeParameters(inherited, local);
            routeParams = new BrouterRouteParameters(merged);

            _cachedRouteParams = routeParams;
            _cachedInheritedRef = inherited;
            _cachedLocalRef = local;
        }

        // Typed wrappers instead of raw object? cascades: a distinct wrapper type per cascade
        // means consumers get compile-time-safe access (Get<T>/TryGet<T>) and match by type
        // alone. The cascades are deliberately unnamed - a named CascadingValue only supplies
        // consumers that request that exact name, so naming them would break plain
        // [CascadingParameter] BrouterRouteData properties. The unique wrapper types make a
        // name redundant for disambiguation.
        var loadedData = _route.LoadedData;
        if (_cachedRouteData is null || ReferenceEquals(_cachedLoadedDataRef, loadedData) is false)
        {
            _cachedRouteData = loadedData is null ? BrouterRouteData.Empty : new BrouterRouteData(loadedData);
            _cachedLoadedDataRef = loadedData;
        }

        var meta = _route.Meta;
        if (_cachedRouteMeta is null || ReferenceEquals(_cachedMetaRef, meta) is false)
        {
            _cachedRouteMeta = meta is null ? BrouterRouteMeta.Empty : new BrouterRouteMeta(meta);
            _cachedMetaRef = meta;
        }
        var routeData = _cachedRouteData;
        var routeMeta = _cachedRouteMeta;

        builder.OpenComponent<CascadingValue<BrouterRouteParameters>>(0);
        builder.AddAttribute(1, "Name", "RouteParameters");
        builder.AddAttribute(2, "Value", routeParams);
        builder.AddAttribute(3, "IsFixed", false);
        builder.AddAttribute(4, "ChildContent", (RenderFragment)(b1 =>
        {
            b1.OpenComponent<CascadingValue<BrouterRouteData>>(0);
            b1.AddAttribute(1, "Value", routeData);
            b1.AddAttribute(2, "ChildContent", (RenderFragment)(b2 =>
            {
                b2.OpenComponent<CascadingValue<BrouterRouteMeta>>(0);
                b2.AddAttribute(1, "Value", routeMeta);
                b2.AddAttribute(2, "ChildContent", (RenderFragment)(b3 =>
                {
                    // Resolve the outlet host: normally the immediate parent, but pathless Group
                    // ancestors are invisible to layout just as they are to the URL - a group that
                    // hosts no outlets of its own passes its children through to ITS parent's
                    // outlets. The walk stops at the first ancestor with outlets (a group CAN host
                    // its own via a layout Content) or at the first non-group ancestor either way.
                    Broute? outletHost = null;
                    for (var p = _route.Parent; p is not null; p = p.Parent)
                    {
                        if (p.Outlets.Count > 0)
                        {
                            outletHost = p;
                            break;
                        }
                        if (p.Group is false) break; // non-group ancestor without outlets: render inline
                    }

                    // Hand the matched child to the host's outlets (the primary outlet renders its
                    // content/error UI, named outlets its BrouterView fragments). Only a *matched*
                    // route may claim the outlets - a hidden KeepAlive pass must not hijack them.
                    if (matched && outletHost is not null)
                    {
                        outletHost.SetOutletChild(_route, routeParams);
                    }

                    // Without a primary outlet on the host, the content renders inline right here.
                    if (outletHost is null || outletHost.HasPrimaryOutlet is false)
                    {
                        if (_route.KeepAlive && _route.EffectiveKeepAliveMax > 1)
                        {
                            // Per-parameter retention: one keyed, hidden-unless-active subtree per
                            // recently-visited parameter set, LRU-evicted over the route's budget.
                            RenderKeptEntries(b3, matched, routeParams, routeData);
                        }
                        else if (_route.KeepAlive)
                        {
                            // Singleton retention: one instance that re-binds across parameter
                            // changes. The stable wrapper element is what preserves the component
                            // subtree across matched <-> hidden flips; only its hidden attribute
                            // toggles.
                            var keepAlive = GetKeepAliveContext(matched);
                            b3.OpenElement(0, "div");
                            if (matched is false) b3.AddAttribute(1, "hidden", true);
                            b3.OpenRegion(2);
                            // Cascade the activate/deactivate signal to the kept content so it can
                            // pause/resume work while hidden (see BrouterKeepAliveContext).
                            b3.OpenComponent<CascadingValue<BrouterKeepAliveContext>>(0);
                            b3.AddAttribute(1, "Value", keepAlive);
                            b3.AddAttribute(2, "IsFixed", false);
                            b3.AddAttribute(3, "ChildContent", (RenderFragment)(bk => EmitContent(bk, routeParams)));
                            b3.CloseComponent();
                            b3.CloseRegion();
                            b3.CloseElement();
                        }
                        else
                        {
                            EmitContent(b3, routeParams);
                        }
                    }
                }));
                b2.CloseComponent();
            }));
            b1.CloseComponent();
        }));
        builder.CloseComponent();
    }

    /// <summary>
    /// Per-parameter keep-alive rendering for the inline (non-outlet) path. Maintains the LRU entry
    /// list (find-or-create the active entry by the current match's parameter key, move it to the
    /// tail, evict the head beyond the budget) and renders every entry inside a keyed wrapper that
    /// is hidden unless active - mirroring BrouterOutlet's kept-children region.
    /// </summary>
    private void RenderKeptEntries(RenderTreeBuilder b3, bool matched, BrouterRouteParameters routeParams, BrouterRouteData? routeData)
    {
        KeptEntry? active = null;
        if (matched)
        {
            var key = _route.ComputeKeepAliveKey();
            active = _keptEntries.Find(e => string.Equals(e.Key, key, StringComparison.Ordinal));
            if (active is null)
            {
                active = new KeptEntry(key);
                _keptEntries.Add(active);
            }
            else if (ReferenceEquals(_keptEntries[^1], active) is false)
            {
                // LRU order: the most recently active entry lives at the tail.
                _keptEntries.Remove(active);
                _keptEntries.Add(active);
            }

            // Only the active entry re-binds to the current match; hidden entries keep the
            // parameters/data they were deactivated with.
            active.Parameters = routeParams;
            active.Data = routeData ?? BrouterRouteData.Empty;

            // Evict beyond the budget: the head is always the least-recently-used hidden entry
            // (the active one was just moved to the tail).
            var max = _route.EffectiveKeepAliveMax;
            while (_keptEntries.Count > max)
            {
                _keptEntries.RemoveAt(0);
            }
        }

        // Constant sequence numbers per iteration (the canonical keyed-list pattern): entries move
        // positions on every LRU reorder, and a moved entry must keep the SAME sequence numbers for
        // its frames or the diff rebuilds its subtree - destroying the very state being kept. The
        // key alone disambiguates siblings.
        foreach (var entry in _keptEntries)
        {
            var isActive = ReferenceEquals(entry, active);
            b3.OpenElement(0, "div");
            b3.SetKey(entry);
            if (isActive is false) b3.AddAttribute(1, "hidden", true);
            b3.OpenRegion(2);
            RenderKeptEntry(b3, entry, isActive);
            b3.CloseRegion();
            b3.CloseElement();
        }
    }

    // One kept entry's subtree: shadows the outer RouteParameters/RouteData cascades with the
    // entry's own (frozen-while-hidden) values, then cascades the activate/deactivate signal, then
    // emits the route content bound to the entry's parameters.
    private void RenderKeptEntry(RenderTreeBuilder b, KeptEntry entry, bool isActive)
    {
        var context = entry.GetKeepAliveContext(isActive);
        var parameters = entry.Parameters;
        var data = entry.Data;

        b.OpenComponent<CascadingValue<BrouterRouteParameters>>(0);
        b.AddAttribute(1, "Name", "RouteParameters");
        b.AddAttribute(2, "Value", parameters);
        b.AddAttribute(3, "IsFixed", false);
        b.AddAttribute(4, "ChildContent", (RenderFragment)(b1 =>
        {
            b1.OpenComponent<CascadingValue<BrouterRouteData>>(0);
            b1.AddAttribute(1, "Value", data);
            b1.AddAttribute(2, "ChildContent", (RenderFragment)(b2 =>
            {
                b2.OpenComponent<CascadingValue<BrouterKeepAliveContext>>(0);
                b2.AddAttribute(1, "Value", context);
                b2.AddAttribute(2, "IsFixed", false);
                b2.AddAttribute(3, "ChildContent", (RenderFragment)(bk => EmitContent(bk, parameters)));
                b2.CloseComponent();
            }));
            b1.CloseComponent();
        }));
        b.CloseComponent();
    }

    // The route's error-boundary/content/component trio for inline (non-outlet) rendering.
    // Same sequence number across the mutually-exclusive branches is fine - only one renders
    // per pass and they diff cleanly across renders.
    private void EmitContent(RenderTreeBuilder b3, BrouterRouteParameters routeParams)
    {
        // Active error boundary: the error UI replaces this route's content while the
        // surrounding cascades (parameters/data/meta) stay available to the fragment.
        if (_route.CurrentError is not null && _route.ErrorContent is not null)
        {
            b3.AddContent(0, _route.ErrorContent(_route.CurrentError));
        }
        else if (_route.Content is not null)
        {
            b3.AddContent(0, _route.Content(routeParams));
        }
        else if (_route.Component is not null)
        {
            // Brouter's effective Found template (the built-in Router.Found counterpart) - the Found
            // parameter, or the built-in AuthorizeRouteView/RouteView composition that Brouter's
            // NotAuthorized/Authorizing/DefaultLayout/Resource parameters enable (see EffectiveFound). The
            // template renders the page itself - typically via RouteView/AuthorizeRouteView bound to
            // the framework RouteData built here - so Brouter must not also instantiate the component.
            // Which branch runs is fixed for the app's lifetime (both sources are wired up before the
            // initial match), so sharing sequence 0 with the direct-instantiation branch diffs cleanly.
            var found = _route.Brouter?.EffectiveFound;
            if (found is not null)
            {
                b3.AddContent(0, found(GetFrameworkRouteData(routeParams)));
            }
            else
            {
                EnsureNoAuthorizationRequirements(_route.Component);
                b3.OpenComponent(0, _route.Component);
                ApplyTypedParameters(b3, _route.Component, routeParams, _route.Brouter?.CurrentLocation,
                    _route.BindComponentParametersByName ? _route.TemplateParameterNames : null);
                b3.CloseComponent();
            }
        }
    }

    // Whether a component type declares authorization requirements ([Authorize] / any IAuthorizeData),
    // cached per type: the check runs on every native render of a Component route.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, bool> _requiresAuthorizationCache = new();

    /// <summary>
    /// Fail-closed guard for code paths that cannot enforce authorization: a component carrying
    /// <c>[Authorize]</c> must never be instantiated by one. The built-in
    /// <c>NotAuthorized</c>/<c>Authorizing</c> composition never reaches this (the framework's
    /// <c>AuthorizeRouteView</c> enforces the attribute there); a user-supplied <c>Brouter.Found</c>
    /// template owns its own auth stack, like the built-in Router. This covers direct instantiation
    /// by Brouter itself - inline or inside a <see cref="BrouterOutlet"/> - and the layout-only
    /// <c>DefaultLayout</c> composition, because the framework's plain <c>RouteView</c> performs
    /// no authorization check at all.
    /// </summary>
    internal static void EnsureNoAuthorizationRequirements(Type componentType)
    {
        var requiresAuthorization = _requiresAuthorizationCache.GetOrAdd(
            componentType,
            static t => t.GetCustomAttributes(inherit: true).OfType<Microsoft.AspNetCore.Authorization.IAuthorizeData>().Any());

        if (requiresAuthorization)
        {
            throw new InvalidOperationException(
                $"The component '{componentType.FullName}' declares authorization requirements ([Authorize]), " +
                "but it is being rendered without authorization support, which would silently skip the check. " +
                "Set Brouter's NotAuthorized/Authorizing parameters (or a Found template ending in AuthorizeRouteView) " +
                "so pages render through the framework's authorization pipeline, or enforce access with a route " +
                "Guard and remove the [Authorize] attribute from the component.");
        }
    }

    // Cached framework RouteData handed to Brouter.Found, rebuilt only when the matched parameters
    // instance or the component type actually changes (same reuse rationale as the wrapper caches
    // above: a stable instance keeps the Found fragment's consumers - RouteView/AuthorizeRouteView -
    // quiet on renders where nothing routing-related changed). Keyed on the BrouterRouteParameters
    // reference: RenderRoute reissues it exactly when a navigation commits new routing state, and
    // per-parameter keep-alive entries each carry their own frozen instance, so a hidden kept page
    // keeps seeing the route values it was deactivated with.
    private RouteData? _cachedPageRouteData;
    private BrouterRouteParameters? _cachedPageRouteDataParamsRef;
    private Type? _cachedPageRouteDataComponentRef;

    private RouteData GetFrameworkRouteData(BrouterRouteParameters routeParams)
    {
        if (_cachedPageRouteData is null
            || ReferenceEquals(_cachedPageRouteDataParamsRef, routeParams) is false
            || ReferenceEquals(_cachedPageRouteDataComponentRef, _route.Component) is false)
        {
            // The merged parameter values (ancestor + own template parameters) mirror what the
            // built-in router's RouteData.RouteValues carries for the matched page: for a flat
            // discovered @page route they are exactly the template's matched values, constraint-
            // converted (e.g. {id:int} -> boxed int) the same way RouteView expects to bind them,
            // normalized with explicit nulls for optional parameters the URL left unfilled.
            _cachedPageRouteData = new RouteData(_route.Component!,
                NormalizeRouteValues(routeParams.Values, _route.TemplateParameterNames));
            _cachedPageRouteDataParamsRef = routeParams;
            _cachedPageRouteDataComponentRef = _route.Component;
        }
        return _cachedPageRouteData;
    }

    // Framework-router parity for RouteData.RouteValues: the built-in router adds an explicit null
    // entry for every route parameter the URL left unfilled (trailing optionals), so RouteView still
    // emits a parameter frame for it and a page instance reused across navigations (e.g.
    // /profile/saleh -> /profile) has the stale value reset instead of silently kept. Returns the
    // original dictionary unchanged when every template parameter is present.
    internal static IReadOnlyDictionary<string, object?> NormalizeRouteValues(
        IReadOnlyDictionary<string, object?> values, IReadOnlySet<string>? templateParameterNames)
    {
        if (templateParameterNames is null || templateParameterNames.Count == 0) return values;

        Dictionary<string, object?>? augmented = null;
        foreach (var name in templateParameterNames)
        {
            if (values.ContainsKey(name)) continue;
            augmented ??= new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);
            augmented[name] = null;
        }
        return augmented ?? values;
    }

    internal static void ApplyTypedParameters(RenderTreeBuilder builder, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] Type componentType, BrouterRouteParameters parameters, BrouterLocation? location, IReadOnlySet<string>? conventionalTemplateParameters = null)
    {
        // Reflect once per type. Simple, correct, allocates only on first hit per type.
        // Trimming: Component is annotated DynamicallyAccessedMemberTypes.All so its members are preserved.
        //
        // Two binding modes:
        //   - Default (conventionalTemplateParameters is null): bind only [BrouterParameter]/[BrouterQuery]
        //     annotated properties. This is the original, opt-in Brouter model.
        //   - Conventional (BindComponentParametersByName / attribute-discovered @page routes): additionally
        //     bind plain [Parameter] properties by name and [SupplyParameterFromQuery] properties from the
        //     query, Blazor-style. Plain [Parameter] properties that don't correspond to a route parameter
        //     in this route's template are skipped so unrelated component parameters aren't clobbered.
        var conventional = conventionalTemplateParameters is not null;
        var bindings = conventional
            ? BrouterTypedParameterCache.GetConventionalBindings(componentType)
            : BrouterTypedParameterCache.GetBindings(componentType);
        // Sequence numbers for dynamic parameter attributes start after the OpenComponent (0).
        // These are stable per render because the same bindings are iterated in the same order.
        var seq = 1;
        foreach (var b in bindings)
        {
            // In conventional mode, a non-query binding whose name isn't one of this route's template
            // parameters is a plain component input, not a route value: leave it untouched. (The skip set
            // is deterministic for a given type+template, so sequence numbers stay stable across renders.)
            if (conventional && b.IsQuery is false && conventionalTemplateParameters!.Contains(b.ParameterName) is false)
                continue;

            // Always emit an attribute frame per binding, even when the binding is missing or
            // unconvertible. Component instances are reused across navigations that match the
            // same Component (e.g. /profile/saleh -> /profile), so silently skipping a frame
            // would leave the previous value on the property instead of clearing it back to
            // its default. Stable per-binding sequence numbers also keep Blazor's diff happy.
            object? value;

            if (b.IsQuery)
            {
                if (location is null || TryBindQuery(b, location, out value) is false)
                {
                    value = DefaultValueFor(b.PropertyType);
                }
            }
            else if (parameters.Values.TryGetValue(b.ParameterName, out var raw) is false || raw is null)
            {
                value = DefaultValueFor(b.PropertyType);
            }
            else if (b.PropertyType.IsAssignableFrom(raw.GetType()))
            {
                value = raw;
            }
            else if (parameters.TryGetWeak(b.ParameterName, b.PropertyType, out var converted))
            {
                value = converted;
            }
            else
            {
                value = DefaultValueFor(b.PropertyType);
            }

            builder.AddAttribute(seq++, b.PropertyName, value);
        }
    }

    // Cache boxed default(T) per value type so we don't allocate a fresh single-element array
    // on every render. ApplyTypedParameters runs frequently and iterates every binding, so the
    // previous per-call Array.CreateInstance produced avoidable GC pressure. Boxed value-type
    // defaults are effectively immutable for our purposes (Blazor unboxes when assigning to the
    // property setter), so a shared instance per Type is safe to hand out repeatedly.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, object?> _boxedDefaultCache = new();

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2067",
        Justification = "t comes from PropertyType of a [Parameter] on a component reached via Route.Component, " +
                        "which is annotated DynamicallyAccessedMemberTypes.All so its parameter property types are preserved.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Same as above. Array.CreateInstance for a single element is safe for the closed set of " +
                        "value types reachable from preserved component parameter properties.")]
    private static object? DefaultValueFor(Type t)
    {
        // For nullable / reference types, default is null. For non-nullable value types,
        // create a single-element array of that type and read element 0; this returns the
        // boxed default(T) without requiring constructor annotations on the Type. The result
        // is cached per Type so subsequent renders reuse the same boxed instance.
        if (t.IsValueType is false || Nullable.GetUnderlyingType(t) is not null)
            return null;

        if (_boxedDefaultCache.TryGetValue(t, out var cached))
            return cached;

        var arr = Array.CreateInstance(t, 1);
        var value = arr.GetValue(0);
        _boxedDefaultCache[t] = value;
        return value;
    }

    private static bool TryBindQuery(BrouterParameterBinding binding, BrouterLocation location, out object? value)
    {
        value = null;
        if (location.QueryParams.TryGetValue(binding.ParameterName, out var values) is false || values.Count == 0)
            return false;

        var propType = binding.PropertyType;

        // Multi-value support: per BrouterQueryAttribute docs, string[]-typed properties receive every value.
        if (propType == typeof(string[]))
        {
            var arr = new string[values.Count];
            for (int i = 0; i < values.Count; i++) arr[i] = values[i];
            value = arr;
            return true;
        }

        // Scalar properties bind to the first value, converted to the property's type.
        return TryConvert(values[0], propType, out value);
    }

    private static bool TryConvert(string raw, Type targetType, out object? value)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlying == typeof(string))
        {
            value = raw;
            return true;
        }

        // Convert.ChangeType doesn't support string -> Guid or string -> Enum, so handle them
        // explicitly before falling back. Mirrors RouteParameters.TryGetWeak so [BrouterQuery]
        // bindings accept the same scalar types as [BrouterParameter]. Nullable<T> is honored
        // because we resolved the underlying type above.
        if (underlying == typeof(Guid))
        {
            if (Guid.TryParse(raw, out var guidVal))
            {
                value = guidVal;
                return true;
            }
            value = null;
            return false;
        }

        if (underlying.IsEnum)
        {
            if (Enum.TryParse(underlying, raw, ignoreCase: true, out var enumVal))
            {
                value = enumVal;
                return true;
            }
            value = null;
            return false;
        }

        try
        {
            value = System.Convert.ChangeType(raw, underlying, System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            // See BrouterRouteParameters.TryGetWeak for the rationale: these are the documented
            // Convert.ChangeType failure modes. Narrower catch keeps genuine programming bugs
            // (NREs, OOM) visible.
            value = null;
            return false;
        }
    }

    private static IReadOnlyDictionary<string, object?> MergeParameters(BrouterRouteParameters? inherited, IReadOnlyDictionary<string, object?>? local)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (inherited is not null)
        {
            foreach (var kv in inherited.Values) result[kv.Key] = kv.Value;
        }
        if (local is not null)
        {
            foreach (var kv in local) result[kv.Key] = kv.Value; // local wins
        }
        return result;
    }
}

internal static class BrouterTypedParameterCache
{
    // ConcurrentDictionary lets cold-start renders run reflection in parallel instead of
    // serializing on a single lock. The cache is read every render of every component that
    // uses [BrouterParameter] / [BrouterQuery], so contention on a coarse lock matters when
    // many such components are mounted at once (e.g. a list page with many cards).
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, BrouterParameterBinding[]> _cache = new();

    // Separate cache for the conventional (by-name) binding set used by attribute-discovered / @page
    // routes. Kept apart from _cache because the two produce different binding sets for the same type
    // (conventional covers every [Parameter] property; the default covers only annotated ones).
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, BrouterParameterBinding[]> _conventionalCache = new();

    public static BrouterParameterBinding[] GetBindings([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        // Fast path: hit the cached value without going through the factory delegate.
        if (_cache.TryGetValue(type, out var cached)) return cached;

        // Compute outside GetOrAdd so the trimmer can see the [DynamicallyAccessedMembers]
        // requirement is satisfied at the call site (passing BuildBindings as a delegate would
        // trip IL2111 because the trimmer can't follow annotation through delegate invocation).
        // Under contention multiple threads may race here and produce equivalent arrays; only
        // one wins via TryAdd, the rest are GC'd. BuildBindings is pure for a given Type so
        // either result is correct.
        var bindings = BuildBindings(type);
        _cache.TryAdd(type, bindings);
        return _cache.TryGetValue(type, out var stored) ? stored : bindings;
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2067",
        Justification = "type flows from GetBindings whose parameter is annotated with " +
                        "DynamicallyAccessedMemberTypes.PublicProperties; the factory only reads public properties.")]
    private static BrouterParameterBinding[] BuildBindings([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        var bindings = new List<BrouterParameterBinding>();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var paramAttr = prop.GetCustomAttribute<BrouterParameterAttribute>();
            var queryAttr = prop.GetCustomAttribute<BrouterQueryAttribute>();
            if (paramAttr is null && queryAttr is null) continue;

            // Reject ambiguous annotations up front: a property carrying both attributes
            // would silently bind as one or the other, leaving the developer unaware that
            // half of their intent was dropped. Fail fast with a clear message that names
            // the offending property and both attribute names.
            if (paramAttr is not null && queryAttr is not null)
                throw new InvalidOperationException(
                    $"Property '{type.FullName}.{prop.Name}' is annotated with both " +
                    $"[{nameof(BrouterParameterAttribute)}] and [{nameof(BrouterQueryAttribute)}]. " +
                    "Pick exactly one: a property can bind to either a route parameter or a query string value, not both.");

            // [BrouterParameter] / [BrouterQuery] only have an effect when Blazor recognises
            // the property as a component parameter, i.e. it's annotated with [Parameter]
            // (or [CascadingParameter], which Brouter doesn't drive) and has a public setter.
            // Without that, AddAttribute below would feed an unknown attribute into the
            // component and Blazor would throw a generic exception the moment the route
            // matches. Failing here gives the developer a clear, actionable message.
            var attrName = paramAttr is not null ? nameof(BrouterParameterAttribute) : nameof(BrouterQueryAttribute);
            if (prop.GetCustomAttribute<ParameterAttribute>() is null)
                throw new InvalidOperationException(
                    $"Property '{type.FullName}.{prop.Name}' is annotated with [{attrName}] but is missing [Parameter]. " +
                    "Add [Parameter] (or remove the Brouter binding attribute).");
            if (prop.SetMethod is null || prop.SetMethod.IsPublic is false)
                throw new InvalidOperationException(
                    $"Property '{type.FullName}.{prop.Name}' is annotated with [{attrName}] but has no public setter. " +
                    "Add a public setter so the router can assign the bound value.");

            if (paramAttr is not null)
            {
                bindings.Add(new BrouterParameterBinding(prop.Name, paramAttr.Name ?? prop.Name, prop.PropertyType, IsQuery: false));
            }
            else
            {
                bindings.Add(new BrouterParameterBinding(prop.Name, queryAttr!.Name ?? prop.Name, prop.PropertyType, IsQuery: true));
            }
        }

        return bindings.ToArray();
    }

    /// <summary>
    /// Builds the binding set for conventional (Blazor-style) route components - those rendered by an
    /// attribute-discovered route or with <see cref="Broute.BindComponentParametersByName"/> set. Every
    /// public <c>[Parameter]</c> property is considered: query-supplied ones (<c>[SupplyParameterFromQuery]</c>
    /// or <c>[BrouterQuery]</c>) become query bindings, the rest become route-parameter bindings keyed by
    /// property name (honoring a <c>[BrouterParameter(Name = ...)]</c> override). The caller filters the
    /// route bindings down to the parameters actually present in the route template.
    /// </summary>
    public static BrouterParameterBinding[] GetConventionalBindings([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        if (_conventionalCache.TryGetValue(type, out var cached)) return cached;

        var bindings = BuildConventionalBindings(type);
        _conventionalCache.TryAdd(type, bindings);
        return _conventionalCache.TryGetValue(type, out var stored) ? stored : bindings;
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2067",
        Justification = "type flows from GetConventionalBindings whose parameter is annotated with " +
                        "DynamicallyAccessedMemberTypes.PublicProperties; the factory only reads public properties.")]
    private static BrouterParameterBinding[] BuildConventionalBindings([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        var bindings = new List<BrouterParameterBinding>();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Only Blazor component parameters participate. [CascadingParameter] properties are driven by
            // the framework, not by route values, so they're intentionally excluded.
            if (prop.GetCustomAttribute<ParameterAttribute>() is null) continue;
            if (prop.SetMethod is null || prop.SetMethod.IsPublic is false) continue;

            var brouterParam = prop.GetCustomAttribute<BrouterParameterAttribute>();
            var brouterQuery = prop.GetCustomAttribute<BrouterQueryAttribute>();
            if (brouterParam is not null && brouterQuery is not null)
                throw new InvalidOperationException(
                    $"Property '{type.FullName}.{prop.Name}' is annotated with both " +
                    $"[{nameof(BrouterParameterAttribute)}] and [{nameof(BrouterQueryAttribute)}]. " +
                    "Pick exactly one: a property can bind to either a route parameter or a query string value, not both.");

            var supplyFromQuery = prop.GetCustomAttribute<SupplyParameterFromQueryAttribute>();

            if (brouterQuery is not null)
            {
                bindings.Add(new BrouterParameterBinding(prop.Name, brouterQuery.Name ?? prop.Name, prop.PropertyType, IsQuery: true));
            }
            else if (supplyFromQuery is not null)
            {
                bindings.Add(new BrouterParameterBinding(prop.Name, supplyFromQuery.Name ?? prop.Name, prop.PropertyType, IsQuery: true));
            }
            else
            {
                bindings.Add(new BrouterParameterBinding(prop.Name, brouterParam?.Name ?? prop.Name, prop.PropertyType, IsQuery: false));
            }
        }

        return bindings.ToArray();
    }
}

internal readonly record struct BrouterParameterBinding(string PropertyName, string ParameterName, Type PropertyType, bool IsQuery);
