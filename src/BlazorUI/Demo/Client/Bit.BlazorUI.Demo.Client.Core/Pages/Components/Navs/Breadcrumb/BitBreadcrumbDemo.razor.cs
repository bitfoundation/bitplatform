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
            Description = "Collapses the items that do not fit the width of the breadcrumb into the overflow menu, and brings them back as the room for them returns, so the trail always stays on a single line. MaxDisplayedItems, when it is set, still caps how many items the automatic collapsing may leave in the trail. It is turned off entirely by Wrap, since a trail that may flow onto another line has no items that do not fit."
        },
        new()
        {
            Name = "AutoReorderOptions",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the rendered order of the items in sync with the markup order of the options even when existing options are only reordered (not added or removed). It reads the DOM order of the options after each render, so it adds a JS interop call per render and is opt-in. It only affects the options API (ChildContent/Options)."
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
            Description = "The general color of the items and the divider of the breadcrumb.",
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
            Description = "Makes the overflow button put the collapsed items back into the trail instead of opening them in a menu. The whole trail is revealed at once and the button is gone with the collapsing it undid. The next change of the items or of the collapsing settings starts the breadcrumb over as collapsed."
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
            Description = "The maximum number of items to display before coalescing. If not specified, all of the items will be rendered."
        },
        new()
        {
            Name = "MaxItemWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The maximum width of the text of each item as a CSS length (for example \"8rem\"). The text of a longer item is truncated with an ellipsis, and the text of an item that carries no Title of its own becomes its tooltip so that the full text stays reachable."
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
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            Description = "Callback for when a breadcrumb item is clicked, no matter whether it is rendered as a link or as a button."
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
            Description = "Aria label of the overflow button and of the overflow menu."
        },
        new()
        {
            Name = "OverflowIndex",
            Type = "uint",
            DefaultValue = "0",
            Description = "Optional index where overflow items will be collapsed. It is the position the overflow button takes among the displayed items, and the items that collapse are the ones that start there, so the default of 0 collapses the trail from its root while 1 keeps the root visible and collapses the middle instead."
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
            Type = "string",
            DefaultValue= "More",
            Description = "The overflow icon name."
        },
        new()
        {
            Name = "OverflowIconTemplate",
            Type = "RenderFragment?",
            DefaultValue= "null",
            Description = "The custom template content to render each overflow icon."
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
            Name = "ReversedIcon",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the positions of the icon and the item text of the item content."
        },
        new()
        {
            Name = "SelectedItemAsText",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the selected item as plain text instead of as a link or a button, which is what the breadcrumb pattern asks of the page the user is already on. It keeps its aria-current either way, and the items around it stay actionable."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the items of the breadcrumb.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "StructuredData",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the trail as a schema.org BreadcrumbList in a JSON-LD script next to it, which is what search engines read to show the hierarchy of the page in their results. The whole hierarchy is written, including the items the overflow menu holds, and the Href of each item is resolved against the base address of the app."
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
            Description = "Lets a long breadcrumb trail wrap into multiple lines instead of overflowing its container in a single line. It turns AutoCollapse off while it is on, though a fixed MaxDisplayedItems still collapses what it is told to."
        }
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
                   Name = "ReversedIcon",
                   Type = "bool?",
                   Description = "Reverses the positions of the icon and the item text of the item content.",
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   Description = "Display the item as the selected item.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether an item is enabled or not.",
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
                   Name = "ReversedIcon",
                   Type = "bool?",
                   Description = "Reverses the positions of the icon and the item text of the item content.",
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   Description = "Display the breadcrumb option as the selected option.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether an option is enabled or not.",
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
                   Name = "ReversedIcon",
                   Type = "BitNameSelectorPair<TItem, bool?>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.ReversedIcon))",
                   Description = "The ReversedIcon field name and selector of the custom input class.",
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
                   Name = "IsEnabled",
                   Type = "BitNameSelectorPair<TItem, bool>",
                   DefaultValue = "new(nameof(BitBreadcrumbItem.IsEnabled))",
                   Description = "The IsEnabled field name and selector of the custom input class.",
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
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Primary", Description = "Primary general color.", Value = "0" },
                new() { Name = "Secondary", Description = "Secondary general color.", Value = "1" },
                new() { Name = "Tertiary", Description = "Tertiary general color.", Value = "2" },
                new() { Name = "Info", Description = "Info general color.", Value = "3" },
                new() { Name = "Success", Description = "Success general color.", Value = "4" },
                new() { Name = "Warning", Description = "Warning general color.", Value = "5" },
                new() { Name = "SevereWarning", Description = "SevereWarning general color.", Value = "6" },
                new() { Name = "Error", Description = "Error general color.", Value = "7" },
                new() { Name = "PrimaryBackground", Description = "Primary background color.", Value = "8" },
                new() { Name = "SecondaryBackground", Description = "Secondary background color.", Value = "9" },
                new() { Name = "TertiaryBackground", Description = "Tertiary background color.", Value = "10" },
                new() { Name = "PrimaryForeground", Description = "Primary foreground color.", Value = "11" },
                new() { Name = "SecondaryForeground", Description = "Secondary foreground color.", Value = "12" },
                new() { Name = "TertiaryForeground", Description = "Tertiary foreground color.", Value = "13" },
                new() { Name = "PrimaryBorder", Description = "Primary border color.", Value = "14" },
                new() { Name = "SecondaryBorder", Description = "Secondary border color.", Value = "15" },
                new() { Name = "TertiaryBorder", Description = "Tertiary border color.", Value = "16" }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Defines the sizes available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Small", Description = "The small size.", Value = "0" },
                new() { Name = "Medium", Description = "The medium size.", Value = "1" },
                new() { Name = "Large", Description = "The large size.", Value = "2" }
            ]
        }
    ];
}
