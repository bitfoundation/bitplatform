using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitChoiceGroup
    {
        private bool isRequired;
        private string? selectedKey;
        private bool SelectedKeyHasBeenSet;
        private BitChoiceOption? SelectedOption;
        private List<BitChoiceOption> AllOptions = new();

        /// <summary>
        /// Default selected key for ChoiceGroup.
        /// </summary>
        [Parameter] public string? DefaultSelectedKey { get; set; }

        /// <summary>
        /// If true, an option must be selected in the ChoiceGroup.
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
        /// Descriptive label for the choice group.
        /// </summary>
        [Parameter] public string? Label { get; set; }

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
        /// Used to customize the label for the choice group.
        /// </summary>
        [Parameter] public RenderFragment? LabelFragment { get; set; }

        /// <summary>
        /// Name of ChoiceGroup, this name is used to group each ChoiceOption into the same logical ChoiceGroup
        /// </summary>
        [Parameter] public string Name { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Value of ChoiceGroup, the value of selected ChoiceOption set on it
        /// </summary>
        [Parameter] public string? Value { get; set; }

        /// <summary>
        /// The content of ChoiceGroup, common values are ChoiceOption component 
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Callback that is called when the value parameter is changed
        /// </summary>
        [Parameter] public EventCallback<string> OnValueChange { get; set; }

        internal async Task SelectOption(BitChoiceOption option)
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

        internal void RegisterOption(BitChoiceOption option)
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
                option.Key = AllOptions.Count.ToString();
            }

            if (SelectedKey == option.Key)
            {
                option.SetState(true);
                SelectedOption = option;
            }
            AllOptions.Add(option);
        }

        internal void UnregisterOption(BitChoiceOption option)
        {
            AllOptions.Remove(option);
        }

        protected override string RootElementClass => "bit-chg";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled && IsRequired
                                       ? $"{RootElementClass}-required-{VisualClassRegistrar()}" : string.Empty);
        }

        protected override Task OnInitializedAsync()
        {
            selectedKey = selectedKey ?? DefaultSelectedKey;
            return base.OnInitializedAsync();
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
    }
}
