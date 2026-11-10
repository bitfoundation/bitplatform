namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>
/// The Overlays tab: a dropdown, a date picker, a menu and a callout that open in place, and the buttons that open the
/// page's dialog, modal, panel and snack bars. The one of the first four the overlay switch names opens once the tab is on
/// the screen, with the tab scrolled into view, as a page that moves closes them.
/// </summary>
public partial class OverlaysSection
{
    private readonly DateTimeOffset? sampleDate = new DateTimeOffset(1990, 5, 17, 0, 0, 0, TimeSpan.Zero);

    private List<BitDropdownItem<string>> options = [];
    private List<BitMenuButtonItem> menuItems = [];
    private ThemeSection? section;
    private bool isDropdownOpen;
    private bool isDatePickerOpen;
    private bool isMenuOpen;
    private bool isCalloutOpen;

    /// <summary>The overlay that opens in place once the tab is shown: dropdown, date, menu or callout; any other opens nothing here.</summary>
    [Parameter] public string? Overlay { get; set; }

    [Parameter] public EventCallback OnDialog { get; set; }

    [Parameter] public EventCallback OnModal { get; set; }

    [Parameter] public EventCallback OnPanel { get; set; }

    [Parameter] public EventCallback OnSnackBars { get; set; }

    private string ChoiceLabel => Localizer[nameof(WorkbenchStrings.ThemeChoice)];

    private string ChoicePlaceholder => Localizer[nameof(WorkbenchStrings.ThemeChoicePlaceholder)];

    private string BirthDateLabel => AppLocalizer[nameof(AppStrings.BirthDate)];

    protected override void OnInitialized()
    {
        options = [.. Enumerable.Range(1, 4).Select(index => new BitDropdownItem<string> { Value = index.ToString(CultureInfo.InvariantCulture), Text = Localizer[nameof(WorkbenchStrings.ThemeOption), index] })];

        menuItems =
        [
            new() { Key = "edit", Text = AppLocalizer[nameof(AppStrings.Edit)], IconName = BitIconName.Edit },
            new() { Key = "delete", Text = AppLocalizer[nameof(AppStrings.Delete)], IconName = BitIconName.Delete }
        ];

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false || Overlay is not ("dropdown" or "date" or "menu" or "callout"))
            return;

        // Tracked as a load of the page's, so a screenshot waits until the overlay is open.
        await ScenarioState.Track(OpenInPlace());
    }

    /// <summary>
    /// Brings the tab into view, waits until the page stops scrolling, as a scroll closes an open callout and the tab bar
    /// slides its tab into view on a narrow screen, then opens the overlay the switch names.
    /// </summary>
    private async Task<bool> OpenInPlace()
    {
        try
        {
            if (section is not null)
            {
                await section.Element.ScrollIntoView(new ScrollIntoViewOptions { Behavior = ScrollBehavior.Instant });
            }

            await JSRuntime.InvokeVoidAsync("workbench.whenStill");
        }
        catch (Exception exception) when (exception is JSDisconnectedException or OperationCanceledException)
        {
            // The view went away while the browser was being asked.
            return false;
        }

        isDropdownOpen = Overlay is "dropdown";
        isDatePickerOpen = Overlay is "date";
        isMenuOpen = Overlay is "menu";
        isCalloutOpen = Overlay is "callout";

        StateHasChanged();

        return true;
    }
}
