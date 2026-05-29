namespace Bit.BlazorUI;

/// <summary>
/// Loads optional alternate CSS bundles by id (creates or updates a <c>&lt;link rel="stylesheet"&gt;</c>).
/// Use only trusted/same-origin URLs. The loader rejects relative-with-scheme strings such as
/// <c>javascript:</c> and <c>data:</c>, rejects protocol-relative URLs (<c>//host/...</c>);
/// for absolute URLs the scheme must be <c>http</c> or <c>https</c>.
/// </summary>
public sealed class BitExternalThemeLoader
{
    private readonly IJSRuntime _js;

    public BitExternalThemeLoader(IJSRuntime js)
    {
        ArgumentNullException.ThrowIfNull(js);

        _js = js;
    }

    /// <summary>
    /// Creates (or updates) a <c>&lt;link rel="stylesheet"&gt;</c> element identified by
    /// <paramref name="linkElementId"/> with the given <paramref name="href"/>.
    /// </summary>
    /// <param name="linkElementId">DOM id used to find / create the link element.</param>
    /// <param name="href">Stylesheet URL. Same-origin paths and absolute http/https URLs are accepted; non-http schemes are rejected.</param>
    public ValueTask AttachStylesheetAsync(string linkElementId, string href)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(linkElementId);
        ArgumentException.ThrowIfNullOrWhiteSpace(href);

        // Defense-in-depth href validation. The XML doc warns the caller to use trusted URLs, but
        // the JS side does no checking, so a tampered or user-supplied href could attach a
        // `javascript:`/`data:` link that browsers may actually load when probed. Rejecting them
        // here keeps the loader honest. Protocol-relative URLs (e.g. `//host/x.css`) are also
        // rejected because the browser resolves them as cross-origin http(s), which violates the
        // documented "same-origin relative path or absolute http/https URL" contract. Relative
        // paths (no scheme) are allowed because that's the common same-origin case
        // ("/css/dark.css", "themes/light.css").
        ValidateHref(href);

        return _js.BitExternalThemeAttach(linkElementId, href);
    }

    /// <summary>Removes a previously-attached <c>&lt;link rel="stylesheet"&gt;</c> by id.</summary>
    /// <param name="linkElementId">DOM id originally passed to <see cref="AttachStylesheetAsync"/>.</param>
    public ValueTask DetachStylesheetAsync(string linkElementId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(linkElementId);

        return _js.BitExternalThemeDetach(linkElementId);
    }

    private static void ValidateHref(string href)
    {
        // Protocol-relative URLs (e.g. "//evil.example/x.css") are not absolute URIs as far as
        // Uri.TryCreate(Absolute) is concerned, so they would otherwise slip past the scheme
        // check below and reach the browser, which resolves them to a cross-origin
        // http(s)://evil.example/x.css. Reject them up front; callers must use a same-origin
        // relative path or an explicit http/https URL. Browsers also normalize backslashes to
        // forward slashes in URLs, so `\\host\x.css` is the same attack surface — block it too.
        var trimmed = href.TrimStart();
        if (trimmed.StartsWith("//", StringComparison.Ordinal) ||
            trimmed.StartsWith(@"\\", StringComparison.Ordinal) ||
            trimmed.StartsWith(@"/\", StringComparison.Ordinal) ||
            trimmed.StartsWith(@"\/", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Stylesheet href must not be a protocol-relative URL. Use a same-origin relative path or an explicit http/https URL.",
                nameof(href));
        }

        if (Uri.TryCreate(href, UriKind.Absolute, out var absolute))
        {
            // Reject every absolute-URL scheme other than http(s). That covers javascript:, data:,
            // file:, vbscript:, blob:, etc. — none of which belong on a <link rel="stylesheet">.
            // Uri.UriSchemeHttp/Https are static readonly fields, not constants, so they can't be
            // used in a relational pattern; compare with case-insensitive ordinal equality instead.
            if (!string.Equals(absolute.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(absolute.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Stylesheet href scheme '{absolute.Scheme}' is not allowed. Use http, https, or a same-origin relative path.",
                    nameof(href));
            }

            return;
        }

        // Relative URLs are allowed. We still want to reject the `javascript:` form even when
        // Uri.TryCreate refuses to parse it as absolute (it does for some inputs depending on
        // platform), so a final string-prefix sanity check covers the residual surface.
        if (LooksLikeDangerousRelativeScheme(trimmed))
        {
            throw new ArgumentException(
                "Stylesheet href appears to use a non-http scheme (javascript:, data:, vbscript:). Use http, https, or a same-origin relative path.",
                nameof(href));
        }
    }

    private static bool LooksLikeDangerousRelativeScheme(string trimmed)
    {
        // Match common attack vectors that browsers used to (or still) accept on attribute values.
        return trimmed.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase);
    }
}
