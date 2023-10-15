using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitOtpInput : IDisposable
{
    private string?[] _inputValues = default!;
    private ElementReference[] _inputRefs = default!;
    private DotNetObjectReference<BitOtpInput> _dotnetObj = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// If true, the first input is auto focused.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitOtpInput.
    /// </summary>
    [Parameter] public BitOtpInputClassStyles? Classes { get; set; }

    /// <summary>
    /// The render direction of the inputs.
    /// </summary>
    [Parameter] public BitOtpInputDirection Direction { get; set; } = BitOtpInputDirection.LeftToRight;

    /// <summary>
    /// Type of the inputs.
    /// </summary>
    [Parameter] public BitOtpInputType InputType { get; set; } = BitOtpInputType.Text;

    /// <summary>
    /// Length of the OTP or number of the inputs.
    /// </summary>
    [Parameter] public int Length { get; set; } = 5;

    /// <summary>
    /// Callback for when the OtpInput value changes.
    /// </summary>
    [Parameter] public EventCallback<string?> OnChange { get; set; }

    /// <summary>
    /// onfocusin event callback for each input.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

    /// <summary>
    /// onfocusout event callback for each input.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

    /// <summary>
    /// oninput event callback for each input.
    /// </summary>
    [Parameter] public EventCallback<ChangeEventArgs> OnInput { get; set; }

    /// <summary>
    /// onkeydown event callback for each input.
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>
    /// onpaste event callback for each input.
    /// </summary>
    [Parameter] public EventCallback<ClipboardEventArgs> OnPaste { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitOtpInput.
    /// </summary>
    [Parameter] public BitOtpInputClassStyles? Styles { get; set; }



    [JSInvokable]
    public async Task SetPastedData(string pastedValue)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;
        if (pastedValue.HasNoValue()) return;
        if (InputType is BitOtpInputType.Number && int.TryParse(pastedValue, out _) is false) return;

        SetInputsValue(pastedValue);

        CurrentValue = string.Join(string.Empty, _inputValues);

        await OnChange.InvokeAsync(CurrentValue);
    }



    protected override string RootElementClass => "bit-otp";

    protected override void OnInitialized()
    {
        _inputRefs = new ElementReference[Length];

        _inputValues = new string[Length];

        _dotnetObj = DotNetObjectReference.Create(this);

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        if (CurrentValue is not null && CurrentValue != string.Join(string.Empty, _inputValues))
        {
            SetInputsValue(CurrentValue);
        }

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false || IsEnabled is false) return;

        if (AutoFocus)
        {
            await _inputRefs[0].FocusAsync();
        }

        foreach (var inputRef in _inputRefs)
        {
            await _js.SetupOtpInput(_dotnetObj, inputRef);
        }
    }

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Direction switch
        {
            BitOtpInputDirection.LeftToRight => $"{RootElementClass}-ltr",
            BitOtpInputDirection.RightToLeft => $"{RootElementClass}-rtl",
            BitOtpInputDirection.TopToBottom => $"{RootElementClass}-ttb",
            BitOtpInputDirection.BottomToTop => $"{RootElementClass}-btt",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }
    
    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dotnetObj.Dispose();
        }

        base.Dispose(disposing);
    }



    private string GetInputType() => InputType switch
    {
        BitOtpInputType.Text => "text",
        BitOtpInputType.Number => "number",
        BitOtpInputType.Password => "password",
        _ => string.Empty
    };

    private string GetInputMode() => InputType switch
    {
        BitOtpInputType.Text => "text",
        BitOtpInputType.Number => "numeric",
        BitOtpInputType.Password => "text",
        _ => string.Empty
    };

    private async Task HandleOnInput(ChangeEventArgs e, int index)
    {
        var oldValue = _inputValues[index];
        var newValue = e.Value?.ToString()?.Trim() ?? string.Empty;

        _inputValues[index] = string.Empty;
        await Task.Delay(1); // waiting for input default behavior before setting a new value.

        if (IsEnabled is false || (ValueHasBeenSet && ValueChanged.HasDelegate is false))
        {
            _inputValues[index] = oldValue;
        }
        else if (newValue.HasValue())
        {
            var diff = DiffValues(oldValue ?? string.Empty, newValue);

            if (InputType is BitOtpInputType.Number && int.TryParse(diff, out _) is false)
            {
                _inputValues[index] = oldValue;
            }
            else
            {
                if (diff.Length > 1)
                {
                    SetInputsValue(diff);
                }
                else
                {
                    _inputValues[index] = diff;
                    int nextIndex = index + 1;
                    if (nextIndex < Length) await _inputRefs[nextIndex].FocusAsync();
                }
            }
        }
        else
        {
            _inputValues[index] = null;
        }

        CurrentValue = string.Join(string.Empty, _inputValues);

        await OnInput.InvokeAsync(e);
        await OnChange.InvokeAsync(CurrentValue);
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e, int index)
    {
        if (IsEnabled is false || e.Code is null) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        await NavigateInput(e.Code, e.Key, index);

        await OnKeyDown.InvokeAsync(e);
    }

    private async Task NavigateInput(string code, string key, int index)
    {
        int nextIndex = index + 1;
        int previousIndex = index - 1;

        if ((code is "Backspace" || key is "Backspace") && previousIndex >= 0)
        {
            await Task.Delay(1);
            await _inputRefs[previousIndex].FocusAsync();
        }
        else if (code is "ArrowLeft")
        {
            if (Direction is BitOtpInputDirection.LeftToRight && previousIndex >= 0)
            {
                await _inputRefs[previousIndex].FocusAsync();
            }
            else if (Direction is BitOtpInputDirection.RightToLeft && nextIndex < Length)
            {
                await _inputRefs[nextIndex].FocusAsync();
            }
        }
        else if (code is "ArrowRight")
        {
            if (Direction is BitOtpInputDirection.LeftToRight && nextIndex < Length)
            {
                await _inputRefs[nextIndex].FocusAsync();
            }
            else if (Direction is BitOtpInputDirection.RightToLeft && previousIndex >= 0)
            {
                await _inputRefs[previousIndex].FocusAsync();
            }
        }
        else if (code is "ArrowUp")
        {
            if (Direction is BitOtpInputDirection.TopToBottom && previousIndex >= 0)
            {
                await _inputRefs[previousIndex].FocusAsync();
            }
            else if (Direction is BitOtpInputDirection.BottomToTop && nextIndex < Length)
            {
                await _inputRefs[nextIndex].FocusAsync();
            }
        }
        else if (code is "ArrowDown")
        {
            if (Direction is BitOtpInputDirection.TopToBottom && nextIndex < Length)
            {
                await _inputRefs[nextIndex].FocusAsync();
            }
            else if (Direction is BitOtpInputDirection.BottomToTop && previousIndex >= 0)
            {
                await _inputRefs[previousIndex].FocusAsync();
            }
        }
    }

    private async Task HandleOnFocusIn(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnFocusIn.InvokeAsync(e);
    }

    private async Task HandleOnFocusOut(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnFocusOut.InvokeAsync(e);
    }

    private async Task HandleOnPaste(ClipboardEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnPaste.InvokeAsync(e);
    }

    private void SetInputsValue(string value)
    {
        var chars = value.Replace(" ", string.Empty, StringComparison.Ordinal).ToCharArray();

        for (int i = 0; i < Length; i++)
        {
            _inputValues[i] = chars.Length > i ? chars[i].ToString() : null;
        }
    }

    private static string DiffValues(string oldValue, string newValue)
    {
        var oldLength = oldValue.Length;
        var newLength = newValue.Length;

        if (newLength == 1) return newValue;
        if (newLength < oldLength) return newValue;

        if (newValue[..oldLength] == oldValue) return newValue[oldLength..newLength];

        if (newValue.Substring(newLength - oldLength, oldLength) == oldValue) return newValue[..(newLength - oldLength)];

        return newValue;
    }
}
