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
<div>
    <div class=""example-desc"">With items rendered as links</div>
    <BitBreadcrumb Items=""BreadcrumbItems""></BitBreadcrumb>
</div>

<div>
    <div class=""example-desc"">With custom rendered divider and overflow icon</div>
    <BitBreadcrumb Items=""BreadcrumbItems""
                    DividerIcon=""BitIconName.Separator""
                    OverflowIndex=""1""
                    MaxDisplayedItems=""2""
                    OnRenderOverflowIcon=""BitIconName.ChevronDown""></BitBreadcrumb>
</div>

<div>
    <BitBreadcrumb Items=""BreadcrumbItems""></BitBreadcrumb>
</div>
";
    private readonly string example2HTMLCode = @"
<div>
    <div class=""example-desc"">With no maxDisplayedItems</div>
    <BitBreadcrumb Items=""BreadcrumbItems""
                    AriaLabel=""Breadcrumb with no maxDisplayedItems""></BitBreadcrumb>
</div>

<div>
    <div class=""example-desc"">With maxDisplayedItems set to 3</div>
    <BitBreadcrumb Items=""BreadcrumbItems""
                    MaxDisplayedItems=""3""
                    AriaLabel=""Breadcrumb with 3 maxDisplayedItems""></BitBreadcrumb>
</div>

<div>
    <div class=""example-desc"">With maxDisplayedItems set to 2 and overflowIndex set to 1 (second element)""</div>
    <BitBreadcrumb Items=""BreadcrumbItems""
                    MaxDisplayedItems=""2""
                    OverflowIndex=""1""></BitBreadcrumb>
</div>
";
    private readonly string example3HTMLCode = @"
<div>
    <BitBreadcrumb Items=""BreadcrumbItems""
                    MaxDisplayedItems=""2""
                    OverflowIndex=""1""
                    IsEnabled=""false""></BitBreadcrumb>
</div>
";
    private readonly string example4HTMLCode = @"
<div>
    <BitBreadcrumb Items=""BreadcrumbItems""
                    MaxDisplayedItems=""3""
                    OverflowIndex=""2""
                    OverflowAriaLabel=""More Items""></BitBreadcrumb>
</div>
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

<div>
    <BitLabel>Current Item Style</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems""
                    MaxDisplayedItems=""3""
                    OverflowIndex=""2""
                    CurrentItemStyle=""color: blue;""
                    CurrentItem=""BreadcrumbItems[3]"" />
</div>

<div>
    <BitLabel>Current Item Style in Overflow</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems""
                    MaxDisplayedItems=""3""
                    OverflowIndex=""2""
                    CurrentItemStyle=""color: blue;""
                    CurrentItem=""BreadcrumbItems[2]"" />
</div>

<div>
    <BitLabel>Current Item Class</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems""
                    MaxDisplayedItems=""3""
                    OverflowIndex=""2""
                    CurrentItemClass=""custom-current-item""
                    CurrentItem=""BreadcrumbItems[3]"" />
</div>

<div>
    <BitLabel>Current Item Class in Overflow</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems""
                    MaxDisplayedItems=""3""
                    OverflowIndex=""2""
                    CurrentItemClass=""custom-current-item""
                    CurrentItem=""BreadcrumbItems[2]"" />
</div>

<div>
    <BitLabel>Other Item Class</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItemsWithClass"" />
</div>

<div>
    <BitLabel>Other Item Style</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItemsWithStyle"" />
</div>
";
    private readonly string example6HTMLCode = @"
<BitBreadcrumb Items=""@BreadcrumbItemsWithControll""
                MaxDisplayedItems=""3""
                OverflowIndex=""2""
                CurrentItem=""@ControlledCurrentItem""
                OnItemClick=""(item) => ControlledCurrentItem = item""
                CurrentItemStyle=""color: blue;""/>
";

    private readonly string example1CSharpCode = @"
private List<BitBreadcrumbItem> BreadcrumbItems = new List<BitBreadcrumbItem>
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
    private readonly string example5CSharpCode = @"
private List<BitBreadcrumbItem> BreadcrumbItemsWithStyle = new List<BitBreadcrumbItem>
{
    new()
    {
        Text = ""Folder 1"",
        Href = ""/components/breadcrumb"",
        Style = ""background-color: lightblue; padding: 2px 5px; border-radius: 10px;""
    },
    new()
    {
        Text = ""Folder 2"",
        Href = ""/components/breadcrumb"",
        Style = ""background-color: lightblue; padding: 2px 5px; border-radius: 10px;""
    },
    new()
    {
        Text = ""Folder 3"",
        Href = ""/components/breadcrumb"",
        Style = ""background-color: lightblue; padding: 2px 5px; border-radius: 10px;""
    },
    new()
    {
        Text = ""Folder 4"",
        Href = ""/components/breadcrumb"",
        Style = ""background-color: lightblue; padding: 2px 5px; border-radius: 10px;""
    }
};

private List<BitBreadcrumbItem> BreadcrumbItemsWithClass = new List<BitBreadcrumbItem>
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
";
    private readonly string example6CSharpCode = @"
 private BitBreadcrumbItem ControlledCurrentItem;

private List<BitBreadcrumbItem> BreadcrumbItemsWithControll = new List<BitBreadcrumbItem>
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
";
}
