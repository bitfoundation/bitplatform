using System.Collections.Generic;
using System.Linq;
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
                Href = "/components/breadcrumb",
                IsCurrent = true
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
                Class = "custom-item",
                IsCurrent = true
            }
        };

        BreadcrumbItemsWithStyle = new List<BitBreadcrumbItem>
        {
            new()
            {
                Text = "Folder 1",
                Href = "/components/breadcrumb",
                Style = "color:red;background:greenyellow"
            },
            new()
            {
                Text = "Folder 2",
                Href = "/components/breadcrumb",
                Style = "color:red;background:darkseagreen"
            },
            new()
            {
                Text = "Folder 3",
                Href = "/components/breadcrumb",
                Style = "color:red;background:lawngreen"
            },
            new()
            {
                Text = "Folder 4",
                Href = "/components/breadcrumb",
                Style = "color:red;background:lightgreen",
                IsCurrent = true
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
            },
            new()
            {
                Text = "Folder 5"
            },
            new()
            {
                Text = "Folder 6",
                IsCurrent = true
            }
        };
    }

    private List<BitBreadcrumbItem> BreadcrumbItems { get; set; }
    private List<BitBreadcrumbItem> BreadcrumbItemsWithClass { get; set; }
    private List<BitBreadcrumbItem> BreadcrumbItemsWithStyle { get; set; }
    private List<BitBreadcrumbItem> BreadcrumbItemsWithControll { get; set; }

    private void HandleOnItemClick(BitBreadcrumbItem item)
    {
        BreadcrumbItemsWithControll.FirstOrDefault(i => i.IsCurrent).IsCurrent = false;
        BreadcrumbItemsWithControll.FirstOrDefault(i => i == item).IsCurrent = true;
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
            Name = "OverflowIcon",
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

    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new ComponentSubParameter()
        {
            Id = "bit-breadcrumb-item",
            Title = "BitBreadcrumbItem",
            Parameters = new List<ComponentParameter>()
            {
               new ComponentParameter()
               {
                   Name = "Text",
                   Type = "string?",
                   Description = "Text to display in the breadcrumb item.",
               },
               new ComponentParameter()
               {
                   Name = "Href",
                   Type = "string?",
                   Description = "URL to navigate to when this breadcrumb item is clicked. If provided, the breadcrumb will be rendered as a link.",
               },
               new ComponentParameter()
               {
                   Name = "Class",
                   Type = "string?",
                   Description = "Class HTML attribute for breadcrumb item.",
               },
               new ComponentParameter()
               {
                   Name = "Style",
                   Type = "string?",
                   Description = "Style HTML attribute for breadcrumb item.",
               },
               new ComponentParameter()
               {
                   Name = "IsCurrent",
                   Type = "bool",
                   Description = "Display the item as a current item.",
               },
               new ComponentParameter()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   Description = "Whether an item is enabled or not.",
               },
            }
        }
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
        Href = ""/components/breadcrumb"",
        IsCurrent = true
    }
};
";

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

    private readonly string example2HTMLCode = @"
<div>
    <BitLabel>MaxDisplayedItems (1)</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""1"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (2)</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""2"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3)</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""3"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (0)</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""3"" OverflowIndex=""0"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (1)</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""3"" OverflowIndex=""1"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (2)</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems"" MaxDisplayedItems=""3"" OverflowIndex=""2"" />
</div>
";

    private readonly string example3HTMLCode = @"
<div>
    <BitLabel>BitIconName (ChevronDown)</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems""
                   MaxDisplayedItems=""3""
                   OverflowIndex=""2""
                   OverflowIcon=""BitIconName.ChevronDown"" />
</div>

<div>
    <BitLabel>BitIconName (CollapseMenu)</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems""
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
        background: lightgreen;
    }
    .custom-item:hover {
        background: greenyellow;
    }

    .custom-current-item {
        color: red;
        margin: 2px 5px;
        border-radius: 2px;
        background: lightgreen;
    }
    .custom-current-item:hover {
        background: greenyellow;
    }
</style>

<div>
    <BitLabel>Items Class</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItemsWithClass"" />
</div>
<div>
    <BitLabel>Items Style</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItemsWithStyle"" />
</div>
<div>
    <BitLabel>Current Item Class</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems""
                   CurrentItemClass=""custom-current-item""
                   CurrentItem=""BreadcrumbItems[3]"" />
</div>
<div>
    <BitLabel>Current Item Style</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems""
                   CurrentItemStyle=""color:red;background:lightgreen""
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
        Href = ""/components/breadcrumb"",
        IsCurrent = true
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
        Class = ""custom-item"",
        IsCurrent = true
    }
};

private List<BitBreadcrumbItem> BreadcrumbItemsWithStyle { get; set; } = new List<BitBreadcrumbItem>
{
    new()
    {
        Text = ""Folder 1"",
        Href = ""/components/breadcrumb"",
        Style = ""color:red;background:lightgreen""
    },
    new()
    {
        Text = ""Folder 2"",
        Href = ""/components/breadcrumb"",
        Style = ""color:red;background:lightgreen""
    },
    new()
    {
        Text = ""Folder 3"",
        Href = ""/components/breadcrumb"",
        Style = ""color:red;background:lightgreen""
    },
    new()
    {
        Text = ""Folder 4"",
        Href = ""/components/breadcrumb"",
        Style = ""color:red;background:lightgreen"",
        IsCurrent = true
    }
};
";

    private readonly string example5HTMLCode = @"
<BitBreadcrumb Items=""@BreadcrumbItemsWithControll""
               MaxDisplayedItems=""3""
               OverflowIndex=""2""
               OnItemClick=""HandleOnItemClick""
               CurrentItemStyle=""color:red;background:lightgreen"" />
";

    private readonly string example5CSharpCode = @"
private BitBreadcrumbItem ControlledCurrentItem;
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
    },
    new()
    {
        Text = ""Folder 5""
    },
    new()
    {
        Text = ""Folder 6"",
        IsCurrent = true
    }
};

private void HandleOnItemClick(BitBreadcrumbItem item)
{
    BreadcrumbItemsWithControll.FirstOrDefault(i => i.IsCurrent).IsCurrent = false;
    BreadcrumbItemsWithControll.FirstOrDefault(i => i == item).IsCurrent = true;
}
";
}
