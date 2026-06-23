namespace Bit.BlazorUI;

public partial class BitFcCalendarTimeline
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;

    private double _positionPx;
    private Timer? _timer;
    private bool _isDisposed;

    protected override void OnInitialized()
    {
        UpdatePosition();

        // Align the first tick to the next clock-minute boundary so the "now" marker doesn't lag
        // by up to ~60s; subsequent ticks fire every minute.
        var now = DateTime.Now;
        var msUntilNextMinute = 60_000 - ((now.Second * 1000) + now.Millisecond);
        var dueTime = TimeSpan.FromMilliseconds(msUntilNextMinute);

        _timer = new Timer(_ =>
        {
            // The timer can fire after disposal; skip queuing work so StateHasChanged isn't called
            // on a disposed component (which throws ObjectDisposedException).
            if (_isDisposed)
                return;

            // Run both the state mutation and the re-render on the renderer's dispatcher so
            // _positionPx is never modified outside the synchronization context.
            InvokeAsync(() =>
            {
                if (_isDisposed)
                    return;

                UpdatePosition();
                StateHasChanged();
            });
        }, null, dueTime, TimeSpan.FromMinutes(1));
    }

    private void UpdatePosition()
    {
        _positionPx = BitFullCalendarHelpers.GetCurrentTimeLineTopPx();
    }

    public void Dispose()
    {
        _isDisposed = true;
        _timer?.Dispose();
    }
}
