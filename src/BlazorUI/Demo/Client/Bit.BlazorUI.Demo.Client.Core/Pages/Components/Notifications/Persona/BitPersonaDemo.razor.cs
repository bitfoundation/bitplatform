namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.Persona;

public partial class BitPersonaDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ActionButtonTitle",
            Type = "string",
            DefaultValue = "Edit image",
            Description = "The title of the action button (tooltip).",
        },
        new()
        {
            Name = "ActionIconName",
            Type = "string?",
            DefaultValue = "",
            Description = "Icon name for the icon button of the custom action.",
        },
        new()
        {
            Name = "ActionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "",
            Description = "Optional Custom template for the custom action element.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitPersonaClassStyles",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitPersona component.",
            Href = "#persona-class-styles",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "CoinColor",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The background color when the user's initials are displayed.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "CoinShape",
            Type = "BitPersonaCoinShape?",
            DefaultValue = "null",
            Description = "The shape of the coin.",
            LinkType = LinkType.Link,
            Href = "#shape-enum",
        },
        new()
        {
            Name = "CoinSize",
            Type = "int?",
            DefaultValue = "",
            Description = "Optional custom persona coin size in pixel.",
        },
        new()
        {
            Name = "CoinTemplate",
            Type = "RenderFragment?",
            DefaultValue = "",
            Description = "Custom persona coin's image template.",
        },
        new()
        {
            Name = "CoinVariant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The variant of the coin.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
        new()
        {
            Name = "HidePersonaDetails",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to not render persona details, and just render the persona image/initials.",
        },
        new()
        {
            Name = "ImageAlt",
            Type = "string?",
            DefaultValue = "null",
            Description = "Alt text for the image to use. default is empty string.",
        },
        new()
        {
            Name = "ImageInitials",
            Type = "string?",
            DefaultValue = "null",
            Description = "The user's initials to display in the image area when there is no image.",
        },
        new()
        {
            Name = "ImageOverlayTemplate",
            Type = "RenderFragment?",
            DefaultValue = "",
            Description = "Optional Custom template for the image overlay.",
        },
        new()
        {
            Name = "ImageOverlayText",
            Type = "string?",
            DefaultValue = "Edit image",
            Description = "The user's initials to display in the image area when there is no image.",
        },
        new()
        {
            Name = "ImageUrl",
            Type = "string?",
            DefaultValue = "null",
            Description = "Url to the image to use, should be a square aspect ratio and big enough to fit in the image area.",
        },
        new()
        {
            Name = "OnActionClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "null",
            Description = "Callback for the persona custom action.",
        },
        new()
        {
            Name = "OnImageClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "null",
            Description = "Callback for when the image clicked.",
        },
        new()
        {
            Name = "OptionalText",
            Type = "string?",
            DefaultValue = "null",
            Description = "Optional text to display, usually a custom message set. The optional text will only be shown when using size100.",
        },
        new()
        {
            Name = "OptionalTextTemplate",
            Type = "RenderFragment?",
            DefaultValue = "",
            Description = "Custom optional text template.",
        },
        new()
        {
            Name = "Presence",
            Type = "BitPersonaPresence",
            LinkType = LinkType.Link,
            Href = "#presence-enum",
            DefaultValue = "BitPersonaPresence.None",
            Description = "Presence of the person to display - will not display presence if undefined.",
        },
        new()
        {
            Name = "PresenceIcons",
            Type = "Dictionary<BitPersonaPresence, string>?",
            DefaultValue = "null",
            Description = "The icons to be used for the presence status.",
        },
        new()
        {
            Name = "PresenceTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "Presence title to be shown as a tooltip on hover over the presence icon.",
        },
        new()
        {
            Name = "PrimaryText",
            Type = "string?",
            DefaultValue = "null",
            Description = "Primary text to display, usually the name of the person.",
        },
        new()
        {
            Name = "PrimaryTextTemplate",
            Type = "RenderFragment?",
            DefaultValue = "",
            Description = "Custom primary text template.",
        },
        new()
        {
            Name = "SecondaryText",
            Type = "string?",
            DefaultValue = "null",
            Description = "Secondary text to display, usually the role of the user.",
        },
        new()
        {
            Name = "SecondaryTextTemplate",
            Type = "RenderFragment?",
            DefaultValue = "",
            Description = "Custom secondary text template.",
        },
        new()
        {
            Name = "ShowInitialsUntilImageLoads",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true renders the initials while the image is loading. This only applies when an imageUrl is provided.",
        },
        new()
        {
            Name = "Unknown",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, show the special coin for unknown persona. It has '?' in place of initials, with static font and background colors.",
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the texts and image location.",
        },
        new()
        {
            Name = "Size",
            Type = "string?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#size-enum",
            Description = "Decides the size of the control.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitPersonaClassStyles",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitPersona component.",
            Href = "#persona-class-styles",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "TertiaryText",
            Type = "string?",
            DefaultValue = "null",
            Description = "Tertiary text to display, usually the status of the user. The tertiary text will only be shown when using size72 or size100.",
        },
        new()
        {
            Name = "TertiaryTextTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom tertiary text template.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "persona-class-styles",
            Title = "BitPersonaClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitPersona."
                },
                new()
                {
                    Name = "CoinContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the coin container of the BitPersona."
                },
                new()
                {
                    Name = "PresentationIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the presentation icon of the BitPersona."
                },
                new()
                {
                    Name = "Presentation",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the presentation of the BitPersona."
                },
                new()
                {
                    Name = "ImageContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the image container of the BitPersona.."
                },
                new()
                {
                    Name = "UnknownIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the unknown icon of the BitPersona."
                },
                new()
                {
                    Name = "ImageOverlay",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the image overlay of the BitPersona."
                },
                new()
                {
                    Name = "ImageOverlayText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the image overlay text of the BitPersona."
                },
                new()
                {
                    Name = "Initials",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the initials of the BitPersona."
                },
                new()
                {
                    Name = "Image",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the image of the BitPersona."
                },
                new()
                {
                    Name = "ActionButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the action button of the BitPersona."
                },
                new()
                {
                    Name = "ActionButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the action button icon of the BitPersona."
                },
                new()
                {
                    Name = "Presence",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the presence of the BitPersona."
                },
                new()
                {
                    Name = "DetailsContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the details container of the BitPersona."
                },
                new()
                {
                    Name = "PrimaryTextContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the primary text container of the BitPersona."
                },
                new()
                {
                    Name = "SecondaryTextContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the secondary text container of the BitPersona."
                },
                new()
                {
                    Name = "TertiaryTextContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the tertiary text container of the BitPersona."
                },
                new()
                {
                    Name = "OptionalTextContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the optional text container of the BitPersona."
                }
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "presence-enum",
            Name = "BitPersonaPresence",
            Items =
            [
                new()
                {
                    Name = "Away",
                    Value = "3",
                },
                new()
                {
                    Name = "Blocked",
                    Value = "5",
                },
                new()
                {
                    Name = "Busy",
                    Value = "6",
                },
                new()
                {
                    Name = "Dnd",
                    Value = "4",
                },
                new()
                {
                    Name = "None",
                    Value = "0",
                },
                new()
                {
                    Name = "Offline",
                    Value = "1",
                },
                new()
                {
                    Name = "Online",
                    Value = "2",
                },
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitPersonaSize",
            Items =
            [
                new()
                {
                    Name = "Size8",
                    Description = "Renders a 8px BitPersonaCoin.",
                    Value = "",
                },
                new()
                {
                    Name = "Size24",
                    Description = "Renders a 24px BitPersonaCoin.",
                    Value = "",
                },
                new()
                {
                    Name = "Size32",
                    Description = "Renders a 32px BitPersonaCoin.",
                    Value = "",
                },
                new()
                {
                    Name = "Size40",
                    Description = "Renders a 40px BitPersonaCoin.",
                    Value = "",
                },
                new()
                {
                    Name = "Size48",
                    Description = "Renders a 48px BitPersonaCoin.",
                    Value = "",
                },
                new()
                {
                    Name = "Size56",
                    Description = "Renders a 56px BitPersonaCoin.",
                    Value = "",
                },
                new()
                {
                    Name = "Size72",
                    Description = "Renders a 72px BitPersonaCoin.",
                    Value = "",
                },
                new()
                {
                    Name = "Size100",
                    Description = "Renders a 100px BitPersonaCoin.",
                    Value = "",
                },
                new()
                {
                    Name = "Size120",
                    Description = "Renders a 120px BitPersonaCoin.",
                    Value = "",
                }
            ]
        },
        new()
        {
            Id = "shape-enum",
            Name = "BitPersonaCoinShape",
            Items =
            [
                new()
                {
                    Name = "Circular",
                    Description = "Represents the traditional round shape of a coin.",
                    Value = "",
                },
                new()
                {
                    Name = "Square",
                    Description = "Represents a square-shaped coin.",
                    Value = "",
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
                    Name = "Primary",
                    Description = "Primary general color.",
                    Value = "0",
                },
                new()
                {
                    Name = "Secondary",
                    Description = "Secondary general color.",
                    Value = "1",
                },
                new()
                {
                    Name = "Tertiary",
                    Description = "Tertiary general color.",
                    Value = "2",
                },
                new()
                {
                    Name = "Info",
                    Description = "Info general color.",
                    Value = "3",
                },
                new()
                {
                    Name = "Success",
                    Description = "Success general color.",
                    Value = "4",
                },
                new()
                {
                    Name = "Warning",
                    Description = "Warning general color.",
                    Value = "5",
                },
                new()
                {
                    Name = "SevereWarning",
                    Description = "SevereWarning general color.",
                    Value = "6",
                },
                new()
                {
                    Name = "Error",
                    Description = "Error general color.",
                    Value = "7",
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
            Id = "variant-enum",
            Name = "BitVariant",
            Description = "Determines the variant of the content that controls the rendered style of the corresponding element(s).",
            Items =
            [
                new()
                {
                    Name = "Fill",
                    Description = "Fill styled variant.",
                    Value = "0",
                },
                new()
                {
                    Name = "Outline",
                    Description = "Outline styled variant.",
                    Value = "1",
                },
                new()
                {
                    Name = "Text",
                    Description = "Text styled variant.",
                    Value = "2",
                },
            ]
        },
    ];



    private int imageClickCount = 0;
    private int actionClickCount = 0;
    private bool isDetailsShown = true;

    private Dictionary<BitPersonaPresence, string> _icons = new()
    {
        {BitPersonaPresence.Offline, BitIconName.UnavailableOffline},
        {BitPersonaPresence.Online, BitIconName.SkypeCheck},
        {BitPersonaPresence.Away, BitIconName.SkypeClock},
        {BitPersonaPresence.Dnd, BitIconName.SkypeMinus},
        {BitPersonaPresence.Blocked, BitIconName.BlockedSolid},
        {BitPersonaPresence.Busy, BitIconName.Blocked2Solid}
    };
}
