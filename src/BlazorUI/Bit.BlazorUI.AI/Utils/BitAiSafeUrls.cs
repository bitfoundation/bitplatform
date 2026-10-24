using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// The one reading of the addresses a model, a tool or a retrieval hands the views - the links and images of its
/// Markdown, an image part, an image artifact, the favicon of a source: which schemes may be followed or loaded at all,
/// and what the <see cref="BitAiImagePolicy"/> is asked about an image.
/// </summary>
internal static class BitAiSafeUrls
{
    // Scheme names a link or an image may use; everything else (javascript:, vbscript:, data: for a link...) is
    // neutralised. Allowing the known-safe ones is cheaper and more reliable than detecting the dangerous ones.
    private static readonly string[] AllowedSchemes = ["http", "https", "mailto", "tel", "ftp"];

    /// <summary>
    /// What a neutralised image source becomes: a transparent pixel, so the image keeps its alternative text.
    /// </summary>
    internal const string BlankImage = "data:image/gif;base64,R0lGODlhAQABAAAAACH5BAEKAAEALAAAAAABAAEAAAICTAEAOw==";

    /// <summary>
    /// Blocks the link schemes that can execute script.
    /// </summary>
    /// <param name="url">The address to check.</param>
    /// <returns>The address, or <c>#</c> when its scheme is not one a link may use.</returns>
    /// <remarks>
    /// A model can be persuaded to emit a javascript: URL. A relative address has no scheme and is kept.
    /// </remarks>
    internal static string Link(string url)
    {
        var trimmed = url.TrimStart();
        if (trimmed.Length == 0) return "#";

        if (SchemeOf(trimmed) is not { } scheme) return url;

        return AllowedSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase) ? url : "#";
    }

    /// <summary>
    /// Blocks the image sources that can execute script, while still allowing inline image data URLs and the browser's
    /// own <c>blob:</c> ones (the preview of a file the user attached).
    /// </summary>
    /// <param name="url">The source to check.</param>
    /// <returns>The source, or <see cref="BlankImage"/> when its scheme is not safe.</returns>
    internal static string Image(string url)
    {
        var trimmed = url.TrimStart();
        if (trimmed.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        var safe = Link(url);
        return safe == "#" ? BlankImage : safe;
    }

    /// <summary>
    /// What the policy decides about an image source already made safe (<see cref="Image"/>).
    /// </summary>
    /// <param name="policy">The policy in force.</param>
    /// <param name="source">The safe source.</param>
    /// <param name="baseUri">Reads the base address of the document, or null where there is none.</param>
    /// <returns>The decision; <see cref="BlankImage"/> is always shown, since it fetches nothing.</returns>
    /// <remarks>
    /// A host list is asked about the address the browser fetches: a relative or protocol-relative source is resolved
    /// against the base address of the document first, so the app's own host can be listed like any other. No other
    /// policy depends on the host, so none of them reads the base address.
    /// </remarks>
    internal static BitAiImageDecision Decide(BitAiImagePolicy policy, string source, Func<string?> baseUri)
    {
        if (source == BlankImage) return BitAiImageDecision.Show;

        return policy.Decide(policy.Mode == BitAiImageMode.AllowListedHosts ? Resolve(source, baseUri) : source);
    }

    /// <summary>
    /// The address the browser fetches for a source: the source as the browser reads it when it names its scheme, and
    /// the source resolved against the base address of the document otherwise.
    /// </summary>
    /// <param name="source">The source, absolute or relative.</param>
    /// <param name="baseUri">Reads the base address of the document, or null where there is none.</param>
    /// <returns>The absolute address, or the source as it was when it cannot be resolved.</returns>
    internal static string Resolve(string source, Func<string?> baseUri)
    {
        var normalized = AsBrowserReadsIt(source);

        // An absolute address is never resolved, so the base address is not read for it.
        if (SchemeOf(normalized) is not null) return normalized;

        return baseUri() is { } address
               && Uri.TryCreate(address, UriKind.Absolute, out var baseAddress)
               && Uri.TryCreate(baseAddress, normalized, out var absolute)
            ? absolute.AbsoluteUri
            : source;
    }

    // The source as the browser parses it (the WHATWG URL standard), which is what a host list has to be asked about:
    // leading and trailing controls and spaces are dropped, so are tabs and line breaks anywhere in it, and a backslash is
    // a slash in a relative address or one of the schemes a browser treats specially. Uri reads "/\evil.com/a.png" as a
    // path on the app's own host and "/<tab>/evil.com/a.png" as one too; a browser fetches both from evil.com.
    internal static string AsBrowserReadsIt(string source)
    {
        var start = 0;
        var end = source.Length;
        while (start < end && source[start] <= ' ') start++;
        while (end > start && source[end - 1] <= ' ') end--;

        var trimmed = source.AsSpan(start, end - start);
        if (trimmed.IndexOfAny("\t\n\r\\") < 0) return start == 0 && end == source.Length ? source : trimmed.ToString();

        var builder = new StringBuilder(trimmed.Length);
        foreach (var c in trimmed)
        {
            if (c is not ('\t' or '\n' or '\r')) builder.Append(c);
        }

        var normalized = builder.ToString();
        var scheme = SchemeOf(normalized);

        return scheme is null || SpecialSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase)
            ? normalized.Replace('\\', '/')
            : normalized;
    }

    // The schemes whose addresses a browser parses with a host, and in which it reads a backslash as a slash.
    private static readonly string[] SpecialSchemes = ["http", "https", "ftp", "ws", "wss", "file"];

    /// <summary>
    /// The host an address points at, for naming what a placeholder would load.
    /// </summary>
    /// <param name="url">An absolute address.</param>
    /// <returns>The host, or the address itself when it has none.</returns>
    internal static string HostOf(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Host.Length > 0 ? uri.Host : url;

    // The scheme an address names: the text before its first colon, when no slash, query or fragment comes before that
    // colon. Read off the text rather than through Uri, which takes a rooted path ("/images/a.png") for a file address on
    // Unix.
    private static string? SchemeOf(string trimmed)
    {
        var colon = trimmed.IndexOf(':');
        if (colon < 0) return null;

        var slash = trimmed.IndexOfAny(['/', '?', '#']);
        if (slash >= 0 && slash < colon) return null;

        return trimmed[..colon];
    }
}
