using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Buttons
{
    public partial class BitIconButtonDemo
    {
        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "allowDisabledFocus",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the icon button can have focus in disabled mode.",
            },
            new ComponentParameter()
            {
                Name = "ariaDescription",
                Type = "string",
                DefaultValue = "",
                Description = "Detailed description of the icon button for the benefit of screen readers.",
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
                Name = "href",
                Type = "string",
                DefaultValue = "",
                Description = "URL the link points to, if provided, icon button renders as an anchor.",
            },
            new ComponentParameter()
            {
                Name = "iconName",
                Type = "string",
                DefaultValue = "",
                Description = "The icon name for the icon shown in the icon button.",
            },
            new ComponentParameter()
            {
                Name = "onClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the icon button clicked.",
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
                Description = "The title to show when the mouse is placed on the icon button.",
            },
        };
    }
}
