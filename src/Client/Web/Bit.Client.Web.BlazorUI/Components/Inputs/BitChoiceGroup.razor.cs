using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitChoiceGroup
    {
        private readonly List<BitChoiceOption> _options = new();

        /// <summary>
        /// Name of choicegroup, this name is used to group each choiceoption into the same logical choicegroup
        /// </summary>
        [Parameter] public string Name { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Value of choice group, the value of selectedchoice option set on it
        /// </summary>
        [Parameter] public string? Value { get; set; }

        /// <summary>
        /// The content of choicegroup, common values are choiceoption component 
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Callback that is called when the value parameter changed
        /// </summary>
        [Parameter] public EventCallback<string> OnValueChange { get; set; }

        protected override string RootElementClass => "bit-chg";

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
            _options.Add(option);
        }

        internal void UnregisterOption(BitChoiceOption option)
        {
            _options.Remove(option);
        }
    }
}
