namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Nav;

public partial class BitNavDemo
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
            Name = "ChevronDownIcon",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom icon name of the chevron-down element of the BitNav component.",
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
            Type = "BitNavClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitNav component.",
            LinkType = LinkType.Link,
            Href = "#nav-class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the nav that is only used for colored parts like icons.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
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
            Description = "Renders the nav in a width to only fit its content."
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the nav in full width of its container element."
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
            Name = "IconOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Only renders the icon of each nav item."
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
            Name = "IndentValue",
            Type = "int",
            DefaultValue = "16",
            Description = "The indentation value in px for each level of depth of child item."
        },
        new()
        {
            Name = "Items",
            Type = "IList<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "A collection of item to display in the navigation bar.",
            LinkType = LinkType.Link,
            Href="#nav-item",
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
            Name = "Match",
            Type = "BitNavMatch?",
            DefaultValue = "null",
            Description = "Gets or sets a value representing the global URL matching behavior of the nav.",
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
            Type = "BitNavNameSelectors<TItem>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties.",
            LinkType = LinkType.Link,
            Href = "#name-selectors",
        },
        new()
        {
            Name = "NoCollapse",
            Type = "bool",
            Description = "Hides all collapse/expand buttons and remove their spaces at the start of each node."
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            Description = "Callback invoked when an item is clicked."
        },
        new()
        {
            Name = "OnItemToggle",
            Type = "EventCallback<TItem>",
            Description = "Callback invoked when a group header is clicked and Expanded or Collapse."
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
            Name = "SelectedItem",
            Type = "TItem?",
            DefaultValue = "null",
            Description = "Selected item to show in the BitNav."
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
            Name = "Styles",
            Type = "BitNavClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitNav component.",
            LinkType = LinkType.Link,
            Href = "#nav-class-styles",
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "CollapseAll",
            Type = "Action<TItem? item>",
            Description = "Collapses all items and children.",
        },
        new()
        {
            Name = "ExpandAll",
            Type = "Action<TItem? item>",
            Description = "Expands all items and children in non-SingleExpand mode.",
        },
        new()
        {
            Name = "ToggleItem",
            Type = "Func<Task, TItem>",
            Description = "Toggles an item.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
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
                   Description = " Aria-current token for active nav item. Must be a valid token value, and defaults to 'page'.",
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
                   Description = "Aria label when nav item is collapsed and can be expanded.",
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
                   Description = "Aria label when nav item is collapsed and can be expanded.",
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
            Id = "nav-option",
            Title = "BitNavOption",
            Parameters =
            [
               new()
               {
                   Name = "AriaCurrent",
                   Type = "BitNavAriaCurrent",
                   DefaultValue = "BitNavAriaCurrent.Page",
                   Description = " Aria-current token for active nav option. Must be a valid token value, and defaults to 'page'.",
                   Href = "#nav-aria-current-enum",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Aria label for nav option. Ignored if CollapseAriaLabel or ExpandAriaLabel is provided.",
               },
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS class for the nav option.",
               },
               new()
               {
                   Name = "ChildContent",
                   Type = "RenderFragment?",
                   DefaultValue = "null",
                   Description = "A list of options to render as children of the current nav option.",
               },
               new()
               {
                   Name = "CollapseAriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Aria label when nav option is collapsed and can be expanded.",
               },
               new()
               {
                   Name = "Data",
                   Type = "object?",
                   DefaultValue = "null",
                   Description = "The custom data for the nav option to provide additional state.",
               },
               new()
               {
                   Name = "Description",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The description for the nav option.",
               },
               new()
               {
                   Name = "ExpandAriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Aria label when nav option is collapsed and can be expanded.",
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
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render next to the nav option.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the nav option is enabled.",
               },
               new()
               {
                   Name = "IsExpanded",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Whether or not the nav option is in an expanded state.",
               },
               new()
               {
                   Name = "IsSeparator",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Indicates that the nav option should render as a separator.",
               },
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "A unique value to use as a key or id of the nav option.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS style for the nav option.",
               },
               new()
               {
                   Name = "Target",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Link target, specifies how to open the nav option's link.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitNavItem>?",
                   DefaultValue = "null",
                   Description = "The custom template for the nav option to render.",
               },
               new()
               {
                   Name = "TemplateRenderMode",
                   Type = "BitNavItemTemplateRenderMode",
                   DefaultValue = "BitNavItemTemplateRenderMode.Normal",
                   Description = "The render mode of the nav option's custom template.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "Text to render for the nav option.",
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text for the tooltip of the nav option.",
               },
               new()
               {
                   Name = "Url",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The nav option's link URL.",
               },
               new()
               {
                   Name = "AdditionalUrls",
                   Type = "IEnumerable<string>?",
                   DefaultValue = "null",
                   Description = "Alternative URLs to be considered when auto mode tries to detect the selected nav option by the current URL.",
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
                   Description = "The AriaCurrent field name and selector of the custom input class."
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
                   Name = "TemplateRenderMode",
                   Type = "BitNameSelectorPair<TItem, BitNavItemTemplateRenderMode?>",
                   DefaultValue = "new(nameof(BitNavItem.TemplateRenderMode))",
                   Description = "The TemplateRenderMode field name and selector of the custom input class."
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
                   Type = "String?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitNav."
               },
               new()
               {
                   Name = "Item",
                   Type = "String?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item of the BitNav."
               },
               new()
               {
                   Name = "SelectedItem",
                   Type = "String?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the selected item of the BitNav."
               },
               new()
               {
                   Name = "ItemContainer",
                   Type = "String?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item container of the BitNav."
               },
               new()
               {
                   Name = "ItemIcon",
                   Type = "String?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item icon of the BitNav."
               },
               new()
               {
                   Name = "ItemText",
                   Type = "String?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item text of the BitNav."
               },
               new()
               {
                   Name = "SelectedItemContainer",
                   Type = "String?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the selected item container of the BitNav."
               },
               new()
               {
                   Name = "ToggleButton",
                   Type = "String?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the toggle button of the BitNav."
               },
               new()
               {
                   Name = "Separator",
                   Type = "String?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the separator of the BitNav."
               },
            ]
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
            Id = "nav-aria-current-enum",
            Name = "BitNavItemAriaCurrent",
            Items =
            [
                new()
                {
                    Name = "Page",
                    Value = "0",
                },
                new()
                {
                    Name = "Step",
                    Value = "1",
                },
                new()
                {
                    Name = "Location",
                    Value = "2",
                },
                new()
                {
                    Name = "Date",
                    Value = "3",
                },
                new()
                {
                    Name = "Time",
                    Value = "4",
                },
                new()
                {
                    Name = "True",
                    Value = "5",
                },

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
    ];
}
