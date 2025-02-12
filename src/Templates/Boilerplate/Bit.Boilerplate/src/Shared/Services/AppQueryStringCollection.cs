using System.Web;

namespace System;

/// <summary>  
/// An alternative to <see cref="HttpUtility.ParseQueryString(string)"/> that utilizes <see cref="Uri.EscapeDataString(string)"/> instead of <see cref="HttpUtility.UrlEncode(string?)"/>.  
/// </summary>
public class AppQueryStringCollection
{
    private readonly Dictionary<string, string> keyValues = [];

    public AppQueryStringCollection Add(string key, string? value)
    {
        keyValues[Uri.EscapeDataString(Uri.UnescapeDataString(key))] = Uri.EscapeDataString(Uri.UnescapeDataString(value ?? ""));

        return this;
    }

    public AppQueryStringCollection Add(AppQueryStringCollection queryStringCollection)
    {
        foreach (var kv in queryStringCollection.keyValues)
        {
            keyValues[kv.Key] = kv.Value;
        }

        return this;
    }

    public AppQueryStringCollection Remove(string key)
    {
        keyValues.Remove(Uri.EscapeDataString(Uri.UnescapeDataString(key)));

        return this;
    }

    public AppQueryStringCollection Clear()
    {
        keyValues.Clear();
        return this;
    }

    public bool IsEmpty => keyValues.Any() is false;

    public string? this[string key]
    {
        get
        {
            key = Uri.EscapeDataString(Uri.UnescapeDataString(key));
            if (keyValues.TryGetValue(key, out var value))
                return value;
            return null;
        }
        set => Add(key, value);
    }

    public override string ToString()
    {
        return string.Join("&", keyValues.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    public static AppQueryStringCollection Parse(string query)
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
            string key = parts[0];
            string value = parts.Length > 1 ? parts[1] : string.Empty;
            qsCollection.Add(key, value);
        }

        return qsCollection;
    }
}
