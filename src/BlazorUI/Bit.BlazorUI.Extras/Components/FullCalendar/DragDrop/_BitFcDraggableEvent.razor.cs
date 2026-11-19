using System.ComponentModel;

namespace Bit.BlazorUI;

/// <summary>
/// A part of <see cref="BitFullCalendar"/>, rendered by the calendar alone: it is not meant to be used on its own.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class _BitFcDraggableEvent
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string? Class { get; set; }
    [Parameter] public BitFullCalendarEvent Event { get; set; } = default!;
    [Parameter] public EventCallback OnClick { get; set; }

    private bool _isDragged => State.IsDragging && State.DraggedEvent?.Id == Event.Id;

    private void OnDragStart() => State.StartDrag(Event);
    private void OnDragEnd() => State.EndDrag();

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        // role="button" must be keyboard-activatable: mirror native button behavior by invoking the
        // click callback on Enter/Space. Ignore auto-repeat keydown events (matching
        // _BitFcDayView.OnHourKeyDownAsync) so holding the key can't fire OnClick repeatedly.
        if (e.Key is "Enter" or " " or "Spacebar" && !e.Repeat)
            await OnClick.InvokeAsync();
    }
}
