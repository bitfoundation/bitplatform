using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitOtpInput
{
    private ElementReference _otpInpt = default!;
    private string?[] _inputValue = default!;
    private string _direction = default!;
    private int? _currentIndex;

    [Inject] private IJSRuntime _js { get; set; } = default!;

    [Parameter] public int InputCount { get; set; }

    [Parameter] public bool AutoFocus { get; set; }

    [Parameter] public BitOtpInputType InputType { get; set; } = BitOtpInputType.Text;

    [Parameter] public BitOtpInputDirection Direction { get; set; } = BitOtpInputDirection.LeftToRight;

    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

    [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

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
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        NavigateInput(e.Code, e.Key);

        CurrentValue = string.Join("", _inputValue);

        await OnKeyDown.InvokeAsync(e);
    }

    private void NavigateInput(string code, string key)
    {
        if (_currentIndex is null) return;

        int nextInput = _currentIndex.Value + 1 >= InputCount ? _currentIndex.Value : _currentIndex.Value + 1;
        int previousInput = _currentIndex.Value - 1 < 0 ? _currentIndex.Value : _currentIndex.Value - 1;

        if (code.Contains("Digit") || code.Contains("Key") || code.Contains("Numpad"))
        {
            _inputValue[_currentIndex.Value] = key;
            _currentIndex = nextInput;
        }
        else if (code is "Backspace")
        {
            _inputValue[_currentIndex.Value] = null;
            _currentIndex = previousInput;
        }
        else if (code is "Delete")
        {
            _inputValue[_currentIndex.Value] = null;
            _currentIndex = nextInput;
        }
        else if (code is "ArrowLeft")
        {
            _currentIndex = Direction is BitOtpInputDirection.LeftToRight
                ? previousInput
                : Direction is BitOtpInputDirection.RightToLeft
                    ? nextInput
                    : _currentIndex;

        }
        else if (code is "ArrowRight")
        {
            _currentIndex = Direction is BitOtpInputDirection.LeftToRight
                ? nextInput
                : Direction is BitOtpInputDirection.RightToLeft
                    ? previousInput
                    : _currentIndex;
        }
        else if (code is "ArrowUp")
        {
            _currentIndex = Direction is BitOtpInputDirection.TopToBottom
                ? previousInput
                : Direction is BitOtpInputDirection.BottomToTop
                    ? nextInput
                    : _currentIndex;
        }
        else if (code is "ArrowDown")
        {
            _currentIndex = Direction is BitOtpInputDirection.TopToBottom
                ? nextInput
                : Direction is BitOtpInputDirection.BottomToTop
                    ? previousInput
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
        var data = await _js.GetPastedData();

        if (data is not null)
        {
            var splitedData = data.ToCharArray();

            for (int i = 0; i < splitedData.Length; i++)
            {
                if (i < InputCount)
                {
                    _inputValue[i] = splitedData[i].ToString();
                }
            }

            CurrentValue = string.Join("", _inputValue);
        }

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
