namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>
/// The Surfaces category: accordions, cards, collapses, panes, splitters and tooltips in their states, and the dialog,
/// modal and panel that open from their buttons.
/// </summary>
public partial class SurfacesSection
{
    private bool isCollapseOpen;
    private bool isDialogOpen;
    private bool isModalOpen;
    private bool isPanelOpen;
    private (string Day, string Hours)[] openingHours = [];

    [AutoInject] private BitModalService modalService = default!;

    private string SampleText => Localizer[nameof(WorkbenchStrings.ThemeSampleText)];

    private string ProfileTitle => AppLocalizer[nameof(AppStrings.ProfileTitle)];

    protected override void OnInitialized()
    {
        // The week from Monday, in the view's language.
        var culture = CultureInfo.CurrentUICulture;
        var monday = new DateTime(2026, 5, 18);

        openingHours = [.. Enumerable.Range(0, 7).Select(day => (culture.DateTimeFormat.GetDayName(monday.AddDays(day).DayOfWeek), day < 5 ? "07:00 – 18:00" : day == 5 ? "08:00 – 16:00" : (string)Localizer[nameof(WorkbenchStrings.ThemeAiClosed)]))];
    }

    /// <summary>Opens a modal from code, the way the app's services do.</summary>
    private async Task ShowModalFromService()
    {
        BitModalReference? modalReference = null;

        Dictionary<string, object> parameters = new()
        {
            { nameof(ThemeModalContent.OnClose), (Action)(() => modalReference?.Close()) }
        };

        modalReference = await modalService.Show<ThemeModalContent>(parameters);
    }
}
