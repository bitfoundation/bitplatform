﻿using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Buttons
{
    public partial class BitIconButtonDemo
    {
        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "AllowDisabledFocus",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the icon button can have focus in disabled mode.",
            },
            new ComponentParameter()
            {
                Name = "AriaDescription",
                Type = "string",
                DefaultValue = "",
                Description = "Detailed description of the icon button for the benefit of screen readers.",
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
                Name = "Href",
                Type = "string",
                DefaultValue = "",
                Description = "URL the link points to, if provided, icon button renders as an anchor.",
            },
            new ComponentParameter()
            {
                Name = "IconName",
                Type = "BitIcon",
                DefaultValue = "",
                Description = "The icon name for the icon shown in the icon button.",
            },
            new ComponentParameter()
            {
                Name = "OnClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the icon button clicked.",
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
                Description = "The title to show when the mouse is placed on the icon button.",
            },
        };

        private readonly string example1HTMLCode = @"<BitIconButton IconName=""BitIconName.Emoji"" />
<BitIconButton IconName=""BitIconName.Emoji2"" Title=""I'm Happy"" />
<BitIconButton IconName=""BitIconName.EmojiDisappointed"" AllowDisabledFocus=""false"" IsEnabled=""false"" />";

        private readonly string example2HTMLCode = @"<BitIconButton IconName=""BitIconName.Home"" Style=""border-radius: 5px;padding: 23px;border: #D7D7D7 solid 2px;"" />
<BitIconButton IconName=""BitIconName.FileImage"" Class=""custom-icon-button"" />";

        private readonly string example3HTMLCode = @"<BitIconButton IconName=""BitIconName.List"" Visibility=""BitComponentVisibility.Visible"">Visible Button</BitIconButton>
<div><span>Hidden Button: </span>[<BitIconButton Visibility=""BitComponentVisibility.Hidden"">Hidden Button</BitIconButton>]</div>
<div><span>Collapsed Button: </span>[<BitIconButton Visibility=""BitComponentVisibility.Collapsed"">Collapsed Button</BitIconButton>]</div>";

        private readonly string example4HTMLCode = @"<BitIconButton IconName=""BitIconName.Library"" AriaDescription=""Detailed description used for screen reader."">
    Button with Aria Description
</BitIconButton>
<BitIconButton IconName=""BitIconName.Library"" AriaHidden=""true"">
    Button with Aria Hidden
</BitIconButton>";

        private readonly string example5HTMLCode = @"<BitIconButton IconName=""BitIconName.Website"" Target=""_blank"" Href=""https://github.com/bitfoundation/bitframework"">
    Open Bit Foundation In New Tab
</BitIconButton>
<BitIconButton IconName=""BitIconName.Website"" Href=""https://github.com/bitfoundation/bitframework"" ButtonStyle=""BitButtonStyle.Standard"">
    Go To Bit Foundation
</BitIconButton>
<BitIconButton IconName=""BitIconName.Website"" Target=""_self"" Href=""https://github.com/bitfoundation/bitframework"" IsEnabled=""false"">
    <span>Bit Foundation From Span</span>
</BitIconButton>";
    }
}
