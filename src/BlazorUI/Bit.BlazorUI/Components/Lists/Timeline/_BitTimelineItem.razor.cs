namespace Bit.BlazorUI;

public partial class _BitTimelineItem<TItem> where TItem : class
{
    [Parameter] public TItem Item { get; set; } = default!;

    [Parameter] public BitTimeline<TItem> Timeline { get; set; } = default!;
}
