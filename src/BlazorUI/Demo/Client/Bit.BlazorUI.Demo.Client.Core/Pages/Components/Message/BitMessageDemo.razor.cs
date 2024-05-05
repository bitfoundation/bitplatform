namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Message;

public partial class BitMessageDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Actions",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the action to show on the message.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of message.",
        },
        new()
        {
            Name = "CollapseIconName",
            Type = "string",
            DefaultValue = "DoubleChevronUp",
            Description = "Custom Fabric icon name for the collapse icon in Truncate mode. If unset, default will be the Fabric DoubleChevronUp icon.",
        },
        new()
        {
            Name = "Content",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The alias for ChildContent.",
        },
        new()
        {
            Name = "DismissIconName",
            Type = "string",
            DefaultValue = "Cancel",
            Description = "Custom Fabric icon name to replace the dismiss icon. If unset, default will be the Fabric Cancel icon.",
        },
        new()
        {
            Name = "ExpandIconName",
            Type = "string",
            DefaultValue = "DoubleChevronDown",
            Description = "Custom Fabric icon name for the expand icon in Truncate mode. If unset, default will be the Fabric DoubleChevronDown icon.",
        },
        new()
        {
            Name = "HideIcon",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents rendering the icon of the message.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom icon to replace the message icon. If unset, default will be the icon set by Type.",
        },
        new()
        {
            Name = "Multiline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines if the message is multi-lined. If false, and the text overflows over buttons or to another line, it is clipped.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback",
            Description = "Whether the message has a dismiss button and its callback. If null, dismiss button won't show.",
        },
        new()
        {
            Name = "Role",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom role to apply to the message text.",
        },
        new()
        {
            Name = "Severity",
            Type = "BitSeverity",
            LinkType = LinkType.Link,
            Href = "#severity-enum",
            DefaultValue = "BitSeverity.Info",
            Description = "The severity of the message. defaults to Info.",
        },
        new()
        {
            Name = "Truncate",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines if the message text is truncated. If true, a button will render to toggle between a single line view and multiline view. This parameter is for single line messages with no buttons only in a limited space scenario.",
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "severity-enum",
            Name = "BitSeverity",
            Description = "Determines the severity of the content that controls the rendered style of the corresponding element(s).",
            Items =
            [
                new()
                {
                    Name = "Info",
                    Description = "Info styled severity.",
                    Value = "0",
                },
                new()
                {
                    Name = "Success",
                    Description = "Success styled severity.",
                    Value = "1",
                },
                new()
                {
                    Name = "Warning",
                    Description = "Warning styled severity.",
                    Value = "2",
                },
                new()
                {
                    Name = "SevereWarning",
                    Description = "SevereWarning styled severity.",
                    Value = "3",
                },
                new()
                {
                    Name = "Error",
                    Description = "Error styled severity.",
                    Value = "4",
                },
            ]
        }
    ];



    private bool isDismissed;
    private bool isWarningDismissed;
    private bool isErrorDismissed;



    private readonly string example1RazorCode = @"
<BitMessage Severity=""@BitSeverity.Info"">
    Info (default) Message. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
</BitMessage>

<BitMessage Severity=""@BitSeverity.Success"">
    Success Message. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
</BitMessage>

<BitMessage Severity=""@BitSeverity.Warning"">
    Warning Message. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
</BitMessage>

<BitMessage Severity=""@BitSeverity.SevereWarning"">
    SevereWarning Message. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
</BitMessage>

<BitMessage Severity=""@BitSeverity.Error"">
    Error Message. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
</BitMessage>";

    private readonly string example2RazorCode = @"
<BitMessage Multiline Severity=""BitSeverity.Success"">
    Multiline parameter makes the content to be rendered in multiple lines.
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
</BitMessage>";

    private readonly string example3RazorCode = @"
<BitMessage Truncate Severity=""BitSeverity.Warning"">
    Truncate parameter cut the overflowed content at the end of the single line Message.
    Truncation is not available if you use multiline and should be used sparingly.
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
</BitMessage>";

    private readonly string example4RazorCode = @"
<BitMessage OnDismiss=""() => isDismissed = true"" Severity=""BitSeverity.SevereWarning"">
    Dismiss option enabled by adding <strong>OnDismiss</strong> parameter.
</BitMessage>";
    private readonly string example4CsharpCode = @"
private bool isDismissed;";

    private readonly string example5RazorCode = @"
<BitMessage>
    <Actions>
        <BitButton ButtonStyle=""BitButtonStyle.Standard"">Yes</BitButton>
        <BitButton ButtonStyle=""BitButtonStyle.Standard"">No</BitButton>
    </Actions>
    <Content>
        Message with single line and action buttons.Message with single line and action buttons.
    </Content>
</BitMessage>";

    private readonly string example6RazorCode = @"
<BitMessage Severity=""@BitSeverity.Info"" HideIcon>
    HideIcon parameter removes the icon.
</BitMessage>

<BitMessage Severity=""@BitSeverity.Success"" HideIcon>
    Success Message. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
</BitMessage>

<BitMessage Severity=""@BitSeverity.Warning"" HideIcon>
    Warning Message. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
</BitMessage>

<BitMessage Severity=""@BitSeverity.SevereWarning"" HideIcon>
    SevereWarning Message. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
</BitMessage>

<BitMessage Severity=""@BitSeverity.Error"" HideIcon>
    Error Message. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
</BitMessage>";

    private readonly string example7RazorCode = @"
<BitMessage Truncate OnDismiss=""() => isWarningDismissed = true"" Severity=""@BitSeverity.Warning"">
    <Content>
        Truncate with OnDismiss and Actions.
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
    </Content>
    <Actions>
        <BitButton ButtonStyle=""BitButtonStyle.Standard"">Yes</BitButton>
        <BitButton ButtonStyle=""BitButtonStyle.Standard"">No</BitButton>
    </Actions>
</BitMessage>

<BitMessage Multiline OnDismiss=""() => isErrorDismissed = true"" Severity=""@BitSeverity.Error"">
    <Content>
        Multiline with OnDismiss and Actions.
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
    </Content>
    <Actions>
        <BitButton ButtonStyle=""BitButtonStyle.Standard"">Yes</BitButton>
        <BitButton ButtonStyle=""BitButtonStyle.Standard"">No</BitButton>
    </Actions>
</BitMessage>";
    private readonly string example7CsharpCode = @"
private bool isWarningDismissed;
private bool isErrorDismissed;";

    private readonly string example8RazorCode = @"
<BitMessage Severity=""BitSeverity.Success"" IconName=""@BitIconName.CheckMark"">
    Message with a custom icon.
</BitMessage>

<BitMessage Severity=""BitSeverity.Warning"" OnDismiss=""() => {}"" DismissIconName=""@BitIconName.Blocked2Solid"">
    Message with a custom dismiss icon.
</BitMessage>

<BitMessage Truncate Severity=""BitSeverity.Warning""
            ExpandIconName=""@BitIconName.ChevronDownEnd""
            CollapseIconName=""@BitIconName.ChevronUpEnd"">
    Message with custom expand and collapse icon.
    Truncation is not available if you use multiline and should be used sparingly.
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
</BitMessage>";

    private readonly string example9RazorCode = @"
<BitMessage Dir=""BitDir.Rtl"" Severity=""@BitSeverity.Info"">
    اطلاعات (پیش فرض) نوار پیام. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Severity=""@BitSeverity.Success"" Truncate OnDismiss=""() => {}"">
    نوار پیام موفق. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Severity=""@BitSeverity.Warning"" Multiline OnDismiss=""() => {}"">
    نوار پیام هشدار. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Severity=""@BitSeverity.SevereWarning"">
    نوار پیام هشدار شدید. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Severity=""@BitSeverity.Error"">
    نوار پیام خطا. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessage>";
}
