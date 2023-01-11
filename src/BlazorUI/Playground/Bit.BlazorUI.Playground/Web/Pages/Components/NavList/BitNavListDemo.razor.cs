using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.NavList;

public partial class BitNavListDemo
{
    private readonly List<NavMenuModel> BasicNavItems= new()
    {
        new NavMenuModel
        {
            Name = "Home",
            TitleAttribute = "Home is Parent Row",
            Url = "http://example.com",
            ExpandAriaLabel = "Expand Home section",
            CollapseAriaLabel = "Collapse Home section",
            IsExpanded = true,
            Items = new List<NavMenuModel>
            {
                new NavMenuModel { Name = "Activity", Url = "http://msn.com", Key = "key1", Target="_blank" },
                new NavMenuModel { Name = "MSN", Url = "http://msn.com", Key = "key2", IsEnabled = false, Target = "_blank" }
            }
        },
        new NavMenuModel { Name = "Documents", Url = "http://msn.com", Key = "key3", Target = "_blank", IsExpanded = true },
        new NavMenuModel { Name = "Pages", Url = "http://msn.com", Key = "key4", Target = "_parent" },
        new NavMenuModel { Name = "Notebook", Url = "http://msn.com", Key = "key5", Target = "_blank", IsEnabled = false },
        new NavMenuModel { Name = "Communication and Media", Url = "http://msn.com", Key = "key6", Target = "_top" },
        new NavMenuModel { Name = "News", Url = "http://msn.com", Key = "key7", Target = "_self", IconName = BitIconName.News },
    };

    private readonly List<NavMenuModel> GroupedNavItems = new()
    {
        new NavMenuModel
        {
            Name = "Basic Inputs",
            CollapseAriaLabel = "Collapsed Basic Inputs section",
            ExpandAriaLabel = "Expanded Basic Inputs section",
            IsExpanded = true,
            Items = new List<NavMenuModel>
            {
                new NavMenuModel { Name= "Bottons", Key = "Bottons", Url = "components/button", Target = "_blank" },
                new NavMenuModel { Name= "DropDown", Key = "DropDown", Url = "components/drop-down", Target = "_blank" },
                new NavMenuModel { Name= "FileUpload", Key = "FileUpload", Url = "components/file-upload", Target = "_blank" }
            }
        },
        new NavMenuModel
        {
            Name = "Items & Lists",
            CollapseAriaLabel = "Collapsed Items & Lists section",
            ExpandAriaLabel = "Expanded Items & Lists section",
            IsExpanded = true,
            Items = new List<NavMenuModel>
            {
                new NavMenuModel { Name = "BasicList", Key = "BasicList", Url ="components/basic-list", Target = "_blank" },
                new NavMenuModel { Name = "DataGrid", Key = "DataGrid", Url ="components/data-grid", Target = "_blank" },
                new NavMenuModel { Name = "Carousel", Key = "Carousel", Url ="components/carousel", Target = "_blank" }
            }
        },
        new NavMenuModel
        {
            Name = "Galleries & Pickers",
            CollapseAriaLabel = "Collapsed Galleries & Pickers section",
            ExpandAriaLabel = "Expanded Galleries & Pickers section",
            IsExpanded = true,
            Items = new List<NavMenuModel>
            {
                new NavMenuModel { Name = "ColorPicker", Key = "ColorPicker", Url = "components/color-picker", Target = "_blank" },
                new NavMenuModel { Name = "DatePicker", Key = "DatePicker", Url = "components/date-picker", Target = "_blank" },
                new NavMenuModel { Name = "Chart", Key = "Chart", Url = "components/chart", Target = "_blank" }
            }
        }
    };

    private readonly List<NavMenuModel> ManualNavItems = new()
    {
        new NavMenuModel
        {
            Name = "Home",
            TitleAttribute = "Home is Parent Row",
            ExpandAriaLabel = "Expand Home section",
            CollapseAriaLabel = "Collapse Home section",
            IsExpanded= true,
            Items = new List<NavMenuModel>
            {
                new NavMenuModel { Name = "Activity", Key = "key1", },
                new NavMenuModel { Name = "MSN", Key = "key2", IsEnabled = false }
            }
        },
        new NavMenuModel { Name = "Documents", Key = "key3" },
        new NavMenuModel { Name = "Pages", Key = "key4" },
        new NavMenuModel { Name = "Notebook", Key = "key5", IsEnabled = false },
        new NavMenuModel { Name = "Communication and Media", Key = "key6" },
        new NavMenuModel { Name = "News", Key = "key7", IconName = BitIconName.News },
    };

    private string ManualSelectedKey = "key1";

    private NavMenuModel ClickedItem;
    private NavMenuModel ExpandedItem;
    private void HandleOnItemExpand(NavMenuModel item)
    {
        ExpandedItem = item;
        ExpandedItem.IsExpanded = !ExpandedItem.IsExpanded;
    }

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AriaCurrentField",
            Type = "string",
            DefaultValue = "AriaCurrent",
            Description = "Aria-current token for active nav item. Must be a valid token value, and defaults to 'page'."
        },
        new ComponentParameter()
        {
            Name = "AriaCurrentFieldSelector",
            Type = "Expression<Func<TItem, BitNavListItemAriaCurrent>>?",
            Href = "nav-list-item-aria-current",
            LinkType = LinkType.Link,
            Description = "Aria-current token for active nav item. Must be a valid token value, and defaults to 'page'."
        },
        new ComponentParameter()
        {
            Name = "AriaLabelField",
            Type = "string",
            DefaultValue = "AriaLabel",
            Description = "Aria label for the item. Ignored if collapseAriaLabel or expandAriaLabel is provided."
        },
        new ComponentParameter()
        {
            Name = "AriaLabelFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Aria label for the item. Ignored if collapseAriaLabel or expandAriaLabel is provided."
        },
        new ComponentParameter()
        {
            Name = "CollapseAriaLabelField",
            Type = "string",
            DefaultValue = "CollapseAriaLabel",
            Description = "Aria label when group is collapsed."
        },
        new ComponentParameter()
        {
            Name = "CollapseAriaLabelFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Aria label when group is collapsed."
        },
        new ComponentParameter()
        {
            Name = "ExpandAriaLabelField",
            Type = "string",
            DefaultValue = "ExpandAriaLabel",
            Description = "Aria label when group is expanded."
        },
        new ComponentParameter()
        {
            Name = "ExpandAriaLabelFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Aria label when group is expanded."
        },
        new ComponentParameter()
        {
            Name = "ForceAnchorField",
            Type = "string",
            DefaultValue = "ForceAnchor",
            Description = "(Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)"
        },
        new ComponentParameter()
        {
            Name = "ForceAnchorFieldelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "(Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)"
        },
        new ComponentParameter()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment<TItem>?",
            Description = "Used to customize how content inside the group header is rendered."
        },
        new ComponentParameter()
        {
            Name = "InitialSelectedKey",
            Type = "string?",
            Description = "(Optional) The key of the nav item initially selected in manual mode."
        },
        new ComponentParameter()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            Description = "Used to customize how content inside the item is rendered."
        },
        new ComponentParameter()
        {
            Name = "Items",
            Type = "IList<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "A collection of item to display in the navigation bar."
        },
        new ComponentParameter()
        {
            Name = "ItemsField",
            Type = "string",
            DefaultValue = "Items",
            Description = "A list of items to render as children of the current item."
        },
        new ComponentParameter()
        {
            Name = "ItemsFieldSelector",
            Type = "Expression<Func<TItem, IList<TItem>>>?",
            Description = "A list of items to render as children of the current item."
        },
        new ComponentParameter()
        {
            Name = "IconNameField",
            Type = "string",
            DefaultValue = "IconName",
            Description = "Name of an icon to render next to the item button."
        },
        new ComponentParameter()
        {
            Name = "IconNameFieldSelector",
            Type = "Expression<Func<TItem, BitIconName>>?",
            Description = "Name of an icon to render next to the item button."
        },
        new ComponentParameter()
        {
            Name = "IsExpandedField",
            Type = "string",
            DefaultValue = "IsExpanded",
            Description = "Whether or not the group is in an expanded state."
        },
        new ComponentParameter()
        {
            Name = "IsExpandedFieldSelector",
            Type = "Expression<Func<TItem, bool>>?",
            Description = "Whether or not the group is in an expanded state."
        },
        new ComponentParameter()
        {
            Name = "IsEnabledField",
            Type = "string",
            DefaultValue = "IsEnabled",
            Description = "Whether or not the item is disabled."
        },
        new ComponentParameter()
        {
            Name = "IsEnabledFieldSelector",
            Type = "Expression<Func<TItem, bool>>?",
            Description = "Whether or not the item is disabled."
        },
        new ComponentParameter()
        {
            Name = "KeyField",
            Type = "string",
            DefaultValue = "Key",
            Description = "A unique value to use as a key or id of the item, used when rendering the list of item and for tracking the currently selected item."
        },
        new ComponentParameter()
        {
            Name = "KeyFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "A unique value to use as a key or id of the item, used when rendering the list of item and for tracking the currently selected item."
        },
        new ComponentParameter()
        {
            Name = "Mode",
            Type = "BitNavListMode",
            DefaultValue = "BitNavListMode.Automatic",
            Href = "nav-list-mode-enum",
            LinkType = LinkType.Link,
            Description = "Determines how the navigation will be handled."
        },
        new ComponentParameter()
        {
            Name = "NameField",
            Type = "string",
            DefaultValue = "Name",
            Description = "Text to render for the item."
        },
        new ComponentParameter()
        {
            Name = "NameFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Text to render for the item."
        },
        new ComponentParameter()
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            Description = "Callback invoked when an item is clicked."
        },
        new ComponentParameter()
        {
            Name = "OnItemExpand",
            Type = "EventCallback<TItem>",
            Description = "Callback invoked when a group header is clicked and Expanded."
        },
        new ComponentParameter()
        {
            Name = "RenderType",
            Type = "BitNavListRenderType",
            DefaultValue = "BitNavListRenderType.Normal",
            Href = "nav-list-render-type-enum",
            LinkType = LinkType.Link,
            Description = "The way to render nav items."
        },
        new ComponentParameter()
        {
            Name = "SelectedKey",
            Type = "string?",
            Description = "The key of the nav item selected by caller."
        },
        new ComponentParameter()
        {
            Name = "StyleField",
            Type = "string",
            DefaultValue = "Style",
            Description = "Custom style for the each item element."
        },
        new ComponentParameter()
        {
            Name = "StyleFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Custom style for the each item element."
        },
        new ComponentParameter()
        {
            Name = "TitleField",
            Type = "string",
            DefaultValue= "Title",
            Description = "Text for the item tooltip."
        },
        new ComponentParameter()
        {
            Name = "TitleFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Text for the item tooltip."
        },
        new ComponentParameter()
        {
            Name = "TargetField",
            Type = "string",
            DefaultValue = "Target",
            Description = "Link target, specifies how to open the item link."
        },
        new ComponentParameter()
        {
            Name = "TargetFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Link target, specifies how to open the item link."
        },
        new ComponentParameter()
        {
            Name = "UrlField",
            Type = "string",
            DefaultValue = "Url",
            Description = "URL to navigate for the item link."
        },
        new ComponentParameter()
        {
            Name = "UrlFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "URL to navigate for the item link."
        },
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "nav-list-item-aria-current",
            Title = "BitNavListItemAriaCurrent Enum",
            EnumList = new List<EnumItem>
            {
                new EnumItem()
                {
                    Name= "Page",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Step",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Location",
                    Value="2",
                },
                new EnumItem()
                {
                    Name= "Date",
                    Value="3",
                },
                new EnumItem()
                {
                    Name= "Time",
                    Value="4",
                },
                new EnumItem()
                {
                    Name= "True",
                    Value="5",
                },
            }
        },
        new EnumParameter()
        {
            Id = "nav-list-mode-enum",
            Title = "BitNavListMode Enum",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Automatic",
                    Description="The value of selected key will change using NavigationManager and the current url inside the component.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Manual",
                    Description="Selected key changes will be sent back to the parent component and the component won't change its value.",
                    Value="1",
                }
            }
        },
        new EnumParameter()
        {
            Id = "nav-list-render-type-enum",
            Title = "BitNavListRenderType Enum",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Normal",
                    Description="",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Grouped",
                    Description="",
                    Value="1",
                }
            }
        },
    };

    #region Sample Code 1

    private static string example1HTMLCode = @"
<BitNavList Items=""BasicNavItems""
            NameField=""@nameof(NavMenuModel.Name)""
            KeyField=""@nameof(NavMenuModel.Key)""
            UrlField=""@nameof(NavMenuModel.Url)""
            TargetField=""@nameof(NavMenuModel.Target)""
            TitleField=""@nameof(NavMenuModel.TitleAttribute)""
            IsExpandedField=""@nameof(NavMenuModel.IsExpanded)""
            IconNameField=""@nameof(NavMenuModel.IconName)""
            IsEnabledField=""@nameof(NavMenuModel.IsEnabled)""
            CollapseAriaLabelField=""@nameof(NavMenuModel.CollapseAriaLabel)""
            ExpandAriaLabelField=""@nameof(NavMenuModel.ExpandAriaLabel)""
            ItemsField=""@nameof(NavMenuModel.Items)"" />
";


    private static string example1CSharpCode = @"
public class NavMenuModel
{
    public string Name { get; set; }
    public string TitleAttribute { get; set; }
    public string Key { get; set; }
    public string Url { get; set; }
    public string Target { get; set; }
    public BitIconName IconName { get; set; }
    public string ExpandAriaLabel { get; set; }
    public string CollapseAriaLabel { get; set; }
    public bool IsExpanded { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<NavMenuModel> Items { get; set; }
}

private readonly List<NavMenuModel> BasicNavItems= new()
{
    new NavMenuModel
    {
        Name = ""Home"",
        TitleAttribute = ""Home is Parent Row"",
        Url = ""http://example.com"",
        ExpandAriaLabel = ""Expand Home section"",
        CollapseAriaLabel = ""Collapse Home section"",
        IsExpanded = true,
        Items = new List<NavMenuModel>
        {
            new NavMenuModel { Name = ""Activity"", Url = ""http://msn.com"", Key = ""key1"", Target=""_blank"" },
            new NavMenuModel { Name = ""MSN"", Url = ""http://msn.com"", Key = ""key2"", IsEnabled = false, Target = ""_blank"" }
        }
    },
    new NavMenuModel { Name = ""Documents"", Url = ""http://msn.com"", Key = ""key3"", Target = ""_blank"", IsExpanded = true },
    new NavMenuModel { Name = ""Pages"", Url = ""http://msn.com"", Key = ""key4"", Target = ""_parent"" },
    new NavMenuModel { Name = ""Notebook"", Url = ""http://msn.com"", Key = ""key5"", Target = ""_blank"", IsEnabled = false },
    new NavMenuModel { Name = ""Communication and Media"", Url = ""http://msn.com"", Key = ""key6"", Target = ""_top"" },
    new NavMenuModel { Name = ""News"", Url = ""http://msn.com"", Key = ""key7"", Target = ""_self"", IconName = BitIconName.News },
};
";

    #endregion

    #region Sample Code 2

    private static string example2HTMLCode = @"
<BitNavList Items=""GroupedNavItems""
            NameFieldSelector=""item => item.Name""
            KeyFieldSelector=""item => item.Key""
            UrlFieldSelector=""item => item.Url""
            TargetFieldSelector=""item => item.Target""
            IsExpandedFieldSelector=""item => item.IsExpanded""
            CollapseAriaLabelFieldSelector=""item => item.CollapseAriaLabel""
            ExpandAriaLabelFieldSelector=""item => item.CollapseAriaLabel""
            ItemsFieldSelector=""item => item.Items""
            RenderType=""BitNavListRenderType.Grouped"" />
";

    private static string example2CSharpCode = @"
public class NavMenuModel
{
    public string Name { get; set; }
    public string TitleAttribute { get; set; }
    public string Key { get; set; }
    public string Url { get; set; }
    public string Target { get; set; }
    public BitIconName IconName { get; set; }
    public string ExpandAriaLabel { get; set; }
    public string CollapseAriaLabel { get; set; }
    public bool IsExpanded { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<NavMenuModel> Items { get; set; }
}

private readonly List<NavMenuModel> GroupedNavItems = new()
{
    new NavMenuModel
    {
        Name = ""Basic Inputs"",
        CollapseAriaLabel = ""Collapsed Basic Inputs section"",
        ExpandAriaLabel = ""Expanded Basic Inputs section"",
        IsExpanded = true,
        Items = new List<NavMenuModel>
        {
            new NavMenuModel { Name= ""Bottons"", Key = ""Bottons"", Url = ""components/button"", Target = ""_blank"" },
            new NavMenuModel { Name= ""DropDown"", Key = ""DropDown"", Url = ""components/drop-down"", Target = ""_blank"" },
            new NavMenuModel { Name= ""FileUpload"", Key = ""FileUpload"", Url = ""components/file-upload"", Target = ""_blank"" }
        }
    },
    new NavMenuModel
    {
        Name = ""Items & Lists"",
        CollapseAriaLabel = ""Collapsed Items & Lists section"",
        ExpandAriaLabel = ""Expanded Items & Lists section"",
        IsExpanded = true,
        Items = new List<NavMenuModel>
        {
            new NavMenuModel { Name = ""BasicList"", Key = ""BasicList"", Url =""components/basic-list"", Target = ""_blank"" },
            new NavMenuModel { Name = ""DataGrid"", Key = ""DataGrid"", Url =""components/data-grid"", Target = ""_blank"" },
            new NavMenuModel { Name = ""Carousel"", Key = ""Carousel"", Url =""components/carousel"", Target = ""_blank"" }
        }
    },
    new NavMenuModel
    {
        Name = ""Galleries & Pickers"",
        CollapseAriaLabel = ""Collapsed Galleries & Pickers section"",
        ExpandAriaLabel = ""Expanded Galleries & Pickers section"",
        IsExpanded = true,
        Items = new List<NavMenuModel>
        {
            new NavMenuModel { Name = ""ColorPicker"", Key = ""ColorPicker"", Url = ""components/color-picker"", Target = ""_blank"" },
            new NavMenuModel { Name = ""DatePicker"", Key = ""DatePicker"", Url = ""components/date-picker"", Target = ""_blank"" },
            new NavMenuModel { Name = ""Chart"", Key = ""Chart"", Url = ""components/chart"", Target = ""_blank"" }
        }
    }
};
";

    #endregion

    #region Sample Code 3

    private static string example3HTMLCode = @"
<div>
    <BitLabel>Basic</BitLabel>
    <BitNavList Items=""ManualNavItems""
                NameField=""@nameof(NavMenuModel.Name)""
                KeyField=""@nameof(NavMenuModel.Key)""
                TitleField=""@nameof(NavMenuModel.TitleAttribute)""
                IconNameField=""@nameof(NavMenuModel.IconName)""
                IsExpandedField=""@nameof(NavMenuModel.IsExpanded)""
                IsEnabledField=""@nameof(NavMenuModel.IsEnabled)""
                ItemsField=""@nameof(NavMenuModel.Items)""
                InitialSelectedKey=""key1""
                Mode=""BitNavListMode.Manual"" />
</div>

<div class=""margin-top"">
    <BitLabel>Two-Way Bind</BitLabel>
    <BitNavList @bind-SelectedKey=""ManualSelectedKey""
                Items=""ManualNavItems""
                NameField=""@nameof(NavMenuModel.Name)""
                KeyField=""@nameof(NavMenuModel.Key)""
                TitleField=""@nameof(NavMenuModel.TitleAttribute)""
                IconNameField=""@nameof(NavMenuModel.IconName)""
                IsExpandedField=""@nameof(NavMenuModel.IsExpanded)""
                IsEnabledField=""@nameof(NavMenuModel.IsEnabled)""
                ItemsField=""@nameof(NavMenuModel.Items)""
                Mode=""BitNavListMode.Manual"" />
    <BitTextField Label=""Enter Key"" @bind-Value=""ManualSelectedKey"" />
</div>
";


    private static string example3CSharpCode = @"
public class NavMenuModel
{
    public string Name { get; set; }
    public string TitleAttribute { get; set; }
    public string Key { get; set; }
    public string Url { get; set; }
    public string Target { get; set; }
    public BitIconName IconName { get; set; }
    public string ExpandAriaLabel { get; set; }
    public string CollapseAriaLabel { get; set; }
    public bool IsExpanded { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<NavMenuModel> Items { get; set; }
}

private readonly List<NavMenuModel> ManualNavItems = new()
{
    new NavMenuModel
    {
        Name = ""Home"",
        TitleAttribute = ""Home is Parent Row"",
        ExpandAriaLabel = ""Expand Home section"",
        CollapseAriaLabel = ""Collapse Home section"",
        IsExpanded= true,
        Items = new List<NavMenuModel>
        {
            new NavMenuModel { Name = ""Activity"", Key = ""key1"", },
            new NavMenuModel { Name = ""MSN"", Key = ""key2"", IsEnabled = false }
        }
    },
    new NavMenuModel { Name = ""Documents"", Key = ""key3"" },
    new NavMenuModel { Name = ""Pages"", Key = ""key4"" },
    new NavMenuModel { Name = ""Notebook"", Key = ""key5"", IsEnabled = false },
    new NavMenuModel { Name = ""Communication and Media"", Key = ""key6"" },
    new NavMenuModel { Name = ""News"", Key = ""key7"", IconName = BitIconName.News },
};

private string ManualSelectedKey = ""key1"";
";

    #endregion

    #region Sample Code 4

    private static string example4HTMLCode = @"
<style>
    .nav-list-custom-header {
        color: green;
    }

    .nav-list-custom-item {
        color: orange;
        font-weight: 600;

        &.disabled-item {
            color: #ffa50066;
        }
    }
</style>

<div>
    <BitLabel>Header Template (in Grouped mode)</BitLabel>
    <BitNavList Items=""GroupedNavItems""
                NameField=""@nameof(NavMenuModel.Name)""
                KeyField=""@nameof(NavMenuModel.Key)""
                UrlField=""@nameof(NavMenuModel.Url)""
                TargetField=""@nameof(NavMenuModel.Target)""
                IsExpandedField=""@nameof(NavMenuModel.IsExpanded)""
                CollapseAriaLabelField=""@nameof(NavMenuModel.CollapseAriaLabel)""
                ExpandAriaLabelField=""@nameof(NavMenuModel.ExpandAriaLabel)""
                ItemsField=""@nameof(NavMenuModel.Items)""
                RenderType=""BitNavListRenderType.Grouped"">
        <HeaderTemplate Context=""item"">
            <div class=""nav-list-custom-header"">
                <BitIcon IconName=""BitIconName.FavoriteStarFill"" />
                <span>@item.Name</span>
            </div>
        </HeaderTemplate>
    </BitNavList>
</div>

<div class=""margin-top"">
    <BitLabel>Item Template</BitLabel>
    <BitNavList Items=""BasicNavItems""
                NameField=""@nameof(NavMenuModel.Name)""
                KeyField=""@nameof(NavMenuModel.Key)""
                UrlField=""@nameof(NavMenuModel.Url)""
                TargetField=""@nameof(NavMenuModel.Target)""
                TitleField=""@nameof(NavMenuModel.TitleAttribute)""
                IsExpandedField=""@nameof(NavMenuModel.IsExpanded)""
                IconNameField=""@nameof(NavMenuModel.IconName)""
                IsEnabledField=""@nameof(NavMenuModel.IsEnabled)""
                CollapseAriaLabelField=""@nameof(NavMenuModel.CollapseAriaLabel)""
                ExpandAriaLabelField=""@nameof(NavMenuModel.ExpandAriaLabel)""
                ItemsField=""@nameof(NavMenuModel.Items)"">
        <ItemTemplate Context=""item"">
            <a href=""@item.Url"" target=""@item.Target"" class=""nav-list-custom-item @(item.IsEnabled is false ? ""disabled-item"" : """")"">
                <BitIcon IconName=""@item.IconName"" />
                <span>@item.Name</span>
            </a>
        </ItemTemplate>
    </BitNavList>
</div>
";


    private static string example4CSharpCode = @"
public class NavMenuModel
{
    public string Name { get; set; }
    public string TitleAttribute { get; set; }
    public string Key { get; set; }
    public string Url { get; set; }
    public string Target { get; set; }
    public BitIconName IconName { get; set; }
    public string ExpandAriaLabel { get; set; }
    public string CollapseAriaLabel { get; set; }
    public bool IsExpanded { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<NavMenuModel> Items { get; set; }
}

private readonly List<NavMenuModel> BasicNavItems= new()
{
    new NavMenuModel
    {
        Name = ""Home"",
        TitleAttribute = ""Home is Parent Row"",
        Url = ""http://example.com"",
        ExpandAriaLabel = ""Expand Home section"",
        CollapseAriaLabel = ""Collapse Home section"",
        IsExpanded = true,
        Items = new List<NavMenuModel>
        {
            new NavMenuModel { Name = ""Activity"", Url = ""http://msn.com"", Key = ""key1"", Target=""_blank"" },
            new NavMenuModel { Name = ""MSN"", Url = ""http://msn.com"", Key = ""key2"", IsEnabled = false, Target = ""_blank"" }
        }
    },
    new NavMenuModel { Name = ""Documents"", Url = ""http://msn.com"", Key = ""key3"", Target = ""_blank"", IsExpanded = true },
    new NavMenuModel { Name = ""Pages"", Url = ""http://msn.com"", Key = ""key4"", Target = ""_parent"" },
    new NavMenuModel { Name = ""Notebook"", Url = ""http://msn.com"", Key = ""key5"", Target = ""_blank"", IsEnabled = false },
    new NavMenuModel { Name = ""Communication and Media"", Url = ""http://msn.com"", Key = ""key6"", Target = ""_top"" },
    new NavMenuModel { Name = ""News"", Url = ""http://msn.com"", Key = ""key7"", Target = ""_self"", IconName = BitIconName.News },
};

private readonly List<NavMenuModel> GroupedNavItems = new()
{
    new NavMenuModel
    {
        Name = ""Basic Inputs"",
        CollapseAriaLabel = ""Collapsed Basic Inputs section"",
        ExpandAriaLabel = ""Expanded Basic Inputs section"",
        IsExpanded = true,
        Items = new List<NavMenuModel>
        {
            new NavMenuModel { Name= ""Bottons"", Key = ""Bottons"", Url = ""components/button"", Target = ""_blank"" },
            new NavMenuModel { Name= ""DropDown"", Key = ""DropDown"", Url = ""components/drop-down"", Target = ""_blank"" },
            new NavMenuModel { Name= ""FileUpload"", Key = ""FileUpload"", Url = ""components/file-upload"", Target = ""_blank"" }
        }
    },
    new NavMenuModel
    {
        Name = ""Items & Lists"",
        CollapseAriaLabel = ""Collapsed Items & Lists section"",
        ExpandAriaLabel = ""Expanded Items & Lists section"",
        IsExpanded = true,
        Items = new List<NavMenuModel>
        {
            new NavMenuModel { Name = ""BasicList"", Key = ""BasicList"", Url =""components/basic-list"", Target = ""_blank"" },
            new NavMenuModel { Name = ""DataGrid"", Key = ""DataGrid"", Url =""components/data-grid"", Target = ""_blank"" },
            new NavMenuModel { Name = ""Carousel"", Key = ""Carousel"", Url =""components/carousel"", Target = ""_blank"" }
        }
    },
    new NavMenuModel
    {
        Name = ""Galleries & Pickers"",
        CollapseAriaLabel = ""Collapsed Galleries & Pickers section"",
        ExpandAriaLabel = ""Expanded Galleries & Pickers section"",
        IsExpanded = true,
        Items = new List<NavMenuModel>
        {
            new NavMenuModel { Name = ""ColorPicker"", Key = ""ColorPicker"", Url = ""components/color-picker"", Target = ""_blank"" },
            new NavMenuModel { Name = ""DatePicker"", Key = ""DatePicker"", Url = ""components/date-picker"", Target = ""_blank"" },
            new NavMenuModel { Name = ""Chart"", Key = ""Chart"", Url = ""components/chart"", Target = ""_blank"" }
        }
    }
};
";

    #endregion

    #region Sample Code 5

    private static string example5HTMLCode = @"
<BitNavList Items=""ManualNavItems""
            NameFieldSelector=""item => item.Name""
            KeyFieldSelector=""item => item.Key""
            TitleFieldSelector=""item => item.TitleAttribute""
            IconNameFieldSelector=""item => item.IconName""
            IsExpandedFieldSelector=""item => item.IsExpanded""
            IsEnabledFieldSelector=""item => item.IsEnabled""
            ItemsFieldSelector=""item => item.Items""
            InitialSelectedKey=""key1""
            Mode=""BitNavListMode.Manual""
            OnItemClick=""(NavMenuModel item) => ClickedItem = item""
            OnItemExpand=""(NavMenuModel item) => HandleOnItemExpand(item)"" />

<span>Clicked Item: @ClickedItem?.Name</span>
<span>Expanded Item: @ExpandedItem?.Name</span>
<span>IsExpanded Value: @(ExpandedItem?.IsExpanded)</span>
";


    private static string example5CSharpCode = @"
public class NavMenuModel
{
    public string Name { get; set; }
    public string TitleAttribute { get; set; }
    public string Key { get; set; }
    public string Url { get; set; }
    public string Target { get; set; }
    public BitIconName IconName { get; set; }
    public string ExpandAriaLabel { get; set; }
    public string CollapseAriaLabel { get; set; }
    public bool IsExpanded { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<NavMenuModel> Items { get; set; }
}

private readonly List<NavMenuModel> ManualNavItems = new()
{
    new NavMenuModel
    {
        Name = ""Home"",
        TitleAttribute = ""Home is Parent Row"",
        ExpandAriaLabel = ""Expand Home section"",
        CollapseAriaLabel = ""Collapse Home section"",
        IsExpanded= true,
        Items = new List<NavMenuModel>
        {
            new NavMenuModel { Name = ""Activity"", Key = ""key1"", },
            new NavMenuModel { Name = ""MSN"", Key = ""key2"", IsEnabled = false }
        }
    },
    new NavMenuModel { Name = ""Documents"", Key = ""key3"" },
    new NavMenuModel { Name = ""Pages"", Key = ""key4"" },
    new NavMenuModel { Name = ""Notebook"", Key = ""key5"", IsEnabled = false },
    new NavMenuModel { Name = ""Communication and Media"", Key = ""key6"" },
    new NavMenuModel { Name = ""News"", Key = ""key7"", IconName = BitIconName.News },
};

private NavMenuModel ClickedItem;
private NavMenuModel ExpandedItem;
private void HandleOnItemExpand(NavMenuModel item)
{
    ExpandedItem = item;
    ExpandedItem.IsExpanded = !ExpandedItem.IsExpanded;
}
";

    #endregion
}
