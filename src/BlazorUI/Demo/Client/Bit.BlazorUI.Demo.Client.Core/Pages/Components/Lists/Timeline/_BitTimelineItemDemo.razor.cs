namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.Timeline;

public partial class _BitTimelineItemDemo
{
    private List<BitTimelineItem> basicItems =
    [
        new() { PrimaryText = "Item 1" },
        new() { PrimaryText = "Item 2", SecondaryText = "Item 2 Secondary" },
        new() { PrimaryText = "Item 3" }
    ];

    private List<BitTimelineItem> disabledItems =
    [
        new() { PrimaryText = "Item 1" },
        new() { PrimaryText = "Item 2", SecondaryText = "Item 2 Secondary", IsEnabled = false },
        new() { PrimaryText = "Item 3" }
    ];

    private List<BitTimelineItem> iconItems =
    [
        new() { PrimaryText = "Item 1", IconName = BitIconName.Add },
        new() { PrimaryText = "Item 2", IconName = BitIconName.Edit, SecondaryText = "Item 2 Secondary", IsEnabled = false },
        new() { PrimaryText = "Item 3", IconName = BitIconName.Delete }
    ];

    private List<BitTimelineItem> reversedItems =
    [
        new() { PrimaryText = "Item 1" },
        new() { PrimaryText = "Item 2", Reversed = true },
        new() { PrimaryText = "Item 3" }
    ];

    private List<BitTimelineItem> styleClassItems =
    [
        new() { PrimaryText = "Styled", Style = "color: dodgerblue;", IconName = BitIconName.Brush },
        new() { PrimaryText = "Classed", Class = "custom-item", IconName = BitIconName.FormatPainter }
    ];

    private List<BitTimelineItem> basicRtlItems =
    [
        new() { PrimaryText = "گزینه ۱" },
        new() { PrimaryText = "گزینه ۲", SecondaryText = "گزینه ۲ ثانویه" },
        new() { PrimaryText = "گزینه ۳" }
    ];
}
