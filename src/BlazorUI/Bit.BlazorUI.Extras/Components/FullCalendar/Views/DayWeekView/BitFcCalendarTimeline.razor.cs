namespace Bit.BlazorUI;

public partial class BitFcCalendarTimeline
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;

    private double _positionPx;
    private Timer? _timer;

    protected override void OnInitialized()
    {
        UpdatePosition();
        _timer = new Timer(_ =>
        {
            // Run both the state mutation and the re-render on the renderer's dispatcher so
            // _positionPx is never modified outside the synchronization context.
            InvokeAsync(() =>
            {
                UpdatePosition();
                StateHasChanged();
            });
        }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    private void UpdatePosition()
    {
        _positionPx = BitFullCalendarHelpers.GetCurrentTimeLineTopPx();
    }

    public void Dispose() => _timer?.Dispose();
}
