namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.NavBar;

public partial class BitNavBarDemo
{
    [CascadingParameter(Name = nameof(RenderForMcpClient))] public bool RenderForMcpClient { get; set; }

    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Accent",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The accent color that fills the hovered and the selected item of the navbar. While it is not set, the selection is conveyed by the Color of the item alone.",
        },
        new()
        {
            Name = "Alignment",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "How the items are distributed along the navbar: packed at its start, its center or its end, or spread over it. While it is not set, the items of a horizontal navbar are spread evenly over its width and the items of a vertical rail are packed at its top. Baseline and Stretch carry no distribution of their own and are left at the default; use Justified to have the items fill the navbar.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "AutoReorderOptions",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the order of the registered options in sync with the markup order of the options, even when an option is added, removed or reordered conditionally after the first render (an option that shows up later registers itself at the end of the list, which leaves the keyboard moving between the items in another order than the one they are rendered in). This is achieved by reading the DOM order of the options after each render, so it adds a JS interop call per change and is opt-in.",
        },
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
            Type = "BitNavBarClassStyles?",
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
            Description = "The general color of the navbar, used for the icon, the text and the indicator of the selected item.",
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
            Description = "Renders the nav bar in a width to only fit its content."
        },
        new()
        {
            Name = "FooterTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content rendered after the items of the navbar, outside of the list they form: the trailing actions of a bar, or the account button at the bottom of a navigation rail."
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
            Name = "HeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content rendered before the items of the navbar, outside of the list they form: the logo or the menu button of a bar, or the button a navigation rail is conventionally headed with."
        },
        new()
        {
            Name = "HideUnselectedText",
            Type = "bool",
            DefaultValue = "false",
            Description = "Only renders the text of the selected item and leaves the rest of the items with their icon alone, which is how a navigation bar keeps its labels readable while holding more destinations. Ignored while IconOnly is enabled."
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
            Name = "Indicator",
            Type = "BitNavBarIndicator?",
            DefaultValue = "null",
            Description = "The shape of the indicator that marks the selected item: a line along the edge of the item, or the pill a Material navigation bar draws behind the icon of its current destination. While it is not set, the selection is conveyed by the color of the item and by the fill the Accent gives it.",
            LinkType = LinkType.Link,
            Href = "#indicator-enum",
        },
        new()
        {
            Name = "InlineText",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the icon and the text of each item side by side instead of stacking the text under the icon."
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
            Name = "ItemTemplateRenderMode",
            Type = "BitNavItemTemplateRenderMode",
            DefaultValue = "BitNavItemTemplateRenderMode.Normal",
            Description = "Whether the ItemTemplate is rendered inside the anchor (or the button) each item is, or replaces it altogether, which is what items that are controls of their own need, since an interactive element cannot be nested in another one. Replaced items are left out of the keyboard navigation of the navbar, and whatever they render owns its own clicks, its own focus and its own accessible name.",
            LinkType = LinkType.Link,
            Href = "#template-render-mode-enum",
        },
        new()
        {
            Name = "Justified",
            Type = "bool",
            DefaultValue = "false",
            Description = "Gives every item an equal share of the navbar so that the items evenly fill it, which is how a navigation bar keeps its destinations on a predictable grid. By default each item only takes the width of its own content."
        },
        new()
        {
            Name = "Match",
            Type = "BitNavMatch?",
            DefaultValue = "null",
            Description = "Modifies how the URL of an item is matched against the current URL in the automatic mode. The Match of an item takes precedence over this value, and the default is an exact match.",
            LinkType = LinkType.Link,
            Href = "#nav-match-enum",
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
            Type = "BitNavBarNameSelectors<TItem>?",
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
            Name = "SafeArea",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reserves the safe area of the device (the home indicator of a phone, for instance) under the navbar, so a bar pinned to the bottom of the screen is not overlapped by it."
        },
        new()
        {
            Name = "Scrollable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets the items scroll along the navbar instead of being squeezed into it, which is what a bar (or a rail) holding more destinations than it has room for needs. The scrollbar itself is hidden, the items keep the size of their own content, and the selected one is scrolled into view as the selection moves."
        },
        new()
        {
            Name = "SelectedItem",
            Type = "TItem?",
            DefaultValue = "null",
            Description = "Selected item to show in the navbar. Supports two-way binding."
        },
        new()
        {
            Name = "SelectOnFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Selects an item as soon as the focus reaches it, so walking the navbar with the arrow keys switches the selection along with it, the way the tabs of a tab list do. It only applies to the Manual mode, where the selection is the navbar's own."
        },
        new()
        {
            Name = "SingleTabStop",
            Type = "bool",
            DefaultValue = "false",
            Description = "Takes the navbar out of the tab sequence as a single stop: only the selected item (or the first one, while nothing is selected) is tabbable and the arrow keys move between the items, exactly like a toolbar. By default every item is a tab stop of its own, the way the links of a navigation are."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the navbar.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitNavBarClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the navbar.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stacks the items of the navbar in a column, which turns it into a vertical navigation rail."
        },
        new()
        {
            Name = "WrapNavigation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets the arrow keys wrap around at both ends of the navbar, from the last item to the first one and back, the way the toolbar pattern does. By default the focus stops at the ends instead."
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "FocusItem",
            Type = "Func<TItem, ValueTask>",
            Description = "Moves the focus to an item of the navbar.",
        },
        new()
        {
            Name = "ScrollItemIntoView",
            Type = "Func<TItem, ValueTask>",
            Description = "Brings an item into the visible area of a Scrollable navbar, without selecting it or moving the focus onto it. It is a no-op on a navbar that does not scroll.",
        },
        new()
        {
            Name = "SelectItem",
            Type = "Func<TItem?, Task>",
            Description = "Selects an item programmatically, exactly like a click on that item would in the manual mode.",
        },
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
                   Name = "AdditionalUrls",
                   Type = "IEnumerable<string>?",
                   DefaultValue = "null",
                   Description = "Alternative URLs to be considered when auto mode tries to detect the selected navbar item by the current URL.",
               },
               new()
               {
                   Name = "AriaCurrent",
                   Type = "BitNavAriaCurrent",
                   DefaultValue = "BitNavAriaCurrent.Page",
                   Description = "The value of the aria-current attribute of the navbar item when it is the selected one.",
                   LinkType = LinkType.Link,
                   Href = "#aria-current-enum",
               },
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The accessible label of the navbar item, announced instead of its content. When it is not provided and the text of the item is not rendered (in the IconOnly or the HideUnselectedText modes), the text of the item is used, so an icon is never left unnamed.",
               },
               new()
               {
                   Name = "Badge",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The badge text to render on the icon of the navbar item, for a count or a short status. Takes precedence over Dot when both are set.",
               },
               new()
               {
                   Name = "BadgeAriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The accessible description of the badge (or the dot) of the navbar item, folded into the accessible name of the item so a count that only exists as a colored bubble is not lost on a screen reader. It falls back to the Badge text, and a Dot is only announced while this is set.",
               },
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
                   Name = "Dot",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Renders a small dot on the icon of the navbar item, to mark it as needing attention without showing a number. Ignored while Badge is set.",
               },
               new()
               {
                   Name = "Icon",
                   Type = "BitIconInfo?",
                   DefaultValue = "null",
                   Description = "Gets or sets the icon to display using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the name of the icon to display from the built-in Fluent UI icons.",
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
                   Name = "Match",
                   Type = "BitNavMatch?",
                   DefaultValue = "null",
                   Description = "Modifies how the URL of the navbar item is matched against the current URL in the automatic mode. Takes precedence over the Match of the navbar itself.",
                   LinkType = LinkType.Link,
                   Href = "#nav-match-enum",
               },
               new()
               {
                   Name = "SelectedIcon",
                   Type = "BitIconInfo?",
                   DefaultValue = "null",
                   Description = "The icon to display while the navbar item is the selected one, using custom CSS classes for external icon libraries. Takes precedence over SelectedIconName when both are set, and falls back to Icon / IconName while neither is set.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "SelectedIconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The name of the icon to display from the built-in Fluent UI icons while the navbar item is the selected one, which is how a navigation bar marks its current destination with the filled variant of the same glyph.",
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
                   Name = "TemplateRenderMode",
                   Type = "BitNavItemTemplateRenderMode",
                   DefaultValue = "BitNavItemTemplateRenderMode.Normal",
                   Description = "Whether the Template of the navbar item is rendered inside the anchor (or the button) the item is, or replaces it altogether, which is what an item that is a control of its own needs, since an interactive element cannot be nested in another one. A replaced item is left out of the keyboard navigation of the navbar.",
                   LinkType = LinkType.Link,
                   Href = "#template-render-mode-enum",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
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
                   Name = "AdditionalUrls",
                   Type = "IEnumerable<string>?",
                   DefaultValue = "null",
                   Description = "Alternative URLs to be considered when auto mode tries to detect the selected navbar option by the current URL.",
               },
               new()
               {
                   Name = "AriaCurrent",
                   Type = "BitNavAriaCurrent",
                   DefaultValue = "BitNavAriaCurrent.Page",
                   Description = "The value of the aria-current attribute of the navbar option when it is the selected one.",
                   LinkType = LinkType.Link,
                   Href = "#aria-current-enum",
               },
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The accessible label of the navbar option, announced instead of its content. When it is not provided and the text of the option is not rendered (in the IconOnly or the HideUnselectedText modes), the text of the option is used, so an icon is never left unnamed.",
               },
               new()
               {
                   Name = "Badge",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The badge text to render on the icon of the navbar option, for a count or a short status. Takes precedence over Dot when both are set.",
               },
               new()
               {
                   Name = "BadgeAriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The accessible description of the badge (or the dot) of the navbar option, folded into the accessible name of the option so a count that only exists as a colored bubble is not lost on a screen reader. It falls back to the Badge text, and a Dot is only announced while this is set.",
               },
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
                   Name = "Dot",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Renders a small dot on the icon of the navbar option, to mark it as needing attention without showing a number. Ignored while Badge is set.",
               },
               new()
               {
                   Name = "Icon",
                   Type = "BitIconInfo?",
                   DefaultValue = "null",
                   Description = "Gets or sets the icon to display using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the name of the icon to display from the built-in Fluent UI icons.",
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
                   Name = "Match",
                   Type = "BitNavMatch?",
                   DefaultValue = "null",
                   Description = "Modifies how the URL of the navbar option is matched against the current URL in the automatic mode. Takes precedence over the Match of the navbar itself.",
                   LinkType = LinkType.Link,
                   Href = "#nav-match-enum",
               },
               new()
               {
                   Name = "SelectedIcon",
                   Type = "BitIconInfo?",
                   DefaultValue = "null",
                   Description = "The icon to display while the navbar option is the selected one, using custom CSS classes for external icon libraries. Takes precedence over SelectedIconName when both are set, and falls back to Icon / IconName while neither is set.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "SelectedIconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The name of the icon to display from the built-in Fluent UI icons while the navbar option is the selected one, which is how a navigation bar marks its current destination with the filled variant of the same glyph.",
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
                   Name = "TemplateRenderMode",
                   Type = "BitNavItemTemplateRenderMode",
                   DefaultValue = "BitNavItemTemplateRenderMode.Normal",
                   Description = "Whether the Template of the navbar option is rendered inside the anchor (or the button) the option is, or replaces it altogether, which is what an option that is a control of its own needs, since an interactive element cannot be nested in another one. A replaced option is left out of the keyboard navigation of the navbar.",
                   LinkType = LinkType.Link,
                   Href = "#template-render-mode-enum",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
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
                   Name = "AdditionalUrls",
                   Type = "BitNameSelectorPair<TItem, IEnumerable<string>?>",
                   DefaultValue = "new(nameof(BitNavBarItem.AdditionalUrls))",
                   Description = "The AdditionalUrls field name and selector of the custom input class."
               },
               new()
               {
                   Name = "AriaCurrent",
                   Type = "BitNameSelectorPair<TItem, BitNavAriaCurrent?>",
                   DefaultValue = "new(nameof(BitNavBarItem.AriaCurrent))",
                   Description = "The AriaCurrent field name and selector of the custom input class."
               },
               new()
               {
                   Name = "AriaLabel",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavBarItem.AriaLabel))",
                   Description = "The AriaLabel field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Badge",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavBarItem.Badge))",
                   Description = "The Badge field name and selector of the custom input class."
               },
               new()
               {
                   Name = "BadgeAriaLabel",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavBarItem.BadgeAriaLabel))",
                   Description = "The BadgeAriaLabel field name and selector of the custom input class."
               },
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
                   Name = "Dot",
                   Type = "BitNameSelectorPair<TItem, bool?>",
                   DefaultValue = "new(nameof(BitNavBarItem.Dot))",
                   Description = "The Dot field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Icon",
                   Type = "BitNameSelectorPair<TItem, BitIconInfo?>",
                   DefaultValue = "new(nameof(BitNavBarItem.Icon))",
                   Description = "The Icon field name and selector of the custom input class. Maps to Icon for external icon libraries.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "IconName",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavBarItem.IconName))",
                   Description = "The IconName field name and selector of the custom input class. Maps to IconName for built-in Fluent UI icons."
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
                   Name = "Match",
                   Type = "BitNameSelectorPair<TItem, BitNavMatch?>",
                   DefaultValue = "new(nameof(BitNavBarItem.Match))",
                   Description = "The Match field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#nav-match-enum",
               },
               new()
               {
                   Name = "SelectedIcon",
                   Type = "BitNameSelectorPair<TItem, BitIconInfo?>",
                   DefaultValue = "new(nameof(BitNavBarItem.SelectedIcon))",
                   Description = "The SelectedIcon field name and selector of the custom input class. Maps to SelectedIcon for external icon libraries.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "SelectedIconName",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavBarItem.SelectedIconName))",
                   Description = "The SelectedIconName field name and selector of the custom input class. Maps to SelectedIconName for built-in Fluent UI icons."
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
                   Name = "TemplateRenderMode",
                   Type = "BitNameSelectorPair<TItem, BitNavItemTemplateRenderMode?>",
                   DefaultValue = "new(nameof(BitNavBarItem.TemplateRenderMode))",
                   Description = "The TemplateRenderMode field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#template-render-mode-enum",
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
                   Name = "Footer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the footer of the BitNavBar, rendered after the items."
               },
               new()
               {
                   Name = "Header",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header of the BitNavBar, rendered before the items."
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
                   Name = "ItemBadge",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the badge (or the dot) of the item of the BitNavBar."
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
                   Name = "ItemIconContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the wrapper of the icon and the badge of the item of the BitNavBar."
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
                   Name = "ItemWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the list item wrapping each item of the BitNavBar."
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
                    Description = "The navbar follows the browser: it selects the item whose URL points at the page the app currently sits on, and it re-selects on every navigation.",
                    Value = "0",
                },
                new()
                {
                    Name = "Manual",
                    Description = "The selection is driven by clicks and by the SelectedItem binding instead of by the current URL, which is what a navbar that switches between the panels of a single page needs.",
                    Value = "1",
                }
            ]
        },
        new()
        {
            Id = "nav-match-enum",
            Name = "BitNavMatch",
            Items =
            [
                new()
                {
                    Name = "Exact",
                    Description = "Specifies that the item should be active when it matches exactly the current URL.",
                    Value = "0",
                },
                new()
                {
                    Name = "Prefix",
                    Description = "Specifies that the item should be active when it matches any prefix of the current URL.",
                    Value = "1",
                },
                new()
                {
                    Name = "Regex",
                    Description = "Specifies that the item should be active when its provided regex matches the current URL.",
                    Value = "2",
                },
                new()
                {
                    Name = "Wildcard",
                    Description = "Specifies that the item should be active when its provided wildcard matches the current URL.",
                    Value = "3",
                }
            ]
        },
        new()
        {
            Id = "aria-current-enum",
            Name = "BitNavAriaCurrent",
            Items =
            [
                new() { Name = "Page", Description = "Represents the current page within a set of pages.", Value = "0" },
                new() { Name = "Step", Description = "Represents the current step within a process.", Value = "1" },
                new() { Name = "Location", Description = "Represents the current location within an environment or context.", Value = "2" },
                new() { Name = "Date", Description = "Represents the current date within a collection of dates.", Value = "3" },
                new() { Name = "Time", Description = "Represents the current time within a set of times.", Value = "4" },
                new() { Name = "True", Description = "Represents the current item within a set.", Value = "5" }
            ]
        },
        new()
        {
            Id = "template-render-mode-enum",
            Name = "BitNavItemTemplateRenderMode",
            Items =
            [
                new() { Name = "Normal", Description = "Renders the template inside the anchor (or the button) the item is, so the item keeps its click, its focus and its place in the keyboard navigation of the navbar.", Value = "0" },
                new() { Name = "Replace", Description = "Replaces the anchor (or the button) the item is with the template, which is what an item that is a control of its own needs. The template owns its clicks, its focus and its accessible name, and the item is left out of the keyboard navigation of the navbar.", Value = "1" }
            ]
        },
        new()
        {
            Id = "indicator-enum",
            Name = "BitNavBarIndicator",
            Items =
            [
                new() { Name = "None", Description = "No indicator of its own: the selection is conveyed by the color of the item and, while an Accent is set, by the fill of the item.", Value = "0" },
                new() { Name = "Line", Description = "A line drawn along the edge of the selected item: its bottom edge in a horizontal navbar and its leading edge in a vertical rail, the way a tab strip marks its current tab.", Value = "1" },
                new() { Name = "Pill", Description = "A pill drawn behind the icon of the selected item, which is how a Material navigation bar marks its current destination. It takes the fill off the item itself, so the pill is the only filled part.", Value = "2" }
            ]
        },
        new()
        {
            Id = "alignment-enum",
            Name = "BitAlignment",
            Items =
            [
                new() { Name = "Start", Description = "Packs the items at the start of the navbar.", Value = "0" },
                new() { Name = "End", Description = "Packs the items at the end of the navbar.", Value = "1" },
                new() { Name = "Center", Description = "Packs the items in the center of the navbar.", Value = "2" },
                new() { Name = "SpaceBetween", Description = "Spreads the items over the navbar, leaving no space before the first one and after the last one.", Value = "3" },
                new() { Name = "SpaceAround", Description = "Spreads the items over the navbar with equal space around each of them, which is what a horizontal navbar does on its own.", Value = "4" },
                new() { Name = "SpaceEvenly", Description = "Spreads the items over the navbar with equal space between them and at both of its ends.", Value = "5" },
                new() { Name = "Baseline", Description = "Carries no distribution of its own here, so the navbar keeps its default.", Value = "6" },
                new() { Name = "Stretch", Description = "Carries no distribution of its own here, so the navbar keeps its default. Use Justified to have the items fill the navbar.", Value = "7" }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Items =
            [
                new() { Name = "Small", Description = "The small size.", Value = "0" },
                new() { Name = "Medium", Description = "The medium size.", Value = "1" },
                new() { Name = "Large", Description = "The large size.", Value = "2" }
            ]
        }
    ];
}
