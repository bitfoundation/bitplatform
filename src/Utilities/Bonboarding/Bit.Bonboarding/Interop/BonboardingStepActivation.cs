namespace Bit.Bonboarding;

/// <summary>Everything the browser layer needs to make one step live.</summary>
public sealed record BonboardingStepActivation
{
    public required string SessionId { get; init; }

    /// <summary>
    /// Identifies this activation of the step. The default browser layer assigns it and echoes it
    /// back with every callback the step raises, so a callback still in flight from an earlier step
    /// can be recognised and dropped instead of being applied to the step that replaced it.
    /// </summary>
    public long Activation { get; init; }

    public required BonboardingTargetDescriptor Target { get; init; }

    /// <summary>Folded into the spotlight and into the rectangle used for positioning.</summary>
    public IReadOnlyList<BonboardingTargetDescriptor>? AdditionalTargets { get; init; }

    /// <summary>Keep polling the DOM through a mutation observer until the target appears.</summary>
    public bool WaitForTarget { get; init; }

    /// <summary>Milliseconds before <see cref="WaitForTarget"/> gives up. Zero waits forever.</summary>
    public int WaitTimeoutMs { get; init; }

    /// <summary>One of <c>smooth</c>, <c>instant</c>, <c>auto</c> or <c>none</c>.</summary>
    public required string Scroll { get; init; }

    public double ScrollPadding { get; init; }

    /// <summary>One of <c>blocked</c>, <c>target</c> or <c>free</c>.</summary>
    public required string Interaction { get; init; }

    public bool CloseOnEscape { get; init; }

    public bool KeyboardNavigation { get; init; }

    /// <summary>
    /// Keep <c>Tab</c> inside the popover (plus the target on a <c>target</c> step) while the tour
    /// owns the keyboard. Sent with every step, since a step may override the tour's setting.
    /// </summary>
    public bool TrapFocus { get; init; }

    public BonboardingAdvanceTriggerDescriptor? AdvanceOn { get; init; }
}
