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
                Type = "string",
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

        private readonly string iconButtonSampleCode = $"<BitIconButton IconName='Emoji2'></BitIconButton>{Environment.NewLine}" +
              $"<BitIconButton IconName='Emoji2' AllowDisabledFocus='false' IsEnabled='false'></BitIconButton>{Environment.NewLine}" +
              $"<BitIconButton IconName='Emoji2' ToolTip='I'm Happy'></BitIconButton>{Environment.NewLine}";

        private readonly string styleAndClassSampleCode = $"<BitIconButton Style='color:red' IconName='Home'>Styled Button</BitIconButton>{Environment.NewLine}" +
                 $"<BitIconButton Class='custom-button' IconName='FileImage'>Classed Button</BitIconButton>{Environment.NewLine}" +
                 $"<style>{Environment.NewLine}" +
                 $".custom-button {{ {Environment.NewLine}" +
                 $"height:40px;{Environment.NewLine}" +
                 $"width:166px;{Environment.NewLine}" +
                 $"font-family:Axiforma;{Environment.NewLine}" +
                 $" font-size:16px;{Environment.NewLine}" +
                 $"background-color:#2F455A;{Environment.NewLine}" +
                 $"border-color:#2F455A;{Environment.NewLine}" +
                 $"}} {Environment.NewLine}" +
                 $"</style>{Environment.NewLine}";

        private readonly string visibilitySampleCode = $"<BitIconButton Visibility='ComponentVisibility.Visible' IconName='List'>Visible Button</BitIconButton>{Environment.NewLine}" +
                 $"<BitIconButton Visibility='ComponentHidden.Hidden' IconName='List'>Hidden Button</BitIconButton>{Environment.NewLine}" +
                 $"<BitIconButton Visibility='ComponentVisibility.Collapsed' IconName='List'>Collapsed Button</BitIconButton>";

        private readonly string areasSampleCode = $"<BitIconButton AriaDescription='Detailed description used for screen reader.' IconName='PowerButton'>Icon Button with Aria Description</BitIconButton>{Environment.NewLine}" +
                $"<BitIconButton AriaHidden='true'  IconName='PowerButton'>Icon Button with Aria Hidden</BitIconButton>{Environment.NewLine}";

        private readonly string iconButtonLikeAnchorSampleCode = $"<BitIconButton IconName='Website' Target='_blank' Href='https://github.com/bitfoundation/bitframework'>Open Bit Foundation In New Tab</BitIconButton>{Environment.NewLine}" +
                 $"<BitIconButton IconName='Website' ButtonStyle='ButtonStyle.Standard' Href='https://github.com/bitfoundation/bitframework'>Go To Bit Foundation</BitIconButton>{Environment.NewLine}" +
                 $"<BitIconButton Target='_self' IconName='Website' Href='https://github.com/bitfoundation/bitframework' IsEnabled='false'<span>Bit Foundation From Span</span></BitIconButton>";
    }
}
