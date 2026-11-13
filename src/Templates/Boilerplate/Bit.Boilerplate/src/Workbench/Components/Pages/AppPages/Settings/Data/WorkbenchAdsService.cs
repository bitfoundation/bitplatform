namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Data;

/// <summary>
/// The stand-in for the app's <see cref="IAdsService"/>, which the settings page's upgrade section shows a rewarded ad with:
/// the ad is ready as the view's data state says, and watching it is a short wait that always earns the reward, so the
/// section works in a view and a screenshot without an ad network.
/// </summary>
public sealed class WorkbenchAdsService(WorkbenchScenarioState scenarioState) : IAdsService
{
    public async Task<AdInitResult> Init(string adUnitPath)
    {
        try
        {
            return await scenarioState.Load("settings.ads", () => AdInitResult.Ready, () => AdInitResult.NotAvailable);
        }
        catch (WorkbenchSimulatedException)
        {
            return AdInitResult.ScriptFailed;
        }
    }

    public async Task<AdWatchResult> Watch()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));

        return AdWatchResult.Rewarded;
    }
}
