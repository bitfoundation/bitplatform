namespace Bit.BlazorUI;

public partial class BitFcDraggableEvent
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [Parameter] public BitFullCalendarEvent Event { get; set; } = default!;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string? Class { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }

    private bool _isDragged => State.IsDragging && State.DraggedEvent?.Id == Event.Id;

    // Drives @onkeydown:preventDefault so Space/Enter activation doesn't also scroll the page,
    // while leaving Tab/arrow keys with their native behaviour (no keyboard trap).
    private bool _preventKeyDefault;

    private void OnDragStart() => State.StartDrag(Event);
    private void OnDragEnd() => State.EndDrag();

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        _preventKeyDefault = e.Key is "Enter" or " " or "Spacebar";
        if (_preventKeyDefault)
            await OnClick.InvokeAsync();
    }
}
