using System.Web;

namespace System;

/// <summary>
/// An alternative to <see cref="HttpUtility.ParseQueryString(string)"/> that utilizes <see cref="Uri.EscapeDataString(string)"/> instead of <see cref="HttpUtility.UrlEncode(string?)"/>.
/// <br/>
/// Keys and values are held <b>decoded</b>: <see cref="Parse(string?)"/> unescapes and <see cref="ToString"/> escapes
/// exactly once. Add values raw - an OData filter such as <c>contains(name,'100%20off')</c> is escaped, never decoded.
/// </summary>
public class AppQueryStringCollection() : Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
{
    /// <returns>The encoded query string <b>without</b> a leading '?', or null when empty - so it can be handed
    /// straight to <c>QueryString.Create</c>, which requires the '?' that this method does not emit.</returns>
    public override string? ToString()
    {
        if (Count == 0)
            return null;

        return string.Join("&", this.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value?.ToString() ?? "")}"));
    }

    public static AppQueryStringCollection Parse(string? query)
    {
        var qsCollection = new AppQueryStringCollection();

        if (string.IsNullOrWhiteSpace(query))
            return qsCollection;

        // Remove leading '?' if present.
        query = query.TrimStart('?');

        // Split the query string by '&' to separate key/value pairs.
        string[] pairs = query.Split(['&'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            // Split the pair into key and value using '='.
            var parts = pair.Split(['='], 2);
            string key = Uri.UnescapeDataString(parts.ElementAt(0));
            string value = Uri.UnescapeDataString(parts.ElementAtOrDefault(1) ?? string.Empty);
            // Last one wins. A repeated key is legal in a url (?utm_source=a&utm_source=b) and Add would throw.
            qsCollection[key] = value;
        }

        return qsCollection;
    }
}
