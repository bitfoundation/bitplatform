namespace Bit.BlazorUI;

public partial class BitFcTimelineEventBlock
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarColorScheme ColorScheme { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarChangeNotifier Notifier { get; set; } = default!;

    [Parameter] public BitFullCalendarEvent Event { get; set; } = default!;
    [Parameter] public EventCallback<BitFullCalendarEvent> OnSelected { get; set; }
    [Parameter] public RenderFragment<BitFullCalendarEvent>? EventTemplate { get; set; }

    /// <summary>Pixels per minute on the timeline axis (e.g. 96/60 for hour columns, 56/1440 for day columns).</summary>
    [Parameter] public double PixelsPerMinute { get; set; }

    /// <summary>Snap interval in minutes used while resizing. Hour timelines snap to 30 min, day timelines snap to a full day.</summary>
    [Parameter] public int SnapMinutes { get; set; } = 30;

    /// <summary>Minimum length the event can be resized to (minutes). Defaults to <see cref="SnapMinutes"/>.</summary>
    [Parameter] public int? MinDurationMinutes { get; set; }

    /// <summary>When true, the resize preview shows the date instead of the time-of-day (used by month timeline).</summary>
    [Parameter] public bool PreviewAsDate { get; set; }

    private readonly string _instanceId = Guid.NewGuid().ToString("N");
    private DotNetObjectReference<BitFcTimelineEventBlock>? _dotNetRef;
    private bool _startResizeInitialized;
    private bool _endResizeInitialized;
    private bool _isResizing;
    private string? _resizeDirection;
    private BitFullCalendarEvent? _resizeBaseEvent;
    private DateTime? _previewStart;
    private DateTime? _previewEnd;
    private DateTime _suppressClickUntilUtc;

    private string _startHandleId => $"bit-bfc-tl-resize-start-{_instanceId}";
    private string _endHandleId => $"bit-bfc-tl-resize-end-{_instanceId}";

    private int EffectiveMinDurationMinutes => MinDurationMinutes ?? Math.Max(1, SnapMinutes);

    /// <summary>Pointer movement below this many pixels keeps the original time so the edge does not jump on press.</summary>
    private double DeadZonePx => Math.Max(2.0, EffectiveMinDurationMinutes * PixelsPerMinute / 2.0);

    private double ToPx(TimeSpan span) => span.TotalMinutes * PixelsPerMinute;

    private void OnDragStart()
    {
        if (_isResizing) return;
        State.StartDrag(Event);
    }

    private void OnDragEnd() => State.EndDrag();

    private async Task OnClick()
    {
        if (DateTime.UtcNow <= _suppressClickUntilUtc || _isResizing) return;
        await OnSelected.InvokeAsync(Event);
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is "Enter" or " " or "Spacebar")
            await OnClick();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (PixelsPerMinute <= 0) return;
        if (_startResizeInitialized && _endResizeInitialized) return;

        try
        {
            _dotNetRef ??= DotNetObjectReference.Create(this);
            // Track each handle independently so a failure registering one handle doesn't force a
            // re-registration of the already-bound handle on the next render.
            if (!_startResizeInitialized)
            {
                await JS.InvokeVoidAsync("BitBlazorUI.FullCalendar.initResizeHorizontal", _dotNetRef, _startHandleId, "start");
                _startResizeInitialized = true;
            }
            if (!_endResizeInitialized)
            {
                await JS.InvokeVoidAsync("BitBlazorUI.FullCalendar.initResizeHorizontal", _dotNetRef, _endHandleId, "end");
                _endResizeInitialized = true;
            }
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException or OperationCanceledException)
        {
            // BitFullCalendar JS not yet available, or the circuit/render is mid-teardown; retry on next render.
        }
    }

    [JSInvokable]
    public void OnResizeStart(string direction)
    {
        if (direction is not ("start" or "end"))
            return;

        _isResizing = true;
        _resizeDirection = direction;
        _resizeBaseEvent = Event;
        _previewStart = null;
        _previewEnd = null;
        _suppressClickUntilUtc = DateTime.UtcNow.AddMilliseconds(300);
        State.EndDrag();
        _ = InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public Task OnResizeMove(string direction, double deltaPx)
    {
        if (direction is not ("start" or "end"))
            return Task.CompletedTask;

        if (!_isResizing || _resizeBaseEvent == null || PixelsPerMinute <= 0)
            return Task.CompletedTask;

        // Inside the dead-zone treat the gesture as "no change" so users can press without
        // immediately snapping the edge. Also clears any previously committed preview.
        if (Math.Abs(deltaPx) <= DeadZonePx)
        {
            if (_previewStart.HasValue || _previewEnd.HasValue)
            {
                _previewStart = null;
                _previewEnd = null;
                return InvokeAsync(StateHasChanged);
            }
            return Task.CompletedTask;
        }

        var sign = Math.Sign(deltaPx);
        var effectivePx = deltaPx - sign * DeadZonePx;
        var deltaMinutes = effectivePx / PixelsPerMinute;

        var snap = Math.Max(1, SnapMinutes);
        var minDur = Math.Max(1, EffectiveMinDurationMinutes);
        var baseEvent = _resizeBaseEvent;

        var newStart = baseEvent.StartDate;
        var newEnd = baseEvent.EndDate;

        if (direction == "start")
        {
            var maxStart = baseEvent.EndDate.AddMinutes(-minDur);
            var candidate = baseEvent.StartDate.AddMinutes(deltaMinutes);
            newStart = deltaMinutes > 0
                ? BitFullCalendarHelpers.CeilToMinuteInterval(candidate, snap)
                : BitFullCalendarHelpers.FloorToMinuteInterval(candidate, snap);
            if (newStart > maxStart)
                newStart = BitFullCalendarHelpers.FloorToMinuteInterval(maxStart, snap);
        }
        else
        {
            var minEnd = baseEvent.StartDate.AddMinutes(minDur);
            var candidate = baseEvent.EndDate.AddMinutes(deltaMinutes);
            newEnd = deltaMinutes > 0
                ? BitFullCalendarHelpers.CeilToMinuteInterval(candidate, snap)
                : BitFullCalendarHelpers.FloorToMinuteInterval(candidate, snap);
            if (newEnd < minEnd)
                newEnd = BitFullCalendarHelpers.CeilToMinuteInterval(minEnd, snap);
        }

        // Snapped range matches drag-start range → treat as restored original.
        if (newStart == baseEvent.StartDate && newEnd == baseEvent.EndDate)
        {
            if (_previewStart.HasValue || _previewEnd.HasValue)
            {
                _previewStart = null;
                _previewEnd = null;
                return InvokeAsync(StateHasChanged);
            }
            return Task.CompletedTask;
        }

        var curStart = _previewStart ?? baseEvent.StartDate;
        var curEnd = _previewEnd ?? baseEvent.EndDate;
        if (newStart == curStart && newEnd == curEnd)
            return Task.CompletedTask;

        _previewStart = newStart;
        _previewEnd = newEnd;
        return InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public async Task OnResizeEnd()
    {
        try
        {
            if (_resizeBaseEvent != null && _previewStart.HasValue && _previewEnd.HasValue)
            {
                var s = _previewStart.Value;
                var e = _previewEnd.Value;
                if (s != _resizeBaseEvent.StartDate || e != _resizeBaseEvent.EndDate)
                {
                    var b = _resizeBaseEvent;
                    // Snapshot the previous state before UpdateEvent runs, so the OldEvent payload
                    // is independent of whether the store mutates or replaces the existing instance.
                    var oldSnapshot = BitFullCalendarChangeNotifier.CloneEvent(b);
                    var updated = new BitFullCalendarEvent
                    {
                        Id = b.Id,
                        Title = b.Title,
                        Description = b.Description,
                        StartDate = s,
                        EndDate = e,
                        Color = b.Color,
                        Resource = b.Resource,
                        Data = b.Data,
                        Attendees = [.. b.Attendees]
                    };

                    State.UpdateEvent(updated);

                    try
                    {
                        await Notifier.NotifyAsync(new BitFullCalendarChangeEventArgs
                        {
                            Event = BitFullCalendarChangeNotifier.CloneEvent(updated),
                            OldEvent = oldSnapshot,
                            Kind = BitFullCalendarChangeKind.Edit,
                            Source = BitFullCalendarChangeSource.Resize
                        });
                    }
                    catch
                    {
                        // Notification failed: restore the pre-resize event so local state stays in
                        // sync with what consumers believe, instead of leaving the committed resize.
                        State.UpdateEvent(oldSnapshot);
                        throw;
                    }
                }
            }
        }
        finally
        {
            _previewStart = null;
            _previewEnd = null;
            _isResizing = false;
            _resizeBaseEvent = null;
            _resizeDirection = null;
            _suppressClickUntilUtc = DateTime.UtcNow.AddMilliseconds(300);
        }

        await InvokeAsync(StateHasChanged);
    }

    private void NoOpResize() { /* pointerdown handled in JS via initResizeHorizontal */ }

    private string FormatPreview(DateTime start, DateTime end)
    {
        if (PreviewAsDate)
        {
            // Month timeline: end is exclusive (00:00 next day) so render the inclusive end.
            var displayEnd = end.TimeOfDay == TimeSpan.Zero && end > start ? end.AddDays(-1) : end;
            var startStr = start.ToString("MMM d", State.Culture);
            var endStr = displayEnd.ToString("MMM d", State.Culture);
            return startStr == endStr ? startStr : $"{startStr} - {endStr}";
        }

        return $"{BitFullCalendarHelpers.FormatTime(start, State.Use24HourFormat, State.Culture)} - {BitFullCalendarHelpers.FormatTime(end, State.Use24HourFormat, State.Culture)}";
    }

    public void Dispose() => _dotNetRef?.Dispose();
}
