using System;
using System.Threading.Tasks;
using Bit.Client.Web.BlazorUI.Components.SpinButtons;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitSpinButton
    {
        private double inputValue;
        private LabelPosition labelPosition = LabelPosition.Left;
        private bool ValueHasBeenSet;

        [Parameter]
        public double Value
        {
            get => inputValue;
            set
            {
                if (value == inputValue) return;
                inputValue = value;
                _ = ValueChanged.InvokeAsync(value);
            }
        }

        [Parameter] public EventCallback<double> ValueChanged { get; set; }
        [Inject] public IJSRuntime? JSRuntime { get; set; }

        [Parameter] public double Min { get; set; } = double.MinValue;
        [Parameter] public double Max { get; set; } = double.MaxValue;
        [Parameter] public double DefaultValue { get; set; } = 0;
        [Parameter] public double Step { get; set; } = 1;
        [Parameter] public string Suffix { get; set; } = string.Empty;
        [Parameter] public string Label { get; set; } = string.Empty;
        [Parameter] public string IconName { get; set; } = string.Empty;

        [Parameter]
        public LabelPosition LabelPosition
        {
            get => labelPosition;
            set
            {
                labelPosition = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter] public EventCallback<double> OnChange { get; set; }

        private async Task HandleButtonClick(SpinButtonAction action)
        {
            if (IsEnabled)
            {
                double result = 0;
                bool isValid = false;
                switch (action)
                {
                    case SpinButtonAction.Up:
                        result = Value + Step;
                        isValid = result <= Max;
                        break;
                    case SpinButtonAction.Down:
                        result = Value - Step;
                        isValid = result >= Min;
                        break;
                }
                if (isValid)
                {
                    Value = Normalize(result);
                    await OnChange.InvokeAsync(Value);
                }
            }
        }

        protected override string RootElementClass => "bit-spb";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => LabelPosition == LabelPosition.Left
                                                ? $"{RootElementClass}-label-left-{VisualClassRegistrar()}"
                                                : $"{RootElementClass}-label-top-{VisualClassRegistrar()}");
        }

        private async Task HandleOnInputChange(ChangeEventArgs e)
        {
            if (IsEnabled)
            {
                var userInput = e.Value?.ToString();
                var isNumber = double.TryParse(userInput, out var numericValue);
                if (isNumber && numericValue >= Min && numericValue <= Max)
                {
                    Value = numericValue;
                    await OnChange.InvokeAsync(Value);
                }
            }
        }

        private bool IsStepDecimal => Step % 1 != 0;

        private double Normalize(double value) => IsStepDecimal ? Math.Round(value, 2) : value;

        private string ValueWithSuffix => string.IsNullOrEmpty(Suffix) ? $"{Normalize(Value)}" : $"{Normalize(Value)} {Suffix}";
    }
}
