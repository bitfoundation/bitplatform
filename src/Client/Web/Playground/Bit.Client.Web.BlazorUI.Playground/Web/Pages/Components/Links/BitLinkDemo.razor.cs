using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Links
{
    public partial class BitLinkDemo
    {
        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "childContent",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "The content of link, can be any custom tag or a text.",
            },
            new ComponentParameter()
            {
                Name = "hasUnderline",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the link is styled with an underline or not. Should be used when the link is placed alongside other text content.",
            },
            new ComponentParameter()
            {
                Name = "href",
                Type = "string",
                DefaultValue = "",
                Description = "URL the link points to.",
            },
            new ComponentParameter()
            {
                Name = "onClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the action button clicked.",
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
                Description = "The title to show when the mouse is placed on the action button.",
            },
        };
    }
}
