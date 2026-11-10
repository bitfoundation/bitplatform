namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme;

/// <summary>
/// The Theme and components page: every token family of the theme with the value it has in the view, the overlays, then
/// every bit component in its states, grouped the way the bit BlazorUI demo groups them, one tab at a time.
/// Scripts/states.ts draws hover, focus and pressed; the Section switch picks the tab the page opens on, the Density
/// switch tries the library's density overlays, and the overlay switch opens one overlay.
/// </summary>
public partial class ThemePage
{
    /// <summary>The id of the page's root, inside the density overlay, where the token values are read.</summary>
    private const string ScopeId = "wb-theme";

    /// <summary>The tab the page opens on when the Section switch names none.</summary>
    private const string DefaultSection = "colors";

    // Each token's value in the view, read in the browser after the first render and when the view's theme or accent changes.
    private Dictionary<string, string> values = [];
    private BitTheme? densityTheme;
    private string? selectedSection;
    // The theme and accent the token values were read for.
    private (WorkbenchThemeMode Theme, string? Accent) readLook;
    // The overlays that open over any tab; the overlay page switch opens one after the first render.
    private bool isDialogOpen;
    private bool isModalOpen;
    private bool isPanelOpen;

    [AutoInject] private BitAccentColorService accentColorService = default!;

    private string SampleText => Localizer[nameof(WorkbenchStrings.ThemeSampleText)];

    private string NameLabel => Localizer[nameof(WorkbenchStrings.ThemeName)];

    private string NamePlaceholder => Localizer[nameof(WorkbenchStrings.ThemeNamePlaceholder)];

    /// <summary>The overlay the overlay switch opens, or null.</summary>
    private string? Overlay => Scenario.GetSwitch("overlay") is { } overlay and not "none" ? overlay : null;

    protected override void OnInitialized()
    {
        densityTheme = Scenario.GetSwitch("density") switch
        {
            "compact" => BitThemeDensityPresets.CreateCompactOverlay(),
            "spacious" => BitThemeDensityPresets.CreateSpaciousOverlay(),
            _ => null
        };

        // The overlays that open in place need their tab; the dialog, the modal, the panel and the snack bars open over any tab.
        selectedSection = Overlay is "dropdown" or "date" or "menu" or "callout" ? "overlays" : Scenario.GetSwitch("section") ?? DefaultSection;

        readLook = (Scenario.Theme, Scenario.Accent);

        // A theme or accent the view switches to in place changes the token values.
        ScenarioState.Changed += OnScenarioChanged;
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

            switch (Overlay)
            {
                case "dialog":
                    isDialogOpen = true;
                    break;
                case "modal":
                    isModalOpen = true;
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

    /// <summary>Each token's value in the view, read in the browser inside the page's root, where the density overlay applies.</summary>
    private ValueTask<Dictionary<string, string>> ReadTokens() =>
        JSRuntime.InvokeAsync<Dictionary<string, string>>("workbench.readTokenValues", ThemeTokens.Requests, ScopeId);

    /// <summary>Writes the tab picked into the view's address, so a reload, a link or a comment opens on it.</summary>
    private Task ReportSection() => ReportScenario(Scenario.WithSwitch("section", selectedSection is DefaultSection ? null : selectedSection));

    /// <summary>Reads the token values again when the view's theme or accent changed, not when only the tab did.</summary>
    private void OnScenarioChanged()
    {
        var look = (Scenario.Theme, Scenario.Accent);

        if (look == readLook)
            return;

        readLook = look;

        _ = InvokeAsync(RefreshTokens);
    }

    private void OnAccentChanged(object? sender, EventArgs e) => _ = InvokeAsync(RefreshTokens);

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
        ScenarioState.Changed -= OnScenarioChanged;
        accentColorService.AccentChanged -= OnAccentChanged;

        base.Dispose(disposing);
    }
}
