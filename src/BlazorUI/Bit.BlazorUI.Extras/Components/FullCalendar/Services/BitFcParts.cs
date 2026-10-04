namespace Bit.BlazorUI;

/// <summary>
/// What the inner parts of one calendar need to know about the calendar that hosts them: its
/// <see cref="BitFullCalendar.Classes"/> and <see cref="BitFullCalendar.Styles"/> (both of the same type, so they
/// travel together rather than as two cascades told apart by name), the id its other element ids are derived from,
/// whether its toolbar is rendered at all, and the templates no single view hands down: a timeline resource row's
/// header, a month-grid day's content and an event row of the agenda and of the event lists.
/// </summary>
internal sealed record BitFcParts(BitFullCalendarClassStyles? Classes,
                                 BitFullCalendarClassStyles? Styles,
                                 string CalendarId,
                                 bool HideHeader,
                                 RenderFragment<BitFullCalendarResource>? ResourceTemplate = null,
                                 RenderFragment<BitFullCalendarCell>? MonthCellTemplate = null,
                                 RenderFragment<BitFullCalendarEvent>? AgendaEventTemplate = null)
{
    /// <summary>The id of the body, the region every view renders into and the panel the view tabs control.</summary>
    public string BodyId => $"{CalendarId}-body";

    /// <summary>The id of the view tab of <paramref name="view"/>.</summary>
    public string ViewTabId(BitFullCalendarView view) => $"{CalendarId}-view-{(int)view}";

    /// <summary>The id of the mode tab of <paramref name="mode"/>.</summary>
    public string ModeTabId(BitFullCalendarMode mode) => $"{CalendarId}-mode-{(int)mode}";
}
