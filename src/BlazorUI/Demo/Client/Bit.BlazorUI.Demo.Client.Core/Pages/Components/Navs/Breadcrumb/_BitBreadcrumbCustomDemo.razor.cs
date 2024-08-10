namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public partial class _BitBreadcrumbCustomDemo
{
    private int ItemsCount = 4;
    private uint OverflowIndex = 2;
    private uint MaxDisplayedItems = 3;

    private readonly List<PageInfoModel> CustomBreadcrumbItems =
    [
        new() { Name = "Folder 1", Address = "/components/breadcrumb" },
        new() { Name = "Folder 2", Address = "/components/breadcrumb" },
        new() { Name = "Folder 3", Address = "/components/breadcrumb" },
        new() { Name = "Folder 4", Address = "/components/breadcrumb", IsCurrent = true }
    ];

    private readonly List<PageInfoModel> CustomBreadcrumbItemsDisabled =
    [
        new() { Name = "Folder 1", Address = "/components/breadcrumb", IsEnabled = false },
        new() { Name = "Folder 2", Address = "/components/breadcrumb", IsEnabled = false },
        new() { Name = "Folder 3", Address = "/components/breadcrumb" },
        new() { Name = "Folder 4", Address = "/components/breadcrumb", IsCurrent = true }
    ];

    private readonly List<PageInfoModel> CustomBreadcrumbItemsWithClass =
    [
        new() { Name = "Folder 1", Address = "/components/breadcrumb", HtmlClass = "custom-item" },
        new() { Name = "Folder 2", Address = "/components/breadcrumb", HtmlClass = "custom-item" },
        new() { Name = "Folder 3", Address = "/components/breadcrumb", HtmlClass = "custom-item" },
        new() { Name = "Folder 4", Address = "/components/breadcrumb", HtmlClass = "custom-item", IsCurrent = true }
    ];

    private readonly List<PageInfoModel> CustomBreadcrumbItemsWithStyle =
    [
        new() { Name = "Folder 1", Address = "/components/breadcrumb", HtmlStyle = "color:red;background:greenyellow" },
        new() { Name = "Folder 2", Address = "/components/breadcrumb", HtmlStyle = "color:red;background:greenyellow" },
        new() { Name = "Folder 3", Address = "/components/breadcrumb", HtmlStyle = "color:red;background:greenyellow" },
        new() { Name = "Folder 4", Address = "/components/breadcrumb", HtmlStyle = "color:red;background:greenyellow", IsCurrent = true }
    ];

    private readonly List<PageInfoModel> CustomBreadcrumbItemsWithControlled =
    [
        new() { Name = "Folder 1" },
        new() { Name = "Folder 2" },
        new() { Name = "Folder 3" },
        new() { Name = "Folder 4" },
        new() { Name = "Folder 5" },
        new() { Name = "Folder 6", IsCurrent = true }
    ];

    private readonly List<PageInfoModel> CustomBreadcrumbItemsWithCustomized =
    [
        new() { Name = "Folder 1" },
        new() { Name = "Folder 2" },
        new() { Name = "Folder 3" },
        new() { Name = "Folder 4", IsCurrent = true }
    ];

    private readonly List<PageInfoModel> RtlCustomBreadcrumbItems =
    [
        new() { Name = "پوشه اول" },
        new() { Name = "پوشه دوم", IsCurrent = true },
        new() { Name = "پوشه سوم" },
        new() { Name = "پوشه چهارم" },
        new() { Name = "پوشه پنجم" },
        new() { Name = "پوشه ششم" },
    ];

    private BitBreadcrumbNameSelectors<PageInfoModel> nameSelectors = new()
    {
        Text = { Selector = c => c.Name },
        Href = { Selector = c => c.Address },
        IsSelected = { Selector = c => c.IsCurrent },
        Class = { Selector = c => c.HtmlClass },
        Style = { Selector = c => c.HtmlStyle }
    };

    private void HandleOnCustomClick(PageInfoModel model)
    {
        CustomBreadcrumbItemsWithControlled.First(i => i.IsCurrent).IsCurrent = false;
        model.IsCurrent = true;
    }

    private void HandleOnCustomizedCustomClick(PageInfoModel model)
    {
        CustomBreadcrumbItemsWithCustomized.First(i => i.IsCurrent).IsCurrent = false;
        model.IsCurrent = true;
    }

    private void AddCustomItem()
    {
        ItemsCount++;
        CustomBreadcrumbItemsWithCustomized.Add(new PageInfoModel()
        {
            Name = $"Folder {ItemsCount}"
        });
    }

    private void RemoveCustomItem()
    {
        if (CustomBreadcrumbItemsWithCustomized.Count > 1)
        {
            ItemsCount--;

            var item = CustomBreadcrumbItemsWithCustomized[^1];
            CustomBreadcrumbItemsWithCustomized.Remove(item);

            if (item.IsCurrent)
            {
                CustomBreadcrumbItemsWithCustomized[^1].IsCurrent = true;
            }
        }
    }
}
