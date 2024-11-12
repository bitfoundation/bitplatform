namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public partial class _BitBreadcrumbItemDemo
{
    private readonly string example1RazorCode = @"
<BitBreadcrumb Items=""BreadcrumbItems"" />

<BitBreadcrumb Items=""BreadcrumbItems"" IsEnabled=""false"" />

<BitBreadcrumb Items=""BreadcrumbItemsDisabled"" />";
    private readonly string example1CsharpCode = @"
private readonly List<BitBreadcrumbItem> BreadcrumbItems =
[
    new() { Text = ""Item 1"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Item 2"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Item 3"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Item 4"", Href = ""/components/breadcrumb"", IsSelected = true }
];

private readonly List<BitBreadcrumbItem> BreadcrumbItemsDisabled =
[
    new() { Text = ""Item 1"", Href = ""/components/breadcrumb"", IsEnabled = false },
    new() { Text = ""Item 2"", Href = ""/components/breadcrumb"", IsEnabled = false },
    new() { Text = ""Item 3"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Item 4"", Href = ""/components/breadcrumb"", IsSelected = true }
];";

    private readonly string example2RazorCode = @"
<BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""1"" />

<BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""2"" />

<BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""3"" />

<BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""3"" OverflowIndex=""0"" />

<BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""3"" OverflowIndex=""1"" />

<BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""3"" OverflowIndex=""2"" />";
    private readonly string example2CsharpCode = @"
private readonly List<BitBreadcrumbItem> BreadcrumbItems =
[
    new() { Text = ""Item 1"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Item 2"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Item 3"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Item 4"", Href = ""/components/breadcrumb"", IsSelected = true }
];";

    private readonly string example3RazorCode = @"
<BitBreadcrumb Items=""BreadcrumbItemsWitIcon""
               DividerIconName=""@BitIconName.CaretRightSolid8""
               OverflowIconName=""@BitIconName.ChevronDown""
               OverflowIndex=""2""
               MaxDisplayedItems=""3"" />

<BitBreadcrumb Items=""BreadcrumbItemsWitIcon""
               OverflowIconName=""@BitIconName.CollapseMenu""
               MaxDisplayedItems=""3""
               OverflowIndex=""2""
               ReversedIcon />";
    private readonly string example3CsharpCode = @"
private readonly List<BitBreadcrumbItem> BreadcrumbItemsWitIcon =
[
    new() { Text = ""Item 1"", Href = ""/components/breadcrumb"", IconName = BitIconName.AdminELogoInverse32 },
    new() { Text = ""Item 2"", Href = ""/components/breadcrumb"", IconName = BitIconName.AppsContent },
    new() { Text = ""Item 3"", Href = ""/components/breadcrumb"", IconName = BitIconName.AzureIcon },
    new() { Text = ""Item 4"", Href = ""/components/breadcrumb"", IsSelected = true, IconName = BitIconName.ClassNotebookLogo16 }
];";

    private readonly string example4RazorCode = @"
<BitBreadcrumb Items=""BreadcrumbItems"">
    <DividerIconTemplate>
        <BitIcon IconName=""@BitIconName.CaretRightSolid8"" Color=""BitColor.Warning"" />
    </DividerIconTemplate>
</BitBreadcrumb>
            
<BitBreadcrumb Items=""BreadcrumbItems"" 
                MaxDisplayedItems=""3""
                OverflowIndex=""2"">
    <ItemTemplate Context=""item"">
        <div style=""font-weight: bold; color: #d13438; font-style:italic;"">
            @item.Text
        </div>
    </ItemTemplate>
    <OverflowTemplate Context=""item"">
        <div style=""font-weight: bold; color: blueviolet; font-style:italic;"">
            @item.Text
        </div>
    </OverflowTemplate>
</BitBreadcrumb>

<BitBreadcrumb Items=""BreadcrumbItemTemplateItems""
                MaxDisplayedItems=""3""
                OverflowIndex=""2"" />";
    private readonly string example4CsharpCode = @"
private readonly List<BitBreadcrumbItem> BreadcrumbItems =
[
    new() { Text = ""Item 1"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Item 2"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Item 3"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Item 4"", Href = ""/components/breadcrumb"", IsSelected = true }
];

private readonly List<BitBreadcrumbItem> BreadcrumbItemTemplateItems =
[
    new()
    {
        Text = ""Item 1"", Href = ""/components/breadcrumb"",
        Template = (item => @<div style=""color:green"">@item.Text</div>),
        OverflowTemplate = (item => @<div style=""color:green;text-decoration:underline;"">@item.Text</div>)
    },
    new ()
    {
        Text = ""Item 2"", Href = ""/components/breadcrumb"",
        Template = (item => @<div style=""color:yellow"">@item.Text</div>),
        OverflowTemplate = (item => @<div style=""color:yellow;text-decoration:underline;"">@item.Text</div>)
    },
    new()
    {
        Text = ""Item 3"", Href = ""/components/breadcrumb"",
        Template = (item => @<div style=""color:red"">@item.Text</div>),
        OverflowTemplate = (item => @<div style=""color:red;text-decoration:underline;"">@item.Text</div>)
    },
    new()
    {
        Text = ""Item 4"", Href = ""/components/breadcrumb"", IsSelected = true,
        Template = (item => @<div style=""color:blue"">@item.Text</div>),
        OverflowTemplate = (item => @<div style=""color:blue;text-decoration:underline;"">@item.Text</div>)
    }
];";

    private readonly string example5RazorCode = @"
<BitBreadcrumb Items=""@BreadcrumbItemsWithControlled""
               MaxDisplayedItems=""3""
               OverflowIndex=""2""
               OnItemClick=""(BitBreadcrumbItem item) => HandleOnItemClick(item)""
               Styles=""@(new() { SelectedItem = ""color: dodgerblue;"", OverflowSelectedItem = ""color: red;"" })"" />";
    private readonly string example5CsharpCode = @"
private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithControlled =
[
    new() { Text = ""Item 1"" },
    new() { Text = ""Item 2"" },
    new() { Text = ""Item 3"" },
    new() { Text = ""Item 4"" },
    new() { Text = ""Item 5"" },
    new() { Text = ""Item 6"", IsSelected = true }
];

private void HandleOnItemClick(BitBreadcrumbItem item)
{
    BreadcrumbItemsWithControlled.FirstOrDefault(i => i.IsSelected).IsSelected = false;
    item.IsSelected = true;
}";

    private readonly string example6RazorCode = @"
<BitBreadcrumb Items=""@BreadcrumbItemsWithCustomized""
                MaxDisplayedItems=""@MaxDisplayedItems""
                OverflowIndex=""@OverflowIndex""
                OnItemClick=""(BitBreadcrumbItem item) => HandleOnCustomizedItemClick(item)"" />

<BitButton OnClick=""AddBreadcrumbItem"">Add Item</BitButton>
<BitButton OnClick=""RemoveBreadcrumbItem"">Remove Item</BitButton>

<BitNumberField @bind-Value=""MaxDisplayedItems"" Label=""Max displayed items"" ShowArrows=""true"" />
<BitNumberField @bind-Value=""OverflowIndex"" Label=""Overflow index"" ShowArrows=""true"" />";
    private readonly string example6CsharpCode = @"
private int ItemsCount = 4;
private uint OverflowIndex = 2;
private uint MaxDisplayedItems = 3;

private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithCustomized =
[
    new() { Text = ""Item 1"" },
    new() { Text = ""Item 2"" },
    new() { Text = ""Item 3"" },
    new() { Text = ""Item 4"", IsSelected = true }
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
        Name = $""Item {ItemsCount}""
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


<BitBreadcrumb Items=""BreadcrumbItems"" Class=""custom-class"" />

<BitBreadcrumb Items=""BreadcrumbItems"" Style=""font-style: italic;text-shadow: aqua 0 0 0.5rem;border-bottom: 1px solid aqua;"" />

<BitBreadcrumb Items=""BreadcrumbItemsWithClass"" />

<BitBreadcrumb Items=""BreadcrumbItemsWithStyle"" />

<BitBreadcrumb Items=""BreadcrumbItems""
               Classes=""@(new() { Item = ""custom-item"", SelectedItem = ""custom-selected-item"" })"" />

<BitBreadcrumb Items=""BreadcrumbItems""
               Styles=""@(new() { Item = ""color: green;"", SelectedItem = ""color: lightseagreen; text-shadow: lightseagreen 0 0 1rem;"" })"" />";
    private readonly string example7CsharpCode = @"
private readonly List<BitBreadcrumbItem> BreadcrumbItems =
[
    new() { Text = ""Item 1"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Item 2"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Item 3"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Item 4"", Href = ""/components/breadcrumb"", IsSelected = true }
];

private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithClass =
[
    new() { Text = ""Item 1"", Href = ""/components/breadcrumb"", Class = ""custom-item-1"" },
    new() { Text = ""Item 2"", Href = ""/components/breadcrumb"", Class = ""custom-item-2"" },
    new() { Text = ""Item 3"", Href = ""/components/breadcrumb"", Class = ""custom-item-1"" },
    new() { Text = ""Item 4"", Href = ""/components/breadcrumb"", Class = ""custom-item-2"", IsSelected = true }
];
        
private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithStyle =
[
    new() { Text = ""Item 1"", Href = ""/components/breadcrumb"", Style = ""color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;"" },
    new() { Text = ""Item 2"", Href = ""/components/breadcrumb"", Style = ""color: aqua; text-shadow: aqua 0 0 1rem;"" },
    new() { Text = ""Item 3"", Href = ""/components/breadcrumb"", Style = ""color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;"" },
    new() { Text = ""Item 4"", Href = ""/components/breadcrumb"", Style = ""color: aqua; text-shadow: aqua 0 0 1rem;"", IsSelected = true }
];";

    private readonly string example8RazorCode = @"
<BitBreadcrumb Dir=""BitDir.Rtl"" Items=""RtlBreadcrumbItems"" MaxDisplayedItems=""3"" OverflowIndex=""2"" />";
    private readonly string example8CsharpCode = @"
private readonly List<BitBreadcrumbItem> RtlBreadcrumbItems =
[
    new() { Text = ""پوشه اول"" },
    new() { Text = ""پوشه دوم"", IsSelected = true },
    new() { Text = ""پوشه سوم"" },
    new() { Text = ""پوشه چهارم"" },
    new() { Text = ""پوشه پنجم"" },
    new() { Text = ""پوشه ششم"" },
];";
}
