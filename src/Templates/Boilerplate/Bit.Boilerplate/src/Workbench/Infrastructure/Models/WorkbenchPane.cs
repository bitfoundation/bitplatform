namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>A view in the shell: its id, which keys its saved page state, and its scenario.</summary>
public sealed record WorkbenchPane(string Id, WorkbenchScenario Scenario);
