using System;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Notification
{
    public partial class BitMessageBarDemo
    {
        private bool IsMessageBarHidden1 = false;
        private bool IsMessageBarHidden2 = false;

        private void HideMessageBar1()
        {
            IsMessageBarHidden1 = true;
        }

        private void HideMessageBar2()
        {
            IsMessageBarHidden2 = true;
        }

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "Actions",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "The content of the action to show on the message bar.",
            },
            new ComponentParameter()
            {
                Name = "ChildContent",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "The content of message bar.",
            },
            new ComponentParameter()
            {
                Name = "DismissButtonAriaLabel",
                Type = "string",
                DefaultValue = "",
                Description = "Aria label on dismiss button if onDismiss is defined.",
            },
            new ComponentParameter()
            {
                Name = "DismissIconName",
                Type = "BitIcon",
                DefaultValue = "BitIcon.Clear",
                Description = "Custom Fabric icon name to replace the dismiss icon. If unset, default will be the Fabric Clear icon.",
            },
            new ComponentParameter()
            {
                Name = "IsMultiline",
                Type = "bool",
                DefaultValue = "false",
                Description = "Determines if the message bar is multi lined. If false, and the text overflows over buttons or to another line, it is clipped.",
            },
            new ComponentParameter()
            {
                Name = "MessageBarType",
                Type = "BitMessageBarType",
                LinkType = LinkType.Link,
                Href = "#messageBarType-enum",
                DefaultValue = "BitMessageBarType.Info",
                Description = "The type of message bar to render.",
            },
            new ComponentParameter()
            {
                Name = "OnDismiss",
                Type = "EventCallback",
                DefaultValue = "",
                Description = "Whether the message bar has a dismiss button and its callback. If null, dismiss button won't show.",
            },
            new ComponentParameter()
            {
                Name = "OverflowButtonAriaLabel",
                Type = "string",
                DefaultValue = "",
                Description = "Aria label on overflow button if truncated is true.",
            },
            new ComponentParameter()
            {
                Name = "Role",
                Type = "string",
                DefaultValue = "Browse",
                Description = "Custom role to apply to the message bar.",
            },
            new ComponentParameter()
            {
                Name = "Truncated",
                Type = "bool",
                DefaultValue = "false",
                Description = "Determines if the message bar text is truncated. If true, a button will render to toggle between a single line view and multiline view. This parameter is for single line message bars with no buttons only in a limited space scenario.",
            },
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "messageBarType-enum",
                Title = "BitMessageBarType Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "Info",
                        Description="Info styled MessageBar.",
                        Value="0",
                    },
                    new EnumItem()
                    {
                        Name= "Warning",
                        Description="Warning styled MessageBar.",
                        Value="1",
                    },
                    new EnumItem()
                    {
                        Name= "Error",
                        Description="Error styled MessageBar.",
                        Value="2",
                    },
                    new EnumItem()
                    {
                        Name= "Blocked",
                        Description="Blocked styled MessageBar.",
                        Value="3",
                    },
                    new EnumItem()
                    {
                        Name= "SevereWarning",
                        Description="SevereWarning styled MessageBar.",
                        Value="4",
                    },
                    new EnumItem()
                    {
                        Name= "Success",
                        Description="Success styled MessageBar.",
                        Value="5",
                    },
                }
            }
        };

        private readonly string successMessagebarSampleCode = $"@if (IsMessageBarHidden1 is false){Environment.NewLine}" +
              $"{{ {Environment.NewLine}" +
              $"<BitMessageBar MessageBarType='@itMessageBarType.Success' OnDismiss='HideMessageBar1' Truncated='true' IsMultiline='false' OverflowButtonAriaLabel='see more'>{Environment.NewLine}" +
              $"Action completed! This is a sample of message bar with dismiss ability{Environment.NewLine}" +
              $"Action completed! This is a sample of message bar with dismiss ability{Environment.NewLine}" +
              $"Action completed! This is a sample of message bar with dismiss ability{Environment.NewLine}" +
              $"Action completed! This is a sample of message bar with dismiss ability{Environment.NewLine}" +
              $"Action completed! This is a sample of message bar with dismiss ability{Environment.NewLine}" +
              $"</BitMessageBar>{Environment.NewLine}" +
              $"<BitMessageBar MessageBarType='@BitMessageBarType.Success' Truncated='true' IsMultiline='false'>{Environment.NewLine}" +
              $"<ChildContent> {Environment.NewLine}" +
              $"Action completed! This is a sample of message bar with dismiss ability{Environment.NewLine}" +
              $"Action completed! This is a sample of message bar with dismiss ability{Environment.NewLine}" +
              $"Action completed! This is a sample of message bar with dismiss ability{Environment.NewLine}" +
              $"Action completed! This is a sample of message bar with dismiss ability{Environment.NewLine}" +
              $"Action completed! This is a sample of message bar with dismiss ability{Environment.NewLine}" +
              $"</ChildContent>{Environment.NewLine}" +
              $"<Actions> {Environment.NewLine}" +
              $"<BitButton>ok</BitButton> {Environment.NewLine}" +
              $"</Actions> {Environment.NewLine}" +
              $"</BitMessageBar> {Environment.NewLine}" +
              $"@if  (IsMessageBarHidden2 is false) {Environment.NewLine}" +
              $"{{ {Environment.NewLine}" +
              $"<BitMessageBar MessageBarType='@BitMessageBarType.Success' {Environment.NewLine}" +
              $"OnDismiss='HideMessageBar2' {Environment.NewLine}" +
              $"IsMultiline='false' {Environment.NewLine}" +
              $"DismissIconName='BitIcon.SkypeCheck' {Environment.NewLine}" +
              $"DismissButtonAriaLabel='close'> {Environment.NewLine}" +
              $"Action completed! This is a sample of message bar with dismiss ability {Environment.NewLine}" +
              $"</BitMessageBar> {Environment.NewLine}" +
              $"}} {Environment.NewLine}" +
              $"<BitMessageBar MessageBarType='@BitMessageBarType.Success' {Environment.NewLine}" +
              $"MessageBarIconName='BitIcon.Emoji2'> {Environment.NewLine}" +
              $"Action completed! This is a sample of message bar with dismiss ability {Environment.NewLine}" +
              $"</BitMessageBar> {Environment.NewLine}" +
              $"@code {{ {Environment.NewLine}" +
              $"private bool IsMessageBarHidden1 = false; {Environment.NewLine}" +
              $"private bool IsMessageBarHidden2 = false; {Environment.NewLine}" +
              $"private void HideMessageBar1() {Environment.NewLine}" +
              $"{{ {Environment.NewLine}" +
              $"IsMessageBarHidden1 = true; {Environment.NewLine}" +
              $"}} {Environment.NewLine}" +
              $"private void HideMessageBar2() {Environment.NewLine}" +
              $"{{ {Environment.NewLine}" +
              $"IsMessageBarHidden2 = true; {Environment.NewLine}" +
              $"}} {Environment.NewLine}" +
              $"}}";

        private readonly string errorMessagebarSampleCode = $"<BitMessageBar MessageBarType='@BitMessageBarType.Error' Truncated='false'>{Environment.NewLine}" +
             $"<ChildContent> {Environment.NewLine}" +
             $"This is an error message bar with the ability to truncate your text Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut" +
             $"aliquip ex ea commodo consequat.Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.{Environment.NewLine}" +
             $"</ChildContent>{Environment.NewLine}" +
             $"<Actions> {Environment.NewLine}" +
             $"<BitButton>ok</BitButton> {Environment.NewLine}" +
             $"</Actions> {Environment.NewLine}" +
             $"</BitMessageBar>";

        private readonly string blockedMessagebarSampleCode = $"<BitMessageBar MessageBarType='@BitMessageBarType.Blocked'>{Environment.NewLine}" +
            $"Blocked MessageBar - single line Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo" +
            $"consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.{Environment.NewLine}" +
            $"</BitMessageBar>";

        private readonly string warningMessagebarSampleCode = $"<BitMessageBar MessageBarType='@BitMessageBarType.Warning' IsMultiline='false'>{Environment.NewLine}" +
             $"Caution! Action may takes long and also this message bar shown multiline messages time Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation" +
             $"ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.{Environment.NewLine}" +
             $"</BitMessageBar>" +
             $"<BitMessageBar MessageBarType='@BitMessageBarType.SevereWarning'> {Environment.NewLine}" +
             $"Cannot connect to the server {Environment.NewLine}" +
             $"</BitMessageBar>";

        private readonly string infoMessagebarSampleCode = $"<BitMessageBar MessageBarType='@BitMessageBarType.Info'>{Environment.NewLine}" +
          $"Visit repository{Environment.NewLine}" +
          $"<BitLink HasUnderline='true'>{Environment.NewLine}" +
          $"the link is rendered as a button{Environment.NewLine}" +
          $"</BitMessageBar>";
    }
}
