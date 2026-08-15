namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Header;

public partial class BitHeaderDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Absolute",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the header with an absolute position at the top of its nearest positioned ancestor. Fixed takes precedence over it, and it takes precedence over Sticky.",
        },
        new()
        {
            Name = "Alignment",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "Gets or sets the horizontal distribution of the content of the BitHeader (the CSS justify-content of the container). Baseline and Stretch act on the cross axis (the vertical alignment) instead.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "Bordered",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders a divider line on the bottom edge of the BitHeader to separate it from the content below.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Gets or sets the content to be rendered inside the BitHeader.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitHeaderClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitHeader.",
            LinkType = LinkType.Link,
            Href = "#header-class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the BitHeader. It is applied through the Variant: as the background color in the Fill variant, and as the text and border color in the Outline and Text variants.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "ElevateOffset",
            Type = "int?",
            DefaultValue = "null",
            Description = "Gets or sets how far (in pixels) the scroll has to travel from the top before an ElevateOnScroll header lifts itself off the content.",
        },
        new()
        {
            Name = "ElevateOnScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the BitHeader flat while the scrolling area sits at its top and lets it cast its shadow only once the content has been scrolled underneath it. It only has an effect on a Fixed or Sticky header, and Elevated takes precedence over it.",
        },
        new()
        {
            Name = "Elevated",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the BitHeader with a shadow cast downwards, to lift it above the content it overlaps.",
        },
        new()
        {
            Name = "Fixed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the header with a fixed position at the top of the page. It takes precedence over Absolute and Sticky when more than one of them is set.",
        },
        new()
        {
            Name = "Gap",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the space between the children of the BitHeader (the CSS gap of the container). It takes any CSS length or the two value form of the gap shorthand.",
        },
        new()
        {
            Name = "Height",
            Type = "int?",
            DefaultValue = "null",
            Description = "Gets or sets the height of the BitHeader (in pixels). The height includes the paddings and the border of the header, and a Fixed or Sticky header adds the top safe area inset of the device on top of it.",
        },
        new()
        {
            Name = "Hidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "Slides the BitHeader out of the view, and brings it back when it is turned off again. A hidden header is also marked inert, so nothing inside it can be clicked or reached with the keyboard while it is out of the view.",
        },
        new()
        {
            Name = "NoGutter",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the default paddings around the content of the BitHeader, so it can span the full width of the header.",
        },
        new()
        {
            Name = "OnRevealChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "Callback for when the reveal state of the header changes. The provided value is true when the header is revealed. Only invoked while Reveal is enabled.",
        },
        new()
        {
            Name = "OnScrolledChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "Callback for when the scrolled state of the header changes. The provided value is true once the scrolling area has travelled past the ElevateOffset. Only invoked while ElevateOnScroll is enabled.",
        },
        new()
        {
            Name = "Reveal",
            Type = "bool",
            DefaultValue = "false",
            Description = "Slides the header out of the view while the page is scrolled down and brings it back while the page is scrolled up. It only has an effect on a Fixed or Sticky header, since the others have nothing to slide over.",
        },
        new()
        {
            Name = "RevealOffset",
            Type = "int?",
            DefaultValue = "null",
            Description = "Gets or sets how far (in pixels) the scroll has to travel from the top before a Reveal header starts hiding itself. The header stays revealed while the scroll is still within this offset.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the BitHeader, which determines the paddings around its content.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "SkipLinkHref",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the target of the skip link of the BitHeader, which is what makes it render at all. It is rendered as the very first focusable element of the header and stays out of sight until it is focused.",
        },
        new()
        {
            Name = "SkipLinkText",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the text of the skip link of the BitHeader. It defaults to \"Skip to main content\" and is only rendered when a SkipLinkHref is provided.",
        },
        new()
        {
            Name = "Sticky",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the header with a sticky position at the top of the viewport. Unlike Fixed, it stays in the normal flow, so it never overlaps the content.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitHeaderClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitHeader.",
            LinkType = LinkType.Link,
            Href = "#header-class-styles",
        },
        new()
        {
            Name = "Translucent",
            Type = "bool",
            DefaultValue = "false",
            Description = "Softens the background of the BitHeader and blurs what passes behind it, for the frosted glass look of a header pinned over scrolling content. Only the Fill variant has a background to soften.",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the BitHeader.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
        new()
        {
            Name = "VerticalAlign",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "Gets or sets the vertical alignment of the content of the BitHeader (the CSS align-items of the container). Only Start, End, Center, Baseline and Stretch align a line on the cross axis, so the three space distributions are ignored here.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "Wrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets the content of the BitHeader wrap onto more than one line instead of being squeezed into a single one. The lines are packed by VerticalAlign and separated by the row part of Gap.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "IsRevealed",
            Type = "bool",
            DefaultValue = "true",
            Description = "Gets a value indicating whether the header is currently revealed. It reports the scroll driven reveal state alone, so it is always true unless Reveal is enabled, and stays true for a header slid out of the view with Hidden.",
        },
        new()
        {
            Name = "IsScrolled",
            Type = "bool",
            DefaultValue = "false",
            Description = "Gets a value indicating whether the scrolling area of the header has travelled past the ElevateOffset. It is always false unless ElevateOnScroll is enabled.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "header-class-styles",
            Title = "BitHeaderClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitHeader."
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container element of the BitHeader that wraps its content."
                },
                new()
                {
                    Name = "SkipLink",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the skip link of the BitHeader, which is only rendered when a SkipLinkHref is provided."
                },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "alignment-enum",
            Name = "BitAlignment",
            Description = "Determines how the free space of the content line is distributed (Alignment) and where the content sits in the height of the header (VerticalAlign).",
            Items =
            [
                new() { Name = "Start", Description = "Packs the content at the start of the line, or at the top of the header.", Value = "0" },
                new() { Name = "End", Description = "Packs the content at the end of the line, or at the bottom of the header.", Value = "1" },
                new() { Name = "Center", Description = "Packs the content at the center of the line, or in the middle of the header.", Value = "2" },
                new() { Name = "SpaceBetween", Description = "Distributes the free space between the items, with no space at the two edges. Ignored by VerticalAlign.", Value = "3" },
                new() { Name = "SpaceAround", Description = "Distributes the free space around the items, so the edges get half of what sits between the items. Ignored by VerticalAlign.", Value = "4" },
                new() { Name = "SpaceEvenly", Description = "Distributes the free space evenly between the items and at the two edges. Ignored by VerticalAlign.", Value = "5" },
                new() { Name = "Baseline", Description = "Aligns the content on its baseline (the cross axis of the header).", Value = "6" },
                new() { Name = "Stretch", Description = "Stretches the content to the full height of the header (the cross axis).", Value = "7" },
            ]
        },
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Primary", Description = "Primary general color.", Value = "0" },
                new() { Name = "Secondary", Description = "Secondary general color.", Value = "1" },
                new() { Name = "Tertiary", Description = "Tertiary general color.", Value = "2" },
                new() { Name = "Info", Description = "Info general color.", Value = "3" },
                new() { Name = "Success", Description = "Success general color.", Value = "4" },
                new() { Name = "Warning", Description = "Warning general color.", Value = "5" },
                new() { Name = "SevereWarning", Description = "SevereWarning general color.", Value = "6" },
                new() { Name = "Error", Description = "Error general color.", Value = "7" },
                new() { Name = "PrimaryBackground", Description = "Primary background color.", Value = "8" },
                new() { Name = "SecondaryBackground", Description = "Secondary background color.", Value = "9" },
                new() { Name = "TertiaryBackground", Description = "Tertiary background color.", Value = "10" },
                new() { Name = "PrimaryForeground", Description = "Primary foreground color.", Value = "11" },
                new() { Name = "SecondaryForeground", Description = "Secondary foreground color.", Value = "12" },
                new() { Name = "TertiaryForeground", Description = "Tertiary foreground color.", Value = "13" },
                new() { Name = "PrimaryBorder", Description = "Primary border color.", Value = "14" },
                new() { Name = "SecondaryBorder", Description = "Secondary border color.", Value = "15" },
                new() { Name = "TertiaryBorder", Description = "Tertiary border color.", Value = "16" },
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Determines the paddings around the content of the header.",
            Items =
            [
                new() { Name = "Small", Description = "The small size.", Value = "0" },
                new() { Name = "Medium", Description = "The medium size.", Value = "1" },
                new() { Name = "Large", Description = "The large size.", Value = "2" },
            ]
        },
        new()
        {
            Id = "variant-enum",
            Name = "BitVariant",
            Description = "Determines the variant of the content that controls the rendered style of the corresponding element(s).",
            Items =
            [
                new() { Name = "Fill", Description = "Fill styled variant.", Value = "0" },
                new() { Name = "Outline", Description = "Outline styled variant.", Value = "1" },
                new() { Name = "Text", Description = "Text styled variant.", Value = "2" },
            ]
        },
    ];



    private bool isHeaderRevealed = true;
    private bool isHeaderScrolled;
    private bool isImmersiveMode;
}
