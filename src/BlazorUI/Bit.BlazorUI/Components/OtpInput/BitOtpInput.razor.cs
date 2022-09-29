using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;

namespace Bit.BlazorUI;

public partial class BitOtpInput
{
    private ElementReference[] _inputRef = default!;
    private string[] _inputValue = default!;
    private string _inputType = default!;

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
                    await _js.SetupOtpInputPaste(obj, inputRef);
                }
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (CurrentValue != null && CurrentValue != string.Join("", _inputValue))
        {
            SyncCurrentValueWithArray();
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

    private async Task HandleOnKeyDown(KeyboardEventArgs e, int index)
    {
        if (IsEnabled is false || e.Code is null) return;

        await NavigateInput(e.Code, e.Key, index);

        await OnKeyDown.InvokeAsync(e);
    }

    private async Task NavigateInput(string code, string key, int index)
    {
        int nextIndex = index + 1 >= InputCount ? index : index + 1;
        int previousIndex = index - 1 < 0 ? index : index - 1;

        if ((code.Contains("Digit") || code.Contains("Numpad") || code.Contains("Key")) && (InputType is BitOtpInputType.Text || InputType is BitOtpInputType.Password))
        {
            _inputValue[index] = string.Empty;
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            _inputValue[index] = key;
            CurrentValue = string.Join("", _inputValue);
            await _inputRef[nextIndex].FocusAsync();
        }
        else if ((code.Contains("Digit") || code.Contains("Numpad")) && InputType is BitOtpInputType.Number)
        {
            _inputValue[index] = string.Empty;
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            _inputValue[index] = key;
            CurrentValue = string.Join("", _inputValue);
            await _inputRef[nextIndex].FocusAsync();
        }
        else if (code is "Backspace")
        {
            _inputValue[index] = " ";
            CurrentValue = string.Join("", _inputValue);
            await _inputRef[previousIndex].FocusAsync();
        }
        else if (code is "Delete")
        {
            _inputValue[index] = " ";
            CurrentValue = string.Join("", _inputValue);
            await _inputRef[nextIndex].FocusAsync();
        }
        else if (code is "ArrowLeft")
        {
            var leftIndex = Direction is BitOtpInputDirection.LeftToRight
                ? previousIndex
                : Direction is BitOtpInputDirection.RightToLeft
                    ? nextIndex
                    : index;

            await _inputRef[leftIndex].FocusAsync();
        }
        else if (code is "ArrowRight")
        {
            var rightIndex = Direction is BitOtpInputDirection.LeftToRight
                ? nextIndex
                : Direction is BitOtpInputDirection.RightToLeft
                    ? previousIndex
                    : index;

            await _inputRef[rightIndex].FocusAsync();
        }
        else if (code is "ArrowUp")
        {
            var upIndex = Direction is BitOtpInputDirection.TopToBottom
                ? previousIndex
                : Direction is BitOtpInputDirection.BottomToTop
                    ? nextIndex
                    : index;

            await _inputRef[upIndex].FocusAsync();
        }
        else if (code is "ArrowDown")
        {
            var downIndex = Direction is BitOtpInputDirection.TopToBottom
                ? nextIndex
                : Direction is BitOtpInputDirection.BottomToTop
                    ? previousIndex
                    : index;

            await _inputRef[downIndex].FocusAsync();
        }
    }

    private async Task HandleOnFocusIn(FocusEventArgs e, int index)
    {
        if (IsEnabled is false) return;

        await _inputRef[index].FocusAsync();

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
        SyncCurrentValueWithArray(pastedValue);
    }

    private void SyncCurrentValueWithArray(string? pastedValue = null)
    {
        if (IsEnabled is false) return;

        var value = pastedValue is null ? CurrentValue : pastedValue;

        if (InputType is BitOtpInputType.Number && int.TryParse(value, out _) is false) return;

        var splitedCurrentValue = value is null ? CurrentValue?.ToCharArray() : value.ToCharArray();
        for (int i = 0; i < InputCount; i++)
        {
            if (splitedCurrentValue?.Length > i)
            {
                _inputValue[i] = splitedCurrentValue[i].ToString();
            }
            else
            {
                _inputValue[i] = string.Empty;
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
