﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitSlider
    {
        private int? fisrtInputValue;
        private int? secoundInputValue;
        private int? upperValue;
        private int? lowerValue;
        private int? value;

        private bool isReadOnly;
        private string? styleProgress;
        private string? styleContainer;
        private int inputHeight;

        private bool ValueHasBeenSet;
        private bool UpperValueHasBeenSet;
        private bool LowerValueHasBeenSet;
        private bool RangeValueHasBeenSet;

        private ElementReference ContainerRef { get; set; }
        private ElementReference TitleRef { get; set; }
        private ElementReference ValueLabelRef { get; set; }

        [Inject] public IJSRuntime? JSRuntime { get; set; }
        [Parameter] public int? DefaultUpperValue { get; set; }
        [Parameter] public int? DefaultLowerValue { get; set; }
        [Parameter] public int? DefaultValue { get; set; }
        [Parameter] public int Min { get; set; }
        [Parameter] public int Max { get; set; } = 10;
        [Parameter] public int Step { get; set; } = 1;

        [Parameter]
        public int? UpperValue
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
        [Parameter] public EventCallback<int?> UpperValueChanged { get; set; }

        [Parameter]
        public int? LowerValue
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
        [Parameter] public EventCallback<int?> LowerValueChanged { get; set; }

        [Parameter]
        public int? Value
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
        [Parameter] public EventCallback<int?> ValueChanged { get; set; }

        [Parameter]
        public (int? Lower, int? Upper) RangeValue
        {
            get => (lowerValue, upperValue);
            set
            {
                if (value.Lower == lowerValue && value.Upper == upperValue) return;
                if ((!value.Lower.HasValue && lowerValue.HasValue) || (!value.Upper.HasValue && upperValue.HasValue)) return;

                lowerValue = value.Lower;
                upperValue = value.Upper;
                SetInputValueOnRanged(value.Lower, value.Upper);
                ClassBuilder.Reset();
                FillSlider();
                _ = RangeValueChanged.InvokeAsync(value);
            }
        }
        [Parameter] public EventCallback<(int? Lower, int? Upper)> RangeValueChanged { get; set; }

        [Parameter] public bool IsOriginFromZero { get; set; }
        [Parameter] public string? Label { get; set; }
        [Parameter] public bool IsRanged { get; set; }
        [Parameter] public bool ShowValue { get; set; } = true;
        [Parameter] public bool IsVertical { get; set; }
        [Parameter] public string? ValueFormat { get; set; }
        [Parameter] public EventCallback<ChangeEventArgs> OnChange { get; set; }
        protected override string RootElementClass => "bit-slider";

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

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsReadonly
                                                ? $"{RootElementClass}-readonly-{VisualClassRegistrar()}"
                                                : string.Empty);

            var rangedClass = IsRanged ? "-ranged" : null;
            ClassBuilder.Register(() => IsVertical
                                                ? $"{RootElementClass}{rangedClass}-column"
                                                : $"{RootElementClass}{rangedClass}-row");
        }

        protected override void OnInitialized()
        {
            if (DefaultUpperValue.HasValue)
            {
                upperValue = DefaultUpperValue.Value;
            }

            if (DefaultLowerValue.HasValue)
            {
                lowerValue = DefaultLowerValue.Value;
            }

            if (IsRanged)
            {
                SetInputValueOnRanged(lowerValue, upperValue);
            }
            else if (!Value.HasValue)
            {
                value = DefaultValue.GetValueOrDefault(Min);
            }

            if (!IsVertical)
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
                    inputHeight = await JSRuntime!.GetClientHeight(RootElement);

                    if (Label.HasValue())
                    {
                        var titleHeight = await JSRuntime!.GetClientHeight(TitleRef);
                        inputHeight -= titleHeight;
                    }

                    if (ShowValue)
                    {
                        var valueLabelHeight = await JSRuntime!.GetClientHeight(ValueLabelRef);
                        inputHeight -= (valueLabelHeight * 2);
                    }
                }
                else
                {
                    inputHeight = await JSRuntime!.GetClientHeight(ContainerRef);
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
                    Value = Convert.ToInt32(e.Value, CultureInfo.InvariantCulture);
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
                        fisrtInputValue = Convert.ToInt32(e.Value, CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        secoundInputValue = Convert.ToInt32(e.Value, CultureInfo.InvariantCulture);
                    }

                    if (fisrtInputValue < secoundInputValue)
                    {
                        lowerValue = fisrtInputValue;
                        upperValue = secoundInputValue;
                    }
                    else
                    {
                        lowerValue = secoundInputValue;
                        upperValue = fisrtInputValue;
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
                styleProgress = $"--l: {fisrtInputValue}; --h: {secoundInputValue}; --min: {Min}; --max: {Max}";
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

        private void SetInputValueOnRanged(int? lower = null, int? upper = null)
        {
            var defaultValue = Min > 0 || Max < 0 ? Min : 0;
            lower = lower.GetValueOrDefault(lowerValue ?? defaultValue);
            upper = upper.GetValueOrDefault(upperValue ?? defaultValue);

            if (upper > lower)
            {
                fisrtInputValue = lower;
                secoundInputValue = upper;
            }
            else
            {
                fisrtInputValue = upper;
                secoundInputValue = lower;
            }
        }

        private string? GetValueDisplay(int? val)
        {
            if (ValueFormat.HasNoValue())
            {
                return $"{val}";
            }
            else if (ValueFormat!.Contains("p", StringComparison.CurrentCultureIgnoreCase))
            {
                int digitCount = $"{(Max - 1)}".Length;
                return (val.GetValueOrDefault() / Math.Pow(10, digitCount)).ToString(ValueFormat, CultureInfo.InvariantCulture);
            }
            else
            {
                return val.GetValueOrDefault().ToString(ValueFormat, CultureInfo.InvariantCulture);
            }
        }
    }
}
