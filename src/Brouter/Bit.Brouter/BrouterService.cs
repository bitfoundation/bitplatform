using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Bit.Brouter;

internal sealed class BrouterService : IBrouter, IAsyncDisposable
{
    private readonly BrouterOptions _options;
    private readonly IJSRuntime _js;
    private readonly ILogger<BrouterService> _logger;
    private Brouter? _activeBrouter;
    private NavigationManager? _navigationManager;

    // Lazily-imported bit-brouter.js module, used for the post-navigation DOM effects
    // (fragment/top scroll, focus) and shared with every BrouterLink that needs JS wiring
    // (Replace links). Imported on first use and reused for the scope's lifetime; disposed
    // in DisposeAsync when DI tears the scoped service down. The pending import Task is
    // cached (rather than the resolved reference) so concurrent first calls - e.g. a page
    // full of Replace links wiring up in the same OnAfterRender pass - coalesce into a
    // single interop round-trip instead of racing N imports.
    private Task<IJSObjectReference>? _moduleTask;

    // The logger is optional (with a null-object fallback) so resolving the service never fails
    // in a container that has no logging registered.
    public BrouterService(IOptions<BrouterOptions> options, IJSRuntime js, ILogger<BrouterService>? logger = null)
    {
        // Resolve once: BrouterService is scoped, BrouterOptions is registered as a singleton
        // via AddOptions, so the resolved value is stable for the lifetime of the scope.
        // We deliberately don't take IOptionsMonitor here because route matching, link
        // activation and link rendering all read these flags many times per navigation;
        // changing options at runtime would require rebuilding the matcher's case rules
        // and re-evaluating every active link, which isn't a supported scenario right now.
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _js = js;
        _logger = logger ?? NullLogger<BrouterService>.Instance;
    }

    internal BrouterOptions Options => _options;

    // The scoped stale-while-revalidate store for loader results (see Broute.StaleTime and link
    // preloading). Lives on the service rather than the Brouter component so it survives router
    // re-mounts within the scope.
    internal BrouterLoaderCache LoaderCache { get; } = new();

    public void ClearLoaderCache() => LoaderCache.Clear();

    public void ClearKeepAlive()
    {
        EnsureMounted();
        _activeBrouter!.ClearKeepAlive();
    }

    internal void Attach(Brouter brouter, NavigationManager navManager)
    {
        // Only one <Brouter/> may be mounted per scope. Two competing instances would race
        // each other through this single _activeBrouter slot: each LocationChanged event
        // would only be processed by whichever instance happened to win the slot at the
        // moment, route registrations would split between the two, and IBrouter.Navigate
        // would target whichever one was attached last. Fail fast with a clear message
        // rather than silently dropping events.
        if (_activeBrouter is not null && ReferenceEquals(_activeBrouter, brouter) is false)
        {
            throw new InvalidOperationException(
                "Another <Brouter/> instance is already attached to this scope. " +
                "Only a single Brouter component may be mounted at a time per DI scope. " +
                "Move the second instance into its own scope, or remove it.");
        }

        _activeBrouter = brouter;
        _navigationManager = navManager;
    }

    internal void Detach(Brouter brouter)
    {
        if (ReferenceEquals(_activeBrouter, brouter))
        {
            _activeBrouter = null;
            _navigationManager = null;
        }
    }

    public BrouterLocation Location => _activeBrouter?.CurrentLocation ?? BrouterLocation.Empty;

    public void Navigate(string url, bool replace = false, bool forceLoad = false, string? historyState = null)
    {
        EnsureMounted();
        // Route-relative URLs ("./edit", "../sibling") resolve against the current location's
        // path via segment math. Anything else (including bare "sibling") flows through to
        // NavigationManager unchanged and keeps its base-relative meaning.
        url = BrouterRelativeUrl.ResolveIfRelative(Location.Path, url);
        NavigateCore(url, replace, forceLoad, historyState);
    }

    public ValueTask<BrouterNavigationOutcome> NavigateAsync(string url, bool replace = false, string? historyState = null)
    {
        EnsureMounted();
        url = BrouterRelativeUrl.ResolveIfRelative(Location.Path, url);

        // Register the awaiter under the exact absolute URI the pipeline will observe as its
        // target, BEFORE triggering the navigation (on some hosts NavigateTo runs the changing
        // handler synchronously, so registering afterwards would miss the whole navigation).
        var absoluteUri = _navigationManager!.ToAbsoluteUri(url).ToString();
        var outcome = _activeBrouter!.RegisterNavigationOutcome(absoluteUri);

        NavigateCore(url, replace, forceLoad: false, historyState);
        return new ValueTask<BrouterNavigationOutcome>(outcome);
    }

    // Shared trigger for Navigate/NavigateAsync: stamps the navigation type and dispatches to the
    // right NavigateTo overload. `url` is already relative-resolved by the caller.
    private void NavigateCore(string url, bool replace, bool forceLoad, string? historyState)
    {
        // Tell the pipeline this navigation is a push/replace before triggering it, so guards, loaders
        // and hooks report the right BrouterNavigationType. Skipped for forceLoad: it's a full-page
        // reload, so no SPA pipeline runs and there is nothing to classify. Back/Forward don't come
        // through here (they use history.go), so they correctly fall through to Pop detection.
        if (forceLoad is false)
            _activeBrouter!.SetPendingNavigationType(replace ? BrouterNavigationType.Replace : BrouterNavigationType.Push);

        if (historyState is null)
        {
            _navigationManager!.NavigateTo(url, forceLoad: forceLoad, replace: replace);
        }
        else
        {
            // The options overload is the only NavigateTo that carries HistoryEntryState. Only taken
            // when state was actually supplied, so the common stateless path keeps its exact
            // pre-existing NavigationManager behavior.
            _navigationManager!.NavigateTo(url, new NavigationOptions
            {
                ForceLoad = forceLoad,
                ReplaceHistoryEntry = replace,
                HistoryEntryState = historyState,
            });
        }
    }

    public void Back()
    {
        EnsureMounted();
        _ = BackAsync(1);
    }

    public ValueTask BackAsync(int delta = 1)
    {
        if (delta < 1)
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Back delta must be >= 1. To go forward, use Forward / ForwardAsync.");
        EnsureMounted();
        // Stamp the traversal type up front: interactive Blazor reports history traversals as
        // intercepted navigations, which the pipeline's fallback heuristic reads as a push. The
        // pending marker wins, so guards/hooks see the correct Pop. (If the traversal is a no-op -
        // history boundary - the marker is consumed by the next navigation instead; rare and benign.)
        _activeBrouter!.SetPendingNavigationType(BrouterNavigationType.Pop);
        return GoAsync(-delta);
    }

    public void Forward()
    {
        EnsureMounted();
        _ = ForwardAsync(1);
    }

    public ValueTask ForwardAsync(int delta = 1)
    {
        if (delta < 1)
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Forward delta must be >= 1. To go back, use Back / BackAsync.");
        EnsureMounted();
        // See BackAsync: mark the traversal so the pipeline reports Pop rather than a push.
        _activeBrouter!.SetPendingNavigationType(BrouterNavigationType.Pop);
        return GoAsync(delta);
    }

    public ValueTask RevalidateAsync()
    {
        EnsureMounted();
        return new ValueTask(_activeBrouter!.RevalidateAsync());
    }

    public ValueTask PreloadAsync(string url)
    {
        EnsureMounted();
        url = BrouterRelativeUrl.ResolveIfRelative(Location.Path, url);
        return new ValueTask(_activeBrouter!.PreloadAsync(url));
    }

    public void NavigateWithQuery(Action<BrouterQueryBuilder> mutate, bool replace = true)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        EnsureMounted();

        var location = Location;
        var builder = new BrouterQueryBuilder(location);
        mutate(builder);

        Navigate(location.Path + builder.ToQueryString() + location.Hash, replace: replace);
    }

    /// <summary>
    /// Starts a View Transition capturing the current (outgoing) DOM, returning true only when a
    /// transition is actually running and will need <see cref="CompleteViewTransitionAsync"/> after
    /// the new route's render lands. False on unsupported browsers, during prerender, disconnected
    /// circuits, or when <see cref="BrouterOptions.ViewTransitions"/> is off - the pipeline then
    /// skips the completion round-trip entirely.
    /// </summary>
    internal async ValueTask<bool> BeginViewTransitionAsync(BrouterNavigationType navigationType)
    {
        if (_options.ViewTransitions is false) return false;

        // The direction token drives the built-in direction-aware default animations (push glides
        // forward, pop mirrors it, replace fades in place) and is exposed on the root element as
        // data-brouter-nav for custom CSS.
        var kind = navigationType switch
        {
            BrouterNavigationType.Replace => "replace",
            BrouterNavigationType.Pop => "pop",
            _ => "push",
        };

        try
        {
            var module = await GetModuleAsync();
            return await module.InvokeAsync<bool>("beginViewTransition", kind,
                _options.ViewTransitionDefaultAnimations, _options.ViewTransitionRespectReducedMotion);
        }
        catch (JSDisconnectedException ex) { LogSuppressedJsFailure(ex, "circuit disconnected mid-call"); }
        catch (JSException ex) { LogSuppressedJsFailure(ex, "JS interop failure (e.g. non-browser host)"); }
        catch (InvalidOperationException ex) { LogSuppressedJsFailure(ex, "JS interop unavailable during pre-render"); }
        catch (TaskCanceledException ex) { LogSuppressedJsFailure(ex, "component disposed mid-call"); }
        return false;
    }

    /// <summary>Resolves the pending View Transition's update promise so the browser animates to the new DOM.</summary>
    internal ValueTask CompleteViewTransitionAsync() =>
        SafeJsCallAsync(async () =>
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync("completeViewTransition");
        });

    public ValueTask SetConfirmExternalNavigationAsync(bool enabled) =>
        // Best-effort like the other fire-and-forget interop: during prerender or on a disconnected
        // circuit there is no browser to arm, and the interactive pass re-arms via BrouterOptions
        // when the always-on option is used.
        SafeJsCallAsync(async () =>
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync("setConfirmExternalNavigation", enabled);
        });

    private ValueTask GoAsync(int delta) =>
        // history.go(0) reloads the page; we reject delta == 0 above so we never hit that.
        SafeJsCallAsync(() => _js.InvokeVoidAsync("history.go", delta));

    // Runs a JS interop call, swallowing the four failures that are expected and non-fatal for
    // Brouter's fire-and-forget interop (navigation effects, scroll save, history.go, module dispose):
    // a disconnected circuit, a generic interop failure (e.g. a non-browser host), interop being
    // unavailable during prerender, and the component being disposed mid-call. Centralized so every
    // JS call site handles the same set identically instead of repeating the catch block. Suppressed
    // failures are logged at Debug so real problems remain diagnosable without becoming fatal.
    private async ValueTask SafeJsCallAsync(Func<ValueTask> call)
    {
        try { await call(); }
        catch (JSDisconnectedException ex) { LogSuppressedJsFailure(ex, "circuit disconnected mid-call"); }
        catch (JSException ex) { LogSuppressedJsFailure(ex, "JS interop failure (e.g. non-browser host)"); }
        catch (InvalidOperationException ex) { LogSuppressedJsFailure(ex, "JS interop unavailable during pre-render"); }
        catch (TaskCanceledException ex) { LogSuppressedJsFailure(ex, "component disposed mid-call"); }
    }

    private void LogSuppressedJsFailure(Exception exception, string reason) =>
        _logger.LogDebug(exception, "Suppressed a non-fatal Brouter JS interop failure ({Reason}).", reason);

    public void NavigateToName(string name, IReadOnlyDictionary<string, object?>? parameters = null,
                               string? query = null, bool replace = false, string? historyState = null)
    {
        var url = ResolveUrl(name, parameters, query);
        Navigate(url, replace: replace, historyState: historyState);
    }

    public string ResolveUrl(string name, IReadOnlyDictionary<string, object?>? parameters = null, string? query = null)
    {
        EnsureMounted();

        var route = _activeBrouter!.FindRouteByName(name)
            ?? throw new InvalidOperationException($"No route is registered with the name '{name}'.");

        if (route.RouteTemplate is null)
            throw new InvalidOperationException($"Route '{name}' has no template.");

        // Normalize parameters into a case-insensitive dictionary so that segment.Value lookups
        // succeed regardless of the casing the caller used for dictionary keys.
        var normalizedParams = parameters is null
            ? null
            : new Dictionary<string, object?>(parameters, StringComparer.OrdinalIgnoreCase);

        var sb = new StringBuilder();
        // Tracks whether a preceding trailing optional was omitted. If so, no later optional
        // may carry a value: emitting it would shift the value into the missing optional's
        // slot ("/a/{b?}/{c?}" + only c => "/a/x" reads back as b="x"). The matcher can't
        // bind that back to the original parameter, so fail loud rather than silently mis-bind.
        var optionalOmitted = false;
        string? omittedOptionalName = null;

        // Names of template parameters, filled in during the segment walk below. Any dictionary
        // entry whose key isn't in this set is appended as a query-string pair after the path,
        // rather than being silently dropped (mirrors ASP.NET's LinkGenerator / React Router).
        var consumedNames = normalizedParams is null ? null : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var segment in route.RouteTemplate.TemplateSegments)
        {
            sb.Append('/');

            if (segment.IsParameter is false)
            {
                if (segment.IsCatchAll || segment.IsSingleWildcard)
                    throw new InvalidOperationException(
                        $"Cannot resolve route '{name}' that contains literal wildcards. " +
                        "Use a catch-all parameter (e.g. '{{**path}}') instead.");

                sb.Append(segment.Value);
                continue;
            }

            consumedNames?.Add(segment.Value);

            var hasValue = normalizedParams is not null && normalizedParams.TryGetValue(segment.Value, out var raw) && raw is not null;
            if (hasValue is false)
            {
                if (segment.IsOptional)
                {
                    // Drop trailing '/' for the absent optional segment.
                    if (sb.Length > 0 && sb[^1] == '/') sb.Length--;
                    optionalOmitted = true;
                    omittedOptionalName ??= segment.Value;
                    continue;
                }
                throw new ArgumentException(
                    $"Missing value for required route parameter '{segment.Value}' when resolving route '{name}'.",
                    nameof(parameters));
            }

            // A trailing optional with a value can't follow an omitted earlier optional, since
            // optionals only ever live at the tail of a template (TemplateParser enforces this).
            // Allowing it would emit a URL that re-binds to the wrong parameter.
            if (segment.IsOptional && optionalOmitted)
            {
                throw new ArgumentException(
                    $"Cannot resolve route '{name}': optional parameter '{omittedOptionalName}' is missing " +
                    $"but a later optional parameter '{segment.Value}' has a value. " +
                    "Trailing optionals must be filled in order from left to right.",
                    nameof(parameters));
            }

            var rawValue = FormatRouteValue(normalizedParams![segment.Value]);

            // Validate the formatted value against any constraints declared on this segment.
            // Without this check ResolveUrl would happily emit a URL ("/users/abc" for {id:int})
            // that fails to match its own template, surfacing as a confusing NotFound far from
            // the call site. Run constraints on the formatted (string) value, mirroring what
            // the matcher will do when the URL comes back through LocationChanged.
            // Catch-all parameters never carry constraints (TemplateParser rejects them) so
            // we don't need a special branch here.
            if (segment.IsParameter && segment.Constraints.Length > 0 && string.IsNullOrEmpty(rawValue) is false)
            {
                foreach (var binding in segment.Constraints)
                {
                    if (binding.Constraint.TryMatch(rawValue, out _) is false)
                    {
                        throw new ArgumentException(
                            $"Value '{rawValue}' for route parameter '{segment.Value}' fails the '{binding.Name}' constraint " +
                            $"on route '{name}'. The generated URL would not round-trip back through this template.",
                            nameof(parameters));
                    }
                }
            }

            // An optional parameter supplied with an empty value is treated the same as a
            // missing one: drop the trailing '/' and mark the optional as omitted so the
            // ordering check above catches any later optional that does carry a value.
            if (segment.IsOptional && string.IsNullOrEmpty(rawValue))
            {
                if (sb.Length > 0 && sb[^1] == '/') sb.Length--;
                optionalOmitted = true;
                omittedOptionalName ??= segment.Value;
                continue;
            }

            // Required non-catch-all segments must round-trip through the router; an empty
            // formatted value would emit a stray '/' (e.g. "/users//edit") that the matcher
            // can't bind back. Catch-all is exempt because it has its own empty handling
            // below (it just drops the trailing slash). Optional missing values are already
            // handled above via the 'hasValue is false' branch.
            if (segment.IsCatchAll is false && string.IsNullOrEmpty(rawValue))
            {
                throw new ArgumentException(
                    $"Missing value for required route parameter '{segment.Value}' when resolving route '{name}'.",
                    nameof(parameters));
            }

            if (segment.IsCatchAll)
            {
                // Allow forward slashes in catch-all values; encode each segment separately.
                var parts = rawValue.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    // Empty catch-all value: remove the trailing '/' we just appended.
                    if (sb.Length > 0 && sb[^1] == '/') sb.Length--;
                }
                else
                {
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (i > 0) sb.Append('/');
                        sb.Append(Uri.EscapeDataString(parts[i]));
                    }
                }
            }
            else
            {
                sb.Append(Uri.EscapeDataString(rawValue));
            }
        }

        if (sb.Length == 0) sb.Append('/');

        var hasQuery = false;
        if (string.IsNullOrEmpty(query) is false)
        {
            sb.Append(query.StartsWith('?') ? query : "?" + query);
            hasQuery = true;
        }

        // Dictionary entries that didn't bind to a template parameter become query-string pairs.
        // Null values are skipped (null already means "absent" for route parameters, so the same
        // convention applies here); non-string enumerables emit one pair per element ("tag=a&tag=b").
        if (normalizedParams is not null)
        {
            foreach (var (key, value) in normalizedParams)
            {
                if (consumedNames!.Contains(key) || value is null) continue;

                if (value is not string && value is System.Collections.IEnumerable items)
                {
                    foreach (var item in items)
                    {
                        if (item is null) continue;
                        AppendQueryPair(sb, key, FormatRouteValue(item), ref hasQuery);
                    }
                }
                else
                {
                    AppendQueryPair(sb, key, FormatRouteValue(value), ref hasQuery);
                }
            }
        }

        return sb.ToString();
    }

    private static void AppendQueryPair(StringBuilder sb, string key, string value, ref bool hasQuery)
    {
        sb.Append(hasQuery ? '&' : '?');
        hasQuery = true;
        sb.Append(Uri.EscapeDataString(key)).Append('=').Append(Uri.EscapeDataString(value));
    }

    private void EnsureMounted()
    {
        if (_activeBrouter is null || _navigationManager is null)
            throw new InvalidOperationException("No Brouter is currently mounted.");
    }

    // Internal (not private): BrouterQueryBuilder formats query values with the identical rules so
    // ResolveUrl-emitted and builder-emitted parameters always round-trip the same way.
    internal static string FormatRouteValue(object? value)
    {
        if (value is null) return string.Empty;

        // Strings pass through unchanged.
        if (value is string s) return s;

        // Booleans: use lowercased invariant form ("true"/"false") for stable, parseable URLs.
        if (value is bool b) return b ? "true" : "false";

        // Enums: emit the symbolic name rather than a (locale-independent but opaque) numeric value.
        if (value is Enum e) return e.ToString();

        // Round-trippable date/time formats.
        if (value is DateTime dt) return dt.ToString("o", CultureInfo.InvariantCulture);
        if (value is DateTimeOffset dto) return dto.ToString("o", CultureInfo.InvariantCulture);
        if (value is TimeSpan ts) return ts.ToString("c", CultureInfo.InvariantCulture);
        if (value is DateOnly d) return d.ToString("o", CultureInfo.InvariantCulture);
        if (value is TimeOnly t) return t.ToString("o", CultureInfo.InvariantCulture);

        // Numerics and other formattable types: force invariant culture.
        if (value is IFormattable f) return f.ToString(null, CultureInfo.InvariantCulture);

        return value.ToString() ?? string.Empty;
    }


    public event Func<BrouterNavigationContext, ValueTask>? OnNavigating;
    public event Func<BrouterNavigationContext, ValueTask>? OnNavigated;
    public event Func<BrouterNavigationContext, Exception?, ValueTask>? OnError;

    internal async ValueTask InvokeOnNavigating(BrouterNavigationContext ctx)
    {
        var handlers = OnNavigating;
        if (handlers is null) return;

        // No ConfigureAwait(false): user handlers typically touch UI state (StateHasChanged,
        // NavigationManager calls). Stay on the Blazor renderer's synchronization context.
        foreach (var handler in handlers.GetInvocationList().Cast<Func<BrouterNavigationContext, ValueTask>>())
        {
            await handler(ctx);
            if (ctx.IsCancelled || ctx.RedirectUrl is not null) return;
        }
    }

    internal async ValueTask InvokeOnNavigated(BrouterNavigationContext ctx)
    {
        var handlers = OnNavigated;
        if (handlers is null) return;

        foreach (var handler in handlers.GetInvocationList().Cast<Func<BrouterNavigationContext, ValueTask>>())
        {
            try { await handler(ctx); }
            catch
            {
                // OnNavigated is best-effort: a faulty user handler must not break the navigation
                // flow or kill subsequent handlers in the invocation list. We deliberately swallow
                // anything user code throws; a global OnError handler stays the place to observe
                // pipeline-level failures (loaders, guards, etc.).
            }
        }
    }

    internal async ValueTask InvokeOnError(BrouterNavigationContext ctx, Exception? ex)
    {
        var handlers = OnError;
        if (handlers is null) return;

        foreach (var handler in handlers.GetInvocationList().Cast<Func<BrouterNavigationContext, Exception?, ValueTask>>())
        {
            try { await handler(ctx, ex); }
            catch
            {
                // OnError is the last line of defense; if a user OnError handler itself throws,
                // there is nothing higher up that can usefully react to it. Swallow so the rest
                // of the invocation list still runs and the original error reporting completes.
            }
        }
    }


    /// <summary>
    /// Applies the post-navigation DOM effects for <paramref name="location"/>: scrolling a URL
    /// fragment into view, moving focus for assistive technologies (see
    /// <see cref="BrouterOptions.FocusOnNavigateSelector"/>), and scroll-to-top. Invoked from
    /// <c>Brouter.OnAfterRenderAsync</c> once the matched route is committed to the DOM, so fragment
    /// and focus selectors resolve against the new page content rather than the previous one.
    /// </summary>
    internal async ValueTask ApplyNavigationEffectsAsync(BrouterLocation location)
    {
        var hash = location.Hash;
        var scrollToFragment = _options.ScrollToFragment && string.IsNullOrEmpty(hash) is false;
        var scrollToTop = _options.ScrollBehavior == BrouterScrollMode.ToTop;
        var focusSelector = _options.FocusOnNavigateSelector;
        var hasFocus = string.IsNullOrEmpty(focusSelector) is false;
        var restore = _options.RestoreScrollPosition;

        // Nothing configured for this navigation -> don't even import the JS module.
        if (scrollToFragment is false && scrollToTop is false && hasFocus is false && restore is false) return;

        // No ConfigureAwait(false): this is awaited from Brouter.OnAfterRenderAsync, so stay on the
        // renderer's synchronization context (required on Blazor Server for interop/state).
        await SafeJsCallAsync(async () =>
        {
            var module = await GetModuleAsync();

            await module.InvokeVoidAsync(
                "applyNavigationEffects",
                scrollToFragment ? hash : null,
                hasFocus ? focusSelector : null,
                scrollToTop,
                // On a Back/Forward the JS side restores the position remembered for this URL; the key
                // is only sent when restoration is enabled so it stays inert (and browser-native
                // restoration untouched) otherwise.
                restore ? location.FullUri : null,
                restore ? ScrollStorageKind : null);
        });
    }

    /// <summary>
    /// Records the scroll position of the page being navigated away from (<paramref name="from"/>) so a
    /// later Back/Forward to that URL can restore it. Invoked from the commit pipeline BEFORE the new
    /// route renders, so the JS side reads the outgoing page's scroll offset rather than the new one.
    /// A no-op unless <see cref="BrouterOptions.RestoreScrollPosition"/> is enabled and there is an
    /// actual page to leave (skipped on the initial load, where <paramref name="from"/> is empty).
    /// </summary>
    internal async ValueTask SaveScrollPositionAsync(BrouterLocation from)
    {
        if (_options.RestoreScrollPosition is false) return;
        if (string.IsNullOrEmpty(from.FullUri)) return;

        await SafeJsCallAsync(async () =>
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync("saveScrollPosition", from.FullUri, ScrollStorageKind);
        });
    }

    // Maps the configured storage mode to the token the JS module understands ('session'/'local'),
    // or null for in-memory. Kept here so both interop call sites stay in sync.
    private string? ScrollStorageKind => _options.ScrollPositionStorage switch
    {
        BrouterScrollPositionStorage.SessionStorage => "session",
        BrouterScrollPositionStorage.LocalStorage => "local",
        _ => null
    };

    // Internal (not private) so BrouterLink can share the scope's single module instance
    // instead of each link importing its own copy.
    internal async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        // Exceptions bubble to the caller's catch block (SafeJsCallAsync here, BrouterLink's
        // wiring try/catch), which handles the pre-render / disconnected / non-browser cases
        // uniformly. A failed import must not stay cached: during pre-render interop throws,
        // and a caller retrying later (once interop is available) should get a fresh attempt
        // rather than the memoised failure.
        var task = _moduleTask ??= _js.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Bit.Brouter/bit-brouter.js").AsTask();

        try
        {
            return await task;
        }
        catch
        {
            if (ReferenceEquals(_moduleTask, task)) _moduleTask = null;
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        var task = _moduleTask;
        _moduleTask = null;
        if (task is not null)
        {
            // If the import itself failed, awaiting it rethrows one of the four expected
            // interop failures, which SafeJsCallAsync swallows.
            await SafeJsCallAsync(async () =>
            {
                var module = await task;
                await module.DisposeAsync();
            });
        }
    }
}
