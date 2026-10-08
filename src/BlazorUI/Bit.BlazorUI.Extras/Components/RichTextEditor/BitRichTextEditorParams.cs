namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitRichTextEditor"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the editors of an application agree on: their localizer (which is where a localized app
/// sets it once), their toolbar, their sanitization policy, how they take pasted content and uploaded images, their
/// typing aids, their size and their look. The value and its events, the label, the description, the validation state
/// and the auto-focus are left out on purpose: they are what makes one editor the one it is.
/// </remarks>
public class BitRichTextEditorParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitRichTextEditor"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitRichTextEditor value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitRichTextEditor)}";



    public string Name => ParamName;



    /// <summary>
    /// Turns a URL typed into the editor into a link as soon as the word is finished.
    /// </summary>
    public bool? AutoLink { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the rich text editor.
    /// </summary>
    public BitRichTextEditorClassStyles? Classes { get; set; }

    /// <summary>
    /// The colors the text and highlight color buttons offer as swatches.
    /// </summary>
    public IReadOnlyList<BitRichTextEditorColor>? ColorPalette { get; set; }

    /// <summary>
    /// Debounce window (ms) for content-change notifications while typing.
    /// </summary>
    public int? DebounceMs { get; set; }

    /// <summary>
    /// Font families offered in the font-family selector.
    /// </summary>
    public IReadOnlyList<string>? FontFamilies { get; set; }

    /// <summary>
    /// Font sizes offered in the font-size selector.
    /// </summary>
    public IReadOnlyList<string>? FontSizes { get; set; }

    /// <summary>
    /// The height the editing surface starts at (any CSS length).
    /// </summary>
    public string? Height { get; set; }

    /// <summary>
    /// Custom key-combo to command map, merged over the built-in defaults.
    /// </summary>
    public IReadOnlyDictionary<string, string>? KeyboardShortcuts { get; set; }

    /// <summary>
    /// Localized labels/tooltips provider.
    /// </summary>
    public IBitRichTextEditorLocalizer? Localizer { get; set; }

    /// <summary>
    /// Maximum height of the editing surface (any CSS length).
    /// </summary>
    public string? MaxHeight { get; set; }

    /// <summary>
    /// The largest image, in bytes, that a drop or a paste may insert.
    /// </summary>
    public long? MaxImageSize { get; set; }

    /// <summary>
    /// Maximum plain-text character count.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Invoked to persist an image binary, returning the URL to embed.
    /// </summary>
    public Func<BitRichTextEditorImageUpload, Task<string?>>? OnImageUpload { get; set; }

    /// <summary>
    /// Supplies the suggestions shown after the user types <c>@</c>.
    /// </summary>
    public Func<string, Task<IReadOnlyList<BitRichTextEditorMention>>>? OnMentionSearch { get; set; }

    /// <summary>
    /// When true, pasted content is inserted as plain text.
    /// </summary>
    public bool? PasteAsPlainText { get; set; }

    /// <summary>
    /// The placeholder value of the editor shown while it is empty.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Makes the editor readonly.
    /// </summary>
    public bool? ReadOnly { get; set; }

    /// <summary>
    /// Marks the editor as required.
    /// </summary>
    public bool? Required { get; set; }

    /// <summary>
    /// Lets the reader drag the bottom edge of the editing surface to change its height.
    /// </summary>
    public bool? Resizable { get; set; }

    /// <summary>
    /// Allowlist policy applied to all content.
    /// </summary>
    public BitRichTextEditorSanitizationPolicy? SanitizationPolicy { get; set; }

    /// <summary>
    /// Show the character/word count footer.
    /// </summary>
    public bool? ShowCount { get; set; }

    /// <summary>
    /// Whether a small formatting toolbar floats next to the current text selection.
    /// </summary>
    public bool? ShowQuickToolbar { get; set; }

    /// <summary>
    /// Whether the formatting toolbar is shown.
    /// </summary>
    public bool? ShowToolbar { get; set; }

    /// <summary>
    /// Replaces common text patterns with their typographic characters as they are typed.
    /// </summary>
    public bool? SmartTypography { get; set; }

    /// <summary>
    /// Whether the browser's native spell checking runs over the editor content.
    /// </summary>
    public bool? SpellCheck { get; set; }

    /// <summary>
    /// Keeps the toolbar in view while the page scrolls past a tall editor.
    /// </summary>
    public bool? StickyToolbar { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the rich text editor.
    /// </summary>
    public BitRichTextEditorClassStyles? Styles { get; set; }

    /// <summary>
    /// Which toolbar groups to display.
    /// </summary>
    public BitRichTextEditorToolbar? Toolbar { get; set; }

    /// <summary>
    /// Custom toolbar items and ordering.
    /// </summary>
    public BitRichTextEditorToolbarConfig? ToolbarConfig { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitRichTextEditor"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitRichTextEditor"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitRichTextEditor"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitRichTextEditor"/>.
    /// </remarks>
    /// <param name="bitRichTextEditor">
    /// The <see cref="BitRichTextEditor"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitRichTextEditor bitRichTextEditor)
    {
        if (bitRichTextEditor is null) return;

        UpdateBaseParameters(bitRichTextEditor);

        if (AutoLink.HasValue)
        {
            bitRichTextEditor.TakeFromCascade(nameof(AutoLink), AutoLink.Value, static r => r.AutoLink, static (r, v) => r.AutoLink = v);
        }

        if (Classes is not null)
        {
            bitRichTextEditor.TakeFromCascade(nameof(Classes), Classes, static r => r.Classes, static (r, v) => r.Classes = v);
        }

        if (ColorPalette is not null)
        {
            bitRichTextEditor.TakeFromCascade(nameof(ColorPalette), ColorPalette, static r => r.ColorPalette, static (r, v) => r.ColorPalette = v);
        }

        if (DebounceMs.HasValue)
        {
            bitRichTextEditor.TakeFromCascade(nameof(DebounceMs), DebounceMs.Value, static r => r.DebounceMs, static (r, v) => r.DebounceMs = v);
        }

        if (FontFamilies is not null)
        {
            bitRichTextEditor.TakeFromCascade(nameof(FontFamilies), FontFamilies, static r => r.FontFamilies, static (r, v) => r.FontFamilies = v);
        }

        if (FontSizes is not null)
        {
            bitRichTextEditor.TakeFromCascade(nameof(FontSizes), FontSizes, static r => r.FontSizes, static (r, v) => r.FontSizes = v);
        }

        if (Height.HasValue())
        {
            bitRichTextEditor.TakeFromCascade(nameof(Height), Height, static r => r.Height, static (r, v) => r.Height = v);
        }

        if (KeyboardShortcuts is not null)
        {
            bitRichTextEditor.TakeFromCascade(nameof(KeyboardShortcuts), KeyboardShortcuts, static r => r.KeyboardShortcuts, static (r, v) => r.KeyboardShortcuts = v);
        }

        if (Localizer is not null)
        {
            bitRichTextEditor.TakeFromCascade(nameof(Localizer), Localizer, static r => r.Localizer, static (r, v) => r.Localizer = v);
        }

        if (MaxHeight.HasValue())
        {
            bitRichTextEditor.TakeFromCascade(nameof(MaxHeight), MaxHeight, static r => r.MaxHeight, static (r, v) => r.MaxHeight = v);
        }

        if (MaxImageSize.HasValue)
        {
            bitRichTextEditor.TakeFromCascade(nameof(MaxImageSize), MaxImageSize.Value, static r => r.MaxImageSize, static (r, v) => r.MaxImageSize = v);
        }

        if (MaxLength.HasValue)
        {
            bitRichTextEditor.TakeFromCascade(nameof(MaxLength), MaxLength.Value, static r => r.MaxLength, static (r, v) => r.MaxLength = v);
        }

        if (OnImageUpload is not null)
        {
            bitRichTextEditor.TakeFromCascade(nameof(OnImageUpload), OnImageUpload, static r => r.OnImageUpload, static (r, v) => r.OnImageUpload = v);
        }

        if (OnMentionSearch is not null)
        {
            bitRichTextEditor.TakeFromCascade(nameof(OnMentionSearch), OnMentionSearch, static r => r.OnMentionSearch, static (r, v) => r.OnMentionSearch = v);
        }

        if (PasteAsPlainText.HasValue)
        {
            bitRichTextEditor.TakeFromCascade(nameof(PasteAsPlainText), PasteAsPlainText.Value, static r => r.PasteAsPlainText, static (r, v) => r.PasteAsPlainText = v);
        }

        if (Placeholder.HasValue())
        {
            bitRichTextEditor.TakeFromCascade(nameof(Placeholder), Placeholder, static r => r.Placeholder, static (r, v) => r.Placeholder = v);
        }

        if (ReadOnly.HasValue)
        {
            bitRichTextEditor.TakeFromCascade(nameof(ReadOnly), ReadOnly.Value, static r => r.ReadOnly, static (r, v) => r.ReadOnly = v);
        }

        if (Required.HasValue)
        {
            bitRichTextEditor.TakeFromCascade(nameof(Required), Required.Value, static r => r.Required, static (r, v) => r.Required = v);
        }

        if (Resizable.HasValue)
        {
            bitRichTextEditor.TakeFromCascade(nameof(Resizable), Resizable.Value, static r => r.Resizable, static (r, v) => r.Resizable = v);
        }

        if (SanitizationPolicy is not null)
        {
            bitRichTextEditor.TakeFromCascade(nameof(SanitizationPolicy), SanitizationPolicy, static r => r.SanitizationPolicy, static (r, v) => r.SanitizationPolicy = v);
        }

        if (ShowCount.HasValue)
        {
            bitRichTextEditor.TakeFromCascade(nameof(ShowCount), ShowCount.Value, static r => r.ShowCount, static (r, v) => r.ShowCount = v);
        }

        if (ShowQuickToolbar.HasValue)
        {
            bitRichTextEditor.TakeFromCascade(nameof(ShowQuickToolbar), ShowQuickToolbar.Value, static r => r.ShowQuickToolbar, static (r, v) => r.ShowQuickToolbar = v);
        }

        if (ShowToolbar.HasValue)
        {
            bitRichTextEditor.TakeFromCascade(nameof(ShowToolbar), ShowToolbar.Value, static r => r.ShowToolbar, static (r, v) => r.ShowToolbar = v);
        }

        if (SmartTypography.HasValue)
        {
            bitRichTextEditor.TakeFromCascade(nameof(SmartTypography), SmartTypography.Value, static r => r.SmartTypography, static (r, v) => r.SmartTypography = v);
        }

        if (SpellCheck.HasValue)
        {
            bitRichTextEditor.TakeFromCascade(nameof(SpellCheck), SpellCheck.Value, static r => r.SpellCheck, static (r, v) => r.SpellCheck = v);
        }

        if (StickyToolbar.HasValue)
        {
            bitRichTextEditor.TakeFromCascade(nameof(StickyToolbar), StickyToolbar.Value, static r => r.StickyToolbar, static (r, v) => r.StickyToolbar = v);
        }

        if (Styles is not null)
        {
            bitRichTextEditor.TakeFromCascade(nameof(Styles), Styles, static r => r.Styles, static (r, v) => r.Styles = v);
        }

        if (Toolbar.HasValue)
        {
            bitRichTextEditor.TakeFromCascade(nameof(Toolbar), Toolbar.Value, static r => r.Toolbar, static (r, v) => r.Toolbar = v);
        }

        if (ToolbarConfig is not null)
        {
            bitRichTextEditor.TakeFromCascade(nameof(ToolbarConfig), ToolbarConfig, static r => r.ToolbarConfig, static (r, v) => r.ToolbarConfig = v);
        }
    }
}
