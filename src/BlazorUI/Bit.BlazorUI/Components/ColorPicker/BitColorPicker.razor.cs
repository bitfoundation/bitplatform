using System.Globalization;

namespace Bit.BlazorUI;

public partial class BitColorPicker : IAsyncDisposable
{
    private bool ColorHasBeenSet;
    private bool AlphaHasBeenSet;

    private ElementReference _saturationPickerRef;
    private string? _onWindowMouseUpAbortControllerId;
    private string? _onWindowMouseMoveAbortControllerId;
    private string? _saturationPickerBackgroundRgbCss;
    private string? _saturationPickerBackgroundRgbaCss;
    private bool _saturationPickerMouseDown;
    private BitColorPosition? _saturationPickerThumbPosition;
    private BitColor _color = new BitColor();
    private BitColorType _colorType;
    private double _hue;
    private double _selectedSaturation = 1;
    private double _selectedValue = 1;
    private string _colorRectangleDescriptionId => $"{UniqueId}-ColorRectangle-description";

    public string? Hex => _color.Hex;
    public string? Rgb => _color.Rgb;

    [Inject] public IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// Indicates the Alpha value.
    /// </summary>
    [Parameter]
    public double Alpha
    {
        get => _color.Alpha;
        set
        {
            if (_color.Alpha == value) return;
            _color.Alpha = value;

            AlphaChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<double> AlphaChanged { get; set; }

    /// <summary>
    /// CSS-compatible string to describe the color.
    /// </summary>
    [Parameter]
    public string Color
    {
        get => _colorType == BitColorType.Hex ? _color.Hex! : _color.Rgb!;
        set
        {
            _colorType = value.HasValue() && value.StartsWith("#", StringComparison.InvariantCultureIgnoreCase) ? BitColorType.Hex : BitColorType.Rgb;

            var valueAsBitColor = new BitColor(value, Alpha);
            if (valueAsBitColor == _color) return;
            _color = valueAsBitColor;
            _hue = _color.Hsv.Hue;
            SetSaturationPickerBackground();

            ColorChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<string> ColorChanged { get; set; }

    /// <summary>
    /// Callback for when the value changed.
    /// </summary>
    [Parameter] public EventCallback<BitColorValue> OnChange { get; set; }

    /// <summary>
    /// Whether to show a slider for editing alpha value.
    /// </summary>
    [Parameter] public bool ShowAlphaSlider { get; set; }

    /// <summary>
    /// Whether to show color preview box.
    /// </summary>
    [Parameter] public bool ShowPreview { get; set; }

    protected override string RootElementClass => "bit-clp";

    protected override void OnInitialized()
    {
        SetSaturationPickerBackground();

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _onWindowMouseUpAbortControllerId = await _js.RegisterOnWindowMouseUpEvent(this, "OnWindowMouseUp");
            _onWindowMouseMoveAbortControllerId = await _js.RegisterOnWindowMouseMoveEvent(this, "OnWindowMouseMove");

            await SetPositionAsync();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task SetPositionAsync()
    {
        var hsv = _color.Hsv;
        var saturationPickerRect = await _js.GetBoundingClientRect(_saturationPickerRef);

        var width = saturationPickerRect?.Width ?? 0;
        var height = saturationPickerRect?.Height ?? 0;

        _saturationPickerThumbPosition = new BitColorPosition
        {
            Left = Convert.ToInt32(width * hsv.Saturation / 100),
            Top = Convert.ToInt32(height - (height * hsv.Value / 100))
        };

        StateHasChanged();
    }

    private void SetSaturationPickerBackground()
    {
        var bitColor = new BitColor(_hue, 1, 1, 1);
        _saturationPickerBackgroundRgbaCss = bitColor.Rgba;
        _saturationPickerBackgroundRgbCss = bitColor.Rgb;
    }

    private async Task PickColorTune(MouseEventArgs e)
    {
        if (ColorHasBeenSet && ColorChanged.HasDelegate is false) return;

        var parent = await _js.GetBoundingClientRect(_saturationPickerRef);
        _saturationPickerThumbPosition = new BitColorPosition
        {
            Left = e.ClientX < parent.Left ? 0 : e.ClientX > parent.Left + parent.Width ? Convert.ToInt32(parent.Width) : Convert.ToInt32(e.ClientX - parent.Left),
            Top = e.ClientY < parent.Top ? 0 : e.ClientY > parent.Top + parent.Height ? Convert.ToInt32(parent.Height) : Convert.ToInt32(e.ClientY - parent.Top)
        };

        _selectedSaturation = Math.Clamp(ToValidSpanValue(0, parent.Width, 0, 1, Convert.ToInt32(e.ClientX - parent.Left)), 0, 1);
        _selectedValue = Math.Clamp(ToValidSpanValue(0, parent.Height, 0, 1, parent.Height - Convert.ToInt32(e.ClientY - parent.Top)), 0, 1);
        _color = new BitColor(_hue, _selectedSaturation, _selectedValue, _color.Alpha);
        SetSaturationPickerBackground();
        string? colorValue = _colorType == BitColorType.Hex ? _color.Hex : _color.Rgb;
        await ColorChanged.InvokeAsync(colorValue);
        await AlphaChanged.InvokeAsync(_color.Alpha);
        await OnChange.InvokeAsync(new() { Color = colorValue, Alpha = _color.Alpha });
        StateHasChanged();
    }

    private async Task PickMainColor(ChangeEventArgs args)
    {
        if (ColorHasBeenSet && ColorChanged.HasDelegate is false) return;

        _hue = Convert.ToInt32(args.Value, CultureInfo.InvariantCulture);
        _color = new BitColor(_hue, _selectedSaturation, _selectedValue, _color.Alpha);
        SetSaturationPickerBackground();
        string? colorValue = _colorType == BitColorType.Hex ? _color.Hex : _color.Rgb;
        await ColorChanged.InvokeAsync(colorValue);
        await AlphaChanged.InvokeAsync(_color.Alpha);
        await OnChange.InvokeAsync(new() { Color = colorValue, Alpha = _color.Alpha });
    }

    private async Task PickAlphaColor(ChangeEventArgs args)
    {
        if (AlphaHasBeenSet && AlphaChanged.HasDelegate is false) return;

        var alpha = Convert.ToDouble(args.Value, CultureInfo.InvariantCulture) / 100;
        _color = new BitColor(_color.Hex ?? "", alpha);
        string? colorValue = _colorType == BitColorType.Hex ? _color.Hex : _color.Rgb;
        await ColorChanged.InvokeAsync(colorValue);
        await AlphaChanged.InvokeAsync(_color.Alpha);
        await OnChange.InvokeAsync(new() { Color = colorValue, Alpha = _color.Alpha });
    }

    private static double ToValidSpanValue(double min, double max, double newMin, double newMax, double value)
    {
        return (value - min) * (newMax - newMin) / (max - min);
    }

    private async Task OnSaturationPickerMouseDown(MouseEventArgs e)
    {
        _saturationPickerMouseDown = true;
        await PickColorTune(e);
    }

    private async Task OnSaturationPickerMouseMove(MouseEventArgs e)
    {
        if (_saturationPickerMouseDown is false) return;

        await PickColorTune(e);
    }

    private string GetRootElAriaLabel()
    {
        var ariaLabel = $"Color picker, Red {_color.Red} Green {_color.Green} Blue {_color.Blue} ";
        if (ShowAlphaSlider)
        {
            ariaLabel += $"Alpha {_color.Alpha * 100}% selected.";
        }
        else
        {
            ariaLabel += "selected.";
        }

        return ariaLabel;
    }

    [JSInvokable]
    public void OnWindowMouseUp(MouseEventArgs e)
    {
        _saturationPickerMouseDown = false;
    }

    [JSInvokable]
    public async Task OnWindowMouseMove(MouseEventArgs e)
    {
        await OnSaturationPickerMouseMove(e);
    }

    public async ValueTask DisposeAsync()
    {
        if (_onWindowMouseUpAbortControllerId.HasValue())
        {
            await _js.AbortProcedure(_onWindowMouseUpAbortControllerId!);
        }

        if (_onWindowMouseMoveAbortControllerId.HasValue())
        {
            await _js.AbortProcedure(_onWindowMouseMoveAbortControllerId!);
        }

        GC.SuppressFinalize(this);
    }
}
