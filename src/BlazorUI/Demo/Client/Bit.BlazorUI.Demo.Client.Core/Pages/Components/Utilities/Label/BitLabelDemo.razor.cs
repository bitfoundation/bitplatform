namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Label;

public partial class BitLabelDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the label: text or any markup. A control put inside it is named without For.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitLabelClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the different parts of the label.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the label. Inherits the color of its container while not set.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Element",
            Type = "string?",
            DefaultValue = "null",
            Description = "The html element of the root, for a group caption (div, legend, ...). Defaults to \"label\", which an invalid tag name falls back to.",
        },
        new()
        {
            Name = "For",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the control the label names (the \"for\" attribute). Ignored when Element renders another tag.",
        },
        new()
        {
            Name = "NoSelect",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents the text of the label from being selected, e.g. by a double click on the caption of a checkbox.",
        },
        new()
        {
            Name = "NoWrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the label on a single line and truncates the overflow with an ellipsis.",
        },
        new()
        {
            Name = "Optional",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the optional indicator after the content. Ignored while Required is set.",
        },
        new()
        {
            Name = "OptionalTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template of the optional indicator. Takes precedence over OptionalText.",
        },
        new()
        {
            Name = "OptionalText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the optional indicator. The default is \"(optional)\".",
        },
        new()
        {
            Name = "Required",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the required indicator after the content. The default asterisk is hidden from assistive technologies.",
        },
        new()
        {
            Name = "RequiredTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template of the required indicator, announced by screen readers. Takes precedence over RequiredText.",
        },
        new()
        {
            Name = "RequiredText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the required indicator, announced by screen readers. The default is \"*\".",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the label. Unset means medium.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitLabelClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for the different parts of the label.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "VisuallyHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the label from the page but not from assistive technologies, so it still names its control. It reappears while a control inside it has the focus.",
        }
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Label-color",
            DefaultValue = "inherit",
            Description = "Color of the caption. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Label-font-size",
            DefaultValue = "--bit-tpg-fs-sm",
            Description = "Size of the text. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Label-font-weight",
            DefaultValue = "--bit-tpg-fw-semibold",
            Description = "Weight of the caption.",
        },
        new()
        {
            Name = "--bit-Label-line-height",
            DefaultValue = "spacing(2.5) (20px)",
            Description = "Height of a line of the caption.",
        },
        new()
        {
            Name = "--bit-Label-padding",
            DefaultValue = "spacing(0.625) 0 (5px 0)",
            Description = "Room around the caption.",
        },
        new()
        {
            Name = "--bit-Label-indicator-gap",
            DefaultValue = "spacing(0.625) (5px)",
            Description = "Gap between the caption and its required or optional indicator.",
        },
        new()
        {
            Name = "--bit-Label-required-color",
            DefaultValue = "--bit-clr-req",
            Description = "Color of the required indicator.",
        },
        new()
        {
            Name = "--bit-Label-optional-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the optional indicator.",
        },
        new()
        {
            Name = "--bit-Label-focus-color",
            DefaultValue = "--bit-clr-pri-focus",
            Description = "Color of the focus ring of a label given a TabIndex.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitLabelClassStyles",
            Description = "The custom CSS classes/styles for the different parts of the label.",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the label.",
                },
                new()
                {
                    Name = "RequiredIndicator",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the required indicator of the label, which only exists while Required is set.",
                },
                new()
                {
                    Name = "OptionalIndicator",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the optional indicator of the label, which only exists while Optional is set and Required is not.",
                }
            ]
        }
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
                    Name= "Primary",
                    Description="Primary general color.",
                    Value="0",
                },
                new()
                {
                    Name= "Secondary",
                    Description="Secondary general color.",
                    Value="1",
                },
                new()
                {
                    Name= "Tertiary",
                    Description="Tertiary general color.",
                    Value="2",
                },
                new()
                {
                    Name= "Info",
                    Description="Info general color.",
                    Value="3",
                },
                new()
                {
                    Name= "Success",
                    Description="Success general color.",
                    Value="4",
                },
                new()
                {
                    Name= "Warning",
                    Description="Warning general color.",
                    Value="5",
                },
                new()
                {
                    Name= "SevereWarning",
                    Description="SevereWarning general color.",
                    Value="6",
                },
                new()
                {
                    Name= "Error",
                    Description="Error general color.",
                    Value="7",
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
            Id = "size-enum",
            Name = "BitSize",
            Description = "Defines the sizes available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size.",
                    Value="2",
                }
            ]
        }
    ];



    private readonly BitLabelParams[] labelParams =
    [
        new()
        {
            Required = true,
            RequiredText = "(required)",
        }
    ];
}
