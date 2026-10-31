namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchFlow(string Id, string Name, WorkbenchFlowStep[] Steps, DateTimeOffset CreatedAt);
