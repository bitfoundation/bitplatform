using System.Web;

namespace System;

public static partial class UriExtensions
{
    public static string GetWithoutQueryParameter(this Uri uri, string key)
    {
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

    /// <summary>
    /// Reads culture from either route segment or query string.
    /// https://adminpanel.bitpaltform.dev/en-US/categories
    /// https://adminpanel.bitpaltform.dev/categories?culture=en-US
    /// </summary>
    public static string? GetCulture(this Uri uri)
    {
        var culture = HttpUtility.ParseQueryString(uri.Query)["culture"];

        if (string.IsNullOrEmpty(culture) is false)
            return culture;

        foreach (var segment in uri.Segments.Take(2))
        {
            var segmentValue = segment.Trim('/');
            if (CultureInfoManager.SupportedCultures.Any(sc => string.Equals(sc.Culture.Name, segmentValue, StringComparison.InvariantCultureIgnoreCase)))
            {
                return segmentValue;
            }
        }

        return null;
    }

    public static string GetWithoutCulture(this Uri uri)
    {
        uri = new Uri(uri.GetWithoutQueryParameter("culture"));

        var culture = uri.GetCulture();

        if (string.IsNullOrEmpty(culture) is false)
        {
            uri = new Uri(uri.ToString()
                .Replace($"{culture}/", string.Empty)
                .Replace(culture, string.Empty));
        }

        return uri.ToString();
    }

    public static string GetPath(this Uri uri)
    {
        var uriBuilder = new UriBuilder(uri.GetWithoutCulture()) { Query = string.Empty, Fragment = string.Empty };
        return uriBuilder.Path;
    }
}
