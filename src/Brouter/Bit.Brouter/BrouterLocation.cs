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
    private readonly string[] _segments;

    internal BrouterLocation(string fullUri, string path, string[] segments, string query, string hash, bool hasTrailingSlash = false)
    {
        FullUri = fullUri;
        Path = path;
        if (segments is null || segments.Length == 0)
        {
            _segments = [];
        }
        else
        {
            _segments = new string[segments.Length];
            Array.Copy(segments, _segments, segments.Length);
        }
        Segments = new ReadOnlyCollection<string>(_segments);
        Query = query;
        Hash = hash;
        HasTrailingSlash = hasTrailingSlash;
        _queryParams = new Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>>(() => ParseQuery(query));
    }

    /// <summary>The absolute URI of the location.</summary>
    public string FullUri { get; }

    /// <summary>The path part starting with '/'. Does not include query or hash.</summary>
    public string Path { get; }

    /// <summary>The path split by '/' with empty segments removed and segments URI-decoded.</summary>
    public IReadOnlyList<string> Segments { get; }

    /// <summary>Internal fast-path access to the raw segment array. Must not be mutated.</summary>
    internal string[] SegmentsArray => _segments;

    /// <summary>
    /// True when the original URL path ended with a trailing '/' and
    /// <see cref="BrouterOptions.IgnoreTrailingSlash"/> is <c>false</c>.
    /// Used by route matching to keep <c>/users</c> and <c>/users/</c> distinguishable
    /// under strict-trailing-slash mode (the slash is otherwise lost when the path is
    /// split into segments).
    /// </summary>
    internal bool HasTrailingSlash { get; }

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
        // Case-insensitive keys mirror ASP.NET Core's IQueryCollection and align with
        // RouteParameters (OrdinalIgnoreCase), so [BrouterQuery]/GetQuery(...) bind reliably
        // regardless of the casing used in the URL (e.g. "?Tab=1" vs "?tab=1").
        var staging = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query))
            return new ReadOnlyDictionary<string, IReadOnlyList<string>>(
                new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase));

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

        var snapshot = new Dictionary<string, IReadOnlyList<string>>(staging.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var kv in staging)
        {
            snapshot[kv.Key] = kv.Value.AsReadOnly();
        }
        return new ReadOnlyDictionary<string, IReadOnlyList<string>>(snapshot);

        static string Decode(string s)
        {
            // Mirrors the defensive decoding used for path segments in Brouter.UpdateLocation:
            // malformed percent-encoding (e.g. "%ZZ" or a stray "%") would otherwise throw
            // UriFormatException and break query parsing / [BrouterQuery] binding the first
            // time QueryParams is accessed. Fall back to the raw substring (with '+' -> ' '
            // already applied) so navigation keeps working and routes can still match.
            var replaced = s.Replace('+', ' ');
            try
            {
                return Uri.UnescapeDataString(replaced);
            }
            catch (UriFormatException)
            {
                return replaced;
            }
        }
    }
}
