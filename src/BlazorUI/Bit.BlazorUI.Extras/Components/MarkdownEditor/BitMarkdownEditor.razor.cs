namespace Bit.BlazorUI;

/// <summary>
/// BitMarkdownEditor is a native Blazor markdown editor with a customizable toolbar, keyboard
/// shortcuts, smart list handling, undo/redo history and a live GitHub-flavored preview powered
/// by the <see cref="BitMarkdownViewer"/>. All markdown transformations happen in C#; a small
/// JS-interop script handles textarea selection control, key interception and the undo/redo
/// history (coalescing rapid typing into single steps).
/// </summary>
public partial class BitMarkdownEditor : BitComponentBase
{
    private string _value = string.Empty;
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
    private bool _scrollLocked;
    private string? _lastConfig;
    private IReadOnlyCollection<BitMarkdownEditorCommand> _activeFormats = [];
    private ElementReference _helpRef = default!;
    private ElementReference _helpCloseRef = default!;
    private ElementReference _findRef = default!;
    private ElementReference _textAreaRef = default!;
    private CancellationTokenSource? _debounceCts;
    private DotNetObjectReference<BitMarkdownEditor>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Moves the keyboard focus into the editor as soon as it is initialized.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

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
    /// The default text value of the editor to use at initialization.
    /// </summary>
    [Parameter] public string? DefaultValue { get; set; }

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
    /// </summary>
    [Parameter] public int? MaxLength { get; set; }

    /// <summary>
    /// The largest pasted or dropped image (in bytes) the editor uploads. A bigger file is
    /// refused before its bytes are read, and reported through <see cref="OnImageRejected"/>.
    /// Null (the default) leaves the size unlimited.
    /// </summary>
    [Parameter] public long? MaxImageSize { get; set; }

    /// <summary>
    /// The image types the paste/drop upload accepts, as a comma separated list of MIME types
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
    /// Callback for when the editor value changes.
    /// </summary>
    [Parameter] public EventCallback<string?> OnChange { get; set; }

    /// <summary>
    /// A handler that uploads a pasted or dropped image and returns the URL to reference
    /// it by. When set, the editor enables clipboard-paste and drag-and-drop image upload:
    /// a placeholder is inserted immediately and replaced with the returned URL once the
    /// handler completes (returning null cancels the insertion). When null, image upload
    /// is disabled and only the manual image command is available.
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
    /// Makes the editor read-only.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

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
    /// The two-way bound text value of the editor.
    /// </summary>
    [Parameter, TwoWayBound, CallOnSetAsync(nameof(OnValueSet))]
    public string? Value { get; set; }



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
        if (IsRendered is false || ReadOnly || IsEnabled is false) return;

        await _js.BitMarkdownEditorRun(_Id, command.ToString());
    }

    /// <summary>
    /// Inserts the given markdown text at the current selection (replacing it) as a single
    /// undo step. Useful for building custom toolbar buttons that emit their own markdown.
    /// </summary>
    public async ValueTask Insert(string text)
    {
        if (IsRendered is false || ReadOnly || IsEnabled is false) return;

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
        if (IsRendered is false || ReadOnly || IsEnabled is false || string.IsNullOrEmpty(search)) return 0;

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
        if (IsRendered is false || ReadOnly || IsEnabled is false) return;

        await _js.BitMarkdownEditorUndo(_Id);
    }

    /// <summary>
    /// Re-applies the most recently undone change.
    /// </summary>
    public async ValueTask Redo()
    {
        if (IsRendered is false || ReadOnly || IsEnabled is false) return;

        await _js.BitMarkdownEditorRedo(_Id);
    }

    /// <summary>
    /// Moves the keyboard focus into the editor textarea.
    /// </summary>
    public async ValueTask Focus()
    {
        if (IsRendered is false) return;

        await _js.BitMarkdownEditorFocus(_Id);
    }

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

        _internalValueChange = true;
        try
        {
            await AssignValue(value);
        }
        finally
        {
            _internalValueChange = false;
        }

        await OnChange.InvokeAsync(value);

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
        if (ReadOnly || IsEnabled is false || Enum.TryParse<BitMarkdownEditorCommand>(command, out var cmd) is false)
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
        var positionChanged = line != _caretLine || column != _caretColumn || selectedLength != _selectedLength;

        if (formatsChanged is false && positionChanged is false) return;

        _activeFormats = formats;
        _caretLine = line;
        _caretColumn = column;
        _selectedLength = selectedLength;

        _ = InvokeAsync(StateHasChanged);
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
        if (OnImageUpload is null || ReadOnly || IsEnabled is false) return null;

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
        }
    }



    protected override string RootElementClass => "bit-mde";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FullScreen ? "bit-mde-fsc" : string.Empty);

        ClassBuilder.Register(() => Resizable ? "bit-mde-rsz" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Height is null ? string.Empty : $"--bit-mde-height:{Height}");

        StyleBuilder.Register(() => MinHeight is null ? string.Empty : $"--bit-mde-min-height:{MinHeight}");

        StyleBuilder.Register(() => MaxHeight is null ? string.Empty : $"--bit-mde-max-height:{MaxHeight}");
    }

    protected override void OnInitialized()
    {
        _value = Value ?? DefaultValue ?? string.Empty;
        _previewValue = _value;

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_focusHelp)
        {
            _focusHelp = false;
            await _helpRef.FocusAsync();
        }

        if (_focusFind)
        {
            _focusFind = false;
            try { await _findRef.FocusAsync(); } catch (JSException) { } // panel may already be gone
        }

        var config = BuildConfig();

        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
            _lastConfig = config.ToString();

            await _js.BitMarkdownEditorInit(_Id, _textAreaRef, RootElement, _dotnetObj, Value ?? DefaultValue, config);
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
        MaxImageSize = MaxImageSize is > 0 ? MaxImageSize.Value : 0,
        ImageAccept = string.IsNullOrWhiteSpace(AcceptedImageTypes) ? null : AcceptedImageTypes,
        UploadingText = ActiveTexts.UploadingText,
        // Nothing on screen reacts to the caret unless a command button can light up or the
        // status bar prints the position, so the (per-caret-move) round trip is not worth
        // making otherwise.
        ReportSelection = (ShowToolbar && ActiveToolbar.Any(IsCommandItem)) || (ShowStatusBar && ShowCursorPosition)
    };

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

    // A limit nobody can see is a limit that surprises: while MaxLength is set, the counter
    // describes the textarea so assistive tech reads out how much room is left.
    private string? TextAreaDescribedBy => ShowStatusBar && MaxLength is > 0 ? CounterId : null;

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
        if (IsEnabled is false) return true;

        var blockedByReadOnly = ReadOnly && item.AlwaysEnabled is false;

        return item.Type switch
        {
            BitMarkdownEditorToolbarItemType.Command => blockedByReadOnly,
            BitMarkdownEditorToolbarItemType.Dropdown => blockedByReadOnly,
            BitMarkdownEditorToolbarItemType.Undo => blockedByReadOnly || _canUndo is false,
            BitMarkdownEditorToolbarItemType.Redo => blockedByReadOnly || _canRedo is false,
            BitMarkdownEditorToolbarItemType.Custom => blockedByReadOnly,
            _ => false
        };
    }

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

    private string GetToolbarItemLabel(BitMarkdownEditorToolbarItem item) =>
        ActiveTexts.GetToolbarTitle(item.Name, item.Title);

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
                await item.OnClick(this);
                break;
        }
    }

    private async Task CloseHelp()
    {
        _showHelp = false;

        // The dialog held the focus, so it has to be put back where the user was.
        await Focus();
    }

    private async Task OnHelpKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is "Escape")
        {
            _showHelp = false;
            // Return focus to the editor when the dialog closes.
            await Focus();
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
        if (IsRendered is false || ReadOnly || IsEnabled is false || string.IsNullOrEmpty(_findText)) return;

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
        try { await _helpCloseRef.FocusAsync(); } catch (JSException) { }
    }

    private async Task CycleMode()
    {
        var next = Mode switch
        {
            BitMarkdownEditorMode.Edit => BitMarkdownEditorMode.Split,
            BitMarkdownEditorMode.Split => BitMarkdownEditorMode.Preview,
            _ => BitMarkdownEditorMode.Edit
        };

        await AssignMode(next);
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

    private async ValueTask OnValueSet()
    {
        _value = Value ?? string.Empty;

        if (_internalValueChange) return;

        _previewValue = _value;

        // The textarea is uncontrolled (JS owns its value to preserve the caret),
        // so external changes must be pushed into it through the interop script.
        // Before the first render there is nothing to push; init seeds the textarea.
        if (IsRendered is false) return;

        try
        {
            await _js.BitMarkdownEditorSetValue(_Id, Value);
        }
        catch (JSDisconnectedException) { } // the circuit dropped; nothing to update
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
