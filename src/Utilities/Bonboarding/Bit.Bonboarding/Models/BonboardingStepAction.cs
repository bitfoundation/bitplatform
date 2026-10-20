namespace Bit.Bonboarding;

/// <summary>
/// An extra button rendered in a step's footer. Combined with
/// <see cref="BonboardingStepActionEffect.GoTo"/> this is the simplest way to build a branching tour.
/// </summary>
public sealed class BonboardingStepAction
{
    /// <summary>Stable identifier, surfaced on the button as <c>data-bonboarding-action</c>.</summary>
    public string? Id { get; set; }

    /// <summary>Button text.</summary>
    public required string Label { get; set; }

    /// <summary>Visual weight. Default <see cref="BonboardingStepActionStyle.Secondary"/>.</summary>
    public BonboardingStepActionStyle Style { get; set; } = BonboardingStepActionStyle.Secondary;

    /// <summary>Optional handler, awaited before <see cref="Effect"/> is applied.</summary>
    public Func<BonboardingStepContext, ValueTask>? OnClick { get; set; }

    /// <summary>Navigation performed after <see cref="OnClick"/>. Default <see cref="BonboardingStepActionEffect.None"/>.</summary>
    public BonboardingStepActionEffect Effect { get; set; } = BonboardingStepActionEffect.None;

    /// <summary>Destination step id when <see cref="Effect"/> is <see cref="BonboardingStepActionEffect.GoTo"/>.</summary>
    public string? TargetStepId { get; set; }

    /// <summary>Disables the button when it returns <see langword="true"/>.</summary>
    public Func<BonboardingStepContext, bool>? IsDisabled { get; set; }

    /// <summary>Extra CSS classes on the button element.</summary>
    public string? CssClass { get; set; }

    /// <summary>Receive focus when the step is shown instead of the default primary button.</summary>
    public bool AutoFocus { get; set; }
}
