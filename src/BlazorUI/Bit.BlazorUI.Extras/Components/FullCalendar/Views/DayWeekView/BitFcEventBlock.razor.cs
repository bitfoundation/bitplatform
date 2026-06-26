namespace Bit.BlazorUI;

public partial class BitFcEventBlock
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarColorScheme ColorScheme { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarChangeNotifier Notifier { get; set; } = default!;
    [Parameter] public BitFullCalendarEvent Event { get; set; } = default!;
    [Parameter] public EventCallback<BitFullCalendarEvent> OnSelected { get; set; }
    [Parameter] public RenderFragment<BitFullCalendarEvent>? EventTemplate { get; set; }

    private readonly string _instanceId = Guid.NewGuid().ToString("N");
    private DotNetObjectReference<BitFcEventBlock>? _dotNetRef;
    private BitFullCalendarEvent? _resizeBaseEvent;
    private string? _resizeDirection;
    private DateTime? _previewStart;
    private DateTime? _previewEnd;
    private bool _resizeInitialized;
    private bool _isResizing;
    private DateTime _suppressClickUntilUtc;
    private string _topHandleId => $"bit-bfc-resize-top-{_instanceId}";
    private string _bottomHandleId => $"bit-bfc-resize-bottom-{_instanceId}";

    /// <summary>Minimum event length enforced by resize (minutes).</summary>
    private const int MinEventDurationMinutes = 30;

    /// <summary>
    /// Pointer movement below this (in minutes along the time axis) does not change start/end,
    /// so the edge does not jump as soon as the user presses the handle.
    /// </summary>
    private const int ResizeDeadZoneMinutes = MinEventDurationMinutes / 2;

    private void OnDragStart()
    {
        if (_isResizing)
            return;
            
        State.StartDrag(Event);
    }

    private void OnDragEnd() => State.EndDrag();

    private async Task OnClick()
    {
        if (DateTime.UtcNow <= _suppressClickUntilUtc || _isResizing)
            return;

        await OnSelected.InvokeAsync(Event);
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (_isResizing)
            return;

        // Ignore auto-repeat keydown events (matching the month badge logic) so holding
        // Enter/Space cannot fire OnSelected repeatedly for the same event.
        if (e.Key is "Enter" or " " or "Spacebar" && !e.Repeat)
            await OnSelected.InvokeAsync(Event);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_resizeInitialized)
            return;

        try
        {
            _dotNetRef ??= DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("BitBlazorUI.FullCalendar.initResize", _dotNetRef, _topHandleId, "top");
            await JS.InvokeVoidAsync("BitBlazorUI.FullCalendar.initResize", _dotNetRef, _bottomHandleId, "bottom");
            _resizeInitialized = true;
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException or OperationCanceledException)
        {
            // BitBlazorUI.FullCalendar JS not yet available, or the circuit/render is mid-teardown; retry on next render.
        }
    }

    [JSInvokable]
    public void OnResizeStart(string direction)
    {
        // Guard against unrecognized directions from JS interop so the block can't enter resize
        // mode with a direction that OnResizeMove would later ignore (leaving it stuck "resizing").
        if (direction is not ("top" or "bottom"))
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
    public Task OnResizeMove(string direction, int deltaMinutes)
    {
        if (!_isResizing || _resizeBaseEvent == null)
            return Task.CompletedTask;

        // Finger back at (or very near) the grab point → show the original span again and cancel
        // any in-progress preview so the user can "undo" without releasing early.
        if (deltaMinutes == 0 || Math.Abs(deltaMinutes) <= ResizeDeadZoneMinutes)
        {
            if (_previewStart.HasValue || _previewEnd.HasValue)
            {
                _previewStart = null;
                _previewEnd = null;
                return InvokeAsync(StateHasChanged);
            }
            return Task.CompletedTask;
        }

        var effectiveDelta = deltaMinutes - Math.Sign(deltaMinutes) * ResizeDeadZoneMinutes;
        const int slotMinutes = MinEventDurationMinutes;
        var baseEvent = _resizeBaseEvent;

        var newStart = baseEvent.StartDate;
        var newEnd = baseEvent.EndDate;

        if (direction == "top")
        {
            var maxStart = baseEvent.EndDate.AddMinutes(-slotMinutes);
            var candidateStart = baseEvent.StartDate.AddMinutes(effectiveDelta);
            newStart = effectiveDelta > 0
                ? BitFullCalendarHelpers.CeilToMinuteInterval(candidateStart, slotMinutes)
                : BitFullCalendarHelpers.FloorToMinuteInterval(candidateStart, slotMinutes);
            if (newStart > maxStart)
                newStart = maxStart;
        }
        else if (direction == "bottom")
        {
            var minEnd = baseEvent.StartDate.AddMinutes(slotMinutes);
            var candidateEnd = baseEvent.EndDate.AddMinutes(effectiveDelta);
            newEnd = effectiveDelta > 0
                ? BitFullCalendarHelpers.CeilToMinuteInterval(candidateEnd, slotMinutes)
                : BitFullCalendarHelpers.FloorToMinuteInterval(candidateEnd, slotMinutes);
            if (newEnd < minEnd)
                newEnd = minEnd;
        }
        else
        {
            // Unknown direction from JS interop: ignore rather than silently mutating the end-time.
            return Task.CompletedTask;
        }

        // Snapped range matches drag-start range → treat as restored original (clear preview).
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

                    await Notifier.NotifyAsync(new BitFullCalendarChangeEventArgs
                    {
                        Event = BitFullCalendarChangeNotifier.CloneEvent(updated),
                        OldEvent = BitFullCalendarChangeNotifier.CloneEvent(b),
                        Kind = BitFullCalendarChangeKind.Edit,
                        Source = BitFullCalendarChangeSource.Resize
                    });
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

            // Render from the finally path so the preview state is always cleared on screen, even if
            // State.UpdateEvent or Notifier.NotifyAsync above threw.
            await InvokeAsync(StateHasChanged);
        }
    }

    private void OnResizeTopStart() { }
    private void OnResizeBottomStart() { }

    public void Dispose() => _dotNetRef?.Dispose();
}
