using System.Linq;
using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Breadcrumb;

public partial class BitBreadcrumbDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "DividerIcon",
            Type = "BitIconName",
            DefaultValue = "BitIconName.ChevronRight",
            Description = "The divider icon name. The default value is BitIconName.ChevronRight."
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
            Type = "uint",
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
            Type = "uint",
            Description = "Optional index where overflow items will be collapsed."
        },
        new()
        {
            Name = "OverflowIcon",
            Type = "BitIconName",
            DefaultValue= "BitIconName.More",
            Description = "The overflow icon name. The default value is BitIconName.More."
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<BitBreadcrumbItem>",
            Description = "Callback for when the breadcrumb item clicked."
        },
        new()
        {
            Name = "SelectedItemClass",
            Type = "string?",
            Description = "The CSS class attribute for the selected item."
        },
        new()
        {
            Name = "SelectedItemStyle",
            Type = "string?",
            Description = "The style attribute for selected item."
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
                   Description = "CSS class attribute for breadcrumb item.",
               },
               new ComponentParameter()
               {
                   Name = "Style",
                   Type = "string?",
                   Description = "Style attribute for breadcrumb item.",
               },
               new ComponentParameter()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   Description = "Display the item as the selected item.",
               },
               new ComponentParameter()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether an item is enabled or not.",
               },
            }
        }
    };


    private readonly List<BitBreadcrumbItem> BreadcrumbItems = new()
    {
        new() { Text = "Folder 1", Href = "/components/breadcrumb" },
        new() { Text = "Folder 2", Href = "/components/breadcrumb" },
        new() { Text = "Folder 3", Href = "/components/breadcrumb" },
        new() { Text = "Folder 4", Href = "/components/breadcrumb", IsSelected = true }
    };

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsDisabled = new()
    {
        new() { Text = "Folder 1", Href = "/components/breadcrumb", IsEnabled = false },
        new() { Text = "Folder 2", Href = "/components/breadcrumb", IsEnabled = false },
        new() { Text = "Folder 3", Href = "/components/breadcrumb" },
        new() { Text = "Folder 4", Href = "/components/breadcrumb", IsSelected = true }
    };

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithClass = new()
    {
        new() { Text = "Folder 1", Href = "/components/breadcrumb", Class = "custom-item" },
        new() { Text = "Folder 2", Href = "/components/breadcrumb", Class = "custom-item" },
        new() { Text = "Folder 3", Href = "/components/breadcrumb", Class = "custom-item" },
        new() { Text = "Folder 4", Href = "/components/breadcrumb", Class = "custom-item", IsSelected = true }
    };

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithStyle = new()
    {
        new() { Text = "Folder 1", Href = "/components/breadcrumb", Style = "color:red;background:greenyellow" },
        new() { Text = "Folder 2", Href = "/components/breadcrumb", Style = "color:red;background:greenyellow" },
        new() { Text = "Folder 3", Href = "/components/breadcrumb", Style = "color:red;background:greenyellow" },
        new() { Text = "Folder 4", Href = "/components/breadcrumb", Style = "color:red;background:greenyellow", IsSelected = true }
    };

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithControlled = new()
    {
        new() { Text = "Folder 1" },
        new() { Text = "Folder 2" },
        new() { Text = "Folder 3" },
        new() { Text = "Folder 4" },
        new() { Text = "Folder 5" },
        new() { Text = "Folder 6", IsSelected = true }
    };

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithCustomized = new()
    {
        new() { Text = "Folder 1" },
        new() { Text = "Folder 2" },
        new() { Text = "Folder 3" },
        new() { Text = "Folder 4", IsSelected = true }
    };

    private int ItemsCount = 4;
    private uint OverflowIndex = 2;
    private uint MaxDisplayedItems = 3;
    private uint NumericTextFieldStep = 1;

    private void HandleOnItemClick(BitBreadcrumbItem item)
    {
        BreadcrumbItemsWithControlled.FirstOrDefault(i => i.IsSelected).IsSelected = false;
        item.IsSelected = true;
    }
    private void HandleOnItemClick_Customized(BitBreadcrumbItem item)
    {
        BreadcrumbItemsWithCustomized.FirstOrDefault(i => i.IsSelected).IsSelected = false;
        item.IsSelected = true;
    }

    private void AddItem()
    {
        ItemsCount++;
        BreadcrumbItemsWithCustomized.Add(new BitBreadcrumbItem()
        {
            Text = $"Folder {ItemsCount}"
        });
    }
    private void RemoveItem()
    {
        if (BreadcrumbItemsWithCustomized.Count > 1)
        {
            ItemsCount--;

            var item = BreadcrumbItemsWithCustomized[^1];
            BreadcrumbItemsWithCustomized.Remove(item);

            if (item.IsSelected)
            {
                BreadcrumbItemsWithCustomized[^1].IsSelected = true;
            }
        }
    }

    private readonly string basicItemsCSharpCode = @"
private List<BitBreadcrumbItem> BreadcrumbItems { get; set; } = new()
{
    new() { Text = ""Folder 1"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 2"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 3"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 4"", Href = ""/components/breadcrumb"", IsSelected = true }
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
<div>
    <BitLabel>Item Disabled</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItemsDisabled"" />
</div>
";
    private readonly string example1CSharpCode = @"
private List<BitBreadcrumbItem> BreadcrumbItems { get; set; } = new()
{
    new() { Text = ""Folder 1"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 2"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 3"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 4"", Href = ""/components/breadcrumb"", IsSelected = true }
};

 private List<BitBreadcrumbItem> BreadcrumbItemsDisabled { get; set; } = new()
{
    new() { Text = ""Folder 1"", Href = ""/components/breadcrumb"", IsEnabled = false },
    new() { Text = ""Folder 2"", Href = ""/components/breadcrumb"", IsEnabled = false },
    new() { Text = ""Folder 3"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 4"", Href = ""/components/breadcrumb"", IsSelected = true }
};
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
    .bit-brc .custom-item {
        color: red;
        margin: 2px 5px;
        border-radius: 2px;
        background: limegreen;
    }

        .bit-brc .custom-item:hover {
            background: greenyellow;
        }

    .bit-brc .custom-selected-item {
        color: red;
        margin: 2px 5px;
        border-radius: 2px;
        background: limegreen;
    }

        .bit-brc .custom-current-item:hover {
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
    <BitLabel>Selected Item Class</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems""
                   SelectedItemClass=""custom-selected-item"" />
</div>
<div>
    <BitLabel>Selected Item Style</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems""
                   SelectedItemStyle=""color:red;background:lightgreen"" />
</div>
";
    private readonly string example4CSharpCode = @"
private List<BitBreadcrumbItem> BreadcrumbItems { get; set; } = new()
{
    new() { Text = ""Folder 1"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 2"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 3"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 4"", Href = ""/components/breadcrumb"", IsSelected = true }
};

private List<BitBreadcrumbItem> BreadcrumbItemsWithClass { get; set; } = new()
{
    new() { Text = ""Folder 1"", Href = ""/components/breadcrumb"", Class = ""custom-item"" },
    new() { Text = ""Folder 2"", Href = ""/components/breadcrumb"", Class = ""custom-item"" },
    new() { Text = ""Folder 3"", Href = ""/components/breadcrumb"", Class = ""custom-item"" },
    new() { Text = ""Folder 4"", Href = ""/components/breadcrumb"", Class = ""custom-item"", IsSelected = true }
};

private List<BitBreadcrumbItem> BreadcrumbItemsWithStyle { get; set; } = new()
{
    new() { Text = ""Folder 1"", Href = ""/components/breadcrumb"", Style = ""color:red;background:greenyellow"" },
    new() { Text = ""Folder 2"", Href = ""/components/breadcrumb"", Style = ""color:red;background:greenyellow"" },
    new() { Text = ""Folder 3"", Href = ""/components/breadcrumb"", Style = ""color:red;background:greenyellow"" },
    new() { Text = ""Folder 4"", Href = ""/components/breadcrumb"", Style = ""color:red;background:greenyellow"", IsSelected = true }
};
";

    private readonly string example5HTMLCode = @"
<BitBreadcrumb Items=""@BreadcrumbItemsWithControlled""
               MaxDisplayedItems=""3""
               OverflowIndex=""2""
               OnItemClick=""HandleOnItemClick""
               SelectedItemStyle=""color:red;background:lightgreen"" />
";
    private readonly string example5CSharpCode = @"
private List<BitBreadcrumbItem> BreadcrumbItemsWithControlled { get; set; } = new()
{
    new() { Text = ""Folder 1"" },
    new() { Text = ""Folder 2"" },
    new() { Text = ""Folder 3"" },
    new() { Text = ""Folder 4"" },
    new() { Text = ""Folder 5"" },
    new() { Text = ""Folder 6"", IsSelected = true }
};

private void HandleOnItemClick(BitBreadcrumbItem item)
{
    BreadcrumbItemsWithControlled.FirstOrDefault(i => i.IsSelected).IsSelected = false;
    item.IsSelected = true;
}
";

    private readonly string example6HTMLCode = @"
<div>
    <BitBreadcrumb Items=""@BreadcrumbItemsWithCustomized""
                    MaxDisplayedItems=""@MaxDisplayedItems""
                    OverflowIndex=""@OverflowIndex""
                    OnItemClick=""HandleOnItemClick"" />
</div>
<div class=""operators"">
    <div>
        <BitButton OnClick=""AddItem"">Add Item</BitButton>
        <BitButton OnClick=""RemoveItem"">Remove Item</BitButton>
    </div>
    <div>
        <BitNumericTextField @bind-Value=""MaxDisplayedItems"" Step=""@NumericTextFieldStep"" Label=""MaxDisplayedItems"" ShowArrows=""true"" />
        <BitNumericTextField @bind-Value=""OverflowIndex"" Step=""@NumericTextFieldStep"" Label=""OverflowIndex"" ShowArrows=""true"" />
    </div>
</div>
";
    private readonly string example6CSharpCode = @"
private List<BitBreadcrumbItem> BreadcrumbItemsWithCustomized { get; set; } = new()
{
    new() { Text = ""Folder 1"" },
    new() { Text = ""Folder 2"" },
    new() { Text = ""Folder 3"" },
    new() { Text = ""Folder 4"", IsSelected = true }
};

private int ItemsCount = 4;
private uint OverflowIndex = 2;
private uint MaxDisplayedItems = 3;
private uint NumericTextFieldStep = 1;

private void AddItem()
{
    ItemsCount++;
    BreadcrumbItemsWithCustomized.Add(new BitBreadcrumbItem()
    {
        Text = $""Folder {ItemsCount}""
    });
}

private void RemoveItem()
{
    if (BreadcrumbItemsWithCustomized.Count > 1)
    {
        ItemsCount--;

        var item = BreadcrumbItemsWithCustomized[^1];
        BreadcrumbItemsWithCustomized.Remove(item);

        if (item.IsSelected)
        {
            BreadcrumbItemsWithCustomized[^1].IsSelected = true;
        }
    }
}

private void HandleOnItemClick(BitBreadcrumbItem item)
{
    BreadcrumbItemsWithCustomized.FirstOrDefault(i => i.IsSelected).IsSelected = false;
    item.IsSelected = true;
}
";
}
