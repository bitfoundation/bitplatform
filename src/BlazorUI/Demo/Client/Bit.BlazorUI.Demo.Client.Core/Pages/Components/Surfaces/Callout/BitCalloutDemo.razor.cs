namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Callout;

public partial class BitCalloutDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Alignment",
            Type = "BitCalloutAlignment?",
            DefaultValue = "null",
            Description = "How the callout is lined up with its anchor along the axis it is not placed on. It defaults to Start.",
            LinkType = LinkType.Link,
            Href = "#callout-alignment-enum"
        },
        new()
        {
            Name = "AlignmentOffset",
            Type = "int",
            DefaultValue = "0",
            Description = "The distance in pixels the callout is slid along the axis it is aligned on, inwards from the edge of the anchor the Alignment lined it up with. A centered callout has no edge for it to run from."
        },
        new()
        {
            Name = "Anchor",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the anchor element of the callout. The anchor is rendered as a plain container, so the content given here should hold the focusable element the user activates.",
        },
        new()
        {
            Name = "AnchorEl",
            Type = "Func<ElementReference>?",
            DefaultValue = "null",
            Description = "The setter function for element reference to the external anchor element."
        },
        new()
        {
            Name = "AnchorId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the external anchor element."
        },
        new()
        {
            Name = "ArrowPadding",
            Type = "int?",
            DefaultValue = "null",
            Description = "The distance in pixels the arrow drawn by ShowArrow is kept away from the corners of the callout, so that the rounding never cuts it. It defaults to 16, and never drops below the size of the arrow itself."
        },
        new()
        {
            Name = "ArrowSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The size in pixels of the arrow drawn by ShowArrow, which is the length of the side of the square the beak is cut out of. It defaults to 12."
        },
        new()
        {
            Name = "AutoClose",
            Type = "bool",
            DefaultValue = "false",
            Description = "Closes the callout as soon as a click lands anywhere inside it."
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Moves the focus into the callout as soon as it opens, to its first focusable element, or to the callout itself when it holds none."
        },
        new()
        {
            Name = "Background",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the background of the callout.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum"
        },
        new()
        {
            Name = "Border",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the border of the callout.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum"
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the callout."
        },
        new()
        {
            Name = "Classes",
            Type = "BitCalloutClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the callout.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "CollisionPadding",
            Type = "int",
            DefaultValue = "0",
            Description = "The distance in pixels the callout keeps from the edges of the screen when it is placed and when it is slid back onto it."
        },
        new()
        {
            Name = "Content",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias for ChildContent."
        },
        new()
        {
            Name = "DefaultIsOpen",
            Type = "bool?",
            DefaultValue = "null",
            Description = "The initial opening state of the callout in the uncontrolled mode, which is when the IsOpen parameter is not set."
        },
        new()
        {
            Name = "Direction",
            Type = "BitDropDirection?",
            DefaultValue = "null",
            Description = "Determines the allowed directions in which the callout should decide to be opened.",
            LinkType = LinkType.Link,
            Href = "#drop-direction-enum"
        },
        new()
        {
            Name = "FixedCalloutWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Holds the callout to the width of its anchor, so that a content wider than the anchor wraps inside it instead of stretching it."
        },
        new()
        {
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of a footer that stays at the bottom of the callout while the rest of it scrolls."
        },
        new()
        {
            Name = "FooterId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the footer element that renders at the end of the scrolling container of the callout content. It wins over the Footer parameter."
        },
        new()
        {
            Name = "Gap",
            Type = "int",
            DefaultValue = "0",
            Description = "The distance in pixels between the anchor and the callout, on whichever side the callout ends up being placed."
        },
        new()
        {
            Name = "Header",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of a header that stays at the top of the callout while the rest of it scrolls."
        },
        new()
        {
            Name = "HeaderId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the header element that renders at the top of the scrolling container of the callout content. It wins over the Header parameter."
        },
        new()
        {
            Name = "HoverCloseDelay",
            Type = "int",
            DefaultValue = "150",
            Description = "The delay in milliseconds before the callout closes once the pointer leaves the callout and its anchor in the OpenOnHover mode."
        },
        new()
        {
            Name = "HoverOpenDelay",
            Type = "int",
            DefaultValue = "0",
            Description = "The delay in milliseconds before the callout opens once the pointer enters the anchor in the OpenOnHover mode."
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines the opening state of the callout."
        },
        new()
        {
            Name = "LazyRender",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the content of the callout out of the page until the callout is opened for the first time. Once rendered it stays, so whatever state the content holds survives the callout closing."
        },
        new()
        {
            Name = "MaxHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "The maximum height of the callout as a CSS value, beyond which its content scrolls."
        },
        new()
        {
            Name = "MaxWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The maximum width of the callout as a CSS value, beyond which its content wraps."
        },
        new()
        {
            Name = "MaxWindowWidth",
            Type = "int?",
            DefaultValue = "null",
            Description = "The window width in pixels below which the callout is allowed to hang off the end of the screen rather than being slid back onto it."
        },
        new()
        {
            Name = "MinWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The minimum width of the callout as a CSS value, so that a narrow content does not end up in a cramped callout."
        },
        new()
        {
            Name = "Modal",
            Type = "bool",
            DefaultValue = "false",
            Description = "Dims the page behind the callout and holds it still while the callout is open, so that the callout reads as the only thing in play. It implies TrapFocus."
        },
        new()
        {
            Name = "NoDismissOnEscape",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the Escape key from dismissing the callout."
        },
        new()
        {
            Name = "NoDismissOnOutsideClick",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the callout open when a click lands outside of it, and when the page is scrolled or resized under it."
        },
        new()
        {
            Name = "NoFlip",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the callout on the Side it was asked for even when there is not enough room for it there, instead of flipping it to the opposite side."
        },
        new()
        {
            Name = "NoOverlay",
            Type = "bool",
            DefaultValue = "false",
            Description = "Leaves the page its own clicks while the callout is open, by not rendering the overlay that otherwise covers it. A Modal callout keeps its overlay."
        },
        new()
        {
            Name = "NoShadow",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the box-shadow from the callout."
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback that is called when the callout is dismissed."
        },
        new()
        {
            Name = "OnOpen",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback that is called when the callout is opened."
        },
        new()
        {
            Name = "OnToggle",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "The callback that is called when the callout opens or closes."
        },
        new()
        {
            Name = "OpenOnHover",
            Type = "bool",
            DefaultValue = "false",
            Description = "Opens the callout when the pointer enters the anchor and closes it when the pointer leaves both the anchor and the callout."
        },
        new()
        {
            Name = "PanelPosition",
            Type = "BitPanelPosition?",
            DefaultValue = "null",
            Description = "The edge of the screen the responsive panel slides in from, for a ResponsiveMode of Panel. It defaults to End.",
            LinkType = LinkType.Link,
            Href = "#panel-position-enum"
        },
        new()
        {
            Name = "ResponsiveMode",
            Type = "BitResponsiveMode?",
            DefaultValue = "null",
            Description = "Configures the responsive mode of the callout for the small screens.",
            LinkType = LinkType.Link,
            Href = "#responsive-mode-enum"
        },
        new()
        {
            Name = "Role",
            Type = "string?",
            DefaultValue = "null",
            Description = "The ARIA role of the callout. It defaults to dialog for a callout that traps the focus (TrapFocus or Modal), and to nothing for the others."
        },
        new()
        {
            Name = "ScrollContainerId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the element which needs to be scrollable in the content of the callout."
        },
        new()
        {
            Name = "ScrollOffset",
            Type = "int?",
            DefaultValue = "null",
            Description = "The vertical offset of the scroll container to consider in the positioning and height calculation of the callout."
        },
        new()
        {
            Name = "SetCalloutWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Widens the callout to at least the width of its anchor, so that a callout with little in it still reads as belonging to what it was opened from."
        },
        new()
        {
            Name = "ShowArrow",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws an arrow on the edge of the callout that faces the anchor, pointing at it."
        },
        new()
        {
            Name = "Side",
            Type = "BitCalloutSide?",
            DefaultValue = "null",
            Description = "The side of the anchor the callout is placed on when there is room for it there. It wins over Direction, falls back to the opposite side, and then to Direction.",
            LinkType = LinkType.Link,
            Href = "#callout-side-enum"
        },
        new()
        {
            Name = "Styles",
            Type = "BitCalloutClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the callout.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "TrapFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the keyboard inside the callout while it is open and reports it as a modal dialog to the screen readers. It implies AutoFocus."
        },
        new()
        {
            Name = "Width",
            Type = "string?",
            DefaultValue = "null",
            Description = "The width of the callout as a CSS value. SetCalloutWidth and FixedCalloutWidth take precedence over it."
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitCalloutClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitCallout."
                },
                new()
                {
                    Name = "AnchorContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the anchor container element of the BitCallout."
                },
                new()
                {
                    Name = "Arrow",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the arrow (beak) element of the BitCallout."
                },
                new()
                {
                    Name = "Opened",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the opened callout state of the BitCallout."
                },
                new()
                {
                    Name = "Content",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content of the BitCallout."
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header element of the BitCallout, which is rendered when the Header parameter is set."
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the scrolling body element of the BitCallout, which is rendered when the Header or the Footer parameter is set."
                },
                new()
                {
                    Name = "Footer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the footer element of the BitCallout, which is rendered when the Footer parameter is set."
                },
                new()
                {
                    Name = "Overlay",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the overlay of the BitCallout."
                },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "drop-direction-enum",
            Name = "BitDropDirection",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "All",
                    Value = "0",
                    Description = "The direction determined automatically based on the available spaces in all directions."
                },
                new()
                {
                    Name = "TopAndBottom",
                    Value = "1",
                    Description = "The direction determined automatically based on the available spaces in only top and bottom directions."
                },
            ]
        },
        new()
        {
            Id = "responsive-mode-enum",
            Name = "BitResponsiveMode",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "None",
                    Value = "0",
                    Description = "Disables the responsive mode."
                },
                new()
                {
                    Name = "Panel",
                    Value = "1",
                    Description = "Enables the panel responsive mode, whose edge comes from the PanelPosition parameter."
                },
                new()
                {
                    Name = "Top",
                    Value = "2",
                    Description = "Enables the responsive mode as a sheet that comes down from the top of the screen."
                },
                new()
                {
                    Name = "Bottom",
                    Value = "3",
                    Description = "Enables the responsive mode as a sheet that comes up from the bottom of the screen."
                },
            ]
        },
        new()
        {
            Id = "callout-side-enum",
            Name = "BitCalloutSide",
            Description = "",
            Items =
            [
                new() { Name = "Top", Value = "0", Description = "Above the anchor." },
                new() { Name = "Bottom", Value = "1", Description = "Below the anchor." },
                new() { Name = "Start", Value = "2", Description = "Beside the anchor, on the side the content starts from - the left in a left-to-right layout." },
                new() { Name = "End", Value = "3", Description = "Beside the anchor, on the side the content ends at - the right in a left-to-right layout." },
            ]
        },
        new()
        {
            Id = "callout-alignment-enum",
            Name = "BitCalloutAlignment",
            Description = "",
            Items =
            [
                new() { Name = "Start", Value = "0", Description = "Lined up with the edge the anchor starts at - its left edge in a left-to-right layout for a callout above or below it, and its top edge for a callout beside it." },
                new() { Name = "Center", Value = "1", Description = "Centered on the anchor." },
                new() { Name = "End", Value = "2", Description = "Lined up with the edge the anchor ends at - its right edge in a left-to-right layout for a callout above or below it, and its bottom edge for a callout beside it." },
            ]
        },
        new()
        {
            Id = "panel-position-enum",
            Name = "BitPanelPosition",
            Description = "",
            Items =
            [
                new() { Name = "Start", Value = "0", Description = "The panel slides in from the start edge of the screen." },
                new() { Name = "End", Value = "1", Description = "The panel slides in from the end edge of the screen." },
                new() { Name = "Top", Value = "2", Description = "The panel slides in from the top edge of the screen." },
                new() { Name = "Bottom", Value = "3", Description = "The panel slides in from the bottom edge of the screen." },
            ]
        },
        new()
        {
            Id = "color-kind-enum",
            Name = "BitColorKind",
            Description = "",
            Items =
            [
                new() { Name = "Primary", Value = "0", Description = "The primary color kind." },
                new() { Name = "Secondary", Value = "1", Description = "The secondary color kind." },
                new() { Name = "Tertiary", Value = "2", Description = "The tertiary color kind." },
                new() { Name = "Transparent", Value = "3", Description = "The transparent color kind." },
            ]
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Open",
            Type = "Task",
            Description = "Opens the callout programmatically, unless it is disabled.",
        },
        new()
        {
            Name = "OpenAt",
            Type = "Task",
            Description = "Opens the callout at a point on the screen rather than against an anchor, which is what a context menu needs. It takes the coordinates (double x, double y) or the MouseEventArgs they came from, and moves an already open callout to the new point.",
        },
        new()
        {
            Name = "Close",
            Type = "Task",
            Description = "Closes the callout programmatically.",
        },
        new()
        {
            Name = "Toggle",
            Type = "Task",
            Description = "Toggles the callout to open/close it.",
        },
        new()
        {
            Name = "Reposition",
            Type = "Task",
            Description = "Lays the open callout out again against what it is placed on, without reopening it or replaying its entry animation. It is for what the callout cannot see on its own: a content that has grown or shrunk, or an anchor moved by something other than a resize of it.",
        }
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Callout-background",
            DefaultValue = "$clr-bg-pri",
            Description = "Background of the callout and its arrow. The Background parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Callout-color",
            DefaultValue = "$clr-fg-pri",
            Description = "Text color of the callout.",
        },
        new()
        {
            Name = "--bit-Callout-border-width",
            DefaultValue = "0 ($shp-border-width with Border)",
            Description = "Border width of the callout and its arrow; setting it draws a border without the Border parameter.",
        },
        new()
        {
            Name = "--bit-Callout-border-color",
            DefaultValue = "$clr-brd-pri",
            Description = "Border color of the callout and its arrow. The Border parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Callout-radius",
            DefaultValue = "$shp-radius-popup",
            Description = "Corner radius of the callout.",
        },
        new()
        {
            Name = "--bit-Callout-shadow",
            DefaultValue = "$box-shadow-popup",
            Description = "Elevation of the callout. NoShadow wins over it.",
        },
        new()
        {
            Name = "--bit-Callout-padding",
            DefaultValue = "0",
            Description = "Room inside the callout, or inside each of its header, body and footer when it has them.",
        },
        new()
        {
            Name = "--bit-Callout-arrow-size",
            DefaultValue = "spacing(1.5)",
            Description = "Side of the square the arrow is cut out of. ArrowSize wins over it.",
        },
        new()
        {
            Name = "--bit-Callout-divider-color",
            DefaultValue = "$clr-brd-sec",
            Description = "The hairline under the header and above the footer.",
        },
        new()
        {
            Name = "--bit-Callout-focus-color",
            DefaultValue = "$clr-pri-focus",
            Description = "Focus ring of the callout itself, shown when it takes the focus while holding nothing focusable.",
        },
        new()
        {
            Name = "--bit-Callout-overlay-background",
            DefaultValue = "$clr-bg-overlay",
            Description = "The dimmed backdrop of a Modal callout.",
        },
    ];



    private ElementReference anchorEl = default!;
    private BitCallout callout1 = default!;
    private BitCallout callout2 = default!;
    private BitCallout callout3 = default!;
    private BitCallout callout4 = default!;
    private BitCallout contextCallout = default!;
    private BitCallout modalCallout = default!;

    private bool isOpen;
    private DateTimeOffset? lazyDate;
    private DateTimeOffset? eagerDate;
    private int openCount;
    private int toggleCount;
    private int dismissCount;
    private string autoCloseAction = "none";
    private string contextAction = "none";
    private int repositionRows = 2;
    private bool repositionAfterRender;

    private string placementSide = "Auto";
    private BitCalloutAlignment placementAlignment = BitCalloutAlignment.Start;
    private int placementGap = 8;
    private int placementOffset;
    private bool placementNoFlip;

    private BitCalloutSide? PlacementSide => Enum.TryParse<BitCalloutSide>(placementSide, out var side) ? side : null;

    private readonly BitCalloutParams[] calloutParams =
    [
        new()
        {
            ShowArrow = true,
            Gap = 8,
            Side = BitCalloutSide.End,
            Border = BitColorKind.Secondary,
            NoShadow = true,
        }
    ];

    private void AddRepositionRow()
    {
        repositionRows++;

        // The callout is laid out against what is actually in it, so the reposition waits for the render
        // that puts the new row there rather than measuring the content the callout still holds.
        repositionAfterRender = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (repositionAfterRender)
        {
            repositionAfterRender = false;

            await callout4.Reposition();
        }
    }
}
