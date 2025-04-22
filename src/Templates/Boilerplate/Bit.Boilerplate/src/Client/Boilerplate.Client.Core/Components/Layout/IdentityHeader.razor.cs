namespace Boilerplate.Client.Core.Components.Layout;

public partial class IdentityHeader : AppComponentBase
{
    private BitDropdownItem<string>[] cultures = default!;


    [AutoInject] private History history = default!;
    [AutoInject] private ThemeService themeService = default!;
    [AutoInject] private CultureService cultureService = default!;


    [CascadingParameter] private BitDir? currentDir { get; set; }
    [CascadingParameter(Name = Parameters.CurrentTheme)] private AppThemeType? currentTheme { get; set; }


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

    private async Task HandleBackLinkClick()
    {
        await history.GoBack();
    }

    private async Task ToggleTheme()
    {
        await themeService.ToggleTheme();
    }

    private async Task OnCultureChanged(string? cultureName)
    {
        await cultureService.ChangeCulture(cultureName);
    }

    private static BitCountry? FindBitCountry(string? cultureName)
    {
        var cultureInfo = CultureInfoManager.GetCultureInfo(cultureName);

        if (cultureInfo is null) return null;

        return BitCountries.All.FirstOrDefault(c => c.Iso2 == new RegionInfo(cultureInfo.LCID).TwoLetterISORegionName);
    }
}
