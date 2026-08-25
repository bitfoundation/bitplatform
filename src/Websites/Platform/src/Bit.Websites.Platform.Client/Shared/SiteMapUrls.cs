namespace Bit.Websites.Platform.Client.Shared;

/// <summary>
/// Single source of truth for the URLs that must not be advertised in sitemap.xml.
/// PageOutlet derives its robots noindex meta from <see cref="NoIndexUrls"/> and the server's
/// sitemap endpoint excludes both lists, so the sitemap and the meta tags can never disagree.
/// </summary>
public static class SiteMapUrls
{
    /// <summary>
    /// Pages that search engines must not index at all.
    /// </summary>
    public static readonly string[] NoIndexUrls =
    [
        "/not-found",
        "/lowcode-nocode/overview", "/lowcode-nocode/benefits", "/lowcode-nocode/specs",
        "/lowcode-nocode/customizations", "/lowcode-nocode/comparison", "/lowcode-nocode/stats"
    ];

    /// <summary>
    /// Alias routes kept for backward compatibility. They stay indexable, but only the canonical
    /// twin their PageOutlet advertises belongs in the sitemap.
    /// </summary>
    public static readonly string[] NonCanonicalUrls =
    [
        "/demo", "/templates/overview", "/templates/development-prerequisites", "/besql/overview"
    ];
}
