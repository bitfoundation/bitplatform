namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings;

/// <summary>The Workbench copy of the app's upgrade section: the ad is a short wait, and having watched it is kept in the store.</summary>
public partial class UpgradeAccountSection
{
    private bool isFailed;
    private bool isLoading = true;
    private bool isWatching;

    [AutoInject] private SettingsStore store = default!;

    protected override async Task OnInitializedAsync()
    {
        Subscribe(store);

        if (IsPrerendering)
            return;

        await WhenStateRestored();

        try
        {
            await ScenarioState.Load("settings.ads", () => true, () => true, CurrentCancellationToken);
        }
        catch (WorkbenchSimulatedException)
        {
            isFailed = true;
        }
        catch (OperationCanceledException)
        {
            return;
        }

        isLoading = false;
    }

    private async Task WatchAd()
    {
        isWatching = true;

        try
        {
            await Task.Delay(1500, CurrentCancellationToken);

            store.WatchAd();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            isWatching = false;
        }
    }
}
