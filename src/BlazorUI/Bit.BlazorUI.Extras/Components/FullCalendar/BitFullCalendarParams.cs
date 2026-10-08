using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitFullCalendar"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the calendars of an application agree on - the culture and the strings they speak,
/// the preferences and the views they offer, the colours their events are drawn in, the toolbar they show and how
/// their events are rendered. The data, the bound view/mode/date, the loading state and the callbacks are
/// deliberately not here: they are what makes one calendar the one it is.
/// <br />
/// A <see cref="Settings"/> object handed down here is shared by every calendar under the <see cref="BitParams"/>,
/// and a preference the user changes from the settings panel of one of them is written back onto it - so the
/// calendars keep the same preferences, which is what sharing one object asks for.
/// </remarks>
public class BitFullCalendarParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitFullCalendar"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitFullCalendar value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitFullCalendar)}";



    public string Name => ParamName;



    /// <summary>
    /// Gets or sets the template that renders the content of an event row in the agenda view and the event lists.
    /// </summary>
    public RenderFragment<BitFullCalendarEvent>? AgendaEventTemplate { get; set; }

    /// <summary>
    /// Gets or sets the custom CSS classes for the different parts of the calendar.
    /// </summary>
    public BitFullCalendarClassStyles? Classes { get; set; }

    /// <summary>
    /// Gets or sets the culture of the calendar.
    /// </summary>
    public CultureInfo? Culture { get; set; }

    /// <summary>
    /// Gets or sets the culture name of the calendar (e.g. "fa-IR"), which takes precedence over Culture.
    /// </summary>
    public string? CultureName { get; set; }

    /// <summary>
    /// Gets or sets the template that renders an event in the day view.
    /// </summary>
    public RenderFragment<BitFullCalendarEvent>? DayEventTemplate { get; set; }

    /// <summary>
    /// Gets or sets the layout mode a calendar opens in when its Mode is not bound.
    /// </summary>
    public BitFullCalendarMode? DefaultMode { get; set; }

    /// <summary>
    /// Gets or sets the view a calendar opens in when its View is not bound.
    /// </summary>
    public BitFullCalendarView? DefaultView { get; set; }

    /// <summary>
    /// Gets or sets the ordered list of event colors offered by the calendar.
    /// </summary>
    public IReadOnlyList<BitFullCalendarColorOption>? EventColorOptions { get; set; }

    /// <summary>
    /// Gets or sets the extra content of the built-in event details dialog.
    /// </summary>
    public RenderFragment<BitFullCalendarEvent>? EventDetailsTemplate { get; set; }

    /// <summary>
    /// Gets or sets the extra fields of the built-in add/edit dialog.
    /// </summary>
    public RenderFragment<BitFullCalendarEvent>? EventEditorTemplate { get; set; }

    /// <summary>
    /// Gets or sets whether the built-in color and attendee filters are hidden.
    /// </summary>
    public bool? HideFilters { get; set; }

    /// <summary>
    /// Gets or sets whether the whole toolbar is removed.
    /// </summary>
    public bool? HideHeader { get; set; }

    /// <summary>
    /// Gets or sets whether the settings gear is hidden.
    /// </summary>
    public bool? HideSettings { get; set; }

    /// <summary>
    /// Gets or sets the latest date the calendar can navigate to and display.
    /// </summary>
    public DateTime? MaxDate { get; set; }

    /// <summary>
    /// Gets or sets the earliest date the calendar can navigate to and display.
    /// </summary>
    public DateTime? MinDate { get; set; }

    /// <summary>
    /// Gets or sets the template that renders extra content in each day of the month grid.
    /// </summary>
    public RenderFragment<BitFullCalendarCell>? MonthCellTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template that renders an event in the month view.
    /// </summary>
    public RenderFragment<BitFullCalendarEvent>? MonthEventTemplate { get; set; }

    /// <summary>
    /// Gets or sets whether the calendar is presentation-only.
    /// </summary>
    public bool? ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the preferences of the calendar (time format, hour window, slot duration, hidden days, ...).
    /// </summary>
    public BitFullCalendarSettings? Settings { get; set; }

    /// <summary>
    /// Gets or sets the template that renders the header cell of a resource row in the timeline.
    /// </summary>
    public RenderFragment<BitFullCalendarResource>? ResourceTemplate { get; set; }

    /// <summary>
    /// Gets or sets the custom CSS styles for the different parts of the calendar.
    /// </summary>
    public BitFullCalendarClassStyles? Styles { get; set; }

    /// <summary>
    /// Gets or sets the localized strings of the calendar.
    /// </summary>
    public BitFullCalendarTexts? Texts { get; set; }

    /// <summary>
    /// Gets or sets the clock the calendars read "now" and "today" from (the user's time zone on Blazor Server).
    /// </summary>
    public TimeProvider? TimeProvider { get; set; }

    /// <summary>
    /// Gets or sets the template that renders an event in the resource timeline.
    /// </summary>
    public RenderFragment<BitFullCalendarEvent>? TimelineEventTemplate { get; set; }

    /// <summary>
    /// Gets or sets the views the calendar offers, in the order the view tabs render them.
    /// </summary>
    public IReadOnlyList<BitFullCalendarView>? Views { get; set; }

    /// <summary>
    /// Gets or sets the template that renders an event in the week view.
    /// </summary>
    public RenderFragment<BitFullCalendarEvent>? WeekEventTemplate { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitFullCalendar"/> instance with any values that have been
    /// set on this object, if those properties have not already been set on the <see cref="BitFullCalendar"/> itself.
    /// </summary>
    /// <param name="bitFullCalendar">
    /// The <see cref="BitFullCalendar"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitFullCalendar bitFullCalendar)
    {
        if (bitFullCalendar is null) return;

        UpdateBaseParameters(bitFullCalendar);

        if (AgendaEventTemplate is not null)
        {
            bitFullCalendar.TakeFromCascade(nameof(AgendaEventTemplate), AgendaEventTemplate, static f => f.AgendaEventTemplate, static (f, v) => f.AgendaEventTemplate = v);
        }

        if (Classes is not null)
        {
            bitFullCalendar.TakeFromCascade(nameof(Classes), Classes, static f => f.Classes, static (f, v) => f.Classes = v);
        }

        if (Culture is not null)
        {
            bitFullCalendar.TakeFromCascade(nameof(Culture), Culture, static f => f.Culture, static (f, v) => f.Culture = v);
        }

        if (CultureName.HasValue())
        {
            bitFullCalendar.TakeFromCascade(nameof(CultureName), CultureName, static f => f.CultureName, static (f, v) => f.CultureName = v);
        }

        if (DayEventTemplate is not null)
        {
            bitFullCalendar.TakeFromCascade(nameof(DayEventTemplate), DayEventTemplate, static f => f.DayEventTemplate, static (f, v) => f.DayEventTemplate = v);
        }

        if (DefaultMode.HasValue)
        {
            bitFullCalendar.TakeFromCascade(nameof(DefaultMode), DefaultMode.Value, static f => f.DefaultMode, static (f, v) => f.DefaultMode = v);
        }

        if (DefaultView.HasValue)
        {
            bitFullCalendar.TakeFromCascade(nameof(DefaultView), DefaultView.Value, static f => f.DefaultView, static (f, v) => f.DefaultView = v);
        }

        if (EventColorOptions is not null)
        {
            bitFullCalendar.TakeFromCascade(nameof(EventColorOptions), EventColorOptions, static f => f.EventColorOptions, static (f, v) => f.EventColorOptions = v);
        }

        if (EventDetailsTemplate is not null)
        {
            bitFullCalendar.TakeFromCascade(nameof(EventDetailsTemplate), EventDetailsTemplate, static f => f.EventDetailsTemplate, static (f, v) => f.EventDetailsTemplate = v);
        }

        if (EventEditorTemplate is not null)
        {
            bitFullCalendar.TakeFromCascade(nameof(EventEditorTemplate), EventEditorTemplate, static f => f.EventEditorTemplate, static (f, v) => f.EventEditorTemplate = v);
        }

        if (HideFilters.HasValue)
        {
            bitFullCalendar.TakeFromCascade(nameof(HideFilters), HideFilters.Value, static f => f.HideFilters, static (f, v) => f.HideFilters = v);
        }

        if (HideHeader.HasValue)
        {
            bitFullCalendar.TakeFromCascade(nameof(HideHeader), HideHeader.Value, static f => f.HideHeader, static (f, v) => f.HideHeader = v);
        }

        if (HideSettings.HasValue)
        {
            bitFullCalendar.TakeFromCascade(nameof(HideSettings), HideSettings.Value, static f => f.HideSettings, static (f, v) => f.HideSettings = v);
        }

        if (MaxDate.HasValue)
        {
            bitFullCalendar.TakeFromCascade(nameof(MaxDate), MaxDate.Value, static f => f.MaxDate, static (f, v) => f.MaxDate = v);
        }

        if (MinDate.HasValue)
        {
            bitFullCalendar.TakeFromCascade(nameof(MinDate), MinDate.Value, static f => f.MinDate, static (f, v) => f.MinDate = v);
        }

        if (MonthCellTemplate is not null)
        {
            bitFullCalendar.TakeFromCascade(nameof(MonthCellTemplate), MonthCellTemplate, static f => f.MonthCellTemplate, static (f, v) => f.MonthCellTemplate = v);
        }

        if (MonthEventTemplate is not null)
        {
            bitFullCalendar.TakeFromCascade(nameof(MonthEventTemplate), MonthEventTemplate, static f => f.MonthEventTemplate, static (f, v) => f.MonthEventTemplate = v);
        }

        if (ReadOnly.HasValue)
        {
            bitFullCalendar.TakeFromCascade(nameof(ReadOnly), ReadOnly.Value, static f => f.ReadOnly, static (f, v) => f.ReadOnly = v);
        }

        if (ResourceTemplate is not null)
        {
            bitFullCalendar.TakeFromCascade(nameof(ResourceTemplate), ResourceTemplate, static f => f.ResourceTemplate, static (f, v) => f.ResourceTemplate = v);
        }

        // The calendar writes the user's preference changes back onto its Settings object, so the shared one is
        // assigned as it is - never copied - or a change made from one calendar's panel would be lost on the next pass.
        if (Settings is not null)
        {
            bitFullCalendar.TakeFromCascade(nameof(Settings), Settings, static f => f.Settings, static (f, v) => f.Settings = v);
        }

        if (Styles is not null)
        {
            bitFullCalendar.TakeFromCascade(nameof(Styles), Styles, static f => f.Styles, static (f, v) => f.Styles = v);
        }

        if (Texts is not null)
        {
            bitFullCalendar.TakeFromCascade(nameof(Texts), Texts, static f => f.Texts, static (f, v) => f.Texts = v);
        }

        if (TimeProvider is not null)
        {
            bitFullCalendar.TakeFromCascade(nameof(TimeProvider), TimeProvider, static f => f.TimeProvider, static (f, v) => f.TimeProvider = v);
        }

        if (TimelineEventTemplate is not null)
        {
            bitFullCalendar.TakeFromCascade(nameof(TimelineEventTemplate), TimelineEventTemplate, static f => f.TimelineEventTemplate, static (f, v) => f.TimelineEventTemplate = v);
        }

        if (Views is not null)
        {
            bitFullCalendar.TakeFromCascade(nameof(Views), Views, static f => f.Views, static (f, v) => f.Views = v);
        }

        if (WeekEventTemplate is not null)
        {
            bitFullCalendar.TakeFromCascade(nameof(WeekEventTemplate), WeekEventTemplate, static f => f.WeekEventTemplate, static (f, v) => f.WeekEventTemplate = v);
        }
    }
}
