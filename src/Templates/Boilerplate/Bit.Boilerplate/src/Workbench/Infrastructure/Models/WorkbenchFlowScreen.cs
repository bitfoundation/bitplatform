namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchFlowScreen(string Id, string PageKey, string Query, string? Note, bool HasState, double X, double Y)
{
    public WorkbenchScenario Scenario => WorkbenchScenario.Parse(Query);
}
