namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Card;

public partial class BitCardDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Actions",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Content at the end of the header, for what acts on the whole card (a menu, a dismiss button). Its clicks never reach the card, and it stays above the link of a linked card.",
        },
        new()
        {
            Name = "Background",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "The neutral surface color of the card. Unset, the card reads --bit-Card-background, then the theme's secondary background.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "Border",
            Type = "BitColorKind?",
            DefaultValue = "null",
            Description = "Draws a border in one of the neutral border colors. Wins over the color Outlined asks for.",
            LinkType = LinkType.Link,
            Href = "#color-kind-enum",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the card: the whole padded box on a plain card, the body on one with a cover, a header or a footer.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitCardClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the card.",
            LinkType = LinkType.Link,
            Href = "#card-class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "Paints the card in a theme role, the way the Variant asks for. Unset, the card stays a neutral surface.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "Cover",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Full-bleed media at the top of the card (a chart, a carousel, a video), clipped to its corners. Takes precedence over ImageUrl.",
        },
        new()
        {
            Name = "CoverOverlay",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lays the cover behind the content, filling the card. Give the card a Height or MinHeight, and a --bit-Card-scrim so its text stays readable.",
        },
        new()
        {
            Name = "CoverRatio",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aspect ratio of the cover, as a CSS ratio such as 16 / 9. Keeps a row of cards level at any width.",
        },
        new()
        {
            Name = "CoverWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The width of the cover of a Horizontal card. Unset, it reads --bit-Card-cover-width, then a third of the card.",
        },
        new()
        {
            Name = "Divider",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws a hairline between the header, the body and the footer - only between parts, never on the outer edge.",
        },
        new()
        {
            Name = "Download",
            Type = "string?",
            DefaultValue = "null",
            Description = "The download attribute of the link of a card that has an Href.",
        },
        new()
        {
            Name = "Elevation",
            Type = "int?",
            DefaultValue = "null",
            Description = "A level of the theme's shadow ramp (0-24, --bit-shd-0 to --bit-shd-24) the card rests at and lifts from on hover. 0 is flat.",
        },
        new()
        {
            Name = "FloatingActions",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Content floated over the top corner of the card, above everything else - a favorite toggle or a selection box on a picture card. Its clicks never reach the card.",
        },
        new()
        {
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Content under the body, for the actions a reader takes. Its clicks never reach the card, and it stays above the link of a linked card.",
        },
        new()
        {
            Name = "FullHeight",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the card height 100% of its parent container.",
        },
        new()
        {
            Name = "FullSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the card width and height 100% of its parent container.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the card width 100% of its parent container.",
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Replaces the icon, the title and the subtitle; Actions still renders beside it. A linked or clickable card with one is named by its body; an AriaLabel says it shorter.",
        },
        new()
        {
            Name = "HeadingLevel",
            Type = "int?",
            DefaultValue = "null",
            Description = "Makes the title a heading of this level (1-6). Ignored under a splatted role that presents its children, such as option or tab.",
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the height of the card explicitly.",
        },
        new()
        {
            Name = "Horizontal",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lays the cover beside the content instead of above it.",
        },
        new()
        {
            Name = "Hoverable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lifts the card under the pointer. A clickable or linked card lifts on its own.",
        },
        new()
        {
            Name = "Href",
            Type = "string?",
            DefaultValue = "null",
            Description = "Stretches a link to this URL over the whole card, named by the AriaLabel, an aria-labelledby, the Title, the Subtitle (which describes a link the Title names) or the body, or else by all the card says. Actions, Footer and FloatingActions stay above it; the rest of the card is under it.",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The header icon from an external library. Takes precedence over IconName.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the header icon from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "IconTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Replaces only the header icon (an avatar, a logo) and keeps the Title and the Subtitle beside it.",
        },
        new()
        {
            Name = "ImageAlt",
            Type = "string?",
            DefaultValue = "null",
            Description = "The alternate text of the cover image. Unset, the image is decorative (an empty alt) and skipped by screen readers.",
        },
        new()
        {
            Name = "ImageHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "The height of the cover image, which is cropped to fill it rather than stretched.",
        },
        new()
        {
            Name = "ImageLoading",
            Type = "BitImageLoading?",
            DefaultValue = "null",
            Description = "The loading behavior of the cover image, eager or lazy.",
            LinkType = LinkType.Link,
            Href = "#image-loading-enum",
        },
        new()
        {
            Name = "ImagePosition",
            Type = "string?",
            DefaultValue = "null",
            Description = "Which part of a cropped cover image stays in frame, as a CSS object-position such as top or 50% 20%.",
        },
        new()
        {
            Name = "ImageUrl",
            Type = "string?",
            DefaultValue = "null",
            Description = "The URL of the cover image at the top of the card.",
        },
        new()
        {
            Name = "Loading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Swaps the body for a placeholder and marks the card aria-busy. The header keeps rendering.",
        },
        new()
        {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "A custom placeholder for the body while Loading is set.",
        },
        new()
        {
            Name = "MaxHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the maximum height of the card.",
        },
        new()
        {
            Name = "MaxWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the maximum width of the card.",
        },
        new()
        {
            Name = "MinHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the minimum height of the card.",
        },
        new()
        {
            Name = "MinWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the minimum width of the card.",
        },
        new()
        {
            Name = "NoPadding",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the inset of the card and of all its parts.",
        },
        new()
        {
            Name = "NoShadow",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the resting shadow. The card still lifts on hover when it is clickable, linked or Hoverable.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Stretches a native button over the card, named like the link of Href, so the title stays a heading and Actions, Footer and FloatingActions stay separate controls. The button lets the pointer through, so the body still scrolls and its controls still answer the pointer. Under a splatted widget role (option, tab, row) the card itself is the control; a structural one (listitem, article) keeps the button.",
        },
        new()
        {
            Name = "Outlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Trades the shadow for a border. An explicit Border still decides its color.",
        },
        new()
        {
            Name = "Rel",
            Type = "BitLinkRels?",
            DefaultValue = "null",
            Description = "The rel attribute of the link of a card that has an Href. A Target of _blank always adds noopener.",
            LinkType = LinkType.Link,
            Href = "#link-rels-enum",
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Puts the cover after the content: under the body, or at the end of a Horizontal card.",
        },
        new()
        {
            Name = "ScrollableBody",
            Type = "bool",
            DefaultValue = "false",
            Description = "Scrolls content that outgrows a bounded height; on a card with a header and a footer only the body scrolls.",
        },
        new()
        {
            Name = "Selected",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws the selection ring. Binding it makes the card a toggle whose button reports aria-pressed, or aria-selected under a splatted option, row, gridcell, tab or treeitem role, or aria-checked under a splatted checkbox, switch or radio role.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "Scales the padding, the gaps and the header type together. An explicit value wins over the --bit-Card-* size variables; left unset, the card is medium unless they say otherwise.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Square",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the corner radius.",
        },
        new()
        {
            Name = "StopPropagation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stops the click of the card from reaching its ancestors.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitCardClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the card.",
            LinkType = LinkType.Link,
            Href = "#card-class-styles",
        },
        new()
        {
            Name = "Subtitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The second line of the header, under the title.",
        },
        new()
        {
            Name = "Target",
            Type = "string?",
            DefaultValue = "null",
            Description = "The target attribute of the link of a card that has an Href.",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The first line of the header. It also names the link of a linked card.",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "How a Color is applied: Fill (the default) paints the surface, Outline the border, Text tints the surface; the last two write in the role's readable foreground shade. Ignored without a Color.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
        new()
        {
            Name = "Width",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the width of the card explicitly.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Focuses the card: the stretched link or button of a linked or clickable card, or else the root of one given a role or a TabIndex."
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "card-class-styles",
            Title = "BitCardClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitCard."
                },
                new()
                {
                    Name = "Link",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the stretched link (Href) or button (OnClick) that covers the card."
                },
                new()
                {
                    Name = "FloatingActions",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the slot floated over the corner of the BitCard."
                },
                new()
                {
                    Name = "Cover",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the full-bleed media area at the head of the BitCard."
                },
                new()
                {
                    Name = "Image",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the image rendered from the ImageUrl of the BitCard."
                },
                new()
                {
                    Name = "Main",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the element that holds the header, the body and the footer of the BitCard."
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header of the BitCard."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the leading icon of the header of the BitCard."
                },
                new()
                {
                    Name = "HeaderText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the element that holds the title and the subtitle of the BitCard."
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the title of the BitCard."
                },
                new()
                {
                    Name = "Subtitle",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the subtitle of the BitCard."
                },
                new()
                {
                    Name = "Actions",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the actions rendered at the trailing edge of the header of the BitCard."
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the body of the BitCard, which is what the ChildContent renders into."
                },
                new()
                {
                    Name = "Skeleton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the default placeholder of a loading BitCard, drawn when no LoadingTemplate is set."
                },
                new()
                {
                    Name = "SkeletonBar",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each bar of the default placeholder of a loading BitCard."
                },
                new()
                {
                    Name = "Footer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the footer of the BitCard."
                },
                new()
                {
                    Name = "Selected",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitCard while it is selected."
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
        DemoSharedEnums.BitColorKind(),
        DemoSharedEnums.BitColor(),
        DemoSharedEnums.BitImageLoading(),
        DemoSharedEnums.BitSize(),
        DemoSharedEnums.BitVariant(),
        DemoSharedEnums.BitLinkRels(),
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-Card-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text color. A Color on the card wins over it.",
        },
        new()
        {
            Name = "--bit-Card-background",
            DefaultValue = "--bit-clr-bg-sec",
            Description = "Background. A Background or a Color on the card wins over it.",
        },
        new()
        {
            Name = "--bit-Card-border-color",
            DefaultValue = "--bit-clr-brd-pri",
            Description = "Border color, on every card that draws a border. A Border or a Color on the card wins over it.",
        },
        new()
        {
            Name = "--bit-Card-border-width",
            DefaultValue = "0; --bit-shp-brd-width on an Outlined card or one given a Border",
            Description = "Border width. Set on its own it draws a border around every card.",
        },
        new()
        {
            Name = "--bit-Card-radius",
            DefaultValue = "--bit-shp-radius-surface",
            Description = "Corner radius. Square wins over it.",
        },
        new()
        {
            Name = "--bit-Card-shadow",
            DefaultValue = "--bit-shd-card",
            Description = "Resting shadow. Elevation, NoShadow and Outlined win over it.",
        },
        new()
        {
            Name = "--bit-Card-hover-shadow",
            DefaultValue = "--bit-shd-card-hover",
            Description = "Shadow of a clickable, linked or Hoverable card under the pointer. An Elevation wins over it.",
        },
        new()
        {
            Name = "--bit-Card-active-shadow",
            DefaultValue = "The resting shadow",
            Description = "Shadow of a clickable or linked card while pressed.",
        },
        new()
        {
            Name = "--bit-Card-hover-background",
            DefaultValue = "The resting background washed 5% with the text color",
            Description = "Background of a clickable or linked card under the pointer. A Fill card uses its role's hover shade instead.",
        },
        new()
        {
            Name = "--bit-Card-active-background",
            DefaultValue = "The resting background washed 10% with the text color",
            Description = "Background of a clickable or linked card while pressed. A Fill card uses its role's pressed shade instead.",
        },
        new()
        {
            Name = "--bit-Card-padding",
            DefaultValue = "--bit-spa-card-md",
            Description = "Inset of the card and of each of its parts. The Size and NoPadding parameters win over it.",
        },
        new()
        {
            Name = "--bit-Card-gap",
            DefaultValue = "spacing(1.5)",
            Description = "Room between the parts, and between the header icon, text and actions. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Card-title-font-size",
            DefaultValue = "--bit-tpg-fs-md",
            Description = "Title text size. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Card-title-font-weight",
            DefaultValue = "--bit-tg-fw-semibold",
            Description = "Title text weight.",
        },
        new()
        {
            Name = "--bit-Card-subtitle-font-size",
            DefaultValue = "--bit-tpg-fs-sm",
            Description = "Subtitle text size. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Card-subtitle-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Subtitle color. A card painted by a Color, or disabled, gives the subtitle its own text color instead.",
        },
        new()
        {
            Name = "--bit-Card-icon-size",
            DefaultValue = "--bit-siz-icon-md",
            Description = "Size of the header icon. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-Card-divider-color",
            DefaultValue = "--bit-clr-brd-sec",
            Description = "Color of the Divider hairlines.",
        },
        new()
        {
            Name = "--bit-Card-selected-color",
            DefaultValue = "--bit-clr-pri",
            Description = "Color of the Selected ring. A Fill card draws it in its own text color instead.",
        },
        new()
        {
            Name = "--bit-Card-focus-color",
            DefaultValue = "The text color",
            Description = "Color of the keyboard focus ring, on the card and on the link of a linked card.",
        },
        new()
        {
            Name = "--bit-Card-cover-width",
            DefaultValue = "33%",
            Description = "Width of the cover of a Horizontal card. CoverWidth wins over it.",
        },
        new()
        {
            Name = "--bit-Card-scrim",
            DefaultValue = "none",
            Description = "A layer painted over a CoverOverlay picture and under the content, such as a dark gradient, so the text over it stays readable.",
        },
        new()
        {
            Name = "--bit-Card-skeleton-background",
            DefaultValue = "The text color at 10%",
            Description = "Resting color of the bars of the default placeholder of a loading card.",
        },
        new()
        {
            Name = "--bit-Card-skeleton-color",
            DefaultValue = "The text color at 20%",
            Description = "Color of the wave that sweeps across those bars.",
        },
    ];



    private int clickCount;
    private bool isPinned;
    private bool isStarred;
    private bool isLoading = true;
    private bool isBackupsSelected = true;
    private bool isMonitoringSelected;
    private bool isBrandSelected = true;
    private double elevation = 4;

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

    private readonly BitCardParams[] cardParams =
    [
        new()
        {
            Outlined = true,
            Divider = true,
            Size = BitSize.Small,
            Width = "14rem",
            HeadingLevel = 3,
        }
    ];
}
