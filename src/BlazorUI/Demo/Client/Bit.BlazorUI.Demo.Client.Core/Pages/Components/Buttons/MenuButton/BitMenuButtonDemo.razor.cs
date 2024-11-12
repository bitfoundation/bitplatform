namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class BitMenuButtonDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the menu button for the benefit of screen readers."
        },
        new()
        {
            Name = "AriaHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, add an aria-hidden attribute instructing screen readers to ignore the menu button."
        },
        new()
        {
            Name = "ButtonType",
            Type = "BitButtonType",
            DefaultValue = "null",
            Description = "The value of the type attribute of the menu button.",
            LinkType = LinkType.Link,
            Href = "#button-type-enum"
        },
        new()
        {
            Name = "ChevronDownIcon",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name of the chevron down part of the menu button.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the menu button, that are BitMenuButtonOption components.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitMenuButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the menu button.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the menu button.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "DefaultSelectedItem",
            Type = "TItem?",
            DefaultValue = "null",
            Description = "Default value of the SelectedItem."
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content inside the header of menu button can be customized.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon to show inside the header of menu button.",
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
            Name = "Items",
            Type = "IEnumerable<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "List of items to show in the menu button.",
            LinkType = LinkType.Link,
            Href = "#menu-button-items"
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template content to render each item.",
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitMenuButtonNameSelectors<TItem>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties.",
            LinkType = LinkType.Link,
            Href = "#name-selectors",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "The callback is called when the menu button header is clicked."
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<TItem>",
            Description = "The callback that is called when the selected item has changed."
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
            Name = "SelectedItem",
            Type = "TItem?",
            DefaultValue = "null",
            Description = "Determines the current selected item that acts as the header item."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the menu button.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Split",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the menu button renders as a split button."
        },
        new()
        {
            Name = "Sticky",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the selected item is going to change the header item."
        },
        new()
        {
            Name = "Styles",
            Type = "BitMenuButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the menu button.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text to show inside the header of menu button."
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the menu button.",
            LinkType = LinkType.Link,
            Href = "#variant-enum"
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "menu-button-items",
            Title = "BitMenuButtonItem",
            Parameters =
            [
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The custom CSS classes of the item.",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render next to the item text.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the item is enabled.",
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Determines the selection state of the item.",
               },
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "A unique value to use as a Key of the item.",
               },
               new()
               {
                   Name = "OnClick",
                   Type = "EventCallback",
                   DefaultValue = "",
                   Description = "Click event handler of the item.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The custom value for the style attribute of the item.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitMenuButtonItem>?",
                   DefaultValue = "null",
                   Description = "The custom template for the item.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text to render in the item.",
               }
            ]
        },
        new()
        {
            Id = "menu-button-options",
            Title = "BitMenuButtonOption",
            Parameters =
            [
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The custom CSS classes of the option.",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render next to the option text.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the option is enabled.",
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Determines the selection state of the item.",
               },
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "A unique value to use as a key of the option.",
               },
               new()
               {
                   Name = "OnClick",
                   Type = "EventCallback",
                   DefaultValue = "",
                   Description = "Click event handler of the option.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The custom value for the style attribute of the option.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitMenuButtonOption>?",
                   DefaultValue = "null",
                   Description = "The custom template for the option.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text to render in the option.",
               }
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitMenuButtonClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitMenuButton.",
               },
               new()
               {
                   Name = "Opened",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the opened callout state of the BitMenuButton.",
               },
               new()
               {
                   Name = "OperatorButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for operator button of the BitMenuButton."
               },
               new()
               {
                   Name = "OperatorButtonIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for operator button icon of the BitMenuButton."
               },
               new()
               {
                   Name = "OperatorButtonText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for operator button text of the BitMenuButton."
               },
               new()
               {
                   Name = "Callout",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the callout of the BitMenuButton."
               },
               new()
               {
                   Name = "CalloutContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the callout container of the BitMenuButton."
               },
               new()
               {
                   Name = "ChevronDownButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the chevron down button of the BitMenuButton."
               },
               new()
               {
                   Name = "ChevronDown",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the chevron down of the BitMenuButton."
               },
               new()
               {
                   Name = "Separator",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the separator of the BitMenuButton."
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the BitMenuButton."
               },
               new()
               {
                   Name = "ItemWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each item wrapper of the BitMenuButton."
               },
               new()
               {
                   Name = "ItemButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each item of the BitMenuButton."
               },
               new()
               {
                   Name = "ItemIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each item icon of the BitMenuButton."
               },
               new()
               {
                   Name = "ItemText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each item text of the BitMenuButton."
               },
               new()
               {
                   Name = "Overlay",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each overlay of the BitMenuButton."
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the text of the BitMenuButton."
               },
            ],
        },
        new()
        {
            Id = "name-selectors",
            Title = "BitMenuButtonNameSelectors",
            Parameters =
            [
                new()
                {
                    Name = "Class",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.Class))",
                    Description = "The CSS Class field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "IconName",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.IconName))",
                    Description = "IconName field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "IsEnabled",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.IsEnabled))",
                    Description = "IsEnabled field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "IsSelected",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.IsSelected))",
                    Description = "IsSelected field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Key",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.Key))",
                    Description = "Key field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "OnClick",
                    Type = "BitNameSelectorPair<TItem, Action<TItem>?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.OnClick))",
                    Description = "OnClick field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Style",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.Style))",
                    Description = "Style field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Text",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.Text))",
                    Description = "Text field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
            ]
        },
        new()
        {
            Id = "name-selector-pair",
            Title = "BitNameSelectorPair",
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

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "button-type-enum",
            Name = "BitButtonType",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Button",
                    Description="The button is a clickable button.",
                    Value="0",
                },
                new()
                {
                    Name= "Submit",
                    Description="The button is a submit button (submits form-data).",
                    Value="1",
                },
                new()
                {
                    Name= "Reset",
                    Description="The button is a reset button (resets the form-data to its initial values).",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name= "Primary",
                    Description="Info Primary general color.",
                    Value="0",
                },
                new()
                {
                    Name= "Secondary",
                    Description="Secondary general color.",
                    Value="1",
                },
                new()
                {
                    Name= "Tertiary",
                    Description="Tertiary general color.",
                    Value="2",
                },
                new()
                {
                    Name= "Info",
                    Description="Info general color.",
                    Value="3",
                },
                new()
                {
                    Name= "Success",
                    Description="Success general color.",
                    Value="4",
                },
                new()
                {
                    Name= "Warning",
                    Description="Warning general color.",
                    Value="5",
                },
                new()
                {
                    Name= "SevereWarning",
                    Description="SevereWarning general color.",
                    Value="6",
                },
                new()
                {
                    Name= "Error",
                    Description="Error general color.",
                    Value="7",
                },
                new()
                {
                    Name= "PrimaryBackground",
                    Description="Primary background color.",
                    Value="8",
                },
                new()
                {
                    Name= "SecondaryBackground",
                    Description="Secondary background color.",
                    Value="9",
                },
                new()
                {
                    Name= "TertiaryBackground",
                    Description="Tertiary background color.",
                    Value="10",
                },
                new()
                {
                    Name= "PrimaryForeground",
                    Description="Primary foreground color.",
                    Value="11",
                },
                new()
                {
                    Name= "SecondaryForeground",
                    Description="Secondary foreground color.",
                    Value="12",
                },
                new()
                {
                    Name= "TertiaryForeground",
                    Description="Tertiary foreground color.",
                    Value="13",
                },
                new()
                {
                    Name= "PrimaryBorder",
                    Description="Primary border color.",
                    Value="14",
                },
                new()
                {
                    Name= "SecondaryBorder",
                    Description="Secondary border color.",
                    Value="15",
                },
                new()
                {
                    Name= "TertiaryBorder",
                    Description="Tertiary border color.",
                    Value="16",
                }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "variant-enum",
            Name = "BitVariant",
            Description = "Determines the variant of the content that controls the rendered style of the corresponding element(s).",
            Items =
            [
                new()
                {
                    Name= "Fill",
                    Description="Fill styled variant.",
                    Value="0",
                },
                new()
                {
                    Name= "Outline",
                    Description="Outline styled variant.",
                    Value="1",
                },
                new()
                {
                    Name= "Text",
                    Description="Text styled variant.",
                    Value="2",
                }
            ]
        },
    ];
}
