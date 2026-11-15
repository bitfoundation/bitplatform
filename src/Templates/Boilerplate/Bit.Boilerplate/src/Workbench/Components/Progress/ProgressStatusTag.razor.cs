namespace Boilerplate.Workbench.Components.Progress;

/// <summary>Where a task stands, as a colored tag: it needs you, the agent works on it, or it is done, failed or stopped.</summary>
public partial class ProgressStatusTag
{
    [Parameter, EditorRequired] public WorkbenchProgressTask ProgressTask { get; set; } = default!;

    [Parameter] public BitSize Size { get; set; } = BitSize.Small;

    private bool NeedsYou => ProgressTask.NeedsYouCount > 0;

    private string Text => ProgressTask.Status switch
    {
        WorkbenchProgressStatus.Working when NeedsYou => Localizer[nameof(WorkbenchStrings.ProgressNeedsYou)],
        WorkbenchProgressStatus.Working => Localizer[nameof(WorkbenchStrings.ProgressWorking)],
        WorkbenchProgressStatus.Done => Localizer[nameof(WorkbenchStrings.ProgressDone)],
        WorkbenchProgressStatus.Failed => Localizer[nameof(WorkbenchStrings.ProgressFailed)],
        _ => Localizer[nameof(WorkbenchStrings.ProgressStopped)]
    };

    private string IconName => ProgressTask.Status switch
    {
        WorkbenchProgressStatus.Working when NeedsYou => BitIconName.Ringer,
        WorkbenchProgressStatus.Working => BitIconName.Running,
        WorkbenchProgressStatus.Done => BitIconName.CheckMark,
        WorkbenchProgressStatus.Failed => BitIconName.ErrorBadge,
        _ => BitIconName.Pause
    };

    private BitColor Color => ProgressTask.Status switch
    {
        WorkbenchProgressStatus.Working when NeedsYou => BitColor.Warning,
        WorkbenchProgressStatus.Working => BitColor.Primary,
        WorkbenchProgressStatus.Done => BitColor.Success,
        WorkbenchProgressStatus.Failed => BitColor.Error,
        _ => BitColor.SecondaryBackground
    };
}
