namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>A flow board: the screens of a task and the arrows between them.</summary>
public sealed record WorkbenchFlow(string Id, string Name, WorkbenchFlowScreen[] Screens, WorkbenchFlowLink[] Links, DateTimeOffset CreatedAt)
{
    /// <summary>One of the flows the Workbench comes with, which nobody can change.</summary>
    public bool IsSample => Id.StartsWith(WorkbenchSampleFlows.IdPrefix, StringComparison.Ordinal);
}
