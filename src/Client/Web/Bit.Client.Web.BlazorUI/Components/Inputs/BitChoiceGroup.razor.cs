using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitChoiceGroup
    {
        private readonly List<BitChoiceOption> _options = new();
        private bool isRequired;
        private string? selectedKey;
        internal BitChoiceOption? SelectedOption { get; set; }

        public bool SelectedKeyHasBeenSet { get; private set; }

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
        /// contain the key of the selected item
        /// </summary>
        [Parameter]
        public string? SelectedKey
        {
            get => selectedKey;
            set
            {
                if (value == selectedKey) return;

                selectedKey = value;
                SelectItemByKey();

                _ = SelectedKeyChanged.InvokeAsync(value);
            }
        }

        private void SelectItemByKey()
        {
            if (SelectedKey is null || _options is null) return;

            SelectedOption = _options.FirstOrDefault(i => i.Key == SelectedKey);
            _ = SelectOption(SelectedOption);
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
        /// Callback that is called when the value parameter changed
        /// </summary>
        [Parameter] public EventCallback<string> OnValueChange { get; set; }

        protected override string RootElementClass => "bit-chg";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled && IsRequired
                                       ? $"{RootElementClass}-required-{VisualClassRegistrar()}" : string.Empty);
        }


        internal async Task SelectOption(BitChoiceOption? option)
        {
            if (SelectedKeyHasBeenSet && SelectedKeyChanged.HasDelegate is false) return;

            Value = option!.Value;
            SelectedKey = option!.Key;

            foreach (BitChoiceOption item in _options)
            {
                item.SetOptionCheckedStatus(item == option);
            }

            SelectedOption?.SelectedItemChanged(option);
            SelectedOption = option;
            SelectedOption?.SelectedItemChanged(option);

            await OnValueChange.InvokeAsync(option!.Value);

            await SelectedKeyChanged.InvokeAsync(SelectedKey);
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

            if (SelectedKey == option.Key)
            {
                _ = SelectOption(option);
            }
            _options.Add(option);
        }

        internal void UnregisterOption(BitChoiceOption option)
        {
            _options.Remove(option);
        }

        protected override Task OnInitializedAsync()
        {
            selectedKey = selectedKey ?? DefaultSelectedKey;
            return base.OnInitializedAsync();
        }


    }
}
