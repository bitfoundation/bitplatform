namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>
/// The theme's motion: each duration and easing pair with where the components use it, played on a dot across a track,
/// then the spinners' loop and the two factors every animation is multiplied by.
/// </summary>
public partial class MotionSection
{
    /// <summary>Where the components use each pair, then the duration and easing a dot moves with.</summary>
    private (string Use, string Duration, string Easing)[] motions = [];

    /// <summary>Whether the dots are at the far end of their tracks.</summary>
    private bool isPlayed;

    protected override void OnInitialized()
    {
        motions =
        [
            (Localizer[nameof(WorkbenchStrings.ThemeMotionQuickFeedbackLikeAButton)], "--bit-mot-duration-short", "--bit-mot-easing"),
            (Localizer[nameof(WorkbenchStrings.ThemeMotionMostChangesLikeAToggle)], "--bit-mot-duration", "--bit-mot-easing"),
            (Localizer[nameof(WorkbenchStrings.ThemeMotionBiggerMovesLikeACalendar)], "--bit-mot-duration-long", "--bit-mot-easing"),
            (Localizer[nameof(WorkbenchStrings.ThemeMotionComingInStartsFastAnd)], "--bit-mot-duration", "--bit-mot-easing-decelerate"),
            (Localizer[nameof(WorkbenchStrings.ThemeMotionGoingOutStartsSlowAnd)], "--bit-mot-duration", "--bit-mot-easing-accelerate")
        ];

        base.OnInitialized();
    }
}
