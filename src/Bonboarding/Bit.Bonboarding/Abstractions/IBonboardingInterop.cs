using Microsoft.AspNetCore.Components;

namespace Bit.Bonboarding;

/// <summary>
/// The only place the library talks to the browser. Substitute it in tests to run the whole engine
/// without a DOM.
/// </summary>
public interface IBonboardingInterop : IAsyncDisposable
{
    /// <summary>
    /// False while prerendering or after the circuit has gone away. Callers must degrade
    /// gracefully rather than throwing.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>Loads the browser module and samples the environment.</summary>
    ValueTask<BonboardingBrowserEnvironment> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>Registers a session so it can receive callbacks.</summary>
    ValueTask AttachAsync(string sessionId, IBonboardingInteropCallbacks callbacks, CancellationToken cancellationToken = default);

    /// <summary>Unregisters a session and tears down everything it owns in the browser.</summary>
    ValueTask DetachAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the target, scrolls it into view, installs observers, and returns the first
    /// measurement. Further measurements arrive through
    /// <see cref="IBonboardingInteropCallbacks.OnGeometryChangedAsync"/>.
    /// </summary>
    ValueTask<BonboardingStepGeometry> ActivateStepAsync(BonboardingStepActivation activation, CancellationToken cancellationToken = default);

    /// <summary>Removes the observers and listeners for the current step.</summary>
    ValueTask DeactivateStepAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Forces a fresh measurement.</summary>
    ValueTask<BonboardingStepGeometry> MeasureAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tells the browser layer which element is the popover, so it can be measured and, when
    /// requested, act as a focus trap. <paramref name="trapFocus"/> covers the gap before the first
    /// step is activated; from then on every <see cref="BonboardingStepActivation.TrapFocus"/>
    /// carries the setting of its own step.
    /// </summary>
    ValueTask SetPopoverAsync(string sessionId, ElementReference? popover, bool trapFocus, CancellationToken cancellationToken = default);

    /// <summary>Remembers the currently focused element so it can be restored when the tour ends.</summary>
    ValueTask CaptureFocusAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Restores the focus captured by <see cref="CaptureFocusAsync"/>.</summary>
    ValueTask RestoreFocusAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Moves focus to the first sensible control inside the popover.</summary>
    ValueTask FocusPopoverAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Reads a value from browser storage; used by the default persistence store.</summary>
    ValueTask<string?> GetStorageAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Writes a value to browser storage.</summary>
    ValueTask SetStorageAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>Removes a value from browser storage.</summary>
    ValueTask RemoveStorageAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Lists the keys in browser storage that start with a prefix.</summary>
    ValueTask<IReadOnlyList<string>> ListStorageKeysAsync(string prefix, CancellationToken cancellationToken = default);
}
