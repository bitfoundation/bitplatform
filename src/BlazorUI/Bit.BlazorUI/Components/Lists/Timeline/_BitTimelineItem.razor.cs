namespace Bit.BlazorUI;

public partial class _BitTimelineItem<TItem> where TItem : class
{
    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitTimeline<TItem> Timeline { get; set; } = default!;

    // Enter and Space activate the item, as the WAI-ARIA button pattern describes.
    // The default action of Space (scrolling the page) is deliberately left alone: Blazor evaluates
    // @onkeydown:preventDefault at render time, so a flag set from this handler only takes effect from
    // the next key press on - one press too late to stop the scroll, and one press too early to let the
    // following Tab through, which it would swallow instead of moving the focus.
    private async Task HandleOnItemKeyDown(KeyboardEventArgs e)
    {
        // Only an item that acts as a button answers to the keyboard, so a presentational item never
        // swallows a key press of the page.
        if (Timeline.IsItemInteractive(Item) is false) return;

        if (e.Key is "Enter" or " " or "Spacebar")
        {
            await Timeline.HandleOnItemClick(Item);
        }
    }
}
