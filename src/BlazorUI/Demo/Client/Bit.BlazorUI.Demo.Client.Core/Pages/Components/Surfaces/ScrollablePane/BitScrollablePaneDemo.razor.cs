namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.ScrollablePane;

public partial class BitScrollablePaneDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the pane pinned to the end of its content as the content grows. It pins once as soon as it is turned on, and after that only while the reader left the pane standing at the end.",
        },
        new()
        {
            Name = "AutoScrollThreshold",
            Type = "int",
            DefaultValue = "0",
            Description = "How near the end of the content (in pixels) the pane has to have been left for AutoScroll to keep pinning it there.",
        },
        new()
        {
            Name = "AutoHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the height of the pane auto.",
        },
        new()
        {
            Name = "AutoSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes both height and width of the pane auto.",
        },
        new()
        {
            Name = "AutoWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the width of the pane auto.",
        },
        new()
        {
            Name = "AutoHideScrollbar",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the Modern scrollbar of the pane out of sight until the pointer is over it or something in it holds the focus.",
        },
        new()
        {
            Name = "Body",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias for the ChildContent parameter.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the pane, it can be any custom tag or text.",
        },
        new()
        {
            Name = "DragMomentum",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets a released drag carry on at the speed it was let go at and slow to a stop, instead of stopping dead with the button. It only applies to DragScroll.",
        },
        new()
        {
            Name = "DragScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets the pane be scrolled by dragging its content with a mouse or a pen, which is how a strip that only scrolls sideways is reached without a horizontal wheel.",
        },
        new()
        {
            Name = "Fade",
            Type = "bool",
            DefaultValue = "false",
            Description = "Fades out each edge of the pane that still has content beyond it, and takes the fade off the moment that edge is reached.",
        },
        new()
        {
            Name = "FadeSize",
            Type = "string?",
            DefaultValue = "null",
            Description = "How far the Fade reaches into the pane, as any CSS length. It defaults to 2rem.",
        },
        new()
        {
            Name = "FitHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the height of the pane fit-content.",
        },
        new()
        {
            Name = "FitSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes both height and width of the pane fit-content.",
        },
        new()
        {
            Name = "FitWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the width of the pane fit-content.",
        },
        new()
        {
            Name = "Focusable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Puts the pane itself in the tab order, so a pane holding nothing focusable can still be scrolled with the keyboard.",
        },
        new()
        {
            Name = "FullHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the height of the pane 100%.",
        },
        new()
        {
            Name = "FullSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes both height and width of the pane 100%.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the width of the pane 100%.",
        },
        new()
        {
            Name = "Gutter",
            Type = "BitScrollbarGutter?",
            DefaultValue= "null",
            Description = "Allows to reserve space for the scrollbar, preventing unwanted layout changes as the content grows while also avoiding unnecessary visuals when scrolling isn't needed.",
            LinkType = LinkType.Link,
            Href = "#scrollbar-gutter-enum",
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue= "null",
            Description = "The height of the pane.",
        },
        new()
        {
            Name = "Horizontal",
            Type = "bool",
            DefaultValue= "false",
            Description = "Lays the content out on a single line that scrolls sideways instead of wrapping, and turns the vertical scrolling off.",
        },
        new()
        {
            Name = "HorizontalWheel",
            Type = "bool",
            DefaultValue= "false",
            Description = "Turns a vertical wheel over a pane that only scrolls sideways into a sideways scroll, and hands the scroll back to the page once the pane reaches that end.",
        },
        new()
        {
            Name = "InitialScrollLeft",
            Type = "double?",
            DefaultValue= "null",
            Description = "Where the pane stands the first time it is rendered, measured from the visual left edge of the content in pixels. It is applied once and never animated.",
        },
        new()
        {
            Name = "InitialScrollTop",
            Type = "double?",
            DefaultValue= "null",
            Description = "Where the pane stands the first time it is rendered, measured from the top of the content in pixels. It is applied once and never animated.",
        },
        new()
        {
            Name = "MaxHeight",
            Type = "string?",
            DefaultValue= "null",
            Description = "The maximum height of the pane, which is what makes it grow with its content and only start scrolling once there is too much of it.",
        },
        new()
        {
            Name = "MaxWidth",
            Type = "string?",
            DefaultValue= "null",
            Description = "The maximum width of the pane.",
        },
        new()
        {
            Name = "MinHeight",
            Type = "string?",
            DefaultValue= "null",
            Description = "The minimum height of the pane.",
        },
        new()
        {
            Name = "MinWidth",
            Type = "string?",
            DefaultValue= "null",
            Description = "The minimum width of the pane.",
        },
        new()
        {
            Name = "Modern",
            Type = "bool",
            DefaultValue= "false",
            Description = "Enables a modern style for the scrollbar of the pane, drawn in the colors of the theme instead of by the operating system.",
        },
        new()
        {
            Name = "NoScroll",
            Type = "bool",
            DefaultValue= "false",
            Description = "Turns the scrolling of the pane off while leaving its content interactive and the scrolling API of the component working.",
        },
        new()
        {
            Name = "OnReachedBottom",
            Type = "EventCallback",
            Description = "Callback for when the pane has been scrolled to the bottom of its content.",
        },
        new()
        {
            Name = "OnReachedLeft",
            Type = "EventCallback",
            Description = "Callback for when the pane has been scrolled to the visual left edge of its content.",
        },
        new()
        {
            Name = "OnReachedRight",
            Type = "EventCallback",
            Description = "Callback for when the pane has been scrolled to the visual right edge of its content.",
        },
        new()
        {
            Name = "OnReachedTop",
            Type = "EventCallback",
            Description = "Callback for when the pane has been scrolled to the top of its content.",
        },
        new()
        {
            Name = "OnScroll",
            Type = "EventCallback<BitScrollOffset>",
            Description = "Callback for when the pane is scrolled, carrying where it now stands.",
            LinkType = LinkType.Link,
            Href = "#scroll-offset-class",
        },
        new()
        {
            Name = "OnScrollEnd",
            Type = "EventCallback<BitScrollOffset>",
            Description = "Callback for when the pane has come to rest after being scrolled, carrying where it stopped.",
            LinkType = LinkType.Link,
            Href = "#scroll-offset-class",
        },
        new()
        {
            Name = "OnScrollStart",
            Type = "EventCallback<BitScrollOffset>",
            Description = "Callback for when the pane starts being scrolled, carrying where it stood as it set off.",
            LinkType = LinkType.Link,
            Href = "#scroll-offset-class",
        },
        new()
        {
            Name = "Overflow",
            Type = "BitOverflow?",
            DefaultValue= "null",
            Description = "Controls the visibility of scrollbars in the pane.",
            LinkType = LinkType.Link,
            Href = "#overflow-enum",
        },
        new()
        {
            Name = "OverflowX",
            Type = "BitOverflow?",
            DefaultValue= "null",
            Description = "Controls the visibility of X-axis scrollbar in the pane. It has the last word on that axis, over both Overflow and Horizontal.",
            LinkType = LinkType.Link,
            Href = "#overflow-enum",
        },
        new()
        {
            Name = "OverflowY",
            Type = "BitOverflow?",
            DefaultValue= "null",
            Description = "Controls the visibility of Y-axis scrollbar in the pane. It has the last word on that axis, over both Overflow and Horizontal.",
            LinkType = LinkType.Link,
            Href = "#overflow-enum",
        },
        new()
        {
            Name = "Overscroll",
            Type = "BitOverscroll?",
            DefaultValue= "null",
            Description = "What the browser does with a scroll that has already reached the edge of the pane.",
            LinkType = LinkType.Link,
            Href = "#overscroll-enum",
        },
        new()
        {
            Name = "OverscrollX",
            Type = "BitOverscroll?",
            DefaultValue= "null",
            Description = "What the browser does with a horizontal scroll that has already reached the edge of the pane.",
            LinkType = LinkType.Link,
            Href = "#overscroll-enum",
        },
        new()
        {
            Name = "OverscrollY",
            Type = "BitOverscroll?",
            DefaultValue= "null",
            Description = "What the browser does with a vertical scroll that has already reached the edge of the pane.",
            LinkType = LinkType.Link,
            Href = "#overscroll-enum",
        },
        new()
        {
            Name = "PreserveScroll",
            Type = "bool",
            DefaultValue= "false",
            Description = "Keeps the reader's place when content is added above what they are looking at, which is what a conversation that loads older messages at its top needs. Every engine but WebKit already does this on its own, so it only steps in where the browser is not anchoring the pane - which a pane setting overflow-anchor: none on itself is not, on any engine.",
        },
        new()
        {
            Name = "ReachOffset",
            Type = "int",
            DefaultValue= "0",
            Description = "How near an edge (in pixels) counts as having reached it, for the four edge callbacks.",
        },
        new()
        {
            Name = "Role",
            Type = "string?",
            DefaultValue= "null",
            Description = "The ARIA role of the pane. A pane renders none of its own; set it to region or group, along with AriaLabel, where the pane is a part of the page in its own right.",
        },
        new()
        {
            Name = "ScrollbarColor",
            Type = "string?",
            DefaultValue= "null",
            Description = "Sets the color of the scrollbar thumb and track, in that order. For specific colors, it has to contain both colors separated by a space or otherwise it won't work.",
        },
        new()
        {
            Name = "ScrollbarWidth",
            Type = "BitScrollbarWidth?",
            DefaultValue= "null",
            Description = "Sets the desired thickness of scrollbars when they are shown.",
            LinkType = LinkType.Link,
            Href = "#scrollbar-width-enum",
        },
        new()
        {
            Name = "ScrollPadding",
            Type = "string?",
            DefaultValue= "null",
            Description = "The inset the pane keeps between its edges and anything scrolled into view inside it, as any CSS length.",
        },
        new()
        {
            Name = "ScrollThrottle",
            Type = "int",
            DefaultValue= "0",
            Description = "The shortest interval (in milliseconds) between two OnScroll reports. Reports are always coalesced to at most one per animation frame.",
        },
        new()
        {
            Name = "Smooth",
            Type = "bool",
            DefaultValue= "false",
            Description = "Animates every move the pane does not make by hand: the scrolling API, a fragment navigation into it, and the pinning AutoScroll does.",
        },
        new()
        {
            Name = "Snap",
            Type = "BitScrollSnap?",
            DefaultValue= "null",
            Description = "Makes the pane come to rest on the snap positions of its content instead of anywhere between them.",
            LinkType = LinkType.Link,
            Href = "#scroll-snap-enum",
        },
        new()
        {
            Name = "SnapAlign",
            Type = "BitScrollSnapAlign?",
            DefaultValue= "null",
            Description = "Where the direct children of the pane come to rest in it while Snap is on.",
            LinkType = LinkType.Link,
            Href = "#scroll-snap-align-enum",
        },
        new()
        {
            Name = "SnapStop",
            Type = "bool",
            DefaultValue= "false",
            Description = "Keeps a fast scroll from passing over the snap positions it goes by, which is what turns a snapping strip into a carousel that moves one item per gesture.",
        },
        new()
        {
            Name = "Width",
            Type = "string?",
            DefaultValue= "null",
            Description = "The width of the pane.",
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask FocusAsync() / ValueTask FocusAsync(bool preventScroll)",
            Description = "Gives the focus to the pane itself, which only takes it while Focusable or a TabIndex has put it in the tab order.",
        },
        new()
        {
            Name = "GetScrollOffset",
            Type = "ValueTask<BitScrollOffset?>",
            Description = "Reads where the pane currently stands, straight from the browser.",
            LinkType = LinkType.Link,
            Href = "#scroll-offset-class",
        },
        new()
        {
            Name = "Refresh",
            Type = "ValueTask",
            Description = "Re-measures the pane and brings the fade and the edge callbacks back up to date, for the size changes neither of its observers can see.",
        },
        new()
        {
            Name = "ScrollBy",
            Type = "ValueTask ScrollBy(double x, double y, bool? smooth = null)",
            Description = "Scrolls the pane by a distance from wherever it currently stands, in pixels and measured on the screen: a positive x always moves the pane rightwards.",
        },
        new()
        {
            Name = "ScrollTo",
            Type = "ValueTask ScrollTo(double? left, double? top, bool? smooth = null)",
            Description = "Scrolls the pane to an absolute position, in pixels from the top and from the visual left edge of the content. An axis that is left null stays where it is.",
        },
        new()
        {
            Name = "ScrollToElement",
            Type = "ValueTask ScrollToElement(string elementId, double offset = 0, bool? smooth = null, BitScrollAlignment alignment = BitScrollAlignment.Start)",
            Description = "Brings an element inside the pane into view by scrolling the pane itself, leaving every scrolling ancestor of it alone.",
            LinkType = LinkType.Link,
            Href = "#scroll-alignment-enum",
        },
        new()
        {
            Name = "ScrollToEnd",
            Type = "ValueTask ScrollToEnd(bool? smooth = null)",
            Description = "Scrolls the pane to the end of its content, both horizontally and vertically, which on the horizontal axis is the visual left edge of a right-to-left pane.",
        },
        new()
        {
            Name = "ScrollToStart",
            Type = "ValueTask ScrollToStart(bool? smooth = null)",
            Description = "Scrolls the pane back to the start of its content, both horizontally and vertically, which on the horizontal axis is the visual right edge of a right-to-left pane.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "scroll-offset-class",
            Title = "BitScrollOffset",
            Description = "Where a pane stands, as measured in the browser. Everything is in CSS pixels.",
            Parameters =
            [
                new()
                {
                    Name = "Left",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The raw scrollLeft of the pane, so in a right-to-left pane it starts at 0 at the right edge and runs negative."
                },
                new()
                {
                    Name = "Top",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "How far the content has been scrolled down."
                },
                new()
                {
                    Name = "ScrollWidth",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The full width of the content, including the part of it that is scrolled out of sight."
                },
                new()
                {
                    Name = "ScrollHeight",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The full height of the content, including the part of it that is scrolled out of sight."
                },
                new()
                {
                    Name = "ClientWidth",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The width of the visible area of the pane, without its scrollbar."
                },
                new()
                {
                    Name = "ClientHeight",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The height of the visible area of the pane, without its scrollbar."
                },
                new()
                {
                    Name = "Rtl",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the pane was laid out right to left when it was measured, which is what tells the two readings of a Left of 0 apart."
                },
                new()
                {
                    Name = "DeltaLeft",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "How far the pane moved sideways since the position before this one was reported, positive rightwards on the screen. Only the reports OnScroll makes carry it."
                },
                new()
                {
                    Name = "DeltaTop",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "How far the pane moved up or down since the position before this one was reported, positive downwards. Only the reports OnScroll makes carry it."
                },
                new()
                {
                    Name = "OffsetLeft",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The distance from the visual left edge of the content, which is Left made positive and direction independent."
                },
                new()
                {
                    Name = "MaxLeft",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The largest horizontal offset the pane can reach."
                },
                new()
                {
                    Name = "MaxTop",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The largest vertical offset the pane can reach."
                },
                new()
                {
                    Name = "ScrollableX",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the content is wider than the pane."
                },
                new()
                {
                    Name = "ScrollableY",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the content is taller than the pane."
                },
                new()
                {
                    Name = "AtLeft",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the pane is standing at the visual left edge of its content."
                },
                new()
                {
                    Name = "AtRight",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the pane is standing at the visual right edge of its content."
                },
                new()
                {
                    Name = "AtTop",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the pane is standing at the top of its content."
                },
                new()
                {
                    Name = "AtBottom",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the pane is standing at the bottom of its content."
                },
                new()
                {
                    Name = "PercentX",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "How far the pane has been scrolled sideways, from 0 at the visual left edge to 1 at the right one."
                },
                new()
                {
                    Name = "PercentY",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "How far the pane has been scrolled down, from 0 at the top to 1 at the bottom."
                },
                new()
                {
                    Name = "ScrollingDown",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the move this report carries was downwards, which is what a header that folds away on the way down reads."
                },
                new()
                {
                    Name = "ScrollingUp",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the move this report carries was upwards."
                },
                new()
                {
                    Name = "ScrollingRight",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the move this report carries was to the right on the screen, whichever way the pane reads."
                },
                new()
                {
                    Name = "ScrollingLeft",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the move this report carries was to the left on the screen, whichever way the pane reads."
                },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "scroll-alignment-enum",
            Name = "BitScrollAlignment",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Start",
                    Value = "0",
                    Description = "The element is brought to the start of the pane: its top edge to the top of the pane, and its leading edge to the leading edge of the pane."
                },
                new()
                {
                    Name = "Center",
                    Value = "1",
                    Description = "The element is centered in the pane along both axes."
                },
                new()
                {
                    Name = "End",
                    Value = "2",
                    Description = "The element is brought to the end of the pane: its bottom edge to the bottom of the pane, and its trailing edge to the trailing edge of the pane."
                },
                new()
                {
                    Name = "Nearest",
                    Value = "3",
                    Description = "The pane moves as little as it can: an element that is already fully in view is not moved to at all, and one that is not is brought to whichever edge it is nearest."
                }
            ]
        },
        new()
        {
            Id = "scroll-snap-enum",
            Name = "BitScrollSnap",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "None",
                    Value = "0",
                    Description = "The pane does not snap, which is the initial value."
                },
                new()
                {
                    Name = "Proximity",
                    Value = "1",
                    Description = "The pane snaps to a position only when it comes to rest near one, so a scroll can still be left anywhere between two items."
                },
                new()
                {
                    Name = "Mandatory",
                    Value = "2",
                    Description = "The pane always comes to rest on a snap position, which is what a carousel or a row of pages wants."
                }
            ]
        },
        new()
        {
            Id = "scroll-snap-align-enum",
            Name = "BitScrollSnapAlign",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "None",
                    Value = "0",
                    Description = "The children carry no snap position of their own."
                },
                new()
                {
                    Name = "Start",
                    Value = "1",
                    Description = "Each child comes to rest at the start of the pane."
                },
                new()
                {
                    Name = "Center",
                    Value = "2",
                    Description = "Each child comes to rest in the middle of the pane."
                },
                new()
                {
                    Name = "End",
                    Value = "3",
                    Description = "Each child comes to rest at the end of the pane."
                }
            ]
        },
        new()
        {
            Id = "overflow-enum",
            Name = "BitOverflow",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Auto",
                    Value = "0",
                    Description = "Scrollbars are displayed automatically when needed based on the content size, and hidden when not needed."
                },
                new()
                {
                    Name = "Hidden",
                    Value = "1",
                    Description = "Scrollbars are always hidden, even if the content overflows the visible area."
                },
                new()
                {
                    Name = "Scroll",
                    Value = "2",
                    Description = "Scrollbars are always visible, allowing users to scroll through the content even if it doesn't overflow the visible area."
                },
                new()
                {
                    Name = "Visible",
                    Value = "3",
                    Description = "Overflow content is not clipped and may be visible outside the element's padding box."
                }
            ]
        },
        new()
        {
            Id = "overscroll-enum",
            Name = "BitOverscroll",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Auto",
                    Value = "0",
                    Description = "The initial value: the scroll carries on into the nearest scrolling ancestor once the pane has reached its edge, and the platform's own overscroll affordance is kept."
                },
                new()
                {
                    Name = "Contain",
                    Value = "1",
                    Description = "The scroll stops at the edge of the pane instead of carrying on into the page behind it, while the platform's own overscroll affordance inside the pane is kept."
                },
                new()
                {
                    Name = "None",
                    Value = "2",
                    Description = "Like Contain, and the platform's own overscroll affordance is suppressed as well, so the pane neither bounces nor triggers a pull to refresh at its edges."
                }
            ]
        },
        new()
        {
            Id = "scrollbar-gutter-enum",
            Name = "BitScrollbarGutter",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Auto",
                    Value = "0",
                    Description = "The initial value. Classic scrollbars create a gutter when overflow is scroll, or when overflow is auto and the box is overflowing. Overlay scrollbars do not consume space."
                },
                new()
                {
                    Name = "Stable",
                    Value = "1",
                    Description = "When using classic scrollbars, the gutter will be present if overflow is auto, scroll, or hidden even if the box is not overflowing. When using overlay scrollbars, the gutter will not be present."
                },
                new()
                {
                    Name = "BothEdges",
                    Value = "2",
                    Description = "If a gutter would be present on one of the inline start/end edges of the box, another will be present on the opposite edge as well."
                }
            ]
        },
        new()
        {
            Id = "scrollbar-width-enum",
            Name = "BitScrollbarWidth",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Auto",
                    Value = "0",
                    Description = "The default scrollbar width for the platform."
                },
                new()
                {
                    Name = "Thin",
                    Value = "1",
                    Description = "A thin scrollbar width variant on platforms that provide that option, or a thinner scrollbar than the default platform scrollbar width."
                },
                new()
                {
                    Name = "None",
                    Value = "2",
                    Description = "No scrollbar shown, however the element will still be scrollable."
                }
            ]
        }
    ];



    private int maxHeightLines = 2;

    private bool noScroll;
    private double overflowItemsCount = 6;
    private BitOverflow overflow;

    private BitOverscroll overscroll = BitOverscroll.Contain;

    private double gutterItemsCount = 6;
    private BitScrollbarGutter gutter;

    private bool autoHideScrollbar = true;

    private bool fade = true;
    private double fadeSize = 2;

    private double scrollThrottle;
    private string scrollState = "-";
    private string scrollDirection = "-";
    private BitScrollOffset? scrollOffset;
    private void HandleScroll(BitScrollOffset offset)
    {
        scrollOffset = offset;

        // A report that carries no move of its own - the first one, or one the pane's own size changed -
        // leaves the direction where it was rather than blanking it out.
        if (offset.ScrollingDown) scrollDirection = $"down ({offset.DeltaTop:0.#}px)";
        else if (offset.ScrollingUp) scrollDirection = $"up ({-offset.DeltaTop:0.#}px)";

        StateHasChanged();
    }
    private void HandleScrollStart() => scrollState = "scrolling...";
    private void HandleScrollEnd(BitScrollOffset offset) => scrollState = $"stopped at {offset.Top:0} ({offset.PercentY * 100:0}%)";

    private bool loadingMore;
    private double reachOffset = 40;
    private string reachedEdge = "-";
    private readonly List<string> endlessRows = [.. Enumerable.Range(1, 12).Select(i => $"Row {i}")];
    private void HandleReachedTop() => reachedEdge = "top";
    private async Task LoadMoreRows()
    {
        reachedEdge = "bottom";

        if (loadingMore || endlessRows.Count >= 60) return;

        loadingMore = true;
        StateHasChanged();

        await Task.Delay(600);

        var next = endlessRows.Count;
        endlessRows.AddRange(Enumerable.Range(next + 1, 12).Select(i => $"Row {i}"));

        loadingMore = false;
        StateHasChanged();
    }

    private bool smooth = true;
    private string readPosition = "-";
    private BitScrollablePane? scrollablePane;
    private async Task ReadScrollOffset()
    {
        if (scrollablePane is null) return;

        var offset = await scrollablePane.GetScrollOffset();

        readPosition = offset is null
            ? "-"
            : $"Top {offset.Top:0} of {offset.MaxTop:0}, at the bottom: {offset.AtBottom}";
    }

    private bool autoScrollRunning;
    private double autoScrollThreshold;
    private readonly List<string> autoScrollLines = [];
    private async Task AddAutoScrollContent()
    {
        autoScrollRunning = true;

        try
        {
            for (var i = 0; i < 15; i++)
            {
                await Task.Delay(700);

                autoScrollLines.Add($"A new line arrived at {DateTime.Now:HH:mm:ss} ({Random.Shared.Next(1, 100)})");

                StateHasChanged();
            }
        }
        finally
        {
            autoScrollRunning = false;
        }
    }

    private bool preserveScroll = true;
    private bool loadingOlder;
    private int oldestMessage = 1;
    private readonly List<string> conversation = [.. Enumerable.Range(1, 14).Select(i => $"Message {i}")];
    private async Task LoadOlderMessages()
    {
        if (loadingOlder || oldestMessage <= -40) return;

        loadingOlder = true;
        StateHasChanged();

        await Task.Delay(500);

        // The older messages go in at the TOP, which is what pushes everything the reader was looking at
        // down the screen unless the pane keeps their place for them.
        conversation.InsertRange(0, Enumerable.Range(oldestMessage - 8, 8).Select(i => $"Message {i}"));
        oldestMessage -= 8;

        loadingOlder = false;
        StateHasChanged();
    }

    private bool focusable = true;

    private bool snapStop = true;
    private BitScrollSnap snap = BitScrollSnap.Mandatory;
    private BitScrollSnapAlign snapAlign = BitScrollSnapAlign.Start;

    private bool dragScroll = true;
    private bool dragMomentum = true;
    private bool horizontalWheel = true;
}
