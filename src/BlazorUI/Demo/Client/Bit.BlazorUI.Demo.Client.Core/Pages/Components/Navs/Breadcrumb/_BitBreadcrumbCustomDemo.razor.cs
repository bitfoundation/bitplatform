namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public partial class _BitBreadcrumbCustomDemo
{
    private int ItemsCount = 4;
    private uint OverflowIndex = 2;
    private uint MaxDisplayedItems = 3;

    private readonly List<PageInfo> CustomBreadcrumbItems =
    [
        new() { Name = "Custom 1", Address = "/components/breadcrumb" },
        new() { Name = "Custom 2", Address = "/components/breadcrumb" },
        new() { Name = "Custom 3", Address = "/components/breadcrumb" },
        new() { Name = "Custom 4", Address = "/components/breadcrumb", IsCurrent = true }
    ];

    private readonly List<PageInfo> CustomBreadcrumbItemsDisabled =
    [
        new() { Name = "Custom 1", Address = "/components/breadcrumb", IsEnabled = false },
        new() { Name = "Custom 2", Address = "/components/breadcrumb", IsEnabled = false },
        new() { Name = "Custom 3", Address = "/components/breadcrumb" },
        new() { Name = "Custom 4", Address = "/components/breadcrumb", IsCurrent = true }
    ];

    private readonly List<PageInfo> CustomBreadcrumbItemsWithIcon =
    [
        new() { Name = "Custom 1", Address = "/components/breadcrumb", Icon = BitIconName.AdminELogoInverse32 },
        new() { Name = "Custom 2", Address = "/components/breadcrumb", Icon = BitIconName.AppsContent },
        new() { Name = "Custom 3", Address = "/components/breadcrumb", Icon = BitIconName.AzureIcon },
        new() { Name = "Custom 4", Address = "/components/breadcrumb", Icon = BitIconName.ClassNotebookLogo16, IsCurrent = true }
    ];

    private readonly List<PageInfo> CustomBreadcrumbItemsWithClass =
    [
        new() { Name = "Custom 1", Address = "/components/breadcrumb", HtmlClass = "custom-item-1" },
        new() { Name = "Custom 2", Address = "/components/breadcrumb", HtmlClass = "custom-item-2" },
        new() { Name = "Custom 3", Address = "/components/breadcrumb", HtmlClass = "custom-item-1" },
        new() { Name = "Custom 4", Address = "/components/breadcrumb", HtmlClass = "custom-item-2", IsCurrent = true }
    ];

    private readonly List<PageInfo> CustomBreadcrumbItemsWithStyle =
    [
        new() { Name = "Custom 1", Address = "/components/breadcrumb", HtmlStyle = "color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;" },
        new() { Name = "Custom 2", Address = "/components/breadcrumb", HtmlStyle = "color: aqua; text-shadow: aqua 0 0 1rem;" },
        new() { Name = "Custom 3", Address = "/components/breadcrumb", HtmlStyle = "color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;" },
        new() { Name = "Custom 4", Address = "/components/breadcrumb", HtmlStyle = "color: aqua; text-shadow: aqua 0 0 1rem;", IsCurrent = true }
    ];

    private readonly List<PageInfo> CustomBreadcrumbItemsWithControlled =
    [
        new() { Name = "Custom 1" },
        new() { Name = "Custom 2" },
        new() { Name = "Custom 3" },
        new() { Name = "Custom 4" },
        new() { Name = "Custom 5" },
        new() { Name = "Custom 6", IsCurrent = true }
    ];

    private readonly List<PageInfo> CustomBreadcrumbItemsWithCustomized =
    [
        new() { Name = "Custom 1" },
        new() { Name = "Custom 2" },
        new() { Name = "Custom 3" },
        new() { Name = "Custom 4", IsCurrent = true }
    ];

    private readonly List<PageInfo> RtlCustomBreadcrumbItems =
    [
        new() { Name = "پوشه اول" },
        new() { Name = "پوشه دوم", IsCurrent = true },
        new() { Name = "پوشه سوم" },
        new() { Name = "پوشه چهارم" },
        new() { Name = "پوشه پنجم" },
        new() { Name = "پوشه ششم" },
    ];

    private BitBreadcrumbNameSelectors<PageInfo> nameSelectors = new()
    {
        Text = { Selector = c => c.Name },
        Href = { Selector = c => c.Address },
        IsSelected = { Selector = c => c.IsCurrent },
        Class = { Selector = c => c.HtmlClass },
        Style = { Selector = c => c.HtmlStyle },
        IconName = { Selector = c => c.Icon },
        Template = { Name = nameof(PageInfo.Fragment) },
        OverflowTemplate = { Name = nameof(PageInfo.OverflowFragment) }
    };

    private void HandleOnCustomClick(PageInfo model)
    {
        CustomBreadcrumbItemsWithControlled.First(i => i.IsCurrent).IsCurrent = false;
        model.IsCurrent = true;
    }

    private void HandleOnCustomizedCustomClick(PageInfo model)
    {
        CustomBreadcrumbItemsWithCustomized.First(i => i.IsCurrent).IsCurrent = false;
        model.IsCurrent = true;
    }

    private void AddCustomItem()
    {
        ItemsCount++;
        CustomBreadcrumbItemsWithCustomized.Add(new PageInfo()
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
