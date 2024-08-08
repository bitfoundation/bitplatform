namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public partial class _BitBreadcrumbCustomDemo
{


    private readonly string example1RazorCode = @"
<div>
    <BitLabel>Basic</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors"" />
</div>
<div>
    <BitLabel>Disabled</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors"" />
</div>
<div>
    <BitLabel>Item Disabled</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItemsDisabled""
                   NameSelectors=""nameSelectors"" />
</div>
";
    private readonly string example1CsharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfoModel> CustomBreadcrumbItems =
[
    new() { Name = ""Folder 1"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 2"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 3"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 4"", Address = ""/components/breadcrumb"", IsCurrent = true }
];

private readonly List<PageInfoModel> CustomBreadcrumbItemsDisabled =
[
    new() { Name = ""Folder 1"", Address = ""/components/breadcrumb"", IsEnabled = false },
    new() { Name = ""Folder 2"", Address = ""/components/breadcrumb"", IsEnabled = false },
    new() { Name = ""Folder 3"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 4"", Address = ""/components/breadcrumb"", IsCurrent = true }
];

private BitBreadcrumbNameSelectors<PageInfoModel> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};
";

    private readonly string example2RazorCode = @"
<div>
    <BitLabel>MaxDisplayedItems (1)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   MaxDisplayedItems=""1"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (2)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   MaxDisplayedItems=""2"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   MaxDisplayedItems=""3"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (0)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   MaxDisplayedItems=""3""
                   OverflowIndex=""0"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (1)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   MaxDisplayedItems=""3""
                   OverflowIndex=""1"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (2)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   MaxDisplayedItems=""3""
                   OverflowIndex=""2"" />
</div>
";
    private readonly string example2CsharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfoModel> CustomBreadcrumbItems =
[
    new() { Name = ""Folder 1"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 2"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 3"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 4"", Address = ""/components/breadcrumb"", IsCurrent = true }
];

private BitBreadcrumbNameSelectors<PageInfoModel> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};
";

    private readonly string example3RazorCode = @"
<div>
    <BitLabel>BitIconName (ChevronDown)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   MaxDisplayedItems=""3""
                   OverflowIndex=""2""
                   OverflowIcon=""@BitIconName.ChevronDown"" />
</div>
<div>
    <BitLabel>BitIconName (CollapseMenu)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   MaxDisplayedItems=""3""
                   OverflowIndex=""2""
                   OverflowIcon=""@BitIconName.CollapseMenu"" />
</div>
";
    private readonly string example3CsharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfoModel> CustomBreadcrumbItems =
[
    new() { Name = ""Folder 1"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 2"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 3"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 4"", Address = ""/components/breadcrumb"", IsCurrent = true }
];

private BitBreadcrumbNameSelectors<PageInfoModel> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};
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
    <BitBreadcrumb Items=""CustomBreadcrumbItemsWithClass""
                   NameSelectors=""nameSelectors"" />
</div>
<div>
    <BitLabel>Items Style</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItemsWithStyle""
                   NameSelectors=""nameSelectors"" />
</div>
<div>
    <BitLabel>Selected Item Class</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   SelectedItemClass=""custom-selected-item"" />
</div>
<div>
    <BitLabel>Selected Item Style</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   SelectedItemStyle=""color:red;background:lightgreen"" />
</div>
";
    private readonly string example4CsharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfoModel> CustomBreadcrumbItems =
[
    new() { Name = ""Folder 1"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 2"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 3"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 4"", Address = ""/components/breadcrumb"", IsCurrent = true }
];

private readonly List<PageInfoModel> CustomBreadcrumbItemsWithClass =
[
    new() { Name = ""Folder 1"", Address = ""/components/breadcrumb"", HtmlClass = ""custom-item"" },
    new() { Name = ""Folder 2"", Address = ""/components/breadcrumb"", HtmlClass = ""custom-item"" },
    new() { Name = ""Folder 3"", Address = ""/components/breadcrumb"", HtmlClass = ""custom-item"" },
    new() { Name = ""Folder 4"", Address = ""/components/breadcrumb"", HtmlClass = ""custom-item"", IsCurrent = true }
];

private readonly List<PageInfoModel> CustomBreadcrumbItemsWithStyle =
[
    new() { Name = ""Folder 1"", Address = ""/components/breadcrumb"", HtmlStyle = ""color:red;background:greenyellow"" },
    new() { Name = ""Folder 2"", Address = ""/components/breadcrumb"", HtmlStyle = ""color:red;background:greenyellow"" },
    new() { Name = ""Folder 3"", Address = ""/components/breadcrumb"", HtmlStyle = ""color:red;background:greenyellow"" },
    new() { Name = ""Folder 4"", Address = ""/components/breadcrumb"", HtmlStyle = ""color:red;background:greenyellow"", IsCurrent = true }
];

private BitBreadcrumbNameSelectors<PageInfoModel> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};
";

    private readonly string example5RazorCode = @"
<BitBreadcrumb Items=""CustomBreadcrumbItemsWithControlled""
               NameSelectors=""nameSelectors""
               MaxDisplayedItems=""3""
               OverflowIndex=""2""
               OnItemClick=""(PageInfoModel model) => HandleOnCustomClick(model)""
               SelectedItemStyle=""color:red;background:lightgreen"" />
";
    private readonly string example5CsharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfoModel> CustomBreadcrumbItemsWithControlled =
[
    new() { Name = ""Folder 1"" },
    new() { Name = ""Folder 2"" },
    new() { Name = ""Folder 3"" },
    new() { Name = ""Folder 4"" },
    new() { Name = ""Folder 5"" },
    new() { Name = ""Folder 6"", IsCurrent = true }
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
}";

    private readonly string example6RazorCode = @"
<div>
    <BitBreadcrumb Items=""CustomBreadcrumbItemsWithCustomized""
                   NameSelectors=""nameSelectors""
                   MaxDisplayedItems=""@MaxDisplayedItems""
                   OverflowIndex=""@OverflowIndex""
                   OnItemClick=""(PageInfoModel model) => HandleOnCustomizedCustomClick(model)"" />
</div>
<div class=""operators"">
    <div>
        <BitButton OnClick=""AddCustomItem"">Add Item</BitButton>
        <BitButton OnClick=""RemoveCustomItem"">Remove Item</BitButton>
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

public class PageInfoModel
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfoModel> CustomBreadcrumbItemsWithCustomized =
[
    new() { Name = ""Folder 1"" },
    new() { Name = ""Folder 2"" },
    new() { Name = ""Folder 3"" },
    new() { Name = ""Folder 4"", IsCurrent = true }
];

private BitBreadcrumbNameSelectors<PageInfoModel> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};

private void HandleOnCustomizedCustomClick(PageInfoModel model)
{
    CustomBreadcrumbItemsWithCustomized.First(i => i.IsCurrent).IsCurrent = false;
    model.IsCurrent = true;
}

private void AddCustomItem()
{
    ItemsCount++;
    BreadcrumbItemsWithCustomized.Add(new BitBreadcrumbItem()
    {
        Text = $""Folder {ItemsCount}""
    });
}

private void RemoveCustomItem()
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
";

    private readonly string example7RazorCode = @"
<BitBreadcrumb Dir=""BitDir.Rtl""
               OverflowIndex=""2""
               MaxDisplayedItems=""3""
               Items=""RtlCustomBreadcrumbItems""
               NameSelectors=""nameSelectors"" />
";
    private readonly string example7CsharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfoModel> RtlCustomBreadcrumbItems =
[
    new() { Name = ""پوشه اول"" },
    new() { Name = ""پوشه دوم"", IsCurrent = true },
    new() { Name = ""پوشه سوم"" },
    new() { Name = ""پوشه چهارم"" },
    new() { Name = ""پوشه پنجم"" },
    new() { Name = ""پوشه ششم"" },
];

private BitBreadcrumbNameSelectors<PageInfoModel> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};";
}
