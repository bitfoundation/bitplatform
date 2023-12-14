namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.SearchBox;

public partial class BitSearchBoxDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "Autocomplete",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies the value of the autocomplete attribute of the input component.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitSearchBoxClassStyles?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#searchbox-class-styles",
            Description = "Custom CSS classes for different parts of the BitSearchBox.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the SearchBox, a list of BitSearchBoxOption components.",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "string?",
            DefaultValue = "null",
            Description = "The default value of the text in the SearchBox, in the case of an uncontrolled component.",
        },
        new()
        {
            Name = "DisableAnimation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not to animate the search box icon on focus.",
        },
        new()
        {
            Name = "FixedIcon",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not to make the icon be always visible (it hides by default when the search box is focused).",
        },
        new()
        {
            Name = "IsUnderlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the SearchBox is underlined.",
        },
        new()
        {
            Name = "IconName",
            Type = "string",
            DefaultValue = "Search",
            Description = "The icon name for the icon shown at the beginning of the search box.",
        },
        new()
        {
            Name = "Items",
            Type = "ICollection<TItem>?",
            DefaultValue = "null",
            Description = "The list of items to display in the callout."
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
            Type = "BitSearchBoxItemsProvider<TItem>?",
            DefaultValue = "null",
            Description = "The function providing items to the list for virtualization.",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template for rendering the items of the BitSearchBox.",
        },
        new()
        {
            Name = "MaxSuggestedItems",
            Type = "int",
            DefaultValue = "5",
            Description = "The maximum number of items or suggestions that will be displayed.",
        },
        new()
        {
            Name = "MinSearchLength",
            Type = "int",
            DefaultValue = "3",
            Description = "The minimum character requirement for doing a search in suggested items.",
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitSearchBoxNameSelectors<TItem>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties.",
            LinkType = LinkType.Link,
            Href = "#name-selectors"
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<string?>",
            Description = "Callback for when the input value changes.",
        },
        new()
        {
            Name = "OnClear",
            Type = "EventCallback",
            Description = "Callback executed when the user clears the search box by either clicking 'X' or hitting escape.",
        },
        new()
        {
            Name = "OnEscape",
            Type = "EventCallback",
            Description = "Callback executed when the user presses escape in the search box.",
        },
        new()
        {
            Name = "OnSearch",
            Type = "EventCallback<string?>",
            Description = "Callback executed when the user presses enter in the search box.",
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
            Description = "Placeholder for the search box.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitSearchBoxClassStyles?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#searchbox-class-styles",
            Description = "Custom CSS styles for different parts of the BitSearchBox.",
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
            Name = "SearchDelay",
            Type = "int",
            DefaultValue = "400",
            Description = "The delay, in milliseconds, applied to the search functionality.",
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
            Id = "searchbox-class-styles",
            Title = "BitSearchBoxClassStyles",
            Description = "",
            Parameters = new()
            {
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the search box.",
                },
                new()
                {
                    Name = "ClearButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's clear button.",
                },
                new()
                {
                    Name = "ClearButtonContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's clear button container.",
                },
                new()
                {
                    Name = "ClearButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's clear button icon.",
                },
                new()
                {
                    Name = "ClearButtonIconContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's clear button icon container.",
                },
                new()
                {
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's Input.",
                },
                new()
                {
                    Name = "SearchIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box search icon.",
                },
                new()
                {
                    Name = "SearchIconContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box's search icon container.",
                }
            }
        },
        new()
        {
            Id = "name-selectors",
            Title = "BitSearchBoxNameSelectors<TItem>",
            Parameters = new()
            {
               new()
               {
                   Name = "AriaLabel",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitSearchBoxItem.AriaLabel))",
                   Description = "The AriaLabel field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Class",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitSearchBoxItem.Class))",
                   Description = "The CSS Class field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Id",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitSearchBoxItem.Id))",
                   Description = "The Id field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "BitNameSelectorPair<TItem, bool>",
                   DefaultValue = "new(nameof(BitSearchBoxItem.IsSelected))",
                   Description = "The IsSelected field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Style",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitSearchBoxItem.Style))",
                   Description = "The CSS Style field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Text",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitSearchBoxItem.Text))",
                   Description = "The Text field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Title",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitSearchBoxItem.Title))",
                   Description = "The Title field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               }
            },
        },
        new()
        {
            Id = "name-selector-pair",
            Title = "BitNameSelectorPair<TItem, TProp>",
            Parameters = new()
            {
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
            }
        },
    };
}
