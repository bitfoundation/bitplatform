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
    private string _findText = string.Empty;
    private string _replaceText = string.Empty;
    private bool _canUndo;
    private bool _canRedo;
    private bool _internalValueChange;
    private IReadOnlyCollection<BitMarkdownEditorCommand> _activeFormats = [];
    private ElementReference _helpRef = default!;
    private ElementReference _helpCloseRef = default!;
    private ElementReference _findRef = default!;
    private ElementReference _textAreaRef = default!;
    private CancellationTokenSource? _debounceCts;
    private DotNetObjectReference<BitMarkdownEditor>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Custom CSS classes for different parts of the editor.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitMarkdownEditorClassStyles? Classes { get; set; }

    /// <summary>
    /// A stable key under which the editor content is autosaved to the browser's
    /// localStorage. When set, a draft is written as the user types and restored on
    /// initialization if no <see cref="Value"/>/<see cref="DefaultValue"/> is supplied.
    /// The draft is cleared automatically once <see cref="Value"/> is committed via
    /// <see cref="ClearDraft"/>.
    /// </summary>
    [Parameter] public string? AutoSaveId { get; set; }

    /// <summary>
    /// Enables wrapping the current selection when a pairing character
    /// (for example <c>*</c>, <c>`</c>, <c>[</c>) is typed. Defaults to true.
    /// </summary>
    [Parameter] public bool AutoPair { get; set; } = true;

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
    [Parameter, TwoWayBound, ResetClassBuilder]
    public bool FullScreen { get; set; }

    /// <summary>
    /// The height of the editor (any CSS length). Ignored in full-screen mode.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Height { get; set; }

    /// <summary>
    /// The string inserted per indent level (default: two spaces).
    /// </summary>
    [Parameter] public string IndentUnit { get; set; } = "  ";

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
    /// The placeholder text shown when the editor is empty.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

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
    /// Whether the word/character status bar is shown.
    /// </summary>
    [Parameter] public bool ShowStatusBar { get; set; } = true;

    /// <summary>
    /// Whether the estimated reading time is shown in the status bar.
    /// </summary>
    [Parameter] public bool ShowReadingTime { get; set; }

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
    [Parameter, TwoWayBound, CallOnSet(nameof(OnValueSet))]
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
        return await _js.BitMarkdownEditorGetValue(_Id);
    }

    /// <summary>
    /// Runs a specific command on the current selection of the editor.
    /// </summary>
    public async ValueTask Run(BitMarkdownEditorCommand command)
    {
        await _js.BitMarkdownEditorRun(_Id, command.ToString());
    }

    /// <summary>
    /// Inserts the given markdown text at the current selection as a single undo step.
    /// Useful for building custom toolbar buttons that emit their own markdown.
    /// </summary>
    public async ValueTask Insert(string text)
    {
        if (ReadOnly || IsEnabled is false) return;

        await _js.BitMarkdownEditorInsert(_Id, text);
    }

    /// <summary>
    /// Replaces occurrences of <paramref name="search"/> with <paramref name="replacement"/>.
    /// Returns the number of replacements made.
    /// </summary>
    public async ValueTask<int> Replace(string search, string replacement, bool all = true)
    {
        if (ReadOnly || IsEnabled is false || string.IsNullOrEmpty(search)) return 0;

        return await _js.BitMarkdownEditorReplaceAll(_Id, search, replacement, all);
    }

    /// <summary>
    /// Clears the autosaved draft (if <see cref="AutoSaveId"/> is set), e.g. after the
    /// content has been persisted server-side.
    /// </summary>
    public async ValueTask ClearDraft()
    {
        if (string.IsNullOrEmpty(AutoSaveId)) return;

        await _js.BitMarkdownEditorClearDraft(_Id);
    }

    /// <summary>
    /// Reverts the editor to the previous state in the undo history.
    /// </summary>
    public async ValueTask Undo()
    {
        if (ReadOnly || IsEnabled is false) return;

        await _js.BitMarkdownEditorUndo(_Id);
    }

    /// <summary>
    /// Re-applies the most recently undone change.
    /// </summary>
    public async ValueTask Redo()
    {
        if (ReadOnly || IsEnabled is false) return;

        await _js.BitMarkdownEditorRedo(_Id);
    }

    /// <summary>
    /// Moves the keyboard focus into the editor textarea.
    /// </summary>
    public async ValueTask Focus()
    {
        await _js.BitMarkdownEditorFocus(_Id);
    }



    /// <summary>
    /// Invoked from JavaScript whenever the textarea value changes (typing, commands, undo/redo).
    /// </summary>
    [JSInvokable("OnChange")]
    public async Task _OnChange(string? value)
    {
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

        return BitMarkdownEditorCommands.Apply(cmd, value, start, end, IndentUnit);
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
    /// reflect the formatting active at the caret.
    /// </summary>
    [JSInvokable("OnSelectionChanged")]
    public void _OnSelectionChanged(int start, int end, string value)
    {
        var formats = BitMarkdownEditorCommands.DetectActiveFormats(value ?? string.Empty, start, end);

        if (formats.Count == _activeFormats.Count && formats.All(_activeFormats.Contains)) return;

        _activeFormats = formats;
        _ = InvokeAsync(StateHasChanged);
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
    /// Invoked from JavaScript when Escape is pressed in the textarea; leaves full-screen mode.
    /// </summary>
    [JSInvokable("OnEscape")]
    public async Task _OnEscape()
    {
        if (_showFind)
        {
            _showFind = false;
            await InvokeAsync(StateHasChanged);
            return;
        }

        if (FullScreen)
        {
            await AssignFullScreen(false);
        }
    }

    /// <summary>
    /// Invoked from JavaScript when Ctrl/Cmd+F is pressed; toggles the find &amp; replace panel.
    /// </summary>
    [JSInvokable("OnFindShortcut")]
    public void _OnFindShortcut()
    {
        _showFind = true;
        _focusFind = true;
        _ = InvokeAsync(StateHasChanged);
    }



    protected override string RootElementClass => "bit-mde";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FullScreen ? "bit-mde-fsc" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Height is null ? string.Empty : $"--bit-mde-height:{Height}");
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

        if (firstRender is false) return;

        _dotnetObj = DotNetObjectReference.Create(this);

        var config = new
        {
            imageUpload = OnImageUpload is not null,
            syncScroll = SyncScroll,
            autoPair = AutoPair,
            autoSaveKey = string.IsNullOrEmpty(AutoSaveId) ? null : AutoSaveId,
            changeDebounceMs = ChangeDebounceTime
        };

        await _js.BitMarkdownEditorInit(_Id, _textAreaRef, RootElement, _dotnetObj, Value ?? DefaultValue, config);
    }



    private static readonly BitMarkdownEditorTexts _defaultTexts = new();

    private IReadOnlyList<BitMarkdownEditorToolbarItem> ActiveToolbar => Toolbar ?? BitMarkdownEditorToolbar.Default;

    private BitMarkdownEditorTexts ActiveTexts => Texts ?? _defaultTexts;

    private int WordCount =>
        string.IsNullOrWhiteSpace(_value)
            ? 0
            : _value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    // Count Unicode text elements (grapheme clusters) so emoji and combining marks
    // are counted as one character each rather than as UTF-16 code units.
    private int CharCount => string.IsNullOrEmpty(_value)
        ? 0
        : new System.Globalization.StringInfo(_value).LengthInTextElements;

    private int ReadingMinutes
    {
        get
        {
            var wpm = WordsPerMinute > 0 ? WordsPerMinute : 200;
            return Math.Max(1, (int)Math.Ceiling(WordCount / (double)wpm));
        }
    }

    private bool IsToolbarItemDisabled(BitMarkdownEditorToolbarItem item) => IsEnabled is false || item.Type switch
    {
        BitMarkdownEditorToolbarItemType.Command => ReadOnly,
        BitMarkdownEditorToolbarItemType.Dropdown => ReadOnly,
        BitMarkdownEditorToolbarItemType.Undo => ReadOnly || _canUndo is false,
        BitMarkdownEditorToolbarItemType.Redo => ReadOnly || _canRedo is false,
        BitMarkdownEditorToolbarItemType.Custom => ReadOnly,
        _ => false
    };

    private bool IsToolbarItemActive(BitMarkdownEditorToolbarItem item) =>
        (item.Type is BitMarkdownEditorToolbarItemType.ToggleFullScreen && FullScreen) ||
        (item.Type is BitMarkdownEditorToolbarItemType.Help && _showHelp) ||
        (item.Type is BitMarkdownEditorToolbarItemType.Find && _showFind) ||
        (item.Type is BitMarkdownEditorToolbarItemType.Command && item.Command is { } cmd && _activeFormats.Contains(cmd));

    private static bool IsToolbarItemToggle(BitMarkdownEditorToolbarItem item) =>
        item.Type is BitMarkdownEditorToolbarItemType.ToggleFullScreen or BitMarkdownEditorToolbarItemType.Help
            or BitMarkdownEditorToolbarItemType.Find or BitMarkdownEditorToolbarItemType.Command;

    private string GetToolbarItemLabel(BitMarkdownEditorToolbarItem item) =>
        ActiveTexts.GetToolbarTitle(item.Name, item.Title);

    private string GetToolbarItemTitle(BitMarkdownEditorToolbarItem item)
    {
        var label = GetToolbarItemLabel(item);
        return string.IsNullOrEmpty(item.Shortcut) ? label : $"{label} ({item.Shortcut})";
    }

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
                break;
            case BitMarkdownEditorToolbarItemType.Find:
                _showFind = _showFind is false;
                _focusFind = _showFind;
                break;
            case BitMarkdownEditorToolbarItemType.Custom when item.OnClick is not null && ReadOnly is false:
                await item.OnClick(this);
                break;
        }
    }

    private async Task OnHelpKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is "Escape")
        {
            _showHelp = false;
            // Return focus to the editor when the dialog closes.
            await Focus();
        }
        // Only the close button is focusable inside the dialog; swallowing Tab keeps
        // focus trapped within the modal instead of leaking to the background.
        // (Handled by @onkeydown:preventDefault in the markup.)
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
                await ReplaceAll();
                break;
            case "Enter":
                await ReplaceOne();
                break;
        }
    }

    private async Task ReplaceOne()
    {
        if (string.IsNullOrEmpty(_findText)) return;
        await Replace(_findText, _replaceText, all: false);
    }

    private async Task ReplaceAll()
    {
        if (string.IsNullOrEmpty(_findText)) return;
        await Replace(_findText, _replaceText, all: true);
    }

    // Focus guards wrap the help dialog: tabbing onto either sentinel bounces focus
    // back to the (only) focusable control, trapping keyboard focus inside the modal.
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
            await _js.BitMarkdownEditorDispose(_Id);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await base.DisposeAsync(disposing);
    }
}
