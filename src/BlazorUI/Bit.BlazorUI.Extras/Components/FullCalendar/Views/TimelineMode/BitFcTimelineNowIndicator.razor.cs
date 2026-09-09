namespace Bit.BlazorUI;

/// <summary>
/// The "current time" rule of a timeline row. The day/week timelines lay an hour on
/// <see cref="PixelsPerMinute"/> * 60 pixels and the month timeline lays a whole day on one column,
/// so the same component serves all three: the caller says where its column starts
/// (<see cref="ColumnOffsetPx"/>), which instant that column starts at (<see cref="DayStart"/>), and
/// how wide a minute is. The marker keeps itself current on a per-minute timer, the way the day and
/// week grids' own indicator does.
/// </summary>
public partial class BitFcTimelineNowIndicator : IDisposable
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;

    /// <summary>The date the column belongs to. The marker only renders while that date is today.</summary>
    [Parameter] public DateTime Day { get; set; }

    /// <summary>Pixel offset of the column's leading edge inside the row.</summary>
    [Parameter] public double ColumnOffsetPx { get; set; }

    /// <summary>Pixels one minute of the time axis occupies.</summary>
    [Parameter] public double PixelsPerMinute { get; set; }

    /// <summary>First hour the column renders (inclusive).</summary>
    [Parameter] public int VisibleStartHour { get; set; }

    /// <summary>Last hour the column renders (exclusive).</summary>
    [Parameter] public int VisibleEndHour { get; set; } = 24;

    private double? _offsetPx;
    private Timer? _timer;
    private bool _isDisposed;

    protected override void OnInitialized()
    {
        UpdateOffset();

        // Align the first tick to the next clock-minute boundary so the marker doesn't lag by up to
        // ~60s; subsequent ticks fire every minute.
        var now = DateTime.Now;
        var dueTime = TimeSpan.FromMilliseconds(60_000 - ((now.Second * 1000) + now.Millisecond));

        _timer = new Timer(_ =>
        {
            if (_isDisposed)
                return;

            // Both the state mutation and the re-render run on the renderer's dispatcher so
            // _offsetPx is never modified outside the synchronization context.
            InvokeAsync(() =>
            {
                if (_isDisposed)
                    return;

                UpdateOffset();
                StateHasChanged();
            });
        }, null, dueTime, TimeSpan.FromMinutes(1));
    }

    protected override void OnParametersSet() => UpdateOffset();

    private void UpdateOffset()
    {
        var now = DateTime.Now;
        if (Day.Date != now.Date || PixelsPerMinute <= 0)
        {
            _offsetPx = null;
            return;
        }

        var (start, end) = BitFullCalendarHelpers.NormalizeVisibleHours(VisibleStartHour, VisibleEndHour);
        var hours = now.TimeOfDay.TotalHours;
        if (hours < start || hours >= end)
        {
            _offsetPx = null;
            return;
        }

        var minutesIntoColumn = (hours - start) * 60.0;
        _offsetPx = ColumnOffsetPx + (minutesIntoColumn * PixelsPerMinute);
    }

    public void Dispose()
    {
        _isDisposed = true;
        _timer?.Dispose();
    }
}
