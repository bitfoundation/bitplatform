namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Data;

/// <summary>
/// The stand-in for the app's <see cref="IAdsService"/>, which the settings page's upgrade section shows a rewarded ad with:
/// it asks for the advertising consent first, as the app's AdsService does, the ad is then ready as the view's data state
/// says, and watching it is a short wait that always earns the reward, so the section works in a view and a screenshot
/// without an ad network.
/// </summary>
public sealed class WorkbenchAdsService(WorkbenchScenarioState scenarioState, ConsentService consentService) : IAdsService
{
    public async Task<AdInitResult> Init(string adUnitPath)
    {
        if (await consentService.IsGranted(ConsentCategory.Advertising) is false)
            return AdInitResult.ConsentRequired;

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
        // As the app's: a consent withdrawn since the ad was ready doesn't let it show.
        if (await consentService.IsGranted(ConsentCategory.Advertising) is false)
            return AdWatchResult.Failed;

        await Task.Delay(TimeSpan.FromSeconds(1));

        return AdWatchResult.Rewarded;
    }
}
