namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>A screen of a flow: a page in a scenario, with the page state saved for it if any, at a spot on the board.</summary>
public sealed record WorkbenchFlowScreen(string Id, string PageKey, string Query, string? Note, bool HasState, double X, double Y)
{
    public WorkbenchScenario Scenario => WorkbenchScenario.Parse(Query);
}
