using System.Linq;
using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.BreadList;

public partial class BitBreadListDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "ClassField",
            Type = "string",
            DefaultValue = "Class",
            Description = "Class HTML attribute for BreadList item."
        },
        new()
        {
            Name = "ClassFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Class HTML attribute for BreadList item."
        },
        new()
        {
            Name = "DividerIcon",
            Type = "BitIconName",
            DefaultValue = "BitIconName.ChevronRight",
            Description = "Render a custom divider in place of the default chevron."
        },
        new()
        {
            Name = "HrefField",
            Type = "string",
            DefaultValue = "Href",
            Description = "URL to navigate to when this BreadList item is clicked. If provided, the BreadList will be rendered as a link."
        },
        new()
        {
            Name = "HrefFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "URL to navigate to when this BreadList item is clicked. If provided, the BreadList will be rendered as a link."
        },
        new()
        {
            Name = "Items",
            Type = "IList<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "Collection of BreadList items to render."
        },
        new()
        {
            Name = "IsSelectedField",
            Type = "string",
            DefaultValue = "IsSelected",
            Description = "Display the item as a Selected item."
        },
        new()
        {
            Name = "IsSelectedFieldSelector",
            Type = "Expression<Func<TItem, bool>>?",
            Description = "Display the item as a Selected item."
        },
        new()
        {
            Name = "IsEnabledField",
            Type = "string",
            DefaultValue = "",
            Description = "Whether an item is enabled or not."
        },
        new()
        {
            Name = "IsEnabledFieldSelector",
            Type = "Expression<Func<TItem, bool>>?",
            Description = "Whether an item is enabled or not."
        },
        new()
        {
            Name = "MaxDisplayedItems",
            Type = "int",
            Description = "The maximum number of BreadLists to display before coalescing. If not specified, all BreadLists will be rendered."
        },
        new()
        {
            Name = "OverflowAriaLabel",
            Type = "string?",
            Description = "Aria label for the overflow button."
        },
        new()
        {
            Name = "OverflowIndex",
            Type = "int",
            Description = "Optional index where overflow items will be collapsed."
        },
        new()
        {
            Name = "OverflowIcon",
            Type = "BitIconName",
            DefaultValue= "BitIconName.More",
            Description = "Render a custom overflow icon in place of the default icon."
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            Description = "Callback for when the BreadList item clicked."
        },
        new()
        {
            Name = "SelectedItemClass",
            Type = "string?",
            Description = "The class HTML attribute for Selected Item."
        },
        new()
        {
            Name = "SelectedItemStyle",
            Type = "string?",
            Description = "The style HTML attribute for Selected Item."
        },
        new()
        {
            Name = "TextField",
            Type = "string",
            DefaultValue = "Text",
            Description = "Text to display in the BreadList item."
        },
        new()
        {
            Name = "TextFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Text to display in the BreadList item."
        },
        new()
        {
            Name = "StyleField",
            Type = "string",
            DefaultValue = "Style",
            Description = "Style HTML attribute for BreadList item."
        },
        new()
        {
            Name = "StyleFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Style HTML attribute for BreadList item."
        },
    };


    private readonly List<PageInfoModel> BasicBreadListItems = new()
    {
        new() { Name = "Item 1", Href = "/components/bread-list" },
        new() { Name = "Item 2", Href = "/components/bread-list" },
        new() { Name = "Item 3", Href = "/components/bread-list" },
        new() { Name = "Item 4", Href = "/components/bread-list", IsSelected = true },
    };

    private readonly List<PageInfoModel> BasicBreadListItemsDisabled = new()
    {
        new PageInfoModel { Name = "Item 1", Href = "/components/bread-list", IsEnabled = false },
        new PageInfoModel { Name = "Item 2", Href = "/components/bread-list", IsEnabled = false },
        new PageInfoModel { Name = "Item 3", Href = "/components/bread-list" },
        new PageInfoModel { Name = "Item 4", Href = "/components/bread-list", IsSelected = true },
    };

    private readonly List<PageInfoModel> BreadListItemsWithClass = new()
    {
        new PageInfoModel { Name = "Item 1", Href = "/components/bread-list", HtmlClass = "custom-item" },
        new PageInfoModel { Name = "Item 2", Href = "/components/bread-list", HtmlClass = "custom-item" },
        new PageInfoModel { Name = "Item 3", Href = "/components/bread-list", HtmlClass = "custom-item" },
        new PageInfoModel { Name = "Item 4", Href = "/components/bread-list", HtmlClass = "custom-item", IsSelected = true },
    };

    private readonly List<PageInfoModel> BreadListItemsWithStyle = new()
    {
        new PageInfoModel { Name = "Item 1", Href = "/components/bread-list", HtmlStyle = "color:red; background:greenyellow;" },
        new PageInfoModel { Name = "Item 2", Href = "/components/bread-list", HtmlStyle = "color:red; background:greenyellow;" },
        new PageInfoModel { Name = "Item 3", Href = "/components/bread-list", HtmlStyle = "color:red; background:greenyellow;" },
        new PageInfoModel { Name = "Item 4", Href = "/components/bread-list", HtmlStyle = "color:red; background:greenyellow;", IsSelected = true },
    };

    private readonly List<PageInfoModel> BreadListItemsWithControlled = new()
    {
        new PageInfoModel { Name = "Item 1" },
        new PageInfoModel { Name = "Item 2" },
        new PageInfoModel { Name = "Item 3" },
        new PageInfoModel { Name = "Item 4" },
        new PageInfoModel { Name = "Item 5" },
        new PageInfoModel { Name = "Item 6", IsSelected = true },
    };

    private readonly List<PageInfoModel> BreadListItemsWithCustomized = new()
    {
        new PageInfoModel { Name = "Item 1" },
        new PageInfoModel { Name = "Item 2" },
        new PageInfoModel { Name = "Item 3" },
        new PageInfoModel { Name = "Item 4", IsSelected = true }
    };

    private uint ItemsCount = 4;
    private uint OverflowIndex = 2;
    private uint MaxDisplayedItems = 3;
    private uint NumericTextFieldStep = 1;

    private void HandleOnItemClick(PageInfoModel item)
    {
        BreadListItemsWithControlled.FirstOrDefault(i => i.IsSelected).IsSelected = false;
        item.IsSelected = true;
    }
    private void HandleOnItemClick_Customized(PageInfoModel item)
    {
        BreadListItemsWithCustomized.FirstOrDefault(i => i.IsSelected).IsSelected = false;
        item.IsSelected = true;
    }

    private void AddItem()
    {
        ItemsCount++;
        BreadListItemsWithCustomized.Add(new PageInfoModel
        {
            Name = $"Item {ItemsCount}"
        });
    }
    private void RemoveItem()
    {
        if (BreadListItemsWithCustomized.Count > 1)
        {
            ItemsCount--;

            var item = BreadListItemsWithCustomized[^1];
            BreadListItemsWithCustomized.Remove(item);

            if (item.IsSelected)
            {
                BreadListItemsWithCustomized[^1].IsSelected = true;
            }
        }
    }


    private readonly string example1HTMLCode = @"
<div>
    <BitLabel>Field parameter</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  IsSelectedField=""@nameof(PageInfoModel.IsSelected)"" />
</div>
<div>
    <BitLabel>Selector parameter</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextFieldSelector=""item => item.Name""
                  HrefFieldSelector=""item => item.Href""
                  IsSelectedFieldSelector=""item => item.IsSelected"" />
</div>
<div>
    <BitLabel>Disabled</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextFieldSelector=""item => item.Name""
                  HrefFieldSelector=""item => item.Href""
                  IsSelectedFieldSelector=""item => item.IsSelected""
                  IsEnabled=""false"" />
</div>
<div>
    <BitLabel>Item Disabled</BitLabel>
    <BitBreadList Items=""BasicBreadListItemsDisabled""
                  TextFieldSelector=""item => item.Name""
                  HrefFieldSelector=""item => item.Href""
                  IsEnabledFieldSelector=""item => item.IsEnabled""
                  IsSelectedFieldSelector=""item => item.IsSelected"" />
</div>
";
    private readonly string example1CSharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }
    public string Href { get; set; }
    public bool IsSelected { get; set; }
    public string HtmlClass { get; set; }
    public string HtmlStyle { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<PageInfoModel> BasicBreadListItems = new()
{
    new() { Name = ""Item 1"", Href = ""/components/bread-list"" },
    new() { Name = ""Item 2"", Href = ""/components/bread-list"" },
    new() { Name = ""Item 3"", Href = ""/components/bread-list"" },
    new() { Name = ""Item 4"", Href = ""/components/bread-list"", IsSelected = true },
};

private List<PageInfoModel> BasicBreadListItemsDisabled = new()
{
    new() { Name = ""Item 1"", Href = ""/components/bread-list"", IsEnabled = false },
    new() { Name = ""Item 2"", Href = ""/components/bread-list"", IsEnabled = false },
    new() { Name = ""Item 3"", Href = ""/components/bread-list"" },
    new() { Name = ""Item 4"", Href = ""/components/bread-list"", IsSelected = true },
};
";

    private readonly string example2HTMLCode = @"
<div>
    <BitLabel>MaxDisplayedItems (1)</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  IsSelectedField=""@nameof(PageInfoModel.IsSelected)""
                  MaxDisplayedItems=""1"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (2)</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  IsSelectedField=""@nameof(PageInfoModel.IsSelected)""
                  MaxDisplayedItems=""2"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3)</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  IsSelectedField=""@nameof(PageInfoModel.IsSelected)""
                  MaxDisplayedItems=""3"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (0)</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  IsSelectedField=""@nameof(PageInfoModel.IsSelected)""
                  MaxDisplayedItems=""3""
                  OverflowIndex=""0"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (1)</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  IsSelectedField=""@nameof(PageInfoModel.IsSelected)""
                  MaxDisplayedItems=""3""
                  OverflowIndex=""1"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (2)</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  IsSelectedField=""@nameof(PageInfoModel.IsSelected)""
                  MaxDisplayedItems=""3""
                  OverflowIndex=""2"" />
</div>
";
    private readonly string example2CSharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }
    public string Href { get; set; }
    public bool IsSelected { get; set; }
    public string HtmlClass { get; set; }
    public string HtmlStyle { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<PageInfoModel> BasicBreadListItems = new()
{
    new PageInfoModel { Name = ""Item 1"", Href = ""/components/bread-list"" },
    new PageInfoModel { Name = ""Item 2"", Href = ""/components/bread-list"" },
    new PageInfoModel { Name = ""Item 3"", Href = ""/components/bread-list"" },
    new PageInfoModel { Name = ""Item 4"", Href = ""/components/bread-list"", IsSelected = true },
};
";

    private readonly string example3HTMLCode = @"
<div>
    <BitLabel>BitIconName (ChevronDown)</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  IsSelectedField=""@nameof(PageInfoModel.IsSelected)""
                  MaxDisplayedItems=""3""
                  OverflowIndex=""2""
                  OverflowIcon=""BitIconName.ChevronDown"" />
</div>
<div>
    <BitLabel>BitIconName (CollapseMenu)</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  IsSelectedField=""@nameof(PageInfoModel.IsSelected)""
                  MaxDisplayedItems=""3""
                  OverflowIndex=""2""
                  OverflowIcon=""BitIconName.CollapseMenu"" />
</div>
";

    private readonly string example4HTMLCode = @"
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
    <BitBreadList Items=""BreadListItemsWithClass""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  IsSelectedField=""@nameof(PageInfoModel.IsSelected)""
                  ClassField=""@nameof(PageInfoModel.HtmlClass)"" />
</div>
<div>
    <BitLabel>Items Style</BitLabel>
    <BitBreadList Items=""BreadListItemsWithStyle""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  IsSelectedField=""@nameof(PageInfoModel.IsSelected)""
                  StyleField=""@nameof(PageInfoModel.HtmlStyle)"" />
</div>
<div>
    <BitLabel>Selected Item Class</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  IsSelectedField=""@nameof(PageInfoModel.IsSelected)""
                  SelectedItemClass=""custom-selected-item"" />
</div>
<div>
    <BitLabel>Selected Item Style</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  IsSelectedField=""@nameof(PageInfoModel.IsSelected)""
                  SelectedItemStyle=""color:red; background:lightgreen;"" />
</div>
";
    private readonly string example4CSharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }
    public string Href { get; set; }
    public bool IsSelected { get; set; }
    public string HtmlClass { get; set; }
    public string HtmlStyle { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<PageInfoModel> BreadListItemsWithClass = new()
{
    new PageInfoModel { Name = ""Item 1"", Href = ""/components/bread-list"", HtmlClass = ""custom-item"" },
    new PageInfoModel { Name = ""Item 2"", Href = ""/components/bread-list"", HtmlClass = ""custom-item"" },
    new PageInfoModel { Name = ""Item 3"", Href = ""/components/bread-list"", HtmlClass = ""custom-item"" },
    new PageInfoModel { Name = ""Item 4"", Href = ""/components/bread-list"", HtmlClass = ""custom-item"", IsSelected = true },
};

private List<PageInfoModel> BreadListItemsWithStyle = new()
{
    new PageInfoModel { Name = ""Item 1"", Href = ""/components/bread-list"", HtmlStyle = ""color:red; background:greenyellow;"" },
    new PageInfoModel { Name = ""Item 2"", Href = ""/components/bread-list"", HtmlStyle = ""color:red; background:greenyellow;"" },
    new PageInfoModel { Name = ""Item 3"", Href = ""/components/bread-list"", HtmlStyle = ""color:red; background:greenyellow;"" },
    new PageInfoModel { Name = ""Item 4"", Href = ""/components/bread-list"", HtmlStyle = ""color:red; background:greenyellow;"", IsSelected = true },
};

private List<PageInfoModel> BasicBreadListItems = new()
{
    new PageInfoModel { Name = ""Item 1"", Href = ""/components/bread-list"" },
    new PageInfoModel { Name = ""Item 2"", Href = ""/components/bread-list"" },
    new PageInfoModel { Name = ""Item 3"", Href = ""/components/bread-list"" },
    new PageInfoModel { Name = ""Item 4"", Href = ""/components/bread-list"", IsSelected = true },
};
";

    private readonly string example5HTMLCode = @"
<BitBreadList Items=""BreadListItemsWithControlled""
              TextField=""@nameof(PageInfoModel.Name)""
              IsSelectedField=""@nameof(PageInfoModel.IsSelected)""
              MaxDisplayedItems=""3""
              OverflowIndex=""2""
              OnItemClick=""(PageInfoModel item) => HandleOnItemClick(item)""
              SelectedItemStyle=""color:red; background:lightgreen;"" />
";
    private readonly string example5CSharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }
    public string Href { get; set; }
    public bool IsSelected { get; set; }
    public string HtmlClass { get; set; }
    public string HtmlStyle { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<PageInfoModel> BreadListItemsWithControlled = new()
{
    new PageInfoModel { Name = ""Item 1"" },
    new PageInfoModel { Name = ""Item 2"" },
    new PageInfoModel { Name = ""Item 3"" },
    new PageInfoModel { Name = ""Item 4"" },
    new PageInfoModel { Name = ""Item 5"" },
    new PageInfoModel { Name = ""Item 6"", IsSelected = true },
};

private void HandleOnItemClick(PageInfoModel item)
{
    BreadListItemsWithControlled.FirstOrDefault(i => i.IsSelected).IsSelected = false;
    item.IsSelected = true;
}
";

    private readonly string example6HTMLCode = @"
<div>
    <BitBreadList Items=""BreadListItemsWithCustomized""
                  TextField=""@nameof(PageInfoModel.Name)""
                  IsSelectedField=""@nameof(PageInfoModel.IsSelected)""
                  MaxDisplayedItems=""@MaxDisplayedItems""
                  OverflowIndex=""@OverflowIndex""
                  OnItemClick=""(PageInfoModel item) => HandleOnItemClick(item)"" />
</div>

<div class=""operators"">
    <div>
        <BitButton OnClick=""AddItem"">Add Item</BitButton>
        <BitButton OnClick=""RemoveItem"">Remove Item</BitButton>
    </div>
    <div>
        <BitNumericTextField @bind-Value=""MaxDisplayedItems"" Step=""@NumericTextFieldStep"" Label=""MaxDisplayedItems"" ShowArrows=""true"" />
        <BitNumericTextField @bind-Value=""OverflowIndex"" Step=""@NumericTextFieldStep"" Label=""OverflowIndex"" ShowArrows=""true"" />
    </div>
</div>
";
    private readonly string example6CSharpCode = @"
private List<PageInfoModel> BreadListItemsWithCustomized = new()
{
    new PageInfoModel { Name = ""Item 1"" },
    new PageInfoModel { Name = ""Item 2"" },
    new PageInfoModel { Name = ""Item 3"" },
    new PageInfoModel { Name = ""Item 4"", IsSelected = true }
};

private uint ItemsCount = 4;
private uint MaxDisplayedItems = 3;
private uint OverflowIndex = 2;
private uint NumericTextFieldStep = 1;

private void AddItem()
{
    ItemsCount++;
    BreadListItemsWithCustomized.Add(new PageInfoModel
    {
        Name = $""Item {ItemsCount}""
    });
}

private void RemoveItem()
{
    if (BreadListItemsWithCustomized.Count > 1)
    {
        ItemsCount--;

        var item = BreadListItemsWithCustomized[^1];
        BreadListItemsWithCustomized.Remove(item);

        if (item.IsSelected)
        {
            BreadListItemsWithCustomized[^1].IsSelected = true;
        }
    }
}

private void HandleOnItemClick(PageInfoModel item)
{
    BreadListItemsWithCustomized.FirstOrDefault(i => i.IsSelected).IsSelected = false;
    item.IsSelected = true;
}
";
}
