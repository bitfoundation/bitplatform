namespace Bit.BlazorUI.Legacy;

/// <summary>
/// BitMarkdownEditorLegacy is a simple editor like GitHub md editor.
/// </summary>
public partial class BitMarkdownEditorLegacy : BitComponentBase
{
    private string? _initialValue;
    private ElementReference _textAreaRef = default!;
    private DotNetObjectReference<BitMarkdownEditorLegacy>? _dotnetObj = null;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The default text value of the editor to use at initialization.
    /// </summary>
    [Parameter] public string? DefaultValue { get; set; }

    /// <summary>
    /// Callback for when the editor value changes.
    /// </summary>
    [Parameter] public EventCallback<string?> OnChange { get; set; }

    /// <summary>
    /// The two-way bound text value of the editor.
    /// </summary>
    [Parameter, TwoWayBound, CallOnSet(nameof(OnValueSet))]
    public string? Value { get; set; }



    /// <summary>
    /// Returns the current value of the editor.
    /// </summary>
    public async ValueTask<string> GetValue()
    {
        return await _js.BitMarkdownEditorGetValue(_Id);
    }

    /// <summary>
    /// Runs a specific command on the editor.
    /// </summary>
    public async ValueTask Run(BitMarkdownEditorLegacyCommand command)
    {
        await _js.BitMarkdownEditorRun(_Id, command switch
        { 
            BitMarkdownEditorLegacyCommand.Heading => "h",
            BitMarkdownEditorLegacyCommand.Bold => "b",
            BitMarkdownEditorLegacyCommand.Italic => "i",
            BitMarkdownEditorLegacyCommand.Link => "l",
            BitMarkdownEditorLegacyCommand.Picture => "p",
            BitMarkdownEditorLegacyCommand.Quote => "q",
            BitMarkdownEditorLegacyCommand.Code => "`",
            // -----------------------------------------
            BitMarkdownEditorLegacyCommand.CodeBlock => "```",
            _ => string.Empty
        });
    }

    /// <summary>
    /// Adds a specific content to the editor.
    /// </summary>
    public async ValueTask Add(string value, BitMarkdownEditorLegacyContentType type = BitMarkdownEditorLegacyContentType.Block)
    {
        await _js.BitMarkdownEditorAdd(_Id, value, type.ToString().ToLower());
    }



    [JSInvokable("OnChange")]
    public async Task _OnChange(string? value)
    {
        await AssignValue(value);
        await OnChange.InvokeAsync(value);
    }



    protected override string RootElementClass => "bit-mde";

    protected override void OnInitialized()
    {
        // Rendered as the textarea's value, so the markdown is there before the page is interactive: a
        // prerender or a static SSR page never runs the JS that sets it. It is captured once and never
        // re-rendered, since the editor's JS owns the textarea from then on (a later Value reaches it through
        // setValue) and a value attribute Blazor re-applied would overwrite what is being typed.
        _initialValue = Value ?? DefaultValue;

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        if ((ValueHasBeenSet && ValueChanged.HasDelegate) || OnChange.HasDelegate)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
        }

        await _js.BitMarkdownEditorInit(_Id, _textAreaRef, _dotnetObj, Value ?? DefaultValue);
    }



    private async ValueTask OnValueSet()
    {
        await _js.BitMarkdownEditorSetValue(_Id, Value);
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _dotnetObj?.Dispose();

        try
        {
            await _js.BitMarkdownEditorDispose(_Id);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here


        await base.DisposeAsync(disposing);
    }
}
