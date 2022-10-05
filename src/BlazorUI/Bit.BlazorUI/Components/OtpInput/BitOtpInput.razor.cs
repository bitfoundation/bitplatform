using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;

namespace Bit.BlazorUI;

public partial class BitOtpInput
{
    private ElementReference[] _inputRef = default!;
    private string?[] _inputValue = default!;
    private string _inputType = default!;
    private string _inputMode = default!;

    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// If true, the first input is focused.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// The OtpInput direction in four available directions.
    /// </summary>
    [Parameter] public BitOtpInputDirection Direction { get; set; } = BitOtpInputDirection.LeftToRight;

    /// <summary>
    /// Type of input shown as text, number, and password.
    /// </summary>
    [Parameter] public BitOtpInputType InputType { get; set; } = BitOtpInputType.Text;

    /// <summary>
    /// Count of input in Otp.
    /// </summary>
    [Parameter] public int InputCount { get; set; }

    /// <summary>
    /// Callback for when OtpInput value changed.
    /// </summary>
    [Parameter] public EventCallback<ChangeEventArgs> OnInput { get; set; }

    /// <summary>
    /// Callback for when a keyboard key is pressed.
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>
    /// Callback for when OtpInput is focused in.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

    /// <summary>
    /// Callback for when OtpInput is focused out.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

    /// <summary>
    /// Callback for when in the OtpInput paste a content.
    /// </summary>
    [Parameter] public EventCallback<ClipboardEventArgs> OnPaste { get; set; }

    protected override string RootElementClass => "bit-otp";

    protected override async Task OnInitializedAsync()
    {
        _inputRef = new ElementReference[InputCount];

        _inputValue = new string[InputCount];

        _inputType = InputType switch
        {
            BitOtpInputType.Text => "text",
            BitOtpInputType.Number => "number",
            BitOtpInputType.Password => "password",
            _ => string.Empty
        };

        _inputMode = InputType switch
        {
            BitOtpInputType.Text => "text",
            BitOtpInputType.Number => "numeric",
            BitOtpInputType.Password => "text",
            _ => string.Empty
        };

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (IsEnabled)
            {
                if (AutoFocus)
                {
                    await _inputRef[0].FocusAsync();
                }

                foreach (var inputRef in _inputRef)
                {
                    var obj = DotNetObjectReference.Create(this);
                    await _js.SetupOtpInput(obj, inputRef);
                }
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (CurrentValue != null && CurrentValue != string.Join("", _inputValue))
        {
            await SyncCurrentValueWithArray(CurrentValue);
        }

        await base.OnParametersSetAsync();
    }

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => ValueInvalid is true
                                   ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => Direction switch
        {
            BitOtpInputDirection.LeftToRight => "left-to-right",
            BitOtpInputDirection.RightToLeft => "right-to-left",
            BitOtpInputDirection.TopToBottom => "top-to-bottom",
            BitOtpInputDirection.BottomToTop => "bottom-to-top",
            _ => string.Empty
        });
    }

    private async Task HandleOnInput(ChangeEventArgs e, int index)
    {
        if (IsEnabled is false) return;

        _inputValue[index] = string.Empty;
        await Task.Delay(TimeSpan.FromMilliseconds(1)); // waiting for input default behavior before setting a new value.

        var value = e.Value!.ToString()!;
        if (value.HasValue())
        {
            int nextIndex = index + 1;
            if (nextIndex < InputCount) await _inputRef[nextIndex].FocusAsync();
            _inputValue[index] = value;
        }
        else
        {
            _inputValue[index] = null;
        }

        await SyncCurrentValueWithArray(string.Join("", _inputValue));

        await OnInput.InvokeAsync(e);
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e, int index)
    {
        if (IsEnabled is false || e.Code is null) return;

        await NavigateInput(e.Code, e.Key, index);

        await OnKeyDown.InvokeAsync(e);
    }

    private async Task NavigateInput(string code, string key, int index)
    {
        int nextIndex = index + 1;
        int previousIndex = index - 1;

        if ((code is "Backspace" || key is "Backspace") && previousIndex >= 0)
        {
            await _inputRef[previousIndex].FocusAsync();
        }
        else if (code is "ArrowLeft")
        {
            if (Direction is BitOtpInputDirection.LeftToRight && previousIndex >= 0)
            {
                await _inputRef[previousIndex].FocusAsync();
            }
            else if (Direction is BitOtpInputDirection.RightToLeft && nextIndex < InputCount)
            {
                await _inputRef[nextIndex].FocusAsync();
            }
        }
        else if (code is "ArrowRight")
        {
            if (Direction is BitOtpInputDirection.LeftToRight && nextIndex < InputCount)
            {
                await _inputRef[nextIndex].FocusAsync();
            }
            else if(Direction is BitOtpInputDirection.RightToLeft && previousIndex >= 0)
            {
                await _inputRef[previousIndex].FocusAsync();
            }
        }
        else if (code is "ArrowUp")
        {
            if (Direction is BitOtpInputDirection.TopToBottom && previousIndex >= 0)
            {
                await _inputRef[previousIndex].FocusAsync();
            }
            else if(Direction is BitOtpInputDirection.BottomToTop && nextIndex < InputCount)
            {
                await _inputRef[nextIndex].FocusAsync();
            }
        }
        else if (code is "ArrowDown")
        {
            if (Direction is BitOtpInputDirection.TopToBottom && nextIndex < InputCount)
            {
                await _inputRef[nextIndex].FocusAsync();
            }
            else if (Direction is BitOtpInputDirection.BottomToTop && previousIndex >= 0)
            {
                await _inputRef[previousIndex].FocusAsync();
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

    [JSInvokable]
    public async Task SetPastedData(string pastedValue)
    {
        if (pastedValue.HasNoValue()) return;

        if (InputType is BitOtpInputType.Number && int.TryParse(pastedValue, out _) is false) return;

        await SyncCurrentValueWithArray(pastedValue);
    }

    private async Task SyncCurrentValueWithArray(string currentValue)
    {
        if (IsEnabled is false) return;

        var splitedCurrentValue = currentValue.Replace(" ", "", StringComparison.Ordinal).ToCharArray();

        for (int i = 0; i < InputCount; i++)
        {
            if (splitedCurrentValue?.Length > i)
            {
                _inputValue[i] = splitedCurrentValue[i].ToString();
            }
            else
            {
                _inputValue[i] = null;
            }
        }

        CurrentValue = string.Join("", _inputValue);
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }
}
