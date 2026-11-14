using System.Diagnostics;
using System.Globalization;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ResourceTimeline;

public partial class BitResourceTimelineDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitResourceTimelineClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitResourceTimeline.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Config",
            Type = "BitResourceTimelineConfig?",
            DefaultValue = "null",
            Description = "The resources, the time range and the bars to render. Compared by reference: a Config changed in place needs a ReloadAsync.",
            LinkType = LinkType.Link,
            Href = "#timeline-config",
        },
        new()
        {
            Name = "LoadAllocationsAsync",
            Type = "Func<BitResourceTimelineWindow, Task<IReadOnlyList<BitResourceTimelineAllocation>>>?",
            DefaultValue = "null",
            Description = "On-demand loading: called with the window the view needs (the visible range plus a buffer) for every allocation overlapping it, and again as a scroll or zoom nears its edge. Calls can overlap - a newer one does not wait for an older one, whose result is dropped. Swapping one delegate for another reloads nothing until the next window is fetched (ReloadAsync fetches it at once). The Config still supplies the resources and the range. Null loads everything up front.",
            LinkType = LinkType.Link,
            Href = "#timeline-window",
        },
        new()
        {
            Name = "LoadBatchSize",
            Type = "int",
            DefaultValue = "10000",
            Description = "Allocations per interop call; a larger set is streamed in batches of this size. Zero or less sends everything at once.",
        },
        new()
        {
            Name = "LoadingContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom content of the loading overlay (a spinner and a text by default).",
        },
        new()
        {
            Name = "LoadingMinDurationMs",
            Type = "int",
            DefaultValue = "0",
            Description = "Minimum time in ms the loading overlay stays visible.",
        },
        new()
        {
            Name = "OnAllocationChanged",
            Type = "EventCallback<BitResourceTimelineAllocation>",
            DefaultValue = "",
            Description = "Raised after a bar is moved, resized or created, with the Config's own instance already updated - a handler only persists it.",
            LinkType = LinkType.Link,
            Href = "#timeline-allocation",
        },
        new()
        {
            Name = "OnAllocationChanging",
            Type = "Func<BitResourceTimelineAllocationChange, Task<bool>>?",
            DefaultValue = "null",
            Description = "Called before a single-bar move or resize is committed, with the bar already previewed; false restores it. Unset, the edit goes to OnAllocationsChanging (accepted when neither is set).",
            LinkType = LinkType.Link,
            Href = "#allocation-change",
        },
        new()
        {
            Name = "OnAllocationCreating",
            Type = "Func<BitResourceTimelineCreateRequest, Task<BitResourceTimelineAllocation?>>?",
            DefaultValue = "null",
            Description = "Called when a bar was drawn on empty content (EmptyDragAction.Create); returns the allocation with an Id of its own, or null to cancel.",
            LinkType = LinkType.Link,
            Href = "#create-request",
        },
        new()
        {
            Name = "OnAllocationsChanging",
            Type = "Func<IReadOnlyList<BitResourceTimelineAllocationChange>, Task<bool>>?",
            DefaultValue = "null",
            Description = "Called before a multi-bar commit (and a single-bar one while OnAllocationChanging is unset) with a change per bar; false restores them all.",
            LinkType = LinkType.Link,
            Href = "#allocation-change",
        },
        new()
        {
            Name = "OnAllocationsCopying",
            Type = "Func<BitResourceTimelineCopyRequest, Task<IReadOnlyList<BitResourceTimelineAllocation>?>>?",
            DefaultValue = "null",
            Description = "Called on paste (Ctrl/Cmd + V); returns the new allocations (with new ids), or null or empty to cancel.",
            LinkType = LinkType.Link,
            Href = "#copy-request",
        },
        new()
        {
            Name = "OnAllocationsDeleting",
            Type = "Func<IReadOnlyList<string>, Task<bool>>?",
            DefaultValue = "null",
            Description = "Called with the ids of the bars Delete/Backspace is about to remove (Options.AllowDelete); false keeps them.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<BitResourceTimelinePointerArgs>",
            DefaultValue = "",
            Description = "Raised on a click or tap: the bar, the +N marker's bars, the resource row and the time under the pointer, its surface and viewport coordinates and the modifier keys.",
            LinkType = LinkType.Link,
            Href = "#pointer-args",
        },
        new()
        {
            Name = "OnContextMenu",
            Type = "EventCallback<BitResourceTimelineContextMenuArgs>",
            DefaultValue = "",
            Description = "Raised on a right-click of a bar, empty content or a resource row, with the arguments of OnClick (the browser's menu is suppressed).",
            LinkType = LinkType.Link,
            Href = "#context-menu-args",
        },
        new()
        {
            Name = "OnDoubleClick",
            Type = "EventCallback<BitResourceTimelinePointerArgs>",
            DefaultValue = "",
            Description = "Raised on the second click of a double-click (or a double-tap), with the same arguments as OnClick, in addition to it.",
            LinkType = LinkType.Link,
            Href = "#pointer-args",
        },
        new()
        {
            Name = "OnResourceAxisWidthChanged",
            Type = "EventCallback<int>",
            DefaultValue = "",
            Description = "Raised with the new, clamped width once the user finishes resizing the resource column; pass it back as Options.ResourceAxisWidth to keep it.",
        },
        new()
        {
            Name = "OnSelectionChanged",
            Type = "EventCallback<BitResourceTimelineAllocation[]>",
            DefaultValue = "",
            Description = "Raised when the set of selected bars actually changes, with every selected bar in selection order (empty when nothing is selected) - the very instances of the Config.",
            LinkType = LinkType.Link,
            Href = "#timeline-allocation",
        },
        new()
        {
            Name = "OnViewChanged",
            Type = "EventCallback<BitResourceTimelineView>",
            DefaultValue = "",
            Description = "Raised at most once a frame when the visible span or the scale changes, for the chrome around the timeline.",
            LinkType = LinkType.Link,
            Href = "#timeline-view",
        },
        new()
        {
            Name = "Options",
            Type = "BitResourceTimelineOptions?",
            DefaultValue = "null",
            Description = "Dimensions, fonts, colors, time zone, zoom limits, editing and surface. A new instance is merged in: a property left null keeps the value in force (a default, or the theme's for colors and fonts).",
            LinkType = LinkType.Link,
            Href = "#timeline-options",
        },
        new()
        {
            Name = "ResourceTemplate",
            Type = "RenderFragment<BitResourceTimelineRowContext>?",
            DefaultValue = "null",
            Description = "Template of each row of the resource column, rendered in an overlay instead of the drawn labels; group rows get an expand/collapse button.",
            LinkType = LinkType.Link,
            Href = "#row-context",
        },
        new()
        {
            Name = "Styles",
            Type = "BitResourceTimelineClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitResourceTimeline.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Texts",
            Type = "BitResourceTimelineTexts?",
            DefaultValue = "null",
            Description = "The texts of the timeline: its accessible names, loading text, tooltip formats and announcements (English by default).",
            LinkType = LinkType.Link,
            Href = "#timeline-texts",
        },
        new()
        {
            Name = "TooltipTemplate",
            Type = "RenderFragment<BitResourceTimelineAllocation>?",
            DefaultValue = "null",
            Description = "Template of the hover tooltip, rendered in a positioned overlay instead of the built-in one (a +N marker keeps the built-in one). Needs Options.ShowTooltips.",
            LinkType = LinkType.Link,
            Href = "#timeline-allocation",
        },
        new()
        {
            Name = "TopStartContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom content of the top-start corner, where the two axes meet.",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-ResourceTimeline-content-background", DefaultValue = "var(--bit-clr-bg-pri)", Description = "Background of the scrollable content. Options.Colors.ContentBg wins over it." },
        new() { Name = "--bit-ResourceTimeline-axis-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Background of the time and resource axes and of their corner. Options.Colors.AxisBg wins over it." },
        new() { Name = "--bit-ResourceTimeline-axis-border-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Lines within and around the axes. Options.Colors.AxisBorder wins over it." },
        new() { Name = "--bit-ResourceTimeline-tick-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Hour ticks of the time axis. Options.Colors.Tick wins over it." },
        new() { Name = "--bit-ResourceTimeline-label-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Hour and resource labels. Options.Colors.Label wins over it." },
        new() { Name = "--bit-ResourceTimeline-date-label-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Date labels of the time axis. Options.Colors.DateLabel wins over it." },
        new() { Name = "--bit-ResourceTimeline-grid-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "Grid lines of the content. Options.Colors.Grid wins over it." },
        new() { Name = "--bit-ResourceTimeline-bar-color", DefaultValue = "var(--bit-clr-pri)", Description = "Fill of a bar that has no Color of its own. Options.Colors.Bar wins over it." },
        new() { Name = "--bit-ResourceTimeline-bar-selected-color", DefaultValue = "var(--bit-clr-pri-dark)", Description = "Fill of a selected bar that has no Color of its own. Options.Colors.BarSelected wins over it." },
        new() { Name = "--bit-ResourceTimeline-bar-selected-border-color", DefaultValue = "var(--bit-clr-pri-dark-active)", Description = "Outline of a selected bar. Options.Colors.BarSelectedBorder wins over it." },
        new() { Name = "--bit-ResourceTimeline-bar-label-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Labels around the bars. Options.Colors.BarLabel wins over it." },
        new() { Name = "--bit-ResourceTimeline-now-color", DefaultValue = "var(--bit-clr-err)", Description = "The current-time line. Options.Colors.Now wins over it." },
        new() { Name = "--bit-ResourceTimeline-selection-background", DefaultValue = "var(--bit-clr-pri-tint)", Description = "Fill of the marquee rectangle. Options.Colors.SelectionFill wins over it." },
        new() { Name = "--bit-ResourceTimeline-selection-border-color", DefaultValue = "var(--bit-clr-pri)", Description = "Outline of the marquee rectangle. Options.Colors.SelectionBorder wins over it." },
        new() { Name = "--bit-ResourceTimeline-focus-color", DefaultValue = "var(--bit-clr-pri-focus)", Description = "Focus indicator of the bars, of the viewport and of the column divider. Options.Colors.Focus wins over it for the bars." },
        new() { Name = "--bit-ResourceTimeline-tooltip-background", DefaultValue = "var(--bit-clr-tooltip-bg)", Description = "Fill of the built-in tooltip. Options.Colors.TooltipBg wins over it." },
        new() { Name = "--bit-ResourceTimeline-tooltip-color", DefaultValue = "var(--bit-clr-tooltip-fg)", Description = "Text of the built-in tooltip. Options.Colors.TooltipText wins over it." },
        new() { Name = "--bit-ResourceTimeline-non-working-background", DefaultValue = "var(--bit-clr-fg-pri) at 6%", Description = "Wash over the non-working days and hours. Options.Colors.NonWorking wins over it." },
        new() { Name = "--bit-ResourceTimeline-font-family", DefaultValue = "var(--bit-tpg-font-family)", Description = "Family of the labels the engine draws, while no font option sets them." },
        new() { Name = "--bit-ResourceTimeline-resource-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Text of the rows of the ResourceTemplate." },
        new() { Name = "--bit-ResourceTimeline-resource-font-size", DefaultValue = "var(--bit-tpg-fs-sm)", Description = "Text size of the rows of the ResourceTemplate." },
        new() { Name = "--bit-ResourceTimeline-toggle-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "The expand/collapse button of a group row of the ResourceTemplate." },
        new() { Name = "--bit-ResourceTimeline-toggle-hover-color", DefaultValue = "var(--bit-clr-pri)", Description = "That button under the pointer." },
        new() { Name = "--bit-ResourceTimeline-loading-background", DefaultValue = "var(--bit-clr-bg-pri) at 70%", Description = "The veil of the loading overlay." },
        new() { Name = "--bit-ResourceTimeline-loading-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "The text of the loading overlay." },
        new() { Name = "--bit-ResourceTimeline-spinner-size", DefaultValue = "spacing(4.5)", Description = "The default spinner of the loading overlay." },
        new() { Name = "--bit-ResourceTimeline-spinner-color", DefaultValue = "var(--bit-clr-pri)", Description = "The turning arc of that spinner." },
        new() { Name = "--bit-ResourceTimeline-spinner-track-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "The track of that spinner." },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "ClearSelectionAsync",
            Type = "Func<Task>",
            DefaultValue = "",
            Description = "Clears the selection of bars.",
        },
        new()
        {
            Name = "GetPixelsPerHourAsync",
            Type = "Func<Task<double>>",
            DefaultValue = "",
            Description = "Returns the current horizontal scale in pixels per hour (0 while the timeline is not ready).",
        },
        new()
        {
            Name = "GetSelectedBarsAsync",
            Type = "Func<Task<IReadOnlyList<BitResourceTimelineAllocation>>>",
            DefaultValue = "",
            Description = "Returns the selected bars in selection order; empty when nothing is selected.",
            LinkType = LinkType.Link,
            Href = "#timeline-allocation",
        },
        new()
        {
            Name = "GoToTodayAsync",
            Type = "Func<Task<bool>>",
            DefaultValue = "",
            Description = "Centers the current time; false when it is outside the time range.",
        },
        new()
        {
            Name = "PanByDaysAsync",
            Type = "Func<int, bool?, Task<bool>>",
            DefaultValue = "",
            Description = "Pans by whole days (negative back, 7 a week): exactly 24 hours, or to a local midnight with Options.PanToDayStart (panToDayStart overrides it). False when already at that end.",
        },
        new()
        {
            Name = "ReloadAsync",
            Type = "Func<Task>",
            DefaultValue = "",
            Description = "Sends the data again in full, after the Config was changed in place.",
        },
        new()
        {
            Name = "RemoveAllocationsAsync",
            Type = "Func<IReadOnlyList<string>, Task>",
            DefaultValue = "",
            Description = "Removes bars by id from the timeline, the selection and the focus; unknown ids are ignored. Config.Allocations is not changed.",
        },
        new()
        {
            Name = "ResetZoomAsync",
            Type = "Func<Task<double>>",
            DefaultValue = "",
            Description = "Returns to the automatic (or configured) scale, and returns the new scale in pixels per hour.",
        },
        new()
        {
            Name = "ScrollToAllocationAsync",
            Type = "Func<string, Task<bool>>",
            DefaultValue = "",
            Description = "Scrolls the bar's start and row into view; false for an unknown id.",
        },
        new()
        {
            Name = "ScrollToResourceAsync",
            Type = "Func<string, Task<bool>>",
            DefaultValue = "",
            Description = "Scrolls the row into view vertically; false for an unknown id.",
        },
        new()
        {
            Name = "ScrollToTimeAsync",
            Type = "Func<DateTimeOffset, Task<bool>>",
            DefaultValue = "",
            Description = "Centers the given time; false when it is outside the time range.",
        },
        new()
        {
            Name = "SelectAsync",
            Type = "Func<IReadOnlyList<string>, bool, Task>",
            DefaultValue = "",
            Description = "Sets the selection to the given allocation ids, or adds them to it when additive is true. Unknown ids are ignored.",
        },
        new()
        {
            Name = "SetPixelsPerHourAsync",
            Type = "Func<double?, Task<double>>",
            DefaultValue = "",
            Description = "Sets the scale in px per hour (clamped), or null for the automatic one; returns the new scale.",
        },
        new()
        {
            Name = "UpsertAllocationsAsync",
            Type = "Func<IReadOnlyList<BitResourceTimelineAllocation>, Task>",
            DefaultValue = "",
            Description = "Merges bars into the timeline by id (the last wins), keeping the selection and the focus. Config.Allocations is not changed.",
            LinkType = LinkType.Link,
            Href = "#timeline-allocation",
        },
        new()
        {
            Name = "ZoomInAsync",
            Type = "Func<Task<double>>",
            DefaultValue = "",
            Description = "Zooms in around the center; returns the new scale (0 while not ready).",
        },
        new()
        {
            Name = "ZoomOutAsync",
            Type = "Func<Task<double>>",
            DefaultValue = "",
            Description = "Zooms out around the center; returns the new scale (0 while not ready).",
        },
        new()
        {
            Name = "ZoomToDaysAsync",
            Type = "Func<double, Task<double>>",
            DefaultValue = "",
            Description = "Fits the given number of days into the content area around its center (clamped); returns the new scale.",
        },
    ];

    // The members of the click, double-click and right-click arguments, which BitResourceTimelineContextMenuArgs
    // inherits from BitResourceTimelinePointerArgs.
    private static readonly List<ComponentParameter> pointerArgsMembers =
    [
        new() { Name = "Allocation", Type = "BitResourceTimelineAllocation?", DefaultValue = "null", Description = "The bar under the pointer - the very instance of the Config - or null when no bar was hit (empty content, an axis, or a +N marker).", LinkType = LinkType.Link, Href = "#timeline-allocation" },
        new() { Name = "OverflowAllocations", Type = "IReadOnlyList<BitResourceTimelineAllocation>", DefaultValue = "[]", Description = "The hidden bars when the pointer hit the +N marker of a cluster; empty otherwise.", LinkType = LinkType.Link, Href = "#timeline-allocation" },
        new() { Name = "Resource", Type = "BitResourceTimelineResource?", DefaultValue = "null", Description = "The resource row under the pointer; null on the time axis, the corner, or below the last row.", LinkType = LinkType.Link, Href = "#timeline-resource" },
        new() { Name = "Time", Type = "DateTimeOffset?", DefaultValue = "null", Description = "The time at the horizontal position of the pointer; null on the resource axis and the corner, which have no time coordinate." },
        new() { Name = "Area", Type = "BitResourceTimelineHitArea", DefaultValue = "Content", Description = "The region of the timeline the pointer was in.", LinkType = LinkType.Link, Href = "#hit-area-enum" },
        new() { Name = "X", Type = "double", DefaultValue = "0", Description = "Pointer X in the coordinates of the timeline surface (origin at its top-left, the sticky axes included), independent of the page scroll." },
        new() { Name = "Y", Type = "double", DefaultValue = "0", Description = "Pointer Y in the coordinates of the timeline surface (origin at its top-left, the sticky axes included), independent of the page scroll." },
        new() { Name = "ClientX", Type = "double", DefaultValue = "0", Description = "Pointer X in viewport coordinates, suited for a position: fixed overlay." },
        new() { Name = "ClientY", Type = "double", DefaultValue = "0", Description = "Pointer Y in viewport coordinates, suited for a position: fixed overlay." },
        new() { Name = "CtrlKey", Type = "bool", DefaultValue = "false", Description = "Whether Ctrl was held." },
        new() { Name = "ShiftKey", Type = "bool", DefaultValue = "false", Description = "Whether Shift was held." },
        new() { Name = "MetaKey", Type = "bool", DefaultValue = "false", Description = "Whether Meta (Cmd on macOS, the Windows key elsewhere) was held." },
        new() { Name = "AltKey", Type = "bool", DefaultValue = "false", Description = "Whether Alt was held." },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "timeline-config",
            Title = "BitResourceTimelineConfig",
            Description = "Everything the timeline renders: the resource rows down the vertical axis, the time range along the horizontal axis and the allocation bars within it. Assign a new instance to the Config parameter to render it.",
            Parameters =
            [
                new() { Name = "Resources", Type = "List<BitResourceTimelineResource>", DefaultValue = "", Description = "The resource rows rendered along the vertical axis. Required.", LinkType = LinkType.Link, Href = "#timeline-resource" },
                new() { Name = "StartDate", Type = "DateTimeOffset", DefaultValue = "", Description = "The start of the time range rendered along the horizontal axis. Required." },
                new() { Name = "EndDate", Type = "DateTimeOffset", DefaultValue = "", Description = "The end of the time range rendered along the horizontal axis. Required." },
                new() { Name = "Allocations", Type = "List<BitResourceTimelineAllocation>", DefaultValue = "", Description = "The allocation bars to display (not read while LoadAllocationsAsync is set). Required.", LinkType = LinkType.Link, Href = "#timeline-allocation" },
            ]
        },
        new()
        {
            Id = "timeline-resource",
            Title = "BitResourceTimelineResource",
            Description = "A row of the timeline (a gate, a vehicle, a machine, a person). Resources nest into a hierarchy of any depth through ParentId: a resource with children renders as a collapsible group row, and can still own allocations of its own.",
            Parameters =
            [
                new() { Name = "Id", Type = "string", DefaultValue = "", Description = "The stable identifier the allocations are associated with. Required." },
                new() { Name = "Name", Type = "string", DefaultValue = "", Description = "The label of the row on the resource axis. Required." },
                new() { Name = "ParentId", Type = "string?", DefaultValue = "null", Description = "The Id of the parent resource. When set (and the parent exists), this resource is nested under it, forming a collapsible group; root resources leave it null. Sibling and root order follows the order of the list." },
                new() { Name = "Collapsed", Type = "bool", DefaultValue = "false", Description = "The initial state of a resource that has children: when true, its descendants start hidden until the group is expanded. Ignored for a resource without children." },
                new() { Name = "Background", Type = "string?", DefaultValue = "null", Description = "CSS color painted behind the row of this resource, under the grid and the bars. Wins over the row stripes and the ResourceColors of Options.Surface." },
                new() { Name = "LineColor", Type = "string?", DefaultValue = "null", Description = "CSS color of the horizontal line along the top of the row of this resource. Wins over the row lines and the ResourceColors of Options.AxisLines." },
            ]
        },
        new()
        {
            Id = "timeline-allocation",
            Title = "BitResourceTimelineAllocation",
            Description = "A single allocation period drawn as a bar on a resource row.",
            Parameters =
            [
                new() { Name = "Id", Type = "string", DefaultValue = "", Description = "The unique identifier of the bar. Required." },
                new() { Name = "ResourceId", Type = "string", DefaultValue = "", Description = "The Id of the resource the bar is drawn on. Required." },
                new() { Name = "StartTime", Type = "DateTimeOffset", DefaultValue = "", Description = "The start of the period." },
                new() { Name = "EndTime", Type = "DateTimeOffset", DefaultValue = "", Description = "The end of the period." },
                new() { Name = "Color", Type = "string?", DefaultValue = "null", Description = "CSS color of the bar fill (for example #e8590c or tomato). Falls back to the bar color of the theme, or Options.Colors.Bar." },
                new() { Name = "Height", Type = "int?", DefaultValue = "null", Description = "The height of the bar in pixels, vertically centered in its row. Falls back to Options.BarHeight. The edge bars share it." },
                new() { Name = "TextAbove", Type = "string?", DefaultValue = "null", Description = "A label centered above the bar." },
                new() { Name = "TextBelow", Type = "string?", DefaultValue = "null", Description = "A label centered below the bar." },
                new() { Name = "TextStart", Type = "string?", DefaultValue = "null", Description = "A label just before the start edge of the bar." },
                new() { Name = "TextEnd", Type = "string?", DefaultValue = "null", Description = "A label just after the end edge of the bar." },
                new() { Name = "Tooltip", Type = "string?", DefaultValue = "null", Description = "The hover text of the bar (requires Options.ShowTooltips), a line break separating its lines. When null, a default is built from the labels, the resource name and the time range." },
                new() { Name = "StartBar", Type = "BitResourceTimelineEdgeBar?", DefaultValue = "null", Description = "A decorative bar drawn immediately before the start edge, in the row and at the height of the bar - commonly a delay before the planned allocation began.", LinkType = LinkType.Link, Href = "#edge-bar" },
                new() { Name = "EndBar", Type = "BitResourceTimelineEdgeBar?", DefaultValue = "null", Description = "A decorative bar drawn immediately after the end edge, in the row and at the height of the bar - commonly a delay after the planned allocation ended.", LinkType = LinkType.Link, Href = "#edge-bar" },
                new() { Name = "Icons", Type = "List<BitResourceTimelineBarIcon>?", DefaultValue = "null", Description = "Icons or small images drawn around (or within) the bar, each anchored to its own position; several at one position line up away from the bar in list order.", LinkType = LinkType.Link, Href = "#bar-icon" },
                new() { Name = "Sections", Type = "List<BitResourceTimelineBarSection>?", DefaultValue = "null", Description = "Parts of the bar drawn with colors, text and an icon of their own, bounded by times or percentages and always within the bar, in list order.", LinkType = LinkType.Link, Href = "#bar-section" },
                new() { Name = "Data", Type = "JsonElement?", DefaultValue = "null", Description = "A payload of the app preserved on the instance; the engine never reads or paints it." },
                new() { Name = "Locked", Type = "bool", DefaultValue = "false", Description = "When true, the bar can be selected and shows its tooltip but cannot be moved or resized, by the pointer or the keyboard." },
                new() { Name = "ClassName", Type = "string?", DefaultValue = "null", Description = "A CSS class put on the element of the bar by the HTML renderer (canvas and SVG ignore it), for paint through the app's own CSS. Pointer events still go to the surface, so :hover does not fire on it." },
            ]
        },
        new()
        {
            Id = "edge-bar",
            Title = "BitResourceTimelineEdgeBar",
            Description = "A decorative bar drawn immediately before (StartBar) or after (EndBar) a main bar, in its row and at its height, typically a delay. It is not selectable.",
            Parameters =
            [
                new() { Name = "Duration", Type = "TimeSpan", DefaultValue = "", Description = "The length of the edge bar: a start bar extends back from the start of the main bar, an end bar forward from its end. A duration of zero or less is not drawn." },
                new() { Name = "Color", Type = "string?", DefaultValue = "null", Description = "CSS color of the edge bar. Falls back to the bar color of the theme." },
            ]
        },
        new()
        {
            Id = "bar-icon",
            Title = "BitResourceTimelineBarIcon",
            Description = "A small icon or image drawn next to (or within) a bar, scaled to fit a square box while keeping its aspect ratio. It is decorative and not selectable.",
            Parameters =
            [
                new() { Name = "Source", Type = "string", DefaultValue = "", Description = "The image source: an absolute or app-relative URL, or a data URI. Required." },
                new() { Name = "Position", Type = "BitResourceTimelineBarIconPosition", DefaultValue = "Start", Description = "Where the icon is anchored relative to the bar.", LinkType = LinkType.Link, Href = "#bar-icon-position-enum" },
                new() { Name = "Inside", Type = "bool", DefaultValue = "false", Description = "Draws the icon within the bar, against the edge its Position names, instead of beside it (Center always draws inside). An icon larger than the bar overhangs it, so give such a bar a Height that fits." },
                new() { Name = "Size", Type = "int?", DefaultValue = "null", Description = "The side of the square box the icon is fitted in, in pixels. Falls back to Options.BarIconSize." },
            ]
        },
        new()
        {
            Id = "bar-section",
            Title = "BitResourceTimelineBarSection",
            Description = "A part of a bar with colors, text and an icon of its own - the phases of a turnaround, the part of a job that has run. Each edge is a time or a percentage of the bar (a time wins), an edge given neither way is the bar's own, and a section always stays within its bar. A time-bounded section stays where it is when the bar is moved, a percentage-bounded one travels with it. Sections are decorative: the whole bar stays the hit target.",
            Parameters =
            [
                new() { Name = "StartTime", Type = "DateTimeOffset?", DefaultValue = "null", Description = "The start of the section; wins over StartPercent. Clamped to the bar." },
                new() { Name = "EndTime", Type = "DateTimeOffset?", DefaultValue = "null", Description = "The end of the section; wins over EndPercent. Clamped to the bar." },
                new() { Name = "StartPercent", Type = "double?", DefaultValue = "null", Description = "The start of the section as a percentage (0-100) of the duration of the bar, from its start. Used when StartTime is null." },
                new() { Name = "EndPercent", Type = "double?", DefaultValue = "null", Description = "The end of the section as a percentage (0-100) of the duration of the bar, from its start. Used when EndTime is null." },
                new() { Name = "BackgroundColor", Type = "string?", DefaultValue = "null", Description = "CSS color filling the section. When null the fill of the bar shows through." },
                new() { Name = "BorderColor", Type = "string?", DefaultValue = "null", Description = "CSS color of a 1px border just inside the edges of the section. When null no border is drawn." },
                new() { Name = "ForegroundColor", Type = "string?", DefaultValue = "null", Description = "CSS color of the Text. Falls back to the bar label color." },
                new() { Name = "Text", Type = "string?", DefaultValue = "null", Description = "The text drawn within the section, in the bar-label font. Text that does not fit is left out, never cut off." },
                new() { Name = "TextPosition", Type = "BitResourceTimelineBarSectionPosition", DefaultValue = "Center", Description = "Where the Text sits within the section.", LinkType = LinkType.Link, Href = "#bar-section-position-enum" },
                new() { Name = "Icon", Type = "string?", DefaultValue = "null", Description = "An icon drawn within the section: an image URL or a data URI. One sharing a position with the text sits beside it, nearer the edge of the section (before the text when both are centered); one that does not fit is not drawn." },
                new() { Name = "IconPosition", Type = "BitResourceTimelineBarSectionPosition", DefaultValue = "Start", Description = "Where the Icon sits within the section.", LinkType = LinkType.Link, Href = "#bar-section-position-enum" },
                new() { Name = "IconSize", Type = "int?", DefaultValue = "null", Description = "The side of the square box the Icon is fitted in, in pixels. Falls back to Options.BarIconSize, and shrinks to the height of a shorter bar." },
            ]
        },
        new()
        {
            Id = "timeline-options",
            Title = "BitResourceTimelineOptions",
            Description = "The visual configuration of the timeline (defaults in parentheses). A new instance is merged into what is in force: a property left null keeps the value applied before it.",
            Parameters =
            [
                new() { Name = "ResourceHeight", Type = "int?", DefaultValue = "null", Description = "Minimum row height in px (40). A row grows as overlapping bars stack." },
                new() { Name = "TimeAxisHeight", Type = "int?", DefaultValue = "null", Description = "Height of the time axis in px (60)." },
                new() { Name = "ResourceAxisWidth", Type = "int?", DefaultValue = "null", Description = "Width of the resource column in px (150)." },
                new() { Name = "ResourceAxisResizable", Type = "bool?", DefaultValue = "null", Description = "Whether the divider of the resource column can be dragged or moved from the keyboard (true); see OnResourceAxisWidthChanged." },
                new() { Name = "ResourceAxisMinWidth", Type = "int?", DefaultValue = "null", Description = "Minimum width of the resource column in px (80)." },
                new() { Name = "ResourceAxisMaxWidth", Type = "int?", DefaultValue = "null", Description = "Maximum width of the resource column in px; unset, the viewport less 100px." },
                new() { Name = "DateRowHeight", Type = "int?", DefaultValue = "null", Description = "Height of the date row of the time axis in px (22). The row labels each day, and thins as the zoom drops so its labels never overlap: every other day, then weeks, months, quarters, half years and years." },
                new() { Name = "BarHeight", Type = "int?", DefaultValue = "null", Description = "Height of a bar without a Height of its own in px (4)." },
                new() { Name = "BarMargin", Type = "int?", DefaultValue = "null", Description = "Gap between bars stacked for overlapping in time, in px (2)." },
                new() { Name = "StackLabelClearance", Type = "bool?", DefaultValue = "null", Description = "Whether stacked bars also keep room for their labels and icons (true). Independent of the zoom." },
                new() { Name = "StackOnLabelCollision", Type = "bool?", DefaultValue = "null", Description = "Whether bars whose labels, icons or edge bars would collide stack too, not only overlapping times (true)." },
                new() { Name = "CollapseMatchingEdgeLabels", Type = "bool?", DefaultValue = "null", Description = "Whether neighbouring bars whose facing labels match (one's TextEnd, the next one's TextStart) draw that text once in the gap between them, without stacking for it, while the gap is too narrow to show both clearly (false). Follows the zoom; the allocations keep both texts." },
                new() { Name = "MinBarWidth", Type = "int?", DefaultValue = "null", Description = "Minimum drawn width of a bar in px (2)." },
                new() { Name = "MinBarWidthForLabels", Type = "int?", DefaultValue = "null", Description = "Minimum bar width for its labels and icons to be drawn, in px (24)." },
                new() { Name = "BarLabelFont", Type = "string?", DefaultValue = "null", Description = "CSS font of the bar labels (11px, theme font family)." },
                new() { Name = "BarLabelGap", Type = "int?", DefaultValue = "null", Description = "Gap between a bar and its labels and icons in px (3)." },
                new() { Name = "BarIconSize", Type = "int?", DefaultValue = "null", Description = "Default box side of a bar icon in px (16)." },
                new() { Name = "DateLabelFont", Type = "string?", DefaultValue = "null", Description = "CSS font of the date-row labels (12px, theme font family). They are measured in it to pick how far apart they go as the zoom drops." },
                new() { Name = "HourLabelFont", Type = "string?", DefaultValue = "null", Description = "CSS font of the hour labels (12px, theme font family)." },
                new() { Name = "ResourceLabelFont", Type = "string?", DefaultValue = "null", Description = "CSS font of the leaf resource names (13px, theme font family)." },
                new() { Name = "ResourceGroupFont", Type = "string?", DefaultValue = "null", Description = "CSS font of the group resource names (bold 13px, theme font family)." },
                new() { Name = "ResourceChevronFont", Type = "string?", DefaultValue = "null", Description = "CSS font of the group chevron (10px, theme font family)." },
                new() { Name = "ResourceChevronGap", Type = "int?", DefaultValue = "null", Description = "Gap between a group's chevron and its name in px (14)." },
                new() { Name = "DragThreshold", Type = "int?", DefaultValue = "null", Description = "Pointer movement in px before a press becomes a drag (4)." },
                new() { Name = "HitTolerance", Type = "int?", DefaultValue = "null", Description = "Extra px around a bar that still hit it (3)." },
                new() { Name = "TimeZone", Type = "string?", DefaultValue = "null", Description = "IANA zone the axis is drawn in (UTC, Europe/Berlin); the viewer's while unset, or empty to return to it. Only the axis moves." },
                new() { Name = "ShowUtcTime", Type = "bool?", DefaultValue = "null", Description = "Adds a UTC hour row above the TimeZone's (false); raise TimeAxisHeight for room." },
                new() { Name = "Locale", Type = "string?", DefaultValue = "null", Description = "BCP 47 locale of the labels, tooltips and announcements; the viewer's while unset, or empty to return to it. Days are labelled in the locale's calendar, the months and years of a zoomed-out date row as Gregorian ones." },
                new() { Name = "FirstDayOfWeek", Type = "DayOfWeek?", DefaultValue = "null", Description = "Where week stripes and the weeks of a zoomed-out date row start; unset, the week start of the Locale (or the viewer's), else Monday." },
                new() { Name = "Hour12", Type = "bool?", DefaultValue = "null", Description = "Labels the hours with a 12-hour clock (false)." },
                new() { Name = "NonWorkingDays", Type = "int[]?", DefaultValue = "null", Description = "Weekdays washed as non-working (0 = Sunday ... 6 = Saturday). Visual only." },
                new() { Name = "WorkingHoursStart", Type = "int?", DefaultValue = "null", Description = "Start of the working hours in minutes from local midnight (540 = 09:00); hours outside are washed. Visual only." },
                new() { Name = "WorkingHoursEnd", Type = "int?", DefaultValue = "null", Description = "End of the working hours in minutes from local midnight (1020 = 17:00). Visual only." },
                new() { Name = "MaxStackLanes", Type = "int?", DefaultValue = "null", Description = "Maximum lanes a cluster of overlapping bars stacks into; 0 is unlimited (the default). The rest hide behind a +N marker." },
                new() { Name = "AllowDelete", Type = "bool?", DefaultValue = "null", Description = "Lets Delete/Backspace remove the selected bars of an editable timeline, once OnAllocationsDeleting agrees (false)." },
                new() { Name = "PixelsPerHour", Type = "double?", DefaultValue = "null", Description = "Horizontal scale; unset fits one day into the viewport. Clamped to Min/MaxPixelsPerHour; a zoom overrides it until ResetZoomAsync." },
                new() { Name = "MinPixelsPerHour", Type = "double?", DefaultValue = "null", Description = "Lower bound of the zoom in px per hour (0.25)." },
                new() { Name = "MaxPixelsPerHour", Type = "double?", DefaultValue = "null", Description = "Upper bound of the zoom in px per hour (1200)." },
                new() { Name = "AutoScrollToNow", Type = "bool?", DefaultValue = "null", Description = "Centers the current time as the first load is laid out (false)." },
                new() { Name = "PreserveScrollOnReload", Type = "bool?", DefaultValue = "null", Description = "Keeps a reload showing the same time and row instead of moving the view (false)." },
                new() { Name = "NowLineRefreshMs", Type = "int?", DefaultValue = "null", Description = "How often the now line repaints, in ms (60000); 0 stops it." },
                new() { Name = "PanToDayStart", Type = "bool?", DefaultValue = "null", Description = "Makes PanByDaysAsync land on a local midnight rather than step exactly 24 hours (false)." },
                new() { Name = "Editable", Type = "bool?", DefaultValue = "null", Description = "Enables moving and resizing bars by pointer and by Alt + arrows (false)." },
                new() { Name = "EditSnapMinutes", Type = "int?", DefaultValue = "null", Description = "Step a move or resize snaps to, in minutes (15); 0 is continuous." },
                new() { Name = "SnapToTimeZone", Type = "bool?", DefaultValue = "null", Description = "Snaps edits to wall-clock steps of the TimeZone (true) rather than the Unix epoch." },
                new() { Name = "EditResizeHandlePx", Type = "int?", DefaultValue = "null", Description = "Grab zone at each end of a bar that resizes it, in px (6)." },
                new() { Name = "EditMinDurationMinutes", Type = "int?", DefaultValue = "null", Description = "Shortest duration a resize or a drawn bar can produce, in minutes (5)." },
                new() { Name = "AllowResourceChange", Type = "bool?", DefaultValue = "null", Description = "Whether a move may put a bar on another row (true)." },
                new() { Name = "AllowOverlap", Type = "bool?", DefaultValue = "null", Description = "Whether unlocked bars of a row may overlap in time (true); false refuses such an edit." },
                new() { Name = "EmptyDragAction", Type = "BitResourceTimelineEmptyDragAction?", DefaultValue = "null", Description = "What a drag on empty content does while Editable: Marquee (the default) or Create (needs OnAllocationCreating).", LinkType = LinkType.Link, Href = "#empty-drag-action-enum" },
                new() { Name = "ShowTooltips", Type = "bool?", DefaultValue = "null", Description = "Whether hovering a bar or a +N marker shows a tooltip (true)." },
                new() { Name = "TooltipDelayMs", Type = "int?", DefaultValue = "null", Description = "Hover delay before a tooltip appears, in ms (300)." },
                new() { Name = "WindowBufferFactor", Type = "double?", DefaultValue = "null", Description = "On-demand loading: viewport widths of extra time asked for on each side (1)." },
                new() { Name = "WindowRefetchThreshold", Type = "double?", DefaultValue = "null", Description = "On-demand loading: a new window is asked for within this many viewport widths of the loaded edge (0.25)." },
                new() { Name = "WindowDebounceMs", Type = "int?", DefaultValue = "null", Description = "On-demand loading: delay after the last scroll before the window is checked, in ms (150)." },
                new() { Name = "Renderer", Type = "BitResourceTimelineRendererType?", DefaultValue = "null", Description = "What paints the timeline: Canvas (the default), Svg or Html; switchable at runtime.", LinkType = LinkType.Link, Href = "#renderer-type-enum" },
                new() { Name = "Colors", Type = "BitResourceTimelineColors?", DefaultValue = "null", Description = "Color overrides, winning over the theme and the --bit-ResourceTimeline-* variables.", LinkType = LinkType.Link, Href = "#timeline-colors" },
                new() { Name = "Surface", Type = "BitResourceTimelineSurface?", DefaultValue = "null", Description = "Stripes, per-resource colors and bands under the grid. Replaced as a whole; empty clears it.", LinkType = LinkType.Link, Href = "#timeline-surface" },
                new() { Name = "AxisLines", Type = "BitResourceTimelineAxisLines?", DefaultValue = "null", Description = "Colors of the time and resource lines. Replaced as a whole; empty clears it.", LinkType = LinkType.Link, Href = "#axis-lines" },
            ]
        },
        new()
        {
            Id = "timeline-colors",
            Title = "BitResourceTimelineColors",
            Description = "Color overrides of the timeline, each a CSS color string. A color set here wins over the bit theme and the --bit-ResourceTimeline-* variables; one left null keeps the color in force, so a partial instance changes only what it sets.",
            Parameters =
            [
                new() { Name = "ContentBg", Type = "string?", DefaultValue = "null", Description = "Background of the scrollable content area." },
                new() { Name = "AxisBg", Type = "string?", DefaultValue = "null", Description = "Background of the time and resource axes and of their corner." },
                new() { Name = "AxisBorder", Type = "string?", DefaultValue = "null", Description = "Lines within and around the axes." },
                new() { Name = "Tick", Type = "string?", DefaultValue = "null", Description = "Hour ticks of the time axis." },
                new() { Name = "Label", Type = "string?", DefaultValue = "null", Description = "Hour labels of the time axis and the resource labels." },
                new() { Name = "DateLabel", Type = "string?", DefaultValue = "null", Description = "Labels of the date row of the time axis (days, or weeks, months and years as the zoom drops)." },
                new() { Name = "Grid", Type = "string?", DefaultValue = "null", Description = "Grid lines across the content area." },
                new() { Name = "Bar", Type = "string?", DefaultValue = "null", Description = "Fill of a bar that has no Color of its own." },
                new() { Name = "BarSelected", Type = "string?", DefaultValue = "null", Description = "Fill of a selected bar that has no Color of its own." },
                new() { Name = "BarSelectedBorder", Type = "string?", DefaultValue = "null", Description = "Outline drawn around a selected bar." },
                new() { Name = "BarLabel", Type = "string?", DefaultValue = "null", Description = "Labels around the bars (above, below, start and end)." },
                new() { Name = "Now", Type = "string?", DefaultValue = "null", Description = "The current-time line and its marker." },
                new() { Name = "SelectionFill", Type = "string?", DefaultValue = "null", Description = "Fill of the marquee selection rectangle." },
                new() { Name = "SelectionBorder", Type = "string?", DefaultValue = "null", Description = "Outline of the marquee selection rectangle." },
                new() { Name = "Focus", Type = "string?", DefaultValue = "null", Description = "The dashed keyboard focus ring of the focused bar." },
                new() { Name = "TooltipBg", Type = "string?", DefaultValue = "null", Description = "Background of the built-in tooltip." },
                new() { Name = "TooltipText", Type = "string?", DefaultValue = "null", Description = "Text of the built-in tooltip." },
                new() { Name = "NonWorking", Type = "string?", DefaultValue = "null", Description = "The wash over the non-working days and hours." },
            ]
        },
        new()
        {
            Id = "timeline-surface",
            Title = "BitResourceTimelineSurface",
            Description = "Colors the content pane under the grid and the bars, painted bottom to top: the content background, the row fills, the column stripes, the Bands, then the non-working wash. A pattern is worked out for the whole range and resource list, so a stripe stays on its day or row as the user scrolls. Visual only.",
            Parameters =
            [
                new() { Name = "Columns", Type = "BitResourceTimelineColumnStripes?", DefaultValue = "null", Description = "Vertical time stripes; two colors on the day unit alternate the calendar days.", LinkType = LinkType.Link, Href = "#column-stripes" },
                new() { Name = "Rows", Type = "BitResourceTimelineRowStripes?", DefaultValue = "null", Description = "Horizontal stripes down the visible resource rows.", LinkType = LinkType.Link, Href = "#row-stripes" },
                new() { Name = "Combine", Type = "BitResourceTimelineSurfaceCombine?", DefaultValue = "null", Description = "How a row color and a column color share a cell. Null paints the columns over the rows.", LinkType = LinkType.Link, Href = "#surface-combine-enum" },
                new() { Name = "ResourceColors", Type = "Dictionary<string, string>?", DefaultValue = "null", Description = "A CSS color per resource id, for the rows that set no Background of their own. Wins over Rows." },
                new() { Name = "Bands", Type = "List<BitResourceTimelineSurfaceBand>?", DefaultValue = "null", Description = "Explicit rectangles painted above the patterns; later entries paint over earlier ones.", LinkType = LinkType.Link, Href = "#surface-band" },
                new() { Name = "ShadeTimeAxis", Type = "bool?", DefaultValue = "null", Description = "Also paints the column colors on the time axis: the date row for day and week stripes, the hour row for hour stripes. The labels stay above the tint." },
                new() { Name = "ShadeResourceAxis", Type = "bool?", DefaultValue = "null", Description = "Also paints the color of each row in the resource column, behind its label." },
            ]
        },
        new()
        {
            Id = "column-stripes",
            Title = "BitResourceTimelineColumnStripes",
            Description = "Vertical stripes, one per time unit, cycling Colors - the columns of a Surface or the vertical lines of AxisLines. Two colors on the default unit (a day) alternate the calendar days, and a day keeps its color wherever the loaded window sits.",
            Parameters =
            [
                new() { Name = "Unit", Type = "BitResourceTimelineSurfaceUnit?", DefaultValue = "null", Description = "What one stripe covers. Null is a day.", LinkType = LinkType.Link, Href = "#surface-unit-enum" },
                new() { Name = "Align", Type = "BitResourceTimelineStripeAlign?", DefaultValue = "null", Description = "Whether the cycle runs continuously across the range or restarts each day or week. Null is continuous.", LinkType = LinkType.Link, Href = "#stripe-align-enum" },
                new() { Name = "Span", Type = "int?", DefaultValue = "null", Description = "How many units one color covers before the next; null or below 1 is 1. An hour pattern of three colors with a span of 8 paints three 8-hour shifts." },
                new() { Name = "Offset", Type = "int?", DefaultValue = "null", Description = "Added to the stripe index before the color is chosen, so the cycle can start on another color without reordering Colors." },
                new() { Name = "Colors", Type = "string[]?", DefaultValue = "null", Description = "The CSS colors cycled in order. At least one is required; a single color paints every stripe the same." },
            ]
        },
        new()
        {
            Id = "row-stripes",
            Title = "BitResourceTimelineRowStripes",
            Description = "Horizontal stripes, one per visible resource row, cycling Colors down the rows - the rows of a Surface or the horizontal lines of AxisLines. A collapsed group re-stripes the rows under it; scrolling does not.",
            Parameters =
            [
                new() { Name = "Colors", Type = "string[]?", DefaultValue = "null", Description = "The CSS colors cycled in order; two colors are a zebra. At least one is required." },
                new() { Name = "Span", Type = "int?", DefaultValue = "null", Description = "How many rows one color covers before the next; null or below 1 is 1." },
                new() { Name = "Offset", Type = "int?", DefaultValue = "null", Description = "Added to the row index before the color is chosen." },
            ]
        },
        new()
        {
            Id = "surface-band",
            Title = "BitResourceTimelineSurfaceBand",
            Description = "An explicit rectangle on the content pane, painted above the column and row patterns: across every visible row, or along the row of one resource.",
            Parameters =
            [
                new() { Name = "Start", Type = "DateTimeOffset", DefaultValue = "", Description = "The start of the band (inclusive)." },
                new() { Name = "End", Type = "DateTimeOffset", DefaultValue = "", Description = "The end of the band (exclusive). A band ending at or before its Start is not painted." },
                new() { Name = "Color", Type = "string?", DefaultValue = "null", Description = "The CSS color. A null or empty color is not painted." },
                new() { Name = "ResourceId", Type = "string?", DefaultValue = "null", Description = "When set, only the row of this resource is painted, while it is visible; null paints every visible row." },
            ]
        },
        new()
        {
            Id = "axis-lines",
            Title = "BitResourceTimelineAxisLines",
            Description = "Colors the grid lines bounding the time columns and the resource rows. A line left without a color keeps Colors.Grid; the day separators of the time axis take the color of their midnight, and a row with a line color of its own draws it across the resource column too. Visual only.",
            Parameters =
            [
                new() { Name = "Vertical", Type = "BitResourceTimelineColumnStripes?", DefaultValue = "null", Description = "The colors of the vertical lines, chosen like column stripes: every vertical grid line takes the color of the stripe it sits in.", LinkType = LinkType.Link, Href = "#column-stripes" },
                new() { Name = "Horizontal", Type = "BitResourceTimelineRowStripes?", DefaultValue = "null", Description = "The colors of the horizontal lines - the top edge of each resource row - cycled down the rows.", LinkType = LinkType.Link, Href = "#row-stripes" },
                new() { Name = "ResourceColors", Type = "Dictionary<string, string>?", DefaultValue = "null", Description = "A CSS color per resource id for the line of its row, for the resources that set no LineColor of their own." },
                new() { Name = "BoundariesOnly", Type = "bool?", DefaultValue = "null", Description = "When true, only the line at the start of each vertical stripe (each midnight for days, the week boundary, every hour the span covers) is recolored; the others keep Colors.Grid." },
            ]
        },
        new()
        {
            Id = "allocation-change",
            Title = "BitResourceTimelineAllocationChange",
            Description = "A move or a resize handed to OnAllocationChanging / OnAllocationsChanging before it is committed. Return false to restore the previous resource and times without a reload.",
            Parameters =
            [
                new() { Name = "Allocation", Type = "BitResourceTimelineAllocation", DefaultValue = "", Description = "The allocation being changed - the very instance of the Config - already updated with the previewed resource and times.", LinkType = LinkType.Link, Href = "#timeline-allocation" },
                new() { Name = "PreviousResourceId", Type = "string", DefaultValue = "", Description = "The resource of the allocation before this edit." },
                new() { Name = "PreviousStartTime", Type = "DateTimeOffset", DefaultValue = "", Description = "The start time before this edit." },
                new() { Name = "PreviousEndTime", Type = "DateTimeOffset", DefaultValue = "", Description = "The end time before this edit." },
                new() { Name = "Kind", Type = "BitResourceTimelineAllocationChangeKind", DefaultValue = "Move", Description = "Whether the edit is a move or a resize.", LinkType = LinkType.Link, Href = "#allocation-change-kind-enum" },
            ]
        },
        new()
        {
            Id = "create-request",
            Title = "BitResourceTimelineCreateRequest",
            Description = "A new bar drawn by a drag on empty content, handed to OnAllocationCreating. The app returns the full allocation with an Id of its own, or null to cancel.",
            Parameters =
            [
                new() { Name = "ResourceId", Type = "string", DefaultValue = "", Description = "The resource row the new bar is drawn on." },
                new() { Name = "StartTime", Type = "DateTimeOffset", DefaultValue = "", Description = "The snapped start of the drawn range." },
                new() { Name = "EndTime", Type = "DateTimeOffset", DefaultValue = "", Description = "The snapped end of the drawn range (at least Options.EditMinDurationMinutes after the start)." },
            ]
        },
        new()
        {
            Id = "copy-request",
            Title = "BitResourceTimelineCopyRequest",
            Description = "The copied bars a paste (Ctrl/Cmd + V) asks the app to clone, handed to OnAllocationsCopying. The app assigns the new ids.",
            Parameters =
            [
                new() { Name = "Allocations", Type = "IReadOnlyList<BitResourceTimelineAllocation>", DefaultValue = "", Description = "The bars on the clipboard, in selection order.", LinkType = LinkType.Link, Href = "#timeline-allocation" },
                new() { Name = "Offset", Type = "TimeSpan", DefaultValue = "", Description = "The suggested time shift of each clone (typically one edit-snap step). The app may place the copies elsewhere." },
                new() { Name = "ResourceId", Type = "string?", DefaultValue = "null", Description = "The resource under the keyboard focus when pasting, or that of the first copied bar when nothing is focused. Null when the timeline has no rows." },
            ]
        },
        new()
        {
            Id = "pointer-args",
            Title = "BitResourceTimelinePointerArgs",
            Description = "The arguments of OnClick and OnDoubleClick: what was under the pointer (a bar, the bars of a +N marker, a resource row, a time) and where the pointer was, in the surface and in the viewport.",
            Parameters = pointerArgsMembers,
        },
        new()
        {
            Id = "context-menu-args",
            Title = "BitResourceTimelineContextMenuArgs",
            Description = "The arguments of OnContextMenu: what was under the pointer of a right-click and where it was, so the app can show and place a menu of its own (the browser's menu is suppressed). It derives from BitResourceTimelinePointerArgs and carries the same members.",
            Parameters = pointerArgsMembers,
        },
        new()
        {
            Id = "row-context",
            Title = "BitResourceTimelineRowContext",
            Description = "The context of the ResourceTemplate for one visible row, reflecting the current state of the hierarchy.",
            Parameters =
            [
                new() { Name = "Resource", Type = "BitResourceTimelineResource", DefaultValue = "", Description = "The resource the row stands for.", LinkType = LinkType.Link, Href = "#timeline-resource" },
                new() { Name = "Depth", Type = "int", DefaultValue = "0", Description = "The zero-based depth of the row in the resource hierarchy (0 for root rows)." },
                new() { Name = "HasChildren", Type = "bool", DefaultValue = "false", Description = "Whether the resource has children, i.e. the row is a group header." },
                new() { Name = "Collapsed", Type = "bool", DefaultValue = "false", Description = "Whether the group is currently collapsed." },
            ]
        },
        new()
        {
            Id = "timeline-view",
            Title = "BitResourceTimelineView",
            Description = "The time span visible in the content area and the scale in force, raised by OnViewChanged.",
            Parameters =
            [
                new() { Name = "Start", Type = "DateTimeOffset", DefaultValue = "", Description = "The time at the left edge of the content area." },
                new() { Name = "End", Type = "DateTimeOffset", DefaultValue = "", Description = "The time at the right edge of the content area." },
                new() { Name = "PixelsPerHour", Type = "double", DefaultValue = "0", Description = "The horizontal scale, in pixels per hour." },
            ]
        },
        new()
        {
            Id = "timeline-window",
            Title = "BitResourceTimelineWindow",
            Description = "The time window LoadAllocationsAsync is asked for, already buffered beyond the visible range. The app returns every allocation overlapping it, across all the resources.",
            Parameters =
            [
                new() { Name = "Start", Type = "DateTimeOffset", DefaultValue = "", Description = "The inclusive start of the requested window." },
                new() { Name = "End", Type = "DateTimeOffset", DefaultValue = "", Description = "The inclusive end of the requested window." },
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitResourceTimelineClassStyles",
            Description = "Custom CSS classes/styles for the different parts of the BitResourceTimeline.",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "The root element." },
                new() { Name = "Viewport", Type = "string?", DefaultValue = "null", Description = "The focusable scroll viewport the renderer draws into." },
                new() { Name = "Corner", Type = "string?", DefaultValue = "null", Description = "The top-start corner holding the TopStartContent." },
                new() { Name = "ResourceOverlay", Type = "string?", DefaultValue = "null", Description = "The overlay rendering the ResourceTemplate." },
                new() { Name = "ResourceRow", Type = "string?", DefaultValue = "null", Description = "Each row of that overlay." },
                new() { Name = "ResourceContent", Type = "string?", DefaultValue = "null", Description = "The rendered ResourceTemplate of each row." },
                new() { Name = "GroupToggle", Type = "string?", DefaultValue = "null", Description = "The expand/collapse button of a group row." },
                new() { Name = "TooltipOverlay", Type = "string?", DefaultValue = "null", Description = "The overlay rendering the TooltipTemplate." },
                new() { Name = "Loading", Type = "string?", DefaultValue = "null", Description = "The loading overlay." },
                new() { Name = "Spinner", Type = "string?", DefaultValue = "null", Description = "The default spinner of the loading overlay." },
                new() { Name = "LoadingText", Type = "string?", DefaultValue = "null", Description = "The default text of the loading overlay." },
            ]
        },
        new()
        {
            Id = "timeline-texts",
            Title = "BitResourceTimelineTexts",
            Description = "The texts of the BitResourceTimeline: its accessible name and role description, the loading text, the labels of its controls, the formats of its tooltips and everything it announces to screen readers. All of them default to English. A text ending in Format is a composite format string.",
            Parameters =
            [
                new() { Name = "AriaLabel", Type = "string", DefaultValue = "Resource timeline", Description = "Accessible name while AriaLabel is unset." },
                new() { Name = "RoleDescription", Type = "string", DefaultValue = "Resource timeline", Description = "Role description of the viewport." },
                new() { Name = "Loading", Type = "string", DefaultValue = "Loading…", Description = "Text of the default loading overlay." },
                new() { Name = "ExpandGroupFormat", Type = "string", DefaultValue = "Expand {0}", Description = "Name of a collapsed group's button; {0} the resource." },
                new() { Name = "CollapseGroupFormat", Type = "string", DefaultValue = "Collapse {0}", Description = "Name of an expanded group's button; {0} the resource." },
                new() { Name = "ResizeResourceColumn", Type = "string", DefaultValue = "Resize resource column", Description = "Name of the resource column's divider." },
                new() { Name = "BarLocked", Type = "string", DefaultValue = "Bar is locked", Description = "Edit key on a locked bar." },
                new() { Name = "SelectionCleared", Type = "string", DefaultValue = "Selection cleared", Description = "Escape cleared the selection." },
                new() { Name = "EmptyRowFormat", Type = "string", DefaultValue = "{0}, no allocations", Description = "Focus on an empty row; {0} the resource." },
                new() { Name = "FocusedBarFormat", Type = "string", DefaultValue = "{0}: {1} to {2}", Description = "Focus on a bar; {0} the resource, {1} start, {2} end." },
                new() { Name = "FocusedLabeledBarFormat", Type = "string", DefaultValue = "{0}: {1}, {2} to {3}", Description = "Focus on a labeled bar; {0} the resource, {1} the label, {2} start, {3} end." },
                new() { Name = "SelectedBarFormat", Type = "string", DefaultValue = "{0}, selected", Description = "A selected focused bar; {0} its announcement." },
                new() { Name = "ResizedFormat", Type = "string", DefaultValue = "Resized: {0} to {1}", Description = "Keyboard resize; {0} start, {1} end." },
                new() { Name = "MovedFormat", Type = "string", DefaultValue = "Moved: {0} to {1}", Description = "Keyboard move; {0} start, {1} end." },
                new() { Name = "MovedToResourceFormat", Type = "string", DefaultValue = "Moved to {0}: {1} to {2}", Description = "Keyboard move to another row; {0} the resource, {1} start, {2} end." },
                new() { Name = "EditRefused", Type = "string", DefaultValue = "Edit refused", Description = "An edit refused." },
                new() { Name = "EditRefusedOverlap", Type = "string", DefaultValue = "Edit refused: overlaps another allocation", Description = "An edit refused for overlapping." },
                new() { Name = "GroupCollapsed", Type = "string", DefaultValue = "Collapsed group", Description = "A group collapsed." },
                new() { Name = "GroupExpanded", Type = "string", DefaultValue = "Expanded group", Description = "A group expanded." },
                new() { Name = "CopiedFormat", Type = "string", DefaultValue = "Copied {0}", Description = "Bars copied; {0} the count." },
                new() { Name = "PastedFormat", Type = "string", DefaultValue = "Pasted {0}", Description = "Bars pasted; {0} the count." },
                new() { Name = "DeletedFormat", Type = "string", DefaultValue = "Deleted {0}", Description = "Bars deleted; {0} the count." },
                new() { Name = "DeleteRefused", Type = "string", DefaultValue = "Delete refused", Description = "A delete refused." },
                new() { Name = "TimeRangeFormat", Type = "string", DefaultValue = "{0} – {1}", Description = "A time range (tooltips, drag readout); {0} start, {1} end." },
                new() { Name = "HiddenAllocationFormat", Type = "string", DefaultValue = "{0} hidden allocation", Description = "A +N tooltip's first line for one bar; {0} the count." },
                new() { Name = "HiddenAllocationsFormat", Type = "string", DefaultValue = "{0} hidden allocations", Description = "A +N tooltip's first line; {0} the count." },
                new() { Name = "HiddenAllocationItemFormat", Type = "string", DefaultValue = "{0} · {1}", Description = "A line of a +N tooltip; {0} the label, {1} the range." },
                new() { Name = "MoreHiddenAllocationsFormat", Type = "string", DefaultValue = "… and {0} more", Description = "A +N tooltip's last line; {0} the bars left out." },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "allocation-change-kind-enum",
            Name = "BitResourceTimelineAllocationChangeKind",
            Description = "Why an allocation is being committed.",
            Items =
            [
                new() { Name = "Move", Value = "0", Description = "The bar was moved in time and/or onto another resource." },
                new() { Name = "Resize", Value = "1", Description = "The start or the end of the bar was resized." },
                new() { Name = "Create", Value = "2", Description = "A new bar is being created. Not handed to the changing callbacks today: a bar drawn on empty content is created through OnAllocationCreating alone." },
            ]
        },
        new()
        {
            Id = "bar-icon-position-enum",
            Name = "BitResourceTimelineBarIconPosition",
            Description = "Where a bar icon is anchored relative to its bar.",
            Items =
            [
                new() { Name = "Start", Value = "0", Description = "Just before the start edge of the bar, vertically centered on it." },
                new() { Name = "End", Value = "1", Description = "Just after the end edge of the bar, vertically centered on it." },
                new() { Name = "Above", Value = "2", Description = "Centered above the bar." },
                new() { Name = "Below", Value = "3", Description = "Centered below the bar." },
                new() { Name = "Center", Value = "4", Description = "On top of the bar, centered horizontally and vertically within it." },
            ]
        },
        new()
        {
            Id = "bar-section-position-enum",
            Name = "BitResourceTimelineBarSectionPosition",
            Description = "Where the text or the icon of a bar section sits within that section.",
            Items =
            [
                new() { Name = "Start", Value = "0", Description = "Just inside the start edge of the section." },
                new() { Name = "Center", Value = "1", Description = "Centered horizontally within the section." },
                new() { Name = "End", Value = "2", Description = "Just inside the end edge of the section." },
            ]
        },
        new()
        {
            Id = "empty-drag-action-enum",
            Name = "BitResourceTimelineEmptyDragAction",
            Description = "What a pointer drag on empty content (no bar hit) does while editing.",
            Items =
            [
                new() { Name = "Marquee", Value = "0", Description = "A rubber-band selection (the default)." },
                new() { Name = "Create", Value = "1", Description = "Draws a new allocation. Requires Options.Editable and an OnAllocationCreating handler; Ctrl/Cmd + drag still marquees." },
            ]
        },
        new()
        {
            Id = "hit-area-enum",
            Name = "BitResourceTimelineHitArea",
            Description = "Which region of the timeline a pointer event landed in.",
            Items =
            [
                new() { Name = "Content", Value = "0", Description = "The scrollable grid of resource rows and bars, beside the resource column and below the time axis." },
                new() { Name = "ResourceAxis", Value = "1", Description = "The sticky resource column. Has a resource but no time." },
                new() { Name = "TimeAxis", Value = "2", Description = "The sticky time axis along the top. Has a time but no resource." },
                new() { Name = "Corner", Value = "3", Description = "The top-start cell where the time axis and the resource column meet. Neither a time nor a resource." },
            ]
        },
        new()
        {
            Id = "renderer-type-enum",
            Name = "BitResourceTimelineRendererType",
            Description = "Which rendering technology paints the timeline. All renderers share the same engine - the data, the layout, the interaction and the events are identical; only how the frame is drawn differs.",
            Items =
            [
                new() { Name = "Canvas", Value = "0", Description = "An HTML canvas (the default): immediate-mode 2D drawing, crisp on HiDPI screens, and the fastest option for large, dense data sets." },
                new() { Name = "Svg", Value = "1", Description = "Inline SVG: resolution-independent vector output that is styleable, inspectable in the developer tools and copy and print friendly. Slower than the canvas on very dense scenes." },
                new() { Name = "Html", Value = "2", Description = "Plain HTML elements: each visible bar is a real element carrying its allocation id in data-bar-id (and its ClassName), so it can be styled with CSS. Slower than the canvas on very dense scenes." },
            ]
        },
        new()
        {
            Id = "stripe-align-enum",
            Name = "BitResourceTimelineStripeAlign",
            Description = "How the color cycle of column stripes lines up with the calendar.",
            Items =
            [
                new() { Name = "Continuous", Value = "0", Description = "The cycle runs across the whole timeline: adjacent days (or hours, or weeks) take the next color, without restarting at midnight or at the start of a week. The default." },
                new() { Name = "Repeat", Value = "1", Description = "The cycle restarts each day when the unit is an hour, and each week when it is a day, so the same hour of the day or weekday always gets the same color. A week unit has no larger cycle and is painted as Continuous." },
            ]
        },
        new()
        {
            Id = "surface-combine-enum",
            Name = "BitResourceTimelineSurfaceCombine",
            Description = "How a row color and a column color of the surface share a cell.",
            Items =
            [
                new() { Name = "Overlay", Value = "0", Description = "The row fills, then the column stripes on top: a translucent column color tints the row under it, an opaque one covers it. The default." },
                new() { Name = "RowsOnTop", Value = "1", Description = "The column stripes, then the row fills on top." },
                new() { Name = "Checker", Value = "2", Description = "Each cell is painted from the column colors by its column index plus its row index, so two colors make a checkerboard over the whole surface; a resource's own Background still paints its row solid and the row stripes are not used. Cells that would be too thin or too many fall back to full-height column stripes." },
            ]
        },
        new()
        {
            Id = "surface-unit-enum",
            Name = "BitResourceTimelineSurfaceUnit",
            Description = "What one column stripe covers, decided from the calendar of Options.TimeZone (or the viewer's zone), so a stripe stays put as the user scrolls.",
            Items =
            [
                new() { Name = "Day", Value = "0", Description = "A stripe per local calendar day. The default." },
                new() { Name = "Hour", Value = "1", Description = "A stripe per clock hour, continuing through midnight." },
                new() { Name = "Week", Value = "2", Description = "A stripe per week, starting on Options.FirstDayOfWeek (or the week start of the locale, otherwise Monday)." },
            ]
        },
    ];



    private readonly BitResourceTimelineConfig basicConfig = CreateBasicConfig();

    private static BitResourceTimelineConfig CreateBasicConfig()
    {
        var today = DateTime.Today;

        // A local time of today (or of a day after it), converted with the offset of its own date, so a DST change in
        // the range does not move the bars after it off their wall-clock times.
        DateTimeOffset At(double hours, int days = 0) => new(today.AddDays(days).AddHours(hours));

        return new()
        {
            StartDate = At(0),
            EndDate = At(0, days: 3),
            Resources =
            [
                new() { Id = "A1", Name = "Gate A1" },
                new() { Id = "A2", Name = "Gate A2" },
                new() { Id = "A3", Name = "Gate A3" },
                new() { Id = "A4", Name = "Gate A4" },
            ],
            Allocations =
            [
                new() { Id = "f1", ResourceId = "A1", StartTime = At(1), EndTime = At(3.5) },
                new() { Id = "f2", ResourceId = "A1", StartTime = At(6), EndTime = At(8) },
                new() { Id = "f3", ResourceId = "A1", StartTime = At(13), EndTime = At(16) },
                new() { Id = "f4", ResourceId = "A2", StartTime = At(2), EndTime = At(4) },
                new() { Id = "f5", ResourceId = "A2", StartTime = At(9.5), EndTime = At(12) },
                new() { Id = "f6", ResourceId = "A3", StartTime = At(4), EndTime = At(9) },
                new() { Id = "f7", ResourceId = "A3", StartTime = At(17), EndTime = At(20) },
                new() { Id = "f8", ResourceId = "A4", StartTime = At(7), EndTime = At(10) },
                new() { Id = "f9", ResourceId = "A4", StartTime = At(20), EndTime = At(3, days: 1) },
                new() { Id = "f10", ResourceId = "A2", StartTime = At(8, days: 1), EndTime = At(11, days: 1) },
            ],
        };
    }



    private readonly BitResourceTimelineConfig hierarchyConfig = CreateHierarchyConfig();

    private static BitResourceTimelineConfig CreateHierarchyConfig()
    {
        var today = BitResourceTimelineDemoData.Today;

        List<BitResourceTimelineResource> resources =
        [
            new() { Id = "TA", Name = "Terminal A" },
            new() { Id = "A1", Name = "Gate A1", ParentId = "TA" },
            new() { Id = "A2", Name = "Gate A2", ParentId = "TA" },
            new() { Id = "AR", Name = "Remote stands", ParentId = "TA" },
            new() { Id = "R1", Name = "Stand R1", ParentId = "AR" },
            new() { Id = "R2", Name = "Stand R2", ParentId = "AR" },
            new() { Id = "TB", Name = "Terminal B", Collapsed = true },
            new() { Id = "B1", Name = "Gate B1", ParentId = "TB" },
            new() { Id = "B2", Name = "Gate B2", ParentId = "TB" },
        ];

        return new()
        {
            StartDate = today,
            EndDate = today.AddDays(3),
            Resources = resources,
            Allocations =
            [
                // The flights of the gates and the stands; the group rows get none from the helper.
                .. BitResourceTimelineDemoData.Flights(resources, today, today.AddDays(3)),
                // A group row can still hold bars of its own.
                new() { Id = "TA-cleaning", ResourceId = "TA", StartTime = today.AddHours(1), EndTime = today.AddHours(4) },
                new() { Id = "TA-inspection", ResourceId = "TA", StartTime = today.AddHours(13), EndTime = today.AddHours(15) },
            ],
        };
    }



    private readonly BitResourceTimelineConfig appearanceConfig = CreateAppearanceConfig();

    private static BitResourceTimelineConfig CreateAppearanceConfig()
    {
        var today = BitResourceTimelineDemoData.Today;

        return new()
        {
            StartDate = today,
            EndDate = today.AddDays(1),
            Resources = BitResourceTimelineDemoData.Gates(4),
            Allocations =
            [
                new()
                {
                    Id = "LH441",
                    ResourceId = "A1",
                    StartTime = today.AddHours(2),
                    EndTime = today.AddHours(6),
                    Color = "#2f9e44",
                    Height = 10,
                    TextAbove = "LH441",
                    TextBelow = "Frankfurt",
                    TextStart = "02:00",
                    TextEnd = "06:00",
                },
                new()
                {
                    Id = "BA117",
                    ResourceId = "A1",
                    StartTime = today.AddHours(11),
                    EndTime = today.AddHours(15),
                    Color = "#1971c2",
                    Height = 6,
                    TextAbove = "BA117 (hover me)",
                    Tooltip = "BA117 to London\nBoarding at 14:20, gate closes at 14:45",
                },
                new()
                {
                    Id = "AF1380",
                    ResourceId = "A2",
                    StartTime = today.AddHours(4),
                    EndTime = today.AddHours(8),
                    Color = "#e8590c",
                    Height = 10,
                    TextAbove = "AF1380 (delayed)",
                    StartBar = new() { Duration = TimeSpan.FromMinutes(50), Color = "#ffa8a8" },
                    EndBar = new() { Duration = TimeSpan.FromMinutes(30), Color = "#fa5252" },
                },
                new()
                {
                    Id = "KL1002",
                    ResourceId = "A3",
                    StartTime = today.AddHours(3),
                    EndTime = today.AddHours(8),
                    Color = "#9c36b5",
                    Height = 8,
                    Icons =
                    [
                        new() { Source = BitResourceTimelineDemoData.WarningIcon, Position = BitResourceTimelineBarIconPosition.Start },
                        new() { Source = BitResourceTimelineDemoData.CheckIcon, Position = BitResourceTimelineBarIconPosition.End },
                        new() { Source = BitResourceTimelineDemoData.StarIcon, Position = BitResourceTimelineBarIconPosition.Above },
                        new() { Source = BitResourceTimelineDemoData.StarIcon, Position = BitResourceTimelineBarIconPosition.Above },
                    ],
                },
                new()
                {
                    Id = "EK47",
                    ResourceId = "A4",
                    StartTime = today.AddHours(2),
                    EndTime = today.AddHours(9),
                    Color = "#0c8599",
                    Height = 22,
                    Icons =
                    [
                        new() { Source = BitResourceTimelineDemoData.PlaneIcon, Position = BitResourceTimelineBarIconPosition.Start, Inside = true },
                        new() { Source = BitResourceTimelineDemoData.LockIcon, Position = BitResourceTimelineBarIconPosition.Center },
                        new() { Source = BitResourceTimelineDemoData.CheckIcon, Position = BitResourceTimelineBarIconPosition.End, Inside = true, Size = 14 },
                    ],
                },
            ],
        };
    }



    private readonly BitResourceTimelineConfig sectionsConfig = CreateSectionsConfig();

    private static BitResourceTimelineConfig CreateSectionsConfig()
    {
        var today = BitResourceTimelineDemoData.Today;

        return new()
        {
            StartDate = today,
            EndDate = today.AddDays(1),
            Resources = BitResourceTimelineDemoData.Gates(3),
            Allocations =
            [
                new()
                {
                    // Bounded by times: deboarding until 03:00, boarding from 06:30.
                    Id = "LH441",
                    ResourceId = "A1",
                    StartTime = today.AddHours(2),
                    EndTime = today.AddHours(8),
                    Color = "#1971c2",
                    Height = 18,
                    Sections =
                    [
                        new() { EndTime = today.AddHours(3), BackgroundColor = "#fab005", ForegroundColor = "#212529", Text = "Deboard" },
                        new()
                        {
                            StartTime = today.AddHours(3),
                            EndTime = today.AddHours(6.5),
                            ForegroundColor = "#ffffff",
                            Text = "LH441",
                            Icon = BitResourceTimelineDemoData.PlaneIcon,
                            IconSize = 14,
                        },
                        new()
                        {
                            StartTime = today.AddHours(6.5),
                            BackgroundColor = "#d0ebff",
                            BorderColor = "#1971c2",
                            ForegroundColor = "#1971c2",
                            Text = "Board",
                            TextPosition = BitResourceTimelineBarSectionPosition.End,
                        },
                    ],
                },
                new()
                {
                    // Bounded by a percentage: the part of the job that has run.
                    Id = "job",
                    ResourceId = "A2",
                    StartTime = today.AddHours(1),
                    EndTime = today.AddHours(9),
                    Color = "#ced4da",
                    Height = 18,
                    Sections =
                    [
                        new()
                        {
                            EndPercent = 40,
                            BackgroundColor = "#2f9e44",
                            ForegroundColor = "#ffffff",
                            Text = "40% done",
                            TextPosition = BitResourceTimelineBarSectionPosition.Start,
                            Icon = BitResourceTimelineDemoData.CheckIcon,
                            IconPosition = BitResourceTimelineBarSectionPosition.End,
                        },
                    ],
                },
                new()
                {
                    // Percentages travel and stretch with the bar when it is edited.
                    Id = "BA117",
                    ResourceId = "A3",
                    StartTime = today.AddHours(3),
                    EndTime = today.AddHours(10),
                    Color = "#9c36b5",
                    Height = 18,
                    Sections =
                    [
                        new() { EndPercent = 20, BackgroundColor = "#e599f7", ForegroundColor = "#212529", Text = "Prep" },
                        new()
                        {
                            StartPercent = 20,
                            EndPercent = 85,
                            ForegroundColor = "#ffffff",
                            Text = "BA117",
                            Icon = BitResourceTimelineDemoData.StarIcon,
                            IconPosition = BitResourceTimelineBarSectionPosition.Center,
                        },
                        new()
                        {
                            StartPercent = 85,
                            BackgroundColor = "#f8f0fc",
                            BorderColor = "#9c36b5",
                            ForegroundColor = "#9c36b5",
                            Text = "Wrap",
                            TextPosition = BitResourceTimelineBarSectionPosition.End,
                        },
                    ],
                },
            ],
        };
    }



    private readonly BitResourceTimelineConfig rendererConfig = CreateRendererConfig();
    private BitResourceTimelineRendererType renderer = BitResourceTimelineRendererType.Canvas;
    private BitResourceTimelineOptions rendererOptions = new() { Renderer = BitResourceTimelineRendererType.Canvas };

    private static BitResourceTimelineConfig CreateRendererConfig()
    {
        var config = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(6), fromDay: 0, toDay: 3, labeled: true);

        // The HTML renderer puts ClassName on the element of the bar: the stylesheet hatches the flights already gone.
        foreach (var flight in config.Allocations.Where(f => f.EndTime < DateTimeOffset.Now))
        {
            flight.ClassName = "departed";
        }

        return config;
    }



    private BitResourceTimeline navigationRef = default!;
    private readonly BitResourceTimelineConfig navigationConfig = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(6), fromDay: -14, toDay: 14, labeled: true);
    private bool panToDayStart;
    private BitResourceTimelineOptions navigationOptions = CreateNavigationOptions(false);
    private BitResourceTimelineView? navigationView;

    private static BitResourceTimelineOptions CreateNavigationOptions(bool panToDayStart) => new()
    {
        AutoScrollToNow = true,
        MinPixelsPerHour = 2,
        MaxPixelsPerHour = 240,
        PanToDayStart = panToDayStart,
    };

    private string NavigationText => navigationView is null
        ? "Scroll or zoom the timeline"
        : $"{navigationView.Start.ToLocalTime():ddd d MMM HH:mm} - {navigationView.End.ToLocalTime():ddd d MMM HH:mm} · {navigationView.PixelsPerHour:0.#} px/h";



    private BitResourceTimeline selectionRef = default!;
    private readonly BitResourceTimelineConfig selectionConfig = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(12), labeled: true);
    private readonly BitResourceTimelineOptions selectionOptions = new() { AutoScrollToNow = true };
    private BitResourceTimelineAllocation[] selectedBars = [];
    private string? clickText;
    private string? doubleClickText;
    private string? contextMenuText;

    private async Task SelectNextFlights()
    {
        var now = DateTimeOffset.Now;
        var next = selectionConfig.Allocations.Where(f => f.StartTime > now).OrderBy(f => f.StartTime).Take(3).Select(f => f.Id).ToArray();

        await selectionRef.SelectAsync(next);
    }

    private async Task ScrollToSelection()
    {
        var selected = await selectionRef.GetSelectedBarsAsync();

        if (selected.Count > 0)
        {
            await selectionRef.ScrollToAllocationAsync(selected[0].Id);
        }
    }

    private static string DescribePointer(BitResourceTimelinePointerArgs args)
    {
        List<string> parts = [args.Area.ToString()];

        if (args.Resource is not null) parts.Add(args.Resource.Name);
        if (args.Time is { } time) parts.Add(time.ToLocalTime().ToString("ddd HH:mm"));
        if (args.Allocation is not null) parts.Add($"flight {args.Allocation.TextAbove}");

        return string.Join(" · ", parts);
    }



    private readonly BitResourceTimelineConfig editingConfig = CreateEditingConfig();
    private bool allowResourceChange = true;
    private BitResourceTimelineOptions editingOptions = CreateEditingOptions(true);
    private readonly List<string> editLog = [];
    private int createdCount;

    private static BitResourceTimelineConfig CreateEditingConfig()
    {
        var config = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(6), labeled: true);
        var now = DateTimeOffset.Now;

        // A flight at its gate right now cannot be moved: Locked keeps it selectable but not editable.
        foreach (var flight in config.Allocations.Where(f => f.StartTime <= now && f.EndTime > now))
        {
            flight.Locked = true;
            flight.Color = "#868e96";
            flight.TextBelow = "at the gate";
        }

        return config;
    }

    private static BitResourceTimelineOptions CreateEditingOptions(bool allowResourceChange) => new()
    {
        AutoScrollToNow = true,
        Editable = true,
        EditSnapMinutes = 15,
        AllowResourceChange = allowResourceChange,
        EmptyDragAction = BitResourceTimelineEmptyDragAction.Create,
        AllowDelete = true,
    };

    private Task<bool> HandleAllocationChanging(BitResourceTimelineAllocationChange change) => Task.FromResult(AcceptEdit([change]));

    private Task<bool> HandleAllocationsChanging(IReadOnlyList<BitResourceTimelineAllocationChange> changes) => Task.FromResult(AcceptEdit(changes));

    // The past is history: an edit that would start a flight before now is refused, and the bars snap back.
    private bool AcceptEdit(IReadOnlyList<BitResourceTimelineAllocationChange> changes)
    {
        var refused = changes.FirstOrDefault(c => c.Allocation.StartTime < DateTimeOffset.Now);

        if (refused is null) return true;

        Log($"Refused: {refused.Allocation.TextAbove} would start in the past");
        return false;
    }

    private void HandleAllocationChanged(BitResourceTimelineAllocation flight)
    {
        Log($"{flight.TextAbove}: Gate {flight.ResourceId}, {flight.StartTime.ToLocalTime():ddd HH:mm} - {flight.EndTime.ToLocalTime():HH:mm}");
    }

    private Task<BitResourceTimelineAllocation?> HandleAllocationCreating(BitResourceTimelineCreateRequest request)
    {
        // The timeline never invents ids: the app builds the allocation (or returns null to cancel).
        var flight = new BitResourceTimelineAllocation
        {
            Id = $"new-{++createdCount}",
            ResourceId = request.ResourceId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Color = "#2f9e44",
            TextAbove = $"NEW{createdCount}",
        };

        editingConfig.Allocations.Add(flight);
        Log($"Created {flight.TextAbove} at Gate {flight.ResourceId}");

        return Task.FromResult<BitResourceTimelineAllocation?>(flight);
    }

    private Task<IReadOnlyList<BitResourceTimelineAllocation>?> HandleAllocationsCopying(BitResourceTimelineCopyRequest request)
    {
        List<BitResourceTimelineAllocation> copies =
        [
            .. request.Allocations.Select(source => new BitResourceTimelineAllocation
            {
                Id = $"copy-{++createdCount}",
                ResourceId = source.ResourceId,
                StartTime = source.StartTime + request.Offset,
                EndTime = source.EndTime + request.Offset,
                Color = "#2f9e44",
                TextAbove = $"{source.TextAbove} copy",
            })
        ];

        editingConfig.Allocations.AddRange(copies);
        Log($"Pasted {copies.Count}");

        return Task.FromResult<IReadOnlyList<BitResourceTimelineAllocation>?>(copies);
    }

    private Task<bool> HandleAllocationsDeleting(IReadOnlyList<string> ids)
    {
        if (editingConfig.Allocations.Any(f => f.Locked && ids.Contains(f.Id)))
        {
            Log("Refused: a flight at its gate cannot be deleted");
            return Task.FromResult(false);
        }

        editingConfig.Allocations.RemoveAll(f => ids.Contains(f.Id));
        Log($"Deleted {ids.Count}");

        return Task.FromResult(true);
    }

    private void Log(string line)
    {
        editLog.Insert(0, line);
        if (editLog.Count > 5) editLog.RemoveAt(5);

        StateHasChanged();
    }



    private readonly BitResourceTimelineConfig overlapConfig = CreateOverlapConfig();
    private int maxStackLanes;
    private bool stackLabelClearance = true;
    private bool stackOnLabelCollision = true;
    private bool collapseMatchingEdgeLabels;
    private bool allowOverlap = true;
    private BitResourceTimelineOptions overlapOptions = CreateOverlapOptions(0, true, true, false, true);

    private void UpdateOverlapOptions() => overlapOptions = CreateOverlapOptions(maxStackLanes, stackLabelClearance, stackOnLabelCollision, collapseMatchingEdgeLabels, allowOverlap);

    private static BitResourceTimelineOptions CreateOverlapOptions(int maxStackLanes, bool stackLabelClearance, bool stackOnLabelCollision, bool collapseMatchingEdgeLabels, bool allowOverlap) => new()
    {
        Editable = true,
        BarHeight = 8,
        BarMargin = 4,
        MaxStackLanes = maxStackLanes,
        StackLabelClearance = stackLabelClearance,
        StackOnLabelCollision = stackOnLabelCollision,
        CollapseMatchingEdgeLabels = collapseMatchingEdgeLabels,
        AllowOverlap = allowOverlap,
    };

    private static BitResourceTimelineConfig CreateOverlapConfig()
    {
        var today = BitResourceTimelineDemoData.Today;

        BitResourceTimelineAllocation Flight(string gate, string id, double from, double to, string? below = null, bool times = false, string? origin = null, string? destination = null) => new()
        {
            Id = id,
            ResourceId = gate,
            StartTime = today.AddHours(from),
            EndTime = today.AddHours(to),
            TextAbove = id,
            TextBelow = below,
            TextStart = origin ?? (times ? today.AddHours(from).ToString("HH:mm", CultureInfo.InvariantCulture) : null),
            TextEnd = destination ?? (times ? today.AddHours(to).ToString("HH:mm", CultureInfo.InvariantCulture) : null),
        };

        return new()
        {
            StartDate = today,
            EndDate = today.AddDays(1),
            Resources = BitResourceTimelineDemoData.Gates(5),
            Allocations =
            [
                // Five at once: five lanes, or MaxStackLanes and a +N marker.
                Flight("A1", "LH400", 2, 7),
                Flight("A1", "LH402", 3, 8),
                Flight("A1", "LH404", 3.5, 6),
                Flight("A1", "LH406", 4, 9),
                Flight("A1", "LH408", 5, 7.5),
                // A quarter of an hour apart: their times collide, though they do not overlap.
                Flight("A2", "BA200", 3, 6, times: true),
                Flight("A2", "BA202", 6.25, 9, times: true),
                // Overlapping, with labels above and below.
                Flight("A3", "AF300", 2, 7, below: "Paris"),
                Flight("A3", "AF302", 4, 9, below: "Nice"),
                // Drop a bar on this one to try AllowOverlap.
                Flight("A4", "KL500", 3, 6),
                // One aircraft's legs: each lands where the next one leaves from, so each
                // turn draws the same airport twice - unless CollapseMatchingEdgeLabels.
                Flight("A5", "KL601", 2, 4, origin: "AMS", destination: "CDG"),
                Flight("A5", "KL602", 4.25, 6.25, origin: "CDG", destination: "AMS"),
                Flight("A5", "KL603", 7, 9, origin: "AMS", destination: "LHR"),
            ],
        };
    }



    private readonly BitResourceTimelineConfig surfaceConfig = CreateSurfaceConfig();
    private string? surfacePreset = "days";
    private bool axisLines;
    private BitResourceTimelineOptions surfaceOptions = CreateSurfaceOptions("days", false);

    private void UpdateSurfaceOptions() => surfaceOptions = CreateSurfaceOptions(surfacePreset, axisLines);

    private static BitResourceTimelineConfig CreateSurfaceConfig()
    {
        var config = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(6), labeled: true);

        // A resource paints its own row and line, over any pattern of the surface.
        config.Resources[2].Name = "Gate A3 (closed)";
        config.Resources[2].Background = "rgba(250, 82, 82, 0.15)";
        config.Allocations.RemoveAll(f => f.ResourceId == "A3");
        config.Resources[4].LineColor = "#e8590c";

        return config;
    }

    private static BitResourceTimelineOptions CreateSurfaceOptions(string? preset, bool axisLines)
    {
        var today = BitResourceTimelineDemoData.Today;

        return new()
        {
            AutoScrollToNow = true,
            // The weekends, and the hours outside 09:00 - 17:00, are washed over whatever the surface paints.
            NonWorkingDays = [0, 6],
            WorkingHoursStart = 9 * 60,
            WorkingHoursEnd = 17 * 60,
            // A new surface replaces the previous one as a whole, and an empty one clears it.
            Surface = preset switch
            {
                "days" => new()
                {
                    Columns = new() { Colors = ["rgba(25, 113, 194, 0.08)", "transparent"] },
                    ShadeTimeAxis = true,
                },
                "shifts" => new()
                {
                    Columns = new()
                    {
                        Unit = BitResourceTimelineSurfaceUnit.Hour,
                        Span = 8,
                        Align = BitResourceTimelineStripeAlign.Repeat,
                        Colors = ["rgba(250, 176, 5, 0.12)", "rgba(47, 158, 68, 0.1)", "rgba(25, 113, 194, 0.1)"],
                    },
                    ShadeTimeAxis = true,
                },
                "rows" => new()
                {
                    Rows = new() { Colors = ["rgba(134, 142, 150, 0.12)", "transparent"] },
                    ResourceColors = new() { ["A6"] = "rgba(156, 54, 181, 0.15)" },
                    ShadeResourceAxis = true,
                },
                "checker" => new()
                {
                    Columns = new() { Colors = ["rgba(25, 113, 194, 0.1)", "transparent"] },
                    Combine = BitResourceTimelineSurfaceCombine.Checker,
                },
                "bands" => new()
                {
                    Bands =
                    [
                        new() { Start = today.AddHours(12), End = today.AddHours(14), Color = "rgba(250, 82, 82, 0.15)" },
                        new() { Start = today.AddHours(6), End = today.AddHours(10), Color = "rgba(250, 176, 5, 0.3)", ResourceId = "A2" },
                    ],
                },
                _ => new(),
            },
            AxisLines = axisLines
                ? new()
                {
                    BoundariesOnly = true,
                    Vertical = new() { Colors = ["#1971c2"] },
                    Horizontal = new() { Colors = ["rgba(134, 142, 150, 0.6)"] },
                }
                : new(),
        };
    }



    private BitResourceTimeline zoneRef = default!;
    private readonly BitResourceTimelineConfig zoneConfig = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(5), fromDay: -21, toDay: 21, labeled: true);
    private string? timeZone = "";
    private string? locale = "";
    private DayOfWeek firstDayOfWeek = DayOfWeek.Monday;
    private bool showUtcTime;
    private bool hour12;
    private BitResourceTimelineOptions zoneOptions = CreateZoneOptions("", "", DayOfWeek.Monday, false, false);

    private void UpdateZoneOptions() => zoneOptions = CreateZoneOptions(timeZone, locale, firstDayOfWeek, showUtcTime, hour12);

    private static BitResourceTimelineOptions CreateZoneOptions(string? timeZone, string? locale, DayOfWeek firstDayOfWeek, bool showUtcTime, bool hour12) => new()
    {
        AutoScrollToNow = true,
        // An empty string is the viewer's own zone and locale: a null option would keep the one applied before.
        TimeZone = timeZone,
        Locale = locale,
        FirstDayOfWeek = firstDayOfWeek,
        ShowUtcTime = showUtcTime,
        // The UTC row shares the band under the day labels, so the axis grows with it.
        TimeAxisHeight = showUtcTime ? 80 : 60,
        Hour12 = hour12,
        NonWorkingDays = [0, 6],
        Surface = new()
        {
            Columns = new() { Unit = BitResourceTimelineSurfaceUnit.Week, Colors = ["rgba(25, 113, 194, 0.1)", "transparent"] },
            ShadeTimeAxis = true,
        },
    };



    private BitResourceTimelineConfig templatesConfig = CreateTemplatesConfig();
    // The axis in UTC, the zone the TooltipTemplate formats its times in: the template is rendered by .NET, whose local
    // zone (a server's) need not be the viewer's the axis would otherwise be drawn in.
    private readonly BitResourceTimelineOptions templatesOptions = new() { TimeZone = "UTC", AutoScrollToNow = true, PreserveScrollOnReload = true };

    private static BitResourceTimelineConfig CreateTemplatesConfig() => BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Terminals(2, 4), labeled: true);

    private int FlightsOf(string resourceId) => templatesConfig.Allocations.Count(f => f.ResourceId == resourceId);



    private readonly BitResourceTimelineConfig columnConfig = CreateColumnConfig();
    private int resourceAxisWidth = 200;
    private bool resourceAxisResizable = true;
    private BitResourceTimelineOptions columnOptions = CreateColumnOptions(200, true);

    private static BitResourceTimelineConfig CreateColumnConfig()
    {
        var config = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(6), labeled: true);

        foreach (var gate in config.Resources)
        {
            gate.Name = $"{gate.Name} - long-haul departures";
        }

        return config;
    }

    // The width a resize committed comes back through OnResourceAxisWidthChanged; passing it back keeps it.
    private void UpdateColumnOptions(int width)
    {
        resourceAxisWidth = width;
        columnOptions = CreateColumnOptions(width, resourceAxisResizable);
    }

    private static BitResourceTimelineOptions CreateColumnOptions(int width, bool resizable) => new()
    {
        AutoScrollToNow = true,
        ResourceAxisWidth = width,
        ResourceAxisMinWidth = 100,
        ResourceAxisMaxWidth = 360,
        ResourceAxisResizable = resizable,
    };



    private BitResourceTimeline largeRef = default!;
    private BitResourceTimelineConfig? largeConfig;
    private readonly BitResourceTimelineOptions largeOptions = new() { AutoScrollToNow = true, MinPixelsPerHour = 1 };
    private string? largeStatus;

    // About 40,000 flights of 160 gates over five weeks, built the first time the example renders.
    private BitResourceTimelineConfig LargeConfig => largeConfig ??= BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Terminals(8, 20), fromDay: -14, toDay: 21);

    private async Task DelayFlights()
    {
        var watch = Stopwatch.StartNew();
        var now = DateTimeOffset.Now;
        var delayed = LargeConfig.Allocations.Where(f => f.StartTime > now && f.EndBar is null).Take(500).ToList();

        foreach (var flight in delayed)
        {
            flight.Color = "#e8590c";
            flight.EndBar = new() { Duration = TimeSpan.FromMinutes(45), Color = "#fa5252" };
        }

        // Merged by id, without reloading the rest or losing the selection.
        await largeRef.UpsertAllocationsAsync(delayed);

        largeStatus = $"{delayed.Count} flights delayed in {watch.ElapsedMilliseconds} ms";
    }

    private async Task CancelFlights()
    {
        var watch = Stopwatch.StartNew();
        var now = DateTimeOffset.Now;
        var cancelled = LargeConfig.Allocations.Where(f => f.StartTime > now).Take(500).Select(f => f.Id).ToHashSet();

        // RemoveAllocationsAsync leaves Config.Allocations alone, so the app keeps it in step itself.
        LargeConfig.Allocations.RemoveAll(f => cancelled.Contains(f.Id));
        await largeRef.RemoveAllocationsAsync([.. cancelled]);

        largeStatus = $"{cancelled.Count} flights cancelled in {watch.ElapsedMilliseconds} ms";
    }

    private async Task ShiftAllFlights()
    {
        var watch = Stopwatch.StartNew();

        foreach (var flight in LargeConfig.Allocations)
        {
            flight.StartTime = flight.StartTime.AddHours(1);
            flight.EndTime = flight.EndTime.AddHours(1);
        }

        // The Config was changed in place and its reference is the same, so ReloadAsync sends it again.
        await largeRef.ReloadAsync();

        largeStatus = $"{LargeConfig.Allocations.Count:N0} flights moved an hour later in {watch.ElapsedMilliseconds} ms";
    }



    // 100 gates over eight months - some 170,000 flights, far too many to load up front. The Config holds the
    // resources and the range alone; the bars come from LoadWindowAsync.
    private readonly BitResourceTimelineConfig windowedConfig = new()
    {
        Resources = BitResourceTimelineDemoData.Terminals(5, 20),
        StartDate = BitResourceTimelineDemoData.Today.AddDays(-120),
        EndDate = BitResourceTimelineDemoData.Today.AddDays(120),
        Allocations = [],
    };
    private readonly BitResourceTimelineOptions windowedOptions = new() { AutoScrollToNow = true, MinPixelsPerHour = 6 };
    private int windowRequests;
    private string windowedStatus = "Waiting for the first window";

    // The timeline drops a window when a newer request arrives before it is applied - while this call works, or while
    // its result waits for the one ahead of it - and every newer request calls this again. So a call says it is loading
    // as it starts, which replaces the status of any window before it, and reports its flights only while no newer call
    // has started: the status never names a window the timeline dropped.
    private async Task<IReadOnlyList<BitResourceTimelineAllocation>> LoadWindowAsync(BitResourceTimelineWindow window)
    {
        var request = ++windowRequests;
        var range = $"{window.Start.ToLocalTime():d MMM HH:mm} - {window.End.ToLocalTime():d MMM HH:mm}";

        windowedStatus = $"Request {request}: loading {range}";
        StateHasChanged();

        await Task.Delay(400); // simulate a query of the server

        var flights = BitResourceTimelineDemoData.Flights(windowedConfig.Resources, window.Start, window.End, labeled: true);

        if (request == windowRequests)
        {
            windowedStatus = $"Request {request}: {range}, {flights.Count:N0} flights";
            StateHasChanged();
        }

        return flights;
    }



    private readonly BitResourceTimelineConfig disabledConfig = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(5), labeled: true);
    private readonly BitResourceTimelineOptions disabledOptions = new() { AutoScrollToNow = true, Editable = true };
    private bool isDisabled = true;
    private int disabledClicks;



    private BitResourceTimelineConfig textsConfig = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(5), labeled: true);
    private readonly BitResourceTimelineOptions textsOptions = new() { AutoScrollToNow = true, Locale = "de-DE" };
    private readonly BitResourceTimelineTexts germanTexts = new()
    {
        AriaLabel = "Belegungsplan der Gates",
        RoleDescription = "Ressourcen-Zeitachse",
        Loading = "Wird geladen…",
        ExpandGroupFormat = "{0} aufklappen",
        CollapseGroupFormat = "{0} zuklappen",
        ResizeResourceColumn = "Breite der Ressourcenspalte ändern",
        BarLocked = "Balken ist gesperrt",
        SelectionCleared = "Auswahl aufgehoben",
        EmptyRowFormat = "{0}, keine Belegungen",
        FocusedBarFormat = "{0}: {1} bis {2}",
        FocusedLabeledBarFormat = "{0}: {1}, {2} bis {3}",
        SelectedBarFormat = "{0}, ausgewählt",
        ResizedFormat = "Größe geändert: {0} bis {1}",
        MovedFormat = "Verschoben: {0} bis {1}",
        MovedToResourceFormat = "Verschoben nach {0}: {1} bis {2}",
        EditRefused = "Änderung abgelehnt",
        EditRefusedOverlap = "Änderung abgelehnt: überschneidet eine andere Belegung",
        GroupCollapsed = "Gruppe zugeklappt",
        GroupExpanded = "Gruppe aufgeklappt",
        CopiedFormat = "{0} kopiert",
        PastedFormat = "{0} eingefügt",
        DeletedFormat = "{0} gelöscht",
        DeleteRefused = "Löschen abgelehnt",
        TimeRangeFormat = "{0} bis {1}",
        HiddenAllocationFormat = "{0} ausgeblendete Belegung",
        HiddenAllocationsFormat = "{0} ausgeblendete Belegungen",
        HiddenAllocationItemFormat = "{0} · {1}",
        MoreHiddenAllocationsFormat = "… und {0} weitere",
    };



    private readonly BitResourceTimelineConfig styleConfig = BitResourceTimelineDemoData.Airport(BitResourceTimelineDemoData.Gates(3), fromDay: 0, toDay: 3, labeled: true);
    private readonly BitResourceTimelineOptions colorsOptions = new()
    {
        Colors = new()
        {
            Bar = "#9c36b5",
            BarSelected = "#5f3dc4",
            BarSelectedBorder = "#e8590c",
            Now = "#e8590c",
            Grid = "rgba(156, 54, 181, 0.2)",
        },
    };
}
