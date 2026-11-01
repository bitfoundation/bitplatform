using Bit.Bonboarding;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>The words of the guides' buttons and announcements, and their steps' texts, which are resource keys.</summary>
public sealed class WorkbenchTourLocalizer(IStringLocalizer<WorkbenchStrings> localizer) : IBonboardingLocalizer
{
    public BonboardingLabels GetLabels(BonboardingTourDefinition? tour) => new()
    {
        Next = localizer[nameof(WorkbenchStrings.TourNext)],
        Previous = localizer[nameof(WorkbenchStrings.TourBack)],
        Skip = localizer[nameof(WorkbenchStrings.TourSkip)],
        Done = localizer[nameof(WorkbenchStrings.TourDone)],
        Close = localizer[nameof(WorkbenchStrings.Close)],
        DialogLabel = localizer[nameof(WorkbenchStrings.TourDialogLabel)],
        ProgressFormat = localizer[nameof(WorkbenchStrings.TourProgress)],
        StepAnnouncementFormat = localizer[nameof(WorkbenchStrings.TourStepAnnouncement)],
        WaitingAnnouncement = localizer[nameof(WorkbenchStrings.TourWaiting)],
        ProgressLabel = localizer[nameof(WorkbenchStrings.TourProgressLabel)],
        HintDone = localizer[nameof(WorkbenchStrings.GotIt)]
    };

    public string? Localize(string? text) => text is null ? null : localizer[text];
}
