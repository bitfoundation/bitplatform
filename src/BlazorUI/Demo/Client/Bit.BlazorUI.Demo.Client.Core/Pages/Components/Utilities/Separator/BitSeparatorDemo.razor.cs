namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Separator;

public partial class BitSeparatorDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AlignContent",
            Type = "BitSeparatorAlignContent?",
            DefaultValue = "null",
            Description = "Where the content should be aligned in the separator. Defaults to the center of the line.",
            LinkType = LinkType.Link,
            Href = "#separator-align-enum",
        },
        new()
        {
            Name = "AutoSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the separator with auto width or height, sizing it to its content instead of its container."
        },
        new()
        {
            Name = "Background",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the background of the content of the separator, which masks the line underneath it.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "Border",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the line of the separator, out of the neutral border tiers of the theme.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Separator, it can be any custom tag or text. It sits on the line and also names the separator to assistive technologies."
        },
        new()
        {
            Name = "Classes",
            Type = "BitSeparatorClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the separator.",
            LinkType = LinkType.Link,
            Href = "#separator-class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the line of the separator, painting it in one of the roles of the theme. Wins over Border.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "ContentOffset",
            Type = "string?",
            DefaultValue = "null",
            Description = "The offset of the content from the edge of the line it is aligned to, as any CSS length. Only takes effect while AlignContent is Start or End."
        },
        new()
        {
            Name = "Decorative",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the separator from the accessibility tree, for a separator that is purely visual and should not be announced."
        },
        new()
        {
            Name = "LineStyle",
            Type = "BitSeparatorLineStyle?",
            DefaultValue = "null",
            Description = "The style the line of the separator is drawn in: solid, dashed or dotted.",
            LinkType = LinkType.Link,
            Href = "#separator-line-style-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitSeparatorClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the separator.",
            LinkType = LinkType.Link,
            Href = "#separator-class-styles",
        },
        new()
        {
            Name = "Thickness",
            Type = "string?",
            DefaultValue = "null",
            Description = "The thickness of the line of the separator, as any CSS length. Defaults to the theme's divider hairline."
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the element is a vertical separator. A vertical separator takes its height from its container."
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "separator-class-styles",
            Title = "BitSeparatorClassStyles",
            Description = "",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the separator.",
                },
                new()
                {
                    Name = "Content",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the element wrapping the ChildContent of the separator, which is only rendered while the separator has content.",
                },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "separator-align-enum",
            Name = "BitSeparatorAlignContent",
            Description = "Where the content of the separator sits along its line.",
            Items =
            [
                new()
                {
                    Name = "Start",
                    Description = "The content sits at the start of the line - the top of a vertical separator.",
                    Value = "0",
                },
                new()
                {
                    Name = "Center",
                    Description = "The content sits at the middle of the line, which is the default.",
                    Value = "1",
                },
                new()
                {
                    Name = "End",
                    Description = "The content sits at the end of the line - the bottom of a vertical separator.",
                    Value = "2",
                },
            ]
        },
        new()
        {
            Id = "separator-line-style-enum",
            Name = "BitSeparatorLineStyle",
            Description = "The style the line of the separator is drawn in.",
            Items =
            [
                new()
                {
                    Name = "Solid",
                    Description = "A continuous line, which is the default.",
                    Value = "0",
                },
                new()
                {
                    Name = "Dashed",
                    Description = "A line of short dashes.",
                    Value = "1",
                },
                new()
                {
                    Name = "Dotted",
                    Description = "A line of dots.",
                    Value = "2",
                },
            ]
        },
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
                    Name = "PrimaryBackground",
                    Description = "Primary background color.",
                    Value = "8",
                },
                new()
                {
                    Name = "SecondaryBackground",
                    Description = "Secondary background color.",
                    Value = "9",
                },
                new()
                {
                    Name = "TertiaryBackground",
                    Description = "Tertiary background color.",
                    Value = "10",
                },
                new()
                {
                    Name = "PrimaryForeground",
                    Description = "Primary foreground color.",
                    Value = "11",
                },
                new()
                {
                    Name = "SecondaryForeground",
                    Description = "Secondary foreground color.",
                    Value = "12",
                },
                new()
                {
                    Name = "TertiaryForeground",
                    Description = "Tertiary foreground color.",
                    Value = "13",
                },
                new()
                {
                    Name = "PrimaryBorder",
                    Description = "Primary border color.",
                    Value = "14",
                },
                new()
                {
                    Name = "SecondaryBorder",
                    Description = "Secondary border color.",
                    Value = "15",
                },
                new()
                {
                    Name = "TertiaryBorder",
                    Description = "Tertiary border color.",
                    Value = "16",
                },
            ]
        },
        new()
        {
            Id = "color-kind-enum",
            Name = "BitColorKind",
            Description = "Defines the color kinds available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name = "Primary",
                    Description = "The primary color kind.",
                    Value = "0",
                },
                new()
                {
                    Name = "Secondary",
                    Description = "The secondary color kind.",
                    Value = "1",
                },
                new()
                {
                    Name = "Tertiary",
                    Description = "The tertiary color kind.",
                    Value = "2",
                },
                new()
                {
                    Name = "Transparent",
                    Description = "The transparent color kind.",
                    Value = "3",
                },
            ]
        },
    ];
}
