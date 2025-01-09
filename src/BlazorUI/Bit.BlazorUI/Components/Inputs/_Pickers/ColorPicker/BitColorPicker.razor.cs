namespace Bit.BlazorUI;

/// <summary>
/// The color picker (ColorPicker) is used to browse through and select colors. By default, it lets people navigate through colors on a color spectrum, or specify a color in either Red-Green-Blue (RGB), or alpha color code; or Hexadecimal textboxes.
/// </summary>
public partial class BitColorPicker : BitComponentBase, IAsyncDisposable
{
    private bool _disposed;
    private double _selectedHue;
    private double _selectedValue = 1;
    private double _selectedSaturation = 1;
    private string? _saturationPickerStyle;
    private BitInternalColor _color = new();
    private BitInternalColorType _colorType;
    private bool _saturationPickerPointerDown;
    private string? _pointerUpAbortControllerId;
    private string? _pointerMoveAbortControllerId;
    private ElementReference _saturationPickerRef;
    private (double Left, double Top)? _saturationPickerThumbPosition;
    private DotNetObjectReference<BitColorPicker> _dotnetObj = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Indicates the Alpha value.
    /// </summary>
    [Parameter, TwoWayBound]
    public double Alpha
    {
        get => _color.A;
        set
        {
            if (_color.A == value) return;

            _color.A = value;
            AlphaChanged.InvokeAsync(value);
        }
    }

    /// <summary>
    /// CSS-compatible string to describe the color.
    /// </summary>
    [Parameter, TwoWayBound]
    public string Color
    {
        get => _colorType == BitInternalColorType.Hex ? _color.Hex! : _color.Rgb!;
        set
        {
            _colorType = value.HasValue() && value.StartsWith("#", StringComparison.InvariantCultureIgnoreCase)
                            ? BitInternalColorType.Hex
                            : BitInternalColorType.Rgb;

            var newColor = new BitInternalColor(value, Alpha);

            if (newColor.R == _color.R && newColor.G == _color.G && newColor.B == _color.B && newColor.A == _color.A) return;

            _color = newColor;
            _selectedHue = _color.Hsv.Hue;

            SetSaturationPickerStyle();

            _ = SetSaturationPickerThumbPositionAsync();

            ColorChanged.InvokeAsync(value);
        }
    }

    /// <summary>
    /// Callback for when the value changed.
    /// </summary>
    [Parameter] public EventCallback<BitColorChangeEventArgs> OnChange { get; set; }

    /// <summary>
    /// Whether to show a slider for editing alpha value.
    /// </summary>
    [Parameter] public bool ShowAlphaSlider { get; set; }

    /// <summary>
    /// Whether to show color preview box.
    /// </summary>
    [Parameter] public bool ShowPreview { get; set; }



    public string? Hex => _color.Hex;
    public string? Rgb => FormattableString.Invariant($"rgb({_color.R},{_color.G},{_color.B})");
    public string? Rgba => FormattableString.Invariant($"rgba({_color.R},{_color.G},{_color.B},{_color.A})");
    public (double Hue, double Saturation, double Value) Hsv => _color.Hsv;

    [JSInvokable(nameof(HandlePointerUp))]
    public void HandlePointerUp(MouseEventArgs e)
    {
        _saturationPickerPointerDown = false;
    }

    [JSInvokable(nameof(HandlePointerMove))]
    public async Task HandlePointerMove(MouseEventArgs e)
    {
        if (_saturationPickerPointerDown is false) return;

        await UpdateColor(e);
    }



    protected override string RootElementClass => "bit-clp";

    protected override void OnInitialized()
    {
        _dotnetObj = DotNetObjectReference.Create(this);

        SetSaturationPickerStyle();

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        _pointerUpAbortControllerId = await _js.BitColorPickerRegisterPointerUp(_dotnetObj, nameof(HandlePointerUp));
        _pointerMoveAbortControllerId = await _js.BitColorPickerRegisterPointerMove(_dotnetObj, nameof(HandlePointerMove));

        await SetSaturationPickerThumbPositionAsync();
    }



    private async Task SetSaturationPickerThumbPositionAsync()
    {
        var (_, saturation, value) = _color.Hsv;
        var saturationPickerRect = await _js.BitUtilsGetBoundingClientRect(_saturationPickerRef);

        var width = saturationPickerRect?.Width ?? 0;
        var height = saturationPickerRect?.Height ?? 0;

        _saturationPickerThumbPosition = new(width * saturation, height - (height * value));

        StateHasChanged();
    }

    private void SetSaturationPickerStyle()
    {
        var rgb = BitInternalColor.ToRgb(_selectedHue, 1, 1).ToString();
        _saturationPickerStyle = $"background-color:rgb{rgb}";
    }

    private async Task UpdateColor(MouseEventArgs e)
    {
        if (ColorHasBeenSet && ColorChanged.HasDelegate is false) return;

        var pickerRect = await _js.BitUtilsGetBoundingClientRect(_saturationPickerRef);
        var left = e.ClientX < pickerRect.Left ? 0
                    : e.ClientX > pickerRect.Left + pickerRect.Width
                    ? pickerRect.Width
                    : (e.ClientX - pickerRect.Left);
        var top = e.ClientY < pickerRect.Top ? 0
                    : e.ClientY > pickerRect.Top + pickerRect.Height
                    ? pickerRect.Height
                    : (e.ClientY - pickerRect.Top);

        _saturationPickerThumbPosition = new(left, top);

        _selectedSaturation = Math.Clamp((e.ClientX - pickerRect.Left) / pickerRect.Width, 0, 1);
        _selectedValue = Math.Clamp((pickerRect.Height - e.ClientY + pickerRect.Top) / pickerRect.Height, 0, 1);

        _color.Update(_selectedHue, _selectedSaturation, _selectedValue, _color.A);
        var colorValue = _colorType == BitInternalColorType.Hex ? _color.Hex : _color.Rgb;

        SetSaturationPickerStyle();

        await ColorChanged.InvokeAsync(colorValue);
        await AlphaChanged.InvokeAsync(_color.A);
        await OnChange.InvokeAsync(new() { Color = colorValue, Alpha = _color.A });

        StateHasChanged();
    }

    private async Task HandleOnHueInput(ChangeEventArgs args)
    {
        if (ColorHasBeenSet && ColorChanged.HasDelegate is false) return;

        _selectedHue = Convert.ToDouble(args.Value);
        _color.Update(_selectedHue, _selectedSaturation, _selectedValue, _color.A);

        var colorValue = _colorType == BitInternalColorType.Hex ? _color.Hex : _color.Rgb;

        SetSaturationPickerStyle();

        await ColorChanged.InvokeAsync(colorValue);
        await AlphaChanged.InvokeAsync(_color.A);
        await OnChange.InvokeAsync(new() { Color = colorValue, Alpha = _color.A });
    }

    private async Task HandleOnAlphaInput(ChangeEventArgs args)
    {
        if (AlphaHasBeenSet && AlphaChanged.HasDelegate is false) return;

        _color.A = Convert.ToDouble(args.Value);
        var colorValue = _colorType == BitInternalColorType.Hex ? _color.Hex : _color.Rgb;

        await ColorChanged.InvokeAsync(colorValue);
        await AlphaChanged.InvokeAsync(_color.A);
        await OnChange.InvokeAsync(new() { Color = colorValue, Alpha = _color.A });
    }

    private async Task HandleOnSaturationPickerPointerDown(MouseEventArgs e)
    {
        _saturationPickerPointerDown = true;
        await UpdateColor(e);
    }

    private string GetRootElAriaLabel()
    {
        var ariaLabel = $"Color picker, Red {_color.R} Green {_color.G} Blue {_color.B} ";
        if (ShowAlphaSlider)
        {
            ariaLabel += $"Alpha {_color.A * 100}% selected.";
        }
        else
        {
            ariaLabel += "selected.";
        }

        return ariaLabel;
    }



    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed || disposing is false) return;

        if (_dotnetObj is not null)
        {
            // _dotnetObj.Dispose(); // it is getting disposed in the following js call:
            try
            {
                await _js.BitColorPickerAbort(_pointerUpAbortControllerId, true);
                await _js.BitColorPickerAbort(_pointerMoveAbortControllerId);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        _disposed = true;
    }
}
