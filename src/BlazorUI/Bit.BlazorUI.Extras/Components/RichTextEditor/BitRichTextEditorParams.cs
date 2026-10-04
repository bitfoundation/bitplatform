namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitRichTextEditor"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the editors of an application agree on: their localizer (which is where a localized app
/// sets it once), their toolbar, their sanitization policy, how they take pasted content and uploaded images, their
/// typing aids, their size and their look. The value and its events, the label and the auto-focus are left out on
/// purpose: they are what makes one editor the one it is.
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

        if (AutoLink.HasValue && bitRichTextEditor.HasNotBeenSet(nameof(AutoLink)))
        {
            bitRichTextEditor.AutoLink = AutoLink.Value;
        }

        if (Classes is not null && bitRichTextEditor.HasNotBeenSet(nameof(Classes)) && bitRichTextEditor.Classes != Classes)
        {
            bitRichTextEditor.Classes = Classes;

            bitRichTextEditor.ClassBuilder.Reset();
        }

        if (DebounceMs.HasValue && bitRichTextEditor.HasNotBeenSet(nameof(DebounceMs)))
        {
            bitRichTextEditor.DebounceMs = DebounceMs.Value;
        }

        if (FontFamilies is not null && bitRichTextEditor.HasNotBeenSet(nameof(FontFamilies)))
        {
            bitRichTextEditor.FontFamilies = FontFamilies;
        }

        if (FontSizes is not null && bitRichTextEditor.HasNotBeenSet(nameof(FontSizes)))
        {
            bitRichTextEditor.FontSizes = FontSizes;
        }

        if (Height.HasValue() && bitRichTextEditor.HasNotBeenSet(nameof(Height)) && bitRichTextEditor.Height != Height)
        {
            bitRichTextEditor.Height = Height;

            bitRichTextEditor.StyleBuilder.Reset();
        }

        if (KeyboardShortcuts is not null && bitRichTextEditor.HasNotBeenSet(nameof(KeyboardShortcuts)))
        {
            bitRichTextEditor.KeyboardShortcuts = KeyboardShortcuts;
        }

        if (Localizer is not null && bitRichTextEditor.HasNotBeenSet(nameof(Localizer)))
        {
            bitRichTextEditor.Localizer = Localizer;
        }

        if (MaxHeight.HasValue() && bitRichTextEditor.HasNotBeenSet(nameof(MaxHeight)) && bitRichTextEditor.MaxHeight != MaxHeight)
        {
            bitRichTextEditor.MaxHeight = MaxHeight;

            bitRichTextEditor.StyleBuilder.Reset();
        }

        if (MaxLength.HasValue && bitRichTextEditor.HasNotBeenSet(nameof(MaxLength)))
        {
            bitRichTextEditor.MaxLength = MaxLength.Value;
        }

        if (OnImageUpload is not null && bitRichTextEditor.HasNotBeenSet(nameof(OnImageUpload)))
        {
            bitRichTextEditor.OnImageUpload = OnImageUpload;
        }

        if (OnMentionSearch is not null && bitRichTextEditor.HasNotBeenSet(nameof(OnMentionSearch)))
        {
            bitRichTextEditor.OnMentionSearch = OnMentionSearch;
        }

        if (PasteAsPlainText.HasValue && bitRichTextEditor.HasNotBeenSet(nameof(PasteAsPlainText)))
        {
            bitRichTextEditor.PasteAsPlainText = PasteAsPlainText.Value;
        }

        if (Placeholder.HasValue() && bitRichTextEditor.HasNotBeenSet(nameof(Placeholder)))
        {
            bitRichTextEditor.Placeholder = Placeholder;
        }

        if (ReadOnly.HasValue && bitRichTextEditor.HasNotBeenSet(nameof(ReadOnly)) && bitRichTextEditor.ReadOnly != ReadOnly)
        {
            bitRichTextEditor.ReadOnly = ReadOnly.Value;

            bitRichTextEditor.ClassBuilder.Reset();
        }

        if (Required.HasValue && bitRichTextEditor.HasNotBeenSet(nameof(Required)))
        {
            bitRichTextEditor.Required = Required.Value;
        }

        if (Resizable.HasValue && bitRichTextEditor.HasNotBeenSet(nameof(Resizable)))
        {
            bitRichTextEditor.Resizable = Resizable.Value;
        }

        if (SanitizationPolicy is not null && bitRichTextEditor.HasNotBeenSet(nameof(SanitizationPolicy)))
        {
            bitRichTextEditor.SanitizationPolicy = SanitizationPolicy;
        }

        if (ShowCount.HasValue && bitRichTextEditor.HasNotBeenSet(nameof(ShowCount)))
        {
            bitRichTextEditor.ShowCount = ShowCount.Value;
        }

        if (ShowQuickToolbar.HasValue && bitRichTextEditor.HasNotBeenSet(nameof(ShowQuickToolbar)))
        {
            bitRichTextEditor.ShowQuickToolbar = ShowQuickToolbar.Value;
        }

        if (ShowToolbar.HasValue && bitRichTextEditor.HasNotBeenSet(nameof(ShowToolbar)))
        {
            bitRichTextEditor.ShowToolbar = ShowToolbar.Value;
        }

        if (SmartTypography.HasValue && bitRichTextEditor.HasNotBeenSet(nameof(SmartTypography)))
        {
            bitRichTextEditor.SmartTypography = SmartTypography.Value;
        }

        if (SpellCheck.HasValue && bitRichTextEditor.HasNotBeenSet(nameof(SpellCheck)))
        {
            bitRichTextEditor.SpellCheck = SpellCheck.Value;
        }

        if (StickyToolbar.HasValue && bitRichTextEditor.HasNotBeenSet(nameof(StickyToolbar)) && bitRichTextEditor.StickyToolbar != StickyToolbar)
        {
            bitRichTextEditor.StickyToolbar = StickyToolbar.Value;

            bitRichTextEditor.ClassBuilder.Reset();
        }

        if (Styles is not null && bitRichTextEditor.HasNotBeenSet(nameof(Styles)) && bitRichTextEditor.Styles != Styles)
        {
            bitRichTextEditor.Styles = Styles;

            bitRichTextEditor.StyleBuilder.Reset();
        }

        if (Toolbar.HasValue && bitRichTextEditor.HasNotBeenSet(nameof(Toolbar)))
        {
            bitRichTextEditor.Toolbar = Toolbar.Value;
        }

        if (ToolbarConfig is not null && bitRichTextEditor.HasNotBeenSet(nameof(ToolbarConfig)))
        {
            bitRichTextEditor.ToolbarConfig = ToolbarConfig;
        }
    }
}
