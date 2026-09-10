namespace Bit.BlazorUI;

// Link insertion / editing, with edit-existing-link prefill, validation, and remove affordances.
public partial class BitRichTextEditor
{
    private bool _showLinkInput;
    private string _linkUrl = "";
    private string _linkText = "";
    private bool _linkNewTab;
    private ElementReference _linkInputRef = default!;

    private void ToggleLinkInput()
    {
        _showLinkInput = !_showLinkInput;
        if (_showLinkInput)
        {
            CloseOtherPanels("link");
            // Prefill when the selection is inside an existing link.
            _linkUrl = _state.InLink && _state.LinkHref is not null ? _state.LinkHref : "";
            _linkNewTab = false;
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
        else if (e.Key == "Escape") ToggleLinkInput();
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
        // A colon before the first slash means some other scheme was typed; leave it alone so the
        // validation below can reject it rather than silently rewriting it.
        var slash = url.IndexOf('/');
        var colon = url.IndexOf(':');
        if (colon >= 0 && (slash < 0 || colon < slash)) return url;

        var host = slash < 0 ? url : url[..slash];
        // Require something that looks like a host (a dot inside a label run) before assuming a
        // scheme is missing, so a single word stays the (invalid) input the user typed.
        var dot = host.IndexOf('.');
        if (dot <= 0 || dot == host.Length - 1) return url;
        return "https://" + url;
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
