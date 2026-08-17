namespace Bit.Bswup.Demo.Client;

/// <summary>
/// The site's identity as search engines and social crawlers see it.
/// <para>
/// Everything here is deliberately absolute and hardcoded rather than derived from the request:
/// a canonical URL exists precisely to name ONE address for a page, so a preview deployment or a
/// bare-IP request has to point at production too, not at itself.
/// </para>
/// </summary>
public static class SiteMetadata
{
    /// <summary>Origin of the production deployment, without a trailing slash.</summary>
    public const string Origin = "https://bswup.bitplatform.dev";

    public const string SiteName = "bit Bswup";

    /// <summary>Appended to a page's catalog title to build the document title.</summary>
    public const string TitleSuffix = " - bit Bswup";

    /// <summary>Shown when a page is shared on a social network (og:image / twitter:image).</summary>
    public const string SocialImageUrl = $"{Origin}/icon-512.png";

    public const int SocialImageSize = 512;

    /// <summary>
    /// Routes that must not be indexed. Both ends read this list - PageOutlet emits the robots
    /// meta from it and the server leaves the same routes out of sitemap.xml - so the two can
    /// never disagree.
    /// </summary>
    public static readonly string[] NoIndexUrls = ["/error", "/not-found"];

    /// <summary>Absolute URL of a route ("" or "/" for the home page).</summary>
    public static string AbsoluteUrl(string url)
    {
        var trimmed = (url ?? string.Empty).Trim('/');

        return trimmed.Length == 0 ? $"{Origin}/" : $"{Origin}/{trimmed}";
    }
}
