using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitChoiceGroup
    {
        private readonly List<BitChoiceOption> _options = new();
        private bool isRequired;

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

        internal async Task ChangeSelection(BitChoiceOption option)
        {
            if (IsEnabled)
            {
                foreach (BitChoiceOption item in _options)
                {
                    item.SetOptionCheckedStatus(item == option);
                }
                Value = option.Value;
                await OnValueChange.InvokeAsync(option.Value);
            }
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
            _options.Add(option);
        }

        internal void UnregisterOption(BitChoiceOption option)
        {
            _options.Remove(option);
        }
    }
}
