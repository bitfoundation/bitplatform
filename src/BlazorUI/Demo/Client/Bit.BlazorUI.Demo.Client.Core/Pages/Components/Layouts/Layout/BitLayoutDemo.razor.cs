namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Layout;

public partial class BitLayoutDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Aside",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the aside section, which sits next to the main content on the side opposite the nav panel. It renders a semantic aside element (the complementary landmark), and is left out of the markup entirely while it is null or HideAside is on.",
        },
        new()
        {
            Name = "AsideAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible label of the aside section, which is what tells more than one complementary landmark apart in the landmark list of a screen reader.",
        },
        new()
        {
            Name = "AsideWidth",
            Type = "int",
            DefaultValue = "0",
            Description = "The width of the aside section in pixels. This is the room the main content leaves for the aside, not a width forced onto the aside itself, so an aside that takes itself out of the flow only has to be given 0 for the main content to span the whole row again.",
        },
        new()
        {
            Name = "Bordered",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws divider lines between the sections of the BitLayout. The two vertical dividers follow the reading direction and swap sides along with ReverseNavPanel.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitLayoutClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitLayout.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the footer section. It renders a semantic footer element (the contentinfo landmark), and is left out of the markup entirely while it is null or HideFooter is on.",
        },
        new()
        {
            Name = "FooterHeight",
            Type = "int?",
            DefaultValue = "null",
            Description = "The height of the footer section in pixels, including its paddings and border. When not set, the footer is as tall as its own content.",
        },
        new()
        {
            Name = "FullHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the BitLayout fill at least the height of the viewport, so the footer of a short page still sits at the bottom of the screen. The height follows the browser chrome of a mobile device as it retracts.",
        },
        new()
        {
            Name = "FullHeightPanels",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the nav panel and the aside span the whole height of the BitLayout, with the header and the footer between them rather than above and below them. The panels are columns of their own here, so NavPanelWidth and AsideWidth are applied to the panels themselves.",
        },
        new()
        {
            Name = "Gap",
            Type = "string?",
            DefaultValue = "null",
            Description = "The space between the nav panel, the main content and the aside (the CSS gap of the middle row). Takes any CSS length or the two value form of the gap shorthand.",
        },
        new()
        {
            Name = "Header",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the header section. It renders a semantic header element (the banner landmark), and is left out of the markup entirely while it is null or HideHeader is on.",
        },
        new()
        {
            Name = "HeaderHeight",
            Type = "int?",
            DefaultValue = "null",
            Description = "The height of the header section in pixels, including its paddings and border. It is also the offset a sticky nav panel or aside pins itself at, so a panel pinned under a sticky header starts right below it.",
        },
        new()
        {
            Name = "HideAside",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the aside section, which is removed from the markup so the main content takes the room it used to reserve for it.",
        },
        new()
        {
            Name = "HideFooter",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the footer section.",
        },
        new()
        {
            Name = "HideHeader",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the header section.",
        },
        new()
        {
            Name = "HideNavPanel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the nav panel section, which is removed from the markup so the main content takes the room it used to reserve for it.",
        },
        new()
        {
            Name = "Main",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the main section. It renders a semantic main element (the main landmark and the target of the skip link), and unlike the other sections it is always rendered.",
        },
        new()
        {
            Name = "NavPanel",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the nav panel section. It renders a semantic nav element (the navigation landmark), and is left out of the markup entirely while it is null or HideNavPanel is on.",
        },
        new()
        {
            Name = "NavPanelAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible label of the nav panel section, which is what tells the navigation landmarks of a page apart in the landmark list of a screen reader.",
        },
        new()
        {
            Name = "NavPanelWidth",
            Type = "int",
            DefaultValue = "0",
            Description = "The width of the nav panel section in pixels. This is the room the main content leaves for the panel, not a width forced onto the panel itself, so a panel that collapses to a rail or turns into an overlay drawer only has to be given 0 for the main content to span the whole row again.",
        },
        new()
        {
            Name = "Nested",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the main section as a plain element instead of the main landmark, for a BitLayout nested inside another one, since a page may hold only one main landmark. The section keeps its id, its classes and its place as the target of the skip link.",
        },
        new()
        {
            Name = "Padding",
            Type = "string?",
            DefaultValue = "null",
            Description = "The padding around the content of the main section. Takes any CSS padding value; the main section is a border-box, so the padding is taken out of the width it already has rather than added to it.",
        },
        new()
        {
            Name = "ReverseNavPanel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the position of the nav panel and the aside inside the middle row, which puts the nav panel at the end of the row and the aside at its start.",
        },
        new()
        {
            Name = "ScrollableMain",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the header and the footer in place and gives the sections of the middle row a scrollport of their own, which is the shape of an application shell. It needs the BitLayout to have a height to fill: FullHeight, or a parent of a definite height.",
        },
        new()
        {
            Name = "SkipLink",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders a skip link as the first focusable element of the BitLayout, which jumps to the main section. The link stays out of sight until it is focused.",
        },
        new()
        {
            Name = "SkipLinkText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the skip link. The default value is \"Skip to main content\".",
        },
        new()
        {
            Name = "StickyAside",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables sticky positioning of the aside, pinned HeaderHeight pixels from the top of the viewport and given the rest of it with its own scrollbar.",
        },
        new()
        {
            Name = "StickyFooter",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables sticky positioning of the footer at the bottom of the viewport.",
        },
        new()
        {
            Name = "StickyHeader",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables sticky positioning of the header at the top of the viewport.",
        },
        new()
        {
            Name = "StickyNavPanel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables sticky positioning of the nav panel, pinned HeaderHeight pixels from the top of the viewport and given the rest of it with its own scrollbar.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitLayoutClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitLayout.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "ZIndex",
            Type = "int?",
            DefaultValue = "null",
            Description = "The stacking order of the pinned sections of the BitLayout, which decides whether a sticky section passes over or under the other layers of the application. When not set, they take the base z-index of the theme.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitLayoutClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitLayout."
                },
                new()
                {
                    Name = "SkipLink",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the skip link of the BitLayout."
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header section of the BitLayout."
                },
                new()
                {
                    Name = "Main",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the middle row of the BitLayout that holds the nav panel, the main content and the aside."
                },
                new()
                {
                    Name = "NavPanel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the nav panel section of the BitLayout."
                },
                new()
                {
                    Name = "MainContent",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the main-content section of the BitLayout."
                },
                new()
                {
                    Name = "Aside",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the aside section of the BitLayout."
                },
                new()
                {
                    Name = "Footer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the footer section of the BitLayout."
                }
            ]
        }
    ];



    private bool hideNavPanel;

    private bool hideAside;

    private bool reverseNavPanel;

    private bool fullHeightPanels;
    private bool reverseNavPanel2;

    private bool hideHeader;
    private bool hideNavPanel2;
    private bool hideAside2;
    private bool hideFooter;

    private bool stickyHeader;
    private bool stickyNavPanel;
    private bool stickyAside;
    private bool stickyFooter;

    private bool fullHeight;

    private bool scrollableMain;
}
