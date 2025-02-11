using System.Web;

namespace Boilerplate.Shared.Services;

public static class UriUtils
{
    /// <summary>
    /// Escapes the specified URL by encoding its query parameter values and route segments.
    /// </summary>
    /// <returns>
    /// A string representing the fully escaped URL, where each query parameter value and
    /// each segment of the path are URL-encoded appropriately.
    /// </returns>
    public static string Escape(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return string.Empty;

        if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri) is false)
            throw new ArgumentException("The provided URL is not valid.", nameof(url));

        if (uri.IsAbsoluteUri is false)
        {
            url = $"http://localhost/{url.TrimStart('/')}";
            return Escape(url);
        }

        UriBuilder builder = new UriBuilder(uri);

        // Process and escape the query string parameters
        var queryParams = HttpUtility.ParseQueryString(builder.Query);
        foreach (var key in queryParams.AllKeys)
        {
            var value = queryParams[key];
            if (string.IsNullOrEmpty(value) is false)
                value = Uri.EscapeDataString(value);
            queryParams.Set(key, value);
        }
        builder.Query = queryParams.ToString();

        // Process and escape each segment of the route (path)
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i] = Uri.UnescapeDataString(Uri.EscapeDataString(segments[i]));
        }
        builder.Path = "/" + string.Join("/", segments);

        return new Uri(builder.ToString()).PathAndQuery;
    }
}
