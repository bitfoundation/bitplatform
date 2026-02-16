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
        Icon = { Selector = i => i.ExternalIcon },
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

    private List<Event> externalIconCustoms1 =
    [
        new() { FirstText = "Custom 1", ExternalIcon = "fa-solid fa-plus" },
        new() { FirstText = "Custom 2", ExternalIcon = "fa-solid fa-pen", SecondText = "Custom 2 Secondary" },
        new() { FirstText = "Custom 3", ExternalIcon = "fa-solid fa-trash" }
    ];

    private List<Event> externalIconCustoms2 =
    [
        new() { FirstText = "Custom 1", ExternalIcon = BitIconInfo.Css("fa-solid fa-plus") },
        new() { FirstText = "Custom 2", ExternalIcon = BitIconInfo.Css("fa-solid fa-pen"), SecondText = "Custom 2 Secondary" },
        new() { FirstText = "Custom 3", ExternalIcon = BitIconInfo.Css("fa-solid fa-trash") }
    ];

    private List<Event> externalIconCustoms3 =
    [
        new() { FirstText = "Custom 1", ExternalIcon = BitIconInfo.Fa("solid plus") },
        new() { FirstText = "Custom 2", ExternalIcon = BitIconInfo.Fa("solid pen"), SecondText = "Custom 2 Secondary" },
        new() { FirstText = "Custom 3", ExternalIcon = BitIconInfo.Fa("solid trash") }
    ];

    private List<Event> bootstrapIconCustoms1 =
    [
        new() { FirstText = "Custom 1", ExternalIcon = "bi bi-plus-lg" },
        new() { FirstText = "Custom 2", ExternalIcon = "bi bi-pencil", SecondText = "Custom 2 Secondary" },
        new() { FirstText = "Custom 3", ExternalIcon = "bi bi-trash" }
    ];

    private List<Event> bootstrapIconCustoms2 =
    [
        new() { FirstText = "Custom 1", ExternalIcon = BitIconInfo.Css("bi bi-plus-lg") },
        new() { FirstText = "Custom 2", ExternalIcon = BitIconInfo.Css("bi bi-pencil"), SecondText = "Custom 2 Secondary" },
        new() { FirstText = "Custom 3", ExternalIcon = BitIconInfo.Css("bi bi-trash") }
    ];

    private List<Event> bootstrapIconCustoms3 =
    [
        new() { FirstText = "Custom 1", ExternalIcon = BitIconInfo.Bi("plus-lg") },
        new() { FirstText = "Custom 2", ExternalIcon = BitIconInfo.Bi("pencil"), SecondText = "Custom 2 Secondary" },
        new() { FirstText = "Custom 3", ExternalIcon = BitIconInfo.Bi("trash") }
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
