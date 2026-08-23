namespace Boilerplate.Client.Core.Components.Layout.Header;

public partial class IdentityHeader : AppComponentBase
{
    [CascadingParameter] public BitDir? CurrentDir { get; set; }

    [CascadingParameter] public AppThemeType? CurrentTheme { get; set; }


    [AutoInject] private ThemeService themeService = default!;
    [AutoInject] private CultureService cultureService = default!;


    private BitDropdownItem<string>[] cultures = default!;

    /// <summary>
    /// The accessible name of the icon-only theme button, which names the theme the click switches TO
    /// (the icon does the same - See the IconName expression in the razor).
    /// </summary>
    private string ThemeToggleLabel => CurrentTheme == AppThemeType.Light
        ? Localizer["Switch to the dark theme"]
        : Localizer["Switch to the light theme"];


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        if (CultureInfoManager.InvariantGlobalization is false)
        {
            cultures = CultureInfoManager.SupportedCultures
                        .Select(sc => new BitDropdownItem<string> { Value = sc.Culture.Name, Text = sc.DisplayName })
                        .ToArray();
        }
    }


    private async Task HandleGoHomeLink()
    {
        NavigationManager.NavigateTo(PageUrls.Home, replace: true);
    }

    private async Task ToggleTheme()
    {
        await themeService.ToggleTheme();
    }

    private async Task OnCultureChanged(string? cultureName)
    {
        await cultureService.ChangeCulture(cultureName);
    }
}
