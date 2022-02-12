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
                Type = "BitIconName",
                DefaultValue = "BitIconName.Clear",
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
            },
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

        private readonly string example1HTMLCode = @"<div>
    <BitMessageBar MessageBarType=""@BitMessageBarType.Success"" IsMultiline=""false"" Truncated=""true"" IsEnabled=""false"">
        <Actions>
            <BitButton>Ok</BitButton>
        </Actions>
        <ChildContent>
            Action completed! This is a sample of message bar with dismiss ability
            Action completed! This is a sample of message bar with dismiss ability
            Action completed! This is a sample of message bar with dismiss ability
            Action completed! This is a sample of message bar with dismiss ability
            Action completed! This is a sample of message bar with dismiss ability
        </ChildContent>
    </BitMessageBar>
    @if (IsMessageBarHidden2 is false)
    {
        <div class=""m-b-10"">
            <BitMessageBar MessageBarType=""@BitMessageBarType.Success"" OnDismiss=""HideMessageBar2"" IsMultiline=""false"" DismissIconName=""BitIconName.SkypeCheck"" DismissButtonAriaLabel=""close"">
                Action completed! This is a sample of message bar with dismiss ability and custom dismiss icon
            </BitMessageBar>
        </div>
    }
    <BitMessageBar MessageBarType=""@BitMessageBarType.Success"" MessageBarIconName=""BitIconName.Emoji2"">
        Action completed! This is a sample of message bar with dismiss ability and custom dismiss icon
    </BitMessageBar>
</div>
@if (IsMessageBarHidden1 is false)
{
    <div class=""m-b-10"">
        <BitMessageBar MessageBarType=""@BitMessageBarType.Success"" OnDismiss=""HideMessageBar1"" IsMultiline=""false"" Truncated=""true"" OverflowButtonAriaLabel=""see more"">
            Action completed! This is a sample of message bar with dismiss ability
            Action completed! This is a sample of message bar with dismiss ability
            Action completed! This is a sample of message bar with dismiss ability
            Action completed! This is a sample of message bar with dismiss ability
            Action completed! This is a sample of message bar with dismiss ability
        </BitMessageBar>
    </div>
}";

        private readonly string example1CSharpCode = @"
@code {
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
}";

        private readonly string example2HTMLCode = @"<BitMessageBar MessageBarType=""@BitMessageBarType.Error"" Truncated=""false"">
    <ChildContent>
        This is an error message bar with the ability to truncate your text Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </ChildContent>
    <Actions>
        <BitButton>Ok</BitButton>
    </Actions>
</BitMessageBar>";

        private readonly string example3HTMLCode = @"<BitMessageBar MessageBarType=""@BitMessageBarType.Blocked"">
    Blocked MessageBar - single line Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
</BitMessageBar>";

        private readonly string example4HTMLCode = @"<div>
    <BitMessageBar MessageBarType=""@BitMessageBarType.Warning"" IsMultiline=""false"">
        Caution! Action may takes long and also this message bar shown multiline messages time Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
    </BitMessageBar>
</div>
<BitMessageBar MessageBarType=""@BitMessageBarType.SevereWarning"">
    Cannot connect to server
</BitMessageBar>";

        private readonly string example5HTMLCode = @"<BitMessageBar MessageBarType=""@BitMessageBarType.Info"">
    Visit repository <BitLink HasUnderline=""true"">the link is rendered as a button</BitLink>
</BitMessageBar>";
    }
}
