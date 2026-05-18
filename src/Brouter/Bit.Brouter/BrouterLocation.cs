using System.Collections.ObjectModel;

namespace Bit.Brouter;

/// <summary>
/// A parsed, immutable representation of a URL.
/// Inspired by <c>useLocation</c> in React Router and <c>$route</c> in Vue Router.
/// </summary>
public sealed class BrouterLocation
{
    /// <summary>An empty/root location.</summary>
    public static readonly BrouterLocation Empty = new("", "/", [], "", "");

    private readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>> _queryParams;

    internal BrouterLocation(string fullUri, string path, string[] segments, string query, string hash)
    {
        FullUri = fullUri;
        Path = path;
        Segments = segments;
        Query = query;
        Hash = hash;
        _queryParams = new Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>>(() => ParseQuery(query));
    }

    /// <summary>The absolute URI of the location.</summary>
    public string FullUri { get; }

    /// <summary>The path part starting with '/'. Does not include query or hash.</summary>
    public string Path { get; }

    /// <summary>The path split by '/' with empty segments removed and segments URI-decoded.</summary>
    public string[] Segments { get; }

    /// <summary>The query part including the leading '?'. Empty when absent.</summary>
    public string Query { get; }

    /// <summary>The fragment part including the leading '#'. Empty when absent.</summary>
    public string Hash { get; }

    /// <summary>Parsed query parameters. Multiple values per key are supported.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> QueryParams => _queryParams.Value;

    /// <summary>Returns the first value for <paramref name="key"/> or null if absent.</summary>
    public string? GetQuery(string key) =>
        QueryParams.TryGetValue(key, out var values) && values.Count > 0 ? values[0] : null;

    /// <summary>Returns all values for <paramref name="key"/>, or an empty list if absent.</summary>
    public IReadOnlyList<string> GetQueryAll(string key) =>
        QueryParams.TryGetValue(key, out var values) ? values : [];


    private static IReadOnlyDictionary<string, IReadOnlyList<string>> ParseQuery(string query)
    {
        var staging = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(query))
            return new ReadOnlyDictionary<string, IReadOnlyList<string>>(new Dictionary<string, IReadOnlyList<string>>());

        var span = query.AsSpan();
        if (span.Length > 0 && span[0] == '?') span = span[1..];

        while (span.IsEmpty is false)
        {
            var ampIdx = span.IndexOf('&');
            ReadOnlySpan<char> pair;
            if (ampIdx < 0)
            {
                pair = span;
                span = ReadOnlySpan<char>.Empty;
            }
            else
            {
                pair = span[..ampIdx];
                span = span[(ampIdx + 1)..];
            }

            if (pair.IsEmpty) continue;

            var eqIdx = pair.IndexOf('=');
            string key, value;
            if (eqIdx < 0)
            {
                key = Decode(pair.ToString());
                value = string.Empty;
            }
            else
            {
                key = Decode(pair[..eqIdx].ToString());
                value = Decode(pair[(eqIdx + 1)..].ToString());
            }

            if (staging.TryGetValue(key, out var list))
            {
                list.Add(value);
            }
            else
            {
                staging[key] = [value];
            }
        }

        var snapshot = new Dictionary<string, IReadOnlyList<string>>(staging.Count, StringComparer.Ordinal);
        foreach (var kv in staging)
        {
            snapshot[kv.Key] = kv.Value.AsReadOnly();
        }
        return new ReadOnlyDictionary<string, IReadOnlyList<string>>(snapshot);

        static string Decode(string s) => Uri.UnescapeDataString(s.Replace('+', ' '));
    }
}
