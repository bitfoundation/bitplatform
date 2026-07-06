namespace Bit.Brouter;

/// <summary>
/// Resolves route-relative URLs (<c>.</c>, <c>..</c>, <c>./x</c>, <c>../x</c>) against a current
/// path using segment math, React Router style: <c>.</c> is the current path, each <c>..</c> drops
/// one trailing segment. Used by <see cref="IBrouter.Navigate"/>,
/// <see cref="BrouterNavigationContext.Redirect"/> and <see cref="BrouterLink"/>.
/// </summary>
internal static class BrouterRelativeUrl
{
    /// <summary>
    /// True when <paramref name="url"/> is a route-relative reference: <c>"."</c>, <c>".."</c>, a
    /// path starting with <c>"./"</c> / <c>"../"</c>, or one of those dot forms followed directly by
    /// a query or hash (<c>".?tab=2"</c>, <c>"..#top"</c>) - <see cref="Resolve"/> preserves that
    /// suffix. Deliberately narrow - a bare segment like <c>"sibling"</c> (or a dotted name like
    /// <c>".well-known"</c>) is NOT treated as relative and keeps its historical base-relative
    /// meaning through <c>NavigationManager</c>, so introducing relative resolution can't silently
    /// change the destination of existing URLs.
    /// </summary>
    internal static bool IsRelative(string url)
    {
        if (string.IsNullOrEmpty(url) || url[0] != '.') return false;
        if (url.Length == 1 || url[1] is '/' or '?' or '#') return true;          // "." or "./..." or ".?..."/".#..."
        if (url[1] == '.') return url.Length == 2 || url[2] is '/' or '?' or '#'; // ".." or "../..." or "..?..."/"..#..."
        return false;
    }

    /// <summary>
    /// Resolves <paramref name="url"/> (which must satisfy <see cref="IsRelative"/>) against
    /// <paramref name="currentPath"/>. Segment math, not RFC 3986 directory semantics:
    /// from <c>/users/42</c>, <c>./edit</c> yields <c>/users/42/edit</c> and <c>../7</c> yields
    /// <c>/users/7</c>. <c>..</c> above the root clamps at the root (mirroring how browsers treat
    /// excess parent references). Any query/hash on <paramref name="url"/> is preserved.
    /// </summary>
    internal static string Resolve(string currentPath, string url)
    {
        // Split any query/hash off first; only the path part participates in segment math.
        var suffixStart = url.AsSpan().IndexOfAny('?', '#');
        var relPath = suffixStart < 0 ? url : url[..suffixStart];
        var suffix = suffixStart < 0 ? string.Empty : url[suffixStart..];

        var segments = new List<string>(currentPath.Split('/', StringSplitOptions.RemoveEmptyEntries));
        foreach (var segment in relPath.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".") continue;
            if (segment == "..")
            {
                if (segments.Count > 0) segments.RemoveAt(segments.Count - 1);
                continue;
            }
            segments.Add(segment);
        }

        return segments.Count == 0 ? "/" + suffix : "/" + string.Join('/', segments) + suffix;
    }

    /// <summary>Resolves <paramref name="url"/> against <paramref name="currentPath"/> when it is relative; returns it unchanged otherwise.</summary>
    internal static string ResolveIfRelative(string currentPath, string url) =>
        IsRelative(url) ? Resolve(currentPath, url) : url;
}
