using System;
using System.Collections.Generic;
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

        private readonly string iconButtonSampleCode = $"<BitIconButton IconName='BitIcon.Emoji2'></BitIconButton>{Environment.NewLine}" +
              $"<BitIconButton IconName='BitIcon.Emoji2' AllowDisabledFocus='false' IsEnabled='false'></BitIconButton>{Environment.NewLine}" +
              $"<BitIconButton IconName='BitIcon.Emoji2' ToolTip='I'm Happy'></BitIconButton>";

        private readonly string styleAndClassSampleCode = $"<BitIconButton Style='color:red' IconName='BitIcon.Home'>Styled Button</BitIconButton>{Environment.NewLine}" +
                 $"<BitIconButton Class='custom-icon-button' IconName='BitIcon.FileImage'>Classed Button</BitIconButton>{Environment.NewLine}" +
                 $"<style>{Environment.NewLine}" +
                 $".custom-icon-button {{ {Environment.NewLine}" +
                 $"height:48px;{Environment.NewLine}" +
                 $"width:48px;{Environment.NewLine}" +
                 $"border-radius:5px;{Environment.NewLine}" +
                 $"background-color:#D7D7D7;{Environment.NewLine}" +
                 $"border-color:#D7D7D7;{Environment.NewLine}" +
                 $"}} {Environment.NewLine}" +
                 $"</style>";

        private readonly string visibilitySampleCode = $"<BitIconButton Visibility='ComponentVisibility.Visible' IconName='BitIcon.List'>Visible Button</BitIconButton>{Environment.NewLine}" +
                 $"<BitIconButton Visibility='ComponentHidden.Hidden' IconName='BitIcon.List'>Hidden Button</BitIconButton>{Environment.NewLine}" +
                 $"<BitIconButton Visibility='ComponentVisibility.Collapsed' IconName='BitIcon.List'>Collapsed Button</BitIconButton>";

        private readonly string areasSampleCode = $"<BitIconButton AriaDescription='Detailed description used for screen reader.' IconName='BitIcon.Library'>Icon Button with Aria Description</BitIconButton>{Environment.NewLine}" +
                $"<BitIconButton AriaHidden='true' IconName='BitIcon.Library'>Icon Button with Aria Hidden</BitIconButton>";

        private readonly string iconButtonLikeAnchorSampleCode = $"<BitIconButton IconName='BitIcon.Website' Target='_blank' Href='https://github.com/bitfoundation/bitframework'>Open Bit Foundation In New Tab</BitIconButton>{Environment.NewLine}" +
                 $"<BitIconButton IconName='BitIcon.Website' ButtonStyle='BitButtonStyle.Standard' Href='https://github.com/bitfoundation/bitframework'>Go To Bit Foundation</BitIconButton>{Environment.NewLine}" +
                 $"<BitIconButton Target='_self' IconName='BitIcon.Website' Href='https://github.com/bitfoundation/bitframework' IsEnabled='false'<span>Bit Foundation From Span</span></BitIconButton>";
    }
}
