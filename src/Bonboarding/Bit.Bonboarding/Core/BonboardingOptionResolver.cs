namespace Bit.Bonboarding;

/// <summary>Merges step, tour and global options into an <see cref="BonboardingEffectiveStepOptions"/>.</summary>
internal static class BonboardingOptionResolver
{
    public static BonboardingEffectiveStepOptions Resolve(
        BonboardingStepDefinition? step,
        BonboardingTourDefinition tour,
        BonboardingOptions global,
        BonboardingLabels labels,
        bool reducedMotion,
        BonboardingDirection? documentDirection)
    {
        var respectReducedMotion = Pick(step?.RespectReducedMotion, tour.RespectReducedMotion, global.RespectReducedMotion, BonboardingDefaults.RespectReducedMotion);
        var effectiveReducedMotion = respectReducedMotion && reducedMotion;
        var duration = Pick(step?.AnimationDuration, tour.AnimationDuration, global.AnimationDuration, BonboardingDefaults.AnimationDuration);

        var direction = Pick(step?.Direction, tour.Direction, global.Direction, BonboardingDefaults.Direction);
        if (direction == BonboardingDirection.Auto)
        {
            direction = documentDirection ?? BonboardingDirection.LeftToRight;
        }

        var interaction = Pick(step?.Interaction, tour.Interaction, global.Interaction, BonboardingDefaults.Interaction);

        // A step that waits for a real interaction has to let that interaction through.
        if (step?.AdvanceOn is not null && step.Interaction is null && tour.Interaction is null && global.Interaction is null)
        {
            interaction = BonboardingInteractionMode.TargetOnly;
        }

        return new BonboardingEffectiveStepOptions
        {
            Placement = Pick(step?.Placement, tour.Placement, global.Placement, BonboardingDefaults.Placement),
            PlacementFallbacks = step?.PlacementFallbacks ?? tour.PlacementFallbacks ?? global.PlacementFallbacks,
            Offset = Pick(step?.Offset, tour.Offset, global.Offset, BonboardingDefaults.Offset),
            ViewportPadding = Pick(step?.ViewportPadding, tour.ViewportPadding, global.ViewportPadding, BonboardingDefaults.ViewportPadding),
            ArrowSize = Pick(step?.ArrowSize, tour.ArrowSize, global.ArrowSize, BonboardingDefaults.ArrowSize),

            ShowSpotlight = Pick(step?.ShowSpotlight, tour.ShowSpotlight, global.ShowSpotlight, BonboardingDefaults.ShowSpotlight),
            SpotlightPadding = Pick(step?.SpotlightPadding, tour.SpotlightPadding, global.SpotlightPadding, BonboardingDefaults.SpotlightPadding),
            SpotlightRadius = Pick(step?.SpotlightRadius, tour.SpotlightRadius, global.SpotlightRadius, BonboardingDefaults.SpotlightRadius),
            SpotlightShape = Pick(step?.SpotlightShape, tour.SpotlightShape, global.SpotlightShape, BonboardingDefaults.SpotlightShape),

            ShowOverlay = Pick(step?.ShowOverlay, tour.ShowOverlay, global.ShowOverlay, BonboardingDefaults.ShowOverlay),
            OverlayOpacity = Pick(step?.OverlayOpacity, tour.OverlayOpacity, global.OverlayOpacity, BonboardingDefaults.OverlayOpacity),
            Interaction = interaction,
            CloseOnOverlayClick = Pick(step?.CloseOnOverlayClick, tour.CloseOnOverlayClick, global.CloseOnOverlayClick, BonboardingDefaults.CloseOnOverlayClick),
            CloseOnEscape = Pick(step?.CloseOnEscape, tour.CloseOnEscape, global.CloseOnEscape, BonboardingDefaults.CloseOnEscape),

            Scroll = Pick(step?.Scroll, tour.Scroll, global.Scroll, BonboardingDefaults.Scroll),
            ScrollPadding = Pick(step?.ScrollPadding, tour.ScrollPadding, global.ScrollPadding, BonboardingDefaults.ScrollPadding),

            MissingTarget = Pick(step?.MissingTarget, tour.MissingTarget, global.MissingTarget, BonboardingDefaults.MissingTarget),
            WaitTimeout = Pick(step?.WaitTimeout, tour.WaitTimeout, global.WaitTimeout, BonboardingDefaults.WaitTimeout),
            WaitTimeoutBehavior = Pick(step?.WaitTimeoutBehavior, tour.WaitTimeoutBehavior, global.WaitTimeoutBehavior, BonboardingDefaults.WaitTimeoutBehavior),

            Progress = Pick(step?.Progress, tour.Progress, global.Progress, BonboardingDefaults.Progress),
            ShowNext = Pick(step?.ShowNext, tour.ShowNext, global.ShowNext, BonboardingDefaults.ShowNext),
            ShowPrevious = Pick(step?.ShowPrevious, tour.ShowPrevious, global.ShowPrevious, BonboardingDefaults.ShowPrevious),
            ShowSkip = Pick(step?.ShowSkip, tour.ShowSkip, global.ShowSkip, BonboardingDefaults.ShowSkip),
            ShowClose = Pick(step?.ShowClose, tour.ShowClose, global.ShowClose, BonboardingDefaults.ShowClose),

            AnimationDuration = effectiveReducedMotion ? TimeSpan.Zero : duration,
            RespectReducedMotion = respectReducedMotion,
            ReducedMotion = effectiveReducedMotion,
            Theme = Pick(step?.Theme, tour.Theme, global.Theme, BonboardingDefaults.Theme),
            Direction = direction,
            ZIndex = Pick(step?.ZIndex, tour.ZIndex, global.ZIndex, BonboardingDefaults.ZIndex),
            PopoverClass = JoinClasses(global.PopoverClass, tour.PopoverClass, step?.PopoverClass),

            KeyboardNavigation = Pick(step?.KeyboardNavigation, tour.KeyboardNavigation, global.KeyboardNavigation, BonboardingDefaults.KeyboardNavigation),
            TrapFocus = Pick(step?.TrapFocus, tour.TrapFocus, global.TrapFocus, BonboardingDefaults.TrapFocus),
            RestoreFocus = Pick(step?.RestoreFocus, tour.RestoreFocus, global.RestoreFocus, BonboardingDefaults.RestoreFocus),
            AutoFocus = Pick(step?.AutoFocus, tour.AutoFocus, global.AutoFocus, BonboardingDefaults.AutoFocus),

            Labels = labels,
        };
    }

    private static T Pick<T>(T? step, T? tour, T? global, T fallback) where T : struct
        => step ?? tour ?? global ?? fallback;

    private static string? JoinClasses(params string?[] values)
    {
        var present = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
        return present.Length == 0 ? null : string.Join(' ', present);
    }
}
