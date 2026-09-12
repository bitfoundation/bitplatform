namespace Bit.BlazorUI;

// Link insertion / editing, with edit-existing-link prefill, validation, and remove affordances.
public partial class BitRichTextEditor
{
    private bool _showLinkInput;
    private string _linkUrl = "";
    private string _linkText = "";
    private bool _linkNewTab;
    private ElementReference _linkInputRef = default!;

    private async Task ToggleLinkInput()
    {
        _showLinkInput = !_showLinkInput;
        if (_showLinkInput)
        {
            await CloseOtherPanels("link");
            // Prefill when the selection is inside an existing link.
            _linkUrl = _state.InLink && _state.LinkHref is not null ? _state.LinkHref : "";
            // Reflect what the link already does, so re-applying an edited URL keeps its
            // new-tab behavior instead of silently dropping the target the author chose.
            _linkNewTab = _state.InLink && _state.LinkNewTab;
            _linkText = "";
            // Editing an existing link keeps its text; only a collapsed selection needs the text
            // field, so the panel opens with the focus request either way.
            RequestPanelFocus(() => _linkInputRef);
        }
        else
        {
            _linkUrl = "";
            _linkText = "";
            _linkNewTab = false;
        }
        ClearInlineError();
    }

    private async Task ApplyLinkAsync()
    {
        if (ControlsDisabled) return;

        var url = NormalizeLinkUrl(_linkUrl.Trim());
        if (string.IsNullOrWhiteSpace(url))
        {
            await RaiseErrorAsync(new BitRichTextEditorError("invalid-url", Label("link-url-required", "Enter a URL for the link.")));
            return;
        }
        if (url.Length > 2048 || IsAcceptableLinkUrl(url) is false)
        {
            await RaiseErrorAsync(new BitRichTextEditorError("invalid-url", Label("link-url-invalid", "That link URL is not valid.")));
            return;
        }

        if (_state.InLink)
            await _js.BitRichTextEditorUpdateLink(_editorRef, url, _linkNewTab);
        else
            await _js.BitRichTextEditorCreateLink(_editorRef, url, _linkNewTab, _linkText.Trim());

        // The link applied successfully, so clear any stale "invalid url" message.
        ClearInlineError();
        _showLinkInput = false;
        _linkUrl = "";
        _linkText = "";
        _linkNewTab = false;
    }

    private async Task RemoveLinkAsync()
    {
        if (ControlsDisabled) return;
        await _js.BitRichTextEditorExec(_editorRef, "unlink", null);
        ClearInlineError();
        _showLinkInput = false;
        _linkUrl = "";
        _linkText = "";
        _linkNewTab = false;
    }

    private async Task OnLinkKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await ApplyLinkAsync();
        else if (e.Key == "Escape") await ToggleLinkInput();
    }

    /// <summary>
    /// Accepts what people actually type. A bare host ("example.com", "example.com/docs") is not a
    /// valid absolute URL and would otherwise be rejected, yet it is never meant as a site-relative
    /// path, so it is promoted to https. Anything that already carries a scheme, or that starts
    /// with '/', '#', or a backslash, is left exactly as typed.
    /// </summary>
    private static string NormalizeLinkUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        if (url.StartsWith('/') || url.StartsWith('#') || url.StartsWith('\\')) return url;
        if (url.Contains("://", StringComparison.Ordinal)) return url;
        if (url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)) return url;
        // An address typed without its scheme is meant as an email link. Promoted as a host it
        // would become "https://john@example.com", a web link to example.com.
        if (LooksLikeEmailAddress(url)) return "mailto:" + url;
        // A colon before the first slash means some other scheme was typed; leave it alone so the
        // validation below can reject it rather than silently rewriting it. The exception is a
        // colon followed only by digits up to the path, which is a port ("example.com:8443/docs").
        var slash = url.IndexOf('/');
        var colon = url.IndexOf(':');
        var hostEnd = slash < 0 ? url.Length : slash;
        if (colon >= 0 && colon < hostEnd)
        {
            var port = url[(colon + 1)..hostEnd];
            if (port.Length == 0 || port.Any(c => c is < '0' or > '9')) return url;
            hostEnd = colon;
        }

        var host = url[..hostEnd];
        // Require something that looks like a host (a dot inside a label run) before assuming a
        // scheme is missing, so a single word stays the (invalid) input the user typed.
        var dot = host.IndexOf('.');
        if (dot <= 0 || dot == host.Length - 1) return url;
        return "https://" + url;
    }

    /// <summary>
    /// "name@domain.tld", optionally followed by a query ("?subject=..."): one '@' with something
    /// before it, a dotted domain after it, and no slash, colon, backslash or white space anywhere,
    /// so a URL carrying a user name ("user@example.com/path") is not mistaken for one.
    /// </summary>
    private static bool LooksLikeEmailAddress(string url)
    {
        var at = url.IndexOf('@');
        if (at <= 0 || url.IndexOf('@', at + 1) >= 0) return false;
        if (url.Any(c => c is '/' or ':' or '\\' || char.IsWhiteSpace(c))) return false;

        var domain = url[(at + 1)..];
        var query = domain.IndexOf('?');
        if (query >= 0) domain = domain[..query];
        var dot = domain.IndexOf('.');
        return dot > 0 && dot < domain.Length - 1;
    }

    private static bool IsAcceptableLinkUrl(string url)
    {
        // Allow absolute http(s)/mailto/tel and site-relative URLs; reject script vectors.
        if (url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)) return false;
        // Protocol-relative URLs (//example.com) are external; require an explicit scheme.
        if (url.StartsWith("//")) return false;
        // Backslash-based protocol-relative forms (/\host, \/host, \\host) are normalized by
        // browsers to //host, so reject any leading backslash before the site-relative check.
        if (url.StartsWith('\\') || url.StartsWith("/\\")) return false;
        if (url.StartsWith('/') || url.StartsWith('#') || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
            return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var u)
            && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
    }
}
