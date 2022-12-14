using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Breadcrumb;

public partial class BitBreadcrumbDemo
{
    public BitBreadcrumbDemo()
    {
        BreadcrumbItems = new List<BitBreadcrumbItem>
        {
            new()
            {
                Text = "Folder 1",
                Href = "/components/breadcrumb"
            },
            new()
            {
                Text = "Folder 2",
                Href = "/components/breadcrumb"
            },
            new()
            {
                Text = "Folder 3",
                Href = "/components/breadcrumb"
            },
            new()
            {
                Text = "Folder 4",
                Href = "/components/breadcrumb"
            }
        };

        BreadcrumbItemsWithClass = new List<BitBreadcrumbItem>
        {
            new()
            {
                Text = "Folder 1",
                Href = "/components/breadcrumb",
                Class = "custom-item"
            },
            new()
            {
                Text = "Folder 2",
                Href = "/components/breadcrumb",
                Class = "custom-item"
            },
            new()
            {
                Text = "Folder 3",
                Href = "/components/breadcrumb",
                Class = "custom-item"
            },
            new()
            {
                Text = "Folder 4",
                Href = "/components/breadcrumb",
                Class = "custom-item"
            }
        };

        BreadcrumbItemsWithStyle = new List<BitBreadcrumbItem>
        {
            new()
            {
                Text = "Folder 1",
                Href = "/components/breadcrumb",
                Style = "background-color: #CC6; padding: 2px 5px; margin: 2px 5px; border-radius: 5px; color: green;"
            },
            new()
            {
                Text = "Folder 2",
                Href = "/components/breadcrumb",
                Style = "background-color: #CC6; padding: 2px 5px; margin: 2px 5px; border-radius: 5px; color: green;"
            },
            new()
            {
                Text = "Folder 3",
                Href = "/components/breadcrumb",
                Style = "background-color: #CC6; padding: 2px 5px; margin: 2px 5px; border-radius: 5px; color: green;"
            },
            new()
            {
                Text = "Folder 4",
                Href = "/components/breadcrumb",
                Style = "background-color: #CC6; padding: 2px 5px; margin: 2px 5px; border-radius: 5px; color: green;"
            }
        };

        BreadcrumbItemsWithControll = new List<BitBreadcrumbItem>
        {
            new()
            {
                Text = "Folder 1"
            },
            new()
            {
                Text = "Folder 2"
            },
            new()
            {
                Text = "Folder 3"
            },
            new()
            {
                Text = "Folder 4"
            }
        };
    }

    private List<BitBreadcrumbItem> BreadcrumbItems { get; set; }
    private List<BitBreadcrumbItem> BreadcrumbItemsWithClass { get; set; }
    private List<BitBreadcrumbItem> BreadcrumbItemsWithStyle { get; set; }
    private List<BitBreadcrumbItem> BreadcrumbItemsWithControll { get; set; }

    private BitBreadcrumbItem ControlledCurrentItem;

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
            Type = "BitBreadcrumbItem?",
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
            Name = "Items",
            Type = "IList<BitBreadcrumbItem>",
            DefaultValue = "new List<BitBreadcrumbItem>()",
            Description = "Collection of breadcrumbs to render"
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
            Name = "OnRenderOverflowIcon",
            Type = "BitIconName",
            DefaultValue= "BitIconName.More",
            Description = "Render a custom overflow icon in place of the default icon."
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<BitBreadcrumbItem>",
            Description = "Callback for when the breadcrumb item clicked."
        },
    };

    private readonly string BasicItemsCSharpCode = @"
private List<BitBreadcrumbItem> BreadcrumbItems { get; set; } = new List<BitBreadcrumbItem>
{
    new()
    {
        Text = ""Folder 1"",
        Href = ""/components/breadcrumb""
    },
    new()
    {
        Text = ""Folder 2"",
        Href = ""/components/breadcrumb""
    },
    new()
    {
        Text = ""Folder 3"",
        Href = ""/components/breadcrumb""
    },
    new()
    {
        Text = ""Folder 4"",
        Href = ""/components/breadcrumb""
    }
};
";

    #region Sample Code 1

    private readonly string example1HTMLCode = @"
<div>
    <BitLabel>Basic</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems"" />
</div>

<div>
    <BitLabel>Disabled</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems"" IsEnabled=""false"" />
</div>
";

    #endregion

    #region Sample Code 2

    private readonly string example2HTMLCode = @"
<div>
    <BitLabel>MaxDisplayedItems</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""1"" />
    <BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""2"" />
    <BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""3"" />
</div>

<div>
    <BitLabel>OverflowIndex</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""3"" OverflowIndex=""0"" />
    <BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""3"" OverflowIndex=""1"" />
    <BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""3"" OverflowIndex=""2"" />
</div>
";

    #endregion

    #region Sample Code 3

    private readonly string example3HTMLCode = @"
<BitBreadcrumb Items=""BreadcrumbItems""
               MaxDisplayedItems=""3""
               OverflowIndex=""2""
               OnRenderOverflowIcon=""BitIconName.ChevronDown"" />

<BitBreadcrumb Items=""BreadcrumbItems""
               MaxDisplayedItems=""3""
               OverflowIndex=""2""
               OnRenderOverflowIcon=""BitIconName.CollapseMenu"" />
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
    <BitLabel>Items Class & Style</BitLabel>

    <BitBreadcrumb Items=""BreadcrumbItemsWithClass"" />
    <BitBreadcrumb Items=""BreadcrumbItemsWithStyle"" />
</div>

<div>
    <BitLabel>Change Current Item Class & Style</BitLabel>

    <BitBreadcrumb Items=""BreadcrumbItems""
                   CurrentItemClass=""custom-current-item""
                   CurrentItem=""BreadcrumbItems[3]"" />

    <BitBreadcrumb Items=""BreadcrumbItems""
                   CurrentItemStyle=""background-color: #CC6; padding: 2px 5px; margin: 0 5px; border-radius: 5px; color: blue;""
                   CurrentItem=""BreadcrumbItems[3]"" />
</div>
";

    private readonly string example4CSharpCode = @"
private List<BitBreadcrumbItem> BreadcrumbItems { get; set; } = new List<BitBreadcrumbItem>
{
    new()
    {
        Text = ""Folder 1"",
        Href = ""/components/breadcrumb""
    },
    new()
    {
        Text = ""Folder 2"",
        Href = ""/components/breadcrumb""
    },
    new()
    {
        Text = ""Folder 3"",
        Href = ""/components/breadcrumb""
    },
    new()
    {
        Text = ""Folder 4"",
        Href = ""/components/breadcrumb""
    }
};

private List<BitBreadcrumbItem> BreadcrumbItemsWithClass { get; set; } = new List<BitBreadcrumbItem>
{
    new()
    {
        Text = ""Folder 1"",
        Href = ""/components/breadcrumb"",
        Class = ""custom-item""
    },
    new()
    {
        Text = ""Folder 2"",
        Href = ""/components/breadcrumb"",
        Class = ""custom-item""
    },
    new()
    {
        Text = ""Folder 3"",
        Href = ""/components/breadcrumb"",
        Class = ""custom-item""
    },
    new()
    {
        Text = ""Folder 4"",
        Href = ""/components/breadcrumb"",
        Class = ""custom-item""
    }
};

private List<BitBreadcrumbItem> BreadcrumbItemsWithStyle { get; set; } = new List<BitBreadcrumbItem>
{
    new()
    {
        Text = ""Folder 1"",
        Href = ""/components/breadcrumb"",
        Style = ""background-color: #CC6; padding: 2px 5px; margin: 2px 5px; border-radius: 5px; color: green;""
    },
    new()
    {
        Text = ""Folder 2"",
        Href = ""/components/breadcrumb"",
        Style = ""background-color: #CC6; padding: 2px 5px; margin: 2px 5px; border-radius: 5px; color: green;""
    },
    new()
    {
        Text = ""Folder 3"",
        Href = ""/components/breadcrumb"",
        Style = ""background-color: #CC6; padding: 2px 5px; margin: 2px 5px; border-radius: 5px; color: green;""
    },
    new()
    {
        Text = ""Folder 4"",
        Href = ""/components/breadcrumb"",
        Style = ""background-color: #CC6; padding: 2px 5px; margin: 2px 5px; border-radius: 5px; color: green;""
    }
};
";

    #endregion

    #region Sample Code 5

    private readonly string example5HTMLCode = @"
<BitBreadcrumb Items=""@BreadcrumbItemsWithControll""
               MaxDisplayedItems=""3""
               OverflowIndex=""2""
               CurrentItem=""@ControlledCurrentItem""
               OnItemClick=""(item) => ControlledCurrentItem = item""
               CurrentItemStyle=""background-color: #CC6; padding: 2px 5px; border-radius: 5px; color: red;"" />
";

    private readonly string example5CSharpCode = @"
private List<BitBreadcrumbItem> BreadcrumbItemsWithControll { get; set; } = new List<BitBreadcrumbItem>
{
    new()
    {
        Text = ""Folder 1""
    },
    new()
    {
        Text = ""Folder 2""
    },
    new()
    {
        Text = ""Folder 3""
    },
    new()
    {
        Text = ""Folder 4""
    }
};

private BitBreadcrumbItem ControlledCurrentItem;
";

    #endregion
}
