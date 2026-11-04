namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class NotFoundPanel
{
    [Parameter] public string HomePath { get; set; } = LandingRoutes.Home;

    [Parameter] public string? Title { get; set; }

    [Parameter] public string? Body { get; set; }

    [Parameter] public RenderFragment? Art { get; set; }

    [Parameter] public RenderFragment? Actions { get; set; }

    [Parameter] public string? Class { get; set; }
}
