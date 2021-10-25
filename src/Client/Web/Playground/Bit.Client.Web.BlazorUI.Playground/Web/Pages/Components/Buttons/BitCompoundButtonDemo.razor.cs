using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Buttons
{
    public partial class BitCompoundButtonDemo
    {
        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "allowDisabledFocus",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the compound button can have focus in disabled mode.",
            },
            new ComponentParameter()
            {
                Name = "ariaDescription",
                Type = "string",
                DefaultValue = "",
                Description = "Detailed description of the compound button for the benefit of screen readers.",
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
                Description = "The style of compound button, Possible values: Primary | Standard",
            },
            new ComponentParameter()
            {
                Name = "href",
                Type = "string",
                DefaultValue = "",
                Description = "URL the link points to, if provided, compound button renders as an anchor.",
            },
            new ComponentParameter()
            {
                Name = "onClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the compound button clicked.",
            },
            new ComponentParameter()
            {
                Name = "secondaryText",
                Type = "string",
                DefaultValue = "",
                Description = "Description of the action compound button takes.",
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
                Name = "text",
                Type = "string",
                DefaultValue = "",
                Description = "The text of compound button.",
            },
            new ComponentParameter()
            {
                Name = "title",
                Type = "string",
                DefaultValue = "",
                Description = "The title to show when the mouse is placed on the compound button.",
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
    }
}
