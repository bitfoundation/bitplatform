using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Link
{
    public partial class BitLinkDemo
    {
        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "ChildContent",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "The content of link, can be any custom tag or a text.",
            },
            new ComponentParameter()
            {
                Name = "HasUnderline",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the link is styled with an underline or not. Should be used when the link is placed alongside other text content.",
            },
            new ComponentParameter()
            {
                Name = "Href",
                Type = "string",
                DefaultValue = "",
                Description = "URL the link points to.",
            },
            new ComponentParameter()
            {
                Name = "OnClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the action button clicked.",
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
                Description = "The title to show when the mouse is placed on the action button.",
            },
            new ComponentParameter()
            {
                Name = "Visibility",
                Type = "BitComponentVisibility",
                LinkType = LinkType.Link,
                Href = "#component-visibility-enum",
                DefaultValue = "BitComponentVisibility.Visible",
                Description = "Whether the component is Visible,Hidden,Collapsed.",
            },
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "component-visibility-enum",
                Title = "BitComponentVisibility Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "Visible",
                        Description="Show content of the component.",
                        Value="0",
                    },
                    new EnumItem()
                    {
                        Name= "Hidden",
                        Description="Hide content of the component,though the space it takes on the page remains.",
                        Value="1",
                    },
                    new EnumItem()
                    {
                        Name= "Collapsed",
                        Description="Hide content of the component,though the space it takes on the page gone.",
                        Value="2",
                    }
                }
            }
        };

        private readonly string example1HTMLCode = @"<p>
    When a link has an href, <BitLink Href=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"" HasUnderline=""true"">it renders as an anchor tag</BitLink>. Without an href, <BitLink HasUnderline=""true"">the link is rendered as a button</BitLink>.
    You can also use the disabled attribute to create a <BitLink Href=""https://github.com/bitfoundation/bitplatform"" IsEnabled=""false"" HasUnderline=""true"">disabled link</BitLink>. It's not recommended to use Links with imgs because
    Links are meant to render textual inline content. Buttons are inline-block or in the case of imgs, block. However, it is possible to create a linked image button by making a button with an unstyled variant and adding the img content and href source to that.
</p>";
    }
}
