namespace Boilerplate.Workbench.Components.Chrome;

public partial class ChromeAccentStore : IBitAccentColorStore
{
    [AutoInject] private ChromeStore chromeStore = default!;
    [AutoInject] private WorkbenchScenarioState scenarioState = default!;

    public Task<string?> GetAccentAsync() => Task.FromResult(scenarioState.Scenario.Accent ?? chromeStore.Accent);

    public Task SetAccentAsync(string accent)
    {
        if (string.Equals(accent, scenarioState.Scenario.Accent, StringComparison.OrdinalIgnoreCase) is false)
        {
            chromeStore.PickAccent(accent);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAccentAsync()
    {
        chromeStore.PickAccent(null);

        return Task.CompletedTask;
    }
}
