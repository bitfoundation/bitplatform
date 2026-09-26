namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.DropMenu;

public partial class BitDropMenuDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Alignment",
            Type = "BitCalloutAlignment?",
            DefaultValue = "null",
            Description = "How the callout is lined up with the button across the side it opens on: Start (the default), Center or End.",
            LinkType = LinkType.Link,
            Href = "#callout-alignment-enum"
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "The description of the drop menu for screen readers, rendered as visually hidden text the button points at through aria-describedby."
        },
        new()
        {
            Name = "AriaHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, adds an aria-hidden attribute instructing screen readers to ignore the button of the drop menu."
        },
        new()
        {
            Name = "AutoClose",
            Type = "bool",
            DefaultValue = "false",
            Description = "Closes the callout as soon as a click lands anywhere inside it, as an action list should. Off by default, so a form or a filter panel stays open while it is used."
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Moves the focus into the callout as soon as it opens, to its first focusable element, or to the callout itself when it holds none."
        },
        new()
        {
            Name = "Background",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the background of the callout of the drop menu.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum"
        },
        new()
        {
            Name = "Body",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of the ChildContent."
        },
        new()
        {
            Name = "Border",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The color kind of the border of the callout of the drop menu.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum"
        },
        new()
        {
            Name = "ChevronDownIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon for the chevron down part of the drop menu using custom CSS classes for external icon libraries. Takes precedence over ChevronDownIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info"
        },
        new()
        {
            Name = "ChevronDownIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name for the chevron down part of the drop menu from the built-in Fluent UI icons. For external icon libraries, use ChevronDownIcon instead.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography"
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the callout of the drop menu."
        },
        new()
        {
            Name = "Classes",
            Type = "BitDropMenuClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the drop menu.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the button of the drop menu.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
        },
        new()
        {
            Name = "DefaultIsOpen",
            Type = "bool?",
            DefaultValue = "null",
            Description = "The initial opening state of the callout in the uncontrolled mode, which is when the IsOpen parameter is not set."
        },
        new()
        {
            Name = "DropDirection",
            Type = "BitDropDirection",
            DefaultValue = "BitDropDirection.TopAndBottom",
            Description = "Determines the allowed drop directions of the callout of the drop menu.",
            LinkType = LinkType.Link,
            Href = "#drop-direction-enum"
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Expands the drop menu width to 100% of the available width."
        },
        new()
        {
            Name = "HoverCloseDelay",
            Type = "int",
            DefaultValue = "150",
            Description = "The delay in milliseconds before the callout closes once the pointer leaves the drop menu in the OpenOnHover mode. It bridges the gap between the button and the callout, so moving the pointer from one to the other does not close what the pointer is on its way to."
        },
        new()
        {
            Name = "HoverOpenDelay",
            Type = "int",
            DefaultValue = "0",
            Description = "The delay in milliseconds before the callout opens once the pointer enters the drop menu in the OpenOnHover mode, so that passing over the button on the way somewhere else does not open it."
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the header using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info"
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon to display inside the header from the built-in Fluent UI icons. For external icon libraries, use Icon instead.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography"
        },
        new()
        {
            Name = "IsLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines whether the drop menu is in the loading state: the icon becomes a spinner, an open callout closes and it cannot be opened again until the loading ends. The button keeps the focus and is marked aria-disabled and aria-busy."
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines the opening state of the callout of the drop menu."
        },
        new()
        {
            Name = "LazyRender",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the content of the callout out of the page until the first opening, then keeps it, so its state survives a close."
        },
        new()
        {
            Name = "MatchWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Expands the callout of the drop menu to at least the width of the button of the drop menu. It is applied after the callout is measured, so it takes precedence over Width."
        },
        new()
        {
            Name = "MaxHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "The maximum height of the callout of the drop menu as a CSS value (e.g. \"20rem\"), beyond which its content scrolls. It takes over from the automatic cap that otherwise keeps the callout within the room the viewport leaves, so it should stay within what the shortest screen the drop menu is used on can show."
        },
        new()
        {
            Name = "MaxWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The maximum width of the callout of the drop menu as a CSS value (e.g. \"20rem\"), beyond which its content wraps."
        },
        new()
        {
            Name = "MinWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The minimum width of the callout of the drop menu as a CSS value (e.g. \"20rem\"), so that a narrow content does not end up in a cramped callout."
        },
        new()
        {
            Name = "NoChevron",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the chevron-down icon from the button of the drop menu."
        },
        new()
        {
            Name = "NoShadow",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the box-shadow from the callout of the drop menu."
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback is called when the drop menu is clicked."
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback is called when the drop menu is dismissed."
        },
        new()
        {
            Name = "OnOpen",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback is called when the callout of the drop menu is opened."
        },
        new()
        {
            Name = "OpenOnHover",
            Type = "bool",
            DefaultValue = "false",
            Description = "Opens the callout when the pointer enters the drop menu and closes it when the pointer leaves it, which is what a navigation menu is usually expected to do. The button keeps toggling the callout on a click, so the keyboard and the touch screens - where hovering does not exist and this mode turns itself off - are left with a way to reach it."
        },
        new()
        {
            Name = "PanelPosition",
            Type = "BitPanelPosition?",
            DefaultValue = "null",
            Description = "The position of the responsive panel to show on the screen.",
            LinkType = LinkType.Link,
            Href = "#panel-position-enum"
        },
        new()
        {
            Name = "Responsive",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the drop menu in responsive mode on small screens."
        },
        new()
        {
            Name = "ScrollContainerId",
            Type = "string?",
            DefaultValue = "null",
            Description = "The id of the element which needs to be scrollable in the content of the callout of the drop menu."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the button of the drop menu.",
            LinkType = LinkType.Link,
            Href = "#size-enum"
        },
        new()
        {
            Name = "Styles",
            Type = "BitDropMenuClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the drop menu.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Template",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content to render inside the header of the drop menu."
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text to show inside the header of the drop menu."
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip to show when the mouse is placed on the button of the drop menu."
        },
        new()
        {
            Name = "Transparent",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the background of the header of the drop menu transparent."
        },
        new()
        {
            Name = "TrapFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the keyboard inside the callout while it is open: the focus moves into it as it opens, Tab and Shift+Tab cycle within it instead of running on into the page behind it, and the callout reports itself as a modal dialog to the screen readers. It implies AutoFocus."
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the button of the drop menu: filled (the default look), outlined, or text only. It decides how the Color is painted onto the button, so the two are set together.",
            LinkType = LinkType.Link,
            Href = "#variant-enum"
        },
        new()
        {
            Name = "Width",
            Type = "string?",
            DefaultValue = "null",
            Description = "The width of the callout of the drop menu as a CSS value (e.g. \"20rem\"). By default the callout is only as wide as its content needs. MatchWidth takes precedence over it."
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Open",
            Type = "() => Task",
            Description = "Opens the callout of the drop menu programmatically, unless the drop menu is disabled or loading."
        },
        new()
        {
            Name = "Close",
            Type = "() => Task",
            Description = "Closes the callout of the drop menu programmatically."
        },
        new()
        {
            Name = "Toggle",
            Type = "() => Task",
            Description = "Toggles the callout of the drop menu programmatically."
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-DropMenu-color",
            DefaultValue = "Per Variant, from the Color role",
            Description = "Text and icon color of the button at rest.",
        },
        new()
        {
            Name = "--bit-DropMenu-background",
            DefaultValue = "Per Variant, from the Color role",
            Description = "Background of the button at rest.",
        },
        new()
        {
            Name = "--bit-DropMenu-border-color",
            DefaultValue = "Per Variant",
            Description = "Border color of the button (at rest, on hover and while open).",
        },
        new()
        {
            Name = "--bit-DropMenu-border-width",
            DefaultValue = "--bit-shp-brd-width",
            Description = "Border thickness of the button.",
        },
        new()
        {
            Name = "--bit-DropMenu-radius",
            DefaultValue = "--bit-shp-radius-button",
            Description = "Corner radius of the button, followed by its focus ring.",
        },
        new()
        {
            Name = "--bit-DropMenu-hover-color",
            DefaultValue = "Per Variant, from the Color role",
            Description = "Text and icon color on hover.",
        },
        new()
        {
            Name = "--bit-DropMenu-hover-background",
            DefaultValue = "The Color role's hover color",
            Description = "Background on hover.",
        },
        new()
        {
            Name = "--bit-DropMenu-active-color",
            DefaultValue = "Per Variant, from the Color role",
            Description = "Text and icon color while pressed or while the callout is open.",
        },
        new()
        {
            Name = "--bit-DropMenu-active-background",
            DefaultValue = "The Color role's active color",
            Description = "Background while pressed or while the callout is open.",
        },
        new()
        {
            Name = "--bit-DropMenu-disabled-color",
            DefaultValue = "The Color role's disabled text color",
            Description = "Text and icon color when disabled or loading.",
        },
        new()
        {
            Name = "--bit-DropMenu-disabled-background",
            DefaultValue = "Per Variant, the Color role's disabled color",
            Description = "Background when disabled or loading.",
        },
        new()
        {
            Name = "--bit-DropMenu-disabled-border-color",
            DefaultValue = "Per Variant",
            Description = "Border color when disabled or loading.",
        },
        new()
        {
            Name = "--bit-DropMenu-focus-color",
            DefaultValue = "The Color role's focus color",
            Description = "Focus ring color of the button.",
        },
        new()
        {
            Name = "--bit-DropMenu-min-height",
            DefaultValue = "Per Size, --bit-siz-ctrl-*",
            Description = "Smallest height of the button, and the width of one that holds only an icon.",
        },
        new()
        {
            Name = "--bit-DropMenu-padding",
            DefaultValue = "Per Size",
            Description = "Padding of the button.",
        },
        new()
        {
            Name = "--bit-DropMenu-gap",
            DefaultValue = "spacing(1)",
            Description = "Room between the icon, the text and the chevron.",
        },
        new()
        {
            Name = "--bit-DropMenu-font-size",
            DefaultValue = "Per Size, from the type ramp",
            Description = "Text size of the button.",
        },
        new()
        {
            Name = "--bit-DropMenu-icon-size",
            DefaultValue = "Per Size, --bit-siz-icon-*",
            Description = "Size of the icon, the chevron and the spinner.",
        },
        new()
        {
            Name = "--bit-DropMenu-callout-background",
            DefaultValue = "--bit-clr-bg-pri, or the Background kind",
            Description = "Background of the callout.",
        },
        new()
        {
            Name = "--bit-DropMenu-callout-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text color of the callout.",
        },
        new()
        {
            Name = "--bit-DropMenu-callout-border-color",
            DefaultValue = "None, or the Border kind",
            Description = "Border color of the callout.",
        },
        new()
        {
            Name = "--bit-DropMenu-callout-border-width",
            DefaultValue = "0, or --bit-shp-brd-width with Border",
            Description = "Border thickness of the callout.",
        },
        new()
        {
            Name = "--bit-DropMenu-callout-radius",
            DefaultValue = "--bit-shp-radius-popup",
            Description = "Corner radius of the callout.",
        },
        new()
        {
            Name = "--bit-DropMenu-callout-shadow",
            DefaultValue = "--bit-shd-popup",
            Description = "Elevation of the callout.",
        },
        new()
        {
            Name = "--bit-DropMenu-callout-padding",
            DefaultValue = "0",
            Description = "Padding around the content of the callout.",
        },
        new()
        {
            Name = "--bit-DropMenu-callout-width",
            DefaultValue = "auto (the Width parameter)",
            Description = "Width of the callout.",
        },
        new()
        {
            Name = "--bit-DropMenu-callout-min-width",
            DefaultValue = "auto (the MinWidth parameter)",
            Description = "Narrowest the callout gets.",
        },
        new()
        {
            Name = "--bit-DropMenu-callout-max-width",
            DefaultValue = "none (the MaxWidth parameter)",
            Description = "Widest the callout gets.",
        },
        new()
        {
            Name = "--bit-DropMenu-callout-max-height",
            DefaultValue = "The MaxHeight parameter",
            Description = "Tallest the callout grows before it scrolls; only read when MaxHeight is set.",
        },
        new()
        {
            Name = "--bit-DropMenu-overlay-background",
            DefaultValue = "transparent",
            Description = "The layer behind an open callout.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitDropMenuClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitDropMenu."
                },
                new()
                {
                    Name = "Opened",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitDropMenu while its callout is open, applied on top of the Root ones."
                },
                new()
                {
                    Name = "Button",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the button of the BitDropMenu."
                },
                new()
                {
                    Name = "Spinner",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the loading spinner of the BitDropMenu."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of the BitDropMenu."
                },
                new()
                {
                    Name = "Text",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the text of the BitDropMenu."
                },
                new()
                {
                    Name = "ChevronDown",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the chevron-down icon of the BitDropMenu."
                },
                new()
                {
                    Name = "Overlay",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the overlay of the BitDropMenu."
                },
                new()
                {
                    Name = "Callout",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the callout of the BitDropMenu."
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

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "callout-alignment-enum",
            Name = "BitCalloutAlignment",
            Description = "How a callout is lined up with its anchor along the axis it is not placed on.",
            Items =
            [
                new() { Name = "Start", Description = "Lined up with the edge the anchor starts at. This is the default.", Value = "0" },
                new() { Name = "Center", Description = "Centered on the anchor.", Value = "1" },
                new() { Name = "End", Description = "Lined up with the edge the anchor ends at.", Value = "2" },
            ]
        },
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
                new() { Name = "TertiaryBorder", Description = "Tertiary border color.", Value = "16" },
            ]
        },
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
            Id = "drop-direction-enum",
            Name = "BitDropDirection",
            Description = "Determines the allowed drop directions of the callout.",
            Items =
            [
                new()
                {
                    Name = "All",
                    Description = "The direction determined automatically based on the available spaces in all directions.",
                    Value = "0",
                },
                new()
                {
                    Name = "TopAndBottom",
                    Description = "The direction determined automatically based on the available spaces in only top and bottom directions.",
                    Value = "1",
                },
            ]
        },
        new()
        {
            Id = "panel-position-enum",
            Name = "BitPanelPosition",
            Description = "Determines the edge the responsive panel slides in from.",
            Items =
            [
                new() { Name = "Start", Description = "The panel is positioned at the start edge (left in LTR).", Value = "0" },
                new() { Name = "End", Description = "The panel is positioned at the end edge (right in LTR).", Value = "1" },
                new() { Name = "Top", Description = "The panel is positioned at the top edge.", Value = "2" },
                new() { Name = "Bottom", Description = "The panel is positioned at the bottom edge.", Value = "3" },
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
                new() { Name = "Large", Description = "The large size.", Value = "2" },
            ]
        },
        new()
        {
            Id = "variant-enum",
            Name = "BitVariant",
            Description = "Determines the variant of the content that controls the rendered style of the corresponding element(s).",
            Items =
            [
                new() { Name = "Fill", Description = "Fill styled variant.", Value = "0" },
                new() { Name = "Outline", Description = "Outline styled variant.", Value = "1" },
                new() { Name = "Text", Description = "Text styled variant.", Value = "2" },
            ]
        }
    ];

    private bool isOpen;
    private bool isLoading;
    private string? lazyOpenedAt;
    private int clickCounter;
    private int openCounter;
    private int dismissCounter;
    private bool mountDefaultIsOpen;
    private BitDropMenu? dropMenuRef;
    private string autoCloseAction = "none";
    private BitColor color = BitColor.Primary;
    private BitVariant variant = BitVariant.Fill;
    private BitColorKind backgroundColorKind = BitColorKind.Primary;
    private BitColorKind borderColorKind = BitColorKind.Primary;
    private BitDropDirection dropDirection = BitDropDirection.TopAndBottom;
    private BitCalloutAlignment alignment = BitCalloutAlignment.Start;
    private BitPanelPosition panelPosition = BitPanelPosition.End;

    private readonly BitDropMenuParams[] toolbarDropMenuParams =
    [
        new()
        {
            Size = BitSize.Small,
            Color = BitColor.Primary,
            Variant = BitVariant.Outline,
            AutoClose = true
        }
    ];
}
