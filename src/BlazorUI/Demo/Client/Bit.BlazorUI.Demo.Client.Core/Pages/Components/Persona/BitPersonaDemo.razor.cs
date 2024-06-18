namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Persona;

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
            Name = "Color",
            Type = "string?",
            DefaultValue = "null",
            Description = "The background color when the user's initials are displayed.",
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
            Href = "#precence-status",
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
            Name = "Size",
            Type = "string?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#bitpersona-size",
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
            Id = "precence-status",
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
            Id = "bitpersona-size",
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

    private readonly string example1RazorCode = @"
<BitPersona PrimaryText=""Saleh Khafan"" Size=""BitPersonaSize.Size72"" />

<BitPersona PrimaryText=""Saleh Khafan""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Unknown""
            SecondaryText=""Developer""
            Size=""BitPersonaSize.Size72""
            Unknown />";

    private readonly string example2RazorCode = @"
<BitPersona PrimaryText=""Saleh Khafan""
            SecondaryText=""Developer""
            Size=""BitPersonaSize.Size72""
            Color=""#038387"" />

<BitPersona PrimaryText=""Annie Lindqvist""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png""
            OnImageClick=""() => {}""
            Color=""#750b1c"" />";

    private readonly string example3RazorCode = @"
<BitCheckbox @bind-Value=""isDetailsShown"" Label=""Include BitPersona details"" />

<div>Size 8 Persona</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Secondary""
            Size=""BitPersonaSize.Size8""
            HidePersonaDetails=""!isDetailsShown""
            Presence=""BitPersonaPresence.Online""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />

<div>Size 24 Persona</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Secondary""
            Size=""BitPersonaSize.Size24""
            HidePersonaDetails=""!isDetailsShown""
            Presence=""BitPersonaPresence.None""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />

<div>Size 32 Persona (Busy)</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Secondary""
            Size=@BitPersonaSize.Size32
            HidePersonaDetails=""!isDetailsShown""
            Presence=""BitPersonaPresence.Busy""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />

<div>Size 40 Persona (Away)</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            Size=""BitPersonaSize.Size40""
            HidePersonaDetails=""!isDetailsShown""
            Presence=""BitPersonaPresence.Away""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />

<div>Size 48 Persona (Blocked)</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            Size=""BitPersonaSize.Size48""
            HidePersonaDetails=""!isDetailsShown""
            Presence=""BitPersonaPresence.Blocked""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />

<div>Size 56 Persona (Online)</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            Size=""BitPersonaSize.Size56""
            HidePersonaDetails=""!isDetailsShown""
            Presence=""BitPersonaPresence.Online""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />

<div>Size 72 Persona (Busy)</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            Size=""BitPersonaSize.Size72""
            HidePersonaDetails=""!isDetailsShown""
            Presence=""BitPersonaPresence.Busy""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />

<div>Size 100 Persona (Offline)</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            TertiaryText=""Off""
            OptionalText=""Available at 4:00pm""
            Size=""BitPersonaSize.Size100""
            HidePersonaDetails=""!isDetailsShown""
            Presence=""BitPersonaPresence.Offline""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />

<div>Size 120 Persona (Do Not Disturb)</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm""
            Size=""BitPersonaSize.Size120""
            HidePersonaDetails=""!isDetailsShown""
            Presence=""BitPersonaPresence.Dnd""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />

<div>Size 150 Persona (Do Not Disturb)</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm""
            Size=""BitPersonaSize.Size120""
            CoinSize=""150""
            HidePersonaDetails=""!isDetailsShown""
            Presence=""BitPersonaPresence.Dnd""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />";
    private readonly string example3CsharpCode = @"
private bool isDetailsShown = true;";

    private readonly string example4RazorCode = @"
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm""
            Size=""BitPersonaSize.Size120""
            Presence=""BitPersonaPresence.None""
            OnActionClick=""() => actionClickCount++""
            ActionIconName=""@BitIconName.CloudUpload""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />
<p>Action Click Count: @actionClickCount</p>

<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm""
            Size=""BitPersonaSize.Size120""
            Presence=""BitPersonaPresence.Online""
            OnImageClick=""() => imageClickCount++""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />
<p>Image Click Count: @imageClickCount</p>";
    private readonly string example4CsharpCode = @"
private int imageClickCount = 0;
private int actionClickCount = 0;";

    private readonly string example5RazorCode = @"
<BitPersona PrimaryText=""Saleh Khafan""
            Size=""BitPersonaSize.Size72""
            ShowInitialsUntilImageLoads
            ImageUrl=""invalid-src"" />

<BitPersona Size=""BitPersonaSize.Size72"" PrimaryText=""Saleh Xafan"" />

<BitPersona Size=""BitPersonaSize.Size72"" PrimaryText=""Saleh Khafan"" ImageInitials=""S"" />";

    private readonly string example6RazorCode = @"
<div>None</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.None""
            PresenceIcons=""_icons""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />

<div>Offline</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Offline""
            PresenceIcons=""_icons""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />

<div>Online</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Online""
            PresenceIcons=""_icons""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />

<div>Away</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Away""
            PresenceIcons=""_icons""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />

<div>Do not Disturb (Dnd)</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Dnd""
            PresenceIcons=""_icons""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />

<div>Blocked</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Blocked""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />

<div>Busy</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Busy""
            PresenceIcons=""_icons""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />";
    private readonly string example6CsharpCode = @"
private Dictionary<BitPersonaPresence, string> _icons = new()
{
    {BitPersonaPresence.Offline, BitIconName.UnavailableOffline},
    {BitPersonaPresence.Online, BitIconName.SkypeCheck},
    {BitPersonaPresence.Away, BitIconName.SkypeClock},
    {BitPersonaPresence.Dnd, BitIconName.SkypeMinus},
    {BitPersonaPresence.Blocked, BitIconName.BlockedSolid},
    {BitPersonaPresence.Busy, BitIconName.Blocked2Solid}
};";

    private readonly string example7RazorCode = @"
<style>
    .custom-ico {
        font-size: 14px;
        margin-right: 5px;
    }

    .custom-coin {
        display: block;
        border-radius: 20px;
    }
</style>

<BitPersona Size=""BitPersonaSize.Size100"" ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" OnImageClick=""() => {}"">
    <PrimaryTextTemplate>
        <BitIcon IconName=""@BitIconName.Contact"" Class=""custom-ico"" />
        Annie Lindqvist
    </PrimaryTextTemplate>
    <SecondaryTextTemplate>
        <BitIcon IconName=""@BitIconName.Suitcase"" Class=""custom-ico"" />
        Software Engineer
    </SecondaryTextTemplate>
    <TertiaryTextTemplate>
        <BitIcon IconName=""@BitIconName.JoinOnlineMeeting"" Class=""custom-ico"" />
        In a meeting
    </TertiaryTextTemplate>
    <OptionalTextTemplate>
        <BitIcon IconName=""@BitIconName.Clock"" Class=""custom-ico"" />
        Available at 7:00pm
    </OptionalTextTemplate>
    <ImageOverlayTemplate>
        <BitIcon IconName=""@BitIconName.Edit"" Class=""custom-ico"" />
        Edit image
    </ImageOverlayTemplate>
</BitPersona>


<BitPersona Size=""BitPersonaSize.Size100"" PrimaryText=""Annie Lindqvist"" SecondaryText=""Software Engineer"" Presence=""BitPersonaPresence.Online"">
    <CoinTemplate>
        <img src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" width=""100px"" height=""100px"" class=""custom-coin"" />
    </CoinTemplate>
</BitPersona>";

    private readonly string example8RazorCode = @"
<style>
    .custom-class {
        padding: 1rem;
        box-shadow: #3d3226 0 0 1rem;
        border-radius: 1rem;
    }

    .custom-img-container {
        color: #ff6a00;
        background-color: #f2cd01;
    }

    .custom-primary-text {
        color: #b6ff00;
        font-weight: bold;
        font-style: italic;
    }
</style>

<BitPersona PrimaryText=""Saleh Khafan""
            Size=""BitPersonaSize.Size72""
            Style=""padding: 1rem; background: gray;border-radius: 1rem;"" />

<BitPersona PrimaryText=""Saleh Khafan""
            Size=""BitPersonaSize.Size72""
            Class=""custom-class"" />


<BitPersona PrimaryText=""Saleh Khafan""
            Size=""BitPersonaSize.Size72""
            Styles=""@(new() { ImageContainer = ""color: #b6ff00; background-color: #00ff90;"",
                              PrimaryTextContainer = ""color: #ea1919; font-weight: bold; font-style: italic;"" })"" />

<BitPersona PrimaryText=""Saleh Khafan""
            Size=""BitPersonaSize.Size72""
            Classes=""@(new() { ImageContainer = ""custom-img-container"",
                               PrimaryTextContainer = ""custom-primary-text"" })"" />";

    private readonly string example9RazorCode = @"
<BitPersona Dir=""BitDir.Rtl""
            PrimaryText=""صالح یوسف نژاد""
            SecondaryText=""مهندس نرم افزار""
            Size=""@BitPersonaSize.Size56"" />";
}
