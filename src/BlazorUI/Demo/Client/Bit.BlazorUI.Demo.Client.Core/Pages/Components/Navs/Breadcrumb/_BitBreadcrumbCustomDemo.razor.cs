namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public partial class _BitBreadcrumbCustomDemo
{
    private int ItemsCount = 4;
    private uint OverflowIndex = 2;
    private uint MaxDisplayedItems = 3;

    private readonly List<PageInfoModel> CustomBreadcrumbItems =
    [
        new() { Name = "Custom 1", Address = "/components/breadcrumb" },
        new() { Name = "Custom 2", Address = "/components/breadcrumb" },
        new() { Name = "Custom 3", Address = "/components/breadcrumb" },
        new() { Name = "Custom 4", Address = "/components/breadcrumb", IsCurrent = true }
    ];

    private readonly List<PageInfoModel> CustomBreadcrumbItemsDisabled =
    [
        new() { Name = "Custom 1", Address = "/components/breadcrumb", IsEnabled = false },
        new() { Name = "Custom 2", Address = "/components/breadcrumb", IsEnabled = false },
        new() { Name = "Custom 3", Address = "/components/breadcrumb" },
        new() { Name = "Custom 4", Address = "/components/breadcrumb", IsCurrent = true }
    ];

    private readonly List<PageInfoModel> CustomBreadcrumbItemsWithClass =
    [
        new() { Name = "Custom 1", Address = "/components/breadcrumb", HtmlClass = "custom-item" },
        new() { Name = "Custom 2", Address = "/components/breadcrumb", HtmlClass = "custom-item" },
        new() { Name = "Custom 3", Address = "/components/breadcrumb", HtmlClass = "custom-item" },
        new() { Name = "Custom 4", Address = "/components/breadcrumb", HtmlClass = "custom-item", IsCurrent = true }
    ];

    private readonly List<PageInfoModel> CustomBreadcrumbItemsWithStyle =
    [
        new() { Name = "Custom 1", Address = "/components/breadcrumb", HtmlStyle = "color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;" },
        new() { Name = "Custom 2", Address = "/components/breadcrumb", HtmlStyle = "color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;" },
        new() { Name = "Custom 3", Address = "/components/breadcrumb", HtmlStyle = "color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;" },
        new() { Name = "Custom 4", Address = "/components/breadcrumb", HtmlStyle = "color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;", IsCurrent = true }
    ];

    private readonly List<PageInfoModel> CustomBreadcrumbItemsWithControlled =
    [
        new() { Name = "Custom 1" },
        new() { Name = "Custom 2" },
        new() { Name = "Custom 3" },
        new() { Name = "Custom 4" },
        new() { Name = "Custom 5" },
        new() { Name = "Custom 6", IsCurrent = true }
    ];

    private readonly List<PageInfoModel> CustomBreadcrumbItemsWithCustomized =
    [
        new() { Name = "Custom 1" },
        new() { Name = "Custom 2" },
        new() { Name = "Custom 3" },
        new() { Name = "Custom 4", IsCurrent = true }
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
            Name = $"Custom {ItemsCount}"
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
