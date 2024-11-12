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
            Description = "Sets the data source that populates the items of the list."
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
            Name = "Styles",
            Type = "BitChoiceGroupClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitChoiceGroup.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
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
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The icon to show as content of the BitChoiceGroup item.",
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
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The icon to show as content of the BitChoiceGroup option.",
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
                   Name = "IconName",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitChoiceGroupItem<TValue>.IconName))",
                   Description = "The icon to show as content of the BitChoiceGroup option.",
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
                   Name = "ItemLabelWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label wrapper of each item of the BitChoiceGroup.",
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
                   Name = "ItemImageDiv",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the image div of each item of the BitChoiceGroup.",
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
                   Name = "ItemIconContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon container of each item of the BitChoiceGroup.",
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
        }
    ];
}
