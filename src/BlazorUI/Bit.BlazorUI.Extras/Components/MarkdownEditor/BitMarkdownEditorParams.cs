namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitMarkdownEditor"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the editors of an application agree on: their texts (which is where a localized app
/// sets them once), their toolbar, how they edit (the markdown spellings, the indent, the pairing and the tab
/// behavior), what the status bar shows, how the preview renders, how images are uploaded and how they are sized
/// and styled, with the read-only and required states every input carries (<see cref="BitInputBaseParams{TValue}"/>).
/// The value and its events, the label, the name, the draft key, the display mode and the full-screen state are left
/// out on purpose: they are what makes one editor the one it is.
/// </remarks>
public class BitMarkdownEditorParams : BitInputBaseParams<string?>, IBitComponentParams
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
    /// Keeps the toolbar on screen while the page scrolls past a tall editor.
    /// </summary>
    public bool? StickyToolbar { get; set; }

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

        UpdateInputBaseParameters(bitMarkdownEditor);

        if (AcceptedImageTypes is not null)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(AcceptedImageTypes), AcceptedImageTypes, static m => m.AcceptedImageTypes, static (m, v) => m.AcceptedImageTypes = v);
        }

        if (AutoHeight.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(AutoHeight), AutoHeight.Value, static m => m.AutoHeight, static (m, v) => m.AutoHeight = v);
        }

        if (AutoClosePairs.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(AutoClosePairs), AutoClosePairs.Value, static m => m.AutoClosePairs, static (m, v) => m.AutoClosePairs = v);
        }

        if (AutoPair.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(AutoPair), AutoPair.Value, static m => m.AutoPair, static (m, v) => m.AutoPair = v);
        }

        if (BoldStyle.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(BoldStyle), BoldStyle.Value, static m => m.BoldStyle, static (m, v) => m.BoldStyle = v);
        }

        if (BulletStyle.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(BulletStyle), BulletStyle.Value, static m => m.BulletStyle, static (m, v) => m.BulletStyle = v);
        }

        if (ChangeDebounceTime.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(ChangeDebounceTime), ChangeDebounceTime.Value, static m => m.ChangeDebounceTime, static (m, v) => m.ChangeDebounceTime = v);
        }

        if (Classes is not null)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(Classes), Classes, static m => m.Classes, static (m, v) => m.Classes = v);
        }

        if (DebounceTime.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(DebounceTime), DebounceTime.Value, static m => m.DebounceTime, static (m, v) => m.DebounceTime = v);
        }

        if (Height is not null)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(Height), Height, static m => m.Height, static (m, v) => m.Height = v);
        }

        if (IndentUnit is not null)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(IndentUnit), IndentUnit, static m => m.IndentUnit, static (m, v) => m.IndentUnit = v);
        }

        if (ItalicStyle.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(ItalicStyle), ItalicStyle.Value, static m => m.ItalicStyle, static (m, v) => m.ItalicStyle = v);
        }

        if (MaxHeight is not null)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(MaxHeight), MaxHeight, static m => m.MaxHeight, static (m, v) => m.MaxHeight = v);
        }

        if (MaxImageSize.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(MaxImageSize), MaxImageSize.Value, static m => m.MaxImageSize, static (m, v) => m.MaxImageSize = v);
        }

        if (MaxLength.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(MaxLength), MaxLength.Value, static m => m.MaxLength, static (m, v) => m.MaxLength = v);
        }

        if (MinHeight is not null)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(MinHeight), MinHeight, static m => m.MinHeight, static (m, v) => m.MinHeight = v);
        }

        if (OnImageUpload is not null)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(OnImageUpload), OnImageUpload, static m => m.OnImageUpload, static (m, v) => m.OnImageUpload = v);
        }

        if (Placeholder is not null)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(Placeholder), Placeholder, static m => m.Placeholder, static (m, v) => m.Placeholder = v);
        }

        if (PreviewPipeline is not null)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(PreviewPipeline), PreviewPipeline, static m => m.PreviewPipeline, static (m, v) => m.PreviewPipeline = v);
        }

        if (PreviewTemplate is not null)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(PreviewTemplate), PreviewTemplate, static m => m.PreviewTemplate, static (m, v) => m.PreviewTemplate = v);
        }

        if (Resizable.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(Resizable), Resizable.Value, static m => m.Resizable, static (m, v) => m.Resizable = v);
        }

        if (ShowCursorPosition.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(ShowCursorPosition), ShowCursorPosition.Value, static m => m.ShowCursorPosition, static (m, v) => m.ShowCursorPosition = v);
        }

        if (ShowReadingTime.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(ShowReadingTime), ShowReadingTime.Value, static m => m.ShowReadingTime, static (m, v) => m.ShowReadingTime = v);
        }

        if (ShowStatusBar.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(ShowStatusBar), ShowStatusBar.Value, static m => m.ShowStatusBar, static (m, v) => m.ShowStatusBar = v);
        }

        if (ShowToolbar.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(ShowToolbar), ShowToolbar.Value, static m => m.ShowToolbar, static (m, v) => m.ShowToolbar = v);
        }

        if (SpellCheck.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(SpellCheck), SpellCheck.Value, static m => m.SpellCheck, static (m, v) => m.SpellCheck = v);
        }

        if (StickyToolbar.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(StickyToolbar), StickyToolbar.Value, static m => m.StickyToolbar, static (m, v) => m.StickyToolbar = v);
        }

        if (Styles is not null)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(Styles), Styles, static m => m.Styles, static (m, v) => m.Styles = v);
        }

        if (SyncScroll.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(SyncScroll), SyncScroll.Value, static m => m.SyncScroll, static (m, v) => m.SyncScroll = v);
        }

        if (TabIndents.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(TabIndents), TabIndents.Value, static m => m.TabIndents, static (m, v) => m.TabIndents = v);
        }

        if (TableColumns.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(TableColumns), TableColumns.Value, static m => m.TableColumns, static (m, v) => m.TableColumns = v);
        }

        if (TableRows.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(TableRows), TableRows.Value, static m => m.TableRows, static (m, v) => m.TableRows = v);
        }

        if (Texts is not null)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(Texts), Texts, static m => m.Texts, static (m, v) => m.Texts = v);
        }

        if (Toolbar is not null)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(Toolbar), Toolbar, static m => m.Toolbar, static (m, v) => m.Toolbar = v);
        }

        if (WordsPerMinute.HasValue)
        {
            bitMarkdownEditor.TakeFromCascade(nameof(WordsPerMinute), WordsPerMinute.Value, static m => m.WordsPerMinute, static (m, v) => m.WordsPerMinute = v);
        }
    }
}
