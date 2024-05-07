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
            Name = "Classes",
            Type = "BitMessageClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitMessage.",
            LinkType = LinkType.Link,
            Href = "#message-class-styles",
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
            DefaultValue = "BitSeverity.Info",
            Description = "The severity of the message. defaults to Info.",
            LinkType = LinkType.Link,
            Href = "#severity-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitMessageClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitMessage.",
            LinkType = LinkType.Link,
            Href = "#message-class-styles",
        },
        new()
        {
            Name = "Truncate",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines if the message text is truncated. If true, a button will render to toggle between a single line view and multiline view. This parameter is for single line messages with no buttons only in a limited space scenario.",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant",
            DefaultValue = "BitVariant.Fill",
            Description = "The variant of the message. defaults to Fill.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "severity-enum",
            Name = "BitSeverity",
            Description = "Determines the severity of the content that controls the colors of the rendered element(s).",
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
        },
        new()
        {
            Id = "variant-enum",
            Name = "BitVariant",
            Description = "Determines the variant of the content that controls the rendered style of the corresponding element(s).",
            Items =
            [
                new()
                {
                    Name = "Fill",
                    Description = "Fill styled variant.",
                    Value = "0",
                },
                new()
                {
                    Name = "Outline",
                    Description = "Outline styled variant.",
                    Value = "1",
                },
                new()
                {
                    Name = "Text",
                    Description = "Text styled variant.",
                    Value = "2",
                },
            ]
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "message-class-styles",
            Title = "BitMessageClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitMessage."
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the main container of the BitMessage."
                },
                new()
                {
                    Name = "IconContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon container of the BitMessage."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon element of the BitMessage."
                },
                new()
                {
                    Name = "ContentContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content container of the BitMessage."
                },
                new()
                {
                    Name = "ContentWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content wrapper element of the BitMessage."
                },
                new()
                {
                    Name = "Content",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content element of the BitMessage."
                },
                new()
                {
                    Name = "Actions",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the actions element of the BitMessage."
                },
                new()
                {
                    Name = "ExpanderButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the truncate expander button of the BitMessage."
                },
                new()
                {
                    Name = "ExpanderIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the truncate expander icon of the BitMessage."
                },
                new()
                {
                    Name = "DismissButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the truncate dismiss button of the BitMessage."
                },
                new()
                {
                    Name = "DismissIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the truncate dismiss icon of the BitMessage."
                },
            ]
        }
    ];



    private bool isDismissed;
    private bool isWarningDismissed;
    private bool isErrorDismissed;



    private readonly string example1RazorCode = @"
<BitMessage>
    This is a Message. <BitLink Href=""https://bitplatform.dev"">Visit our website.</BitLink>
</BitMessage>";

    private readonly string example2RazorCode = @"
<BitMessage Severity=""BitSeverity.Info"">Info (default).</BitMessage>
<BitMessage Severity=""BitSeverity.Success"">Success.</BitMessage>
<BitMessage Severity=""BitSeverity.Warning"">Warning.</BitMessage>
<BitMessage Severity=""BitSeverity.SevereWarning"">SevereWarning.</BitMessage>
<BitMessage Severity=""BitSeverity.Error"">Error.</BitMessage>";

    private readonly string example3RazorCode = @"
<BitMessage Severity=""BitSeverity.Info"" Variant=""BitVariant.Fill"">Info.</BitMessage>
<BitMessage Severity=""BitSeverity.Success"" Variant=""BitVariant.Fill"">Success.</BitMessage>
<BitMessage Severity=""BitSeverity.Warning"" Variant=""BitVariant.Fill"">Warning.</BitMessage>
<BitMessage Severity=""BitSeverity.SevereWarning"" Variant=""BitVariant.Fill"">SevereWarning.</BitMessage>
<BitMessage Severity=""BitSeverity.Error"" Variant=""BitVariant.Fill"">Error.</BitMessage>

<BitMessage Severity=""BitSeverity.Info"" Variant=""BitVariant.Outline"">Info.</BitMessage>
<BitMessage Severity=""BitSeverity.Success"" Variant=""BitVariant.Outline"">Success.</BitMessage>
<BitMessage Severity=""BitSeverity.Warning"" Variant=""BitVariant.Outline"">Warning.</BitMessage>
<BitMessage Severity=""BitSeverity.SevereWarning"" Variant=""BitVariant.Outline"">SevereWarning.</BitMessage>
<BitMessage Severity=""BitSeverity.Error"" Variant=""BitVariant.Outline"">Error.</BitMessage>

<BitMessage Severity=""BitSeverity.Info"" Variant=""BitVariant.Text"">Info.</BitMessage>
<BitMessage Severity=""BitSeverity.Success"" Variant=""BitVariant.Text"">Success.</BitMessage>
<BitMessage Severity=""BitSeverity.Warning"" Variant=""BitVariant.Text"">Warning.</BitMessage>
<BitMessage Severity=""BitSeverity.SevereWarning"" Variant=""BitVariant.Text"">SevereWarning.</BitMessage>
<BitMessage Severity=""BitSeverity.Error"" Variant=""BitVariant.Text"">Error.</BitMessage>";

    private readonly string example4RazorCode = @"
<BitMessage Multiline Severity=""BitSeverity.Success"">
    <b>Multiline</b> parameter makes the content to be rendered in multiple lines.
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
</BitMessage>";

    private readonly string example5RazorCode = @"
<BitMessage Truncate Severity=""BitSeverity.Warning"">
    <b>Truncate</b> parameter cut the overflowed content at the end of the single line Message.
    Truncation is not available if you use multiline and should be used sparingly.
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
</BitMessage>";

    private readonly string example6RazorCode = @"
<BitMessage OnDismiss=""() => isDismissed = true"" Severity=""BitSeverity.SevereWarning"">
    Dismiss option enabled by adding <strong>OnDismiss</strong> parameter.
</BitMessage>";
    private readonly string example6CsharpCode = @"
private bool isDismissed;";

    private readonly string example7RazorCode = @"
<BitMessage>
    <Actions>
        <BitIconButton IconName=""@BitIconName.TriangleSolidUp12"" />
        &nbsp;
        <BitIconButton IconName=""@BitIconName.TriangleSolidDown12"" />
    </Actions>
    <Content>
        Message with single line and action buttons.
    </Content>
</BitMessage>";

    private readonly string example8RazorCode = @"
<BitMessage Severity=""BitSeverity.Info"" HideIcon>Info (default) Message.</BitMessage>
<BitMessage Severity=""BitSeverity.Success"" HideIcon>Success Message.</BitMessage>
<BitMessage Severity=""BitSeverity.Warning"" HideIcon>Warning Message.</BitMessage>
<BitMessage Severity=""BitSeverity.SevereWarning"" HideIcon>SevereWarning Message.</BitMessage>
<BitMessage Severity=""BitSeverity.Error"" HideIcon>Error Message.</BitMessage>";

    private readonly string example9RazorCode = @"
<style>
    .custom-class {
        padding: 1rem;
        color: deeppink;
        font-size: 16px;
        font-style: italic;
    }

    .custom-icon {
        font-size: 2rem;
    }

    .custom-content {
        font-size: 1.5rem;
    }

    .custom-expander-icon {
        margin: 0.5rem;
        font-size: 2rem;
    }

    .custom-dismiss-icon {
        margin: 0.5rem;
        font-size: 2rem;
    }
</style>

<BitMessage Severity=""BitSeverity.Info"" Multiline OnDismiss=""() => {}""
            Style=""padding:8px;color:red;"">
    <b>Styled Message.</b>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
</BitMessage>

<BitMessage Severity=""BitSeverity.Success"" Truncate Class=""custom-class"">
    <b>Classed Message.</b>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
</BitMessage>


<BitMessage Severity=""BitSeverity.Warning"" OnDismiss=""() => {}"" Multiline
            Styles=""@(new() { Root=""padding:1rem"",
                              IconContainer=""line-height:1.25"",
                              Content=""color:pink"",
                              ContentContainer=""margin:0 10px"",
                              DismissIcon=""font-size:1rem"",
                              Actions=""justify-content:center;gap:1rem"" })"">
    <Content>
        <b>Styles.</b>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
    </Content>
    <Actions>
        <BitButton ButtonStyle=""BitButtonStyle.Text"">Ok</BitButton>
        <BitButton ButtonStyle=""BitButtonStyle.Text"">Cancel</BitButton>
    </Actions>
</BitMessage>

<BitMessage Severity=""BitSeverity.SevereWarning"" OnDismiss=""() => {}"" Truncate
            Classes=""@(new() { Icon=""custom-icon"",
                               Content=""custom-content"",
                               ExpanderIcon=""custom-expander-icon"",
                               DismissIcon=""custom-dismiss-icon"" })"">
    <b>Classes.</b>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
</BitMessage>";

    private readonly string example10RazorCode = @"
<BitMessage Truncate OnDismiss=""() => isWarningDismissed = true"" Severity=""BitSeverity.Warning"">
    <Content>
        <b>Truncate</b> with <b>OnDismiss</b> and <b>Actions</b>.
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
    </Content>
    <Actions>
        <div style=""display:flex;align-items:center;gap:4px"">
            <button>Yes</button>
            <button>No</button>
        </div>
    </Actions>
</BitMessage>

<BitMessage Multiline OnDismiss=""() => isErrorDismissed = true"" Severity=""BitSeverity.Error"">
    <Content>
        <b>Multiline</b> with <b>OnDismiss</b> and <b>Actions</b>.
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
    </Content>
    <Actions>
        <BitButton ButtonStyle=""BitButtonStyle.Standard"">Yes</BitButton>
        &nbsp;
        <BitButton ButtonStyle=""BitButtonStyle.Standard"">No</BitButton>
    </Actions>
</BitMessage>";
    private readonly string example10CsharpCode = @"
private bool isWarningDismissed;
private bool isErrorDismissed;";

    private readonly string example11RazorCode = @"
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

    private readonly string example12RazorCode = @"
<BitMessage Dir=""BitDir.Rtl"" Severity=""BitSeverity.Info"">
    پیام خبری (پیش فرض). <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Severity=""BitSeverity.Success"" Truncate OnDismiss=""() => {}"">
    پیام موفق. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Severity=""BitSeverity.Warning"" Multiline OnDismiss=""() => {}"">
    پیام هشدار. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi luctus, purus a lobortis tristique, odio augue pharetra metus, ac placerat nunc mi nec dui. Vestibulum aliquam et nunc semper scelerisque. Curabitur vitae orci nec quam condimentum porttitor et sed lacus. Vivamus ac efficitur leo. Cras faucibus mauris libero, ac placerat erat euismod et. Donec pulvinar commodo odio sit amet faucibus. In hac habitasse platea dictumst. Duis eu ante commodo, condimentum nibh pellentesque, laoreet enim. Fusce massa lorem, ultrices eu mi a, fermentum suscipit magna. Integer porta purus pulvinar, hendrerit felis eget, condimentum mauris.
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Severity=""BitSeverity.SevereWarning"">
    پیام هشدار شدید. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessage>

<BitMessage Dir=""BitDir.Rtl"" Severity=""BitSeverity.Error"">
    پیام خطا. <BitLink Href=""https://bitplatform.dev"">به وبسایت ما سر بزنید.</BitLink>
</BitMessage>";
}
