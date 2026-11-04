namespace Boilerplate.LandingPage.Server.Features.Email.Templates;

public partial class WaitlistConfirmationEmail
{
    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;

    [Parameter] public string SiteName { get; set; } = default!;

    [Parameter] public string AccentColor { get; set; } = "#17171A";

    [Parameter] public Uri ConfirmUrl { get; set; } = default!;

    [Parameter] public Uri UnsubscribeUrl { get; set; } = default!;

    [Parameter] public int Position { get; set; }
}
