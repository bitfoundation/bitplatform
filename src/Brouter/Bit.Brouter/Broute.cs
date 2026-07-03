using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Brouter;

/// <summary>
/// Declares a single route inside a <see cref="Brouter"/>.
/// </summary>
public class BrouterRoute : ComponentBase, IDisposable
{
    /// <summary>
    /// The route path to match. Supports literal segments, parameter segments, constraints and wildcards.
    /// E.g. <c>"/users/{id:int}"</c>, <c>"/files/{**path}"</c>, <c>"/posts/{slug?}"</c>.
    /// For nested (child) routes, an empty string matches the parent path exactly (index route).
    /// </summary>
    [Parameter, EditorRequired] public string Path { get; set; } = string.Empty;

    /// <summary>Optional unique name for this route. Used by <see cref="IBrouter.NavigateToName"/> and <see cref="IBrouter.ResolveUrl"/>.</summary>
    [Parameter] public string? Name { get; set; }

    /// <summary>
    /// When set, navigating to this route redirects to the given URL instead of running loaders or rendering.
    /// Guards (on this route and its ancestors) still run first, so a guard may cancel the navigation or
    /// redirect elsewhere; only when guards pass is the redirect to <see cref="RedirectTo"/> performed.
    /// </summary>
    [Parameter] public string? RedirectTo { get; set; }

    /// <summary>The component type to render when this route matches.</summary>
    [Parameter, DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type? Component { get; set; }

    /// <summary>A render fragment to render when this route matches. The argument carries the route parameters.</summary>
    [Parameter] public RenderFragment<BrouterRouteParameters>? Content { get; set; }

    /// <summary>
    /// Async guard. Use <c>ctx.Cancel()</c> or <c>ctx.Redirect("/login")</c> to deny.
    /// Inspired by Vue Router's <c>beforeEnter</c> and Angular's <c>CanActivate</c>.
    /// </summary>
    [Parameter] public Func<BrouterNavigationContext, ValueTask>? Guard { get; set; }

    /// <summary>
    /// Async data loader. Runs after the route matches and guards pass, before render.
    /// The result is exposed via the cascading <c>RouteData</c> value.
    /// Inspired by React Router v6's <c>loader</c> and Angular's <c>Resolve</c>.
    /// </summary>
    [Parameter] public Func<BrouterNavigationContext, ValueTask<object?>>? Loader { get; set; }

    /// <summary>Optional metadata. Exposed via the cascading <c>RouteMeta</c> value.</summary>
    [Parameter] public object? Meta { get; set; }

    /// <summary>Child routes (used for nesting).</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }


    [CascadingParameter(Name = "Brouter")] internal Brouter? Brouter { get; set; }
    [CascadingParameter(Name = "ParentRoute")] internal BrouterRoute? Parent { get; set; }
    [CascadingParameter(Name = "RouteParameters")] internal BrouterRouteParameters? InheritedParameters { get; set; }


    internal string FullTemplate { get; private set; } = string.Empty;


    private readonly List<BrouterRoute> _children = [];
    internal void AddChild(BrouterRoute route) => _children.Add(route);
    internal void RemoveChild(BrouterRoute route) => _children.Remove(route);

    internal BrouterOutlet? Outlet { get; set; }

    internal BrouterRouteTemplate? RouteTemplate { get; private set; }
    // Tightened from IDictionary to IReadOnlyDictionary: callers only ever read these and the
    // pipeline replaces them wholesale on a match commit. Exposing the mutable interface let
    // any internal caller .Add/.Remove/.Clear them mid-render which would be a footgun against
    // a route that's still part of an actively-rendering matched chain.
    internal IReadOnlyDictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();
    internal IReadOnlyDictionary<string, string[]> ConstraintsByParameter { get; set; } = new Dictionary<string, string[]>();
    internal object? LoadedData { get; set; }

    private BrouterRouteRenderer? _renderer;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (Brouter is null)
            throw new InvalidOperationException("A Route must be nested inside a Brouter.");

        if (Parent is null && string.IsNullOrWhiteSpace(Path))
            throw new InvalidOperationException("A root-level Route must have a non-empty Path. " +
                "Only nested (child) routes may use an empty path to act as an index route.");

        // Compute and parse the template (and build the renderer) before registering with the
        // Brouter or attaching to the Parent. If parsing throws we don't want this Route to be
        // left half-initialized in the parent/router collections.
        if (Parent is null || string.IsNullOrWhiteSpace(Parent.FullTemplate))
        {
            FullTemplate = Path.Trim('/');
        }
        else if (string.IsNullOrEmpty(Path.Trim('/')))
        {
            // Index route (empty/slashes-only Path): inherit the parent's template without a trailing slash
            // so "parent/" doesn't leak into matching/specificity calculations.
            FullTemplate = Parent.FullTemplate.TrimEnd('/');
        }
        else
        {
            FullTemplate = $"{Parent.FullTemplate.TrimEnd('/')}/{Path.TrimStart('/')}";
        }

        RouteTemplate = BrouterTemplateParser.ParseTemplate(FullTemplate);

        // Precompute Specificity / Depth / IsIndex once. These are stable for the lifetime
        // of the route (template and parent chain don't change after registration), so the
        // matching loop and the winner-selection in Brouter.ProcessNavigationAsync can read
        // them as plain field accesses instead of recomputing on every navigation.
        var specificity = 0;
        foreach (var seg in RouteTemplate.TemplateSegments) specificity += seg.Specificity;
        Specificity = specificity;

        var depth = 0;
        for (var p = Parent; p is not null; p = p.Parent) depth++;
        Depth = depth;

        IsIndex = Parent is not null && string.IsNullOrEmpty(Path.Trim('/'));

        _renderer = new BrouterRouteRenderer(this);

        Brouter.RegisterRoute(this);
        Parent?.AddChild(this);
    }

    /// <summary>The combined specificity score of this route's full template.</summary>
    /// <remarks>
    /// Cached at construction. RouteTemplate / parent chain are assigned in OnInitialized
    /// and don't change after registration, so the score never needs to be recomputed.
    /// Recomputing per navigation showed up as a hot loop on apps with many routes.
    /// </remarks>
    internal int Specificity { get; private set; }

    /// <summary>Nesting depth (root routes are 0, each level of nesting adds 1).</summary>
    /// <remarks>Cached at construction. See <see cref="Specificity"/>.</remarks>
    internal int Depth { get; private set; }

    /// <summary>True for nested index routes (child routes whose <see cref="Path"/> is empty or contains only slashes).</summary>
    /// <remarks>Cached at construction. See <see cref="Specificity"/>.</remarks>
    internal bool IsIndex { get; private set; }


    internal bool Matched { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);
        _renderer?.BuildRenderTree(builder, Matched);
    }

    internal void SetMatched()
    {
        Matched = true;

        StateHasChanged();

        Parent?.SetMatched();
    }

    internal async ValueTask<bool> InvokeGuardsAsync(BrouterNavigationContext ctx)
    {
        // Walk from root to leaf so parents authorize children, mirroring Angular's hierarchical guards.
        var chain = new List<BrouterRoute>();
        for (var r = this; r is not null; r = r.Parent) chain.Add(r);
        chain.Reverse();

        // No ConfigureAwait(false): guards typically touch UI state (redirect/cancel via ctx,
        // injected services that expect the renderer context), and the navigation pipeline
        // continues with component state mutations after we return.
        // Observe ctx.CancellationToken at every yield point so a superseded navigation
        // (the pipeline cancels its CTS when a newer one starts) doesn't keep running guards
        // that may perform expensive auth/IO calls or mutate state on behalf of a stale URL.
        if (ctx.CancellationToken.IsCancellationRequested) return false;

        foreach (var node in chain)
        {
            if (node.Guard is not null)
            {
                if (ctx.CancellationToken.IsCancellationRequested) return false;
                await node.Guard(ctx);
                if (ctx.CancellationToken.IsCancellationRequested) return false;
                if (ctx.IsCancelled || ctx.RedirectUrl is not null) return false;
            }
        }

        return true;
    }


    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Brouter?.UnregisterRoute(this);
        Parent?.RemoveChild(this);
    }

    private bool _disposed;
}
