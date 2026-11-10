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
    private readonly BelemetryRelaySource _relaySource;
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

        // On a server the page at the other end of the circuit can be anything that speaks the interop protocol, and
        // the queue it relays into is shared by every user: it gets the most an honest page sends at once (a full
        // queue plus the records a consent decision releases) as a burst, and MaxRelayedPerMinute after it - a rate of
        // its own, since how many records a page holds says nothing about how fast an honest one produces them.
        _relaySource = new BelemetryRelaySource(settings.IsServer
            ? new BelemetryRateLimiter(Math.Max(1, settings.Options.Browser.MaxQueueSize) + Math.Max(0, settings.Options.Consent.PendingLimit), settings.Options.Browser.MaxRelayedPerMinute, TimeSpan.FromMinutes(1))
            : null);
    }

    /// <summary>The last context the script reported (per circuit on Blazor Server).</summary>
    public BelemetryJsContext? Context { get; private set; }

    /// <summary>The page's last grant, apart from the bridge: what a capture keeps instead of the bridge.</summary>
    public BelemetryPageConsent PageConsent { get; } = new();

    public bool IsStarted => _module is not null;

    private bool UnloadDrain => _settings.IsBrowser && _settings.CollectDotnet && _settings.HasExporters;

    /// <summary>Raised when the script reports a new context.</summary>
    public event Action<BelemetryJsContext>? ContextChanged;

    /// <summary>Raised when the page reports a JavaScript error as a diagnostic trigger (type, message).</summary>
    public event Action<string?, string?>? DiagnosticTriggered;

    // A browser timeline is at most BrowserMaxEvents (1000) events; the script trims a larger one to fit (getDiagnosticsStream).
    private const long MaxDiagnosticsBytes = 4 * 1024 * 1024;

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
            // A script started already (a server's <BelemetryScript />, before a circuit or an Auto app's WebAssembly
            // runtime) has the routes its page was rendered with, which a repeated start adds to: only the configured
            // ones are listed again (they may be this side's own), not the discovered ones - those would cost their
            // bytes on the wire and, where they are not found yet (WebAssembly), a scan of the app's assemblies.
            var includeDiscoveredRoutes = await module.InvokeAsync<bool>("isStarted") is false;
            await module.InvokeVoidAsync("start", BelemetryBrowserConfigFactory.ToElement(BelemetryBrowserConfigFactory.Create(_settings, includeDiscoveredRoutes)));
            if (includeDiscoveredRoutes) _pipeline.WarnIfNoPageRoutes();

            _callbacks = DotNetObjectReference.Create(new BelemetryJsCallbacks(this));
            var relay = _settings.BrowserTransport == BelemetryBrowserTransport.Relay;
            using var attachOptions = JsonDocument.Parse($"{{\"relay\":{(relay ? "true" : "false")},\"unloadDrain\":{(UnloadDrain ? "true" : "false")}}}");
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
        PageConsent.Update(context.Consent);

        if (_settings.IsServer is false)
        {
            if (context.Decided) _consent.Update((BelemetryFeatures)context.Consent, decided: true);
            else if (_consent.IsDecided is false) _consent.Update(_consent.Granted & (BelemetryFeatures)context.Consent, decided: false);
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

    /// <summary>
    /// The script's diagnostic timeline, or null when it is unreachable or keeps none. A server reads it as a stream: a
    /// circuit's messages from the page are capped (32 KiB by default), and a timeline can be larger.
    /// </summary>
    public async ValueTask<JsonElement?> GetDiagnosticsAsync(CancellationToken cancellationToken)
    {
        if (await StartAsync() is false || _module is null) return null;

        try
        {
            if (_settings.IsServer)
            {
                var reference = await _module.InvokeAsync<IJSStreamReference?>("getDiagnosticsStream", cancellationToken);
                if (reference is null) return null;
                await using (reference)
                {
                    await using var stream = await reference.OpenReadStreamAsync(MaxDiagnosticsBytes, cancellationToken);
                    using var document = await JsonDocument.ParseAsync(stream, new JsonDocumentOptions { MaxDepth = 16 }, cancellationToken);
                    return document.RootElement.Clone();
                }
            }

            var element = await _module.InvokeAsync<JsonElement>("getDiagnostics", cancellationToken);
            return element.ValueKind == JsonValueKind.Object ? element : null;
        }
        // ArgumentOutOfRangeException: a stream larger than MaxDiagnosticsBytes (a page that did not trim it, or an
        // untrusted one) - the browser evidence is missing, the capture still goes on.
        catch (Exception ex) when (IsUnavailable(ex) || ex is JsonException or IOException or ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>Forgets the page's diagnostic timeline and its stored copy.</summary>
    public ValueTask ClearDiagnosticsAsync() => InvokeAsync("clearDiagnostics");

    /// <summary>
    /// Saves a file in the browser (a download). False when there is no browser to save it in - nor a web view that
    /// saves downloads: Android's WebView and WKWebView (MAUI on Android, iOS and Mac Catalyst) ignore one without a
    /// download handler of the app's, so the script's click would report a save that never happened.
    /// </summary>
    public async ValueTask<bool> DownloadAsync(string fileName, byte[] bytes, CancellationToken cancellationToken)
    {
        if (SavesDownloads is false || await StartAsync() is false || _module is null) return false;

        try
        {
            if (_settings.IsServer)
            {
                // Streamed: a bundle can be larger than a circuit's single message.
                using var reference = new DotNetStreamReference(new MemoryStream(bytes, writable: false));
                await _module.InvokeVoidAsync("downloadFile", cancellationToken, fileName, reference);
            }
            else
            {
                await _module.InvokeVoidAsync("downloadFile", cancellationToken, fileName, bytes);
            }
            return true;
        }
        catch (Exception ex) when (IsUnavailable(ex))
        {
            return false;
        }
    }

    // WebView2 (Windows: WinForms, WPF, MAUI) saves downloads by default; the mobile and Apple web views do not.
    private bool SavesDownloads => _settings.Host != BelemetryHostKind.Hybrid || OperatingSystem.IsWindows();

    internal void OnDiagnosticTrigger(JsonElement error)
    {
        if (error.ValueKind != JsonValueKind.Object) return;

        string? Read(string name) => error.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        var type = Read("type");
        var message = Read("message");
        try
        {
            DiagnosticTriggered?.Invoke(type is { Length: > 256 } ? type[..256] : type, message is { Length: > 1024 } ? message[..1024] : message);
        }
        catch
        {
            // A trigger never fails the page's call.
        }
    }

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
        // Only a page told to relay sends batches. The callback object is handed to every page, whatever the transport,
        // so without this check any client of a server could feed the pipeline directly.
        if (_settings.BrowserTransport != BelemetryBrowserTransport.Relay) return Task.CompletedTask;

        try
        {
            if (payload.Deserialize(BelemetryJsonContext.Default.BelemetryRelayBatch) is { } batch) _pipeline.Relay(batch, _relaySource);
        }
        catch (JsonException ex)
        {
            _pipeline.LogDiagnostic(ex, "A relayed browser batch could not be read.");
        }
        return Task.CompletedTask;
    }

    internal JsonElement DrainForUnload()
    {
        var requests = UnloadDrain ? _pipeline.DrainForUnload() : [];
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
