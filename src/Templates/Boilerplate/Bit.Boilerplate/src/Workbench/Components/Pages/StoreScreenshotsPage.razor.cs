namespace Boilerplate.Workbench.Components.Pages;

public partial class StoreScreenshotsPage
{
    private const int MaxShotsPerDownload = 24;

    private static readonly StoreInfo[] stores =
    [
        new("App Store", BitIconName.CellPhone, AllowsFraming: true, FramedByDefault: true, nameof(WorkbenchStrings.StoreRuleAppStore)),
        new("Google Play", BitIconName.Play, AllowsFraming: true, FramedByDefault: false, nameof(WorkbenchStrings.StoreRuleGooglePlay)),
        new("Microsoft Store", BitIconName.TVMonitor, AllowsFraming: false, FramedByDefault: false, nameof(WorkbenchStrings.StoreRuleMicrosoftStore))
    ];

    private static readonly WorkbenchThemeMode[] themes = [WorkbenchThemeMode.Light, WorkbenchThemeMode.Dark];

    private static readonly Dictionary<string, string> noteKeys = new()
    {
        ["store-apple-iphone-69"] = nameof(WorkbenchStrings.StoreNoteIphoneRequired),
        ["store-apple-iphone-63"] = nameof(WorkbenchStrings.StoreNoteIphoneOptional),
        ["store-apple-ipad-13"] = nameof(WorkbenchStrings.StoreNoteIpad),
        ["store-apple-mac"] = nameof(WorkbenchStrings.StoreNoteMac),
        ["store-play-phone"] = nameof(WorkbenchStrings.StoreNotePlayPhone),
        ["store-play-tablet"] = nameof(WorkbenchStrings.StoreNotePlayTablet),
        ["store-microsoft"] = nameof(WorkbenchStrings.StoreNoteMicrosoft)
    };

    private StoreInfo selectedStore = stores[0];
    private bool framed = stores[0].FramedByDefault;
    private WorkbenchBackdrop selectedBackdrop = WorkbenchBackdrop.Brand;
    private WorkbenchThemeMode selectedTheme = WorkbenchThemeMode.Light;
    private string? selectedCulture = CultureInfoManager.DefaultCulture.Name;
    private HashSet<string> selectedPages = [];
    private readonly Dictionary<string, string> captions = [];
    private List<BitDropdownItem<string>> cultureItems = [];

    [SupplyParameterFromQuery(Name = "page")] public string? InitialPageKey { get; set; }

    private bool IsFramed => framed && selectedStore.AllowsFraming;

    private DevicePreset[] StoreDevices => [.. DevicePresets.All.Where(d => d.Store == selectedStore.Name)];

    private WorkbenchPageInfo[] SelectedPages => [.. WorkbenchPageCatalog.Pages.Where(p => selectedPages.Contains(p.Key))];

    private int ShotCount => StoreDevices.Length * selectedPages.Count;

    protected override void OnInitialized()
    {
        var initialPage = WorkbenchPageCatalog.Find(InitialPageKey);

        selectedPages = initialPage is null ? [.. WorkbenchPageCatalog.Pages.Select(p => p.Key)] : [initialPage.Key];
        cultureItems = [.. CultureInfoManager.SupportedCultures.Select(sc => new BitDropdownItem<string> { Value = sc.Culture.Name, Text = sc.DisplayName })];
    }

    private void SelectStore(StoreInfo store)
    {
        selectedStore = store;
        framed = store.FramedByDefault;
    }

    private void TogglePage(string pageKey, bool isSelected)
    {
        if (isSelected)
        {
            selectedPages.Add(pageKey);
        }
        else
        {
            selectedPages.Remove(pageKey);
        }
    }

    private void SetCaption(string pageKey, string? caption)
    {
        if (string.IsNullOrWhiteSpace(caption))
        {
            captions.Remove(pageKey);
        }
        else
        {
            captions[pageKey] = caption.Trim();
        }
    }

    private WorkbenchScenario GetScenario(DevicePreset device) => new()
    {
        Device = device.Key,
        Theme = selectedTheme,
        Culture = selectedCulture == CultureInfoManager.DefaultCulture.Name ? null : selectedCulture,
        Frame = false
    };

    private WorkbenchComposition? GetComposition(WorkbenchPageInfo page) => IsFramed ? new(selectedBackdrop, captions.GetValueOrDefault(page.Key)) : null;

    private static string GetNoteKey(DevicePreset device) => noteKeys.GetValueOrDefault(device.Key, nameof(WorkbenchStrings.StoreNoteOther));

    private string GetSizeCountText(StoreInfo store)
    {
        var count = DevicePresets.All.Count(d => d.Store == store.Name);

        return count is 1 ? Localizer[nameof(WorkbenchStrings.StoreSizeCountOne)] : Localizer[nameof(WorkbenchStrings.StoreSizeCount), count];
    }

    private async Task Download(WorkbenchPageInfo page, DevicePreset device)
    {
        await JSRuntime.InvokeVoidAsync("workbench.download", WorkbenchUrls.Screenshot(page.Key, GetScenario(device), GetComposition(page)));
    }

    private async Task DownloadAll()
    {
        var shots = SelectedPages.SelectMany(page => StoreDevices.Select(device => (page.Key, GetScenario(device), GetComposition(page))));

        await JSRuntime.InvokeVoidAsync("workbench.download", WorkbenchUrls.ScreenshotArchive(shots));
    }

    private sealed record StoreInfo(string Name, string IconName, bool AllowsFraming, bool FramedByDefault, string RuleKey);
}
