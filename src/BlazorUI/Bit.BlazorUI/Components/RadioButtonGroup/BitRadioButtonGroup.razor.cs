using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI
{
    public partial class BitRadioButtonGroup
    {
        private bool isRequired;
        private BitRadioButtonOption? SelectedOption;
        private List<BitRadioButtonOption> AllOptions = new();

        /// <summary>
        /// Default value for RadioButtonGroup.
        /// </summary>
        [Parameter] public string? DefaultValue { get; set; }

        /// <summary>
        /// If true, an option must be selected in the RadioButtonGroup.
        /// </summary>
        [Parameter]
        public bool IsRequired
        {
            get => isRequired;
            set
            {
                isRequired = value;
                ClassBuilder.Reset();
            }
        }

        /// <summary>
        /// Descriptive label for the RadioButtonGroup.
        /// </summary>
        [Parameter] public string? Label { get; set; }

        /// <summary>
        /// ID of an element to use as the aria label for this RadioButtonGroup.
        /// </summary>
        [Parameter] public string AriaLabelledBy { get; set; } = string.Empty;

        /// <summary>
        /// Used to customize the label for the RadioButtonGroup.
        /// </summary>
        [Parameter] public RenderFragment? LabelFragment { get; set; }

        /// <summary>
        /// Name of RadioButtonGroup, this name is used to group each RadioButtonGroup into the same logical RadioButtonGroup
        /// </summary>
        [Parameter] public string Name { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The content of RadioButtonGroup, common values are RadioButtonGroup component 
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Callback that is called when the value parameter is changed
        /// </summary>
        [Parameter] public EventCallback<string> OnValueChange { get; set; }

        public string LabelId { get; set; } = string.Empty;

        protected override string RootElementClass => "bit-rbg";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled && IsRequired
                                       ? $"{RootElementClass}-required-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => ValueInvalid is true
                                       ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}" : string.Empty);
        }

        protected override async Task OnInitializedAsync()
        {
            CurrentValue ??= DefaultValue;
            LabelId = $"RadioButtonGroupLabel{UniqueId}";
            OnCurrentValueChanged += HandleOnCurrentValueChanged;

            await base.OnInitializedAsync();
        }

        internal async Task SelectOption(BitRadioButtonOption option)
        {
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

            SelectedOption?.SetState(false);
            option.SetState(true);

            SelectedOption = option;
            CurrentValue = option.Value;

            await OnValueChange.InvokeAsync(CurrentValue);
        }

        internal void RegisterOption(BitRadioButtonOption option)
        {
            if (option.Value.HasNoValue())
            {
                option.Value = AllOptions.Count.ToString(CultureInfo.InvariantCulture);
            }

            if (CurrentValue == option.Value)
            {
                option.SetState(true);
                SelectedOption = option;
            }

            AllOptions.Add(option);
        }

        internal void UnregisterOption(BitRadioButtonOption option)
        {
            AllOptions.Remove(option);
        }

        private void SelectOptionByKey(string? value)
        {
            var newOption = AllOptions.FirstOrDefault(i => i.Value == value);

            if (newOption is null || newOption == SelectedOption || newOption.IsEnabled is false)
            {
                _ = ValueChanged.InvokeAsync(Value);
                return;
            }

            _ = SelectOption(newOption);
        }

        private string GetAriaLabelledBy() => Label.HasValue() || LabelFragment is not null ? LabelId : AriaLabelledBy;

        private void HandleOnCurrentValueChanged(object? sender, EventArgs args)
        {
            SelectOptionByKey(CurrentValue);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                OnCurrentValueChanged -= HandleOnCurrentValueChanged;
            }

            base.Dispose(disposing);
        }

        /// <inheritdoc />
        protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string result, [NotNullWhen(false)] out string? validationErrorMessage)
            => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");
    }
}
