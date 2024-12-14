namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.NavBar;

public partial class BitNavBarDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Items to render as children.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitNavClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the navbar.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the navbar.",
        },
        new()
        {
            Name = "DefaultSelectedItem",
            Type = "TItem?",
            DefaultValue = "null",
            Description = "The initially selected item in manual mode."
        },
        new()
        {
            Name = "FitWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the nav bat in a width to only fit its content."
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the nav bar in full width of its container element."
        },
        new()
        {
            Name = "IconOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Only renders the icon of each navbar item."
        },
        new()
        {
            Name = "Items",
            Type = "IList<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "A collection of items to display in the navbar.",
            LinkType = LinkType.Link,
            Href="#navbar-item",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "Used to customize how content inside the item is rendered."
        },
        new()
        {
            Name = "Mode",
            Type = "BitNavMode",
            DefaultValue = "BitNavMode.Automatic",
            Description = "Determines how the navigation will be handled.",
            LinkType = LinkType.Link,
            Href = "#nav-mode-enum",
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitNavNameSelectors<TItem>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties.",
            LinkType = LinkType.Link,
            Href = "#name-selectors",
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            Description = "Callback invoked when an item is clicked."
        },
        new()
        {
            Name = "OnSelectItem",
            Type = "EventCallback<TItem>",
            Description = "Callback invoked when an item is selected."
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
            Name = "Reselectable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables recalling the select events when the same item is selected."
        },
        new()
        {
            Name = "SelectedItem",
            Type = "TItem?",
            DefaultValue = "null",
            Description = "Selected item to show in the navbar."
        },
        new()
        {
            Name = "Styles",
            Type = "BitNavClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the navbar.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "navbar-item",
            Title = "BitNavBarItem",
            Parameters =
            [
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS class for the navbar item.",
               },
               new()
               {
                   Name = "Data",
                   Type = "object?",
                   DefaultValue = "null",
                   Description = "The custom data for the navbar item to provide additional state.",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render next to the navbar item.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the navbar item is enabled.",
               },
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "A unique value to use as a key or id of the navbar item.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS style for the navbar item.",
               },
               new()
               {
                   Name = "Target",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Link target, specifies how to open the navbar item's link.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitNavBarItem>?",
                   DefaultValue = "null",
                   Description = "The custom template for the navbar item to render.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "Text to render for the navbar item.",
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text for the tooltip of the navbar item.",
               },
               new()
               {
                   Name = "Url",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The navbar item's link URL.",
               },
               new()
               {
                   Name = "AdditionalUrls",
                   Type = "IEnumerable<string>?",
                   DefaultValue = "null",
                   Description = "Alternative URLs to be considered when auto mode tries to detect the selected navbar item by the current URL.",
               }
            ]
        },
        new()
        {
            Id = "navbar-option",
            Title = "BitNavBarOption",
            Parameters =
            [
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS class for the navbar option.",
               },
               new()
               {
                   Name = "Data",
                   Type = "object?",
                   DefaultValue = "null",
                   Description = "The custom data for the navbar option to provide additional state.",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render next to the navbar option.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the navbar option is enabled.",
               },
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "A unique value to use as a key or id of the navbar option.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS style for the navbar option.",
               },
               new()
               {
                   Name = "Target",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Link target, specifies how to open the navbar option's link.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitNavBarOption>?",
                   DefaultValue = "null",
                   Description = "The custom template for the navbar option to render.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "Text to render for the navbar option.",
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text for the tooltip of the navbar option.",
               },
               new()
               {
                   Name = "Url",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The navbar option's link URL.",
               },
               new()
               {
                   Name = "AdditionalUrls",
                   Type = "IEnumerable<string>?",
                   DefaultValue = "null",
                   Description = "Alternative URLs to be considered when auto mode tries to detect the selected navbar option by the current URL.",
               }
            ]
        },
        new()
        {
            Id = "name-selectors",
            Title = "BitNavBarNameSelectors<TItem>",
            Parameters =
            [
               new()
               {
                   Name = "Class",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavBarItem.Class))",
                   Description = "The Class field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Data",
                   Type = "BitNameSelectorPair<TItem, object?>",
                   DefaultValue = "new(nameof(BitNavBarItem.Data))",
                   Description = "The Data field name and selector of the custom input class."
               },
               new()
               {
                   Name = "IconName",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavBarItem.IconName))",
                   Description = "The IconName field name and selector of the custom input class."
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "BitNameSelectorPair<TItem, bool?>",
                   DefaultValue = "new(nameof(BitNavBarItem.IsEnabled))",
                   Description = "The IsEnabled field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Key",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavBarItem.Key))",
                   Description = "The Key field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Style",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavBarItem.Style))",
                   Description = "The Style field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Target",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavBarItem.Target))",
                   Description = "The Target field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Template",
                   Type = "BitNameSelectorPair<TItem, RenderFragment<TItem>?>",
                   DefaultValue = "new(nameof(BitNavBarItem.Template))",
                   Description = "The Template field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Text",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavBarItem.Text))",
                   Description = "The Text field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Title",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavBarItem.Title))",
                   Description = "The Title field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Url",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavBarItem.Url))",
                   Description = "The Url field name and selector of the custom input class."
               },
               new()
               {
                   Name = "AdditionalUrls",
                   Type = "BitNameSelectorPair<TItem, IEnumerable<string>?>",
                   DefaultValue = "new(nameof(BitNavBarItem.AdditionalUrls))",
                   Description = "The AdditionalUrls field name and selector of the custom input class."
               },
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitNavBarClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitNavBar."
               },
               new()
               {
                   Name = "Container",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the container of the items of the BitNavBar."
               },
               new()
               {
                   Name = "Item",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item of the BitNavBar."
               },
               new()
               {
                   Name = "ItemIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item icon of the BitNavBar."
               },
               new()
               {
                   Name = "ItemText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item text of the BitNavBar."
               },
               new()
               {
                   Name = "SelectedItem",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the selected item of the BitNavBar."
               },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "nav-mode-enum",
            Name = "BitNavMode",
            Items =
            [
                new()
                {
                    Name = "Automatic",
                    Description = "The value of selected key will change using NavigationManager and the current url inside the component.",
                    Value = "0",
                },
                new()
                {
                    Name = "Manual",
                    Description = "Selected key changes will be sent back to the parent component and the component won't change its value.",
                    Value = "1",
                }
            ]
        },
    ];
}
