namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>A case only one page has, like how many items it lists; its first option is the default.</summary>
public sealed record WorkbenchPageSwitch(string Key, string NameKey, WorkbenchPageSwitchOption[] Options)
{
    public WorkbenchPageSwitchOption Default => Options[0];

    /// <summary>The option the scenario picks, or the default one.</summary>
    public WorkbenchPageSwitchOption Resolve(WorkbenchScenario scenario) => Options.FirstOrDefault(o => o.Value == scenario.GetSwitch(Key)) ?? Default;
}
