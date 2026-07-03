namespace Bit.BlazorUI;

// Link insertion / editing, with edit-existing-link prefill, validation, and remove affordances.
public partial class BitRichTextEditor
{
    private bool _showLinkInput;
    private string _linkUrl = "";

    private void ToggleLinkInput()
    {
        _showLinkInput = !_showLinkInput;
        if (_showLinkInput)
        {
            // Prefill when the selection is inside an existing link.
            _linkUrl = _state.InLink && _state.LinkHref is not null ? _state.LinkHref : "";
        }
        else
        {
            _linkUrl = "";
        }
        ClearInlineError();
    }

    private async Task ApplyLinkAsync()
    {
        if (ControlsDisabled) return;

        var url = _linkUrl.Trim();
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
            await _js.BitRichTextEditorUpdateLink(_editorRef, url);
        else
            await _js.BitRichTextEditorCreateLink(_editorRef, url);

        // The link applied successfully, so clear any stale "invalid url" message.
        ClearInlineError();
        _showLinkInput = false;
        _linkUrl = "";
    }

    private async Task RemoveLinkAsync()
    {
        if (ControlsDisabled) return;
        await _js.BitRichTextEditorExec(_editorRef, "unlink", null);
        ClearInlineError();
        _showLinkInput = false;
        _linkUrl = "";
    }

    private async Task OnLinkKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await ApplyLinkAsync();
        else if (e.Key == "Escape") ToggleLinkInput();
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
