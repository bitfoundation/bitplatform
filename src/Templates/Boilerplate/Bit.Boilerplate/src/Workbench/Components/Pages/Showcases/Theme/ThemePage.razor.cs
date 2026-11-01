namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme;

/// <summary>
/// The Theme and components page: the theme's colors, type scale and corner radii, then every bit component the app
/// uses in each of its states. Scripts/states.ts draws hover, focus and pressed; the overlay switch opens one overlay.
/// </summary>
public partial class ThemePage
{
    /// <summary>The suffixes of each color family's tokens: main, hover, active, dark, light and text.</summary>
    private static readonly string[] variants = ["", "-hover", "-active", "-dark", "-light", "-text"];

    /// <summary>The color tokens the Colors section shows, by family.</summary>
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

    /// <summary>Every typography except Inherit, which has no look of its own.</summary>
    private static readonly BitTypography[] typographies = [.. Enum.GetValues<BitTypography>().Where(t => t is not BitTypography.Inherit)];

    private static readonly string[] radii = ["xs", "sm", "md", "lg", "xl", "2xl", "control", "surface", "popup", "dialog", "full"];

    private static readonly BitColor[] buttonColors = [BitColor.Primary, BitColor.Secondary, BitColor.Tertiary, BitColor.Info, BitColor.Success, BitColor.Warning, BitColor.SevereWarning, BitColor.Error];

    private static readonly BitColor[] messageColors = [BitColor.Info, BitColor.Success, BitColor.Warning, BitColor.SevereWarning, BitColor.Error];

    /// <summary>The states shown for interactive components, with the state states.ts forces; null is the untouched default.</summary>
    private static readonly (string NameKey, string? Force)[] interactionStates =
    [
        (nameof(WorkbenchStrings.ThemeStateDefault), null),
        (nameof(WorkbenchStrings.ThemeStateHover), "hover"),
        (nameof(WorkbenchStrings.ThemeStateFocused), "focus"),
        (nameof(WorkbenchStrings.ThemeStatePressed), "active")
    ];

    /// <summary>Sample people, one per presence, for the BitPersona row.</summary>
    private static readonly (string NameKey, BitPersonaPresence Presence)[] people =
    [
        (nameof(WorkbenchStrings.DemoUserSarah), BitPersonaPresence.Online),
        (nameof(WorkbenchStrings.DemoUserDaan), BitPersonaPresence.Away),
        (nameof(WorkbenchStrings.DemoUserNiloofar), BitPersonaPresence.Dnd),
        (nameof(WorkbenchStrings.PosStaffEmma), BitPersonaPresence.Offline)
    ];

    private static readonly string[] multipleValues = ["1", "3"];

    private readonly DateTimeOffset? sampleDate = new DateTimeOffset(1990, 5, 17, 0, 0, 0, TimeSpan.Zero);

    // Each color token's computed value, read in the browser after the first render and when the view's theme or accent changes.
    private Dictionary<string, string> values = [];
    private List<BitDropdownItem<string>> options = [];
    private List<BitChoiceGroupItem<string>> choices = [];
    private List<BitMenuButtonItem> menuItems = [];
    private List<BitNavBarItem> navBarItems = [];
    private List<BitNavItem> navItems = [];
    // Which overlay is open; the overlay page switch opens one after the first render.
    private bool isDropdownOpen;
    private bool isDatePickerOpen;
    private bool isMenuOpen;
    private bool isDialogOpen;
    private bool isPanelOpen;
    private ElementReference overlaysSection;

    [AutoInject] private BitAccentColorService accentColorService = default!;

    // Texts the samples share.
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

        // A theme or accent the view switches to in place changes the token values.
        ScenarioState.Changed += OnLookChanged;
        accentColorService.AccentChanged += OnAccentChanged;
    }

    /// <summary>Forces the marked states after each render; the first also reads the tokens and opens the switch's overlay.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("workbench.forceStates");

            if (firstRender is false)
                return;

            values = await ReadTokens();

            var overlay = Scenario.GetSwitch("overlay");

            // These open in place, so their section is brought into view first.
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
        catch (Exception exception) when (exception is JSDisconnectedException or OperationCanceledException)
        {
            // The view went away while the browser was being asked.
        }
    }

    /// <summary>Each color token's computed value, read in the browser.</summary>
    private ValueTask<Dictionary<string, string>> ReadTokens() =>
        JSRuntime.InvokeAsync<Dictionary<string, string>>("workbench.readTokens", [families.SelectMany(family => family.Tokens).ToArray()]);

    private void OnLookChanged() => _ = InvokeAsync(RefreshTokens);

    private void OnAccentChanged(object? sender, EventArgs e) => OnLookChanged();

    /// <summary>Reads the token values again and shows them.</summary>
    private async Task RefreshTokens()
    {
        try
        {
            values = await ReadTokens();
            StateHasChanged();
        }
        catch (Exception exception) when (exception is JSDisconnectedException or OperationCanceledException)
        {
            // The view went away while the browser was being asked.
        }
    }

    /// <summary>Shows a success, a warning and an error snack bar.</summary>
    private void ShowSnackBars()
    {
        SnackBarService.Success(Localizer[nameof(WorkbenchStrings.ThemeSuccess)], Localizer[nameof(WorkbenchStrings.ThemeMessage), BitColor.Success]);
        SnackBarService.Warning(Localizer[nameof(WorkbenchStrings.ThemeWarning)], Localizer[nameof(WorkbenchStrings.ThemeMessage), BitColor.Warning]);
        SnackBarService.Error(Localizer[nameof(WorkbenchStrings.ThemeError)], Localizer[nameof(WorkbenchStrings.ThemeMessage), BitColor.Error]);
    }

    protected override void Dispose(bool disposing)
    {
        ScenarioState.Changed -= OnLookChanged;
        accentColorService.AccentChanged -= OnAccentChanged;

        base.Dispose(disposing);
    }
}
