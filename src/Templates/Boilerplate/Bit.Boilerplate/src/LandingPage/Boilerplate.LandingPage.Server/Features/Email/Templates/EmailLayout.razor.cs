namespace Boilerplate.LandingPage.Server.Features.Email.Templates;

public partial class EmailLayout
{
    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;

    [Parameter] public string SiteName { get; set; } = default!;

    [Parameter] public string AccentColor { get; set; } = "#17171A";

    [Parameter] public string? Preview { get; set; }

    [Parameter] public Uri UnsubscribeUrl { get; set; } = default!;

    [Parameter] public RenderFragment? ChildContent { get; set; }
}
