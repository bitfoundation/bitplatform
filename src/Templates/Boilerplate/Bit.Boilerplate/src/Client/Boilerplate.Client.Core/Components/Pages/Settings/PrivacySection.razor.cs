namespace Boilerplate.Client.Core.Components.Pages.Settings;

public partial class PrivacySection
{
    private Dictionary<ConsentCategory, bool> consents = [];

    [AutoInject] private HttpClient httpClient = default!;
    [AutoInject] private ConsentService consentService = default!;
    [AutoInject] private FileSaveService fileSaveService = default!;

    /// <summary>After the first render: the answers live in storage, which is unreachable while prerendering.</summary>
    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        await LoadConsents();

        StateHasChanged();
    }

    private async Task LoadConsents()
    {
        foreach (var category in ConsentService.AskableCategories)
        {
            consents[category] = await consentService.IsGranted(category);
        }
    }

    private async Task SetConsent(ConsentCategory category, bool granted)
    {
        await consentService.Set(category, granted);

        await LoadConsents();
    }

    private string CategoryLabel(ConsentCategory category) => category switch
    {
        ConsentCategory.Analytics => Localizer[nameof(AppStrings.ConsentAnalytics)],
        ConsentCategory.Advertising => Localizer[nameof(AppStrings.ConsentAdvertising)],
        _ => category.ToString()
    };

    private string CategoryDescription(ConsentCategory category) => category switch
    {
        ConsentCategory.Analytics => Localizer[nameof(AppStrings.ConsentAnalyticsDescription)],
        ConsentCategory.Advertising => Localizer[nameof(AppStrings.ConsentAdvertisingDescription)],
        _ => string.Empty
    };

    /// <summary>
    /// HttpClient rather than the generated proxy: the response is a zip and the proxy speaks json. The handler
    /// chain still attaches the access token.
    /// </summary>
    private async Task ExportPersonalData()
    {
        // Same gate as deleting the account: a copy of an entire identity is worth as much to a stolen session.
        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false)
            return;

        using var response = await httpClient.GetAsync(IUserController.ExportPersonalDataUri, CurrentCancellationToken);

        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "personal-data.zip";
        var content = await response.Content.ReadAsByteArrayAsync(CurrentCancellationToken);

        await fileSaveService.Save(fileName, "application/zip", content);
    }
}
