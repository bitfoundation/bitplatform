using System.ComponentModel;

namespace Bit.BlazorUI;

/// <summary>
/// A part of <see cref="BitFullCalendar"/>, rendered by the calendar alone: it is not meant to be used on its own.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class _BitFcBody
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] internal BitFcParts Parts { get; set; } = default!;

    [Parameter] public RenderFragment<BitFullCalendarEvent>? DayEventTemplate { get; set; }

    [Parameter] public bool Loading { get; set; }

    [Parameter] public RenderFragment<BitFullCalendarEvent>? MonthEventTemplate { get; set; }

    [Parameter] public RenderFragment<BitFullCalendarEvent>? TimelineEventTemplate { get; set; }

    [Parameter] public RenderFragment<BitFullCalendarEvent>? WeekEventTemplate { get; set; }

    // The tab strips render only with the toolbar, the view strip only with more than one view and the mode strip
    // only while Timeline is reachable - and the panel role is worth stating only when a tab actually controls it.
    private bool _HasViewTabs => Parts.HideHeader is false && State.AvailableViews.Count > 1;
    private bool _HasModeTabs => Parts.HideHeader is false && State.IsTimelineModeAvailable;
    private bool _IsTabPanel => _HasViewTabs || _HasModeTabs;

    private string _LabelledBy => string.Join(' ', new[]
    {
        _HasModeTabs ? Parts.ModeTabId(State.Mode) : null,
        _HasViewTabs ? Parts.ViewTabId(State.View) : null,
    }.Where(id => id is not null));

    private List<BitFullCalendarEvent> _singleDayEvents = [];
    private List<BitFullCalendarEvent> _multiDayEvents = [];
    private List<BitFullCalendarEvent> _timelineEvents = [];

    protected override void OnInitialized()
    {
        State.OnStateChanged += Refresh;
        ComputeEvents();
    }

    private void Refresh()
    {
        ComputeEvents();
        InvokeAsync(StateHasChanged);
    }

    private void ComputeEvents()
    {
        // The split decides which surface renders an event: the hour grid, or the all-day row above
        // it. An event explicitly marked all-day joins the multi-day bucket even when it covers a
        // single date, so it is never placed on the time axis.
        _singleDayEvents = State.Events.Where(e => e.IsAllDayOrMultiDay is false).ToList();
        _multiDayEvents = State.Events.Where(e => e.IsAllDayOrMultiDay).ToList();
        _timelineEvents = State.Events.ToList();
    }

    public void Dispose() => State.OnStateChanged -= Refresh;
}
