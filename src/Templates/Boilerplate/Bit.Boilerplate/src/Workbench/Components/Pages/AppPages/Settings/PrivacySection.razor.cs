//+:cnd:noEmit
using System.Text.Json;

namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings;

/// <summary>The Workbench copy of the app's privacy section: consents live in the store; the export downloads the profile.</summary>
public partial class PrivacySection
{
    private const string Analytics = nameof(Analytics);
    private const string Advertising = nameof(Advertising);

    private readonly string[] askableCategories =
    [
        //#if (appInsights == true)
        Analytics,
        //#endif
        //#if (ads == true)
        Advertising,
        //#endif
    ];

    [AutoInject] private SettingsStore store = default!;

    [CascadingParameter] public SettingsPage Page { get; set; } = default!;

    protected override void OnInitialized() => Subscribe(store);

    private string CategoryLabel(string category) => category switch
    {
        Analytics => AppLocalizer[nameof(AppStrings.ConsentAnalytics)],
        Advertising => AppLocalizer[nameof(AppStrings.ConsentAdvertising)],
        _ => category
    };

    private string CategoryDescription(string category) => category switch
    {
        Analytics => AppLocalizer[nameof(AppStrings.ConsentAnalyticsDescription)],
        Advertising => AppLocalizer[nameof(AppStrings.ConsentAdvertisingDescription)],
        _ => string.Empty
    };

    /// <summary>Downloads the profile as a JSON file, where the app downloads a zip from its server.</summary>
    private async Task ExportPersonalData()
    {
        try
        {
            await ScenarioState.Save(CurrentCancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var json = JsonSerializer.Serialize(Page.Profile, new JsonSerializerOptions { WriteIndented = true });

        await JSRuntime.InvokeVoidAsync("workbench.download", $"data:application/json;charset=utf-8,{Uri.EscapeDataString(json)}");
    }

    private void OpenAccount() => store.Expand(PageUrls.SettingsSections.Account);
}
