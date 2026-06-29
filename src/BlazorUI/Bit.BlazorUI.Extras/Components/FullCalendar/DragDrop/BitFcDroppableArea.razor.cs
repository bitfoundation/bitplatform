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
    // Set when a dragover fires while already hovering: the hover state is unchanged, so the render
    // it would otherwise trigger is suppressed to avoid re-rendering on every dragover callback.
    private bool _skipRender;

    private void OnDragOver()
    {
        if (_isOver)
        {
            _skipRender = true;
            return;
        }
        _isOver = true;
    }
    private void OnDragLeave()
    {
        // A real hover-state transition must be allowed to render (to drop the drag-over styling),
        // so clear any pending skip left by a previous repeated dragover before flipping _isOver.
        _skipRender = false;
        _isOver = false;
    }

    private async Task OnDrop()
    {
        // Same as OnDragLeave: the post-drop UI update must render, so never let a stale skip flag
        // from a prior dragover suppress it.
        _skipRender = false;
        _isOver = false;
        await Notifier.HandleDropAsync(Date, Hour, Minute);
    }

    protected override bool ShouldRender()
    {
        if (_skipRender)
        {
            _skipRender = false;
            return false;
        }
        return true;
    }
}
