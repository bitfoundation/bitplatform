namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.Timeline;

public partial class BitTimelineDemo
{
    [CascadingParameter(Name = nameof(RenderForMcpClient))] public bool RenderForMcpClient { get; set; }

    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Alternate",
            Type = "bool",
            DefaultValue = "false",
            Description = "Alternates the side of the items, so each item sits on the opposite side of the line of the item before it.",
        },
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
            Name = "DotTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The default custom template for the dot of the items, used by the items that provide no dot template of their own.",
        },
        new()
        {
            Name = "Horizontal",
            Type = "bool",
            DefaultValue = "false",
            Description = "Defines whether to render the timeline items horizontally."
        },
        new()
        {
            Name = "Items",
            Type = "IEnumerable<TItem>",
            DefaultValue = "[]",
            Description = "The list of the items to render in the timeline, each one describing a single event.",
            LinkType = LinkType.Link,
            Href = "#timeline-item",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The default custom template that replaces the whole content of the items, used by the items that provide no template of their own.",
        },
        new()
        {
            Name = "LineVariant",
            Type = "BitTimelineLineVariant?",
            DefaultValue = "null",
            Description = "The way the connecting line of the timeline is painted, which the items can override one by one.",
            LinkType = LinkType.Link,
            Href = "#line-variant-enum",
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
            Description = "The callback that is called when an item of the timeline is clicked."
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
            Name = "ReverseOrder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the items in the reverse order, so the last item of the list is rendered first.",
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses all of the timeline items direction, so their contents swap sides of the line.",
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
            Name = "TruncateLine",
            Type = "BitTimelineTruncateLine?",
            DefaultValue = "null",
            Description = "Truncates the connecting line of the timeline at the first dot, the last dot, or both of them.",
            LinkType = LinkType.Link,
            Href = "#truncate-line-enum",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
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
            Id = "timeline-item",
            Title = "BitTimelineItem",
            Parameters =
            [
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The accessible label of the item, announced by assistive technologies.",
               },
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
                   Description = "The general color of the item, overriding the color of the timeline.",
                   LinkType = LinkType.Link,
                   Href = "#color-enum",
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
                   Name = "Icon",
                   Type = "BitIconInfo?",
                   DefaultValue = "null",
                   Description = "The icon to render in the item. Takes precedence over IconName.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
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
                   Name = "LineVariant",
                   Type = "BitTimelineLineVariant?",
                   DefaultValue = "null",
                   Description = "The way the connecting line of the item is painted, overriding the line variant of the timeline.",
                   LinkType = LinkType.Link,
                   Href = "#line-variant-enum",
               },
               new()
               {
                   Name = "OnClick",
                   Type = "Action<BitTimelineItem>?",
                   DefaultValue = "null",
                   Description = "Click event handler of the item.",
               },
               new()
               {
                   Name = "PrimaryContent",
                   Type = "RenderFragment<BitTimelineItem>?",
                   DefaultValue = "null",
                   Description = "The primary content of the item, rendered before the line.",
               },
               new()
               {
                   Name = "PrimaryText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The primary text of the item, rendered before the line.",
               },
               new()
               {
                   Name = "Reversed",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Reverses the item direction, so its contents swap sides of the line.",
               },
               new()
               {
                   Name = "SecondaryContent",
                   Type = "RenderFragment<BitTimelineItem>?",
                   DefaultValue = "null",
                   Description = "The secondary content of the item, rendered after the line.",
               },
               new()
               {
                   Name = "SecondaryText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The secondary text of the item, rendered after the line.",
               },
               new()
               {
                   Name = "Size",
                   Type = "BitSize?",
                   DefaultValue = "null",
                   Description = "The size of the item, overriding the size of the timeline.",
                   LinkType = LinkType.Link,
                   Href = "#timeline-size-enum",
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
                   Description = "The custom template that replaces the whole content of the item, dot and line included.",
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The value of the title attribute of the item, shown as the native tooltip.",
               },
               new()
               {
                   Name = "Variant",
                   Type = "BitVariant?",
                   DefaultValue = "null",
                   Description = "The visual variant of the item's dot, overriding the variant of the timeline.",
                   LinkType = LinkType.Link,
                   Href = "#variant-enum",
               }
            ]
        },
        new()
        {
            Id = "timeline-option",
            Title = "BitTimelineOption",
            Parameters =
            [
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The accessible label of the option, announced by assistive technologies.",
               },
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
                   Description = "The general color of the option, overriding the color of the timeline.",
                   LinkType = LinkType.Link,
                   Href = "#color-enum",
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
                   Name = "Icon",
                   Type = "BitIconInfo?",
                   DefaultValue = "null",
                   Description = "The icon to render in the option. Takes precedence over IconName.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
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
                   Name = "LineVariant",
                   Type = "BitTimelineLineVariant?",
                   DefaultValue = "null",
                   Description = "The way the connecting line of the option is painted, overriding the line variant of the timeline.",
                   LinkType = LinkType.Link,
                   Href = "#line-variant-enum",
               },
               new()
               {
                   Name = "OnClick",
                   Type = "EventCallback<BitTimelineOption>",
                   DefaultValue = "",
                   Description = "Click event handler of the option.",
               },
               new()
               {
                   Name = "PrimaryContent",
                   Type = "RenderFragment<BitTimelineOption>?",
                   DefaultValue = "null",
                   Description = "The primary content of the option, rendered before the line.",
               },
               new()
               {
                   Name = "PrimaryText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The primary text of the option, rendered before the line.",
               },
               new()
               {
                   Name = "Reversed",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Reverses the option direction, so its contents swap sides of the line.",
               },
               new()
               {
                   Name = "SecondaryContent",
                   Type = "RenderFragment<BitTimelineOption>?",
                   DefaultValue = "null",
                   Description = "The secondary content of the option, rendered after the line.",
               },
               new()
               {
                   Name = "SecondaryText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The secondary text of the option, rendered after the line.",
               },
               new()
               {
                   Name = "Size",
                   Type = "BitSize?",
                   DefaultValue = "null",
                   Description = "The size of the option, overriding the size of the timeline.",
                   LinkType = LinkType.Link,
                   Href = "#timeline-size-enum",
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
                   Description = "The custom template that replaces the whole content of the option, dot and line included.",
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The value of the title attribute of the option, shown as the native tooltip.",
               },
               new()
               {
                   Name = "Variant",
                   Type = "BitVariant?",
                   DefaultValue = "null",
                   Description = "The visual variant of the option's dot, overriding the variant of the timeline.",
                   LinkType = LinkType.Link,
                   Href = "#variant-enum",
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
                    Name = "AriaLabel",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitTimelineItem.AriaLabel))",
                    Description = "The AriaLabel field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
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
                    Name = "Color",
                    Type = "BitNameSelectorPair<TItem, BitColor?>",
                    DefaultValue = "new(nameof(BitTimelineItem.Color))",
                    Description = "The Color field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "DotTemplate",
                    Type = "BitNameSelectorPair<TItem, RenderFragment<TItem>?>",
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
                    Name = "Icon",
                    Type = "BitNameSelectorPair<TItem, BitIconInfo?>",
                    DefaultValue = "new(nameof(BitTimelineItem.Icon))",
                    Description = "Icon field name and selector of the custom input class.",
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
                    Name = "LineVariant",
                    Type = "BitNameSelectorPair<TItem, BitTimelineLineVariant?>",
                    DefaultValue = "new(nameof(BitTimelineItem.LineVariant))",
                    Description = "LineVariant field name and selector of the custom input class.",
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
                    Type = "BitNameSelectorPair<TItem, RenderFragment<TItem>?>",
                    DefaultValue = "new(nameof(BitTimelineItem.PrimaryContent))",
                    Description = "PrimaryContent field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "PrimaryText",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitTimelineItem.PrimaryText))",
                    Description = "PrimaryText field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Reversed",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitTimelineItem.Reversed))",
                    Description = "Reversed field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "SecondaryContent",
                    Type = "BitNameSelectorPair<TItem, RenderFragment<TItem>?>",
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
                    Name = "Size",
                    Type = "BitNameSelectorPair<TItem, BitSize?>",
                    DefaultValue = "new(nameof(BitTimelineItem.Size))",
                    Description = "The Size field name and selector of the custom input class.",
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
                    Type = "BitNameSelectorPair<TItem, RenderFragment<TItem>?>",
                    DefaultValue = "new(nameof(BitTimelineItem.Template))",
                    Description = "Template field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Title",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitTimelineItem.Title))",
                    Description = "The Title field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Variant",
                    Type = "BitNameSelectorPair<TItem, BitVariant?>",
                    DefaultValue = "new(nameof(BitTimelineItem.Variant))",
                    Description = "The Variant field name and selector of the custom input class.",
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
                   Description = "Custom CSS classes/styles for the root element of the BitTimeline."
               },
               new()
               {
                   Name = "Item",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item of the BitTimeline."
               },
               new()
               {
                   Name = "PrimaryContent",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the primary content of the BitTimeline."
               },
               new()
               {
                   Name = "PrimaryText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the primary text of the BitTimeline."
               },
               new()
               {
                   Name = "SecondaryContent",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the secondary content of the BitTimeline."
               },
               new()
               {
                   Name = "SecondaryText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the secondary text of the BitTimeline."
               },
               new()
               {
                   Name = "Divider",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the divider of the BitTimeline."
               },
               new()
               {
                   Name = "Dot",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the dot of the BitTimeline."
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the BitTimeline."
               }
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
            Description = "Determines the size of the dots and the font of the timeline.",
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
        new()
        {
            Id = "line-variant-enum",
            Name = "BitTimelineLineVariant",
            Description = "Determines how the connecting line of the timeline is painted.",
            Items =
            [
                new()
                {
                    Name= "Solid",
                    Description="An uninterrupted line.",
                    Value="0",
                },
                new()
                {
                    Name= "Dashed",
                    Description="A line drawn as a series of dashes, which usually marks a stretch of the timeline as pending or estimated.",
                    Value="1",
                },
                new()
                {
                    Name= "Dotted",
                    Description="A line drawn as a series of dots, a lighter version of the dashed line.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "truncate-line-enum",
            Name = "BitTimelineTruncateLine",
            Description = "Determines which ends of the connecting line of the timeline are truncated at the first and the last dot.",
            Items =
            [
                new()
                {
                    Name= "None",
                    Description="The line runs through the whole extent of the timeline, past the first and the last dot.",
                    Value="0",
                },
                new()
                {
                    Name= "Start",
                    Description="The line starts at the first dot instead of the leading edge of the timeline.",
                    Value="1",
                },
                new()
                {
                    Name= "End",
                    Description="The line ends at the last dot instead of the trailing edge of the timeline.",
                    Value="2",
                },
                new()
                {
                    Name= "Both",
                    Description="The line spans from the first dot to the last dot only.",
                    Value="3",
                }
            ]
        },
    ];
}
