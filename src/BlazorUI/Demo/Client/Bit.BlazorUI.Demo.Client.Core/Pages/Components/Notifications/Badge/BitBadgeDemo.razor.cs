namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.Badge;

public partial class BitBadgeDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Bordered",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws a ring around the badge in the color of the page behind it, so it stays legible over a busy child such as an avatar or an image."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content the badge applies to. Without it the badge renders standalone, in the flow of the page."
        },
        new()
        {
            Name = "Classes",
            Type = "BitBadgeClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitBadge.",
            LinkType = LinkType.Link,
            Href = "#badge-class-styles"
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the badge.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
        },
        new()
        {
            Name = "Content",
            Type = "object?",
            DefaultValue = "null",
            Description = "Content you want inside the badge. A number is capped by Max and hidden at zero by ShowZero; any other value renders through its ToString(). A badge with nothing to show is not rendered."
        },
        new()
        {
            Name = "ContentTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom markup rendered in place of Content. Max and ShowZero do not read it, and a Live badge showing one needs a Description to announce."
        },
        new()
        {
            Name = "Decorative",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the badge (never its child) from assistive technologies, for a badge whose focusable child already names the count. A Live region keeps announcing, and a button or link badge is never hidden."
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "Text alternative for assistive technologies (e.g. \"5 unread messages\"), read in place of the visible content. On a button or link badge named by an AriaLabel it describes the control instead; on a plain dot or icon-only badge the AriaLabel stands in for it, while a badge showing a count keeps saying the count."
        },
        new()
        {
            Name = "Dot",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reduces the size of the badge and hides any of its content. Pair it with a Description."
        },
        new()
        {
            Name = "Hidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "The visibility of the badge. A hidden badge is removed from the DOM while its child content keeps rendering."
        },
        new()
        {
            Name = "Href",
            Type = "string?",
            DefaultValue = "null",
            Description = "Turns the badge into a real link to the URL. While Disabled is true the href is dropped and the badge leaves the tab order."
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
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "Inline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lays the badge beside its child content instead of over it. Overlap no longer applies and only the side of Position is read: Start/Left before the child, the rest after."
        },
        new()
        {
            Name = "Live",
            Type = "bool",
            DefaultValue = "false",
            Description = "Announces every change of the badge through a polite live region, including it appearing and disappearing. Reads the Description, or the counter itself."
        },
        new()
        {
            Name = "Max",
            Type = "int?",
            DefaultValue = "null",
            Description = "Caps a numeric Content: above it the badge shows the max followed by a plus (99+) and the real figure as its tooltip, shown wherever the badge takes the pointer (an overlaid one leaves it to its child)."
        },
        new()
        {
            Name = "OffsetX",
            Type = "string?",
            DefaultValue = "null",
            Description = "Moves the badge along the horizontal axis by the given CSS length, on top of its Position. A positive value moves the badge to the right in both directions of writing."
        },
        new()
        {
            Name = "OffsetY",
            Type = "string?",
            DefaultValue = "null",
            Description = "Moves the badge along the vertical axis by the given CSS length, on top of its Position. A positive value moves the badge down."
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Turns the badge into a real, keyboard-operable button. The click does not reach the element underneath."
        },
        new()
        {
            Name = "Overlap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Pulls the badge in over the child content, for a child with a rounded outline such as an avatar."
        },
        new()
        {
            Name = "Position",
            Type = "BitPosition?",
            DefaultValue = "null",
            Description = "The position of the badge. The Left/Right positions are physical, while the Start/End ones follow the direction of writing.",
            LinkType = LinkType.Link,
            Href = "#position-enum"
        },
        new()
        {
            Name = "Pulse",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders an expanding ring around the badge to report that something is in progress. Under reduced motion it stops and stays as a still halo."
        },
        new()
        {
            Name = "Rel",
            Type = "BitLinkRels?",
            DefaultValue = "null",
            Description = "The relationship between the current document and the one the Href of the badge leads to. With no value of its own, a badge opening in a new browsing context gets rel=\"noopener\" automatically.",
            LinkType = LinkType.Link,
            Href = "#link-rels-enum"
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the direction flow of the content of the badge, which puts the icon after the content."
        },
        new()
        {
            Name = "Shape",
            Type = "BitShape?",
            DefaultValue = "null",
            Description = "The corner shape of the badge. Only Pill, Rounded and Square are honoured: a badge takes its box from its own content, so Circle has no proportions to impose and falls back to the default.",
            LinkType = LinkType.Link,
            Href = "#shape-enum"
        },
        new()
        {
            Name = "ShowZero",
            Type = "bool",
            DefaultValue = "true",
            Description = "Renders the badge when its numeric Content is zero. Turn it off to hide an emptied counter; an icon or a ContentTemplate keeps the badge on the page."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the badge: its height, type size and padding, and the diameter of a dot.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitBadgeClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitBadge.",
            LinkType = LinkType.Link,
            Href = "#badge-class-styles"
        },
        new()
        {
            Name = "Target",
            Type = "string?",
            DefaultValue = "null",
            Description = "The browsing context the Href of the badge is opened in, for example _blank."
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip of the badge itself (not of its child). Replaces the real figure a capped count shows on its own. Not a text alternative: use Description for screen readers. An overlaid badge lets the pointer through to its child, so its tooltip shows once it is standalone, inline or clickable."
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the badge.",
            LinkType = LinkType.Link,
            Href = "#variant-enum"
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        DemoSharedEnums.BitColor(),
        DemoSharedEnums.BitSize(),
        DemoSharedEnums.BitShape(),
        DemoSharedEnums.BitPosition(),
        DemoSharedEnums.BitVariant(),
        DemoSharedEnums.BitLinkRels(),
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "badge-class-styles",
            Title = "BitBadgeClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitBadge."
               },
               new()
               {
                   Name = "BadgeWrapper",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the badge wrapper of the BitBadge."
               },
               new()
               {
                   Name = "Badge",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the badge of the BitBadge."
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the BitBadge."
               },
               new()
               {
                   Name = "Content",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the content of the BitBadge."
               },
               new()
               {
                   Name = "Description",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the visually hidden description of the BitBadge."
               },
               new()
               {
                   Name = "LiveRegion",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the visually hidden live region of the BitBadge, rendered while Live is on and the badge is not a button."
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



    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Badge-color",
            DefaultValue = "Per variant, from the Color role",
            Description = "Text and icon color. In the Outline variant it is also the border color.",
        },
        new()
        {
            Name = "--bit-Badge-background",
            DefaultValue = "Per variant, from the Color role",
            Description = "Background. In the Fill variant it is also the border color.",
        },
        new()
        {
            Name = "--bit-Badge-border-color",
            DefaultValue = "The background (Fill), the text color (Outline), transparent (Text)",
            Description = "Border color, in every state.",
        },
        new()
        {
            Name = "--bit-Badge-hover-background",
            DefaultValue = "Per variant, from the Color role",
            Description = "Background of a clickable (OnClick or Href) badge on hover. Set it together with --bit-Badge-background.",
        },
        new()
        {
            Name = "--bit-Badge-active-background",
            DefaultValue = "Per variant, from the Color role",
            Description = "Background of a clickable badge while pressed.",
        },
        new()
        {
            Name = "--bit-Badge-focus-color",
            DefaultValue = "The Color role's focus color",
            Description = "Keyboard focus ring color of a clickable badge.",
        },
        new()
        {
            Name = "--bit-Badge-border-width",
            DefaultValue = "--bit-shp-brd-width",
            Description = "Border width.",
        },
        new()
        {
            Name = "--bit-Badge-radius",
            DefaultValue = "Per Shape",
            Description = "Corner radius. A dot is always a circle.",
        },
        new()
        {
            Name = "--bit-Badge-height",
            DefaultValue = "--bit-siz-badge-{sm,md,lg}, per Size",
            Description = "Height, and the smallest width, which keeps a single digit a circle.",
        },
        new()
        {
            Name = "--bit-Badge-padding",
            DefaultValue = "Per Size",
            Description = "Padding.",
        },
        new()
        {
            Name = "--bit-Badge-font-size",
            DefaultValue = "Per Size, from the type ramp",
            Description = "Text size.",
        },
        new()
        {
            Name = "--bit-Badge-font-weight",
            DefaultValue = "--bit-tpg-fw-semibold",
            Description = "Text weight.",
        },
        new()
        {
            Name = "--bit-Badge-gap",
            DefaultValue = "spacing(0.5)",
            Description = "Room between the icon and the content.",
        },
        new()
        {
            Name = "--bit-Badge-dot-size",
            DefaultValue = "--bit-siz-badge-dot-{sm,md,lg}, per Size",
            Description = "Diameter of a Dot badge.",
        },
        new()
        {
            Name = "--bit-Badge-inset",
            DefaultValue = "spacing(0.5)",
            Description = "How far an overlaid badge reaches back in over the edge of its child.",
        },
        new()
        {
            Name = "--bit-Badge-overlap-inset",
            DefaultValue = "spacing(1.5)",
            Description = "The same inset when Overlap is on.",
        },
        new()
        {
            Name = "--bit-Badge-inline-gap",
            DefaultValue = "spacing(0.75)",
            Description = "Room between an Inline badge and its child.",
        },
        new()
        {
            Name = "--bit-Badge-z-index",
            DefaultValue = "1",
            Description = "Layer an overlaid badge is painted on, one above its child by default.",
        },
        new()
        {
            Name = "--bit-Badge-ring-color",
            DefaultValue = "--bit-clr-bg-pri",
            Description = "Color of the Bordered ring; match it to the surface behind the badge.",
        },
        new()
        {
            Name = "--bit-Badge-ring-width",
            DefaultValue = "--bit-shp-brd-width-thick",
            Description = "Width of the Bordered ring.",
        },
        new()
        {
            Name = "--bit-Badge-pulse-color",
            DefaultValue = "The Color role's main color",
            Description = "Color of the Pulse ring.",
        },
    ];



    private bool hidden;
    private int count = 3;
    private int counter;
    private int unread = 3;
    private BitPosition badgePosition;
    private readonly List<BitDropdownItem<BitPosition>> badgePositionList = Enum.GetValues<BitPosition>()
        .Select(enumValue => new BitDropdownItem<BitPosition>
        {
            Value = enumValue,
            Text = enumValue.ToString()
        })
        .ToList();

    private readonly BitColor[] semanticColors =
    [
        BitColor.Primary,
        BitColor.Secondary,
        BitColor.Tertiary,
        BitColor.Info,
        BitColor.Success,
        BitColor.Warning,
        BitColor.SevereWarning,
        BitColor.Error,
    ];

    private readonly BitBadgeParams[] badgeParams =
    [
        new()
        {
            Max = 99,
            Bordered = true,
            Shape = BitShape.Rounded,
            Variant = BitVariant.Outline,
        }
    ];
}
