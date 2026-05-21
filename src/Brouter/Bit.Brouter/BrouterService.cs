using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Brouter;

internal sealed class BrouterService : IBrouter
{
    private readonly BrouterOptions _options;
    private readonly IJSRuntime _js;
    private Brouter? _activeBrouter;
    private NavigationManager? _navigationManager;

    public BrouterService(BrouterOptions options, IJSRuntime js)
    {
        _options = options;
        _js = js;
    }

    internal BrouterOptions Options => _options;

    internal void Attach(Brouter brouter, NavigationManager navManager)
    {
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
        _navigationManager!.NavigateTo(url, forceLoad: forceLoad, replace: replace);
    }

    public void Back()
    {
        EnsureMounted();
        _ = BackAsync();
    }

    private async Task BackAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("history.back").ConfigureAwait(false);
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
                    continue;
                }
                throw new ArgumentException(
                    $"Missing value for required route parameter '{segment.Value}' when resolving route '{name}'.",
                    nameof(parameters));
            }

            var rawValue = normalizedParams![segment.Value]!.ToString() ?? string.Empty;

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


    public event Func<NavigationContext, ValueTask>? OnNavigating;
    public event Func<NavigationContext, ValueTask>? OnNavigated;
    public event Func<NavigationContext, Exception?, ValueTask>? OnError;

    internal async ValueTask InvokeOnNavigating(NavigationContext ctx)
    {
        var handlers = OnNavigating;
        if (handlers is null) return;

        foreach (var handler in handlers.GetInvocationList().Cast<Func<NavigationContext, ValueTask>>())
        {
            await handler(ctx).ConfigureAwait(false);
            if (ctx.IsCancelled || ctx.RedirectUrl is not null) return;
        }
    }

    internal async ValueTask InvokeOnNavigated(NavigationContext ctx)
    {
        var handlers = OnNavigated;
        if (handlers is null) return;

        foreach (var handler in handlers.GetInvocationList().Cast<Func<NavigationContext, ValueTask>>())
        {
            try { await handler(ctx).ConfigureAwait(false); }
            catch { /* OnNavigated should not break navigation flow */ }
        }
    }

    internal async ValueTask InvokeOnError(NavigationContext ctx, Exception? ex)
    {
        var handlers = OnError;
        if (handlers is null) return;

        foreach (var handler in handlers.GetInvocationList().Cast<Func<NavigationContext, Exception?, ValueTask>>())
        {
            try { await handler(ctx, ex).ConfigureAwait(false); }
            catch { /* swallow secondary errors */ }
        }
    }


    internal async ValueTask ApplyScrollAsync()
    {
        if (_options.ScrollBehavior != BrouterScrollMode.ToTop) return;
        try { await _js.InvokeVoidAsync("window.scrollTo", 0, 0).ConfigureAwait(false); }
        catch { /* no-op during pre-render or when JS interop is unavailable */ }
    }
}
