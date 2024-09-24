namespace Boilerplate.Client.Core.Components.Layout.Identity;

public partial class IdentityHeader : AppComponentBase
{
    private BitDropdownItem<string>[] cultures = default!;


    [AutoInject] private IThemeService themeService = default!;
    [AutoInject] private ICultureService cultureService = default!;


    [CascadingParameter(Name = Parameters.CurrentDir)] private BitDir? currentDir { get; set; }
    [CascadingParameter(Name = Parameters.CurrentTheme)] private AppThemeType? currentTheme { get; set; }


    protected override async Task OnInitAsync()
    {
        if (CultureInfoManager.MultilingualEnabled)
        {
            cultures = CultureInfoManager.SupportedCultures
                        .Select(sc => new BitDropdownItem<string> { Value = sc.Culture.Name, Text = sc.DisplayName })
                        .ToArray();
        }

        await base.OnInitAsync();
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
