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
            Description = "Child content of component, the content that the badge will apply to. When it is not set the badge renders standalone, in the normal flow of the page."
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
            Description = "Content you want inside the badge. A number is capped by Max and hidden by ShowZero when it is zero, a string is rendered as it is, and any other value is rendered through its ToString(). A badge given no content, no icon and no template at all is not rendered."
        },
        new()
        {
            Name = "ContentTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render inside the badge, in place of Content. A template is content of its own, so neither Max nor ShowZero reads it, and it is markup rather than words, so a Live badge showing one needs a Description before its live region has anything to announce."
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text alternative of the badge for assistive technologies, for example \"5 unread messages\", read in place of the visible content. On a button or link badge that also has an AriaLabel, it describes the control (aria-describedby); on a plain badge the AriaLabel is used in its place when it is not set."
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
            Description = "The URL the badge navigates to, which also turns the badge into a link: an anchor that is focusable, offers the context menu and the middle click, and is announced as a link. While IsEnabled is false the href is dropped and the badge leaves the tab order."
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
            Description = "Lays the badge out next to its child content in the normal flow of the page instead of over it. Overlap stops applying and only the side of Position is read: the Start and Left families put the badge before the child content, every other one after it."
        },
        new()
        {
            Name = "Live",
            Type = "bool",
            DefaultValue = "false",
            Description = "Announces the badge to assistive technologies whenever its content changes, by turning it into a polite live region. The region is kept on the page whether or not the badge itself is, so a counter that appears, changes and disappears is announced every time. It reads out the Description when there is one and the counter itself otherwise."
        },
        new()
        {
            Name = "Max",
            Type = "int?",
            DefaultValue = "null",
            Description = "Max value to display when content is a number. A content above it renders as the max followed by a plus sign, for example 99+, and the badge carries the figure it shortened as its tooltip unless a Title of its own says something better."
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
            Description = "The click event of the badge, which also turns the badge into a keyboard-operable button."
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
            Description = "Renders an expanding ring around the badge to report that something is in progress."
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
            Type = "BitBadgeShape?",
            DefaultValue = "null",
            Description = "The corner shape of the badge.",
            LinkType = LinkType.Link,
            Href = "#shape-enum"
        },
        new()
        {
            Name = "ShowZero",
            Type = "bool",
            DefaultValue = "true",
            Description = "Renders the badge when its content is the number zero. Turn it off for a counter that should disappear once it is emptied. Only a numeric Content counts as zero, and a string is rendered as it is. An icon or a ContentTemplate is content of its own, so it keeps the badge on the page and only the emptied number is taken off it."
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
            Description = "The tooltip to show when the mouse is placed on the badge. It is rendered on the badge itself rather than on the child content underneath it. A badge whose Max has capped its count already spells that count out on hover, so this is only needed when there is something better to say than the figure itself. A title is not a text alternative, so what a screen reader should hear belongs in Description."
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
            Id = "size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size badge.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size badge.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size badge.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "shape-enum",
            Name = "BitBadgeShape",
            Description = "Determines the corner shape of the BitBadge.",
            Items =
            [
                new()
                {
                    Name= "Circular",
                    Description="Fully rounded corners, so a counter reads as a circle and a longer label as a pill.",
                    Value="0",
                },
                new()
                {
                    Name= "Rounded",
                    Description="The corner radius the current theme gives to its controls.",
                    Value="1",
                },
                new()
                {
                    Name= "Square",
                    Description="Square corners with no radius at all.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "position-enum",
            Name = "BitPosition",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "TopLeft",
                    Value = "0"
                },
                new()
                {
                    Name = "TopCenter",
                    Value = "1"
                },
                new()
                {
                    Name = "TopRight",
                    Value = "2"
                },
                new()
                {
                    Name = "TopStart",
                    Value = "3"
                },
                new()
                {
                    Name = "TopEnd",
                    Value = "4"
                },
                new()
                {
                    Name = "CenterLeft",
                    Value = "5"
                },
                new()
                {
                    Name = "Center",
                    Value = "6"
                },
                new()
                {
                    Name = "CenterRight",
                    Value = "7"
                },
                new()
                {
                    Name = "CenterStart",
                    Value = "8"
                },
                new()
                {
                    Name = "CenterEnd",
                    Value = "9"
                },
                new()
                {
                    Name = "BottomLeft",
                    Value = "10"
                },
                new()
                {
                    Name = "BottomCenter",
                    Value = "11"
                },
                new()
                {
                    Name = "BottomRight",
                    Value = "12"
                },
                new()
                {
                    Name = "BottomStart",
                    Value = "13"
                },
                new()
                {
                    Name = "BottomEnd",
                    Value = "14"
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
        new()
        {
            Id = "link-rels-enum",
            Name = "BitLinkRels",
            Description = "The rel attribute defines the relationship between a linked resource and the current document.",
            Items =
            [
                new()
                {
                    Name = "Alternate",
                    Value = "1",
                    Description = "Provides a link to an alternate representation of the document. (i.e. print page, translated or mirror)"
                },
                new()
                {
                    Name = "Author",
                    Value = "2",
                    Description = "Provides a link to the author of the document."
                },
                new()
                {
                    Name = "Bookmark",
                    Value = "4",
                    Description = "Permanent URL used for bookmarking."
                },
                new()
                {
                    Name = "External",
                    Value = "8",
                    Description = "Indicates that the referenced document is not part of the same site as the current document."
                },
                new()
                {
                    Name = "Help",
                    Value = "16",
                    Description = "Provides a link to a help document."
                },
                new()
                {
                    Name = "License",
                    Value = "32",
                    Description = "Provides a link to licensing information for the document."
                },
                new()
                {
                    Name = "Next",
                    Value = "64",
                    Description = "Provides a link to the next document in the series."
                },
                new()
                {
                    Name = "NoFollow",
                    Value = "128",
                    Description = @"Links to an unendorsed document, like a paid link. (""NoFollow"" is used by Google, to specify that the Google search spider should not follow that link)"
                },
                new()
                {
                    Name = "NoOpener",
                    Value = "256",
                    Description = "Requires that any browsing context created by following the hyperlink must not have an opener browsing context."
                },
                new()
                {
                    Name = "NoReferrer",
                    Value = "512",
                    Description = "Makes the referrer unknown. No referrer header will be included when the user clicks the hyperlink."
                },
                new()
                {
                    Name = "Prev",
                    Value = "1024",
                    Description = "The previous document in a selection."
                },
                new()
                {
                    Name = "Search",
                    Value = "2048",
                    Description = "Links to a search tool for the document."
                },
                new()
                {
                    Name = "Tag",
                    Value = "4096",
                    Description = "A tag (keyword) for the current document."
                },
                new()
                {
                    Name = "Me",
                    Value = "8192",
                    Description = "Indicates that the linked document represents the person who owns the current content. (used for identity verification)"
                },
                new()
                {
                    Name = "Opener",
                    Value = "16384",
                    Description = "Requires that any browsing context created by following the hyperlink keeps its opener browsing context. (reverses the implicit noopener modern browsers apply to _blank targets)"
                },
                new()
                {
                    Name = "PrivacyPolicy",
                    Value = "32768",
                    Description = "Links to the privacy policy that applies to the current document. (rendered as privacy-policy)"
                },
                new()
                {
                    Name = "Sponsored",
                    Value = "65536",
                    Description = "Marks the link as an advertisement or paid placement, so search engines do not count it as an organic endorsement."
                },
                new()
                {
                    Name = "TermsOfService",
                    Value = "131072",
                    Description = "Links to the terms of service that apply to the current document. (rendered as terms-of-service)"
                },
                new()
                {
                    Name = "Ugc",
                    Value = "262144",
                    Description = "Marks the link as user-generated content, like forum posts or comments, for search engines."
                }
            ]
        },
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
            DefaultValue = "Per Size",
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
            DefaultValue = "Per Size",
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
            Overlap = true,
            Bordered = true,
            Size = BitSize.Small,
            Color = BitColor.Success,
        }
    ];
}
