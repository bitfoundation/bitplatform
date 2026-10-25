namespace Bit.Bonboarding;

/// <summary>
/// A fully resolved, non-nullable snapshot of the options that apply to one step, produced by
/// merging step, tour and global settings. Templates and custom UIs read this rather than walking
/// the three levels themselves.
/// </summary>
public sealed class BonboardingEffectiveStepOptions
{
    internal BonboardingEffectiveStepOptions() { }

    public BonboardingPlacement Placement { get; internal init; } = BonboardingDefaults.Placement;
    public IReadOnlyList<BonboardingPlacement>? PlacementFallbacks { get; internal init; }
    public double Offset { get; internal init; } = BonboardingDefaults.Offset;
    public double ViewportPadding { get; internal init; } = BonboardingDefaults.ViewportPadding;
    public double ArrowSize { get; internal init; } = BonboardingDefaults.ArrowSize;

    public bool ShowSpotlight { get; internal init; } = BonboardingDefaults.ShowSpotlight;
    public double SpotlightPadding { get; internal init; } = BonboardingDefaults.SpotlightPadding;
    public double SpotlightRadius { get; internal init; } = BonboardingDefaults.SpotlightRadius;
    public BonboardingSpotlightShape SpotlightShape { get; internal init; } = BonboardingDefaults.SpotlightShape;

    public bool ShowOverlay { get; internal init; } = BonboardingDefaults.ShowOverlay;
    public double OverlayOpacity { get; internal init; } = BonboardingDefaults.OverlayOpacity;
    public BonboardingInteractionMode Interaction { get; internal init; } = BonboardingDefaults.Interaction;
    public bool CloseOnOverlayClick { get; internal init; } = BonboardingDefaults.CloseOnOverlayClick;
    public bool CloseOnEscape { get; internal init; } = BonboardingDefaults.CloseOnEscape;

    public BonboardingScrollMode Scroll { get; internal init; } = BonboardingDefaults.Scroll;
    public double ScrollPadding { get; internal init; } = BonboardingDefaults.ScrollPadding;

    public BonboardingMissingTargetBehavior MissingTarget { get; internal init; } = BonboardingDefaults.MissingTarget;
    public TimeSpan WaitTimeout { get; internal init; } = BonboardingDefaults.WaitTimeout;
    public BonboardingMissingTargetBehavior WaitTimeoutBehavior { get; internal init; } = BonboardingDefaults.WaitTimeoutBehavior;

    public BonboardingProgressStyle Progress { get; internal init; } = BonboardingDefaults.Progress;
    public bool ShowNext { get; internal init; } = BonboardingDefaults.ShowNext;
    public bool ShowPrevious { get; internal init; } = BonboardingDefaults.ShowPrevious;
    public bool ShowSkip { get; internal init; } = BonboardingDefaults.ShowSkip;
    public bool ShowClose { get; internal init; } = BonboardingDefaults.ShowClose;

    /// <summary>Already zero when reduced motion is requested and honoured.</summary>
    public TimeSpan AnimationDuration { get; internal init; } = BonboardingDefaults.AnimationDuration;

    public bool RespectReducedMotion { get; internal init; } = BonboardingDefaults.RespectReducedMotion;

    /// <summary>True when the user asked for reduced motion and the tour honours it.</summary>
    public bool ReducedMotion { get; internal init; }

    public BonboardingTheme Theme { get; internal init; } = BonboardingDefaults.Theme;
    public BonboardingDirection Direction { get; internal init; } = BonboardingDefaults.Direction;
    public int ZIndex { get; internal init; } = BonboardingDefaults.ZIndex;
    public string? PopoverClass { get; internal init; }

    public bool KeyboardNavigation { get; internal init; } = BonboardingDefaults.KeyboardNavigation;
    public bool TrapFocus { get; internal init; } = BonboardingDefaults.TrapFocus;
    public bool RestoreFocus { get; internal init; } = BonboardingDefaults.RestoreFocus;
    public bool AutoFocus { get; internal init; } = BonboardingDefaults.AutoFocus;

    /// <summary>The label set in force, after tour-level overrides and localization.</summary>
    public BonboardingLabels Labels { get; internal init; } = BonboardingLabels.Default;

    /// <summary>True when the layout should be mirrored.</summary>
    public bool RightToLeft => Direction == BonboardingDirection.RightToLeft;
}
