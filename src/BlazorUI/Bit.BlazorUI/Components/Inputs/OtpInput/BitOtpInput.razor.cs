using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The OTP input is used for MFA procedure of authenticating users by a one-time password.
/// </summary>
public partial class BitOtpInput : BitInputBase<string?>
{
    private string _labelId = default!;
    private string?[] _inputIds = default!;
    private string?[] _inputValues = default!;
    private bool[] _inputFocusStates = default!;
    private ElementReference[] _inputRefs = default!;
    private DotNetObjectReference<BitOtpInput> _dotnetObj = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// If true, the first input is auto focused.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// Enables auto shifting the indexes while clearing the inputs using Delete or Backspace.
    /// </summary>
    [Parameter] public bool AutoShift { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitOtpInput.
    /// </summary>
    [Parameter] public BitOtpInputClassStyles? Classes { get; set; }

    /// <summary>
    /// Label displayed above the inputs.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Custom template for the label displayed above the inputs.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Length of the OTP or number of the inputs.
    /// </summary>
    [Parameter] public int Length { get; set; } = 5;

    /// <summary>
    /// Callback for when all of the inputs are filled.
    /// </summary>
    [Parameter] public EventCallback<string?> OnFill { get; set; }

    /// <summary>
    /// onfocusin event callback for each input.
    /// </summary>
    [Parameter] public EventCallback<(FocusEventArgs Event, int Index)> OnFocusIn { get; set; }

    /// <summary>
    /// onfocusout event callback for each input.
    /// </summary>
    [Parameter] public EventCallback<(FocusEventArgs Event, int Index)> OnFocusOut { get; set; }

    /// <summary>
    /// oninput event callback for each input.
    /// </summary>
    [Parameter] public EventCallback<(ChangeEventArgs Event, int Index)> OnInput { get; set; }

    /// <summary>
    /// onkeydown event callback for each input.
    /// </summary>
    [Parameter] public EventCallback<(KeyboardEventArgs Event, int Index)> OnKeyDown { get; set; }

    /// <summary>
    /// onpaste event callback for each input.
    /// </summary>
    [Parameter] public EventCallback<(ClipboardEventArgs Event, int Index)> OnPaste { get; set; }

    /// <summary>
    /// Defines whether to render inputs in the opposite direction.
    /// </summary>
    [Parameter] public bool Reversed { get; set; }

    /// <summary>
    /// The size of the inputs.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitOtpInput.
    /// </summary>
    [Parameter] public BitOtpInputClassStyles? Styles { get; set; }

    /// <summary>
    /// Type of the inputs.
    /// </summary>
    [Parameter] public BitInputType? Type { get; set; }

    /// <summary>
    /// Defines whether to render inputs vertically.
    /// </summary>
    [Parameter] public bool Vertical { get; set; }



    /// <summary>
    /// The ElementReferences to the input elements of the BitOtpInput.
    /// </summary>
    public ElementReference[] InputElements => _inputRefs;

    /// <summary>
    /// Gives focus to a specific input element of the BitOtpInput.
    /// </summary>
    public ValueTask FocusAsync(int index = 0) => _inputRefs[index].FocusAsync();



    [JSInvokable("SetValue")]
    public async Task _SetValue(string value)
    {
        if (IsEnabled is false || InvalidValueBinding()) return;
        if (value.HasNoValue()) return;
        if (Type is BitInputType.Number && int.TryParse(value, out _) is false) return;

        SetInputsValue(value);

        CurrentValueAsString = string.Join(string.Empty, _inputValues);

        await CallOnFill();
    }



    protected override string RootElementClass => "bit-otp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Reversed ? "bit-otp-rvs" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-otp-sm",
            BitSize.Medium => "bit-otp-md",
            BitSize.Large => "bit-otp-lg",
            _ => "bit-otp-md"
        });

        ClassBuilder.Register(() => Vertical ? "bit-otp-vrt" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        _labelId = $"BitOtpInput-{UniqueId}-label";
        _inputIds = Enumerable.Range(0, Length).Select(i => $"BitOtpInput-{UniqueId}-input-{i}").ToArray();

        _inputRefs = new ElementReference[Length];

        _inputValues = new string[Length];

        _inputFocusStates = new bool[Length];

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
            await _js.BitOtpInputSetup(_Id, _dotnetObj, inputRef);
        }
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        result = value;
        parsingErrorMessage = null;
        return true;
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            _dotnetObj?.Dispose();
            await _js.BitOtpInputDispose(_Id);
        }

        base.Dispose(disposing);
    }



    private string GetInputType() => Type switch
    {
        BitInputType.Text => "text",
        BitInputType.Number => "number",
        BitInputType.Password => "password",
        BitInputType.Email => "email",
        BitInputType.Tel => "tel",
        BitInputType.Url => "url",
        _ => "text"
    };

    private string GetInputMode() => Type switch
    {
        BitInputType.Text => "text",
        BitInputType.Number => "numeric",
        _ => "text"
    };

    private string GetInputStyles(int index)
    {
        StringBuilder cssStyles = new();

        if (Styles?.Input is not null)
        {
            cssStyles.Append(Styles?.Input);
        }

        if (Styles?.Focused is not null && _inputFocusStates[index])
        {
            cssStyles.Append(' ').Append(Styles?.Focused);
        }

        return cssStyles.ToString();
    }

    private string GetInputClasses(int index)
    {
        StringBuilder cssClasses = new();

        cssClasses.Append("bit-otp-inp");

        if (Classes?.Input is not null)
        {
            cssClasses.Append(' ').Append(Classes?.Input);
        }

        if (Classes?.Focused is not null && _inputFocusStates[index])
        {
            cssClasses.Append(' ').Append(Classes?.Focused);
        }

        return cssClasses.ToString();
    }

    private async Task HandleOnFocusIn(FocusEventArgs e, int index)
    {
        if (IsEnabled is false) return;

        _inputFocusStates[index] = true;
        await OnFocusIn.InvokeAsync((e, index));
    }

    private async Task HandleOnFocusOut(FocusEventArgs e, int index)
    {
        if (IsEnabled is false) return;

        _inputFocusStates[index] = false;
        await OnFocusOut.InvokeAsync((e, index));
    }

    private async Task HandleOnInput(ChangeEventArgs e, int index)
    {
        var oldValue = _inputValues[index];
        var newValue = e.Value?.ToString()?.Trim() ?? string.Empty;

        _inputValues[index] = string.Empty;
        await Task.Delay(1); // waiting for input default behavior before setting a new value.

        if (IsEnabled is false || InvalidValueBinding())
        {
            _inputValues[index] = oldValue;
        }
        else if (newValue.HasValue())
        {
            var diff = DiffValues(oldValue ?? string.Empty, newValue);

            if (Type is BitInputType.Number && int.TryParse(diff, out _) is false)
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

        CurrentValueAsString = string.Join(string.Empty, _inputValues);

        await OnInput.InvokeAsync((e, index));
        await CallOnFill();
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e, int index)
    {
        if (IsEnabled is false || InvalidValueBinding()) return;
        if (e.Code is null) return;

        await NavigateInput(e.Code, e.Key, index);

        await OnKeyDown.InvokeAsync((e, index));
    }

    private async Task HandleOnPaste(ClipboardEventArgs e, int index)
    {
        if (IsEnabled is false) return;

        await OnPaste.InvokeAsync((e, index));
    }

    private async Task NavigateInput(string code, string key, int index)
    {
        int nextIndex = Math.Min(index + 1, Length - 1);
        int previousIndex = Math.Max(index - 1, 0);

        if (code is "Backspace" || key is "Backspace")
        {
            await Task.Delay(1);
            await _inputRefs[previousIndex].FocusAsync();
            if (AutoShift)
            {
                for (int i = index; i < Length; i++)
                {
                    _inputValues[i] = i < Length - 1 ? _inputValues[i + 1] : string.Empty;
                }
            }
            return;
        }

        if (code is "Delete" || key is "Delete")
        {
            await Task.Delay(1);
            _inputValues[index] = string.Empty;
            await _inputRefs[index].FocusAsync();
            if (AutoShift)
            {
                for (int i = index; i < Length; i++)
                {
                    _inputValues[i] = i < Length - 1 ? _inputValues[i + 1] : string.Empty;
                }
            }
            return;
        }

        var targetIndex = code switch
        {
            "ArrowLeft" => Vertical ? index : ((Dir is BitDir.Rtl ^ Reversed) ? nextIndex : previousIndex),
            "ArrowRight" => Vertical ? index : ((Dir is BitDir.Rtl ^ Reversed) ? previousIndex : nextIndex),
            "ArrowUp" => Vertical ? (Reversed ? nextIndex : previousIndex) : index,
            "ArrowDown" => Vertical ? (Reversed ? previousIndex : nextIndex) : index,
            _ => -1 // For Tab key
        };

        if (targetIndex is not -1)
        {
            await _inputRefs[targetIndex].FocusAsync();
        }
    }

    private void SetInputsValue(string value)
    {
        var chars = value.Replace(" ", string.Empty, StringComparison.Ordinal).ToCharArray();

        for (int i = 0; i < Length; i++)
        {
            _inputValues[i] = chars.Length > i ? chars[i].ToString() : null;
        }
    }

    private async Task CallOnFill()
    {
        if (Length == CurrentValue?.Length)
        {
            await OnFill.InvokeAsync(CurrentValue);
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
