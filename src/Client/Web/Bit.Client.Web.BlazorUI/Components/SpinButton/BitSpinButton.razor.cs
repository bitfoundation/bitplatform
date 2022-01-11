using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitSpinButton : IDisposable
    {
        const int INITIAL_STEP_DELAY = 400;
        const int STEP_DELAY = 75;
        private double inputValue;
        private BitSpinButtonLabelPosition labelPosition = BitSpinButtonLabelPosition.Left;
        private int precision;
        private double min;
        private double max;
        private string InputId = $"input{Guid.NewGuid()}";
        private string IntermediateValue = string.Empty;
        private bool ValueHasBeenSet;
        private Timer? timer;

        /// <summary>
        /// Detailed description of the input for the benefit of screen readers
        /// </summary>
        [Parameter] public string? AriaDescription { get; set; }

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
        /// Min value of the spin button. If not provided, the spin button has minimum value of double type
        /// </summary>
        [Parameter] public double? Min { get; set; }

        /// <summary>
        /// Max value of the spin button. If not provided, the spin button has max value of double type
        /// </summary>
        [Parameter] public double? Max { get; set; }

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
        [Parameter] public EventCallback<BitSpinButtonChangeValue> OnDecrement { get; set; }

        /// <summary>
        /// Callback for when the increment button or up arrow key is pressed
        /// </summary>
        [Parameter] public EventCallback<BitSpinButtonChangeValue> OnIncrement { get; set; }

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
        /// Shows the custom Label for spin button. If you don't call default label, ensure that you give your custom label an id and that you set the input's aria-labelledby prop to that id.
        /// </summary>
        [Parameter] public RenderFragment? LabelFragment { get; set; }

        /// <summary>
        /// Icon name for an icon to display alongside the spin button's label
        /// </summary>
        [Parameter] public BitIconName? IconName { get; set; }

        /// <summary>
        /// The aria label of the icon for the benefit of screen readers
        /// </summary>
        [Parameter] public string IconAriaLabel { get; set; } = string.Empty;

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
        [Parameter] public BitIconName DecrementButtonIconName { get; set; } = BitIconName.ChevronDownSmall;

        /// <summary>
        /// Custom icon name for the increment button
        /// </summary>
        [Parameter] public BitIconName IncrementButtonIconName { get; set; } = BitIconName.ChevronUpSmall;

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
#pragma warning disable CA2227 // Collection properties should be read only
        [Parameter] public Dictionary<string, object>? InputHtmlAttributes { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        [Parameter] public EventCallback<BitSpinButtonAction> ChangeHandler { get; set; }

        protected override string RootElementClass => "bit-spb";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => LabelPosition == BitSpinButtonLabelPosition.Left
                                                ? $"{RootElementClass}-label-left-{VisualClassRegistrar()}"
                                                : $"{RootElementClass}-label-top-{VisualClassRegistrar()}");
        }

        protected async override Task OnParametersSetAsync()
        {
            min = Min is not null ? Min.Value : double.MinValue;
            max = Max is not null ? Max.Value : double.MaxValue;
            if (min > max)
            {
                min = min + max;
                max = min - max;
                min = min - max;
            }

            precision = Precision is not null ? Precision.Value : CalculatePrecision(Step);
            if (ValueHasBeenSet is false)
            {
                Value = DefaultValue ?? Math.Min(0, min);
            }

            CheckValue();
            IntermediateValue = $"{Value}";
            if (ChangeHandler.HasDelegate is false)
            {
                ChangeHandler = EventCallback.Factory.Create(this, async (BitSpinButtonAction action) =>
                {
                    double result = 0;
                    bool isValid = false;

                    switch (action)
                    {
                        case BitSpinButtonAction.Increment:
                            result = Value + Step;
                            isValid = result <= max && result >= min;
                            break;

                        case BitSpinButtonAction.Decrement:
                            result = Value - Step;
                            isValid = result <= max && result >= min;
                            break;

                        default:
                            break;
                    }

                    if (isValid is false) return;

                    Value = Normalize(result);
                    CheckValue();
                    IntermediateValue = $"{Value}";
                    await OnChange.InvokeAsync(Value);
                });
            }

            await base.OnParametersSetAsync();
        }

        private async void HandleMouseDown(BitSpinButtonAction action, MouseEventArgs e)
        {
            await HandleMouseDownAction(action, e);
            timer = new Timer((_) =>
            {
                InvokeAsync(async () =>
                {
                    await HandleMouseDownAction(action, e);
                    StateHasChanged();
                });
            }, null, INITIAL_STEP_DELAY, STEP_DELAY);
        }

        private void HandleMouseUpOrOut()
        {
            if (timer is null) return;
            timer.Dispose();
        }

        private void HandleChange(ChangeEventArgs e)
        {
            if (IsEnabled is false) return;
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

            IntermediateValue = $"{e.Value}";
        }

        private async Task HandleMouseDownAction(BitSpinButtonAction action, MouseEventArgs e)
        {
            if (IsEnabled is false) return;
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

            await ChangeHandler.InvokeAsync(action);
            if (action is BitSpinButtonAction.Increment && OnIncrement.HasDelegate is true)
            {
                var args = new BitSpinButtonChangeValue
                {
                    Value = Value,
                    MouseEventArgs = e
                };
                await OnIncrement.InvokeAsync(args);
            }

            if (action is BitSpinButtonAction.Decrement && OnDecrement.HasDelegate is true)
            {
                var args = new BitSpinButtonChangeValue
                {
                    Value = Value,
                    MouseEventArgs = e
                };
                await OnDecrement.InvokeAsync(args);
            }
        }

        private async Task HandleKeyDown(KeyboardEventArgs e)
        {
            if (IsEnabled is false) return;
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

            switch (e.Key)
            {
                case "ArrowUp":
                    await ChangeHandler.InvokeAsync(BitSpinButtonAction.Increment);
                    break;

                case "ArrowDown":
                    await ChangeHandler.InvokeAsync(BitSpinButtonAction.Decrement);
                    break;

                case "Enter":
                    if (IntermediateValue == $"{Value}") break;

                    var isNumber = double.TryParse(IntermediateValue, out var numericValue);
                    if (isNumber)
                    {
                        Value = Normalize(numericValue);
                        CheckValue();
                        await OnChange.InvokeAsync(Value);
                    }
                    else
                    {
                        await Task.Delay(1);
                        IntermediateValue = $"{Value}";
                        StateHasChanged();
                    }
                    break;

                default:
                    break;
            }

            if (e.Key is "ArrowUp" && OnIncrement.HasDelegate is true)
            {
                var args = new BitSpinButtonChangeValue
                {
                    Value = Value,
                    KeyboardEventArgs = e
                };
                await OnIncrement.InvokeAsync(args);
            }

            if (e.Key is "ArrowDown" && OnDecrement.HasDelegate is true)
            {
                var args = new BitSpinButtonChangeValue
                {
                    Value = Value,
                    KeyboardEventArgs = e
                };
                await OnDecrement.InvokeAsync(args);
            }
        }

        private async Task HandleBlur(FocusEventArgs e)
        {
            if (IsEnabled is false) return;
            await OnBlur.InvokeAsync(e);
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;
            if (IntermediateValue == $"{Value}") return;

            var isNumber = double.TryParse(IntermediateValue, out var numericValue);
            if (isNumber)
            {
                Value = Normalize(numericValue);
                CheckValue();
                IntermediateValue = $"{Value}";
                await OnChange.InvokeAsync(Value);
            }
            else
            {
                IntermediateValue = $"{Value}";
                StateHasChanged();
            }
        }

        private async Task HandleFocus(FocusEventArgs e)
        {
            if (IsEnabled)
            {
                await OnFocus.InvokeAsync(e);
            }
        }

        private static int CalculatePrecision(double value)
        {
            var regex = new Regex(@"[1-9]([0]+$)|\.([0-9]*)");
            if (regex.IsMatch(value.ToString(CultureInfo.InvariantCulture)) is false) return 0;

            var matches = regex.Matches(value.ToString(CultureInfo.InvariantCulture));
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

        private void CheckValue()
        {
            if (Value > max) Value = max;
            if (Value < min) Value = min;
        }

        private double Normalize(double value) => Math.Round(value, precision);

        private double? GetAriaValueNow => AriaValueNow is not null ? AriaValueNow : Suffix.HasNoValue() ? Value : null;
        private string? GetAriaValueText => AriaValueText.HasValue() ? AriaValueText : Suffix.HasValue() ? $"{Normalize(Value)}{Suffix}" : null;
        private string? GetIconRole => IconAriaLabel.HasValue() ? "img" : null;
        private string GetLabelId => Label.HasValue() ? $"label{Guid.NewGuid()}" : string.Empty;

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            timer?.Dispose();

            _disposed = true;
        }
    }
}
