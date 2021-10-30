using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Notifications
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
                Type = "string",
                DefaultValue = "Clear",
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
                        Value="Info = 0",
                    },
                    new EnumItem()
                    {
                        Name= "Warning",
                        Description="Warning styled MessageBar.",
                        Value="Warning = 1",
                    },
                    new EnumItem()
                    {
                        Name= "Error",
                        Description="Error styled MessageBar.",
                        Value="Error = 2",
                    },
                    new EnumItem()
                    {
                        Name= "Blocked",
                        Description="Blocked styled MessageBar.",
                        Value="Blocked = 3",
                    },
                    new EnumItem()
                    {
                        Name= "SevereWarning",
                        Description="SevereWarning styled MessageBar.",
                        Value="SevereWarning = 4",
                    },
                    new EnumItem()
                    {
                        Name= "Success",
                        Description="Success styled MessageBar.",
                        Value="Success = 5",
                    },
                }
            }
        };
    }
}
