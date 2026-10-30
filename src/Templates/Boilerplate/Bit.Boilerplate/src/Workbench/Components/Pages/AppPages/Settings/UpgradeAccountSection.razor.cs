namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings;

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
        finally
        {
            isWatching = false;
        }
    }
}
