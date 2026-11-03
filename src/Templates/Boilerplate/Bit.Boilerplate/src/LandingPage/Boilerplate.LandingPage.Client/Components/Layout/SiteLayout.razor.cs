namespace Boilerplate.LandingPage.Client.Components.Layout;

public partial class SiteLayout
{
    [AutoInject] private IStringLocalizer<LandingStrings> Localizer = default!;
    [AutoInject] private LandingConsentPrompt consentPrompt = default!;
}
