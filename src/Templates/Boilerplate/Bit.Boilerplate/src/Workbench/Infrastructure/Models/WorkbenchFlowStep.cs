namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchFlowStep(string Id, string PageKey, string Query, string? Note, bool HasState)
{
    public WorkbenchScenario Scenario => WorkbenchScenario.Parse(Query);
}
