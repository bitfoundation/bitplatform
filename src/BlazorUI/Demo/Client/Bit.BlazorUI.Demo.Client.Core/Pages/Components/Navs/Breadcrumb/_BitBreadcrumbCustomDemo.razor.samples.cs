namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public partial class _BitBreadcrumbCustomDemo
{
    private readonly string example1RazorCode = @"
<BitBreadcrumb Items=""CustomBreadcrumbItems""
               NameSelectors=""nameSelectors"" />

<BitBreadcrumb Items=""CustomBreadcrumbItems""
               NameSelectors=""nameSelectors"" />

<BitBreadcrumb Items=""CustomBreadcrumbItemsDisabled""
               NameSelectors=""nameSelectors"" />";
    private readonly string example1CsharpCode = @"
public class PageInfo
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfo> CustomBreadcrumbItems =
[
    new() { Name = ""Custom 1"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Custom 2"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Custom 3"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Custom 4"", Address = ""/components/breadcrumb"", IsCurrent = true }
];

private readonly List<PageInfo> CustomBreadcrumbItemsDisabled =
[
    new() { Name = ""Custom 1"", Address = ""/components/breadcrumb"", IsEnabled = false },
    new() { Name = ""Custom 2"", Address = ""/components/breadcrumb"", IsEnabled = false },
    new() { Name = ""Custom 3"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Custom 4"", Address = ""/components/breadcrumb"", IsCurrent = true }
];

private BitBreadcrumbNameSelectors<PageInfo> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};";

    private readonly string example2RazorCode = @"
<BitBreadcrumb Items=""CustomBreadcrumbItems""
               NameSelectors=""nameSelectors""
               MaxDisplayedItems=""1"" />

<BitBreadcrumb Items=""CustomBreadcrumbItems""
               NameSelectors=""nameSelectors""
               MaxDisplayedItems=""2"" />

<BitBreadcrumb Items=""CustomBreadcrumbItems""
               NameSelectors=""nameSelectors""
               MaxDisplayedItems=""3"" />

<BitBreadcrumb Items=""CustomBreadcrumbItems""
               NameSelectors=""nameSelectors""
               MaxDisplayedItems=""3""
               OverflowIndex=""0"" />

<BitBreadcrumb Items=""CustomBreadcrumbItems""
               NameSelectors=""nameSelectors""
               MaxDisplayedItems=""3""
               OverflowIndex=""1"" />

<BitBreadcrumb Items=""CustomBreadcrumbItems""
               NameSelectors=""nameSelectors""
               MaxDisplayedItems=""3""
               OverflowIndex=""2"" />";
    private readonly string example2CsharpCode = @"
public class PageInfo
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfo> CustomBreadcrumbItems =
[
    new() { Name = ""Custom 1"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Custom 2"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Custom 3"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Custom 4"", Address = ""/components/breadcrumb"", IsCurrent = true }
];

private BitBreadcrumbNameSelectors<PageInfo> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};";

    private readonly string example3RazorCode = @"
<BitBreadcrumb Items=""CustomBreadcrumbItemsWithIcon""
               NameSelectors=""nameSelectors""
               MaxDisplayedItems=""3""
               OverflowIndex=""2""
               DividerIconName=""@BitIconName.CaretRightSolid8""
               OverflowIconName=""@BitIconName.ChevronDown"" />

<BitBreadcrumb Items=""CustomBreadcrumbItemsWithIcon""
               NameSelectors=""nameSelectors""
               MaxDisplayedItems=""3""
               OverflowIndex=""2""
               OverflowIconName=""@BitIconName.CollapseMenu""
               ReversedIcon />";
    private readonly string example3CsharpCode = @"
public class PageInfo
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public string Icon { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfo> CustomBreadcrumbItemsWithIcon =
[
    new() { Name = ""Custom 1"", Address = ""/components/breadcrumb"", Icon = BitIconName.AdminELogoInverse32 },
    new() { Name = ""Custom 2"", Address = ""/components/breadcrumb"", Icon = BitIconName.AppsContent },
    new() { Name = ""Custom 3"", Address = ""/components/breadcrumb"", Icon = BitIconName.AzureIcon },
    new() { Name = ""Custom 4"", Address = ""/components/breadcrumb"", Icon = BitIconName.ClassNotebookLogo16, IsCurrent = true }
];

private BitBreadcrumbNameSelectors<PageInfo> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle },
    IconName = { Selector = c => c.Icon },
};";

    private readonly string example4RazorCode = @"
<BitBreadcrumb Items=""CustomBreadcrumbItems""
               NameSelectors=""nameSelectors"">
    <DividerIconTemplate>
        <BitIcon IconName=""@BitIconName.CaretRightSolid8"" Color=""BitColor.Warning"" />
    </DividerIconTemplate>
</BitBreadcrumb>

<BitBreadcrumb Items=""CustomBreadcrumbItems""
               NameSelectors=""nameSelectors""
               MaxDisplayedItems=""3""
               OverflowIndex=""2"">
    <ItemTemplate Context=""item"">
        <div style=""font-weight: bold; color: #d13438; font-style:italic;"">
            @item.Name
        </div>
    </ItemTemplate>
    <OverflowTemplate Context=""item"">
        <div style=""font-weight: bold; color: blueviolet; font-style:italic;"">
            @item.Name
        </div>
    </OverflowTemplate>
</BitBreadcrumb>

<BitBreadcrumb Items=""CustomBreadcrumbItemTemplateItems""
               NameSelectors=""nameSelectors""
               MaxDisplayedItems=""3""
               OverflowIndex=""2"" />";
    private readonly string example4CsharpCode = @"
public class PageInfo
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;

    public RenderFragment<PageInfo>? Fragment { get; set; }

    public RenderFragment<PageInfo>? OverflowFragment { get; set; }
}

private readonly List<PageInfo> CustomBreadcrumbItems =
[
    new() { Name = ""Custom 1"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Custom 2"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Custom 3"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Custom 4"", Address = ""/components/breadcrumb"", IsCurrent = true }
];

private readonly List<PageInfo> CustomBreadcrumbItemTemplateItems =
[
    new()
    {
        Name = ""Item 1"", Address = ""/components/breadcrumb"",
        Fragment = (item => @<div style=""color:green"">@item.Name</div>),
        OverflowFragment = (item => @<div style=""color:green;text-decoration:underline;"">@item.Name</div>)
    },
    new ()
    {
        Name = ""Item 2"", Address = ""/components/breadcrumb"",
        Fragment = (item => @<div style=""color:yellow"">@item.Name</div>),
        OverflowFragment = (item => @<div style=""color:yellow;text-decoration:underline;"">@item.Name</div>)
    },
    new()
    {
        Name = ""Item 3"", Address = ""/components/breadcrumb"",
        Fragment = (item => @<div style=""color:red"">@item.Name</div>),
        OverflowFragment = (item => @<div style=""color:red;text-decoration:underline;"">@item.Name</div>)
    },
    new()
    {
        Name = ""Item 4"", Address = ""/components/breadcrumb"", IsCurrent = true,
        Fragment = (item => @<div style=""color:blue"">@item.Name</div>),
        OverflowFragment = (item => @<div style=""color:blue;text-decoration:underline;"">@item.Name</div>)
    }
];

private BitBreadcrumbNameSelectors<PageInfo> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle },
    Template = { Name = nameof(PageInfo.Fragment) },
    OverflowTemplate = { Name = nameof(PageInfo.OverflowFragment) }
};";

    private readonly string example5RazorCode = @"
<BitBreadcrumb Items=""CustomBreadcrumbItemsWithControlled""
               NameSelectors=""nameSelectors""
               MaxDisplayedItems=""3""
               OverflowIndex=""2""
               OnItemClick=""(PageInfo model) => HandleOnCustomClick(model)""
               Styles=""@(new() { SelectedItem = ""color: dodgerblue;"", OverflowSelectedItem = ""color: red;"" })"" />";
    private readonly string example5CsharpCode = @"
public class PageInfo
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfo> CustomBreadcrumbItemsWithControlled =
[
    new() { Name = ""Custom 1"" },
    new() { Name = ""Custom 2"" },
    new() { Name = ""Custom 3"" },
    new() { Name = ""Custom 4"" },
    new() { Name = ""Custom 5"" },
    new() { Name = ""Custom 6"", IsCurrent = true }
];

private BitBreadcrumbNameSelectors<PageInfo> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};

private void HandleOnCustomClick(PageInfo model)
{
    CustomBreadcrumbItemsWithControlled.First(i => i.IsCurrent).IsCurrent = false;
    model.IsCurrent = true;
}";

    private readonly string example6RazorCode = @"
<BitBreadcrumb Items=""CustomBreadcrumbItemsWithCustomized""
               NameSelectors=""nameSelectors""
               MaxDisplayedItems=""@MaxDisplayedItems""
               OverflowIndex=""@OverflowIndex""
               OnItemClick=""(PageInfo model) => HandleOnCustomizedCustomClick(model)"" />

<BitButton OnClick=""AddCustomItem"">Add Item</BitButton>
<BitButton OnClick=""RemoveCustomItem"">Remove Item</BitButton>

<BitNumberField @bind-Value=""MaxDisplayedItems"" Label=""Max displayed items"" ShowArrows=""true"" />
<BitNumberField @bind-Value=""OverflowIndex"" Label=""Overflow index"" ShowArrows=""true"" />";
    private readonly string example6CsharpCode = @"
private int ItemsCount = 4;
private uint OverflowIndex = 2;
private uint MaxDisplayedItems = 3;

public class PageInfo
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfo> CustomBreadcrumbItemsWithCustomized =
[
    new() { Name = ""Custom 1"" },
    new() { Name = ""Custom 2"" },
    new() { Name = ""Custom 3"" },
    new() { Name = ""Custom 4"", IsCurrent = true }
];

private BitBreadcrumbNameSelectors<PageInfo> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};

private void HandleOnCustomizedCustomClick(PageInfo model)
{
    CustomBreadcrumbItemsWithCustomized.First(i => i.IsCurrent).IsCurrent = false;
    model.IsCurrent = true;
}

private void AddCustomItem()
{
    ItemsCount++;
    BreadcrumbItemsWithCustomized.Add(new BitBreadcrumbItem()
    {
        Text = $""Custom {ItemsCount}""
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
}";

    private readonly string example7RazorCode = @"
<style>
    .custom-class {
        font-style: italic;
        text-shadow: dodgerblue 0 0 0.5rem;
        border-bottom: 1px solid dodgerblue;
    }

    .custom-item {
        color: #ffcece;

        &:hover {
            color: #ff6868;
            background: transparent;
        }
    }

    .custom-item-1 {
        color: #b6ff00;

        &:hover {
            color: #2aff00;
            background: transparent;
        }
    }

    .custom-item-2 {
        color: #ffd800;

        &:hover {
            color: #ff6a00;
            background: transparent;
        }
    }

    .custom-selected-item {
        color: blueviolet;

        &:hover {
            color: blueviolet;
            background: transparent;
            text-shadow: blueviolet 0 0 1rem;
        }
    }
</style>

<BitBreadcrumb Items=""CustomBreadcrumbItems""
               NameSelectors=""nameSelectors"" 
               Class=""custom-class"" />

<BitBreadcrumb Items=""CustomBreadcrumbItems""
               NameSelectors=""nameSelectors"" 
               Style=""font-style: italic;text-shadow: aqua 0 0 0.5rem;border-bottom: 1px solid aqua;"" />

<BitBreadcrumb Items=""CustomBreadcrumbItemsWithClass""
               NameSelectors=""nameSelectors"" />

<BitBreadcrumb Items=""CustomBreadcrumbItemsWithStyle""
               NameSelectors=""nameSelectors"" />

<BitBreadcrumb Items=""CustomBreadcrumbItems""
               NameSelectors=""nameSelectors""
               Classes=""@(new() { Item = ""custom-item"", SelectedItem = ""custom-selected-item"" })"" />

<BitBreadcrumb Items=""CustomBreadcrumbItems""
               NameSelectors=""nameSelectors""
               Styles=""@(new() { Item = ""color: green;"", SelectedItem = ""color: lightseagreen; text-shadow: lightseagreen 0 0 1rem;"" })"" />";
    private readonly string example7CsharpCode = @"
public class PageInfo
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfo> CustomBreadcrumbItems =
[
    new() { Name = ""Custom 1"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Custom 2"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Custom 3"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Custom 4"", Address = ""/components/breadcrumb"", IsCurrent = true }
];

private readonly List<PageInfo> CustomBreadcrumbItemsWithClass =
[
    new() { Name = ""Custom 1"", Address = ""/components/breadcrumb"", HtmlClass = ""custom-item-1"" },
    new() { Name = ""Custom 2"", Address = ""/components/breadcrumb"", HtmlClass = ""custom-item-2"" },
    new() { Name = ""Custom 3"", Address = ""/components/breadcrumb"", HtmlClass = ""custom-item-1"" },
    new() { Name = ""Custom 4"", Address = ""/components/breadcrumb"", HtmlClass = ""custom-item-2"", IsCurrent = true }
];

private readonly List<PageInfo> CustomBreadcrumbItemsWithStyle =
[
    new() { Name = ""Custom 1"", Address = ""/components/breadcrumb"", HtmlStyle = ""color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;"" },
    new() { Name = ""Custom 2"", Address = ""/components/breadcrumb"", HtmlStyle = ""color: aqua; text-shadow: aqua 0 0 1rem;"" },
    new() { Name = ""Custom 3"", Address = ""/components/breadcrumb"", HtmlStyle = ""color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;"" },
    new() { Name = ""Custom 4"", Address = ""/components/breadcrumb"", HtmlStyle = ""color: aqua; text-shadow: aqua 0 0 1rem;"", IsCurrent = true }
];

private BitBreadcrumbNameSelectors<PageInfo> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};";

    private readonly string example8RazorCode = @"
<BitBreadcrumb Dir=""BitDir.Rtl""
               OverflowIndex=""2""
               MaxDisplayedItems=""3""
               Items=""RtlCustomBreadcrumbItems""
               NameSelectors=""nameSelectors"" />";
    private readonly string example8CsharpCode = @"
public class PageInfo
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfo> RtlCustomBreadcrumbItems =
[
    new() { Name = ""پوشه اول"" },
    new() { Name = ""پوشه دوم"", IsCurrent = true },
    new() { Name = ""پوشه سوم"" },
    new() { Name = ""پوشه چهارم"" },
    new() { Name = ""پوشه پنجم"" },
    new() { Name = ""پوشه ششم"" },
];

private BitBreadcrumbNameSelectors<PageInfo> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};";
}
