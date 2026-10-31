namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme;

public partial class ThemePage
{
    private static readonly string[] variants = ["", "-hover", "-active", "-dark", "-light", "-text"];

    private static readonly (string NameKey, string[] Tokens)[] families =
    [
        .. new (string NameKey, string Code)[]
        {
            (nameof(WorkbenchStrings.ThemePrimary), "pri"),
            (nameof(WorkbenchStrings.ThemeSecondary), "sec"),
            (nameof(WorkbenchStrings.ThemeTertiary), "ter"),
            (nameof(WorkbenchStrings.ThemeInfo), "inf"),
            (nameof(WorkbenchStrings.ThemeSuccess), "suc"),
            (nameof(WorkbenchStrings.ThemeWarning), "wrn"),
            (nameof(WorkbenchStrings.ThemeSevereWarning), "swr"),
            (nameof(WorkbenchStrings.ThemeError), "err")
        }.Select(family => (family.NameKey, variants.Select(variant => $"--bit-clr-{family.Code}{variant}").ToArray())),
        (nameof(WorkbenchStrings.ThemeForeground), ["--bit-clr-fg-pri", "--bit-clr-fg-sec", "--bit-clr-fg-ter", "--bit-clr-fg-dis"]),
        (nameof(WorkbenchStrings.ThemeBackground), ["--bit-clr-bg-pri", "--bit-clr-bg-sec", "--bit-clr-bg-ter", "--bit-clr-bg-dis", "--bit-clr-bg-overlay"]),
        (nameof(WorkbenchStrings.ThemeBorder), ["--bit-clr-brd-pri", "--bit-clr-brd-sec", "--bit-clr-brd-ter", "--bit-clr-brd-dis"])
    ];

    private static readonly BitTypography[] typographies = [.. Enum.GetValues<BitTypography>().Where(t => t is not BitTypography.Inherit)];

    private static readonly string[] radii = ["xs", "sm", "md", "lg", "xl", "2xl", "control", "surface", "popup", "dialog", "full"];

    private static readonly BitColor[] buttonColors = [BitColor.Primary, BitColor.Secondary, BitColor.Tertiary, BitColor.Info, BitColor.Success, BitColor.Warning, BitColor.SevereWarning, BitColor.Error];

    private static readonly BitColor[] messageColors = [BitColor.Info, BitColor.Success, BitColor.Warning, BitColor.SevereWarning, BitColor.Error];

    private static readonly (string NameKey, string? Force)[] interactionStates =
    [
        (nameof(WorkbenchStrings.ThemeStateDefault), null),
        (nameof(WorkbenchStrings.ThemeStateHover), "hover"),
        (nameof(WorkbenchStrings.ThemeStateFocused), "focus"),
        (nameof(WorkbenchStrings.ThemeStatePressed), "active")
    ];

    private static readonly (string NameKey, BitPersonaPresence Presence)[] people =
    [
        (nameof(WorkbenchStrings.DemoUserSarah), BitPersonaPresence.Online),
        (nameof(WorkbenchStrings.DemoUserDaan), BitPersonaPresence.Away),
        (nameof(WorkbenchStrings.DemoUserNiloofar), BitPersonaPresence.Dnd),
        (nameof(WorkbenchStrings.PosStaffEmma), BitPersonaPresence.Offline)
    ];

    private static readonly string[] multipleValues = ["1", "3"];

    private readonly DateTimeOffset? sampleDate = new DateTimeOffset(1990, 5, 17, 0, 0, 0, TimeSpan.Zero);

    private Dictionary<string, string> values = [];
    private List<BitDropdownItem<string>> options = [];
    private List<BitChoiceGroupItem<string>> choices = [];
    private List<BitMenuButtonItem> menuItems = [];
    private List<BitNavBarItem> navBarItems = [];
    private List<BitNavItem> navItems = [];
    private bool isDropdownOpen;
    private bool isDatePickerOpen;
    private bool isMenuOpen;
    private bool isDialogOpen;
    private bool isPanelOpen;
    private ElementReference overlaysSection;

    private string SampleText => Localizer[nameof(WorkbenchStrings.ThemeSampleText)];

    private string SampleName => Localizer[nameof(WorkbenchStrings.DemoUserSarah)];

    private string SaveText => AppLocalizer[nameof(AppStrings.Save)];

    private string FavoriteText => Localizer[nameof(WorkbenchStrings.ThemeFavorite)];

    private string ActionsText => Localizer[nameof(WorkbenchStrings.ThemeActions)];

    private string NameLabel => Localizer[nameof(WorkbenchStrings.ThemeName)];

    private string NamePlaceholder => Localizer[nameof(WorkbenchStrings.ThemeNamePlaceholder)];

    private string ErrorText => Localizer[nameof(WorkbenchStrings.ThemeNameError)];

    private string SearchText => Localizer[nameof(WorkbenchStrings.ThemeSearch)];

    private string QuantityLabel => Localizer[nameof(WorkbenchStrings.ThemeQuantity)];

    private string CodeLabel => Localizer[nameof(WorkbenchStrings.ThemeCode)];

    private string PhoneLabel => AppLocalizer[nameof(AppStrings.PhoneNumber)];

    private string PhonePlaceholder => AppLocalizer[nameof(AppStrings.PhoneNumberPlaceholder)];

    private string BirthDateLabel => AppLocalizer[nameof(AppStrings.BirthDate)];

    private string ChoiceLabel => Localizer[nameof(WorkbenchStrings.ThemeChoice)];

    private string ChoicePlaceholder => Localizer[nameof(WorkbenchStrings.ThemeChoicePlaceholder)];

    private string OptionLabel => Localizer[nameof(WorkbenchStrings.ThemeOption), 1];

    private string ToggleLabel => Localizer[nameof(WorkbenchStrings.ThemeToggle)];

    private string AmountLabel => Localizer[nameof(WorkbenchStrings.ThemeAmount)];

    protected override void OnInitialized()
    {
        options = [.. Enumerable.Range(1, 4).Select(index => new BitDropdownItem<string> { Value = index.ToString(CultureInfo.InvariantCulture), Text = Localizer[nameof(WorkbenchStrings.ThemeOption), index] })];
        choices = [.. Enumerable.Range(1, 3).Select(index => new BitChoiceGroupItem<string> { Value = index.ToString(CultureInfo.InvariantCulture), Text = Localizer[nameof(WorkbenchStrings.ThemeOption), index] })];

        menuItems =
        [
            new() { Key = "edit", Text = AppLocalizer[nameof(AppStrings.Edit)], IconName = BitIconName.Edit },
            new() { Key = "delete", Text = AppLocalizer[nameof(AppStrings.Delete)], IconName = BitIconName.Delete }
        ];

        navBarItems =
        [
            new() { Key = "home", Text = AppLocalizer[nameof(AppStrings.Home)], IconName = BitIconName.Home },
            new() { Key = "profile", Text = AppLocalizer[nameof(AppStrings.ProfileTitle)], IconName = BitIconName.Contact },
            new() { Key = "settings", Text = AppLocalizer[nameof(AppStrings.Settings)], IconName = BitIconName.Settings }
        ];

        navItems =
        [
            new() { Key = "home", Text = AppLocalizer[nameof(AppStrings.Home)], IconName = BitIconName.Home },
            new() { Key = "profile", Text = AppLocalizer[nameof(AppStrings.ProfileTitle)], IconName = BitIconName.Contact },
            new() { Key = "settings", Text = AppLocalizer[nameof(AppStrings.Settings)], IconName = BitIconName.Settings }
        ];
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JSRuntime.InvokeVoidAsync("workbench.forceStates");

        if (firstRender is false)
            return;

        values = await JSRuntime.InvokeAsync<Dictionary<string, string>>("workbench.readTokens", [families.SelectMany(family => family.Tokens).ToArray()]);

        var overlay = Scenario.GetSwitch("overlay");

        if (overlay is "dropdown" or "date" or "menu")
        {
            await overlaysSection.ScrollIntoView(true);
        }

        switch (overlay)
        {
            case "dropdown":
                isDropdownOpen = true;
                break;
            case "date":
                isDatePickerOpen = true;
                break;
            case "menu":
                isMenuOpen = true;
                break;
            case "dialog":
                isDialogOpen = true;
                break;
            case "panel":
                isPanelOpen = true;
                break;
            case "snackbar":
                ShowSnackBars();
                break;
        }

        StateHasChanged();
    }

    private void ShowSnackBars()
    {
        SnackBarService.Success(nameof(BitColor.Success), Localizer[nameof(WorkbenchStrings.ThemeMessage), BitColor.Success]);
        SnackBarService.Warning(nameof(BitColor.Warning), Localizer[nameof(WorkbenchStrings.ThemeMessage), BitColor.Warning]);
        SnackBarService.Error(nameof(BitColor.Error), Localizer[nameof(WorkbenchStrings.ThemeMessage), BitColor.Error]);
    }
}
