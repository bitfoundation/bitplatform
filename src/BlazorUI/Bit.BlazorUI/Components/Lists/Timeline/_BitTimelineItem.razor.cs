namespace Bit.BlazorUI;

public partial class _BitTimelineItem<TItem> where TItem : class
{
    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitTimeline<TItem> Timeline { get; set; } = default!;

    // Enter and Space activate the item, as the WAI-ARIA button pattern describes.
    // Blazor evaluates @onkeydown:preventDefault at render time, so it cannot tell Space from Tab and would
    // swallow the Tab as well. The page scroll of Space is the default action of its keypress instead, which
    // the button suppresses unconditionally (@onkeypress:preventDefault): keypress fires for the character
    // keys only, so Tab and the arrow keys keep their default behavior.
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
