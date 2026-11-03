namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>The Flows form as it stands: the flow picked, what is typed for the next screen and the flow a screen last went to.</summary>
public sealed class WorkbenchFlowDraft
{
    public string? TargetFlowId { get; set; }

    public string? NewFlowName { get; set; }

    public string? Note { get; set; }

    public string? LinkLabel { get; set; }

    public string? AddedFlowId { get; set; }
}
