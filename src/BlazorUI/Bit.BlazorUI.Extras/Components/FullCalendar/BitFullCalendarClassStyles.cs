namespace Bit.BlazorUI;

public class BitFullCalendarClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitFullCalendar.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the toolbar of the BitFullCalendar: the navigation, the mode and view tabs,
    /// the filters, the add button and the settings gear.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the body of the BitFullCalendar, the region that renders the active view.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for every event the BitFullCalendar draws: the month badges, the day and week
    /// blocks, the timeline blocks and the agenda rows. Applied alongside the event's own CssClass.
    /// </summary>
    public string? Event { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the built-in dialogs of the BitFullCalendar (add/edit, details, event list and
    /// the recurrence scope choice).
    /// </summary>
    public string? Dialog { get; set; }
}
