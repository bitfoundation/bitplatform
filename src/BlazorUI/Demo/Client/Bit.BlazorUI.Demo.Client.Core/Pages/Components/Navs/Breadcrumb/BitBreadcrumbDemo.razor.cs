﻿namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public partial class BitBreadcrumbDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the BitBreadcrumb, that are BitBreadOption components."
        },
        new()
        {
            Name = "DividerIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The divider icon name."
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
            Name = "MaxDisplayedItems",
            Type = "uint",
            DefaultValue = "0",
            Description = "The maximum number of breadcrumbs to display before coalescing. If not specified, all breadcrumbs will be rendered."
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitBreadcrumbNameSelectors<TItem>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties.",
            LinkType = LinkType.Link,
            Href = "#name-selectors"
        },
        new()
        {
            Name = "OverflowAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "Aria label for the overflow button."
        },
        new()
        {
            Name = "OverflowIndex",
            Type = "uint",
            DefaultValue = "0",
            Description = "Optional index where overflow items will be collapsed."
        },
        new()
        {
            Name = "OverflowIconName",
            Type = "string",
            DefaultValue= "More",
            Description = "The overflow icon name."
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
            DefaultValue = "null",
            Description = "The CSS class attribute for the selected item."
        },
        new()
        {
            Name = "SelectedItemStyle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The style attribute for selected item."
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "bit-breadcrumb-item",
            Title = "BitBreadcrumbItem",
            Parameters =
            [
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   Description = "A unique value to use as a key of the breadcrumb item.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   Description = "Text to display in the breadcrumb item.",
               },
               new()
               {
                   Name = "Href",
                   Type = "string?",
                   Description = "URL to navigate to when the breadcrumb item is clicked. If provided, the breadcrumb will be rendered as a link.",
               },
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   Description = "CSS class attribute for breadcrumb item.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   Description = "Style attribute for breadcrumb item.",
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   Description = "Display the item as the selected item.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether an item is enabled or not.",
               },
               new()
               {
                   Name = "OnClick",
                   Type = "Action<BitBreadcrumbItem>?",
                   Description = "Click event handler of the breadcrumb item.",
               }
            ]
        },
        new()
        {
            Id = "bit-breadcrumb-option",
            Title = "BitBreadcrumbOption",
            Parameters =
            [
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   Description = "A unique value to use as a key of the breadcrumb option.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   Description = "Text to display in the breadcrumb option.",
               },
               new()
               {
                   Name = "Href",
                   Type = "string?",
                   Description = "URL to navigate to when the breadcrumb option is clicked. If provided, the breadcrumb will be rendered as a link.",
               },
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   Description = "CSS class attribute for breadcrumb option.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   Description = "Style attribute for breadcrumb option.",
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   Description = "Display the breadcrumb option as the selected option.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether an option is enabled or not.",
               },
               new()
               {
                   Name = "OnClick",
                   Type = "EventCallback<BitBreadcrumbOption>",
                   Description = "Click event handler of the breadcrumb option.",
               }
            ]
        },
        new()
        {
            Id = "name-selectors",
            Title = "BitBreadcrumbNameSelectors<TItem>",
            Parameters =
            [
               new()
               {
                   Name = "Key",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.Key))",
                   Description = "The Id field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Text",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.Text))",
                   Description = "The Text field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Href",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.Href))",
                   Description = "The Href field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Class",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.Class))",
                   Description = "The CSS Class field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Style",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.Style))",
                   Description = "The CSS Style field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "BitNameSelectorPair<TItem, bool>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.IsSelected))",
                   Description = "The IsSelected field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "BitNameSelectorPair<TItem, bool>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.IsEnabled))",
                   Description = "The IsEnabled field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "OnClick",
                   Type = "Action<TItem>?",
                   Description = "Click event handler of the item.",
               }
            ],
        },
        new()
        {
            Id = "name-selector-pair",
            Title = "BitNameSelectorPair<TItem, TProp>",
            Parameters =
            [
               new()
               {
                   Name = "Name",
                   Type = "string",
                   Description = "Custom class property name."
               },
               new()
               {
                   Name = "Selector",
                   Type = "Func<TItem, TProp?>?",
                   Description = "Custom class property selector."
               }
            ]
        },
    ];



    private uint OverflowIndex = 2;
    private uint MaxDisplayedItems = 3;
    private uint NumberFieldStep = 1;
    private int ItemsCount = 4;
    private int SelectedOptionNumber = 6;
    private int CustomizedSelectedOptionNumber = 4;



    private readonly List<BitBreadcrumbItem> BreadcrumbItems =
    [
        new() { Text = "Folder 1", Href = "/components/breadcrumb" },
        new() { Text = "Folder 2", Href = "/components/breadcrumb" },
        new() { Text = "Folder 3", Href = "/components/breadcrumb" },
        new() { Text = "Folder 4", Href = "/components/breadcrumb", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsDisabled =
    [
        new() { Text = "Folder 1", Href = "/components/breadcrumb", IsEnabled = false },
        new() { Text = "Folder 2", Href = "/components/breadcrumb", IsEnabled = false },
        new() { Text = "Folder 3", Href = "/components/breadcrumb" },
        new() { Text = "Folder 4", Href = "/components/breadcrumb", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithClass =
    [
        new() { Text = "Folder 1", Href = "/components/breadcrumb", Class = "custom-item" },
        new() { Text = "Folder 2", Href = "/components/breadcrumb", Class = "custom-item" },
        new() { Text = "Folder 3", Href = "/components/breadcrumb", Class = "custom-item" },
        new() { Text = "Folder 4", Href = "/components/breadcrumb", Class = "custom-item", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithStyle =
    [
        new() { Text = "Folder 1", Href = "/components/breadcrumb", Style = "color:red;background:greenyellow" },
        new() { Text = "Folder 2", Href = "/components/breadcrumb", Style = "color:red;background:greenyellow" },
        new() { Text = "Folder 3", Href = "/components/breadcrumb", Style = "color:red;background:greenyellow" },
        new() { Text = "Folder 4", Href = "/components/breadcrumb", Style = "color:red;background:greenyellow", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithControlled =
    [
        new() { Text = "Folder 1" },
        new() { Text = "Folder 2" },
        new() { Text = "Folder 3" },
        new() { Text = "Folder 4" },
        new() { Text = "Folder 5" },
        new() { Text = "Folder 6", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> BreadcrumbItemsWithCustomized =
    [
        new() { Text = "Folder 1" },
        new() { Text = "Folder 2" },
        new() { Text = "Folder 3" },
        new() { Text = "Folder 4", IsSelected = true }
    ];

    private readonly List<BitBreadcrumbItem> RtlBreadcrumbItems =
    [
        new() { Text = "پوشه اول" },
        new() { Text = "پوشه دوم", IsSelected = true },
        new() { Text = "پوشه سوم" },
        new() { Text = "پوشه چهارم" },
        new() { Text = "پوشه پنجم" },
        new() { Text = "پوشه ششم" },
    ];



    private readonly List<PageInfoModel> CustomBreadcrumbItems =
    [
        new() { Name = "Folder 1", Address = "/components/breadcrumb" },
        new() { Name = "Folder 2", Address = "/components/breadcrumb" },
        new() { Name = "Folder 3", Address = "/components/breadcrumb" },
        new() { Name = "Folder 4", Address = "/components/breadcrumb", IsCurrent = true }
    ];

    private readonly List<PageInfoModel> CustomBreadcrumbItemsDisabled =
    [
        new() { Name = "Folder 1", Address = "/components/breadcrumb", IsEnabled = false },
        new() { Name = "Folder 2", Address = "/components/breadcrumb", IsEnabled = false },
        new() { Name = "Folder 3", Address = "/components/breadcrumb" },
        new() { Name = "Folder 4", Address = "/components/breadcrumb", IsCurrent = true }
    ];

    private readonly List<PageInfoModel> CustomBreadcrumbItemsWithClass =
    [
        new() { Name = "Folder 1", Address = "/components/breadcrumb", HtmlClass = "custom-item" },
        new() { Name = "Folder 2", Address = "/components/breadcrumb", HtmlClass = "custom-item" },
        new() { Name = "Folder 3", Address = "/components/breadcrumb", HtmlClass = "custom-item" },
        new() { Name = "Folder 4", Address = "/components/breadcrumb", HtmlClass = "custom-item", IsCurrent = true }
    ];

    private readonly List<PageInfoModel> CustomBreadcrumbItemsWithStyle =
    [
        new() { Name = "Folder 1", Address = "/components/breadcrumb", HtmlStyle = "color:red;background:greenyellow" },
        new() { Name = "Folder 2", Address = "/components/breadcrumb", HtmlStyle = "color:red;background:greenyellow" },
        new() { Name = "Folder 3", Address = "/components/breadcrumb", HtmlStyle = "color:red;background:greenyellow" },
        new() { Name = "Folder 4", Address = "/components/breadcrumb", HtmlStyle = "color:red;background:greenyellow", IsCurrent = true }
    ];

    private readonly List<PageInfoModel> CustomBreadcrumbItemsWithControlled =
    [
        new() { Name = "Folder 1" },
        new() { Name = "Folder 2" },
        new() { Name = "Folder 3" },
        new() { Name = "Folder 4" },
        new() { Name = "Folder 5" },
        new() { Name = "Folder 6", IsCurrent = true }
    ];

    private readonly List<PageInfoModel> CustomBreadcrumbItemsWithCustomized =
    [
        new() { Name = "Folder 1" },
        new() { Name = "Folder 2" },
        new() { Name = "Folder 3" },
        new() { Name = "Folder 4", IsCurrent = true }
    ];

    private readonly List<PageInfoModel> RtlCustomBreadcrumbItems =
    [
        new() { Name = "پوشه اول" },
        new() { Name = "پوشه دوم", IsCurrent = true },
        new() { Name = "پوشه سوم" },
        new() { Name = "پوشه چهارم" },
        new() { Name = "پوشه پنجم" },
        new() { Name = "پوشه ششم" },
    ];

    private BitBreadcrumbNameSelectors<PageInfoModel> nameSelectors = new()
    {
        Text = { Selector = c => c.Name },
        Href = { Selector = c => c.Address },
        IsSelected = { Selector = c => c.IsCurrent },
        Class = { Selector = c => c.HtmlClass },
        Style = { Selector = c => c.HtmlStyle }
    };



    private void HandleOnItemClick(BitBreadcrumbItem item)
    {
        BreadcrumbItemsWithControlled.First(i => i.IsSelected).IsSelected = false;
        item.IsSelected = true;
    }
    private void HandleOnCustomizedItemClick(BitBreadcrumbItem item)
    {
        BreadcrumbItemsWithCustomized.First(i => i.IsSelected).IsSelected = false;
        item.IsSelected = true;
    }

    private void HandleOnCustomClick(PageInfoModel model)
    {
        CustomBreadcrumbItemsWithControlled.First(i => i.IsCurrent).IsCurrent = false;
        model.IsCurrent = true;
    }
    private void HandleOnCustomizedCustomClick(PageInfoModel model)
    {
        CustomBreadcrumbItemsWithCustomized.First(i => i.IsCurrent).IsCurrent = false;
        model.IsCurrent = true;
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
        CustomBreadcrumbItemsWithCustomized.Add(new PageInfoModel()
        {
            Name = $"Folder {ItemsCount}"
        });
    }
    private void RemoveCustomItem()
    {
        if (CustomBreadcrumbItemsWithCustomized.Count > 1)
        {
            ItemsCount--;

            var item = CustomBreadcrumbItemsWithCustomized[^1];
            CustomBreadcrumbItemsWithCustomized.Remove(item);

            if (item.IsCurrent)
            {
                CustomBreadcrumbItemsWithCustomized[^1].IsCurrent = true;
            }
        }
    }



    private readonly string example1BreadcrumbItemRazorCode = @"
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
    private readonly string example1CustomItemRazorCode = @"
<div>
    <BitLabel>Basic</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors"" />
</div>
<div>
    <BitLabel>Disabled</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors"" />
</div>
<div>
    <BitLabel>Item Disabled</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItemsDisabled""
                   NameSelectors=""nameSelectors"" />
</div>
";
    private readonly string example1BreadcrumbOptionRazorCode = @"
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

    private readonly string example2BreadcrumbItemRazorCode = @"
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
    private readonly string example2CustomItemRazorCode = @"
<div>
    <BitLabel>MaxDisplayedItems (1)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   MaxDisplayedItems=""1"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (2)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   MaxDisplayedItems=""2"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   MaxDisplayedItems=""3"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (0)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   MaxDisplayedItems=""3""
                   OverflowIndex=""0"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (1)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   MaxDisplayedItems=""3""
                   OverflowIndex=""1"" />
</div>
<div>
    <BitLabel>MaxDisplayedItems (3), OverflowIndex (2)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   MaxDisplayedItems=""3""
                   OverflowIndex=""2"" />
</div>
";
    private readonly string example2BreadcrumbOptionRazorCode = @"
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

    private readonly string example3BreadcrumbItemRazorCode = @"
<div>
    <BitLabel>BitIconName (ChevronDown)</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems""
                   MaxDisplayedItems=""3""
                   OverflowIndex=""2""
                   OverflowIcon=""@BitIconName.ChevronDown"" />
</div>
<div>
    <BitLabel>BitIconName (CollapseMenu)</BitLabel>
    <BitBreadcrumb Items=""BreadcrumbItems""
                   MaxDisplayedItems=""3""
                   OverflowIndex=""2""
                   OverflowIcon=""@BitIconName.CollapseMenu"" />
</div>
";
    private readonly string example3CustomItemRazorCode = @"
<div>
    <BitLabel>BitIconName (ChevronDown)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   MaxDisplayedItems=""3""
                   OverflowIndex=""2""
                   OverflowIcon=""@BitIconName.ChevronDown"" />
</div>
<div>
    <BitLabel>BitIconName (CollapseMenu)</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   MaxDisplayedItems=""3""
                   OverflowIndex=""2""
                   OverflowIcon=""@BitIconName.CollapseMenu"" />
</div>
";
    private readonly string example3BreadcrumbOptionRazorCode = @"
<div>
    <BitLabel>BitIconName (ChevronDown)</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"" OverflowIcon=""@BitIconName.ChevronDown"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>

<div>
    <BitLabel>BitIconName (CollapseMenu)</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"" OverflowIcon=""@BitIconName.CollapseMenu"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
";

    private readonly string example4BreadcrumbItemRazorCode = @"
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
    private readonly string example4CustomItemRazorCode = @"
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
                   NameSelectors=""nameSelectors"" />
</div>
<div>
    <BitLabel>Items Style</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItemsWithStyle""
                   NameSelectors=""nameSelectors"" />
</div>
<div>
    <BitLabel>Selected Item Class</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   SelectedItemClass=""custom-selected-item"" />
</div>
<div>
    <BitLabel>Selected Item Style</BitLabel>
    <BitBreadcrumb Items=""CustomBreadcrumbItems""
                   NameSelectors=""nameSelectors""
                   SelectedItemStyle=""color:red;background:lightgreen"" />
</div>
";
    private readonly string example4BreadcrumbOptionRazorCode = @"
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

    private readonly string example5BreadcrumbItemRazorCode = @"
<BitBreadcrumb Items=""@BreadcrumbItemsWithControlled""
               MaxDisplayedItems=""3""
               OverflowIndex=""2""
               OnItemClick=""(BitBreadcrumbItem item) => HandleOnItemClick(item)""
               SelectedItemStyle=""color:red;background:lightgreen"" />
";
    private readonly string example5CustomItemRazorCode = @"
<BitBreadcrumb Items=""CustomBreadcrumbItemsWithControlled""
               NameSelectors=""nameSelectors""
               MaxDisplayedItems=""3""
               OverflowIndex=""2""
               OnItemClick=""(PageInfoModel model) => HandleOnCustomClick(model)""
               SelectedItemStyle=""color:red;background:lightgreen"" />
";
    private readonly string example5BreadcrumbOptionRazorCode = @"
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"" SelectedItemStyle=""color:red; background:lightgreen;"">
    <BitBreadcrumbOption Text=""Option 1"" IsSelected=""@(SelectedOptionNumber == 1)"" OnClick=""() => SelectedOptionNumber = 1"" />
    <BitBreadcrumbOption Text=""Option 2"" IsSelected=""@(SelectedOptionNumber == 2)"" OnClick=""() => SelectedOptionNumber = 2"" />
    <BitBreadcrumbOption Text=""Option 3"" IsSelected=""@(SelectedOptionNumber == 3)"" OnClick=""() => SelectedOptionNumber = 3"" />
    <BitBreadcrumbOption Text=""Option 4"" IsSelected=""@(SelectedOptionNumber == 4)"" OnClick=""() => SelectedOptionNumber = 4"" />
    <BitBreadcrumbOption Text=""Option 5"" IsSelected=""@(SelectedOptionNumber == 5)"" OnClick=""() => SelectedOptionNumber = 5"" />
    <BitBreadcrumbOption Text=""Option 6"" IsSelected=""@(SelectedOptionNumber == 6)"" OnClick=""() => SelectedOptionNumber = 6"" />
</BitBreadcrumb>
";

    private readonly string example6BreadcrumbItemRazorCode = @"
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
        <BitNumberField @bind-Value=""MaxDisplayedItems"" Step=""@NumberFieldStep"" Label=""MaxDisplayedItems"" ShowArrows=""true"" />
        <BitNumberField @bind-Value=""OverflowIndex"" Step=""@NumberFieldStep"" Label=""OverflowIndex"" ShowArrows=""true"" />
    </div>
</div>
";
    private readonly string example6CustomItemRazorCode = @"
<div>
    <BitBreadcrumb Items=""CustomBreadcrumbItemsWithCustomized""
                   NameSelectors=""nameSelectors""
                   MaxDisplayedItems=""@MaxDisplayedItems""
                   OverflowIndex=""@OverflowIndex""
                   OnItemClick=""(PageInfoModel model) => HandleOnCustomizedCustomClick(model)"" />
</div>
<div class=""operators"">
    <div>
        <BitButton OnClick=""AddCustomItem"">Add Item</BitButton>
        <BitButton OnClick=""RemoveCustomItem"">Remove Item</BitButton>
    </div>
    <div>
        <BitNumberField @bind-Value=""MaxDisplayedItems"" Step=""@NumberFieldStep"" Label=""MaxDisplayedItems"" ShowArrows=""true"" />
        <BitNumberField @bind-Value=""OverflowIndex"" Step=""@NumberFieldStep"" Label=""OverflowIndex"" ShowArrows=""true"" />
    </div>
</div>
";
    private readonly string example6BreadcrumbOptionRazorCode = @"
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
        <BitNumberField @bind-Value=""MaxDisplayedItems"" Step=""@NumberFieldStep"" Label=""MaxDisplayedOption"" ShowArrows=""true"" />
        <BitNumberField @bind-Value=""OverflowIndex"" Step=""@NumberFieldStep"" Label=""OverflowIndex"" ShowArrows=""true"" />
    </div>
</div>
";

    private readonly string example7BreadcrumbItemRazorCode = @"
<BitBreadcrumb Dir=""BitDir.Rtl"" Items=""RtlBreadcrumbItems"" MaxDisplayedItems=""3"" OverflowIndex=""2"" />
";
    private readonly string example7CustomItemRazorCode = @"
<BitBreadcrumb Dir=""BitDir.Rtl""
               OverflowIndex=""2""
               MaxDisplayedItems=""3""
               Items=""RtlCustomBreadcrumbItems""
               NameSelectors=""nameSelectors"" />
";
    private readonly string example7BreadcrumbOptionRazorCode = @"
<BitBreadcrumb Dir=""BitDir.Rtl"" TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"">
    <BitBreadcrumbOption Text=""پوشه اول"" />
    <BitBreadcrumbOption Text=""پوشه دوم"" IsSelected=""true"" />
    <BitBreadcrumbOption Text=""پوشه سوم"" />
    <BitBreadcrumbOption Text=""پوشه چهارم"" />
    <BitBreadcrumbOption Text=""پوشه پنجم"" />
    <BitBreadcrumbOption Text=""پوشه ششم"" />
</BitBreadcrumb>";



    private readonly string example1BreadcrumbItemCsharpCode = @"
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
    private readonly string example1CustomItemCsharpCode = @"
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

private BitBreadcrumbNameSelectors<PageInfoModel> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};
";

    private readonly string example2BreadcrumbItemCsharpCode = @"
private List<BitBreadcrumbItem> BreadcrumbItems { get; set; } = new()
{
    new() { Text = ""Folder 1"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 2"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 3"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 4"", Href = ""/components/breadcrumb"", IsSelected = true }
};
";
    private readonly string example2CustomItemCsharpCode = @"
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

private BitBreadcrumbNameSelectors<PageInfoModel> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};
";

    private readonly string example3BreadcrumbItemCsharpCode = @"
private List<BitBreadcrumbItem> BreadcrumbItems { get; set; } = new()
{
    new() { Text = ""Folder 1"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 2"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 3"", Href = ""/components/breadcrumb"" },
    new() { Text = ""Folder 4"", Href = ""/components/breadcrumb"", IsSelected = true }
};
";
    private readonly string example3CustomItemCsharpCode = @"
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

private BitBreadcrumbNameSelectors<PageInfoModel> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};
";

    private readonly string example4BreadcrumbItemCsharpCode = @"
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
    private readonly string example4CustomItemCsharpCode = @"
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

private BitBreadcrumbNameSelectors<PageInfoModel> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};
";

    private readonly string example5BreadcrumbItemCsharpCode = @"
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
    private readonly string example5CustomItemCsharpCode = @"
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

private BitBreadcrumbNameSelectors<PageInfoModel> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};

private void HandleOnCustomClick(PageInfoModel model)
{
    CustomBreadcrumbItemsWithControlled.First(i => i.IsCurrent).IsCurrent = false;
    model.IsCurrent = true;
}";

    private readonly string example5BreadcrumbOptionCsharpCode = @"
private int SelectedOptionNumber = 6;
";

    private readonly string example6BreadcrumbItemCsharpCode = @"
private int ItemsCount = 4;
private uint OverflowIndex = 2;
private uint MaxDisplayedItems = 3;
private uint NumberFieldStep = 1;

private List<BitBreadcrumbItem> BreadcrumbItemsWithCustomized { get; set; } = new()
{
    new() { Text = ""Folder 1"" },
    new() { Text = ""Folder 2"" },
    new() { Text = ""Folder 3"" },
    new() { Text = ""Folder 4"", IsSelected = true }
};

private void HandleOnCustomizedItemClick(BitBreadcrumbItem item)
{
    BreadcrumbItemsWithCustomized.FirstOrDefault(i => i.IsSelected).IsSelected = false;
    item.IsSelected = true;
}

private void AddCustomItem()
{
    ItemsCount++;
    CustomBreadcrumbItemsWithCustomized.Add(new PageInfoModel()
    {
        Name = $""Folder {ItemsCount}""
    });
}
private void RemoveCustomItem()
{
    if (CustomBreadcrumbItemsWithCustomized.Count > 1)
    {
        ItemsCount--;

        var item = CustomBreadcrumbItemsWithCustomized[^1];
        CustomBreadcrumbItemsWithCustomized.Remove(item);

        if (item.IsCurrent)
        {
            CustomBreadcrumbItemsWithCustomized[^1].IsCurrent = true;
        }
    }
}";
    private readonly string example6CustomItemCsharpCode = @"
private int ItemsCount = 4;
private uint OverflowIndex = 2;
private uint MaxDisplayedItems = 3;
private uint NumberFieldStep = 1;

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

private BitBreadcrumbNameSelectors<PageInfoModel> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};

private void HandleOnCustomizedCustomClick(PageInfoModel model)
{
    CustomBreadcrumbItemsWithCustomized.First(i => i.IsCurrent).IsCurrent = false;
    model.IsCurrent = true;
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
    private readonly string example6BreadcrumbOptionCsharpCode = @"
private int ItemsCount = 4;
private uint OverflowIndex = 2;
private uint MaxDisplayedItems = 3;
private uint NumberFieldStep = 1;
private int CustomizedSelectedOptionNumber = 4;
";

    private readonly string example7BreadcrumbItemCsharpCode = @"
private readonly List<BitBreadcrumbItem> RtlBreadcrumbItems = new()
{
    new() { Text = ""پوشه اول"" },
    new() { Text = ""پوشه دوم"", IsSelected = true },
    new() { Text = ""پوشه سوم"" },
    new() { Text = ""پوشه چهارم"" },
    new() { Text = ""پوشه پنجم"" },
    new() { Text = ""پوشه ششم"" },
};";
    private readonly string example7CustomItemCsharpCode = @"
public class PageInfoModel
{
    public string Name { get; set; }

    public string Address { get; set; }

    public string HtmlClass { get; set; }

    public string HtmlStyle { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}

private readonly List<PageInfoModel> RtlCustomBreadcrumbItems = new()
{
    new() { Name = ""پوشه اول"" },
    new() { Name = ""پوشه دوم"", IsCurrent = true },
    new() { Name = ""پوشه سوم"" },
    new() { Name = ""پوشه چهارم"" },
    new() { Name = ""پوشه پنجم"" },
    new() { Name = ""پوشه ششم"" },
};

private BitBreadcrumbNameSelectors<PageInfoModel> nameSelectors = new()
{
    Text = { Selector = c => c.Name },
    Href = { Selector = c => c.Address },
    IsSelected = { Selector = c => c.IsCurrent },
    Class = { Selector = c => c.HtmlClass },
    Style = { Selector = c => c.HtmlStyle }
};";
}
