using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Brouter;

/// <summary>
/// Declares a single route inside a <see cref="Brouter"/>.
/// </summary>
public partial class Route : ComponentBase, IDisposable
{
    internal readonly string Id = Guid.NewGuid().ToString();

    /// <summary>
    /// The route path to match. Supports literal segments, parameter segments, constraints and wildcards.
    /// E.g. <c>"/users/{id:int}"</c>, <c>"/files/{**path}"</c>, <c>"/posts/{slug?}"</c>.
    /// For nested (child) routes, an empty string matches the parent path exactly (index route).
    /// </summary>
    [Parameter, EditorRequired] public string Path { get; set; } = string.Empty;

    /// <summary>Optional unique name for this route. Used by <see cref="IBrouter.NavigateToName"/> and <see cref="IBrouter.ResolveUrl"/>.</summary>
    [Parameter] public string? Name { get; set; }

    /// <summary>When set, navigating to this route redirects to the given URL instead of rendering anything.</summary>
    [Parameter] public string? RedirectTo { get; set; }

    /// <summary>The component type to render when this route matches.</summary>
    [Parameter, DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type? Component { get; set; }

    /// <summary>A render fragment to render when this route matches. The argument carries the route parameters.</summary>
    [Parameter] public RenderFragment<RouteParameters>? Content { get; set; }

    /// <summary>
    /// Async guard. Use <c>ctx.Cancel()</c> or <c>ctx.Redirect("/login")</c> to deny.
    /// Inspired by Vue Router's <c>beforeEnter</c> and Angular's <c>CanActivate</c>.
    /// </summary>
    [Parameter] public Func<NavigationContext, ValueTask>? Guard { get; set; }

    /// <summary>
    /// Async data loader. Runs after the route matches and guards pass, before render.
    /// The result is exposed via the cascading <c>RouteData</c> value.
    /// Inspired by React Router v6's <c>loader</c> and Angular's <c>Resolve</c>.
    /// </summary>
    [Parameter] public Func<NavigationContext, ValueTask<object?>>? Loader { get; set; }

    /// <summary>Optional metadata. Exposed via the cascading <c>RouteMeta</c> value.</summary>
    [Parameter] public object? Meta { get; set; }

    /// <summary>Child routes (used for nesting).</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }


    [CascadingParameter(Name = "Brouter")] internal Brouter? Brouter { get; set; }
    [CascadingParameter(Name = "ParentRoute")] internal Route? Parent { get; set; }
    [CascadingParameter(Name = "RouteParameters")] internal RouteParameters? InheritedParameters { get; set; }


    internal string FullTemplate { get; private set; } = string.Empty;


    private readonly List<Route> _children = [];
    internal void AddChild(Route route) => _children.Add(route);
    internal void RemoveChild(Route route) => _children.Remove(route);

    internal Outlet? Outlet { get; set; }

    internal RouteTemplate? RouteTemplate { get; private set; }
    internal IDictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();
    internal IDictionary<string, string[]> ConstraintsByParameter { get; set; } = new Dictionary<string, string[]>();
    internal object? LoadedData { get; set; }

    private RouteRenderer? _renderer;

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

        RouteTemplate = TemplateParser.ParseTemplate(FullTemplate);

        _renderer = new RouteRenderer(this);

        Brouter.RegisterRoute(this);
        Parent?.AddChild(this);
    }

    /// <summary>The combined specificity score of this route's full template.</summary>
    internal int Specificity
    {
        get
        {
            if (RouteTemplate is null) return 0;
            var sum = 0;
            foreach (var s in RouteTemplate.TemplateSegments) sum += s.Specificity;
            return sum;
        }
    }

    /// <summary>Nesting depth (root routes are 0, each level of nesting adds 1).</summary>
    internal int Depth
    {
        get
        {
            var d = 0;
            for (var p = Parent; p is not null; p = p.Parent) d++;
            return d;
        }
    }

    /// <summary>True for nested index routes (child routes whose <see cref="Path"/> is empty or contains only slashes).</summary>
    internal bool IsIndex => Parent is not null && string.IsNullOrEmpty(Path.Trim('/'));


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

    internal async ValueTask<bool> InvokeGuardsAsync(NavigationContext ctx)
    {
        // Walk from root to leaf so parents authorize children, mirroring Angular's hierarchical guards.
        var chain = new List<Route>();
        for (var r = this; r is not null; r = r.Parent) chain.Add(r);
        chain.Reverse();

        // No ConfigureAwait(false): guards typically touch UI state (redirect/cancel via ctx,
        // injected services that expect the renderer context), and the navigation pipeline
        // continues with component state mutations after we return.
        foreach (var node in chain)
        {
            if (node.Guard is not null)
            {
                await node.Guard(ctx);
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
