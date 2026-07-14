using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Brouter;

/// <summary>
/// Placeholder that renders the matched child route inside its parent route's content.
/// Equivalent to React Router's <c>&lt;Outlet/&gt;</c> and Vue Router's <c>&lt;router-view/&gt;</c>.
/// The default (unnamed) outlet hosts the child's <c>Content</c>/<c>Component</c>; an outlet with a
/// <see cref="Name"/> hosts the matched child's same-named <see cref="BrouterView"/> fragment
/// (Vue's named views / Angular's secondary outlets, minus URL serialization).
/// </summary>
public class BrouterOutlet : ComponentBase, IDisposable
{
    [CascadingParameter(Name = "ParentRoute")] internal Broute? Parent { get; set; }

    /// <summary>
    /// The outlet's name. Empty (the default) is the primary outlet rendering the matched child's
    /// <c>Content</c>/<c>Component</c>; a named outlet renders the child's
    /// <c>&lt;BrouterView Name="..."&gt;</c> fragment of the same name, or nothing when the child
    /// declares none.
    /// </summary>
    [Parameter] public string Name { get; set; } = string.Empty;

    // Per-child render state. One entry for the currently matched child, plus - on the primary
    // outlet - one retained entry per ever-matched KeepAlive child (their component subtrees stay
    // mounted inside a hidden wrapper so their state survives sibling navigations).
    private sealed class ChildEntry
    {
        public required Broute Route;
        // Retention key for per-parameter keep-alive (Broute.KeepAliveMax > 1): the matched
        // parameter values, so each visited parameter set owns its own entry/subtree. Constant
        // (empty) for singleton keep-alive and transient routes - one entry per route.
        public required string Key;
        public BrouterRouteParameters Parameters = BrouterRouteParameters.Empty;

        // Cached cascade wrappers, mirroring BrouterRouteRenderer: rebuild only when the
        // underlying reference changes so CascadingValue change-detection stays quiet.
        public BrouterRouteData? CachedRouteData;
        public object? CachedLoadedDataRef;
        public BrouterRouteMeta? CachedRouteMeta;
        public object? CachedMetaRef;

        // Route lifecycle context for this child's content (see BrouterRouteContext): stable for
        // the entry's whole life, so a kept instance's handlers stay registered across hide/show
        // flips. Transient children get a fresh entry (and context) per content session - the
        // pipeline's departure notification discards the entry when the child leaves. Born active:
        // entries are only ever created for the current match (see Render).
        public BrouterRouteContext Context { get; } = new(initiallyActive: true);
    }

    private ChildEntry? _current;
    private readonly List<ChildEntry> _kept = [];

    /// <summary>Receives the matched child from the parent route (see <see cref="Broute.SetOutletChild"/>).</summary>
    internal void Render(Broute route, BrouterRouteParameters parameters)
    {
        // Per-parameter keep-alive (KeepAliveMax > 1) keys retained entries by the matched
        // parameter values (ComputeKeepAliveKey returns the constant empty key in singleton mode,
        // preserving the one-entry-that-rebinds behavior).
        var key = Name.Length == 0 && route.KeepAlive ? route.ComputeKeepAliveKey() : string.Empty;

        if (_current is null
            || ReferenceEquals(_current.Route, route) is false
            || string.Equals(_current.Key, key, StringComparison.Ordinal) is false)
        {
            _current = _kept.Find(k => ReferenceEquals(k.Route, route) && string.Equals(k.Key, key, StringComparison.Ordinal))
                ?? new ChildEntry { Route = route, Key = key };
        }
        _current.Parameters = parameters;

        // Keep-alive retention is a primary-outlet concern: named outlets render lightweight view
        // fragments whose state lives in the (kept) primary content anyway.
        if (Name.Length == 0 && route.KeepAlive)
        {
            // LRU order: the most recently active entry lives at the tail (Remove is a no-op for a
            // fresh entry). SetKey keeps each entry's subtree stable across reorders.
            _kept.Remove(_current);
            _kept.Add(_current);

            // Evict this route's least-recently-used hidden entries beyond its budget. Entries of
            // other keep-alive routes sharing this outlet have their own budgets and are untouched.
            var max = route.EffectiveKeepAliveMax;
            var count = 0;
            foreach (var k in _kept)
            {
                if (ReferenceEquals(k.Route, route)) count++;
            }
            for (int i = 0; i < _kept.Count && count > max;)
            {
                if (ReferenceEquals(_kept[i].Route, route) && ReferenceEquals(_kept[i], _current) is false)
                {
                    _kept.RemoveAt(i);
                    count--;
                }
                else
                {
                    i++;
                }
            }
        }

        StateHasChanged();
    }

    /// <summary>Re-renders the outlet (named-view fragments changed on a host re-render).</summary>
    internal void Refresh() => StateHasChanged();

    /// <summary>Drops any retained entry for a disposed route (see <see cref="Broute.Dispose"/>).</summary>
    internal void ForgetChild(Broute route)
    {
        // The route is being torn down outside a navigation: any still-active content gets its
        // Disposing deactivation before the subtree unmounts (idempotent for hidden entries, which
        // were deactivated when they were hidden). Snapshot: deactivation handlers run
        // synchronously and can mutate _kept (e.g. via IBrouter.ClearKeepAlive).
        foreach (var k in _kept.ToArray())
        {
            if (ReferenceEquals(k.Route, route)) NotifyEntryTeardown(k);
        }
        if (_current is not null && ReferenceEquals(_current.Route, route))
        {
            NotifyEntryTeardown(_current);
            _current = null;
        }

        _kept.RemoveAll(k => ReferenceEquals(k.Route, route));
    }

    /// <summary>
    /// Releases every retained (hidden) keep-alive child, keeping only the currently active one.
    /// Backs <see cref="IBrouter.ClearKeepAlive"/>; re-renders so the dropped subtrees are disposed.
    /// </summary>
    internal void ClearKeepAlive()
    {
        var active = _current is not null && _current.Route.Matched ? _current : null;

        // Teardown parity with ForgetChild/Dispose: any still-active dropped entry gets its
        // Disposing deactivation before the re-render unmounts its subtree (a no-op for hidden
        // entries, which were deactivated when they were hidden). Snapshot: deactivation handlers
        // run synchronously and can mutate _kept.
        foreach (var k in _kept.ToArray())
        {
            if (ReferenceEquals(k, active) is false) NotifyEntryTeardown(k);
        }

        var removed = _kept.RemoveAll(k => ReferenceEquals(k, active) is false);

        // If the current entry was among the dropped (route not matched right now), forget it too:
        // reusing it on the next visit would recycle its lifecycle context - and any handlers of
        // the disposed subtree - for a brand-new component instance, corrupting IsFirstActivation.
        // Mirrors the inline renderer's DropKeptContent nulling its _context.
        if (_current is not null && ReferenceEquals(_current, active) is false)
        {
            NotifyEntryTeardown(_current);
            _current = null;
        }

        if (removed > 0) StateHasChanged();
    }

    // Fires the Disposing deactivation for an entry whose content is being torn down outside a
    // navigation (route removal, outlet unmount). Idempotent per entry via the context's IsActive
    // guard; failures surface through the same channel as pipeline lifecycle errors.
    private void NotifyEntryTeardown(ChildEntry entry)
    {
        var brouter = Parent?.Brouter;
        var location = brouter?.CurrentLocation ?? BrouterLocation.Empty;
        entry.Context.FireDeactivated(BrouterRouteDeactivationReason.Disposing, location,
            ex => brouter?.ReportLifecycleError(location, ex));
    }

    // Wraps an outlet child's content in the route lifecycle cascade (see BrouterRouteContext) -
    // primary and named outlets, kept and transient children alike, since the lifecycle is
    // universal. The context is stable per entry, so the cascade is fixed; activate/deactivate/
    // renavigate flow through IBrouterRoute callbacks, not cascade updates. Named views are never
    // kept (retention is a primary-outlet concern), but BrouterRouteBase descendants inside them
    // still need lifecycle and navigation-lock callbacks, so each named entry carries its own
    // context - dispatched alongside the primary content's (see Broute.CollectActiveRouteContexts).
    private static RenderFragment WrapRouteContext(ChildEntry entry, RenderFragment inner) => b =>
    {
        b.OpenComponent<CascadingValue<BrouterRouteContext>>(0);
        b.AddAttribute(1, "Value", entry.Context);
        b.AddAttribute(2, "IsFixed", true);
        b.AddAttribute(3, "ChildContent", inner);
        b.CloseComponent();
    };

    /// <summary>
    /// Fires the deactivation side of the route lifecycle for an outlet-hosted child, called by the
    /// navigation pipeline BEFORE the render that hides or unmounts its content (see
    /// <see cref="BrouterRouteRenderer.NotifyDeparture"/> for the timing/willRemainMatched contract).
    /// </summary>
    internal void NotifyDeparture(Broute route, BrouterLocation to, bool willRemainMatched, Action<Exception> onError)
    {
        if (Name.Length == 0 && route.KeepAlive)
        {
            // Kept entries survive; a transient hide (pending-UI render while the route stays
            // matched) is a no-op for them, resolving as a renavigation at commit. Snapshot:
            // deactivation handlers run synchronously and can mutate _kept.
            if (willRemainMatched) return;
            foreach (var k in _kept.ToArray())
            {
                if (ReferenceEquals(k.Route, route))
                {
                    k.Context.FireDeactivated(BrouterRouteDeactivationReason.Hidden, to, onError);
                }
            }
            return;
        }

        // Transient child - or any named view, which is never kept regardless of KeepAlive (see
        // WrapRouteContext): its content unmounts and disposes; the entry (and its context/session)
        // goes with it so a later visit starts fresh.
        if (_current is not null && ReferenceEquals(_current.Route, route))
        {
            _current.Context.FireDeactivated(BrouterRouteDeactivationReason.Disposing, to, onError);
            _current = null;
        }
    }

    /// <summary>
    /// Pre-render half of an arrival for an outlet-hosted per-parameter keep-alive child:
    /// deactivates (Hidden) the previously active entries whose key differs from the incoming
    /// match, BEFORE the commit render - so an entry that render is about to LRU-evict has always
    /// received its Hidden deactivation first (see <see cref="BrouterRouteRenderer.PrepareArrival"/>).
    /// </summary>
    internal void PrepareArrival(Broute route, BrouterLocation to, Action<Exception> onError)
    {
        if (route.KeepAlive is false) return;

        var key = route.ComputeKeepAliveKey();
        // Snapshot: deactivation handlers run synchronously and can mutate _kept.
        foreach (var k in _kept.ToArray())
        {
            if (ReferenceEquals(k.Route, route) is false) continue;
            if (string.Equals(k.Key, key, StringComparison.Ordinal)) continue;
            k.Context.FireDeactivated(BrouterRouteDeactivationReason.Hidden, to, onError);
        }
    }

    /// <summary>
    /// Whether any of <paramref name="route"/>'s outlet-hosted content is active (visible) AND has
    /// lifecycle handlers registered - the outlet counterpart of
    /// <see cref="BrouterRouteRenderer.HasActiveLifecycleHandlers"/>.
    /// </summary>
    internal bool HasActiveLifecycleHandlers(Broute route)
    {
        if (_current is not null && ReferenceEquals(_current.Route, route)
            && _current.Context is { IsActive: true, HasHandlers: true }) return true;

        foreach (var k in _kept)
        {
            if (ReferenceEquals(k.Route, route) && k.Context is { IsActive: true, HasHandlers: true }) return true;
        }
        return false;
    }

    /// <summary>
    /// Collects the lifecycle contexts of <paramref name="route"/>'s active (visible) outlet-hosted
    /// content that have handlers registered, for the pre-commit navigation-lock dispatch - the
    /// outlet counterpart of <see cref="BrouterRouteRenderer.CollectActiveContexts"/>. Hidden kept
    /// entries get no vote; the current entry may also live in the kept list, so it is deduplicated.
    /// </summary>
    internal void CollectActiveContexts(Broute route, List<BrouterRouteContext> into)
    {
        if (_current is not null && ReferenceEquals(_current.Route, route)
            && _current.Context is { IsActive: true, HasHandlers: true })
        {
            into.Add(_current.Context);
        }

        foreach (var k in _kept)
        {
            if (ReferenceEquals(k.Route, route) is false) continue;
            if (ReferenceEquals(k, _current)) continue;
            if (k.Context is { IsActive: true, HasHandlers: true }) into.Add(k.Context);
        }
    }

    /// <summary>
    /// Fires the arrival side of the route lifecycle for an outlet-hosted child, called by the
    /// navigation pipeline AFTER the commit render has landed (see
    /// <see cref="BrouterRouteRenderer.FireArrival"/> for the timing contract). Hidden kept siblings
    /// of the arriving entry (per-parameter keep-alive) are deactivated here - their instances are
    /// retained, so the post-render timing is safe.
    /// </summary>
    internal void FireArrival(Broute route, BrouterLocation from, BrouterLocation to, Action<Exception> onError)
    {
        var current = _current;
        if (current is null || ReferenceEquals(current.Route, route) is false) return;

        // Sibling entries were already deactivated pre-render by PrepareArrival; this sweep is a
        // cheap idempotent backstop (FireDeactivated no-ops on inactive contexts). Snapshot:
        // handlers run synchronously and can mutate _kept.
        foreach (var k in _kept.ToArray())
        {
            if (ReferenceEquals(k.Route, route) && ReferenceEquals(k, current) is false)
            {
                k.Context.FireDeactivated(BrouterRouteDeactivationReason.Hidden, to, onError);
            }
        }

        current.Context.FireArrival(from, to, onError);
    }

    protected override void OnInitialized()
    {
        if (Parent is null)
            throw new InvalidOperationException("An Outlet must be placed inside a Brouter route.");

        Parent.RegisterOutlet(Name, this);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);

        // Matched is the authoritative "still selected" flag: Brouter resets it on every navigation
        // and only the winning chain gets it back, so a stale _current from a previous navigation
        // renders nothing (kept entries render hidden).
        var current = _current is not null && _current.Route.Matched ? _current : null;

        if (Name.Length > 0)
        {
            // Named outlet: render the matched child's same-named view fragment, if any.
            if (current is null) return;
            var view = current.Route.NamedViews is { } views && views.TryGetValue(Name, out var fragment)
                ? fragment
                : null;
            if (view is null) return;

            RenderChild(builder, current, WrapRouteContext(current, b => b.AddContent(0, view(current.Parameters))));
            return;
        }

        if (current is null && _kept.Count == 0) return;

        // Region 0: retained KeepAlive children. Each stays mounted inside a div that is hidden
        // unless it is the current match; the stable element (keyed by route) is what preserves
        // the component subtree - and its state - across visibility flips.
        builder.OpenRegion(0);
        // Constant sequence numbers per iteration (the canonical keyed-list pattern): entries move
        // positions on LRU reorders, and a moved entry must keep the SAME sequence numbers for its
        // frames or the diff rebuilds its subtree - destroying the very state being kept.
        foreach (var entry in _kept)
        {
            var isActive = ReferenceEquals(entry, current);
            builder.OpenElement(0, "div");
            // Keyed by the entry (stable per route + parameter key), not the route alone, so
            // per-parameter keep-alive entries of the same route each keep their own subtree.
            builder.SetKey(entry);
            if (isActive is false) builder.AddAttribute(1, "hidden", true);
            builder.OpenRegion(2);
            RenderChild(builder, entry, WrapRouteContext(entry, EmitRoutedContent(entry)), refreshData: isActive);
            builder.CloseRegion();
            builder.CloseElement();
        }
        builder.CloseRegion();

        // Region 1: the current match when it isn't a kept entry - the classic transient path,
        // rendered without any wrapper element (unchanged markup for non-KeepAlive routes). The
        // lifecycle cascade still applies: the route lifecycle is universal, keep-alive only
        // changes what follows deactivation.
        builder.OpenRegion(1);
        if (current is not null && _kept.Contains(current) is false)
        {
            RenderChild(builder, current, WrapRouteContext(current, EmitRoutedContent(current)));
        }
        builder.CloseRegion();
    }

    // The matched child's error-boundary/content/component trio, identical in behavior to the
    // pre-named-outlet rendering.
    private RenderFragment EmitRoutedContent(ChildEntry entry) => b2 =>
    {
        var child = entry.Route;

        if (child.CurrentError is not null && child.ErrorContent is not null)
        {
            // This render disposes the directly-instantiated page (if any) that the error UI
            // replaces; drop its auto-registration from the entry's surviving context - see
            // BrouterRouteContext.ClearAutoRegistered.
            entry.Context.ClearAutoRegistered();
            b2.AddContent(0, child.ErrorContent(child.CurrentError));
        }
        else if (child.Content is not null)
        {
            b2.AddContent(0, child.Content(entry.Parameters));
        }
        else if (child.Component is not null)
        {
            // Outlet-hosted routes always render natively (Found deliberately doesn't apply here -
            // see Brouter.Found), so the same fail-closed [Authorize] guard as the inline path applies.
            BrouterRouteRenderer.EnsureNoAuthorizationRequirements(child.Component);
            b2.OpenComponent(0, child.Component);
            var seq = BrouterRouteRenderer.ApplyTypedParameters(b2, child.Component, entry.Parameters, child.Brouter?.CurrentLocation,
                child.TemplateParameterNames);
            // Auto-register the page instance for the route lifecycle (see the matching capture in
            // BrouterRouteRenderer.EmitContent). The sequence follows the parameter frames and is
            // stable per component type.
            b2.AddComponentReferenceCapture(seq, entry.Context.AutoRegisterDelegate);
            b2.CloseComponent();
        }

        // Deliberately NOT rendering child.ChildContent here: the child's own renderer always
        // renders it at the declaration site (that's what registers descendant <Broute>s and
        // <BrouterView>s). Rendering a second copy inside the outlet would mount every descendant
        // component twice - duplicate route registrations (ambiguity errors) and, for
        // BrouterView, an infinite register->refresh->re-render loop.
    };

    /// <summary>
    /// Wraps <paramref name="content"/> in the child's cascade stack (Outlet marker, ParentRoute,
    /// RouteParameters, RouteData, RouteMeta) - the child's own values, not the hosting layout's,
    /// because the DOM renders here rather than at the child route's declaration site.
    /// </summary>
    private void RenderChild(RenderTreeBuilder builder, ChildEntry entry, RenderFragment content, bool refreshData = true)
    {
        var child = entry.Route;

        // refreshData is false for kept-but-hidden entries: their data/meta stay frozen at the
        // values they were deactivated with (the route's live LoadedData belongs to the currently
        // active parameter set). The null checks still run so a first render always has wrappers.
        var loadedData = child.LoadedData;
        if (entry.CachedRouteData is null || (refreshData && ReferenceEquals(entry.CachedLoadedDataRef, loadedData) is false))
        {
            entry.CachedRouteData = loadedData is null ? BrouterRouteData.Empty : new BrouterRouteData(loadedData);
            entry.CachedLoadedDataRef = loadedData;
        }
        var meta = child.Meta;
        if (entry.CachedRouteMeta is null || (refreshData && ReferenceEquals(entry.CachedMetaRef, meta) is false))
        {
            entry.CachedRouteMeta = meta is null ? BrouterRouteMeta.Empty : new BrouterRouteMeta(meta);
            entry.CachedMetaRef = meta;
        }
        var routeData = entry.CachedRouteData;
        var routeMeta = entry.CachedRouteMeta;
        var parameters = entry.Parameters;

        builder.OpenComponent<CascadingValue<BrouterOutlet>>(0);
        builder.AddAttribute(1, "Name", "Outlet");
        builder.AddAttribute(2, "Value", this);
        builder.AddAttribute(3, "ChildContent", (RenderFragment)(b =>
        {
            // Re-establish ParentRoute for any nested routes declared inside the matched child's
            // content, so they can register themselves and recurse correctly.
            b.OpenComponent<CascadingValue<Broute>>(0);
            b.AddAttribute(1, "Name", "ParentRoute");
            b.AddAttribute(2, "Value", child);
            b.AddAttribute(3, "ChildContent", (RenderFragment)(bp =>
            {
                bp.OpenComponent<CascadingValue<BrouterRouteParameters>>(0);
                bp.AddAttribute(1, "Name", "RouteParameters");
                bp.AddAttribute(2, "Value", parameters);
                bp.AddAttribute(3, "IsFixed", false);
                bp.AddAttribute(4, "ChildContent", (RenderFragment)(bd =>
                {
                    bd.OpenComponent<CascadingValue<BrouterRouteData>>(0);
                    bd.AddAttribute(1, "Value", routeData);
                    bd.AddAttribute(2, "ChildContent", (RenderFragment)(bm =>
                    {
                        bm.OpenComponent<CascadingValue<BrouterRouteMeta>>(0);
                        bm.AddAttribute(1, "Value", routeMeta);
                        bm.AddAttribute(2, "ChildContent", content);
                        bm.CloseComponent();
                    }));
                    bd.CloseComponent();
                }));
                bp.CloseComponent();
            }));
            b.CloseComponent();
        }));
        builder.CloseComponent();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // The outlet unmounting destroys every child subtree it hosts - kept and current alike.
        // Any still-active content gets its Disposing deactivation (hidden kept entries already got
        // Hidden when they were hidden; FireDeactivated no-ops on them). Snapshot: deactivation
        // handlers run synchronously and can mutate _kept.
        foreach (var k in _kept.ToArray()) NotifyEntryTeardown(k);
        if (_current is not null) NotifyEntryTeardown(_current);

        _current = null;
        _kept.Clear();
        Parent?.UnregisterOutlet(Name, this);
    }

    private bool _disposed;
}
