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

    /// <summary>
    /// Minimum event length enforced by resize, and the interval the edges snap to: the calendar's
    /// configured slot duration, so a 15-minute grid resizes in 15-minute steps.
    /// </summary>
    private int MinEventDurationMinutes => Math.Max(1, State.SlotDurationMinutes);

    /// <summary>
    /// Pointer movement below this (in minutes along the time axis) does not change start/end,
    /// so the edge does not jump as soon as the user presses the handle.
    /// </summary>
    private int ResizeDeadZoneMinutes => Math.Max(1, MinEventDurationMinutes / 2);

    /// <summary>
    /// True while this block may be moved or resized: the calendar has to be editable AND the event
    /// itself must not be locked with <see cref="BitFullCalendarEvent.IsReadOnly"/>.
    /// </summary>
    private bool CanEdit => State.ReadOnly is false && Event.IsReadOnly is false;

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
        {
            await OnSelected.InvokeAsync(Event);
            return;
        }

        if (e.Key is not ("ArrowUp" or "ArrowDown") || CanEdit is false)
            return;

        // Keyboard parity for the pointer gestures: Alt+Arrow moves the block by one slot,
        // Shift+Arrow stretches or shrinks its end. Both commit through the same rules a drag or a
        // resize obeys, so a refusal is reported the same way.
        var step = TimeSpan.FromMinutes(e.Key == "ArrowDown" ? State.SlotDurationMinutes : -State.SlotDurationMinutes);

        if (e.AltKey)
            await ApplyKeyboardEditAsync(step, step, BitFullCalendarChangeSource.Drag);
        else if (e.ShiftKey)
            await ApplyKeyboardEditAsync(TimeSpan.Zero, step, BitFullCalendarChangeSource.Resize);
    }

    /// <summary>
    /// Commits a keyboard-driven move or resize: the same range, overlap, and read-only rules the
    /// pointer gestures obey, and the same <c>OnChange</c> payload.
    /// </summary>
    private async Task ApplyKeyboardEditAsync(TimeSpan startDelta, TimeSpan endDelta, BitFullCalendarChangeSource source)
    {
        var start = Event.StartDate + startDelta;
        var end = Event.EndDate + endDelta;

        // A resize can never shrink the event below one slot.
        if (end - start < TimeSpan.FromMinutes(MinEventDurationMinutes))
            return;

        if (State.IsDateInAllowedRange(start) is false || State.IsDateInAllowedRange(end.AddTicks(-1)) is false)
        {
            Notifier.ReportRefusal(BitFullCalendarChangeRefusal.OutOfRange);
            return;
        }

        if (State.IsRangeAvailable(Event.Id, start, end, Event.Resource) is false)
        {
            Notifier.ReportRefusal(BitFullCalendarChangeRefusal.Overlap);
            return;
        }

        var oldSnapshot = BitFullCalendarChangeNotifier.CloneEvent(Event);
        var updated = new BitFullCalendarEvent
        {
            Id = Event.Id,
            Title = Event.Title,
            Description = Event.Description,
            StartDate = start,
            EndDate = end,
            Color = Event.Color,
            Resource = Event.Resource,
            Data = Event.Data,
            Attendees = [.. Event.Attendees],
            IsAllDay = Event.IsAllDay,
            Recurrence = Event.Recurrence,
            IsReadOnly = Event.IsReadOnly,
            CssClass = Event.CssClass
        };

        State.UpdateEvent(updated);

        try
        {
            await Notifier.NotifyAsync(new BitFullCalendarChangeEventArgs
            {
                Event = BitFullCalendarChangeNotifier.CloneEvent(updated),
                OldEvent = oldSnapshot,
                Kind = BitFullCalendarChangeKind.Edit,
                Source = source
            });
        }
        catch
        {
            // Notification failed: restore the previous times so the local state stays in sync with
            // what consumers believe, mirroring the pointer resize's compensation.
            State.UpdateEvent(oldSnapshot);
            throw;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // A read-only block renders no resize handles, so there is nothing to bind the JS listeners
        // to. The handles are removed from the DOM (taking their listeners with them), so the flag
        // is cleared as well - otherwise the fresh handles rendered when read-only is turned back
        // off would be skipped here and never receive listeners.
        if (CanEdit is false)
        {
            _resizeInitialized = false;
            return;
        }

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

        // The handles are not rendered while read-only, but a listener bound before the switch can
        // still deliver a start; refuse it so the block never enters resize mode in read-only.
        if (CanEdit is false)
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

        // Read-only can be switched on mid-gesture: the handle leaves the DOM, but the document-level
        // pointer listeners keep running. Cancel the whole gesture (not just the preview) so the block
        // snaps back to the stored times and stays there - keeping the resize alive would let it pick
        // up again, and commit on release, if read-only were switched back off before the pointer up.
        if (CanEdit is false)
        {
            _previewStart = null;
            _previewEnd = null;
            _isResizing = false;
            _resizeBaseEvent = null;
            _resizeDirection = null;
            // The pointer is still down: swallow the click its release produces so cancelling a resize
            // doesn't select the event.
            _suppressClickUntilUtc = DateTime.UtcNow.AddMilliseconds(300);
            return InvokeAsync(StateHasChanged);
        }

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
        var slotMinutes = MinEventDurationMinutes;
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
            // Never commit in read-only: the switch can land between the last move and the release,
            // which would otherwise persist a resize the calendar no longer allows.
            if (CanEdit && _resizeBaseEvent != null && _previewStart.HasValue && _previewEnd.HasValue)
            {
                var s = _previewStart.Value;
                var e = _previewEnd.Value;
                if (s != _resizeBaseEvent.StartDate || e != _resizeBaseEvent.EndDate)
                {
                    var b = _resizeBaseEvent;

                    // The resized span has to obey the same rules a drop does, so a calendar that
                    // disallows double booking refuses the resize instead of quietly creating one.
                    if (State.IsRangeAvailable(b.Id, s, e, b.Resource) is false)
                    {
                        Notifier.ReportRefusal(BitFullCalendarChangeRefusal.Overlap);
                        return;
                    }

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
                        Attendees = [.. b.Attendees],
                        IsAllDay = b.IsAllDay,
                        Recurrence = b.Recurrence,
                        IsReadOnly = b.IsReadOnly,
                        CssClass = b.CssClass
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
