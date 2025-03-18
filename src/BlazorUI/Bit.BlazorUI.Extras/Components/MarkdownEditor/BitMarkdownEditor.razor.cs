﻿namespace Bit.BlazorUI;

/// <summary>
/// BitMarkdownEditor is a simple editor like GitHub md editor.
/// </summary>
public partial class BitMarkdownEditor : BitComponentBase
{
    private ElementReference _textAreaRef = default!;
    private DotNetObjectReference<BitMarkdownEditor>? _dotnetObj = null;



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
    [Parameter, TwoWayBound]
    public string? Value { get; set; }



    /// <summary>
    /// Returns the current value of the editor.
    /// </summary>
    public async ValueTask<string> GetValue()
    {
        return await _js.BitMarkdownEditorGetValue(_Id);
    }



    [JSInvokable("OnChange")]
    public async Task _OnChange(string? value)
    {
        await AssignValue(value);
        await OnChange.InvokeAsync(value);
    }



    protected override string RootElementClass => "bit-mde";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        if ((ValueHasBeenSet && ValueChanged.HasDelegate) || OnChange.HasDelegate)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
        }

        await _js.BitMarkdownEditorInit(_Id, _textAreaRef, _dotnetObj, DefaultValue);
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
