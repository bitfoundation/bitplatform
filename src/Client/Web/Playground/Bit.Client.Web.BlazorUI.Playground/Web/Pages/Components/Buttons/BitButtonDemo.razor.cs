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
                Name = "AllowDisabledFocus",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the button can have focus in disabled mode.",
            },
            new ComponentParameter()
            {
                Name = "AriaDescription",
                Type = "string",
                DefaultValue = "",
                Description = "Detailed description of the button for the benefit of screen readers.",
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
                DefaultValue = "BitButtonStyle.Primary",
                Description = "The style of button, Possible values: Primary | Standard.",
            },
            new ComponentParameter()
            {
                Name = "ChildContent",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "The content of button, It can be Any custom tag or a text.",
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
                Description = "The title to show when the mouse is placed on the button.",
            },
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "button-style-enum",
                Title = "BitButtonStyle Enum",
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

        private readonly string example1HTMLCode = @"<BitButton IsEnabled=""true"">
    Primary
</BitButton>
<BitButton ButtonStyle=""BitButtonStyle.Standard"" Style=""margin-right: 2px;"" IsEnabled=""true"">
    Standard
</BitButton>
<BitButton IsEnabled=""false"" AllowDisabledFocus=""false"">
    Disabled
</BitButton>
<BitButton Class=""label-btn"" IsEnabled=""true"">
    <label>A Text from label element</label>
</BitButton>";

        private readonly string example2HTMLCode = @"<BitButton Style=""height: 40px;width: 166px;font-family: 'Axiforma';font-size: 16px;"" Class=""styled-btn"">
    Styled Button
</BitButton>
<BitButton Class=""custom-button"">
    Classed Button
</BitButton>";

        private readonly string example3HTMLCode = @"<BitButton Style=""margin-bottom: 10px;"" Visibility=""BitComponentVisibility.Visible"">Visible Button</BitButton>
<div>Hidden Button: [<BitButton Visibility=""BitComponentVisibility.Hidden"">Hidden Button</BitButton>]</div>
<div>Collapsed Button: [<BitButton Visibility=""BitComponentVisibility.Collapsed"">Collapsed Button</BitButton>]</div>";

        private readonly string example4HTMLCode = @"<BitButton AriaDescription=""Detailed description used for screen reader."">
    Button with Aria Description
</BitButton>
<BitButton ButtonStyle=""BitButtonStyle.Standard"" AriaHidden=""true"">
    Button with Aria Hidden
</BitButton>";

        private readonly string example5HTMLCode = @"<BitButton Style=""margin-right: 10px;"" Title=""Primary"" Target=""_blank"" Href=""https://github.com/bitfoundation/bitframework"">
    Open Bit Foundation In New Tab
</BitButton>
<BitButton Class=""lnk-btn"" Style=""margin-right: 10px;"" Title=""Standard"" Href=""https://github.com/bitfoundation/bitframework"" ButtonStyle=""BitButtonStyle.Standard"">
    Go To Bit Foundation
</BitButton>
<BitButton Class=""disable-btn"" Target=""_self"" Href=""https://github.com/bitfoundation/bitframework"" IsEnabled=""false"">
    <span>Bit Foundation From Span</span>
</BitButton>";
    }
}
