namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Dropdown;

public partial class BitDropdownDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoFocusSearchBox",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables auto-focusing of the SearchBox input when the callout is open.",
        },
        new()
        {
            Name = "CalloutHeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template to render as a header in the callout.",
        },
        new()
        {
            Name = "CalloutFooterTemplate",
            Type = "RenderFragment?",
            DefaultValue = "false",
            Description = "Custom template to render as a footer in the callout.",
        },
        new()
        {
            Name = "CaretDownIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name of the chevron down element of the dropdown.",
        },
        new()
        {
            Name = "CaretDownTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the chevron down element of the dropdown.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Dropdown, a list of BitDropdownOption components.",
        },
        new()
        {
            Name = "Chips",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the selected items like chips in the BitDropdown.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitDropdownClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitDropdown.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Combo",
            Type = "bool",
            DefaultValue = "false",
            Description = "Activates the ComboBox feature in BitDropDown component.",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "string?",
            DefaultValue = "null",
            Description = "The default value that will be initially used to set selected item if the Value parameter is not set.",
        },
        new()
        {
            Name = "DefaultValues",
            Type = "IEnumerable<string?>?",
            DefaultValue = "null",
            Description = "The default values that will be initially used to set selected items in multi select mode if the Values parameter is not set.",
        },
        new()
        {
            Name = "DropDirection",
            Type = "BitDropDirection",
            DefaultValue = "BitDropDirection.TopAndBottom",
            Description = "Determines the allowed drop directions of the callout.",
        },
        new()
        {
            Name = "Dynamic",
            Type = "bool",
            DefaultValue = "false",
            Description = "It is allowed to add a new item in the ComboBox mode.",
        },
        new()
        {
            Name = "DynamicValueGenerator",
            Type = "Func<TItem, TValue>?",
            DefaultValue = "null",
            Description = "The function for generating value in a custom item when a new item is on added Dynamic ComboBox mode.",
        },
        new()
        {
            Name = "ExistsSelectedItemFunction",
            Type = "Func<ICollection<TItem>, string, bool>",
            Description = "Custom search function to be used in place of the default search algorithm for checking existing an item in selected items in the ComboBox mode.",
        },
        new()
        {
            Name = "FindItemFunction",
            Type = "Func<ICollection<TItem>, string, TItem>",
            Description = "Custom search function to be used in place of the default search algorithm for checking existing an item in items in the ComboBox mode.",
        },
        new()
        {
            Name = "FitWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables fit-content value for the width of the root element.",
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template for rendering the header items of the dropdown.",
        },
        new()
        {
            Name = "InitialSelectedItems",
            Type = "IEnumerable<TItem>?",
            DefaultValue = "null",
            Description = "The initial items that will be used to set selected items when using an ItemProvider.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines the opening state of the callout. (two-way bound)",
        },
        new()
        {
            Name = "Items",
            Type = "ICollection<TItem>?",
            DefaultValue = "null",
            Description = "The list of items to display in the callout.",
            LinkType = LinkType.Link,
            Href = "#dropdown-item"
        },
        new()
        {
            Name = "ItemSize",
            Type = "int",
            DefaultValue = "35",
            Description = "The height of each item in pixels for virtualization.",
        },
        new()
        {
            Name = "ItemsProvider",
            Type = "BitDropdownItemsProvider<TItem>?",
            DefaultValue = "null",
            Description = "The function providing items to the list for virtualization.",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template for rendering the items of the dropdown.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the label element of the dropdown.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the label of the dropdown.",
        },
        new()
        {
            Name = "MultiSelect",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the multi select mode.",
        },
        new()
        {
            Name = "MultiSelectDelimiter",
            Type = "string",
            DefaultValue = ", ",
            Description = "The delimiter for joining the values to create the text of the dropdown in multi select mode.",
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitDropdownNameSelectors<TItem, TValue>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties.",
            LinkType = LinkType.Link,
            Href = "#name-selectors"
        },
        new()
        {
            Name = "NoBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the border from the root element.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "The click callback for the dropdown.",
        },
        new()
        {
            Name = "OnDynamicAdd",
            Type = "EventCallback<string>",
            Description = "The callback that is called when a new item is on added Dynamic ComboBox mode.",
        },
        new()
        {
            Name = "OnSearch",
            Type = "EventCallback<string>",
            Description = "The callback that is called when the search value changes.",
        },
        new()
        {
            Name = "OnSelectItem",
            Type = "EventCallback<TItem>",
            Description = "The callback that called when an item gets selected.",
        },
        new()
        {
            Name = "OnValuesChange",
            Type = "EventCallback<IEnumerable<TValue>>",
            Description = "The callback that called when selected items change.",
        },
        new()
        {
            Name = "Options",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of ChildContent.",
        },
        new()
        {
            Name = "OverscanCount",
            Type = "int",
            DefaultValue = "3",
            Description = "Determines how many additional items are rendered before and after the visible region.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder text of the dropdown.",
        },
        new()
        {
            Name = "PlaceholderTemplate",
            Type = "RenderFragment<BitDropdown<TItem, TValue>>?",
            DefaultValue = "null",
            Description = "The custom template for the placeholder of the dropdown.",
        },
        new()
        {
            Name = "Prefix",
            Type = "string?",
            DefaultValue = "null",
            Description = "Prefix displayed before the dropdown contents. This is not included in the value. \r\n Ensure a descriptive label is present to assist screen readers, as the value does not include the prefix.",
        },
        new()
        {
            Name = "PrefixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Shows the custom prefix for dropdown.",
        },
        new()
        {
            Name = "PreserveCalloutWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables automatic setting of the callout width and preserves its original width.",
        },
        new()
        {
            Name = "Reselectable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables calling the select events when the same item is selected in single select mode.",
        },
        new()
        {
            Name = "Responsive",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the responsive mode of the component for small screens.",
        },
        new()
        {
            Name = "SearchBoxPlaceholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder text of the SearchBox input.",
        },
        new()
        {
            Name = "SearchFunction",
            Type = "Func<ICollection<TItem>, string, ICollection<TItem>>?",
            DefaultValue = "null",
            Description = "Custom search function to be used in place of the default search algorithm.",
        },
        new()
        {
            Name = "ShowClearButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the clear button when an item is selected.",
        },
        new()
        {
            Name = "ShowSearchBox",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the SearchBox element in the callout.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitDropdownClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitDropdown.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Suffix",
            Type = "string?",
            DefaultValue = "null",
            Description = "Suffix displayed after the dropdown contents. This is not included in the value. \r\n Ensure a descriptive label is present to assist screen readers, as the value does not include the suffix.",
        },
        new()
        {
            Name = "SuffixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Shows the custom suffix for dropdown.",
        },
        new()
        {
            Name = "TextTemplate",
            Type = "RenderFragment<<TItem, TValue>>?",
            DefaultValue = "null",
            Description = "The custom template for the text of the dropdown.",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title to show when the mouse hovers over the dropdown.",
        },
        new()
        {
            Name = "Transparent",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the default background color from the root element.",
        },
        new()
        {
            Name = "Values",
            Type = "IEnumerable<TValue?>?",
            DefaultValue = "null",
            Description = "The values of the selected items in multi select mode. (two-way bound)",
        },
        new()
        {
            Name = "Virtualize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables virtualization to render only the visible items.",
        },
        new()
        {
            Name = "VirtualizePlaceholder",
            Type = "RenderFragment<PlaceholderContext>?",
            DefaultValue = "null",
            Description = "The template for items that have not yet been rendered in virtualization mode.",
        }
    ];
    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "dropdown-item",
            Title = "BitDropdownItem<TValue>",
            Parameters =
            [
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The aria label attribute for the dropdown item."
               },
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS class for the dropdown item."
               },
               new()
               {
                   Name = "Id",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The id for the dropdown item."
               },
               new()
               {
                   Name = "Data",
                   Type = "object?",
                   DefaultValue = "null",
                   Description = "The custom data for the dropdown item to provide state for the item template."
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "null",
                   Description = "Determines if the dropdown item is enabled."
               },
               new()
               {
                   Name = "IsHidden",
                   Type = "bool",
                   DefaultValue = "null",
                   Description = "Determines if the dropdown item is hidden."
               },
               new()
               {
                   Name = "ItemType",
                   Type = "BitDropdownItemType",
                   DefaultValue = "BitDropdownItemType.Normal",
                   Description = "The type of the dropdown item.",
                   LinkType = LinkType.Link,
                   Href = "#item-type-enum"
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS style for the dropdown item."
               },
               new()
               {
                   Name = "Text",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "The text to render for the dropdown item."
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The title attribute for the dropdown item."
               },
               new()
               {
                   Name = "Value",
                   Type = "TValue?",
                   DefaultValue = "null",
                   Description = "The value of the dropdown item."
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Determines if the item is selected. This property's value is assigned by the component."
               },
            ],
        },
        new()
        {
            Id = "dropdown-option",
            Title = "BitDropdownOption<TValue>",
            Parameters =
            [
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The aria label attribute for the dropdown option."
               },
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS class for the dropdown option."
               },
               new()
               {
                   Name = "Id",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The id for the dropdown option."
               },
               new()
               {
                   Name = "Data",
                   Type = "object?",
                   DefaultValue = "null",
                   Description = "The custom data for the dropdown option to provide extra state for the template."
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "null",
                   Description = "Determines if the dropdown option is enabled."
               },
               new()
               {
                   Name = "IsHidden",
                   Type = "bool",
                   DefaultValue = "null",
                   Description = "Determines if the dropdown option is hidden."
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   DefaultValue = "null",
                   Description = "Determines if the dropdown option is selected."
               },
               new()
               {
                   Name = "ItemType",
                   Type = "BitDropdownItemType",
                   DefaultValue = "BitDropdownItemType.Normal",
                   Description = "The type of the dropdown option.",
                   LinkType = LinkType.Link,
                   Href = "#item-type-enum"
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS style for the dropdown option."
               },
               new()
               {
                   Name = "Text",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "The text to render for the dropdown option."
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The title attribute for the dropdown option."
               },
               new()
               {
                   Name = "Value",
                   Type = "TValue?",
                   DefaultValue = "null",
                   Description = "The value of the dropdown option."
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Determines if the option is selected. This property's value is assigned by the component."
               },
            ],
        },
        new()
        {
            Id = "name-selectors",
            Title = "BitDropdownNameSelectors<TItem, TValue>",
            Parameters =
            [
               new()
               {
                   Name = "AriaLabel",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.AriaLabel))",
                   Description = "The AriaLabel field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Class",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.Class))",
                   Description = "The CSS Class field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Id",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.Id))",
                   Description = "The Id field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Data",
                   Type = "BitNameSelectorPair<TItem, object?>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.Data))",
                   Description = "The Data field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "BitNameSelectorPair<TItem, bool>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.IsEnabled))",
                   Description = "The IsEnabled field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "IsHidden",
                   Type = "BitNameSelectorPair<TItem, bool>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.IsHidden))",
                   Description = "The IsHidden field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "ItemType",
                   Type = "BitNameSelectorPair<TItem, BitDropdownItemType>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.ItemType))",
                   Description = "The ItemType field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#item-type-enum"
               },
               new()
               {
                   Name = "Style",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.Style))",
                   Description = "The CSS Style field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Text",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.Text))",
                   Description = "The Text field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Title",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.Title))",
                   Description = "The Title field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Value",
                   Type = "BitNameSelectorPair<TItem, TValue?>",
                   DefaultValue = "new(nameof(BitDropdownItem<TValue>.Value))",
                   Description = "The Value field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "TextSetter",
                   Type = "Action<string, TItem>?",
                   Description = "The setter function for updating Text property of custom item in Dynamic ComboBox mode upon new item addition.",
               },
               new()
               {
                   Name = "ValueSetter",
                   Type = "Action<TItem, TItem>?",
                   Description = "The setter function for updating Value property of custom item in Dynamic ComboBox mode upon new item addition.",
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "string",
                   Description = "The IsSelected field name of the custom input class. This property's value is assigned by the component.",
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
        new()
        {
            Id = "class-styles",
            Title = "BitDropdownClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitDropdown."
               },
               new()
               {
                   Name = "Label",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label of the BitDropdown."
               },
               new()
               {
                   Name = "Container",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the main container of the BitDropdown."
               },
               new()
               {
                   Name = "TextContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the text container of the BitDropdown."
               },
               new()
               {
                   Name = "ClearButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the clear button of the BitDropdown."
               },
               new()
               {
                   Name = "CaretDownIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the caret down icon of the BitDropdown."
               },
               new()
               {
                   Name = "Overlay",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the overlay of the BitDropdown."
               },
               new()
               {
                   Name = "Callout",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the callout of the BitDropdown."
               },
               new()
               {
                   Name = "ResponsiveLabelContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the responsive panel's label container of the BitDropdown."
               },
               new()
               {
                   Name = "ResponsiveLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the responsive panel label of the BitDropdown."
               },
               new()
               {
                   Name = "ResponsiveCloseButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the responsive panel's close button of the BitDropdown."
               },
               new()
               {
                   Name = "ResponsiveCloseIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the responsive panel's close icon of the BitDropdown."
               },
               new()
               {
                   Name = "SearchBoxContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the search box container of the BitDropdown."
               },
               new()
               {
                   Name = "SearchBoxIconContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the search box's icon container of the BitDropdown."
               },
               new()
               {
                   Name = "SearchBoxIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the search box icon of the BitDropdown."
               },
               new()
               {
                   Name = "SearchBoxInput",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the search box input of the BitDropdown."
               },
               new()
               {
                   Name = "SearchBoxClearButtonContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the search box's clear button container of the BitDropdown."
               },
               new()
               {
                   Name = "SearchBoxClearButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the search box's clear button of the BitDropdown."
               },
               new()
               {
                   Name = "SearchBoxClearIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the search box's clear icon of the BitDropdown."
               },
               new()
               {
                   Name = "ScrollContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the scroll container of the BitDropdown."
               },
               new()
               {
                   Name = "ItemHeader",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item header of the BitDropdown."
               },
               new()
               {
                   Name = "ItemWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item wrapper of the multi-select BitDropdown."
               },
               new()
               {
                   Name = "ItemButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item button of the BitDropdown."
               },
               new()
               {
                   Name = "ItemCheckBox",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item check box of the multi-select BitDropdown."
               },
               new()
               {
                   Name = "ItemCheckIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item check icon of the multi-select BitDropdown."
               },
               new()
               {
                   Name = "ItemText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item text of the BitDropdown."
               },
               new()
               {
                   Name = "ItemDivider",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item divider of the BitDropdown."
               },
            ],
        }
    ];
    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "item-type-enum",
            Name = "BitDropdownItemType",
            Items =
            [
                new()
                {
                    Name = "Normal",
                    Description = "Dropdown items are being rendered as a normal item.",
                    Value = "0",
                },
                new()
                {
                    Name = "Header",
                    Description = "Dropdown items are being rendered as a header, they cannot be selected.",
                    Value = "1",
                },
                new()
                {
                    Name = "Divider",
                    Description = "Dropdown items are being rendered as a divider, just draw a line.",
                    Value = "2",
                }
            ]
        },
    ];
    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "SelectedItems",
            Type = "IReadOnlyList<TItem>",
            Description = "A readonly list of the current selected items in multi-select mode.",
        },
        new()
        {
            Name = "SelectedItem",
            Type = "TItem?",
            Description = "The current selected item in single-select mode.",
        },
        new()
        {
            Name = "ComboInputElement",
            Type = "ElementReference",
            Description = "The ElementReference to the combo input element.",
        },
        new()
        {
            Name = "FocusComboInputAsync",
            Type = "ValueTask",
            Description = "Gives focus to the combo input element.",
        },
        new()
        {
            Name = "SearchInputElement",
            Type = "ElementReference",
            Description = "The ElementReference to the search input element.",
        },
        new()
        {
            Name = "FocusSearchInputAsync",
            Type = "ValueTask",
            Description = "Gives focus to the search input element.",
        }
    ];
}
