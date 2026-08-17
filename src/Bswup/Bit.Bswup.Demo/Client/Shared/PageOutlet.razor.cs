using Microsoft.AspNetCore.Components;

namespace Bit.Bswup.Demo.Client.Shared;

public partial class PageOutlet
{
    /// <summary>
    /// The page's route without its leading slash ("" for the home page). Everything else is
    /// looked up from <see cref="DocsCatalog"/> by this slug, so a documentation page states its
    /// identity exactly once - in the catalog the MCP server already renders it from.
    /// </summary>
    [Parameter] public string Slug { get; set; } = string.Empty;

    /// <summary>Overrides the catalog title. Used verbatim - the site suffix is not appended.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Overrides the catalog description.</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// Explicit override; when unset it is taken from <see cref="SiteMetadata.NoIndexUrls"/>,
    /// which the sitemap endpoint excludes as well.
    /// </summary>
    [Parameter] public bool? NoIndex { get; set; }

    /// <summary>Extra head tags for this page, rendered after the ones above.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string _url = "/";
    private string _title = SiteMetadata.SiteName;
    private string _description = string.Empty;
    private string _canonicalUrl = SiteMetadata.Origin;
    private bool _noIndex;

    protected override void OnParametersSet()
    {
        var page = DocsCatalog.FindBySlug(Slug);

        _url = page?.Url ?? (Slug.Trim('/').Length == 0 ? "/" : $"/{Slug.Trim('/')}");
        _title = Title ?? (page is null ? SiteMetadata.SiteName : $"{page.Title}{SiteMetadata.TitleSuffix}");
        _description = Description ?? page?.Description ?? string.Empty;
        _canonicalUrl = SiteMetadata.AbsoluteUrl(_url);
        _noIndex = NoIndex ?? SiteMetadata.NoIndexUrls.Contains(_url);

        base.OnParametersSet();
    }
}
