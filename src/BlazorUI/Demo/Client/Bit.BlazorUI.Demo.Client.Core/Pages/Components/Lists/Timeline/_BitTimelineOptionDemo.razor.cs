namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.Timeline;

public partial class _BitTimelineOptionDemo
{
    private string? clickedOption;

    private readonly BitTimelineParams[] timelineParams =
    [
        new()
        {
            Horizontal = true,
            Color = BitColor.Success,
            Variant = BitVariant.Outline,
            TruncateLine = BitTimelineTruncateLine.Both,
        }
    ];
}
