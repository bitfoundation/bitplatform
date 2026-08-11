namespace Bit.Websites.Platform.Client.Shared;

public partial class PageOutlet
{
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? Description { get; set; }
    [Parameter] public string? Keywords { get; set; }
    [Parameter] public string? Url { get; set; }

    /// <summary>
    /// Explicit override; when unset, the value comes from <see cref="SiteMapUrls.NoIndexUrls"/>,
    /// keeping the robots meta and the sitemap exclusions in one place.
    /// </summary>
    [Parameter] public bool? NoIndex { get; set; }

    private bool IsNoIndex => NoIndex ?? SiteMapUrls.NoIndexUrls.Contains($"/{Url}");
    [Parameter] public string ImageUrl { get; set; } = "https://bitplatform.dev/images/og-image.webp";

    [Parameter] public RenderFragment? ChildContent { get; set; }
}
