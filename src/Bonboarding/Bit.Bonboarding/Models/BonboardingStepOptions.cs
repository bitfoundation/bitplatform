namespace Bit.Bonboarding;

/// <summary>
/// Presentation and behaviour knobs shared by <see cref="BonboardingOptions"/> (global defaults),
/// <see cref="BonboardingTourDefinition"/> (per tour) and <see cref="BonboardingStepDefinition"/> (per step).
/// Every member is nullable: <see langword="null"/> means "inherit from the level above".
/// </summary>
public abstract class BonboardingStepOptions
{
    // ---- Positioning -------------------------------------------------------

    /// <summary>Preferred side of the target. Defaults to <see cref="BonboardingPlacement.Auto"/>.</summary>
    public BonboardingPlacement? Placement { get; set; }

    /// <summary>
    /// Ordered placements tried when the preferred one does not fit. When null the engine
    /// derives a sensible order (opposite side first, then the perpendicular sides).
    /// </summary>
    public IReadOnlyList<BonboardingPlacement>? PlacementFallbacks { get; set; }

    /// <summary>Gap in pixels between the spotlight edge and the popover. Default <c>12</c>.</summary>
    public double? Offset { get; set; }

    /// <summary>Minimum distance in pixels the popover keeps from the viewport edges. Default <c>16</c>.</summary>
    public double? ViewportPadding { get; set; }

    /// <summary>Length of the popover arrow's visible edge in pixels. <c>0</c> hides the arrow. Default <c>10</c>.</summary>
    public double? ArrowSize { get; set; }

    // ---- Spotlight ---------------------------------------------------------

    /// <summary>Whether to cut a spotlight hole in the overlay around the target. Default <see langword="true"/>.</summary>
    public bool? ShowSpotlight { get; set; }

    /// <summary>Extra pixels of breathing room around the target inside the spotlight. Default <c>8</c>.</summary>
    public double? SpotlightPadding { get; set; }

    /// <summary>Corner radius of the spotlight in pixels. Default <c>10</c>.</summary>
    public double? SpotlightRadius { get; set; }

    /// <summary>Shape of the spotlight. Default <see cref="BonboardingSpotlightShape.Rounded"/>.</summary>
    public BonboardingSpotlightShape? SpotlightShape { get; set; }

    // ---- Overlay and interaction ------------------------------------------

    /// <summary>Whether the dimming overlay is rendered at all. Default <see langword="true"/>.</summary>
    public bool? ShowOverlay { get; set; }

    /// <summary>Opacity of the dimming overlay, 0-1. Default <c>0.55</c>.</summary>
    public double? OverlayOpacity { get; set; }

    /// <summary>How much of the page stays interactive. Default <see cref="BonboardingInteractionMode.Blocked"/>.</summary>
    public BonboardingInteractionMode? Interaction { get; set; }

    /// <summary>Whether clicking the overlay ends the tour. Default <see langword="false"/>.</summary>
    public bool? CloseOnOverlayClick { get; set; }

    /// <summary>Whether <c>Escape</c> ends the tour. Default <see langword="true"/>.</summary>
    public bool? CloseOnEscape { get; set; }

    // ---- Scrolling ---------------------------------------------------------

    /// <summary>How the target is brought into view. Default <see cref="BonboardingScrollMode.Smooth"/>.</summary>
    public BonboardingScrollMode? Scroll { get; set; }

    /// <summary>Margin kept around the target when scrolling it into view. Default <c>24</c>.</summary>
    public double? ScrollPadding { get; set; }

    // ---- Missing targets ---------------------------------------------------

    /// <summary>What to do when the target cannot be found. Default <see cref="BonboardingMissingTargetBehavior.Wait"/>.</summary>
    public BonboardingMissingTargetBehavior? MissingTarget { get; set; }

    /// <summary>How long <see cref="BonboardingMissingTargetBehavior.Wait"/> waits. Default 10 seconds.</summary>
    public TimeSpan? WaitTimeout { get; set; }

    /// <summary>What happens when the wait expires. Default <see cref="BonboardingMissingTargetBehavior.Skip"/>.</summary>
    public BonboardingMissingTargetBehavior? WaitTimeoutBehavior { get; set; }

    // ---- Chrome ------------------------------------------------------------

    /// <summary>Progress indicator style. Default <see cref="BonboardingProgressStyle.Dots"/>.</summary>
    public BonboardingProgressStyle? Progress { get; set; }

    /// <summary>Show the "next"/"done" button. Default <see langword="true"/>.</summary>
    public bool? ShowNext { get; set; }

    /// <summary>Show the "back" button. Default <see langword="true"/>.</summary>
    public bool? ShowPrevious { get; set; }

    /// <summary>Show the "skip tour" button. Default <see langword="true"/>.</summary>
    public bool? ShowSkip { get; set; }

    /// <summary>Show the close (X) button. Default <see langword="true"/>.</summary>
    public bool? ShowClose { get; set; }

    // ---- Motion, theme, layering ------------------------------------------

    /// <summary>Duration of transitions. Default 220ms. Forced to zero under reduced motion.</summary>
    public TimeSpan? AnimationDuration { get; set; }

    /// <summary>Honour <c>prefers-reduced-motion</c>. Default <see langword="true"/>.</summary>
    public bool? RespectReducedMotion { get; set; }

    /// <summary>Colour scheme. Default <see cref="BonboardingTheme.System"/>.</summary>
    public BonboardingTheme? Theme { get; set; }

    /// <summary>Text direction. Default <see cref="BonboardingDirection.Auto"/> (inherits from the document).</summary>
    public BonboardingDirection? Direction { get; set; }

    /// <summary>Base z-index of the overlay. The popover sits one above. Default <c>9000</c>.</summary>
    public int? ZIndex { get; set; }

    /// <summary>Extra CSS class applied to the popover root.</summary>
    public string? PopoverClass { get; set; }

    // ---- Keyboard and focus -----------------------------------------------

    /// <summary>Arrow keys / Enter navigate between steps. Default <see langword="true"/>.</summary>
    public bool? KeyboardNavigation { get; set; }

    /// <summary>Keep <c>Tab</c> inside the popover (plus the target when it is interactive). Default <see langword="true"/>.</summary>
    public bool? TrapFocus { get; set; }

    /// <summary>Return focus to the element that was focused before the tour started. Default <see langword="true"/>.</summary>
    public bool? RestoreFocus { get; set; }

    /// <summary>Move focus into the popover when a step is shown. Default <see langword="true"/>.</summary>
    public bool? AutoFocus { get; set; }
}
