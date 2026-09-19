namespace Boilerplate.Client.Core.Components.Pages.Settings;

public partial class SettingsPage
{
    [Parameter] public string? Section { get; set; }


    /// <summary>
    /// The expanded section. The router reuses this page instance when only the section changes
    /// (/settings -> /settings/profile), so it follows <see cref="Section"/> on every navigation that lands
    /// here - one to the section the URL already holds included - while the user stays free to expand another
    /// section in between, which no re-render undoes.
    /// </summary>
    private string? expandedSection;
    private string? lastSection;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        NavigationManager.LocationChanged += NavigationManager_LocationChanged;
    }

    protected override async Task OnParamsSetAsync()
    {
        await base.OnParamsSetAsync();

        if (string.Equals(Section, lastSection, StringComparison.OrdinalIgnoreCase)) return;

        lastSection = Section;
        Expand();
    }

    /// <summary>
    /// A navigation to the section the URL already holds - clicking the nav entry of the page one is on - leaves
    /// <see cref="Section"/> untouched, so <see cref="OnParamsSetAsync"/> has nothing to react to, and the
    /// accordion still has to follow the entry the user just clicked. Whichever of the two runs first, both end
    /// up expanding the section the URL now names.
    /// </summary>
    private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        Expand();
        StateHasChanged();
    }

    // The accordion keys (PageUrls.SettingsSections) are lowercase and matched case-sensitively.
    private void Expand() => expandedSection = Section?.ToLowerInvariant();

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            NavigationManager.LocationChanged -= NavigationManager_LocationChanged;
        }

        await base.DisposeAsync(disposing);
    }
}
