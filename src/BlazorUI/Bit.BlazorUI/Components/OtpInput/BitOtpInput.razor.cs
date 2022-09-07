using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitOtpInput
{
    private ElementReference _otpInpt = default!;
    private string?[] _inputValue = default!;
    private string _direction = default!;
    private int? _currentIndex;

    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// Count of input in Otp.
    /// </summary>
    [Parameter] public int InputCount { get; set; }

    /// <summary>
    /// If true, the first input is focused.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// Type of input shown as text, number, and password.
    /// </summary>
    [Parameter] public BitOtpInputType InputType { get; set; } = BitOtpInputType.Text;

    /// <summary>
    /// the OtpInput direction in four available directions.
    /// </summary>
    [Parameter] public BitOtpInputDirection Direction { get; set; } = BitOtpInputDirection.LeftToRight;

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
        _inputValue = new string[InputCount];

        _direction = Direction switch
        {
            BitOtpInputDirection.LeftToRight => "left-to-right",
            BitOtpInputDirection.RightToLeft => "right-to-left",
            BitOtpInputDirection.TopToBottom => "top-to-bottom",
            BitOtpInputDirection.BottomToTop => "bottom-to-top",
            _ => string.Empty
        };

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (AutoFocus)
            {
                await _otpInpt.FocusAsync();
            }

            var obj = DotNetObjectReference.Create(this);
            await _js.SetupOtpInputPaste(obj, _otpInpt);
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

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        NavigateInput(e.Code, e.Key);

        await OnKeyDown.InvokeAsync(e);
    }

    private void NavigateInput(string code, string key)
    {
        if (_currentIndex is null) return;

        int nextIndex = _currentIndex.Value + 1 >= InputCount ? _currentIndex.Value : _currentIndex.Value + 1;
        int previousIndex = _currentIndex.Value - 1 < 0 ? _currentIndex.Value : _currentIndex.Value - 1;

        if (code.Contains("Digit") || code.Contains("Key") || code.Contains("Numpad"))
        {
            _inputValue[_currentIndex.Value] = key;
            CurrentValue = string.Join("", _inputValue);
            _currentIndex = nextIndex;
        }
        else if (code is "Backspace")
        {
            _inputValue[_currentIndex.Value] = null;
            CurrentValue = string.Join("", _inputValue);
            _currentIndex = previousIndex;
        }
        else if (code is "Delete")
        {
            _inputValue[_currentIndex.Value] = null;
            CurrentValue = string.Join("", _inputValue);
            _currentIndex = nextIndex;
        }
        else if (code is "ArrowLeft")
        {
            _currentIndex = Direction is BitOtpInputDirection.LeftToRight
                ? previousIndex
                : Direction is BitOtpInputDirection.RightToLeft
                    ? nextIndex
                    : _currentIndex;

        }
        else if (code is "ArrowRight")
        {
            _currentIndex = Direction is BitOtpInputDirection.LeftToRight
                ? nextIndex
                : Direction is BitOtpInputDirection.RightToLeft
                    ? previousIndex
                    : _currentIndex;
        }
        else if (code is "ArrowUp")
        {
            _currentIndex = Direction is BitOtpInputDirection.TopToBottom
                ? previousIndex
                : Direction is BitOtpInputDirection.BottomToTop
                    ? nextIndex
                    : _currentIndex;
        }
        else if (code is "ArrowDown")
        {
            _currentIndex = Direction is BitOtpInputDirection.TopToBottom
                ? nextIndex
                : Direction is BitOtpInputDirection.BottomToTop
                    ? previousIndex
                    : _currentIndex;
        }
    }

    private async Task HandleOnFocusIn(FocusEventArgs e)
    {
        _currentIndex = 0;

        await OnFocusIn.InvokeAsync(e);
    }

    private async Task HandleOnFocusOut(FocusEventArgs e)
    {
        _currentIndex = null;

        await OnFocusOut.InvokeAsync(e);
    }

    private async Task HandleOnPaste(ClipboardEventArgs e)
    {
        await OnPaste.InvokeAsync(e);
    }

    [JSInvokable]
    public async Task SetPastedData(string data)
    {
        CurrentValue = data;
    }

    private void SyncCurrentValueWithArray()
    {
        if (CurrentValue is null) return;

        var splitedCurrentValue = CurrentValue.ToCharArray();
        for (int i = 0; i < InputCount; i++)
        {
            if (splitedCurrentValue.Length > i)
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
