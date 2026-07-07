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
    private bool _canUndo;
    private bool _canRedo;
    private bool _internalValueChange;
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
    /// The debounce window (in milliseconds) before the preview re-renders while typing.
    /// </summary>
    [Parameter] public int DebounceTime { get; set; } = 150;

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
    /// Whether the formatting toolbar is shown.
    /// </summary>
    [Parameter] public bool ShowToolbar { get; set; } = true;

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
    /// Reverts the editor to the previous state in the undo history.
    /// </summary>
    public async ValueTask Undo()
    {
        if (ReadOnly) return;

        await _js.BitMarkdownEditorUndo(_Id);
    }

    /// <summary>
    /// Re-applies the most recently undone change.
    /// </summary>
    public async ValueTask Redo()
    {
        if (ReadOnly) return;

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

        if (firstRender is false) return;

        _dotnetObj = DotNetObjectReference.Create(this);

        await _js.BitMarkdownEditorInit(_Id, _textAreaRef, RootElement, _dotnetObj, Value ?? DefaultValue);
    }



    private static readonly BitMarkdownEditorTexts _defaultTexts = new();

    private IReadOnlyList<BitMarkdownEditorToolbarItem> ActiveToolbar => Toolbar ?? BitMarkdownEditorToolbar.Default;

    private BitMarkdownEditorTexts ActiveTexts => Texts ?? _defaultTexts;

    private int WordCount =>
        string.IsNullOrWhiteSpace(_value)
            ? 0
            : _value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    private bool IsToolbarItemDisabled(BitMarkdownEditorToolbarItem item) => IsEnabled is false || item.Type switch
    {
        BitMarkdownEditorToolbarItemType.Command => ReadOnly,
        BitMarkdownEditorToolbarItemType.Undo => ReadOnly || _canUndo is false,
        BitMarkdownEditorToolbarItemType.Redo => ReadOnly || _canRedo is false,
        _ => false
    };

    private bool IsToolbarItemActive(BitMarkdownEditorToolbarItem item) =>
        (item.Type is BitMarkdownEditorToolbarItemType.ToggleFullScreen && FullScreen) ||
        (item.Type is BitMarkdownEditorToolbarItemType.Help && _showHelp);

    private static bool IsToolbarItemToggle(BitMarkdownEditorToolbarItem item) =>
        item.Type is BitMarkdownEditorToolbarItemType.ToggleFullScreen or BitMarkdownEditorToolbarItemType.Help;

    private string GetToolbarItemTitle(BitMarkdownEditorToolbarItem item) =>
        string.IsNullOrEmpty(item.Shortcut) ? item.Title : $"{item.Title} ({item.Shortcut})";

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
                break;
            case BitMarkdownEditorToolbarItemType.Custom when item.OnClick is not null:
                await item.OnClick(this);
                break;
        }
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

    private void OnValueSet()
    {
        _value = Value ?? string.Empty;

        if (_internalValueChange) return;

        // The textarea is uncontrolled (JS owns its value to preserve the caret),
        // so external changes must be pushed into it through the interop script.
        _previewValue = _value;
        _ = _js.BitMarkdownEditorSetValue(_Id, Value);
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
