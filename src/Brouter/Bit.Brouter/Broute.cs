using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Brouter;

/// <summary>
/// Declares a single route inside a <see cref="Brouter"/>.
/// </summary>
public class Broute : ComponentBase, IDisposable
{
    /// <summary>
    /// The route path to match. Supports literal segments, parameter segments, constraints and wildcards.
    /// E.g. <c>"/users/{id:int}"</c>, <c>"/files/{**path}"</c>, <c>"/posts/{slug?}"</c>.
    /// For nested (child) routes, an empty string matches the parent path exactly (index route).
    /// </summary>
    [Parameter, EditorRequired] public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Marks this as a pathless <em>grouping</em> route: it contributes no URL segments and never
    /// matches by itself, existing purely to attach shared behavior - a <see cref="Guard"/>,
    /// <see cref="LeaveGuard"/>, <see cref="Loader"/>, <see cref="ErrorContent"/> or a layout
    /// <see cref="Content"/> (with a <see cref="BrouterOutlet"/>) - to the routes declared in its
    /// <see cref="ChildContent"/>. Children inherit the surrounding path as if the group weren't
    /// there. Mirrors SvelteKit's <c>(group)</c> directories and TanStack Router's pathless layout
    /// routes. A group must not declare a <see cref="Path"/>.
    /// </summary>
    [Parameter] public bool Group { get; set; }

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
    /// Async leave guard: runs when a navigation would deactivate this route (it is part of the
    /// currently rendered chain but not of the new one), before <c>OnNavigating</c> and any enter
    /// <see cref="Guard"/>s, leaf to root. <c>ctx.Cancel()</c> / <c>ctx.Redirect(...)</c> are
    /// preventive - the URL never changes on a cancelled leave, enabling real "unsaved changes"
    /// prompts per route. A navigation that keeps this route matched (e.g. only a parameter or a
    /// descendant changed) does not fire it. During the leave call, <c>ctx.Route</c> is the route
    /// being left. Inspired by Vue Router's <c>beforeRouteLeave</c> and Angular's <c>CanDeactivate</c>.
    /// </summary>
    [Parameter] public Func<BrouterNavigationContext, ValueTask>? LeaveGuard { get; set; }

    /// <summary>
    /// Async data loader. Runs after the route matches and guards pass, before render.
    /// The result is exposed to the rendered content as an unnamed cascading <see cref="BrouterRouteData"/>
    /// (matched by type) with typed <c>Get&lt;T&gt;</c>/<c>TryGet&lt;T&gt;</c> accessors.
    /// In a nested chain, loaders run sequentially root -> leaf by default (each parent's
    /// loader completes before its child's starts); set <see cref="Brouter.ParallelLoaders"/>
    /// to run independent loaders concurrently instead.
    /// Inspired by React Router v6's <c>loader</c> and Angular's <c>Resolve</c>.
    /// </summary>
    [Parameter] public Func<BrouterNavigationContext, ValueTask<object?>>? Loader { get; set; }

    /// <summary>
    /// Optional metadata. Exposed to the rendered content as an unnamed cascading <see cref="BrouterRouteMeta"/>
    /// (matched by type) with typed <c>Get&lt;T&gt;</c>/<c>TryGet&lt;T&gt;</c> accessors.
    /// </summary>
    [Parameter] public object? Meta { get; set; }

    /// <summary>
    /// When <c>true</c>, this route's rendered content is kept mounted (hidden) after the user
    /// navigates away, so returning to it restores the exact component state - scroll inside
    /// widgets, form input, expanded panels - instead of recreating the component. The Angular
    /// <c>RouteReuseStrategy</c> / Vue <c>KeepAlive</c> idea, scoped per route. The preserved
    /// content lives inside a <c>&lt;div hidden&gt;</c> wrapper while inactive, so it costs memory
    /// and (hidden) DOM for as long as its hosting layout stays mounted; state survives sibling
    /// navigations under the same layout, not the layout's own unmount. Opt-in per route.
    /// </summary>
    [Parameter] public bool KeepAlive { get; set; }

    /// <summary>
    /// Retained-instance budget for this <see cref="KeepAlive"/> route. At the default of 1 (or when
    /// unset and <see cref="BrouterOptions.DefaultKeepAliveMax"/> is 1) the route keeps a single live
    /// instance that re-binds when its parameter values change - state carries <em>across</em>
    /// parameter changes. Above 1, instances are kept <em>per matched parameter values</em>: visiting
    /// <c>/item/1</c> then <c>/item/2</c> mounts two separate instances, and returning to
    /// <c>/item/1</c> resumes its exact state. When the budget is exceeded the least-recently-used
    /// hidden instance is evicted (disposed). The key is built from the route's template parameter
    /// values only - the query string is deliberately not part of it, so <c>?tab=2</c> variations
    /// share one instance. No effect unless <see cref="KeepAlive"/> is set; values below 1 act as 1.
    /// </summary>
    [Parameter] public int? KeepAliveMax { get; set; }

    /// <summary>
    /// Freshness window for this route's <see cref="Loader"/> result, enabling the router's
    /// stale-while-revalidate cache: a navigation (or Back/Forward) to a URL whose cached result is
    /// younger than this skips the loader entirely; an older-but-not-garbage-collected result is
    /// served per <see cref="BrouterOptions.StaleReloadMode"/> (rendered immediately with a
    /// background refresh by default). Null (the default) falls back to
    /// <see cref="BrouterOptions.DefaultLoaderStaleTime"/>, which itself defaults to no caching.
    /// Cache entries key on the full URL (path + query), so different parameters cache separately.
    /// Inspired by TanStack Router's <c>staleTime</c>.
    /// </summary>
    [Parameter] public TimeSpan? StaleTime { get; set; }

    /// <summary>
    /// Error UI for this route. When a commit-phase failure occurs (typically a <see cref="Loader"/>
    /// in the matched chain throwing), the nearest <c>ErrorContent</c> - walking from the failed
    /// route up through its ancestors - renders in place of the routed content, while ancestor
    /// layouts above the boundary keep rendering normally. The fragment receives a
    /// <see cref="BrouterErrorContext"/> with the exception, the location, and a
    /// <c>RetryAsync()</c> that re-runs the navigation. When no route in the chain declares one,
    /// the failure bubbles to <see cref="Brouter.ErrorContent"/>. The global
    /// <see cref="IBrouter.OnError"/> hook fires either way.
    /// </summary>
    [Parameter] public RenderFragment<BrouterErrorContext>? ErrorContent { get; set; }

    /// <summary>
    /// When <c>true</c>, the matched route parameters (and query-string values) are bound to the
    /// rendered <see cref="Component"/>'s conventional <c>[Parameter]</c> properties <em>by name</em>,
    /// Blazor-style, in addition to any <c>[BrouterParameter]</c>/<c>[BrouterQuery]</c> annotated
    /// properties. This is what makes plain <c>@page</c> components (which bind route values to
    /// <c>[Parameter]</c> properties, and query values via <c>[SupplyParameterFromQuery]</c>) render
    /// correctly. It is enabled automatically for attribute-discovered routes
    /// (see <see cref="Brouter.AppAssembly"/> / <see cref="Brouter.AdditionalAssemblies"/>).
    /// Defaults to <c>false</c> so existing <c>[BrouterParameter]</c>-only components are unaffected.
    /// </summary>
    [Parameter] public bool BindComponentParametersByName { get; set; }

    /// <summary>Child routes (used for nesting).</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }


    [CascadingParameter(Name = "Brouter")] internal Brouter? Brouter { get; set; }
    [CascadingParameter(Name = "ParentRoute")] internal Broute? Parent { get; set; }
    [CascadingParameter(Name = "RouteParameters")] internal BrouterRouteParameters? InheritedParameters { get; set; }
    // True for the synthetic Broutes Brouter emits for attribute-discovered (@page) routes; the
    // discovered region is wrapped in a fixed cascading value (see Brouter.BuildRenderTree).
    // RegisterRoute's ambiguity check exempts a hand-declared/discovered pair with the same
    // template - that's the documented "hand-declared routes win ties over discovered ones"
    // override pattern - and only rejects same-kind duplicates.
    [CascadingParameter(Name = "IsDiscoveredRoute")] internal bool IsDiscovered { get; set; }


    internal string FullTemplate { get; private set; } = string.Empty;


    private readonly List<Broute> _children = [];
    internal void AddChild(Broute route) => _children.Add(route);
    internal void RemoveChild(Broute route) => _children.Remove(route);

    // The <BrouterOutlet>s declared inside this route's Content, keyed by outlet name ("" is the
    // primary outlet, which hosts the matched child's Content/Component; named outlets host the
    // child's <BrouterView Name=...> fragments). Same single-dispatcher discipline as everything
    // else on this type.
    internal Dictionary<string, BrouterOutlet> Outlets { get; } = new(StringComparer.Ordinal);

    internal bool HasPrimaryOutlet => Outlets.ContainsKey(string.Empty);

    internal void RegisterOutlet(string name, BrouterOutlet outlet) => Outlets[name] = outlet;

    internal void UnregisterOutlet(string name, BrouterOutlet outlet)
    {
        // Only detach when the slot still points at this instance; a newer outlet (recreated on
        // re-render) may already have taken the name over.
        if (Outlets.TryGetValue(name, out var existing) && ReferenceEquals(existing, outlet))
        {
            Outlets.Remove(name);
        }
    }

    /// <summary>
    /// Hands the matched child (and its merged parameters) to every outlet this route hosts:
    /// the primary outlet renders the child's content, named outlets render the child's
    /// corresponding <see cref="BrouterView"/> fragments. Called from the child's renderer on
    /// every render pass, mirroring the old single-outlet Render call.
    /// </summary>
    internal void SetOutletChild(Broute child, BrouterRouteParameters parameters)
    {
        foreach (var outlet in Outlets.Values)
        {
            outlet.Render(child, parameters);
        }
    }

    // Named view fragments declared by <BrouterView> children of this route, rendered by the
    // parent's same-named outlets when this route is matched (Vue named-views style). Null until
    // the first view registers.
    internal Dictionary<string, RenderFragment<BrouterRouteParameters>>? NamedViews { get; private set; }

    internal void SetNamedView(string name, RenderFragment<BrouterRouteParameters>? fragment)
    {
        if (fragment is null)
        {
            NamedViews?.Remove(name);
        }
        else
        {
            (NamedViews ??= new Dictionary<string, RenderFragment<BrouterRouteParameters>>(StringComparer.Ordinal))[name] = fragment;
        }

        // The fragments render inside the PARENT's outlets; nudge them so view content updated by
        // a host re-render (a new fragment instance) actually reaches the screen.
        if (Parent is null) return;
        foreach (var outlet in Parent.Outlets.Values)
        {
            outlet.Refresh();
        }
    }

    internal BrouterRouteTemplate? RouteTemplate { get; private set; }

    // The canonical-template key this route was registered under in Brouter's ambiguity dictionary
    // (see Brouter.RegisterRoute). Stored so UnregisterRoute removes exactly the key that was added,
    // even if Options.CaseSensitive flips between registration and disposal.
    internal string? TemplateCollisionKey { get; set; }
    // Tightened from IDictionary to IReadOnlyDictionary: callers only ever read these and the
    // pipeline replaces them wholesale on a match commit. Exposing the mutable interface let
    // any internal caller .Add/.Remove/.Clear them mid-render which would be a footgun against
    // a route that's still part of an actively-rendering matched chain.
    internal IReadOnlyDictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();
    internal IReadOnlyDictionary<string, string[]> ConstraintsByParameter { get; set; } = new Dictionary<string, string[]>();
    internal object? LoadedData { get; set; }

    // Set by Brouter.RenderNavigationError when this route is the nearest error boundary for a
    // failed navigation; the renderer then emits ErrorContent instead of Content/Component.
    // Only ever set on routes whose ErrorContent is non-null; cleared at the start of every
    // navigation alongside Matched.
    internal BrouterErrorContext? CurrentError { get; set; }

    private BrouterRouteRenderer? _renderer;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (Brouter is null)
            throw new InvalidOperationException("A Route must be nested inside a Brouter.");

        if (Group && string.IsNullOrWhiteSpace(Path) is false)
            throw new InvalidOperationException(
                "A Group route must not declare a Path: it contributes no URL segments. " +
                "Put the path on its child routes (or remove Group).");

        if (Group is false && Parent is null && string.IsNullOrWhiteSpace(Path))
            throw new InvalidOperationException("A root-level Route must have a non-empty Path. " +
                "Only nested (child) routes may use an empty path to act as an index route, and " +
                "pathless grouping requires the Group flag.");

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

        // Resolve constraints against this Brouter's DI-container-scoped registry (custom constraints
        // registered via BrouterOptions.Constraints), falling back to the built-in constraints only.
        // See BrouterConstraintRegistry.Create (custom-then-built-in) and BrouterTemplateParser.ParseTemplate.
        // Brouter is non-null here (checked above).
        RouteTemplate = BrouterTemplateParser.ParseTemplate(FullTemplate, Brouter.Options.Constraints);

        // Precompute Specificity / Depth / IsIndex once. These are stable for the lifetime
        // of the route (template and parent chain don't change after registration), so the
        // matching loop and the winner-selection in Brouter.ProcessNavigationAsync can read
        // them as plain field accesses instead of recomputing on every navigation.
        var specificity = 0;
        foreach (var seg in RouteTemplate.TemplateSegments) specificity += seg.Specificity;
        Specificity = specificity;

        // Group ancestors are invisible in the URL, so they must be invisible to the depth
        // tiebreak too - otherwise wrapping a route in a group would silently change how it
        // wins/loses ties against an identically-templated sibling.
        var depth = 0;
        for (var p = Parent; p is not null; p = p.Parent)
        {
            if (p.Group is false) depth++;
        }
        Depth = depth;

        IsIndex = Group is false && Parent is not null && string.IsNullOrEmpty(Path.Trim('/'));

        // Precompute the set of parameter names declared in this route's template. Used only by the
        // conventional (by-name) component binding path to decide which [Parameter] properties on the
        // rendered Component correspond to an actual route parameter - so unrelated component parameters
        // are left untouched rather than forced to their default on every render.
        var templateParamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var seg in RouteTemplate.TemplateSegments)
        {
            if (seg.IsParameter) templateParamNames.Add(seg.Value);
        }
        TemplateParameterNames = templateParamNames;

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

    /// <summary>
    /// The parameter names declared in this route's template (case-insensitive). Cached at construction
    /// and consumed by the conventional by-name component binding (<see cref="BindComponentParametersByName"/>).
    /// </summary>
    internal IReadOnlySet<string>? TemplateParameterNames { get; private set; }


    internal bool Matched { get; set; }

    // True once this route has been matched at least once; with KeepAlive it gates "there is
    // content worth keeping mounted while unmatched".
    internal bool HasEverMatched { get; private set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);
        _renderer?.BuildRenderTree(builder, Matched);
    }

    internal void SetMatched()
    {
        HasEverMatched = true;
        // Mark the whole ancestor chain matched, then issue a single render request at the
        // topmost node. Re-rendering the top of the chain re-renders every descendant Broute:
        // a Broute is always declared inside some render region of its parent (that's how the
        // ParentRoute cascade reaches it), and its render-relevant parameters (Content /
        // ChildContent fragments, the Component Type) are reference types Blazor's change
        // detection always treats as maybe-changed, so the subtree diff descends through every
        // level - the same mechanism the pipeline's final StateHasChanged relies on to unrender
        // the routes that lost the match. Calling StateHasChanged per ancestor (as this used
        // to) queued one redundant render request per level of nesting for work the root's
        // single request already covers.
        Matched = true;

        if (Parent is null)
        {
            StateHasChanged();
        }
        else
        {
            Parent.SetMatched();
        }
    }

    // The resolved retained-instance budget: per-route KeepAliveMax, else the global default.
    // Clamped to >= 1 so a misconfigured 0/negative value degrades to the singleton behavior
    // instead of rendering nothing.
    internal int EffectiveKeepAliveMax => Math.Max(1, KeepAliveMax ?? Brouter?.Options.DefaultKeepAliveMax ?? 1);

    /// <summary>
    /// Builds the retention key for the current match of a per-parameter keep-alive route
    /// (<see cref="EffectiveKeepAliveMax"/> &gt; 1): the route's template parameter values, in
    /// template order, formatted invariantly. In singleton mode the key is constant (empty) so every
    /// match reuses the one retained instance - the pre-existing re-binding behavior. The query
    /// string is deliberately excluded (documented on <see cref="KeepAliveMax"/>).
    /// </summary>
    internal string ComputeKeepAliveKey()
    {
        if (EffectiveKeepAliveMax <= 1 || RouteTemplate is null) return string.Empty;

        var sb = new System.Text.StringBuilder();
        foreach (var seg in RouteTemplate.TemplateSegments)
        {
            if (seg.IsParameter is false) continue;
            Parameters.TryGetValue(seg.Value, out var value);
            // U+001F (unit separator) can't appear in a template's parameter name, so the
            // key is unambiguous even when values themselves contain '=' or '/'.
            sb.Append(seg.Value).Append('=').Append(BrouterService.FormatRouteValue(value)).Append('\u001f');
        }
        return sb.ToString();
    }

    // Releases this route's retained keep-alive state - both its own inline hidden content (when it
    // is a kept-but-hidden top-level/inline route, or hidden per-parameter siblings of the active
    // instance) and any kept children held by the outlets it hosts. The currently active instance is
    // left untouched. Backs IBrouter.ClearKeepAlive.
    internal void ClearKeepAlive()
    {
        if (_renderer is not null && KeepAlive && HasEverMatched)
        {
            _renderer.DropKeptContent(Matched);
            StateHasChanged();
        }

        foreach (var outlet in Outlets.Values)
        {
            outlet.ClearKeepAlive();
        }
    }

    internal async ValueTask<bool> InvokeGuardsAsync(BrouterNavigationContext ctx)
    {
        // Walk from root to leaf so parents authorize children, mirroring Angular's hierarchical guards.
        var chain = new List<Broute>();
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

        // Drop any kept-alive render entry the parent's outlets hold for this route, so a disposed
        // (conditionally removed) route can't linger as hidden content.
        if (Parent is not null)
        {
            foreach (var outlet in Parent.Outlets.Values)
            {
                outlet.ForgetChild(this);
            }
        }
    }

    private bool _disposed;
}
