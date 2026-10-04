namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitMarkdownEditor"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the editors of an application agree on: their texts (which is where a localized app
/// sets them once), their toolbar, how they edit (the markdown spellings, the indent, the pairing and the tab
/// behavior), what the status bar shows, how the preview renders, how images are uploaded and how they are sized
/// and styled. The value and its events, the label, the name, the draft key, the display mode and the full-screen
/// state are left out on purpose: they are what makes one editor the one it is.
/// </remarks>
public class BitMarkdownEditorParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitMarkdownEditor"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitMarkdownEditor value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitMarkdownEditor)}";



    public string Name => ParamName;



    /// <summary>
    /// The image types the paste/drop/picker upload accepts, as a comma separated list of MIME types or extensions.
    /// </summary>
    public string? AcceptedImageTypes { get; set; }

    /// <summary>
    /// Grows the editor with its content instead of scrolling it, between MinHeight and MaxHeight.
    /// </summary>
    public bool? AutoHeight { get; set; }

    /// <summary>
    /// Closes a bracket or a quote as it is typed with nothing selected.
    /// </summary>
    public bool? AutoClosePairs { get; set; }

    /// <summary>
    /// Wraps the current selection when a pairing character is typed over it.
    /// </summary>
    public bool? AutoPair { get; set; }

    /// <summary>
    /// The characters the Bold command wraps a selection in.
    /// </summary>
    public BitMarkdownEditorEmphasisStyle? BoldStyle { get; set; }

    /// <summary>
    /// The character an unordered or task list item starts with.
    /// </summary>
    public BitMarkdownEditorBulletStyle? BulletStyle { get; set; }

    /// <summary>
    /// The debounce window (in milliseconds) before the typed value is pushed to .NET.
    /// </summary>
    public int? ChangeDebounceTime { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the editor.
    /// </summary>
    public BitMarkdownEditorClassStyles? Classes { get; set; }

    /// <summary>
    /// The debounce window (in milliseconds) before the preview re-renders while typing.
    /// </summary>
    public int? DebounceTime { get; set; }

    /// <summary>
    /// The height of the editor (any CSS length).
    /// </summary>
    public string? Height { get; set; }

    /// <summary>
    /// The string inserted per indent level.
    /// </summary>
    public string? IndentUnit { get; set; }

    /// <summary>
    /// The character the Italic command wraps a selection in.
    /// </summary>
    public BitMarkdownEditorEmphasisStyle? ItalicStyle { get; set; }

    /// <summary>
    /// The largest height the editor may grow to (any CSS length).
    /// </summary>
    public string? MaxHeight { get; set; }

    /// <summary>
    /// The largest pasted, dropped or picked image (in bytes) the editor uploads.
    /// </summary>
    public long? MaxImageSize { get; set; }

    /// <summary>
    /// The maximum number of characters the editor accepts.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// The smallest height the editor may shrink to (any CSS length).
    /// </summary>
    public string? MinHeight { get; set; }

    /// <summary>
    /// The handler that uploads a pasted, dropped or picked image and returns the URL to reference it by.
    /// </summary>
    public Func<BitMarkdownEditorImageUploadInfo, Task<string?>>? OnImageUpload { get; set; }

    /// <summary>
    /// The placeholder text shown when the editor is empty.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// The markdown processing pipeline used by the preview pane.
    /// </summary>
    public BitMarkdownPipeline? PreviewPipeline { get; set; }

    /// <summary>
    /// A custom template to render the preview pane.
    /// </summary>
    public RenderFragment<string>? PreviewTemplate { get; set; }

    /// <summary>
    /// Makes the editor read-only.
    /// </summary>
    public bool? ReadOnly { get; set; }

    /// <summary>
    /// Lets the user drag the bottom edge of the editor to change its height.
    /// </summary>
    public bool? Resizable { get; set; }

    /// <summary>
    /// Whether the status bar reports the caret's line and column.
    /// </summary>
    public bool? ShowCursorPosition { get; set; }

    /// <summary>
    /// Whether the estimated reading time is shown in the status bar.
    /// </summary>
    public bool? ShowReadingTime { get; set; }

    /// <summary>
    /// Whether the status bar is shown.
    /// </summary>
    public bool? ShowStatusBar { get; set; }

    /// <summary>
    /// Whether the formatting toolbar is shown.
    /// </summary>
    public bool? ShowToolbar { get; set; }

    /// <summary>
    /// Enables the native browser spell checking in the textarea.
    /// </summary>
    public bool? SpellCheck { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the editor.
    /// </summary>
    public BitMarkdownEditorClassStyles? Styles { get; set; }

    /// <summary>
    /// Synchronizes scrolling between the editor and preview panes in split mode.
    /// </summary>
    public bool? SyncScroll { get; set; }

    /// <summary>
    /// Whether the Tab key indents the selection instead of moving the focus to the next control.
    /// </summary>
    public bool? TabIndents { get; set; }

    /// <summary>
    /// How many columns the toolbar's table command inserts.
    /// </summary>
    public int? TableColumns { get; set; }

    /// <summary>
    /// How many body rows the toolbar's table command inserts.
    /// </summary>
    public int? TableRows { get; set; }

    /// <summary>
    /// The localized strings of the editor UI.
    /// </summary>
    public BitMarkdownEditorTexts? Texts { get; set; }

    /// <summary>
    /// A custom toolbar layout.
    /// </summary>
    public IReadOnlyList<BitMarkdownEditorToolbarItem>? Toolbar { get; set; }

    /// <summary>
    /// Words-per-minute used to estimate reading time.
    /// </summary>
    public int? WordsPerMinute { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitMarkdownEditor"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitMarkdownEditor"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitMarkdownEditor"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitMarkdownEditor"/>.
    /// </remarks>
    /// <param name="bitMarkdownEditor">
    /// The <see cref="BitMarkdownEditor"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitMarkdownEditor bitMarkdownEditor)
    {
        if (bitMarkdownEditor is null) return;

        UpdateBaseParameters(bitMarkdownEditor);

        if (AcceptedImageTypes is not null && bitMarkdownEditor.HasNotBeenSet(nameof(AcceptedImageTypes)))
        {
            bitMarkdownEditor.AcceptedImageTypes = AcceptedImageTypes;
        }

        if (AutoHeight.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(AutoHeight)) && bitMarkdownEditor.AutoHeight != AutoHeight.Value)
        {
            bitMarkdownEditor.AutoHeight = AutoHeight.Value;

            bitMarkdownEditor.ClassBuilder.Reset();
        }

        if (AutoClosePairs.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(AutoClosePairs)))
        {
            bitMarkdownEditor.AutoClosePairs = AutoClosePairs.Value;
        }

        if (AutoPair.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(AutoPair)))
        {
            bitMarkdownEditor.AutoPair = AutoPair.Value;
        }

        if (BoldStyle.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(BoldStyle)))
        {
            bitMarkdownEditor.BoldStyle = BoldStyle.Value;
        }

        if (BulletStyle.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(BulletStyle)))
        {
            bitMarkdownEditor.BulletStyle = BulletStyle.Value;
        }

        if (ChangeDebounceTime.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(ChangeDebounceTime)))
        {
            bitMarkdownEditor.ChangeDebounceTime = ChangeDebounceTime.Value;
        }

        // This runs on every render of every editor under the BitParams, so the values that drive the class or
        // the style of the root are only assigned - and the builder only reset - when they differ from the ones
        // the editor already holds: an unchanged one would rebuild both strings on every render for nothing.
        if (Classes is not null && bitMarkdownEditor.HasNotBeenSet(nameof(Classes)) && ReferenceEquals(bitMarkdownEditor.Classes, Classes) is false)
        {
            bitMarkdownEditor.Classes = Classes;

            bitMarkdownEditor.ClassBuilder.Reset();
        }

        if (DebounceTime.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(DebounceTime)))
        {
            bitMarkdownEditor.DebounceTime = DebounceTime.Value;
        }

        if (Height is not null && bitMarkdownEditor.HasNotBeenSet(nameof(Height)) && bitMarkdownEditor.Height != Height)
        {
            bitMarkdownEditor.Height = Height;

            bitMarkdownEditor.StyleBuilder.Reset();
        }

        if (IndentUnit is not null && bitMarkdownEditor.HasNotBeenSet(nameof(IndentUnit)))
        {
            bitMarkdownEditor.IndentUnit = IndentUnit;
        }

        if (ItalicStyle.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(ItalicStyle)))
        {
            bitMarkdownEditor.ItalicStyle = ItalicStyle.Value;
        }

        if (MaxHeight is not null && bitMarkdownEditor.HasNotBeenSet(nameof(MaxHeight)) && bitMarkdownEditor.MaxHeight != MaxHeight)
        {
            bitMarkdownEditor.MaxHeight = MaxHeight;

            bitMarkdownEditor.StyleBuilder.Reset();
        }

        if (MaxImageSize.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(MaxImageSize)))
        {
            bitMarkdownEditor.MaxImageSize = MaxImageSize.Value;
        }

        if (MaxLength.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(MaxLength)))
        {
            bitMarkdownEditor.MaxLength = MaxLength.Value;
        }

        if (MinHeight is not null && bitMarkdownEditor.HasNotBeenSet(nameof(MinHeight)) && bitMarkdownEditor.MinHeight != MinHeight)
        {
            bitMarkdownEditor.MinHeight = MinHeight;

            bitMarkdownEditor.StyleBuilder.Reset();
        }

        if (OnImageUpload is not null && bitMarkdownEditor.HasNotBeenSet(nameof(OnImageUpload)))
        {
            bitMarkdownEditor.OnImageUpload = OnImageUpload;
        }

        if (Placeholder is not null && bitMarkdownEditor.HasNotBeenSet(nameof(Placeholder)))
        {
            bitMarkdownEditor.Placeholder = Placeholder;
        }

        if (PreviewPipeline is not null && bitMarkdownEditor.HasNotBeenSet(nameof(PreviewPipeline)))
        {
            bitMarkdownEditor.PreviewPipeline = PreviewPipeline;
        }

        if (PreviewTemplate is not null && bitMarkdownEditor.HasNotBeenSet(nameof(PreviewTemplate)))
        {
            bitMarkdownEditor.PreviewTemplate = PreviewTemplate;
        }

        if (ReadOnly.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(ReadOnly)))
        {
            bitMarkdownEditor.ReadOnly = ReadOnly.Value;
        }

        if (Resizable.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(Resizable)) && bitMarkdownEditor.Resizable != Resizable.Value)
        {
            bitMarkdownEditor.Resizable = Resizable.Value;

            bitMarkdownEditor.ClassBuilder.Reset();
        }

        if (ShowCursorPosition.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(ShowCursorPosition)))
        {
            bitMarkdownEditor.ShowCursorPosition = ShowCursorPosition.Value;
        }

        if (ShowReadingTime.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(ShowReadingTime)))
        {
            bitMarkdownEditor.ShowReadingTime = ShowReadingTime.Value;
        }

        if (ShowStatusBar.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(ShowStatusBar)))
        {
            bitMarkdownEditor.ShowStatusBar = ShowStatusBar.Value;
        }

        if (ShowToolbar.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(ShowToolbar)))
        {
            bitMarkdownEditor.ShowToolbar = ShowToolbar.Value;
        }

        if (SpellCheck.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(SpellCheck)))
        {
            bitMarkdownEditor.SpellCheck = SpellCheck.Value;
        }

        if (Styles is not null && bitMarkdownEditor.HasNotBeenSet(nameof(Styles)) && ReferenceEquals(bitMarkdownEditor.Styles, Styles) is false)
        {
            bitMarkdownEditor.Styles = Styles;

            bitMarkdownEditor.StyleBuilder.Reset();
        }

        if (SyncScroll.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(SyncScroll)))
        {
            bitMarkdownEditor.SyncScroll = SyncScroll.Value;
        }

        if (TabIndents.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(TabIndents)))
        {
            bitMarkdownEditor.TabIndents = TabIndents.Value;
        }

        if (TableColumns.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(TableColumns)))
        {
            bitMarkdownEditor.TableColumns = TableColumns.Value;
        }

        if (TableRows.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(TableRows)))
        {
            bitMarkdownEditor.TableRows = TableRows.Value;
        }

        if (Texts is not null && bitMarkdownEditor.HasNotBeenSet(nameof(Texts)))
        {
            bitMarkdownEditor.Texts = Texts;
        }

        if (Toolbar is not null && bitMarkdownEditor.HasNotBeenSet(nameof(Toolbar)))
        {
            bitMarkdownEditor.Toolbar = Toolbar;
        }

        if (WordsPerMinute.HasValue && bitMarkdownEditor.HasNotBeenSet(nameof(WordsPerMinute)))
        {
            bitMarkdownEditor.WordsPerMinute = WordsPerMinute.Value;
        }
    }
}
