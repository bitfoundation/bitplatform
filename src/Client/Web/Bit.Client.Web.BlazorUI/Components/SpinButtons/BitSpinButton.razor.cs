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

        [Inject] public IJSRuntime? JSRuntime { get; set; }

        /// <summary>
        /// Current value of the spin button
        /// </summary>
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

        /// <summary>
        /// Callback for when the spin button value change
        /// </summary>
        [Parameter] public EventCallback<double> ValueChanged { get; set; }

        /// <summary>
        /// Min value of the spin button. If not provided, the spin button has minimum value of double type
        /// </summary>
        [Parameter] public double Min { get; set; } = double.MinValue;

        /// <summary>
        /// Max value of the spin button. If not provided, the spin button has max value of double type
        /// </summary>
        [Parameter] public double Max { get; set; } = double.MaxValue;

        /// <summary>
        /// Initial value of the spin button 
        /// </summary>
        [Parameter] public double DefaultValue { get; set; } = 0;

        /// <summary>
        /// Difference between two adjacent values of the spin button
        /// </summary>
        [Parameter] public double Step { get; set; } = 1;

        /// <summary>
        /// A text is shown after the spin button value
        /// </summary>
        [Parameter] public string Suffix { get; set; } = string.Empty;

        /// <summary>
        /// Descriptive label for the spin button, Label displayed above the spin button and read by screen readers
        /// </summary>
        [Parameter] public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Icon name for an icon to display alongside the spin button's label
        /// </summary>
        [Parameter] public string IconName { get; set; } = string.Empty;

        /// <summary>
        /// The position of the label in regards to the spin button
        /// </summary>
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

        /// <summary>
        /// Callback for when the spin button value change
        /// </summary>
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

        private string ValueWithSuffix => Suffix.HasNoValue() ? $"{Normalize(Value)}" : $"{Normalize(Value)} {Suffix}";
    }
}
