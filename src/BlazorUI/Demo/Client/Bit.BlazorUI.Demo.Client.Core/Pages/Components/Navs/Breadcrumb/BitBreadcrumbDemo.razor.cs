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
            Name = "DividerIconTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template content to render divider icon."
        },
        new()
        {
            Name = "Items",
            Type = "IList<TItem>",
            DefaultValue = "[]",
            Description = "Collection of BreadLists to render."
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template content to render each item."
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
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            Description = "Callback for when the breadcrumb item clicked."
        },
        new()
        {
            Name = "Options",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of the ChildContent."
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
            Name = "OverflowIconTemplate",
            Type = "RenderFragment?",
            DefaultValue= "null",
            Description = "The custom template content to render each overflow icon."
        },
        new()
        {
            Name = "OverflowTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue= "null",
            Description = "The custom template content to render each item in overflow list."
        },
        new()
        {
            Name = "ReversedIcon",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the positions of the icon and the item text of the item content."
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
                   Name = "IconName",
                   Type = "string?",
                   Description = "Name of an icon to render next to the item text.",
               },
               new()
               {
                   Name = "ReversedIcon",
                   Type = "bool?",
                   Description = "Reverses the positions of the icon and the item text of the item content.",
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
               },
               new()
               {
                   Name = "OverflowTemplate",
                   Type = "RenderFragment<BitBreadcrumbItem>?",
                   Description = "The custom template for the item in overflow list.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitBreadcrumbItem>?",
                   Description = "The custom template for the item.",
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
                   Name = "IconName",
                   Type = "string?",
                   Description = "Name of an icon to render next to the item text.",
               },
               new()
               {
                   Name = "ReversedIcon",
                   Type = "bool?",
                   Description = "Reverses the positions of the icon and the item text of the item content.",
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
               },
               new()
               {
                   Name = "OverflowTemplate",
                   Type = "RenderFragment<BitBreadcrumbItem>?",
                   Description = "The custom template for the item in overflow list.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitBreadcrumbItem>?",
                   Description = "The custom template for the item.",
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
                   Name = "IconName",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.IconName))",
                   Description = "The IconName field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "ReversedIcon",
                   Type = "BitNameSelectorPair<TItem, bool?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.ReversedIcon))",
                   Description = "The ReversedIcon field name and selector of the custom input class.",
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
               },
               new()
               {
                   Name = "OverflowTemplate",
                   Type = "BitNameSelectorPair<TItem, RenderFragment<TItem>?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.OverflowTemplate))",
                   Description = "The OverflowTemplate field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Template",
                   Type = "BitNameSelectorPair<TItem, RenderFragment<TItem>?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.Template))",
                   Description = "The Template field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
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
}
