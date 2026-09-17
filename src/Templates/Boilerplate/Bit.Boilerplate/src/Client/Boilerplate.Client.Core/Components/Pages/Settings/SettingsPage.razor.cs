namespace Boilerplate.Client.Core.Components.Pages.Settings;

public partial class SettingsPage
{
    [Parameter] public string? Section { get; set; }


    /// <summary>
    /// The expanded section. The router reuses this page instance when only the section changes
    /// (/settings -> /settings/profile), so it follows <see cref="Section"/> whenever that changes,
    /// while the user stays free to expand another section in between.
    /// </summary>
    private string? expandedSection;
    private string? lastSection;

    protected override async Task OnParamsSetAsync()
    {
        await base.OnParamsSetAsync();

        if (string.Equals(Section, lastSection, StringComparison.OrdinalIgnoreCase)) return;

        lastSection = Section;
        expandedSection = Section;
    }
}
