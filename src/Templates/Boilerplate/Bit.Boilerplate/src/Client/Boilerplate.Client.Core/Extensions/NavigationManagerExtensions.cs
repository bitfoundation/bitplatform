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
            ? String.Format("{0}?{1}", pagePathWithoutQueryString, newQueryString)
            : pagePathWithoutQueryString;
    }
}
