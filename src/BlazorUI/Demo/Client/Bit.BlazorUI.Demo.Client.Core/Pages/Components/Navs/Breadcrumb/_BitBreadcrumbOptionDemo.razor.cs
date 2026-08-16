namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public partial class _BitBreadcrumbOptionDemo
{
    private int ItemsCount = 4;
    private uint OverflowIndex = 2;
    private uint MaxDisplayedItems = 3;
    private int SelectedOptionNumber = 6;
    private int CustomizedSelectedOptionNumber = 4;

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
