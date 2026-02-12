namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ChoiceGroup;

public partial class BitChoiceGroupDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AriaLabelledBy",
            Type = "string?",
            DefaultValue = "null",
            Description = "Id of an element to use as the aria label for the ChoiceGroup."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the ChoiceGroup, a list of BitChoiceGroupOption components."
        },
        new()
        {
            Name = "Classes",
            Type = "BitChoiceGroupClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitChoiceGroup.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the ChoiceGroup.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "string?",
            DefaultValue = "null",
            Description = "Default selected Value for ChoiceGroup."
        },
        new()
        {
            Name = "Inline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the icons and images in a single line with the items in the ChoiceGroup."
        },
        new()
        {
            Name = "Horizontal",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the items in the ChoiceGroup horizontally."
        },
        new()
        {
            Name = "Items",
            Type = "IEnumerable<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "Sets the data source that populates the items of the list.",
            LinkType = LinkType.Link,
            Href = "#choice-group-item"
        },
        new()
        {
            Name = "ItemLabelTemplate",
            Type = "RenderFragment<TItem>?",
            Description = "Used to customize the label for the Item Label content."
        },
        new()
        {
            Name = "ItemPrefixTemplate",
            Type = "RenderFragment<TItem>?",
            Description = "Used to add a prefix to each item."
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "Used to customize the label for the Item content."
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The label for the ChoiceGroup."
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom RenderFragment for the label of the ChoiceGroup."
        },
        new()
        {
            Name = "Name",
            Type = "string",
            DefaultValue = "Guid.NewGuid().ToString()",
            Description = "Name of the ChoiceGroup, this unique name is used to group each item into the same logical component."
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitChoiceGroupNameSelectors<TItem, TValue>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties.",
            Href = "#name-selectors",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "NoCircle",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the circle from the start of each item."
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the option clicked.",
        },
        new()
        {
            Name = "Options",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of ChildContent."
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the label and radio button location."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the BitChoiceGroup.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitChoiceGroupClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitChoiceGroup.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
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
                    Description="The small size checkbox.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size checkbox.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size checkbox.",
                    Value="2",
                }
            ]
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "choice-group-item",
            Title = "BitChoiceGroupItem",
            Parameters =
            [
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "AriaLabel attribute for the BitChoiceGroup item.",
               },
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "CSS class attribute for the BitChoiceGroup item.",
               },
               new()
               {
                   Name = "Id",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Id attribute of the BitChoiceGroup item.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether the BitChoiceGroup item is enabled.",
               },
               new()
               {
                   Name = "Icon",
                   Type = "BitIconInfo?",
                   DefaultValue = "null",
                   Description = "The icon to show as content of the BitChoiceGroup item. Takes precedence over IconName when both are set.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The icon name (built-in Fluent UI) to show as content of the BitChoiceGroup item.",
               },
               new()
               {
                   Name = "ImageSrc",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The image address to show as the content of the BitChoiceGroup item.",
               },
               new()
               {
                   Name = "ImageAlt",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The alt attribute for the image of the BitChoiceGroup item.",
               },
               new()
               {
                   Name = "ImageSize",
                   Type = "BitImageSize?",
                   DefaultValue = "null",
                   Description = "Provides Width and Height for the image of the BitChoiceGroup item.",
               },
               new()
               {
                   Name = "Prefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The text to show as a prefix for the BitChoiceGroup item.",
               },
               new()
               {
                   Name = "SelectedImageSrc",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Provides a new image for the selected state of the image of the BitChoiceGroup item.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "CSS style attribute for the BitChoiceGroup item.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitChoiceGroupItem<TValue>>?",
                   DefaultValue = "null",
                   Description = "The custom template for the BitChoiceGroup item.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text to show as the content of BitChoiceGroup item.",
               },
               new()
               {
                   Name = "Value",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The value returned when BitChoiceGroup item is checked.",
               },
               new()
               {
                   Name = "Index",
                   Type = "int",
                   DefaultValue = "null",
                   Description = "Index of the BitChoiceGroup item. This property's value is set by the component at render.",
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Determines if the item is selected. This property's value is assigned by the component.",
               }
            ]
        },
        new()
        {
            Id = "choice-group-option",
            Title = "BitChoiceGroupOption",
            Parameters =
            [
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "AriaLabel attribute for the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "CSS class attribute for the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "Id",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Id attribute of the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether the BitChoiceGroup option is enabled.",
               },
               new()
               {
                   Name = "Icon",
                   Type = "BitIconInfo?",
                   DefaultValue = "null",
                   Description = "The icon to show as content of the BitChoiceGroup option. Takes precedence over IconName when both are set.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The icon name (built-in Fluent UI) to show as content of the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "ImageSrc",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The image address to show as the content of the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "ImageAlt",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The alt attribute for the image of the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "ImageSize",
                   Type = "BitImageSize?",
                   DefaultValue = "null",
                   Description = "Provides Width and Height for the image of the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "Prefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The text to show as a prefix for the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "SelectedImageSrc",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Provides a new image for the selected state of the image of the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "CSS style attribute for the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitChoiceGroupOption<TValue>>?",
                   DefaultValue = "null",
                   Description = "The custom template for the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text to show as the content of BitChoiceGroup option.",
               },
               new()
               {
                   Name = "Value",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The value returned when BitChoiceGroup option is checked.",
               },
               new()
               {
                   Name = "Index",
                   Type = "int",
                   DefaultValue = "null",
                   Description = "Index of the BitChoiceGroup option. This property's value is set by the component at render.",
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Determines if the option is selected. This property's value is assigned by the component.",
               }
            ]
        },
        new()
        {
            Id = "name-selectors",
            Title = "BitChoiceGroupNameSelectors<TItem, TValue>",
            Parameters =
            [
               new()
               {
                   Name = "AriaLabel",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitChoiceGroupItem<TValue>.AriaLabel))",
                   Description = "AriaLabel attribute for the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "Class",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitChoiceGroupItem<TValue>.Class))",
                   Description = "CSS class attribute for the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "Id",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitChoiceGroupItem<TValue>.Id))",
                   Description = "Id attribute of the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "BitNameSelectorPair<TItem, bool>",
                   DefaultValue = "new(nameof(BitChoiceGroupItem<TValue>.IsEnabled))",
                   Description = "Whether the BitChoiceGroup option is enabled.",
               },
               new()
               {
                   Name = "Icon",
                   Type = "BitNameSelectorPair<TItem, BitIconInfo?>",
                   DefaultValue = "new(nameof(BitChoiceGroupItem<TValue>.Icon))",
                   Description = "Icon field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "IconName",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitChoiceGroupItem<TValue>.IconName))",
                   Description = "IconName field name and selector of the custom input class.",
               },
               new()
               {
                   Name = "ImageSrc",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitChoiceGroupItem<TValue>.ImageSrc))",
                   Description = "The image address to show as the content of the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "ImageAlt",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitChoiceGroupItem<TValue>.ImageAlt))",
                   Description = "The alt attribute for the image of the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "ImageSize",
                   Type = "BitNameSelectorPair<TItem, BitImageSize?>",
                   DefaultValue = "new(nameof(BitChoiceGroupItem<TValue>.ImageSize))",
                   Description = "Provides Width and Height for the image of the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "SelectedImageSrc",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitChoiceGroupItem<TValue>.SelectedImageSrc))",
                   Description = "Provides a new image for the selected state of the image of the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "Style",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitChoiceGroupItem<TValue>.Style))",
                   Description = "CSS style attribute for the BitChoiceGroup option.",
               },
               new()
               {
                   Name = "Template",
                   Type = "BitNameSelectorPair<TItem, RenderFragment<TItem>?>",
                   DefaultValue = "new(nameof(BitChoiceGroupItem<TValue>.Template))",
                   Description = "Template field name and selector of the custom input class.",
               },
               new()
               {
                   Name = "Text",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitChoiceGroupItem<TValue>.Text))",
                   Description = "Text to show as the content of BitChoiceGroup option.",
               },
               new()
               {
                   Name = "Value",
                   Type = "BitNameSelectorPair<TItem, TValue?>",
                   DefaultValue = "new(nameof(BitChoiceGroupItem<TValue>.Value))",
                   Description = "The value returned when BitChoiceGroup option is checked.",
               },
               new()
               {
                   Name = "Index",
                   Type = "string",
                   DefaultValue = "nameof(BitChoiceGroupItem<TValue>.Index))",
                   Description = "The Index field name of the custom input class. This property's value is set by the component at render.",
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "string",
                   DefaultValue = "nameof(BitChoiceGroupItem<TValue>.IsSelected))",
                   Description = "The IsSelected field name of the custom input class. This property's value is assigned by the component.",
               }
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitChoiceGroupClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitChoiceGroup.",
               },
               new()
               {
                   Name = "LabelContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label container of the BitChoiceGroup.",
               },
               new()
               {
                   Name = "Label",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label of the BitChoiceGroup.",
               },
               new()
               {
                   Name = "Container",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the container of the BitChoiceGroup.",
               },
               new()
               {
                   Name = "ItemChecked",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the checked item of the BitChoiceGroup.",
               },
               new()
               {
                   Name = "ItemContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the container of each item of the BitChoiceGroup.",
               },
               new()
               {
                   Name = "ItemLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label of each item of the BitChoiceGroup.",
               },
               new()
               {
                   Name = "ItemImageContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the image container of each item of the BitChoiceGroup.",
               },
               new()
               {
                   Name = "ItemImageWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the image wrapper of each item of the BitChoiceGroup.",
               },
               new()
               {
                   Name = "ItemRadioButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the radio button of each item of the BitChoiceGroup.",
               },
               new()
               {
                   Name = "ItemImage",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the image of each item of the BitChoiceGroup.",
               },
               new()
               {
                   Name = "ItemIconWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon wrapper of each item of the BitChoiceGroup.",
               },
               new()
               {
                   Name = "ItemIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of each item of the BitChoiceGroup.",
               },
               new()
               {
                   Name = "ItemPrefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the prefix of each item of the BitChoiceGroup.",
               },
               new()
               {
                   Name = "ItemTextWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the text wrapper of each item of the BitChoiceGroup.",
               },
               new()
               {
                   Name = "ItemText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the text of each item of the BitChoiceGroup.",
               },
            ]
        },
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Parameters =
            [
               new()
               {
                   Name = "Name",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the name of the icon."
               },
               new()
               {
                   Name = "BaseClass",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the base CSS class for the icon. For built-in Fluent UI icons, this defaults to \"bit-icon\". For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty."
               },
               new()
               {
                   Name = "Prefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the CSS class prefix used before the icon name. For built-in Fluent UI icons, this defaults to \"bit-icon--\". For external icon libraries, you might set this to \"fa-\" or leave empty."
               },
            ]
        }
    ];
}
