namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>A flow board: the screens of a task and the arrows between them.</summary>
public sealed record WorkbenchFlow(string Id, string Name, WorkbenchFlowScreen[] Screens, WorkbenchFlowLink[] Links, DateTimeOffset CreatedAt);
