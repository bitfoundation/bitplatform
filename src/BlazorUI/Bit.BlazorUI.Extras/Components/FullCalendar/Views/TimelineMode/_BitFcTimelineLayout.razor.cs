using System.ComponentModel;

namespace Bit.BlazorUI;

/// <summary>
/// A part of <see cref="BitFullCalendar"/>, rendered by the calendar alone: it is not meant to be used on its own.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class _BitFcTimelineLayout
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [CascadingParameter] internal BitFcParts Parts { get; set; } = default!;

    /// <summary>Total number of time-axis columns (e.g. 24 for day, 168 for week, days-in-month for month).</summary>
    [Parameter] public int ColumnCount { get; set; }

    /// <summary>Width of a single time-axis column in pixels.</summary>
    [Parameter] public int ColumnWidthPx { get; set; } = BitFullCalendarHelpers.TimelineHourWidthPx;

    /// <summary>Whether to render the trailing "Unassigned" row.</summary>
    [Parameter] public bool HasUnassignedRow { get; set; }

    /// <summary>Time-axis header(s) rendered in the top-right cell. Total width must equal <c>ColumnCount * ColumnWidthPx</c>.</summary>
    [Parameter] public RenderFragment? HeaderContent { get; set; }

    /// <summary>Width of the sticky resource gutter on the left.</summary>
    [Parameter] public int ResourceColumnWidthPx { get; set; } = 200;

    /// <summary>Row content for a single resource.</summary>
    [Parameter] public RenderFragment<BitFullCalendarResource> RowContent { get; set; } = default!;

    /// <summary>Resolves the height in pixels for a given resource row (lets each view stack overlapping events into lanes).</summary>
    [Parameter] public Func<BitFullCalendarResource, int> RowHeightFor { get; set; } = _ => 56;

    /// <summary>DOM id assigned to the horizontal scroll container so views can scroll it via JS interop.</summary>
    [Parameter] public string ScrollContainerId { get; set; } = "bit-bfc-tl-scroll-" + Guid.NewGuid().ToString("N");

    /// <summary>Content rendered inside the unassigned row.</summary>
    [Parameter] public RenderFragment? UnassignedRowContent { get; set; }

    /// <summary>Height in pixels for the unassigned row.</summary>
    [Parameter] public int UnassignedRowHeight { get; set; } = 56;
}
