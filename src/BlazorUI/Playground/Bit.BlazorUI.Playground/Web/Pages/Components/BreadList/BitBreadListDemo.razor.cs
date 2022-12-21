using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;
using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.BreadList;

public partial class BitBreadListDemo
{
    private List<PageInfoModel> BasicBreadListItems = new()
    {
        new PageInfoModel { Name = "Item 1", Href = "/components/bread-list" },
        new PageInfoModel { Name = "Item 2", Href = "/components/bread-list" },
        new PageInfoModel { Name = "Item 3", Href = "/components/bread-list" },
        new PageInfoModel { Name = "Item 4", Href = "/components/bread-list" },
    };

    private List<PageInfoModel> BreadListItemsWithClass = new()
    {
        new PageInfoModel { Name = "Item 1", Href = "/components/bread-list", HtmlClass = "custom-item" },
        new PageInfoModel { Name = "Item 2", Href = "/components/bread-list", HtmlClass = "custom-item" },
        new PageInfoModel { Name = "Item 3", Href = "/components/bread-list", HtmlClass = "custom-item" },
        new PageInfoModel { Name = "Item 4", Href = "/components/bread-list", HtmlClass = "custom-item" },
    };

    private List<PageInfoModel> BreadListItemsWithStyle = new()
    {
        new PageInfoModel { Name = "Item 1", Href = "/components/bread-list", HtmlStyle = "background-color: #CC6; padding: 2px 5px; margin: 2px 5px; border-radius: 5px; color: green;" },
        new PageInfoModel { Name = "Item 2", Href = "/components/bread-list", HtmlStyle = "background-color: #CC6; padding: 2px 5px; margin: 2px 5px; border-radius: 5px; color: green;" },
        new PageInfoModel { Name = "Item 3", Href = "/components/bread-list", HtmlStyle = "background-color: #CC6; padding: 2px 5px; margin: 2px 5px; border-radius: 5px; color: green;" },
        new PageInfoModel { Name = "Item 4", Href = "/components/bread-list", HtmlStyle = "background-color: #CC6; padding: 2px 5px; margin: 2px 5px; border-radius: 5px; color: green;" },
    };

    private List<PageInfoModel> BreadListItemsWithControll = new()
    {
        new PageInfoModel { Name = "Item 1" },
        new PageInfoModel { Name = "Item 2" },
        new PageInfoModel { Name = "Item 3" },
        new PageInfoModel { Name = "Item 4" },
    };

    private PageInfoModel ControlledCurrentItem;

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
            Name = "CurrentItem",
            Type = "TItem?",
            Description = "by default, the current item is the last item. But it can also be specified manually."
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
            Name = "ItemClassField",
            Type = "string",
            DefaultValue = "ItemClass",
            Description = "Class HTML attribute for breadcrumb item."
        },
        new()
        {
            Name = "ItemClassSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Class HTML attribute for breadcrumb item."
        },
        new()
        {
            Name = "ItemStyleField",
            Type = "string",
            DefaultValue = "ItemStyle",
            Description = "Style HTML attribute for breadcrumb item."
        },
        new()
        {
            Name = "ItemStyleSelector",
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
}

private List<PageInfoModel> BasicBreadListItems = new()
{
    new PageInfoModel { Name = ""Item 1"", Href = ""/components/bread-list"" },
    new PageInfoModel { Name = ""Item 2"", Href = ""/components/bread-list"" },
    new PageInfoModel { Name = ""Item 3"", Href = ""/components/bread-list"" },
    new PageInfoModel { Name = ""Item 4"", Href = ""/components/bread-list"" },
};
";

    #region Sample Code 1

    private readonly string example1HTMLCode = @"
<div>
    <BitLabel>Field parameter</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)"" />
</div>
<div>
    <BitLabel>Selector parameter</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextSelector=""item => item.Name""
                  HrefSelector=""item => item.Href"" />
</div>
<div>
    <BitLabel>Disabled</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextSelector=""item => item.Name""
                  HrefSelector=""item => item.Href""
                  IsEnabled=""false"" />
</div>
";

    #endregion

    #region Sample Code 2

    private readonly string example2HTMLCode = @"
<div>
    <BitLabel>MaxDisplayedItems (1)</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  MaxDisplayedItems=""1"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (2)</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  MaxDisplayedItems=""2"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3)</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  MaxDisplayedItems=""3"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (0)</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  MaxDisplayedItems=""3""
                  OverflowIndex=""0"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (1)</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  MaxDisplayedItems=""3""
                  OverflowIndex=""1"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (2)</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
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
                  MaxDisplayedItems=""3""
                  OverflowIndex=""2""
                  OverflowIcon=""BitIconName.ChevronDown"" />
</div>

<div>
    <BitLabel>BitIconName (CollapseMenu)</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
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
        background-color: #CC6;
        padding: 2px 5px !important;
        margin: 2px 5px;
        border-radius: 5px;
        color: red !important;
    }

    .custom-current-item {
        background-color: #CC6;
        padding: 2px 5px !important;
        margin: 0 5px;
        border-radius: 5px;
        color: blue !important;
    }
</style>

<div>
    <BitLabel>Items Class</BitLabel>
    <BitBreadList Items=""BreadListItemsWithClass""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  ItemClassField=""@nameof(PageInfoModel.HtmlClass)"" />
</div>
<div>
    <BitLabel>Items Style</BitLabel>
    <BitBreadList Items=""BreadListItemsWithStyle""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  ItemStyleField=""@nameof(PageInfoModel.HtmlStyle)"" />
</div>
<div>
    <BitLabel>Current Item Class</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  CurrentItemClass=""custom-current-item""
                  CurrentItem=""BasicBreadListItems[3]"" />
</div>
<div>
    <BitLabel>Current Item Style</BitLabel>
    <BitBreadList Items=""BasicBreadListItems""
                  TextField=""@nameof(PageInfoModel.Name)""
                  HrefField=""@nameof(PageInfoModel.Href)""
                  CurrentItemStyle=""background-color: #CC6; padding: 2px 5px; margin: 0 5px; border-radius: 5px; color: blue;""
                  CurrentItem=""BasicBreadListItems[3]"" />
</div>
";

    private readonly string example4CSharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }

    public string Href { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }
}

private List<PageInfoModel> BreadListItemsWithClass = new()
{
    new PageInfoModel { Name = ""Item 1"", Href = ""/components/bread-list"", HtmlClass = ""custom-item"" },
    new PageInfoModel { Name = ""Item 2"", Href = ""/components/bread-list"", HtmlClass = ""custom-item"" },
    new PageInfoModel { Name = ""Item 3"", Href = ""/components/bread-list"", HtmlClass = ""custom-item"" },
    new PageInfoModel { Name = ""Item 4"", Href = ""/components/bread-list"", HtmlClass = ""custom-item"" },
};

private List<PageInfoModel> BreadListItemsWithStyle = new()
{
    new PageInfoModel { Name = ""Item 1"", Href = ""/components/bread-list"", HtmlStyle = ""background-color: #CC6; padding: 2px 5px; margin: 2px 5px; border-radius: 5px; color: green;"" },
    new PageInfoModel { Name = ""Item 2"", Href = ""/components/bread-list"", HtmlStyle = ""background-color: #CC6; padding: 2px 5px; margin: 2px 5px; border-radius: 5px; color: green;"" },
    new PageInfoModel { Name = ""Item 3"", Href = ""/components/bread-list"", HtmlStyle = ""background-color: #CC6; padding: 2px 5px; margin: 2px 5px; border-radius: 5px; color: green;"" },
    new PageInfoModel { Name = ""Item 4"", Href = ""/components/bread-list"", HtmlStyle = ""background-color: #CC6; padding: 2px 5px; margin: 2px 5px; border-radius: 5px; color: green;"" },
};

private List<PageInfoModel> BasicBreadListItems = new()
{
    new PageInfoModel { Name = ""Item 1"", Href = ""/components/bread-list"" },
    new PageInfoModel { Name = ""Item 2"", Href = ""/components/bread-list"" },
    new PageInfoModel { Name = ""Item 3"", Href = ""/components/bread-list"" },
    new PageInfoModel { Name = ""Item 4"", Href = ""/components/bread-list"" },
};
";

    #endregion

    #region Sample Code 5

    private readonly string example5HTMLCode = @"
<BitBreadList Items=""BreadListItemsWithControll""
              TextField=""@nameof(PageInfoModel.Name)""
              MaxDisplayedItems=""3""
              OverflowIndex=""2""
              CurrentItem=""@ControlledCurrentItem""
              OnItemClick=""(PageInfoModel item) => ControlledCurrentItem = item""
              CurrentItemStyle=""background-color: #CC6; padding: 0 5px; border-radius: 5px; color: red;"" />
";

    private readonly string example5CSharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }

    public string Href { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }
}

private List<PageInfoModel> BreadListItemsWithControll = new()
{
    new PageInfoModel { Name = ""Item 1"" },
    new PageInfoModel { Name = ""Item 2"" },
    new PageInfoModel { Name = ""Item 3"" },
    new PageInfoModel { Name = ""Item 4"" },
};

 private PageInfoModel ControlledCurrentItem;
";

    #endregion
}
