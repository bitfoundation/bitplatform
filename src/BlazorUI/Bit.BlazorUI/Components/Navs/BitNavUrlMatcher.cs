using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// The URL matching that the automatic mode of the navigation components (<see cref="BitNav&lt;TItem&gt;"/>,
/// <see cref="BitNavBar&lt;TItem&gt;"/>) picks their selected item with.
/// <br />
/// Both sides of a comparison are reduced to the same shape first: an app-relative URL that starts with a
/// slash. The path is kept apart from the query and the fragment, so an item URL that carries neither still
/// matches a page that was reached with one.
/// </summary>
internal static class BitNavUrlMatcher
{
    private static readonly char[] _UrlSeparators = ['?', '#'];

    private const string DOUBLE_STAR_PLACEHOLDER = "___BIT_NAV_DOUBLESTAR_PLACEHOLDER___";



    /// <summary>
    /// Reduces the URL the app currently sits on to the app-relative form the item URLs are compared
    /// against, and splits the path off the query and the fragment.
    /// </summary>
    internal static (string Url, string Path) GetCurrentUrl(NavigationManager navigationManager)
    {
        var url = ToRelativeUrl(navigationManager.BaseUri, navigationManager.Uri);
        var separatorIndex = url.IndexOfAny(_UrlSeparators);

        return (url, separatorIndex < 0 ? url : url[..separatorIndex]);
    }

    /// <summary>
    /// Whether the URL of an item points at the page the app currently sits on, in the given matching mode.
    /// </summary>
    internal static bool IsMatch(string? itemUrl, BitNavMatch match, string currentUrl, string currentPath, string baseUri)
    {
        if (itemUrl.HasNoValue()) return false;

        // A pattern is matched as it was written: normalizing it (adding a leading slash, trimming a
        // trailing one) would corrupt the anchors and the quantifiers it is made of.
        if (match is BitNavMatch.Regex)
        {
            return IsRegexMatch(currentUrl, itemUrl!) ||
                   (currentPath != currentUrl && IsRegexMatch(currentPath, itemUrl!));
        }

        if (match is BitNavMatch.Wildcard)
        {
            // A wildcard is not a regex the caller writes options into, so it matches the way the plain
            // modes do: the case of the path does not distinguish two pages.
            var pattern = $"^{WildcardToRegex(itemUrl!)}$";
            return IsRegexMatch(currentUrl, pattern, RegexOptions.IgnoreCase) ||
                   (currentPath != currentUrl && IsRegexMatch(currentPath, pattern, RegexOptions.IgnoreCase));
        }

        var url = ToRelativeUrl(baseUri, itemUrl!);
        var target = url.IndexOfAny(_UrlSeparators) < 0 ? currentPath : currentUrl;

        if (UrlEquals(target, url)) return true;

        return match is BitNavMatch.Prefix && IsStrictlyPrefixWithSeparator(target, url);
    }



    private static string WildcardToRegex(string pattern)
    {
        pattern = Regex.Escape(pattern);

        pattern = pattern.Replace(@"\*\*", DOUBLE_STAR_PLACEHOLDER);
        pattern = pattern.Replace(@"\*", "[^/]*");
        pattern = pattern.Replace(@"\?", "[^/]");
        pattern = pattern.Replace(DOUBLE_STAR_PLACEHOLDER, ".*");

        return pattern;
    }

    // Turns anything an item may carry as its URL into the app-relative form the current URL is reduced
    // to: an absolute URL under the base of the app loses that base, and a relative one gains the leading
    // slash it is missing, so "products", "/products" and "https://host/app/products" all compare equal.
    private static string ToRelativeUrl(string baseUri, string url)
    {
        url = url.Trim();

        if (url.StartsWith(baseUri, StringComparison.OrdinalIgnoreCase))
        {
            // The base URI always ends with a slash, which is kept as the leading slash of the result.
            return url[(baseUri.Length - 1)..];
        }

        if (url.StartsWith("./", StringComparison.Ordinal)) return url[1..];

        return url.StartsWith('/') ? url : $"/{url}";
    }

    // URLs are compared the way the browser treats them: the case of the path does not distinguish two
    // pages, and a trailing slash does not either. This mirrors what Blazor's own NavLink does.
    private static bool UrlEquals(string current, string url)
    {
        if (string.Equals(current, url, StringComparison.OrdinalIgnoreCase)) return true;

        // "/products" is the same page as "/products/", whichever of the two carries the slash.
        if (current.Length == url.Length - 1 && url[^1] == '/')
        {
            return url.StartsWith(current, StringComparison.OrdinalIgnoreCase);
        }

        if (url.Length == current.Length - 1 && current[^1] == '/')
        {
            return current.StartsWith(url, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    // A prefix only matches on a path boundary, so "/product" does not light up on "/products" the way a
    // plain StartsWith would. The separators are the ones that end a path segment: "/", "?" and "#".
    private static bool IsStrictlyPrefixWithSeparator(string current, string prefix)
    {
        var prefixLength = prefix.Length;

        if (current.Length <= prefixLength) return false;

        if (current.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) is false) return false;

        return prefixLength == 0
            || prefix[prefixLength - 1] == '/'
            || current[prefixLength] == '/'
            || current[prefixLength] == '?'
            || current[prefixLength] == '#';
    }

    // The Regex and Wildcard modes run a pattern that comes from the item, so the match is given a
    // timeout to keep a pathological pattern from hanging the render, and a malformed one is simply
    // treated as a non-match instead of tearing the whole nav down.
    private static bool IsRegexMatch(string input, string pattern, RegexOptions options = RegexOptions.None)
    {
        try
        {
            return Regex.IsMatch(input, pattern, options, TimeSpan.FromSeconds(1));
        }
        catch (RegexMatchTimeoutException) { return false; }
        catch (ArgumentException) { return false; }
    }
}
