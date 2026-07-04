using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Bit.Brouter;

internal sealed class BrouterService : IBrouter, IAsyncDisposable
{
    private readonly BrouterOptions _options;
    private readonly IJSRuntime _js;
    private Brouter? _activeBrouter;
    private NavigationManager? _navigationManager;

    // Lazily-imported BitBrouter.js module, used for the post-navigation DOM effects
    // (fragment/top scroll, focus). Imported on first use and reused for the scope's lifetime;
    // disposed in DisposeAsync when DI tears the scoped service down.
    private IJSObjectReference? _module;

    public BrouterService(IOptions<BrouterOptions> options, IJSRuntime js)
    {
        // Resolve once: BrouterService is scoped, BrouterOptions is registered as a singleton
        // via AddOptions, so the resolved value is stable for the lifetime of the scope.
        // We deliberately don't take IOptionsMonitor here because route matching, link
        // activation and link rendering all read these flags many times per navigation;
        // changing options at runtime would require rebuilding the matcher's case rules
        // and re-evaluating every active link, which isn't a supported scenario right now.
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _js = js;
    }

    internal BrouterOptions Options => _options;

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

    public void Navigate(string url, bool replace = false, bool forceLoad = false)
    {
        EnsureMounted();
        // Tell the pipeline this navigation is a push/replace before triggering it, so guards, loaders
        // and hooks report the right BrouterNavigationType. Skipped for forceLoad: it's a full-page
        // reload, so no SPA pipeline runs and there is nothing to classify. Back/Forward don't come
        // through here (they use history.go), so they correctly fall through to Pop detection.
        if (forceLoad is false)
            _activeBrouter!.SetPendingNavigationType(replace ? BrouterNavigationType.Replace : BrouterNavigationType.Push);
        _navigationManager!.NavigateTo(url, forceLoad: forceLoad, replace: replace);
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
        return GoAsync(delta);
    }

    private async ValueTask GoAsync(int delta)
    {
        try
        {
            // history.go(0) reloads the page; we reject delta == 0 above so we never hit that.
            await _js.InvokeVoidAsync("history.go", delta).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { /* Circuit disconnected; nothing to do. */ }
        catch (JSException) { /* JS interop failure; nothing to do. */ }
        catch (InvalidOperationException) { /* JS interop not available during pre-render. */ }
        catch (TaskCanceledException) { /* Component disposed mid-call. */ }
    }

    public void NavigateToName(string name, IReadOnlyDictionary<string, object?>? parameters = null,
                               string? query = null, bool replace = false)
    {
        var url = ResolveUrl(name, parameters, query);
        Navigate(url, replace: replace);
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

        if (string.IsNullOrEmpty(query) is false)
        {
            sb.Append(query.StartsWith('?') ? query : "?" + query);
        }

        return sb.ToString();
    }

    private void EnsureMounted()
    {
        if (_activeBrouter is null || _navigationManager is null)
            throw new InvalidOperationException("No Brouter is currently mounted.");
    }

    private static string FormatRouteValue(object? value)
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
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return;

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
        }
        catch (JSDisconnectedException) { /* circuit disconnected mid-call */ }
        catch (JSException) { /* JS interop failure (e.g. non-browser host) */ }
        catch (InvalidOperationException) { /* JS interop unavailable during pre-render */ }
        catch (TaskCanceledException) { /* component disposed mid-call */ }
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

        try
        {
            var module = await GetModuleAsync();
            if (module is null) return;

            await module.InvokeVoidAsync("saveScrollPosition", from.FullUri, ScrollStorageKind);
        }
        catch (JSDisconnectedException) { /* circuit disconnected mid-call */ }
        catch (JSException) { /* JS interop failure (e.g. non-browser host) */ }
        catch (InvalidOperationException) { /* JS interop unavailable during pre-render */ }
        catch (TaskCanceledException) { /* component disposed mid-call */ }
    }

    // Maps the configured storage mode to the token the JS module understands ('session'/'local'),
    // or null for in-memory. Kept here so both interop call sites stay in sync.
    private string? ScrollStorageKind => _options.ScrollPositionStorage switch
    {
        BrouterScrollPositionStorage.SessionStorage => "session",
        BrouterScrollPositionStorage.LocalStorage => "local",
        _ => null
    };

    private async ValueTask<IJSObjectReference?> GetModuleAsync()
    {
        // Exceptions bubble to ApplyNavigationEffectsAsync's catch block, which handles the
        // pre-render / disconnected / non-browser cases uniformly.
        return _module ??= await _js.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Bit.Brouter/BitBrouter.js");
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try { await _module.DisposeAsync(); }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
            catch (InvalidOperationException) { }
            catch (TaskCanceledException) { }
            _module = null;
        }
    }
}
