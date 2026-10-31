namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchPageSwitch(string Key, string NameKey, WorkbenchPageSwitchOption[] Options)
{
    public WorkbenchPageSwitchOption Default => Options[0];

    public WorkbenchPageSwitchOption Resolve(WorkbenchScenario scenario) => Options.FirstOrDefault(o => o.Value == scenario.GetSwitch(Key)) ?? Default;
}
