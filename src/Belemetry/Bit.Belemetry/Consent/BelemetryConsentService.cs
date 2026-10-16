namespace Bit.Belemetry;

/// <summary>The scoped <see cref="IBelemetryConsent"/>.</summary>
internal sealed class BelemetryConsentService : IBelemetryConsent, IDisposable
{
    private readonly BelemetrySettings _settings;
    private readonly BelemetryConsentState _state;
    private readonly BelemetryJsBridge _bridge;

    public BelemetryConsentService(BelemetrySettings settings, BelemetryConsentState state, BelemetryJsBridge bridge)
    {
        _settings = settings;
        _state = state;
        _bridge = bridge;

        if (_settings.IsServer) _bridge.ContextChanged += OnBrowserContextChanged;
        else _state.Changed += OnStateChanged;
    }

    public BelemetryFeatures Available => _settings.ConfiguredFeatures;

    public BelemetryFeatures Granted => Available & (_settings.IsServer
        ? (BelemetryFeatures?)_bridge.Context?.Consent ?? _settings.Options.Consent.Default
        : _state.Granted);

    public bool IsDecided => _settings.IsServer ? _bridge.Context?.Decided ?? false : _state.IsDecided;

    public event EventHandler? Changed;

    public bool IsGranted(BelemetryFeatures feature) => feature != BelemetryFeatures.None && (Granted & feature) == feature;

    public async ValueTask SetAsync(BelemetryFeatures features)
    {
        // A grant of a feature the configuration switched off would be recorded but never collected.
        features &= Available;

        if (_settings.IsServer is false)
        {
            _state.Decide(features);
            BelemetryConsentFile.Save(_settings, features);
        }

        await _bridge.SetConsentAsync(features);

        if (_settings.IsServer)
        {
            // Read back what the script decided (Global Privacy Control may have narrowed it).
            if (await _bridge.GetContextAsync() is { } context) _bridge.Apply(context);
        }
    }

    public ValueTask GrantAsync(BelemetryFeatures features) => SetAsync(Granted | features);

    public ValueTask RevokeAsync(BelemetryFeatures features) => SetAsync(Granted & ~features);

    public async ValueTask RefreshAsync()
    {
        if (await _bridge.StartAsync() && await _bridge.GetContextAsync() is { } context) _bridge.Apply(context);
    }

    private void OnBrowserContextChanged(BelemetryJsContext context) => Changed?.Invoke(this, EventArgs.Empty);

    private void OnStateChanged(BelemetryFeatures previous, BelemetryFeatures current) => Changed?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        _bridge.ContextChanged -= OnBrowserContextChanged;
        _state.Changed -= OnStateChanged;
    }
}
