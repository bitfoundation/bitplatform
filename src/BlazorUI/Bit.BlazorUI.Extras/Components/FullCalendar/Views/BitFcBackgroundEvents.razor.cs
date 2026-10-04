using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// Draws the <see cref="BitFullCalendarEvent.IsBackground"/> events that fall inside one stretch of a time axis as
/// bands. A vertical axis (a time-grid day column) is laid out against the hour-height custom property like the
/// event blocks are; a horizontal one (a timeline row) in pixels from <see cref="OffsetPx"/>.
/// </summary>
public partial class BitFcBackgroundEvents
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarColorScheme ColorScheme { get; set; } = default!;
    [CascadingParameter] internal BitFcParts Parts { get; set; } = default!;

    /// <summary>
    /// The calendar's background events (<see cref="BitFullCalendarState.BackgroundEvents"/>). Passed rather than read
    /// off the state so a change to them re-renders the bands even when the axis they are drawn on did not move.
    /// </summary>
    [Parameter] public IReadOnlyList<BitFullCalendarEvent> Events { get; set; } = [];

    /// <summary>The instant the axis stretch starts at.</summary>
    [Parameter] public DateTime AxisStart { get; set; }

    /// <summary>The instant the axis stretch ends at (exclusive).</summary>
    [Parameter] public DateTime AxisEnd { get; set; }

    /// <summary>True for a timeline row, where time runs along the inline axis.</summary>
    [Parameter] public bool Horizontal { get; set; }

    /// <summary>Horizontal only: where the stretch starts inside the row, in pixels.</summary>
    [Parameter] public double OffsetPx { get; set; }

    /// <summary>Horizontal only: the pixels one hour of the stretch occupies.</summary>
    [Parameter] public double PixelsPerHour { get; set; }

    /// <summary>The resource row the stretch belongs to; <c>null</c> for the unassigned row.</summary>
    [Parameter] public string? ResourceId { get; set; }

    /// <summary>True on the event-mode grids, which have no resource rows to tell apart.</summary>
    [Parameter] public bool AnyResource { get; set; } = true;

    private List<Segment> _segments = [];

    protected override void OnParametersSet()
    {
        _segments = [];
        if (Events.Count == 0 || AxisEnd <= AxisStart)
            return;

        var inv = CultureInfo.InvariantCulture;
        foreach (var ev in BitFullCalendarHelpers.GetBackgroundEventsAt(Events, AxisStart, AxisEnd, ResourceId, AnyResource))
        {
            var (bgStart, bgEnd) = BitFullCalendarHelpers.GetOccupiedRange(ev);
            var start = bgStart > AxisStart ? bgStart : AxisStart;
            var end = bgEnd < AxisEnd ? bgEnd : AxisEnd;
            var offsetHours = (start - AxisStart).TotalHours;
            var lengthHours = (end - start).TotalHours;

            var style = Horizontal
                ? $"inset-inline-start:{(OffsetPx + offsetHours * PixelsPerHour).ToString("F2", inv)}px;width:{(lengthHours * PixelsPerHour).ToString("F2", inv)}px;"
                : $"top:{BitFullCalendarHelpers.HoursToCssLength(offsetHours)};height:{BitFullCalendarHelpers.HoursToCssLength(lengthHours)};";

            // A band filling a time-grid column top to bottom is named by the day header instead, which stays in view
            // however the grid is scrolled; every other stretch carries its own title.
            var fillsDay = Horizontal is false && bgStart <= AxisStart.Date && bgEnd >= AxisStart.Date.AddDays(1);
            _segments.Add(new(ev, style, ShowTitle: fillsDay is false));
        }
    }

    private sealed record Segment(BitFullCalendarEvent Event, string Style, bool ShowTitle);
}
