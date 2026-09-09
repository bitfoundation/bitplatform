namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.NavPanel;

public partial class BitNavPanelDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Accent",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The accent color of the nav.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "AllExpanded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Expands all items on first render."
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Moves the focus into the nav panel as it opens - onto the search box, or onto the first item of a panel without one.",
        },
        new()
        {
            Name = "ChevronDownIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the chevron-down element of each nav item, for icons of external libraries. It takes precedence over ChevronDownIconName.",
            Href = "#bit-icon-info",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "ChevronDownIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom icon name of the chevron-down element of each nav item.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitNavPanelClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the nav panel.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "CollapseAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The default aria-label of the expand/collapse button of an expanded item of the nav.",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the nav.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "DefaultSelectedItem",
            Type = "TItem?",
            DefaultValue = "null",
            Description = "The initially selected item of the nav in manual mode.",
        },
        new()
        {
            Name = "EmptyListTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for when the search result is empty.",
        },
        new()
        {
            Name = "EmptyListMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom message for when the search result is empty.",
        },
        new()
        {
            Name = "ExpandAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The default aria-label of the expand/collapse button of a collapsed item of the nav.",
        },
        new()
        {
            Name = "ExpandOnHover",
            Type = "bool",
            DefaultValue = "false",
            Description = "Expands the toggled (rail) nav panel back to its full width while the pointer is over it.",
        },
        new()
        {
            Name = "FitWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the nav panel with fit-content width.",
        },
        new()
        {
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render as the footer of the nav panel.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the nav panel with full (100%) width.",
        },
        new()
        {
            Name = "Header",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render as the header of the nav panel.",
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "Used to customize how content inside the group header is rendered."
        },
        new()
        {
            Name = "HeaderTemplateRenderMode",
            Type = "BitNavItemTemplateRenderMode",
            DefaultValue = "BitNavItemTemplateRenderMode.Normal",
            Description = "The render mode of the custom HeaderTemplate.",
            LinkType = LinkType.Link,
            Href = "#nav-itemtemplate-rendermode",
        },
        new()
        {
            Name = "HideToggle",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the toggle button.",
        },
        new()
        {
            Name = "IconNavUrl",
            Type = "string?",
            DefaultValue = "null",
            Description = "Renders an anchor wrapping the icon to navigate to the specified url.",
        },
        new()
        {
            Name = "IconUrl",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon url to show in the header of the nav panel.",
        },
        new()
        {
            Name = "IndentValue",
            Type = "int",
            DefaultValue = "16",
            Description = "The indentation value in px for each level of depth of child item."
        },
        new()
        {
            Name = "IndentPadding",
            Type = "int",
            DefaultValue = "27",
            Description = "The indentation padding in px for items without children (compensation space for chevron icon)."
        },
        new()
        {
            Name = "IndentReversedPadding",
            Type = "int",
            DefaultValue = "4",
            Description = "The indentation padding in px for items in reversed mode."
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines if the nav panel is open in small screens.",
        },
        new()
        {
           Name = "IsToggled",
           Type = "bool",
           DefaultValue = "false",
           Description = "Determines if the nav panel is in the toggled state.",
        },
        new()
        {
            Name = "Items",
            Type = "IList<TItem>",
            DefaultValue = "[]",
            Description = "A collection of items to display in the nav panel.",
            Href = "#nav-item",
            LinkType = LinkType.Link,
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
            Description = "The render mode of the custom ItemTemplate.",
            LinkType = LinkType.Link,
            Href = "#nav-itemtemplate-rendermode",
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitNavNameSelectors<TItem>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties.",
            Href = "#name-selectors",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "NavClasses",
            Type = "BitNavClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the nav component of the nav panel.",
            Href = "#nav-class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "NavMatch",
            Type = "BitNavMatch?",
            DefaultValue = "null",
            Description = "Determines the global URL matching behavior of the nav.",
            LinkType = LinkType.Link,
            Href = "#nav-match-enum",
        },
        new()
        {
            Name = "NavMode",
            Type = "BitNavMode",
            DefaultValue = "BitNavMode.Automatic",
            Description = "Determines how the navigation will be handled.",
            LinkType = LinkType.Link,
            Href = "#nav-mode-enum",
        },
        new()
        {
           Name = "NavStyles",
           Type = "BitNavClassStyles?",
           DefaultValue = "null",
           Description = "Custom CSS styles for different parts of the nav component of the nav panel.",
           Href = "#nav-class-styles",
           LinkType = LinkType.Link,
        },
        new()
        {
            Name = "NoAutoClose",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the nav panel open when an item with a URL is clicked, instead of closing it.",
        },
        new()
        {
            Name = "NoCollapse",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables and hides all collapse/expand buttons of the nav component.",
        },
        new()
        {
            Name = "NoOverlay",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the overlay that is rendered behind the open nav panel in small screens.",
        },
        new()
        {
            Name = "NoPad",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables the padded mode of the nav panel.",
        },
        new()
        {
            Name = "NoSearchBox",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the search box from the nav panel.",
        },
        new()
        {
            Name = "NoSwipe",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables the swipe gesture that closes the open nav panel in small screens.",
        },
        new()
        {
            Name = "NoToggle",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables the toggle feature of the nav panel.",
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            DefaultValue = "",
            Description = "Event fired up when an item is clicked.",
        },
        new()
        {
            Name = "OnItemToggle",
            Type = "EventCallback<TItem>",
            Description = "Callback invoked when a group header is clicked and Expanded or Collapse."
        },
        new()
        {
            Name = "OnSearch",
            Type = "EventCallback<string?>",
            Description = "Callback invoked when the search text of the nav panel changes."
        },
        new()
        {
            Name = "OnSelectItem",
            Type = "EventCallback<TItem>",
            Description = "Callback invoked when an item is selected."
        },
        new()
        {
            Name = "RenderType",
            Type = "BitNavRenderType",
            DefaultValue = "BitNavRenderType.Normal",
            Description = "The way to render nav items.",
            LinkType = LinkType.Link,
            Href = "#nav-render-type-enum",
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
            Name = "ReversedChevron",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the location of the expander chevron."
        },
        new()
        {
            Name = "SearchBoxClasses",
            Type = "BitSearchBoxClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the search box of the nav panel.",
        },
        new()
        {
            Name = "SearchBoxPlaceholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder of the input element of the search box of the nav panel.",
        },
        new()
        {
            Name = "SearchBoxStyles",
            Type = "BitSearchBoxClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the search box of the nav panel.",
        },
        new()
        {
            Name = "SearchAnnouncementProvider",
            Type = "Func<int, string?>?",
            DefaultValue = "null",
            Description = "Builds the text that the screen reader announces through the live region of the nav panel whenever the search filters the items, in place of the built-in English announcement. The argument is the number of matched items.",
        },
        new()
        {
            Name = "SearchDebounceTime",
            Type = "int",
            DefaultValue = "500",
            Description = "The debounce time in milliseconds of the search box of the nav panel.",
        },
        new()
        {
            Name = "SearchFilter",
            Type = "Func<TItem, string, bool>?",
            DefaultValue = "null",
            Description = "The custom function to decide whether an item matches a search term, replacing the default matching over the text, the description and the data of an item.",
        },
        new()
        {
            Name = "SearchText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The search text of the nav panel that filters its items.",
        },
        new()
        {
            Name = "SelectedItem",
            Type = "TItem?",
            DefaultValue = "null",
            Description = "The selected item of the nav in manual mode.",
        },
        new()
        {
            Name = "SingleExpand",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the single-expand mode in the BitNav."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the nav items.",
            Href = "#size-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Styles",
            Type = "BitNavPanelClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the nav panel.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "ToggleAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria-label of the toggle button of the nav panel.",
        },
        new()
        {
            Name = "ToggledWidth",
            Type = "int",
            DefaultValue = "0",
            Description = "The width of the nav panel in px in its toggled (rail) state.",
        },
        new()
        {
            Name = "ToggleIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the toggle button of the nav panel. It takes precedence over ToggleIconName.",
            Href = "#bit-icon-info",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "ToggleIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon of the toggle button of the nav panel.",
        },
        new()
        {
            Name = "Top",
            Type = "int",
            DefaultValue = "0",
            Description = "The top CSS property value of the root element of the nav panel in px.",
        },
        new()
        {
            Name = "Width",
            Type = "int",
            DefaultValue = "0",
            Description = "The width of the nav panel in px. It is ignored in the FitWidth and FullWidth modes.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
         {
            Id = "class-styles",
            Title = "BitNavPanelClassStyles",
            Parameters=
            [
                new()
                {
                    Name = "Overlay",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the overlay of the BitNavPanel.",
                },
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitNavPanel.",
                },
                new()
                {
                    Name = "Toggled",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitNavPanel when toggled.",
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of the BitNavPanel.",
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header container of the BitNavPanel.",
                },
                new()
                {
                    Name = "HeaderIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header icon of the BitNavPanel.",
                },
                new()
                {
                    Name = "ToggleButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the toggle button of the BitNavPanel.",
                },
                new()
                {
                    Name = "SearchBox",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box of the BitNavPanel.",
                },
                new()
                {
                    Name = "ToggleSearchButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the toggle search button of the BitNavPanel.",
                },
                new()
                {
                    Name = "EmptyListMessage",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the empty list message of the BitNavPanel.",
                },
                new()
                {
                    Name = "Nav",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the nav component of the BitNavPanel.",
                },
            ]
        },
        new()
        {
            Id = "nav-item",
            Title = "BitNavItem",
            Parameters =
            [
               new()
               {
                   Name = "AriaCurrent",
                   Type = "BitNavAriaCurrent",
                   DefaultValue = "BitNavAriaCurrent.Page",
                   Description = "Aria-current token for active nav item. Must be a valid token value, and defaults to 'page'.",
                   Href = "#nav-aria-current-enum",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Aria label for nav item. Ignored if CollapseAriaLabel or ExpandAriaLabel is provided.",
               },
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS class for the nav item.",
               },
               new()
               {
                   Name = "ChildItems",
                   Type = "List<BitNavItem>",
                   DefaultValue = "[]",
                   Description = "A list of items to render as children of the current nav item.",
               },
               new()
               {
                   Name = "CollapseAriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Aria label of the toggle button when the nav item is expanded and can be collapsed.",
               },
               new()
               {
                   Name = "Data",
                   Type = "object?",
                   DefaultValue = "null",
                   Description = "The custom data for the nav item to provide additional state.",
               },
               new()
               {
                   Name = "Description",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The description for the nav item.",
               },
               new()
               {
                   Name = "ExpandAriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Aria label of the toggle button when the nav item is collapsed and can be expanded.",
               },
               new()
               {
                   Name = "ForceAnchor",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Forces an anchor element render instead of button.",
               },
               new()
               {
                   Name = "Icon",
                   Type = "BitIconInfo?",
                   DefaultValue = "null",
                   Description = "The icon to render next to the nav item. Takes precedence over IconName when both are set.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render next to the nav item.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the nav item is enabled.",
               },
               new()
               {
                   Name = "IsExpanded",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Whether or not the nav item is in an expanded state.",
               },
               new()
               {
                   Name = "IsSeparator",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Indicates that the nav item should render as a separator.",
               },
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "A unique value to use as a key or id of the nav item.",
               },
               new()
               {
                   Name = "Match",
                   Type = "BitNavMatch?",
                   DefaultValue = "null",
                   Description = "Gets or sets a value representing the URL matching behavior of the nav item.",
                   Href = "#nav-match-enum",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS style for the nav item.",
               },
               new()
               {
                   Name = "Target",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Link target, specifies how to open the nav item's link.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitNavItem>?",
                   DefaultValue = "null",
                   Description = "The custom template for the nav item to render.",
               },
               new()
               {
                   Name = "TemplateRenderMode",
                   Type = "BitNavItemTemplateRenderMode",
                   DefaultValue = "BitNavItemTemplateRenderMode.Normal",
                   Description = "The render mode of the nav item's custom template.",
                   Href = "#nav-itemtemplate-rendermode",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "Text",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "Text to render for the nav item.",
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text for the tooltip of the nav item.",
               },
               new()
               {
                   Name = "Url",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The nav item's link URL.",
               },
               new()
               {
                   Name = "AdditionalUrls",
                   Type = "IEnumerable<string>?",
                   DefaultValue = "null",
                   Description = "Alternative URLs to be considered when auto mode tries to detect the selected nav item by the current URL.",
               }
            ]
        },
        new()
        {
            Id = "name-selectors",
            Title = "BitNavNameSelectors<TItem>",
            Parameters =
            [
               new()
               {
                   Name = "AriaCurrent",
                   Type = "BitNameSelectorPair<TItem, BitNavAriaCurrent?>",
                   DefaultValue = "new(nameof(BitNavItem.AriaCurrent))",
                   Description = "The AriaCurrent field name and selector of the custom input class.",
                   Href = "#nav-aria-current-enum",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "AriaLabel",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.AriaLabel))",
                   Description = "The AriaLabel field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Class",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.Class))",
                   Description = "The Class field name and selector of the custom input class."
               },
               new()
               {
                   Name = "ChildItems",
                   Type = "BitNameSelectorPair<TItem, List<TItem>?>",
                   DefaultValue = "new(nameof(BitNavItem.ChildItems))",
                   Description = "The ChildItems field name and selector of the custom input class."
               },
               new()
               {
                   Name = "CollapseAriaLabel",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.CollapseAriaLabel))",
                   Description = "The CollapseAriaLabel field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Data",
                   Type = "BitNameSelectorPair<TItem, object?>",
                   DefaultValue = "new(nameof(BitNavItem.Data))",
                   Description = "The Data field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Description",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.Description))",
                   Description = "The Description field name and selector of the custom input class."
               },
               new()
               {
                   Name = "ExpandAriaLabel",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.ExpandAriaLabel))",
                   Description = "The ExpandAriaLabel field name and selector of the custom input class."
               },
               new()
               {
                   Name = "ForceAnchor",
                   Type = "BitNameSelectorPair<TItem, bool?>",
                   DefaultValue = "new(nameof(BitNavItem.ForceAnchor))",
                   Description = "The ForceAnchor field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Icon",
                   Type = "BitNameSelectorPair<TItem, BitIconInfo?>",
                   DefaultValue = "new(nameof(BitNavItem.Icon))",
                   Description = "The Icon field name and selector of the custom input class.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "IconName",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.IconName))",
                   Description = "The IconName field name and selector of the custom input class."
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "BitNameSelectorPair<TItem, bool?>",
                   DefaultValue = "new(nameof(BitNavItem.IsEnabled))",
                   Description = "The IsEnabled field name and selector of the custom input class."
               },
               new()
               {
                   Name = "IsExpanded",
                   Type = "BitNameSelectorPair<TItem, bool?>",
                   DefaultValue = "new(nameof(BitNavItem.IsExpanded))",
                   Description = "The IsExpanded field name and selector of the custom input class."
               },
               new()
               {
                   Name = "IsSeparator",
                   Type = "BitNameSelectorPair<TItem, bool?>",
                   DefaultValue = "new(nameof(BitNavItem.IsSeparator))",
                   Description = "The IsSeparator field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Key",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.Key))",
                   Description = "The Key field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Match",
                   Type = "BitNameSelectorPair<TItem, BitNavMatch?>",
                   DefaultValue = "new(nameof(BitNavItem.Match))",
                   Description = "The Match field name and selector of the custom input class.",
                   Href = "#nav-match-enum",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "Style",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.Style))",
                   Description = "The Style field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Target",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.Target))",
                   Description = "The Target field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Template",
                   Type = "BitNameSelectorPair<TItem, RenderFragment<TItem>?>",
                   DefaultValue = "new(nameof(BitNavItem.Template))",
                   Description = "The Template field name and selector of the custom input class."
               },
               new()
               {
                   Name = "TemplateRenderMode",
                   Type = "BitNameSelectorPair<TItem, BitNavItemTemplateRenderMode?>",
                   DefaultValue = "new(nameof(BitNavItem.TemplateRenderMode))",
                   Description = "The TemplateRenderMode field name and selector of the custom input class.",
                   Href = "#nav-itemtemplate-rendermode",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "Text",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.Text))",
                   Description = "The Text field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Title",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.Title))",
                   Description = "The Title field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Url",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.Url))",
                   Description = "The Url field name and selector of the custom input class."
               },
               new()
               {
                   Name = "AdditionalUrls",
                   Type = "BitNameSelectorPair<TItem, IEnumerable<string>?>",
                   DefaultValue = "new(nameof(BitNavItem.AdditionalUrls))",
                   Description = "The AdditionalUrls field name and selector of the custom input class."
               },
            ]
        },
        new()
        {
            Id = "nav-class-styles",
            Title = "BitNavClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitNav."
               },
               new()
               {
                   Name = "Description",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the description of the BitNav."
               },
               new()
               {
                   Name = "Header",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the group header button of the BitNav in the Grouped render type."
               },
               new()
               {
                   Name = "HeaderText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the text of the group header of the BitNav in the Grouped render type."
               },
               new()
               {
                   Name = "Item",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item of the BitNav."
               },
               new()
               {
                   Name = "SelectedItem",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the selected item of the BitNav."
               },
               new()
               {
                   Name = "ItemContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item container of the BitNav."
               },
               new()
               {
                   Name = "ItemIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item icon of the BitNav."
               },
               new()
               {
                   Name = "ItemText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item text of the BitNav."
               },
               new()
               {
                   Name = "SelectedItemContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the selected item container of the BitNav."
               },
               new()
               {
                   Name = "ToggleButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the toggle button of the BitNav."
               },
               new()
               {
                   Name = "ToggleIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the chevron icon inside the toggle button of the BitNav."
               },
               new()
               {
                   Name = "Separator",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the separator of the BitNav."
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

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "ClearSearch",
            Type = "Task",
            Description = "Clears the search text of the nav panel, so the whole list of items is shown again.",
        },
        new()
        {
            Name = "Close",
            Type = "Task",
            Description = "Closes the nav panel.",
        },
        new()
        {
            Name = "CollapseAll",
            Type = "void",
            Description = "Collapses all items of the nav.",
        },
        new()
        {
            Name = "CollapseItem",
            Type = "Task",
            Description = "Collapses an item of the nav, and does nothing when it is already collapsed.",
        },
        new()
        {
            Name = "ExpandAll",
            Type = "void",
            Description = "Expands all items of the nav in non-SingleExpand mode.",
        },
        new()
        {
            Name = "ExpandItem",
            Type = "Task",
            Description = "Expands an item of the nav, and does nothing when it is already expanded.",
        },
        new()
        {
            Name = "FocusItem",
            Type = "ValueTask",
            Description = "Moves the focus to an item of the nav, opening the branches it is nested in when it is not rendered yet.",
        },
        new()
        {
            Name = "FocusSearchBox",
            Type = "Task",
            Description = "Moves the focus to the search box of the nav panel, opening the panel out of its toggled state first when the search box is not on screen.",
        },
        new()
        {
            Name = "Open",
            Type = "Task",
            Description = "Opens the nav panel.",
        },
        new()
        {
            Name = "SelectItem",
            Type = "Task",
            Description = "Selects an item of the nav programmatically, exactly like a click on that item would in the manual mode.",
        },
        new()
        {
            Name = "Toggle",
            Type = "Task",
            Description = "Toggles the nav panel if possible.",
        },
        new()
        {
            Name = "ToggleItem",
            Type = "Task",
            Description = "Toggles an item of the nav.",
        }
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
            Id = "nav-match-enum",
            Name = "BitNavMatch",
            Description = "Modifies the URL matching behavior for a BitNav<TItem>.",
            Items =
            [
                new()
                {
                    Name = "Exact",
                    Description = "Specifies that the nav item should be active when it matches exactly the current URL.",
                    Value = "0",
                },
                new()
                {
                    Name = "Prefix",
                    Description = "Specifies that the nav item should be active when it matches any prefix of the current URL.",
                    Value = "1",
                },
                new()
                {
                    Name = "Regex",
                    Description = "Specifies that the nav item should be active when its provided regex matches the current URL.",
                    Value = "2",
                },
                new()
                {
                    Name = "Wildcard",
                    Description = "Specifies that the nav item should be active when its provided wildcard matches the current URL.",
                    Value = "3",
                }
            ]
        },
        new()
        {
            Id = "nav-mode-enum",
            Name = "BitNavMode",
            Items =
            [
                new()
                {
                    Name = "Automatic",
                    Description = "The value of selected key will change using NavigationManager and the current url inside the component.",
                    Value = "0",
                },
                new()
                {
                    Name = "Manual",
                    Description = "Selected key changes will be sent back to the parent component and the component won't change its value.",
                    Value = "1",
                }
            ]
        },
        new()
        {
            Id = "nav-render-type-enum",
            Name = "BitNavRenderType",
            Description="Determines how the nav items are rendered visually.",
            Items =
            [
                new()
                {
                    Name = "Normal",
                    Value = "0",
                    Description="All items will be rendered normally only based on their own properties."
                },
                new()
                {
                    Name = "Grouped",
                    Value = "1",
                    Description="Root elements are rendered in a specific way that resembles a grouped list of items."
                }
            ]
        },
        new()
        {
            Id = "nav-itemtemplate-rendermode",
            Name = "BitNavItemTemplateRenderMode",
            Items =
            [
                new()
                {
                    Name = "Normal",
                    Description = "Renders the template inside the button/anchor root element of the item.",
                    Value = "0",
                },
                new()
                {
                    Name = "Replace",
                    Description = "Replaces the button/anchor root element of the item.",
                    Value = "1",
                }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Defines the sizes available for a component.",
            Items =
            [
                new()
                {
                    Name = "Small",
                    Description = "The small size.",
                    Value = "0",
                },
                new()
                {
                    Name = "Medium",
                    Description = "The medium size.",
                    Value = "1",
                },
                new()
                {
                    Name = "Large",
                    Description = "The large size.",
                    Value = "2",
                }
            ]
        },
    ];


    
    private bool basicIsOpen;
    private bool fitWidthIsOpen;
    private bool fullWidthIsOpen;
    private bool widthIsOpen;
    private bool expandOnHoverIsOpen;
    private bool noToggleIsOpen;
    private bool iconUrlIsOpen;
    private bool searchBoxPlaceholderIsOpen;
    private bool noSearchBoxIsOpen;
    private bool searchIsOpen;
    private bool emptyListMessageIsOpen;
    private bool selectionIsOpen;
    private bool singleExpandIsOpen;
    private bool customIsOpen;
    private bool behaviorIsOpen;
    private bool templateIsOpen;
    private bool eventIsOpen;
    private bool colorIsOpen;
    private bool externalIconIsOpen;
    private bool sizeIsOpen;
    private bool classStyleIsOpen;
    private bool rtlIsOpen;

    private bool publicApiIsOpen;
    private BitNavPanel<BitNavItem> navPanelRef = default!;

    private string? searchText;
    private string? lastSearchedTerm;

    private BitNavItem? selectedItem;

    private BitNavItem? onItemClick;
    private BitNavItem? onItemToggle;

    private List<BitNavItem> basicNavItems =
    [
        new()
        {
            Text = "Home",
            IconName = BitIconName.Home,
            Url = "HomePage",
            Data = 13,
        },
        new()
        {
            Text = "AdminPanel",
            IconName = BitIconName.Admin,
            ChildItems =
            [
                new() {
                    Text = "Dashboard",
                    IconName = BitIconName.BarChartVerticalFill,
                    Url = "DashboardPage",
                    Data = 63,
                },
                new() {
                    Text = "Categories",
                    IconName = BitIconName.BuildQueue,
                    Url = "CategoriesPage",
                },
                new() {
                    Text = "Products",
                    IconName = BitIconName.Product,
                    Url = "ProductsPage",
                }
            ]
        },
        new()
        {
            Text = "Todo",
            IconName = BitIconName.ToDoLogoOutline,
            Url = "TodoPage",
        },
        new()
        {
            Text = "Settings",
            IconName = BitIconName.Equalizer,
            Url = "SettingsPage",
            Data = 85,
        },
        new()
        {
            Text = "Terms",
            IconName = BitIconName.EntityExtraction,
            Url = "TermsPage",
        }
    ];
    private List<BitNavItem> singleExpandNavItems =
    [
        new()
        {
            Text = "Home",
            IconName = BitIconName.Home,
            Url = "HomePage",
            Data = 13,
        },
        new()
        {
            Text = "AdminPanel",
            IconName = BitIconName.Admin,
            ChildItems =
            [
                new() {
                    Text = "Dashboard",
                    IconName = BitIconName.BarChartVerticalFill,
                    Url = "DashboardPage",
                    Data = 63,
                },
                new() {
                    Text = "Categories",
                    IconName = BitIconName.BuildQueue,
                    Url = "CategoriesPage",
                },
                new() {
                    Text = "Products",
                    IconName = BitIconName.Product,
                    Url = "ProductsPage",
                }
            ]
        },
        new()
        {
            Text = "Todo",
            IconName = BitIconName.ToDoLogoOutline,
            Url = "TodoPage",
        },
        new()
        {
            Text = "Settings",
            IconName = BitIconName.Equalizer,
            ChildItems =
            [
                new() {
                    Text = "Views",
                    IconName = BitIconName.BarChartVerticalFill,
                    Url = "ViewsPage",
                    Data = 63,
                },
                new() {
                    Text = "Users",
                    IconName = BitIconName.BuildQueue,
                    Url = "UsersPage",
                }
            ]
        },
        new()
        {
            Text = "Terms",
            IconName = BitIconName.EntityExtraction,
            Url = "TermsPage",
        }
    ];
    private List<BitNavItem> eventNavItems =
    [
        new()
        {
            Text = "Home",
            IconName = BitIconName.Home,
        },
        new()
        {
            Text = "AdminPanel",
            IconName = BitIconName.Admin,
            ChildItems =
            [
                new() {
                    Text = "Dashboard",
                    IconName = BitIconName.BarChartVerticalFill,
                },
                new() {
                    Text = "Categories",
                    IconName = BitIconName.BuildQueue,
                },
                new() {
                    Text = "Products",
                    IconName = BitIconName.Product,
                }
            ]
        },
        new()
        {
            Text = "Todo",
            IconName = BitIconName.ToDoLogoOutline,
        },
        new()
        {
            Text = "Settings",
            IconName = BitIconName.Equalizer,
        },
        new()
        {
            Text = "Terms",
            IconName = BitIconName.EntityExtraction,
        }
    ];
    private List<BitNavItem> rtlNavItems =
    [
        new()
        {
            Text = "خانه",
            IconName = BitIconName.Home,
            Url = "HomePage",
        },
        new()
        {
            Text = "ادمین پنل",
            IconName = BitIconName.Admin,
            ChildItems =
            [
                new() {
                    Text = "داشبورد",
                    IconName = BitIconName.BarChartVerticalFill,
                    Url = "DashboardPage",
                },
                new() {
                    Text = "دسته‌ها",
                    IconName = BitIconName.BuildQueue,
                    Url = "CategoriesPage",
                },
                new() {
                    Text = "کالاها",
                    IconName = BitIconName.Product,
                    Url = "ProductsPage",
                }
            ]
        },
        new()
        {
            Text = "وظایف",
            IconName = BitIconName.ToDoLogoOutline,
            Url = "TodoPage",
        },
        new()
        {
            Text = "تنظیمات",
            IconName = BitIconName.Equalizer,
            Url = "SettingsPage"
        },
        new()
        {
            Text = "قوانین",
            IconName = BitIconName.EntityExtraction,
            Url = "TermsPage",
        }
    ];

    // Only the members whose names differ from the ones of BitNavItem are mapped here; the rest (Url, for
    // instance) keep matching by convention.
    private static readonly BitNavNameSelectors<CustomNavItem> customSelectors = new()
    {
        Text = { Name = nameof(CustomNavItem.Name) },
        IconName = { Name = nameof(CustomNavItem.Glyph) },
        ChildItems = { Name = nameof(CustomNavItem.Children) },
    };

    private readonly List<CustomNavItem> customNavItems =
    [
        new()
        {
            Name = "Home",
            Glyph = BitIconName.Home,
            Url = "HomePage",
        },
        new()
        {
            Name = "AdminPanel",
            Glyph = BitIconName.Admin,
            Children =
            [
                new() { Name = "Dashboard", Glyph = BitIconName.BarChartVerticalFill, Url = "DashboardPage" },
                new() { Name = "Categories", Glyph = BitIconName.BuildQueue, Url = "CategoriesPage" },
                new() { Name = "Products", Glyph = BitIconName.Product, Url = "ProductsPage" }
            ]
        },
        new()
        {
            Name = "Settings",
            Glyph = BitIconName.Equalizer,
            Url = "SettingsPage",
        }
    ];

    private readonly List<BitNavItem> externalIconNavItems =
    [
        new()
        {
            Text = "Home",
            Icon = BitIconInfo.Fa("solid house"),
            Url = "HomePage",
        },
        new()
        {
            Text = "AdminPanel",
            Icon = BitIconInfo.Fa("solid user-shield"),
            ChildItems =
            [
                new() { Text = "Dashboard", Icon = BitIconInfo.Fa("solid chart-simple"), Url = "DashboardPage" },
                new() { Text = "Products", Icon = BitIconInfo.Fa("solid box"), Url = "ProductsPage" }
            ]
        },
        new()
        {
            Text = "Settings",
            Icon = BitIconInfo.Fa("solid gear"),
            Url = "SettingsPage",
        }
    ];

    public class CustomNavItem
    {
        public string? Name { get; set; }
        public string? Glyph { get; set; }
        public string? Url { get; set; }
        public List<CustomNavItem>? Children { get; set; }
    }

    private void HandleOnItemClick(BitNavItem item)
    {
        onItemClick = item;
    }

    private void HandleOnItemToggle(BitNavItem item)
    {
        onItemToggle = item;
    }
}
