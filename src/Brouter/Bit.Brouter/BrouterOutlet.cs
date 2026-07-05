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
        public BrouterRouteParameters Parameters = BrouterRouteParameters.Empty;

        // Cached cascade wrappers, mirroring BrouterRouteRenderer: rebuild only when the
        // underlying reference changes so CascadingValue change-detection stays quiet.
        public BrouterRouteData? CachedRouteData;
        public object? CachedLoadedDataRef;
        public BrouterRouteMeta? CachedRouteMeta;
        public object? CachedMetaRef;
    }

    private ChildEntry? _current;
    private readonly List<ChildEntry> _kept = [];

    /// <summary>Receives the matched child from the parent route (see <see cref="Broute.SetOutletChild"/>).</summary>
    internal void Render(Broute route, BrouterRouteParameters parameters)
    {
        if (_current is null || ReferenceEquals(_current.Route, route) is false)
        {
            _current = _kept.Find(k => ReferenceEquals(k.Route, route)) ?? new ChildEntry { Route = route };
        }
        _current.Parameters = parameters;

        // Keep-alive retention is a primary-outlet concern: named outlets render lightweight view
        // fragments whose state lives in the (kept) primary content anyway.
        if (Name.Length == 0 && route.KeepAlive && _kept.Contains(_current) is false)
        {
            _kept.Add(_current);
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
        var seq = 0;
        foreach (var entry in _kept)
        {
            var isActive = ReferenceEquals(entry, current);
            builder.OpenElement(seq++, "div");
            builder.SetKey(entry.Route);
            if (isActive is false) builder.AddAttribute(seq, "hidden", true);
            seq++;
            builder.OpenRegion(seq++);
            RenderChild(builder, entry, EmitRoutedContent(entry));
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
    private void RenderChild(RenderTreeBuilder builder, ChildEntry entry, RenderFragment content)
    {
        var child = entry.Route;

        var loadedData = child.LoadedData;
        if (entry.CachedRouteData is null || ReferenceEquals(entry.CachedLoadedDataRef, loadedData) is false)
        {
            entry.CachedRouteData = loadedData is null ? BrouterRouteData.Empty : new BrouterRouteData(loadedData);
            entry.CachedLoadedDataRef = loadedData;
        }
        var meta = child.Meta;
        if (entry.CachedRouteMeta is null || ReferenceEquals(entry.CachedMetaRef, meta) is false)
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
