namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public partial class _BitBreadcrumbItemDemo
{
    private int ItemsCount = 4;
    private uint OverflowIndex = 2;
    private uint MaxDisplayedItems = 3;


    private readonly List<BitBreadcrumbItem> BreadcrumbItems =
    [
        new() { Text = "Folder 1", Href = "/components/breadcrumb" },
        new() { Text = "Folder 2", Href = "/components/breadcrumb" },
        new() { Text = "Folder 3", Href = "/components/breadcrumb" },
        new() { Text = "Folder 4", Href = "/components/breadcrumb", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsDisabled =
    [
        new() { Text = "Folder 1", Href = "/components/breadcrumb", IsEnabled = false },
        new() { Text = "Folder 2", Href = "/components/breadcrumb", IsEnabled = false },
        new() { Text = "Folder 3", Href = "/components/breadcrumb" },
        new() { Text = "Folder 4", Href = "/components/breadcrumb", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithClass =
    [
        new() { Text = "Folder 1", Href = "/components/breadcrumb", Class = "custom-item" },
        new() { Text = "Folder 2", Href = "/components/breadcrumb", Class = "custom-item" },
        new() { Text = "Folder 3", Href = "/components/breadcrumb", Class = "custom-item" },
        new() { Text = "Folder 4", Href = "/components/breadcrumb", Class = "custom-item", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithStyle =
    [
        new() { Text = "Folder 1", Href = "/components/breadcrumb", Style = "color:red;background:greenyellow" },
        new() { Text = "Folder 2", Href = "/components/breadcrumb", Style = "color:red;background:greenyellow" },
        new() { Text = "Folder 3", Href = "/components/breadcrumb", Style = "color:red;background:greenyellow" },
        new() { Text = "Folder 4", Href = "/components/breadcrumb", Style = "color:red;background:greenyellow", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithControlled =
    [
        new() { Text = "Folder 1" },
        new() { Text = "Folder 2" },
        new() { Text = "Folder 3" },
        new() { Text = "Folder 4" },
        new() { Text = "Folder 5" },
        new() { Text = "Folder 6", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithCustomized =
    [
        new() { Text = "Folder 1" },
        new() { Text = "Folder 2" },
        new() { Text = "Folder 3" },
        new() { Text = "Folder 4", IsSelected = true }
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
            Text = $"Folder {ItemsCount}"
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
