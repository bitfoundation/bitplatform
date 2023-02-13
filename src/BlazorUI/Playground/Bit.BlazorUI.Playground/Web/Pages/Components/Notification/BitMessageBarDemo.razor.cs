using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Notification;

public partial class BitMessageBarDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "Actions",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "The content of the action to show on the message bar.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "The content of message bar.",
        },
        new()
        {
            Name = "DismissButtonAriaLabel",
            Type = "string",
            DefaultValue = "",
            Description = "Aria label on dismiss button if onDismiss is defined.",
        },
        new()
        {
            Name = "DismissIconName",
            Type = "BitIconName",
            DefaultValue = "BitIconName.Clear",
            Description = "Custom Fabric icon name to replace the dismiss icon. If unset, default will be the Fabric Clear icon.",
        },
        new()
        {
            Name = "IsMultiline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines if the message bar is multi lined. If false, and the text overflows over buttons or to another line, it is clipped.",
        },
        new()
        {
            Name = "MessageBarType",
            Type = "BitMessageBarType",
            LinkType = LinkType.Link,
            Href = "#messageBarType-enum",
            DefaultValue = "BitMessageBarType.Info",
            Description = "The type of message bar to render.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Whether the message bar has a dismiss button and its callback. If null, dismiss button won't show.",
        },
        new()
        {
            Name = "OverflowButtonAriaLabel",
            Type = "string",
            DefaultValue = "",
            Description = "Aria label on overflow button if truncated is true.",
        },
        new()
        {
            Name = "Role",
            Type = "string",
            DefaultValue = "Browse",
            Description = "Custom role to apply to the message bar.",
        },
        new()
        {
            Name = "Truncated",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines if the message bar text is truncated. If true, a button will render to toggle between a single line view and multiline view. This parameter is for single line message bars with no buttons only in a limited space scenario.",
        },
        new()
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
        new()
        {
            Id = "messageBarType-enum",
            Title = "BitMessageBarType Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new()
                {
                    Name = "Info",
                    Description = "Info styled MessageBar.",
                    Value = "0",
                },
                new()
                {
                    Name = "Warning",
                    Description = "Warning styled MessageBar.",
                    Value = "1",
                },
                new()
                {
                    Name = "Error",
                    Description = "Error styled MessageBar.",
                    Value = "2",
                },
                new()
                {
                    Name = "Blocked",
                    Description = "Blocked styled MessageBar.",
                    Value = "3",
                },
                new()
                {
                    Name = "SevereWarning",
                    Description = "SevereWarning styled MessageBar.",
                    Value = "4",
                },
                new()
                {
                    Name = "Success",
                    Description = "Success styled MessageBar.",
                    Value = "5",
                },
            }
        }
    };


    private bool IsMessageBarHidden1 = false;
    private bool IsMessageBarHidden2 = false;
    private bool IsMessageBarHidden3 = false;

    private void HideMessageBar1()
    {
        IsMessageBarHidden1 = true;
    }

    private void HideMessageBar2()
    {
        IsMessageBarHidden2 = true;
    }

    private void HideMessageBar3()
    {
        IsMessageBarHidden3 = true;
    }

    private void Reset()
    {
        IsMessageBarHidden1 = false;
        IsMessageBarHidden2 = false;
        IsMessageBarHidden3 = false;
    }


    private readonly string example1HTMLCode = @"
<div class=""example-box"">
    <BitMessageBar MessageBarType=""@BitMessageBarType.Info"">
        Info (default) MessageBar. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
    </BitMessageBar>
</div>
<div class=""example-box"">
    <BitMessageBar MessageBarType=""@BitMessageBarType.Error"">
        Error MessageBar. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
    </BitMessageBar>
</div>
<div class=""example-box"">
    <BitMessageBar MessageBarType=""@BitMessageBarType.Blocked"">
        Blocked MessageBar. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
    </BitMessageBar>
</div>
<div class=""example-box"">
    <BitMessageBar MessageBarType=""@BitMessageBarType.SevereWarning"">
        SevereWarning MessageBar. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
    </BitMessageBar>
</div>
<div class=""example-box"">
    <BitMessageBar MessageBarType=""@BitMessageBarType.Success"">
        Success MessageBar. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
    </BitMessageBar>
</div>
<div class=""example-box"">
    <BitMessageBar MessageBarType=""@BitMessageBarType.Warning"">
        Warning MessageBar. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
    </BitMessageBar>
</div>";

    private readonly string example2HTMLCode = @"
<style>
    .example-box {
        margin-bottom: 10px;
    }
</style>
<div class=""example-box"">
    @if (IsMessageBarHidden1 is false)
    {
        <BitMessageBar MessageBarType=""@BitMessageBarType.Info"" OnDismiss=""HideMessageBar1"">
            Dismiss option enabled by adding <strong>OnDismiss</strong> parameter.
        </BitMessageBar>
    }
</div>
<div class=""example-box"">
    @if (IsMessageBarHidden2 is false)
    {
        <BitMessageBar MessageBarType=""@BitMessageBarType.Error"" OnDismiss=""HideMessageBar2"" Truncated=""true"" IsMultiline=""false"">
            single line, with dismiss button and truncated text.
            Truncation is not available if you use action buttons or multiline and should be used sparingly.
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
        </BitMessageBar>
    }
</div>
<div class=""example-box"">
    <BitMessageBar MessageBarType=""@BitMessageBarType.Blocked"">
        <Actions>
            <div style=""width:100%;text-align:right"">
                <BitButton ButtonStyle=""BitButtonStyle.Standard"">Yes</BitButton>
                <BitButton ButtonStyle=""BitButtonStyle.Standard"">No</BitButton>
            </div>
        </Actions>
        <ChildContent>
            MessageBar with action buttons which defaults to multiline.
        </ChildContent>
    </BitMessageBar>
</div>
<div class=""example-box"">
    <BitMessageBar MessageBarType=""@BitMessageBarType.Success"" IsMultiline=""false"">
        <Actions>
            <div style=""display:flex;align-items:center"">
                <BitButton ButtonStyle=""BitButtonStyle.Standard"" Style=""padding:0"">Yes</BitButton>
                <BitButton ButtonStyle=""BitButtonStyle.Standard"" Style=""padding:0"">No</BitButton>
            </div>
        </Actions>
        <ChildContent>
            MessageBar with single line and action buttons.MessageBar with single line and action buttons.
        </ChildContent>
    </BitMessageBar>
</div>
<div class=""example-box"">
    @if (IsMessageBarHidden3 is false)
    {
        <BitMessageBar MessageBarType=""@BitMessageBarType.Warning"" OnDismiss=""HideMessageBar3"">
            <ChildContent>
                MessageBar defaults to multiline.
                Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
            </ChildContent>
            <Actions>
                <div style=""width:100%;text-align:right"">
                    <BitButton ButtonStyle=""BitButtonStyle.Standard"" Style=""padding:0"">Yes</BitButton>
                    <BitButton ButtonStyle=""BitButtonStyle.Standard"" Style=""padding:0"">No</BitButton>
                </div>
            </Actions>
        </BitMessageBar>
    }
</div>";
    private readonly string example2CSharpCode = @"
private bool IsMessageBarHidden1 = false;
private bool IsMessageBarHidden2 = false;
private bool IsMessageBarHidden3 = false;

private void HideMessageBar1()
{
    IsMessageBarHidden1 = true;
}

private void HideMessageBar2()
{
    IsMessageBarHidden2 = true;
}

private void HideMessageBar3()
{
    IsMessageBarHidden3 = true;
}";
}
