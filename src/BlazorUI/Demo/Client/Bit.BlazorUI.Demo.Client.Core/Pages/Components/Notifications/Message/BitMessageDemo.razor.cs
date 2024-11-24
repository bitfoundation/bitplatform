namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.Message;

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
            Name = "Alignment",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "Determines the alignment of the content section of the message.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
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
            Name = "CollapseIcon",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom Fabric icon name for the collapse icon in Truncate mode.",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the message.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
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
            Name = "DismissIcon",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom Fabric icon name to replace the dismiss icon. If unset, default will be the Fabric Cancel icon.",
        },
        new()
        {
            Name = "Elevation",
            Type = "int?",
            DefaultValue = "null",
            Description = "Determines the elevation of the message, a scale from 1 to 24.",
        },
        new()
        {
            Name = "ExpandIcon",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom Fabric icon name for the expand icon in Truncate mode.",
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
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of Message, Possible values: Small | Medium | Large.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
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
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The variant of the message.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name = "Primary",
                    Description = "Primary general color.",
                    Value = "0",
                },
                new()
                {
                    Name = "Secondary",
                    Description = "Secondary general color.",
                    Value = "1",
                },
                new()
                {
                    Name = "Tertiary",
                    Description = "Tertiary general color.",
                    Value = "2",
                },
                new()
                {
                    Name = "Info",
                    Description = "Info general color.",
                    Value = "3",
                },
                new()
                {
                    Name = "Success",
                    Description = "Success general color.",
                    Value = "4",
                },
                new()
                {
                    Name = "Warning",
                    Description = "Warning general color.",
                    Value = "5",
                },
                new()
                {
                    Name = "SevereWarning",
                    Description = "SevereWarning general color.",
                    Value = "6",
                },
                new()
                {
                    Name = "Error",
                    Description = "Error general color.",
                    Value = "7",
                },
                new()
                {
                    Name= "PrimaryBackground",
                    Description="Primary background color.",
                    Value="8",
                },
                new()
                {
                    Name= "SecondaryBackground",
                    Description="Secondary background color.",
                    Value="9",
                },
                new()
                {
                    Name= "TertiaryBackground",
                    Description="Tertiary background color.",
                    Value="10",
                },
                new()
                {
                    Name= "PrimaryForeground",
                    Description="Primary foreground color.",
                    Value="11",
                },
                new()
                {
                    Name= "SecondaryForeground",
                    Description="Secondary foreground color.",
                    Value="12",
                },
                new()
                {
                    Name= "TertiaryForeground",
                    Description="Tertiary foreground color.",
                    Value="13",
                },
                new()
                {
                    Name= "PrimaryBorder",
                    Description="Primary border color.",
                    Value="14",
                },
                new()
                {
                    Name= "SecondaryBorder",
                    Description="Secondary border color.",
                    Value="15",
                },
                new()
                {
                    Name= "TertiaryBorder",
                    Description="Tertiary border color.",
                    Value="16",
                }
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
        },
        new()
        {
            Id = "alignment-enum",
            Name = "BitAlignment",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Start",
                    Value = "0",
                },
                new()
                {
                    Name = "End",
                    Value = "1",
                },
                new()
                {
                    Name = "Center",
                    Value = "2",
                },
                new()
                {
                    Name = "SpaceBetween",
                    Value = "3",
                },
                new()
                {
                    Name = "SpaceAround",
                    Value = "4",
                },
                new()
                {
                    Name = "SpaceEvenly",
                    Value = "5",
                },
                new()
                {
                    Name = "Baseline",
                    Value = "6",
                },
                new()
                {
                    Name = "Stretch",
                    Value = "7",
                }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size message.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size message.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size message.",
                    Value="2",
                }
            ]
        },
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
                    Name = "RootContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root container of the BitMessage."
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon and content container of the BitMessage."
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
    private double elevation = 7;
    private bool isErrorDismissed;
    private bool isWarningDismissed;
}
