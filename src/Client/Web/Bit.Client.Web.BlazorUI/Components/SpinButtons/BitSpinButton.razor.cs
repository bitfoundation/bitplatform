using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitSpinButton
    {
        private double inputValue;
        private BitSpinButtonLabelPosition labelPosition = BitSpinButtonLabelPosition.Left;
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
        [Parameter] public EventCallback<BitSpinButtonChangeEventArgs> OnDecrement { get; set; }

        /// <summary>
        /// Callback for when the increment button or up arrow key is pressed
        /// </summary>
        [Parameter] public EventCallback<BitSpinButtonChangeEventArgs> OnIncrement { get; set; }

        /// <summary>
        /// Min value of the spin button. If not provided, the spin button has minimum value of double type
        /// </summary>
        [Parameter] public double? Min { get; set; }

        /// <summary>
        /// Max value of the spin button. If not provided, the spin button has max value of double type
        /// </summary>
        [Parameter] public double? Max { get; set; }

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
        public BitSpinButtonLabelPosition LabelPosition
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
        [Parameter] public string DecrementButtonIconName { get; set; } = "ChevronDownSmall";

        /// <summary>
        /// Custom icon name for the increment button
        /// </summary>
        [Parameter] public string IncrementButtonIconName { get; set; } = "ChevronUpSmall";

        /// <summary>
        /// A more descriptive title for the control, visible on its tooltip
        /// </summary>
        [Parameter] public string? Title { get; set; }

        /// <summary>
        /// How many decimal places the value should be rounded to
        /// </summary>
        [Parameter] public int? Precision { get; set; }

        /// <summary>
        /// Additional props for the input field
        /// </summary>
        [Parameter]
        public Dictionary<string, object>? InputHtmlAttributes { get; set; }

        protected async override Task OnInitializedAsync()
        {
            min = Min is not null ? Min.Value : double.MinValue;
            max = Max is not null ? Max.Value : double.MaxValue;
            precision = Precision is not null ? Precision.Value : CalculatePrecision(Step);
            if (ValueHasBeenSet is false)
            {
                Value = DefaultValue ?? Math.Min(0, min);
            }

            intermediateValue = $"{Value}";
            await base.OnInitializedAsync();
        }

        protected override string RootElementClass => "bit-spb";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => LabelPosition == BitSpinButtonLabelPosition.Left
                                                ? $"{RootElementClass}-label-left-{VisualClassRegistrar()}"
                                                : $"{RootElementClass}-label-top-{VisualClassRegistrar()}");
        }

        protected virtual void HandleChange(ChangeEventArgs e)
        {
            if (IsEnabled is false) return;
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false)
            {
                //update input field value
                //intermediateValue = $"{Value}";
                StateHasChanged();
                return;
            }

            intermediateValue = e.Value?.ToString();
        }

        protected virtual async Task HandleButtonClick(BitSpinButtonAction action, MouseEventArgs e)
        {
            if (IsEnabled is false) return;
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

            double result = 0;
            bool isValid = false;

            switch (action)
            {
                case BitSpinButtonAction.Increment:
                    if (OnIncrement.HasDelegate is true)
                    {
                        var args = new BitSpinButtonChangeEventArgs();
                        args.Value = Value;
                        args.MouseEventArgs = e;
                        await OnIncrement.InvokeAsync(args);
                        break;
                    }

                    result = Value + Step;
                    isValid = result <= max && result >= min;
                    break;

                case BitSpinButtonAction.Decrement:
                    if (OnDecrement.HasDelegate is true)
                    {
                        var args = new BitSpinButtonChangeEventArgs();
                        args.Value = Value;
                        args.MouseEventArgs = e;
                        await OnDecrement.InvokeAsync(args);
                        break;
                    }

                    result = Value - Step;
                    isValid = result <= max && result >= min;
                    break;

                default:
                    break;
            }

            if (isValid is false) return;
            Value = Normalize(result);
            await OnChange.InvokeAsync(Value);
        }

        protected virtual async Task HandleKeyDown(KeyboardEventArgs e)
        {
            if (IsEnabled is false) return;
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

            double result = 0;
            bool isValid = false;

            switch (e.Key)
            {
                case "ArrowUp":
                    if (OnIncrement.HasDelegate is true)
                    {
                        var args = new BitSpinButtonChangeEventArgs();
                        args.Value = Value;
                        args.KeyboardEventArgs = e;
                        await OnIncrement.InvokeAsync(args);
                        break;
                    }

                    result = Value + Step;
                    isValid = result <= max && result >= min;
                    break;

                case "ArrowDown":
                    if (OnDecrement.HasDelegate is true)
                    {
                        var args = new BitSpinButtonChangeEventArgs();
                        args.Value = Value;
                        args.KeyboardEventArgs = e;
                        await OnDecrement.InvokeAsync(args);
                        break;
                    }

                    result = Value - Step;
                    isValid = result <= max && result >= min;
                    break;

                case "Enter":
                    if (intermediateValue == Value.ToString()) break;

                    var isNumber = double.TryParse(intermediateValue, out var numericValue);
                    if (isNumber)
                    {
                        Value = Normalize(numericValue);
                        if (Value > max) Value = max;
                        if (Value < min) Value = min;
                        await OnChange.InvokeAsync(Value);
                    }
                    else
                    {
                        //update input field value
                    }

                    intermediateValue = Value.ToString();
                    break;

                default:
                    break;
            }

            if (isValid is false) return;
            Value = Normalize(result);
            await OnChange.InvokeAsync(Value);
        }

        protected virtual async Task HandleBlur(FocusEventArgs e)
        {
            if (IsEnabled is false) return;
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

            if (intermediateValue == Value.ToString()) return;

            var isNumber = double.TryParse(intermediateValue, out var numericValue);
            if (isNumber)
            {
                Value = Normalize(numericValue);
                if (Value > max) Value = max;
                if (Value < min) Value = min;
                await OnBlur.InvokeAsync(e);
                await OnChange.InvokeAsync(Value);
            }
            else
            {
                //update input field value
                intermediateValue = Value.ToString();
            }
        }

        protected virtual async Task HandleFocus(FocusEventArgs e)
        {
            if (IsEnabled)
            {
                await OnFocus.InvokeAsync(e);
            }
        }

        private int CalculatePrecision(double value)
        {
            var regex = new Regex(@"[1-9]([0]+$)|\.([0-9]*)");
            if (regex.IsMatch(value.ToString()) is false) return 0;

            var matches = regex.Matches(value.ToString());
            if (matches.Count == 0) return 0;

            var groups = matches[0].Groups;
            if (groups[1] != null && groups[1].Length != 0)
            {
                return -groups[1].Length;
            }

            if (groups[2] != null && groups[2].Length != 0)
            {
                return groups[2].Length;
            }

            return 0;
        }

        private double Normalize(double value) => Math.Round(value, precision);
        private double? ariaValueNow => AriaValueNow is not null ? AriaValueNow : Suffix.HasNoValue() ? Value : null;
        private string? ariaValueText => AriaValueText.HasValue() ? AriaValueText : Suffix.HasValue() ? $"{Normalize(Value)}{Suffix}" : null;
        private string intermediateValue { get; set; } = String.Empty;
        private string inputId { get; set; } = $"input{Guid.NewGuid()}";
        private string labelId => Label.HasValue() ? $"label{Guid.NewGuid()}" : String.Empty;
        private int precision { get; set; }
        private double min { get; set; }
        private double max { get; set; }
        //private double value { get; set; }
    }
}
