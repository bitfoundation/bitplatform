using System.Web;

namespace System;

/// <summary>  
/// An alternative to <see cref="HttpUtility.ParseQueryString(string)"/> that utilizes <see cref="Uri.EscapeDataString(string)"/> instead of <see cref="HttpUtility.UrlEncode(string?)"/>.  
/// </summary>
public class AppQueryStringCollection() : Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
{
    public override string? ToString()
    {
        if (Count == 0)
            return null;

        return string.Join("&", this.Select(kv => $"{Uri.EscapeDataString(Uri.UnescapeDataString(kv.Key))}={Uri.EscapeDataString(Uri.UnescapeDataString(kv.Value?.ToString() ?? ""))}"));
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
            string key = parts.ElementAt(0);
            string value = parts.ElementAtOrDefault(1) ?? string.Empty;
            qsCollection.Add(key, value);
        }

        return qsCollection;
    }
}
