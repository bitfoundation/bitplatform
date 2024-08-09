namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public partial class _BitBreadcrumbItemDemo
{
    private readonly string example1RazorCode = @"
<div>
    <BitLabel>Basic</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems"" />
</div>
<div>
    <BitLabel>Disabled</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems"" IsEnabled=""false"" />
</div>
<div>
    <BitLabel>Item Disabled</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItemsDisabled"" />
</div>
";
    private readonly string example1CsharpCode = @"
private readonly List<BitBreadcrumbItem> BreadcrumbItems =
[
    new() { Text = ""Folder 1"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 2"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 3"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 4"", Href = ""/components/breadcrumb"", IsSelected = true }
];

private readonly List<BitBreadcrumbItem> BreadcrumbItemsDisabled =
[
    new() { Text = ""Folder 1"", Href = ""/components/breadcrumb"", IsEnabled = false },
    new() { Text = ""Folder 2"", Href = ""/components/breadcrumb"", IsEnabled = false },
    new() { Text = ""Folder 3"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 4"", Href = ""/components/breadcrumb"", IsSelected = true }
];
";

    private readonly string example2RazorCode = @"
<div>
    <BitLabel>MaxDisplayedItems (1)</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""1"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (2)</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""2"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3)</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""3"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (0)</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""3"" OverflowIndex=""0"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (1)</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""3"" OverflowIndex=""1"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (2)</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""3"" OverflowIndex=""2"" />
</div>
";
    private readonly string example2CsharpCode = @"
private readonly List<BitBreadcrumbItem> BreadcrumbItems =
[
    new() { Text = ""Folder 1"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 2"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 3"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 4"", Href = ""/components/breadcrumb"", IsSelected = true }
];
";

    private readonly string example3RazorCode = @"
<div>
    <BitLabel>BitIconName (ChevronDown)</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems""
                   MaxDisplayedItems=""3""
                   OverflowIndex=""2""
                   OverflowIcon=""@BitIconName.ChevronDown"" />
</div>
<div>
    <BitLabel>BitIconName (CollapseMenu)</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems""
                   MaxDisplayedItems=""3""
                   OverflowIndex=""2""
                   OverflowIcon=""@BitIconName.CollapseMenu"" />
</div>
";
    private readonly string example3CsharpCode = @"
private readonly List<BitBreadcrumbItem> BreadcrumbItems =
[
    new() { Text = ""Folder 1"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 2"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 3"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 4"", Href = ""/components/breadcrumb"", IsSelected = true }
];
";

    private readonly string example4RazorCode = @"
<style>
    .custom-item {
        color: red;
        margin: 2px 5px;
        border-radius: 2px;
        background: limegreen;

        &:hover {
            background: greenyellow;
        }
    }

    .custom-selected-item {
        color: red;
        margin: 2px 5px;
        border-radius: 2px;
        background: mediumspringgreen;

        &:hover {
            background: greenyellow;
        }
    }
</style>

<div>
    <BitLabel>Items Class</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItemsWithClass"" />
</div>
<div>
    <BitLabel>Items Style</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItemsWithStyle"" />
</div>
<div>
    <BitLabel>Selected Item Class</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems""
                   SelectedItemClass=""custom-selected-item"" />
</div>
<div>
    <BitLabel>Selected Item Style</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems""
                   SelectedItemStyle=""color:red;background:lightgreen"" />
</div>
";
    private readonly string example4CsharpCode = @"
private readonly List<BitBreadcrumbItem> BreadcrumbItems =
[
    new() { Text = ""Folder 1"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 2"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 3"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 4"", Href = ""/components/breadcrumb"", IsSelected = true }
];

private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithClass =
[
    new() { Text = ""Folder 1"", Href = ""/components/breadcrumb"", Class = ""custom-item"" },
    new() { Text = ""Folder 2"", Href = ""/components/breadcrumb"", Class = ""custom-item"" },
    new() { Text = ""Folder 3"", Href = ""/components/breadcrumb"", Class = ""custom-item"" },
    new() { Text = ""Folder 4"", Href = ""/components/breadcrumb"", Class = ""custom-item"", IsSelected = true }
];

private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithStyle =
[
    new() { Text = ""Folder 1"", Href = ""/components/breadcrumb"", Style = ""color:red;background:greenyellow"" },
    new() { Text = ""Folder 2"", Href = ""/components/breadcrumb"", Style = ""color:red;background:greenyellow"" },
    new() { Text = ""Folder 3"", Href = ""/components/breadcrumb"", Style = ""color:red;background:greenyellow"" },
    new() { Text = ""Folder 4"", Href = ""/components/breadcrumb"", Style = ""color:red;background:greenyellow"", IsSelected = true }
];
";

    private readonly string example5RazorCode = @"
<BitBreadcrumb Items=""@BreadcrumbItemsWithControlled""
               MaxDisplayedItems=""3""
               OverflowIndex=""2""
               OnItemClick=""(BitBreadcrumbItem item) => HandleOnItemClick(item)""
               SelectedItemStyle=""color:red;background:lightgreen"" />
";
    private readonly string example5CsharpCode = @"
private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithControlled =
[
    new() { Text = ""Folder 1"" },
    new() { Text = ""Folder 2"" },
    new() { Text = ""Folder 3"" },
    new() { Text = ""Folder 4"" },
    new() { Text = ""Folder 5"" },
    new() { Text = ""Folder 6"", IsSelected = true }
];

private void HandleOnItemClick(BitBreadcrumbItem item)
{
    BreadcrumbItemsWithControlled.FirstOrDefault(i => i.IsSelected).IsSelected = false;
    item.IsSelected = true;
}
";

    private readonly string example6RazorCode = @"
<div>
    <BitBreadcrumb Items=""@BreadcrumbItemsWithCustomized""
                    MaxDisplayedItems=""@MaxDisplayedItems""
                    OverflowIndex=""@OverflowIndex""
                    OnItemClick=""(BitBreadcrumbItem item) => HandleOnCustomizedItemClick(item)"" />
</div>
<div class=""operators"">
    <div>
        <BitButton OnClick=""AddBreadcrumbItem"">Add Item</BitButton>
        <BitButton OnClick=""RemoveBreadcrumbItem"">Remove Item</BitButton>
    </div>
    <div>
        <BitNumberField @bind-Value=""MaxDisplayedItems"" Label=""MaxDisplayedItems"" ShowArrows=""true"" />
        <BitNumberField @bind-Value=""OverflowIndex"" Label=""OverflowIndex"" ShowArrows=""true"" />
    </div>
</div>
";
    private readonly string example6CsharpCode = @"
private int ItemsCount = 4;
private uint OverflowIndex = 2;
private uint MaxDisplayedItems = 3;

private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithCustomized =
[
    new() { Text = ""Folder 1"" },
    new() { Text = ""Folder 2"" },
    new() { Text = ""Folder 3"" },
    new() { Text = ""Folder 4"", IsSelected = true }
];

private void HandleOnCustomizedItemClick(BitBreadcrumbItem item)
{
    BreadcrumbItemsWithCustomized.FirstOrDefault(i => i.IsSelected).IsSelected = false;
    item.IsSelected = true;
}

private void AddCustomItem()
{
    ItemsCount++;
    CustomBreadcrumbItemsWithCustomized.Add(new PageInfoModel()
    {
        Name = $""Folder {ItemsCount}""
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
}";

    private readonly string example7RazorCode = @"
<BitBreadcrumb Dir=""BitDir.Rtl"" Items=""RtlBreadcrumbItems"" MaxDisplayedItems=""3"" OverflowIndex=""2"" />
";
    private readonly string example7CsharpCode = @"
private readonly List<BitBreadcrumbItem> RtlBreadcrumbItems =
[
    new() { Text = ""پوشه اول"" },
    new() { Text = ""پوشه دوم"", IsSelected = true },
    new() { Text = ""پوشه سوم"" },
    new() { Text = ""پوشه چهارم"" },
    new() { Text = ""پوشه پنجم"" },
    new() { Text = ""پوشه ششم"" },
];
";
}
