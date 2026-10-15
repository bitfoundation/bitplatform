using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Bit.Bonboarding;

/// <summary>
/// The default browser layer: one lazily imported JS module per scope, one
/// <see cref="DotNetObjectReference"/> for every session, and no polling anywhere.
/// </summary>
internal sealed class BonboardingInterop : IBonboardingInterop, IDisposable
{
    private const string ModulePath = "./_content/Bit.Bonboarding/bit-bonboarding.js";

    private readonly IJSRuntime _js;
    private readonly BonboardingOptions _options;
    private readonly ILogger<BonboardingInterop> _logger;

    // Sessions attach and detach from the renderer while browser callbacks arrive, and a session can
    // be driven from outside the renderer altogether, so both maps tolerate concurrent access.
    private readonly ConcurrentDictionary<string, IBonboardingInteropCallbacks> _callbacks = new(StringComparer.Ordinal);

    /// <summary>The live activation of each session's current step. Absent while no step is active.</summary>
    private readonly ConcurrentDictionary<string, long> _activations = new(StringComparer.Ordinal);

    private readonly SemaphoreSlim _importGate = new(1, 1);

    private long _activationSource;

    private IJSObjectReference? _module;
    private DotNetObjectReference<BonboardingInterop>? _self;
    private bool _disposed;

    public BonboardingInterop(IJSRuntime js, IOptions<BonboardingOptions> options, ILogger<BonboardingInterop> logger)
    {
        _js = js;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsAvailable => !_disposed && _module is not null;

    public async ValueTask<BonboardingBrowserEnvironment> InitializeAsync(CancellationToken cancellationToken = default)
    {
        var module = await EnsureModuleAsync(cancellationToken).ConfigureAwait(false);
        if (module is null) return BonboardingBrowserEnvironment.Unknown;

        return await module.InvokeAsync<BonboardingBrowserEnvironment>(
            "initialize",
            cancellationToken,
            new InitializeOptions(_options.EnableJsDiagnostics)).ConfigureAwait(false);
    }

    /// <summary>Payload for <c>initialize</c>; a named type so the trimmer can be told to keep it.</summary>
    internal sealed record InitializeOptions(bool EnableJsDiagnostics);

    private async ValueTask<IJSObjectReference?> EnsureModuleAsync(CancellationToken cancellationToken)
    {
        if (_disposed) return null;
        if (_module is not null) return _module;

        await _importGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_module is not null) return _module;

            // During prerendering this throws InvalidOperationException, which is a normal state
            // rather than an error: the engine simply runs without a browser until hydration.
            _module = await _js.InvokeAsync<IJSObjectReference>("import", cancellationToken, ModulePath)
                .ConfigureAwait(false);

            _self = DotNetObjectReference.Create(this);
            return _module;
        }
        finally
        {
            _importGate.Release();
        }
    }

    public async ValueTask AttachAsync(
        string sessionId, IBonboardingInteropCallbacks callbacks, CancellationToken cancellationToken = default)
    {
        var module = await EnsureModuleAsync(cancellationToken).ConfigureAwait(false);
        if (module is null) return;

        _callbacks[sessionId] = callbacks;
        await module.InvokeVoidAsync("attach", cancellationToken, sessionId, _self).ConfigureAwait(false);
    }

    public async ValueTask DetachAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _callbacks.TryRemove(sessionId, out _);
        _activations.TryRemove(sessionId, out _);

        if (_module is null) return;
        await _module.InvokeVoidAsync("detach", cancellationToken, sessionId).ConfigureAwait(false);
    }

    public async ValueTask<BonboardingStepGeometry> ActivateStepAsync(
        BonboardingStepActivation activation, CancellationToken cancellationToken = default)
    {
        var module = await EnsureModuleAsync(cancellationToken).ConfigureAwait(false);
        if (module is null) return BonboardingStepGeometry.Missing;

        // Recorded before the call: frames of this activation can overtake the reply.
        var id = Interlocked.Increment(ref _activationSource);
        _activations[activation.SessionId] = id;

        return await module.InvokeAsync<BonboardingStepGeometry>(
            "activateStep", cancellationToken, activation with { Activation = id }).ConfigureAwait(false);
    }

    public async ValueTask DeactivateStepAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        // From here on, anything the old step still has in flight is stale.
        _activations.TryRemove(sessionId, out _);

        if (_module is null) return;
        await _module.InvokeVoidAsync("deactivateStep", cancellationToken, sessionId).ConfigureAwait(false);
    }

    public async ValueTask<BonboardingStepGeometry> MeasureAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_module is null) return BonboardingStepGeometry.Missing;
        return await _module.InvokeAsync<BonboardingStepGeometry>("measure", cancellationToken, sessionId).ConfigureAwait(false);
    }

    public async ValueTask SetPopoverAsync(
        string sessionId, ElementReference? popover, bool trapFocus, CancellationToken cancellationToken = default)
    {
        if (_module is null) return;
        await _module.InvokeVoidAsync("setPopover", cancellationToken, sessionId, popover, trapFocus)
            .ConfigureAwait(false);
    }

    public async ValueTask CaptureFocusAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_module is null) return;
        await _module.InvokeVoidAsync("captureFocus", cancellationToken, sessionId).ConfigureAwait(false);
    }

    public async ValueTask RestoreFocusAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_module is null) return;
        await _module.InvokeVoidAsync("restoreFocus", cancellationToken, sessionId).ConfigureAwait(false);
    }

    public async ValueTask FocusPopoverAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_module is null) return;
        await _module.InvokeVoidAsync("focusPopover", cancellationToken, sessionId).ConfigureAwait(false);
    }

    // ---- Storage -----------------------------------------------------------

    public async ValueTask<string?> GetStorageAsync(string key, CancellationToken cancellationToken = default)
    {
        var module = await EnsureModuleAsync(cancellationToken).ConfigureAwait(false);
        if (module is null) return null;

        return await module.InvokeAsync<string?>("storageGet", cancellationToken, key).ConfigureAwait(false);
    }

    public async ValueTask SetStorageAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var module = await EnsureModuleAsync(cancellationToken).ConfigureAwait(false);
        if (module is null) return;

        await module.InvokeVoidAsync("storageSet", cancellationToken, key, value).ConfigureAwait(false);
    }

    public async ValueTask RemoveStorageAsync(string key, CancellationToken cancellationToken = default)
    {
        var module = await EnsureModuleAsync(cancellationToken).ConfigureAwait(false);
        if (module is null) return;

        await module.InvokeVoidAsync("storageRemove", cancellationToken, key).ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<string>> ListStorageKeysAsync(
        string prefix, CancellationToken cancellationToken = default)
    {
        var module = await EnsureModuleAsync(cancellationToken).ConfigureAwait(false);
        if (module is null) return [];

        return await module.InvokeAsync<string[]>("storageKeys", cancellationToken, prefix).ConfigureAwait(false);
    }

    // ---- Callbacks from the browser ---------------------------------------

    /// <summary>Payload for <see cref="OnKey"/>; kept as an object so the shape can grow.</summary>
    public sealed record KeyPayload(string Key, bool ShiftKey);

    // Step-scoped callbacks carry the activation they were raised for. One that was in flight when
    // its step was replaced must not be applied to the new step: a late advance trigger would skip
    // a step the user never saw, and a late frame would briefly place the new card against the old
    // target. Keys are not step-scoped: a key press always means the step on screen.

    [JSInvokable]
    public ValueTask OnGeometryChanged(string sessionId, BonboardingStepGeometry geometry)
        => IsCurrent(sessionId, geometry.Activation)
            ? Dispatch(sessionId, callbacks => callbacks.OnGeometryChangedAsync(geometry))
            : ValueTask.CompletedTask;

    [JSInvokable]
    public ValueTask OnTargetLost(string sessionId, long activation)
        => IsCurrent(sessionId, activation)
            ? Dispatch(sessionId, callbacks => callbacks.OnTargetLostAsync())
            : ValueTask.CompletedTask;

    [JSInvokable]
    public ValueTask OnKey(string sessionId, KeyPayload payload)
        => Dispatch(sessionId, callbacks => callbacks.OnKeyAsync(payload.Key, payload.ShiftKey));

    [JSInvokable]
    public ValueTask OnAdvanceTriggered(string sessionId, long activation)
        => IsCurrent(sessionId, activation)
            ? Dispatch(sessionId, callbacks => callbacks.OnAdvanceTriggeredAsync())
            : ValueTask.CompletedTask;

    [JSInvokable]
    public ValueTask OnWaitTimedOut(string sessionId, long activation)
        => IsCurrent(sessionId, activation)
            ? Dispatch(sessionId, callbacks => callbacks.OnWaitTimedOutAsync())
            : ValueTask.CompletedTask;

    private bool IsCurrent(string sessionId, long activation)
        => _activations.TryGetValue(sessionId, out var current) && current == activation;

    private async ValueTask Dispatch(string sessionId, Func<IBonboardingInteropCallbacks, ValueTask> action)
    {
        if (_disposed || !_callbacks.TryGetValue(sessionId, out var callbacks)) return;

        try
        {
            await action(callbacks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // A throw here would surface in the browser console as an unhandled interop failure and
            // tell the user nothing useful.
            _logger.LogError(ex, "An onboarding browser callback failed for session {SessionId}.", sessionId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        var module = _module;
        ReleaseLocalResources();

        if (module is not null)
        {
            try
            {
                await module.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
            {
                // The circuit is already gone; there is nothing left to clean up in the browser.
                _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not dispose its browser module.");
            }
        }
    }

    /// <summary>
    /// Synchronous teardown for hosts that dispose scopes without awaiting. The JS module handle is
    /// dropped rather than released, which is safe: releasing it needs a round trip that a
    /// synchronous disposal cannot make, and the page is on its way out regardless.
    /// </summary>
    public void Dispose() => ReleaseLocalResources();

    private void ReleaseLocalResources()
    {
        if (_disposed) return;
        _disposed = true;

        _callbacks.Clear();
        _activations.Clear();
        _module = null;
        _self?.Dispose();
        _self = null;
        _importGate.Dispose();
    }
}
