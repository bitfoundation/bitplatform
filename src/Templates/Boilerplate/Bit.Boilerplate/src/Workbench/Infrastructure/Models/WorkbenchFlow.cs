namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchFlow(string Id, string Name, WorkbenchFlowScreen[] Screens, WorkbenchFlowLink[] Links, DateTimeOffset CreatedAt);
