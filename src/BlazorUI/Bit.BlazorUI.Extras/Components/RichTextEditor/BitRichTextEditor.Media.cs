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

    private long _maxImageSize = DefaultMaxImageSize;
    /// <summary>
    /// The largest image, in bytes, that a drop or a paste may insert (10 MB by default). It is checked in the browser
    /// before the file is read and again here before it is decoded or handed to <see cref="OnImageUpload"/>. Zero or a
    /// negative value restores the default.
    /// </summary>
    [Parameter]
    public long MaxImageSize
    {
        get => _maxImageSize;
        set => _maxImageSize = value > 0 ? value : DefaultMaxImageSize;
    }

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

    // A select whose value matches none of its options draws blank, so a font the content inherits from the page
    // (or a size outside the list) reads as the placeholder option rather than as nothing at all.
    private static string SelectedOption(string? value, IReadOnlyList<string> options)
        => value is not null && options.Any(o => string.Equals(o, value, StringComparison.OrdinalIgnoreCase))
            ? options.First(o => string.Equals(o, value, StringComparison.OrdinalIgnoreCase))
            : "";

    // ---- image insertion ----
    private bool _showImageInput;
    private string _imageUrl = "";
    private string _imageAlt = "";
    private int? _imageWidth;
    private ElementReference _imageInputRef = default!;

    private async Task ToggleImageInput()
    {
        _showImageInput = !_showImageInput;
        // Opening the panel on a selected image edits that image: it starts out showing the source
        // and the alternative text it already carries. Inserting is the only moment alt text would
        // otherwise be written, which is how images end up published without any.
        _imageUrl = _showImageInput && _state.ImageSelected ? _state.ImageSrc ?? "" : "";
        _imageAlt = _showImageInput && _state.ImageSelected ? _state.ImageAlt ?? "" : "";
        // The width is the keyboard's (and a single pointer's) alternative to dragging the resize handle.
        _imageWidth = _showImageInput && _state.ImageSelected ? _state.ImageWidth : null;
        if (_showImageInput)
        {
            await CloseOtherPanels("image");
            RequestPanelFocus(() => _imageInputRef);
        }
        ClearInlineError();
    }

    private async Task CloseImageInput()
    {
        if (_showImageInput) await ToggleImageInput();
        RequestEditorFocus();
    }

    private async Task OnImageKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await ApplyImageAsync();
    }

    /// <summary>
    /// Applies the panel: rewrites the selected image when there is one, otherwise inserts a new
    /// one at the caret. Both paths go through the same URL validation.
    /// </summary>
    private async Task ApplyImageAsync()
    {
        if (ControlsDisabled) return;

        var url = _imageUrl.Trim();
        if (IsAcceptableImageUrl(url) is false)
        {
            await RaiseErrorAsync(new BitRichTextEditorError("invalid-url", Loc("image-url-invalid", "That image URL is not valid.")));
            return;
        }

        var width = _imageWidth is > 0 ? _imageWidth : null;
        if (_state.ImageSelected)
        {
            await _js.BitRichTextEditorUpdateImage(_editorRef, url, _imageAlt.Trim(), width);
        }
        else
        {
            await _js.BitRichTextEditorInsertImageUrl(_editorRef, url, _imageAlt.Trim(), width);
        }

        _showImageInput = false;
        _imageUrl = "";
        _imageAlt = "";
        _imageWidth = null;
        ClearInlineError();
        RequestEditorFocus();
    }

    // Known image MIME types accepted for data: URLs, mirroring the bridge's IMAGE_MIME set.
    private static readonly string[] KnownImageMimeTypes =
        ["image/png", "image/jpeg", "image/gif", "image/webp", "image/svg+xml"];

    // The default of MaxImageSize, the decoded image payload past which a file is refused. Enforced before decoding so
    // an oversized base64 string cannot exhaust memory on the server side.
    private const long DefaultMaxImageSize = 10 * 1024 * 1024;

    // How the limit is written in a message, the same way the bridge writes it ("10 MB", "512 KB").
    private static string FormatSize(long bytes)
        => bytes >= 1024 * 1024
            ? $"{Math.Round(bytes / (1024d * 1024), 1).ToString(System.Globalization.CultureInfo.InvariantCulture)} MB"
            : $"{Math.Max(1, (long)Math.Round(bytes / 1024d))} KB";

    private Task RaiseImageTooLargeAsync(string fileName)
        => RaiseErrorAsync(new BitRichTextEditorError("file-too-large",
            string.Format(Loc("image-too-large", "\"{0}\" exceeds the {1} limit."), fileName, FormatSize(MaxImageSize))));

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
        if (estimatedBytes > MaxImageSize)
        {
            await RaiseImageTooLargeAsync(fileName);
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
                string.Format(Loc("image-unsupported-type", "\"{0}\" is not a supported image type."), fileName)));
            return null;
        }

        if (OnImageUpload is null)
        {
            // Inline data URL fallback: also require the policy to permit data: image URIs before
            // embedding the (already MIME-validated) payload as one.
            if (DataImageUrisAllowed is false)
            {
                await RaiseErrorAsync(new BitRichTextEditorError("invalid-image",
                    string.Format(Loc("image-unsupported-type", "\"{0}\" is not a supported image type."), fileName)));
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
            if (bytes.Length > MaxImageSize)
            {
                await RaiseImageTooLargeAsync(fileName);
                return null;
            }
            var url = await OnImageUpload(new BitRichTextEditorImageUpload(fileName, mimeType, bytes));
            if (string.IsNullOrWhiteSpace(url))
            {
                await RaiseErrorAsync(new BitRichTextEditorError("upload-failed",
                    string.Format(Loc("image-upload-no-url", "Upload of \"{0}\" did not return a URL."), fileName)));
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
                string.Format(Loc("image-upload-failed", "Upload of \"{0}\" failed. Please try again."), fileName)));
            return null;
        }
    }

    /// <summary>
    /// Reported by the bridge once dropped or pasted images are in. A file name is not a text alternative, so they go in
    /// with an empty one, and the live region says so - with where to write a real one while the image group is there.
    /// </summary>
    [JSInvokable("OnImagesInserted")]
    public void _OnImagesInserted(int count)
    {
        var message = count == 1
            ? Loc("image-inserted", "Image inserted.")
            : string.Format(Loc("images-inserted", "{0} images inserted."), count);

        if (Has(BitRichTextEditorToolbar.Image))
        {
            message += " " + Loc("image-alt-hint", "Select an image and use the image button to describe it.");
        }

        Announce(message);
    }

    /// <summary>
    /// Called by the bridge to surface client-side validation errors (e.g. bad file). The bridge says what went wrong
    /// as a localization key with its English template and the values that fill it, so the message reaches the
    /// reader through <see cref="Localizer"/> like every other one rather than in the bridge's English.
    /// </summary>
    [JSInvokable("OnClientError")]
    public Task _OnClientError(string code, string key, string template, string[]? args)
    {
        var text = Loc(key, template);
        if (args is { Length: > 0 })
        {
            try
            {
                text = string.Format(text, (object[])args);
            }
            catch (FormatException)
            {
                // A translation whose placeholders do not match the values falls back to the English template.
                text = string.Format(template, (object[])args);
            }
        }
        return RaiseErrorAsync(new BitRichTextEditorError(code, text));
    }

    /// <summary>
    /// The image alignment buttons only mean anything while an image is selected, so they stay
    /// disabled until the bridge reports one.
    /// </summary>
    private bool ImageOpsDisabled => ControlsDisabled || _state.ImageSelected is false;

    // Applies (or clears, when the same alignment is already active) the alignment of the selected
    // image. Clicking the active alignment again is the natural way to get an image back inline.
    private async Task AlignImageAsync(string align)
    {
        if (ImageOpsDisabled) return;
        await _js.BitRichTextEditorAlignImage(_editorRef, _state.ImageAlign == align ? "none" : align);
    }

    // ---- color ----
    private async Task ApplyColorAsync(string kind, ChangeEventArgs e)
    {
        var value = e.Value?.ToString();
        if (ControlsDisabled || string.IsNullOrWhiteSpace(value)) return;
        await _js.BitRichTextEditorApplyColor(_editorRef, kind, value);
    }

    // Takes the text or highlight color back off the selection, which "clear formatting" can only
    // do by removing every other inline style with it.
    private async Task ClearColorAsync(string kind)
    {
        if (ControlsDisabled) return;
        await _js.BitRichTextEditorClearColor(_editorRef, kind);
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
