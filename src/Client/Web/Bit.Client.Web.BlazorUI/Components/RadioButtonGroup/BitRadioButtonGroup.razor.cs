using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitRadioButtonGroup
    {
        private bool isRequired;
        private string? selectedKey;
        private bool SelectedKeyHasBeenSet;
        private BitRadioButtonOption? SelectedOption;
        private List<BitRadioButtonOption> AllOptions = new();

        /// <summary>
        /// Default selected key for RadioButtonGroup.
        /// </summary>
        [Parameter] public string? DefaultSelectedKey { get; set; }

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
        /// Contains the key of the selected item
        /// </summary>
        [Parameter]
        public string? SelectedKey
        {
            get => selectedKey;
            set
            {
                if (value == selectedKey) return;
                SelectOptionByKey(value);
            }
        }

        [Parameter] public EventCallback<string?> SelectedKeyChanged { get; set; }

        /// <summary>
        /// Used to customize the label for the RadioButtonGroup.
        /// </summary>
        [Parameter] public RenderFragment? LabelFragment { get; set; }

        /// <summary>
        /// Name of RadioButtonGroup, this name is used to group each RadioButtonGroup into the same logical RadioButtonGroup
        /// </summary>
        [Parameter] public string Name { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Value of RadioButtonGroup, the value of selected RadioButtonGroup set on it
        /// </summary>
        [Parameter] public string? Value { get; set; }

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
        }

        protected override Task OnInitializedAsync()
        {
            selectedKey = selectedKey ?? DefaultSelectedKey;
            LabelId = $"RadioButtonGroupLabel{UniqueId}";
            return base.OnInitializedAsync();
        }

        internal async Task SelectOption(BitRadioButtonOption option)
        {
            if (SelectedKeyHasBeenSet && SelectedKeyChanged.HasDelegate is false) return;

            SelectedOption?.SetState(false);
            option.SetState(true);

            Value = option.Value;
            SelectedOption = option;
            selectedKey = option.Key;

            await SelectedKeyChanged.InvokeAsync(selectedKey);

            await OnValueChange.InvokeAsync(Value);
        }

        internal void RegisterOption(BitRadioButtonOption option)
        {
            if (IsEnabled is false)
            {
                option.IsEnabled = false;
            }

            if (IsRequired)
            {
                option.IsRequired = true;
            }

            if (option.Key.HasNoValue())
            {
                option.Key = AllOptions.Count.ToString(CultureInfo.InvariantCulture);
            }

            if (SelectedKey == option.Key)
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

        private void SelectOptionByKey(string? key)
        {
            var newOption = AllOptions.FirstOrDefault(i => i.Key == key);

            if (newOption == null || newOption == SelectedOption || newOption.IsEnabled is false)
            {
                _ = SelectedKeyChanged.InvokeAsync(selectedKey);
                return;
            }

            _ = SelectOption(newOption);
        }

        private string GetAriaLabelledBy() => Label.HasValue() || LabelFragment is not null ? LabelId : AriaLabelledBy;
    }
}
