namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.Timeline;

public partial class _BitTimelineItemDemo
{
    private string? clickedItem;
    private List<BitTimelineItem> clickItems = [];

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

    private List<BitTimelineItem> twoSidedItems =
    [
        new() { PrimaryText = "09:00", SecondaryText = "Item 1", IconName = BitIconName.Add },
        new() { PrimaryText = "10:30", SecondaryText = "Item 2", IconName = BitIconName.Edit },
        new() { PrimaryText = "13:15", SecondaryText = "Item 3", IconName = BitIconName.Delete },
        new() { PrimaryText = "16:45", SecondaryText = "Item 4", IconName = BitIconName.Accept }
    ];

    private List<BitTimelineItem> lineVariantItems =
    [
        new() { PrimaryText = "Ordered", IconName = BitIconName.Accept, Color = BitColor.Success },
        new() { PrimaryText = "Shipped", IconName = BitIconName.Accept, Color = BitColor.Success, LineVariant = BitTimelineLineVariant.Dashed },
        new() { PrimaryText = "Delivered", Variant = BitVariant.Outline, LineVariant = BitTimelineLineVariant.Dashed }
    ];

    private List<BitTimelineItem> customizedItems =
    [
        new() { PrimaryText = "Success", IconName = BitIconName.Accept, Color = BitColor.Success },
        new() { PrimaryText = "Warning", IconName = BitIconName.Warning, Color = BitColor.Warning, Variant = BitVariant.Outline },
        new() { PrimaryText = "Error", IconName = BitIconName.ErrorBadge, Color = BitColor.Error, Size = BitSize.Large },
        new() { PrimaryText = "No dot", HideDot = true }
    ];

    private List<BitTimelineItem> alignItems =
    [
        new() { PrimaryText = "09:00", SecondaryText = "Kickoff: the scope, the milestones and an owner for each of them are agreed on.", IconName = BitIconName.Add },
        new() { PrimaryText = "11:30", SecondaryText = "Design review: the proposal is walked through and the open questions are collected.", IconName = BitIconName.Edit },
        new() { PrimaryText = "15:00", SecondaryText = "Sign-off: the plan is approved and the work is scheduled.", IconName = BitIconName.Accept }
    ];

    private List<BitTimelineItem> a11yItems =
    [
        new() { PrimaryText = "Ordered", IconName = BitIconName.Accept, Color = BitColor.Success, AriaLabel = "Ordered, done", Title = "Done on 3 March" },
        new() { PrimaryText = "Shipped", IconName = BitIconName.Accept, Color = BitColor.Success, LineVariant = BitTimelineLineVariant.Dashed, AriaLabel = "Shipped, done", Title = "Done on 4 March" },
        new() { PrimaryText = "Delivered", Variant = BitVariant.Outline, LineVariant = BitTimelineLineVariant.Dashed, AriaLabel = "Delivered, pending", Title = "Expected on 7 March" }
    ];

    private List<BitTimelineItem> cssVarItems =
    [
        new() { PrimaryText = "Ordered", IconName = BitIconName.Accept },
        new() { PrimaryText = "Shipped", IconName = BitIconName.Accept, Style = "--bit-Timeline-dot-background: gold; --bit-Timeline-dot-border-color: goldenrod; --bit-Timeline-icon-color: black;" },
        new() { PrimaryText = "Delivered", Variant = BitVariant.Outline }
    ];

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

    private List<BitTimelineItem> externalIconItems1 =
    [
        new() { PrimaryText = "Item 1", Icon = "fa-solid fa-plus" },
        new() { PrimaryText = "Item 2", Icon = "fa-solid fa-pen", SecondaryText = "Item 2 Secondary" },
        new() { PrimaryText = "Item 3", Icon = "fa-solid fa-trash" }
    ];

    private List<BitTimelineItem> externalIconItems2 =
    [
        new() { PrimaryText = "Item 1", Icon = BitIconInfo.Css("fa-solid fa-plus") },
        new() { PrimaryText = "Item 2", Icon = BitIconInfo.Css("fa-solid fa-pen"), SecondaryText = "Item 2 Secondary" },
        new() { PrimaryText = "Item 3", Icon = BitIconInfo.Css("fa-solid fa-trash") }
    ];

    private List<BitTimelineItem> externalIconItems3 =
    [
        new() { PrimaryText = "Item 1", Icon = BitIconInfo.Fa("solid plus") },
        new() { PrimaryText = "Item 2", Icon = BitIconInfo.Fa("solid pen"), SecondaryText = "Item 2 Secondary" },
        new() { PrimaryText = "Item 3", Icon = BitIconInfo.Fa("solid trash") }
    ];

    private List<BitTimelineItem> bootstrapIconItems1 =
    [
        new() { PrimaryText = "Item 1", Icon = "bi bi-plus-lg" },
        new() { PrimaryText = "Item 2", Icon = "bi bi-pencil", SecondaryText = "Item 2 Secondary" },
        new() { PrimaryText = "Item 3", Icon = "bi bi-trash" }
    ];

    private List<BitTimelineItem> bootstrapIconItems2 =
    [
        new() { PrimaryText = "Item 1", Icon = BitIconInfo.Css("bi bi-plus-lg") },
        new() { PrimaryText = "Item 2", Icon = BitIconInfo.Css("bi bi-pencil"), SecondaryText = "Item 2 Secondary" },
        new() { PrimaryText = "Item 3", Icon = BitIconInfo.Css("bi bi-trash") }
    ];

    private List<BitTimelineItem> bootstrapIconItems3 =
    [
        new() { PrimaryText = "Item 1", Icon = BitIconInfo.Bi("plus-lg") },
        new() { PrimaryText = "Item 2", Icon = BitIconInfo.Bi("pencil"), SecondaryText = "Item 2 Secondary" },
        new() { PrimaryText = "Item 3", Icon = BitIconInfo.Bi("trash") }
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

    protected override void OnInitialized()
    {
        // The item's own OnClick handler needs the component instance, so these items are built here
        // instead of in a field initializer.
        clickItems =
        [
            new() { PrimaryText = "Item 1", IconName = BitIconName.Add },
            new() { PrimaryText = "Item 2", IconName = BitIconName.Edit, OnClick = HandleOnItemClick },
            new() { PrimaryText = "Item 3", IconName = BitIconName.Delete, IsEnabled = false }
        ];

        base.OnInitialized();
    }

    private void HandleOnItemClick(BitTimelineItem item)
    {
        clickedItem = $"{item.PrimaryText} (item's own OnClick)";
        StateHasChanged();
    }
}
