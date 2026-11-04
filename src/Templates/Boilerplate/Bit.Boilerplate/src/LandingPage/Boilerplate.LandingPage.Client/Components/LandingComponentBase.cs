namespace Boilerplate.LandingPage.Client.Components;

public partial class LandingComponentBase : ComponentBase
{
    [AutoInject] protected NavigationManager NavigationManager = default!;

    [AutoInject] protected IStringLocalizer<LandingStrings> Localizer = default!;

    protected static CultureInfo CurrentCulture => CultureInfo.CurrentUICulture;

    protected static bool IsRtl => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

    protected static string LocalUrl(string path) => LandingCultures.Localize(path, CultureInfo.CurrentUICulture);

    protected string AbsoluteUrl(string path) => new Uri(new Uri(NavigationManager.BaseUri), path.TrimStart('/')).ToString();
}
