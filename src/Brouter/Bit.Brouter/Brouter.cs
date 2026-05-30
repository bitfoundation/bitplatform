using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Bit.Brouter;

/// <summary>
/// The root component of Bit.Brouter. Hosts a tree of <see cref="BrouterRoute"/> children and renders
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
    [Parameter] public Func<BrouterRoute, ValueTask>? OnMatch { get; set; }

    /// <summary>Async hook fired when no route matches the current URL.</summary>
    [Parameter] public Func<BrouterLocation, ValueTask>? OnNotFound { get; set; }


    [Inject] private NavigationManager _navManager { get; set; } = default!;
    [Inject] private INavigationInterception _navInterception { get; set; } = default!;
    [Inject] private BrouterService _brouterService { get; set; } = default!;


    internal BrouterLocation CurrentLocation { get; private set; } = BrouterLocation.Empty;
    internal BrouterOptions Options => _brouterService.Options;

    private readonly List<BrouterRoute> _routes = [];
    // Snapshot of _routes refreshed lazily after Register/Unregister. The matching loop
    // iterates this snapshot so we don't allocate a fresh array on every navigation.
    // Volatile read/write keeps the snapshot publication ordered relative to the dirty
    // flag (we only ever flip _routesDirty -> true under the same dispatcher that calls
    // Register/Unregister, but a navigation pipeline awaiting back can re-enter on the
    // dispatcher and observe a stale snapshot if not for the volatile read/write pair).
    private BrouterRoute[] _routesSnapshot = [];
    private bool _routesDirty = true;
    internal void RegisterRoute(BrouterRoute route)
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
    internal void UnregisterRoute(BrouterRoute route)
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
    private BrouterRoute[] GetRoutesSnapshot()
    {
        if (_routesDirty is false) return _routesSnapshot;
        var arr = _routes.ToArray();
        _routesSnapshot = arr;
        _routesDirty = false;
        return arr;
    }

    internal BrouterRoute? FindRouteByName(string name) =>
        _routes.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));

    private CancellationTokenSource? _navCts;
    private bool _noRouteMatched;
    private long _navVersion;


    protected override void OnInitialized()
    {
        base.OnInitialized();

        _brouterService.Attach(this, _navManager);

        _navManager.LocationChanged += NavManagerLocationChanged;

        // Establish the initial location synchronously so any code that reads
        // BrouterService.Location before the first navigation pipeline runs sees
        // the real URL (not BrouterLocation.Empty).
        CurrentLocation = ComputeLocation();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        // Enabling navigation interception is best-effort: under prerender, on a disconnected
        // circuit, or on an interop failure it can throw, but the navigation pipeline itself
        // (and any subsequent reconnects / interactivity handoff) does not depend on it
        // succeeding right now. Mirror the defensive style used in BrouterLink and
        // BrouterService.BackAsync so a transient failure here can't kill the whole first
        // navigation. Once the circuit/runtime is fully ready, Blazor will retry interception
        // attachment naturally on the next user click via NavigationManager fallback paths.
        try
        {
            await _navInterception.EnableNavigationInterceptionAsync();
        }
        catch (JSDisconnectedException) { /* circuit disconnected before/during interop */ }
        catch (JSException) { /* JS interop failure; non-fatal */ }
        catch (InvalidOperationException) { /* interop unavailable during prerender */ }
        catch (TaskCanceledException) { /* component disposed mid-call */ }

        // Initial render: the From is Empty (we just mounted), the To is the URL we're at now.
        await ProcessNavigationAsync(BrouterLocation.Empty, CurrentLocation);
    }

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
            // Render the inline fallback when no route matched and either NotFound is unset, or
            // NotFound resolves to the current URL (no redirect happened, so we'd otherwise show nothing).
            if (_noRouteMatched && NotFoundContent is not null &&
                (string.IsNullOrEmpty(NotFound) || IsSamePath(CurrentLocation.Path, NotFound)))
            {
                b.AddContent(1, NotFoundContent(CurrentLocation));
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
            // Defense in depth: ComputeLocation is intended to be no-throw (it normalises
            // off-base URLs to an empty-path location), but if a future change ever lets an
            // exception escape, we still surface it through OnError instead of letting it
            // out of the async-void event handler.
            await SafeInvokeOnError(from, CurrentLocation, ex);
            return;
        }

        try
        {
            await InvokeAsync(() => ProcessNavigationAsync(from, to).AsTask());
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
    /// malformed input is normalised to an empty-path location so the navigation pipeline can
    /// run and surface the issue through NotFound / OnError instead of crashing the handler.
    /// </summary>
    private BrouterLocation ComputeLocation()
    {
        var uri = _navManager.Uri;

        // ToBaseRelativePath throws ArgumentException if the current Uri is not within
        // NavigationManager.BaseUri (base href misconfigured, programmatic NavigateTo to an
        // off-base absolute URL, etc.). Don't propagate: that would kill an async-void
        // handler permanently. Synthesise an empty-path location so the pipeline runs and
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

    // Cache the most recently-computed normalisation. NotFound is typically a constant per
    // Brouter instance, and BuildRenderTree calls IsSamePath on every render (NotFoundContent
    // fallback check). One-slot cache is enough; on a NotFound parameter change the cached
    // entry is replaced.
    private string? _isSamePathCacheTarget;
    private string? _isSamePathCacheNormalised;

    /// <summary>
    /// Compares an already-normalised <paramref name="currentPath"/> (as produced by
    /// <see cref="ComputeLocation"/>) against an arbitrary target URL/path. Returns true
    /// when their normalised path components are equal.
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
            // _isSamePathCacheNormalised is null only when the previous call returned false
            // for an off-base/malformed target; replicate that result.
            if (_isSamePathCacheNormalised is null) return false;
            targetPath = _isSamePathCacheNormalised;
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
                _isSamePathCacheNormalised = null;
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
            _isSamePathCacheNormalised = targetPath;
        }

        var comparison = Options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return string.Equals(currentPath, targetPath, comparison);
    }

    private async ValueTask ProcessNavigationAsync(BrouterLocation from, BrouterLocation to)
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
            // No ConfigureAwait(false) anywhere in this pipeline: subsequent calls
            // (StateHasChanged, NavigationManager.NavigateTo, route/component state mutations,
            // Outlet rendering) require the Blazor renderer's synchronization context.
            await service.InvokeOnNavigating(ctx);
            if (HandleSideEffects(ctx, from)) return;
            if (token.IsCancellationRequested || version != _navVersion) return;

            // Snapshot the route list before any awaits / chain walks below: routes can register
            // or unregister during awaits (component lifecycle on the renderer dispatcher), and
            // the chain walks (winner.Parent) read state we mustn't see torn. The snapshot is
            // reused across navigations while the registration set is stable - see GetRoutesSnapshot.
            var routesSnapshot = GetRoutesSnapshot();

            // Match routes. Match is pure: it returns a MatchResult and never mutates the route.
            foreach (var r in routesSnapshot) r.Matched = false;
            var candidates = new List<MatchResult>();
            foreach (var r in routesSnapshot)
            {
                if (TryMatch(r, to.SegmentsArray, to.HasTrailingSlash, out var result))
                {
                    candidates.Add(result);
                }
            }

            if (candidates.Count == 0)
            {
                _noRouteMatched = true;
                if (OnNotFound is not null) await OnNotFound(to);

                // The OnNotFound handler may have awaited; if a newer navigation has started or
                // this one was cancelled in the meantime, abandon the fallback path so we don't
                // redirect/render on behalf of a superseded navigation.
                if (token.IsCancellationRequested || version != _navVersion) return;

                if (string.IsNullOrEmpty(NotFound) is false)
                {
                    // Avoid a self-redirect loop when the current URL is already the NotFound target
                    // (and still doesn't match any route). Render the fallback UI instead.
                    // Compare normalised base-relative paths rather than raw absolute URIs:
                    // "http://host/x" vs "http://host/x/" or vs "http://host/x?foo=1" would
                    // otherwise miss the equality check and trigger an infinite redirect loop
                    // (the NotFound URL keeps not matching, we keep navigating to it).
                    if (IsSamePath(to.Path, NotFound) is false)
                    {
                        _navManager.NavigateTo(NotFound);
                        return;
                    }
                }
                StateHasChanged();
                return;
            }

            _noRouteMatched = false;

            // Pick the most specific match. Ties broken by deeper nesting (so an index child
            // wins over its parent when their full templates are identical), then by index-route
            // preference, then by declaration order.
            MatchResult winnerMatch = candidates[0];
            int winnerIndex = 0;
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
                    winnerIndex = i;
                }
            }
            // Suppress unused-variable warning while documenting that declaration order is the
            // final tiebreaker (lower index wins, which is what the loop above naturally yields).
            _ = winnerIndex;

            var winner = winnerMatch.Route;

            // Commit the winner's matched parameters / constraints. Until this point Match was
            // pure, so candidates that lost have not had their Parameters/Constraints touched
            // (avoiding a race where a still-rendering, previously-matched route gets blanked).
            winner.Parameters = winnerMatch.Parameters;
            winner.ConstraintsByParameter = winnerMatch.ConstraintsByParameter;

            ctx.Route = winner;
            ctx.Parameters = new BrouterRouteParameters(winner.Parameters);

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
            var matchedChain = new List<BrouterRoute>();
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

            foreach (var node in matchedChain)
            {
                if (node.Loader is null) continue;

                object? loaded;
                try
                {
                    loaded = await node.Loader(ctx);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    await service.InvokeOnError(ctx, ex);
                    return;
                }

                if (HandleSideEffects(ctx, from)) return;
                if (token.IsCancellationRequested || version != _navVersion) return;

                node.LoadedData = loaded;
            }

            winner.SetMatched();

            if (OnMatch is not null) await OnMatch(winner);
            // Each await below can yield long enough for a newer navigation to start. If that
            // happens, bail out so we don't fire OnNavigated, scroll, or re-render on behalf
            // of a superseded navigation (and overwrite the new one's UI / scroll position).
            if (token.IsCancellationRequested || version != _navVersion) return;

            await service.InvokeOnNavigated(ctx);
            if (token.IsCancellationRequested || version != _navVersion) return;

            await service.ApplyScrollAsync();
            if (token.IsCancellationRequested || version != _navVersion) return;

            StateHasChanged();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // navigation was superseded; nothing to do
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
        public BrouterRoute Route { get; }
        public Dictionary<string, object?> Parameters { get; }
        public Dictionary<string, string[]> ConstraintsByParameter { get; }

        public MatchResult(BrouterRoute route,
                           Dictionary<string, object?> parameters,
                           Dictionary<string, string[]> constraintsByParameter)
        {
            Route = route;
            Parameters = parameters;
            ConstraintsByParameter = constraintsByParameter;
        }
    }

    private bool TryMatch(BrouterRoute route, string[] segments, bool hasTrailingSlash, out MatchResult result)
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
        // not match a trailing-slash URL. Catch-all is exempt: it absorbs the trailing position
        // (matching zero or more remaining segments, including the implicit empty one).
        if (hasTrailingSlash && last.IsCatchAll is false) return false;

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
