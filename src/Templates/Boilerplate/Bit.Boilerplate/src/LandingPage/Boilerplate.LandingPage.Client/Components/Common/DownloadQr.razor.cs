namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class DownloadQr
{
    [Parameter, EditorRequired] public LandingSite Site { get; set; } = default!;

    [Parameter] public string? Class { get; set; }
}
