namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AppShell;

public partial class BitAppShellDemo
{
    private bool noInsets;
    private bool noTopInset;
    private bool noEndInset;
    private bool noStartInset;
    private bool noBottomInset;

    private bool noScroll;
    private bool instantScroll;

    private bool stableGutter;
    private bool clipOverflowX;
    private bool shortOverflowPage;

    private bool stickyPadding;
    private string? paddingValue => stickyPadding ? "2.5rem 0 0 0" : null;

    private bool autoScroll = true;
    private bool preserveScroll = true;
    private int feedNext = 13;
    private int feedOlder;
    private readonly List<string> feed = [.. Enumerable.Range(1, 12).Select(i => $"Message {i}")];

    private string? message;
    private double keyboardInset;

    private BitAppShell? feedShell;
    private BitAppShell? clipShell;
    private BitAppShell? scrollShell;
    private BitAppShell? paddingShell;
    private BitAppShell? behaviorShell;

    private string offsetText = "-";

    private double scrollTop;
    private double scrollPercent;
    private string scrollPhase = "idle";
    private string reachedEdge = "-";
    private string scrollDirection = "-";

    private string userName = "Saleh Yusefnejad";

    // The four bars are 0 tall on a desktop browser, so the example gives them a size and a color of their
    // own - which is also the shortest demonstration of what Styles reaches.
    private readonly BitAppShellClassStyles insetStyles = new()
    {
        Top = "height:1.5rem;background:#0d7bbd",
        Bottom = "height:1.5rem;background:#0d7bbd",
        Left = "width:1rem;background:#7a3fb5",
        Right = "width:1rem;background:#7a3fb5",
    };

    private readonly BitAppShellClassStyles shellStyles = new()
    {
        Root = "border-radius:0.5rem;overflow:hidden",
        Top = "height:0.5rem;background:#3a9b3a",
        Bottom = "height:0.5rem;background:#3a9b3a",
        Main = "padding:0.75rem",
    };

    private readonly BitAppShellClassStyles shellClasses = new()
    {
        Main = "styled-main",
    };

    private IEnumerable<BitCascadingValue> cascadingValues =>
    [
        new(new AppShellDemoUser(userName, "Developer")),
        new("bit platform", "Tenant"),
    ];

    private void RenameUser()
    {
        userName = userName == "Saleh Yusefnejad" ? "Yaser Moradi" : "Saleh Yusefnejad";
    }

    private async Task ReadOffset()
    {
        var offset = await (scrollShell?.GetScrollOffset() ?? Task.FromResult<BitScrollOffset?>(null));

        offsetText = offset is null
            ? "-"
            : $"Top {offset.Top:0} of {offset.MaxTop:0} ({offset.PercentY * 100:0}%), AtTop: {offset.AtTop}, AtBottom: {offset.AtBottom}";
    }

    private Task ScrollToTarget() => scrollShell?.ScrollToElement("target-row") ?? Task.CompletedTask;

    private void HandleScroll(BitScrollOffset offset)
    {
        scrollTop = offset.Top;
        scrollPercent = offset.PercentY;
        scrollDirection = offset.ScrollingDown ? "down" : offset.ScrollingUp ? "up" : "-";

        StateHasChanged();
    }

    private void HandleScrollStart() { scrollPhase = "scrolling"; StateHasChanged(); }

    private void HandleScrollEnd() { scrollPhase = "idle"; StateHasChanged(); }

    private void HandleReachedTop() { reachedEdge = "top"; StateHasChanged(); }

    private void HandleReachedBottom() { reachedEdge = "bottom"; StateHasChanged(); }

    private void HandleKeyboardInset(double inset) { keyboardInset = inset; StateHasChanged(); }

    private Task ScrollToPaddedRow() => paddingShell?.ScrollToElement("padded-row") ?? Task.CompletedTask;

    private void AppendMessage() => feed.Add($"Message {feedNext++}");

    // Older content lands ABOVE what the reader is looking at, which is the arrival PreserveScroll is for.
    private void PrependMessages()
    {
        for (var i = 0; i < 5; i++)
        {
            feed.Insert(0, $"Older message {--feedOlder}");
        }
    }



    private readonly List<ComponentParameter> componentParameters =
    [
         new()
         {
            Name = "AutoGoToTop",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables auto-scroll to the top of the main container on navigation. A navigation that only changes the fragment of the url (an in-page anchor) is left alone. PersistScroll takes precedence over it.",
         },
         new()
         {
            Name = "AutoScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the main container pinned to the end of its content as the content grows, for as long as the reader left it standing at the end.",
         },
         new()
         {
            Name = "AutoScrollThreshold",
            Type = "int",
            DefaultValue = "0",
            Description = "How near the end of the content (in pixels) the main container has to have been left for AutoScroll to keep pinning it there.",
         },
         new()
         {
            Name = "AvoidKeyboard",
            Type = "bool",
            DefaultValue = "false",
            Description = "Takes the height of the on-screen keyboard off the scrolling area while it is open, publishes it on the root as the --bit-ash-keyboard-inset CSS variable and marks the root with the data-bit-ash-keyboard attribute. It measures 0 wherever the browser shrinks the layout viewport itself.",
         },
         new()
         {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the app shell. It is rendered inside the main (scrolling) container.",
         },
         new()
         {
            Name = "Classes",
            Type = "BitAppShellClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the app shell.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
         },
         new()
         {
            Name = "FullScreen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Pins the app shell to the four edges of the screen, so it fills the window whatever height the page around it has - which is what saves the host page from carrying a height of its own down through html and body.",
         },
         new()
         {
            Name = "Gutter",
            Type = "BitScrollbarGutter?",
            DefaultValue = "null",
            Description = "Reserves the room the scrollbar of the main container takes, whether or not there is anything left to scroll, so the layout does not shift between a page that scrolls and a page that does not.",
            LinkType = LinkType.Link,
            Href = "#scrollbar-gutter-enum"
         },
         new()
         {
            Name = "NoBottomInset",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the bottom safe area inset of the app shell, leaving the other three where they are.",
         },
         new()
         {
            Name = "NoEndInset",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the trailing side safe area inset of the app shell - the right of a left-to-right shell - leaving the other three where they are.",
         },
         new()
         {
            Name = "NoInsets",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the safe area insets, so the four edges of the app shell are not inset at all and the content fills the whole screen.",
         },
         new()
         {
            Name = "NoScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Prevents the reader from scrolling the main container at all; the content that overflows is clipped. The scrolling methods of the component still move it.",
         },
         new()
         {
            Name = "NoStartInset",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the leading side safe area inset of the app shell - the left of a left-to-right shell - leaving the other three where they are.",
         },
         new()
         {
            Name = "NoTopInset",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the top safe area inset of the app shell, leaving the other three where they are.",
         },
         new()
         {
            Name = "OnKeyboardInsetChanged",
            Type = "EventCallback<double>",
            DefaultValue = "",
            Description = "Callback for how much of the app shell the on-screen keyboard covers, in pixels, raised as that changes and with 0 as it closes. Only a shell with AvoidKeyboard set measures it at all.",
         },
         new()
         {
            Name = "OnReachedBottom",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Callback for when the main container reaches the bottom of its content, raised once per arrival rather than on every frame that stays there.",
         },
         new()
         {
            Name = "OnReachedLeft",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Callback for when the main container reaches the visual left edge of its content, which is the same edge whichever way the shell reads.",
         },
         new()
         {
            Name = "OnReachedRight",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Callback for when the main container reaches the visual right edge of its content.",
         },
         new()
         {
            Name = "OnReachedTop",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Callback for when the main container reaches the top of its content.",
         },
         new()
         {
            Name = "OnScroll",
            Type = "EventCallback<BitScrollOffset>",
            DefaultValue = "",
            Description = "Callback for the scroll position of the main container, raised as it is scrolled. Nothing is measured or reported until one of the scroll callbacks is handled.",
            LinkType = LinkType.Link,
            Href = "#scroll-offset"
         },
         new()
         {
            Name = "OnScrollEnd",
            Type = "EventCallback<BitScrollOffset>",
            DefaultValue = "",
            Description = "Callback for when a scroll of the main container comes to a stop.",
            LinkType = LinkType.Link,
            Href = "#scroll-offset"
         },
         new()
         {
            Name = "OnScrollStart",
            Type = "EventCallback<BitScrollOffset>",
            DefaultValue = "",
            Description = "Callback for when a scroll of the main container begins.",
            LinkType = LinkType.Link,
            Href = "#scroll-offset"
         },
         new()
         {
            Name = "OverflowX",
            Type = "BitOverflow?",
            DefaultValue = "null",
            Description = "What the main container does with content that overflows it sideways. Hidden clips it instead of offering it, and NoScroll wins over both axes.",
            LinkType = LinkType.Link,
            Href = "#overflow-enum"
         },
         new()
         {
            Name = "OverflowY",
            Type = "BitOverflow?",
            DefaultValue = "null",
            Description = "What the main container does with content that overflows it downwards. See OverflowX.",
            LinkType = LinkType.Link,
            Href = "#overflow-enum"
         },
         new()
         {
            Name = "Overscroll",
            Type = "BitOverscroll?",
            DefaultValue = "null",
            Description = "Determines what happens when the main container is scrolled past its edge. It defaults to None: no scroll chaining out of the shell and no pull-to-refresh or rubber-banding.",
            LinkType = LinkType.Link,
            Href = "#overscroll-enum"
         },
         new()
         {
            Name = "PersistScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Persists scroll position of the main container per url in session storage and restores it on navigation. A fragment-only navigation is left alone.",
         },
         new()
         {
            Name = "PreserveScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the place of the reader when content is added above what they are looking at, which is what an endless list growing upwards needs.",
         },
         new()
         {
            Name = "ReachOffset",
            Type = "int",
            DefaultValue = "0",
            Description = "How near an edge (in pixels) counts as having reached it, for OnReachedTop and OnReachedBottom.",
         },
         new()
         {
            Name = "ScrollBehavior",
            Type = "BitScrollBehavior?",
            DefaultValue = "null",
            Description = "The scroll behavior of the main container, which decides how every move the reader does not make by hand is animated. It defaults to Smooth, and is taken back off under the reduced motion preference.",
            LinkType = LinkType.Link,
            Href = "#scroll-behavior-enum"
         },
         new()
         {
            Name = "ScrollPadding",
            Type = "string?",
            DefaultValue = "null",
            Description = "The room the main container keeps between its edges and anything scrolled into view inside it, as any CSS length - which is what keeps a header stuck to the top of the shell from covering what was just scrolled to.",
         },
         new()
         {
            Name = "ScrollThrottle",
            Type = "int",
            DefaultValue = "0",
            Description = "The shortest interval (in milliseconds) between two OnScroll reports. The default of 0 reports once per animation frame.",
         },
         new()
         {
            Name = "StableInsets",
            Type = "bool",
            DefaultValue = "false",
            Description = "Sizes the four inset bars from the largest safe areas the device can ask for rather than from the ones it is asking for right now, so the layout is not relaid out as the browser slides its own chrome in and out.",
         },
         new()
         {
            Name = "Styles",
            Type = "BitAppShellClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the app shell.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
         },
         new()
         {
            Name = "ValueList",
            Type = "BitCascadingValueList?",
            DefaultValue = "null",
            Description = "The cascading value list to be provided for the children of the app shell. Its values are provided before (so they can be overridden by) the ones of the Values parameter.",
            LinkType = LinkType.Link,
            Href = "#cascading-value-list"
         },
         new()
         {
            Name = "Values",
            Type = "IEnumerable<BitCascadingValue>?",
            DefaultValue = "null",
            Description = "The cascading values to be provided for the children of the app shell.",
            LinkType = LinkType.Link,
            Href = "#cascading-value"
         },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "ClearPersistedScroll",
            Type = "Func<string?, Task>",
            DefaultValue = "",
            Description = "Forgets every scroll position PersistScroll has kept, for the pages of this app shell and of any other - or, given a url, only the position kept for that one page.",
        },
        new()
        {
            Name = "Container",
            Type = "const string",
            DefaultValue = "\"BitAppShell.Container\"",
            Description = "The name the app shell cascades the element of its main container under, which is what a Modal, a Panel, a Dialog or an Overlay inside the shell reads to hold the right scroller.",
        },
        new()
        {
            Name = "ContainerId",
            Type = "const string",
            DefaultValue = "\"BitAppShell-container\"",
            Description = "The id the main container carries when the app shell has no Id of its own.",
        },
        new()
        {
            Name = "ContainerRef",
            Type = "ElementReference?",
            DefaultValue = "null",
            Description = "The element reference to the main container of the app shell.",
        },
        new()
        {
            Name = "GetScrollOffset",
            Type = "Func<Task<BitScrollOffset?>>",
            DefaultValue = "",
            Description = "Reads where the main container currently stands, measured in the browser.",
            LinkType = LinkType.Link,
            Href = "#scroll-offset"
        },
        new()
        {
            Name = "GoToBottom",
            Type = "Func<BitScrollBehavior?, Task>",
            DefaultValue = "",
            Description = "Scrolls the main container to the bottom of its content.",
            LinkType = LinkType.Link,
            Href = "#scroll-behavior-enum"
        },
        new()
        {
            Name = "GoToTop",
            Type = "Func<BitScrollBehavior?, Task>",
            DefaultValue = "",
            Description = "Scrolls the main container to top.",
            LinkType = LinkType.Link,
            Href = "#scroll-behavior-enum"
        },
        new()
        {
            Name = "MainContainerId",
            Type = "string",
            DefaultValue = "",
            Description = "The id of the main container element of this app shell: ContainerId, or the Id of the shell with \"-container\" after it.",
        },
        new()
        {
            Name = "Refresh",
            Type = "Func<Task>",
            DefaultValue = "",
            Description = "Re-measures the main container and reports whatever has changed since it was last measured - for the changes neither its own size nor its content announce, such as a web font that has finished loading.",
        },
        new()
        {
            Name = "ScrollBy",
            Type = "Func<double, double, BitScrollBehavior?, Task>",
            DefaultValue = "",
            Description = "Scrolls the main container by an amount, from wherever it currently stands.",
            LinkType = LinkType.Link,
            Href = "#scroll-behavior-enum"
        },
        new()
        {
            Name = "ScrollTo",
            Type = "Func<double?, double?, BitScrollBehavior?, Task>",
            DefaultValue = "",
            Description = "Scrolls the main container to a position. A null axis is left where it stands.",
            LinkType = LinkType.Link,
            Href = "#scroll-behavior-enum"
        },
        new()
        {
            Name = "ScrollToElement",
            Type = "Func<string, double, bool, BitScrollAlignment, BitScrollBehavior?, Task>",
            DefaultValue = "",
            Description = "Brings an element inside the main container into view by scrolling the container itself rather than every scroller the page sits in.",
            LinkType = LinkType.Link,
            Href = "#scroll-alignment-enum"
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "cascading-value-list",
            Title = "BitCascadingValueList",
            Description = "A helper class to ease the using of a list of the BitCascadingValue.",
            Parameters =
            [
                new()
                {
                    Name = "Add<T>(T value, string? name = null, bool isFixed = false)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds a typed BitCascadingValue to the list.",
                }
            ]
        },
        new()
        {
            Id = "cascading-value",
            Title = "BitCascadingValue",
            Description = "The cascading value to be provided using the BitCascadingValueProvider component.",
            Parameters =
            [
                new()
                {
                    Name = "Name",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The optional name of the cascading value.",
                },
                new()
                {
                    Name = "Value",
                    Type = "object?",
                    DefaultValue = "null",
                    Description = "The value to be provided.",
                },
                new()
                {
                    Name = "IsFixed",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "If true, indicates that Value will not change.",
                }
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitAppShellClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root of the BitAppShell.",
                },
                new()
                {
                    Name = "Top",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the top inset bar of the BitAppShell.",
                },
                new()
                {
                    Name = "Center",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the center row of the BitAppShell, which holds the two side inset bars and the main container.",
                },
                new()
                {
                    Name = "Left",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the leading side inset bar of the BitAppShell.",
                },
                new()
                {
                    Name = "Main",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the main (scrolling) container of the BitAppShell.",
                },
                new()
                {
                    Name = "Right",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the trailing side inset bar of the BitAppShell.",
                },
                new()
                {
                    Name = "Bottom",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the bottom inset bar of the BitAppShell.",
                },
            ]
        },
        new()
        {
            Id = "scroll-offset",
            Title = "BitScrollOffset",
            Description = "Where the main container of the app shell stands, as measured in the browser. Everything is in CSS pixels; the derived members below the first six cost nothing to read.",
            Parameters =
            [
                new()
                {
                    Name = "Left",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The raw scrollLeft of the container.",
                },
                new()
                {
                    Name = "Top",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "How far the content has been scrolled down.",
                },
                new()
                {
                    Name = "ScrollWidth",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The full width of the content, including the part scrolled out of sight.",
                },
                new()
                {
                    Name = "ScrollHeight",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The full height of the content, including the part scrolled out of sight.",
                },
                new()
                {
                    Name = "ClientWidth",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The width of the visible area, without its scrollbar.",
                },
                new()
                {
                    Name = "ClientHeight",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "The height of the visible area, without its scrollbar.",
                },
                new()
                {
                    Name = "Rtl",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the container was laid out right to left when it was measured.",
                },
                new()
                {
                    Name = "DeltaLeft / DeltaTop",
                    Type = "double",
                    DefaultValue = "0",
                    Description = "How far the container has moved since the position before this one was reported. Only the OnScroll reports carry them.",
                },
                new()
                {
                    Name = "OffsetLeft",
                    Type = "double",
                    DefaultValue = "",
                    Description = "The distance from the visual left edge, which is Left made positive and direction independent.",
                },
                new()
                {
                    Name = "MaxLeft / MaxTop",
                    Type = "double",
                    DefaultValue = "",
                    Description = "The largest offset each axis can reach, which is how much of the content is out of sight.",
                },
                new()
                {
                    Name = "ScrollableX / ScrollableY",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "Whether there is anything to scroll along each axis at all.",
                },
                new()
                {
                    Name = "AtLeft / AtRight / AtTop / AtBottom",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "Whether the container is standing at each edge, within a pixel of slack.",
                },
                new()
                {
                    Name = "PercentX / PercentY",
                    Type = "double",
                    DefaultValue = "",
                    Description = "How far the container has been scrolled along each axis, from 0 to 1.",
                },
                new()
                {
                    Name = "ScrollingUp / ScrollingDown / ScrollingLeft / ScrollingRight",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "Which way the move this report carries went, derived from the deltas - which is what a header that folds away on the way down reads.",
                },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "scroll-behavior-enum",
            Name = "BitScrollBehavior",
            Description = "Determines whether scrolling is instant or animates smoothly.",
            Items =
            [
                new()
                {
                    Name= "Smooth",
                    Description="Scrolling should animate smoothly.",
                    Value="0",
                },
                new()
                {
                    Name= "Instant",
                    Description="Scrolling should happen instantly in a single jump.",
                    Value="1",
                },
                new()
                {
                    Name= "Auto",
                    Description="Scroll behavior is determined by the computed value of scroll-behavior.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "overflow-enum",
            Name = "BitOverflow",
            Description = "What the main container of the app shell does with content that overflows it along one axis.",
            Items =
            [
                new()
                {
                    Name= "Auto",
                    Description="A scrollbar is offered along that axis when the content overflows, and nothing is shown when it does not.",
                    Value="0",
                },
                new()
                {
                    Name= "Hidden",
                    Description="The overflow is clipped and no scrollbar is offered, though the axis can still be moved through the scrolling methods of the component.",
                    Value="1",
                },
                new()
                {
                    Name= "Scroll",
                    Description="A scrollbar is always shown along that axis, whether or not there is anything to scroll.",
                    Value="2",
                },
                new()
                {
                    Name= "Visible",
                    Description="The overflow is neither clipped nor scrollable, so it is painted outside the container.",
                    Value="3",
                }
            ]
        },
        new()
        {
            Id = "scrollbar-gutter-enum",
            Name = "BitScrollbarGutter",
            Description = "How much room the main container of the app shell reserves for its scrollbar.",
            Items =
            [
                new()
                {
                    Name= "Auto",
                    Description="The initial value: a classic scrollbar takes its room only while there is something to scroll, and an overlay scrollbar takes none at all.",
                    Value="0",
                },
                new()
                {
                    Name= "Stable",
                    Description="The room is reserved whether or not there is anything to scroll, so the layout does not shift as pages of different lengths follow one another.",
                    Value="1",
                },
                new()
                {
                    Name= "BothEdges",
                    Description="Like Stable, with the same room reserved on the opposite edge as well, so the content stays centered.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "overscroll-enum",
            Name = "BitOverscroll",
            Description = "What the browser does with a scroll that has already reached the edge of the main container.",
            Items =
            [
                new()
                {
                    Name= "Auto",
                    Description="The scroll carries on into the nearest scrolling ancestor, and the platform's own overscroll affordance is kept.",
                    Value="0",
                },
                new()
                {
                    Name= "Contain",
                    Description="The scroll stops at the edge instead of carrying on into the page behind it, while the platform's own overscroll affordance is kept.",
                    Value="1",
                },
                new()
                {
                    Name= "None",
                    Description="Like Contain, and the platform's own overscroll affordance is suppressed as well, so the container neither bounces nor triggers a pull to refresh.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "scroll-alignment-enum",
            Name = "BitScrollAlignment",
            Description = "Where inside the main container an element is left after ScrollToElement has brought it into view.",
            Items =
            [
                new()
                {
                    Name= "Start",
                    Description="The element is brought to the start of the container.",
                    Value="0",
                },
                new()
                {
                    Name= "Center",
                    Description="The element is centered in the container along both axes.",
                    Value="1",
                },
                new()
                {
                    Name= "End",
                    Description="The element is brought to the end of the container.",
                    Value="2",
                },
                new()
                {
                    Name= "Nearest",
                    Description="The container moves as little as it can.",
                    Value="3",
                }
            ]
        },
    ];
}
