namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class BitButtonGroupDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the BitButtonGroup, that are BitButtonGroupOption components.",
        },
        new()
        {
            Name = "IconOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines that only the icon should be rendered.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitButtonGroupClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the ButtonGroup.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the button group.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Expand the ButtonGroup width to 100% of the available width.",
        },
        new()
        {
            Name = "Items",
            Type = "IEnumerable<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "List of Item, each of which can be a Button with different action in the ButtonGroup.",
            LinkType = LinkType.Link,
            Href = "#button-group-items",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The content inside the item can be customized.",
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitButtonGroupNameSelectors<TItem>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties.",
            LinkType = LinkType.Link,
            Href = "#name-selectors",
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            Description = "The callback that is called when a button is clicked."
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
            Name = "Toggle",
            Type = "bool",
            DefaultValue = "false",
            Description = "Display ButtonGroup with toggle mode enabled for each button.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize",
            DefaultValue = "null",
            Description = "The size of ButtonGroup, Possible values: Small | Medium | Large.",
            LinkType = LinkType.Link,
            Href = "#button-size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitButtonGroupClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the ButtonGroup.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the button group.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Defines whether to render ButtonGroup children vertically."
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "button-group-items",
            Title = "BitButtonGroupItem",
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
                   Name = "Key",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "A unique value to use as a Key of the item.",
               },
               new()
               {
                   Name = "OffIconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The icon of the item when it is not checked in toggle mode.",
               },
               new()
               {
                   Name = "OffText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The text of the item when it is not checked in toggle mode.",
               },
               new()
               {
                   Name = "OffTitle",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The title of the item when it is not checked in toggle mode.",
               },
               new()
               {
                   Name = "OnIconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The icon of the item when it is checked in toggle mode.",
               },
               new()
               {
                   Name = "OnText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The text of the item when it is checked in toggle mode.",
               },
               new()
               {
                   Name = "OnTitle",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The title of the item when it is checked in toggle mode.",
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
                   Name = "ReversedIcon",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Reverses the positions of the icon and the main content of the item.",
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
                   Type = "RenderFragment<BitButtonGroupItem>?",
                   DefaultValue = "null",
                   Description = "The custom template for the item.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text to render in the item.",
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Title to render in the item.",
               }
            ]
        },
        new()
        {
            Id = "button-group-options",
            Title = "BitButtonGroupOption",
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
                   Name = "Key",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "A unique value to use as a key of the option.",
               },
               new()
               {
                   Name = "OffIconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The icon of the option when it is not checked in toggle mode.",
               },
               new()
               {
                   Name = "OffText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The text of the option when it is not checked in toggle mode.",
               },
               new()
               {
                   Name = "OffTitle",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The title of the option when it is not checked in toggle mode.",
               },
               new()
               {
                   Name = "OnIconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The icon of the option when it is checked in toggle mode.",
               },
               new()
               {
                   Name = "OnText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The text of the option when it is checked in toggle mode.",
               },
               new()
               {
                   Name = "OnTitle",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The title of the option when it is checked in toggle mode.",
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
                   Name = "ReversedIcon",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Reverses the positions of the icon and the main content of the option.",
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
                   Type = "RenderFragment<BitButtonGroupOption>?",
                   DefaultValue = "null",
                   Description = "The custom template for the option.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text to render in the option.",
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Title to render in the option.",
               }
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitButtonGroupClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitButtonGroup.",
               },
               new()
               {
                   Name = "Button",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the internal button of the BitButtonGroup.",
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the BitButtonGroup."
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the text of the BitButtonGroup."
               },
               new()
               {
                   Name = "ToggledButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the button when in toggle mode of the BitButtonGroup.",
               },
            ],
        },
        new()
        {
            Id = "name-selectors",
            Title = "BitButtonGroupNameSelectors",
            Parameters =
            [
                new()
                {
                    Name = "Class",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Class))",
                    Description = "The CSS Class field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "IconName",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.IconName))",
                    Description = "IconName field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "IsEnabled",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.IsEnabled))",
                    Description = "IsEnabled field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Key",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Key))",
                    Description = "Key field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "OffIconName",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.OffIconName))",
                    Description = "OffIconName field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "OffText",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.OffText))",
                    Description = "OffText field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "OffTitle",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.OffTitle))",
                    Description = "OffTitle field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "OnIconName",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.OnIconName))",
                    Description = "OnIconName field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "OnText",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.OnText))",
                    Description = "OnText field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "OnTitle",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.OnTitle))",
                    Description = "OnTitle field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "OnClick",
                    Type = "BitNameSelectorPair<TItem, Action<TItem>?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.OnClick))",
                    Description = "OnClick field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "ReversedIcon",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.ReversedIcon))",
                    Description = "ReversedIcon field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Style",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Style))",
                    Description = "Style field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Template",
                    Type = "BitNameSelectorPair<TItem, RenderFragment?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Template))",
                    Description = "Template field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Text",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Text))",
                    Description = "Text field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Title",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Title))",
                    Description = "Title field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                }
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
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Primary",
                    Description="Primary general color.",
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
                    Description="Severe Warning general color.",
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
            Id = "button-size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size button.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size button.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size button.",
                    Value="2",
                }
            ]
        },
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
        }
    ];
}
