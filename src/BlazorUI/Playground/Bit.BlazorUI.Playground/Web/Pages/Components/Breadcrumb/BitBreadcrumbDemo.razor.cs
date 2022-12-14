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

        BreadcrumbItemsWithStyle = new List<BitBreadcrumbItem>
        {
            new()
            {
                Text = "Folder 1",
                Href = "/components/breadcrumb",
                Style = "background-color: lightblue; padding: 2px 5px; border-radius: 10px;"
            },
            new()
            {
                Text = "Folder 2",
                Href = "/components/breadcrumb",
                Style = "background-color: lightblue; padding: 2px 5px; border-radius: 10px;"
            },
            new()
            {
                Text = "Folder 3",
                Href = "/components/breadcrumb",
                Style = "background-color: lightblue; padding: 2px 5px; border-radius: 10px;"
            },
            new()
            {
                Text = "Folder 4",
                Href = "/components/breadcrumb",
                Style = "background-color: lightblue; padding: 2px 5px; border-radius: 10px;"
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
    private List<BitBreadcrumbItem> BreadcrumbItemsWithStyle { get; set; }
    private List<BitBreadcrumbItem> BreadcrumbItemsWithClass { get; set; }
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

    #region Sample Code 1

    private readonly string example1HTMLCode = @"

";

    private readonly string example1CSharpCode = @"

";

    #endregion

    #region Sample Code 2

    private readonly string example2HTMLCode = @"

";

    #endregion

    #region Sample Code 3

    private readonly string example3HTMLCode = @"

";

    #endregion

    #region Sample Code 4

    private readonly string example4HTMLCode = @"

";

    #endregion

    #region Sample Code 5

    private readonly string example5HTMLCode = @"
<style>
    .custom-item {
        background-color: lightgreen;
        padding: rem(2px) rem(5px);
        border-radius: rem(10px);
    }

    .custom-current-item {
        color: deeppink;
    }
</style>
";

    private readonly string example5CSharpCode = @"

";

    #endregion

    #region Sample Code 6

    private readonly string example6HTMLCode = @"

";

    private readonly string example6CSharpCode = @"

";

    #endregion
}
