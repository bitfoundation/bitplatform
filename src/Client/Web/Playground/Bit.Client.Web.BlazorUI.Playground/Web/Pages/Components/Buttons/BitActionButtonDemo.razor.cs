using System;
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

        private readonly string actionButtonSampleCode = $"<BitActionButton IconName='BitIcon.AddFriend'>Create account</BitActionButton>{Environment.NewLine}" +
             $"<BitActionButton IconName='BitIcon.UserRemove' AllowDisabledFocus='false' IsEnabled='false'>Remove user</BitActionButton>";

        private readonly string styleAndClassSampleCode = $"<BitActionButton IconName='BitIcon.ThisPC' Style='font-family:'Axiforma';font-size:16px;border:2px solid #32385B;justify-content: center;width: 232px;'>Styled Action Button</BitActionButton>{Environment.NewLine}" +
                 $"<BitActionButton IconName='BitIcon.ThisPC' Class='custom-action-button'>Classed Action Button</BitActionButton>{Environment.NewLine}" +
                 $"<style>{Environment.NewLine}" +
                 $".custom-action-button {{ {Environment.NewLine}" +
                 $"width:232px;{Environment.NewLine}" +
                 $"font-family:Axiforma;{Environment.NewLine}" +
                 $"font-size:16px;{Environment.NewLine}" +
                 $"background-color:#D7D7D7;{Environment.NewLine}" +
                 $"border-color:#D7D7D7;{Environment.NewLine}" +
                 $"justify-content:center;{Environment.NewLine}" +
                 $"}} {Environment.NewLine}" +
                 $"</style>";

        private readonly string visibilitySampleCode = $"<BitActionButton Visibility='ComponentVisibility.Visible' IconName='BitIcon.AddEvent'>Add Event</BitActionButton>{Environment.NewLine}" +
                 $"<BitActionButton Hidden='ComponentHidden.Hidden'>Hidden Action Button</BitActionButton>{Environment.NewLine}" +
                 $"<BitActionButton Visibility='ComponentVisibility.Collapsed'>Collapsed Action Button</BitActionButton>";

        private readonly string ariasSampleCode = $"<BitActionButton AriaDescription='Detailed description used for screen reader.'  IconName='BitIcon.Library'>Action Button with Aria Description</BitActionButton>{Environment.NewLine}" +
                 $"<BitActionButton AriaHidden='true' IconName='BitIcon.Library'>Action Button with Aria Hidden</BitActionButton>{Environment.NewLine}";

        private readonly string actionButtonLikeAnchorSampleCode = $"<BitActionButton IconName='BitIcon.Website' Target='_blank' Href='https://github.com/bitfoundation/bitframework'>Open Bit Foundation In New Tab</BitActionButton>{Environment.NewLine}" +
                 $"<BitActionButton IconName='BitIcon.Website' ButtonStyle='ButtonStyle.Standard' Href='https://github.com/bitfoundation/bitframework'>Go To Bit Foundation</BitActionButton>{Environment.NewLine}" +
                 $"<BitActionButton Target='_self' IconName='BitIcon.Website' Href='https://github.com/bitfoundation/bitframework' IsEnabled='false'<span>Bit Foundation From Span</span></BitActionButton>";
    }
}
