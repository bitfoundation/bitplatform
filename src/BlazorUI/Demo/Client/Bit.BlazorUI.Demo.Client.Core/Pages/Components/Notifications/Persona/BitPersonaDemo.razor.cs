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
            Name = "ActionIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Icon for the icon button of the custom action using BitIconInfo. Takes precedence over ActionIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
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
            Name = "Active",
            Type = "bool",
            DefaultValue = "false",
            Description = "Marks the persona as active, which decorates its coin according to ActiveAppearance.",
        },
        new()
        {
            Name = "ActiveAppearance",
            Type = "BitPersonaActiveAppearance?",
            DefaultValue = "null",
            Description = "How the coin is decorated while Active is true. The default is a ring.",
            LinkType = LinkType.Link,
            Href = "#active-appearance-enum",
        },
        new()
        {
            Name = "AutoCoinColor",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, automatically generates a stable coin background color derived from CoinColorSeed, ImageInitials or PrimaryText. Only takes effect when CoinColor is not explicitly set.",
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
            Name = "CoinColorSeed",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text AutoCoinColor hashes to pick a coin color, so the color follows the identity of the person rather than the name being displayed. Falls back to ImageInitials and then PrimaryText.",
        },
        new()
        {
            Name = "CoinIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon rendered inside the coin in place of the initials. Takes precedence over CoinIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "CoinIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon rendered inside the coin in place of the initials.",
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
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the persona in full width of its container element.",
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
            Name = "ImageAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "new Dictionary<string, object>()",
            Description = "Captures additional HTML attributes to be applied to the rendered img element of the coin (crossorigin, referrerpolicy, decoding, fetchpriority, draggable, ...).",
        },
        new()
        {
            Name = "ImageInitials",
            Type = "string?",
            DefaultValue = "null",
            Description = "The user's initials to display in the image area when there is no image. When it is not set, the initials are derived from PrimaryText.",
        },
        new()
        {
            Name = "ImageLoading",
            Type = "BitImageLoading?",
            DefaultValue = "null",
            Description = "Specifies the loading behavior of the image. Maps to the HTML loading attribute (e.g., \"lazy\" or \"eager\").",
            LinkType = LinkType.Link,
            Href = "#image-loading"
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
            Type = "string",
            DefaultValue = "Edit image",
            Description = "The text of the image overlay.",
        },
        new()
        {
            Name = "ImageSizes",
            Type = "string?",
            DefaultValue = "null",
            Description = "The set of media conditions that tells the browser which of the ImageSrcSet candidates to pick. Maps to the HTML img sizes attribute.",
        },
        new()
        {
            Name = "ImageSrcSet",
            Type = "string?",
            DefaultValue = "null",
            Description = "A set of image source URLs for different display densities or sizes. Maps to the HTML img srcset attribute.",
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
            Name = "OnImageError",
            Type = "EventCallback<ErrorEventArgs>",
            DefaultValue = "null",
            Description = "Callback for when the image fails to load.",
        },
        new()
        {
            Name = "OnImageLoad",
            Type = "EventCallback<ProgressEventArgs>",
            DefaultValue = "null",
            Description = "Callback for when the image successfully loads.",
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
            Type = "Dictionary<BitPersonaPresence, BitIconInfo>?",
            DefaultValue = "null",
            Description = "The icons to be used for the presence status with BitIconInfo. Takes precedence over PresenceIconNames when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "PresenceIconNames",
            Type = "Dictionary<BitPersonaPresence, string>?",
            DefaultValue = "null",
            Description = "The icon names to be used for the presence status.",
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
            Name = "PresenceTitles",
            Type = "Dictionary<BitPersonaPresence, string>?",
            DefaultValue = "null",
            Description = "The titles to be shown as a tooltip on hover over the presence dot, one per status. The matching entry also becomes the accessible name of the dot and takes precedence over PresenceTitle.",
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
            Description = "If true, show the special coin for unknown persona. It shows an icon in place of the initials, and takes precedence over the image and the initials.",
        },
        new()
        {
            Name = "UnknownIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Icon for the unknown persona coin using BitIconInfo. Takes precedence over UnknownIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "UnknownIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Icon name for the unknown persona coin.",
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
            Type = "BitPersonaSize",
            DefaultValue = "BitPersonaSize.Size48",
            LinkType = LinkType.Link,
            Href = "#size-enum",
            Description = "Decides the size of the control.",
        },
        new()
        {
            Name = "Squared",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, renders the coin with a square shape instead of the default circular shape.",
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
                    Description = "Custom CSS classes/styles for the presence dot of the BitPersona at Size8. Kept for backward compatibility - Presence is applied to the dot at every size and is what new code should use."
                },
                new()
                {
                    Name = "ImageContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the image container of the BitPersona."
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
                    Name = "CoinIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the coin icon of the BitPersona, which is the icon shown inside the coin in place of the initials."
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
                    Description = "Custom CSS classes/styles for the presence dot of the BitPersona."
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
                   Description = @"Gets or sets the name of the icon. 
                                   For external icons, this can be the full CSS class name if ""BaseClass"" and ""Prefix"" are empty."
               },
               new()
               {
                   Name = "BaseClass",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = @"Gets or sets the base CSS class for the icon.
                                   For external icon libraries like FontAwesome, you might set this to ""fa"" or leave empty."
               },
               new()
               {
                   Name = "Prefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = @"Gets or sets the CSS class prefix used before the icon name.
                                   For external icon libraries, you might set this to ""fa-"" or leave empty."
               },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "presence-enum",
            Name = "BitPersonaPresence",
            Description = "The availability of the person a BitPersona represents, shown as a dot on the coin.",
            Items =
            [
                new()
                {
                    Name = "None",
                    Description = "No presence is known or worth showing, so no dot is rendered at all.",
                    Value = "0",
                },
                new()
                {
                    Name = "Offline",
                    Description = "The person is signed out.",
                    Value = "1",
                },
                new()
                {
                    Name = "Online",
                    Description = "The person is signed in and available.",
                    Value = "2",
                },
                new()
                {
                    Name = "Away",
                    Description = "The person is signed in but idle.",
                    Value = "3",
                },
                new()
                {
                    Name = "Dnd",
                    Description = "The person has asked not to be interrupted.",
                    Value = "4",
                },
                new()
                {
                    Name = "Blocked",
                    Description = "The person cannot be reached from here.",
                    Value = "5",
                },
                new()
                {
                    Name = "Busy",
                    Description = "The person is signed in and occupied.",
                    Value = "6",
                },
                new()
                {
                    Name = "OutOfOffice",
                    Description = "The person is away from work for an extended period.",
                    Value = "7",
                },
                new()
                {
                    Name = "Unknown",
                    Description = "The presence of the person could not be determined.",
                    Value = "8",
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
                    Description = "A presence dot and the primary text only, with no coin at all.",
                    Value = "0",
                },
                new()
                {
                    Name = "Size24",
                    Description = "A 24px coin with the primary text.",
                    Value = "1",
                },
                new()
                {
                    Name = "Size32",
                    Description = "A 32px coin with the primary text.",
                    Value = "2",
                },
                new()
                {
                    Name = "Size40",
                    Description = "A 40px coin with the primary and secondary texts.",
                    Value = "3",
                },
                new()
                {
                    Name = "Size48",
                    Description = "A 48px coin with the primary and secondary texts.",
                    Value = "4",
                },
                new()
                {
                    Name = "Size56",
                    Description = "A 56px coin with the primary and secondary texts.",
                    Value = "5",
                },
                new()
                {
                    Name = "Size72",
                    Description = "A 72px coin with the primary, secondary and tertiary texts.",
                    Value = "6",
                },
                new()
                {
                    Name = "Size100",
                    Description = "A 100px coin with all four texts.",
                    Value = "7",
                },
                new()
                {
                    Name = "Size120",
                    Description = "A 120px coin with all four texts.",
                    Value = "8",
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
            Id = "active-appearance-enum",
            Name = "BitPersonaActiveAppearance",
            Description = "Determines how the coin of a BitPersona is decorated while the persona is active.",
            Items =
            [
                new()
                {
                    Name = "Ring",
                    Description = "Draws a ring around the coin in the coin color, separated from it by a gap in the page background color.",
                    Value = "0",
                },
                new()
                {
                    Name = "Shadow",
                    Description = "Lifts the coin with an elevation shadow.",
                    Value = "1",
                },
                new()
                {
                    Name = "RingShadow",
                    Description = "Combines the ring and the elevation shadow.",
                    Value = "2",
                },
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
        new()
        {
            Id = "image-loading",
            Name = "BitImageLoading",
            Description = "Represents the img loading attribute values explained here: https://developer.mozilla.org/en-US/docs/Web/API/HTMLImageElement/loading",
            Items =
            [
                new()
                {
                    Name= "Eager",
                    Description="The default behavior, eager tells the browser to load the image as soon as the img element is processed.",
                    Value="0",
                },
                new()
                {
                    Name= "Lazy",
                    Description="Tells the user agent to hold off on loading the image until the browser estimates that it will be needed imminently.",
                    Value="1",
                }
            ]
        },
    ];



    private int imageClickCount = 0;
    private int actionClickCount = 0;
    private int imageLoadCount = 0;
    private int imageErrorCount = 0;
    private bool isDetailsShown = true;

    private readonly Dictionary<BitPersonaPresence, BitIconInfo> _icons = new()
    {
        { BitPersonaPresence.Offline, BitIconInfo.Bi("wifi-off") },
        { BitPersonaPresence.Online, BitIconInfo.Bi("check-circle-fill") },
        { BitPersonaPresence.Away, BitIconInfo.Bi("clock-fill") },
        { BitPersonaPresence.Dnd, BitIconInfo.Bi("dash-circle-fill") },
        { BitPersonaPresence.Blocked, BitIconInfo.Bi("ban") },
        { BitPersonaPresence.Busy, BitIconInfo.Bi("exclamation-circle-fill") },
    };

    private readonly Dictionary<BitPersonaPresence, string> _iconNames = new()
    {
        { BitPersonaPresence.Offline, BitIconName.UnavailableOffline },
        { BitPersonaPresence.Online, BitIconName.SkypeCheck },
        { BitPersonaPresence.Away, BitIconName.SkypeClock },
        { BitPersonaPresence.Dnd, BitIconName.SkypeMinus },
        { BitPersonaPresence.Blocked, BitIconName.BlockedSolid },
        { BitPersonaPresence.Busy, BitIconName.Blocked2Solid },
        { BitPersonaPresence.OutOfOffice, BitIconName.Airplane },
        { BitPersonaPresence.Unknown, BitIconName.StatusCircleQuestionMark },
    };

    private readonly Dictionary<BitPersonaPresence, string> _presenceTitles = new()
    {
        { BitPersonaPresence.Offline, "Signed out" },
        { BitPersonaPresence.Online, "Available" },
        { BitPersonaPresence.Away, "Be right back" },
        { BitPersonaPresence.Dnd, "Do not disturb" },
        { BitPersonaPresence.Blocked, "Blocked" },
        { BitPersonaPresence.Busy, "In a call" },
        { BitPersonaPresence.OutOfOffice, "Out of office" },
        { BitPersonaPresence.Unknown, "Presence unknown" },
    };
}
