using System.Globalization;

namespace Bit.BlazorUI;

public partial class BitSlider
{
    private bool ValueHasBeenSet;
    private bool UpperValueHasBeenSet;
    private bool LowerValueHasBeenSet;
    private bool RangeValueHasBeenSet;

    private bool isRanged;
    private bool isReadOnly;
    private bool isVertical;
    private double? lowerValue;
    private double? upperValue;
    private double? value;

    private double? _firstInputValue;
    private double? _secondInputValue;
    private string? _styleProgress;
    private string? _styleContainer;
    private int _inputHeight;
    private string _sliderBoxId = default!;
    private string _minInputId = default!;
    private string _maxInputId = default!;

    private ElementReference _labelRef;
    private ElementReference _containerRef;
    private ElementReference _valueLabelRef;

    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    ///  A text description of the Slider number value for the benefit of screen readers
    ///  This should be used when the Slider number value is not accurately represented by a number
    /// </summary>
    [Parameter] public Func<double, string>? AriaValueText { get; set; }

    /// <summary>
    /// The default lower value of the ranged Slider.
    /// </summary>
    [Parameter] public double? DefaultLowerValue { get; set; }

    /// <summary>
    /// The default upper value of the ranged Slider.
    /// </summary>
    [Parameter] public double? DefaultUpperValue { get; set; }

    /// <summary>
    /// The default value of the Slider.
    /// </summary>
    [Parameter] public double? DefaultValue { get; set; }

    /// <summary>
    /// Whether to attach the origin of slider to zero
    /// </summary>
    [Parameter] public bool IsOriginFromZero { get; set; }

    /// <summary>
    /// If ranged is true, display two thumbs that allow the lower and upper bounds of a range to be selected
    /// </summary>
    [Parameter]
    public bool IsRanged
    {
        get => isRanged;
        set
        {
            if (isRanged == value) return;

            isRanged = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Whether to render the Slider as readonly
    /// </summary>
    [Parameter]
    public bool IsReadonly
    {
        get => isReadOnly;
        set
        {
            if (isReadOnly == value) return;

            isReadOnly = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Whether to render the slider vertically
    /// </summary>
    [Parameter]
    public bool IsVertical
    {
        get => isVertical;
        set
        {
            if (isVertical == value) return;

            isVertical = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Description label of the Slider
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The lower value of the ranged Slider.
    /// </summary>
    [Parameter]
    public double? LowerValue
    {
        get => lowerValue;
        set
        {
            if (value == lowerValue) return;

            lowerValue = value;
            SetInputValueOnRanged(lower: value);
            FillSlider();
            _ = LowerValueChanged.InvokeAsync(value);
        }
    }
    [Parameter] public EventCallback<double?> LowerValueChanged { get; set; }

    /// <summary>
    /// The min value of the Slider
    /// </summary>
    [Parameter] public double Min { get; set; }

    /// <summary>
    /// The max value of the Slider
    /// </summary>
    [Parameter] public double Max { get; set; } = 10;

    /// <summary>
    /// Callback when the value has been changed. This will be called on every individual step
    /// </summary>
    [Parameter] public EventCallback<ChangeEventArgs> OnChange { get; set; }

    /// <summary>
    /// The initial range value of the Slider. Use this parameter to set value for both LowerValue and UpperValue.
    /// </summary>
    [Parameter]
    public BitSliderRangeValue? RangeValue
    {
        get => new() { Lower = lowerValue, Upper = upperValue };
        set
        {
            if (value?.Lower == lowerValue && value?.Upper == upperValue) return;
            if (value is null || (value.Lower.HasValue is false && lowerValue.HasValue) || (value.Upper.HasValue is false && upperValue.HasValue)) return;

            lowerValue = value.Lower;
            upperValue = value.Upper;
            SetInputValueOnRanged(value.Lower, value.Upper);
            FillSlider();
            _ = RangeValueChanged.InvokeAsync(value);
        }
    }
    [Parameter] public EventCallback<BitSliderRangeValue?> RangeValueChanged { get; set; }

    /// <summary>
    /// Whether to show the value on the right of the Slider
    /// </summary>
    [Parameter] public bool ShowValue { get; set; } = true;

    /// <summary>
    /// Additional parameter for the Slider box
    /// </summary>
    [Parameter] public Dictionary<string, object>? SliderBoxHtmlAttributes { get; set; }

    /// <summary>
    /// The difference between the two adjacent values of the Slider
    /// </summary>
    [Parameter] public double Step { get; set; } = 1;

    /// <summary>
    /// The upper value of the ranged Slider.
    /// </summary>
    [Parameter]
    public double? UpperValue
    {
        get => upperValue;
        set
        {
            if (value == upperValue) return;

            upperValue = value;
            SetInputValueOnRanged(upper: value);
            FillSlider();
            _ = UpperValueChanged.InvokeAsync(value);
        }
    }
    [Parameter] public EventCallback<double?> UpperValueChanged { get; set; }

    /// <summary>
    /// The value of the Slider
    /// </summary>
    [Parameter]
    public double? Value
    {
        get => value;
        set
        {
            if (value == this.value) return;

            this.value = value;
            FillSlider();
            _ = ValueChanged.InvokeAsync(value);
        }
    }
    [Parameter] public EventCallback<double?> ValueChanged { get; set; }

    /// <summary>
    /// Custom formatter for the Slider value
    /// </summary>
    [Parameter] public string? ValueFormat { get; set; }


    protected override string RootElementClass => "bit-sld";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsReadonly ? $"{RootElementClass}-rdl" : string.Empty);

        ClassBuilder.Register(() => $"{RootElementClass}-{(IsRanged ? "rgd-" : null)}{(IsVertical ? "vrt" : "hrz")}");
    }

    protected override void OnInitialized()
    {
        _sliderBoxId = $"Slider-{UniqueId}";
        _minInputId = $"{_sliderBoxId}-input-min";
        _maxInputId = $"{_sliderBoxId}-input-max";

        if (LowerValue.HasValue is false && DefaultLowerValue.HasValue)
        {
            LowerValue = DefaultLowerValue.Value;
        }

        if (UpperValue.HasValue is false && DefaultUpperValue.HasValue)
        {
            UpperValue = DefaultUpperValue.Value;
        }

        if (IsRanged)
        {
            SetInputValueOnRanged(LowerValue, UpperValue);
        }
        else if (Value.HasValue is false)
        {
            Value = DefaultValue.GetValueOrDefault(Min);
        }

        if (IsVertical is false)
        {
            FillSlider();
        }

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false || IsVertical is false) return;

        if (IsRanged)
        {
            _inputHeight = await _js.GetClientHeight(RootElement);

            if (Label.HasValue())
            {
                var titleHeight = await _js.GetClientHeight(_labelRef);
                _inputHeight -= titleHeight;
            }

            if (ShowValue)
            {
                var valueLabelHeight = await _js.GetClientHeight(_valueLabelRef);
                _inputHeight -= (valueLabelHeight * 2);
            }
        }
        else
        {
            _inputHeight = await _js.GetClientHeight(_containerRef);
        }

        FillSlider();
    }

    private async Task HandleOnInput(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        Value = Convert.ToDouble(e.Value, CultureInfo.InvariantCulture);
        FillSlider();

        await OnChange.InvokeAsync(e);
    }

    private async Task HandleOnRangeInput(ChangeEventArgs e, bool isFirstInput)
    {
        if (IsEnabled is false) return;
        if (LowerValueHasBeenSet && LowerValueChanged.HasDelegate is false) return;
        if (UpperValueHasBeenSet && UpperValueChanged.HasDelegate is false) return;

        if (isFirstInput)
        {
            _firstInputValue = Convert.ToDouble(e.Value, CultureInfo.InvariantCulture);
        }
        else
        {
            _secondInputValue = Convert.ToDouble(e.Value, CultureInfo.InvariantCulture);
        }

        if (_firstInputValue < _secondInputValue)
        {
            LowerValue = _firstInputValue;
            UpperValue = _secondInputValue;
        }
        else
        {
            LowerValue = _secondInputValue;
            UpperValue = _firstInputValue;
        }

        FillSlider();


        await OnChange.InvokeAsync(e);
    }

    private void FillSlider()
    {
        if (IsRanged)
        {
            _styleProgress = $"--l: {_firstInputValue}; --h: {_secondInputValue}; --min: {Min}; --max: {Max}";
            if (IsVertical)
            {
                _styleContainer = $"width: {_inputHeight}px; height: {_inputHeight}px;";
                StateHasChanged();
            }
        }
        else
        {
            if (IsVertical)
            {
                _styleProgress = $"--value: {Value}; --min: {Min}; --max: {Max}; width: {_inputHeight}px; transform: rotate(270deg) translateX(-{(_inputHeight - 12)}px);";
                StateHasChanged();
            }
            else
            {
                _styleProgress = $"--value: {Value}; --min: {Min}; --max: {Max};";
            }
        }
    }

    private void SetInputValueOnRanged(double? lower = null, double? upper = null)
    {
        var defaultValue = Min > 0 || Max < 0 ? Min : 0;
        lower = lower.GetValueOrDefault(LowerValue ?? defaultValue);
        upper = upper.GetValueOrDefault(UpperValue ?? defaultValue);

        if (upper > lower)
        {
            _firstInputValue = lower;
            _secondInputValue = upper;
        }
        else
        {
            _firstInputValue = upper;
            _secondInputValue = lower;
        }
    }

    private string? GetValueDisplay(double? value)
    {
        if (ValueFormat.HasNoValue()) return $"{value}";

        if (ValueFormat!.Contains('p', StringComparison.CurrentCultureIgnoreCase))
        {
            int digitCount = $"{(Max - 1)}".Length;
            return (value.GetValueOrDefault() / Math.Pow(10, digitCount)).ToString(ValueFormat, CultureInfo.InvariantCulture);
        }

        return value.GetValueOrDefault().ToString(ValueFormat, CultureInfo.InvariantCulture);
    }

    private string GetAriaValueText(double value) => AriaValueText != null ? AriaValueText(value) : value.ToString(CultureInfo.InvariantCulture);

    private int? GetTabIndex => IsEnabled ? 0 : null;
}
