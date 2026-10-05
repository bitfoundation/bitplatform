namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Tooltip;

public partial class BitTooltipDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Alignment",
            Type = "BitPlacement",
            DefaultValue = "BitPlacement.Center",
            Description = "Where along Placement the tooltip lines up with its anchor. Start, Center and End are honoured on either axis (Start and End follow the reading direction across, and read top to bottom down); Left and Right only above or below the anchor, Top and Bottom only beside it. Anything else centers it.",
            LinkType = LinkType.Link,
            Href = "#placement-enum"
        },
        new()
        {
            Name = "Anchor",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of ChildContent: the anchor the tooltip belongs to."
        },
        new()
        {
            Name = "ArrowSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The side in pixels of the square the arrow is drawn from. Unset keeps the theme's size."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The anchor the tooltip belongs to and is shown next to."
        },
        new()
        {
            Name = "Classes",
            Type = "BitTooltipClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the tooltip.",
            LinkType = LinkType.Link,
            Href = "#tooltip-class-styles"
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the tooltip surface and its arrow.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
        },
        new()
        {
            Name = "DefaultIsShown",
            Type = "bool?",
            DefaultValue = "null",
            Description = "The shown state the tooltip starts in when IsShown is not bound."
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stretches the element the anchor is wrapped in to the full width, so a block-level anchor keeps its width."
        },
        new()
        {
            Name = "HideArrow",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the arrow."
        },
        new()
        {
            Name = "HideDelay",
            Type = "int",
            DefaultValue = "0",
            Description = "Delay in ms before hiding. Inside a BitTooltipGroup an unset one takes the group's."
        },
        new()
        {
            Name = "HideOnClick",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the tooltip when the anchor is pressed (pointer, Enter or Space). ShowOnClick takes the press over."
        },
        new()
        {
            Name = "Interactive",
            Type = "bool",
            DefaultValue = "true",
            Description = "Keeps the tooltip shown while the pointer moves into it (WCAG 1.4.13). False lets the pointer through to what lies underneath."
        },
        new()
        {
            Name = "IsShown",
            Type = "bool",
            DefaultValue = "false",
            Description = "The shown state of the tooltip. Bound one way (without IsShownChanged) it is yours alone: the triggers leave it alone."
        },
        new()
        {
            Name = "IsShownChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "The callback for when the shown state changes."
        },
        new()
        {
            Name = "LazyRender",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the content out of the DOM until the first show. The accessible text is only there from then on."
        },
        new()
        {
            Name = "MaxWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS width the text wraps at; \"none\" removes the cap. Unset keeps the theme's."
        },
        new()
        {
            Name = "NoAnimation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the fade the tooltip is shown and hidden with."
        },
        new()
        {
            Name = "NoDismissOnEscape",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps Escape from dismissing the tooltip. Only for a tooltip that covers nothing (WCAG 1.4.13)."
        },
        new()
        {
            Name = "NoTouch",
            Type = "bool",
            DefaultValue = "false",
            Description = "Ignores touch and pen, leaving the tap to the anchor."
        },
        new()
        {
            Name = "Offset",
            Type = "int?",
            DefaultValue = "null",
            Description = "The gap in pixels between the anchor and the tooltip, never less than the arrow needs. Unset keeps the theme's."
        },
        new()
        {
            Name = "OnHide",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback for when the tooltip is hidden."
        },
        new()
        {
            Name = "OnShow",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback for when the tooltip is shown."
        },
        new()
        {
            Name = "OnToggle",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "The callback for when the tooltip is shown or hidden, with the new state."
        },
        new()
        {
            Name = "Placement",
            Type = "BitPlacement",
            DefaultValue = "BitPlacement.Top",
            Description = "The side of the anchor the tooltip is placed on. Only Top, Bottom, Start, End, Left and Right are honoured: Start and End follow the reading direction, Left and Right stay on the same side of the screen. Anything else leaves it above the anchor.",
            LinkType = LinkType.Link,
            Href = "#placement-enum"
        },
        new()
        {
            Name = "Relationship",
            Type = "BitTooltipRelationship",
            DefaultValue = "BitTooltipRelationship.Description",
            Description = "Whether the tooltip describes (aria-describedby), names (aria-labelledby) or is hidden from its anchor. Copied onto the first focusable control inside.",
            LinkType = LinkType.Link,
            Href = "#tooltip-relationship-enum"
        },
        new()
        {
            Name = "ShowDelay",
            Type = "int",
            DefaultValue = "0",
            Description = "Delay in ms before showing on hover; focus and click show at once. Inside a BitTooltipGroup an unset one takes the group's."
        },
        new()
        {
            Name = "ShowOnClick",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes a press of the anchor (pointer, Enter or Space) toggle the tooltip. Escape, a press outside and Tab also hide it."
        },
        new()
        {
            Name = "ShowOnFocus",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows the tooltip when the anchor takes the keyboard focus. A focus from a pointer press is left to the pointer."
        },
        new()
        {
            Name = "ShowOnHover",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows the tooltip while the pointer is over the anchor."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the text and the padding.",
            LinkType = LinkType.Link,
            Href = "#size-enum"
        },
        new()
        {
            Name = "Styles",
            Type = "BitTooltipClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the tooltip.",
            LinkType = LinkType.Link,
            Href = "#tooltip-class-styles"
        },
        new()
        {
            Name = "Template",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the tooltip, in place of Text."
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the tooltip."
        },
        new()
        {
            Name = "TouchHideDelay",
            Type = "int",
            DefaultValue = "1500",
            Description = "How long in ms a tooltip shown by a touch stays. Zero keeps it until something else hides it."
        },
        new()
        {
            Name = "TouchShowDelay",
            Type = "int",
            DefaultValue = "0",
            Description = "How long in ms a touch has to rest on the anchor before the tooltip shows, making it a long press."
        },
        new()
        {
            Name = "ZIndex",
            Type = "int?",
            DefaultValue = "null",
            Description = "The stacking order of the surface and its arrow. Unset keeps the theme's popup layer."
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Show",
            Type = "Task",
            Description = "Shows the tooltip programmatically, at once and regardless of the triggers it is configured with, unless it is disabled."
        },
        new()
        {
            Name = "Hide",
            Type = "Task",
            Description = "Hides the tooltip programmatically, at once and regardless of the delays it is configured with."
        },
        new()
        {
            Name = "Toggle",
            Type = "Task",
            Description = "Shows the tooltip if it is hidden and hides it if it is shown."
        },
        new()
        {
            Name = "TooltipId",
            Type = "string",
            Description = "The id of the element the text of the tooltip is rendered in, which is what an anchor of your own points its aria-describedby or aria-labelledby at."
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        SharedSubEnums.BitPlacement,
        new()
        {
            Id = "tooltip-relationship-enum",
            Name = "BitTooltipRelationship",
            Description = "Determines the accessible relationship between a tooltip and the anchor it belongs to.",
            Items =
            [
                new()
                {
                    Name = "Description",
                    Value = "0",
                    Description = "The tooltip adds information to an anchor that already has a name of its own, and is pointed at with aria-describedby."
                },
                new()
                {
                    Name = "Label",
                    Value = "1",
                    Description = "The tooltip is the name of an anchor that has none of its own - an icon-only button, above all - and is pointed at with aria-labelledby."
                },
                new()
                {
                    Name = "None",
                    Value = "2",
                    Description = "The tooltip is left out of the accessibility tree altogether, for the case where the anchor already carries the same text by another route."
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
                new() { Name = "Primary", Value = "0", Description = "Primary general color." },
                new() { Name = "Secondary", Value = "1", Description = "Secondary general color." },
                new() { Name = "Tertiary", Value = "2", Description = "Tertiary general color." },
                new() { Name = "Info", Value = "3", Description = "Info general color." },
                new() { Name = "Success", Value = "4", Description = "Success general color." },
                new() { Name = "Warning", Value = "5", Description = "Warning general color." },
                new() { Name = "SevereWarning", Value = "6", Description = "SevereWarning general color." },
                new() { Name = "Error", Value = "7", Description = "Error general color." },
                new() { Name = "PrimaryBackground", Value = "8", Description = "Primary background color." },
                new() { Name = "SecondaryBackground", Value = "9", Description = "Secondary background color." },
                new() { Name = "TertiaryBackground", Value = "10", Description = "Tertiary background color." },
                new() { Name = "PrimaryForeground", Value = "11", Description = "Primary foreground color." },
                new() { Name = "SecondaryForeground", Value = "12", Description = "Secondary foreground color." },
                new() { Name = "TertiaryForeground", Value = "13", Description = "Tertiary foreground color." },
                new() { Name = "PrimaryBorder", Value = "14", Description = "Primary border color." },
                new() { Name = "SecondaryBorder", Value = "15", Description = "Secondary border color." },
                new() { Name = "TertiaryBorder", Value = "16", Description = "Tertiary border color." }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new() { Name = "Small", Value = "0", Description = "The small size tooltip." },
                new() { Name = "Medium", Value = "1", Description = "The medium size tooltip." },
                new() { Name = "Large", Value = "2", Description = "The large size tooltip." }
            ]
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "tooltip-group",
            Title = "BitTooltipGroup",
            Description = "Groups the tooltips inside it: they share its delays, show one at a time, and skip the show delay right after one hides. It renders nothing of its own.",
            Parameters =
            [
                new()
                {
                    Name = "AllowMultiple",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Lets more than one tooltip of the group be shown at a time."
                },
                new()
                {
                    Name = "ChildContent",
                    Type = "RenderFragment?",
                    DefaultValue = "null",
                    Description = "The tooltips the group is around."
                },
                new()
                {
                    Name = "HideDelay",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "The delay in milliseconds before hiding, for every tooltip in the group that does not set one of its own."
                },
                new()
                {
                    Name = "ShowDelay",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "The delay in milliseconds before showing, for every tooltip in the group that does not set one of its own."
                },
                new()
                {
                    Name = "SkipDelay",
                    Type = "int",
                    DefaultValue = "300",
                    Description = "How long in ms after a tooltip of the group hides the next one is shown without its show delay. Zero turns it off."
                }
            ]
        },
        new()
        {
            Id = "tooltip-class-styles",
            Title = "BitTooltipClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitTooltip."
               },
               new()
               {
                   Name = "TooltipWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the tooltip wrapper of the BitTooltip."
               },
               new()
               {
                   Name = "Tooltip",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the tooltip of the BitTooltip."
               },
               new()
               {
                   Name = "Arrow",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the arrow of the BitTooltip."
               }
            ]
        }
    ];



    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Tooltip-background",
            DefaultValue = "--bit-clr-tooltip-bg",
            Description = "Fill of the surface and the arrow. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Tooltip-color",
            DefaultValue = "--bit-clr-tooltip-fg",
            Description = "Text color. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Tooltip-padding",
            DefaultValue = "spacing(1.25)",
            Description = "Room around the content. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Tooltip-font-size",
            DefaultValue = "--bit-tpg-fs-xs",
            Description = "Size of the text. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Tooltip-font-weight",
            DefaultValue = "--bit-tpg-fw-medium",
            Description = "Weight of the text.",
        },
        new()
        {
            Name = "--bit-Tooltip-line-height",
            DefaultValue = "--bit-tpg-caption1-line-height",
            Description = "Height of a line of text, as a ratio of the font size so it follows Size.",
        },
        new()
        {
            Name = "--bit-Tooltip-text-align",
            DefaultValue = "start",
            Description = "Alignment of a text that wraps onto more than one line.",
        },
        new()
        {
            Name = "--bit-Tooltip-radius",
            DefaultValue = "--bit-shp-radius-popup",
            Description = "Corner of the surface.",
        },
        new()
        {
            Name = "--bit-Tooltip-shadow",
            DefaultValue = "--bit-shd-tooltip",
            Description = "Elevation of the surface and the arrow.",
        },
        new()
        {
            Name = "--bit-Tooltip-max-width",
            DefaultValue = "20rem",
            Description = "Width the text wraps at. The MaxWidth parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Tooltip-offset",
            DefaultValue = "spacing(1.25)",
            Description = "Distance from the anchor, never less than the arrow needs. The Offset parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Tooltip-arrow-size",
            DefaultValue = "spacing(1.5)",
            Description = "Side of the square the arrow is drawn from. The ArrowSize parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Tooltip-z-index",
            DefaultValue = "--bit-zin-callout",
            Description = "Stacking order of the surface and the arrow. The ZIndex parameter wins over it.",
        },
    ];



    private readonly BitTooltipParams[] tooltipParams =
    [
        new()
        {
            Relationship = BitTooltipRelationship.Label,
            Placement = BitPlacement.Bottom,
            Color = BitColor.PrimaryForeground,
            ShowDelay = 400,
        }
    ];

    // The twelve placements of the Placement & alignment example, as the pairs the two parameters take. The four
    // sides are named logically, so the grid reads the same way round in both directions.
    private readonly (BitPlacement Placement, BitPlacement Alignment, string Text)[] tooltipPlacements =
    [
        (BitPlacement.Top, BitPlacement.Start, "Top / Start"),
        (BitPlacement.Top, BitPlacement.Center, "Top"),
        (BitPlacement.Top, BitPlacement.End, "Top / End"),
        (BitPlacement.End, BitPlacement.Start, "End / Start"),
        (BitPlacement.End, BitPlacement.Center, "End"),
        (BitPlacement.End, BitPlacement.End, "End / End"),
        (BitPlacement.Bottom, BitPlacement.Start, "Bottom / Start"),
        (BitPlacement.Bottom, BitPlacement.Center, "Bottom"),
        (BitPlacement.Bottom, BitPlacement.End, "Bottom / End"),
        (BitPlacement.Start, BitPlacement.Start, "Start / Start"),
        (BitPlacement.Start, BitPlacement.Center, "Start"),
        (BitPlacement.Start, BitPlacement.End, "Start / End"),
    ];

    private bool isShown;

    private BitTooltip? tooltipRef;

    private readonly List<string> events = [];
}
