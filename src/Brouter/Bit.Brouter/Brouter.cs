using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Brouter;

/// <summary>
/// The root component of Bit.Brouter. Hosts a tree of <see cref="Broute"/> children and renders
/// the matching one for the current URL.
/// </summary>
public class Brouter : ComponentBase, IDisposable, IAsyncDisposable
{
    private static readonly char[] _separators = ['/'];


    /// <summary>The route declarations and any other markup.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Alias for <see cref="ChildContent"/>. When another template (<see cref="Found"/>,
    /// <see cref="NotFound"/>, <see cref="Navigating"/>, ...) forces the child fragments to be
    /// spelled out explicitly, <c>&lt;Routes&gt;</c> states the intent better than
    /// <c>&lt;ChildContent&gt;</c>. Set one or the other, not both.
    /// </summary>
    [Parameter] public RenderFragment? Routes { get; set; }

    /// <summary>
    /// When set, every matched route that renders a <see cref="Broute.Component"/> (attribute-discovered
    /// <c>@page</c> routes and hand-declared <c>Component=</c> routes alike) renders through this template
    /// instead of Brouter instantiating the component itself. The fragment receives a real framework
    /// <see cref="RouteData"/> - the matched page type plus the route parameter values - which is exactly
    /// what the built-in <c>RouteView</c>, <c>AuthorizeRouteView</c> and <c>FocusOnNavigate</c> consume.
    /// This makes the built-in Router's <c>&lt;Found Context="routeData"&gt;</c> block port over unchanged:
    /// <c>[Authorize]</c> handling, <c>Authorizing</c>/<c>NotAuthorized</c> UI, per-page <c>@layout</c>
    /// resolution and focus-on-navigate all keep working through those framework components, with no
    /// Brouter-specific reimplementation. The template is responsible for rendering the page (typically by
    /// ending in a <c>RouteView</c>/<c>AuthorizeRouteView</c> bound to the supplied route data); guards,
    /// loaders, keep-alive and error boundaries still run around it as usual. Routes that render a
    /// <see cref="Broute.Content"/> fragment ignore this template - they have no page type to expose -
    /// and so do child routes rendered into a <see cref="BrouterOutlet"/>: nested-outlet layouts are
    /// Brouter's own layout model, and a Found template that resolves per-page <c>@layout</c>s inside
    /// an outlet-hosted layout would apply two layout systems at once. Mirrors <c>Router.Found</c>.
    /// For the common case you don't need this template at all: set <see cref="NotAuthorized"/> /
    /// <see cref="Authorizing"/> / <see cref="DefaultLayout"/> instead and Brouter composes the
    /// standard <c>AuthorizeRouteView</c>/<c>RouteView</c> rendering itself; an explicitly set Found
    /// always wins over that composition.
    /// </summary>
    [Parameter] public RenderFragment<RouteData>? Found { get; set; }

    /// <summary>
    /// Displayed when the user is not authorized for the matched <c>[Authorize]</c> page, receiving
    /// the current <c>AuthenticationState</c>. Setting this (or <see cref="Authorizing"/> /
    /// <see cref="Resource"/>) routes every Component-rendering route through the framework's own
    /// <c>AuthorizeRouteView</c> - so policy/role evaluation, per-page <c>@layout</c> +
    /// <see cref="DefaultLayout"/> resolution and reaction to live authentication-state changes are
    /// all the framework's implementation, with no routing template in the app. Pages without
    /// <c>[Authorize]</c> render normally through the same path. Like <c>AuthorizeRouteView</c>
    /// itself, this requires the cascading <c>Task&lt;AuthenticationState&gt;</c>
    /// (<c>CascadingAuthenticationState</c> / <c>AddCascadingAuthenticationState()</c>) and
    /// <c>AddAuthorizationCore()</c>. When null, the framework's default "Not authorized" content
    /// is shown. Ignored when <see cref="Found"/> is set.
    /// </summary>
    [Parameter] public RenderFragment<AuthenticationState>? NotAuthorized { get; set; }

    /// <summary>
    /// Displayed while the framework determines whether the user is authorized for an
    /// <c>[Authorize]</c> page (e.g. an async <c>AuthenticationStateProvider</c> is still
    /// resolving). Enables the same <c>AuthorizeRouteView</c> composition as
    /// <see cref="NotAuthorized"/>. When null, the framework's default "Authorizing..." content is
    /// shown. Ignored when <see cref="Found"/> is set.
    /// </summary>
    [Parameter] public RenderFragment? Authorizing { get; set; }

    /// <summary>
    /// The layout for matched pages that don't declare their own <c>@layout</c> - a page-level
    /// <c>@layout</c> (and its nested layout chain) always wins, exactly like the built-in
    /// <c>RouteView.DefaultLayout</c>. On its own it routes Component-rendering routes through the
    /// framework's <c>RouteView</c>; combined with <see cref="NotAuthorized"/> /
    /// <see cref="Authorizing"/> / <see cref="Resource"/> it flows to their
    /// <c>AuthorizeRouteView</c> composition instead. Ignored when <see cref="Found"/> is set.
    /// </summary>
    [Parameter, DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type? DefaultLayout { get; set; }

    /// <summary>
    /// Optional resource passed to the authorization service for resource-based policy evaluation,
    /// forwarded to <c>AuthorizeRouteView.Resource</c>. Enables the same <c>AuthorizeRouteView</c>
    /// composition as <see cref="NotAuthorized"/>. Ignored when <see cref="Found"/> is set.
    /// </summary>
    [Parameter] public object? Resource { get; set; }

    /// <summary>
    /// URL to navigate to when no route matches. If null, no redirect happens and
    /// <see cref="NotFound"/> (if any) is rendered in place.
    /// </summary>
    [Parameter] public string? NotFoundUrl { get; set; }

    /// <summary>Inline content to render when no route matches and <see cref="NotFoundUrl"/> is null.</summary>
    [Parameter] public RenderFragment<BrouterLocation>? NotFound { get; set; }

    /// <summary>
    /// Router-level error UI: the fallback boundary for commit-phase navigation failures (typically
    /// a route <see cref="Broute.Loader"/> throwing) when no route in the failed chain declares its
    /// own <see cref="Broute.ErrorContent"/>. Rendered in place of the routed content with a
    /// <see cref="BrouterErrorContext"/> (exception, location, <c>RetryAsync()</c>). When neither
    /// this nor any route boundary exists, failures keep the previous behavior: the old page stays
    /// visible and only the <see cref="IBrouter.OnError"/> hook observes the error.
    /// </summary>
    [Parameter] public RenderFragment<BrouterErrorContext>? ErrorContent { get; set; }

    /// <summary>
    /// Optional "pending navigation" UI, shown while a navigation is awaiting its route
    /// <see cref="Broute.Loader"/>s. Mirrors the built-in <c>Router.Navigating</c>: when a matched
    /// route (or one of its ancestors in the matched chain) has a slow loader, this fragment is
    /// rendered in place of the routed content until the loaders finish, then the matched route is
    /// revealed. It is shown lazily - only once a loader is actually about to run - so navigations
    /// with no loaders (or whose loader results were restored from prerender state) never flash it,
    /// and instant navigations don't flicker. Left null (the default), the previous page simply
    /// stays visible until the new route is ready.
    /// </summary>
    [Parameter] public RenderFragment? Navigating { get; set; }

    /// <summary>
    /// When true, the <see cref="Broute.Loader"/>s of a matched route chain run concurrently
    /// instead of one at a time. By default loaders run sequentially root -> leaf (mirroring
    /// guard order), so a child's loader can rely on work its parent's loader completed (e.g.
    /// state stashed in a scoped service) - but the total wait is the sum of every loader in
    /// the chain. When the loaders are independent (the common case), enable this to start
    /// them together so the wait is only as long as the slowest one, like React Router.
    /// Results are still committed and errors still surfaced in root -> leaf order, so render
    /// and failure behavior are unchanged; only the awaiting overlaps. Leave this off if any
    /// child loader depends on its parent's loader having finished.
    /// </summary>
    [Parameter] public bool ParallelLoaders { get; set; }

    /// <summary>Async hook fired whenever a route is successfully matched.</summary>
    [Parameter] public Func<Broute, ValueTask>? OnMatch { get; set; }

    /// <summary>Async hook fired when no route matches the current URL.</summary>
    [Parameter] public Func<BrouterLocation, ValueTask>? OnNotFound { get; set; }

    /// <summary>
    /// The assembly to scan for attribute-routed components (<c>@page</c> / <c>[Route]</c>). Discovered
    /// routes are matched alongside any hand-declared <see cref="Broute"/> children, so pages can live
    /// colocated with their route templates instead of being enumerated in one tree. Mirrors
    /// <c>Router.AppAssembly</c>. When null, no assembly scanning happens.
    /// </summary>
    [Parameter] public Assembly? AppAssembly { get; set; }

    /// <summary>
    /// Additional assemblies to scan for attribute-routed components, e.g. Razor class libraries or
    /// lazily-loaded assemblies. Add to this collection (with a new instance/re-render) as assemblies are
    /// loaded to register their routes at runtime. Mirrors <c>Router.AdditionalAssemblies</c>.
    /// </summary>
    [Parameter] public IEnumerable<Assembly>? AdditionalAssemblies { get; set; }

    /// <summary>
    /// Async per-navigation hook that runs before route matching, for loading route assemblies on
    /// demand (the Brouter counterpart of the built-in <c>Router.OnNavigateAsync</c> +
    /// <c>LazyAssemblyLoader</c> pattern): inspect <c>ctx.To</c>, load what the target needs (e.g.
    /// <c>LazyAssemblyLoader.LoadAssembliesAsync</c> on WebAssembly), and return the loaded
    /// assemblies - their <c>@page</c>/<c>[Route]</c> components are scanned and registered
    /// <em>within the same navigation</em>, so the URL being navigated to can match a page whose
    /// assembly loaded moments ago. Return null (or already-known assemblies) for no-op. Runs in
    /// the preventive phase (a slow load keeps the current page visible; combine with
    /// <see cref="Navigating"/> for pending UI); observe <c>ctx.CancellationToken</c> for
    /// supersession. A thrown exception fails the navigation closed and surfaces via OnError.
    /// </summary>
    [Parameter] public Func<BrouterNavigationContext, ValueTask<IEnumerable<Assembly>?>>? OnNavigateAsync { get; set; }


    [Inject] private NavigationManager _navManager { get; set; } = default!;
    [Inject] private INavigationInterception _navInterception { get; set; } = default!;
    [Inject] private BrouterService _brouterService { get; set; } = default!;
    [Inject] private IServiceProvider _services { get; set; } = default!;


    internal BrouterLocation CurrentLocation { get; private set; } = BrouterLocation.Empty;
    internal BrouterOptions Options => _brouterService.Options;


    // Cached delegates for the built-in Found compositions below. Method-group conversion allocates
    // a fresh delegate per use, and EffectiveFound is read on every render of every matched route,
    // so the delegate is minted once and reused. The composing methods read the live parameter
    // values (NotAuthorized, DefaultLayout, ...) at render time, so caching the delegate never
    // captures stale configuration.
    private RenderFragment<RouteData>? _authorizeRouteViewFound;
    private RenderFragment<RouteData>? _routeViewFound;

    /// <summary>
    /// The Found template in effect for Component-rendering routes. An explicit <see cref="Found"/>
    /// parameter always wins; otherwise setting any of <see cref="NotAuthorized"/> /
    /// <see cref="Authorizing"/> / <see cref="Resource"/> composes the framework's
    /// <c>AuthorizeRouteView</c>, a lone <see cref="DefaultLayout"/> composes the framework's
    /// <c>RouteView</c> (behind Brouter's own fail-closed <c>[Authorize]</c> guard - see
    /// <see cref="RenderPageWithLayout"/>), and with nothing set Brouter renders components
    /// natively (null).
    /// </summary>
    internal RenderFragment<RouteData>? EffectiveFound
    {
        get
        {
            if (Found is not null) return Found;

            // AuthorizeRouteView only when an auth-related parameter opted in: it unconditionally
            // requires the cascading Task<AuthenticationState>, so composing it for every app would
            // break auth-less hosts.
            if (NotAuthorized is not null || Authorizing is not null || Resource is not null)
            {
                return _authorizeRouteViewFound ??= RenderPageWithAuthorization;
            }

            if (DefaultLayout is not null)
            {
                return _routeViewFound ??= RenderPageWithLayout;
            }

            return null;
        }
    }

    // The built-in authorization composition: the framework AuthorizeRouteView bound to the
    // RouteData the router built for the matched page. Authorization correctness (policy/role
    // evaluation, Authorizing/NotAuthorized states, layout resolution, live auth-state changes)
    // deliberately stays the framework's implementation - Brouter only composes it. Null fragments
    // are passed through; AuthorizeRouteView substitutes its own defaults.
    private RenderFragment RenderPageWithAuthorization(RouteData routeData) => builder =>
    {
        builder.OpenComponent<AuthorizeRouteView>(0);
        builder.AddAttribute(1, nameof(AuthorizeRouteView.RouteData), routeData);
        builder.AddAttribute(2, nameof(AuthorizeRouteView.DefaultLayout), DefaultLayout);
        builder.AddAttribute(3, nameof(AuthorizeRouteView.NotAuthorized), NotAuthorized);
        builder.AddAttribute(4, nameof(AuthorizeRouteView.Authorizing), Authorizing);
        builder.AddAttribute(5, nameof(AuthorizeRouteView.Resource), Resource);
        builder.CloseComponent();
    };

    // The layout-only composition: the framework RouteView, which resolves per-page @layout chains
    // with DefaultLayout as the fallback. RouteView performs no authorization check whatsoever
    // (the Components assembly doesn't even reference the authorization package), so Brouter
    // applies its own fail-closed guard first - a layout-only configuration must never silently
    // render an [Authorize] page.
    private RenderFragment RenderPageWithLayout(RouteData routeData) => builder =>
    {
        BrouterRouteRenderer.EnsureNoAuthorizationRequirements(routeData.PageType);

        builder.OpenComponent<RouteView>(0);
        builder.AddAttribute(1, nameof(RouteView.RouteData), routeData);
        builder.AddAttribute(2, nameof(RouteView.DefaultLayout), DefaultLayout);
        builder.CloseComponent();
    };


    // The framework RouteData of the currently committed navigation: the matched page type plus its
    // route parameter values, or null when nothing (or a Content-fragment route, which has no page
    // type) is committed. Cascaded unnamed-by-type from BuildRenderTree as an observer channel, so
    // components anywhere under the Brouter (route-data publishers, breadcrumbs, telemetry) can take
    // a [CascadingParameter] RouteData? without threading it through a Found template. Written only
    // by the navigation pipeline on the renderer's dispatcher, like the rest of the nav-state fields.
    private RouteData? _currentRouteData;


    // Routes discovered by scanning AppAssembly / AdditionalAssemblies for [Route]/@page components.
    // Rendered as synthetic <Broute> children in BuildRenderTree so they reuse the whole matching /
    // guard / loader / render pipeline. Recomputed only when the assembly set actually changes.
    private IReadOnlyList<BrouteScanner.DiscoveredRoute> _discoveredRoutes = [];
    private Assembly? _lastAppAssembly;
    private Assembly[]? _lastAdditionalAssemblies;
    private bool _discoveryComputed;

    // Assemblies handed back by the OnNavigateAsync hook at runtime (lazy loading). Merged into
    // every discovery scan alongside AppAssembly/AdditionalAssemblies; grows monotonically.
    private readonly List<Assembly> _runtimeAssemblies = [];

    // Prerender -> interactive loader-state bridge (see BroutePrerenderState). Only active when
    // Options.PersistLoaderState is set and a PersistentComponentState is available in the scope.
    // _loaderStateJsonOptions is non-null only when the consumer supplied a source-generated
    // resolver (Options.LoaderStateTypeInfoResolver) for trimming/AOT-safe serialization.
    private System.Text.Json.JsonSerializerOptions? _loaderStateJsonOptions;
    private PersistentComponentState? _persistentState;
    private PersistingComponentStateSubscription _persistSubscription;
    private bool _persistSubscribed;
    // Loader results staged during the current navigation's commit, keyed by their persistence key.
    // Serialized by the RegisterOnPersisting callback at the end of prerender.
    private readonly Dictionary<string, object?> _loaderStateToPersist = new(StringComparer.Ordinal);

    private readonly List<Broute> _routes = [];
    // Names of the currently-registered routes, for O(1) uniqueness enforcement in RegisterRoute
    // (a linear scan there made startup O(n^2)). Case-insensitive to match FindRouteByName's lookup
    // comparison, so a name that collides on lookup also collides on registration. Kept in lockstep
    // with _routes: every named route added/removed updates this set on the same single dispatcher,
    // so no synchronization is needed (see the threading note above).
    private readonly HashSet<string> _routeNames = new(StringComparer.OrdinalIgnoreCase);
    // Canonical-template -> registered holders, for O(1) ambiguity detection in RegisterRoute. Two
    // routes map to the same key exactly when winner selection could only ever tell them apart by
    // registration order (see BuildTemplateCollisionKey), which is a silent coin-flip we refuse
    // instead - mirroring the built-in router's AmbiguousMatchException for duplicate templates.
    // The one sanctioned cohabitation is a hand-declared route shadowing an attribute-discovered
    // one (the documented override pattern; the hand-declared route registers first and wins the
    // order tie), so a key holds at most one route of each kind - the list never exceeds two.
    // Ordinal comparer: all case normalization is baked into the key itself so a CaseSensitive
    // flip can't corrupt lookups.
    private readonly Dictionary<string, List<Broute>> _routesByTemplateKey = new(StringComparer.Ordinal);
    // Snapshot of _routes refreshed lazily after Register/Unregister. The matching loop
    // iterates this snapshot so we don't allocate a fresh array on every navigation.
    //
    // These fields are deliberately plain (not volatile) and accessed without any
    // synchronization: safety comes entirely from every reader and writer running on the
    // renderer's single-threaded dispatcher. RegisterRoute/UnregisterRoute are called from
    // Broute component lifecycle (OnInitialized/Dispose), and GetRoutesSnapshot/SelectWinner/
    // FindRouteByName run inside the navigation pipeline (ProcessNavigationAsync / the
    // changing handler) - all on that same dispatcher. There is therefore no cross-thread
    // publication to order, so no volatile/Interlocked is needed. If any of these were ever
    // called off-dispatcher this reasoning breaks and the access would need real synchronization.
    private Broute[] _routesSnapshot = [];
    private bool _routesDirty = true;
    internal void RegisterRoute(Broute route)
    {
        // Reject templates that would be ambiguous with an already-registered route. A hand-declared /
        // attribute-discovered pair is exempt: that's the documented override pattern (the hand-declared
        // route wins the order tie), not a duplication bug. This is a pure lookup (nothing is mutated
        // before a throw), so a rejected route leaves every set untouched.
        string? templateKey = null;
        List<Broute>? templateHolders = null;
        // Group routes never match by themselves (GetRouteIndex excludes them), so they can't be
        // ambiguous with anything - two sibling groups sharing a template is the normal case.
        if (route.Group is false && route.RouteTemplate is not null)
        {
            templateKey = BuildTemplateCollisionKey(route);
            if (_routesByTemplateKey.TryGetValue(templateKey, out templateHolders))
            {
                foreach (var existing in templateHolders)
                {
                    if (existing.IsDiscovered != route.IsDiscovered) continue;

                    var existingDescription = existing.Component is null
                        ? $"'{existing.FullTemplate}'"
                        : $"'{existing.FullTemplate}' (component '{existing.Component.FullName}')";
                    throw new InvalidOperationException(
                        $"The route template '{route.FullTemplate}' is ambiguous with the already registered template " +
                        $"{existingDescription}: both match exactly the same URLs, so the winner would be decided " +
                        "by registration order alone. Remove or change one of the routes. Note that parameter names " +
                        "(and, under case-insensitive matching, letter casing) do not distinguish templates. If you are " +
                        "swapping two same-template <Broute>s in a single render (e.g. an @if/else), the new one " +
                        "initializes before the old one is disposed - keep a single <Broute> rendered and vary its " +
                        "parameters instead.");
                }
            }
        }

        // Enforce the documented uniqueness contract for Route.Name. The name set uses the same
        // case-insensitive comparison as FindRouteByName, so name lookups stay unambiguous. Adding to
        // the set is the uniqueness check: a failed Add means an equal name is already registered.
        // O(1) per route, keeping startup registration linear rather than O(n^2).
        var named = string.IsNullOrEmpty(route.Name) is false;
        if (named && _routeNames.Add(route.Name!) is false)
        {
            throw new InvalidOperationException(
                $"A route with the name '{route.Name}' is already registered. Route names must be unique (case-insensitive).");
        }

        if (templateKey is not null)
        {
            if (templateHolders is null)
            {
                templateHolders = [];
                _routesByTemplateKey.Add(templateKey, templateHolders);
            }
            templateHolders.Add(route);
            route.TemplateCollisionKey = templateKey;
        }
        _routes.Add(route);
        _routesDirty = true;
    }
    internal void UnregisterRoute(Broute route)
    {
        if (_routes.Remove(route) is false) return;
        // Prune the committed-chain record so leave guards never run against a disposed route
        // (e.g. a conditionally-rendered <Broute> that was removed while its content was shown).
        if (Array.IndexOf(_committedChain, route) >= 0)
        {
            _committedChain = _committedChain.Where(r => ReferenceEquals(r, route) is false).ToArray();
        }
        // Keep the name and template sets in lockstep so the freed name/template can be re-registered
        // later. The template key is removed by the exact string it was registered under, not
        // recomputed, so an Options.CaseSensitive flip in between can't strand a stale entry.
        if (string.IsNullOrEmpty(route.Name) is false) _routeNames.Remove(route.Name);
        if (route.TemplateCollisionKey is not null)
        {
            if (_routesByTemplateKey.TryGetValue(route.TemplateCollisionKey, out var holders))
            {
                holders.Remove(route);
                if (holders.Count == 0) _routesByTemplateKey.Remove(route.TemplateCollisionKey);
            }
            route.TemplateCollisionKey = null;
        }
        _routesDirty = true;
    }

    /// <summary>
    /// Builds the canonical identity of a route's matching behavior, used to detect ambiguous
    /// registrations. Two routes get the same key exactly when <see cref="ConsiderCandidate"/>
    /// could only ever break a tie between them by registration order: they match the same URLs
    /// with the same specificity, and share the depth / index-route tiebreak inputs.
    /// </summary>
    /// <remarks>
    /// Deliberate normalizations, matching what <see cref="TryMatch"/> actually distinguishes:
    /// parameter names are dropped ("/users/{id}" and "/users/{userId}" match identically), the
    /// literal catch-all "**" and a catch-all parameter "{**rest}" unify (a catch-all's optional
    /// flag is also irrelevant - it already matches zero segments), literal casing folds when
    /// matching is case-insensitive, and constraint tokens fold case to mirror the case-insensitive
    /// constraint registry. Depth and index-ness are part of the key because identical templates at
    /// different depths (a parent and its index child) are resolved deterministically by the
    /// documented depth/index tiebreaks - only a full tie is ambiguous. Constraint order is kept:
    /// the last constraint's conversion wins, so reordered constraints are behaviorally distinct.
    /// </remarks>
    private string BuildTemplateCollisionKey(Broute route)
    {
        var sb = new StringBuilder();
        sb.Append(route.Depth).Append(route.IsIndex ? "i|" : "-|");

        var caseSensitive = Options.CaseSensitive;
        foreach (var seg in route.RouteTemplate!.TemplateSegments)
        {
            sb.Append('/');
            if (seg.IsCatchAll)
            {
                sb.Append("**");
            }
            else if (seg.IsParameter)
            {
                sb.Append('{');
                for (var i = 0; i < seg.Constraints.Length; i++)
                {
                    if (i > 0) sb.Append(':');
                    sb.Append(seg.Constraints[i].Name.ToLowerInvariant());
                }
                if (seg.IsOptional) sb.Append('?');
                sb.Append('}');
            }
            else
            {
                // Covers plain literals and the single-segment wildcard "*" (its Value is "*", which
                // can't collide with a real literal: TemplateParser never produces a literal "*").
                sb.Append(caseSensitive ? seg.Value : seg.Value.ToLowerInvariant());
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Returns a snapshot of the registered routes. The array is reused across navigations
    /// while the registration set is stable; <see cref="RegisterRoute"/> /
    /// <see cref="UnregisterRoute"/> mark it dirty so the next call rebuilds it.
    /// </summary>
    /// <remarks>
    /// The returned array is treated as a read-only snapshot by callers. We never hand the
    /// underlying List itself out so a caller can't accidentally mutate the registration
    /// set mid-pipeline.
    /// </remarks>
    private Broute[] GetRoutesSnapshot()
    {
        if (_routesDirty is false) return _routesSnapshot;
        var arr = _routes.ToArray();
        _routesSnapshot = arr;
        _routesDirty = false;
        return arr;
    }

    // First-segment matching index, derived lazily from the route snapshot. Keeps navigation off the
    // O(routes) full scan: SelectWinner only has to try routes whose first template segment can match
    // the URL's first segment, plus the (usually small) set of routes that start with a parameter /
    // wildcard / catch-all / empty template. Rebuilt only when the snapshot reference changes (routes
    // registered/unregistered) or the case-sensitivity option flips, since the literal buckets key on
    // it. See GetRouteIndex.
    private RouteIndex? _routeIndex;
    private Broute[]? _indexedSnapshot;
    private bool _indexedCaseSensitive;

    // A route paired with its registration order (index in the snapshot). Order is carried alongside
    // the route so winner selection can break exact specificity/depth/index ties by earliest
    // declaration - reproducing the old "keep the first candidate on a tie" behavior even though the
    // index visits routes bucket-first rather than in pure registration order.
    private readonly struct RouteEntry
    {
        public Broute Route { get; }
        public int Order { get; }
        public RouteEntry(Broute route, int order)
        {
            Route = route;
            Order = order;
        }
    }

    // The precomputed first-segment index. LiteralBuckets maps a first literal segment (compared with
    // the same comparer the matcher uses for literals) to the routes that start with it. NonLiteralFirst
    // holds routes whose first segment isn't a fixed literal (parameter / '*' / '**' / catch-all) or
    // whose template is empty - all of which can match regardless of the URL's first segment, so they're
    // always considered.
    private sealed class RouteIndex
    {
        public Dictionary<string, List<RouteEntry>> LiteralBuckets { get; }
        public List<RouteEntry> NonLiteralFirst { get; }
        public RouteIndex(Dictionary<string, List<RouteEntry>> literalBuckets, List<RouteEntry> nonLiteralFirst)
        {
            LiteralBuckets = literalBuckets;
            NonLiteralFirst = nonLiteralFirst;
        }
    }

    /// <summary>
    /// Returns the first-segment matching index for the current route snapshot, rebuilding it only
    /// when the snapshot reference changes (a route registered/unregistered) or the case-sensitivity
    /// option flips. Shares the snapshot with <see cref="GetRoutesSnapshot"/> so the two never disagree
    /// about the route set, and runs on the same single dispatcher as every other reader (no locking).
    /// </summary>
    private RouteIndex GetRouteIndex()
    {
        var snapshot = GetRoutesSnapshot();
        var caseSensitive = Options.CaseSensitive;
        if (_routeIndex is not null &&
            ReferenceEquals(_indexedSnapshot, snapshot) &&
            _indexedCaseSensitive == caseSensitive)
        {
            return _routeIndex;
        }

        // Literal buckets key on the first segment using the same comparer the matcher applies to
        // literal segments, so a lookup by the URL's first segment returns exactly the routes whose
        // first literal would match it.
        var comparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var literalBuckets = new Dictionary<string, List<RouteEntry>>(comparer);
        var nonLiteralFirst = new List<RouteEntry>();

        for (int i = 0; i < snapshot.Length; i++)
        {
            // Pathless grouping routes stay registered (their Matched flag must be reset each
            // navigation with everyone else's) but never participate in matching: they win or lose
            // with their children, via the parent chain.
            if (snapshot[i].Group) continue;

            var entry = new RouteEntry(snapshot[i], i);
            var template = snapshot[i].RouteTemplate;

            // No template (not yet initialized) or an empty template (root/index) can't be bucketed by
            // a first literal - always consider it. TryMatch still filters it correctly.
            if (template is null || template.TemplateSegments.Count == 0)
            {
                nonLiteralFirst.Add(entry);
                continue;
            }

            var first = template.TemplateSegments[0];
            if (first.IsParameter || first.IsCatchAll || first.IsSingleWildcard)
            {
                // Parameter / '*' / '**' / '{**catch}' first segment matches many URL first segments.
                nonLiteralFirst.Add(entry);
            }
            else
            {
                if (literalBuckets.TryGetValue(first.Value, out var list) is false)
                {
                    list = [];
                    literalBuckets[first.Value] = list;
                }
                list.Add(entry);
            }
        }

        _routeIndex = new RouteIndex(literalBuckets, nonLiteralFirst);
        _indexedSnapshot = snapshot;
        _indexedCaseSensitive = caseSensitive;
        return _routeIndex;
    }

    // Releases all retained keep-alive state across every registered route (their inline hidden
    // content and any kept children held by outlets), keeping only the currently active route.
    // Backs IBrouter.ClearKeepAlive. Runs on the renderer dispatcher like every other reader; each
    // affected route/outlet issues its own re-render so the dropped subtrees are disposed.
    internal void ClearKeepAlive()
    {
        foreach (var route in GetRoutesSnapshot()) route.ClearKeepAlive();
    }

    // Reads the snapshot, not the live List: mirrors SelectWinner/ProcessNavigationAsync so name
    // lookups never touch the mutable registration set mid-pipeline (see GetRoutesSnapshot's remarks).
    internal Broute? FindRouteByName(string name)
    {
        var routesSnapshot = GetRoutesSnapshot();
        foreach (var r in routesSnapshot)
        {
            if (string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)) return r;
        }
        return null;
    }

    private CancellationTokenSource? _navCts;
    private bool _noRouteMatched;
    private long _navVersion;

    // The active router-level error boundary state (see RenderNavigationError). Non-null only when a
    // commit-phase failure bubbled past every route boundary and Brouter.ErrorContent is set; rendered
    // by BuildRenderTree in place of routed content. Cleared at the start of each navigation.
    private BrouterErrorContext? _navError;

    // The route chain (root -> leaf) whose content is currently committed to the screen. Consumed by
    // InvokeLeaveGuardsAsync to determine which routes a pending navigation deactivates. Updated at
    // every commit outcome: the matched chain on success, the chain down to the boundary on an error
    // render, and empty on a not-found render. Same single-dispatcher discipline as the other
    // navigation-state fields above.
    private Broute[] _committedChain = [];

    // Route-lifecycle arrivals (IBrouterRoute activation/renavigation) staged by a commit and fired
    // from OnAfterRenderAsync once the commit render has landed - the content must be mounted (so
    // handlers are registered) and the DOM available before the callbacks run. Deactivations are
    // never staged: they fire synchronously in the pipeline BEFORE the render that hides/unmounts
    // the departing content. Each entry carries the lifecycle navigation generation of the commit
    // that staged it so the flush can drop arrivals from a commit that was superseded before its
    // flush ran (the newer commit staged its own; firing the stale ones would deliver arrivals -
    // resolved against the routes' CURRENT parameters - for a location that never settled). Plain
    // tuples rather than closures: nothing captured, inspectable in a debugger. Same
    // single-dispatcher discipline as the surrounding fields. Note this staging also makes the
    // lifecycle a no-op under static prerendering (OnAfterRenderAsync never runs there), mirroring
    // Vue's "not called during SSR" contract.
    private readonly List<(Broute Node, BrouterNavigationContext Ctx, long Version)> _pendingLifecycleFlush = [];

    // Generation counter for _pendingLifecycleFlush: increments only when a NAVIGATION pipeline
    // starts. Deliberately distinct from _navVersion, which revalidation also bumps for
    // supersession - a revalidation running between a commit and its OnAfterRenderAsync flush is
    // not a navigation (it stages no arrivals of its own) and must not invalidate the arrivals the
    // commit staged. Same single-dispatcher discipline as the surrounding fields.
    private long _lifecycleNavGeneration;

    /// <summary>
    /// Fires the route-lifecycle departures for the routes an imminent render will remove from the
    /// screen - leaf -> root, mirroring leave-guard order, so a child can flush state before its
    /// parent's deactivation tears shared context down. Must run BEFORE the render that unmounts
    /// the departing content (see <see cref="IBrouterRoute"/>'s Disposing contract). When
    /// <paramref name="surviving"/> is set, routes it contains are skipped - unless
    /// <paramref name="notifySurvivorsAsRemaining"/> is true (the pending-UI render, which unmounts
    /// EVERYTHING for the duration of the load): then survivors are notified with
    /// willRemainMatched, so retained keep-alive content no-ops while transient content gets its
    /// honest Disposing notification for the instance that render destroys.
    /// </summary>
    private void NotifyChainDepartures(BrouterNavigationContext ctx, Broute[] departingChain,
        List<Broute>? surviving = null, bool notifySurvivorsAsRemaining = false, Broute? contentReplacedNode = null)
    {
        // A departure callback's synchronous prefix can start a new navigation, superseding this
        // one. The rest of the chain then belongs to that navigation's own departure phase (which
        // idempotently skips the nodes already notified here) and must not receive callbacks
        // describing a target that will never commit. The generation check covers callers whose
        // context carries no cancellable token (HandleAppNotFoundAsync passes
        // CancellationToken.None); it only ever changes when a navigation pipeline starts.
        var generation = _lifecycleNavGeneration;

        for (var i = departingChain.Length - 1; i >= 0; i--)
        {
            var node = departingChain[i];
            var survives = surviving is not null && surviving.Contains(node);
            if (survives && notifySurvivorsAsRemaining is false) continue;
            // contentReplacedNode is the error-boundary route whose committed content the coming
            // render REPLACES with its ErrorContent (see RenderNavigationError): its departure is
            // forced to Disposing - keep-alive retention keeps a subtree that no longer holds the
            // page, so Hidden would be a lie.
            node.NotifyDeparture(ctx, willRemainMatched: survives,
                contentReplaced: ReferenceEquals(node, contentReplacedNode));
            if (ctx.CancellationToken.IsCancellationRequested || generation != _lifecycleNavGeneration) return;
        }
    }

    /// <summary>
    /// Runs the pre-render arrival preparation for a chain about to be committed (per-parameter
    /// keep-alive sibling deactivation - see <see cref="Broute.PrepareArrival"/>). Root -> leaf,
    /// like guards and loaders. Must run after the chain's parameters are committed and before the
    /// commit render.
    /// </summary>
    private void PrepareArrivals(BrouterNavigationContext ctx, List<Broute> chain)
    {
        // A per-parameter keep-alive sibling deactivation fired here runs synchronously into user
        // code (OnDeactivated), whose prefix can start a new navigation and supersede this one - the
        // same hazard NotifyChainDepartures guards. Stop the moment that happens so we don't
        // deactivate siblings on behalf of a target that will never commit; the caller's post-loop
        // stale-commit check then abandons the rest of the commit.
        var generation = _lifecycleNavGeneration;
        foreach (var node in chain)
        {
            node.PrepareArrival(ctx);
            if (ctx.CancellationToken.IsCancellationRequested || generation != _lifecycleNavGeneration) return;
        }
    }

    /// <summary>
    /// Stages the route-lifecycle arrivals for a committed chain (root -> leaf), to be fired by
    /// OnAfterRenderAsync once the commit render has landed. Stamped with the current lifecycle
    /// navigation generation; see <see cref="_pendingLifecycleFlush"/>.
    /// </summary>
    private void StageArrivals(BrouterNavigationContext ctx, List<Broute> chain)
    {
        var version = _lifecycleNavGeneration;
        foreach (var node in chain)
        {
            _pendingLifecycleFlush.Add((node, ctx, version));
        }
    }

    // Awaited-navigation bookkeeping (IBrouter.NavigateAsync). At most one navigation outcome is
    // pending at a time: registering a new one supersedes the old (that IS the Superseded outcome).
    // Resolution is keyed on the target's absolute URI so a pipeline for some other navigation can
    // never resolve an awaiter that wasn't asking about it. Same single-dispatcher discipline as
    // the other navigation-state fields.
    private TaskCompletionSource<BrouterNavigationOutcome>? _pendingOutcome;
    private string? _pendingOutcomeUri;

    /// <summary>
    /// Registers an awaiter for the navigation about to be triggered toward <paramref name="absoluteUri"/>.
    /// The returned task resolves when the pipeline concludes that navigation (see BrouterNavigationOutcome).
    /// </summary>
    internal Task<BrouterNavigationOutcome> RegisterNavigationOutcome(string absoluteUri)
    {
        _pendingOutcome?.TrySetResult(BrouterNavigationOutcome.Superseded());
        // RunContinuationsAsynchronously: resolvers run inside the navigation pipeline on the
        // renderer dispatcher; awaiter continuations must not run inline there.
        var tcs = new TaskCompletionSource<BrouterNavigationOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingOutcome = tcs;
        _pendingOutcomeUri = absoluteUri;
        return tcs.Task;
    }

    // Resolves the pending awaiter when (and only when) it was registered for exactly this target.
    private void ResolveNavigationOutcome(string targetUri, BrouterNavigationOutcome outcome)
    {
        var pending = _pendingOutcome;
        if (pending is null || string.Equals(_pendingOutcomeUri, targetUri, StringComparison.Ordinal) is false) return;
        _pendingOutcome = null;
        _pendingOutcomeUri = null;
        pending.TrySetResult(outcome);
    }

    // True only while the current navigation is awaiting a route loader and a Navigating fragment is
    // set. Drives the pending-navigation UI emitted by BuildRenderTree. Set and cleared exclusively on
    // the renderer's single-threaded dispatcher inside the navigation pipeline (see the pending-UI
    // handling in ProcessNavigationAsync), so - like the other plain nav-state fields above - it needs
    // no synchronization.
    private bool _navigating;

    // Registration returned by NavigationManager.RegisterLocationChangingHandler. Disposed on
    // teardown to unhook the preventive guard/redirect/cancel decision (see OnLocationChanging).
    private IDisposable? _locationChangingRegistration;

    // True when the latest committed navigation started a View Transition whose completion is owed
    // after its render lands. Consumed by OnAfterRenderAsync (see the view-transition handshake in
    // ProcessNavigationAsync). Same single-dispatcher discipline as the surrounding fields.
    private bool _pendingViewTransitionCompletion;

    // Location whose post-navigation DOM effects (fragment/top scroll, focus) are pending. Staged
    // by ProcessNavigationAsync on a successful commit and consumed by OnAfterRenderAsync after the
    // matching render lands, so fragment/focus selectors resolve against the new route's DOM. Only
    // the most recent commit is held; a later navigation overwrites an unconsumed value so we never
    // apply effects for a page the user has already navigated away from. Staged and consumed on the
    // renderer dispatcher; accessed via Interlocked.Exchange for a clean read-and-clear (mirrors the
    // plain-field + Interlocked style used for _navCts rather than the `volatile` keyword).
    private BrouterLocation? _pendingEffectsLocation;

    // Hand-off from the preventive "changing" phase to the "changed" (commit) phase. When the
    // LocationChanging handler has already run OnNavigating + guards for a target and approved it,
    // it records the target's absolute URI here so the subsequent LocationChanged commit phase can
    // skip re-running those side-effecting hooks (they must run exactly once per navigation).
    // Read-and-cleared by the commit phase; overwritten by each new approved decision.
    private string? _approvedTargetUri;

    // Navigation-type bookkeeping (see BrouterNavigationType). _pendingNavigationType is stamped by
    // Brouter's own programmatic navigations (BrouterService.Navigate and the internal redirect/restore
    // calls, all funnelled through NavigateInternal / SetPendingNavigationType) right before they call
    // NavigationManager.NavigateTo, so the phase that observes the resulting navigation knows it was a
    // push vs a replace rather than having to guess. It is consumed exactly once - by whichever of the
    // changing or commit phase runs first for that navigation - so it can never leak into a later one.
    // A navigation with no pending type is either an intercepted link click (a push) or, when not
    // intercepted, a history traversal (Back/Forward => pop). Like the other nav-state fields it is only
    // touched on the renderer's single dispatcher, so it needs no synchronization.
    private BrouterNavigationType? _pendingNavigationType;
    // Hand-off of the resolved type from the preventive changing phase to the commit phase, paired with
    // _approvedTargetUri: set only when the changing phase approves a navigation, read-and-cleared by the
    // commit phase so it re-uses the exact type the changing phase computed instead of recomputing it.
    private BrouterNavigationType? _approvedNavigationType;


    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Validate here as well as in OnParametersSet: the first OnParametersSet only runs after the
        // async initialization below completes, and a misconfiguration should fail the first render
        // synchronously instead of surfacing as an unhandled async exception.
        ValidateChildContentAlias();

        // Compute discovered routes here (not only in OnParametersSet): the initial route match runs in
        // OnInitializedAsync, before OnParametersSet, and the synthetic <Broute> children must already be
        // present in the first render so they register in time to be matched on the initial navigation.
        // OnParametersSet still re-checks afterwards to pick up runtime changes to the assembly set.
        RefreshDiscoveredRoutesIfNeeded();

        _brouterService.Attach(this, _navManager);

        // Wire up prerender loader-state persistence once, before the first navigation runs its loaders.
        // PersistentComponentState is resolved optionally (GetService, not [Inject]) so Brouter still works
        // in hosts/tests where it isn't registered (e.g. bUnit, plain WASM without prerender).
        if (Options.PersistLoaderState && _persistSubscribed is false)
        {
            _persistentState = _services.GetService<PersistentComponentState>();
            if (_persistentState is not null)
            {
                if (Options.LoaderStateTypeInfoResolver is not null)
                {
                    _loaderStateJsonOptions = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
                    {
                        TypeInfoResolver = Options.LoaderStateTypeInfoResolver,
                    };
                }
                _persistSubscription = _persistentState.RegisterOnPersisting(PersistLoaderStateAsync);
                _persistSubscribed = true;
            }
        }

        _navManager.LocationChanged += NavManagerLocationChanged;
#if NET10_0_OR_GREATER
        // .NET 10 not-found contract: app code (or the framework) calls NavigationManager.NotFound()
        // to signal a missing resource; routers subscribe to OnNotFound and render their fallback.
        _navManager.OnNotFound += NavManagerOnNotFound;
#endif

        // Establish the initial location synchronously so any code that reads
        // BrouterService.Location before the first navigation pipeline runs sees
        // the real URL (not BrouterLocation.Empty).
        CurrentLocation = ComputeLocation();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        ValidateChildContentAlias();

        // Refresh discovered routes whenever the assembly set changes (including the first parameter set,
        // and when AdditionalAssemblies grows because a lazy-loaded assembly was added). Kept out of the
        // navigation pipeline so scanning cost is paid on parameter changes, not per navigation.
        RefreshDiscoveredRoutesIfNeeded();
    }

    private void ValidateChildContentAlias()
    {
        if (ChildContent is not null && Routes is not null)
            throw new InvalidOperationException(
                $"{nameof(Brouter)} accepts either {nameof(ChildContent)} or {nameof(Routes)} ({nameof(Routes)} is an alias for {nameof(ChildContent)}), not both.");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Attribute-route discovery is opt-in via AppAssembly/AdditionalAssemblies. The " +
                        "consumer is responsible for preserving their routable components under trimming, " +
                        "exactly as the built-in Blazor Router requires.")]
    private void RefreshDiscoveredRoutesIfNeeded()
    {
        // Materialize the enumerable once: it may be a lazily-evaluated sequence, and we both compare and
        // (potentially) hand it to the scanner.
        var additional = AdditionalAssemblies as Assembly[] ?? AdditionalAssemblies?.ToArray();

        if (_discoveryComputed &&
            ReferenceEquals(AppAssembly, _lastAppAssembly) &&
            SameAssemblies(_lastAdditionalAssemblies, additional))
        {
            return;
        }

        _lastAppAssembly = AppAssembly;
        _lastAdditionalAssemblies = additional;
        _discoveryComputed = true;

        // Runtime-loaded assemblies (OnNavigateAsync) join the scan set transparently.
        var scanSet = additional;
        if (_runtimeAssemblies.Count > 0)
        {
            scanSet = additional is null || additional.Length == 0
                ? _runtimeAssemblies.ToArray()
                : additional.Concat(_runtimeAssemblies).Distinct().ToArray();
        }

        _discoveredRoutes = (AppAssembly is null && (scanSet is null || scanSet.Length == 0))
            ? []
            : BrouteScanner.Discover(AppAssembly, scanSet);
    }

    /// <summary>
    /// Runs the <see cref="OnNavigateAsync"/> hook for a pending navigation and, when it returns
    /// new assemblies, folds them into route discovery and yields one render so the new synthetic
    /// &lt;Broute&gt;s register - all before matching runs, so the very navigation that triggered
    /// the load can match a freshly-loaded page.
    /// </summary>
    private async ValueTask RunOnNavigateHookAsync(BrouterNavigationContext ctx)
    {
        if (OnNavigateAsync is null) return;

        var assemblies = await OnNavigateAsync(ctx);
        if (assemblies is null) return;

        var added = false;
        foreach (var assembly in assemblies)
        {
            if (assembly is null || _runtimeAssemblies.Contains(assembly)) continue;
            _runtimeAssemblies.Add(assembly);
            added = true;
        }
        if (added is false) return;

        // Recompute discovery with the grown runtime set, then let the renderer flush once so the
        // new synthetic <Broute> children run OnInitialized and register - the same yield-to-register
        // technique the initial mount uses (see OnInitializedAsync).
        _discoveryComputed = false;
        RefreshDiscoveredRoutesIfNeeded();
        StateHasChanged();
        await Task.Yield();
    }

    private static bool SameAssemblies(Assembly[]? a, Assembly[]? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (ReferenceEquals(a[i], b[i]) is false) return false;
        }
        return true;
    }

    // Serializes the loader results staged during prerender into PersistentComponentState so the interactive
    // pass can restore them instead of re-fetching. Registered via RegisterOnPersisting; fires once at the
    // end of prerender.
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Only reached when the consumer opts into Options.PersistLoaderState and accepts the " +
                        "reflection-based JSON serialization contract documented on that option.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "See above; PersistLoaderState is opt-in and documents its AOT limitations.")]
    private Task PersistLoaderStateAsync()
    {
        var state = _persistentState;
        if (state is null) return Task.CompletedTask;

        foreach (var kv in _loaderStateToPersist)
        {
            // A null capture means the value wasn't serializable under the active resolver/options;
            // skip it so the loader simply re-runs on the interactive pass.
            var captured = BroutePrerenderState.Capture(kv.Value, _loaderStateJsonOptions);
            if (captured is not null)
            {
                state.PersistAsJson(kv.Key, captured);
            }
        }

        return Task.CompletedTask;
    }

    // Attempts to restore a loader result persisted during prerender. Returns true (with the restored value,
    // which may legitimately be null) when the loader should be skipped for this navigation.
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Only reached when the consumer opts into Options.PersistLoaderState; see PersistLoaderStateAsync.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "See above; PersistLoaderState is opt-in and documents its AOT limitations.")]
    private bool TryRestoreLoaderState(BrouterLocation to, int chainIndex, out object? value)
    {
        value = null;
        var state = _persistentState;
        if (state is null) return false;

        var key = BroutePrerenderState.MakeKey(to.Path, to.Query, chainIndex);
        if (state.TryTakeFromJson<PersistedLoaderState>(key, out var persisted) is false) return false;

        return BroutePrerenderState.TryRestore(persisted, out value, _loaderStateJsonOptions);
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        // Yield once so ComponentBase performs the initial synchronous render of our
        // ChildContent. That first render is what causes the declared <Broute> children to
        // register themselves with us (each one calls RegisterRoute from its own OnInitialized).
        // Until they've registered there is nothing to match against, which is why the initial
        // match cannot run any earlier than this.
        //
        // Doing the initial match here - rather than in OnAfterRenderAsync - is what enables
        // static server prerendering. OnAfterRenderAsync never runs during prerender, so the old
        // placement left the prerendered HTML empty (no route was ever matched server-side).
        // OnInitializedAsync, by contrast, runs during prerender and the renderer awaits it - and
        // the StateHasChanged it triggers - before serializing the HTML, so the matched route is
        // included in the prerendered output. When the component later becomes interactive its
        // lifecycle runs again and the match re-runs naturally.
        await Task.Yield();

        // Initial render: the From is Empty (we just mounted), the To is the URL we're at now.
        // decisionAlreadyMade is false - the LocationChanging handler is not registered yet (and does
        // not fire for the initial load anyway), so the full pipeline runs the guards here. The first
        // mount is reported as a Push (a fresh navigation), never a Pop.
        await ProcessNavigationAsync(BrouterLocation.Empty, CurrentLocation, decisionAlreadyMade: false, BrouterNavigationType.Push);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // Claim this render pass's staged route-lifecycle arrivals up front, before any await
        // below can yield the dispatcher. The interop awaits in this method can overlap a newer
        // navigation's commit, and the arrivals that commit stages must wait for ITS render's
        // OnAfterRenderAsync: draining them from this older invocation would fire activations
        // before their content has mounted (handlers not registered, per-parameter kept entries
        // not materialized yet), silently losing them. Entries staged while this flush is in
        // flight stay queued for the invocation that matches their render; the generation filter
        // in the finally below still drops entries whose commit a newer navigation superseded.
        var staged = Array.Empty<(Broute Node, BrouterNavigationContext Ctx, long Version)>();
        if (_pendingLifecycleFlush.Count > 0)
        {
            staged = _pendingLifecycleFlush.ToArray();
            _pendingLifecycleFlush.Clear();
        }

        // Everything below can await JS interop and throw (a dropped circuit failing the
        // first-render SetConfirmExternalNavigationAsync, the effects/view-transition interop):
        // the finally flushes the arrivals claimed above either way, so the committed
        // navigation's activation callbacks are never stranded by a setup or effects failure.
        try
        {
            if (firstRender)
            {
                // Enabling navigation interception genuinely requires an interactive runtime, so it stays
                // in OnAfterRenderAsync, which only runs once interactivity is established. Under prerender
                // this method doesn't run at all - that's fine: the initial match already happened in
                // OnInitializedAsync, and interception is enabled here once the component goes interactive.
                //
                // Enabling navigation interception is best-effort: on a disconnected circuit or an interop
                // failure it can throw, but the navigation pipeline itself (and any subsequent reconnects /
                // interactivity handoff) does not depend on it succeeding right now. Mirror the defensive
                // style used in BrouterLink and BrouterService.BackAsync so a transient failure here can't
                // kill navigation. Once the circuit/runtime is fully ready, Blazor will retry interception
                // attachment naturally on the next user click via NavigationManager fallback paths.
                try
                {
                    await _navInterception.EnableNavigationInterceptionAsync();
                }
                catch (JSDisconnectedException) { /* circuit disconnected before/during interop */ }
                catch (JSException) { /* JS interop failure; non-fatal */ }
                catch (InvalidOperationException) { /* interop unavailable during prerender */ }
                catch (TaskCanceledException) { /* component disposed mid-call */ }

                // Arm the always-on external-navigation confirmation once interactive (the JS side is
                // idempotent; runtime toggling goes through IBrouter.SetConfirmExternalNavigationAsync).
                if (Options.ConfirmExternalNavigation)
                {
                    await _brouterService.SetConfirmExternalNavigationAsync(true);
                }

                // Register the preventive navigation handler now that the runtime is interactive.
                // RegisterLocationChangingHandler (NET 7+) runs BEFORE the URL commits to history, so a
                // guard / OnNavigating hook that cancels or redirects prevents the navigation outright
                // (LocationChangingContext.PreventNavigation) instead of reactively "undoing" a URL change
                // that already happened. This is what makes guards preventive rather than reactive: no
                // address-bar flicker, no corrupted history on a cancelled Back, and real "unsaved changes"
                // prompts become possible. LocationChanged is kept only for the commit phase (loaders +
                // render). During static prerender this method never runs, so the handler simply isn't
                // registered there - which is correct, since there is no interactive navigation to guard.
                _locationChangingRegistration ??= _navManager.RegisterLocationChangingHandler(OnLocationChanging);
            }

            // Apply any post-navigation DOM effects (fragment/top scroll, focus) staged by the last
            // committed navigation. Running here - after the render batch has been applied to the DOM -
            // is what lets fragment (#section) and focus selectors resolve against the newly rendered
            // route instead of the previous page. Exchange to null so each staged navigation's effects
            // run exactly once; a navigation with nothing pending is a no-op. During static prerender
            // this method never runs, so effects are correctly skipped server-side (no DOM/JS there).
            var pending = Interlocked.Exchange(ref _pendingEffectsLocation, null);
            if (pending is not null)
            {
                await _brouterService.ApplyNavigationEffectsAsync(pending);
            }

            // Complete the navigation's View Transition after the effects above, so the incoming
            // snapshot the browser animates to already reflects the final scroll/focus state.
            if (_pendingViewTransitionCompletion)
            {
                _pendingViewTransitionCompletion = false;
                await _brouterService.CompleteViewTransitionAsync();
            }
        }
        finally
        {
            // Fire the route-lifecycle arrivals (IBrouterRoute activation/renavigation) claimed at
            // the top of this method, now that the commit render is on screen - after the DOM
            // effects and view-transition completion above, so lifecycle callbacks never delay the
            // visual navigation. Entries staged by a commit that has since been superseded are
            // dropped: the newer commit staged its own, and firing stale ones would resolve
            // against the routes' current state with a location that never settled. The callbacks
            // themselves are started-but-not-awaited (see BrouterRouteContext.Invoke); handler
            // failures surface via ReportLifecycleError, never out of this method.
            foreach (var (node, ctx, version) in staged)
            {
                // Live read per entry (not captured before the loop): a callback's synchronous
                // prefix can start a new navigation mid-flush, superseding the remaining entries.
                if (version != _lifecycleNavGeneration) continue;
                node.FireArrival(ctx);
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2110",
        Justification = "The Broute.Component backing field requires DynamicallyAccessedMembers.All; the value " +
                        "assigned here is BrouteScanner.DiscoveredRoute.ComponentType, whose property carries the " +
                        "same annotation, so the requirement is satisfied.")]
    [UnconditionalSuppressMessage("Trimming", "IL2111",
        Justification = "Broute.Component's setter has a DynamicallyAccessedMembers.All parameter and is invoked " +
                        "via Blazor's reflection-based component parameter binding. DiscoveredRoute.ComponentType " +
                        "carries the matching annotation, so the members are preserved.")]
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Sequence numbers are per RenderFragment scope: each lambda passed to
        // builder.AddAttribute("ChildContent", ...) starts its own 0-based sequence. The
        // outer scope here uses 0..3 for the CascadingValue<Brouter> open/attributes; the
        // inner ChildContent lambda restarts at 0 for its own AddContent calls. Renumbering
        // these manually after edits is required - Blazor's diff relies on stable, ordered
        // sequence numbers within each scope to match frames across renders.
        base.BuildRenderTree(builder);

        builder.OpenComponent<CascadingValue<Brouter>>(0);
        builder.AddAttribute(1, "Name", "Brouter");
        builder.AddAttribute(2, "Value", this);
        builder.AddAttribute(3, "ChildContent", (RenderFragment)(bo =>
        {
            // Observer channel: the committed navigation's framework RouteData (or null when nothing
            // routed is committed), cascaded unnamed-by-type so any descendant can take a
            // [CascadingParameter] RouteData? - publishers, breadcrumbs, telemetry - without a Found
            // template. Not fixed: the value is replaced on every commit and subscribers must see it.
            bo.OpenComponent<CascadingValue<RouteData?>>(0);
            bo.AddAttribute(1, "Value", _currentRouteData);
            bo.AddAttribute(2, "IsFixed", false);
            bo.AddAttribute(3, "ChildContent", (RenderFragment)(b =>
            {
            b.AddContent(0, ChildContent ?? Routes);

            // Emit a synthetic <Broute> for each attribute-discovered route. They register themselves
            // exactly like hand-declared children (via Broute.OnInitialized) and so participate in the
            // same specificity-based matching, guards, loaders and rendering. Wrapped in a region so
            // their sequence numbers live in an isolated space, and keyed by the (template, type) pair
            // so Blazor keeps instances stable if the discovered set is reordered or grows at runtime.
            //
            // Known trade-off (scalability): unlike the built-in Router - which builds a plain RouteTable
            // and instantiates only the matched component - every route here (hand-declared and discovered
            // alike) exists as a permanently-mounted Broute in the render tree, each carrying a
            // BrouterRouteRenderer, its cached template/parameter dictionaries and cascading-value
            // subscriptions. An unmatched Broute renders nothing (BuildRenderTree short-circuits on the
            // Matched flag), so the steady-state cost is memory/instances rather than render work, and the
            // first-segment index (GetRouteIndex) keeps per-navigation match cost off the full O(routes)
            // scan - but it does NOT reduce this instantiation cost. Fine for typical apps; for very large
            // route sets (hundreds of pages) benchmark against the built-in Router and consider splitting
            // routes across lazily-loaded assemblies. See the README "Performance & scalability" section.
            if (_discoveredRoutes.Count > 0)
            {
                b.OpenRegion(1);
                // Fixed cascading marker (never changes, so no subscriptions) flagging every Broute in
                // this region as attribute-discovered. RegisterRoute's ambiguity check uses it to let a
                // hand-declared route deliberately shadow a discovered one (see Broute.IsDiscovered).
                b.OpenComponent<CascadingValue<bool>>(0);
                b.AddAttribute(1, "Name", "IsDiscoveredRoute");
                b.AddAttribute(2, "Value", true);
                b.AddAttribute(3, "IsFixed", true);
                b.AddAttribute(4, "ChildContent", (RenderFragment)(b1 =>
                {
                    var seq = 0;
                    foreach (var discovered in _discoveredRoutes)
                    {
                        b1.OpenComponent<Broute>(seq++);
                        b1.SetKey(discovered);
                        b1.AddAttribute(seq++, nameof(Broute.Path), discovered.Template);
                        b1.AddAttribute(seq++, nameof(Broute.Component), discovered.ComponentType);
                        b1.CloseComponent();
                    }
                }));
                b.CloseComponent();
                b.CloseRegion();
            }

            // Render the inline fallback when no route matched and either NotFoundUrl is unset, or
            // NotFoundUrl resolves to the current URL (no redirect happened, so we'd otherwise show nothing).
            if (_noRouteMatched && NotFound is not null &&
                (string.IsNullOrEmpty(NotFoundUrl) || IsSamePath(CurrentLocation.Path, NotFoundUrl)))
            {
                b.AddContent(2, NotFound(CurrentLocation));
            }

            // Pending-navigation UI. Shown while the current navigation awaits its loaders (see the
            // pending-UI handling in ProcessNavigationAsync). The declared/discovered <Broute> children
            // above stay in the tree so they remain registered, but each renders nothing while unmatched
            // (Matched is reset to false for the duration of the load), so this fragment is what the user
            // sees until the matched route is revealed - matching the built-in Router.Navigating.
            if (_navigating && Navigating is not null)
            {
                b.AddContent(3, Navigating);
            }

            // Router-level error boundary. Set only when a commit-phase failure found no route-level
            // ErrorContent to bubble to (see RenderNavigationError); the matched chain is unmatched at
            // that point, so this fragment is what the user sees in place of the routed content.
            if (_navError is not null && ErrorContent is not null)
            {
                b.AddContent(4, ErrorContent(_navError));
            }
            }));
            bo.CloseComponent();
        }));
        builder.CloseComponent();
    }


    private async void NavManagerLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        // The handler thread is whatever raised LocationChanged (renderer dispatcher in most
        // cases, but never something we can rely on). We:
        //   1. Capture `from` (the location the previous pipeline left in CurrentLocation) and
        //      compute `to` for THIS event synchronously, so they can never desync from each
        //      other if a second LocationChanged fires before this one is dispatched.
        //   2. Dispatch onto the renderer's synchronization context via InvokeAsync, so the
        //      navigation pipeline runs where StateHasChanged() / NavigateTo() / component
        //      state mutations are valid.
        // We deliberately do NOT mutate CurrentLocation here on the raw event thread; that
        // happens at the start of ProcessNavigationAsync once we own the dispatcher.
        BrouterLocation from = CurrentLocation;
        BrouterLocation to;
        try
        {
            to = ComputeLocation();
        }
        catch (Exception ex)
        {
            // Defense in depth: ComputeLocation is intended to be no-throw (it normalizes
            // off-base URLs to an empty-path location), but if a future change ever lets an
            // exception escape, we still surface it through OnError instead of letting it
            // out of the async-void event handler.
            await SafeInvokeOnError(from, CurrentLocation, ex);
            return;
        }

        // Did the preventive "changing" phase already run OnNavigating + guards for this exact
        // target and approve it? If so, commit without re-running those side-effecting hooks.
        // Otherwise this is a navigation the changing handler never saw (initial load, forceLoad,
        // or a nav that raced ahead of interception being enabled) - run the full pipeline, which
        // still honours guards/OnNavigating, falling back to the reactive URL-restore behavior.
        var approved = _approvedTargetUri;
        _approvedTargetUri = null;
        var approvedType = _approvedNavigationType;
        _approvedNavigationType = null;
        var decisionAlreadyMade =
            approved is not null && string.Equals(approved, to.FullUri, StringComparison.Ordinal);

        // When the changing phase already ran, re-use the type it resolved and stashed alongside the
        // approval. Otherwise (initial load races, forceLoad, a nav that outran interception) resolve it
        // here from our pending marker + whether the framework saw an intercepted link click.
        var navType = decisionAlreadyMade
            ? (approvedType ?? BrouterNavigationType.Push)
            : ConsumeNavigationType(isInitial: false, isIntercepted: e.IsNavigationIntercepted);

        try
        {
            await InvokeAsync(() => ProcessNavigationAsync(from, to, decisionAlreadyMade, navType).AsTask());
        }
        catch (Exception ex)
        {
            // ProcessNavigationAsync routes its own exceptions to OnError, so reaching this
            // catch generally means InvokeAsync itself failed (renderer detached / disposed,
            // or an exception during dispatcher scheduling). Surface it through OnError, never
            // let it escape async void.
            await SafeInvokeOnError(from, to, ex);
        }
    }

#if NET10_0_OR_GREATER
    // True while Brouter itself is calling NavigationManager.NotFound() to propagate a router-level
    // "no route matched" to the framework (so static SSR responds with HTTP 404 instead of a 200
    // carrying fallback HTML). Our own OnNotFound event handler must no-op for that call: the
    // pipeline is already rendering the fallback, and reacting again would double-handle it.
    private bool _raisingFrameworkNotFound;

    /// <summary>
    /// Whether the component is being rendered by an interactive renderer. Unknown hosts (unit-test
    /// renderers that don't populate <c>RendererInfo</c>) are treated as interactive, which keeps the
    /// framework-404 propagation (a static-SSR-only concern) switched off there.
    /// </summary>
    private bool IsInteractiveRuntime
    {
        get
        {
            try { return RendererInfo.IsInteractive; }
            catch (InvalidOperationException) { return true; }
        }
    }

    /// <summary>
    /// Handles <c>NavigationManager.OnNotFound</c> (.NET 10): application code called
    /// <c>NavigationManager.NotFound()</c> to report a missing resource (e.g. a page whose entity
    /// lookup failed). Brouter takes over rendering: it fires its own <see cref="OnNotFound"/> hook,
    /// then either redirects to <see cref="NotFoundUrl"/> or renders <see cref="NotFound"/> in
    /// place, and signals the framework via <c>NotFoundEventArgs.Path</c> that the rendering is
    /// handled (mirroring the built-in Router's contract).
    /// </summary>
    private void NavManagerOnNotFound(object? sender, NotFoundEventArgs args)
    {
        if (_raisingFrameworkNotFound) return;

        // Signal "handled" synchronously (the framework reads args after the event returns). Only
        // claim it when this Brouter actually has a fallback to show; otherwise leave the args
        // untouched so the framework's own not-found handling (status codes / re-execution) runs.
        if (string.IsNullOrEmpty(NotFoundUrl) is false)
        {
            args.Path = NotFoundUrl;
        }
        else if (NotFound is not null)
        {
            args.Path = CurrentLocation.Path;
        }
        else
        {
            return;
        }

        _ = HandleAppNotFoundAsync();
    }

    // Async continuation of NavManagerOnNotFound, dispatched onto the renderer like the
    // LocationChanged handler. Exceptions surface through OnError, never out of the event.
    private async Task HandleAppNotFoundAsync()
    {
        var location = CurrentLocation;
        try
        {
            await InvokeAsync(async () =>
            {
                // Captured to detect a navigation starting while OnNotFound is awaited below (the
                // generation only ever changes when a navigation pipeline starts, never for
                // revalidation - see _lifecycleNavGeneration).
                var generation = _lifecycleNavGeneration;

                // Unmatch the currently rendered chain so the fallback replaces the page content -
                // the URL deliberately stays put (the resource at this URL is what's missing).
                foreach (var r in GetRoutesSnapshot()) r.Matched = false;
                _noRouteMatched = true;
                // Everything routed leaves the screen (the fallback replaces it): notify the
                // route-lifecycle departures while the departing content is still alive, before the
                // render below (or the NotFoundUrl redirect's eventual commit) unmounts it. There is
                // no navigation here - the URL stays put - so the context describes a same-location
                // "navigation".
                NotifyChainDepartures(
                    new BrouterNavigationContext(location, location, CancellationToken.None),
                    _committedChain);
                // A departure callback's synchronous prefix can start a new navigation (same
                // re-entrancy FlushPendingLifecycle guards against) - that navigation owns the
                // router state now; clearing the committed chain below would skip its leave guards
                // for content still on screen.
                if (generation != _lifecycleNavGeneration) return;
                // The fallback replaces the routed content, so nothing routed is on screen anymore -
                // a later navigation must not run leave guards for the replaced chain.
                _committedChain = [];
                _currentRouteData = null;

                if (OnNotFound is not null)
                {
                    await OnNotFound(location);
                    // A navigation that started while OnNotFound was awaited owns the router state
                    // now (committed chain, route data, URL): redirecting to the fallback or
                    // re-rendering the unmatched state here would hijack it.
                    if (generation != _lifecycleNavGeneration) return;
                }

                if (string.IsNullOrEmpty(NotFoundUrl) is false && IsSamePath(location.Path, NotFoundUrl) is false)
                {
                    NavigateInternal(NotFoundUrl);
                    return;
                }

                StateHasChanged();
            });
        }
        catch (Exception ex)
        {
            await SafeInvokeOnError(location, location, ex);
        }
    }
#endif

    /// <summary>
    /// Preventive navigation decision. Registered via <c>NavigationManager.RegisterLocationChangingHandler</c>
    /// so it runs BEFORE the URL commits to history. Runs the OnNavigating hooks and route guards for the
    /// pending target and, if any of them cancels or redirects, calls
    /// <see cref="LocationChangingContext.PreventNavigation"/> so the navigation never happens - instead of
    /// letting the URL change and reactively undoing it. When the decision approves, the navigation is
    /// allowed to commit and the subsequent LocationChanged event runs the commit phase (loaders + render).
    /// </summary>
    /// <remarks>
    /// Only the decision (OnNavigating + guards + redirect/cancel + RedirectTo + NotFound-redirect) lives
    /// here. Loaders and rendering deliberately stay in the commit phase: they produce and show the new
    /// view, which is meaningful only once navigation is committed. This mirrors the issue's guidance to
    /// "keep LocationChanged only for the commit phase".
    /// </remarks>
    private async ValueTask OnLocationChanging(LocationChangingContext context)
    {
        // Clear any prior approval up front: only an outcome that actually approves THIS navigation
        // below may set it. This guarantees a decision that ends up cancelled, redirected, superseded
        // or errored never leaves a stale approval that a later commit could misread as "guards ran".
        _approvedTargetUri = null;
        _approvedNavigationType = null;

        // Resolve the navigation type now (before hooks/guards run) so they can read it off the context.
        // Consuming the pending marker here is what keeps it from leaking into a later navigation.
        var navType = ConsumeNavigationType(isInitial: false, isIntercepted: context.IsNavigationIntercepted);

        BrouterLocation from = CurrentLocation;
        BrouterLocation to;
        try
        {
            // The URL has NOT committed yet, so resolve the pending target rather than
            // NavigationManager.Uri (which still holds the current location). The pending entry's
            // history state likewise comes from the changing context, not the NavigationManager.
            to = ComputeLocation(_navManager.ToAbsoluteUri(context.TargetLocation).ToString(), context.HistoryEntryState);
        }
        catch (Exception ex) when (ex is ArgumentException or UriFormatException or InvalidOperationException)
        {
            // Malformed / off-base target. Let the navigation commit and be handled by the commit
            // phase (which routes it through NotFound / OnError). Do not block on a parse failure.
            return;
        }

        // Supersession here rides on the framework: context.CancellationToken is cancelled when a
        // newer navigation starts, so guards/hooks that await observe it and bail. We deliberately
        // do NOT touch _navCts / _navVersion in this phase - that machinery belongs to the commit
        // phase, and mixing the two would leak or double-cancel token sources.
        var token = context.CancellationToken;
        var ctx = new BrouterNavigationContext(from, to, token) { NavigationType = navType };
        var service = _brouterService;

        try
        {
            // Lazy route loading first: the target may live in an assembly that isn't loaded yet,
            // and everything below (leave-guard staying-set, matching, guards) needs the final
            // route set to reason correctly.
            await RunOnNavigateHookAsync(ctx);
            if (token.IsCancellationRequested) return;
            if (ApplyPreventiveDecision(context, ctx)) return;

            // Leave guards run first (Vue's beforeRouteLeave / Angular's CanDeactivate ordering):
            // the routes being deactivated get the first chance to veto, before global hooks and
            // any enter guards on the target.
            var leaveOk = await InvokeLeaveGuardsAsync(to, ctx);
            if (token.IsCancellationRequested) return;
            if (ApplyPreventiveDecision(context, ctx)) return;
            if (leaveOk is false) return; // superseded inside the leave chain

            await service.InvokeOnNavigating(ctx);
            if (token.IsCancellationRequested) return;
            if (ApplyPreventiveDecision(context, ctx)) return;

            var winnerMatch = SelectWinner(to);

            if (winnerMatch is null)
            {
                // No route matched. Fire OnNotFound, then either redirect to the NotFoundUrl target
                // (preventively, so the unmatched URL never appears in the address bar) or allow
                // the commit phase to render NotFound in place.
                if (OnNotFound is not null) await OnNotFound(to);
                if (token.IsCancellationRequested) return;

                if (string.IsNullOrEmpty(NotFoundUrl) is false && IsSamePath(to.Path, NotFoundUrl) is false)
                {
                    context.PreventNavigation();
                    ResolveNavigationOutcome(to.FullUri, BrouterNavigationOutcome.NotFound());
                    NavigateInternal(NotFoundUrl);
                    return;
                }

                _approvedTargetUri = to.FullUri;
                _approvedNavigationType = navType;
                return;
            }

            var winner = winnerMatch.Value.Route;
            ctx.Route = winner;
            ctx.Parameters = new BrouterRouteParameters(winnerMatch.Value.Parameters);

            var guardsOk = await winner.InvokeGuardsAsync(ctx);
            if (token.IsCancellationRequested) return;
            if (ApplyPreventiveDecision(context, ctx)) return;
            if (guardsOk is false) return; // superseded (token cancelled inside the guard chain)

            if (winner.RedirectTo is not null)
            {
                context.PreventNavigation();
                ResolveNavigationOutcome(to.FullUri, BrouterNavigationOutcome.Redirected(winner.RedirectTo));
                NavigateInternal(winner.RedirectTo);
                return;
            }

            // Approved: let the URL commit. The LocationChanged commit phase re-selects this same
            // winner (matching is pure) and runs its loaders + render, skipping the hooks above.
            _approvedTargetUri = to.FullUri;
            _approvedNavigationType = navType;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // The navigation was superseded while a guard/hook was awaiting. The framework has
            // already cancelled it, so there is nothing to prevent and no error to report.
        }
        catch (Exception ex)
        {
            // A guard / OnNavigating hook threw. Fail closed: block the navigation rather than
            // committing into a state whose authorization never completed, and surface the error.
            context.PreventNavigation();
            ResolveNavigationOutcome(to.FullUri, BrouterNavigationOutcome.Failed(ex));
            await SafeInvokeOnError(from, to, ex);
        }
    }

    /// <summary>
    /// Translates a cancel/redirect request captured on <paramref name="ctx"/> (by an OnNavigating
    /// hook or a guard) into a preventive outcome on <paramref name="context"/>. Returns true when the
    /// navigation has been handled (prevented, and redirected if applicable) and the caller should stop.
    /// </summary>
    private bool ApplyPreventiveDecision(LocationChangingContext context, BrouterNavigationContext ctx)
    {
        if (ctx.RedirectUrl is not null)
        {
            context.PreventNavigation();
            // Resolve the awaited outcome BEFORE triggering the redirect: the redirect's own
            // pipeline starts (synchronously on some hosts) and would otherwise supersede the
            // still-pending awaiter of the navigation we're concluding here.
            ResolveNavigationOutcome(ctx.To.FullUri, BrouterNavigationOutcome.Redirected(ctx.RedirectUrl));
            NavigateInternal(ctx.RedirectUrl);
            return true;
        }

        if (ctx.IsCancelled)
        {
            context.PreventNavigation();
            ResolveNavigationOutcome(ctx.To.FullUri, BrouterNavigationOutcome.Cancelled());
            return true;
        }

        return false;
    }

    /// <summary>
    /// Resolves the <see cref="BrouterNavigationType"/> for a navigation and consumes the pending marker.
    /// A pending type (stamped by <see cref="NavigateInternal"/> / <see cref="SetPendingNavigationType"/>
    /// for Brouter's own programmatic navigations) wins. Otherwise the first mount and an intercepted link
    /// click are pushes, and a non-intercepted navigation with no pending marker is a history traversal
    /// (Back/Forward) - reported as <see cref="BrouterNavigationType.Pop"/>. Consuming the pending marker
    /// here (exactly once, in whichever phase runs first) is what stops it leaking into a later navigation.
    /// </summary>
    private BrouterNavigationType ConsumeNavigationType(bool isInitial, bool isIntercepted)
    {
        var pending = _pendingNavigationType;
        _pendingNavigationType = null;
        if (pending is not null) return pending.Value;
        if (isInitial || isIntercepted) return BrouterNavigationType.Push;
        return BrouterNavigationType.Pop;
    }

    /// <summary>
    /// Marks the next navigation this Brouter is about to trigger programmatically as a push or a
    /// replace, then delegates to <c>NavigationManager.NavigateTo</c>. Used for all of Brouter's own
    /// internal navigations (redirects, NotFoundUrl redirect, cancelled-navigation address-bar restore) so
    /// the phase that observes the resulting navigation classifies it correctly instead of guessing.
    /// </summary>
    private void NavigateInternal(string url, bool replace = false)
    {
        _pendingNavigationType = replace ? BrouterNavigationType.Replace : BrouterNavigationType.Push;
        _navManager.NavigateTo(url, replace: replace);
    }

    /// <summary>
    /// Records the type of the next navigation the caller is about to trigger via
    /// <c>NavigationManager.NavigateTo</c>. Called by <see cref="BrouterService"/> before an
    /// <see cref="IBrouter.Navigate"/> so the pipeline reports the correct push/replace type.
    /// </summary>
    internal void SetPendingNavigationType(BrouterNavigationType type) => _pendingNavigationType = type;

    private async ValueTask SafeInvokeOnError(BrouterLocation from, BrouterLocation to, Exception ex)
    {
        try
        {
            await _brouterService.InvokeOnError(
                new BrouterNavigationContext(from, to, CancellationToken.None), ex);
        }
        catch { /* OnError must never crash the navigation handler */ }
    }

    // Observability sink for route-lifecycle callback failures (IBrouterRoute handlers): surfaced
    // through IBrouter.OnError like loader/guard failures, fire-and-forget because lifecycle
    // callbacks must never block or fail the navigation (see BrouterRouteContext.Invoke).
    internal void ReportLifecycleError(BrouterNavigationContext ctx, Exception ex)
    {
        _ = SafeInvokeOnError(ctx.From, ctx.To, ex).AsTask();
    }

    // Same sink for lifecycle work that happens outside any navigation (teardown notifications:
    // route removal, outlet unmount), where only the current location is meaningful.
    internal void ReportLifecycleError(BrouterLocation location, Exception ex)
    {
        _ = SafeInvokeOnError(location, location, ex).AsTask();
    }

    /// <summary>
    /// Pure: builds a <see cref="BrouterLocation"/> from the current <c>NavigationManager.Uri</c>
    /// (and the current entry's <c>HistoryEntryState</c>). Does not mutate
    /// <see cref="CurrentLocation"/>. Never throws: an off-base URL or other malformed input is
    /// normalized to an empty-path location so the navigation pipeline can run and surface the
    /// issue through NotFound / OnError instead of crashing the handler.
    /// </summary>
    private BrouterLocation ComputeLocation() => ComputeLocation(_navManager.Uri, _navManager.HistoryEntryState);

    /// <summary>
    /// Pure: builds a <see cref="BrouterLocation"/> from an arbitrary absolute URI. Used by the
    /// LocationChanging handler, where the navigation has not committed yet so we must resolve the
    /// pending target URL (from <c>LocationChangingContext.TargetLocation</c>) rather than the still
    /// current <c>NavigationManager.Uri</c>. Shares all normalization with the no-arg overload so a
    /// location computed during the "changing" phase is identical to the one recomputed after commit.
    /// <paramref name="historyState"/> is the history-entry state travelling with the navigation
    /// (from <c>NavigationManager.HistoryEntryState</c> after commit, or
    /// <c>LocationChangingContext.HistoryEntryState</c> for a pending target).
    /// </summary>
    private BrouterLocation ComputeLocation(string uri, string? historyState = null)
    {
        // ToBaseRelativePath throws ArgumentException if the current Uri is not within
        // NavigationManager.BaseUri (base href misconfigured, programmatic NavigateTo to an
        // off-base absolute URL, etc.). Don't propagate: that would kill an async-void
        // handler permanently. Synthesize an empty-path location so the pipeline runs and
        // typically routes through NotFound, which surfaces the issue cleanly.
        string raw;
        try
        {
            raw = _navManager.ToBaseRelativePath(uri);
        }
        catch (ArgumentException)
        {
            return new BrouterLocation(uri, "/", [], "", "", historyState: historyState);
        }

        var hashIndex = raw.IndexOf('#');
        var hash = string.Empty;
        if (hashIndex >= 0)
        {
            hash = raw[hashIndex..];
            raw = raw[..hashIndex];
        }

        var queryIndex = raw.IndexOf('?');
        var query = string.Empty;
        if (queryIndex >= 0)
        {
            query = raw[queryIndex..];
            raw = raw[..queryIndex];
        }

        var path = "/" + raw;
        // Detect a meaningful trailing slash before any normalization, so that under
        // Options.IgnoreTrailingSlash == false we can distinguish "/users/" from "/users"
        // during matching. The split below drops the trailing empty segment unconditionally,
        // so without this flag the option would have no effect on route matching.
        var hasTrailingSlash = Options.IgnoreTrailingSlash is false && path.Length > 1 && path[^1] == '/';
        if (Options.IgnoreTrailingSlash && path.Length > 1 && path[^1] == '/')
        {
            path = path[..^1];
        }

        var rawSegments = path.Trim('/').Split(_separators, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < rawSegments.Length; i++)
        {
            // Decode defensively: malformed percent-encoding (e.g. "%ZZ" or a stray "%") would
            // otherwise throw UriFormatException. Falling back to the raw segment lets the
            // pipeline run normally - the bad URL typically won't match any route, which routes
            // the request through NotFound/OnError as it should.
            try
            {
                rawSegments[i] = Uri.UnescapeDataString(rawSegments[i]);
            }
            catch (UriFormatException) { /* keep the raw, still-escaped segment */ }
        }

        return new BrouterLocation(uri, path, rawSegments, query, hash, hasTrailingSlash, historyState);
    }

    // Cache the most recently-computed normalization. NotFoundUrl is typically a constant per
    // Brouter instance, and BuildRenderTree calls IsSamePath on every render (NotFound
    // fallback check). One-slot cache is enough; on a NotFoundUrl parameter change the cached
    // entry is replaced.
    private string? _isSamePathCacheTarget;
    private string? _isSamePathCacheNormalized;

    /// <summary>
    /// Compares an already-normalized <paramref name="currentPath"/> (as produced by
    /// <see cref="ComputeLocation()"/>) against an arbitrary target URL/path. Returns true
    /// when their normalized path components are equal.
    /// </summary>
    /// <remarks>
    /// Used by the not-found logic to detect the "we're already at the NotFoundUrl target"
    /// case without triggering a redirect loop. The target may be absolute, base-relative,
    /// trailing-slash, query-bearing, or fragment-bearing; we strip query/fragment, drop
    /// the trailing slash under <see cref="BrouterOptions.IgnoreTrailingSlash"/>, and apply
    /// the same case sensitivity rule the matcher uses for literal segments.
    /// </remarks>
    private bool IsSamePath(string currentPath, string target)
    {
        if (string.IsNullOrEmpty(target)) return false;

        string targetPath;
        if (ReferenceEquals(_isSamePathCacheTarget, target)
            || string.Equals(_isSamePathCacheTarget, target, StringComparison.Ordinal))
        {
            // Cache hit: skip the ToAbsoluteUri / ToBaseRelativePath / split work.
            // _isSamePathCacheNormalized is null only when the previous call returned false
            // for an off-base/malformed target; replicate that result.
            if (_isSamePathCacheNormalized is null) return false;
            targetPath = _isSamePathCacheNormalized;
        }
        else
        {
            string raw;
            try
            {
                // ToAbsoluteUri + ToBaseRelativePath gives us the canonical base-relative form
                // for absolute URLs, base-relative paths, and "/"-prefixed paths alike.
                var abs = _navManager.ToAbsoluteUri(target);
                raw = _navManager.ToBaseRelativePath(abs.ToString());
            }
            catch (Exception ex) when (ex is ArgumentException or UriFormatException or InvalidOperationException)
            {
                // Off-base or malformed target: not equal to anything we'd legitimately be at.
                _isSamePathCacheTarget = target;
                _isSamePathCacheNormalized = null;
                return false;
            }

            var qIdx2 = raw.IndexOf('?');
            if (qIdx2 >= 0) raw = raw[..qIdx2];
            var hIdx2 = raw.IndexOf('#');
            if (hIdx2 >= 0) raw = raw[..hIdx2];

            targetPath = "/" + raw;
            if (Options.IgnoreTrailingSlash && targetPath.Length > 1 && targetPath[^1] == '/')
            {
                targetPath = targetPath[..^1];
            }

            _isSamePathCacheTarget = target;
            _isSamePathCacheNormalized = targetPath;
        }

        var comparison = Options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return string.Equals(currentPath, targetPath, comparison);
    }

    /// <summary>
    /// The navigation commit pipeline: publishes the target location, (re)matches the route, runs
    /// loaders and renders. When <paramref name="decisionAlreadyMade"/> is true the preventive
    /// <see cref="OnLocationChanging"/> phase has already run the OnNavigating hooks and guards for
    /// this target and approved it, so those side-effecting steps (and the cancel/redirect handling)
    /// are skipped here to avoid running them twice. When false - the initial load, a forceLoad, or a
    /// navigation the changing handler never observed - the full pipeline runs, including guards and
    /// the reactive URL-restore fallback in <see cref="HandleSideEffects"/>.
    /// </summary>
    private async ValueTask ProcessNavigationAsync(BrouterLocation from, BrouterLocation to, bool decisionAlreadyMade, BrouterNavigationType navType)
    {
        // Now that we own the renderer's dispatcher (via InvokeAsync from the LocationChanged
        // handler, or directly from OnAfterRenderAsync for the initial render), publish the
        // target location atomically with the start of this pipeline. The whole pipeline below
        // reads `to` rather than CurrentLocation, so a later navigation publishing a newer
        // CurrentLocation cannot make our `ctx.To` desync from what we're matching against.
        CurrentLocation = to;

        // Supersede any in-flight navigation work.
        var version = Interlocked.Increment(ref _navVersion);
        // A navigation (never a revalidation) starting is what invalidates staged lifecycle
        // arrivals - see _lifecycleNavGeneration.
        _lifecycleNavGeneration++;
        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _navCts, newCts);
        // Cancel the previous navigation if any. We do NOT dispose oldCts here: the
        // superseded pipeline may still be observing its token (e.g. inside an awaited
        // user guard/loader, or via OperationCanceledException continuations) and disposing
        // would race that with ObjectDisposedException. The superseded pipeline disposes
        // its own CTS in its `finally` block once it returns. See ProcessNavigationAsync's
        // finally below.
        oldCts?.Cancel();
        var token = newCts.Token;

        var ctx = new BrouterNavigationContext(from, to, token) { NavigationType = navType };
        var service = _brouterService;

        // Awaited-navigation bookkeeping: capture the awaiter this pipeline is responsible for (the
        // one registered for exactly this target), and supersede any pending awaiter for a DIFFERENT
        // target - a pipeline for another URL starting is precisely what "superseded" means for it.
        var myOutcome = string.Equals(_pendingOutcomeUri, to.FullUri, StringComparison.Ordinal) ? _pendingOutcome : null;
        if (myOutcome is null && _pendingOutcome is not null)
        {
            var stale = _pendingOutcome;
            _pendingOutcome = null;
            _pendingOutcomeUri = null;
            stale.TrySetResult(BrouterNavigationOutcome.Superseded());
        }

        // A fresh navigation supersedes any previously rendered error boundary; if it fails too,
        // RenderNavigationError re-establishes the right one for the new failure.
        _navError = null;

        // View-transition state for THIS pipeline: started = JS snapshotted the outgoing DOM and
        // holds an open update promise; staged = the success path scheduled its completion for
        // OnAfterRenderAsync. The finally block completes a started-but-unstaged transition so a
        // failed/superseded navigation can never leave the browser holding a frozen snapshot.
        var viewTransitionStarted = false;
        var viewTransitionStaged = false;

        try
        {
            // Remember where the page we're leaving was scrolled to, keyed by its URL, so a later
            // Back/Forward to it can restore the position. Done here - before StateHasChanged renders
            // the new route - so the JS side reads the OUTGOING page's scroll offset, not the new
            // one's. Awaited so the read is ordered ahead of the render batch on Blazor Server too.
            // No-op unless Options.RestoreScrollPosition is enabled and `from` is a real page.
            await service.SaveScrollPositionAsync(from);

            // No ConfigureAwait(false) anywhere in this pipeline: subsequent calls
            // (StateHasChanged, NavigationManager.NavigateTo, route/component state mutations,
            // Outlet rendering) require the Blazor renderer's synchronization context.
            //
            // OnNavigating (and its cancel/redirect handling) only runs when the preventive changing
            // phase did NOT already run it. When decisionAlreadyMade is true, OnLocationChanging has
            // run these hooks and approved the navigation, so re-running them here would double-fire
            // side effects.
            if (decisionAlreadyMade is false)
            {
                // Lazy route loading for navigations the preventive phase never saw (notably the
                // initial load, where a deep link may target a lazily-loaded page).
                await RunOnNavigateHookAsync(ctx);
                if (HandleSideEffects(ctx, from)) return;
                if (token.IsCancellationRequested || version != _navVersion) return;

                // Leave guards for navigations the preventive phase never saw (initial load never has
                // a committed chain, but a forceLoad-less nav that outran interception does). On this
                // reactive path a cancel restores the URL via HandleSideEffects instead of preventing it.
                var leaveOk = await InvokeLeaveGuardsAsync(to, ctx);
                if (HandleSideEffects(ctx, from)) return;
                if (token.IsCancellationRequested || version != _navVersion) return;
                if (leaveOk is false) return;

                await service.InvokeOnNavigating(ctx);
                if (HandleSideEffects(ctx, from)) return;
                if (token.IsCancellationRequested || version != _navVersion) return;
            }

            // Snapshot the route list before any awaits / chain walks below: routes can register
            // or unregister during awaits (component lifecycle on the renderer dispatcher), and
            // the chain walks (winner.Parent) read state we mustn't see torn. The snapshot is
            // reused across navigations while the registration set is stable - see GetRoutesSnapshot.
            var routesSnapshot = GetRoutesSnapshot();

            // Reset the previous match's render flags before selecting the new winner. This lives in
            // the commit phase (never the preventive changing phase): blanking Matched before the URL
            // commits could unrender the current route while a guard is still deciding. No render can
            // interleave between here and the SetMatched below (no StateHasChanged until the end), so
            // the reset is invisible to the user. CurrentError travels with Matched: a route that was
            // an error boundary for the previous navigation must not re-render stale error UI when a
            // later navigation matches it again.
            foreach (var r in routesSnapshot)
            {
                r.Matched = false;
                r.CurrentError = null;
            }

            // Match is pure (SelectWinner never mutates a route), so the same selection runs in both
            // the changing and commit phases and yields the same winner for a stable route set.
            var winnerMatch = SelectWinner(to);

            if (winnerMatch is null)
            {
                _noRouteMatched = true;
                // Nothing routed is on screen once the fallback renders below; leave guards of the
                // previous chain already ran for this navigation. Either way this navigation's
                // awaited outcome is NotFound (resolved before any NotFoundUrl redirect fires).
                //
                // Everything routed leaves the screen (the fallback - or the NotFoundUrl redirect's
                // eventual commit - replaces it): notify route-lifecycle departures while the
                // departing content is still alive, before any render unmounts it.
                NotifyChainDepartures(ctx, _committedChain);
                // Departure callbacks run synchronously into user code and can start a new
                // navigation; when that happened, the newer pipeline owns the committed state now.
                if (token.IsCancellationRequested || version != _navVersion) return;
                _committedChain = [];
                _currentRouteData = null;
                ResolveNavigationOutcome(to.FullUri, BrouterNavigationOutcome.NotFound());

                // OnNotFound + the preventive NotFoundUrl redirect already ran in the changing phase
                // when decisionAlreadyMade is true; only run them here for the full-pipeline path.
                if (decisionAlreadyMade is false)
                {
                    if (OnNotFound is not null) await OnNotFound(to);

                    // The OnNotFound handler may have awaited; if a newer navigation has started or
                    // this one was cancelled in the meantime, abandon the fallback path so we don't
                    // redirect/render on behalf of a superseded navigation.
                    if (token.IsCancellationRequested || version != _navVersion) return;

                    if (string.IsNullOrEmpty(NotFoundUrl) is false)
                    {
                        // Avoid a self-redirect loop when the current URL is already the NotFoundUrl target
                        // (and still doesn't match any route). Render the fallback UI instead.
                        // Compare normalized base-relative paths rather than raw absolute URIs:
                        // "http://host/x" vs "http://host/x/" or vs "http://host/x?foo=1" would
                        // otherwise miss the equality check and trigger an infinite redirect loop
                        // (the NotFoundUrl keeps not matching, we keep navigating to it).
                        if (IsSamePath(to.Path, NotFoundUrl) is false)
                        {
                            NavigateInternal(NotFoundUrl);
                            return;
                        }
                    }
                }
#if NET10_0_OR_GREATER
                // During static SSR/prerender an unmatched URL must produce an HTTP 404 response, not
                // a 200 whose body happens to contain fallback HTML (an SEO/correctness bug).
                // NavigationManager.NotFound() is the framework's channel for that status code (and it
                // drives UseStatusCodePagesWithReExecute re-execution when configured). Interactive
                // renderers show the fallback UI below instead - there is no response status to set.
                if (decisionAlreadyMade is false && IsInteractiveRuntime is false)
                {
                    try
                    {
                        _raisingFrameworkNotFound = true;
                        _navManager.NotFound();
                    }
                    finally
                    {
                        _raisingFrameworkNotFound = false;
                    }
                }
#endif
                StateHasChanged();
                return;
            }

            _noRouteMatched = false;

            var winner = winnerMatch.Value.Route;

            // Commit the winner's matched parameters / constraints. Until this point Match was
            // pure, so candidates that lost have not had their Parameters/Constraints touched
            // (avoiding a race where a still-rendering, previously-matched route gets blanked).
            winner.Parameters = winnerMatch.Value.Parameters;
            winner.ConstraintsByParameter = winnerMatch.Value.ConstraintsByParameter;

            ctx.Route = winner;
            ctx.Parameters = new BrouterRouteParameters(winner.Parameters);

            // Guards + RedirectTo run only on the full-pipeline path. When decisionAlreadyMade is
            // true, the changing phase already ran the guard chain and honoured RedirectTo (a
            // RedirectTo route would have redirected there, so this commit is only reached for
            // routes that render).
            if (decisionAlreadyMade is false)
            {
                // Guards run before RedirectTo so a guard can still authorize/cancel/redirect-elsewhere
                // (e.g. an auth guard on a redirect route, or a parent guard inherited via the chain).
                // For routes without any guards in the chain, InvokeGuardsAsync is effectively a no-op,
                // so pure redirect routes still redirect immediately below.
                var guardsOk = await winner.InvokeGuardsAsync(ctx);
                if (HandleSideEffects(ctx, from)) return;
                if (token.IsCancellationRequested || version != _navVersion) return;
                if (guardsOk is false) return;

                // RedirectTo: once guards pass, redirect instead of running loaders/rendering. This honors
                // the documented "redirects to the given URL instead of rendering anything" contract even
                // when Guard is also set.
                if (winner.RedirectTo is not null)
                {
                    ResolveNavigationOutcome(to.FullUri, BrouterNavigationOutcome.Redirected(winner.RedirectTo));
                    NavigateInternal(winner.RedirectTo);
                    return;
                }
            }

            // Loaders. Walk root -> leaf so parent layouts get their data populated before
            // children run, mirroring guard ordering (see Route.InvokeGuardsAsync). Reset
            // LoadedData on every route in the matched chain first so data from a previous
            // navigation can't leak into parent layouts whose current loader is null.
            // Capture each loader's result into a local before committing to shared state,
            // so a superseded navigation can't leave stale LoadedData on the route.
            //
            // Snapshot the chain BEFORE any await: a parent route can be disposed while
            // an await is in-flight (conditional rendering, route tree mutation), and we
            // must not walk a torn `Parent` chain afterwards.
            var matchedChain = new List<Broute>();
            for (var node = winner; node is not null; node = node.Parent) matchedChain.Add(node);
            matchedChain.Reverse();

            // Propagate matched parameter values from the winner into every ancestor in the
            // matched chain. Match() only ran on the winner (parents typically don't match
            // the longer URL by themselves), so without this step parent layouts would see
            // an empty cascading RouteParameters even when their template declares parameters
            // (e.g. parent "/users/{id}" + child "/edit"). An ancestor's template params are a
            // subset of the winner's, so we just copy the slice that the ancestor declares.
            foreach (var node in matchedChain)
            {
                if (ReferenceEquals(node, winner)) continue;

                var ancestorTemplate = node.RouteTemplate;
                if (ancestorTemplate is null) continue;

                var ancestorParams = new Dictionary<string, object?>();
                var ancestorConstraints = new Dictionary<string, string[]>();
                foreach (var seg in ancestorTemplate.TemplateSegments)
                {
                    if (seg.IsParameter is false) continue;
                    if (winner.Parameters.TryGetValue(seg.Value, out var val))
                        ancestorParams[seg.Value] = val;
                    if (winner.ConstraintsByParameter.TryGetValue(seg.Value, out var cons))
                        ancestorConstraints[seg.Value] = cons;
                }
                node.Parameters = ancestorParams;
                node.ConstraintsByParameter = ancestorConstraints;
            }

            foreach (var node in matchedChain) node.LoadedData = null;

            // Discard any loader results staged by a previous navigation: only the latest committed
            // navigation's data should be persisted at the end of prerender.
            if (_persistentState is not null) _loaderStateToPersist.Clear();

            // First pass: restore prerendered results, consult the stale-while-revalidate cache, and
            // collect the loaders that still need to run. The chain index is carried alongside each
            // node because it is part of the persistence key.
            List<(Broute Node, int ChainIndex)> pendingLoaders = [];
            var staleServed = false;
            for (int chainIndex = 0; chainIndex < matchedChain.Count; chainIndex++)
            {
                var node = matchedChain[chainIndex];
                if (node.Loader is null) continue;

                // Prerender bridge: if this loader already ran server-side and its result was persisted,
                // restore it and skip the fetch. The key is derived from the URL + chain position, which
                // are identical across the prerender and interactive passes, so restoration lines up.
                // Restored values also seed the cache so a later in-SPA return can reuse them.
                if (TryRestoreLoaderState(to, chainIndex, out var restored))
                {
                    node.LoadedData = restored;
                    CacheLoaderResult(node, to, restored);
                    continue;
                }

                // SWR cache: a fresh entry skips the loader; a stale one is served immediately with a
                // background refresh (Options.StaleReloadMode.Background) or treated as a miss
                // (Blocking). Preload-produced entries are readable even without a configured
                // StaleTime (see BrouterLoaderCache.TryGet).
                if (service.LoaderCache.TryGet(
                        BrouterLoaderCache.MakeKey(node.FullTemplate, to),
                        EffectiveStaleTime(node), Options.PreloadStaleTime, Options.LoaderCacheGcTime,
                        out var cached, out var isStale))
                {
                    if (isStale is false)
                    {
                        node.LoadedData = cached;
                        continue;
                    }
                    if (Options.StaleReloadMode == BrouterStaleReloadMode.Background)
                    {
                        node.LoadedData = cached;
                        staleServed = true;
                        continue;
                    }
                    // Blocking mode: stale counts as a miss - run the loader below.
                }

                pendingLoaders.Add((node, chainIndex));
            }

            // Set when a render in THIS pipeline has already unmounted the departing content and the
            // route-lifecycle departures were notified ahead of it; the commit point below then must
            // not notify them again.
            var departuresNotified = false;

            if (pendingLoaders.Count > 0)
            {
                // Reveal the pending-navigation UI lazily - only now that a loader is actually about to
                // run (restored/loaderless navigations never reach here, so they never flash it). The
                // matched chain's Matched flags were reset to false above and no render has happened yet,
                // so this StateHasChanged replaces the routed content with the Navigating fragment for the
                // duration of the await(s), then it's cleared before SetMatched reveals the new route.
                if (Navigating is not null && _navigating is false)
                {
                    // This render unmounts the whole committed chain (everything is unmatched for the
                    // duration of the load), so route-lifecycle departures fire now, while the content
                    // is still alive. Routes staying in the new chain are notified with
                    // willRemainMatched: retained keep-alive content merely hides for the duration
                    // (no event - it resolves as a renavigation at commit), while transient content
                    // really is disposed by this render and gets its Disposing notification.
                    NotifyChainDepartures(ctx, _committedChain, matchedChain, notifySurvivorsAsRemaining: true);
                    departuresNotified = true;

                    // A departure callback's synchronous prefix may have started a new navigation;
                    // don't reveal pending UI (or run loaders) on behalf of a superseded one.
                    if (token.IsCancellationRequested || version != _navVersion) return;

                    _navigating = true;
                    StateHasChanged();
                }

                if (ParallelLoaders && pendingLoaders.Count > 1)
                {
                    // Opt-in parallel mode: start every pending loader at once and await them all,
                    // then process results in the same root -> leaf order sequential mode uses, so
                    // commit and failure semantics are identical - only the awaiting overlaps.
                    // Each loader's synchronous prefix still executes root -> leaf here (concurrency
                    // begins at the first await inside a loader).
                    var loaderTasks = new Task<(object? Result, Exception? Error)>[pendingLoaders.Count];
                    for (var i = 0; i < pendingLoaders.Count; i++)
                    {
                        loaderTasks[i] = RunLoaderAsync(pendingLoaders[i].Node);
                    }

                    var results = await Task.WhenAll(loaderTasks);

                    async Task<(object? Result, Exception? Error)> RunLoaderAsync(Broute node)
                    {
                        try
                        {
                            return (await node.Loader!(ctx), null);
                        }
                        catch (Exception ex)
                        {
                            return (null, ex);
                        }
                    }

                    // During static server rendering / prerender, NavigationManager.NavigateTo throws
                    // NavigationException as the framework's redirect signal (a loader may redirect,
                    // e.g. an auth gate). It must unwind out of OnInitializedAsync so the endpoint can
                    // issue the HTTP redirect; swallowing it into OnError would drop the redirect
                    // entirely. Scan for it before any other error handling so an SSR redirect wins
                    // over a sibling loader's failure (root-most redirect wins if several threw).
                    // Interactive NavigateTo never throws, so this is inert outside SSR.
                    foreach (var (_, error) in results)
                    {
                        if (error is NavigationException)
                        {
                            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
                        }
                    }

                    for (var i = 0; i < results.Length; i++)
                    {
                        var (node, chainIndex) = pendingLoaders[i];
                        var (loaded, error) = results[i];

                        if (error is OperationCanceledException && token.IsCancellationRequested) return;
                        if (error is not null)
                        {
                            await service.InvokeOnError(ctx, error);
                            if (token.IsCancellationRequested || version != _navVersion) return;
                            RenderNavigationError(node, ctx, error);
                            return;
                        }

                        if (HandleSideEffects(ctx, from)) return;
                        if (token.IsCancellationRequested || version != _navVersion) return;

                        node.LoadedData = loaded;
                        CacheLoaderResult(node, to, loaded);

                        // Stage the result for persistence. It is written to PersistentComponentState
                        // only if the RegisterOnPersisting callback fires (i.e. during prerender);
                        // interactive passes stage it too but simply never get asked to persist.
                        if (_persistentState is not null)
                        {
                            _loaderStateToPersist[BroutePrerenderState.MakeKey(to.Path, to.Query, chainIndex)] = loaded;
                        }
                    }
                }
                else
                {
                    // Default sequential mode: each loader completes before the next starts, so a
                    // child's loader can rely on work its parent's loader has already done. The cost
                    // is that the total wait is the sum of the chain's loader times - opt into
                    // ParallelLoaders when the loaders are independent.
                    foreach (var (node, chainIndex) in pendingLoaders)
                    {
                        object? loaded;
                        try
                        {
                            loaded = await node.Loader!(ctx);
                        }
                        catch (OperationCanceledException) when (token.IsCancellationRequested)
                        {
                            return;
                        }
                        catch (NavigationException)
                        {
                            // SSR redirect signal - must unwind so the endpoint can issue the HTTP
                            // redirect (see the parallel branch's scan above for the full story).
                            throw;
                        }
                        catch (Exception ex)
                        {
                            await service.InvokeOnError(ctx, ex);
                            if (token.IsCancellationRequested is false && version == _navVersion)
                            {
                                RenderNavigationError(node, ctx, ex);
                            }
                            return;
                        }

                        if (HandleSideEffects(ctx, from)) return;
                        if (token.IsCancellationRequested || version != _navVersion) return;

                        node.LoadedData = loaded;
                        CacheLoaderResult(node, to, loaded);

                        // Stage the result for persistence. It is written to PersistentComponentState
                        // only if the RegisterOnPersisting callback fires (i.e. during prerender);
                        // interactive passes stage it too but simply never get asked to persist.
                        if (_persistentState is not null)
                        {
                            _loaderStateToPersist[BroutePrerenderState.MakeKey(to.Path, to.Query, chainIndex)] = loaded;
                        }
                    }
                }
            }

            // View transition: snapshot the outgoing DOM now - after loaders (so the snapshot isn't
            // held across arbitrary awaits) and immediately before the renders below mutate the page.
            // The completion (which lets the browser animate to the new state) runs in
            // OnAfterRenderAsync once the new DOM is committed.
            if (Options.ViewTransitions)
            {
                viewTransitionStarted = await service.BeginViewTransitionAsync(navType);
                if (token.IsCancellationRequested || version != _navVersion) return;
            }

            // Route-lifecycle departures for the routes this commit removes from the screen. They
            // must fire BEFORE SetMatched: its StateHasChanged renders synchronously on this
            // dispatcher, unmounting (and disposing) the departing content - and the Disposing
            // notification's contract is that its synchronous part runs first. Routes present in
            // both chains aren't notified here: their content survives the commit untouched (the
            // no-pending-render path never unmounted it) and resolves as a renavigation below.
            if (departuresNotified is false)
            {
                NotifyChainDepartures(ctx, _committedChain, matchedChain);
                // Departure callbacks run synchronously into user code and can start a new
                // navigation; abandon this commit before it mutates state the newer one owns.
                if (token.IsCancellationRequested || version != _navVersion) return;
            }

            // Pre-render arrival preparation: per-parameter keep-alive routes deactivate the
            // outgoing sibling entry now, while its instance is guaranteed alive - the commit
            // render below may LRU-evict it, and an evicted entry must never die still-active.
            PrepareArrivals(ctx, matchedChain);
            // Same synchronous-supersession hazard as the departures above.
            if (token.IsCancellationRequested || version != _navVersion) return;

            // Loaders are done: hide the pending-navigation UI. SetMatched marks the whole chain and
            // issues one render request at its topmost route, which renders the now-matched route in
            // the pending UI's place, so clearing the flag first avoids showing both at once.
            _navigating = false;
            winner.SetMatched();

            // Record what is now on screen so the next navigation's leave guards know which routes
            // they would deactivate.
            _committedChain = matchedChain.ToArray();

            // Publish the committed navigation's framework RouteData on the observer cascade (see
            // _currentRouteData). Content-fragment routes have no page type, so they publish null.
            // Route values carry explicit nulls for unfilled optional parameters, matching the
            // framework router's RouteData contract (see NormalizeRouteValues).
            _currentRouteData = winner.Component is not null
                ? new RouteData(winner.Component,
                    BrouterRouteRenderer.NormalizeRouteValues(winner.Parameters, winner.TemplateParameterNames))
                : null;

            if (OnMatch is not null) await OnMatch(winner);
            // Each await below can yield long enough for a newer navigation to start. If that
            // happens, bail out so we don't fire OnNavigated, scroll, or re-render on behalf
            // of a superseded navigation (and overwrite the new one's UI / scroll position).
            if (token.IsCancellationRequested || version != _navVersion) return;

            await service.InvokeOnNavigated(ctx);
            if (token.IsCancellationRequested || version != _navVersion) return;

            // Stage the post-navigation DOM effects (fragment/top scroll, focus). They can't run
            // here: fragment and focus selectors must resolve against the NEW route's DOM, which
            // isn't committed until the render triggered below flushes. OnAfterRenderAsync applies
            // them once that render lands. Only the latest staged location is ever applied, so a
            // superseded navigation can't scroll/focus on behalf of the page the user left.
            _pendingEffectsLocation = to;

            // Stage the route-lifecycle arrivals (activation / renavigation per route; see
            // IBrouterRoute). OnAfterRenderAsync fires them once the render below has landed: only
            // then is the new content mounted (so freshly created components have registered their
            // handlers) and the DOM available to activation callbacks. Staged after the last
            // supersession bail-outs above so an abandoned pipeline never strands stale arrivals.
            StageArrivals(ctx, matchedChain);

            StateHasChanged();

            // Hand the open transition to OnAfterRenderAsync: it completes once the render above has
            // been applied to the DOM, which is exactly when the browser should snapshot the new state.
            if (viewTransitionStarted)
            {
                viewTransitionStaged = true;
                _pendingViewTransitionCompletion = true;
            }

            ResolveNavigationOutcome(to.FullUri, BrouterNavigationOutcome.Success());

            // Stale-while-revalidate: some chain node rendered a stale cached result above - kick
            // off the background refresh now that the (stale) content is committed. Revalidation
            // re-runs the chain's loaders, updates the cache and re-renders with fresh data.
            if (staleServed)
            {
                _ = RevalidateAsync();
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // navigation was superseded; nothing to do
        }
        catch (NavigationException)
        {
            // SSR/prerender redirect signal (see the loader catch above). Let it propagate out of
            // OnInitializedAsync so the framework can turn it into an HTTP redirect. A guard or
            // OnNavigating handler that redirects via NavigationManager during prerender lands here.
            throw;
        }
        catch (Exception ex)
        {
            await service.InvokeOnError(ctx, ex);
            // Route the failure to an error boundary too (guard/OnMatch/hook errors land here; ctx.Route
            // carries the winner when matching got that far, so bubbling starts at the right depth).
            // Guarded by version so a superseded pipeline can't paint an error over the newer navigation.
            if (token.IsCancellationRequested is false && version == _navVersion)
            {
                RenderNavigationError(ctx.Route, ctx, ex);
            }
        }
        finally
        {
            // Safety net for the pending-navigation UI: if this navigation revealed it but bailed before
            // clearing it above (e.g. a loader threw and we routed to OnError), and we're still the current
            // navigation, hide it now. Guarded by version so a superseded pipeline can't clear the flag out
            // from under the newer navigation that may have just turned it on.
            if (_navigating && version == _navVersion)
            {
                _navigating = false;
                StateHasChanged();
            }

            // A started transition that never made it to the success staging (loader error, guard
            // side-effects, supersession) is released immediately: the browser must not sit on the
            // old-page snapshot waiting for a completion that will never come.
            if (viewTransitionStarted && viewTransitionStaged is false)
            {
                await service.CompleteViewTransitionAsync();
            }

            // A superseded pipeline resolves its own awaiter (a newer same-target registration was
            // already resolved by RegisterNavigationOutcome, making this TrySetResult a no-op; the
            // store is only cleared when it still points at OUR awaiter, so a newer registration is
            // never clobbered).
            if (myOutcome is not null && (token.IsCancellationRequested || version != _navVersion))
            {
                if (ReferenceEquals(_pendingOutcome, myOutcome))
                {
                    _pendingOutcome = null;
                    _pendingOutcomeUri = null;
                }
                myOutcome.TrySetResult(BrouterNavigationOutcome.Superseded());
            }

            // Dispose our CTS exactly when it can no longer be observed by any other path:
            //   - It's been superseded (a newer pipeline replaced _navCts), or
            //   - The Brouter has been disposed (Dispose() swapped _navCts out and disposed it).
            // While our CTS is still the active one, leave it alive: future supersedes need
            // to call Cancel() on it, and Dispose() needs to find a usable CTS to tear down.
            // CancellationTokenSource.Dispose() is idempotent, so a benign race with Dispose()
            // (which may have already disposed this same CTS) is safe.
            if (ReferenceEquals(Volatile.Read(ref _navCts), newCts) is false)
            {
                newCts.Dispose();
            }
        }
    }

    /// <summary>The freshness window this route's loader results cache under, or null for "no caching".</summary>
    private TimeSpan? EffectiveStaleTime(Broute node) => node.StaleTime ?? Options.DefaultLoaderStaleTime;

    /// <summary>
    /// Speculatively runs the loaders of the route chain that <paramref name="url"/> would match and
    /// stores the results in the SWR cache, so an actual navigation there finds warm data (see
    /// <see cref="BrouterLink.Preload"/>). Guards do NOT run and nothing renders; loaders observe
    /// <see cref="BrouterNavigationContext.IsPreload"/>. Nodes whose cache entry is still fresh are
    /// skipped, so repeated hover-triggers don't refetch. Failures are swallowed - a speculative
    /// fetch failing must neither surface error UI nor fire OnError; the real navigation will run
    /// the loader again and handle the error through the normal pipeline.
    /// </summary>
    internal Task PreloadAsync(string url) => InvokeAsync(() => PreloadCoreAsync(url).AsTask());

    private async ValueTask PreloadCoreAsync(string url)
    {
        BrouterLocation to;
        try
        {
            to = ComputeLocation(_navManager.ToAbsoluteUri(url).ToString());
        }
        catch (Exception ex) when (ex is ArgumentException or UriFormatException or InvalidOperationException)
        {
            return; // malformed target; the real navigation will surface it properly
        }

        var match = SelectWinner(to);
        if (match is null) return;

        var chain = new List<Broute>();
        for (var node = match.Value.Route; node is not null; node = node.Parent) chain.Add(node);
        chain.Reverse();

        var ctx = new BrouterNavigationContext(CurrentLocation, to, CancellationToken.None)
        {
            NavigationType = BrouterNavigationType.Push,
            IsPreload = true,
            Route = match.Value.Route,
            Parameters = new BrouterRouteParameters(match.Value.Parameters),
        };

        foreach (var node in chain)
        {
            if (node.Loader is null) continue;

            var key = BrouterLoaderCache.MakeKey(node.FullTemplate, to);
            // A still-fresh entry (from a previous preload or a committed navigation) needs no work.
            if (_brouterService.LoaderCache.TryGet(
                    key, EffectiveStaleTime(node) ?? Options.PreloadStaleTime,
                    Options.PreloadStaleTime, Options.LoaderCacheGcTime,
                    out _, out var isStale) && isStale is false)
            {
                continue;
            }

            object? loaded;
            try
            {
                loaded = await node.Loader(ctx);
            }
            catch
            {
                // Speculative fetch failed; the real navigation will re-run and report it.
                continue;
            }

            CacheLoaderResult(node, to, loaded, fromPreload: true);
        }
    }

    /// <summary>
    /// Stores a loader result in the SWR cache. Navigation/revalidation results are only stored for
    /// routes that participate in caching (an effective StaleTime); preload results always store,
    /// marked so lookups judge them against <see cref="BrouterOptions.PreloadStaleTime"/>.
    /// </summary>
    private void CacheLoaderResult(Broute node, BrouterLocation to, object? value, bool fromPreload = false)
    {
        if (fromPreload is false && EffectiveStaleTime(node) is null) return;
        _brouterService.LoaderCache.Set(
            BrouterLoaderCache.MakeKey(node.FullTemplate, to), value, Options.MaxLoaderCacheEntries, fromPreload);
    }

    /// <summary>
    /// The leave phase of the pending navigation to <paramref name="to"/>, leaf -> root (children
    /// veto before their parents, mirroring Angular's CanDeactivate order). For every currently
    /// committed route it runs, in order: the component-level navigation locks of its active
    /// content (<see cref="IBrouterRoute.OnDeactivatingAsync"/> when the route is being left,
    /// <see cref="IBrouterRoute.OnRenavigatingAsync"/> when it stays matched - so parameter changes
    /// are voteable too), then - only for routes actually being left - the route-declared
    /// <see cref="Broute.LeaveGuard"/>. Locks are awaited, so they can hold the navigation open for
    /// user input. Returns false when a lock/guard cancelled/redirected (the decision is on
    /// <paramref name="ctx"/> for the caller to apply) or the navigation was superseded; true to
    /// continue the pipeline. A throwing lock/guard propagates to the caller, which fails closed.
    /// </summary>
    private async ValueTask<bool> InvokeLeaveGuardsAsync(BrouterLocation to, BrouterNavigationContext ctx)
    {
        var committed = _committedChain;
        if (committed.Length == 0) return true;

        // Anything to do at all? Route-declared LeaveGuards and component-level locks (content
        // with registered lifecycle handlers, see IBrouterRoute.OnDeactivatingAsync) share this
        // phase; with neither present the (pure but non-free) SelectWinner below is skipped.
        var anyWork = false;
        foreach (var node in committed)
        {
            if (node.LeaveGuard is not null || node.HasActiveLifecycleHandlers()) { anyWork = true; break; }
        }
        if (anyWork is false) return true;

        // Which committed routes survive the new URL? Match it (SelectWinner is pure, so this is
        // safe pre-commit) and collect the new chain; committed routes present in it are updated,
        // not left. A null match means everything is being left.
        HashSet<Broute>? staying = null;
        if (SelectWinner(to) is { } newMatch)
        {
            staying = [];
            for (var node = newMatch.Route; node is not null; node = node.Parent) staying.Add(node);
        }

        // Leaf -> root, and per route the content's own locks run before the route-declared
        // LeaveGuard - innermost first, so the code closest to the state at risk gets the first
        // veto. Locks are awaited (they may hold the navigation open for a custom confirmation
        // dialog); the first cancel/redirect anywhere settles the phase and skips everything else.
        List<BrouterRouteContext>? lockContexts = null;
        BrouterRouteRenavigatingContext? renavigating = null;

        for (var i = committed.Length - 1; i >= 0; i--)
        {
            var node = committed[i];
            var stays = staying is not null && staying.Contains(node);

            // 1. Component-level locks. A route being left dispatches OnDeactivatingAsync to its
            // active content; a route that stays matched dispatches OnRenavigatingAsync instead
            // (a parameter change is not a "leave", but a dirty form must still be able to veto
            // it - Vue's beforeRouteUpdate). The Hidden/Disposing reason mirrors the retention
            // that would follow the commit: keep-alive content is Hidden only when its retained
            // subtree actually survives the leave - a kept child whose outlet host is itself being
            // torn down dies with the host and honestly reports Disposing (see
            // KeptContentSurvivesLeave). One pre-commit nuance: on a per-parameter keep-alive
            // route (KeepAliveMax > 1) that stays matched across a parameter change, the active
            // entry receives OnRenavigating here even though the commit then surfaces as a Hidden
            // deactivation + sibling activation - the new parameter key isn't known until the
            // navigation's parameters commit. The reason is per-context: retention only ever
            // preserves primary content, so a keep-alive route's named-view contexts (collected
            // after the primary content's, see Broute.CollectActiveRouteContexts) always report
            // Disposing even when the primary content reports Hidden.
            if (node.HasActiveLifecycleHandlers())
            {
                lockContexts ??= [];
                lockContexts.Clear();
                var namedFrom = node.CollectActiveRouteContexts(lockContexts);

                for (var c = 0; c < lockContexts.Count; c++)
                {
                    var context = lockContexts[c];
                    if (ctx.CancellationToken.IsCancellationRequested) return false;
                    if (stays)
                    {
                        renavigating ??= new BrouterRouteRenavigatingContext(ctx);
                        await context.FireRenavigatingAsync(renavigating);
                    }
                    else
                    {
                        var reason = c < namedFrom
                            && node.KeepAlive && KeptContentSurvivesLeave(node, staying)
                            ? BrouterRouteDeactivationReason.Hidden
                            : BrouterRouteDeactivationReason.Disposing;
                        await context.FireDeactivatingAsync(new BrouterRouteDeactivatingContext(ctx, reason));
                    }
                    if (ctx.CancellationToken.IsCancellationRequested) return false;
                    if (ctx.IsCancelled || ctx.RedirectUrl is not null) return false;
                }
            }

            // 2. The route-declared LeaveGuard, only when the route is actually being left.
            if (stays || node.LeaveGuard is null) continue;

            if (ctx.CancellationToken.IsCancellationRequested) return false;
            // Expose the route being left for the duration of its own guard call only.
            ctx.Route = node;
            try
            {
                await node.LeaveGuard(ctx);
            }
            finally
            {
                ctx.Route = null;
            }
            if (ctx.CancellationToken.IsCancellationRequested) return false;
            if (ctx.IsCancelled || ctx.RedirectUrl is not null) return false;
        }

        return true;
    }

    /// <summary>
    /// Whether a keep-alive route's content actually survives being left, for the lock phase's
    /// Hidden/Disposing reason: retention keeps the subtree mounted (hidden) inside its outlet
    /// host's content, so the retained content only lives while every hosting content stays
    /// mounted. Walking the hosting chain: a host that stays matched keeps its content (and
    /// outlets) mounted; a keep-alive host being left keeps hosting from its own retained hidden
    /// content, so the walk continues at ITS host; a transient host being left unmounts everything
    /// inside it, kept children included (see
    /// <c>KeepAliveTests.KeepAlive_state_is_lost_across_the_hosting_layout_unmount</c>). Inline
    /// content renders at the route's declaration site, which stays mounted regardless of matching.
    /// </summary>
    private static bool KeptContentSurvivesLeave(Broute node, HashSet<Broute>? staying)
    {
        for (var host = node.FindOutletHost(); host is not null; host = host.FindOutletHost())
        {
            if (staying is not null && staying.Contains(host)) return true;
            if (host.KeepAlive is false) return false;
        }
        return true;
    }

    /// <summary>
    /// Routes a commit-phase navigation failure to the nearest error boundary: walking leaf -> root
    /// from <paramref name="origin"/> (the failed route, when known), the first route with an
    /// <see cref="Broute.ErrorContent"/> renders it in place of its content - ancestor layouts above
    /// the boundary keep rendering. With no route boundary in the chain, the router-level
    /// <see cref="ErrorContent"/> renders instead. With neither, this is a no-op and the previous
    /// page simply stays visible (the pre-boundary behavior). The <see cref="IBrouter.OnError"/>
    /// hook has already fired by the time this runs - boundaries are UI, not observability.
    /// </summary>
    private void RenderNavigationError(Broute? origin, BrouterNavigationContext ctx, Exception ex)
    {
        ResolveNavigationOutcome(ctx.To.FullUri, BrouterNavigationOutcome.Failed(ex));

        var errorContext = new BrouterErrorContext(ex, ctx.To, this);

        for (var node = origin; node is not null; node = node.Parent)
        {
            if (node.ErrorContent is not null)
            {
                // The boundary and its ancestors are what's on screen after this commit; compute the
                // chain first so route-lifecycle departures can fire for everything it evicts -
                // BEFORE SetMatched's synchronous render unmounts/disposes that content. A pipeline
                // whose pending-navigation render already notified them is fine: departures are
                // idempotent. The boundary node itself is deliberately NOT treated as surviving:
                // when it was on screen, the CurrentError render REPLACES its committed content
                // with ErrorContent - disposing the old page even when the route is keep-alive -
                // so it gets an honest, forced-Disposing departure (ending its content session;
                // the error UI then activates as a fresh one) instead of a renavigation or Hidden
                // notification delivered to a destroyed instance.
                var chain = new List<Broute>();
                for (var n = node; n is not null; n = n.Parent) chain.Add(n);
                chain.Reverse();

                var surviving = new List<Broute>(chain);
                surviving.Remove(node);
                NotifyChainDepartures(ctx, _committedChain, surviving, contentReplacedNode: node);

                // Departure callbacks run synchronously into user code and can start a new
                // navigation (cancelling this one's token); the newer pipeline owns the
                // committed/error state then - don't paint the boundary over it.
                if (ctx.CancellationToken.IsCancellationRequested) return;

                node.CurrentError = errorContext;
                // SetMatched marks node + ancestors and issues the render; node's renderer emits
                // ErrorContent instead of Content/Component while CurrentError is set. Descendants
                // of the boundary stay unmatched (the winner was never SetMatched on this path).
                node.SetMatched();

                // Record the boundary chain so a later navigation still runs their leave guards.
                _committedChain = chain.ToArray();

                // Stage the route-lifecycle arrivals for the boundary chain (ancestor layouts that
                // stayed resolve as renavigations; a freshly mounted layout - or the boundary's
                // error content itself - activates). Flushed from OnAfterRenderAsync like a normal
                // commit's arrivals.
                StageArrivals(ctx, chain);

                // The failed target's page never rendered - the boundary is on screen in its place -
                // so the observer cascade must stop publishing the previous page's RouteData.
                // StateHasChanged re-emits the cascade (SetMatched only re-renders the routes).
                _currentRouteData = null;
                StateHasChanged();
                return;
            }
        }

        if (ErrorContent is not null)
        {
            // The router-level boundary evicts everything routed: notify departures while the
            // departing content is still alive, before the render below unmounts it.
            NotifyChainDepartures(ctx, _committedChain);

            // Same synchronous-supersession hazard as the route-boundary branch above.
            if (ctx.CancellationToken.IsCancellationRequested) return;

            _navError = errorContext;
            _committedChain = [];
            _currentRouteData = null;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Re-runs the full navigation pipeline (guards, loaders, render) for the current URL. Exposed
    /// to error boundaries via <see cref="BrouterErrorContext.RetryAsync"/>. Runs as a fresh
    /// full-pipeline pass - guards deliberately re-run, since a retried navigation must re-establish
    /// the same authorization a first attempt would.
    /// </summary>
    internal Task RetryNavigationAsync()
    {
        var to = ComputeLocation();
        // Replace, not Push: the retry re-processes the entry the user is already on.
        return InvokeAsync(() => ProcessNavigationAsync(CurrentLocation, to, decisionAlreadyMade: false, BrouterNavigationType.Replace).AsTask());
    }

    /// <summary>
    /// Re-runs the loaders of the currently committed route chain and re-renders with the fresh
    /// data - the router-level primitive for "a mutation happened, refresh what's on screen"
    /// (React Router's revalidation / SvelteKit's invalidateAll). Not a navigation: the URL is
    /// unchanged, guards and OnNavigating/OnNavigated do not run, and the current content stays
    /// visible while loaders work (stale-while-revalidate, no pending-UI flash). Participates in
    /// the navigation supersession machinery, so a real navigation starting mid-revalidate cancels
    /// it (and vice versa an in-flight navigation's loaders are superseded by the revalidate).
    /// Loader failures route to error boundaries and OnError exactly like navigation loads.
    /// </summary>
    internal Task RevalidateAsync() => InvokeAsync(() => RevalidateCoreAsync().AsTask());

    private async ValueTask RevalidateCoreAsync()
    {
        var chain = _committedChain;
        if (chain.Length == 0) return;

        var anyLoader = false;
        foreach (var node in chain)
        {
            if (node.Loader is not null) { anyLoader = true; break; }
        }
        if (anyLoader is false) return;

        var to = CurrentLocation;
        var leaf = chain[^1];

        // Same supersession discipline as ProcessNavigationAsync: bumping the version and swapping
        // the CTS makes this revalidate cancel an in-flight navigation's awaits, and makes any
        // navigation that starts later cancel this revalidate.
        var version = Interlocked.Increment(ref _navVersion);
        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _navCts, newCts);
        oldCts?.Cancel();
        var token = newCts.Token;

        var ctx = new BrouterNavigationContext(to, to, token)
        {
            // The history entry is untouched; Replace is the closest classification and
            // IsRevalidation is the authoritative signal for loader logic.
            NavigationType = BrouterNavigationType.Replace,
            IsRevalidation = true,
            Route = leaf,
            Parameters = new BrouterRouteParameters(leaf.Parameters),
        };
        var service = _brouterService;

        try
        {
            // Collect the loaders up front; results commit only after each completes so the screen
            // keeps showing the previous data until fresh data is actually available.
            List<Broute> pendingLoaders = [];
            foreach (var node in chain)
            {
                if (node.Loader is not null) pendingLoaders.Add(node);
            }

            if (ParallelLoaders && pendingLoaders.Count > 1)
            {
                var loaderTasks = new Task<(object? Result, Exception? Error)>[pendingLoaders.Count];
                for (var i = 0; i < loaderTasks.Length; i++)
                {
                    loaderTasks[i] = RunLoaderAsync(pendingLoaders[i]);
                }

                var results = await Task.WhenAll(loaderTasks);

                async Task<(object? Result, Exception? Error)> RunLoaderAsync(Broute node)
                {
                    try { return (await node.Loader!(ctx), null); }
                    catch (Exception ex) { return (null, ex); }
                }

                for (var i = 0; i < results.Length; i++)
                {
                    var (loaded, error) = results[i];
                    if (error is OperationCanceledException && token.IsCancellationRequested) return;
                    if (error is not null)
                    {
                        await service.InvokeOnError(ctx, error);
                        if (token.IsCancellationRequested is false && version == _navVersion)
                        {
                            RenderNavigationError(pendingLoaders[i], ctx, error);
                        }
                        return;
                    }
                    if (token.IsCancellationRequested || version != _navVersion) return;
                    pendingLoaders[i].LoadedData = loaded;
                    CacheLoaderResult(pendingLoaders[i], to, loaded);
                }
            }
            else
            {
                foreach (var node in pendingLoaders)
                {
                    object? loaded;
                    try
                    {
                        loaded = await node.Loader!(ctx);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        await service.InvokeOnError(ctx, ex);
                        if (token.IsCancellationRequested is false && version == _navVersion)
                        {
                            RenderNavigationError(node, ctx, ex);
                        }
                        return;
                    }

                    if (token.IsCancellationRequested || version != _navVersion) return;
                    node.LoadedData = loaded;
                    CacheLoaderResult(node, to, loaded);
                }
            }

            // Fresh LoadedData references make the renderers rebuild their BrouterRouteData wrappers,
            // which re-notifies every cascading subscriber; one render request at the leaf covers the
            // whole chain (see SetMatched).
            leaf.SetMatched();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // superseded by a navigation; nothing to do
        }
        catch (Exception ex)
        {
            await service.InvokeOnError(ctx, ex);
            if (token.IsCancellationRequested is false && version == _navVersion)
            {
                RenderNavigationError(leaf, ctx, ex);
            }
        }
        finally
        {
            // Same CTS ownership rule as ProcessNavigationAsync's finally.
            if (ReferenceEquals(Volatile.Read(ref _navCts), newCts) is false)
            {
                newCts.Dispose();
            }
        }
    }

    private bool HandleSideEffects(BrouterNavigationContext ctx, BrouterLocation from)
    {
        if (ctx.RedirectUrl is not null)
        {
            // Resolve before NavigateInternal for the same reason as ApplyPreventiveDecision: the
            // redirect's pipeline must not observe (and supersede) the awaiter we're concluding.
            ResolveNavigationOutcome(ctx.To.FullUri, BrouterNavigationOutcome.Redirected(ctx.RedirectUrl));
            NavigateInternal(ctx.RedirectUrl);
            return true;
        }

        if (ctx.IsCancelled)
        {
            ResolveNavigationOutcome(ctx.To.FullUri, BrouterNavigationOutcome.Cancelled());
            // Restore the address bar. If From is empty (initial render), we leave the URL alone.
            if (string.IsNullOrEmpty(from.FullUri) is false &&
                string.Equals(from.FullUri, ctx.To.FullUri, StringComparison.Ordinal) is false)
            {
                NavigateInternal(from.FullUri, replace: true);
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Result of a single match attempt. Pure value type: matching never mutates the route,
    /// so candidates that lose can't blank a previously-matched, still-rendering route.
    /// </summary>
    private readonly struct MatchResult
    {
        public Broute Route { get; }
        public Dictionary<string, object?> Parameters { get; }
        public Dictionary<string, string[]> ConstraintsByParameter { get; }

        public MatchResult(Broute route,
                           Dictionary<string, object?> parameters,
                           Dictionary<string, string[]> constraintsByParameter)
        {
            Route = route;
            Parameters = parameters;
            ConstraintsByParameter = constraintsByParameter;
        }
    }

    /// <summary>
    /// Matches <paramref name="to"/> against the registered routes and returns the winning
    /// <see cref="MatchResult"/>, or null when nothing matches. Pure: never mutates a route (in
    /// particular it does not touch <c>Broute.Matched</c>), so it is safe to call from the preventive
    /// changing phase (where the current route is still rendered) as well as the commit phase. Both
    /// phases run identical selection, so an approved changing decision and its commit agree on the winner.
    /// </summary>
    private MatchResult? SelectWinner(BrouterLocation to)
    {
        var index = GetRouteIndex();
        var segments = to.SegmentsArray;

        MatchResult best = default;
        var bestOrder = 0;
        var haveBest = false;

        // Only routes whose first template segment can match the URL's first segment are viable: the
        // bucket keyed on that segment, plus every route that doesn't start with a fixed literal.
        // This is what keeps matching off a full O(routes) scan when routes number in the hundreds.
        if (segments.Length > 0 &&
            index.LiteralBuckets.TryGetValue(segments[0], out var literalEntries))
        {
            foreach (var entry in literalEntries)
                ConsiderCandidate(entry, to, segments, ref best, ref bestOrder, ref haveBest);
        }

        foreach (var entry in index.NonLiteralFirst)
            ConsiderCandidate(entry, to, segments, ref best, ref bestOrder, ref haveBest);

        return haveBest ? best : null;
    }

    /// <summary>
    /// Runs <see cref="TryMatch"/> for one candidate and, on a match, keeps it as the running best when
    /// it beats the current one. Ranking: most specific wins; ties broken by deeper nesting (so an
    /// index child wins over its parent when their full templates are identical), then by index-route
    /// preference, then by earliest registration order. The explicit order tiebreak reproduces the old
    /// full-scan behavior (which kept the first candidate on a tie) regardless of the order the index
    /// visits routes in. Exact-duplicate templates are rejected at registration (see
    /// <see cref="RegisterRoute"/>), so the order tiebreak only ever arbitrates between distinct
    /// templates whose match sets merely overlap for this particular URL (e.g. "/a/{x:int}" vs "/a/{x:min(0)}").
    /// </summary>
    private void ConsiderCandidate(in RouteEntry entry, BrouterLocation to, string[] segments,
        ref MatchResult best, ref int bestOrder, ref bool haveBest)
    {
        if (TryMatch(entry.Route, segments, to.HasTrailingSlash, out var result) is false) return;

        if (haveBest is false)
        {
            best = result;
            bestOrder = entry.Order;
            haveBest = true;
            return;
        }

        int cmp = result.Route.Specificity - best.Route.Specificity;
        if (cmp == 0) cmp = result.Route.Depth - best.Route.Depth;
        if (cmp == 0) cmp = (result.Route.IsIndex ? 1 : 0) - (best.Route.IsIndex ? 1 : 0);
        if (cmp == 0) cmp = bestOrder - entry.Order; // earlier registration wins an otherwise exact tie
        if (cmp > 0)
        {
            best = result;
            bestOrder = entry.Order;
        }
    }

    private bool TryMatch(Broute route, string[] segments, bool hasTrailingSlash, out MatchResult result)
    {
        result = default;

        var routeTemplate = route.RouteTemplate;
        if (routeTemplate is null) return false;

        var literalComparison = Options.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        var templateSegments = routeTemplate.TemplateSegments;
        if (templateSegments.Count == 0)
        {
            if (segments.Length == 0 && hasTrailingSlash is false)
            {
                result = new MatchResult(route, [], []);
                return true;
            }
            return false;
        }

        var lastIdx = templateSegments.Count - 1;
        var last = templateSegments[lastIdx];

        // Under Options.IgnoreTrailingSlash == false a URL ending in '/' is distinct from one
        // that doesn't. Templates are always normalized via TemplateParser to drop trailing
        // slashes, so a non-catch-all route can never legitimately require the slash and must
        // not match a trailing-slash URL. Two exceptions absorb the trailing position:
        //   - Catch-all: it matches zero or more remaining segments, including the implicit
        //     empty one.
        //   - An optional final segment left unfilled by the URL: the trailing slash stands in
        //     for that empty optional value (e.g. "/users/" against "/users/{id?}"). This only
        //     applies while the optional segment is genuinely unfilled - i.e. the URL is shorter
        //     than the template. Once the template is fully satisfied a trailing slash is a real
        //     extra slash ("/users/1/" against "/users/{id?}") and must still be rejected.
        if (hasTrailingSlash && last.IsCatchAll is false
            && (last.IsOptional is false || segments.Length >= templateSegments.Count))
        {
            return false;
        }

        if (templateSegments.Count != segments.Length)
        {
            // Allow shorter URLs if every missing trailing segment is optional or the last one is catch-all.
            if (segments.Length < templateSegments.Count)
            {
                if (last.IsCatchAll && segments.Length >= lastIdx)
                {
                    // OK: catch-all may match zero remaining segments.
                }
                else
                {
                    for (int i = segments.Length; i < templateSegments.Count; i++)
                    {
                        if (templateSegments[i].IsOptional is false &&
                            templateSegments[i].IsCatchAll is false) return false;
                    }
                }
            }
            else
            {
                // URL is longer than template: only a catch-all (**) can absorb extra segments.
                if (last.IsCatchAll is false) return false;
            }
        }

        // Build matched parameter values into local dictionaries; only published onto the
        // winning route after selection.
        var parameters = new Dictionary<string, object?>();
        var constraints = new Dictionary<string, string[]>();

        for (int i = 0; i < templateSegments.Count; i++)
        {
            var templateSegment = templateSegments[i];

            // Catch-all: collect every remaining URL segment.
            if (templateSegment.IsCatchAll)
            {
                if (templateSegment.IsParameter)
                {
                    var remaining = i < segments.Length
                        ? string.Join('/', segments[i..])
                        : string.Empty;

                    parameters[templateSegment.Value] = remaining;
                    constraints[templateSegment.Value] = [];
                }
                result = new MatchResult(route, parameters, constraints);
                return true;
            }

            // Out of URL segments: only valid if optional.
            if (i >= segments.Length)
            {
                if (templateSegment.IsOptional) continue;
                return false;
            }

            var segment = segments[i];

            if (templateSegment.TryMatch(segment, literalComparison, out var matchedValue) is false) return false;

            if (templateSegment.IsParameter)
            {
                parameters[templateSegment.Value] = matchedValue;
                constraints[templateSegment.Value] =
                    templateSegment.Constraints.Select(rc => rc.Name).ToArray();
            }
        }

        result = new MatchResult(route, parameters, constraints);
        return true;
    }


    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Drop any staged route-lifecycle arrivals: with the router torn down there is no commit
        // render left to anchor them (each staged FireArrival also self-guards on route state).
        _pendingLifecycleFlush.Clear();

        _navManager.LocationChanged -= NavManagerLocationChanged;
#if NET10_0_OR_GREATER
        _navManager.OnNotFound -= NavManagerOnNotFound;
#endif
        // Unhook the preventive changing handler so a disposed Brouter can't keep vetoing navigations.
        _locationChangingRegistration?.Dispose();
        _locationChangingRegistration = null;
        // Unsubscribe the prerender persistence callback so a disposed Brouter isn't asked to persist.
        if (_persistSubscribed)
        {
            _persistSubscription.Dispose();
            _persistSubscribed = false;
        }
        // Detach the active CTS and cancel it, but DON'T dispose here. A still-running
        // ProcessNavigationAsync may be observing this CTS via its `token` parameter or
        // about to throw OperationCanceledException through it; disposing now would race
        // those continuations with ObjectDisposedException. The pipeline's own `finally`
        // checks "am I still the published CTS?" and disposes itself when it sees we've
        // detached. CancellationTokenSource.Dispose() is idempotent, so even if both
        // paths reach disposal, the second call is a no-op.
        var cts = Interlocked.Exchange(ref _navCts, null);
        cts?.Cancel();
        // Never leave a NavigateAsync caller hanging on a disposed router.
        var pendingOutcome = _pendingOutcome;
        _pendingOutcome = null;
        _pendingOutcomeUri = null;
        pendingOutcome?.TrySetResult(BrouterNavigationOutcome.Superseded());
        _brouterService.Detach(this);
    }

    /// <summary>
    /// Async dispose. Currently sync-only work; the override exists so callers using
    /// <c>await using</c> get a deterministic teardown signal and the type can grow
    /// async cleanup (e.g. JS module teardown) in the future without changing its
    /// public contract.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private bool _disposed;
}
