﻿using System.Web;
using System.Text.RegularExpressions;

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

    /// <summary>
    /// Reads culture from either route segment or query string.
    /// https://adminpanel.bitpaltform.dev/en-US/categories
    /// https://adminpanel.bitpaltform.dev/categories?culture=en-US
    /// </summary>
    public static string? GetCultureFromUri(this NavigationManager navigationManager, Uri uri)
    {
        var culture = HttpUtility.ParseQueryString(uri.Query)["culture"];

        if (string.IsNullOrEmpty(culture) is false)
            return culture;

        var match = RouteDataRequestCulture().Match(uri.ToString());

        try
        {
            CultureInfoManager.CreateCultureInfo(match.Value);
            return match.Value;
        }
        catch { };

        return null;
    }

    [GeneratedRegex(@"([a-zA-Z]{2}-[a-zA-Z]{2})")]
    public static partial Regex RouteDataRequestCulture();

    public static string GetUriWithoutCulture(this NavigationManager navigationManager)
    {
        var uri = navigationManager.GetUriWithoutQueryParameter("culture");

        var culture = navigationManager.GetCultureFromUri(new Uri(uri));

        if (string.IsNullOrEmpty(culture) is false)
        {
            uri = uri
                .Replace($"{culture}/", string.Empty)
                .Replace(culture, string.Empty);
        }

        return uri;
    }
}
