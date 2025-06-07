namespace Boilerplate.Client.Core.Components.Layout;

public partial class IdentityHeader : AppComponentBase
{
    private BitDropdownItem<string>[] cultures = default!;


    [AutoInject] private History history = default!;
    [AutoInject] private ThemeService themeService = default!;
    [AutoInject] private CultureService cultureService = default!;


    [CascadingParameter(Name = Parameters.CurrentDir)]
    public BitDir? CurrentDir { get; set; }

    [CascadingParameter(Name = Parameters.CurrentTheme)]
    public AppThemeType? CurrentTheme { get; set; }


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
        NavigationManager.NavigateTo(Urls.HomePage, replace: true);
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
