namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public partial class _BitBreadcrumbItemDemo
{
    private readonly List<BitBreadcrumbItem> BreadcrumbItems =
    [
        new() { Text = "Item 1", Href = "/components/breadcrumb" },
        new() { Text = "Item 2", Href = "/components/breadcrumb" },
        new() { Text = "Item 3", Href = "/components/breadcrumb" },
        new() { Text = "Item 4", Href = "/components/breadcrumb", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsDisabled =
    [
        new() { Text = "Item 1", Href = "/components/breadcrumb", IsDisabled = true },
        new() { Text = "Item 2", Href = "/components/breadcrumb", IsDisabled = true },
        new() { Text = "Item 3", Href = "/components/breadcrumb" },
        new() { Text = "Item 4", Href = "/components/breadcrumb", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithTarget =
    [
        new() { Text = "Item 1", Href = "/components/breadcrumb" },
        new() { Text = "Item 2", Href = "/components/breadcrumb" },
        new() { Text = "bit BlazorUI", Href = "https://blazorui.bitplatform.dev", Target = "_blank", Title = "Opens the bit BlazorUI website in a new tab" },
        new() { Text = "Item 4", Href = "/components/breadcrumb", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithLongText =
    [
        new() { Text = "Very long folder name 1", Href = "/components/breadcrumb", Title = "Very long folder name 1" },
        new() { Text = "Very long folder name 2", Href = "/components/breadcrumb", Title = "Very long folder name 2" },
        new() { Text = "Very long folder name 3", Href = "/components/breadcrumb", Title = "Very long folder name 3" },
        new() { Text = "Very long folder name 4", Href = "/components/breadcrumb", Title = "Very long folder name 4", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithIcon =
    [
        new() { Text = "Item 1", Href = "/components/breadcrumb", IconName = BitIconName.AdminELogoInverse32 },
        new() { Text = "Item 2", Href = "/components/breadcrumb", IconName = BitIconName.AppsContent },
        new() { Text = "Item 3", Href = "/components/breadcrumb", IconName = BitIconName.AzureIcon },
        new() { Text = "Item 4", Href = "/components/breadcrumb", IsSelected = true, IconName = BitIconName.ClassNotebookLogo16 }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithHomeIcon =
    [
        new() { IconName = BitIconName.Home, AriaLabel = "Home", Href = "/components/breadcrumb" },
        new() { Text = "Item 2", Href = "/components/breadcrumb" },
        new() { Text = "Item 3", Href = "/components/breadcrumb" },
        new() { Text = "Item 4", Href = "/components/breadcrumb", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithClass =
    [
        new() { Text = "Item 1", Href = "/components/breadcrumb", Class = "custom-item-1" },
        new() { Text = "Item 2", Href = "/components/breadcrumb", Class = "custom-item-2" },
        new() { Text = "Item 3", Href = "/components/breadcrumb", Class = "custom-item-1" },
        new() { Text = "Item 4", Href = "/components/breadcrumb", Class = "custom-item-2", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithStyle =
    [
        new() { Text = "Item 1", Href = "/components/breadcrumb", Style = "color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;" },
        new() { Text = "Item 2", Href = "/components/breadcrumb", Style = "color: aqua; text-shadow: aqua 0 0 1rem;" },
        new() { Text = "Item 3", Href = "/components/breadcrumb", Style = "color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;" },
        new() { Text = "Item 4", Href = "/components/breadcrumb", Style = "color: aqua; text-shadow: aqua 0 0 1rem;", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithControlled =
    [
        new() { Text = "Item 1" },
        new() { Text = "Item 2" },
        new() { Text = "Item 3" },
        new() { Text = "Item 4" },
        new() { Text = "Item 5" },
        new() { Text = "Item 6", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithCustomized =
    [
        new() { Text = "Item 1" },
        new() { Text = "Item 2" },
        new() { Text = "Item 3" },
        new() { Text = "Item 4", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithExternalIcon1 =
    [
        new() { Text = "Home", Icon = "fa-solid fa-house" },
        new() { Text = "Products", Icon = "fa-solid fa-box" },
        new() { Text = "Electronics", Icon = "fa-solid fa-microchip" },
        new() { Text = "Laptops", Icon = "fa-solid fa-laptop", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithExternalIcon2 =
    [
        new() { Text = "Home", Icon = BitIconInfo.Css("fa-solid fa-house") },
        new() { Text = "Products", Icon = BitIconInfo.Css("fa-solid fa-box") },
        new() { Text = "Electronics", Icon = BitIconInfo.Css("fa-solid fa-microchip") },
        new() { Text = "Laptops", Icon = BitIconInfo.Css("fa-solid fa-laptop"), IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithExternalIcon3 =
    [
        new() { Text = "Home", Icon = BitIconInfo.Fa("solid house") },
        new() { Text = "Products", Icon = BitIconInfo.Fa("solid box") },
        new() { Text = "Electronics", Icon = BitIconInfo.Fa("solid microchip") },
        new() { Text = "Laptops", Icon = BitIconInfo.Fa("solid laptop"), IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithExternalIcon4 =
    [
        new() { Text = "Home", Icon = BitIconInfo.Bi("house-fill") },
        new() { Text = "Products", Icon = BitIconInfo.Bi("box-seam-fill") },
        new() { Text = "Electronics", Icon = BitIconInfo.Bi("cpu-fill") },
        new() { Text = "Laptops", Icon = BitIconInfo.Bi("laptop-fill"), IsSelected = true }
    ];

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

    private readonly List<BitBreadcrumbItem> RtlBreadcrumbItems =
    [
        new() { Text = "پوشه اول" },
        new() { Text = "پوشه دوم" },
        new() { Text = "پوشه سوم" },
        new() { Text = "پوشه چهارم" },
        new() { Text = "پوشه پنجم" },
        new() { Text = "پوشه ششم", IsSelected = true },
    ];


    // Going back up to an item drops the levels below it, which makes it the current page.
    private void HandleOnCustomizedItemClick(BitBreadcrumbItem item)
    {
        var index = BreadcrumbItemsWithCustomized.IndexOf(item);

        BreadcrumbItemsWithCustomized.RemoveRange(index + 1, BreadcrumbItemsWithCustomized.Count - index - 1);

        item.IsSelected = true;
    }

    private void AddBreadcrumbItem()
    {
        BreadcrumbItemsWithCustomized[^1].IsSelected = false;

        BreadcrumbItemsWithCustomized.Add(new() { Text = $"Item {BreadcrumbItemsWithCustomized.Count + 1}", IsSelected = true });
    }
}
