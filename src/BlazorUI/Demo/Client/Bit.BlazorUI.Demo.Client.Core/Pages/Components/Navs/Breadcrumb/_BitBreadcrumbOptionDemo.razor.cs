namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public partial class _BitBreadcrumbOptionDemo
{
    private int ItemsCount = 4;

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
}
