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
    public string ProgressFormat { get; set; } = "{0} of {1}";

    /// <summary>Screen-reader announcement made when a step appears. <c>{0}</c> position, <c>{1}</c> total, <c>{2}</c> title.</summary>
    public string StepAnnouncementFormat { get; set; } = "Step {0} of {1}. {2}";

    /// <summary>Screen-reader announcement made while waiting for a target element.</summary>
    public string WaitingAnnouncement { get; set; } = "Waiting for the next part of the page.";

    /// <summary>Accessible label of the progress bar / dots group.</summary>
    public string ProgressLabel { get; set; } = "Tour progress";

    /// <summary>Creates a copy so callers can tweak a clone without mutating a shared instance.</summary>
    public BonboardingLabels Clone() => (BonboardingLabels)MemberwiseClone();
}
