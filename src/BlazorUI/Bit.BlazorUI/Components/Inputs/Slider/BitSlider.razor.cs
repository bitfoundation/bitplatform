using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// A slider provides a visual indication of adjustable content, as well as the current setting in the total range of content.
/// </summary>
public partial class BitSlider : BitComponentBase
{
    private int _inputHeight;
    private string? _styleProgress;
    private string? _styleContainer;
    private double _firstInputValue;
    private double _secondInputValue;
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
    /// Custom CSS classes for different parts of the BitSlider.
    /// </summary>
    [Parameter] public BitSliderClassStyles? Classes { get; set; }

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
    [Parameter, ResetClassBuilder]
    public bool IsRanged { get; set; }

    /// <summary>
    /// Whether to render the Slider as readonly
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Whether to render the slider vertically
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool IsVertical { get; set; }

    /// <summary>
    /// Description label of the Slider
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The lower value of the ranged Slider.
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetValues))]
    public double LowerValue { get; set; }

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
    [Parameter, TwoWayBound]
    public BitSliderRangeValue? RangeValue
    {
        get => new() { Lower = LowerValue, Upper = UpperValue };
        set
        {
            if (value is null) return;
            if (value.Lower == LowerValue && value.Upper == UpperValue) return;

            _ = AssignLowerValue(value.Lower);
            _ = AssignUpperValue(value.Upper);
            _ = RangeValueChanged.InvokeAsync(value);

            SetInputValueOnRanged(value.Lower, value.Upper);

            FillSlider();
        }
    }

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
    /// Custom CSS styles for different parts of the BitSlider.
    /// </summary>
    [Parameter] public BitSliderClassStyles? Styles { get; set; }

    /// <summary>
    /// The upper value of the ranged Slider.
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetValues))]
    public double UpperValue { get; set; }

    /// <summary>
    /// The value of the Slider
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetValues))]
    public double Value { get; set; }

    /// <summary>
    /// Custom formatter for the Slider value
    /// </summary>
    [Parameter] public string? ValueFormat { get; set; }



    protected override string RootElementClass => "bit-sld";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IsReadOnly ? "bit-sld-rdl" : string.Empty);

        ClassBuilder.Register(() => $"bit-sld-{(IsRanged ? "rgd-" : null)}{(IsVertical ? "vrt" : "hrz")}");
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async Task OnInitializedAsync()
    {
        _sliderBoxId = $"BitSlider-{UniqueId}-box";
        _minInputId = $"BitSlider-{UniqueId}-min-input";
        _maxInputId = $"BitSlider-{UniqueId}-max-input";

        if (LowerValueHasBeenSet is false && DefaultLowerValue.HasValue)
        {
            await AssignLowerValue(DefaultLowerValue.Value);
        }

        if (UpperValueHasBeenSet is false && DefaultUpperValue.HasValue)
        {
            await AssignUpperValue(DefaultUpperValue.Value);
        }

        if (IsRanged)
        {
            SetInputValueOnRanged(LowerValue, UpperValue);
        }
        else if (ValueHasBeenSet is false && DefaultValue.HasValue)
        {
            await AssignValue(DefaultValue.Value);
        }

        if (IsVertical is false)
        {
            FillSlider();
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false || IsVertical is false) return;

        if (IsRanged)
        {
            _inputHeight = await GetClientHeight(RootElement);

            if (Label.HasValue())
            {
                var titleHeight = await GetClientHeight(_labelRef);
                _inputHeight -= titleHeight;
            }

            if (ShowValue)
            {
                var valueLabelHeight = await GetClientHeight(_valueLabelRef);
                _inputHeight -= (valueLabelHeight * 2);
            }
        }
        else
        {
            _inputHeight = await GetClientHeight(_containerRef);
        }

        FillSlider();
    }



    private async Task HandleOnInput(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (await AssignValue(Convert.ToDouble(e.Value, CultureInfo.InvariantCulture)) is false) return;
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
            await AssignLowerValue(_firstInputValue);
            await AssignUpperValue(_secondInputValue);
        }
        else
        {
            await AssignLowerValue(_secondInputValue);
            await AssignUpperValue(_firstInputValue);
        }

        FillSlider();


        await OnChange.InvokeAsync(e);
    }

    private void FillSlider()
    {
        if (IsRanged)
        {
            _styleProgress = FormattableString.Invariant($"--l: {_firstInputValue}; --h: {_secondInputValue}; --min: {Min}; --max: {Max}");
            if (IsVertical)
            {
                _styleContainer = FormattableString.Invariant($"width: {_inputHeight}px; height: {_inputHeight}px;");
                StateHasChanged();
            }
        }
        else
        {
            if (IsVertical)
            {
                _styleProgress = FormattableString.Invariant($"--value: {Value}; --min: {Min}; --max: {Max}; width: {_inputHeight}px; transform: rotate(270deg) translateX(-{(_inputHeight - 12)}px);");
                StateHasChanged();
            }
            else
            {
                _styleProgress = FormattableString.Invariant($"--value: {Value}; --min: {Min}; --max: {Max};");
            }
        }
    }

    private void SetInputValueOnRanged(double lower, double upper)
    {
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

    private string? GetDisplayValue(double value)
    {
        if (ValueFormat.HasNoValue()) return $"{value}";

        return value.ToString(ValueFormat, CultureInfo.InvariantCulture);
    }

    private string GetAriaValueText(double value) => AriaValueText != null ? AriaValueText(value) : value.ToString(CultureInfo.InvariantCulture);

    private int? GetTabIndex => IsEnabled ? 0 : null;

    private void OnSetValues()
    {
        SetInputValueOnRanged(LowerValue, UpperValue);

        FillSlider();
    }

    private async ValueTask<int> GetClientHeight(ElementReference element)
    {
        var height = await _js.BitUtilsGetProperty(element, "clientHeight");
        return height.HasNoValue()
            ? 0
            : int.Parse(height);
    }
}
