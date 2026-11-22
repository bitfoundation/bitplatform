namespace Boilerplate.Client.Core.Components.Layout;

public partial class NavBar
{
    /// <summary>
    /// The page under the current culture, which an option links to so a search engine that follows it meets no redirect
    /// (See <see cref="CultureInfoManager.GetCurrentCultureUrl"/>).
    /// </summary>
    private static string UrlOf(string url) => CultureInfoManager.GetCurrentCultureUrl(url);

    /// <summary>
    /// Every address of a page: the culture-less one the app's own code navigates to, and the page under each culture,
    /// so the option is marked whichever the page was opened by.
    /// </summary>
    private static string[] AddressesOf(string url) => [url, .. CultureInfoManager.GetCultureUrls(url)];
}
