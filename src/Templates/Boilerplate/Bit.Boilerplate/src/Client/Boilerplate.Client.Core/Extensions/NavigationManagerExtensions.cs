using System.Web;

namespace Microsoft.AspNetCore.Components;

public static partial class NavigationManagerExtensions
{
    public static string GetUriWithoutQueryParameter(this NavigationManager navigationManager, string key)
    {
        var url = navigationManager.Uri;

        var uri = new Uri(url);

        // this gets all the query string key value pairs as a collection
        var newQueryString = HttpUtility.ParseQueryString(uri.Query);

        // this removes the key if exists
        newQueryString.Remove(key);

        // this gets the page path from root without QueryString
        string pagePathWithoutQueryString = uri.GetLeftPart(UriPartial.Path);

        return newQueryString.Count > 0
            ? string.Format("{0}?{1}", pagePathWithoutQueryString, newQueryString)
            : pagePathWithoutQueryString;
    }

    private static readonly string[] allCultures = CultureInfoManager.MultilingualEnabled
        ? CultureInfo.GetCultures(CultureTypes.AllCultures).Select(c => c.Name).ToArray()
        : [];

    /// <summary>
    /// Reads culture from either route segment or query string.
    /// https://adminpanel.bitpaltform.dev/en-US/categories
    /// https://adminpanel.bitpaltform.dev/categories?culture=en-US
    /// </summary>
    public static string? GetCultureFromUri(this NavigationManager navigationManager)
    {
        var url = navigationManager.Uri;

        var uri = new Uri(url);

        var culture = HttpUtility.ParseQueryString(uri.Query)["culture"];

        if (string.IsNullOrEmpty(culture) is false)
            return culture;

        foreach (var segment in uri.Segments.Take(2))
        {
            if (allCultures.Contains(segment, StringComparer.InvariantCultureIgnoreCase))
            {
                return segment;
            }
        }

        return null;
    }

    public static string GetUriWithoutCulture(this NavigationManager navigationManager)
    {
        var uri = navigationManager.GetUriWithoutQueryParameter("culture");

        var culture = navigationManager.GetCultureFromUri();

        if (string.IsNullOrEmpty(culture) is false)
        {
            uri = uri
                .Replace($"{culture}/", string.Empty)
                .Replace(culture, string.Empty);
        }

        return uri;
    }
}
