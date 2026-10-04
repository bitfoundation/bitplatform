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

        if (AgendaEventTemplate is not null && bitFullCalendar.HasNotBeenSet(nameof(AgendaEventTemplate)))
        {
            bitFullCalendar.AgendaEventTemplate = AgendaEventTemplate;
        }

        if (Classes is not null && bitFullCalendar.HasNotBeenSet(nameof(Classes)) && bitFullCalendar.Classes != Classes)
        {
            bitFullCalendar.Classes = Classes;

            bitFullCalendar.ClassBuilder.Reset();
        }

        if (Culture is not null && bitFullCalendar.HasNotBeenSet(nameof(Culture)))
        {
            bitFullCalendar.Culture = Culture;
        }

        if (CultureName.HasValue() && bitFullCalendar.HasNotBeenSet(nameof(CultureName)))
        {
            bitFullCalendar.CultureName = CultureName;
        }

        if (DayEventTemplate is not null && bitFullCalendar.HasNotBeenSet(nameof(DayEventTemplate)))
        {
            bitFullCalendar.DayEventTemplate = DayEventTemplate;
        }

        if (DefaultMode.HasValue && bitFullCalendar.HasNotBeenSet(nameof(DefaultMode)))
        {
            bitFullCalendar.DefaultMode = DefaultMode.Value;
        }

        if (DefaultView.HasValue && bitFullCalendar.HasNotBeenSet(nameof(DefaultView)))
        {
            bitFullCalendar.DefaultView = DefaultView.Value;
        }

        if (EventColorOptions is not null && bitFullCalendar.HasNotBeenSet(nameof(EventColorOptions)))
        {
            bitFullCalendar.EventColorOptions = EventColorOptions;
        }

        if (HideFilters.HasValue && bitFullCalendar.HasNotBeenSet(nameof(HideFilters)))
        {
            bitFullCalendar.HideFilters = HideFilters.Value;
        }

        if (HideHeader.HasValue && bitFullCalendar.HasNotBeenSet(nameof(HideHeader)))
        {
            bitFullCalendar.HideHeader = HideHeader.Value;
        }

        if (HideSettings.HasValue && bitFullCalendar.HasNotBeenSet(nameof(HideSettings)))
        {
            bitFullCalendar.HideSettings = HideSettings.Value;
        }

        if (MaxDate.HasValue && bitFullCalendar.HasNotBeenSet(nameof(MaxDate)))
        {
            bitFullCalendar.MaxDate = MaxDate.Value;
        }

        if (MinDate.HasValue && bitFullCalendar.HasNotBeenSet(nameof(MinDate)))
        {
            bitFullCalendar.MinDate = MinDate.Value;
        }

        if (MonthCellTemplate is not null && bitFullCalendar.HasNotBeenSet(nameof(MonthCellTemplate)))
        {
            bitFullCalendar.MonthCellTemplate = MonthCellTemplate;
        }

        if (MonthEventTemplate is not null && bitFullCalendar.HasNotBeenSet(nameof(MonthEventTemplate)))
        {
            bitFullCalendar.MonthEventTemplate = MonthEventTemplate;
        }

        if (ReadOnly.HasValue && bitFullCalendar.HasNotBeenSet(nameof(ReadOnly)))
        {
            bitFullCalendar.ReadOnly = ReadOnly.Value;
        }

        if (ResourceTemplate is not null && bitFullCalendar.HasNotBeenSet(nameof(ResourceTemplate)))
        {
            bitFullCalendar.ResourceTemplate = ResourceTemplate;
        }

        // The calendar writes the user's preference changes back onto its Settings object, so the shared one is
        // assigned as it is - never copied - or a change made from one calendar's panel would be lost on the next pass.
        if (Settings is not null && bitFullCalendar.HasNotBeenSet(nameof(Settings)))
        {
            bitFullCalendar.Settings = Settings;
        }

        if (Styles is not null && bitFullCalendar.HasNotBeenSet(nameof(Styles)) && bitFullCalendar.Styles != Styles)
        {
            bitFullCalendar.Styles = Styles;

            bitFullCalendar.StyleBuilder.Reset();
        }

        if (Texts is not null && bitFullCalendar.HasNotBeenSet(nameof(Texts)))
        {
            bitFullCalendar.Texts = Texts;
        }

        if (TimelineEventTemplate is not null && bitFullCalendar.HasNotBeenSet(nameof(TimelineEventTemplate)))
        {
            bitFullCalendar.TimelineEventTemplate = TimelineEventTemplate;
        }

        if (Views is not null && bitFullCalendar.HasNotBeenSet(nameof(Views)))
        {
            bitFullCalendar.Views = Views;
        }

        if (WeekEventTemplate is not null && bitFullCalendar.HasNotBeenSet(nameof(WeekEventTemplate)))
        {
            bitFullCalendar.WeekEventTemplate = WeekEventTemplate;
        }
    }
}
