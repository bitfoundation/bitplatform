namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.DropMenu;

public partial class BitDropMenuDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "The description of the drop menu for the benefit of screen readers, rendered as the aria-describedby of the button."
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
            Description = "Determines whether the drop menu is in the loading state. It replaces the icon of the button with a spinner and prevents the callout from being opened."
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
            Name = "MatchWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Expands the callout of the drop menu to at least the width of the button of the drop menu."
        },
        new()
        {
            Name = "MaxHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "The maximum height of the callout of the drop menu as a CSS value (e.g. \"20rem\"), beyond which its content scrolls."
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
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Open",
            Type = "() => Task",
            Description = "Opens the callout of the drop menu programmatically."
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
                    Description = "Custom CSS classes/styles for the opened callout state of the BitDropMenu."
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
        }
    ];

    private bool isOpen;
    private bool isLoading;
    private int clickCounter;
    private int openCounter;
    private int dismissCounter;
    private bool mountDefaultIsOpen;
    private BitDropMenu? dropMenuRef;
    private BitColor color = BitColor.Primary;
    private BitColorKind backgroundColorKind = BitColorKind.Primary;
    private BitColorKind borderColorKind = BitColorKind.Primary;
    private BitDropDirection dropDirection = BitDropDirection.TopAndBottom;
}
