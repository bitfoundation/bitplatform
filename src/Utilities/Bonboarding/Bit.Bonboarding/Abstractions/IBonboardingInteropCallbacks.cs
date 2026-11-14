namespace Bit.Bonboarding;

/// <summary>Callbacks the browser layer raises against a session.</summary>
public interface IBonboardingInteropCallbacks
{
    /// <summary>A new measurement is available.</summary>
    ValueTask OnGeometryChangedAsync(BonboardingStepGeometry geometry);

    /// <summary>The target element left the DOM while the step was showing.</summary>
    ValueTask OnTargetLostAsync();

    /// <summary>
    /// The target element left the DOM, or stopped being shown, while the step was showing; <paramref name="issue"/>
    /// says which. Calls <see cref="OnTargetLostAsync()"/> unless implemented.
    /// </summary>
    ValueTask OnTargetLostAsync(BonboardingTargetIssue issue) => OnTargetLostAsync();

    /// <summary>A key was pressed while the tour had control of the keyboard.</summary>
    ValueTask OnKeyAsync(string key, bool shiftKey);

    /// <summary>The user clicked the dimmed area.</summary>
    ValueTask OnOverlayClickAsync();

    /// <summary>The step's <see cref="BonboardingAdvanceTrigger"/> fired.</summary>
    ValueTask OnAdvanceTriggeredAsync();

    /// <summary>Waiting for the target timed out.</summary>
    ValueTask OnWaitTimedOutAsync();

    /// <summary>
    /// Waiting for the target timed out; <paramref name="issue"/> says why it is still missing at that moment (it may
    /// have rendered hidden since the wait began). Calls <see cref="OnWaitTimedOutAsync()"/> unless implemented.
    /// </summary>
    ValueTask OnWaitTimedOutAsync(BonboardingTargetIssue issue) => OnWaitTimedOutAsync();
}
