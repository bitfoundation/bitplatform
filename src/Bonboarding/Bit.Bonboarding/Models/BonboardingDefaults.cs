namespace Bit.Bonboarding;

/// <summary>
/// The terminal fallback values for every inheritable option. Resolution order is
/// step, then tour, then global <see cref="BonboardingOptions"/>, then these constants.
/// </summary>
public static class BonboardingDefaults
{
    public const double Offset = 12;
    public const double ViewportPadding = 16;
    public const double ArrowSize = 10;

    /// <summary>
    /// Corner radius assumed for the popover when the browser has not measured it, matching the
    /// default stylesheet. Only used to keep the arrow off the rounded corners.
    /// </summary>
    public const double PopoverRadius = 14;

    public const bool ShowSpotlight = true;
    public const double SpotlightPadding = 8;
    public const double SpotlightRadius = 10;
    public const BonboardingSpotlightShape SpotlightShape = BonboardingSpotlightShape.Rounded;

    public const bool ShowOverlay = true;
    public const double OverlayOpacity = 0.55;
    public const BonboardingInteractionMode Interaction = BonboardingInteractionMode.Blocked;
    public const bool CloseOnOverlayClick = false;
    public const bool CloseOnEscape = true;

    public const BonboardingScrollMode Scroll = BonboardingScrollMode.Smooth;
    public const double ScrollPadding = 24;

    public const BonboardingMissingTargetBehavior MissingTarget = BonboardingMissingTargetBehavior.Wait;
    public const BonboardingMissingTargetBehavior WaitTimeoutBehavior = BonboardingMissingTargetBehavior.Skip;
    public static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

    public const BonboardingProgressStyle Progress = BonboardingProgressStyle.Dots;
    public const bool ShowNext = true;
    public const bool ShowPrevious = true;
    public const bool ShowSkip = true;
    public const bool ShowClose = true;

    public static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(220);
    public const bool RespectReducedMotion = true;
    public const BonboardingTheme Theme = BonboardingTheme.System;
    public const BonboardingDirection Direction = BonboardingDirection.Auto;
    public const int ZIndex = 9000;

    public const bool KeyboardNavigation = true;
    public const bool TrapFocus = true;
    public const bool RestoreFocus = true;
    public const bool AutoFocus = true;

    public const BonboardingPlacement Placement = BonboardingPlacement.Auto;
}
