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

    // Route lifecycle context (see BrouterRouteContext) for inline content in the singleton modes:
    // transient (non-keep-alive) routes and KeepAlive routes with EffectiveKeepAliveMax <= 1. One
    // stable instance per content session - created when the content mounts (see RenderRoute) and
    // discarded when the session ends (NotifyDeparture for transient routes, DropKeptContent for
    // cleared keep-alive content), so IsFirstActivation is accurate per component instance.
    private BrouterRouteContext? _context;

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

        // Per-entry lifecycle context: stable for the entry's whole life, so a kept instance's
        // handlers stay registered across hide/show flips and eviction disposes them with the
        // entry. Born active: entries are only ever created for the current match.
        public BrouterRouteContext Context { get; } = new(initiallyActive: true);

        public KeptEntry(string key) => Key = key;
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
        // The lifecycle context dies with the dropped content so a later visit starts a fresh
        // session (IsFirstActivation true again) instead of reusing state that belonged to the
        // disposed instance.
        if (routeIsMatched is false)
        {
            _keptDropped = true;
            _context = null;
        }
    }

    /// <summary>
    /// Fires the deactivation side of the route lifecycle for this route's inline-rendered content,
    /// called by the navigation pipeline BEFORE the render that hides or unmounts it (so a
    /// <see cref="BrouterRouteDeactivationReason.Disposing"/> callback's synchronous part runs while
    /// the components are still alive). <paramref name="willRemainMatched"/> is true when the route
    /// stays in the new committed chain but an intermediate render (pending-navigation UI) is about
    /// to unmount its content anyway: retained keep-alive content skips the event (it merely hides
    /// for the duration and resolves as a renavigation at commit), while transient content really is
    /// torn down and gets its Disposing notification plus a fresh session.
    /// <paramref name="contentReplaced"/> is true when the coming render replaces this route's
    /// committed content in place (an error boundary painting its ErrorContent, see
    /// <see cref="Brouter.RenderNavigationError"/>): the page is disposed even on a keep-alive
    /// route, so the departure is forced to Disposing and the session ends - the replacement
    /// output then activates as a fresh one.
    /// </summary>
    public void NotifyDeparture(BrouterLocation to, bool willRemainMatched, Action<Exception> onError, bool contentReplaced = false)
    {
        if (_route.KeepAlive && _route.EffectiveKeepAliveMax > 1)
        {
            // The error-boundary render replaces the ACTIVE entry's page with ErrorContent: that
            // entry's session honestly ends (Disposing) and the entry is dropped, so the
            // replacement output renders with a fresh context (RenderKeptEntries recreates the
            // missing active entry). Hidden siblings keep their retention - they already received
            // their Hidden deactivation when they were hidden.
            if (contentReplaced)
            {
                foreach (var entry in _keptEntries.ToArray())
                {
                    if (entry.Context.IsActive is false) continue;
                    entry.Context.FireDeactivated(BrouterRouteDeactivationReason.Disposing, to, onError);
                    _keptEntries.Remove(entry);
                }
                return;
            }

            // Per-parameter entries are always retained; a transient hide is a no-op for them.
            if (willRemainMatched) return;
            // Snapshot: deactivation handlers run synchronously and can mutate _keptEntries
            // (e.g. via IBrouter.ClearKeepAlive -> DropKeptContent).
            foreach (var entry in _keptEntries.ToArray())
            {
                entry.Context.FireDeactivated(BrouterRouteDeactivationReason.Hidden, to, onError);
            }
            return;
        }

        if (_route.KeepAlive && _keptDropped is false && contentReplaced is false)
        {
            if (willRemainMatched) return;
            _context?.FireDeactivated(BrouterRouteDeactivationReason.Hidden, to, onError);
            return;
        }

        // Transient content - or content an error boundary's render is about to replace
        // (contentReplaced, keep-alive included): the departing render unmounts and disposes it,
        // ending the session.
        _context?.FireDeactivated(BrouterRouteDeactivationReason.Disposing, to, onError);
        _context = null;
    }

    /// <summary>
    /// Pre-render half of an arrival for per-parameter keep-alive: deactivates (Hidden) the
    /// previously active sibling entries of the incoming parameter key. Called by the pipeline at
    /// commit, BEFORE the render - so an entry the commit render is about to LRU-evict has always
    /// received its Hidden deactivation first, and never dies silently while still marked active.
    /// Singleton and transient modes have no sibling entries; their arrival resolves entirely at
    /// flush time. The key reads the route's just-committed parameters (assigned before this runs).
    /// </summary>
    public void PrepareArrival(BrouterLocation to, Action<Exception> onError)
    {
        if (_route.KeepAlive is false || _route.EffectiveKeepAliveMax <= 1) return;

        var key = _route.ComputeKeepAliveKey();
        // Snapshot: deactivation handlers run synchronously and can mutate _keptEntries
        // (e.g. via IBrouter.ClearKeepAlive -> DropKeptContent).
        foreach (var entry in _keptEntries.ToArray())
        {
            if (string.Equals(entry.Key, key, StringComparison.Ordinal)) continue;
            entry.Context.FireDeactivated(BrouterRouteDeactivationReason.Hidden, to, onError);
        }
    }

    /// <summary>
    /// Fires a Disposing deactivation on any still-active inline content when this route itself is
    /// being torn down outside a navigation (conditionally-removed route, hosting layout unmount) -
    /// so active content never dies without its deactivation callback. Already-hidden retained
    /// content was deactivated when it was hidden; disposal is its final signal.
    /// </summary>
    public void NotifyTeardown(BrouterLocation location, Action<Exception> onError)
    {
        // Snapshot: deactivation handlers run synchronously and can mutate _keptEntries
        // (e.g. via IBrouter.ClearKeepAlive -> DropKeptContent).
        foreach (var entry in _keptEntries.ToArray())
        {
            entry.Context.FireDeactivated(BrouterRouteDeactivationReason.Disposing, location, onError);
        }
        _context?.FireDeactivated(BrouterRouteDeactivationReason.Disposing, location, onError);
        _context = null;
    }

    /// <summary>
    /// Whether any of this route's inline-rendered content is active (visible) AND has lifecycle
    /// handlers registered - the pre-flight for the navigation-lock phase (see
    /// <see cref="CollectActiveContexts"/>), so navigations away from handler-less content skip
    /// the lock walk entirely.
    /// </summary>
    public bool HasActiveLifecycleHandlers()
    {
        foreach (var entry in _keptEntries)
        {
            if (entry.Context is { IsActive: true, HasHandlers: true }) return true;
        }
        return _context is { IsActive: true, HasHandlers: true };
    }

    /// <summary>
    /// Collects the lifecycle contexts of this route's active (visible) inline content that have
    /// handlers registered, for the pre-commit navigation-lock dispatch (see
    /// <see cref="BrouterRouteContext.FireDeactivatingAsync"/>). Hidden kept entries are excluded -
    /// they aren't being deactivated by the pending navigation and get no vote. At most one context
    /// is active per renderer (the singleton context or the active per-parameter entry).
    /// </summary>
    public void CollectActiveContexts(List<BrouterRouteContext> into)
    {
        foreach (var entry in _keptEntries)
        {
            if (entry.Context is { IsActive: true, HasHandlers: true }) into.Add(entry.Context);
        }
        if (_context is { IsActive: true, HasHandlers: true }) into.Add(_context);
    }

    /// <summary>
    /// Fires the arrival side of the route lifecycle for this route's inline-rendered content,
    /// called by the navigation pipeline AFTER the commit render has landed (content mounted,
    /// handlers registered, DOM available): an activation when the content wasn't active before,
    /// a renavigation when the same instance stayed active through the commit. In per-parameter
    /// mode the previously active sibling entry is deactivated (Hidden) here too - its instance is
    /// retained, so the post-render timing is safe - which is how a parameter change surfaces as an
    /// activate/deactivate pair instead of a renavigation (see <see cref="Broute.KeepAliveMax"/>).
    /// </summary>
    public void FireArrival(BrouterLocation from, BrouterLocation to, Action<Exception> onError)
    {
        if (_route.KeepAlive && _route.EffectiveKeepAliveMax > 1)
        {
            var key = _route.ComputeKeepAliveKey();
            var entry = _keptEntries.Find(e => string.Equals(e.Key, key, StringComparison.Ordinal));
            if (entry is null) return; // defensive: the commit render materializes the entry first

            // Sibling entries were already deactivated pre-render by PrepareArrival; this sweep is
            // a cheap idempotent backstop (FireDeactivated no-ops on inactive contexts). Snapshot:
            // handlers run synchronously and can mutate _keptEntries (e.g. via ClearKeepAlive).
            foreach (var other in _keptEntries.ToArray())
            {
                if (ReferenceEquals(other, entry) is false)
                {
                    other.Context.FireDeactivated(BrouterRouteDeactivationReason.Hidden, to, onError);
                }
            }
            entry.Context.FireArrival(from, to, onError);
            return;
        }

        _context?.FireArrival(from, to, onError);
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
                    // Resolve the outlet host (see Broute.FindOutletHost - the shared walk also
                    // used by the lifecycle dispatch, so where content renders and where its
                    // lifecycle events go can never drift apart).
                    var outletHost = _route.FindOutletHost();

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
                        else
                        {
                            // Both singleton modes share one stable per-session lifecycle context
                            // (see BrouterRouteContext): keep-alive keeps it across hide/show flips,
                            // transient content gets a fresh one per session (NotifyDeparture resets
                            // it when a navigation unmounts the content) - the route lifecycle is
                            // universal, keep-alive only changes what follows deactivation. Contexts
                            // are created by the pass that mounts the content, so `matched` is the
                            // accurate initial IsActive (a keep-alive route re-rendering hidden
                            // never creates one: its context already exists or its content is
                            // dropped).
                            _context ??= new BrouterRouteContext(matched);
                            var context = _context;

                            if (_route.KeepAlive)
                            {
                                // Singleton retention: one instance that re-binds across parameter
                                // changes. The stable wrapper element is what preserves the
                                // component subtree across matched <-> hidden flips; only its
                                // hidden attribute toggles.
                                b3.OpenElement(0, "div");
                                if (matched is false) b3.AddAttribute(1, "hidden", true);
                                b3.OpenRegion(2);
                                EmitContextCascade(b3, context, routeParams);
                                b3.CloseRegion();
                                b3.CloseElement();
                            }
                            else
                            {
                                EmitContextCascade(b3, context, routeParams);
                            }
                        }
                    }
                }));
                b2.CloseComponent();
            }));
            b1.CloseComponent();
        }));
        builder.CloseComponent();
    }

    // The one definition of the lifecycle cascade contract for inline content: an unnamed, FIXED
    // CascadingValue<BrouterRouteContext> (the instance never changes for its content's lifetime;
    // lifecycle flows through IBrouterRoute callbacks, not cascade updates) wrapping the route's
    // content trio. Sequence numbers live in the caller's current scope (0..3).
    private void EmitContextCascade(RenderTreeBuilder builder, BrouterRouteContext context, BrouterRouteParameters routeParams)
    {
        builder.OpenComponent<CascadingValue<BrouterRouteContext>>(0);
        builder.AddAttribute(1, "Value", context);
        builder.AddAttribute(2, "IsFixed", true);
        builder.AddAttribute(3, "ChildContent", (RenderFragment)(bk => EmitContent(bk, routeParams, context)));
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
            RenderKeptEntry(b3, entry);
            b3.CloseRegion();
            b3.CloseElement();
        }
    }

    // One kept entry's subtree: shadows the outer RouteParameters/RouteData cascades with the
    // entry's own (frozen-while-hidden) values, then cascades the entry's stable lifecycle context,
    // then emits the route content bound to the entry's parameters.
    private void RenderKeptEntry(RenderTreeBuilder b, KeptEntry entry)
    {
        var context = entry.Context;
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
            b1.AddAttribute(2, "ChildContent", (RenderFragment)(b2 => EmitContextCascade(b2, context, parameters)));
            b1.CloseComponent();
        }));
        b.CloseComponent();
    }

    // The route's error-boundary/content/component trio for inline (non-outlet) rendering.
    // Same sequence number across the mutually-exclusive branches is fine - only one renders
    // per pass and they diff cleanly across renders.
    private void EmitContent(RenderTreeBuilder b3, BrouterRouteParameters routeParams, BrouterRouteContext? context)
    {
        // Active error boundary: the error UI replaces this route's content while the
        // surrounding cascades (parameters/data/meta) stay available to the fragment.
        if (_route.CurrentError is not null && _route.ErrorContent is not null)
        {
            // This render disposes the directly-instantiated page (if any) that the error UI
            // replaces; drop its auto-registration from the (possibly surviving keep-alive)
            // context - see BrouterRouteContext.ClearAutoRegistered.
            context?.ClearAutoRegistered();
            b3.AddContent(0, _route.ErrorContent(_route.CurrentError));
        }
        else if (_route.Content is not null)
        {
            // A route that previously rendered a directly-instantiated Component page can be
            // re-declared with a Content fragment at runtime: this render disposes that page, so
            // drop its auto-registration from the surviving context - same rationale as the error
            // branch above (a no-op for routes that always rendered Content).
            context?.ClearAutoRegistered();
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
                // A route that previously rendered a directly-instantiated Component page (EffectiveFound
                // was null) can switch to the Found template at runtime if a Found/layout/auth parameter
                // is set: this render disposes that page, so drop its auto-registration from the surviving
                // context - same rationale as the error and Content branches (a no-op when the Found
                // template rendered from the first match).
                context?.ClearAutoRegistered();
                b3.AddContent(0, found(GetFrameworkRouteData(routeParams)));
            }
            else
            {
                EnsureNoAuthorizationRequirements(_route.Component);
                b3.OpenComponent(0, _route.Component);
                var seq = ApplyTypedParameters(b3, _route.Component, routeParams, _route.Brouter?.CurrentLocation,
                    _route.TemplateParameterNames);
                // Auto-register the page instance for the route lifecycle when it implements
                // IBrouterRoute - the router instantiates the component here, so this is the one
                // render path where interface discovery needs no cooperation from the page (the
                // AntDesign ReuseTabs / framework IHandleAfterRender idiom). Content fragments and
                // Found-template pages register through the cascaded context instead. The sequence
                // number follows the parameter frames and is stable per component type.
                if (context is not null)
                {
                    b3.AddComponentReferenceCapture(seq, context.AutoRegisterDelegate);
                }
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

    // Returns the next free sequence number after the emitted parameter frames, so callers can
    // append further frames (e.g. a component reference capture) at a stable position.
    internal static int ApplyTypedParameters(RenderTreeBuilder builder, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] Type componentType, BrouterRouteParameters parameters, BrouterLocation? location, IReadOnlySet<string>? templateParameterNames)
    {
        // Reflect once per type. Simple, correct, allocates only on first hit per type.
        // Trimming: Component is annotated DynamicallyAccessedMemberTypes.All so its members are preserved.
        //
        // One binding mode, Blazor-style: plain [Parameter] properties bind route parameters by name
        // (filtered to the names in this route's template so unrelated component inputs aren't
        // clobbered), and [SupplyParameterFromQuery] (or opt-in [BrouterQuery]) properties bind from
        // the query string. [BrouterParameter] optionally remaps a route parameter to a
        // differently-named property; such explicitly annotated properties bypass the template-name
        // filter (the annotation is the developer's stated intent that the route drives this property).
        var bindings = BrouterTypedParameterCache.GetBindings(componentType);
        // Sequence numbers for dynamic parameter attributes start after the OpenComponent (0).
        // These are stable per render because the same bindings are iterated in the same order.
        var seq = 1;
        foreach (var b in bindings)
        {
            // A convention-bound (non-annotated) binding whose name isn't one of this route's template
            // parameters is a plain component input, not a route value: leave it untouched. (The skip set
            // is deterministic for a given type+template, so sequence numbers stay stable across renders.)
            if (b.IsQuery is false && b.IsExplicit is false
                && (templateParameterNames is null || templateParameterNames.Contains(b.ParameterName) is false))
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

        return seq;
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

        // Multi-value support: string[]-typed query-bound properties receive every value.
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
        // explicitly before falling back. Mirrors RouteParameters.TryGetWeak so query bindings
        // accept the same scalar types as route parameter bindings. Nullable<T> is honored
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
    // serializing on a single lock. The cache is read every render of every routed component,
    // so contention on a coarse lock matters when many such components are mounted at once
    // (e.g. a list page with many cards).
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, BrouterParameterBinding[]> _cache = new();

    /// <summary>
    /// Builds the binding set for a routed component. Every public <c>[Parameter]</c> property is
    /// considered: query-supplied ones (<c>[SupplyParameterFromQuery]</c> or <c>[BrouterQuery]</c>)
    /// become query bindings, the rest become route-parameter bindings keyed by property name
    /// (honoring a <c>[BrouterParameter(Name = ...)]</c> override). The caller filters the
    /// convention-bound route bindings down to the parameters actually present in the route template;
    /// explicitly annotated ones (<see cref="BrouterParameterBinding.IsExplicit"/>) are always applied.
    /// </summary>
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
            var brouterParam = prop.GetCustomAttribute<BrouterParameterAttribute>();
            var brouterQuery = prop.GetCustomAttribute<BrouterQueryAttribute>();
            var brouterAttrName = brouterParam is not null ? nameof(BrouterParameterAttribute)
                : brouterQuery is not null ? nameof(BrouterQueryAttribute) : null;

            // Only Blazor component parameters participate. [CascadingParameter] properties are driven by
            // the framework, not by route values, so they're intentionally excluded. For plain properties
            // that's a silent skip (standard Blazor semantics); for Brouter-annotated ones it's a
            // developer error - the annotation states binding intent that can never take effect - so
            // fail fast with an actionable message instead of leaving the property mysteriously unbound.
            if (prop.GetCustomAttribute<ParameterAttribute>() is null)
            {
                if (brouterAttrName is not null)
                    throw new InvalidOperationException(
                        $"Property '{type.FullName}.{prop.Name}' is annotated with [{brouterAttrName}] but is missing [Parameter]. " +
                        "Add [Parameter] (or remove the Brouter binding attribute).");
                continue;
            }
            if (prop.SetMethod is null || prop.SetMethod.IsPublic is false)
            {
                if (brouterAttrName is not null)
                    throw new InvalidOperationException(
                        $"Property '{type.FullName}.{prop.Name}' is annotated with [{brouterAttrName}] but has no public setter. " +
                        "Add a public setter so the router can assign the bound value.");
                continue;
            }

            var supplyFromQuery = prop.GetCustomAttribute<SupplyParameterFromQueryAttribute>();

            // Reject ambiguous annotation pairs up front: a property carrying two binding attributes
            // would silently bind as one or the other, leaving the developer unaware that half of their
            // intent was dropped. [BrouterQuery] + [SupplyParameterFromQuery] is additionally
            // self-defeating: the framework's query supplier reacts to its own attribute regardless of
            // Brouter, so combining them re-introduces the framework's type restrictions that
            // [BrouterQuery] exists to escape. Fail fast with a clear message naming the property.
            if (brouterParam is not null && brouterQuery is not null)
                throw new InvalidOperationException(
                    $"Property '{type.FullName}.{prop.Name}' is annotated with both " +
                    $"[{nameof(BrouterParameterAttribute)}] and [{nameof(BrouterQueryAttribute)}]. " +
                    "Pick exactly one: a property can bind to either a route parameter or a query string value, not both.");
            if (brouterParam is not null && supplyFromQuery is not null)
                throw new InvalidOperationException(
                    $"Property '{type.FullName}.{prop.Name}' is annotated with both " +
                    $"[{nameof(BrouterParameterAttribute)}] and [{nameof(SupplyParameterFromQueryAttribute)}]. " +
                    "Pick exactly one: a property can bind to either a route parameter or a query string value, not both.");
            if (brouterQuery is not null && supplyFromQuery is not null)
                throw new InvalidOperationException(
                    $"Property '{type.FullName}.{prop.Name}' is annotated with both " +
                    $"[{nameof(BrouterQueryAttribute)}] and [{nameof(SupplyParameterFromQueryAttribute)}]. " +
                    "Pick exactly one: they are alternative ways to bind the same query value, and " +
                    "[SupplyParameterFromQuery] additionally subjects the property to the framework " +
                    "query supplier's type restrictions.");

            if (brouterQuery is not null)
            {
                bindings.Add(new BrouterParameterBinding(prop.Name, brouterQuery.Name ?? prop.Name, prop.PropertyType, IsQuery: true, IsExplicit: true));
            }
            else if (supplyFromQuery is not null)
            {
                bindings.Add(new BrouterParameterBinding(prop.Name, supplyFromQuery.Name ?? prop.Name, prop.PropertyType, IsQuery: true, IsExplicit: true));
            }
            else
            {
                bindings.Add(new BrouterParameterBinding(prop.Name, brouterParam?.Name ?? prop.Name, prop.PropertyType, IsQuery: false, IsExplicit: brouterParam is not null));
            }
        }

        return bindings.ToArray();
    }
}

internal readonly record struct BrouterParameterBinding(string PropertyName, string ParameterName, Type PropertyType, bool IsQuery, bool IsExplicit);
