namespace Bit.BlazorUI;

public partial class BitFcDraggableEvent
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [Parameter] public BitFullCalendarEvent Event { get; set; } = default!;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string? Class { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }

    private bool _isDragged => State.IsDragging && State.DraggedEvent?.Id == Event.Id;

    private void OnDragStart() => State.StartDrag(Event);
    private void OnDragEnd() => State.EndDrag();
}
