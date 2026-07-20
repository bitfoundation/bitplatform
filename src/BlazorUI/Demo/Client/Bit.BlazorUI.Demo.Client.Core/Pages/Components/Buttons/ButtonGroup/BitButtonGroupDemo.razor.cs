namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class BitButtonGroupDemo
{
    [CascadingParameter(Name = nameof(RenderForMcpClient))] public bool RenderForMcpClient { get; set; }

    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the BitButtonGroup, that are BitButtonGroupOption components.",
        },
        new()
        {
            Name = "IconOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines that only the icon should be rendered.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitButtonGroupClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the ButtonGroup.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the button group.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "DefaultToggleKey",
            Type = "string?",
            DefaultValue = "null",
            Description = "The default key that will be initially used to set toggled item in toggle mode if the ToggleKey parameter is not set.",
        },
        new()
        {
            Name = "DefaultToggleKeys",
            Type = "IEnumerable<string>?",
            DefaultValue = "null",
            Description = "The default keys that will be initially used to set the toggled items in the Multiple selection mode if the ToggleKeys parameter is not set.",
        },
        new()
        {
            Name = "Detached",
            Type = "bool",
            DefaultValue = "false",
            Description = "Detaches the buttons from each other, so each button is rendered as a separate rounded button.",
        },
        new()
        {
            Name = "DisabledInteractive",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the disabled buttons focusable by rendering them with the aria-disabled attribute instead of the disabled attribute, so that assistive technologies can still discover them.",
        },
        new()
        {
            Name = "FixedToggle",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the fixed-toggle mode that ensures one item to be always toggled. In the Multiple selection mode it prevents un-toggling the last toggled item.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Expand the ButtonGroup width to 100% of the available width.",
        },
        new()
        {
            Name = "Gap",
            Type = "string?",
            DefaultValue = "null",
            Description = "The gap between the buttons of the ButtonGroup in the detached mode.",
        },
        new()
        {
            Name = "Justified",
            Type = "bool",
            DefaultValue = "false",
            Description = "Gives every button an equal width so that the buttons evenly fill the width of the ButtonGroup.",
        },
        new()
        {
            Name = "Items",
            Type = "IEnumerable<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "List of Item, each of which can be a Button with different action in the ButtonGroup.",
            LinkType = LinkType.Link,
            Href = "#button-group-item",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The content inside the item can be customized.",
        },
        new()
        {
            Name = "MaxToggles",
            Type = "int?",
            DefaultValue = "null",
            Description = "The maximum number of items that can be toggled at the same time in the Multiple selection mode.",
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitButtonGroupNameSelectors<TItem>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties.",
            LinkType = LinkType.Link,
            Href = "#name-selectors",
        },
        new()
        {
            Name = "Navigable",
            Type = "bool",
            DefaultValue = "true",
            Description = "Enables the roving tabindex behavior, which turns the whole ButtonGroup into a single tab stop that is navigable using the arrow, Home, and End keys.",
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            Description = "The callback that is called when a button is clicked."
        },
        new()
        {
            Name = "OnToggleChange",
            Type = "EventCallback<TItem>",
            Description = "The callback that called when toggled item change."
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
            Name = "Overflow",
            Type = "BitButtonGroupOverflow?",
            DefaultValue = "null",
            Description = "Determines how the ButtonGroup behaves when its buttons do not fit in the available space.",
            LinkType = LinkType.Link,
            Href = "#overflow-enum",
        },
        new()
        {
            Name = "Rounded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the ButtonGroup with fully rounded (pill shaped) corners.",
        },
        new()
        {
            Name = "SelectionMode",
            Type = "BitButtonGroupSelectionMode?",
            DefaultValue = "null",
            Description = "Determines how many items can be toggled at the same time. When not set, it falls back to Single if the Toggle parameter is enabled, otherwise None.",
            LinkType = LinkType.Link,
            Href = "#selection-mode-enum",
        },
        new()
        {
            Name = "SelectOnFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Toggles the focused item while navigating the ButtonGroup using the keyboard, so that the selection follows the focus.",
        },
        new()
        {
            Name = "ShowSelectionIndicator",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders a check mark at the start of the toggled buttons.",
        },
        new()
        {
            Name = "Toggle",
            Type = "bool",
            DefaultValue = "false",
            Description = "Display ButtonGroup with toggle mode enabled for each button. It is a shorthand of setting the SelectionMode parameter to Single.",
        },
        new()
        {
            Name = "ToggleKey",
            Type = "string?",
            DefaultValue = "null",
            Description = "The key of the toggled item in the Single selection mode. (two-way bound)",
        },
        new()
        {
            Name = "ToggleKeys",
            Type = "IEnumerable<string>?",
            DefaultValue = "null",
            Description = "The keys of the toggled items in the Multiple selection mode. (two-way bound)",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize",
            DefaultValue = "null",
            Description = "The size of ButtonGroup, Possible values: Small | Medium | Large.",
            LinkType = LinkType.Link,
            Href = "#button-size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitButtonGroupClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the ButtonGroup.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the button group.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Defines whether to render ButtonGroup children vertically."
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "button-group-item",
            Title = "BitButtonGroupItem",
            Parameters =
            [
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The accessible label of the item, rendered as the aria-label attribute. Required for icon-only items, and strongly recommended in toggle mode when OnText/OffText are used, so that the accessible name of the item stays the same while its toggle state changes.",
               },
               new()
               {
                   Name = "Badge",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The content of the badge rendered at the end of the item, usually a short count.",
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
                   Name = "Href",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The url of the link rendered by the item. If provided, the item renders as an anchor tag instead of a button.",
               },
               new()
               {
                   Name = "Icon",
                   Type = "BitIconInfo?",
                   DefaultValue = "null",
                   Description = "The icon to render next to the item text. Takes precedence over IconName.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render next to the item text.",
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
                   Name = "IsLoading",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Whether or not the item is in the loading state, which replaces its icon with a spinner and blocks its click.",
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
                   Name = "OffIcon",
                   Type = "BitIconInfo?",
                   DefaultValue = "null",
                   Description = "The icon of the item when it is not checked in toggle mode. Takes precedence over OffIconName.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "OffIconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The icon of the item when it is not checked in toggle mode.",
               },
               new()
               {
                   Name = "OffText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The text of the item when it is not checked in toggle mode.",
               },
               new()
               {
                   Name = "OffTitle",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The title of the item when it is not checked in toggle mode.",
               },
               new()
               {
                   Name = "OnIcon",
                   Type = "BitIconInfo?",
                   DefaultValue = "null",
                   Description = "The icon of the item when it is checked in toggle mode. Takes precedence over OnIconName.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "OnIconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The icon of the item when it is checked in toggle mode.",
               },
               new()
               {
                   Name = "OnText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The text of the item when it is checked in toggle mode.",
               },
               new()
               {
                   Name = "OnTitle",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The title of the item when it is checked in toggle mode.",
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
                   Name = "ReversedIcon",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Reverses the positions of the icon and the main content of the item.",
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
                   Name = "Target",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The target attribute of the link when the item renders as an anchor (by providing the Href property).",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitButtonGroupItem>?",
                   DefaultValue = "null",
                   Description = "The custom template for the item.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text to render in the item.",
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Title to render in the item.",
               }
            ]
        },
        new()
        {
            Id = "button-group-option",
            Title = "BitButtonGroupOption",
            Parameters =
            [
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The accessible label of the option, rendered as the aria-label attribute. Required for icon-only options, and strongly recommended in toggle mode when OnText/OffText are used, so that the accessible name of the option stays the same while its toggle state changes.",
               },
               new()
               {
                   Name = "Badge",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The content of the badge rendered at the end of the option, usually a short count.",
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
                   Name = "Href",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The url of the link rendered by the option. If provided, the option renders as an anchor tag instead of a button.",
               },
               new()
               {
                   Name = "Icon",
                   Type = "BitIconInfo?",
                   DefaultValue = "null",
                   Description = "The icon to render next to the option text. Takes precedence over IconName.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render next to the option text.",
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
                   Name = "IsLoading",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Whether or not the option is in the loading state, which replaces its icon with a spinner and blocks its click.",
               },
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "A unique value to use as a key of the option.",
               },
               new()
               {
                   Name = "OffIcon",
                   Type = "BitIconInfo?",
                   DefaultValue = "null",
                   Description = "The icon of the option when it is not checked in toggle mode. Takes precedence over OffIconName.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "OffIconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The icon of the option when it is not checked in toggle mode.",
               },
               new()
               {
                   Name = "OffText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The text of the option when it is not checked in toggle mode.",
               },
               new()
               {
                   Name = "OffTitle",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The title of the option when it is not checked in toggle mode.",
               },
               new()
               {
                   Name = "OnIcon",
                   Type = "BitIconInfo?",
                   DefaultValue = "null",
                   Description = "The icon of the option when it is checked in toggle mode. Takes precedence over OnIconName.",
                   LinkType = LinkType.Link,
                   Href = "#bit-icon-info",
               },
               new()
               {
                   Name = "OnIconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The icon of the option when it is checked in toggle mode.",
               },
               new()
               {
                   Name = "OnText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The text of the option when it is checked in toggle mode.",
               },
               new()
               {
                   Name = "OnTitle",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The title of the option when it is checked in toggle mode.",
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
                   Name = "ReversedIcon",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Reverses the positions of the icon and the main content of the option.",
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
                   Name = "Target",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The target attribute of the link when the option renders as an anchor (by providing the Href parameter).",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitButtonGroupOption>?",
                   DefaultValue = "null",
                   Description = "The custom template for the option.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text to render in the option.",
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Title to render in the option.",
               }
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitButtonGroupClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitButtonGroup.",
               },
               new()
               {
                   Name = "Button",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the internal button of the BitButtonGroup.",
               },
               new()
               {
                   Name = "Badge",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the badge of the buttons of the BitButtonGroup.",
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the BitButtonGroup."
               },
               new()
               {
                   Name = "SelectionIndicator",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the selection indicator (check mark) of the toggled buttons of the BitButtonGroup.",
               },
               new()
               {
                   Name = "Spinner",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the loading spinner of the buttons of the BitButtonGroup.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the text of the BitButtonGroup."
               },
               new()
               {
                   Name = "ToggledButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the button when in toggle mode of the BitButtonGroup.",
               },
            ],
        },
        new()
        {
            Id = "name-selectors",
            Title = "BitButtonGroupNameSelectors",
            Parameters =
            [
                new()
                {
                    Name = "AriaLabel",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.AriaLabel))",
                    Description = "AriaLabel field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "Badge",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Badge))",
                    Description = "Badge field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "Class",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Class))",
                    Description = "The CSS Class field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "Href",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Href))",
                    Description = "Href field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "Icon",
                    Type = "BitNameSelectorPair<TItem, BitIconInfo?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Icon))",
                    Description = "Icon field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "IconName",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.IconName))",
                    Description = "IconName field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "IsEnabled",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.IsEnabled))",
                    Description = "IsEnabled field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "IsLoading",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.IsLoading))",
                    Description = "IsLoading field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "Key",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Key))",
                    Description = "Key field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "OffIcon",
                    Type = "BitNameSelectorPair<TItem, BitIconInfo?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.OffIcon))",
                    Description = "OffIcon field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "OffIconName",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.OffIconName))",
                    Description = "OffIconName field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "OffText",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.OffText))",
                    Description = "OffText field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "OffTitle",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.OffTitle))",
                    Description = "OffTitle field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "OnIcon",
                    Type = "BitNameSelectorPair<TItem, BitIconInfo?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.OnIcon))",
                    Description = "OnIcon field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "OnIconName",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.OnIconName))",
                    Description = "OnIconName field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "OnText",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.OnText))",
                    Description = "OnText field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "OnTitle",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.OnTitle))",
                    Description = "OnTitle field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "OnClick",
                    Type = "BitNameSelectorPair<TItem, Action<TItem>?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.OnClick))",
                    Description = "OnClick field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "ReversedIcon",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.ReversedIcon))",
                    Description = "ReversedIcon field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "Style",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Style))",
                    Description = "Style field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "Target",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Target))",
                    Description = "Target field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "Template",
                    Type = "BitNameSelectorPair<TItem, RenderFragment?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Template))",
                    Description = "Template field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "Text",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Text))",
                    Description = "Text field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
                },
                new()
                {
                    Name = "Title",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitButtonGroupItem.Title))",
                    Description = "Title field name and selector of the custom input class.",
                    LinkType = LinkType.Link,
                    Href = "#name-selector-pair",
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
                    Name= "Fill",
                    Description="Fill styled variant.",
                    Value="0",
                },
                new()
                {
                    Name= "Outline",
                    Description="Outline styled variant.",
                    Value="1",
                },
                new()
                {
                    Name= "Text",
                    Description="Text styled variant.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "selection-mode-enum",
            Name = "BitButtonGroupSelectionMode",
            Description = "Determines how many items of a BitButtonGroup can be toggled at the same time.",
            Items =
            [
                new()
                {
                    Name= "None",
                    Description="The items act as plain action buttons and cannot be toggled.",
                    Value="0",
                },
                new()
                {
                    Name= "Single",
                    Description="At most one item can be toggled at a time (rendered with the radiogroup accessibility pattern).",
                    Value="1",
                },
                new()
                {
                    Name= "Multiple",
                    Description="Any number of items can be toggled at the same time (rendered with the toolbar accessibility pattern).",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "overflow-enum",
            Name = "BitButtonGroupOverflow",
            Description = "Determines how a BitButtonGroup behaves when its items do not fit in the available space.",
            Items =
            [
                new()
                {
                    Name= "Clip",
                    Description="The items are kept on a single line and the overflowing part is clipped.",
                    Value="0",
                },
                new()
                {
                    Name= "Wrap",
                    Description="The items wrap onto multiple lines.",
                    Value="1",
                },
                new()
                {
                    Name= "Scroll",
                    Description="The items are kept on a single line and the group becomes scrollable, without rendering a scrollbar. It can still be scrolled by swiping, by shift+wheel, and through the arrow keys.",
                    Value="2",
                },
                new()
                {
                    Name= "Scrollbar",
                    Description="The items are kept on a single line and the group becomes scrollable, with a visible scrollbar. The scrollbar is laid out inside the border of the group, which makes the group taller.",
                    Value="3",
                }
            ]
        },
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Primary",
                    Description="Primary general color.",
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
                    Description="Severe Warning general color.",
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
            Id = "button-size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size button.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size button.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size button.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "button-type-enum",
            Name = "BitButtonType",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Button",
                    Description="The button is a clickable button.",
                    Value="0",
                },
                new()
                {
                    Name= "Submit",
                    Description="The button is a submit button (submits form-data).",
                    Value="1",
                },
                new()
                {
                    Name= "Reset",
                    Description="The button is a reset button (resets the form-data to its initial values).",
                    Value="2",
                }
            ]
        }
    ];
}
