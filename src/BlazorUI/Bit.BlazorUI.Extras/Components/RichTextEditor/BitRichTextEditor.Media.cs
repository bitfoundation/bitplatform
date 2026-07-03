using System.Diagnostics;

namespace Bit.BlazorUI;

// Image insertion (URL, drag-drop, paste, upload callback), color, and font.
public partial class BitRichTextEditor
{
    /// <summary>
    /// Invoked to persist an image binary, returning the URL to embed. When null, dropped or
    /// pasted images are embedded as inline data URLs.
    /// </summary>
    [Parameter] public Func<BitRichTextEditorImageUpload, Task<string?>>? OnImageUpload { get; set; }

    /// <summary>Font families offered in the font-family selector. Null/empty uses defaults.</summary>
    [Parameter] public IReadOnlyList<string>? FontFamilies { get; set; }

    /// <summary>Font sizes offered in the font-size selector. Null/empty uses defaults.</summary>
    [Parameter] public IReadOnlyList<string>? FontSizes { get; set; }

    private static readonly string[] DefaultFontFamilies =
        ["Arial", "Georgia", "Tahoma", "Times New Roman", "Verdana", "Courier New"];

    private static readonly string[] DefaultFontSizes =
        ["10px", "12px", "14px", "16px", "18px", "24px", "32px"];

    private IReadOnlyList<string> EffectiveFontFamilies
        => FontFamilies is { Count: > 0 } ? FontFamilies : DefaultFontFamilies;

    private IReadOnlyList<string> EffectiveFontSizes
        => FontSizes is { Count: > 0 } ? FontSizes : DefaultFontSizes;

    // ---- image insertion ----
    private bool _showImageInput;
    private string _imageUrl = "";

    private void ToggleImageInput()
    {
        _showImageInput = !_showImageInput;
        _imageUrl = "";
        ClearInlineError();
    }

    private async Task ApplyImageUrlAsync()
    {
        if (ControlsDisabled) return;

        var url = _imageUrl.Trim();
        if (IsAcceptableImageUrl(url) is false)
        {
            await RaiseErrorAsync(new BitRichTextEditorError("invalid-url", Label("image-url-invalid", "That image URL is not valid.")));
            return;
        }
        await _js.BitRichTextEditorInsertImageUrl(_editorRef, url);
        _showImageInput = false;
        _imageUrl = "";
        ClearInlineError();
    }

    // Known image MIME types accepted for data: URLs, mirroring the bridge's IMAGE_MIME set.
    private static readonly string[] KnownImageMimeTypes =
        ["image/png", "image/jpeg", "image/gif", "image/webp", "image/svg+xml"];

    // Maximum decoded image payload, mirroring the bridge's MAX_IMAGE_BYTES (10 MB). Enforced
    // before decoding so an oversized base64 string cannot exhaust memory on the server side.
    private const long MaxImageBytes = 10 * 1024 * 1024;

    // data: image URIs are only honored when the active policy permits them (the default policy
    // allows them); a null policy maps to the bridge default which also permits them.
    private bool DataImageUrisAllowed => SanitizationPolicy?.AllowDataImageUris ?? true;

    private static bool IsKnownImageMimeType(string contentType)
        => TryNormalizeImageMimeType(contentType, out _);

    // Validates the content type and, on success, exposes the canonical MIME value (parameters
    // stripped, matched against the known set in its canonical casing) so callers can pass the
    // normalized value through instead of reusing the raw client-reported content type.
    private static bool TryNormalizeImageMimeType(string? contentType, out string normalized)
    {
        normalized = "";
        var mime = contentType?.Trim();
        if (string.IsNullOrEmpty(mime)) return false;
        // Strip any parameters (e.g. "image/png; charset=...") before matching.
        var semicolon = mime.IndexOf(';');
        if (semicolon >= 0) mime = mime[..semicolon].Trim();
        var match = KnownImageMimeTypes.FirstOrDefault(m => string.Equals(m, mime, StringComparison.OrdinalIgnoreCase));
        if (match is null) return false;
        normalized = match;
        return true;
    }

    private bool IsAcceptableImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Length > 2048) return false;
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            // Only allow data: URLs when the policy permits them and the declared MIME is a
            // known image type, so non-image payloads cannot be smuggled in as an "image".
            if (DataImageUrisAllowed is false) return false;
            // Parse the declared MIME exactly (the segment between "data:" and the first ';' or
            // ',') and require that delimiter, so values like "data:image/pngfoo" are rejected.
            var rest = url["data:".Length..];
            var delimiter = rest.IndexOfAny([';', ',']);
            if (delimiter < 0) return false;
            return IsKnownImageMimeType(rest[..delimiter]);
        }
        return false;
    }

    /// <summary>Called by the bridge for each dropped/pasted image; returns the URL to embed.</summary>
    [JSInvokable("ResolveImageUrl")]
    public async Task<string?> _ResolveImageUrl(string fileName, string contentType, string base64)
    {
        // Reject oversized payloads from the base64 length before either path runs so neither the
        // inline data-URL fallback nor the upload path can embed/allocate an image past the limit.
        // Every 4 base64 chars decode to 3 bytes minus the trailing '=' padding, so subtract the
        // padding to avoid rejecting valid images that sit just under the limit.
        var padding = base64.EndsWith("==", StringComparison.Ordinal) ? 2 : base64.EndsWith("=", StringComparison.Ordinal) ? 1 : 0;
        var estimatedBytes = (long)base64.Length / 4 * 3 - padding;
        if (estimatedBytes > MaxImageBytes)
        {
            await RaiseErrorAsync(new BitRichTextEditorError("file-too-large",
                string.Format(Label("image-too-large", "\"{0}\" exceeds the 10 MB limit."), fileName)));
            return null;
        }

        // Validate the client-reported MIME on the shared path before either branch so unsupported
        // content types can neither be embedded as inline data URLs nor reach OnImageUpload. The
        // upload callback then acts as an additional guard rather than the first/only check. The
        // normalized (parameter-stripped, canonical) MIME is reused by both branches so neither the
        // inline data URL nor the upload contract carries the raw client-reported content type.
        if (TryNormalizeImageMimeType(contentType, out var mimeType) is false)
        {
            await RaiseErrorAsync(new BitRichTextEditorError("invalid-image",
                string.Format(Label("image-unsupported-type", "\"{0}\" is not a supported image type."), fileName)));
            return null;
        }

        if (OnImageUpload is null)
        {
            // Inline data URL fallback: also require the policy to permit data: image URIs before
            // embedding the (already MIME-validated) payload as one.
            if (DataImageUrisAllowed is false)
            {
                await RaiseErrorAsync(new BitRichTextEditorError("invalid-image",
                    string.Format(Label("image-unsupported-type", "\"{0}\" is not a supported image type."), fileName)));
                return null;
            }
            // Clear any lingering upload error so a successful retry doesn't keep showing the
            // previous banner.
            ClearInlineError();
            return $"data:{mimeType};base64,{base64}";   // inline data URL fallback
        }

        try
        {
            var bytes = Convert.FromBase64String(base64);
            if (bytes.Length > MaxImageBytes)
            {
                await RaiseErrorAsync(new BitRichTextEditorError("file-too-large",
                    string.Format(Label("image-too-large", "\"{0}\" exceeds the 10 MB limit."), fileName)));
                return null;
            }
            var url = await OnImageUpload(new BitRichTextEditorImageUpload(fileName, mimeType, bytes));
            if (string.IsNullOrWhiteSpace(url))
            {
                await RaiseErrorAsync(new BitRichTextEditorError("upload-failed",
                    string.Format(Label("image-upload-no-url", "Upload of \"{0}\" did not return a URL."), fileName)));
                return null;
            }
            // Clear any lingering upload error so a successful retry doesn't keep showing the
            // previous banner.
            ClearInlineError();
            return url;
        }
        catch (Exception ex)
        {
            // Keep infrastructure details out of the user-facing error; log them instead. Use
            // Trace (not Debug) so the failure is still recorded in Release builds, matching the
            // other always-on logging paths in this component (e.g. _OnCommandError).
            Trace.TraceError($"BitRichTextEditor image upload failed for \"{fileName}\": {ex}");
            await RaiseErrorAsync(new BitRichTextEditorError("upload-failed",
                string.Format(Label("image-upload-failed", "Upload of \"{0}\" failed. Please try again."), fileName)));
            return null;
        }
    }

    /// <summary>Called by the bridge to surface client-side validation errors (e.g. bad file).</summary>
    [JSInvokable("OnClientError")]
    public Task _OnClientError(string code, string message)
        => RaiseErrorAsync(new BitRichTextEditorError(code, message));

    // ---- color ----
    private async Task ApplyColorAsync(string kind, ChangeEventArgs e)
    {
        var value = e.Value?.ToString();
        if (ControlsDisabled || string.IsNullOrWhiteSpace(value)) return;
        await _js.BitRichTextEditorApplyColor(_editorRef, kind, value);
    }

    // ---- font ----
    private async Task ApplyFontAsync(string kind, ChangeEventArgs e)
    {
        var value = e.Value?.ToString();
        if (ControlsDisabled || string.IsNullOrWhiteSpace(value)) return;
        await _js.BitRichTextEditorApplyFont(_editorRef, kind, value);
    }

    // ---- indent / script ----
    private Task IndentAsync() => ExecAsync("indent");
    private Task OutdentAsync() => ExecAsync("outdent");
    private Task SubscriptAsync() => ExecAsync("subscript");
    private Task SuperscriptAsync() => ExecAsync("superscript");
}
