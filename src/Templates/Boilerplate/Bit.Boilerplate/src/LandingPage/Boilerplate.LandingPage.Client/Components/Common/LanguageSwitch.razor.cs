namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class LanguageSwitch
{
    [Parameter] public string? Class { get; set; }

    private string Localized(CultureInfo culture)
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        var (_, path) = LandingCultures.Split(uri.AbsolutePath);

        return $"{LandingCultures.Localize(path, culture)}{uri.Query}";
    }
}
