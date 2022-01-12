using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Buttons
{
    public partial class BitActionButtonDemo
    {
        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "AllowDisabledFocus",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the action button can have focus in disabled mode.",
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
                Name = "ChildContent",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "The content of action button, It can be Any custom tag or a text.",
            },
            new ComponentParameter()
            {
                Name = "Href",
                Type = "string",
                DefaultValue = "",
                Description = "URL the link points to, if provided, action button renders as an anchor.",
            },
            new ComponentParameter()
            {
                Name = "IconName",
                Type = "BitIcon",
                DefaultValue = "",
                Description = "The icon name for the icon shown in the action button.",
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
        };

        private readonly string example1HTMLCode = @"<BitActionButton IconName=""BitIconName.AddFriend"">Create account</BitActionButton>
<BitActionButton AllowDisabledFocus=""false"" IsEnabled=""false"" IconName=""BitIconName.UserRemove"">Remove user</BitActionButton>";

        private readonly string example2HTMLCode = @"<BitActionButton IconName=""BitIconName.ThisPC"" Style=""font-family:'Axiforma';font-size:16px;border:2px solid #32385B;justify-content: center;width: 232px;"">
    Styled Action Button
</BitActionButton>
<BitActionButton IconName=""BitIconName.ThisPC"" Class=""custom-action-button"">
    Classed Action Button
</BitActionButton>";

        private readonly string example3HTMLCode = @"<BitActionButton IconName=""BitIconName.AddEvent"" Visibility=""BitComponentVisibility.Visible"">Add Event</BitActionButton>
<div><span>Hidden Button: </span>[<BitActionButton Visibility=""BitComponentVisibility.Hidden"">Hidden Action Button</BitActionButton>]</div>
<div><span>Collapsed Button: </span>[<BitActionButton Visibility=""BitComponentVisibility.Collapsed"">Collapsed Action Button</BitActionButton>]</div>";

        private readonly string example4HTMLCode = @"<BitActionButton IconName=""BitIconName.Library"" AriaDescription=""Detailed description used for screen reader."">
    Action Button with Aria Description
</BitActionButton>
<BitActionButton IconName=""BitIconName.Library"" AriaHidden=""true"">
    Action Button with Aria Hidden
</BitActionButton>";

        private readonly string example5HTMLCode = @"<BitActionButton IconName=""BitIconName.Website"" Target=""_blank"" Href=""https://github.com/bitfoundation/bitframework"">
    Open Bit Foundation In New Tab
</BitActionButton>
<BitActionButton IconName=""BitIconName.Website"" Href=""https://github.com/bitfoundation/bitframework"" ButtonStyle=""BitButtonStyle.Standard"">
    Go To Bit Foundation
</BitActionButton>
<BitActionButton IconName=""BitIconName.Website"" Target=""_self"" Href=""https://github.com/bitfoundation/bitframework"" IsEnabled=""false"">
    <span>Bit Foundation From Span</span>
</BitActionButton>";
    }
}
