using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
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
                href = "/components/breadcrumb"
            },
            new()
            {
                Text = "Folder 2",
                href = "/components/breadcrumb"
            },
            new()
            {
                Text = "Folder 3",
                href = "/components/breadcrumb"
            },
            new()
            {
                Text = "Folder 4",
                href = "/components/breadcrumb"
            }
        };

        BreadcrumbItemsWithStyle = new List<BitBreadcrumbItem>()
        {
            new()
            {
                Text = "Folder 1",
                href = "/components/breadcrumb",
                Style = "background-color: lightblue; padding: 2px 5px; border-radius: 10px;"
            },
            new()
            {
                Text = "Folder 2",
                href = "/components/breadcrumb",
                Style = "background-color: lightblue; padding: 2px 5px; border-radius: 10px;"
            },
            new()
            {
                Text = "Folder 3",
                href = "/components/breadcrumb",
                Style = "background-color: lightblue; padding: 2px 5px; border-radius: 10px;"
            },
            new()
            {
                Text = "Folder 4",
                href = "/components/breadcrumb",
                Style = "background-color: lightblue; padding: 2px 5px; border-radius: 10px;"
            }
        };

        BreadcrumbItemsWithClass = new List<BitBreadcrumbItem>()
        {
            new()
            {
                Text = "Folder 1",
                href = "/components/breadcrumb",
                Class = "custom-item"
            },
            new()
            {
                Text = "Folder 2",
                href = "/components/breadcrumb",
                Class = "custom-item"
            },
            new()
            {
                Text = "Folder 3",
                href = "/components/breadcrumb",
                 Class = "custom-item"
            },
            new()
            {
                Text = "Folder 4",
                href = "/components/breadcrumb",
                Class = "custom-item"
            }
        };

        BreadcrumbItemsWithControll = new List<BitBreadcrumbItem>
        {
            new()
            {
                Text = "Folder 1",
                href = "/components/breadcrumb",
                OnClick = () => 
                {
                    ControlledCurrentItem = BreadcrumbItemsWithControll[0];
                    StateHasChanged();
                }
            },
            new()
            {
                Text = "Folder 2",
                href = "/components/breadcrumb",
                OnClick = () => ControlledCurrentItem = BreadcrumbItemsWithControll[1]
            },
            new()
            {
                Text = "Folder 3",
                href = "/components/breadcrumb",
                OnClick = () => ControlledCurrentItem = BreadcrumbItemsWithControll[2]
            },
            new()
            {
                Text = "Folder 4",
                href = "/components/breadcrumb",
                OnClick = () => ControlledCurrentItem = BreadcrumbItemsWithControll[3]
            }
        };
    }

    private List<BitBreadcrumbItem> BreadcrumbItems { get; set; }
    private List<BitBreadcrumbItem> BreadcrumbItemsWithStyle { get; set; }
    private List<BitBreadcrumbItem> BreadcrumbItemsWithClass { get; set; }
    private List<BitBreadcrumbItem> BreadcrumbItemsWithControll { get; set; }
    private BitBreadcrumbItem ControlledCurrentItem { get; set; }

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "Items",
            Type = "List<BitBreadcrumbItem>",
            DefaultValue = "new List<BitBreadcrumbItem>()",
            Description = "Collection of breadcrumbs to render"
        },
        new()
        {
            Name = "AriaLabel",
            Type = "string",
            DefaultValue = "",
            Description = "Aria label for the root element of the breadcrumb (which is a navigation landmark)."
        },
        new()
        {
            Name = "Class",
            Type = "string",
            DefaultValue = "",
            Description = "Custom CSS class for the root element of the component."
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
            Name = "MaxDisplayedItems",
            Type = "int",
            DefaultValue = "",
            Description = "The maximum number of breadcrumbs to display before coalescing. If not specified, all breadcrumbs will be rendered."
        },
        new()
        {
            Name = "OnRenderOverflowIcon",
            Type = "BitIconName",
            DefaultValue = "BitIconName.More",
            Description = "Render a custom overflow icon in place of the default icon."
        },
        new()
        {
            Name = "OverflowAriaLabel",
            Type = "string?",
            DefaultValue = "",
            Description = "Aria label for the overflow button."
        },
        new()
        {
            Name = "OverflowIndex",
            Type = "int",
            DefaultValue = "",
            Description = "Optional index where overflow items will be collapsed."
        },
        new()
        {
            Name = "IsEnabled",
            Type = "bool",
            DefaultValue = "",
            Description = "Whether or not the component is enabled."
        },
        new()
        {
            Name = "Visibility",
            Type = "BitComponentVisibility",
            LinkType = LinkType.Link,
            Href = "#component-visibility-enum",
            DefaultValue = "BitComponentVisibility.Visible",
            Description = "Whether the component is Visible,Hidden,Collapsed."
        },
        new()
        {
            Name = "HtmlAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "",
            Description = "Capture and render additional attributes in addition to the component's parameters."
        },
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
            Name = "CurrentItemKey",
            Type = "string?",
            Description = "by default, the current item is the last item. But it can also be specified manually."
        },
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "component-visibility-enum",
            Title = "BitComponentVisibility Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new ()
                {
                    Name= "Visible",
                    Description="Show content of the component.",
                    Value="0"
                },
                new ()
                {
                    Name= "Hidden",
                    Description="Hide content of the component,though the space it takes on the page remains.",
                    Value="1"
                },
                new ()
                {
                    Name= "Collapsed",
                    Description="Hide content of the component,though the space it takes on the page gone.",
                    Value="2"
                }
            }
        }
    };

    private readonly string example1HTMLCode = @"

";

    private readonly string example1CSharpCode = @"
";

    private readonly string example2HTMLCode = @"

";

    private readonly string example3HTMLCode = @"
";

    private readonly string example4HTMLCode = @"

";

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

    private readonly string example6HTMLCode = @"

";
    private readonly string example6CSharpCode = @"

";

}
