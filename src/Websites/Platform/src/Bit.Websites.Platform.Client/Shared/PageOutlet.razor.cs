 namespace Bit.Websites.Platform.Client.Shared;

public partial class PageOutlet
{
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? Description { get; set; }
    [Parameter] public string? Url { get; set; }
    [Parameter] public string ImageUrl { get; set; } = "https://bitplatform.dev/images/og-image.svg";
}
