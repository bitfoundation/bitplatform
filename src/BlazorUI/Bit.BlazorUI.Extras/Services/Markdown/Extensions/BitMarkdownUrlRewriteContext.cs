namespace Bit.BlazorUI;

/// <summary>
/// What a URL rewriter is told about the destination it is being asked to rewrite: the URL as the
/// author wrote it (already sanitized) and whether it belongs to an image or to a link.
/// </summary>
/// <param name="Url">The sanitized destination, exactly as it would otherwise be rendered.</param>
/// <param name="IsImage">True for an image source, false for a link destination.</param>
public readonly record struct BitMarkdownUrlRewriteContext(string Url, bool IsImage)
{
    /// <summary>
    /// True when the URL is relative - it has no scheme and does not begin with <c>//</c> - which
    /// is what a rewriter resolving a document against a base address is looking for. A fragment
    /// (<c>#section</c>) is not relative in this sense: it points inside the rendered page and
    /// resolving it elsewhere would break every heading link in the document.
    /// </summary>
    public bool IsRelative
    {
        get
        {
            if (string.IsNullOrEmpty(Url)) return false;
            if (Url[0] == '#') return false;
            if (Url.StartsWith("//", StringComparison.Ordinal)) return false;

            int colon = Url.IndexOf(':');
            if (colon < 0) return true;

            // A ':' that comes after a '/', '?' or '#' belongs to the path, not to a scheme.
            int separator = Url.IndexOfAny(['/', '?', '#']);
            return separator >= 0 && separator < colon;
        }
    }
}
