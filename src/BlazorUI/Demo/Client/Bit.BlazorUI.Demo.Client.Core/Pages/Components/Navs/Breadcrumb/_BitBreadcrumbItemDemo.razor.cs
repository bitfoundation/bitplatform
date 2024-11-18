namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public partial class _BitBreadcrumbItemDemo
{
    private int ItemsCount = 4;
    private uint OverflowIndex = 2;
    private uint MaxDisplayedItems = 3;


    private readonly List<BitBreadcrumbItem> BreadcrumbItems =
    [
        new() { Text = "Item 1", Href = "/components/breadcrumb" },
        new() { Text = "Item 2", Href = "/components/breadcrumb" },
        new() { Text = "Item 3", Href = "/components/breadcrumb" },
        new() { Text = "Item 4", Href = "/components/breadcrumb", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsDisabled =
    [
        new() { Text = "Item 1", Href = "/components/breadcrumb", IsEnabled = false },
        new() { Text = "Item 2", Href = "/components/breadcrumb", IsEnabled = false },
        new() { Text = "Item 3", Href = "/components/breadcrumb" },
        new() { Text = "Item 4", Href = "/components/breadcrumb", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWitIcon =
    [
        new() { Text = "Item 1", Href = "/components/breadcrumb", IconName = BitIconName.AdminELogoInverse32 },
        new() { Text = "Item 2", Href = "/components/breadcrumb", IconName = BitIconName.AppsContent },
        new() { Text = "Item 3", Href = "/components/breadcrumb", IconName = BitIconName.AzureIcon },
        new() { Text = "Item 4", Href = "/components/breadcrumb", IsSelected = true, IconName = BitIconName.ClassNotebookLogo16 }
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

    private readonly List<BitBreadcrumbItem> RtlBreadcrumbItems =
    [
        new() { Text = "پوشه اول" },
        new() { Text = "پوشه دوم", IsSelected = true },
        new() { Text = "پوشه سوم" },
        new() { Text = "پوشه چهارم" },
        new() { Text = "پوشه پنجم" },
        new() { Text = "پوشه ششم" },
    ];


    private void HandleOnItemClick(BitBreadcrumbItem item)
    {
        BreadcrumbItemsWithControlled.First(i => i.IsSelected).IsSelected = false;
        item.IsSelected = true;
    }

    private void HandleOnCustomizedItemClick(BitBreadcrumbItem item)
    {
        BreadcrumbItemsWithCustomized.First(i => i.IsSelected).IsSelected = false;
        item.IsSelected = true;
    }

    private void AddBreadcrumbItem()
    {
        ItemsCount++;
        BreadcrumbItemsWithCustomized.Add(new BitBreadcrumbItem()
        {
            Text = $"Item {ItemsCount}"
        });
    }

    private void RemoveBreadcrumbItem()
    {
        if (BreadcrumbItemsWithCustomized.Count > 1)
        {
            ItemsCount--;

            var item = BreadcrumbItemsWithCustomized[^1];
            BreadcrumbItemsWithCustomized.Remove(item);

            if (item.IsSelected)
            {
                BreadcrumbItemsWithCustomized[^1].IsSelected = true;
            }
        }
    }
}
