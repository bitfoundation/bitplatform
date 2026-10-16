using System.Text.Json;
using Microsoft.JSInterop;

namespace Bit.Belemetry;

/// <summary>
/// The scoped connection to <c>bit-belemetry.js</c>: starts it, keeps consent and user context in step in both
/// directions, receives relayed records, and serves the unload drain.
/// </summary>
/// <remarks>
/// Every call tolerates the JS runtime being unavailable (prerendering, a disconnected circuit): telemetry must never
/// be the reason a page fails.
/// </remarks>
internal sealed class BelemetryJsBridge : IAsyncDisposable
{
    internal const string ModulePath = "./_content/Bit.Belemetry/bit-belemetry.js";

    private readonly IJSRuntime? _js;
    private readonly BelemetrySettings _settings;
    private readonly BelemetryPipeline _pipeline;
    private readonly BelemetryConsentState _consent;
    private readonly BelemetryBrowserBridgeRegistry _registry;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private IJSObjectReference? _module;
    private DotNetObjectReference<BelemetryJsCallbacks>? _callbacks;
    private bool _disposed;

    /// <param name="settings">The resolved settings.</param>
    /// <param name="pipeline">The .NET pipeline relayed records go to.</param>
    /// <param name="consent">The consent state the script's context is kept in step with.</param>
    /// <param name="registry">Where singletons find the script of the one page of a WebAssembly or Hybrid process.</param>
    /// <param name="js">
    /// The JS runtime, when the host has one. A host without Blazor (an API, a worker) registers none, and the DI
    /// container passes the default: there is no browser to bridge to, so the <see cref="IBelemetry"/> calls of such
    /// a host go straight to the .NET pipeline.
    /// </param>
    public BelemetryJsBridge(BelemetrySettings settings, BelemetryPipeline pipeline, BelemetryConsentState consent, BelemetryBrowserBridgeRegistry registry, IJSRuntime? js = null)
    {
        _js = js;
        _settings = settings;
        _pipeline = pipeline;
        _consent = consent;
        _registry = registry;
    }

    /// <summary>The last context the script reported (per circuit on Blazor Server).</summary>
    public BelemetryJsContext? Context { get; private set; }

    public bool IsStarted => _module is not null;

    /// <summary>Raised when the script reports a new context.</summary>
    public event Action<BelemetryJsContext>? ContextChanged;

    /// <summary>Starts the script (idempotent). Returns false when it cannot run here or now.</summary>
    public async ValueTask<bool> StartAsync()
    {
        if (_module is not null) return true;
        if (_js is null || _disposed || _settings.Options.Enabled is false || _settings.Options.Browser.Enabled is false) return false;

        await _startLock.WaitAsync();
        try
        {
            if (_module is not null) return true;

            var module = await _js.InvokeAsync<IJSObjectReference>("import", ModulePath);
            await module.InvokeVoidAsync("start", BelemetryBrowserConfigFactory.ToElement(BelemetryBrowserConfigFactory.Create(_settings)));

            _callbacks = DotNetObjectReference.Create(new BelemetryJsCallbacks(this));
            var relay = _settings.BrowserTransport == BelemetryBrowserTransport.Relay;
            var unloadDrain = _settings.IsBrowser && _settings.CollectDotnet && _settings.HasExporters;
            using var attachOptions = JsonDocument.Parse($"{{\"relay\":{(relay ? "true" : "false")},\"unloadDrain\":{(unloadDrain ? "true" : "false")}}}");
            await module.InvokeVoidAsync("attachDotNet", _callbacks, attachOptions.RootElement);

            _module = module;

            if (_settings.IsServer is false)
            {
                // One page per process: singletons may reach the script through the registry.
                _registry.SendRequests = SendRequestsAsync;
                _registry.SetConsent = features => SetConsentAsync(features);
                _registry.SetUser = userId => SetUserAsync(userId);
            }

            var context = await GetContextAsync();
            if (context is not null) await SynchronizeAsync(context);
            return true;
        }
        catch (Exception ex) when (IsUnavailable(ex))
        {
            return false;
        }
        finally
        {
            _startLock.Release();
        }
    }

    /// <summary>
    /// Reconciles the .NET and browser views of consent once the script is up. On WebAssembly and Hybrid a decision
    /// made on the .NET side (restored from the file store, or set before the page was ready) wins and is pushed to the
    /// script; otherwise a decision the script restored from <c>localStorage</c> is adopted.
    /// </summary>
    private async Task SynchronizeAsync(BelemetryJsContext context)
    {
        if (_settings.IsServer is false && _consent.IsDecided && (context.Decided is false || context.Consent != (int)_consent.Granted))
        {
            await SetConsentAsync(_consent.Granted);
            if (_consent.UserId is { } userId) await SetUserAsync(userId);
            return;
        }

        Apply(context);
    }

    internal void Apply(BelemetryJsContext context)
    {
        Context = context;

        if (_settings.IsServer is false)
        {
            if (context.Decided) _consent.Update((BelemetryFeatures)context.Consent, decided: true);
            _consent.SetContext(context.SessionId, context.PseudoId, context.UserId);
        }

        ContextChanged?.Invoke(context);
    }

    public async ValueTask<BelemetryJsContext?> GetContextAsync()
    {
        if (_module is null) return null;

        try
        {
            var element = await _module.InvokeAsync<JsonElement>("getContext");
            return element.Deserialize(BelemetryJsonContext.Default.BelemetryJsContext);
        }
        catch (Exception ex) when (IsUnavailable(ex) || ex is JsonException)
        {
            return null;
        }
    }

    public ValueTask SetConsentAsync(BelemetryFeatures features) => InvokeAsync("setConsent", (int)features);

    public ValueTask SetUserAsync(string? userId) => InvokeAsync("setUser", userId);

    public ValueTask TrackEventAsync(string name, IReadOnlyDictionary<string, object?>? attributes) => InvokeAsync("trackEvent", name, ToElement(attributes));

    public ValueTask TrackPageViewAsync(string? name, string? url, IReadOnlyDictionary<string, object?>? attributes) => InvokeAsync("trackPageView", name, url, ToElement(attributes));

    public ValueTask TrackExceptionAsync(string type, string message, string? stack, IReadOnlyDictionary<string, object?>? attributes) => InvokeAsync("trackException", type, message, stack, ToElement(attributes));

    public ValueTask FlushAsync() => InvokeAsync("flush");

    private async ValueTask SendRequestsAsync(IReadOnlyList<BelemetryHttpRequest> requests)
    {
        var element = JsonSerializer.SerializeToElement(requests.ToList(), BelemetryJsonContext.Default.ListBelemetryHttpRequest);
        await InvokeAsync("sendRequests", element);
    }

    private async ValueTask InvokeAsync(string identifier, params object?[] args)
    {
        if (await StartAsync() is false || _module is null) return;

        try
        {
            await _module.InvokeVoidAsync(identifier, args);
        }
        catch (Exception ex) when (IsUnavailable(ex))
        {
            // The page went away; nothing to deliver it to.
        }
    }

    internal Task RelayAsync(JsonElement payload)
    {
        try
        {
            if (payload.Deserialize(BelemetryJsonContext.Default.BelemetryRelayBatch) is { } batch) _pipeline.Relay(batch);
        }
        catch (JsonException ex)
        {
            _pipeline.LogDiagnostic(ex, "A relayed browser batch could not be read.");
        }
        return Task.CompletedTask;
    }

    internal JsonElement DrainForUnload()
    {
        var requests = _pipeline.DrainForUnload();
        return JsonSerializer.SerializeToElement(requests, BelemetryJsonContext.Default.ListBelemetryHttpRequest);
    }

    private static JsonElement? ToElement(IReadOnlyDictionary<string, object?>? attributes)
    {
        if (attributes is null || attributes.Count == 0) return null;

        var normalized = new Dictionary<string, object>(attributes.Count);
        foreach (var (key, value) in attributes)
        {
            if (BelemetryAttributes.Normalize(value) is { } v) normalized[key] = v;
        }
        return JsonSerializer.SerializeToElement(normalized, BelemetryJsonContext.Default.DictionaryStringObject);
    }

    private static bool IsUnavailable(Exception ex)
    {
        // Prerendering (InvalidOperationException), a gone circuit (JSDisconnectedException), a canceled call, or a
        // script that failed to load (JSException) - all mean "no browser to talk to right now".
        return ex is InvalidOperationException or JSDisconnectedException or TaskCanceledException or OperationCanceledException or JSException or ObjectDisposedException;
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;

        if (_settings.IsServer is false && _registry.SendRequests == SendRequestsAsync)
        {
            _registry.SendRequests = null;
            _registry.SetConsent = null;
            _registry.SetUser = null;
        }

        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (Exception ex) when (IsUnavailable(ex))
            {
                // Already gone.
            }
        }

        _callbacks?.Dispose();
        _startLock.Dispose();
    }
}
