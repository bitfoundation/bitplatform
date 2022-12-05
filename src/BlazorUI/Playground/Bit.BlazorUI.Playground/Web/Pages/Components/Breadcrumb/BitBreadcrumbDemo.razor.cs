using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Breadcrumb;

public partial class BitBreadcrumbDemo
{
    public string OnClickValue { get; set; } = string.Empty;

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
        }
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

    private List<BitBreadcrumbItem> GetBreadcrumbItems()
    {
        return new List<BitBreadcrumbItem>()
        {
            new()
            {
                Text = "Folder 1",
                Key = "f1",
                href = "/components/breadcrumb",
                OnClick = (() => OnClickValue = "Folder 1 clicked")
            },
            new()
            {
                Text = "Folder 2",
                Key = "f2",
                href = "/components/breadcrumb",
                OnClick = (() => OnClickValue = "Folder 2 clicked")
            },
            new()
            {
                Text = "Folder 3",
                Key = "f3",
                href = "/components/breadcrumb",
                OnClick = (() => OnClickValue = "Folder 3 clicked")
            },
            new()
            {
                Text = "Folder 4",
                Key = "f4",
                href = "/components/breadcrumb",
                IsCurrentItem = true,
                OnClick = (() => OnClickValue = "Folder 4 clicked")
            }
        };
    }

    private List<BitBreadcrumbItem> GetBreadcrumbItemsWithStyle()
    {
        return new List<BitBreadcrumbItem>()
        {
            new()
            {
                Text = "Folder 1",
                Key = "f1",
                href = "/components/breadcrumb",
                OnClick = (() => OnClickValue = "Folder 1 clicked"),
            },
            new()
            {
                Text = "Folder 2",
                Key = "f2",
                href = "/components/breadcrumb",
                OnClick = (() => OnClickValue = "Folder 2 clicked"),
            },
            new()
            {
                Text = "Folder 3",
                Key = "f3",
                href = "/components/breadcrumb",
                OnClick = (() => OnClickValue = "Folder 3 clicked"),
                Style = "color: red;"
            },
            new()
            {
                Text = "Folder 4",
                Key = "f4",
                href = "/components/breadcrumb",
                IsCurrentItem = true,
                OnClick = (() => OnClickValue = "Folder 4 clicked")
            }
        };
    }

    private List<BitBreadcrumbItem> GetBreadcrumbItemsWithClass()
    {
        return new List<BitBreadcrumbItem>()
        {
            new()
            {
                Text = "Folder 1",
                Key = "f1",
                href = "/components/breadcrumb",
                OnClick = (() => OnClickValue = "Folder 1 clicked"),
                Class = "custom-item"
            },
            new()
            {
                Text = "Folder 2",
                Key = "f2",
                href = "/components/breadcrumb",
                OnClick = (() => OnClickValue = "Folder 2 clicked"),
                Class = "custom-item"
            },
            new()
            {
                Text = "Folder 3",
                Key = "f3",
                href = "/components/breadcrumb",
                OnClick = (() => OnClickValue = "Folder 3 clicked"),
                 Class = "custom-item"
            },
            new()
            {
                Text = "Folder 4",
                Key = "f4",
                href = "/components/breadcrumb",
                IsCurrentItem = true,
                OnClick = (() => OnClickValue = "Folder 4 clicked"),
                Class = "custom-item last-item"
            }
        };
    }

    private readonly string example1HTMLCode = @"<div class=""example-desc"">With items rendered as links</div>
<div>
    <BitBreadcrumb Items=""GetBreadcrumbItems()""></BitBreadcrumb>
</div>

<div class=""example-desc"">With custom rendered divider and overflow icon</div>
<div>
    <BitBreadcrumb Items=""GetBreadcrumbItems()""
                   DividerIcon=""BitIconName.Separator""
                   OverflowIndex=""1""
                   MaxDisplayedItems=""2""
                   OnRenderOverflowIcon=""BitIconName.ChevronDown""></BitBreadcrumb>
</div>

<div class=""example-desc"">With item OnClick event: @OnClickValue</div>
<div>
    <BitBreadcrumb Items=""GetBreadcrumbItems()""></BitBreadcrumb>
</div>";

    private readonly string example2HTMLCode = @"<div class=""example-desc"">With no maxDisplayedItems</div>
<div>
    <BitBreadcrumb Items=""GetBreadcrumbItems()""
                   AriaLabel=""Breadcrumb with no maxDisplayedItems""></BitBreadcrumb>
</div>
<div class=""example-desc"">With maxDisplayedItems set to 3</div>
<div>
    <BitBreadcrumb Items=""GetBreadcrumbItems()""
                   MaxDisplayedItems=""3""
                   AriaLabel=""Breadcrumb with 3 maxDisplayedItems""></BitBreadcrumb>
</div>

<div class=""example-desc"">With maxDisplayedItems set to 2 and overflowIndex set to 1 (second element)""</div>
<div>
    <BitBreadcrumb Items=""GetBreadcrumbItems()""
                   MaxDisplayedItems=""2""
                   OverflowIndex=""1""></BitBreadcrumb>
</div>";

    private readonly string example3HTMLCode = @"<div class=""example-desc"">BitBreadcrumb can be disabled or enabled by setting IsEnabled attribute.</div>
<div>
    <BitBreadcrumb Items=""GetBreadcrumbItems()""
                   MaxDisplayedItems=""2""
                   OverflowIndex=""1""
                   IsEnabled=""false""></BitBreadcrumb>
</div>";

    private readonly string example4HTMLCode = @"<div>
    <BitBreadcrumb Items=""GetBreadcrumbItems()""
                   MaxDisplayedItems=""3""
                   OverflowIndex=""2""
                   OverflowAriaLabel=""More Items""></BitBreadcrumb>
</div>";

    private readonly string example1CSharpCode = @"
public string OnClickValue { get; set; } = string.Empty;

private List<BitBreadcrumbItem> GetBreadcrumbItems()
{
    return new List<BitBreadcrumbItem>()
    {
        new()
        {
            Text = ""Folder 1"",
            Key = ""f1"",
            href = ""/components/breadcrumb"",
            OnClick = (() => OnClickValue = ""Folder 1 clicked"")
        },
        new()
        {
            Text = ""Folder 2"",
            Key = ""f2"",
            href = ""/components/breadcrumb"",
            OnClick = (() => OnClickValue = ""Folder 2 clicked"")
        },
        new()
        {
            Text = ""Folder 3"",
            Key = ""f3"",
            href = ""/components/breadcrumb"",
            OnClick = (() => OnClickValue = ""Folder 3 clicked"")
        },
        new()
        {
            Text = ""Folder 4"",
            Key = ""f4"",
            href = ""/components/breadcrumb"",
            IsCurrentItem = true,
            OnClick = (() => OnClickValue = ""Folder 4 clicked"")
        }
    };
}";

    private readonly string example5HTMLCode = @"
<style>
    .custom-item {
        background-color: lightgreen;
        padding: 2px;
        margin: 0 5px;
        border-radius: 10px;
    }

    .last-item {
        background-color: green;
        padding: 5px;
    }
</style>

<div>
    <BitLabel>Class</BitLabel>
    <BitBreadcrumb Items=""GetBreadcrumbItemsWithClass()"" />
</div>
<div>
    <BitLabel>Style</BitLabel>
    <BitBreadcrumb Items=""GetBreadcrumbItemsWithStyle()"" />
</div>
";

    private readonly string example5CSharpCode = @"
private List<BitBreadcrumbItem> GetBreadcrumbItemsWithStyle()
{
    return new List<BitBreadcrumbItem>()
    {
        new()
        {
            Text = ""Folder 1"",
            Key = ""f1"",
            href = ""/components/breadcrumb"",
            OnClick = (() => OnClickValue = ""Folder 1 clicked""),
        },
        new()
        {
            Text = ""Folder 2"",
            Key = ""f2"",
            href = ""/components/breadcrumb"",
            OnClick = (() => OnClickValue = ""Folder 2 clicked""),
        },
        new()
        {
            Text = ""Folder 3"",
            Key = ""f3"",
            href = ""/components/breadcrumb"",
            OnClick = (() => OnClickValue = ""Folder 3 clicked""),
            Style = ""color: red;""
        },
        new()
        {
            Text = ""Folder 4"",
            Key = ""f4"",
            href = ""/components/breadcrumb"",
            IsCurrentItem = true,
            OnClick = (() => OnClickValue = ""Folder 4 clicked"")
        }
    };
}

private List<BitBreadcrumbItem> GetBreadcrumbItemsWithClass()
{
    return new List<BitBreadcrumbItem>()
    {
        new()
        {
            Text = ""Folder 1"",
            Key = ""f1"",
            href = ""/components/breadcrumb"",
            OnClick = (() => OnClickValue = ""Folder 1 clicked""),
            Class = ""custom-item""
        },
        new()
        {
            Text = ""Folder 2"",
            Key = ""f2"",
            href = ""/components/breadcrumb"",
            OnClick = (() => OnClickValue = ""Folder 2 clicked""),
            Class = ""custom-item""
        },
        new()
        {
            Text = ""Folder 3"",
            Key = ""f3"",
            href = ""/components/breadcrumb"",
            OnClick = (() => OnClickValue = ""Folder 3 clicked""),
            Class = ""custom-item""
        },
        new()
        {
            Text = ""Folder 4"",
            Key = ""f4"",
            href = ""/components/breadcrumb"",
            IsCurrentItem = true,
            OnClick = (() => OnClickValue = ""Folder 4 clicked""),
            Class = ""custom-item last-item""
        }
    };
}
";
}
