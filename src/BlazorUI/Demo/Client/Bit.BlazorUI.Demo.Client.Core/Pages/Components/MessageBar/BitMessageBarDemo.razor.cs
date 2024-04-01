namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.MessageBar;

public partial class BitMessageBarDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "Actions",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the action to show on the message bar.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of message bar.",
        },
        new()
        {
            Name = "DismissButtonAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "Aria label on dismiss button if onDismiss is defined.",
        },
        new()
        {
            Name = "IsMultiline",
            Type = "bool",
            DefaultValue = "true",
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
            Name = "DismissIconName",
            Type = "string",
            DefaultValue = "Clear",
            Description = "Custom Fabric icon name to replace the dismiss icon. If unset, default will be the Fabric Clear icon.",
        },
        new()
        {
            Name = "MessageBarIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom icon to replace the message bar icon. If unset, default will be the icon set by messageBarType.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback",
            Description = "Whether the message bar has a dismiss button and its callback. If null, dismiss button won't show.",
        },
        new()
        {
            Name = "OverflowButtonAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "Aria label on overflow button if truncated is true.",
        },
        new()
        {
            Name = "Role",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom role to apply to the message bar.",
        },
        new()
        {
            Name = "Truncated",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines if the message bar text is truncated. If true, a button will render to toggle between a single line view and multiline view. This parameter is for single line message bars with no buttons only in a limited space scenario.",
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "messageBarType-enum",
            Name = "BitMessageBarType",
            Description = "",
            Items = new()
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



    private readonly string example1RazorCode = @"
<BitMessageBar MessageBarType=""@BitMessageBarType.Info"">
    Info (default) MessageBar. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
</BitMessageBar>

<BitMessageBar MessageBarType=""@BitMessageBarType.Success"">
    Success MessageBar. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
</BitMessageBar>

<BitMessageBar MessageBarType=""@BitMessageBarType.Warning"">
    Warning MessageBar. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
</BitMessageBar>

<BitMessageBar MessageBarType=""@BitMessageBarType.SevereWarning"">
    SevereWarning MessageBar. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
</BitMessageBar>

<BitMessageBar MessageBarType=""@BitMessageBarType.Error"">
    Error MessageBar. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
</BitMessageBar>

<BitMessageBar MessageBarType=""@BitMessageBarType.Blocked"">
    Blocked MessageBar. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
</BitMessageBar>";

    private readonly string example2RazorCode = @"
@if (IsInfoMessageBarHidden is false)
{
    <BitMessageBar MessageBarType=""@BitMessageBarType.Info"" OnDismiss=""() => IsInfoMessageBarHidden = true"">
        Dismiss option enabled by adding <strong>OnDismiss</strong> parameter.
    </BitMessageBar>
}

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

@if (IsWarningMessageBarHidden is false)
{
    <BitMessageBar MessageBarType=""@BitMessageBarType.Warning"" OnDismiss=""() => IsWarningMessageBarHidden = true"">
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

@if (IsErrorMessageBarHidden is false)
{
    <BitMessageBar MessageBarType=""@BitMessageBarType.Error"" Truncated=""true"" IsMultiline=""false"" OnDismiss=""() => IsErrorMessageBarHidden = true"">
        single line, with dismiss button and truncated text.
        Truncation is not available if you use action buttons or multiline and should be used sparingly.
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
    </BitMessageBar>
}
 
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
</BitMessageBar>";
    private readonly string example2CsharpCode = @"
private bool IsInfoMessageBarHidden = false;
private bool IsWarningMessageBarHidden = false;
private bool IsErrorMessageBarHidden = false;";

    private readonly string example3RazorCode = @"
<BitMessageBar Dir=""BitDir.Rtl"" MessageBarType=""@BitMessageBarType.Info"">
    اطلاعات (پیش فرض) نوار پیام. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessageBar>

<BitMessageBar Dir=""BitDir.Rtl"" MessageBarType=""@BitMessageBarType.Success"">
    نوار پیام موفق. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessageBar>

<BitMessageBar Dir=""BitDir.Rtl"" MessageBarType=""@BitMessageBarType.Warning"">
    نوار پیام هشدار. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessageBar>

<BitMessageBar Dir=""BitDir.Rtl"" MessageBarType=""@BitMessageBarType.SevereWarning"">
    نوار پیام هشدار شدید. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessageBar>

<BitMessageBar Dir=""BitDir.Rtl"" MessageBarType=""@BitMessageBarType.Error"">
    نوار پیام خطا. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessageBar>

<BitMessageBar Dir=""BitDir.Rtl"" MessageBarType=""@BitMessageBarType.Blocked"">
    نوار پیام مسدود. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessageBar>";
}
