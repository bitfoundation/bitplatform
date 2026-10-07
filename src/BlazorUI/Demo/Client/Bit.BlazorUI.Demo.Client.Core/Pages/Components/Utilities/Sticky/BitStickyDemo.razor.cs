namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Sticky;

public partial class BitStickyDemo
{
    private bool isStuck;
    private bool isStickyDisabled;
    private bool reservesScrollPadding = true;
    private BitStickyEdges stuckEdges;
    private BitPlacement verticalPosition = BitPlacement.TopAndBottom;
    private BitPlacement horizontalPosition = BitPlacement.StartAndEnd;

    private record Person(string Name, string Role, string KnownFor, int Born);

    private readonly Person[] people =
    [
        new("Ada Lovelace", "Mathematician", "The first published algorithm", 1815),
        new("Grace Hopper", "Rear Admiral", "The first compiler", 1906),
        new("Alan Turing", "Cryptanalyst", "The Turing machine", 1912),
        new("Katherine Johnson", "Physicist", "Orbital mechanics at NASA", 1918),
        new("Barbara Liskov", "Computer Scientist", "The substitution principle", 1939),
        new("Donald Knuth", "Author", "The Art of Computer Programming", 1938),
        new("Edsger Dijkstra", "Computer Scientist", "The shortest-path algorithm", 1930),
        new("Margaret Hamilton", "Software Engineer", "The Apollo flight software", 1936),
    ];

    private readonly BitStickyParams[] stickyParams =
    [
        new() { ElevateOnStuck = true, Top = "0.5rem" }
    ];

    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Bottom",
            Type = "string?",
            DefaultValue = "null",
            Description = "The vertical offset the element pins at from the bottom edge. A bare number is read as a pixel count; anything else is used as written, so any CSS length is accepted."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Sticky, it can be any custom tag or text."
        },
        new()
        {
            Name = "Element",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom html element used for the root node, which is a div by default - a header, a footer, a nav, an aside or a th is what names the sticky region for assistive technologies. A name that is not one a tag can have falls back to the default."
        },
        new()
        {
            Name = "ElevateOnStuck",
            Type = "bool",
            DefaultValue = "false",
            Description = "Gives the element a surface and the theme's shadow only while it is stuck, cast away from the edge holding it (the app-bar shadow on the top and bottom, the card shadow on the sides). A background from Class or Style still wins; forced colors mode draws an outline instead. Attaches the stuck detection."
        },
        new()
        {
            Name = "Left",
            Type = "string?",
            DefaultValue = "null",
            Description = "The horizontal offset the element pins at from the left edge, for a container that scrolls horizontally. A bare number is read as a pixel count; any CSS length is accepted."
        },
        new()
        {
            Name = "OnStuckChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "Callback for when the stuck state changes: true while the element is pinned to an edge of its scrolling container. Using it (or OnStuckEdgesChanged, StuckClass, StuckStyle or ElevateOnStuck) attaches the stuck detection."
        },
        new()
        {
            Name = "OnStuckEdgesChanged",
            Type = "EventCallback<BitStickyEdges>",
            DefaultValue = "",
            Description = "Callback for when the set of edges the element is pinned to changes. Unlike OnStuckChanged it also reports the move from one edge of a pair to the other, which never flips the boolean.",
            Href = "#sticky-edges-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Placement",
            Type = "BitPlacement?",
            DefaultValue = "null",
            Description = "The edge of the scrolling container the element pins to. Start and End follow the reading direction. Left, Right and Center are not honoured and fall back to the default: when neither a Placement nor any offset is set, the component sticks to the top.",
            Href = "#placement-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Right",
            Type = "string?",
            DefaultValue = "null",
            Description = "The horizontal offset the element pins at from the right edge, for a container that scrolls horizontally. A bare number is read as a pixel count; any CSS length is accepted."
        },
        new()
        {
            Name = "ScrollPadding",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reserves the room the element covers (its offset plus its size) as the scroll padding of its scrolling container on every edge it can pin to, so nothing the browser scrolls into view - the focused control, an anchor - lands underneath it (WCAG 2.4.11). Stickies of one container keep the largest claim per edge, and a larger scroll padding the container already has is kept; released while disabled."
        },
        new()
        {
            Name = "StuckClass",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS class applied to the root element only while the component is stuck - a shadow, an opaque background, a border once content passes underneath. The bit-stk-stc class and one naming each pinned edge accompany it."
        },
        new()
        {
            Name = "StuckStyle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS style applied to the root element only while the component is stuck, the inline counterpart of StuckClass."
        },
        new()
        {
            Name = "Top",
            Type = "string?",
            DefaultValue = "null",
            Description = "The vertical offset the element pins at from the top edge. A bare number is read as a pixel count; anything else is used as written, so any CSS length is accepted."
        },
        new()
        {
            Name = "ZIndex",
            Type = "int?",
            DefaultValue = "null",
            Description = "The z-index of the root element. When not set, the component keeps a z-index of 1 - enough to stay above the plain flowing content it sticks over without covering popups and overlays. The --bit-Sticky-z-index variable sets the same default from a stylesheet."
        }
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Sticky-z-index",
            DefaultValue = "1",
            Description = "Stacking order of the element. The ZIndex parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Sticky-offset-top",
            DefaultValue = "0",
            Description = "Gap a Top or TopAndBottom sticky (and one with no Placement or offset) keeps from the top edge - e.g. the height of a fixed app bar above the container. The Top parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Sticky-offset-bottom",
            DefaultValue = "0",
            Description = "Gap a Bottom or TopAndBottom sticky keeps from the bottom edge. The Bottom parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Sticky-offset-start",
            DefaultValue = "0",
            Description = "Gap a Start or StartAndEnd sticky keeps from the start edge (left in LTR, right in RTL). The Left and Right parameters win over it.",
        },
        new()
        {
            Name = "--bit-Sticky-offset-end",
            DefaultValue = "0",
            Description = "Gap an End or StartAndEnd sticky keeps from the end edge. The Left and Right parameters win over it.",
        },
        new()
        {
            Name = "--bit-Sticky-background",
            DefaultValue = "var(--bit-clr-bg-pri)",
            Description = "Surface of an ElevateOnStuck sticky while stuck. A background from Class or Style wins over it.",
        },
        new()
        {
            Name = "--bit-Sticky-shadow-top",
            DefaultValue = "var(--bit-shd-appbar-top)",
            Description = "Shadow of an ElevateOnStuck sticky pinned to the top edge.",
        },
        new()
        {
            Name = "--bit-Sticky-shadow-bottom",
            DefaultValue = "var(--bit-shd-appbar-bottom)",
            Description = "Shadow of an ElevateOnStuck sticky pinned to the bottom edge.",
        },
        new()
        {
            Name = "--bit-Sticky-shadow-left",
            DefaultValue = "var(--bit-shd-card)",
            Description = "Shadow of an ElevateOnStuck sticky pinned to the left edge.",
        },
        new()
        {
            Name = "--bit-Sticky-shadow-right",
            DefaultValue = "var(--bit-shd-card)",
            Description = "Shadow of an ElevateOnStuck sticky pinned to the right edge.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "IsStuck",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the component is currently stuck to an edge of its scrolling container. Always false unless one of OnStuckChanged, OnStuckEdgesChanged, StuckClass, StuckStyle or ElevateOnStuck is used, since those are what attach the stuck detection."
        },
        new()
        {
            Name = "StuckEdges",
            Type = "BitStickyEdges",
            DefaultValue = "BitStickyEdges.None",
            Description = "The edges of the scrolling container the component is currently pinned to. This is IsStuck with the edges named, and it carries both of them while the element is pinned into a corner.",
            Href = "#sticky-edges-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "RefreshAsync",
            Type = "ValueTask",
            DefaultValue = "",
            Description = "Reads the stuck state and the scroll padding claim again, along with everything they are derived from. Both settle themselves on every scroll, on every resize of the element, its parent, the container or the page, and on a change of Position, the offsets, Dir, Class or Style, so this is only for a change none of those can see - content moved around inside the container, a stylesheet or a --bit-Sticky-offset-* variable changed."
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        DemoSharedEnums.BitPlacement(),
        new()
        {
            Id = "sticky-edges-enum",
            Name = "BitStickyEdges",
            Description = "The edges of the scrolling container a BitSticky is currently pinned to. These are the physical edges the way the browser resolves them, so a Start sticky reports Left in an LTR container and Right in an RTL one, and more than one of them is set while the element is pinned into a corner.",
            Items =
            [
                new()
                {
                    Name = "None",
                    Value = "0",
                    Description = "The element is not pinned: it is travelling with the content of its scrolling container."
                },
                new()
                {
                    Name = "Top",
                    Value = "1",
                    Description = "The element is pinned to the top edge of its scrolling container."
                },
                new()
                {
                    Name = "Bottom",
                    Value = "2",
                    Description = "The element is pinned to the bottom edge of its scrolling container."
                },
                new()
                {
                    Name = "Left",
                    Value = "4",
                    Description = "The element is pinned to the left edge of its scrolling container."
                },
                new()
                {
                    Name = "Right",
                    Value = "8",
                    Description = "The element is pinned to the right edge of its scrolling container."
                }
            ]
        }
    ];
}
