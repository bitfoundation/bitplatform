using System.Collections.Generic;
using System.Linq;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Nav;

public partial class BitNavDemo
{
    private static readonly List<BitNavItem> BasicNavItems = new()
    {
        new BitNavItem
        {
            Text = "Home",
            Title = "Home is Parent Row",
            ExpandAriaLabel = "Expand Home section",
            CollapseAriaLabel = "Collapse Home section",
            IsExpanded = true,
            Items = new List<BitNavItem>
            {
                new BitNavItem { Text = "Activity", Url = "http://msn.com", Target="_blank" },
                new BitNavItem { Text = "MSN", Url = "http://msn.com", IsEnabled = false, Target = "_blank" }
            }
        },
        new BitNavItem { Text = "Documents", Url = "http://msn.com", Target = "_blank", IsExpanded = true },
        new BitNavItem { Text = "Pages", Url = "http://msn.com", Target = "_parent" },
        new BitNavItem { Text = "Notebook", Url = "http://msn.com", Target = "_blank", IsEnabled = false },
        new BitNavItem { Text = "Communication and Media", Url = "http://msn.com", Target = "_top" },
        new BitNavItem { Text = "News", Url = "http://msn.com", Target = "_self", IconName = BitIconName.News },
    };

    private static readonly List<BitNavItem> GroupedNavItems = new()
    {
        new BitNavItem
        {
            Text = "Basic Inputs",
            CollapseAriaLabel = "Collapsed Basic Inputs section",
            ExpandAriaLabel = "Expanded Basic Inputs section",
            IsExpanded = true,
            Items = new List<BitNavItem>
            {
                new BitNavItem { Text= "Bottons", Url = "components/button", Target = "_blank" },
                new BitNavItem { Text= "DropDown", Url = "components/drop-down", Target = "_blank" },
                new BitNavItem { Text= "FileUpload", Url = "components/file-upload", Target = "_blank" }
            }
        },
        new BitNavItem
        {
            Text = "Items & Lists",
            CollapseAriaLabel = "Collapsed Items & Lists section",
            ExpandAriaLabel = "Expanded Items & Lists section",
            IsExpanded = true,
            Items = new List<BitNavItem>
            {
                new BitNavItem { Text = "BasicList", Url ="components/basic-list", Target = "_blank" },
                new BitNavItem { Text = "DataGrid", Url ="components/data-grid", Target = "_blank" },
                new BitNavItem { Text = "Carousel", Url ="components/carousel", Target = "_blank" }
            }
        },
        new BitNavItem
        {
            Text = "Galleries & Pickers",
            CollapseAriaLabel = "Collapsed Galleries & Pickers section",
            ExpandAriaLabel = "Expanded Galleries & Pickers section",
            IsExpanded = true,
            Items = new List<BitNavItem>
            {
                new BitNavItem { Text = "ColorPicker", Url = "components/color-picker", Target = "_blank" },
                new BitNavItem { Text = "DatePicker", Url = "components/date-picker", Target = "_blank" },
                new BitNavItem { Text = "Chart", Url = "components/chart", Target = "_blank" }
            }
        }
    };

    private static readonly List<BitNavItem> ManualNavItems = new()
    {
        new BitNavItem
        {
            Text = "Home",
            Title = "Home is Parent Row",
            ExpandAriaLabel = "Expand Home section",
            CollapseAriaLabel = "Collapse Home section",
            Items = new List<BitNavItem>
            {
                new BitNavItem { Text = "Activity", },
                new BitNavItem { Text = "MSN", IsEnabled = false }
            }
        },
        new BitNavItem { Text = "Documents" },
        new BitNavItem { Text = "Pages" },
        new BitNavItem { Text = "Notebook", IsEnabled = false },
        new BitNavItem { Text = "Communication and Media" },
        new BitNavItem { Text = "News", IconName = BitIconName.News },
    };

    private static readonly List<BitDropDownItem> DropDownItems = new()
    {
        new BitDropDownItem
        {
            Text = "Activity",
            Value = "Activity",
        },
        new BitDropDownItem
        {
            Text = "MSN",
            Value = "MSN",
        },
        new BitDropDownItem
        {
            Text = "Documents",
            Value = "Documents",
        },
        new BitDropDownItem
        {
            Text = "Pages",
            Value = "Pages",
        },
        new BitDropDownItem
        {
            Text = "Notebook",
            Value = "Notebook",
        },
        new BitDropDownItem
        {
            Text = "Communication and Media",
            Value = "Communication and Media",
        },
        new BitDropDownItem
        {
            Text = "News",
            Value = "News",
        },
    };
    private static List<BitNavItem> Flatten(IList<BitNavItem> e) => e.SelectMany(c => Flatten(c.Items)).Concat(e).ToList();
    private BitNavItem SelectedItemNav = ManualNavItems[0].Items[0];
    private string SelectedItemText = ManualNavItems[0].Items[0].Text;

    private BitNavItem ClickedItem;
    private BitNavItem ToggledItem;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter
        {
            Name = "DefaultSelectedItem",
            Type = "BitNavItem?",
            Description = "The initially selected item in manual mode.",
            Href = "#nav-item",
            LinkType = LinkType.Link,
        },
        new ComponentParameter
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment<BitNavItem>?",
            Description = "Used to customize how content inside the group header is rendered.",
        },
        new ComponentParameter
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitNavItem>?",
            Description = "Used to customize how content inside the link tag is rendered.",
        },
        new ComponentParameter
        {
            Name = "Items",
            Type = "IList<BitNavItem>",
            DefaultValue = "new List<BitNavItem>()",
            Description = "A collection of link items to display in the navigation bar.",
        },
        new ComponentParameter
        {
            Name = "Mode",
            Type = "BitNavMode",
            DefaultValue = "BitNavMode.Automatic",
            Description = "Determines how the navigation will be handled. The default value is Automatic.",
            Href = "#nav-mode-enum",
            LinkType = LinkType.Link
        },
        new ComponentParameter
        {
            Name = "OnItemClick",
            Type = "EventCallback<BitNavItem>",
            Description = "Callback invoked when an item is clicked.",
        },
        new ComponentParameter
        {
            Name = "OnSelectItem",
            Type = "EventCallback<BitNavItem>",
            Description = "Callback invoked when an item is selected.",
        },
        new ComponentParameter
        {
            Name = "OnItemToggle",
            Type = "EventCallback<BitNavItem>",
            Description = "Callback invoked when a group header is clicked and Expanded or Collapse.",
        },
        new ComponentParameter
        {
            Name = "RenderType",
            Type = "BitNavRenderType",
            DefaultValue = "BitNavRenderType.Normal",
            Description = "The way to render nav links.",
            Href = "#nav-render-type-enum",
            LinkType = LinkType.Link,
        },
        new ComponentParameter
        {
            Name = "SelectedItem",
            Type = "BitNavItem?",
            Description = "Selected item to show in Nav.",
        },
    };
    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new ComponentSubParameter()
        {
            Id = "nav-item",
            Title = "BitNavItem",
            Parameters = new List<ComponentParameter>()
            {
               new ComponentParameter()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   Description = "Aria label for nav link. Ignored if collapseAriaLabel or expandAriaLabel is provided",
               },
               new ComponentParameter()
               {
                   Name = "AriaCurrent",
                   Type = "BitNavLinkItemAriaCurrent",
                   DefaultValue = "BitNavItemAriaCurrent.Page",
                   Description = "Aria-current token for active nav links. Must be a valid token value, and defaults to 'page'",
                   Href = "#nav-item-aria-current-enum",
                   LinkType = LinkType.Link,
               },
               new ComponentParameter()
               {
                   Name = "CollapseAriaLabel",
                   Type = "string?",
                   Description = "Aria label when items is collapsed and can be expanded.",
               },
               new ComponentParameter()
               {
                   Name = "ExpandAriaLabel",
                   Type = "string?",
                   Description = "Aria label when group is collapsed and can be expanded.",
               },
               new ComponentParameter()
               {
                   Name = "ForceAnchor",
                   Type = "bool",
                   Description = "(Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)",
               },
               new ComponentParameter()
               {
                   Name = "Items",
                   Type = "IList<BitNavItem>",
                   DefaultValue = "new List<BitNavItem>()",
                   Description = "A list of items to render as children of the current item.",
               },
               new ComponentParameter()
               {
                   Name = "IconName",
                   Type = "BitIconName",
                   Description = "Name of an icon to render next to this link button.",
               },
               new ComponentParameter()
               {
                   Name = "IsExpanded",
                   Type = "bool",
                   Description = "Whether or not the link is in an expanded state.",
               },
               new ComponentParameter()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   Description = "Whether or not the link is enabled.",
               },
               new ComponentParameter()
               {
                   Name = "Style",
                   Type = "string?",
                   Description = "Custom style for the each item element.",
               },
               new ComponentParameter()
               {
                   Name = "Text",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "Text to render for this link.",
               },
               new ComponentParameter()
               {
                   Name = "Title",
                   Type = "string?",
                   Description = "Text for title tooltip.",
               },
               new ComponentParameter()
               {
                   Name = "Target",
                   Type = "string?",
                   Description = "Link target, specifies how to open the link.",
               },
               new ComponentParameter()
               {
                   Name = "Url",
                   Type = "string?",
                   Description = "URL to navigate to for this link.",
               }
            }
        }
    };
    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "nav-mode-enum",
            Title = "BitNavMode Enum",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name = "Automatic",
                    Description = "The value of selected key will change using NavigationManager and the current url inside the component.",
                    Value = "0",
                },
                new EnumItem()
                {
                    Name = "Manual",
                    Description = "Selected key changes will be sent back to the parent component and the component won't change its value.",
                    Value = "1",
                }
            }
        },
        new EnumParameter()
        {
            Id = "nav-render-type-enum",
            Title = "BitNavRenderType Enum",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name = "Normal",
                    Value = "0",
                },
                new EnumItem()
                {
                    Name = "Grouped",
                    Value = "1",
                }
            }
        },
        new EnumParameter()
        {
            Id = "nav-item-aria-current-enum",
            Title = "BitNavItemAriaCurrent Enum",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name = "Page",
                    Value = "0",
                },
                new EnumItem()
                {
                    Name = "Step",
                    Value = "1",
                },
                new EnumItem()
                {
                    Name = "Location",
                    Value = "2",
                },
                new EnumItem()
                {
                    Name = "Date",
                    Value = "3",
                },
                new EnumItem()
                {
                    Name = "Time",
                    Value = "4",
                },
                new EnumItem()
                {
                    Name = "True",
                    Value = "5",
                },

            }
        },
    };

    #region Sample Code 1

    private static string example1HTMLCode = @"
<BitNav Items=""BasicNavItems"" />
";

    private static string example1CSharpCode = @"
private static readonly List<BitNavItem> BasicNavItems = new()
{
    new BitNavItem
    {
        Text = ""Home"",
        Title = ""Home is Parent Row"",
        ExpandAriaLabel = ""Expand Home section"",
        CollapseAriaLabel = ""Collapse Home section"",
        IsExpanded = true,
        Items = new List<BitNavItem>
        {
            new BitNavItem { Text = ""Activity"", Url = ""http://msn.com"", Target=""_blank"" },
            new BitNavItem { Text = ""MSN"", Url = ""http://msn.com"", IsEnabled = false, Target = ""_blank"" }
        }
    },
    new BitNavItem { Text = ""Documents"", Url = ""http://msn.com"", Target = ""_blank"", IsExpanded = true },
    new BitNavItem { Text = ""Pages"", Url = ""http://msn.com"", Target = ""_parent"" },
    new BitNavItem { Text = ""Notebook"", Url = ""http://msn.com"", Target = ""_blank"", IsEnabled = false },
    new BitNavItem { Text = ""Communication and Media"", Url = ""http://msn.com"", Target = ""_top"" },
    new BitNavItem { Text = ""News"", Url = ""http://msn.com"", Target = ""_self"", IconName = BitIconName.News },
};
";

    #endregion

    #region Sample Code 2

    private static string example2HTMLCode = @"
<BitNav Items=""GroupedNavItems"" RenderType=""BitNavRenderType.Grouped"" />
";

    private static string example2CSharpCode = @"
private static readonly List<BitNavItem> GroupedNavItems = new()
{
    new BitNavItem
    {
        Text = ""Basic Inputs"",
        CollapseAriaLabel = ""Collapsed Basic Inputs section"",
        ExpandAriaLabel = ""Expanded Basic Inputs section"",
        IsExpanded = true,
        Items = new List<BitNavItem>
        {
            new BitNavItem { Text= ""Bottons"", Url = ""components/button"", Target = ""_blank"" },
            new BitNavItem { Text= ""DropDown"", Url = ""components/drop-down"", Target = ""_blank"" },
            new BitNavItem { Text= ""FileUpload"", Url = ""components/file-upload"", Target = ""_blank"" }
        }
    },
    new BitNavItem
    {
        Text = ""Items & Lists"",
        CollapseAriaLabel = ""Collapsed Items & Lists section"",
        ExpandAriaLabel = ""Expanded Items & Lists section"",
        IsExpanded = true,
        Items = new List<BitNavItem>
        {
            new BitNavItem { Text = ""BasicList"", Url =""components/basic-list"", Target = ""_blank"" },
            new BitNavItem { Text = ""DataGrid"", Url =""components/data-grid"", Target = ""_blank"" },
            new BitNavItem { Text = ""Carousel"", Url =""components/carousel"", Target = ""_blank"" }
        }
    },
    new BitNavItem
    {
        Text = ""Galleries & Pickers"",
        CollapseAriaLabel = ""Collapsed Galleries & Pickers section"",
        ExpandAriaLabel = ""Expanded Galleries & Pickers section"",
        IsExpanded = true,
        Items = new List<BitNavItem>
        {
            new BitNavItem { Text = ""ColorPicker"", Url = ""components/color-picker"", Target = ""_blank"" },
            new BitNavItem { Text = ""DatePicker"", Url = ""components/date-picker"", Target = ""_blank"" },
            new BitNavItem { Text = ""Chart"", Url = ""components/chart"", Target = ""_blank"" }
        }
    }
};
";

    #endregion

    #region Sample Code 3

    private static string example3HTMLCode = @"
<div>
    <BitLabel>Basic</BitLabel>
    <BitNav Items=""ManualNavItems""
            DefaultSelectedItem=""ManualNavItems[0].Items[0]""
            Mode=""BitNavMode.Manual"" />
</div>

<div class=""margin-top"">
    <BitLabel>Two-Way Bind</BitLabel>

    <BitNav @bind-SelectedItem=""SelectedItemNav""
            Items=""ManualNavItems""
            Mode=""BitNavMode.Manual""
            OnSelectItem=""(item) => SelectedItemText = DropDownItems.FirstOrDefault(i => i.Text == item.Text).Text"" />

    <BitDropDown @bind-Value=""SelectedItemText""
                    Label=""Select Item""
                    Items=""DropDownItems""
                    OnSelectItem=""(item) => SelectedItemNav = Flatten(ManualNavItems).FirstOrDefault(i => i.Text == item.Value)"" />
</div>
";

    private static string example3CSharpCode = @"
private static readonly List<BitNavItem> ManualNavItems = new()
{
    new BitNavItem
    {
        Text = ""Home"",
        Title = ""Home is Parent Row"",
        ExpandAriaLabel = ""Expand Home section"",
        CollapseAriaLabel = ""Collapse Home section"",
        Items = new List<BitNavItem>
        {
            new BitNavItem { Text = ""Activity"", },
            new BitNavItem { Text = ""MSN"", IsEnabled = false }
        }
    },
    new BitNavItem { Text = ""Documents"" },
    new BitNavItem { Text = ""Pages"" },
    new BitNavItem { Text = ""Notebook"", IsEnabled = false },
    new BitNavItem { Text = ""Communication and Media"" },
    new BitNavItem { Text = ""News"", IconName = BitIconName.News },
};

private static readonly List<BitDropDownItem> DropDownItems = new()
{
    new BitDropDownItem
    {
        Text = ""Activity"",
        Value = ""Activity"",
    },
    new BitDropDownItem
    {
        Text = ""MSN"",
        Value = ""MSN"",
    },
    new BitDropDownItem
    {
        Text = ""Documents"",
        Value = ""Documents"",
    },
    new BitDropDownItem
    {
        Text = ""Pages"",
        Value = ""Pages"",
    },
    new BitDropDownItem
    {
        Text = ""Notebook"",
        Value = ""Notebook"",
    },
    new BitDropDownItem
    {
        Text = ""Communication and Media"",
        Value = ""Communication and Media"",
    },
    new BitDropDownItem
    {
        Text = ""News"",
        Value = ""News"",
    },
};
private static List<BitNavItem> Flatten(IList<BitNavItem> e) => e.SelectMany(c => Flatten(c.Items)).Concat(e).ToList();
private BitNavItem SelectedItemNav = ManualNavItems[0].Items[0];
private string SelectedItemText = ManualNavItems[0].Items[0].Text;
";

    #endregion

    #region Sample Code 4

    private static string example4HTMLCode = @"
<style>
    .nav-custom-header {
        color: green;
    }

    .nav-custom-item {
        color: orange;
        font-weight: 600;

        &.disabled-item {
            color: #ffa50066;
        }
    }
</style>

<div>
    <BitLabel>Header Template (in Grouped mode)</BitLabel>
    <BitNav Items=""GroupedNavItems"" RenderType=""BitNavRenderType.Grouped"">
        <HeaderTemplate Context=""item"">
            <div class=""nav-custom-header"">
                <BitIcon IconName=""BitIconName.FavoriteStarFill"" />
                <span>@item.Text</span>
            </div>
        </HeaderTemplate>
    </BitNav>
</div>

<div class=""margin-top"">
    <BitLabel>Item Template</BitLabel>
    <BitNav Items=""BasicNavItems"">
        <ItemTemplate Context=""item"">
            <a href=""@item.Url"" target=""@item.Target"" class=""nav-custom-item @(item.IsEnabled is false ? ""disabled-item"" : """")"">
                @if (item.IconName.HasValue)
                {
                    <BitIcon IconName=""@item.IconName.Value"" />
                }
                <span>@item.Text</span>
            </a>
        </ItemTemplate>
    </BitNav>
</div>
";

    private static string example4CSharpCode = @"
private static readonly List<BitNavItem> BasicNavItems = new()
{
    new BitNavItem
    {
        Text = ""Home"",
        Title = ""Home is Parent Row"",
        ExpandAriaLabel = ""Expand Home section"",
        CollapseAriaLabel = ""Collapse Home section"",
        IsExpanded = true,
        Items = new List<BitNavItem>
        {
            new BitNavItem { Text = ""Activity"", Url = ""http://msn.com"", Target=""_blank"" },
            new BitNavItem { Text = ""MSN"", Url = ""http://msn.com"", IsEnabled = false, Target = ""_blank"" }
        }
    },
    new BitNavItem { Text = ""Documents"", Url = ""http://msn.com"", Target = ""_blank"", IsExpanded = true },
    new BitNavItem { Text = ""Pages"", Url = ""http://msn.com"", Target = ""_parent"" },
    new BitNavItem { Text = ""Notebook"", Url = ""http://msn.com"", Target = ""_blank"", IsEnabled = false },
    new BitNavItem { Text = ""Communication and Media"", Url = ""http://msn.com"", Target = ""_top"" },
    new BitNavItem { Text = ""News"", Url = ""http://msn.com"", Target = ""_self"", IconName = BitIconName.News },
};

private static readonly List<BitNavItem> GroupedNavItems = new()
{
    new BitNavItem
    {
        Text = ""Basic Inputs"",
        CollapseAriaLabel = ""Collapsed Basic Inputs section"",
        ExpandAriaLabel = ""Expanded Basic Inputs section"",
        IsExpanded = true,
        Items = new List<BitNavItem>
        {
            new BitNavItem { Text= ""Bottons"", Url = ""components/button"", Target = ""_blank"" },
            new BitNavItem { Text= ""DropDown"", Url = ""components/drop-down"", Target = ""_blank"" },
            new BitNavItem { Text= ""FileUpload"", Url = ""components/file-upload"", Target = ""_blank"" }
        }
    },
    new BitNavItem
    {
        Text = ""Items & Lists"",
        CollapseAriaLabel = ""Collapsed Items & Lists section"",
        ExpandAriaLabel = ""Expanded Items & Lists section"",
        IsExpanded = true,
        Items = new List<BitNavItem>
        {
            new BitNavItem { Text = ""BasicList"", Url =""components/basic-list"", Target = ""_blank"" },
            new BitNavItem { Text = ""DataGrid"", Url =""components/data-grid"", Target = ""_blank"" },
            new BitNavItem { Text = ""Carousel"", Url =""components/carousel"", Target = ""_blank"" }
        }
    },
    new BitNavItem
    {
        Text = ""Galleries & Pickers"",
        CollapseAriaLabel = ""Collapsed Galleries & Pickers section"",
        ExpandAriaLabel = ""Expanded Galleries & Pickers section"",
        IsExpanded = true,
        Items = new List<BitNavItem>
        {
            new BitNavItem { Text = ""ColorPicker"", Url = ""components/color-picker"", Target = ""_blank"" },
            new BitNavItem { Text = ""DatePicker"", Url = ""components/date-picker"", Target = ""_blank"" },
            new BitNavItem { Text = ""Chart"", Url = ""components/chart"", Target = ""_blank"" }
        }
    }
};
";

    #endregion

    #region Sample Code 5

    private static string example5HTMLCode = @"
                <BitNav Items=""ManualNavItems""
                        DefaultSelectedItem=""ManualNavItems[0].Items[0]""
                        Mode=""BitNavMode.Manual""
                        OnItemClick=""(item) => ClickedItem = item""
                        OnItemToggle=""(item) => ToggledItem = item"" />
                <span>Clicked Item: @ClickedItem?.Text</span>
                <span>Toggled Item: @ToggledItem?.Text</span>
                <span>IsExpanded Value: @ToggledItem?.IsExpanded</span>
";

    private static string example5CSharpCode = @"
private static readonly List<BitNavItem> ManualNavItems = new()
{
    new BitNavItem
    {
        Text = ""Home"",
        Title = ""Home is Parent Row"",
        ExpandAriaLabel = ""Expand Home section"",
        CollapseAriaLabel = ""Collapse Home section"",
        Items = new List<BitNavItem>
        {
            new BitNavItem { Text = ""Activity"", },
            new BitNavItem { Text = ""MSN"", IsEnabled = false }
        }
    },
    new BitNavItem { Text = ""Documents"" },
    new BitNavItem { Text = ""Pages"" },
    new BitNavItem { Text = ""Notebook"", IsEnabled = false },
    new BitNavItem { Text = ""Communication and Media"" },
    new BitNavItem { Text = ""News"", IconName = BitIconName.News },
};

private BitNavItem ClickedItem;
private BitNavItem ToggledItem;
";

    #endregion
}
