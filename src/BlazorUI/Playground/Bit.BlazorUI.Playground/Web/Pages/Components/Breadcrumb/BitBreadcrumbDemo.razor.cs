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
            Name = "ClassField",
            Type = "string",
            DefaultValue = "Class",
            Description = "Class HTML attribute for BreadList item."
        },
        new()
        {
            Name = "ClassFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Class HTML attribute for BreadList item."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            Description = "The content of the BitBreadcrumb, that are BitBreadOption components."
        },
        new()
        {
            Name = "DividerIcon",
            Type = "BitIconName",
            DefaultValue = "BitIconName.ChevronRight",
            Description = "The divider icon name. The default value is BitIconName.ChevronRight."
        },
        new()
        {
            Name = "HrefField",
            Type = "string",
            DefaultValue = "Href",
            Description = "URL to navigate to when this BreadList item is clicked. If provided, the BreadList will be rendered as a link."
        },
        new()
        {
            Name = "HrefFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "URL to navigate to when this BreadList item is clicked. If provided, the BreadList will be rendered as a link."
        },
        new()
        {
            Name = "Items",
            Type = "IList<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "Collection of breadcrumbs to render"
        },
        new()
        {
            Name = "IsSelectedField",
            Type = "string",
            DefaultValue = "IsSelected",
            Description = "Display the item as a Selected item."
        },
        new()
        {
            Name = "IsSelectedFieldSelector",
            Type = "Expression<Func<TItem, bool>>?",
            Description = "Display the item as a Selected item."
        },
        new()
        {
            Name = "IsEnabledField",
            Type = "string",
            DefaultValue = "",
            Description = "Whether an item is enabled or not."
        },
        new()
        {
            Name = "IsEnabledFieldSelector",
            Type = "Expression<Func<TItem, bool>>?",
            Description = "Whether an item is enabled or not."
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
            Type = "EventCallback<TItem>",
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
        new()
        {
            Name = "StyleField",
            Type = "string",
            DefaultValue = "Style",
            Description = "Style HTML attribute for BreadList item."
        },
        new()
        {
            Name = "StyleFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Style HTML attribute for BreadList item."
        },
        new()
        {
            Name = "TextField",
            Type = "string",
            DefaultValue = "Text",
            Description = "Text to display in the BreadList item."
        },
        new()
        {
            Name = "TextFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Text to display in the BreadList item."
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
        },
        new ComponentSubParameter()
        {
            Id = "bit-breadcrumb-option",
            Title = "BitBreadcrumbOption",
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
        },
    };


    private uint OverflowIndex = 2;
    private uint MaxDisplayedItems = 3;
    private uint NumericTextFieldStep = 1;
    private int ItemsCount = 4;
    private int SelectedOptionNumber = 6;
    private int CustomizedSelectedOptionNumber = 4;

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


    private readonly List<PageInfoModel> CustomBreadcrumbItems = new()
    {
        new() { Name = "Folder 1", Address = "/components/breadcrumb" },
        new() { Name = "Folder 2", Address = "/components/breadcrumb" },
        new() { Name = "Folder 3", Address = "/components/breadcrumb" },
        new() { Name = "Folder 4", Address = "/components/breadcrumb", IsCurrent = true }
    };

    private readonly List<PageInfoModel> CustomBreadcrumbItemsDisabled = new()
    {
        new() { Name = "Folder 1", Address = "/components/breadcrumb", IsEnabled = false },
        new() { Name = "Folder 2", Address = "/components/breadcrumb", IsEnabled = false },
        new() { Name = "Folder 3", Address = "/components/breadcrumb" },
        new() { Name = "Folder 4", Address = "/components/breadcrumb", IsCurrent = true }
    };

    private readonly List<PageInfoModel> CustomBreadcrumbItemsWithClass = new()
    {
        new() { Name = "Folder 1", Address = "/components/breadcrumb", HtmlClass = "custom-item" },
        new() { Name = "Folder 2", Address = "/components/breadcrumb", HtmlClass = "custom-item" },
        new() { Name = "Folder 3", Address = "/components/breadcrumb", HtmlClass = "custom-item" },
        new() { Name = "Folder 4", Address = "/components/breadcrumb", HtmlClass = "custom-item", IsCurrent = true }
    };

    private readonly List<PageInfoModel> CustomBreadcrumbItemsWithStyle = new()
    {
        new() { Name = "Folder 1", Address = "/components/breadcrumb", HtmlStyle = "color:red;background:greenyellow" },
        new() { Name = "Folder 2", Address = "/components/breadcrumb", HtmlStyle = "color:red;background:greenyellow" },
        new() { Name = "Folder 3", Address = "/components/breadcrumb", HtmlStyle = "color:red;background:greenyellow" },
        new() { Name = "Folder 4", Address = "/components/breadcrumb", HtmlStyle = "color:red;background:greenyellow", IsCurrent = true }
    };

    private readonly List<PageInfoModel> CustomBreadcrumbItemsWithControlled = new()
    {
        new() { Name = "Folder 1" },
        new() { Name = "Folder 2" },
        new() { Name = "Folder 3" },
        new() { Name = "Folder 4" },
        new() { Name = "Folder 5" },
        new() { Name = "Folder 6", IsCurrent = true }
    };

    private readonly List<PageInfoModel> CustomBreadcrumbItemsWithCustomized = new()
    {
        new() { Name = "Folder 1" },
        new() { Name = "Folder 2" },
        new() { Name = "Folder 3" },
        new() { Name = "Folder 4", IsCurrent = true }
    };

    private void HandleOnItemClick(BitBreadcrumbItem item)
    {
        BreadcrumbItemsWithControlled.FirstOrDefault(i => i.IsSelected).IsSelected = false;
        item.IsSelected = true;
    }
    private void HandleOnCustomizedItemClick(BitBreadcrumbItem item)
    {
        BreadcrumbItemsWithCustomized.FirstOrDefault(i => i.IsSelected).IsSelected = false;
        item.IsSelected = true;
    }

    private void HandleOnItemClick(PageInfoModel item)
    {
        BreadcrumbItemsWithControlled.FirstOrDefault(i => i.IsSelected).IsSelected = false;
        item.IsCurrent = true;
    }
    private void HandleOnCustomizedItemClick(PageInfoModel item)
    {
        BreadcrumbItemsWithCustomized.FirstOrDefault(i => i.IsSelected).IsSelected = false;
        item.IsCurrent = true;
    }

    private void AddBreadcrumbItem()
    {
        ItemsCount++;
        BreadcrumbItemsWithCustomized.Add(new BitBreadcrumbItem()
        {
            Text = $"Folder {ItemsCount}"
        });
    }
    private void RemoveBreadcrumbItem()
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

    private void AddCustomItem()
    {
        ItemsCount++;
        BreadcrumbItemsWithCustomized.Add(new BitBreadcrumbItem()
        {
            Text = $"Folder {ItemsCount}"
        });
    }
    private void RemoveCustomItem()
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

    private readonly string example1BreadcrumbItemHTMLCode = @"
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
    private readonly string example1CustomItemHTMLCode = @"
<div>
    <BitLabel>Basic</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                    TextField=""@nameof(PageInfoModel.Name)""
                    HrefField=""@nameof(PageInfoModel.Address)""
                    IsSelectedField=""@nameof(PageInfoModel.IsCurrent)""
                    ClassField=""@nameof(PageInfoModel.HtmlClass)""
                    StyleField=""@nameof(PageInfoModel.HtmlStyle)"" />
</div>
<div>
    <BitLabel>Disabled</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                    TextField=""@nameof(PageInfoModel.Name)""
                    HrefField=""@nameof(PageInfoModel.Address)""
                    IsSelectedField=""@nameof(PageInfoModel.IsCurrent)""
                    ClassField=""@nameof(PageInfoModel.HtmlClass)""
                    StyleField=""@nameof(PageInfoModel.HtmlStyle)""
                    IsEnabled=""false"" />
</div>
<div>
    <BitLabel>Item Disabled</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItemsDisabled""
                    TextField=""@nameof(PageInfoModel.Name)""
                    HrefField=""@nameof(PageInfoModel.Address)""
                    IsSelectedField=""@nameof(PageInfoModel.IsCurrent)""
                    ClassField=""@nameof(PageInfoModel.HtmlClass)""
                    StyleField=""@nameof(PageInfoModel.HtmlStyle)"" />
</div>
";
    private readonly string example1BreadcrumbOptionHTMLCode = @"
<div>
    <BitLabel>Basic</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>

<div>
    <BitLabel>Group Disabled</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" IsEnabled=""false"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>

<div>
    <BitLabel>Option Disabled</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" IsEnabled=""false"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" IsEnabled=""false"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
";

    private readonly string example2BreadcrumbItemHTMLCode = @"
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
    private readonly string example2CustomItemHTMLCode = @"
<div>
    <BitLabel>MaxDisplayedItems (1)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                    TextField=""@nameof(PageInfoModel.Name)""
                    HrefField=""@nameof(PageInfoModel.Address)""
                    IsSelectedField=""@nameof(PageInfoModel.IsCurrent)""
                    ClassField=""@nameof(PageInfoModel.HtmlClass)""
                    StyleField=""@nameof(PageInfoModel.HtmlStyle)""
                    MaxDisplayedItems=""1"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (2)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                    TextField=""@nameof(PageInfoModel.Name)""
                    HrefField=""@nameof(PageInfoModel.Address)""
                    IsSelectedField=""@nameof(PageInfoModel.IsCurrent)""
                    ClassField=""@nameof(PageInfoModel.HtmlClass)""
                    StyleField=""@nameof(PageInfoModel.HtmlStyle)""
                    MaxDisplayedItems=""2"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                    TextField=""@nameof(PageInfoModel.Name)""
                    HrefField=""@nameof(PageInfoModel.Address)""
                    IsSelectedField=""@nameof(PageInfoModel.IsCurrent)""
                    ClassField=""@nameof(PageInfoModel.HtmlClass)""
                    StyleField=""@nameof(PageInfoModel.HtmlStyle)""
                    MaxDisplayedItems=""3"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (0)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                    TextField=""@nameof(PageInfoModel.Name)""
                    HrefField=""@nameof(PageInfoModel.Address)""
                    IsSelectedField=""@nameof(PageInfoModel.IsCurrent)""
                    ClassField=""@nameof(PageInfoModel.HtmlClass)""
                    StyleField=""@nameof(PageInfoModel.HtmlStyle)""
                    MaxDisplayedItems=""3""
                    OverflowIndex=""0"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (1)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                    TextField=""@nameof(PageInfoModel.Name)""
                    HrefField=""@nameof(PageInfoModel.Address)""
                    IsSelectedField=""@nameof(PageInfoModel.IsCurrent)""
                    ClassField=""@nameof(PageInfoModel.HtmlClass)""
                    StyleField=""@nameof(PageInfoModel.HtmlStyle)""
                    MaxDisplayedItems=""3""
                    OverflowIndex=""1"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (2)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                    TextField=""@nameof(PageInfoModel.Name)""
                    HrefField=""@nameof(PageInfoModel.Address)""
                    IsSelectedField=""@nameof(PageInfoModel.IsCurrent)""
                    ClassField=""@nameof(PageInfoModel.HtmlClass)""
                    StyleField=""@nameof(PageInfoModel.HtmlStyle)""
                    MaxDisplayedItems=""3""
                    OverflowIndex=""2"" />
</div>
";
    private readonly string example2BreadcrumbOptionHTMLCode = @"
<div>
    <BitLabel>MaxDisplayedOptions (1)</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""1"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
<div>
    <BitLabel>MaxDisplayedOptions (2)</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""2"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
<div>
    <BitLabel>MaxDisplayedOptions (3)</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
<div>
    <BitLabel>MaxDisplayedOptions (3), OverflowIndex (0)</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""0"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
<div>
    <BitLabel>MaxDisplayedOptions (3), OverflowIndex (1)</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""1"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
<div>
    <BitLabel>MaxDisplayedOptions (3), OverflowIndex (2)</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
";

    private readonly string example3BreadcrumbItemHTMLCode = @"
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
    private readonly string example3CustomItemHTMLCode = @"
<div>
    <BitLabel>BitIconName (ChevronDown)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                    TextField=""@nameof(PageInfoModel.Name)""
                    HrefField=""@nameof(PageInfoModel.Address)""
                    IsSelectedField=""@nameof(PageInfoModel.IsCurrent)""
                    ClassField=""@nameof(PageInfoModel.HtmlClass)""
                    StyleField=""@nameof(PageInfoModel.HtmlStyle)""
                    MaxDisplayedItems=""3""
                    OverflowIndex=""2""
                    OverflowIcon=""BitIconName.ChevronDown"" />
</div>
<div>
    <BitLabel>BitIconName (CollapseMenu)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                    TextField=""@nameof(PageInfoModel.Name)""
                    HrefField=""@nameof(PageInfoModel.Address)""
                    IsSelectedField=""@nameof(PageInfoModel.IsCurrent)""
                    ClassField=""@nameof(PageInfoModel.HtmlClass)""
                    StyleField=""@nameof(PageInfoModel.HtmlStyle)""
                    MaxDisplayedItems=""3""
                    OverflowIndex=""2""
                    OverflowIcon=""BitIconName.CollapseMenu"" />
</div>
";
    private readonly string example3BreadcrumbOptionHTMLCode = @"
<div>
    <BitLabel>BitIconName (ChevronDown)</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"" OverflowIcon=""BitIconName.ChevronDown"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>

<div>
    <BitLabel>BitIconName (CollapseMenu)</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"" OverflowIcon=""BitIconName.CollapseMenu"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
";

    private readonly string example4BreadcrumbItemHTMLCode = @"
<style>
    .custom-item {
        color: red;
        margin: 2px 5px;
        border-radius: 2px;
        background: limegreen;

        &:hover {
            background: greenyellow;
        }
    }

    .custom-selected-item {
        color: red;
        margin: 2px 5px;
        border-radius: 2px;
        background: mediumspringgreen;

        &:hover {
            background: greenyellow;
        }
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
    private readonly string example4CustomItemHTMLCode = @"
<style>
    .custom-item {
        color: red;
        margin: 2px 5px;
        border-radius: 2px;
        background: limegreen;

        &:hover {
            background: greenyellow;
        }
    }

    .custom-selected-item {
        color: red;
        margin: 2px 5px;
        border-radius: 2px;
        background: mediumspringgreen;

        &:hover {
            background: greenyellow;
        }
    }
</style>

<div>
    <BitLabel>Items Class</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItemsWithClass""
                    TextField=""@nameof(PageInfoModel.Name)""
                    HrefField=""@nameof(PageInfoModel.Address)""
                    IsSelectedField=""@nameof(PageInfoModel.IsCurrent)""
                    ClassField=""@nameof(PageInfoModel.HtmlClass)""
                    StyleField=""@nameof(PageInfoModel.HtmlStyle)"" />
</div>
<div>
    <BitLabel>Items Style</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItemsWithStyle""
                    TextField=""@nameof(PageInfoModel.Name)""
                    HrefField=""@nameof(PageInfoModel.Address)""
                    IsSelectedField=""@nameof(PageInfoModel.IsCurrent)""
                    ClassField=""@nameof(PageInfoModel.HtmlClass)""
                    StyleField=""@nameof(PageInfoModel.HtmlStyle)"" />
</div>
<div>
    <BitLabel>Selected Item Class</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                    TextField=""@nameof(PageInfoModel.Name)""
                    HrefField=""@nameof(PageInfoModel.Address)""
                    IsSelectedField=""@nameof(PageInfoModel.IsCurrent)""
                    ClassField=""@nameof(PageInfoModel.HtmlClass)""
                    StyleField=""@nameof(PageInfoModel.HtmlStyle)""
                    SelectedItemClass=""custom-selected-item"" />
</div>
<div>
    <BitLabel>Selected Item Style</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                    TextField=""@nameof(PageInfoModel.Name)""
                    HrefField=""@nameof(PageInfoModel.Address)""
                    IsSelectedField=""@nameof(PageInfoModel.IsCurrent)""
                    ClassField=""@nameof(PageInfoModel.HtmlClass)""
                    StyleField=""@nameof(PageInfoModel.HtmlStyle)""
                    SelectedItemStyle=""color:red;background:lightgreen"" />
</div>
";
    private readonly string example4BreadcrumbOptionHTMLCode = @"
<style>
    .custom-item {
        color: red;
        margin: 2px 5px;
        border-radius: 2px;
        background: limegreen;

        &:hover {
            background: greenyellow;
        }
    }

    .custom-selected-item {
        color: red;
        margin: 2px 5px;
        border-radius: 2px;
        background: mediumspringgreen;

        &:hover {
            background: greenyellow;
        }
    }
</style>

<div>
    <BitLabel>Options Class</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" Class=""custom-item"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" Class=""custom-item"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" Class=""custom-item"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" Class=""custom-item"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
<div>
    <BitLabel>Options Style</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" Style=""color:red;background:greenyellow"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" Style=""color:red;background:greenyellow"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" Style=""color:red;background:greenyellow"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" Style=""color:red;background:greenyellow"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
<div>
    <BitLabel>Selected Option Class</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" SelectedItemClass=""custom-selected-item"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
<div>
    <BitLabel>Selected Option Style</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" SelectedItemStyle=""color:red; background:lightgreen;"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
";

    private readonly string example5BreadcrumbItemHTMLCode = @"
<BitBreadcrumb Items=""@BreadcrumbItemsWithControlled""
                MaxDisplayedItems=""3""
                OverflowIndex=""2""
                OnItemClick=""(BitBreadcrumbItem item) => HandleOnItemClick(item)""
                SelectedItemStyle=""color:red;background:lightgreen"" />
";
    private readonly string example5CustomItemHTMLCode = @"
<BitBreadcrumb Items=""CustomBreadcrumbItemsWithControlled""
                TextField=""@nameof(PageInfoModel.Name)""
                HrefField=""@nameof(PageInfoModel.Address)""
                IsSelectedField=""@nameof(PageInfoModel.IsCurrent)""
                ClassField=""@nameof(PageInfoModel.HtmlClass)""
                StyleField=""@nameof(PageInfoModel.HtmlStyle)""
                MaxDisplayedItems=""3""
                OverflowIndex=""2""
                OnItemClick=""(PageInfoModel item) => HandleOnItemClick(item)""
                SelectedItemStyle=""color:red;background:lightgreen"" />
";
    private readonly string example5BreadcrumbOptionHTMLCode = @"
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"" SelectedItemStyle=""color:red; background:lightgreen;"">
    <BitBreadcrumbOption Text=""Option 1"" IsSelected=""@(SelectedOptionNumber == 1)"" OnClick=""() => SelectedOptionNumber = 1"" />
    <BitBreadcrumbOption Text=""Option 2"" IsSelected=""@(SelectedOptionNumber == 2)"" OnClick=""() => SelectedOptionNumber = 2"" />
    <BitBreadcrumbOption Text=""Option 3"" IsSelected=""@(SelectedOptionNumber == 3)"" OnClick=""() => SelectedOptionNumber = 3"" />
    <BitBreadcrumbOption Text=""Option 4"" IsSelected=""@(SelectedOptionNumber == 4)"" OnClick=""() => SelectedOptionNumber = 4"" />
    <BitBreadcrumbOption Text=""Option 5"" IsSelected=""@(SelectedOptionNumber == 5)"" OnClick=""() => SelectedOptionNumber = 5"" />
    <BitBreadcrumbOption Text=""Option 6"" IsSelected=""@(SelectedOptionNumber == 6)"" OnClick=""() => SelectedOptionNumber = 6"" />
</BitBreadcrumb>
";

    private readonly string example6BreadcrumbItemHTMLCode = @"
<div>
    <BitBreadcrumb Items=""@BreadcrumbItemsWithCustomized""
                    MaxDisplayedItems=""@MaxDisplayedItems""
                    OverflowIndex=""@OverflowIndex""
                    OnItemClick=""(BitBreadcrumbItem item) => HandleOnCustomizedItemClick(item)"" />
</div>
<div class=""operators"">
    <div>
        <BitButton OnClick=""AddBreadcrumbItem"">Add Item</BitButton>
        <BitButton OnClick=""RemoveBreadcrumbItem"">Remove Item</BitButton>
    </div>
    <div>
        <BitNumericTextField @bind-Value=""MaxDisplayedItems"" Step=""@NumericTextFieldStep"" Label=""MaxDisplayedItems"" ShowArrows=""true"" />
        <BitNumericTextField @bind-Value=""OverflowIndex"" Step=""@NumericTextFieldStep"" Label=""OverflowIndex"" ShowArrows=""true"" />
    </div>
</div>
";
    private readonly string example6CustomItemHTMLCode = @"
<div>
    <BitBreadcrumb Items=""CustomBreadcrumbItemsWithCustomized""
                    TextField=""@nameof(PageInfoModel.Name)""
                    HrefField=""@nameof(PageInfoModel.Address)""
                    IsSelectedField=""@nameof(PageInfoModel.IsCurrent)""
                    ClassField=""@nameof(PageInfoModel.HtmlClass)""
                    StyleField=""@nameof(PageInfoModel.HtmlStyle)""
                    MaxDisplayedItems=""@MaxDisplayedItems""
                    OverflowIndex=""@OverflowIndex""
                    OnItemClick=""(PageInfoModel item) => HandleOnCustomizedItemClick(item)"" />
</div>
<div class=""operators"">
    <div>
        <BitButton OnClick=""AddCustomItem"">Add Item</BitButton>
        <BitButton OnClick=""RemoveCustomItem"">Remove Item</BitButton>
    </div>
    <div>
        <BitNumericTextField @bind-Value=""MaxDisplayedItems"" Step=""@NumericTextFieldStep"" Label=""MaxDisplayedItems"" ShowArrows=""true"" />
        <BitNumericTextField @bind-Value=""OverflowIndex"" Step=""@NumericTextFieldStep"" Label=""OverflowIndex"" ShowArrows=""true"" />
    </div>
</div>
";
    private readonly string example6BreadcrumbOptionHTMLCode = @"
<div>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""@MaxDisplayedItems"" OverflowIndex=""@OverflowIndex"">
        @for (int i = 0; i < ItemsCount; i++)
        {
            int index = i + 1;
            <BitBreadcrumbOption Text=""@($""Option {index}"")"" IsSelected=""@(CustomizedSelectedOptionNumber == index)"" OnClick=""() => CustomizedSelectedOptionNumber = index"" />
        }
    </BitBreadcrumb>
</div>
<div class=""operators"">
    <div>
        <BitButton OnClick=""() => ItemsCount++"">Add Option</BitButton>
        <BitButton OnClick=""() => ItemsCount--"">Remove Option</BitButton>
    </div>
    <div>
        <BitNumericTextField @bind-Value=""MaxDisplayedItems"" Step=""@NumericTextFieldStep"" Label=""MaxDisplayedOption"" ShowArrows=""true"" />
        <BitNumericTextField @bind-Value=""OverflowIndex"" Step=""@NumericTextFieldStep"" Label=""OverflowIndex"" ShowArrows=""true"" />
    </div>
</div>
";

    private readonly string example1BreadcrumbItemCSharpCode = @"
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
    private readonly string example1CustomItemCSharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfoModel> CustomBreadcrumbItems = new()
{
    new() { Name = ""Folder 1"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 2"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 3"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 4"", Address = ""/components/breadcrumb"", IsCurrent = true }
};

private readonly List<PageInfoModel> CustomBreadcrumbItemsDisabled = new()
{
    new() { Name = ""Folder 1"", Address = ""/components/breadcrumb"", IsEnabled = false },
    new() { Name = ""Folder 2"", Address = ""/components/breadcrumb"", IsEnabled = false },
    new() { Name = ""Folder 3"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 4"", Address = ""/components/breadcrumb"", IsCurrent = true }
};
";

    private readonly string example2BreadcrumbItemCSharpCode = @"
private List<BitBreadcrumbItem> BreadcrumbItems { get; set; } = new()
{
    new() { Text = ""Folder 1"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 2"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 3"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 4"", Href = ""/components/breadcrumb"", IsSelected = true }
};
";
    private readonly string example2CustomItemCSharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfoModel> CustomBreadcrumbItems = new()
{
    new() { Name = ""Folder 1"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 2"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 3"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 4"", Address = ""/components/breadcrumb"", IsCurrent = true }
};
";

    private readonly string example3BreadcrumbItemCSharpCode = @"
private List<BitBreadcrumbItem> BreadcrumbItems { get; set; } = new()
{
    new() { Text = ""Folder 1"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 2"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 3"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 4"", Href = ""/components/breadcrumb"", IsSelected = true }
};
";
    private readonly string example3CustomItemCSharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfoModel> CustomBreadcrumbItems = new()
{
    new() { Name = ""Folder 1"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 2"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 3"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 4"", Address = ""/components/breadcrumb"", IsCurrent = true }
};
";

    private readonly string example4BreadcrumbItemCSharpCode = @"
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
    private readonly string example4CustomItemCSharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfoModel> CustomBreadcrumbItems = new()
{
    new() { Name = ""Folder 1"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 2"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 3"", Address = ""/components/breadcrumb"" },
    new() { Name = ""Folder 4"", Address = ""/components/breadcrumb"", IsCurrent = true }
};

private readonly List<PageInfoModel> CustomBreadcrumbItemsWithClass = new()
{
    new() { Name = ""Folder 1"", Address = ""/components/breadcrumb"", HtmlClass = ""custom-item"" },
    new() { Name = ""Folder 2"", Address = ""/components/breadcrumb"", HtmlClass = ""custom-item"" },
    new() { Name = ""Folder 3"", Address = ""/components/breadcrumb"", HtmlClass = ""custom-item"" },
    new() { Name = ""Folder 4"", Address = ""/components/breadcrumb"", HtmlClass = ""custom-item"", IsCurrent = true }
};

private readonly List<PageInfoModel> CustomBreadcrumbItemsWithStyle = new()
{
    new() { Name = ""Folder 1"", Address = ""/components/breadcrumb"", HtmlStyle = ""color:red;background:greenyellow"" },
    new() { Name = ""Folder 2"", Address = ""/components/breadcrumb"", HtmlStyle = ""color:red;background:greenyellow"" },
    new() { Name = ""Folder 3"", Address = ""/components/breadcrumb"", HtmlStyle = ""color:red;background:greenyellow"" },
    new() { Name = ""Folder 4"", Address = ""/components/breadcrumb"", HtmlStyle = ""color:red;background:greenyellow"", IsCurrent = true }
};
";

    private readonly string example5BreadcrumbItemCSharpCode = @"
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
    private readonly string example5CustomItemCSharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfoModel> CustomBreadcrumbItemsWithControlled = new()
{
    new() { Name = ""Folder 1"" },
    new() { Name = ""Folder 2"" },
    new() { Name = ""Folder 3"" },
    new() { Name = ""Folder 4"" },
    new() { Name = ""Folder 5"" },
    new() { Name = ""Folder 6"", IsCurrent = true }
};

private void HandleOnItemClick(PageInfoModel item)
{
    BreadcrumbItemsWithControlled.FirstOrDefault(i => i.IsSelected).IsSelected = false;
    item.IsCurrent = true;
}
";
    private readonly string example5BreadcrumbOptionCSharpCode = @"
private int SelectedOptionNumber = 6;
";

    private readonly string example6BreadcrumbItemCSharpCode = @"
private int ItemsCount = 4;
private uint OverflowIndex = 2;
private uint MaxDisplayedItems = 3;
private uint NumericTextFieldStep = 1;

private List<BitBreadcrumbItem> BreadcrumbItemsWithCustomized { get; set; } = new()
{
    new() { Text = ""Folder 1"" },
    new() { Text = ""Folder 2"" },
    new() { Text = ""Folder 3"" },
    new() { Text = ""Folder 4"", IsSelected = true }
};

private void AddBreadcrumbItem()
{
    ItemsCount++;
    BreadcrumbItemsWithCustomized.Add(new BitBreadcrumbItem()
    {
        Text = $""Folder {ItemsCount}""
    });
}

private void RemoveBreadcrumbItem()
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

private void HandleOnCustomizedItemClick(BitBreadcrumbItem item)
{
    BreadcrumbItemsWithCustomized.FirstOrDefault(i => i.IsSelected).IsSelected = false;
    item.IsSelected = true;
}
";
    private readonly string example6CustomItemCSharpCode = @"
private int ItemsCount = 4;
private uint OverflowIndex = 2;
private uint MaxDisplayedItems = 3;
private uint NumericTextFieldStep = 1;

public class PageInfoModel
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfoModel> CustomBreadcrumbItemsWithCustomized = new()
{
    new() { Name = ""Folder 1"" },
    new() { Name = ""Folder 2"" },
    new() { Name = ""Folder 3"" },
    new() { Name = ""Folder 4"", IsCurrent = true }
};

private void HandleOnCustomizedItemClick(PageInfoModel item)
{
    BreadcrumbItemsWithCustomized.FirstOrDefault(i => i.IsSelected).IsSelected = false;
    item.IsCurrent = true;
}

private void AddCustomItem()
{
    ItemsCount++;
    BreadcrumbItemsWithCustomized.Add(new BitBreadcrumbItem()
    {
        Text = $""Folder {ItemsCount}""
    });
}

private void RemoveCustomItem()
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
";
    private readonly string example6BreadcrumbOptionCSharpCode = @"
private int ItemsCount = 4;
private uint OverflowIndex = 2;
private uint MaxDisplayedItems = 3;
private uint NumericTextFieldStep = 1;
private int CustomizedSelectedOptionNumber = 4;


";
}
