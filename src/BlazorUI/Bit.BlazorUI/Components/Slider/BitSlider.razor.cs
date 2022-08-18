using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.BlazorUI;

public partial class BitSlider
{
    private bool ValueHasBeenSet;
    private bool UpperValueHasBeenSet;
    private bool LowerValueHasBeenSet;
    private bool RangeValueHasBeenSet;

    private double? firstInputValue;
    private double? secondInputValue;
    private double? upperValue;
    private double? lowerValue;
    private double? value;

    private bool isReadOnly;
    private string? styleProgress;
    private string? styleContainer;
    private int inputHeight;
    private readonly string sliderBoxId = $"Slider{Guid.NewGuid()}";

    private ElementReference ContainerRef { get; set; }
    private ElementReference TitleRef { get; set; }
    private ElementReference ValueLabelRef { get; set; }

    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// The initial upper value of the Slider is ranged is true
    /// </summary>
    [Parameter] public double? DefaultUpperValue { get; set; }

    /// <summary>
    /// The initial lower value of the Slider is ranged is true
    /// </summary>
    [Parameter] public double? DefaultLowerValue { get; set; }

    /// <summary>
    /// The initial value of the Slider
    /// </summary>
    [Parameter] public double? DefaultValue { get; set; }

    /// <summary>
    /// The min value of the Slider
    /// </summary>
    [Parameter] public double Min { get; set; }

    /// <summary>
    /// The max value of the Slider
    /// </summary>
    [Parameter] public double Max { get; set; } = 10;

    /// <summary>
    /// The difference between the two adjacent values of the Slider
    /// </summary>
    [Parameter] public double Step { get; set; } = 1;

    /// <summary>
    /// The initial upper value of the Slider is ranged is true
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
            ClassBuilder.Reset();
            FillSlider();
            _ = UpperValueChanged.InvokeAsync(value);
        }
    }

    /// <summary>
    /// Callback for when lower value changed
    /// </summary>
    [Parameter] public EventCallback<double?> UpperValueChanged { get; set; }

    /// <summary>
    /// The initial lower value of the Slider is ranged is true
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
            ClassBuilder.Reset();
            FillSlider();
            _ = LowerValueChanged.InvokeAsync(value);
        }
    }

    /// <summary>
    /// Callback for when lower value changed
    /// </summary>
    [Parameter] public EventCallback<double?> LowerValueChanged { get; set; }

    /// <summary>
    /// The initial value of the Slider
    /// </summary>
    [Parameter]
    public double? Value
    {
        get => value;
        set
        {
            if (value == this.value) return;
            this.value = value;
            ClassBuilder.Reset();
            FillSlider();
            _ = ValueChanged.InvokeAsync(value);
        }
    }

    /// <summary>
    /// Callback for when the value changed
    /// </summary>
    [Parameter] public EventCallback<double?> ValueChanged { get; set; }

    /// <summary>
    /// The initial range value of the Slider. Use this parameter to set value for both LowerValue and UpperValue
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
            ClassBuilder.Reset();
            FillSlider();
            _ = RangeValueChanged.InvokeAsync(value);
        }
    }

    /// <summary>
    /// Callback for when range value changed
    /// </summary>
    [Parameter] public EventCallback<BitSliderRangeValue?> RangeValueChanged { get; set; }

    /// <summary>
    /// Whether to attach the origin of slider to zero
    /// </summary>
    [Parameter] public bool IsOriginFromZero { get; set; }

    /// <summary>
    /// Description label of the Slider
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// If ranged is true, display two thumbs that allow the lower and upper bounds of a range to be selected
    /// </summary>
    [Parameter] public bool IsRanged { get; set; }

    /// <summary>
    /// Whether to show the value on the right of the Slider
    /// </summary>
    [Parameter] public bool ShowValue { get; set; } = true;

    /// <summary>
    /// Whether to render the slider vertically
    /// </summary>
    [Parameter] public bool IsVertical { get; set; }

    /// <summary>
    /// Custom formatter for the Slider value
    /// </summary>
    [Parameter] public string? ValueFormat { get; set; }

    /// <summary>
    /// Callback when the value has been changed. This will be called on every individual step
    /// </summary>
    [Parameter] public EventCallback<ChangeEventArgs> OnChange { get; set; }

    /// <summary>
    ///  A text description of the Slider number value for the benefit of screen readers
    ///  This should be used when the Slider number value is not accurately represented by a number
    /// </summary>
    [Parameter] public Func<double, string>? AriaValueText { get; set; }

    /// <summary>
    /// Additional parameter for the Slider box
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
    [Parameter] public Dictionary<string, object>? SliderBoxHtmlAttributes { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

    /// <summary>
    /// Whether to render the Slider as readonly
    /// </summary>
    [Parameter]
    public bool IsReadonly
    {
        get => isReadOnly;
        set
        {
            isReadOnly = value;
            ClassBuilder.Reset();
        }
    }

    protected override string RootElementClass => "bit-slider";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsReadonly
                                            ? $"{RootElementClass}-readonly-{VisualClassRegistrar()}"
                                            : string.Empty);

        var rangedClass = IsRanged ? "-ranged" : null;
        ClassBuilder.Register(() => IsVertical
                                            ? $"{RootElementClass}{rangedClass}-vertical"
                                            : $"{RootElementClass}{rangedClass}-horizontal");
    }

    protected override void OnInitialized()
    {
        if (UpperValue.HasValue is false && DefaultUpperValue.HasValue)
        {
            UpperValue = DefaultUpperValue.Value;
        }

        if (LowerValue.HasValue is false && DefaultLowerValue.HasValue)
        {
            LowerValue = DefaultLowerValue.Value;
        }

        if (IsRanged)
        {
            SetInputValueOnRanged(lowerValue, upperValue);
        }
        else if (Value.HasValue is false)
        {
            value = DefaultValue.GetValueOrDefault(Min);
        }

        if (IsVertical is false)
        {
            FillSlider();
        }

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && IsVertical)
        {
            if (IsRanged)
            {
                inputHeight = await JSRuntime.GetClientHeight(RootElement);

                if (Label.HasValue())
                {
                    var titleHeight = await JSRuntime.GetClientHeight(TitleRef);
                    inputHeight -= titleHeight;
                }

                if (ShowValue)
                {
                    var valueLabelHeight = await JSRuntime.GetClientHeight(ValueLabelRef);
                    inputHeight -= (valueLabelHeight * 2);
                }
            }
            else
            {
                inputHeight = await JSRuntime.GetClientHeight(ContainerRef);
            }
            FillSlider();
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task HandleInput(ChangeEventArgs e)
    {
        if (IsEnabled)
        {
            if (!IsRanged)
            {
                Value = Convert.ToDouble(e.Value, CultureInfo.InvariantCulture);
                FillSlider();
            }
            else
            {
                UpperValue = null;
                LowerValue = null;
            }

            await OnChange.InvokeAsync(e);
        }
    }

    private async Task HandleInput(ChangeEventArgs e, bool isFirstInput)
    {
        if (IsEnabled)
        {
            if (IsRanged)
            {
                if (isFirstInput)
                {
                    firstInputValue = Convert.ToDouble(e.Value, CultureInfo.InvariantCulture);
                }
                else
                {
                    secondInputValue = Convert.ToDouble(e.Value, CultureInfo.InvariantCulture);
                }

                if (firstInputValue < secondInputValue)
                {
                    LowerValue = firstInputValue;
                    UpperValue = secondInputValue;
                }
                else
                {
                    LowerValue = secondInputValue;
                    UpperValue = firstInputValue;
                }

                FillSlider();
            }
            else
            {
                Value = null;
            }

            await OnChange.InvokeAsync(e);
        }
    }

    private void FillSlider()
    {
        if (IsRanged)
        {
            styleProgress = $"--l: {firstInputValue}; --h: {secondInputValue}; --min: {Min}; --max: {Max}";
            if (IsVertical)
            {
                styleContainer = $"width: {inputHeight}px; height: {inputHeight}px;";
                StateHasChanged();
            }
        }
        else
        {
            if (IsVertical)
            {
                styleProgress = $"--value: {Value}; --min: {Min}; --max: {Max}; width: {inputHeight}px; transform: rotate(270deg) translateX(-{(inputHeight - 12)}px);";
                StateHasChanged();
            }
            else
            {
                styleProgress = $"--value: {Value}; --min: {Min}; --max: {Max};";
            }
        }
    }

    private void SetInputValueOnRanged(double? lower = null, double? upper = null)
    {
        var defaultValue = Min > 0 || Max < 0 ? Min : 0;
        lower = lower.GetValueOrDefault(lowerValue ?? defaultValue);
        upper = upper.GetValueOrDefault(upperValue ?? defaultValue);

        if (upper > lower)
        {
            firstInputValue = lower;
            secondInputValue = upper;
        }
        else
        {
            firstInputValue = upper;
            secondInputValue = lower;
        }
    }

    private string? GetValueDisplay(double? val)
    {
        if (ValueFormat.HasNoValue())
        {
            return $"{val}";
        }
        else if (ValueFormat!.Contains('p', StringComparison.CurrentCultureIgnoreCase))
        {
            int digitCount = $"{(Max - 1)}".Length;
            return (val.GetValueOrDefault() / Math.Pow(10, digitCount)).ToString(ValueFormat, CultureInfo.InvariantCulture);
        }
        else
        {
            return val.GetValueOrDefault().ToString(ValueFormat, CultureInfo.InvariantCulture);
        }
    }

    private string GetAriaValueText(double value)
    {
        return AriaValueText != null ? AriaValueText(value) : value.ToString(CultureInfo.InvariantCulture);
    }

    private bool GetAriaDisabled => !IsEnabled;
    private int? GetTabIndex => IsEnabled ? 0 : null;
    private bool GetDataIsFocusable => !IsEnabled;
}
