namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Dropdown;

public partial class BitDropdownDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AutoFocusSearchBox",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables auto-focusing of the SearchBox input when the callout is open.",
        },
        new()
        {
            Name = "CaretDownIconName",
            Type = "string",
            DefaultValue = "ChevronDown",
            Description = "The icon name of the chevron down element of the dropdown. The default value is ChevronDown.",
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
            Name = "DefaultValue",
            Type = "string?",
            DefaultValue = "null",
            Description = "The default key value that will be initially used to set selected item if the Value parameter is not set.",
        },
        new()
        {
            Name = "DefaultValues",
            Type = "List<string>",
            DefaultValue = "new List<string>()",
            Description = "The default key value that will be initially used to set selected items in multi select mode if the Values parameter is not set.",
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
            Name = "IsMultiSelect",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the multi select mode.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines the opening state of the callout.",
        },
        new()
        {
            Name = "IsRequired",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the required mode of the dropdown.",
        },
        new()
        {
            Name = "IsReselectable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables calling the select events when the same item is selected in single select mode.",
        },
        new()
        {
            Name = "IsResponsive",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the responsive mode of the component for small screens.",
        },
        new()
        {
            Name = "IsRtl",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the RTL direction for the component.",
        },
        new()
        {
            Name = "Items",
            Type = "List<BitDropdownItem>?",
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
            Type = "BitDropdownItemsProvider<BitDropdownItem>?",
            DefaultValue = "null",
            Description = "The function providing items to the list for virtualization.",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitDropdownItem>?",
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
            Name = "MultiSelectDelimiter",
            Type = "string",
            DefaultValue = ", ",
            Description = "The delimiter for joining the values to create the text of the dropdown in multi select mode.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<BitDropdownItem[]>",
            Description = "The callback that called when selected items change.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "The click callback for the dropdown.",
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
            Type = "EventCallback<BitDropdownItem>",
            Description = "The callback that called when an item gets selected.",
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
            Type = "RenderFragment<BitDropdown>?",
            DefaultValue = "null",
            Description = "The custom template for the placeholder of the dropdown.",
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
            Name = "SelectedItem",
            Type = "BitDropdownItem?",
            DefaultValue = "null",
            Description = "The selected item in single select mode.",
        },
        new()
        {
            Name = "SelectedItems",
            Type = "List<BitDropdownItem>",
            DefaultValue = "new List<BitDropdownItem>()",
            Description = "The selected items in multi select mode.",
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
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title to show when the mouse hovers over the dropdown.",
        },
        new()
        {
            Name = "TextTemplate",
            Type = "RenderFragment<BitDropdown>?",
            DefaultValue = "null",
            Description = "The custom template for the text of the dropdown.",
        },
        new()
        {
            Name = "Values",
            Type = "List<string>",
            DefaultValue = "new List<string>()",
            Description = "The key values of the selected items in multi select mode.",
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
    };
    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "dropdown-item",
            Title = "BitDropdownItem",
            Parameters = new()
            {
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The aria label attribute for the dropdown item."
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
                   Name = "IsSelected",
                   Type = "bool",
                   DefaultValue = "null",
                   Description = "Determines if the dropdown item is selected."
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
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "The value of the dropdown item."
               },
            },
        }
    };
    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "item-type-enum",
            Name = "BitDropdownItemType",
            Items = new()
            {
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
            }
        },
    };
}
