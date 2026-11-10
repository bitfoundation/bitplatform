namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>The Buttons category: every variant and color, then each button component in its states.</summary>
public partial class ButtonsSection
{
    private List<BitButtonGroupItem> periods = [];
    private List<BitButtonGroupItem> alignments = [];
    private List<BitMenuButtonItem> menuItems = [];

    private string SaveText => AppLocalizer[nameof(AppStrings.Save)];

    private string FavoriteText => Localizer[nameof(WorkbenchStrings.ThemeFavorite)];

    private string ActionsText => Localizer[nameof(WorkbenchStrings.ThemeActions)];

    protected override void OnInitialized()
    {
        periods =
        [
            new() { Key = "day", Text = Localizer[nameof(WorkbenchStrings.CalendarViewDay)] },
            new() { Key = "week", Text = Localizer[nameof(WorkbenchStrings.CalendarViewWeek)] },
            new() { Key = "month", Text = Localizer[nameof(WorkbenchStrings.CalendarViewMonth)] }
        ];

        alignments =
        [
            new() { Key = "start", IconName = BitIconName.AlignLeft, AriaLabel = Localizer[nameof(WorkbenchStrings.ThemeButtonsAlignStart)], Title = Localizer[nameof(WorkbenchStrings.ThemeButtonsAlignStart)] },
            new() { Key = "center", IconName = BitIconName.AlignCenter, AriaLabel = Localizer[nameof(WorkbenchStrings.ThemeButtonsAlignCenter)], Title = Localizer[nameof(WorkbenchStrings.ThemeButtonsAlignCenter)] },
            new() { Key = "end", IconName = BitIconName.AlignRight, AriaLabel = Localizer[nameof(WorkbenchStrings.ThemeButtonsAlignEnd)], Title = Localizer[nameof(WorkbenchStrings.ThemeButtonsAlignEnd)] }
        ];

        menuItems =
        [
            new() { Key = "edit", Text = AppLocalizer[nameof(AppStrings.Edit)], IconName = BitIconName.Edit },
            new() { Key = "share", Text = Localizer[nameof(WorkbenchStrings.ThemeButtonsShare)], IconName = BitIconName.Share },
            new() { Key = "delete", Text = AppLocalizer[nameof(AppStrings.Delete)], IconName = BitIconName.Delete }
        ];
    }
}
