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
            Description = "The general color of the timeline. An explicit value (or the color of an item) wins over the --bit-Timeline-* dot color variables; left unset, the timeline is primary unless they say otherwise.",
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
            Name = "DotAlignment",
            Type = "BitPlacement?",
            DefaultValue = "null",
            Description = "Where the dot of each item sits along its item, with the contents aligned to it: at its middle (Center, the default), its Start (the top in a vertical timeline) or its End (the bottom in a vertical timeline). Start pins the dot to the first line of multi-line contents. Only Center, Start and End are honoured; every other value renders the default Center.",
            LinkType = LinkType.Link,
            Href = "#placement-enum",
        },
        new()
        {
            Name = "Horizontal",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the timeline horizontally."
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
            Name = "LineStyle",
            Type = "BitLineStyle?",
            DefaultValue = "null",
            Description = "The way the connecting line of the timeline is painted, which the items can override one by one. Only Solid, Dashed and Dotted are drawn: the connector is a hairline, which leaves Double no room for its two strokes, so it is drawn solid.",
            LinkType = LinkType.Link,
            Href = "#line-style-enum",
        },
        new()
        {
            Name = "LinePlacement",
            Type = "BitPlacement?",
            DefaultValue = "null",
            Description = "Where the connecting line runs: through the middle (Center, the default), with the primary contents on one side of it and the secondary ones on the other, or along the Start edge (the top in a horizontal timeline) or the End edge (the bottom in a horizontal timeline), with the contents of each item stacked beside it. Reversed and Alternate only apply to the centered line. Only Center, Start and End are honoured; every other value renders the default Center.",
            LinkType = LinkType.Link,
            Href = "#placement-enum",
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
            Description = "The callback that is called when an item is clicked. A clickable item is a button whose contents are read as its name, so it should hold no links or controls of its own."
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
            Description = "Renders the items in the reverse order, so the last item of the list is painted first. The reading and the focus order keep the order of the list.",
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
            Description = "The size of the timeline, which sets the size of the dots and of the text. An explicit value (or the size of an item) wins over --bit-Timeline-font-size and --bit-Timeline-dot-size; left unset, the timeline is medium unless they say otherwise.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
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

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Timeline-font-size",
            DefaultValue = "--bit-tpg-fs-sm",
            Description = "Text size of the contents. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Timeline-dot-size",
            DefaultValue = "spacing(3.75)",
            Description = "Diameter of the dot, and of the hidden dot's placeholder. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Timeline-dot-background",
            DefaultValue = "--bit-clr-pri (Fill), transparent (Outline, Text)",
            Description = "Fill of the dot. A disabled item keeps its disabled fill. The Color parameter wins over it in Fill; the transparent dot of Outline and Text is its alone.",
        },
        new()
        {
            Name = "--bit-Timeline-dot-border-color",
            DefaultValue = "--bit-clr-pri, transparent (Text)",
            Description = "Border color of the dot. A disabled item keeps its disabled border. The Color parameter wins over it, except over the transparent border of Text.",
        },
        new()
        {
            Name = "--bit-Timeline-dot-border-width",
            DefaultValue = "--bit-shp-border-width",
            Description = "Border width of the dot.",
        },
        new()
        {
            Name = "--bit-Timeline-dot-radius",
            DefaultValue = "--bit-shp-radius-full",
            Description = "Corner radius of the dot; a smaller one turns it into a rounded square.",
        },
        new()
        {
            Name = "--bit-Timeline-dot-shadow",
            DefaultValue = "none",
            Description = "Shadow of the dot, e.g. a halo that sets it off the line.",
        },
        new()
        {
            Name = "--bit-Timeline-icon-color",
            DefaultValue = "--bit-clr-pri-text (Fill), --bit-clr-pri (Outline, Text)",
            Description = "Color of the icon inside the dot. A disabled item keeps its disabled color. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Timeline-icon-size",
            DefaultValue = "1em",
            Description = "Size of the icon inside the dot.",
        },
        new()
        {
            Name = "--bit-Timeline-line-color",
            DefaultValue = "--bit-clr-brd-sec",
            Description = "Color of the connecting line, whatever its LineStyle.",
        },
        new()
        {
            Name = "--bit-Timeline-line-width",
            DefaultValue = "--bit-shp-border-width-thick",
            Description = "Thickness of the connecting line.",
        },
        new()
        {
            Name = "--bit-Timeline-content-gap",
            DefaultValue = "Half the dot size",
            Description = "Room between the line and the contents on either side of it.",
        },
        new()
        {
            Name = "--bit-Timeline-item-spacing",
            DefaultValue = "Half the dot size",
            Description = "Room each item keeps around its contents along the line; two neighbours sit twice this apart.",
        },
        new()
        {
            Name = "--bit-Timeline-item-radius",
            DefaultValue = "--bit-shp-radius-control",
            Description = "Corner radius of a clickable item's hover plate and focus ring.",
        },
        new()
        {
            Name = "--bit-Timeline-item-hover-background",
            DefaultValue = "--bit-clr-bg-pri-hover",
            Description = "Hover plate of a clickable item.",
        },
        new()
        {
            Name = "--bit-Timeline-item-active-background",
            DefaultValue = "--bit-clr-bg-pri-active",
            Description = "Pressed plate of a clickable item.",
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
                   Description = "The general color of the item, overriding the color of the timeline. An explicit value wins over the Color of the timeline and over the --bit-Timeline-* dot color variables, on this item alone; left unset, the item takes the Color of the timeline, or is primary unless the variables say otherwise.",
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
                   Name = "IsDisabled",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Whether or not the item is disabled.",
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
                   Name = "LineStyle",
                   Type = "BitLineStyle?",
                   DefaultValue = "null",
                   Description = "The way the connecting line of the item is painted, overriding the line style of the timeline. Only Solid, Dashed and Dotted are drawn; Double is drawn solid.",
                   LinkType = LinkType.Link,
                   Href = "#line-style-enum",
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
                   Description = "The size of the item, overriding the size of the timeline. An explicit value wins over the Size of the timeline and over --bit-Timeline-font-size and --bit-Timeline-dot-size, on this item alone; left unset, the item takes the Size of the timeline, or is medium unless the variables say otherwise.",
                   LinkType = LinkType.Link,
                   Href = "#size-enum",
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
                   Description = "The general color of the option, overriding the color of the timeline. An explicit value wins over the Color of the timeline and over the --bit-Timeline-* dot color variables, on this option alone; left unset, the option takes the Color of the timeline, or is primary unless the variables say otherwise.",
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
                   Name = "IsDisabled",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Whether or not the option is disabled.",
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
                   Name = "LineStyle",
                   Type = "BitLineStyle?",
                   DefaultValue = "null",
                   Description = "The way the connecting line of the option is painted, overriding the line style of the timeline. Only Solid, Dashed and Dotted are drawn; Double is drawn solid.",
                   LinkType = LinkType.Link,
                   Href = "#line-style-enum",
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
                   Description = "The size of the option, overriding the size of the timeline. An explicit value wins over the Size of the timeline and over --bit-Timeline-font-size and --bit-Timeline-dot-size, on this option alone; left unset, the option takes the Size of the timeline, or is medium unless the variables say otherwise.",
                   LinkType = LinkType.Link,
                   Href = "#size-enum",
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
                    Description = "The Color field name and selector of the custom input class. Like the Color of an item, the value it reads wins over the Color of the timeline and over the --bit-Timeline-* dot color variables.",
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
                    Name = "IsDisabled",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitTimelineItem.IsDisabled))",
                    Description = "IsDisabled field name and selector of the custom input class.",
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
                    Name = "LineStyle",
                    Type = "BitNameSelectorPair<TItem, BitLineStyle?>",
                    DefaultValue = "new(nameof(BitTimelineItem.LineStyle))",
                    Description = "LineStyle field name and selector of the custom input class.",
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
                    Description = "The Size field name and selector of the custom input class. Like the Size of an item, the value it reads wins over the Size of the timeline and over --bit-Timeline-font-size and --bit-Timeline-dot-size.",
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
        DemoSharedEnums.BitVariant(),
        DemoSharedEnums.BitColor(),
        DemoSharedEnums.BitSize(description: "Determines the size of the dots and the font of the timeline."),
        DemoSharedEnums.BitLineStyle(),
        DemoSharedEnums.BitPlacement(),
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
