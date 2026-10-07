namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Separator;

public partial class BitSeparatorDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AlignContent",
            Type = "BitPlacement?",
            DefaultValue = "null",
            Description = "Where the content should be aligned along the line of the separator. Only Start, Center and End are honoured; any other value, or none, centers the content.",
            LinkType = LinkType.Link,
            Href = "#placement-enum",
        },
        new()
        {
            Name = "AutoSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the separator with auto width or height, sizing it to its content instead of its container or the flex row it stands in."
        },
        new()
        {
            Name = "Background",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the background behind the content of the separator. Defaults to transparent.",
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
            Description = "The content of the Separator, it can be any custom tag or text. It sits between the two segments of the line and also names the separator to assistive technologies, so nothing focusable belongs in it."
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
            Description = "The offset of the content from the edge of the line it is aligned to, as any CSS length, where a percentage measures against the length of the separator. Only takes effect while AlignContent is Start or End."
        },
        new()
        {
            Name = "Decorative",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the separator from the accessibility tree, for a separator that is purely visual and should not be announced. Its content, if any, is then read as plain text."
        },
        new()
        {
            Name = "Element",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom html element used for the root node, such as \"li\" between the items of a list or a menu. A tag that cannot hold content falls back to the default \"div\"."
        },
        new()
        {
            Name = "Inset",
            Type = "string?",
            DefaultValue = "null",
            Description = "Holds the separator off the ends of its container: one CSS length for both ends, or two for the start and the end."
        },
        new()
        {
            Name = "LineStyle",
            Type = "BitLineStyle?",
            DefaultValue = "null",
            Description = "The style the line of the separator is drawn in: solid, dashed, dotted or double.",
            LinkType = LinkType.Link,
            Href = "#line-style-enum",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the line of the separator, out of the sizes of the theme. Thickness wins over it.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
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
            Description = "The thickness of the line of the separator, as any CSS length. Defaults to the weight of the current Size, which starts at the theme's divider hairline."
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the element is a vertical separator. It stretches to the height of the flex row it stands in and takes it from its container anywhere else, but is never shorter than a line of text."
        }
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Separator-color",
            DefaultValue = "--bit-clr-brd-sec",
            Description = "Color of the line. The Color and Border parameters win over it.",
        },
        new()
        {
            Name = "--bit-Separator-thickness",
            DefaultValue = "--bit-siz-divider",
            Description = "Weight of the line. The Size and Thickness parameters win over it.",
        },
        new()
        {
            Name = "--bit-Separator-line-style",
            DefaultValue = "solid",
            Description = "Style of the line, as any CSS border style. The LineStyle parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Separator-spacing",
            DefaultValue = "spacing(0.5)",
            Description = "Room on either side of the line, across it: above and below a horizontal separator, beside a vertical one.",
        },
        new()
        {
            Name = "--bit-Separator-inset",
            DefaultValue = "0",
            Description = "Room held off both ends of the line, or off the start and the end given two lengths. The Inset parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Separator-content-gap",
            DefaultValue = "spacing(1.5)",
            Description = "Room between the content and the line on either side of it.",
        },
        new()
        {
            Name = "--bit-Separator-content-offset",
            DefaultValue = "0",
            Description = "Length of the segment before a start- or end-aligned content. The ContentOffset parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Separator-content-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text color of the content.",
        },
        new()
        {
            Name = "--bit-Separator-content-background",
            DefaultValue = "transparent",
            Description = "Background of the content. The Background parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Separator-content-font-size",
            DefaultValue = "inherit",
            Description = "Text size of the content.",
        },
        new()
        {
            Name = "--bit-Separator-content-font-weight",
            DefaultValue = "inherit",
            Description = "Text weight of the content.",
        },
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
        DemoSharedEnums.BitPlacement(),
        DemoSharedEnums.BitLineStyle(),
        DemoSharedEnums.BitSize(),
        DemoSharedEnums.BitColor(),
        DemoSharedEnums.BitColorKind(),
    ];



    private readonly BitSeparatorParams[] separatorParams =
    [
        new()
        {
            Thickness = "2px",
            LineStyle = BitLineStyle.Dashed,
            AlignContent = BitPlacement.Start,
        }
    ];
}
