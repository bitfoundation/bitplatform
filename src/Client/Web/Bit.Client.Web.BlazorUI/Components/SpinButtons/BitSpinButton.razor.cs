using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Client.Web.BlazorUI.Components.SpinButtons;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitSpinButton
    {
        private double inputValue;
        private LabelPosition labelPosition = LabelPosition.Left;
        private bool ValueHasBeenSet;

        /// <summary>
        /// Detailed description of the input for the benefit of screen readers
        /// </summary>
        [Parameter] public string? AriaDescription { get; set; }

        // <summary>
        // If true, add an aria-hidden attribute instructing screen readers to ignore the element
        // </summary>
        [Parameter] public bool AriaHidden { get; set; }

        /// <summary>
        /// The position in the parent set (if in a set)
        /// </summary>
        [Parameter]
        public int? AriaPositionInSet { get; set; }

        /// <summary>
        /// The total size of the parent set (if in a set)
        /// </summary>
        [Parameter]
        public int? AriaSetSize { get; set; }

        /// <summary>
        /// Sets the control's aria-valuenow. Providing this only makes sense when using as a controlled component.
        /// </summary>
        [Parameter]
        public double? AriaValueNow { get; set; }

        /// <summary>
        /// Sets the control's aria-valuetext.
        /// </summary>
        [Parameter]
        public string? AriaValueText { get; set; }

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
        /// Callback for when the spin button value change
        /// </summary>
        [Parameter] public EventCallback<double> OnChange { get; set; }

        /// <summary>
        /// Callback for when focus moves into the input
        /// </summary>
        [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }

        /// <summary>
        /// Callback for when the control loses focus
        /// </summary>
        [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }

        /// <summary>
        /// Callback for when the decrement button or down arrow key is pressed
        /// </summary>
        [Parameter] public EventCallback<BitSpinButtonEventArgs> OnDecrement { get; set; }

        /// <summary>
        /// Callback for when the increment button or up arrow key is pressed
        /// </summary>
        [Parameter] public EventCallback<BitSpinButtonEventArgs> OnIncrement { get; set; }

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
        [Parameter] public double? DefaultValue { get; set; }

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
        /// Accessible label text for the decrement button (for screen reader users)
        /// </summary>
        [Parameter] public string? DecrementButtonAriaLabel { get; set; }

        /// <summary>
        /// Accessible label text for the increment button (for screen reader users)
        /// </summary>
        [Parameter] public string? IncrementButtonAriaLabel { get; set; }

        /// <summary>
        /// Custom icon name for the decrement button
        /// </summary>
        [Parameter] public string DecrementButtonIcon { get; set; } = "ChevronDownSmall";

        /// <summary>
        /// Custom icon name for the increment button
        /// </summary>
        [Parameter] public string IncrementButtonIcon { get; set; } = "ChevronUpSmall";

        /// <summary>
        /// A more descriptive title for the control, visible on its tooltip
        /// </summary>
        [Parameter] public string? Title { get; set; }

        /// <summary>
        /// How many decimal places the value should be rounded to
        /// </summary>
        [Parameter] public int Precision { get; set; } = 1;

        protected async override Task OnInitializedAsync()
        {
            if (ValueChanged.HasDelegate is false)
            {
                Value = DefaultValue ?? Math.Min(0, Min);
            }

            await base.OnInitializedAsync();
        }

        protected virtual async Task HandleButtonClick(SpinButtonAction action, MouseEventArgs e)
        {
            if (IsEnabled is false) return;
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

            double result = 0;
            bool isValid = false;

            switch (action)
            {
                case SpinButtonAction.Up:
                    if (OnIncrement.HasDelegate is true)
                    {
                        var args = new BitSpinButtonEventArgs();
                        args.Value = Value;
                        args.MouseEventArgs = e;
                        await OnIncrement.InvokeAsync(args);
                        break;
                    }

                    result = Value + Step;
                    isValid = result <= Max && result >= Min;
                    break;

                case SpinButtonAction.Down:
                    if (OnDecrement.HasDelegate is true)
                    {
                        var args = new BitSpinButtonEventArgs();
                        args.Value = Value;
                        args.MouseEventArgs = e;
                        await OnDecrement.InvokeAsync(args);
                        break;
                    }

                    result = Value - Step;
                    isValid = result <= Max && result >= Min;
                    break;

                default:
                    break;
            }

            if (isValid is false) return;
            await OnChange.InvokeAsync(Value);
            Value = Normalize(result);
        }

        protected virtual async Task HandleFocus(FocusEventArgs e)
        {
            if (IsEnabled)
            {
                await OnFocus.InvokeAsync(e);
            }
        }

        protected virtual async Task HandleBlur(FocusEventArgs e)
        {
            if (IsEnabled)
            {
                if (Value > Max) Value = Max;
                if (Value < Min) Value = Min;
                await OnBlur.InvokeAsync(e);
                await OnChange.InvokeAsync(Value);
            }
        }

        protected override string RootElementClass => "bit-spb";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => LabelPosition == LabelPosition.Left
                                                ? $"{RootElementClass}-label-left-{VisualClassRegistrar()}"
                                                : $"{RootElementClass}-label-top-{VisualClassRegistrar()}");
        }

        protected virtual async Task HandleChange(ChangeEventArgs e)
        {
            if (IsEnabled is false) return;
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false)
            {
                //update input field value
                StateHasChanged();
                return;
            }

            var userInput = e.Value?.ToString();
            var isNumber = double.TryParse(userInput, out var numericValue);
            if (isNumber)
            {
                Value = numericValue;
            }
            else
            {
                //update input field value
            }
        }

        private bool IsStepDecimal => Step % 1 != 0;

        private double Normalize(double value) => IsStepDecimal ? Math.Round(value, 2) : value;

        private double? ariaValueNow => AriaValueNow is not null ? AriaValueNow : Suffix.HasNoValue() ? Value : null;
        private string? ariaValueText => AriaValueText.HasValue() ? AriaValueText : Suffix.HasValue() ? $"{Normalize(Value)}{Suffix}" : null;
        private ElementReference inputRef { get; set; }
    }
}
