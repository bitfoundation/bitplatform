namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class BitAccordionListDemo
{
    [CascadingParameter(Name = nameof(RenderForMcpClient))] public bool RenderForMcpClient { get; set; }

    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ActionsTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template to render beside the header of each item, outside of the toggle button and of the heading it sits in, so that it can hold its own interactive elements. Used when an item does not provide its own actions.",
        },
        new()
        {
            Name = "Background",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the background of all the accordion items.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "Border",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the border of all the accordion items.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "BodyTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template to render the body (content) of each item. Used when an item does not provide its own body.",
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
            Name = "Collapsible",
            Type = "bool",
            DefaultValue = "true",
            Description = "Allows the expanded item to be collapsed again from its own header. Setting it to false keeps one item open at all times: the header of the last expanded item reports itself as aria-disabled and no longer answers the pointer or the keyboard, while the public methods still drive the list.",
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
            Name = "ExpandedExpanderIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to show in place of the expander icon of all items while they are expanded, using custom CSS classes for external icon libraries. Setting it also turns the rotation of the expander icon off. Can be overridden per item.",
        },
        new()
        {
            Name = "ExpandedExpanderIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon, from the built-in Fluent UI icons, to show in place of the expander icon of all items while they are expanded. Can be overridden per item.",
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
            Name = "ExpanderIconPosition",
            Type = "BitIconPosition?",
            DefaultValue = "null",
            Description = "The side of the header the expander icon of all the items sits on. The default value is End.",
            LinkType = LinkType.Link,
            Href = "#icon-position-enum",
        },
        new()
        {
            Name = "ExpanderTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template to render in place of the expander icon of each item, leaving the rest of the header as it is. Used when an item does not provide its own expander template.",
        },
        new()
        {
            Name = "ExpandOnPrint",
            Type = "bool",
            DefaultValue = "false",
            Description = "Opens the panel of every item while the page is being printed, so that a collapsed section is not left out of the paper as a bare header.",
        },
        new()
        {
            Name = "EmptyContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content to render in place of the items when the list has none. A list built from Options or ChildContent only knows it is empty once its options have had their turn to register, so its empty content takes the render after the first; a list built from Items shows it right away.",
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
            Name = "HeadingLevel",
            Type = "int?",
            DefaultValue = "null",
            Description = "The heading level (aria-level) reported for the header of every item. The default value is 3, and the value is clamped to the 1..6 range.",
        },
        new()
        {
            Name = "HideExpanderIcon",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the expander icon from the header of all the items. Can be overridden per item.",
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
            Name = "LazyContent",
            Type = "bool",
            DefaultValue = "false",
            Description = "Delays the first render of the content of each item until it is expanded for the first time. The content stays in the DOM afterwards, so the state it holds survives a collapse.",
        },
        new()
        {
            Name = "MaxHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "The maximum height of the content of every item (any CSS length), beyond which the content scrolls inside the item instead of growing it.",
        },
        new()
        {
            Name = "MaxExpanded",
            Type = "int?",
            DefaultValue = "null",
            Description = "The greatest number of items that can be expanded at the same time in multiple-expand mode. Expanding one more closes the panel that has been open the longest, so nothing is ever turned away. A value below 1 is no limit at all, the cap applies to ExpandAll and to the default and bound keys as well, and it means nothing outside of Multiple.",
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
            Name = "Navigable",
            Type = "bool",
            DefaultValue = "true",
            Description = "Moves the focus between the headers of the items with the ArrowUp, ArrowDown, Home and End keys, in addition to the Tab key. The navigation wraps around at both ends of the list, skips the disabled items, and leaves the same keys pressed inside a panel to whatever the panel holds.",
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
            Name = "NoContentRegion",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the region role from the panel of every item, leaving it a plain container. The WAI-ARIA authoring practices ask for it beyond about six panels that can all be open at the same time.",
        },
        new()
        {
            Name = "NoExpanderRotation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the expander icon of every item still instead of turning it over when the item is expanded.",
        },
        new()
        {
            Name = "NoNavigationLoop",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the keyboard navigation of Navigable at the two ends of the list instead of wrapping it around from the last header to the first and back. The Home and End keys still reach both ends either way.",
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
            Name = "OnToggling",
            Type = "EventCallback<BitAccordionListToggleArgs<TItem>>",
            Description = "The callback invoked before an item expands or collapses, letting the change be cancelled. It is awaited, and nothing else toggles the list while it runs.",
            LinkType = LinkType.Link,
            Href = "#toggle-args",
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
            Name = "ReadOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Leaves every item where it is: the headers keep their colors and their place in the tab order and report themselves as aria-disabled, but they no longer answer the pointer or the keyboard. Can be overridden per item.",
        },
        new()
        {
            Name = "ScrollIntoViewOnExpand",
            Type = "bool",
            DefaultValue = "false",
            Description = "Brings the item that has just been expanded into view, moving it as little as the browser can - so nothing happens to one that is already in view - and instantly rather than smoothly for a reader who has asked for less motion. It covers every way a panel opens: a click, one of the public methods, and the bound keys.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of all the accordion items, which drives the padding of the headers and of the contents and the size of the titles. The default value is Medium.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
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
        new()
        {
            Name = "TitleTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template to render in place of the title of each item, leaving the rest of the header as it is. Used when an item does not provide its own title template.",
        },
        new()
        {
            Name = "TransitionDuration",
            Type = "int?",
            DefaultValue = "null",
            Description = "The duration of the expand/collapse transition of every item in milliseconds, overriding the duration the theme provides.",
        },
        new()
        {
            Name = "UnmountOnCollapse",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the content of an item from the DOM while it is collapsed, so that nothing it holds keeps running behind a closed header.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "ExpandAll",
            Type = "Task",
            Description = "Expands all the items (only effective in multiple-expand mode). Disabled items are left as they are, since their headers could not close again what would be opened for them.",
        },
        new()
        {
            Name = "CollapseAll",
            Type = "Task",
            Description = "Collapses all the expanded items, the disabled ones included, so that nothing is left open with no way of closing it.",
        },
        new()
        {
            Name = "Expand",
            Type = "Task",
            Description = "Expands the item with the provided key. In single-expand mode the currently expanded item is collapsed along the way. Not turned away by IsEnabled, ReadOnly or Collapsible.",
        },
        new()
        {
            Name = "Collapse",
            Type = "Task",
            Description = "Collapses the item with the provided key. Not turned away by IsEnabled, ReadOnly or Collapsible.",
        },
        new()
        {
            Name = "Toggle",
            Type = "Task",
            Description = "Expands the item with the provided key if it is collapsed and collapses it if it is expanded.",
        },
        new()
        {
            Name = "IsExpanded",
            Type = "bool",
            Description = "Reports whether the item with the provided key is currently expanded.",
        },
        new()
        {
            Name = "GetExpandedKeys",
            Type = "IReadOnlyList<string>",
            Description = "Returns the keys of the currently expanded items, in the order of the items of the list.",
        },
        new()
        {
            Name = "FocusItem",
            Type = "Task",
            Description = "Gives the focus to the header of the item with the provided key.",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "Task",
            Description = "Gives the focus to the header of the first item of the list that can take it.",
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
                new() { Name = "Actions", Type = "RenderFragment<BitAccordionListItem>?", DefaultValue = "null", Description = "The content rendered beside the header of the item, outside of the toggle button and of the heading it sits in. The context value provides the item itself." },
                new() { Name = "Class", Type = "string?", DefaultValue = "null", Description = "The custom CSS classes of the item." },
                new() { Name = "Description", Type = "string?", DefaultValue = "null", Description = "A short description rendered in the header of the item." },
                new() { Name = "ExpandedExpanderIcon", Type = "BitIconInfo?", DefaultValue = "null", Description = "The icon to show in place of the expander icon while the item is expanded, using custom CSS classes for external icon libraries." },
                new() { Name = "ExpandedExpanderIconName", Type = "string?", DefaultValue = "null", Description = "The name of the icon, from the built-in Fluent UI icons, to show in place of the expander icon while the item is expanded." },
                new() { Name = "ExpanderIcon", Type = "BitIconInfo?", DefaultValue = "null", Description = "The icon to display as the expander using custom CSS classes for external icon libraries. Takes precedence over ExpanderIconName." },
                new() { Name = "ExpanderIconName", Type = "string?", DefaultValue = "null", Description = "The name of the icon to display as the expander from the built-in Fluent UI icons." },
                new() { Name = "ExpanderTemplate", Type = "RenderFragment<BitAccordionListItem>?", DefaultValue = "null", Description = "The custom content to render in place of the expander icon of the item. The context value provides the item itself." },
                new() { Name = "Body", Type = "RenderFragment<BitAccordionListItem>?", DefaultValue = "null", Description = "The content (body) of the item that is shown when the item is expanded. The context value provides the item itself." },
                new() { Name = "HeaderAriaLabel", Type = "string?", DefaultValue = "null", Description = "The accessible label of the toggle button in the header of the item, for a header whose own content does not name it." },
                new() { Name = "HeaderTemplate", Type = "RenderFragment<BitAccordionListItem>?", DefaultValue = "null", Description = "The custom template for the header of the item. The context value provides the item itself." },
                new() { Name = "HideExpanderIcon", Type = "bool?", DefaultValue = "null", Description = "Removes the expander icon from the header of the item, overriding the value of the AccordionList." },
                new() { Name = "Icon", Type = "BitIconInfo?", DefaultValue = "null", Description = "The icon to display at the start of the header of the item using custom CSS classes for external icon libraries. Takes precedence over IconName." },
                new() { Name = "IconName", Type = "string?", DefaultValue = "null", Description = "The name of the icon to display at the start of the header of the item from the built-in Fluent UI icons." },
                new() { Name = "IsEnabled", Type = "bool", DefaultValue = "true", Description = "Whether or not the item is enabled." },
                new() { Name = "IsExpanded", Type = "bool", DefaultValue = "false", Description = "Determines whether the item is expanded. This value is also assigned by the component during interactions." },
                new() { Name = "Key", Type = "string?", DefaultValue = "null", Description = "A unique value to use as the key of the item. A key that is not given is generated from the position of the item." },
                new() { Name = "OnClick", Type = "Action<BitAccordionListItem>?", DefaultValue = "null", Description = "The click event handler of the header of the item." },
                new() { Name = "ReadOnly", Type = "bool?", DefaultValue = "null", Description = "Leaves the item where it is: its header keeps its colors and its place in the tab order, but it no longer answers the pointer or the keyboard. Overrides the value of the AccordionList." },
                new() { Name = "Style", Type = "string?", DefaultValue = "null", Description = "The custom value for the style attribute of the item." },
                new() { Name = "Title", Type = "string?", DefaultValue = "null", Description = "The title (header text) of the item." },
                new() { Name = "TitleTemplate", Type = "RenderFragment<BitAccordionListItem>?", DefaultValue = "null", Description = "The custom content to render in place of the Title of the item. The context value provides the item itself." },
            ]
        },
        new()
        {
            Id = "accordion-list-option",
            Title = "BitAccordionListOption",
            Description = "The component for the items of the BitAccordionList when using the BitAccordionListOption components.",
            Parameters =
            [
                new() { Name = "Actions", Type = "RenderFragment<BitAccordionListOption>?", DefaultValue = "null", Description = "The content rendered beside the header of the option, outside of the toggle button and of the heading it sits in. The context value provides the option itself." },
                new() { Name = "Class", Type = "string?", DefaultValue = "null", Description = "The custom CSS classes of the option." },
                new() { Name = "Description", Type = "string?", DefaultValue = "null", Description = "A short description rendered in the header of the option." },
                new() { Name = "ExpandedExpanderIcon", Type = "BitIconInfo?", DefaultValue = "null", Description = "The icon to show in place of the expander icon while the option is expanded, using custom CSS classes for external icon libraries." },
                new() { Name = "ExpandedExpanderIconName", Type = "string?", DefaultValue = "null", Description = "The name of the icon, from the built-in Fluent UI icons, to show in place of the expander icon while the option is expanded." },
                new() { Name = "ExpanderIcon", Type = "BitIconInfo?", DefaultValue = "null", Description = "The icon to display as the expander using custom CSS classes for external icon libraries. Takes precedence over ExpanderIconName." },
                new() { Name = "ExpanderIconName", Type = "string?", DefaultValue = "null", Description = "The name of the icon to display as the expander from the built-in Fluent UI icons." },
                new() { Name = "ExpanderTemplate", Type = "RenderFragment<BitAccordionListOption>?", DefaultValue = "null", Description = "The custom content to render in place of the expander icon of the option. The context value provides the option itself." },
                new() { Name = "Body", Type = "RenderFragment<BitAccordionListOption>?", DefaultValue = "null", Description = "The content (body) of the option that is shown when the option is expanded. The context value provides the option itself." },
                new() { Name = "ChildContent", Type = "RenderFragment?", DefaultValue = "null", Description = "The default child content of the option, for simple inline content without context. It takes precedence over Body when both are set." },
                new() { Name = "HeaderAriaLabel", Type = "string?", DefaultValue = "null", Description = "The accessible label of the toggle button in the header of the option, for a header whose own content does not name it." },
                new() { Name = "HeaderTemplate", Type = "RenderFragment<BitAccordionListOption>?", DefaultValue = "null", Description = "The custom template for the header of the option. The context value provides the option itself." },
                new() { Name = "HideExpanderIcon", Type = "bool?", DefaultValue = "null", Description = "Removes the expander icon from the header of the option, overriding the value of the AccordionList." },
                new() { Name = "Icon", Type = "BitIconInfo?", DefaultValue = "null", Description = "The icon to display at the start of the header of the option using custom CSS classes for external icon libraries. Takes precedence over IconName." },
                new() { Name = "IconName", Type = "string?", DefaultValue = "null", Description = "The name of the icon to display at the start of the header of the option from the built-in Fluent UI icons." },
                new() { Name = "IsEnabled", Type = "bool", DefaultValue = "true", Description = "Whether or not the option is enabled." },
                new() { Name = "IsExpanded", Type = "bool", DefaultValue = "false", Description = "Determines whether the option is initially expanded." },
                new() { Name = "Key", Type = "string?", DefaultValue = "null", Description = "A unique value to use as the key of the option. A key that is not given is generated from the markup order of the options." },
                new() { Name = "OnClick", Type = "EventCallback<BitAccordionListOption>", DefaultValue = "", Description = "The click event handler of the header of the option." },
                new() { Name = "ReadOnly", Type = "bool?", DefaultValue = "null", Description = "Leaves the option where it is: its header keeps its colors and its place in the tab order, but it no longer answers the pointer or the keyboard. Overrides the value of the AccordionList." },
                new() { Name = "Style", Type = "string?", DefaultValue = "null", Description = "The custom value for the style attribute of the option." },
                new() { Name = "Title", Type = "string?", DefaultValue = "null", Description = "The title (header text) of the option." },
                new() { Name = "TitleTemplate", Type = "RenderFragment<BitAccordionListOption>?", DefaultValue = "null", Description = "The custom content to render in place of the Title of the option. The context value provides the option itself." },
            ]
        },
        new()
        {
            Id = "name-selectors",
            Title = "BitAccordionListNameSelectors",
            Description = "The names and selectors of the custom input type properties.",
            Parameters =
            [
                new() { Name = "Actions", Type = "BitNameSelectorPair<TItem, RenderFragment<TItem>?>", DefaultValue = "new(nameof(BitAccordionListItem.Actions))", Description = "Actions field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Class", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitAccordionListItem.Class))", Description = "The CSS Class field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Description", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitAccordionListItem.Description))", Description = "Description field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "ExpandedExpanderIcon", Type = "BitNameSelectorPair<TItem, BitIconInfo?>", DefaultValue = "new(nameof(BitAccordionListItem.ExpandedExpanderIcon))", Description = "ExpandedExpanderIcon field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "ExpandedExpanderIconName", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitAccordionListItem.ExpandedExpanderIconName))", Description = "ExpandedExpanderIconName field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "ExpanderIcon", Type = "BitNameSelectorPair<TItem, BitIconInfo?>", DefaultValue = "new(nameof(BitAccordionListItem.ExpanderIcon))", Description = "ExpanderIcon field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "ExpanderIconName", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitAccordionListItem.ExpanderIconName))", Description = "ExpanderIconName field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "ExpanderTemplate", Type = "BitNameSelectorPair<TItem, RenderFragment<TItem>?>", DefaultValue = "new(nameof(BitAccordionListItem.ExpanderTemplate))", Description = "ExpanderTemplate field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Body", Type = "BitNameSelectorPair<TItem, RenderFragment<TItem>?>", DefaultValue = "new(nameof(BitAccordionListItem.Body))", Description = "Body field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "HeaderAriaLabel", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitAccordionListItem.HeaderAriaLabel))", Description = "HeaderAriaLabel field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "HeaderTemplate", Type = "BitNameSelectorPair<TItem, RenderFragment<TItem>?>", DefaultValue = "new(nameof(BitAccordionListItem.HeaderTemplate))", Description = "HeaderTemplate field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "HideExpanderIcon", Type = "BitNameSelectorPair<TItem, bool?>", DefaultValue = "new(nameof(BitAccordionListItem.HideExpanderIcon))", Description = "HideExpanderIcon field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Icon", Type = "BitNameSelectorPair<TItem, BitIconInfo?>", DefaultValue = "new(nameof(BitAccordionListItem.Icon))", Description = "Icon field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "IconName", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitAccordionListItem.IconName))", Description = "IconName field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "IsEnabled", Type = "BitNameSelectorPair<TItem, bool>", DefaultValue = "new(nameof(BitAccordionListItem.IsEnabled))", Description = "IsEnabled field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "IsExpanded", Type = "BitNameSelectorPair<TItem, bool>", DefaultValue = "new(nameof(BitAccordionListItem.IsExpanded))", Description = "IsExpanded field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Key", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitAccordionListItem.Key))", Description = "Key field name and selector of the custom input class. An item that carries no key of its own is given a generated one, kept beside it rather than written into it.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "OnClick", Type = "BitNameSelectorPair<TItem, Action<TItem>?>", DefaultValue = "new(nameof(BitAccordionListItem.OnClick))", Description = "OnClick field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "ReadOnly", Type = "BitNameSelectorPair<TItem, bool?>", DefaultValue = "new(nameof(BitAccordionListItem.ReadOnly))", Description = "ReadOnly field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Style", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitAccordionListItem.Style))", Description = "The CSS Style field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "Title", Type = "BitNameSelectorPair<TItem, string?>", DefaultValue = "new(nameof(BitAccordionListItem.Title))", Description = "Title field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
                new() { Name = "TitleTemplate", Type = "BitNameSelectorPair<TItem, RenderFragment<TItem>?>", DefaultValue = "new(nameof(BitAccordionListItem.TitleTemplate))", Description = "TitleTemplate field name and selector of the custom input class.", LinkType = LinkType.Link, Href = "#name-selector-pair" },
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
            Id = "toggle-args",
            Title = "BitAccordionListToggleArgs",
            Description = "The arguments of the OnToggling callback of the BitAccordionList.",
            Parameters =
            [
                new() { Name = "Item", Type = "TItem", Description = "The item that is about to expand or collapse." },
                new() { Name = "Key", Type = "string?", Description = "The key of the item that is about to expand or collapse." },
                new() { Name = "IsExpanding", Type = "bool", Description = "The state the item is about to move to: true while it is expanding, false while it is collapsing." },
                new() { Name = "Reason", Type = "BitAccordionToggleReason", Description = "What made the item expand or collapse: a click on its header, or a call to one of the public methods.", LinkType = LinkType.Link, Href = "#accordion-toggle-reason-enum" },
                new() { Name = "Cancel", Type = "bool", DefaultValue = "false", Description = "Set to true to cancel the expansion or the collapse and leave the item as it is." },
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
                new() { Name = "ItemHeaderWrapper", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the header wrapper of each accordion item, which holds the heading and the actions." },
                new() { Name = "ItemHeading", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the heading element of each accordion item that wraps the header button." },
                new() { Name = "ItemHeader", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the header of each accordion item of the BitAccordionList." },
                new() { Name = "ItemIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icon at the start of the header of each accordion item." },
                new() { Name = "ItemHeaderContent", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the header content of each accordion item of the BitAccordionList." },
                new() { Name = "ItemTitle", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the title of each accordion item of the BitAccordionList." },
                new() { Name = "ItemDescription", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the description of each accordion item of the BitAccordionList." },
                new() { Name = "ItemExpanderIconWrapper", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the expander icon wrapper of each accordion item of the BitAccordionList." },
                new() { Name = "ItemExpanderIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the expander icon of each accordion item of the BitAccordionList." },
                new() { Name = "ItemExpandedIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the expander icon of each accordion item of the BitAccordionList in the expanded state." },
                new() { Name = "ItemActions", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the actions of each accordion item, rendered beside the header." },
                new() { Name = "ItemContentContainer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the content container of each accordion item of the BitAccordionList." },
                new() { Name = "ItemContentWrapper", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the content wrapper of each accordion item, which clips the content while it collapses." },
                new() { Name = "ItemContent", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the content of each accordion item of the BitAccordionList." },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "color-kind-enum",
            Name = "BitColorKind",
            Description = "Defines the color kinds available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Primary", Description = "The primary color kind.", Value = "0" },
                new() { Name = "Secondary", Description = "The secondary color kind.", Value = "1" },
                new() { Name = "Tertiary", Description = "The tertiary color kind.", Value = "2" },
                new() { Name = "Transparent", Description = "The transparent color kind.", Value = "3" },
            ]
        },
        new()
        {
            Id = "icon-position-enum",
            Name = "BitIconPosition",
            Description = "Describes the placement of an icon relative to other content.",
            Items =
            [
                new() { Name = "Start", Description = "Icon renders before the content.", Value = "0" },
                new() { Name = "End", Description = "Icon renders after the content (default).", Value = "1" },
            ]
        },
        new()
        {
            Id = "accordion-toggle-reason-enum",
            Name = "BitAccordionToggleReason",
            Description = "What made an item of the list expand or collapse.",
            Items =
            [
                new() { Name = "Click", Description = "The header of the item was clicked, or activated by the Enter or the Space key.", Value = "0" },
                new() { Name = "Method", Description = "One of the Expand, Collapse, Toggle, ExpandAll and CollapseAll methods was called.", Value = "1" },
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Defines the sizes available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Small", Description = "The small size.", Value = "0" },
                new() { Name = "Medium", Description = "The medium size.", Value = "1" },
                new() { Name = "Large", Description = "The large size.", Value = "2" },
            ]
        },
    ];
}
