namespace Bit.Bonboarding;

/// <summary>Callbacks the browser layer raises against a session.</summary>
public interface IBonboardingInteropCallbacks
{
    /// <summary>A new measurement is available.</summary>
    ValueTask OnGeometryChangedAsync(BonboardingStepGeometry geometry);

    /// <summary>The target element left the DOM while the step was showing.</summary>
    ValueTask OnTargetLostAsync();

    /// <summary>A key was pressed while the tour had control of the keyboard.</summary>
    ValueTask OnKeyAsync(string key, bool shiftKey);

    /// <summary>The user clicked the dimmed area.</summary>
    ValueTask OnOverlayClickAsync();

    /// <summary>The step's <see cref="BonboardingAdvanceTrigger"/> fired.</summary>
    ValueTask OnAdvanceTriggeredAsync();

    /// <summary>Waiting for the target timed out.</summary>
    ValueTask OnWaitTimedOutAsync();
}
