using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;
using System.Collections.Generic;
using System.Linq;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.BreadList;

public partial class BitBreadListDemo
{
    private List<PageInfoModel> BasicBreadListItems = new()
    {
        new PageInfoModel { Name = "Item 1", Href = "/components/bread-list" },
        new PageInfoModel { Name = "Item 2", Href = "/components/bread-list" },
        new PageInfoModel { Name = "Item 3", Href = "/components/bread-list" },
        new PageInfoModel { Name = "Item 4", Href = "/components/bread-list", IsSelected = true },
    };

    private List<PageInfoModel> BasicBreadListItemsDisabled = new()
    {
        new PageInfoModel { Name = "Item 1", Href = "/components/bread-list", IsEnabled = false },
        new PageInfoModel { Name = "Item 2", Href = "/components/bread-list", IsEnabled = false },
        new PageInfoModel { Name = "Item 3", Href = "/components/bread-list" },
        new PageInfoModel { Name = "Item 4", Href = "/components/bread-list", IsSelected = true },
    };

    private List<PageInfoModel> BreadListItemsWithClass = new()
    {
        new PageInfoModel { Name = "Item 1", Href = "/components/bread-list", HtmlClass = "custom-item" },
        new PageInfoModel { Name = "Item 2", Href = "/components/bread-list", HtmlClass = "custom-item" },
        new PageInfoModel { Name = "Item 3", Href = "/components/bread-list", HtmlClass = "custom-item" },
        new PageInfoModel { Name = "Item 4", Href = "/components/bread-list", HtmlClass = "custom-item", IsSelected = true },
    };

    private List<PageInfoModel> BreadListItemsWithStyle = new()
    {
        new PageInfoModel { Name = "Item 1", Href = "/components/bread-list", HtmlStyle = "color:red; background:greenyellow;" },
        new PageInfoModel { Name = "Item 2", Href = "/components/bread-list", HtmlStyle = "color:red; background:greenyellow;" },
        new PageInfoModel { Name = "Item 3", Href = "/components/bread-list", HtmlStyle = "color:red; background:greenyellow;" },
        new PageInfoModel { Name = "Item 4", Href = "/components/bread-list", HtmlStyle = "color:red; background:greenyellow;", IsSelected = true },
    };

    private List<PageInfoModel> BreadListItemsWithControll = new()
    {
        new PageInfoModel { Name = "Item 1" },
        new PageInfoModel { Name = "Item 2" },
        new PageInfoModel { Name = "Item 3" },
        new PageInfoModel { Name = "Item 4" },
        new PageInfoModel { Name = "Item 5" },
        new PageInfoModel { Name = "Item 6", IsSelected = true },
    };

    private void HandleOnItemClick(PageInfoModel item)
    {
        BreadListItemsWithControll.FirstOrDefault(i => i.IsSelected).IsSelected = false;
        BreadListItemsWithControll.FirstOrDefault(i => i == item).IsSelected = true;
    }

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "CurrentItemClass",
            Type = "string?",
            Description = "The class HTML attribute for Current Item."
        },
        new()
        {
            Name = "CurrentItemStyle",
            Type = "string?",
            Description = "The style HTML attribute for Current Item."
        },
        new()
        {
            Name = "DividerIcon",
            Type = "BitIconName",
            DefaultValue = "BitIconName.ChevronRight",
            Description = "Render a custom divider in place of the default chevron >"
        },
        new()
        {
            Name = "HrefField",
            Type = "string",
            DefaultValue = "Href",
            Description = "URL to navigate to when this breadcrumb item is clicked. If provided, the breadcrumb will be rendered as a link."
        },
        new()
        {
            Name = "HrefSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "URL to navigate to when this breadcrumb item is clicked. If provided, the breadcrumb will be rendered as a link."
        },
        new()
        {
            Name = "Items",
            Type = "IList<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "Collection of breadcrumbs to render."
        },
        new()
        {
            Name = "ClassField",
            Type = "string",
            DefaultValue = "ItemClass",
            Description = "Class HTML attribute for breadcrumb item."
        },
        new()
        {
            Name = "ClassSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Class HTML attribute for breadcrumb item."
        },
        new()
        {
            Name = "StyleField",
            Type = "string",
            DefaultValue = "ItemStyle",
            Description = "Style HTML attribute for breadcrumb item."
        },
        new()
        {
            Name = "StyleSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Style HTML attribute for breadcrumb item."
        },
        new()
        {
            Name = "MaxDisplayedItems",
            Type = "int",
            Description = "The maximum number of breadcrumbs to display before coalescing. If not specified, all breadcrumbs will be rendered."
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
            Description = "Callback for when the breadcrumb item clicked."
        },
        new()
        {
            Name = "TextField",
            Type = "string",
            DefaultValue = "Text",
            Description = "Text to display in the breadcrumb item."
        },
        new()
        {
            Name = "TextSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Text to display in the breadcrumb item."
        },
    };

    private readonly string BasicItemsCSharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }

    public string Href { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsSelected { get; set; }

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

    #region Sample Code 1

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
                  TextSelector=""item => item.Name""
                  HrefSelector=""item => item.Href""
                  IsSelectedSelector=""item => item.IsSelected"" />
</div>
<div>
    <BitLabel>Disabled</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextSelector=""item => item.Name""
                  HrefSelector=""item => item.Href""
                  IsSelectedSelector=""item => item.IsSelected""
                  IsEnabled=""false"" />
</div>
<div>
    <BitLabel>Item Disabled</BitLabel>
    <BitBreadList Items=""BasicBreadListItemsDisabled""
                    TextSelector=""item => item.Name""
                    HrefSelector=""item => item.Href""
                    IsEnabledSelector=""item => item.IsEnabled""
                    IsSelectedSelector=""item => item.IsSelected"" />
</div>
";

    private readonly string example1CSharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }

    public string Href { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsSelected { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private List<PageInfoModel> BasicBreadListItems = new()
{
    new PageInfoModel { Name = ""Item 1"", Href = ""/components/bread-list"" },
    new PageInfoModel { Name = ""Item 2"", Href = ""/components/bread-list"" },
    new PageInfoModel { Name = ""Item 3"", Href = ""/components/bread-list"" },
    new PageInfoModel { Name = ""Item 4"", Href = ""/components/bread-list"", IsSelected = true },
};

private List<PageInfoModel> BasicBreadListItemsDisabled = new()
{
    new PageInfoModel { Name = ""Item 1"", Href = ""/components/bread-list"", IsEnabled = false },
    new PageInfoModel { Name = ""Item 2"", Href = ""/components/bread-list"", IsEnabled = false },
    new PageInfoModel { Name = ""Item 3"", Href = ""/components/bread-list"" },
    new PageInfoModel { Name = ""Item 4"", Href = ""/components/bread-list"", IsSelected = true },
};
";

    #endregion

    #region Sample Code 2

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

    #endregion

    #region Sample Code 3

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

    #endregion

    #region Sample Code 4

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

    .custom-current-item {
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
    <BitLabel>Current Item Class</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  IsSelectedField=""@nameof(PageInfoModel.IsSelected)""
                  CurrentItemClass=""custom-current-item"" />
</div>
<div>
    <BitLabel>Current Item Style</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  IsSelectedField=""@nameof(PageInfoModel.IsSelected)""
                  CurrentItemStyle=""color:red; background:lightgreen;"" />
</div>
";

    private readonly string example4CSharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }

    public string Href { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsSelected { get; set; }

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

    #endregion

    #region Sample Code 5

    private readonly string example5HTMLCode = @"
<BitBreadList Items=""BreadListItemsWithControll""
              TextField=""@nameof(PageInfoModel.Name)""
              IsSelectedField=""@nameof(PageInfoModel.IsSelected)""
              MaxDisplayedItems=""3""
              OverflowIndex=""2""
              OnItemClick=""(PageInfoModel item) => HandleOnItemClick(item)""
              CurrentItemStyle=""color:red; background:lightgreen;"" />
";

    private readonly string example5CSharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }

    public string Href { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsSelected { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private List<PageInfoModel> BreadListItemsWithControll = new()
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
    BreadListItemsWithControll.FirstOrDefault(i => i.IsSelected).IsSelected = false;
    BreadListItemsWithControll.FirstOrDefault(i => i == item).IsSelected = true;
}
";

    #endregion
}
