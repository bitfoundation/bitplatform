namespace Boilerplate.Workbench.Components.Chrome;

/// <summary>
/// The store behind bit's accent switcher in a view: the view's accent setting comes first, and an accent picked on
/// the page is kept in the chrome store, so it survives reloads and reaches screenshots.
/// </summary>
public partial class ChromeAccentStore : IBitAccentColorStore
{
    [AutoInject] private ChromeStore chromeStore = default!;
    [AutoInject] private WorkbenchScenarioState scenarioState = default!;

    /// <summary>The view's accent setting when it has one, otherwise the accent last picked on the page.</summary>
    public Task<string?> GetAccentAsync() => Task.FromResult(scenarioState.Scenario.Accent ?? chromeStore.Accent);

    public Task SetAccentAsync(string accent)
    {
        // The view's own accent setting is not a pick, so it isn't stored.
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
