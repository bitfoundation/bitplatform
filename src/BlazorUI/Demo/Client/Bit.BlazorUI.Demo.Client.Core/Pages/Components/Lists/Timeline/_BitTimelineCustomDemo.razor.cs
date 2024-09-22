using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.Timeline;

public partial class _BitTimelineCustomDemo
{
    BitTimelineNameSelectors<Event> nameSelectors = new()
    {
        PrimaryText = { Selector = i => i.FirstText },
        SecondaryText = { Selector = i => i.SecondText },
        IsEnabled = { Selector = i => i.Disabled is false },
        IconName = { Selector = i => i.Icon },
        DotTemplate = { Selector = i => i.DotContent },
        PrimaryContent = { Selector = i => i.FirstContent },
        SecondaryContent = { Selector = i => i.SecondContent },
    };

    private List<Event> basicCustoms =
    [
        new() { FirstText = "Custom 1" },
        new() { FirstText = "Custom 2", SecondText = "Custom 2 Secondary" },
        new() { FirstText = "Custom 3" }
    ];

    private List<Event> disabledCustoms =
    [
        new() { FirstText = "Custom 1" },
        new() { FirstText = "Custom 2", SecondText = "Custom 2 Secondary", Disabled = true },
        new() { FirstText = "Custom 3" }
    ];

    private List<Event> iconCustoms =
    [
        new() { FirstText = "Custom 1", Icon = BitIconName.Add },
        new() { FirstText = "Custom 2", Icon = BitIconName.Edit, SecondText = "Custom 2 Secondary", Disabled = true },
        new() { FirstText = "Custom 3", Icon = BitIconName.Delete }
    ];

    private List<Event> reversedCustoms =
    [
        new() { FirstText = "Custom 1" },
        new() { FirstText = "Custom 2", Reversed = true },
        new() { FirstText = "Custom 3" }
    ];

    private List<Event> styleClassCustoms =
    [
        new() { FirstText = "Styled", Style = "color: dodgerblue;", Icon = BitIconName.Brush },
        new() { FirstText = "Classed", Class = "custom-item", Icon = BitIconName.FormatPainter }
    ];

    private List<Event> basicRtlCustoms =
    [
        new() { FirstText = "گزینه ۱" },
        new() { FirstText = "گزینه ۲", SecondText = "گزینه ۲ ثانویه" },
        new() { FirstText = "گزینه ۳" }
    ];
}
