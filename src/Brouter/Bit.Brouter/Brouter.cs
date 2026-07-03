using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Rendering;
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
    /// URL to navigate to when no route matches. If null, no redirect happens and
    /// <see cref="NotFoundContent"/> (if any) is rendered in place.
    /// </summary>
    [Parameter] public string? NotFound { get; set; }

    /// <summary>Inline content to render when no route matches and <see cref="NotFound"/> is null.</summary>
    [Parameter] public RenderFragment<BrouterLocation>? NotFoundContent { get; set; }

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


    [Inject] private NavigationManager _navManager { get; set; } = default!;
    [Inject] private INavigationInterception _navInterception { get; set; } = default!;
    [Inject] private BrouterService _brouterService { get; set; } = default!;
    [Inject] private IServiceProvider _services { get; set; } = default!;


    internal BrouterLocation CurrentLocation { get; private set; } = BrouterLocation.Empty;
    internal BrouterOptions Options => _brouterService.Options;


    // Routes discovered by scanning AppAssembly / AdditionalAssemblies for [Route]/@page components.
    // Rendered as synthetic <Broute> children in BuildRenderTree so they reuse the whole matching /
    // guard / loader / render pipeline. Recomputed only when the assembly set actually changes.
    private IReadOnlyList<BrouteScanner.DiscoveredRoute> _discoveredRoutes = [];
    private Assembly? _lastAppAssembly;
    private Assembly[]? _lastAdditionalAssemblies;
    private bool _discoveryComputed;

    // Prerender -> interactive loader-state bridge (see BroutePrerenderState). Only active when
    // Options.PersistLoaderState is set and a PersistentComponentState is available in the scope.
    private PersistentComponentState? _persistentState;
    private PersistingComponentStateSubscription _persistSubscription;
    private bool _persistSubscribed;
    // Loader results staged during the current navigation's commit, keyed by their persistence key.
    // Serialized by the RegisterOnPersisting callback at the end of prerender.
    private readonly Dictionary<string, object?> _loaderStateToPersist = new(StringComparer.Ordinal);

    private readonly List<Broute> _routes = [];
    // Snapshot of _routes refreshed lazily after Register/Unregister. The matching loop
    // iterates this snapshot so we don't allocate a fresh array on every navigation.
    // Volatile read/write keeps the snapshot publication ordered relative to the dirty
    // flag (we only ever flip _routesDirty -> true under the same dispatcher that calls
    // Register/Unregister, but a navigation pipeline awaiting back can re-enter on the
    // dispatcher and observe a stale snapshot if not for the volatile read/write pair).
    private Broute[] _routesSnapshot = [];
    private bool _routesDirty = true;
    internal void RegisterRoute(Broute route)
    {
        // Enforce the documented uniqueness contract for Route.Name. Comparison matches
        // FindRouteByName (case-insensitive), so name lookups stay unambiguous.
        if (string.IsNullOrEmpty(route.Name) is false)
        {
            for (int i = 0; i < _routes.Count; i++)
            {
                var existing = _routes[i];
                if (ReferenceEquals(existing, route)) continue;
                if (string.Equals(existing.Name, route.Name, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"A route with the name '{route.Name}' is already registered. Route names must be unique (case-insensitive).");
                }
            }
        }

        _routes.Add(route);
        _routesDirty = true;
    }
    internal void UnregisterRoute(Broute route)
    {
        if (_routes.Remove(route)) _routesDirty = true;
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

    internal Broute? FindRouteByName(string name) =>
        _routes.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));

    private CancellationTokenSource? _navCts;
    private bool _noRouteMatched;
    private long _navVersion;

    // Registration returned by NavigationManager.RegisterLocationChangingHandler. Disposed on
    // teardown to unhook the preventive guard/redirect/cancel decision (see OnLocationChanging).
    private IDisposable? _locationChangingRegistration;

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


    protected override void OnInitialized()
    {
        base.OnInitialized();

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
                _persistSubscription = _persistentState.RegisterOnPersisting(PersistLoaderStateAsync);
                _persistSubscribed = true;
            }
        }

        _navManager.LocationChanged += NavManagerLocationChanged;

        // Establish the initial location synchronously so any code that reads
        // BrouterService.Location before the first navigation pipeline runs sees
        // the real URL (not BrouterLocation.Empty).
        CurrentLocation = ComputeLocation();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Refresh discovered routes whenever the assembly set changes (including the first parameter set,
        // and when AdditionalAssemblies grows because a lazy-loaded assembly was added). Kept out of the
        // navigation pipeline so scanning cost is paid on parameter changes, not per navigation.
        RefreshDiscoveredRoutesIfNeeded();
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

        _discoveredRoutes = (AppAssembly is null && (additional is null || additional.Length == 0))
            ? []
            : BrouteScanner.Discover(AppAssembly, additional);
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
            state.PersistAsJson(kv.Key, BroutePrerenderState.Capture(kv.Value));
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

        return BroutePrerenderState.TryRestore(persisted, out value);
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
        // not fire for the initial load anyway), so the full pipeline runs the guards here.
        await ProcessNavigationAsync(BrouterLocation.Empty, CurrentLocation, decisionAlreadyMade: false);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

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
        builder.AddAttribute(3, "ChildContent", (RenderFragment)(b =>
        {
            b.AddContent(0, ChildContent);

            // Emit a synthetic <Broute> for each attribute-discovered route. They register themselves
            // exactly like hand-declared children (via Broute.OnInitialized) and so participate in the
            // same specificity-based matching, guards, loaders and rendering. Wrapped in a region so
            // their sequence numbers live in an isolated space, and keyed by the (template, type) pair
            // so Blazor keeps instances stable if the discovered set is reordered or grows at runtime.
            if (_discoveredRoutes.Count > 0)
            {
                b.OpenRegion(1);
                var seq = 0;
                foreach (var discovered in _discoveredRoutes)
                {
                    b.OpenComponent<Broute>(seq++);
                    b.SetKey(discovered);
                    b.AddAttribute(seq++, nameof(Broute.Path), discovered.Template);
                    b.AddAttribute(seq++, nameof(Broute.Component), discovered.ComponentType);
                    b.AddAttribute(seq++, nameof(Broute.BindComponentParametersByName), true);
                    b.CloseComponent();
                }
                b.CloseRegion();
            }

            // Render the inline fallback when no route matched and either NotFound is unset, or
            // NotFound resolves to the current URL (no redirect happened, so we'd otherwise show nothing).
            if (_noRouteMatched && NotFoundContent is not null &&
                (string.IsNullOrEmpty(NotFound) || IsSamePath(CurrentLocation.Path, NotFound)))
            {
                b.AddContent(2, NotFoundContent(CurrentLocation));
            }
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
        var decisionAlreadyMade =
            approved is not null && string.Equals(approved, to.FullUri, StringComparison.Ordinal);

        try
        {
            await InvokeAsync(() => ProcessNavigationAsync(from, to, decisionAlreadyMade).AsTask());
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

        BrouterLocation from = CurrentLocation;
        BrouterLocation to;
        try
        {
            // The URL has NOT committed yet, so resolve the pending target rather than
            // NavigationManager.Uri (which still holds the current location).
            to = ComputeLocation(_navManager.ToAbsoluteUri(context.TargetLocation).ToString());
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
        var ctx = new BrouterNavigationContext(from, to, token);
        var service = _brouterService;

        try
        {
            await service.InvokeOnNavigating(ctx);
            if (token.IsCancellationRequested) return;
            if (ApplyPreventiveDecision(context, ctx)) return;

            var winnerMatch = SelectWinner(to);

            if (winnerMatch is null)
            {
                // No route matched. Fire OnNotFound, then either redirect to the NotFound target
                // (preventively, so the unmatched URL never appears in the address bar) or allow
                // the commit phase to render NotFoundContent in place.
                if (OnNotFound is not null) await OnNotFound(to);
                if (token.IsCancellationRequested) return;

                if (string.IsNullOrEmpty(NotFound) is false && IsSamePath(to.Path, NotFound) is false)
                {
                    context.PreventNavigation();
                    _navManager.NavigateTo(NotFound);
                    return;
                }

                _approvedTargetUri = to.FullUri;
                return;
            }

            var winner = winnerMatch.Value.Route;
            ctx.Route = winner;
            ctx.Parameters = new BrouteParameters(winnerMatch.Value.Parameters);

            var guardsOk = await winner.InvokeGuardsAsync(ctx);
            if (token.IsCancellationRequested) return;
            if (ApplyPreventiveDecision(context, ctx)) return;
            if (guardsOk is false) return; // superseded (token cancelled inside the guard chain)

            if (winner.RedirectTo is not null)
            {
                context.PreventNavigation();
                _navManager.NavigateTo(winner.RedirectTo);
                return;
            }

            // Approved: let the URL commit. The LocationChanged commit phase re-selects this same
            // winner (matching is pure) and runs its loaders + render, skipping the hooks above.
            _approvedTargetUri = to.FullUri;
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
            _navManager.NavigateTo(ctx.RedirectUrl);
            return true;
        }

        if (ctx.IsCancelled)
        {
            context.PreventNavigation();
            return true;
        }

        return false;
    }

    private async ValueTask SafeInvokeOnError(BrouterLocation from, BrouterLocation to, Exception ex)
    {
        try
        {
            await _brouterService.InvokeOnError(
                new BrouterNavigationContext(from, to, CancellationToken.None), ex);
        }
        catch { /* OnError must never crash the navigation handler */ }
    }

    /// <summary>
    /// Pure: builds a <see cref="BrouterLocation"/> from the current <c>NavigationManager.Uri</c>.
    /// Does not mutate <see cref="CurrentLocation"/>. Never throws: an off-base URL or other
    /// malformed input is normalized to an empty-path location so the navigation pipeline can
    /// run and surface the issue through NotFound / OnError instead of crashing the handler.
    /// </summary>
    private BrouterLocation ComputeLocation() => ComputeLocation(_navManager.Uri);

    /// <summary>
    /// Pure: builds a <see cref="BrouterLocation"/> from an arbitrary absolute URI. Used by the
    /// LocationChanging handler, where the navigation has not committed yet so we must resolve the
    /// pending target URL (from <c>LocationChangingContext.TargetLocation</c>) rather than the still
    /// current <c>NavigationManager.Uri</c>. Shares all normalization with the no-arg overload so a
    /// location computed during the "changing" phase is identical to the one recomputed after commit.
    /// </summary>
    private BrouterLocation ComputeLocation(string uri)
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
            return new BrouterLocation(uri, "/", [], "", "");
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

        return new BrouterLocation(uri, path, rawSegments, query, hash, hasTrailingSlash);
    }

    // Cache the most recently-computed normalization. NotFound is typically a constant per
    // Brouter instance, and BuildRenderTree calls IsSamePath on every render (NotFoundContent
    // fallback check). One-slot cache is enough; on a NotFound parameter change the cached
    // entry is replaced.
    private string? _isSamePathCacheTarget;
    private string? _isSamePathCacheNormalized;

    /// <summary>
    /// Compares an already-normalized <paramref name="currentPath"/> (as produced by
    /// <see cref="ComputeLocation()"/>) against an arbitrary target URL/path. Returns true
    /// when their normalized path components are equal.
    /// </summary>
    /// <remarks>
    /// Used by the NotFound logic to detect the "we're already at the NotFound target"
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
    private async ValueTask ProcessNavigationAsync(BrouterLocation from, BrouterLocation to, bool decisionAlreadyMade)
    {
        // Now that we own the renderer's dispatcher (via InvokeAsync from the LocationChanged
        // handler, or directly from OnAfterRenderAsync for the initial render), publish the
        // target location atomically with the start of this pipeline. The whole pipeline below
        // reads `to` rather than CurrentLocation, so a later navigation publishing a newer
        // CurrentLocation cannot make our `ctx.To` desync from what we're matching against.
        CurrentLocation = to;

        // Supersede any in-flight navigation work.
        var version = Interlocked.Increment(ref _navVersion);
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

        var ctx = new BrouterNavigationContext(from, to, token);
        var service = _brouterService;

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
            // the reset is invisible to the user.
            foreach (var r in routesSnapshot) r.Matched = false;

            // Match is pure (SelectWinner never mutates a route), so the same selection runs in both
            // the changing and commit phases and yields the same winner for a stable route set.
            var winnerMatch = SelectWinner(to);

            if (winnerMatch is null)
            {
                _noRouteMatched = true;

                // OnNotFound + the preventive NotFound redirect already ran in the changing phase
                // when decisionAlreadyMade is true; only run them here for the full-pipeline path.
                if (decisionAlreadyMade is false)
                {
                    if (OnNotFound is not null) await OnNotFound(to);

                    // The OnNotFound handler may have awaited; if a newer navigation has started or
                    // this one was cancelled in the meantime, abandon the fallback path so we don't
                    // redirect/render on behalf of a superseded navigation.
                    if (token.IsCancellationRequested || version != _navVersion) return;

                    if (string.IsNullOrEmpty(NotFound) is false)
                    {
                        // Avoid a self-redirect loop when the current URL is already the NotFound target
                        // (and still doesn't match any route). Render the fallback UI instead.
                        // Compare normalized base-relative paths rather than raw absolute URIs:
                        // "http://host/x" vs "http://host/x/" or vs "http://host/x?foo=1" would
                        // otherwise miss the equality check and trigger an infinite redirect loop
                        // (the NotFound URL keeps not matching, we keep navigating to it).
                        if (IsSamePath(to.Path, NotFound) is false)
                        {
                            _navManager.NavigateTo(NotFound);
                            return;
                        }
                    }
                }
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
            ctx.Parameters = new BrouteParameters(winner.Parameters);

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
                    _navManager.NavigateTo(winner.RedirectTo);
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

            for (int chainIndex = 0; chainIndex < matchedChain.Count; chainIndex++)
            {
                var node = matchedChain[chainIndex];
                if (node.Loader is null) continue;

                // Prerender bridge: if this loader already ran server-side and its result was persisted,
                // restore it and skip the fetch. The key is derived from the URL + chain position, which
                // are identical across the prerender and interactive passes, so restoration lines up.
                if (TryRestoreLoaderState(to, chainIndex, out var restored))
                {
                    node.LoadedData = restored;
                    continue;
                }

                object? loaded;
                try
                {
                    loaded = await node.Loader(ctx);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return;
                }
                catch (NavigationException)
                {
                    // During static server rendering / prerender, NavigationManager.NavigateTo
                    // throws NavigationException as the framework's redirect signal (a loader may
                    // redirect, e.g. an auth gate). It must unwind out of OnInitializedAsync so the
                    // endpoint can issue the HTTP redirect; swallowing it into OnError would drop
                    // the redirect entirely. Interactive NavigateTo never throws, so this is inert
                    // outside SSR.
                    throw;
                }
                catch (Exception ex)
                {
                    await service.InvokeOnError(ctx, ex);
                    return;
                }

                if (HandleSideEffects(ctx, from)) return;
                if (token.IsCancellationRequested || version != _navVersion) return;

                node.LoadedData = loaded;

                // Stage the result for persistence. It is written to PersistentComponentState only if the
                // RegisterOnPersisting callback fires (i.e. during prerender); interactive passes stage it
                // too but simply never get asked to persist.
                if (_persistentState is not null)
                {
                    _loaderStateToPersist[BroutePrerenderState.MakeKey(to.Path, to.Query, chainIndex)] = loaded;
                }
            }

            winner.SetMatched();

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

            StateHasChanged();
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
        }
        finally
        {
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

    private bool HandleSideEffects(BrouterNavigationContext ctx, BrouterLocation from)
    {
        if (ctx.RedirectUrl is not null)
        {
            _navManager.NavigateTo(ctx.RedirectUrl);
            return true;
        }

        if (ctx.IsCancelled)
        {
            // Restore the address bar. If From is empty (initial render), we leave the URL alone.
            if (string.IsNullOrEmpty(from.FullUri) is false &&
                string.Equals(from.FullUri, ctx.To.FullUri, StringComparison.Ordinal) is false)
            {
                _navManager.NavigateTo(from.FullUri, replace: true);
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
        var routesSnapshot = GetRoutesSnapshot();

        var candidates = new List<MatchResult>();
        foreach (var r in routesSnapshot)
        {
            if (TryMatch(r, to.SegmentsArray, to.HasTrailingSlash, out var result))
            {
                candidates.Add(result);
            }
        }

        if (candidates.Count == 0) return null;

        // Pick the most specific match. Ties broken by deeper nesting (so an index child
        // wins over its parent when their full templates are identical), then by index-route
        // preference, then by declaration order (the loop keeps the earliest on an exact tie).
        MatchResult winnerMatch = candidates[0];
        for (int i = 1; i < candidates.Count; i++)
        {
            var c = candidates[i];
            var w = winnerMatch;
            int cmp = c.Route.Specificity - w.Route.Specificity;
            if (cmp == 0) cmp = c.Route.Depth - w.Route.Depth;
            if (cmp == 0) cmp = (c.Route.IsIndex ? 1 : 0) - (w.Route.IsIndex ? 1 : 0);
            if (cmp > 0)
            {
                winnerMatch = c;
            }
        }

        return winnerMatch;
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

        _navManager.LocationChanged -= NavManagerLocationChanged;
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
