﻿using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;

namespace Bit.BlazorUI;

public partial class BitOtpInput
{
    private ElementReference _otpInpt = default!;
    private string[] _inputValue = default!;
    private string _direction = default!;
    private int? _currentIndex;
    private bool _keyDownPreventDefault;
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

            if (IsEnabled)
            {
                var obj = DotNetObjectReference.Create(this);

                await _js.SetupOtpInputPaste(obj, _otpInpt);
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

        _keyDownPreventDefault = false;

        int nextIndex = _currentIndex.Value + 1 >= InputCount ? _currentIndex.Value : _currentIndex.Value + 1;
        int previousIndex = _currentIndex.Value - 1 < 0 ? _currentIndex.Value : _currentIndex.Value - 1;

        if ((code.Contains("Digit") || code.Contains("Numpad") || code.Contains("Key")) && (InputType is BitOtpInputType.Text || InputType is BitOtpInputType.Password))
        {
            _inputValue[_currentIndex.Value] = key;
            CurrentValue = string.Join("", _inputValue);
            _currentIndex = nextIndex;
        }
        else if ((code.Contains("Digit") || code.Contains("Numpad")) && InputType is BitOtpInputType.Number)
        {
            _inputValue[_currentIndex.Value] = key;
            CurrentValue = string.Join("", _inputValue);
            _currentIndex = nextIndex;
        }
        else if (code is "Backspace")
        {
            _inputValue[_currentIndex.Value] = " ";
            CurrentValue = string.Join("", _inputValue);
            _currentIndex = previousIndex;
        }
        else if (code is "Delete")
        {
            _inputValue[_currentIndex.Value] = " ";
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
            _keyDownPreventDefault = true;

            _currentIndex = Direction is BitOtpInputDirection.TopToBottom
                ? previousIndex
                : Direction is BitOtpInputDirection.BottomToTop
                    ? nextIndex
                    : _currentIndex;
        }
        else if (code is "ArrowDown")
        {
            _keyDownPreventDefault = true;

            _currentIndex = Direction is BitOtpInputDirection.TopToBottom
                ? nextIndex
                : Direction is BitOtpInputDirection.BottomToTop
                    ? previousIndex
                    : _currentIndex;
        }
    }

    private async Task HandleOnFocusIn(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        _currentIndex = 0;

        await OnFocusIn.InvokeAsync(e);
    }

    private async Task HandleOnFocusOut(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        _currentIndex = null;

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
