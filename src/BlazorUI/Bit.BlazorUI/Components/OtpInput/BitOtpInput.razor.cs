using System;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitOtpInput
{
    private ElementReference[] _inputRef = default!;
    private string?[] _inputValue = default!;

    [Parameter] public int InputCount { get; set; }

    [Parameter] public bool DefaultIsFirstInputFocused { get; set; }

    [Parameter] public BitOtpInputType Type { get; set; }

    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyUp { get; set; }

    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyPress { get; set; }

    [Parameter] public EventCallback<ClipboardEventArgs> OnPaste { get; set; }

    protected override string RootElementClass => "bit-otpi";

    protected override async Task OnInitializedAsync()
    {
        _inputRef = new ElementReference[InputCount];
        _inputValue = new string[InputCount];

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (DefaultIsFirstInputFocused)
            {
                await _inputRef[0].FocusAsync();
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e, int index)
    {
        if (IsEnabled is false) return;

        if (e.Code is "Backspace" || e.Code is "ArrowLeft")
        {
            int previousInput = index - 1;

            if (previousInput < 0)
            {
                await _inputRef[index].FocusAsync();
            }
            else
            {
                await _inputRef[previousInput].FocusAsync();
            }
        }
        else if (e.Code.Contains("Digit") || e.Code.Contains("Key") || e.Code is "ArrowRight")
        {
            int nextInput = index + 1;

            if (nextInput >= InputCount)
            {
                await RootElement.FocusAsync();
            }
            else
            {
                await _inputRef[nextInput].FocusAsync();
            }
        }

        CurrentValue = string.Join("", _inputValue);
        await OnKeyDown.InvokeAsync(e);
    }

    private async Task HandleOnPaste(ClipboardEventArgs e, int index)
    {
        // TODO
        await OnPaste.InvokeAsync(e);
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }
}
