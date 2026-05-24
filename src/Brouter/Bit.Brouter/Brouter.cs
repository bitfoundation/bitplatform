using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Brouter;

/// <summary>
/// The root component of Bit.Brouter. Hosts a tree of <see cref="Route"/> children and renders
/// the matching one for the current URL.
/// </summary>
public partial class Brouter : ComponentBase, IDisposable
{
    private static readonly char[] _Separator = ['/'];


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
    [Parameter] public Func<Route, ValueTask>? OnMatch { get; set; }

    /// <summary>Async hook fired when no route matches the current URL.</summary>
    [Parameter] public Func<BrouterLocation, ValueTask>? OnNotFound { get; set; }


    [Inject] private NavigationManager _navManager { get; set; } = default!;
    [Inject] private INavigationInterception _navInterception { get; set; } = default!;
    [Inject] private BrouterService _brouterService { get; set; } = default!;


    internal BrouterLocation CurrentLocation { get; private set; } = BrouterLocation.Empty;
    internal BrouterOptions Options => _brouterService.Options;

    private readonly List<Route> _routes = [];
    internal void RegisterRoute(Route route)
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
    }
    internal void UnregisterRoute(Route route) => _routes.Remove(route);

    internal Route? FindRouteByName(string name) =>
        _routes.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));

    private CancellationTokenSource? _navCts;
    private bool _noRouteMatched;
    private long _navVersion;


    protected override void OnInitialized()
    {
        base.OnInitialized();

        _brouterService.Attach(this, _navManager);

        _navManager.LocationChanged += NavManagerLocationChanged;

        UpdateLocation();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        await _navInterception.EnableNavigationInterceptionAsync();

        await ProcessNavigationAsync(BrouterLocation.Empty);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
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
                (string.IsNullOrEmpty(NotFound) ||
                 string.Equals(_navManager.ToAbsoluteUri(NotFound).ToString(), _navManager.Uri, StringComparison.Ordinal)))
            {
                b.AddContent(1, NotFoundContent(CurrentLocation));
            }
        }));
        builder.CloseComponent();
    }


    private async void NavManagerLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        try
        {
            var from = CurrentLocation;
            UpdateLocation();
            // No ConfigureAwait(false): keep the navigation pipeline on the renderer's
            // dispatcher so subsequent StateHasChanged() / UI mutations are valid.
            await ProcessNavigationAsync(from);
        }
        catch
        {
            // Errors are reported via IBrouter.OnError; never let an exception escape async void.
        }
    }

    private void UpdateLocation()
    {
        var raw = _navManager.ToBaseRelativePath(_navManager.Uri);

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
        if (Options.IgnoreTrailingSlash && path.Length > 1 && path[^1] == '/')
        {
            path = path[..^1];
        }

        var rawSegments = path.Trim('/').Split(_Separator, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < rawSegments.Length; i++)
        {
            // Decode defensively: malformed percent-encoding (e.g. "%ZZ" or a stray "%") would
            // otherwise throw UriFormatException and bubble out of the async-void LocationChanged
            // handler, silently breaking routing without surfacing NotFound / OnError. Falling
            // back to the raw segment lets ProcessNavigationAsync run normally — the bad URL
            // typically won't match any route, which routes the request through NotFound/OnError
            // as it should.
            try
            {
                rawSegments[i] = Uri.UnescapeDataString(rawSegments[i]);
            }
            catch (UriFormatException) { /* keep the raw, still-escaped segment */ }
        }

        CurrentLocation = new BrouterLocation(_navManager.Uri, path, rawSegments, query, hash);
    }

    private async ValueTask ProcessNavigationAsync(BrouterLocation from)
    {
        // Supersede any in-flight navigation work.
        var version = Interlocked.Increment(ref _navVersion);
        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _navCts, newCts);
        if (oldCts is not null)
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }
        var token = newCts.Token;

        var ctx = new NavigationContext(from, CurrentLocation, token);
        var service = _brouterService;

        try
        {
            // No ConfigureAwait(false) anywhere in this pipeline: subsequent calls
            // (StateHasChanged, NavigationManager.NavigateTo, route/component state mutations,
            // Outlet rendering) require the Blazor renderer's synchronization context.
            await service.InvokeOnNavigating(ctx);
            if (HandleSideEffects(ctx, from)) return;
            if (token.IsCancellationRequested || version != _navVersion) return;

            // Match routes.
            foreach (var r in _routes) r.Matched = false;
            var candidates = _routes.Where(r => Match(r, CurrentLocation.SegmentsArray)).ToList();

            if (candidates.Count == 0)
            {
                _noRouteMatched = true;
                if (OnNotFound is not null) await OnNotFound(CurrentLocation);

                // The OnNotFound handler may have awaited; if a newer navigation has started or
                // this one was cancelled in the meantime, abandon the fallback path so we don't
                // redirect/render on behalf of a superseded navigation.
                if (token.IsCancellationRequested || version != _navVersion) return;

                if (string.IsNullOrEmpty(NotFound) is false)
                {
                    // Avoid a self-redirect loop when the current URL is already the NotFound target
                    // (and still doesn't match any route). Render the fallback UI instead.
                    var targetUri = _navManager.ToAbsoluteUri(NotFound).ToString();
                    if (string.Equals(_navManager.Uri, targetUri, StringComparison.Ordinal) is false)
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
            var winner = candidates
                .Select((r, i) => (Route: r, Specificity: r.Specificity, Depth: r.Depth, IsIndex: r.IsIndex, Order: i))
                .OrderByDescending(t => t.Specificity)
                .ThenByDescending(t => t.Depth)
                .ThenByDescending(t => t.IsIndex)
                .ThenBy(t => t.Order)
                .First()
                .Route;

            // Pure redirect (no guard) takes precedence.
            if (winner.Guard is null && winner.RedirectTo is not null)
            {
                _navManager.NavigateTo(winner.RedirectTo);
                return;
            }

            ctx.Route = winner;
            ctx.Parameters = new RouteParameters(winner.Parameters);

            // Guards.
            var guardsOk = await winner.InvokeGuardsAsync(ctx);
            if (HandleSideEffects(ctx, from)) return;
            if (token.IsCancellationRequested || version != _navVersion) return;
            if (guardsOk is false) return;

            // Loaders. Walk root -> leaf so parent layouts get their data populated before
            // children run, mirroring guard ordering (see Route.InvokeGuardsAsync). Reset
            // LoadedData on every route in the matched chain first so data from a previous
            // navigation can't leak into parent layouts whose current loader is null.
            // Capture each loader's result into a local before committing to shared state,
            // so a superseded navigation can't leave stale LoadedData on the route.
            var matchedChain = new List<Route>();
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

            await service.InvokeOnNavigated(ctx);
            await service.ApplyScrollAsync();

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
    }

    private bool HandleSideEffects(NavigationContext ctx, BrouterLocation from)
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

    private bool Match(Route route, string[] segments)
    {
        route.Parameters = new Dictionary<string, object?>();
        route.ConstraintsByParameter = new Dictionary<string, string[]>();

        var routeTemplate = route.RouteTemplate;
        if (routeTemplate is null) return false;

        var literalComparison = Options.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        var templateSegments = routeTemplate.TemplateSegments;
        if (templateSegments.Count == 0) return segments.Length == 0;

        var lastIdx = templateSegments.Count - 1;
        var last = templateSegments[lastIdx];

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

                    route.Parameters[templateSegment.Value] = remaining;
                    route.ConstraintsByParameter[templateSegment.Value] = [];
                }
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
                route.Parameters[templateSegment.Value] = matchedValue;
                route.ConstraintsByParameter[templateSegment.Value] =
                    templateSegment.Constraints.Select(rc => rc.Name).ToArray();
            }
        }

        return true;
    }


    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _navManager.LocationChanged -= NavManagerLocationChanged;
        var cts = Interlocked.Exchange(ref _navCts, null);
        if (cts is not null)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _brouterService.Detach(this);
    }

    private bool _disposed;
}
