using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Buttons
{
    public partial class BitButtonDemo
    {
        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "allowDisabledFocus",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the button can have focus in disabled mode.",
            },
            new ComponentParameter()
            {
                Name = "ariaDescription",
                Type = "string",
                DefaultValue = "",
                Description = "Detailed description of the button for the benefit of screen readers.",
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
                Description = "The style of button, Possible values: Primary | Standard",
            },
            new ComponentParameter()
            {
                Name = "childContent",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "The content of button, It can be Any custom tag or a text.",
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
                Description = "The title to show when the mouse is placed on the button.",
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
                        Value="primary = 0",
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
    }
}
