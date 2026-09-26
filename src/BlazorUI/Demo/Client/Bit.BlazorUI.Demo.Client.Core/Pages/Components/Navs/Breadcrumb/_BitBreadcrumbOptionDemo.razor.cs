namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public partial class _BitBreadcrumbOptionDemo
{
    private int ItemsCount = 4;
    private uint OverflowIndex = 2;
    private uint MaxDisplayedItems = 3;
    private int SelectedOptionNumber = 4;

    private readonly BitBreadcrumbParams[] breadcrumbParams =
    [
        new()
        {
            DividerText = "/",
            MaxDisplayedItems = 3,
            OverflowIndex = 1,
            SelectedItemAsText = true,
        }
    ];

    private readonly BitColor[] colors =
    [
        BitColor.Primary,
        BitColor.Secondary,
        BitColor.Tertiary,
        BitColor.Info,
        BitColor.Success,
        BitColor.Warning,
        BitColor.SevereWarning,
        BitColor.Error
    ];

    private readonly BitSize[] sizes = [BitSize.Small, BitSize.Medium, BitSize.Large];

    // The trail always keeps at least one option, and the selection moves onto the new last one when
    // the option that carried it is the one that leaves.
    private void RemoveOption()
    {
        if (ItemsCount <= 1) return;

        ItemsCount--;

        if (SelectedOptionNumber > ItemsCount)
        {
            SelectedOptionNumber = ItemsCount;
        }
    }
}
