namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class BitMenuButtonDemo
{
    [CascadingParameter(Name = nameof(RenderForMcpClient))] public bool RenderForMcpClient { get; set; }

    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the menu button for the benefit of screen readers."
        },
        new()
        {
            Name = "AriaHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, add an aria-hidden attribute instructing screen readers to ignore the menu button."
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the header button automatically receives focus when the page renders. It is dropped on a menu button hidden from assistive technologies, and on a disabled one that is not kept focusable.",
        },
        new()
        {
            Name = "AutoLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enters the loading state automatically while awaiting OnClick and ignores further clicks of the header button until it returns. Meant for the main half of a split menu button, whose chevron keeps opening the menu while the command runs.",
        },
        new()
        {
            Name = "Background",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The background color kind of the callout.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "ButtonType",
            Type = "BitButtonType?",
            DefaultValue = "null",
            Description = "The value of the type attribute of the menu button.",
            LinkType = LinkType.Link,
            Href = "#button-type-enum"
        },
        new()
        {
            Name = "CheckIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the check mark shown on a checked item, from an external icon library. Takes precedence over CheckIconName.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "CheckIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon of the check mark shown on a checked item. Defaults to the Accept icon.",
        },
        new()
        {
            Name = "ChevronDownAriaLabel",
            Type = "string?",
            DefaultValue = "\"More options\"",
            Description = "The aria-label of the chevron down button of the split menu button. The chevron carries no text of its own, so without a name it reaches a screen reader as an unlabelled button.",
        },
        new()
        {
            Name = "ChevronDownIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon for the chevron down part of the menu button.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "ChevronDownIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name of the chevron down part of the menu button.",
        },
        new()
        {
            Name = "ChevronDownTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip to show when the mouse is placed on the chevron down button of the split menu button.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the menu button, that are BitMenuButtonOption components.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitMenuButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the menu button.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "CloseOnItemClick",
            Type = "bool",
            DefaultValue = "true",
            Description = "Closes the callout when an item is clicked. Turn it off for a menu of checkable items, so several can be toggled without reopening it.",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the menu button.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "DefaultSelectedItem",
            Type = "TItem?",
            DefaultValue = "null",
            Description = "Default value of the SelectedItem."
        },
        new()
        {
            Name = "DefaultIsToggled",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Default value of the IsToggled parameter in toggle mode.",
        },
        new()
        {
            Name = "DisabledInteractive",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps a disabled menu button, and the disabled items of any menu button, focusable: the state is conveyed with the aria-disabled attribute instead of the native disabled one, so they stay reachable and are announced as unavailable rather than silently skipped. Their actions stay suppressed either way.",
        },
        new()
        {
            Name = "DropDirection",
            Type = "BitDropDirection",
            DefaultValue = "BitDropDirection.TopAndBottom",
            Description = "Determines the allowed drop directions of the callout.",
            LinkType = LinkType.Link,
            Href = "#drop-direction-enum",
        },
        new()
        {
            Name = "FormId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the form element the menu button is associated with, rendered as the form attribute of the header button and of the items. It lets a submit or reset command sit outside of its form.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Expands the menu button width to 100% of the available width.",
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content inside the header of menu button can be customized.",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon from built-in Fluent UI icons to show inside the header of menu button.",
        },
        new()
        {
            Name = "IsLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines whether the menu button is in the loading state. It replaces the default icon of the header button with a spinner and ignores its click; in split mode the chevron still opens the menu.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines the opening state of the callout.",
        },
        new()
        {
            Name = "IsToggled",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines whether the header button is in the toggled state when Toggle is enabled.",
        },
        new()
        {
            Name = "Items",
            Type = "IEnumerable<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "List of items to show in the menu button.",
            LinkType = LinkType.Link,
            Href = "#menu-button-item"
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "The custom template content to render each item.",
        },
        new()
        {
            Name = "LoadingDelay",
            Type = "int",
            DefaultValue = "0",
            Description = "The delay in milliseconds before the spinner appears after the menu button enters the loading state, which keeps a fast operation from flashing one. The click guard applies immediately regardless.",
        },
        new()
        {
            Name = "LoadingLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text to show beside the spinner in the loading state, replacing the text of the header button. It is also announced by screen readers through a status live region.",
        },
        new()
        {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template that replaces the spinner and the label of the header button while the menu button is in the loading state.",
        },
        new()
        {
            Name = "MaxHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tallest the callout grows before its items start to scroll, as a CSS length. Without one the callout is capped to the room the viewport leaves.",
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitMenuButtonNameSelectors<TItem>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties.",
            LinkType = LinkType.Link,
            Href = "#name-selectors",
        },
        new()
        {
            Name = "NoIcon",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the icon of the header button is hidden.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<TItem?>",
            Description = "The callback that is called when the header button is clicked, with the selected item in Sticky mode and null otherwise, and when an item of the menu is activated outside of Sticky mode, with that item."
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<TItem>",
            Description = "The callback that is called when the selected item has changed."
        },
        new()
        {
            Name = "OnToggleChange",
            Type = "EventCallback<bool>",
            Description = "The callback that is called when the IsToggled value changes in toggle mode.",
        },
        new()
        {
            Name = "Options",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of the ChildContent.",
        },
        new()
        {
            Name = "RadioIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the bullet shown on a checked single-choice item, using custom CSS classes for external icon libraries. Takes precedence over RadioIconName when both are set.",
        },
        new()
        {
            Name = "RadioIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon of the bullet shown on a checked single-choice item (one whose RadioGroup is set).",
        },
        new()
        {
            Name = "Reclickable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables re-clicking the header button while the menu button is in the loading state. By default its click is ignored, which is what protects against a double submission.",
        },
        new()
        {
            Name = "SelectedItem",
            Type = "TItem?",
            DefaultValue = "null",
            Description = "Determines the current selected item that acts as the header item."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the menu button.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Split",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the menu button renders as a split button."
        },
        new()
        {
            Name = "Sticky",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the selected item is going to change the header item."
        },
        new()
        {
            Name = "StopPropagation",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, stops the propagation of the click event of the menu button to the parent elements. Useful when the menu button is placed inside clickable containers like rows or cards.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitMenuButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the menu button.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "SubmenuIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the chevron an item that opens a submenu carries, using custom CSS classes for external icon libraries. Takes precedence over SubmenuIconName when both are set.",
        },
        new()
        {
            Name = "SubmenuIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon of the chevron an item that opens a submenu carries. It is mirrored in a right-to-left menu, so one icon serves both directions.",
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text to show inside the header of menu button."
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip to show when the mouse is placed on the header button.",
        },
        new()
        {
            Name = "Toggle",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, enables toggle behavior on the header button in Split mode.",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the menu button.",
            LinkType = LinkType.Link,
            Href = "#variant-enum"
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-MenuButton-color",
            DefaultValue = "The Color role's on-color (Fill) or main color (Outline, Text)",
            Description = "Text and icon color of both halves of the button at rest.",
        },
        new()
        {
            Name = "--bit-MenuButton-background",
            DefaultValue = "The Color role's main color (Fill), transparent (Outline, Text)",
            Description = "Background of the button at rest.",
        },
        new()
        {
            Name = "--bit-MenuButton-border-color",
            DefaultValue = "The background, or transparent for the Text variant",
            Description = "Border color of the button.",
        },
        new()
        {
            Name = "--bit-MenuButton-border-width",
            DefaultValue = "--bit-shp-brd-width",
            Description = "Border thickness of the button.",
        },
        new()
        {
            Name = "--bit-MenuButton-radius",
            DefaultValue = "--bit-shp-radius-button",
            Description = "Corner radius of the button, followed by its focus ring.",
        },
        new()
        {
            Name = "--bit-MenuButton-hover-color",
            DefaultValue = "The Color role's on-color",
            Description = "Text and icon color of the hovered half (pointer devices only).",
        },
        new()
        {
            Name = "--bit-MenuButton-hover-background",
            DefaultValue = "The Color role's hover color",
            Description = "Background of the hovered half (pointer devices only).",
        },
        new()
        {
            Name = "--bit-MenuButton-active-color",
            DefaultValue = "The Color role's on-color",
            Description = "Text and icon color of the pressed half, of a toggled header button, and of the chevron while the menu is open.",
        },
        new()
        {
            Name = "--bit-MenuButton-active-background",
            DefaultValue = "The Color role's active color",
            Description = "Background of the pressed half, of a toggled header button, and of the chevron while the menu is open.",
        },
        new()
        {
            Name = "--bit-MenuButton-disabled-color",
            DefaultValue = "The Color role's disabled text color",
            Description = "Text and icon color when the menu button is disabled.",
        },
        new()
        {
            Name = "--bit-MenuButton-disabled-background",
            DefaultValue = "The Color role's disabled color (Fill), transparent (Outline, Text)",
            Description = "Background when the menu button is disabled.",
        },
        new()
        {
            Name = "--bit-MenuButton-disabled-border-color",
            DefaultValue = "The disabled background",
            Description = "Border color when the menu button is disabled.",
        },
        new()
        {
            Name = "--bit-MenuButton-focus-color",
            DefaultValue = "The Color role's focus color",
            Description = "Focus ring color of the button.",
        },
        new()
        {
            Name = "--bit-MenuButton-min-height",
            DefaultValue = "--bit-siz-ctrl-sm/md/lg per Size",
            Description = "Smallest height of the button, and the width of the chevron half unless that is set on its own. It is a floor, so the button still grows with a taller icon or a wrapped label.",
        },
        new()
        {
            Name = "--bit-MenuButton-padding",
            DefaultValue = "Per Size, from the control padding tokens",
            Description = "Padding of each half of the button. The chevron half drops the side padding, since it is a square.",
        },
        new()
        {
            Name = "--bit-MenuButton-gap",
            DefaultValue = "0.5rem",
            Description = "Room between the icon, the text and the chevron.",
        },
        new()
        {
            Name = "--bit-MenuButton-font-size",
            DefaultValue = "--bit-tpg-fs-xs/sm/md per Size",
            Description = "Text size of the button and of the items, which inherit it from the callout.",
        },
        new()
        {
            Name = "--bit-MenuButton-icon-size",
            DefaultValue = "--bit-siz-icon-sm/md/lg per Size",
            Description = "Size of every glyph the component draws: the header icon, the chevron, the spinner, an item icon and a check mark.",
        },
        new()
        {
            Name = "--bit-MenuButton-chevron-width",
            DefaultValue = "The min-height of the button",
            Description = "Width of the chevron half of a split button.",
        },
        new()
        {
            Name = "--bit-MenuButton-divider-color",
            DefaultValue = "The text color (Fill) or the border color (Outline, Text)",
            Description = "The hairline between the two halves of a split button.",
        },
        new()
        {
            Name = "--bit-MenuButton-callout-background",
            DefaultValue = "--bit-clr-bg-pri, or the surface of the Background kind",
            Description = "Background of the callout, and of every submenu opened from inside it.",
        },
        new()
        {
            Name = "--bit-MenuButton-callout-radius",
            DefaultValue = "--bit-shp-radius-popup",
            Description = "Corner radius of the callout, applied to the corners away from the button.",
        },
        new()
        {
            Name = "--bit-MenuButton-callout-shadow",
            DefaultValue = "--bit-shd-popup",
            Description = "Elevation of the callout.",
        },
        new()
        {
            Name = "--bit-MenuButton-callout-max-height",
            DefaultValue = "The room the viewport leaves",
            Description = "The tallest the callout grows before its items scroll. Only read when the MaxHeight parameter is set, which is what the parameter writes it as.",
        },
        new()
        {
            Name = "--bit-MenuButton-callout-min-width",
            DefaultValue = "The width of the button",
            Description = "Narrowest the callout gets. The positioning code already stretches the callout to the width of the button, so this is a floor beyond that.",
        },
        new()
        {
            Name = "--bit-MenuButton-callout-max-width",
            DefaultValue = "The width of the viewport",
            Description = "The widest the callout gets before the labels of its items are ellipsized. Without a cap, one long label would widen the callout past the side of the screen.",
        },
        new()
        {
            Name = "--bit-MenuButton-callout-padding",
            DefaultValue = "0",
            Description = "Padding around the list of items inside the callout. Flush by default, so a menu is the list of its rows and nothing else.",
        },
        new()
        {
            Name = "--bit-MenuButton-item-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text and icon color of an item.",
        },
        new()
        {
            Name = "--bit-MenuButton-item-min-height",
            DefaultValue = "--bit-siz-item-sm/md/lg per Size",
            Description = "Smallest height of an item.",
        },
        new()
        {
            Name = "--bit-MenuButton-item-padding",
            DefaultValue = "Per Size, from the control padding tokens",
            Description = "Padding of an item, and of a group header.",
        },
        new()
        {
            Name = "--bit-MenuButton-item-gap",
            DefaultValue = "0.5rem",
            Description = "Room between the check column, the icon, the label and the secondary text of an item.",
        },
        new()
        {
            Name = "--bit-MenuButton-item-hover-background",
            DefaultValue = "--bit-clr-bg-pri-hover",
            Description = "Background of a hovered item, and of the row whose submenu is open. The items are neutral surfaces, so the label keeps its own color.",
        },
        new()
        {
            Name = "--bit-MenuButton-item-active-background",
            DefaultValue = "--bit-clr-bg-pri-active",
            Description = "Background of a pressed item.",
        },
        new()
        {
            Name = "--bit-MenuButton-item-checked-background",
            DefaultValue = "--bit-clr-bg-sec",
            Description = "Background of a checked item, kept while the pointer is elsewhere.",
        },
        new()
        {
            Name = "--bit-MenuButton-item-checked-color",
            DefaultValue = "The Color role's main color",
            Description = "The mark of a checked item: the check mark of a check item, the bullet of a single-choice one.",
        },
        new()
        {
            Name = "--bit-MenuButton-item-disabled-color",
            DefaultValue = "--bit-clr-fg-dis",
            Description = "Text and icon color of a disabled item.",
        },
        new()
        {
            Name = "--bit-MenuButton-item-focus-color",
            DefaultValue = "The Color role's focus color",
            Description = "Focus ring of the item the keyboard navigation is on. It is drawn inside the item, since the callout clips what overflows it.",
        },
        new()
        {
            Name = "--bit-MenuButton-item-separator-color",
            DefaultValue = "--bit-clr-brd-sec",
            Description = "The hairline of a separator item.",
        },
        new()
        {
            Name = "--bit-MenuButton-item-secondary-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "The secondary text of an item - a keyboard shortcut, a count.",
        },
        new()
        {
            Name = "--bit-MenuButton-item-header-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "The label of a group header item.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "menu-button-item",
            Title = "BitMenuButtonItem",
            Parameters =
            [
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The accessible name of the item, for the benefit of screen readers. Set it on an item whose visible label is an icon alone.",
               },
               new()
               {
                   Name = "Checkable",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Turns the item into a check item: it is announced as a checkbox inside the menu, carries its IsChecked state as a check mark, and flips that state when it is clicked.",
               },
               new()
               {
                   Name = "ChildItems",
                   Type = "List<BitMenuButtonItem>",
                   DefaultValue = "[]",
                   Description = "The items of the submenu that opens from this item. An item that has children opens its submenu instead of raising a click: it is announced with aria-haspopup, it carries a trailing chevron, and the arrow keys walk into and out of it.",
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
                   Description = "The value of the href attribute of the item. If provided, the item renders as an anchor tag instead of button.",
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
                   Name = "IsChecked",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "The checked state of a Checkable item. The menu button writes it back as the item is clicked.",
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
                   Name = "IsHeader",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "If true, the item renders as the label of the group of items that follow it. It is presentational: the keyboard navigation steps over it.",
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Determines the selection state of the item.",
               },
               new()
               {
                   Name = "IsSeparator",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "If true, the item renders as a separator line instead of a clickable item.",
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
                   Name = "RadioGroup",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Turns the item into a single-choice item: it is announced as a radio button inside the menu, carries its IsChecked state as a bullet, and checking it clears every other item of the menu button that names the same group. It outranks Checkable where both are set.",
               },
               new()
               {
                   Name = "SecondaryText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The trailing text of the item, shown at its far end and read after its label - a keyboard shortcut, a count, a short hint.",
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
                   Description = "The value of the target attribute of the item when the item renders as an anchor tag (by providing the Href value).",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitMenuButtonItem>?",
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
                   Description = "The tooltip to show when the mouse is placed on the item.",
               }
            ]
        },
        new()
        {
            Id = "menu-button-option",
            Title = "BitMenuButtonOption",
            Parameters =
            [
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The accessible name of the option, for the benefit of screen readers. Set it on an option whose visible label is an icon alone.",
               },
               new()
               {
                   Name = "Checkable",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Turns the option into a check item: it is announced as a checkbox inside the menu, carries its IsChecked state as a check mark, and flips that state when it is clicked.",
               },
               new()
               {
                   Name = "ChildContent",
                   Type = "RenderFragment?",
                   DefaultValue = "null",
                   Description = "The nested BitMenuButtonOption components of the submenu that opens from this option. An option that has children opens its submenu instead of raising a click: it is announced with aria-haspopup, it carries a trailing chevron, and the arrow keys walk into and out of it.",
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
                   Description = "The value of the href attribute of the option. If provided, the option renders as an anchor tag instead of button.",
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
                   Name = "IsChecked",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "The checked state of a Checkable option, which supports two-way binding (@bind-IsChecked).",
               },
               new()
               {
                   Name = "IsCheckedChanged",
                   Type = "EventCallback<bool>",
                   DefaultValue = "",
                   Description = "The callback that is called when the IsChecked value changes, which is what makes @bind-IsChecked work.",
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
                   Name = "IsHeader",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "If true, the option renders as the label of the group of options that follow it. It is presentational: the keyboard navigation steps over it.",
               },
               new()
               {
                   Name = "IsSelected",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Determines the selection state of the item.",
               },
               new()
               {
                   Name = "IsSeparator",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "If true, the option renders as a separator line instead of a clickable item.",
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
                   Name = "OnClick",
                   Type = "EventCallback",
                   DefaultValue = "",
                   Description = "Click event handler of the option.",
               },
               new()
               {
                   Name = "RadioGroup",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Turns the option into a single-choice option: it is announced as a radio button inside the menu, carries its IsChecked state as a bullet, and checking it clears every other option of the menu button that names the same group. It outranks Checkable where both are set.",
               },
               new()
               {
                   Name = "SecondaryText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The trailing text of the option, shown at its far end and read after its label - a keyboard shortcut, a count, a short hint.",
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
                   Description = "The value of the target attribute of the option when the option renders as an anchor tag (by providing the Href value).",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitMenuButtonOption>?",
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
                   Description = "The tooltip to show when the mouse is placed on the option.",
               }
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitMenuButtonClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitMenuButton.",
               },
               new()
               {
                   Name = "Opened",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the opened callout state of the BitMenuButton.",
               },
               new()
               {
                   Name = "OperatorButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for operator button of the BitMenuButton."
               },
               new()
               {
                   Name = "OperatorButtonIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for operator button icon of the BitMenuButton."
               },
               new()
               {
                   Name = "OperatorButtonText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for operator button text of the BitMenuButton."
               },
               new()
               {
                   Name = "Callout",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the callout of the BitMenuButton."
               },
               new()
               {
                   Name = "CalloutContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the callout container of the BitMenuButton."
               },
               new()
               {
                   Name = "ChevronDownButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the chevron down button of the BitMenuButton."
               },
               new()
               {
                   Name = "ChevronDown",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the chevron down of the BitMenuButton."
               },
               new()
               {
                   Name = "Separator",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the separator of the BitMenuButton."
               },
               new()
               {
                   Name = "Submenu",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the submenu callout of each item of the BitMenuButton that opens one."
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the BitMenuButton."
               },
               new()
               {
                   Name = "ItemWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each item wrapper of the BitMenuButton."
               },
               new()
               {
                   Name = "ItemButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each item of the BitMenuButton."
               },
               new()
               {
                   Name = "ItemIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each item icon of the BitMenuButton."
               },
               new()
               {
                   Name = "ItemCheckIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the mark of each check or single-choice item of the BitMenuButton."
               },
               new()
               {
                   Name = "ItemChevron",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the chevron of each item of the BitMenuButton that opens a submenu."
               },
               new()
               {
                   Name = "ItemHeader",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each group header item of the BitMenuButton."
               },
               new()
               {
                   Name = "ItemSecondaryText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the secondary text of each item of the BitMenuButton."
               },
               new()
               {
                   Name = "ItemSeparator",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each item separator of the BitMenuButton."
               },
               new()
               {
                   Name = "ItemText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each item text of the BitMenuButton."
               },
               new()
               {
                   Name = "Overlay",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each overlay of the BitMenuButton."
               },
               new()
               {
                   Name = "Spinner",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the spinner of the BitMenuButton in the loading state."
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the text of the BitMenuButton."
               },
               new()
               {
                   Name = "Toggled",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the toggled state of the BitMenuButton."
               },
            ],
        },
        new()
        {
            Id = "name-selectors",
            Title = "BitMenuButtonNameSelectors",
            Parameters =
            [
                new()
                {
                    Name = "AriaLabel",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.AriaLabel))",
                    Description = "AriaLabel field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Checkable",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.Checkable))",
                    Description = "Checkable field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "ChildItems",
                    Type = "BitNameSelectorPair<TItem, List<TItem>?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.ChildItems))",
                    Description = "ChildItems field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Class",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.Class))",
                    Description = "The CSS Class field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Href",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.Href))",
                    Description = "Href field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Icon",
                    Type = "BitNameSelectorPair<TItem, BitIconInfo?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.Icon))",
                    Description = "Icon field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "IconName",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.IconName))",
                    Description = "IconName field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "IsChecked",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.IsChecked))",
                    Description = "IsChecked field name and selector of the custom input class. The menu button writes the new state back to the named property as a check item is clicked, so a selector alone leaves the toggling to the page.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "IsEnabled",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.IsEnabled))",
                    Description = "IsEnabled field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "IsHeader",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.IsHeader))",
                    Description = "IsHeader field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "IsSelected",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.IsSelected))",
                    Description = "IsSelected field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "IsSeparator",
                    Type = "BitNameSelectorPair<TItem, bool>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.IsSeparator))",
                    Description = "IsSeparator field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Key",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.Key))",
                    Description = "Key field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "OnClick",
                    Type = "BitNameSelectorPair<TItem, Action<TItem>?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.OnClick))",
                    Description = "OnClick field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "RadioGroup",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.RadioGroup))",
                    Description = "RadioGroup field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "SecondaryText",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.SecondaryText))",
                    Description = "SecondaryText field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Style",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.Style))",
                    Description = "Style field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Target",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.Target))",
                    Description = "Target field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Text",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.Text))",
                    Description = "Text field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
                new()
                {
                    Name = "Title",
                    Type = "BitNameSelectorPair<TItem, string?>",
                    DefaultValue = "new(nameof(BitMenuButtonItem.Title))",
                    Description = "Title field name and selector of the custom input class.",
                    Href = "#name-selector-pair",
                    LinkType = LinkType.Link,
                },
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
            Id = "color-kind-enum",
            Name = "BitColorKind",
            Description = "Defines the color kinds available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name = "Primary",
                    Description = "The primary color kind.",
                    Value = "0",
                },
                new()
                {
                    Name = "Secondary",
                    Description = "The secondary color kind.",
                    Value = "1",
                },
                new()
                {
                    Name = "Tertiary",
                    Description = "The tertiary color kind.",
                    Value = "2",
                },
                new()
                {
                    Name = "Transparent",
                    Description = "The transparent color kind.",
                    Value = "3",
                },
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
            Id = "drop-direction-enum",
            Name = "BitDropDirection",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "All",
                    Description = "The callout is positioned in all directions.",
                    Value = "0",
                },
                new()
                {
                    Name = "TopAndBottom",
                    Description = "The callout is positioned in the top and bottom directions.",
                    Value = "1",
                }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size.",
                    Value="2",
                }
            ]
        },
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
    ];
}
