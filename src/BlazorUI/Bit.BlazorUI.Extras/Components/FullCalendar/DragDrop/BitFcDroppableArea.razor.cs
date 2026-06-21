namespace Bit.BlazorUI;

public partial class BitFcDroppableArea
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarChangeNotifier Notifier { get; set; } = default!;
    [Parameter] public DateTime Date { get; set; }
    [Parameter] public int? Hour { get; set; }
    [Parameter] public int? Minute { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string? Class { get; set; }

    private bool _isOver;

    private void OnDragOver()
    {
        if (_isOver) return;
        _isOver = true;
    }
    private void OnDragLeave() => _isOver = false;

    private async Task OnDrop()
    {
        _isOver = false;
        await Notifier.HandleDropAsync(Date, Hour, Minute);
    }
}
