namespace Bit.BlazorUI;

public partial class _BitTimelineItem<TItem> where TItem : class
{
    private bool _preventKeyDownDefault;

    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitTimeline<TItem> Timeline { get; set; } = default!;

    private async Task HandleOnItemKeyDown(KeyboardEventArgs e)
    {
        var isSpace = e.Key == " " || e.Key == "Spacebar";

        // Space scrolls the page by default; suppress it on keydown while it is the space key (Enter has
        // no default to suppress). Kept key-scoped so Tab and other keys still behave normally.
        _preventKeyDownDefault = isSpace;

        if (isSpace || e.Key == "Enter")
        {
            await Timeline.HandleOnItemClick(Item);
        }
    }
}
