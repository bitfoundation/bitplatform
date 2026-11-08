namespace Bit.BlazorUI;

/// <summary>
/// The texts of the <see cref="BitResourceTimeline"/>: its accessible name and description, the loading text, the
/// labels of its controls and everything it announces to screen readers. All of them default to English; an instance
/// with the ones to override localizes the timeline. A text ending in Format is a composite format string whose
/// placeholders the remarks of the property describe.
/// </summary>
public class BitResourceTimelineTexts
{
    /// <summary>The accessible name of the timeline while its own AriaLabel is not set.</summary>
    public string AriaLabel { get; set; } = "Resource timeline";

    /// <summary>The accessible role description of the focusable viewport of the timeline.</summary>
    public string RoleDescription { get; set; } = "Resource timeline";

    /// <summary>The text of the default loading overlay.</summary>
    public string Loading { get; set; } = "Loading…";

    /// <summary>The accessible name of the button expanding a collapsed group row of the resource template; {0} is the name of the resource.</summary>
    public string ExpandGroupFormat { get; set; } = "Expand {0}";

    /// <summary>The accessible name of the button collapsing an expanded group row of the resource template; {0} is the name of the resource.</summary>
    public string CollapseGroupFormat { get; set; } = "Collapse {0}";

    /// <summary>The accessible name of the divider that resizes the resource column.</summary>
    public string ResizeResourceColumn { get; set; } = "Resize resource column";

    /// <summary>Announced when an edit key is pressed on a locked bar.</summary>
    public string BarLocked { get; set; } = "Bar is locked";

    /// <summary>Announced when Escape clears the selection.</summary>
    public string SelectionCleared { get; set; } = "Selection cleared";

    /// <summary>Announced when the keyboard focus moves to a row without bars; {0} is the name of the resource.</summary>
    public string EmptyRowFormat { get; set; } = "{0}, no allocations";

    /// <summary>Announced when the keyboard focus moves to a bar without a label; {0} is the name of the resource, {1} the start and {2} the end.</summary>
    public string FocusedBarFormat { get; set; } = "{0}: {1} to {2}";

    /// <summary>Announced when the keyboard focus moves to a bar with a label; {0} is the name of the resource, {1} the label, {2} the start and {3} the end.</summary>
    public string FocusedLabeledBarFormat { get; set; } = "{0}: {1}, {2} to {3}";

    /// <summary>Announced for a focused bar that is selected; {0} is the announcement of the bar (<see cref="FocusedBarFormat"/> or <see cref="FocusedLabeledBarFormat"/>).</summary>
    public string SelectedBarFormat { get; set; } = "{0}, selected";

    /// <summary>Announced after a bar is resized from the keyboard; {0} is the new start and {1} the new end.</summary>
    public string ResizedFormat { get; set; } = "Resized: {0} to {1}";

    /// <summary>Announced after a bar is moved in time from the keyboard; {0} is the new start and {1} the new end.</summary>
    public string MovedFormat { get; set; } = "Moved: {0} to {1}";

    /// <summary>Announced after a bar is moved to another resource from the keyboard; {0} is the name of that resource, {1} the start and {2} the end.</summary>
    public string MovedToResourceFormat { get; set; } = "Moved to {0}: {1} to {2}";

    /// <summary>Announced when an edit is refused.</summary>
    public string EditRefused { get; set; } = "Edit refused";

    /// <summary>Announced when an edit is refused because the bar would overlap another one.</summary>
    public string EditRefusedOverlap { get; set; } = "Edit refused: overlaps another allocation";

    /// <summary>Announced when a group row is collapsed.</summary>
    public string GroupCollapsed { get; set; } = "Collapsed group";

    /// <summary>Announced when a group row is expanded.</summary>
    public string GroupExpanded { get; set; } = "Expanded group";

    /// <summary>Announced after bars are copied; {0} is the number of bars.</summary>
    public string CopiedFormat { get; set; } = "Copied {0}";

    /// <summary>Announced after bars are pasted; {0} is the number of bars.</summary>
    public string PastedFormat { get; set; } = "Pasted {0}";

    /// <summary>Announced after bars are deleted; {0} is the number of bars.</summary>
    public string DeletedFormat { get; set; } = "Deleted {0}";

    /// <summary>Announced when a delete is refused.</summary>
    public string DeleteRefused { get; set; } = "Delete refused";

    /// <summary>A time range in the default tooltip, the list of a +N marker and the readout of a drag; {0} is the start and {1} the end.</summary>
    public string TimeRangeFormat { get; set; } = "{0} – {1}";

    /// <summary>The first line of the tooltip of a +N marker hiding a single bar; {0} is the count (1).</summary>
    public string HiddenAllocationFormat { get; set; } = "{0} hidden allocation";

    /// <summary>The first line of the tooltip of a +N marker hiding several bars; {0} is the count.</summary>
    public string HiddenAllocationsFormat { get; set; } = "{0} hidden allocations";

    /// <summary>A line of the tooltip of a +N marker; {0} is the label of the bar and {1} its time range (<see cref="TimeRangeFormat"/>).</summary>
    public string HiddenAllocationItemFormat { get; set; } = "{0} · {1}";

    /// <summary>The last line of the tooltip of a +N marker that lists only some of its bars; {0} is the number of bars left out.</summary>
    public string MoreHiddenAllocationsFormat { get; set; } = "… and {0} more";
}
