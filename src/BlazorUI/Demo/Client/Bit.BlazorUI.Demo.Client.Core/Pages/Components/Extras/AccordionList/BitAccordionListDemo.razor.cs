namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class BitAccordionListDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Background",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the background of all the accordion items.",
        },
        new()
        {
            Name = "Border",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the border of all the accordion items.",
        },
        new()
        {
            Name = "BodyTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template to render the body (content) of each item.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the AccordionList, composed of BitAccordionListOption components.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitAccordionListClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the AccordionList.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "DefaultExpandedKey",
            Type = "string?",
            DefaultValue = "null",
            Description = "The default expanded key in single-expand mode (used when ExpandedKey is not set).",
        },
        new()
        {
            Name = "DefaultExpandedKeys",
            Type = "IEnumerable<string>?",
            DefaultValue = "null",
            Description = "The default expanded keys in multiple-expand mode (used when ExpandedKeys is not set).",
        },
        new()
        {
            Name = "ExpandedKey",
            Type = "string?",
            DefaultValue = "null",
            Description = "The expanded key in single-expand mode. (two-way bound)",
        },
        new()
        {
            Name = "ExpandedKeys",
            Type = "IEnumerable<string>?",
            DefaultValue = "null",
            Description = "The expanded keys in multiple-expand mode. (two-way bound)",
        },
        new()
        {
            Name = "ExpanderIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display as the expander of all items using custom CSS classes for external icon libraries. Can be overridden per item.",
        },
        new()
        {
            Name = "ExpanderIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon to display as the expander of all items from the built-in Fluent UI icons. Can be overridden per item.",
        },
        new()
        {
            Name = "Gap",
            Type = "int?",
            DefaultValue = "null",
            Description = "The space (gap) in pixels between the accordion items.",
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template to render the header of each item. Replaces the default Title/Description header.",
        },
        new()
        {
            Name = "Items",
            Type = "IEnumerable<TItem>",
            DefaultValue = "[]",
            Description = "The collection of items to render in the AccordionList.",
            LinkType = LinkType.Link,
            Href = "#accordion-list-item",
        },
        new()
        {
            Name = "Multiple",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the multiple-expand mode in which more than one item can be expanded at the same time.",
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitAccordionListNameSelectors<TItem>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties.",
            LinkType = LinkType.Link,
            Href = "#name-selectors",
        },
        new()
        {
            Name = "NoBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the default border of all the accordion items and gives a background color to their body.",
        },
        new()
        {
            Name = "OnCollapse",
            Type = "EventCallback<TItem>",
            Description = "The callback that is called when an item is collapsed.",
        },
        new()
        {
            Name = "OnExpand",
            Type = "EventCallback<TItem>",
            Description = "The callback that is called when an item is expanded.",
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            Description = "The callback that is called when the header of an item is clicked.",
        },
        new()
        {
            Name = "OnToggle",
            Type = "EventCallback<TItem>",
            Description = "The callback that is called when an item is toggled (expanded or collapsed).",
        },
        new()
        {
            Name = "Options",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of the ChildContent.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAccordionListClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the AccordionList.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "ExpandAll",
            Type = "Task",
            Description = "Expands all the items (only effective in multiple-expand mode).",
        },
        new()
        {
            Name = "CollapseAll",
            Type = "Task",
            Description = "Collapses all the expanded items.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "accordion-list-item",
            Title = "BitAccordionListItem",
            Description = "The class for the items of the BitAccordionList when using the Items parameter.",
            Parameters =
            [
                new() { Name = "Class", Type = "string?", DefaultValue = "null", Description = "The custom CSS classes of the item." },
                new() { Name = "Description", Type = "string?", DefaultValue = "null", Description = "A short description rendered in the header of the item." },
                new() { Name = "ExpanderIcon", Type = "BitIconInfo?", DefaultValue = "null", Description = "The icon to display as the expander using custom CSS classes for external icon libraries. Takes precedence over ExpanderIconName." },
                new() { Name = "ExpanderIconName", Type = "string?", DefaultValue = "null", Description = "The name of the icon to display as the expander from the built-in Fluent UI icons." },
                new() { Name = "Body", Type = "RenderFragment<BitAccordionListItem>?", DefaultValue = "null", Description = "The content (body) of the item that is shown when the item is expanded. The context value provides the item itself." },
                new() { Name = "HeaderTemplate", Type = "RenderFragment<BitAccordionListItem>?", DefaultValue = "null", Description = "The custom template for the header of the item. The context value provides the item itself." },
                new() { Name = "IsEnabled", Type = "bool", DefaultValue = "true", Description = "Whether or not the item is enabled." },
                new() { Name = "IsExpanded", Type = "bool", DefaultValue = "false", Description = "Determines whether the item is expanded. This value is also assigned by the component during interactions." },
                new() { Name = "Key", Type = "string?", DefaultValue = "null", Description = "A unique value to use as the key of the item." },
                new() { Name = "OnClick", Type = "Action<BitAccordionListItem>?", DefaultValue = "null", Description = "The click event handler of the header of the item." },
                new() { Name = "Style", Type = "string?", DefaultValue = "null", Description = "The custom value for the style attribute of the item." },
                new() { Name = "Title", Type = "string?", DefaultValue = "null", Description = "The title (header text) of the item." },
            ]
        },
        new()
        {
            Id = "accordion-list-option",
            Title = "BitAccordionListOption",
            Description = "The component for the items of the BitAccordionList when using the BitAccordionListOption components.",
            Parameters =
            [
                new() { Name = "Class", Type = "string?", DefaultValue = "null", Description = "The custom CSS classes of the option." },
                new() { Name = "Description", Type = "string?", DefaultValue = "null", Description = "A short description rendered in the header of the option." },
                new() { Name = "ExpanderIcon", Type = "BitIconInfo?", DefaultValue = "null", Description = "The icon to display as the expander using custom CSS classes for external icon libraries. Takes precedence over ExpanderIconName." },
                new() { Name = "ExpanderIconName", Type = "string?", DefaultValue = "null", Description = "The name of the icon to display as the expander from the built-in Fluent UI icons." },
                new() { Name = "Body", Type = "RenderFragment<BitAccordionListOption>?", DefaultValue = "null", Description = "The content (body) of the option that is shown when the option is expanded. The context value provides the option itself." },
                new() { Name = "ChildContent", Type = "RenderFragment?", DefaultValue = "null", Description = "Alias for the Body parameter (the default child content). Used for simple inline content." },
                new() { Name = "HeaderTemplate", Type = "RenderFragment<BitAccordionListOption>?", DefaultValue = "null", Description = "The custom template for the header of the option. The context value provides the option itself." },
                new() { Name = "IsEnabled", Type = "bool", DefaultValue = "true", Description = "Whether or not the option is enabled." },
                new() { Name = "IsExpanded", Type = "bool", DefaultValue = "false", Description = "Determines whether the option is initially expanded." },
                new() { Name = "Key", Type = "string?", DefaultValue = "null", Description = "A unique value to use as the key of the option." },
                new() { Name = "OnClick", Type = "EventCallback<BitAccordionListOption>", DefaultValue = "", Description = "The click event handler of the header of the option." },
                new() { Name = "Style", Type = "string?", DefaultValue = "null", Description = "The custom value for the style attribute of the option." },
                new() { Name = "Title", Type = "string?", DefaultValue = "null", Description = "The title (header text) of the option." },
            ]
        },
        new()
        {
            Id = "name-selectors",
            Title = "BitAccordionListNameSelectors",
            Description = "The names and selectors of the custom input type properties.",
            Parameters =
            [
                new() { Name = "Class", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitAccordionListItem.Class))", Description = "The CSS Class field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Description", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitAccordionListItem.Description))", Description = "Description field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "ExpanderIcon", Type = "BitNameSelectorPair<TItem, BitIconInfo?>", DefaultValue = "new(nameof(BitAccordionListItem.ExpanderIcon))", Description = "ExpanderIcon field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "ExpanderIconName", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitAccordionListItem.ExpanderIconName))", Description = "ExpanderIconName field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Body", Type = "BitNameSelectorPair<TItem, RenderFragment<TItem>?>", DefaultValue = "new(nameof(BitAccordionListItem.Body))", Description = "Body field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "HeaderTemplate", Type = "BitNameSelectorPair<TItem, RenderFragment<TItem>?>", DefaultValue = "new(nameof(BitAccordionListItem.HeaderTemplate))", Description = "HeaderTemplate field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "IsEnabled", Type = "BitNameSelectorPair<TItem, bool>", DefaultValue = "new(nameof(BitAccordionListItem.IsEnabled))", Description = "IsEnabled field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "IsExpanded", Type = "BitNameSelectorPair<TItem, bool>", DefaultValue = "new(nameof(BitAccordionListItem.IsExpanded))", Description = "IsExpanded field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Key", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitAccordionListItem.Key))", Description = "Key field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "OnClick", Type = "BitNameSelectorPair<TItem, Action<TItem>?>", DefaultValue = "new(nameof(BitAccordionListItem.OnClick))", Description = "OnClick field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Style", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitAccordionListItem.Style))", Description = "Style field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Title", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitAccordionListItem.Title))", Description = "Title field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
            ]
        },
        new()
        {
            Id = "name-selector-pair",
            Title = "BitNameSelectorPair",
            Parameters =
            [
                new() { Name = "Name", Type = "string", Description = "Custom class property name." },
                new() { Name = "Selector", Type = "Func<TItem, TProp?>?", Description = "Custom class property selector." }
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitAccordionListClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitAccordionList." },
                new() { Name = "Item", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each accordion item of the BitAccordionList." },
                new() { Name = "ItemExpanded", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the expanded state of each accordion item of the BitAccordionList." },
                new() { Name = "ItemHeader", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the header of each accordion item of the BitAccordionList." },
                new() { Name = "ItemHeaderContent", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the header content of each accordion item of the BitAccordionList." },
                new() { Name = "ItemTitle", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the title of each accordion item of the BitAccordionList." },
                new() { Name = "ItemDescription", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the description of each accordion item of the BitAccordionList." },
                new() { Name = "ItemExpanderIconWrapper", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the expander icon wrapper of each accordion item of the BitAccordionList." },
                new() { Name = "ItemExpanderIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the expander icon of each accordion item of the BitAccordionList." },
                new() { Name = "ItemExpandedIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the expander icon of each accordion item of the BitAccordionList in the expanded state." },
                new() { Name = "ItemContentContainer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the content container of each accordion item of the BitAccordionList." },
                new() { Name = "ItemContent", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the content of each accordion item of the BitAccordionList." },
            ]
        }
    ];
}
