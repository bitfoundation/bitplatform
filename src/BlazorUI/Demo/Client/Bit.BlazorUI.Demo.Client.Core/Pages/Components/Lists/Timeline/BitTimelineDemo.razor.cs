namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.Timeline;

public partial class BitTimelineDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the BitTimeline, that are BitTimelineOption components.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitTimelineClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitTimeline.",
            LinkType = LinkType.Link,
            Href = "#timeline-class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the timeline.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Horizontal",
            Type = "bool",
            DefaultValue = "false",
            Description = "Defines whether to render timeline children horizontaly."
        },
        new()
        {
            Name = "Items",
            Type = "IEnumerable<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "List of Item, each of which can be a timeline with different action in the timeline.",
            LinkType = LinkType.Link,
            Href = "#timeline-group-items",
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitTimelineNameSelectors<TItem>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties.",
            LinkType = LinkType.Link,
            Href = "#name-selectors",
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            Description = "The callback that is called when a timeline is clicked."
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
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses all of the timeline items direction.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of timeline, Possible values: Small | Medium | Large",
            LinkType = LinkType.Link,
            Href = "#timeline-size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitTimelineClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitTimeline.",
            LinkType = LinkType.Link,
            Href = "#timeline-class-styles",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant",
            DefaultValue = "null",
            Description = "The visual variant of the timeline.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "timeline-group-items",
            Title = "BitTimelineItem",
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
                   Name = "Color",
                   Type = "BitColor?",
                   DefaultValue = "null",
                   Description = "The general color of the item."
               },
               new()
               {
                   Name = "DotTemplate",
                   Type = "RenderFragment<BitTimelineItem>?",
                   DefaultValue = "null",
                   Description = "The custom template for the item's dot.",
               },
               new()
               {
                   Name = "HideDot",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Hides the item's dot.",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render in the item.",
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
                   Name = "OnClick",
                   Type = "EventCallback",
                   DefaultValue = "",
                   Description = "Click event handler of the item.",
               },
               new()
               {
                   Name = "PrimaryContent",
                   Type = "RenderFragment<BitTimelineItem>?",
                   DefaultValue = "null",
                   Description = "The primary content of the item.",
               },
               new()
               {
                   Name = "PrimaryText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The primary text of the item.",
               },
               new()
               {
                   Name = "Reversed",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Reverses the item direction.",
               },
               new()
               {
                   Name = "SecondaryContent",
                   Type = "RenderFragment<BitTimelineItem>?",
                   DefaultValue = "null",
                   Description = "The secondary content of the item.",
               },
               new()
               {
                   Name = "SecondaryText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The secondary text of the item.",
               },
               new()
               {
                   Name = "Size",
                   Type = "BitSize?",
                   DefaultValue = "null",
                   Description = "The size of the item."
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
                   Type = "RenderFragment<BitTimelineItem>?",
                   DefaultValue = "null",
                   Description = "The custom template for the item.",
               }
            ]
        },
        new()
        {
            Id = "timeline-group-options",
            Title = "BitTimelineOption",
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
                   Name = "Color",
                   Type = "BitColor?",
                   DefaultValue = "null",
                   Description = "The general color of the option."
               },
               new()
               {
                   Name = "DotTemplate",
                   Type = "RenderFragment<BitTimelineOption>?",
                   DefaultValue = "null",
                   Description = "The custom template for the option's dot.",
               },
               new()
               {
                   Name = "HideDot",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Hides the option's dot.",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render in the option.",
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
                   Description = "A unique value to use as a Key of the option.",
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
                   Name = "PrimaryContent",
                   Type = "RenderFragment<BitTimelineOption>?",
                   DefaultValue = "null",
                   Description = "The primary content of the option.",
               },
               new()
               {
                   Name = "PrimaryText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The primary text of the option.",
               },
               new()
               {
                   Name = "Reversed",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Reverses the option direction.",
               },
               new()
               {
                   Name = "SecondaryContent",
                   Type = "RenderFragment<BitTimelineOption>?",
                   DefaultValue = "null",
                   Description = "The secondary content of the option.",
               },
               new()
               {
                   Name = "SecondaryText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The secondary text of the option.",
               },
               new()
               {
                   Name = "Size",
                   Type = "BitSize?",
                   DefaultValue = "null",
                   Description = "The size of the option."
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
                   Type = "RenderFragment<BitTimelineOption>?",
                   DefaultValue = "null",
                   Description = "The custom template for the option.",
               }
            ]
        },
        new()
        {
            Id = "name-selectors",
            Title = "BitTimelineNameSelectors",
            Parameters =
            [
                new()
                {
                    Name = "Class",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitTimelineItem.Class))",
                    Description = "The CSS Class field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "DotTemplate",
                    Type = "BitNameSelectorPair<TItem, RenderFragment?>",
                    DefaultValue = "new(nameof(BitTimelineItem.DotTemplate))",
                    Description = "DotTemplate field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "HideDot",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitTimelineItem.HideDot))",
                    Description = "HideDot field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "IconName",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitTimelineItem.IconName))",
                    Description = "IconName field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "IsEnabled",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitTimelineItem.IsEnabled))",
                    Description = "IsEnabled field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Key",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitTimelineItem.Key))",
                    Description = "Key field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "OnClick",
                    Type = "BitNameSelectorPair<TItem, Action<TItem>?>",
                    DefaultValue = "new(nameof(BitTimelineItem.OnClick))",
                    Description = "OnClick field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "PrimaryContent",
                    Type = "BitNameSelectorPair<TItem, RenderFragment?>",
                    DefaultValue = "new(nameof(BitTimelineItem.PrimaryContent))",
                    Description = "PrimaryContent field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Reversed",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitTimelineItem.Reversed))",
                    Description = "Reversed field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "IsEnabled",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitTimelineItem.IsEnabled))",
                    Description = "IsEnabled field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "SecondaryContent",
                    Type = "BitNameSelectorPair<TItem, RenderFragment?>",
                    DefaultValue = "new(nameof(BitTimelineItem.SecondaryContent))",
                    Description = "SecondaryContent field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "SecondaryText",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitTimelineItem.SecondaryText))",
                    Description = "SecondaryText field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Style",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitTimelineItem.Style))",
                    Description = "Style field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Template",
                    Type = "BitNameSelectorPair<TItem, RenderFragment?>",
                    DefaultValue = "new(nameof(BitTimelineItem.Template))",
                    Description = "Template field name and selector of the custom input class.",
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
        new()
        {
            Id = "timeline-class-styles",
            Title = "BitTimelineClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitButton."
               },
               new()
               {
                   Name = "Item",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item of the BitButton."
               },
               new()
               {
                   Name = "PrimaryContent",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the primary content of the BitButton."
               },
               new()
               {
                   Name = "PrimaryText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the primary text of the BitButton."
               },
               new()
               {
                   Name = "SecondaryContent",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the secondary content of the BitButton."
               },
               new()
               {
                   Name = "SecondaryText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the secondary text of the BitButton."
               },
               new()
               {
                   Name = "Divider",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the divider of the BitButton."
               },
               new()
               {
                   Name = "Dot",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the dot of the BitButton."
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the BitButton."
               }
            ]
        }
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
                    Name = "Fill",
                    Description = "Fill styled variant.",
                    Value = "0",
                },
                new()
                {
                    Name = "Outline",
                    Description = "Outline styled variant.",
                    Value = "1",
                },
                new()
                {
                    Name = "Text",
                    Description = "Text styled variant.",
                    Value = "2",
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
                }
            ]
        },
        new()
        {
            Id = "timeline-size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size timeline.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size timeline.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size timeline.",
                    Value="2",
                }
            ]
        },
    ];
}
