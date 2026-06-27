namespace Bit.BlazorUI;

/// <summary>
/// BitRichTextEditor is a native WYSIWYG rich text editor. All component logic lives in C#;
/// a thin JavaScript bridge handles the browser-only concerns (contenteditable events,
/// formatting commands, and selection). Two-way bind the HTML content with <c>@bind-Value</c>.
/// </summary>
public partial class BitRichTextEditor : BitComponentBase
{
    private bool _initialized;
    private string _currentHtml = "";
    private ElementReference _editorRef = default!;
    private BitRichTextEditorContentFacts _facts;
    private BitRichTextEditorSelectionState _state = new();
    private DotNetObjectReference<BitRichTextEditor>? _dotnetObj = null;

    /// <summary>Transient inline error message shown in the editor chrome.</summary>
    private string? _inlineError;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Custom CSS classes for different parts of the rich text editor.
    /// </summary>
    [Parameter] public BitRichTextEditorClassStyles? Classes { get; set; }

    /// <summary>
    /// Debounce window (ms) for content-change notifications while typing.
    /// </summary>
    [Parameter] public int DebounceMs { get; set; } = 200;

    /// <summary>
    /// Minimum height of the editing surface (any CSS length).
    /// </summary>
    [Parameter] public string Height { get; set; } = "300px";

    /// <summary>
    /// Callback for when the editor loses focus.
    /// </summary>
    [Parameter] public EventCallback OnBlur { get; set; }

    /// <summary>
    /// Callback for when the editor content changes.
    /// </summary>
    [Parameter] public EventCallback<string?> OnChange { get; set; }

    /// <summary>
    /// Callback for when the editor encounters a recoverable error (invalid input, etc.).
    /// </summary>
    [Parameter] public EventCallback<BitRichTextEditorError> OnError { get; set; }

    /// <summary>
    /// Callback for when the editor gains focus.
    /// </summary>
    [Parameter] public EventCallback OnFocus { get; set; }

    /// <summary>
    /// The placeholder value of the editor shown while it is empty.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Makes the editor readonly.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Whether the formatting toolbar is shown.
    /// </summary>
    [Parameter] public bool ShowToolbar { get; set; } = true;

    /// <summary>
    /// Custom CSS styles for different parts of the rich text editor.
    /// </summary>
    [Parameter] public BitRichTextEditorClassStyles? Styles { get; set; }

    /// <summary>
    /// Which toolbar groups to display.
    /// </summary>
    [Parameter] public BitRichTextEditorToolbar Toolbar { get; set; } = BitRichTextEditorToolbar.All;

    /// <summary>
    /// The two-way bound HTML content of the editor.
    /// </summary>
    [Parameter, TwoWayBound, CallOnSet(nameof(OnValueSet))]
    public string? Value { get; set; }



    /// <summary>
    /// Moves keyboard focus into the editor.
    /// </summary>
    public async ValueTask FocusAsync()
    {
        await _js.BitRichTextEditorFocus(_editorRef);
    }

    /// <summary>
    /// Returns the current HTML content of the editor.
    /// </summary>
    public async ValueTask<string> GetHtmlAsync()
    {
        if (_initialized is false) return _currentHtml;
        return await _js.BitRichTextEditorGetHtml(_editorRef);
    }

    /// <summary>
    /// Runs a raw editing command against the editor.
    /// </summary>
    public Task ExecuteCommandAsync(string command, string? value = null) => ExecAsync(command, value);



    private bool ControlsDisabled => ReadOnly || _inSourceView;

    private bool Has(BitRichTextEditorToolbar group) => Toolbar.HasFlag(group);



    // ---- callbacks from JS ----

    [JSInvokable("OnContentChanged")]
    public async Task _OnContentChanged(string html, BitRichTextEditorContentFacts facts)
    {
        _currentHtml = html;
        _facts = facts;
        if (ShowCount)
        {
            StateHasChanged();
        }

        await AssignValue(html);
        NotifyEditContextChanged();
        await OnChange.InvokeAsync(html);
    }

    [JSInvokable("OnSelectionChanged")]
    public void _OnSelectionChanged(BitRichTextEditorSelectionState state)
    {
        _state = state;
        StateHasChanged();
    }

    [JSInvokable("OnFocused")]
    public Task _OnFocused() => OnFocus.InvokeAsync();

    [JSInvokable("OnBlurred")]
    public Task _OnBlurred() => OnBlur.InvokeAsync();

    /// <summary>Reported by the bridge when a formatting command fails; content is unchanged.</summary>
    [JSInvokable("OnCommandError")]
    public Task _OnCommandError(string command, string message)
        => RaiseErrorAsync(new BitRichTextEditorError("command-failed", $"Command '{command}' failed: {message}"));



    // ---- commands ----

    private async Task ExecAsync(string command, string? value = null)
    {
        if (ReadOnly) return;
        await _js.BitRichTextEditorExec(_editorRef, command, value);
    }

    private Task UndoAsync() => ExecAsync("undo");
    private Task RedoAsync() => ExecAsync("redo");

    private Task OnBlockFormatChanged(ChangeEventArgs e)
        => ExecBlockAsync(e.Value?.ToString() ?? "p");

    private async Task ExecBlockAsync(string tag)
    {
        if (ReadOnly) return;
        await _js.BitRichTextEditorExecBlock(_editorRef, tag);
    }

    private Task FormatBlockToggleAsync(string tag)
        => ExecBlockAsync(_state.Block == tag ? "p" : tag);

    private async Task ClearFormattingAsync()
    {
        if (ReadOnly) return;
        await _js.BitRichTextEditorExec(_editorRef, "removeFormat", null);
        await _js.BitRichTextEditorExecBlock(_editorRef, "p");
    }



    // ---- helpers ----

    private async Task RaiseErrorAsync(BitRichTextEditorError error)
    {
        _inlineError = error.Message;
        StateHasChanged();
        await OnError.InvokeAsync(error);
    }

    private void ClearInlineError()
    {
        if (_inlineError is not null)
        {
            _inlineError = null;
            StateHasChanged();
        }
    }



    protected override string RootElementClass => "bit-rte";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
        ClassBuilder.Register(() => _fullScreen ? "bit-rte-fsc" : string.Empty);
        ClassBuilder.Register(() => ReadOnly ? "bit-rte-ro" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        _dotnetObj = DotNetObjectReference.Create(this);
        _currentHtml = Value ?? "";

        await _js.BitRichTextEditorSetup(_editorRef, _dotnetObj, new()
        {
            Debounce = DebounceMs,
            Policy = BuildPolicyPayload(),
            HasUpload = OnImageUpload is not null,
            PlainTextPaste = PasteAsPlainText,
            MaxLength = MaxLength
        });

        if (ShowToolbar)
        {
            await _js.BitRichTextEditorEnableToolbarRoving(_toolbarRef);
        }

        if (string.IsNullOrEmpty(_currentHtml) is false)
        {
            await _js.BitRichTextEditorSetHtml(_editorRef, _currentHtml);
        }

        _initialized = true;
    }

    private async ValueTask OnValueSet()
    {
        if (_initialized is false) return;
        if (_inSourceView) return;
        if ((Value ?? "") == _currentHtml) return; // originated from the editor

        var html = Value ?? "";
        if (SanitizationPolicy is not null && string.IsNullOrEmpty(html) is false)
        {
            html = await _js.BitRichTextEditorSanitizeHtml(_editorRef, html);
        }
        _currentHtml = html;
        await _js.BitRichTextEditorSetHtml(_editorRef, html);
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _dotnetObj?.Dispose();

        try
        {
            await _js.BitRichTextEditorDispose(_editorRef);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await base.DisposeAsync(disposing);
    }
}
