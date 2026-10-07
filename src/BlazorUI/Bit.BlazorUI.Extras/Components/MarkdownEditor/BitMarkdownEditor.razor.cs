using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// BitMarkdownEditor is a native Blazor markdown editor with a customizable toolbar, keyboard
/// shortcuts, smart list handling, undo/redo history and a live GitHub-flavored preview powered
/// by the <see cref="BitMarkdownViewer"/>. All markdown transformations happen in C#; a small
/// JS-interop script handles textarea selection control, key interception and the undo/redo
/// history (coalescing rapid typing into single steps).
/// </summary>
public partial class BitMarkdownEditor : BitInputBase<string?>
{
    private string _value = string.Empty;
    // What the textarea is rendered with: the value as it stood until the first render, frozen after it (see the
    // textarea in the markup).
    private string _seedValue = string.Empty;
    private string _previewValue = string.Empty;
    private bool _showHelp;
    private bool _focusHelp;
    private bool _showFind;
    private bool _focusFind;
    private bool _matchCase;
    private string _findText = string.Empty;
    private string _replaceText = string.Empty;
    private BitMarkdownEditorFindResult? _findResult;
    private bool _canUndo;
    private bool _canRedo;
    private int _caretLine = 1;
    private int _caretColumn = 1;
    private int _selectedLength;
    private bool _internalValueChange;
    private bool _valueChanged;
    private bool _scrollLocked;
    private string? _lastConfig;
    private IReadOnlyCollection<BitMarkdownEditorCommand> _activeFormats = [];
    private ElementReference _helpRef = default!;
    private ElementReference _helpCloseRef = default!;
    private ElementReference _findRef = default!;
    private ElementReference _previewRef = default!;
    private CancellationTokenSource? _debounceCts;
    private DotNetObjectReference<BitMarkdownEditor>? _dotnetObj;
    private BitMarkdownEditorMode? _renderedMode;
    private string? _announcement;



    public BitMarkdownEditor()
    {
        // The textarea is uncontrolled (the script owns its value to keep the caret), so a Value set from outside
        // has to be pushed into it; the one the editor reports itself is already there.
        OnValueChanged += (_, _) =>
        {
            if (_internalValueChange is false) _valueChanged = true;
        };
    }



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the markdown editor component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings (the texts of a localized app, its toolbar and
    /// its image upload handler, above all) to be applied to multiple editors through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitMarkdownEditorParams.ParamName)]
    public BitMarkdownEditorParams? CascadingParameters { get; set; }



    /// <summary>
    /// Moves the keyboard focus into the editor as soon as it is initialized.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// Grows the editor with its content instead of scrolling it, from <see cref="MinHeight"/> up to
    /// <see cref="MaxHeight"/> (past which it scrolls again), the way a comment box does. <see cref="Height"/>
    /// and <see cref="Resizable"/> have no effect while it is on, and full-screen mode still fills the viewport.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool AutoHeight { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the editor.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitMarkdownEditorClassStyles? Classes { get; set; }

    /// <summary>
    /// A stable key under which the editor content is autosaved to the browser's
    /// localStorage. When set, a draft is written as the user types and restored on
    /// initialization if no <see cref="Value"/>/<see cref="DefaultValue"/> is supplied.
    /// Call <see cref="ClearDraft"/> to drop the draft once the content is persisted
    /// elsewhere. The key is used verbatim, so prefix it to keep it unique per document.
    /// </summary>
    [Parameter] public string? AutoSaveId { get; set; }

    /// <summary>
    /// Enables wrapping the current selection when a pairing character
    /// (for example <c>*</c>, <c>`</c>, <c>[</c>) is typed. Defaults to true.
    /// </summary>
    [Parameter] public bool AutoPair { get; set; } = true;

    /// <summary>
    /// Closes a bracket or a quote as it is typed with nothing selected: the closing half is
    /// inserted after the caret, typing that closing character steps over it instead of
    /// doubling it, and Backspace between the two halves removes both. Only
    /// <c>(</c>, <c>[</c>, <c>{</c>, <c>`</c> and <c>"</c> take part - a <c>*</c> or a
    /// <c>-</c> opens a list far more often than it opens a span. Defaults to false.
    /// </summary>
    [Parameter] public bool AutoClosePairs { get; set; }

    /// <summary>
    /// The characters the Bold command wraps a selection in: <c>**</c> (the default) or <c>__</c>.
    /// </summary>
    [Parameter] public BitMarkdownEditorEmphasisStyle BoldStyle { get; set; }

    /// <summary>
    /// The character the Italic command wraps a selection in: <c>*</c> (the default) or <c>_</c>.
    /// </summary>
    [Parameter] public BitMarkdownEditorEmphasisStyle ItalicStyle { get; set; }

    /// <summary>
    /// The character an unordered or task list item starts with: <c>-</c> (the default),
    /// <c>*</c> or <c>+</c>.
    /// </summary>
    [Parameter] public BitMarkdownEditorBulletStyle BulletStyle { get; set; }

    /// <summary>
    /// The debounce window (in milliseconds) before the preview re-renders while typing.
    /// </summary>
    [Parameter] public int DebounceTime { get; set; } = 150;

    /// <summary>
    /// The debounce window (in milliseconds) before the typed value is pushed to .NET.
    /// Increase it to reduce interop traffic on Blazor Server. Defaults to 0 (immediate).
    /// </summary>
    [Parameter] public int ChangeDebounceTime { get; set; }

    /// <summary>
    /// A hint rendered below the panes (the markdown flavor accepted, what the text is for) and tied to the
    /// textarea through aria-describedby, so assistive tech reads it out with the field.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// A custom template for the description of the editor, replacing <see cref="Description"/>.
    /// </summary>
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// Whether the editor is rendered in full-screen mode.
    /// </summary>
    [Parameter, TwoWayBound, ResetClassBuilder, CallOnSetAsync(nameof(OnFullScreenSet))]
    public bool FullScreen { get; set; }

    /// <summary>
    /// The height of the editor (any CSS length). Ignored in full-screen mode.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Height { get; set; }

    /// <summary>
    /// The smallest height the editor may shrink to (any CSS length), which is also the floor
    /// of the <see cref="Resizable"/> drag handle. Ignored in full-screen mode.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? MinHeight { get; set; }

    /// <summary>
    /// The largest height the editor may grow to (any CSS length), which is also the ceiling
    /// of the <see cref="Resizable"/> drag handle. Ignored in full-screen mode.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? MaxHeight { get; set; }

    /// <summary>
    /// A visible label rendered above the toolbar and tied to the textarea, so clicking it
    /// moves the focus into the editor and assistive tech announces it as the field's name.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// A custom template for the label of the editor, replacing <see cref="Label"/>.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The string inserted per indent level (default: two spaces).
    /// </summary>
    [Parameter] public string IndentUnit { get; set; } = "  ";

    /// <summary>
    /// The maximum number of characters the editor accepts. Null (the default) leaves the
    /// length unlimited; when set, the status bar counter shows the limit alongside the count.
    /// A command, a replace-all run or an image upload that would overrun the limit is refused
    /// whole rather than applied and paid for by cutting the end of the document off; text
    /// being pasted or inserted is cut down to what still fits, the way the browser cuts it.
    /// </summary>
    [Parameter] public int? MaxLength { get; set; }

    /// <summary>
    /// The largest pasted, dropped or picked image (in bytes) the editor uploads. A bigger file is
    /// refused before its bytes are read, and reported through <see cref="OnImageRejected"/>.
    /// Null (the default) leaves the size unlimited.
    /// </summary>
    [Parameter] public long? MaxImageSize { get; set; }

    /// <summary>
    /// The image types the paste/drop/picker upload accepts, as a comma separated list of MIME types
    /// or extensions (the shape of an input's accept attribute, e.g. <c>image/png,image/jpeg</c>
    /// or <c>.png,.jpg</c>). Null (the default) accepts every image type.
    /// </summary>
    [Parameter] public string? AcceptedImageTypes { get; set; }

    /// <summary>
    /// Determines which panes of the editor are visible (edit / split / preview).
    /// </summary>
    [Parameter, TwoWayBound]
    public BitMarkdownEditorMode Mode { get; set; } = BitMarkdownEditorMode.Split;

    /// <summary>
    /// A handler that uploads a pasted, dropped or picked image and returns the URL to reference
    /// it by. When set, the editor enables clipboard-paste and drag-and-drop image upload and shows
    /// the toolbar's upload button, the keyboard's way to pick a file from the disk: a placeholder
    /// is inserted immediately and replaced with the returned URL once the handler completes
    /// (returning null cancels the insertion). When null, image upload is disabled and only the
    /// manual image command is available.
    /// </summary>
    [Parameter] public Func<BitMarkdownEditorImageUploadInfo, Task<string?>>? OnImageUpload { get; set; }

    /// <summary>
    /// Callback for when the editor loses the keyboard focus.
    /// </summary>
    [Parameter] public EventCallback OnBlur { get; set; }

    /// <summary>
    /// Callback for Ctrl/Cmd+Enter pressed inside the editor, carrying the current value.
    /// The shortcut is only captured while this callback is set, so a form that binds it
    /// elsewhere keeps its own submit key otherwise.
    /// </summary>
    [Parameter] public EventCallback<string?> OnSubmit { get; set; }

    /// <summary>
    /// Callback for when the editor receives the keyboard focus.
    /// </summary>
    [Parameter] public EventCallback OnFocus { get; set; }

    /// <summary>
    /// Callback for an autosaved draft restored at initialization, carrying the restored text.
    /// It only fires when <see cref="AutoSaveId"/> is set and the draft was actually used, so
    /// the app can say so and offer to drop it rather than guessing where the text came from.
    /// </summary>
    [Parameter] public EventCallback<string?> OnDraftRestored { get; set; }

    /// <summary>
    /// Callback for a pasted or dropped image the editor refused to upload because of
    /// <see cref="MaxImageSize"/> or <see cref="AcceptedImageTypes"/>, so the app can tell
    /// the user why nothing was inserted.
    /// </summary>
    [Parameter] public EventCallback<BitMarkdownEditorImageRejection> OnImageRejected { get; set; }

    /// <summary>
    /// The placeholder text shown when the editor is empty.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// How many columns the toolbar's table command inserts. Defaults to 2.
    /// </summary>
    [Parameter] public int TableColumns { get; set; } = 2;

    /// <summary>
    /// How many body rows the toolbar's table command inserts, beside its header row. Defaults to 1.
    /// </summary>
    [Parameter] public int TableRows { get; set; } = 1;

    /// <summary>
    /// The markdown processing pipeline used by the preview pane.
    /// Defaults to <see cref="BitMarkdownPipelines.GitHub"/>.
    /// </summary>
    [Parameter] public BitMarkdownPipeline? PreviewPipeline { get; set; }

    /// <summary>
    /// A custom template to render the preview pane. Receives the current markdown value
    /// and replaces the built-in <see cref="BitMarkdownViewer"/> based preview.
    /// </summary>
    [Parameter] public RenderFragment<string>? PreviewTemplate { get; set; }

    /// <summary>
    /// Lets the user drag the bottom edge of the editor to change its height.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Resizable { get; set; }

    /// <summary>
    /// Whether the word/character status bar is shown.
    /// </summary>
    [Parameter] public bool ShowStatusBar { get; set; } = true;

    /// <summary>
    /// Whether the estimated reading time is shown in the status bar.
    /// </summary>
    [Parameter] public bool ShowReadingTime { get; set; }

    /// <summary>
    /// Whether the status bar reports the caret's line and column, and how much text is
    /// selected. Reporting it costs a (debounced) round trip per caret move, so it is off
    /// by default.
    /// </summary>
    [Parameter] public bool ShowCursorPosition { get; set; }

    /// <summary>
    /// Whether the formatting toolbar is shown.
    /// </summary>
    [Parameter] public bool ShowToolbar { get; set; } = true;

    /// <summary>
    /// Synchronizes scrolling between the editor and preview panes in split mode.
    /// Defaults to true.
    /// </summary>
    [Parameter] public bool SyncScroll { get; set; } = true;

    /// <summary>
    /// Whether the Tab key indents the selection instead of moving the focus to the next
    /// control. Defaults to true; pressing Escape first always lets a single Tab out, so the
    /// editor never traps the keyboard either way.
    /// </summary>
    [Parameter] public bool TabIndents { get; set; } = true;

    /// <summary>
    /// Words-per-minute used to estimate reading time. Defaults to 200.
    /// </summary>
    [Parameter] public int WordsPerMinute { get; set; } = 200;

    /// <summary>
    /// Enables the native browser spell checking in the textarea.
    /// </summary>
    [Parameter] public bool SpellCheck { get; set; } = true;

    /// <summary>
    /// Keeps the toolbar on screen while the page scrolls past a tall editor (an <see cref="AutoHeight"/> one,
    /// above all), pinned <c>--bit-MarkdownEditor-toolbar-sticky-offset</c> below the top of the scrolling
    /// ancestor - the height of a fixed app header, for one.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool StickyToolbar { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the editor.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitMarkdownEditorClassStyles? Styles { get; set; }

    /// <summary>
    /// The localized strings of the editor UI (status bar, help panel, aria labels).
    /// Defaults to English; override individual properties on a <see cref="BitMarkdownEditorTexts"/>
    /// instance to localize them.
    /// </summary>
    [Parameter] public BitMarkdownEditorTexts? Texts { get; set; }

    /// <summary>
    /// A custom toolbar layout. Defaults to <see cref="BitMarkdownEditorToolbar.Default"/> when null.
    /// </summary>
    [Parameter] public IReadOnlyList<BitMarkdownEditorToolbarItem>? Toolbar { get; set; }



    /// <summary>
    /// True when there is at least one change that can be undone.
    /// </summary>
    public bool CanUndo => _canUndo;

    /// <summary>
    /// True when there is at least one undone change that can be redone.
    /// </summary>
    public bool CanRedo => _canRedo;

    /// <summary>
    /// Returns the current value of the editor directly from the textarea.
    /// </summary>
    public async ValueTask<string> GetValue()
    {
        if (IsRendered is false) return _value;

        return await _js.BitMarkdownEditorGetValue(_Id);
    }

    /// <summary>
    /// Runs a specific command on the current selection of the editor.
    /// </summary>
    public async ValueTask Run(BitMarkdownEditorCommand command)
    {
        if (IsRendered is false || ReadOnly || Disabled) return;

        await _js.BitMarkdownEditorRun(_Id, command.ToString());
    }

    /// <summary>
    /// Inserts the given markdown text at the current selection (replacing it) as a single
    /// undo step. Useful for building custom toolbar buttons that emit their own markdown.
    /// </summary>
    public async ValueTask Insert(string text)
    {
        if (IsRendered is false || ReadOnly || Disabled) return;

        await _js.BitMarkdownEditorInsert(_Id, text ?? string.Empty);
    }

    /// <summary>
    /// Replaces occurrences of <paramref name="search"/> with <paramref name="replacement"/>.
    /// With <paramref name="all"/> off, the first occurrence at or after the caret is replaced
    /// (wrapping to the top), so repeated calls walk the document.
    /// Returns the number of replacements made.
    /// </summary>
    public async ValueTask<int> Replace(string search, string replacement, bool all = true, bool matchCase = true)
    {
        if (IsRendered is false || ReadOnly || Disabled || string.IsNullOrEmpty(search)) return 0;

        return await _js.BitMarkdownEditorReplaceAll(_Id, search, replacement ?? string.Empty, all, matchCase);
    }

    /// <summary>
    /// Selects the next occurrence of <paramref name="search"/> after the caret, wrapping
    /// around the end of the document. Returns how many occurrences exist and which one is
    /// now selected.
    /// </summary>
    public async ValueTask<BitMarkdownEditorFindResult> FindNext(string search, bool matchCase = false)
    {
        if (IsRendered is false || string.IsNullOrEmpty(search)) return BitMarkdownEditorFindResult.None;

        return await _js.BitMarkdownEditorFind(_Id, search, matchCase, backwards: false, focusEditor: true);
    }

    /// <summary>
    /// Selects the occurrence of <paramref name="search"/> before the caret, wrapping around
    /// the start of the document. Returns how many occurrences exist and which one is now
    /// selected.
    /// </summary>
    public async ValueTask<BitMarkdownEditorFindResult> FindPrevious(string search, bool matchCase = false)
    {
        if (IsRendered is false || string.IsNullOrEmpty(search)) return BitMarkdownEditorFindResult.None;

        return await _js.BitMarkdownEditorFind(_Id, search, matchCase, backwards: true, focusEditor: true);
    }

    /// <summary>
    /// Returns the current selection range of the editor along with the selected text.
    /// </summary>
    public async ValueTask<BitMarkdownEditorSelection> GetSelection()
    {
        // Text is documented as empty for a caret, so the answer before the first render
        // says the same rather than handing back a null string.
        if (IsRendered is false) return new BitMarkdownEditorSelection(0, 0, string.Empty);

        return await _js.BitMarkdownEditorGetSelection(_Id);
    }

    /// <summary>
    /// Selects the given range in the editor and moves the focus into it. The range is
    /// clamped to the current content length.
    /// </summary>
    public async ValueTask SetSelection(int start, int end)
    {
        if (IsRendered is false) return;

        await _js.BitMarkdownEditorSetSelection(_Id, start, end);
    }

    /// <summary>
    /// Clears the autosaved draft (if <see cref="AutoSaveId"/> is set), e.g. after the
    /// content has been persisted server-side.
    /// </summary>
    public async ValueTask ClearDraft()
    {
        if (IsRendered is false || string.IsNullOrEmpty(AutoSaveId)) return;

        await _js.BitMarkdownEditorClearDraft(_Id);
    }

    /// <summary>
    /// Reverts the editor to the previous state in the undo history.
    /// </summary>
    public async ValueTask Undo()
    {
        if (IsRendered is false || ReadOnly || Disabled) return;

        await _js.BitMarkdownEditorUndo(_Id);
    }

    /// <summary>
    /// Re-applies the most recently undone change.
    /// </summary>
    public async ValueTask Redo()
    {
        if (IsRendered is false || ReadOnly || Disabled) return;

        await _js.BitMarkdownEditorRedo(_Id);
    }

    /// <summary>
    /// Moves the keyboard focus into the editor textarea, or into the preview pane in the Preview mode, which hides
    /// the textarea.
    /// </summary>
    public async ValueTask Focus()
    {
        if (IsRendered is false) return;

        await _js.BitMarkdownEditorFocus(_Id);
    }

    /// <summary>
    /// Gives focus to the editor textarea, or to the preview pane in the Preview mode, which hides the textarea.
    /// </summary>
    public override ValueTask FocusAsync() => Mode is BitMarkdownEditorMode.Preview ? _previewRef.FocusAsync() : base.FocusAsync();

    /// <inheritdoc cref="FocusAsync()"/>
    /// <param name="preventScroll">Whether the browser leaves the document where it is rather than scrolling the
    /// newly focused element into view.</param>
    public override ValueTask FocusAsync(bool preventScroll) =>
        Mode is BitMarkdownEditorMode.Preview ? _previewRef.FocusAsync(preventScroll) : base.FocusAsync(preventScroll);

    /// <summary>
    /// Moves the keyboard focus out of the editor textarea.
    /// </summary>
    public async ValueTask Blur()
    {
        if (IsRendered is false) return;

        await _js.BitMarkdownEditorBlur(_Id);
    }



    /// <summary>
    /// Invoked from JavaScript whenever the textarea value changes (typing, commands, undo/redo).
    /// </summary>
    [JSInvokable("OnChange")]
    public async Task _OnChange(string? value)
    {
        // The script keeps running for a moment after the component is gone (a debounced
        // change already in flight), and writing to a disposed component throws.
        if (IsDisposed) return;

        _value = value ?? string.Empty;

        // Binds the value, has a form validate the field as it is edited, and raises OnChange, the way every other
        // input of a form does.
        _internalValueChange = true;
        try
        {
            await SetCurrentValueAsync(value);
        }
        finally
        {
            _internalValueChange = false;
        }

        await UpdatePreviewAsync();
    }

    /// <summary>
    /// Invoked from JavaScript to run a command against the current selection.
    /// Returns the transformed text and the selection to restore; JS writes it
    /// back to the textarea so the binding stays in sync.
    /// </summary>
    [JSInvokable("ApplyCommand")]
    public BitMarkdownEditorEditResult _ApplyCommand(string command, int start, int end, string value)
    {
        if (ReadOnly || Disabled || Enum.TryParse<BitMarkdownEditorCommand>(command, out var cmd) is false)
        {
            return BitMarkdownEditorEditResult.NotHandled(value, start, end);
        }

        return BitMarkdownEditorCommands.Apply(cmd, value, start, end, CommandOptions);
    }

    /// <summary>
    /// Invoked from JavaScript whenever the undo/redo history changes, so the
    /// toolbar buttons can reflect the current availability.
    /// </summary>
    [JSInvokable("OnHistoryChanged")]
    public void _OnHistoryChanged(bool canUndo, bool canRedo)
    {
        if (canUndo == _canUndo && canRedo == _canRedo) return;

        _canUndo = canUndo;
        _canRedo = canRedo;

        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Invoked from JavaScript (debounced) when the selection moves, so the toolbar can
    /// reflect the formatting active at the caret. Only the lines the selection touches
    /// are sent, since every format is decided line by line.
    /// </summary>
    [JSInvokable("OnSelectionChanged")]
    public void _OnSelectionChanged(int start, int end, string value, int line = 1, int column = 1, int selectedLength = 0)
    {
        var formats = BitMarkdownEditorCommands.DetectActiveFormats(value ?? string.Empty, start, end, CommandOptions);

        var formatsChanged = formats.Count != _activeFormats.Count || formats.All(_activeFormats.Contains) is false;
        // The caret position is rendered by the status bar alone, so it only earns a re-render
        // while that is showing. Counting it unconditionally would re-render (and, on Blazor
        // Server, diff the circuit) on every single caret move.
        var positionChanged = ShowStatusBar && ShowCursorPosition &&
                              (line != _caretLine || column != _caretColumn || selectedLength != _selectedLength);

        // Still recorded either way, so turning the status bar on shows where the caret
        // actually is instead of where it was when the bar was last visible.
        _caretLine = line;
        _caretColumn = column;
        _selectedLength = selectedLength;

        if (formatsChanged is false && positionChanged is false) return;

        _activeFormats = formats;

        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Invoked from JavaScript when the shortcut of a custom toolbar item is pressed in the editor, with the position
    /// of the item in the toolbar, its menus' items included, which is what its shortcut was bound under.
    /// </summary>
    [JSInvokable("OnToolbarShortcut")]
    public async Task _OnToolbarShortcut(string position)
    {
        if (IsDisposed || int.TryParse(position, out var index) is false || index < 0) return;

        var item = EnumerateToolbarItems(ActiveToolbar).ElementAtOrDefault(index);
        if (item is null || item.Type is not BitMarkdownEditorToolbarItemType.Custom || IsToolbarItemDisabled(item)) return;

        await OnToolbarItemClick(item);

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Invoked from JavaScript when an autosaved draft was restored at initialization.
    /// </summary>
    [JSInvokable("OnDraftRestored")]
    public async Task _OnDraftRestored(string? value)
    {
        await OnDraftRestored.InvokeAsync(value);
    }

    /// <summary>
    /// Invoked from JavaScript when Ctrl/Cmd+Enter is pressed inside the editor.
    /// </summary>
    [JSInvokable("OnSubmit")]
    public async Task _OnSubmit(string? value)
    {
        _value = value ?? string.Empty;

        await OnSubmit.InvokeAsync(value);
    }

    /// <summary>
    /// Invoked from JavaScript to upload a pasted/dropped image via <see cref="OnImageUpload"/>.
    /// Returns the URL to reference the image by, or null to cancel the insertion.
    /// </summary>
    [JSInvokable("UploadImage")]
    public async Task<string?> _UploadImage(string fileName, string base64, string contentType)
    {
        if (OnImageUpload is null || ReadOnly || Disabled) return null;

        byte[] data;
        try
        {
            data = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return null;
        }

        return await OnImageUpload(new BitMarkdownEditorImageUploadInfo(fileName, contentType, data));
    }

    /// <summary>
    /// Invoked from JavaScript when a pasted or dropped image was refused before any of its
    /// bytes were read, so nothing was inserted and no upload was attempted.
    /// </summary>
    [JSInvokable("OnImageRejected")]
    public async Task _OnImageRejected(string fileName, string contentType, long size, string reason)
    {
        if (OnImageRejected.HasDelegate is false) return;

        var parsed = Enum.TryParse<BitMarkdownEditorImageRejectionReason>(reason, out var value)
            ? value
            : BitMarkdownEditorImageRejectionReason.Type;

        await OnImageRejected.InvokeAsync(new BitMarkdownEditorImageRejection(fileName, contentType, size, parsed));
    }

    /// <summary>
    /// Invoked from JavaScript when Escape is pressed in the textarea; closes the find
    /// panel, then leaves full-screen mode.
    /// </summary>
    [JSInvokable("OnEscape")]
    public async Task _OnEscape()
    {
        if (_showHelp)
        {
            _showHelp = false;
            await InvokeAsync(StateHasChanged);
            return;
        }

        if (_showFind)
        {
            _showFind = false;
            await InvokeAsync(StateHasChanged);
            return;
        }

        if (FullScreen)
        {
            await AssignFullScreen(false);
            // Nothing re-renders a component on its own after a call in from JS.
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Invoked from JavaScript for the shortcuts that drive the component's own chrome
    /// rather than the text: the find panel, the display mode and full-screen.
    /// </summary>
    [JSInvokable("OnShortcut")]
    public async Task _OnShortcut(string name)
    {
        switch (name)
        {
            case "find":
                await OpenFind();
                await InvokeAsync(StateHasChanged);
                break;
            case "mode":
                await CycleMode();
                await InvokeAsync(StateHasChanged);
                break;
            case "fullscreen":
                await AssignFullScreen(FullScreen is false);
                await InvokeAsync(StateHasChanged);
                break;
            case "help" when _showHelp:
                await CloseHelp();
                await InvokeAsync(StateHasChanged);
                break;
            case "help":
                _showHelp = true;
                _focusHelp = true;
                await InvokeAsync(StateHasChanged);
                break;
        }
    }



    protected override string RootElementClass => "bit-mde";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FullScreen ? "bit-mde-fsc" : string.Empty);

        ClassBuilder.Register(() => Resizable ? "bit-mde-rsz" : string.Empty);

        ClassBuilder.Register(() => AutoHeight ? "bit-mde-ahg" : string.Empty);

        ClassBuilder.Register(() => StickyToolbar ? "bit-mde-stk" : string.Empty);
    }

    // The three sizes are the public variables themselves, written on the instance: what an app sets on :root
    // is the default, and the parameter is the instance's own value of it.
    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Height is null ? string.Empty : $"--bit-MarkdownEditor-height:{Height}");

        StyleBuilder.Register(() => MinHeight is null ? string.Empty : $"--bit-MarkdownEditor-min-height:{MinHeight}");

        StyleBuilder.Register(() => MaxHeight is null ? string.Empty : $"--bit-MarkdownEditor-max-height:{MaxHeight}");
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitMarkdownEditorParams))]
    protected override void OnInitialized()
    {
        // The initial value is seeded below, and the parameters a BitParams fills in are already in place for it.
        CascadingParameters?.UpdateParameters(this);

        SetDefaultValue();

        _value = Value ?? string.Empty;
        _seedValue = _value;
        _previewValue = _value;
        _valueChanged = false;

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        // Before anything below reads the parameters it may fill in. A BitParams that has gone away takes what it
        // had cascaded with it, which the base class has already put back by now.
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_valueChanged)
        {
            _valueChanged = false;

            _value = Value ?? string.Empty;
            _previewValue = _value;

            // Before the first render there is nothing to push it into; init seeds the textarea, and until then the
            // markup carries it.
            if (IsRendered is false)
            {
                _seedValue = _value;
            }
            else
            {
                try
                {
                    await _js.BitMarkdownEditorSetValue(_Id, Value);
                }
                catch (JSDisconnectedException) { } // the circuit dropped; nothing to update
            }
        }

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // A pane that held the focus and was just hidden by a mode switch hands it to the pane now on screen, so
        // the keyboard is not dropped on the body of the page (where F9 could not bring the pane back either).
        var modeChanged = _renderedMode is { } rendered && rendered != Mode;
        _renderedMode = Mode;

        if (modeChanged && firstRender is false)
        {
            try { await _js.BitMarkdownEditorSyncFocus(_Id); } catch (JSDisconnectedException) { }
        }

        if (_focusHelp)
        {
            _focusHelp = false;
            await _helpRef.FocusSafelyAsync();
        }

        if (_focusFind)
        {
            _focusFind = false;
            await _findRef.FocusSafelyAsync();
        }

        var config = BuildConfig();

        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
            _lastConfig = config.ToString();

            await _js.BitMarkdownEditorInit(_Id, InputElement, RootElement, _dotnetObj, Value, config);
            await ApplyScrollLock();
            return;
        }

        // Everything in the config lives in the script, so a parameter change to any of it
        // has to be pushed across; otherwise it would silently only apply to a fresh editor.
        var signature = config.ToString();
        if (signature == _lastConfig) return;

        _lastConfig = signature;

        try
        {
            await _js.BitMarkdownEditorSetConfig(_Id, config);
        }
        catch (JSDisconnectedException) { } // the circuit dropped; nothing to update
    }



    private BitMarkdownEditorCommandOptions CommandOptions => new()
    {
        IndentUnit = IndentUnit,
        TableColumns = TableColumns,
        TableRows = TableRows,
        BoldStyle = BoldStyle,
        ItalicStyle = ItalicStyle,
        BulletStyle = BulletStyle
    };

    private static readonly BitMarkdownEditorTexts _defaultTexts = new();

    private IReadOnlyList<BitMarkdownEditorToolbarItem> ActiveToolbar => Toolbar ?? BitMarkdownEditorToolbar.Default;

    private BitMarkdownEditorTexts ActiveTexts => Texts ?? _defaultTexts;

    private BitMarkdownEditorConfig BuildConfig() => new()
    {
        ImageUpload = OnImageUpload is not null,
        SyncScroll = SyncScroll,
        AutoPair = AutoPair,
        AutoSaveKey = string.IsNullOrEmpty(AutoSaveId) ? null : AutoSaveId,
        ChangeDebounceMs = ChangeDebounceTime,
        MaxLength = MaxLength is > 0 ? MaxLength.Value : 0,
        AutoFocus = AutoFocus,
        TabIndents = TabIndents,
        AutoClose = AutoClosePairs,
        Submit = OnSubmit.HasDelegate,
        AutoHeight = AutoHeight,
        MaxImageSize = MaxImageSize is > 0 ? MaxImageSize.Value : 0,
        ImageAccept = string.IsNullOrWhiteSpace(AcceptedImageTypes) ? null : AcceptedImageTypes,
        UploadingText = ActiveTexts.UploadingText,
        // Nothing on screen reacts to the caret unless a command button can light up or the
        // status bar prints the position, so the (per-caret-move) round trip is not worth
        // making otherwise.
        ReportSelection = (ShowToolbar && ActiveToolbar.Any(IsCommandItem)) || (ShowStatusBar && ShowCursorPosition),
        Shortcuts = BuildShortcuts()
    };

    // The keys of every shortcut the script handles on its own, spelled the way NormalizeShortcut spells them.
    private static readonly HashSet<string> _builtInShortcuts =
    [
        "ctrl+b", "ctrl+i", "ctrl+e", "ctrl+k", "ctrl+d", "ctrl+f", "ctrl+z", "ctrl+y", "ctrl+/", "ctrl+enter",
        "ctrl+shift+s", "ctrl+shift+d", "ctrl+shift+z", "ctrl+shift+.", "ctrl+shift+7", "ctrl+shift+8", "ctrl+shift+9",
        "ctrl+alt+c", "ctrl+alt+1", "ctrl+alt+2", "ctrl+alt+3", "ctrl+alt+4", "ctrl+alt+5", "ctrl+alt+6",
        "alt+arrowup", "alt+arrowdown", "f9", "f11"
    ];

    /// <summary>
    /// Spells a shortcut hint (<c>"Ctrl+Shift+K"</c>, <c>"Cmd+Alt+1"</c>, <c>"F2"</c>) the way the script spells a
    /// keydown: the modifiers in a fixed order (Cmd and Meta are Ctrl, Option is Alt), then the key, all lower case.
    /// Null for a hint that names no key, or one that would fire on plain typing (no Ctrl or Alt, not a function key).
    /// </summary>
    internal static string? NormalizeShortcut(string? shortcut)
    {
        if (string.IsNullOrWhiteSpace(shortcut)) return null;

        var parts = shortcut.Split('+', StringSplitOptions.TrimEntries);
        bool ctrl = false, alt = false, shift = false;
        string? key = null;

        for (var i = 0; i < parts.Length; i++)
        {
            switch (parts[i].ToLowerInvariant())
            {
                case "ctrl" or "control" or "cmd" or "command" or "meta" or "mod": ctrl = true; break;
                case "alt" or "option": alt = true; break;
                case "shift": shift = true; break;
                // "Ctrl++" splits into an empty part before the plus key it names.
                case "": if (i == parts.Length - 1) key = "+"; break;
                case var other: key = other; break;
            }
        }

        if (key is null) return null;

        var functionKey = key.Length is 2 or 3 && key[0] == 'f' && int.TryParse(key.AsSpan(1), out var n) && n is >= 1 and <= 24;
        if (ctrl is false && alt is false && functionKey is false) return null;

        return $"{(ctrl ? "ctrl+" : null)}{(alt ? "alt+" : null)}{(shift ? "shift+" : null)}{key}";
    }

    // A shortcut on a command item runs the command in the script with no round trip; one on a custom item calls
    // back, naming the item by its position, since nothing makes a Name unique (or even set). The first item to
    // claim a key keeps it.
    private Dictionary<string, string>? BuildShortcuts()
    {
        Dictionary<string, string>? map = null;
        var index = -1;

        foreach (var item in EnumerateToolbarItems(ActiveToolbar))
        {
            index++;

            if (NormalizeShortcut(item.Shortcut) is not { } keys) continue;

            var action = item.Type switch
            {
                BitMarkdownEditorToolbarItemType.Command when item.Command is { } command => $"cmd:{command}",
                BitMarkdownEditorToolbarItemType.Custom when item.OnClick is not null => $"item:{index}",
                _ => null
            };

            if (action is not null) (map ??= []).TryAdd(keys, action);
        }

        return map;
    }

    private static IEnumerable<BitMarkdownEditorToolbarItem> EnumerateToolbarItems(IEnumerable<BitMarkdownEditorToolbarItem> items)
    {
        foreach (var item in items)
        {
            yield return item;

            if (item.Children is null) continue;

            foreach (var child in EnumerateToolbarItems(item.Children)) yield return child;
        }
    }

    private static bool IsCommandItem(BitMarkdownEditorToolbarItem item) =>
        item.Type is BitMarkdownEditorToolbarItemType.Command ||
        (item.Children?.Any(IsCommandItem) ?? false);

    private int WordCount => CountWords(_value);

    /// <summary>
    /// Counts words the way a writing tool is expected to: runs of non-space characters, except
    /// that every CJK ideograph and kana counts on its own. Splitting on whitespace alone would
    /// report a whole Chinese or Japanese paragraph - which carries no spaces - as one word.
    /// </summary>
    private static int CountWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;

        var count = 0;
        var inWord = false;

        foreach (var rune in text.EnumerateRunes())
        {
            if (System.Text.Rune.IsWhiteSpace(rune))
            {
                inWord = false;
                continue;
            }

            if (IsCjk(rune))
            {
                count++;
                inWord = false;
                continue;
            }

            if (inWord is false)
            {
                count++;
                inWord = true;
            }
        }

        return count;
    }

    // CJK ideographs (with their extensions and compatibility blocks) plus the Japanese kana.
    // Hangul is deliberately left out: Korean separates its words with spaces already.
    private static bool IsCjk(System.Text.Rune rune) => rune.Value switch
    {
        >= 0x3040 and <= 0x30FF => true,   // hiragana + katakana
        >= 0x3400 and <= 0x4DBF => true,   // CJK extension A
        >= 0x4E00 and <= 0x9FFF => true,   // CJK unified ideographs
        >= 0xF900 and <= 0xFAFF => true,   // CJK compatibility ideographs
        >= 0x20000 and <= 0x2FA1F => true, // CJK extensions B..F and compatibility supplement
        _ => false
    };

    // Count Unicode text elements (grapheme clusters) so emoji and combining marks
    // are counted as one character each rather than as UTF-16 code units.
    private int CharCount => string.IsNullOrEmpty(_value)
        ? 0
        : new System.Globalization.StringInfo(_value).LengthInTextElements;

    // What MaxLength (and the textarea's own maxlength attribute) actually caps: UTF-16 code
    // units. Reporting graphemes against that limit would let the counter say "3 / 4" while
    // the editor already refused the next keystroke.
    private int LimitedCount => _value?.Length ?? 0;

    private bool IsAtLimit => MaxLength is > 0 && LimitedCount >= MaxLength.Value;

    private string CharCountText => MaxLength is > 0
        ? string.Format(ActiveTexts.CharsWithMaxFormat, LimitedCount, MaxLength.Value)
        : string.Format(ActiveTexts.CharsFormat, CharCount);

    private string CursorPositionText => string.Format(ActiveTexts.CursorPositionFormat, _caretLine, _caretColumn);

    private string SelectedText => string.Format(ActiveTexts.SelectedFormat, _selectedLength);

    private string TextAreaId => $"{_Id}-txt";

    private string CounterId => $"{_Id}-cnt";

    private string DescriptionId => $"{_Id}-des";

    private string HelpTitleId => $"{_Id}-hlt";

    // By the menu's position in the toolbar: the Name of an item is neither required nor unique.
    private string GetMenuId(int index) => $"{_Id}-mnu-{index}";

    private bool HasDescription => string.IsNullOrEmpty(Description) is false || DescriptionTemplate is not null;

    // The description is read out with the field, and so is the counter while MaxLength is set: a limit
    // nobody can see is a limit that surprises, so assistive tech reads out how much room is left.
    private string? TextAreaDescribedBy
    {
        get
        {
            var description = HasDescription ? DescriptionId : null;
            var counter = ShowStatusBar && MaxLength is > 0 ? CounterId : null;

            return description is null ? counter : counter is null ? description : $"{description} {counter}";
        }
    }

    // A visible label already names the field through its for/id pair, and an aria-label on
    // top of it would win over the label and hide it from the accessibility tree.
    private string? TextAreaAriaLabel => AriaLabel ??
        (Label is null && LabelTemplate is null ? ActiveTexts.EditorAriaLabel : null);

    private int ReadingMinutes
    {
        get
        {
            var words = WordCount;
            if (words == 0) return 0;

            var wpm = WordsPerMinute > 0 ? WordsPerMinute : 200;
            return Math.Max(1, (int)Math.Ceiling(words / (double)wpm));
        }
    }

    private string FindStatusText => _findResult is not { } result
        ? string.Empty
        : result.HasMatches
            ? string.Format(ActiveTexts.MatchesFormat, result.Index, result.Count)
            : ActiveTexts.NoMatchesText;

    private bool IsToolbarItemDisabled(BitMarkdownEditorToolbarItem item)
    {
        if (Disabled) return true;

        if (ReadOnly is false)
        {
            return item.Type switch
            {
                BitMarkdownEditorToolbarItemType.Undo => _canUndo is false,
                BitMarkdownEditorToolbarItemType.Redo => _canRedo is false,
                _ => false
            };
        }

        // Read-only blocks everything that would change the text, and AlwaysEnabled is the
        // opt-out for a custom item that only reads it. It deliberately does not reach the
        // built-in editing items: Run/Undo/Redo refuse to touch a read-only editor whatever
        // the toolbar says, so honouring it there would only draw a button that does nothing.
        return item.Type switch
        {
            BitMarkdownEditorToolbarItemType.Custom => item.AlwaysEnabled is false,
            // A menu is still worth opening while read-only when something inside it is.
            BitMarkdownEditorToolbarItemType.Dropdown => item.Children?.Any(c => IsToolbarItemDisabled(c) is false) is not true,
            BitMarkdownEditorToolbarItemType.Command or
            BitMarkdownEditorToolbarItemType.Undo or
            BitMarkdownEditorToolbarItemType.Redo or
            BitMarkdownEditorToolbarItemType.ImageUpload => true,
            _ => false
        };
    }

    // The upload button picks a file for the upload handler, so without one it has nothing to hand the file to.
    private bool IsToolbarItemVisible(BitMarkdownEditorToolbarItem item) =>
        item.Type is not BitMarkdownEditorToolbarItemType.ImageUpload || OnImageUpload is not null;

    private bool IsToolbarItemActive(BitMarkdownEditorToolbarItem item) =>
        (item.Type is BitMarkdownEditorToolbarItemType.ToggleFullScreen && FullScreen) ||
        (item.Type is BitMarkdownEditorToolbarItemType.Help && _showHelp) ||
        (item.Type is BitMarkdownEditorToolbarItemType.Find && _showFind) ||
        (item.Type is BitMarkdownEditorToolbarItemType.Command && item.Command is { } cmd && _activeFormats.Contains(cmd)) ||
        // A menu that holds the active command says so on its trigger, so the caret's heading
        // level can be read off the closed toolbar instead of only by opening the menu.
        (item.Type is BitMarkdownEditorToolbarItemType.Dropdown && (item.Children?.Any(IsToolbarItemActive) ?? false));

    // The commands whose state the caret can be inside of. Every other command inserts
    // something instead of toggling it, so reporting aria-pressed on it would be a lie.
    private static readonly HashSet<BitMarkdownEditorCommand> _toggleCommands =
    [
        BitMarkdownEditorCommand.Bold,
        BitMarkdownEditorCommand.Italic,
        BitMarkdownEditorCommand.Strikethrough,
        BitMarkdownEditorCommand.InlineCode,
        BitMarkdownEditorCommand.Heading1,
        BitMarkdownEditorCommand.Heading2,
        BitMarkdownEditorCommand.Heading3,
        BitMarkdownEditorCommand.Heading4,
        BitMarkdownEditorCommand.Heading5,
        BitMarkdownEditorCommand.Heading6,
        BitMarkdownEditorCommand.Quote,
        BitMarkdownEditorCommand.UnorderedList,
        BitMarkdownEditorCommand.OrderedList,
        BitMarkdownEditorCommand.TaskList
    ];

    private static bool IsToolbarItemToggle(BitMarkdownEditorToolbarItem item) =>
        item.Type is BitMarkdownEditorToolbarItemType.ToggleFullScreen or BitMarkdownEditorToolbarItemType.Help
            or BitMarkdownEditorToolbarItemType.Find ||
        (item.Type is BitMarkdownEditorToolbarItemType.Command && item.Command is { } cmd && _toggleCommands.Contains(cmd));

    // An item with no title of its own is named by its text.
    private string GetToolbarItemLabel(BitMarkdownEditorToolbarItem item) =>
        ActiveTexts.GetToolbarTitle(item.Name, string.IsNullOrEmpty(item.Title) ? item.Text ?? string.Empty : item.Title);

    private string GetToolbarItemTitle(BitMarkdownEditorToolbarItem item)
    {
        var label = GetToolbarItemLabel(item);
        return string.IsNullOrEmpty(item.Shortcut) ? label : $"{label} ({item.Shortcut})";
    }

    // aria-keyshortcuts spells modifiers out and joins them with "+", so "Ctrl+Shift+S"
    // has to become "Control+Shift+S" before assistive tech will read it correctly.
    private static string? GetToolbarItemKeyShortcuts(BitMarkdownEditorToolbarItem item) =>
        string.IsNullOrEmpty(item.Shortcut)
            ? null
            : item.Shortcut.Replace("Ctrl", "Control", StringComparison.OrdinalIgnoreCase)
                           .Replace("Cmd", "Meta", StringComparison.OrdinalIgnoreCase);

    // The help panel is built from this list rather than from hard-coded markup, so a
    // shortcut can never be documented in one place and missing from the other.
    private IEnumerable<(string Label, string Keys)> HelpShortcuts
    {
        get
        {
            foreach (var shortcut in _helpShortcuts) yield return shortcut;

            // A shortcut the editor does not capture has no business being documented as one,
            // and Ctrl+Enter is only captured while something is listening for the submit.
            if (OnSubmit.HasDelegate) yield return (ActiveTexts.ShortcutSubmit, "Ctrl/Cmd + Enter");

            // The app's own: every custom item's bound shortcut, and a command item's on keys of its own choosing.
            HashSet<string> listed = [];
            foreach (var item in EnumerateToolbarItems(ActiveToolbar))
            {
                if (NormalizeShortcut(item.Shortcut) is not { } keys) continue;

                var bound = item.Type switch
                {
                    BitMarkdownEditorToolbarItemType.Custom => item.OnClick is not null,
                    BitMarkdownEditorToolbarItemType.Command => item.Command is not null && _builtInShortcuts.Contains(keys) is false,
                    _ => false
                };

                if (bound && listed.Add(keys)) yield return (GetToolbarItemLabel(item), item.Shortcut!);
            }
        }
    }

    private IEnumerable<(string Label, string Keys)> _helpShortcuts =>
    [
        (ActiveTexts.ShortcutBold, "Ctrl/Cmd + B"),
        (ActiveTexts.ShortcutItalic, "Ctrl/Cmd + I"),
        (ActiveTexts.ShortcutStrikethrough, "Ctrl/Cmd + Shift + S"),
        (ActiveTexts.ShortcutInlineCode, "Ctrl/Cmd + E"),
        (ActiveTexts.ShortcutCodeBlock, "Ctrl/Cmd + Alt + C"),
        (ActiveTexts.ShortcutLink, "Ctrl/Cmd + K"),
        (ActiveTexts.ShortcutHeadings, "Ctrl/Cmd + Alt + 1 … 6"),
        (ActiveTexts.ShortcutQuote, "Ctrl/Cmd + Shift + ."),
        (ActiveTexts.ShortcutOrderedList, "Ctrl/Cmd + Shift + 7"),
        (ActiveTexts.ShortcutUnorderedList, "Ctrl/Cmd + Shift + 8"),
        (ActiveTexts.ShortcutTaskList, "Ctrl/Cmd + Shift + 9"),
        (ActiveTexts.ShortcutUndo, "Ctrl/Cmd + Z"),
        (ActiveTexts.ShortcutRedo, "Ctrl/Cmd + Y (or Shift + Z)"),
        (ActiveTexts.ShortcutIndentOutdent, "Tab / Shift + Tab"),
        (ActiveTexts.ShortcutTableCells, "Tab / Shift + Tab"),
        (ActiveTexts.ShortcutContinueList, "Enter"),
        (ActiveTexts.ShortcutMoveLine, "Alt + ↑ / ↓"),
        (ActiveTexts.ShortcutDuplicateLine, "Ctrl/Cmd + D"),
        (ActiveTexts.ShortcutDeleteLine, "Ctrl/Cmd + Shift + D"),
        (ActiveTexts.ShortcutFind, "Ctrl/Cmd + F"),
        (ActiveTexts.ShortcutTogglePreview, "F9"),
        (ActiveTexts.ShortcutFullScreen, "F11"),
        (ActiveTexts.ShortcutHelp, "Ctrl/Cmd + /"),
        (ActiveTexts.ShortcutEscapeTab, "Esc, then Tab")
    ];

    private async Task OnToolbarItemClick(BitMarkdownEditorToolbarItem item)
    {
        switch (item.Type)
        {
            case BitMarkdownEditorToolbarItemType.Command when item.Command is { } cmd:
                await Run(cmd);
                break;
            case BitMarkdownEditorToolbarItemType.Undo:
                await Undo();
                break;
            case BitMarkdownEditorToolbarItemType.Redo:
                await Redo();
                break;
            case BitMarkdownEditorToolbarItemType.TogglePreview:
                await CycleMode();
                break;
            case BitMarkdownEditorToolbarItemType.ToggleFullScreen:
                await AssignFullScreen(FullScreen is false);
                break;
            case BitMarkdownEditorToolbarItemType.Help:
                _showHelp = _showHelp is false;
                _focusHelp = _showHelp;
                if (_showHelp is false) await Focus();
                break;
            case BitMarkdownEditorToolbarItemType.Find when _showFind:
                await CloseFind();
                break;
            case BitMarkdownEditorToolbarItemType.Find:
                await OpenFind();
                break;
            case BitMarkdownEditorToolbarItemType.Custom when item.OnClick is not null && (ReadOnly is false || item.AlwaysEnabled):
                // A handler written in the component that owns the toolbar changes that component's state, so it is
                // run as one of its event handlers and the component re-renders after it, the way it does after any
                // other; a handler with no component behind it re-renders the editor alone.
                var onClick = item.OnClick;
                await EventCallback.Factory.Create(FindHandlerOwner(onClick.Target) ?? (object)this, () => onClick(this)).InvokeAsync();
                break;
        }
    }

    // The target of a lambda is the component it was written in, unless the lambda captures a local (a loop
    // variable, a parameter): then it is the closure the compiler made for that scope, which reaches the component
    // through its "this" field, or through the closure of an enclosing scope that does.
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The fields of a closure are the ones its lambda reads, so they are never trimmed away.")]
    private static IHandleEvent? FindHandlerOwner(object? target, int depth = 0)
    {
        if (target is IHandleEvent owner) return owner;

        if (target is null || depth > 4) return null;

        var type = target.GetType();
        if (type.IsDefined(typeof(CompilerGeneratedAttribute), false) is false) return null;

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            // "<>4__this" is the component, "CS$<>8__locals*" an enclosing closure; every other field is a captured
            // local, which may well hold some other component (an @ref) that is not the one the handler belongs to.
            if (field.Name is not "<>4__this" && field.Name.StartsWith("CS$<>8__locals", StringComparison.Ordinal) is false) continue;

            if (FindHandlerOwner(field.GetValue(target), depth + 1) is { } found) return found;
        }

        return null;
    }

    private async Task CloseHelp()
    {
        _showHelp = false;

        // The dialog held the focus, so it has to be put back where the user was.
        await Focus();
    }

    private async Task OnHelpKeyDown(KeyboardEventArgs e)
    {
        // The shortcut that opened the dialog closes it too, keyed by the physical key as the script keys it.
        if (e.Key is "Escape" || ((e.CtrlKey || e.MetaKey) && e.Code is "Slash"))
        {
            await CloseHelp();
        }
    }

    private async Task OnFindKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "Escape":
                _showFind = false;
                await Focus();
                break;
            case "Enter" when e.ShiftKey:
                await FindPreviousMatch();
                break;
            case "Enter":
                await FindNextMatch();
                break;
        }
    }

    // Enter belongs to the field it is pressed in: in the replace box it replaces, which is
    // what every find & replace panel does, rather than walking to the next match instead.
    private async Task OnReplaceKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "Escape":
                _showFind = false;
                await Focus();
                break;
            case "Enter" when e.CtrlKey || e.MetaKey || e.AltKey:
                await ReplaceAll();
                break;
            case "Enter":
                await ReplaceOne();
                break;
        }
    }

    private void OnFindTextChanged(string value)
    {
        _findText = value;

        // The old count describes the old term; keep it out of the way until the next search.
        _findResult = null;
    }

    private Task FindNextMatch() => FindFromPanel(backwards: false);

    private Task FindPreviousMatch() => FindFromPanel(backwards: true);

    // The panel walks the matches from its own controls, so the match is selected without the
    // focus moving into the textarea: otherwise the Enter that found a match would leave the
    // caret in the document and the next Enter would type a newline into it instead of
    // reaching the find input again.
    private async Task FindFromPanel(bool backwards)
    {
        if (string.IsNullOrEmpty(_findText)) { _findResult = null; return; }

        if (IsRendered is false) return;

        _findResult = await _js.BitMarkdownEditorFind(_Id, _findText, _matchCase, backwards, focusEditor: false);
    }

    // Opening the panel seeds it with whatever is selected, the way every find box does, so
    // the "select a word, then hit Ctrl+F" gesture searches for that word straight away.
    private async Task OpenFind()
    {
        var selection = await GetSelection();

        if (selection.IsEmpty is false && selection.Text.Contains('\n') is false)
        {
            _findText = selection.Text;
            _findResult = null;
        }

        _showFind = true;
        _focusFind = true;
    }

    private async Task CloseFind()
    {
        _showFind = false;

        // The panel's own controls are gone with it, so the focus has to be put somewhere.
        await Focus();
    }

    private async Task ToggleMatchCase()
    {
        _matchCase = _matchCase is false;

        if (string.IsNullOrEmpty(_findText) is false)
        {
            await FindNextMatch();
        }
    }

    private async Task ReplaceOne()
    {
        if (IsRendered is false || ReadOnly || Disabled || string.IsNullOrEmpty(_findText)) return;

        _findResult = await _js.BitMarkdownEditorReplaceOne(_Id, _findText, _replaceText, _matchCase, focusEditor: false);
    }

    private async Task ReplaceAll()
    {
        if (string.IsNullOrEmpty(_findText)) return;

        await Replace(_findText, _replaceText, all: true, matchCase: _matchCase);

        // Report what is left: a replacement containing the search term still has matches.
        await FindFromPanel(backwards: false);
    }

    // Focus guards wrap the help dialog: tabbing onto either sentinel bounces focus
    // back to the (only) focusable control, trapping keyboard focus inside the modal.
    // They are deliberately not aria-hidden - hiding a focusable element from assistive
    // tech is exactly what the aria-hidden-focus rule forbids - and carry no content, so
    // nothing is announced when focus passes through them.
    private async Task FocusHelpClose()
    {
        await _helpCloseRef.FocusSafelyAsync();
    }

    private async Task CycleMode()
    {
        var next = Mode switch
        {
            BitMarkdownEditorMode.Edit => BitMarkdownEditorMode.Split,
            BitMarkdownEditorMode.Split => BitMarkdownEditorMode.Preview,
            _ => BitMarkdownEditorMode.Edit
        };

        if (await AssignMode(next) is false) return;

        // Which panes are on screen is only ever shown, so a screen reader is told what the cycle landed on.
        _announcement = string.Format(ActiveTexts.ModeAnnouncementFormat, ActiveTexts.GetModeLabel(Mode));
    }

    // A full-screen editor covers the viewport, so a page that goes on scrolling behind it
    // moves what the user comes back to out from under them. The hold is the same counted one
    // BitModal takes, keyed by this editor's id.
    private async ValueTask OnFullScreenSet()
    {
        if (IsRendered is false) return;

        await ApplyScrollLock();
    }

    private async Task ApplyScrollLock()
    {
        if (FullScreen == _scrollLocked) return;

        _scrollLocked = FullScreen;

        try
        {
            if (FullScreen)
            {
                await _js.BitUtilsLockScroll(_Id);
            }
            else
            {
                await _js.BitUtilsUnlockScroll(_Id);
            }
        }
        catch (JSDisconnectedException) { } // the circuit dropped; the page goes with it
    }

    // The value is the text itself, so there is nothing that could fail to parse.
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        result = value;
        parsingErrorMessage = null;
        return true;
    }

    private async Task UpdatePreviewAsync()
    {
        if (DebounceTime > 0)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            var cts = _debounceCts = new();
            try
            {
                await Task.Delay(DebounceTime, cts.Token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }

        // Disposal may have started while awaiting the debounce delay.
        if (IsDisposed) return;

        _previewValue = _value;

        await InvokeAsync(StateHasChanged);
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = null;

        _dotnetObj?.Dispose();

        try
        {
            if (_scrollLocked)
            {
                _scrollLocked = false;
                await _js.BitUtilsUnlockScroll(_Id);
            }

            await _js.BitMarkdownEditorDispose(_Id);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await base.DisposeAsync(disposing);
    }
}
