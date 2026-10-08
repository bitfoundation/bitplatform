namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public partial class BitBreadcrumbDemo
{
    [CascadingParameter(Name = nameof(RenderForMcpClient))] public bool RenderForMcpClient { get; set; }

    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoCollapse",
            Type = "bool",
            DefaultValue = "false",
            Description = "Collapses the items that do not fit into the overflow menu and brings them back as room returns, keeping the trail on one line; once only the last item is left, its text truncates. MaxDisplayedItems still caps the trail and Wrap turns it off."
        },
        new()
        {
            Name = "AutoReorderOptions",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the rendered order in sync with the markup order of the options even when they are only reordered. Costs one JS interop call per render; options API only."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the BitBreadcrumb, that are BitBreadcrumbOption components."
        },
        new()
        {
            Name = "Classes",
            Type = "BitBreadcrumbClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the breadcrumb.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the items and the divider of the breadcrumb. An explicit value wins over the --bit-Breadcrumb-* color variables; left unset, the trail keeps the theme's foreground colors unless they say otherwise.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "DividerIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Render a custom divider icon in place of the default chevron.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "DividerIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The divider icon name."
        },
        new()
        {
            Name = "DividerIconTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template content to render divider icon."
        },
        new()
        {
            Name = "DividerText",
            Type = "string?",
            DefaultValue = "null",
            Description = "A plain text divider (for example \"/\" or \"›\") to render in place of the default chevron icon. It is ignored when the DividerIconTemplate is provided."
        },
        new()
        {
            Name = "ExpandOverflow",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the overflow button reveal the collapsed items in place instead of opening a menu. The next change of the items or of the collapsing settings collapses the trail again."
        },
        new()
        {
            Name = "IconPlacement",
            Type = "BitPlacement?",
            DefaultValue = "null",
            Description = "Where the icon of each item is rendered relative to its text: before it (Start, the default) or after it (End). An item's own IconPlacement wins.",
            LinkType = LinkType.Link,
            Href = "#placement-enum",
        },
        new()
        {
            Name = "Items",
            Type = "IList<TItem>",
            DefaultValue = "[]",
            Description = "Collection of the items to render in the breadcrumb.",
            LinkType = LinkType.Link,
            Href = "#breadcrumb-item",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template content to render each item."
        },
        new()
        {
            Name = "MaxDisplayedItems",
            Type = "uint",
            DefaultValue = "0",
            Description = "The maximum number of items to display; the rest collapse into the overflow menu. 0 displays them all."
        },
        new()
        {
            Name = "MaxItemWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The maximum width of the text of each item as a CSS length (for example \"8rem\"). Longer text is truncated with an ellipsis and, when the item has no Title, becomes its tooltip."
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitBreadcrumbNameSelectors<TItem>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties.",
            LinkType = LinkType.Link,
            Href = "#name-selectors"
        },
        new()
        {
            Name = "NewTabHint",
            Type = "string?",
            DefaultValue = "null",
            Description = "Replaces the visually hidden \"(opens in a new tab)\" an item whose Target is _blank is announced with, e.g. to translate it. An empty value removes it.",
        },
        new()
        {
            Name = "NoNewTabHint",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the items whose Target is _blank from announcing that they open a new tab - only for where a visible label or heading already says so.",
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            Description = "Callback for when an item is clicked, whether it is rendered as a link or as a button."
        },
        new()
        {
            Name = "Options",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of the ChildContent."
        },
        new()
        {
            Name = "OverflowAriaLabel",
            Type = "string?",
            DefaultValue = "More items",
            Description = "Accessible label of the overflow button, which also names the menu it opens."
        },
        new()
        {
            Name = "OverflowIndex",
            Type = "uint",
            DefaultValue = "0",
            Description = "Where the overflow button sits among the displayed items; the collapsed items start there. 0 collapses from the root, 1 keeps the root and collapses the middle."
        },
        new()
        {
            Name = "OverflowIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Render a custom overflow icon in place of the default icon.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "OverflowIconName",
            Type = "string?",
            DefaultValue= "More",
            Description = "The overflow icon name."
        },
        new()
        {
            Name = "OverflowIconTemplate",
            Type = "RenderFragment?",
            DefaultValue= "null",
            Description = "The custom template content to render the overflow icon."
        },
        new()
        {
            Name = "OverflowTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue= "null",
            Description = "The custom template content to render each item in overflow list."
        },
        new()
        {
            Name = "Scrollable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets a long trail scroll sideways instead of overflowing its container, scrolled to its end so the current page is in view. Ignored while Wrap is on."
        },
        new()
        {
            Name = "SelectedItemAsText",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the selected item as plain text instead of as a link or a button. It keeps its aria-current."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the items of the breadcrumb. An explicit value wins over the --bit-Breadcrumb-* size variables; left unset, the breadcrumb is medium unless they say otherwise.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "StructuredData",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the whole trail, collapsed items included, as a schema.org BreadcrumbList JSON-LD script for search engines, with each Href resolved to an absolute URL."
        },
        new()
        {
            Name = "Styles",
            Type = "BitBreadcrumbClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the breadcrumb.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Wrap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets a long trail wrap onto more lines instead of overflowing its container. It turns AutoCollapse and Scrollable off; a fixed MaxDisplayedItems still applies."
        }
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Breadcrumb-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text of the items, in the trail and in the overflow menu. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-selected-color",
            DefaultValue = "--bit-Breadcrumb-color",
            Description = "Text of the current (selected) item. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-selected-font-weight",
            DefaultValue = "--bit-tpg-fw-semibold",
            Description = "Weight of the current (selected) item.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-disabled-color",
            DefaultValue = "--bit-clr-fg-dis",
            Description = "Text of a disabled item or of a disabled breadcrumb.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-hover-color",
            DefaultValue = "The color at rest",
            Description = "Text of a hovered item or menu item, and the glyph of a hovered overflow button. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-hover-background",
            DefaultValue = "--bit-clr-bg-pri-hover",
            Description = "Background of a hovered item, overflow button or menu item.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-active-background",
            DefaultValue = "--bit-clr-bg-pri-active",
            Description = "Background of a pressed item, overflow button or menu item.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-focus-color",
            DefaultValue = "--bit-clr-pri-focus",
            Description = "Focus indicator color. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-font-size",
            DefaultValue = "--bit-tpg-fs-sm",
            Description = "Text size of the items. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-item-height",
            DefaultValue = "--bit-siz-ctrl-md",
            Description = "Line height of the items, which sets the height of the trail. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-item-padding",
            DefaultValue = "0 8px",
            Description = "Padding of the items.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-item-gap",
            DefaultValue = "8px",
            Description = "Space between the icon and the text of an item.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-item-radius",
            DefaultValue = "--bit-shp-radius-control",
            Description = "Corner radius of the items and the overflow button.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-item-max-width",
            DefaultValue = "none",
            Description = "Width the text of an item truncates at. The MaxItemWidth parameter takes precedence and also adds the tooltips.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-divider-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the dividers and of the overflow button glyph. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-divider-size",
            DefaultValue = "--bit-tpg-fs-md",
            Description = "Size of the divider icons. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-divider-spacing",
            DefaultValue = "0",
            Description = "Space on each side of a divider.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-callout-background",
            DefaultValue = "--bit-clr-bg-pri",
            Description = "Background of the overflow menu.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-callout-radius",
            DefaultValue = "--bit-shp-radius-popup",
            Description = "Corner radius of the overflow menu.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-callout-shadow",
            DefaultValue = "--bit-shd-popup",
            Description = "Elevation of the overflow menu.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-callout-max-width",
            DefaultValue = "The width of the viewport",
            Description = "Widest the overflow menu gets before the text of its rows is truncated.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-overflow-item-height",
            DefaultValue = "--bit-siz-item-md",
            Description = "Height of a row of the overflow menu. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Breadcrumb-overflow-font-size",
            DefaultValue = "--bit-tpg-fs-sm",
            Description = "Text size of a row of the overflow menu. The Size parameter wins over it.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "breadcrumb-item",
            Title = "BitBreadcrumbItem",
            Parameters =
            [
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   Description = "A unique value to use as a key of the breadcrumb item.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   Description = "Text to display in the breadcrumb item.",
               },
               new()
               {
                   Name = "Href",
                   Type = "string?",
                   Description = "URL to navigate to when the breadcrumb item is clicked. If provided, the breadcrumb will be rendered as a link.",
               },
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   Description = "CSS class attribute for breadcrumb item.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   Description = "Style attribute for breadcrumb item.",
               },
               new()
               {
                   Name = "Icon",
                   Type = "BitIconInfo?",
                   Description = "Icon to render next to the item text.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   Description = "Name of an icon to render next to the item text.",
               },
               new()
               {
                   Name = "IconPlacement",
                   Type = "BitPlacement?",
                   Description = "Where the icon is rendered relative to the text, in place of the IconPlacement of the breadcrumb.",
                   LinkType = LinkType.Link,
                   Href = "#placement-enum",
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   Description = "Display the item as the selected item.",
               },
               new()
               {
                   Name = "IsDisabled",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Whether an item is disabled or not.",
               },
               new()
               {
                   Name = "OnClick",
                   Type = "Action<BitBreadcrumbItem>?",
                   Description = "Click event handler of the breadcrumb item.",
               },
               new()
               {
                   Name = "OverflowTemplate",
                   Type = "RenderFragment<BitBreadcrumbItem>?",
                   Description = "The custom template for the item in overflow list.",
               },
               new()
               {
                   Name = "Rel",
                   Type = "BitLinkRels?",
                   Description = "The rel attribute of the link of the breadcrumb item. A Target of _blank adds noopener to it, unless it already says NoOpener, NoReferrer or Opener; noreferrer is never added on its own.",
               },
               new()
               {
                   Name = "Target",
                   Type = "string?",
                   Description = "The target of the link of the breadcrumb item (for example \"_blank\"), applied when the Href is provided.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitBreadcrumbItem>?",
                   Description = "The custom template for the item.",
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   Description = "The title (tooltip) of the breadcrumb item, useful to reveal the full text of a truncated item.",
               },
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   Description = "The accessible label of the breadcrumb item, replacing its text content for assistive technologies.",
               }
            ]
        },
        new()
        {
            Id = "breadcrumb-option",
            Title = "BitBreadcrumbOption",
            Parameters =
            [
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   Description = "A unique value to use as a key of the breadcrumb option.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   Description = "Text to display in the breadcrumb option.",
               },
               new()
               {
                   Name = "Href",
                   Type = "string?",
                   Description = "URL to navigate to when the breadcrumb option is clicked. If provided, the breadcrumb will be rendered as a link.",
               },
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   Description = "CSS class attribute for breadcrumb option.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   Description = "Style attribute for breadcrumb option.",
               },
               new()
               {
                   Name = "Icon",
                   Type = "BitIconInfo?",
                   Description = "Icon to render next to the item text.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   Description = "Name of an icon to render next to the item text.",
               },
               new()
               {
                   Name = "IconPlacement",
                   Type = "BitPlacement?",
                   Description = "Where the icon is rendered relative to the text, in place of the IconPlacement of the breadcrumb.",
                   LinkType = LinkType.Link,
                   Href = "#placement-enum",
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   Description = "Display the breadcrumb option as the selected option.",
               },
               new()
               {
                   Name = "IsDisabled",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Whether an option is disabled or not.",
               },
               new()
               {
                   Name = "OnClick",
                   Type = "EventCallback<BitBreadcrumbOption>",
                   Description = "Click event handler of the breadcrumb option.",
               },
               new()
               {
                   Name = "OverflowTemplate",
                   Type = "RenderFragment<BitBreadcrumbOption>?",
                   Description = "The custom template for the option in overflow list.",
               },
               new()
               {
                   Name = "Rel",
                   Type = "BitLinkRels?",
                   Description = "The rel attribute of the link of the breadcrumb option. A Target of _blank adds noopener to it, unless it already says NoOpener, NoReferrer or Opener; noreferrer is never added on its own.",
               },
               new()
               {
                   Name = "Target",
                   Type = "string?",
                   Description = "The target of the link of the breadcrumb option (for example \"_blank\"), applied when the Href is provided.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitBreadcrumbOption>?",
                   Description = "The custom template for the option.",
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   Description = "The title (tooltip) of the breadcrumb option, useful to reveal the full text of a truncated option.",
               },
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   Description = "The accessible label of the breadcrumb option, replacing its text content for assistive technologies.",
               }
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitBreadcrumbClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitBreadcrumb.",
               },
               new()
               {
                   Name = "Overlay",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the overlay of the BitBreadcrumb.",
               },
               new()
               {
                   Name = "ItemContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item container of the BitBreadcrumb."
               },
               new()
               {
                   Name = "OverflowButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the overflow button of the BitBreadcrumb."
               },
               new()
               {
                   Name = "OverflowButtonIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the overflow button icon of the BitBreadcrumb."
               },
               new()
               {
                   Name = "ItemWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item wrapper of the BitBreadcrumb."
               },
               new()
               {
                   Name = "Item",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each item of the BitBreadcrumb."
               },
               new()
               {
                   Name = "ItemIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each item icon of the BitBreadcrumb."
               },
               new()
               {
                   Name = "ItemText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each item text of the BitBreadcrumb."
               },
               new()
               {
                   Name = "SelectedItem",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the selected item of the BitBreadcrumb."
               },
               new()
               {
                   Name = "Divider",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the divider of the BitBreadcrumb."
               },
               new()
               {
                   Name = "DividerIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the divider icon of the BitBreadcrumb."
               },
               new()
               {
                   Name = "Callout",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the callout element of the BitBreadcrumb."
               },
               new()
               {
                   Name = "CalloutContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the callout container of the BitBreadcrumb."
               },
               new()
               {
                   Name = "OverflowItemWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the overflow item wrapper of the BitBreadcrumb."
               },
               new()
               {
                   Name = "OverflowItem",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each overflow item of the BitBreadcrumb."
               },
               new()
               {
                   Name = "OverflowItemIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each overflow item icon of the BitBreadcrumb."
               },
               new()
               {
                   Name = "OverflowItemText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each overflow item text of the BitBreadcrumb."
               },
               new()
               {
                   Name = "OverflowSelectedItem",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the overflow selected item of the BitBreadcrumb."
               }
            ],
        },
        new()
        {
            Id = "name-selectors",
            Title = "BitBreadcrumbNameSelectors<TItem>",
            Parameters =
            [
               new()
               {
                   Name = "AriaLabel",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.AriaLabel))",
                   Description = "The AriaLabel field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Key",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.Key))",
                   Description = "The Key field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Text",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.Text))",
                   Description = "The Text field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Href",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.Href))",
                   Description = "The Href field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Class",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.Class))",
                   Description = "The CSS Class field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Style",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.Style))",
                   Description = "The CSS Style field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Icon",
                   Type = "BitNameSelectorPair<TItem, BitIconInfo?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.Icon))",
                   Description = "The Icon field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "IconName",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.IconName))",
                   Description = "The IconName field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "IconPlacement",
                   Type = "BitNameSelectorPair<TItem, BitPlacement?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.IconPlacement))",
                   Description = "The IconPlacement field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "BitNameSelectorPair<TItem, bool>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.IsSelected))",
                   Description = "The IsSelected field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "IsDisabled",
                   Type = "BitNameSelectorPair<TItem, bool>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.IsDisabled))",
                   Description = "The IsDisabled field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "OnClick",
                   Type = "Action<TItem>?",
                   Description = "Click event handler of the item.",
               },
               new()
               {
                   Name = "OverflowTemplate",
                   Type = "BitNameSelectorPair<TItem, RenderFragment<TItem>?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.OverflowTemplate))",
                   Description = "The OverflowTemplate field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Rel",
                   Type = "BitNameSelectorPair<TItem, BitLinkRels?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.Rel))",
                   Description = "The Rel field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair",
               },
               new()
               {
                   Name = "Target",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.Target))",
                   Description = "The Target field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Template",
                   Type = "BitNameSelectorPair<TItem, RenderFragment<TItem>?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.Template))",
                   Description = "The Template field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               },
               new()
               {
                   Name = "Title",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.Title))",
                   Description = "The Title field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#name-selector-pair"
               }
            ],
        },
        new()
        {
            Id = "name-selector-pair",
            Title = "BitNameSelectorPair<TItem, TProp>",
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
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Description = "Represents icon information for rendering icons. Supports built-in Fluent UI icons and external icon libraries (FontAwesome, Bootstrap Icons, etc.). Use BitIconInfo.Css(\"fa-solid fa-star\"), BitIconInfo.Fa(\"solid star\"), or BitIconInfo.Bi(\"star-fill\") for external icons.",
            Parameters =
            [
                new()
                {
                    Name = "Name",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the name of the icon. For external icons, this can be the full CSS class name if BaseClass and Prefix are empty."
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
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        DemoSharedEnums.BitColor(),
        DemoSharedEnums.BitPlacement(),
        DemoSharedEnums.BitSize()
    ];
}
