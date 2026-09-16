using System.Collections.Concurrent;

namespace Bit.BlazorUI;

/// <summary>
/// Process-wide record of which provider script / stylesheet URLs have already been
/// requested by <em>any</em> <see cref="BitMap{TMapProvider}"/>.
/// <para>
/// This lives on a non-generic type on purpose. Static state declared inside
/// <c>BitMap&lt;TMapProvider&gt;</c> is per closed generic, so two maps over different
/// provider types that happen to share a CDN URL would each pay their own interop
/// round-trip. The browser dedupes the underlying network request either way; what the
/// cache saves is serialising the URL list and awaiting a JS promise that does nothing.
/// </para>
/// </summary>
internal static class BitMapAssetCache
{
    // Keyed case-sensitively - URLs are case-sensitive on most servers.
    private static readonly ConcurrentDictionary<string, byte> _scripts = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, byte> _stylesheets = new(StringComparer.Ordinal);

    /// <summary>Returns the subset of <paramref name="urls"/> that has not been requested yet.</summary>
    public static List<string> FilterUnloadedScripts(IReadOnlyList<string> urls) => Filter(urls, _scripts);

    /// <summary>Returns the subset of <paramref name="urls"/> that has not been requested yet.</summary>
    public static List<string> FilterUnloadedStylesheets(IReadOnlyList<string> urls) => Filter(urls, _stylesheets);

    /// <summary>Marks scripts as loaded. Called only after the JS side reported success.</summary>
    public static void MarkScriptsLoaded(IEnumerable<string> urls)
    {
        foreach (var url in urls) _scripts[url] = 1;
    }

    /// <summary>Marks stylesheets as loaded. Called only after the JS side reported success.</summary>
    public static void MarkStylesheetsLoaded(IEnumerable<string> urls)
    {
        foreach (var url in urls) _stylesheets[url] = 1;
    }

    /// <summary>Clears the cache. Intended for unit tests only.</summary>
    public static void Reset()
    {
        _scripts.Clear();
        _stylesheets.Clear();
    }

    private static List<string> Filter(IReadOnlyList<string> urls, ConcurrentDictionary<string, byte> cache)
    {
        if (urls.Count == 0) return [];
        var result = new List<string>(urls.Count);
        foreach (var url in urls)
        {
            if (cache.ContainsKey(url) is false) result.Add(url);
        }
        return result;
    }
}
