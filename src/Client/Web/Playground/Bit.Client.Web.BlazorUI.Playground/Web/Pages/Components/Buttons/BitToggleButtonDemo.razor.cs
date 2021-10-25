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
                Name = "allowDisabledFocus",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the toggle button can have focus in disabled mode.",
            },
            new ComponentParameter()
            {
                Name = "ariaDescription",
                Type = "string",
                DefaultValue = "",
                Description = "Detailed description of the toggle button for the benefit of screen readers.",
            },
            new ComponentParameter()
            {
                Name = "ariaHidden",
                Type = "bool",
                DefaultValue = "false",
                Description = "If true, add an aria-hidden attribute instructing screen readers to ignore the element.",
            },
            new ComponentParameter()
            {
                Name = "buttonStyle",
                Type = "ButtonStyle",
                LinkType = LinkType.Link,
                Href = "#button-style-enum",
                DefaultValue = "ButtonStyle.primary",
                Description = "The style of toggle button, Possible values: Primary | Standard",
            },
            new ComponentParameter()
            {
                Name = "href",
                Type = "string",
                DefaultValue = "",
                Description = "URL the link points to, if provided, button renders as an anchor.",
            },
            new ComponentParameter()
            {
                Name = "iconName",
                Type = "string",
                DefaultValue = "",
                Description = "The icon that shows in the toggle button.",
            },
            new ComponentParameter()
            {
                Name = "isChecked",
                Type = "bool",
                DefaultValue = "",
                Description = "Determine if the toggle button is in checked state, default is true.",
            },
            new ComponentParameter()
            {
                Name = "label",
                Type = "string",
                DefaultValue = "",
                Description = "The text that shows in the label.",
            },
            new ComponentParameter()
            {
                Name = "onChange",
                Type = "EventCallback<bool>",
                DefaultValue = "",
                Description = "Callback that is called when the IsChecked value has changed.",
            },
            new ComponentParameter()
            {
                Name = "onClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the button clicked.",
            },
            new ComponentParameter()
            {
                Name = "target",
                Type = "string",
                DefaultValue = "",
                Description = "If Href provided, specifies how to open the link.",
            },
            new ComponentParameter()
            {
                Name = "title",
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
                        Name= "primary",
                        Description="",
                        Value="primary = 1",
                    },
                    new EnumItem()
                    {
                        Name= "standard",
                        Description="",
                        Value="standard = 1",
                    }
                }
            }
        };

        private void ToggleButtonChanged(bool newValue)
        {
            OnToggleButtonChanged = newValue;
        }
    }
}
