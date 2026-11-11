namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>The Pickers category: the time, color, date and date range pickers, closed in their states and open inline.</summary>
public partial class PickersSection
{
    /// <summary>The colors the color picker offers in one tap; a picker's sample values are data, not the theme's.</summary>
    private static readonly string[] presets = ["#1276c6", "#6750a4", "#107c10", "#ca5010", "#038387", "#c239b3"];

    private readonly TimeSpan? sampleTime = new TimeSpan(9, 30, 0);
    private readonly TimeSpan? lateTime = new TimeSpan(19, 15, 0);
    private readonly DateTimeOffset? birthDate = new DateTimeOffset(1990, 5, 17, 0, 0, 0, TimeSpan.Zero);
    private readonly BitDateRangePickerValue stay = new() { StartDate = ThemeSamples.Today.AddDays(4), EndDate = ThemeSamples.Today.AddDays(9) };
    private readonly BitDateRangePickerValue oneNight = new() { StartDate = ThemeSamples.Today.AddDays(4), EndDate = ThemeSamples.Today.AddDays(5) };

    private string StartLabel => Localizer[nameof(WorkbenchStrings.ThemePickersOpensAt)];

    private string TimePlaceholder => Localizer[nameof(WorkbenchStrings.ThemePickersPickATime)];

    private string StayLabel => Localizer[nameof(WorkbenchStrings.ThemePickersStay)];

    private string BirthDateLabel => AppLocalizer[nameof(AppStrings.BirthDate)];
}
