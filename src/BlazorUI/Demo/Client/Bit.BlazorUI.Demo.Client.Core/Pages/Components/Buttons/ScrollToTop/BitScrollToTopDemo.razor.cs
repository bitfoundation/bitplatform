namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ScrollToTop;

public partial class BitScrollToTopDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitScrollToTopClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the different parts of the scroll to top button.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Float",
            Type = "bool",
            DefaultValue = "false",
            Description = "Floats the button over the content, pinned to a corner or an edge of the viewport (FloatPosition). Left unset, the button sits in the flow of the page where it is written.",
        },
        new()
        {
            Name = "FloatAbsolute",
            Type = "bool",
            DefaultValue = "false",
            Description = "Floats the button over the content, pinned to a corner or an edge of its nearest positioned ancestor rather than of the viewport. Takes precedence over Float.",
        },
        new()
        {
            Name = "FloatOffset",
            Type = "string?",
            DefaultValue = "null",
            Description = "The inset of a floating button from the edge it is pinned to: any CSS length (1rem, 5%, a calc()), or a bare number, which is read as pixels. It wins over --bit-ScrollToTop-float-offset.",
        },
        new()
        {
            Name = "FloatPosition",
            Type = "BitPosition?",
            DefaultValue = "null",
            Description = "Where a floating button is pinned. Defaults to BottomEnd: the bottom right corner, and the bottom left one in a right-to-left page.",
            LinkType = LinkType.Link,
            Href = "#position-enum",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the button, provided as custom CSS classes of an external icon library. Takes precedence over IconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon of the button, from the built-in Fluent UI icons. A button with no Text shows an arrow pointing up (Up) when no icon is given, and one with a text shows no icon unless one is given.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "RevealOffset",
            Type = "int?",
            DefaultValue = "null",
            Description = "How far (in pixels) the scroll has to travel from the top before the button is revealed. Within it the button is hidden - out of the view, the tab order and the accessibility tree - from the first render on. When not set, the button is always shown.",
        },
        new()
        {
            Name = "ScrollTarget",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS selector of the element whose scrolling the button scrolls back to its top, and reveals itself by. By default the button walks up from itself to the box that scrolls it - the page or a pane it sits in; a selector that matches nothing falls back to that walk.",
        },
        new()
        {
            Name = "Smooth",
            Type = "bool",
            DefaultValue = "true",
            Description = "Scrolls back to the top smoothly rather than in a single jump. It collapses to a jump under a reduced-motion preference, unless ForceAnimation is set.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitScrollToTopClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for the different parts of the scroll to top button.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the button, shown beside its icon, which also names the button. When not set, the button shows its icon alone and is named by the AriaLabel, or else by BitStrings.ScrollToTop (\"Scroll to top\").",
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        DemoSharedEnums.BitPosition(),
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitScrollToTopClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element (the button) of the BitScrollToTop." },
                new() { Name = "Icon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icon of the BitScrollToTop." },
                new() { Name = "Text", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the text of the BitScrollToTop." },
            ]
        },
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Parameters =
            [
                new() { Name = "Name", Type = "string?", DefaultValue = "null", Description = "Gets or sets the name of the icon." },
                new() { Name = "BaseClass", Type = "string?", DefaultValue = "null", Description = "Gets or sets the base CSS class for the icon. For built-in Fluent UI icons, this defaults to \"bit-icon\". For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty." },
                new() { Name = "Prefix", Type = "string?", DefaultValue = "null", Description = "Gets or sets the CSS class prefix used before the icon name. For built-in Fluent UI icons, this defaults to \"bit-icon--\". For external icon libraries, you might set this to \"fa-\" or leave empty." },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-ScrollToTop-color", DefaultValue = "--bit-clr-pri-text", Description = "Foreground (text and icon) at rest." },
        new() { Name = "--bit-ScrollToTop-background", DefaultValue = "--bit-clr-pri", Description = "Background at rest." },
        new() { Name = "--bit-ScrollToTop-border-color", DefaultValue = "the background at rest", Description = "Border color." },
        new() { Name = "--bit-ScrollToTop-border-width", DefaultValue = "--bit-shp-border-width", Description = "Thickness of the border." },
        new() { Name = "--bit-ScrollToTop-hover-color", DefaultValue = "the foreground at rest", Description = "Foreground on hover." },
        new() { Name = "--bit-ScrollToTop-hover-background", DefaultValue = "--bit-clr-pri-hover", Description = "Background on hover." },
        new() { Name = "--bit-ScrollToTop-active-color", DefaultValue = "the foreground on hover", Description = "Foreground while pressed." },
        new() { Name = "--bit-ScrollToTop-active-background", DefaultValue = "--bit-clr-pri-active", Description = "Background while pressed." },
        new() { Name = "--bit-ScrollToTop-disabled-color", DefaultValue = "--bit-clr-pri-dis-text", Description = "Foreground when disabled." },
        new() { Name = "--bit-ScrollToTop-disabled-background", DefaultValue = "--bit-clr-pri-dis", Description = "Background and border when disabled." },
        new() { Name = "--bit-ScrollToTop-focus-color", DefaultValue = "--bit-clr-pri-focus", Description = "Focus ring color." },
        new() { Name = "--bit-ScrollToTop-radius", DefaultValue = "--bit-shp-radius-full", Description = "Corner radius of the box and its focus ring." },
        new() { Name = "--bit-ScrollToTop-size", DefaultValue = "--bit-siz-ctrl-lg", Description = "Height of the box, and the width of an icon-only one." },
        new() { Name = "--bit-ScrollToTop-padding", DefaultValue = "0 --bit-siz-ctrl-pad-x-md", Description = "Padding of a button with a text." },
        new() { Name = "--bit-ScrollToTop-gap", DefaultValue = "spacing(1)", Description = "Room between the icon and the text." },
        new() { Name = "--bit-ScrollToTop-font-size", DefaultValue = "--bit-tpg-fs-sm", Description = "Size of the text." },
        new() { Name = "--bit-ScrollToTop-icon-size", DefaultValue = "--bit-siz-icon-md", Description = "Size of the icon." },
        new() { Name = "--bit-ScrollToTop-float-offset", DefaultValue = "spacing(2)", Description = "Inset of a floating button from the edge it is pinned to. The FloatOffset parameter wins over it." },
        new() { Name = "--bit-ScrollToTop-float-shadow", DefaultValue = "--bit-shd-card", Description = "Elevation of a floating button, which lifts it off the content it sits over. The focus ring is a shadow too and replaces it while the button is focused." },
    ];
}
