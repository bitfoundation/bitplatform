using System;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Buttons
{
    public partial class BitToggleButtonDemo
    {
        private bool TogglePrimaryButtonChecked;
        private bool ToggleStandardButtonChecked;
        private bool ToggleDisabledButtonChecked;
        private bool ToggleButtonChecked;

        private bool ToggleButtonForOnChange = true;
        private bool OnToggleButtonChanged = true;

        private bool ToggleButtonTwoWayValue = true;

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "AllowDisabledFocus",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the toggle button can have focus in disabled mode.",
            },
            new ComponentParameter()
            {
                Name = "AriaDescription",
                Type = "string",
                DefaultValue = "",
                Description = "Detailed description of the toggle button for the benefit of screen readers.",
            },
            new ComponentParameter()
            {
                Name = "AriaHidden",
                Type = "bool",
                DefaultValue = "false",
                Description = "If true, add an aria-hidden attribute instructing screen readers to ignore the element.",
            },
            new ComponentParameter()
            {
                Name = "ButtonStyle",
                Type = "BitButtonStyle",
                LinkType = LinkType.Link,
                Href = "#button-style-enum",
                DefaultValue = "ButtonStyle.Primary",
                Description = "The style of toggle button, Possible values: Primary | Standard",
            },
            new ComponentParameter()
            {
                Name = "Href",
                Type = "string",
                DefaultValue = "",
                Description = "URL the link points to, if provided, button renders as an anchor.",
            },
            new ComponentParameter()
            {
                Name = "IconName",
                Type = "string",
                DefaultValue = "",
                Description = "The icon that shows in the toggle button.",
            },
            new ComponentParameter()
            {
                Name = "IsChecked",
                Type = "bool",
                DefaultValue = "",
                Description = "Determine if the toggle button is in checked state, default is true.",
            },
            new ComponentParameter()
            {
                Name = "Label",
                Type = "string",
                DefaultValue = "",
                Description = "The text that shows in the label.",
            },
            new ComponentParameter()
            {
                Name = "OnChange",
                Type = "EventCallback<bool>",
                DefaultValue = "",
                Description = "Callback that is called when the IsChecked value has changed.",
            },
            new ComponentParameter()
            {
                Name = "OnClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the button clicked.",
            },
            new ComponentParameter()
            {
                Name = "Target",
                Type = "string",
                DefaultValue = "",
                Description = "If Href provided, specifies how to open the link.",
            },
            new ComponentParameter()
            {
                Name = "Title",
                Type = "string",
                DefaultValue = "",
                Description = "The title to show when the mouse is placed on the toggle button.",
            },
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "button-style-enum",
                Title = "ButtonStyle enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "Primary",
                        Description="The button with white text on a blue background.",
                        Value="0",
                    },
                    new EnumItem()
                    {
                        Name= "Standard",
                        Description="The button with black text on a white background.",
                        Value="1",
                    }
                }
            }
        };

        private readonly string toggleButtonSampleCode = $"<BitToggleButton @bind-IsChecked='TogglePrimaryButtonChecked' Label='@(TogglePrimaryButtonChecked ? 'Primary Mute' : 'Primary Unmute')' IconName='@(TogglePrimaryButtonChecked ? 'MicOff' : 'Microphone') ButtonStyle='ButtonStyle.Primary'></BitToggleButton>{Environment.NewLine}" +
             $"<BitToggleButton @bind-IsChecked='ToggleStandardButtonChecked' Label='@(ToggleStandardButtonChecked ? 'Standard Mute' : 'Standard Unmute')' IconName='@(ToggleStandardButtonChecked ? 'MicOff' : 'Microphone')' ButtonStyle='ButtonStyle.Standard'></BitToggleButton>{Environment.NewLine}" +
             $"<BitToggleButton @bind-IsChecked='ToggleDisabledButtonChecked' Label='@(ToggleDisabledButtonChecked ? 'Disabled Mute' : 'Disabled Unmute')' IconName='@(ToggleDisabledButtonChecked ? 'MicOff' : 'Microphone')' IsEnabled='false'></BitToggleButton>{Environment.NewLine}" +
             $"@code {{ {Environment.NewLine}" +
             $"private bool TogglePrimaryButtonChecked = false; {Environment.NewLine}" +
             $"private bool ToggleStandardButtonChecked =  false; {Environment.NewLine}" +
             $"private bool ToggleDisabledButtonChecked = false; {Environment.NewLine}" +
             "}";

        private readonly string onChangeSampleCode = $"<BitToggleButton @bind-IsChecked='ToggleButtonForOnChange' Label='@(ToggleButtonForOnChange ? 'Mute' : 'Unmute')' IconName='@(ToggleButtonForOnChange ? 'MicOff' : 'Microphone')'OnChange='ToggleButtonChanged'></BitToggleButton>{Environment.NewLine}" +
             $"<span>Check status is: @OnToggleButtonChanged</span>{Environment.NewLine}" +
             $"@code {{ {Environment.NewLine}" +
             $"private bool ToggleButtonForOnChange = true; {Environment.NewLine}" +
             $"private bool OnToggleButtonChanged = true; {Environment.NewLine}" +
             "}";

        private readonly string twoWayBindingSampleCode = $"<input type=checkbox id='BitTogleButtonTwoWayValue' @bind='TogleButtonTwoWayValue'>{Environment.NewLine}" +
             $"<label for='BitTogleButtonTwoWayValue'>Checked Toggle Button</label>{Environment.NewLine}" +
             $"<BitToggleButton @bind-IsChecked='TogleButtonTwoWayValue' Label='@(TogleButtonTwoWayValue ? 'Mute' : 'Unmute')' IconName='@(TogleButtonTwoWayValue ? 'MicOff' : 'Microphone')'></BitToggleButton>{Environment.NewLine}" +
             $"<span>Check status is: @OnToggleButtonChanged</span> {Environment.NewLine}" +
             $"@code {{ {Environment.NewLine}" +
             $"private bool ToggleButtonTwoWayValue  = true; {Environment.NewLine}" +
             "}";

        private readonly string ariasSampleCode = $"<BitToggleButton @bind-IsChecked='ToggleButtonChecked' Label='@(ToggleButtonChecked ? 'Aria Description Mute' : 'Aria Description Unmute')' IconName='@(ToggleButtonChecked ? 'MicOff' : 'Microphone')' AriaDescription='Detailed description used for screen reader'></BitToggleButton>{Environment.NewLine}" +
             $"<BitToggleButton @bind-IsChecked='ToggleButtonChecked' Label='@(ToggleButtonChecked ? 'Aria Hidden Mute' : 'Aria Hidden Unmute')' IconName='@(ToggleButtonChecked ? 'MicOff' : 'Microphone')' AriaHidden='true'></BitToggleButton>{Environment.NewLine}" +
             $"@code {{ {Environment.NewLine}" +
             $"private bool ToggleButtonChecked = false; {Environment.NewLine}" +
             "}";

        private void ToggleButtonChanged(bool newValue)
        {
            OnToggleButtonChanged = newValue;
        }
    }
}
