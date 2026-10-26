using System.Globalization;

namespace Bit.Bonboarding;

/// <summary>
/// User-visible strings for the built-in UI. Replace individual values, swap the whole object
/// per tour via <see cref="BonboardingTourDefinition.Labels"/>, or register an
/// <see cref="IBonboardingLocalizer"/> to source them from resource files.
/// </summary>
public sealed class BonboardingLabels
{
    /// <summary>The built-in English strings.</summary>
    public static BonboardingLabels Default { get; } = new();

    public string Next { get; set; } = "Next";
    public string Previous { get; set; } = "Back";
    public string Skip { get; set; } = "Skip tour";
    public string Done { get; set; } = "Done";
    public string Close { get; set; } = "Close";

    /// <summary>Accessible name of the dialog when a step has no title.</summary>
    public string DialogLabel { get; set; } = "Product tour";

    /// <summary>
    /// Progress text. <c>{0}</c> is the 1-based position, <c>{1}</c> the total number of steps.
    /// </summary>
    public string ProgressFormat { get; set; } = DefaultProgressFormat;

    /// <summary>Screen-reader announcement made when a step appears. <c>{0}</c> position, <c>{1}</c> total, <c>{2}</c> title.</summary>
    public string StepAnnouncementFormat { get; set; } = DefaultStepAnnouncementFormat;

    /// <summary>Screen-reader announcement made while waiting for a target element.</summary>
    public string WaitingAnnouncement { get; set; } = "Waiting for the next part of the page.";

    /// <summary>Accessible label of the progress bar / dots group.</summary>
    public string ProgressLabel { get; set; } = "Tour progress";

    /// <summary>Accessible name of a <see cref="BonboardingBeacon"/> that has no title of its own.</summary>
    public string BeaconLabel { get; set; } = "Show tip";

    /// <summary>The button that closes a beacon's hint.</summary>
    public string HintDone { get; set; } = "Got it";

    /// <summary>Accessible name of a <see cref="BonboardingChecklist"/>'s progress. <c>{0}</c> done, <c>{1}</c> total.</summary>
    public string ChecklistProgressFormat { get; set; } = DefaultChecklistProgressFormat;

    /// <summary>The button that hides a <see cref="BonboardingChecklist"/>.</summary>
    public string ChecklistDismiss { get; set; } = "Dismiss checklist";

    /// <summary>What a screen reader hears after a checklist item that is done.</summary>
    public string ChecklistItemDone { get; set; } = "Done";

    /// <summary>What a screen reader hears after a checklist item still to do.</summary>
    public string ChecklistItemTodo { get; set; } = "Not done";

    /// <summary>State of a tour in a <see cref="BonboardingTourList"/> the user has never started.</summary>
    public string TourNew { get; set; } = "New";

    /// <summary>State of a tour in a <see cref="BonboardingTourList"/> the user stopped part way through.</summary>
    public string TourInProgress { get; set; } = "In progress";

    /// <summary>State of a tour in a <see cref="BonboardingTourList"/> the user finished.</summary>
    public string TourCompleted { get; set; } = "Completed";

    /// <summary>The button that starts a tour in a <see cref="BonboardingTourList"/>.</summary>
    public string TourStart { get; set; } = "Start";

    /// <summary>The button that continues a tour in a <see cref="BonboardingTourList"/>.</summary>
    public string TourResume { get; set; } = "Resume";

    /// <summary>The button that runs a finished tour again in a <see cref="BonboardingTourList"/>.</summary>
    public string TourReplay { get; set; } = "Replay";

    internal const string DefaultChecklistProgressFormat = "{0} of {1} done";

    internal const string DefaultProgressFormat = "{0} of {1}";

    internal const string DefaultStepAnnouncementFormat = "Step {0} of {1}. {2}";

    /// <summary>
    /// Formats one of the format strings above. A malformed one (a translation with a stray brace,
    /// say) falls back to the built-in English format instead of throwing while the popover renders.
    /// </summary>
    internal static string Format(string? format, string fallback, params object?[] args)
    {
        if (!string.IsNullOrEmpty(format))
        {
            try
            {
                return string.Format(CultureInfo.CurrentCulture, format, args);
            }
            catch (FormatException)
            {
            }
        }

        return string.Format(CultureInfo.CurrentCulture, fallback, args);
    }

    /// <summary>Creates a copy so callers can tweak a clone without mutating a shared instance.</summary>
    public BonboardingLabels Clone() => (BonboardingLabels)MemberwiseClone();
}
