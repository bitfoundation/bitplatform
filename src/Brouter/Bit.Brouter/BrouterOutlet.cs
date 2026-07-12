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

        // Keep-alive lifecycle context for this kept child (see BrouterKeepAliveContext). A fresh
        // instance is minted only when the child's active/hidden state flips, so consumers reading
        // IsActive in OnParametersSet are notified on each transition without spurious churn.
        private bool _keepAliveActive;
        private BrouterKeepAliveContext? _keepAliveContext;
        public BrouterKeepAliveContext GetKeepAliveContext(bool active)
        {
            if (_keepAliveContext is null || _keepAliveActive != active)
            {
                _keepAliveContext = new BrouterKeepAliveContext(active);
                _keepAliveActive = active;
            }
            return _keepAliveContext;
        }
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
        _kept.RemoveAll(k => ReferenceEquals(k.Route, route));
        if (_current is not null && ReferenceEquals(_current.Route, route))
        {
            _current = null;
        }
    }

    /// <summary>
    /// Releases every retained (hidden) keep-alive child, keeping only the currently active one.
    /// Backs <see cref="IBrouter.ClearKeepAlive"/>; re-renders so the dropped subtrees are disposed.
    /// </summary>
    internal void ClearKeepAlive()
    {
        var active = _current is not null && _current.Route.Matched ? _current : null;
        var removed = _kept.RemoveAll(k => ReferenceEquals(k, active) is false);
        if (removed > 0) StateHasChanged();
    }

    // Wraps a kept child's content in the keep-alive cascade so it receives activate/deactivate
    // transitions (see BrouterKeepAliveContext). Only kept (primary-outlet KeepAlive) children get
    // it; transient content renders unwrapped.
    private static RenderFragment WrapKeepAlive(ChildEntry entry, bool active, RenderFragment inner) => b =>
    {
        var context = entry.GetKeepAliveContext(active);
        b.OpenComponent<CascadingValue<BrouterKeepAliveContext>>(0);
        b.AddAttribute(1, "Value", context);
        b.AddAttribute(2, "IsFixed", false);
        b.AddAttribute(3, "ChildContent", inner);
        b.CloseComponent();
    };

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

            RenderChild(builder, current, b => b.AddContent(0, view(current.Parameters)));
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
            RenderChild(builder, entry, WrapKeepAlive(entry, isActive, EmitRoutedContent(entry)), refreshData: isActive);
            builder.CloseRegion();
            builder.CloseElement();
        }
        builder.CloseRegion();

        // Region 1: the current match when it isn't a kept entry - the classic transient path,
        // rendered without any wrapper element (unchanged markup for non-KeepAlive routes).
        builder.OpenRegion(1);
        if (current is not null && _kept.Contains(current) is false)
        {
            RenderChild(builder, current, EmitRoutedContent(current));
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
            BrouterRouteRenderer.ApplyTypedParameters(b2, child.Component, entry.Parameters, child.Brouter?.CurrentLocation,
                child.BindComponentParametersByName ? child.TemplateParameterNames : null);
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

        _current = null;
        _kept.Clear();
        Parent?.UnregisterOutlet(Name, this);
    }

    private bool _disposed;
}
