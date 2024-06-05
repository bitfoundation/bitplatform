namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Persona;

public partial class BitPersonaDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AllowPhoneInitials",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether initials are calculated for phone numbers and number sequences.",
        },
        new()
        {
            Name = "CoinSize",
            Type = "int",
            DefaultValue = "-1",
            Description = "Optional custom persona coin size in pixel.",
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
            Name = "IsOutOfOffice",
            Type = "bool",
            DefaultValue = "false",
            Description = "This flag can be used to signal the persona is out of office. This will change the way the presence icon looks for statuses that support dual-presence.",
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
            Name = "InitialsColor",
            Type = "BitPersonaInitialsColor?",
            LinkType = LinkType.Link,
            Href = "#bitpersona-initial-color",
            DefaultValue = "null",
            Description = "The background color when the user's initials are displayed.",
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
            Name = "Presence",
            Type = "BitPersonaPresenceStatus",
            LinkType = LinkType.Link,
            Href = "#precence-status",
            DefaultValue = "BitPersonaPresenceStatus.None",
            Description = "Presence of the person to display - will not display presence if undefined.",
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
            Name = "SecondaryText",
            Type = "string?",
            DefaultValue = "null",
            Description = "Secondary text to display, usually the role of the user.",
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
            Name = "ShowUnknownPersonaCoin",
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
            Name = "PrimaryText",
            Type = "string?",
            DefaultValue = "null",
            Description = "Primary text to display, usually the name of the person.",
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
            Name = "ActionIconName",
            Type = "string",
            DefaultValue = "Edit",
            Description = "Icon name for the icon button of the custom action.",
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
            Name = "ActionFragment",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Optional Custom template for the custom action element.",
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
            Name = "ImageOverlayFragment",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Optional Custom template for the image overlay.",
        }
    };
    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "precence-status",
            Name = "BitPersonaPresenceStatus",
            Items = new()
            {
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
            }
        },
        new()
        {
            Id = "bitpersona-initial-color",
            Name = "BitPersonaInitialsColor",
            Items = new()
            {
                new()
                {
                    Name = "Black",
                    Value = "Black=11",
                },
                new()
                {
                    Name = "Blue",
                    Value = "Blue=1",
                },
                new()
                {
                    Name = "Burgundy",
                    Value = "Burgundy=19",
                },
                new()
                {
                    Name = "CoolGray",
                    Value = "CoolGray=21",
                },
                new()
                {
                    Name = "Cyan",
                    Value = "Cyan=23",
                },
                new()
                {
                    Name = "DarkBlue",
                    Value = "DarkBlue=2",
                },
                new()
                {
                    Name = "DarkGreen",
                    Value = "DarkGreen=6",
                },
                new()
                {
                    Name = "DarkRed",
                    Value = "DarkRed=14",
                },
                new()
                {
                    Name = "Gold",
                    Value = "Gold=18",
                },
                new()
                {
                    Name = "Gray",
                    Description = "gray is a color that can result in offensive Bitpersona coins with some initials combinations, so it can only be set with overrides.",
                    Value = "Gray=22",
                },
                new()
                {
                    Name = "Green",
                    Value = "Green=5",
                },
                new()
                {
                    Name = "LightBlue",
                    Value = "LightBlue=0",
                },
                new()
                {
                    Name = "LightGreen",
                    Value = "LightGreen=4",
                },
                new()
                {
                    Name = "LightPink",
                    Value = "LightPink=7",
                },
                new()
                {
                    Name = "LightRed",
                    Value = "LightRed=17",
                },
                new()
                {
                    Name = "Magenta",
                    Value = "Magenta=9",
                },
                new()
                {
                    Name = "Orange",
                    Value = "Orange=12",
                },
                new()
                {
                    Name = "Pink",
                    Value = "Pink=8",
                },
                new()
                {
                    Name = "Purple",
                    Value = "Purple=10",
                },
                new()
                {
                    Name = "Red",
                    Value = "Red=13",
                },
                new()
                {
                    Name = "Rust",
                    Value = "Rust=24",
                },
                new()
                {
                    Name = "Teal",
                    Value = "Teal=3",
                },
                new()
                {
                    Name = "Transparent",
                    Description = "Transparent is not intended to be used with typical initials due to accessibility issues. Its primary use is for overflow buttons, so it is considered a reserved color and can only be set with overrides.",
                    Value = "Transparent=15",
                },
                new()
                {
                    Name = "Violet",
                    Value = "Violet=16",
                },
                new()
                {
                    Name = "WarmGray",
                    Value = "WarmGray=20",
                },
            },
        },
        new()
        {
            Id = "bitpersona-size",
            Name = "BitPersonaSize",
            Items = new()
            {
                new()
                {
                    Name = "Size20",
                    Description = "Renders a 20px BitPersonaCoin.",
                    Value = "20px",
                },
                new()
                {
                    Name = "Size24",
                    Description = "Renders a 24px BitPersonaCoin.",
                    Value = "24px",
                },
                new()
                {
                    Name = "Size32",
                    Description = "Renders a 32px BitPersonaCoin.",
                    Value = "32px",
                },
                new()
                {
                    Name = "Size40",
                    Description = "Renders a 40px BitPersonaCoin.",
                    Value = "40px",
                },
                new()
                {
                    Name = "Size48",
                    Description = "Renders a 48px BitPersonaCoin.",
                    Value = "48px",
                },
                new()
                {
                    Name = "Size56",
                    Description = "Renders a 56px BitPersonaCoin.",
                    Value = "56px",
                },
                new()
                {
                    Name = "Size72",
                    Description = "Renders a 72px BitPersonaCoin.",
                    Value = "72px",
                },
                new()
                {
                    Name = "Size100",
                    Description = "Renders a 100px BitPersonaCoin.",
                    Value = "100px",
                },
                new()
                {
                    Name = "Size120",
                    Description = "Renders a 120px BitPersonaCoin.",
                    Value = "120px",
                }
            }
        },
    };



    private readonly string example1RazorCode = @"
<BitCheckbox @bind-Value=""IsDetailsHidden"" 
             OnClick=""() => IsDetailsHidden = !IsDetailsHidden"">Include BitPersona details</BitCheckbox>

<div>Size 24 BitPersona</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            Size=@BitPersonaSize.Size24
            HidePersonaDetails=""!IsDetailsHidden""
            Presence=@BitPersonaPresenceStatus.None
            ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />

<div>Size 32 BitPersona</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            Size=@BitPersonaSize.Size32
            HidePersonaDetails=""!IsDetailsHidden""
            Presence=@BitPersonaPresenceStatus.Busy
            ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />

<div>Size 40 BitPersona</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            Size=@BitPersonaSize.Size40
            HidePersonaDetails=""!IsDetailsHidden""
            Presence=@BitPersonaPresenceStatus.Away
            ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />

<div>Size 48 BitPersona</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            Size=@BitPersonaSize.Size48
            HidePersonaDetails=""!IsDetailsHidden""
            Presence=@BitPersonaPresenceStatus.Blocked
            ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />

<div>Size 56 BitPersona</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            Size=@BitPersonaSize.Size56
            HidePersonaDetails=""!IsDetailsHidden""
            Presence=@BitPersonaPresenceStatus.Online
            ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />

<div>Size 72 BitPersona</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            Size=@BitPersonaSize.Size72
            HidePersonaDetails=""!IsDetailsHidden""
            Presence=@BitPersonaPresenceStatus.Busy
            ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />

<div>Size 100 BitPersona</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm""
            Size=@BitPersonaSize.Size100
            HidePersonaDetails=""!IsDetailsHidden""
            Presence=@BitPersonaPresenceStatus.Offline
            ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />

<div>Size 120 BitPersona</div>
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm"" /
            Size=@BitPersonaSize.Size120
            HidePersonaDetails=""!IsDetailsHidden""
            Presence=@BitPersonaPresenceStatus.Dnd
            ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" >";
    private readonly string example1CsharpCode = @"
public bool IsDetailsHidden { get; set; } = true;";

    private readonly string example2RazorCode = @"
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm"" 
            Size=@BitPersonaSize.Size120
            HidePersonaDetails=""!IsDetailsHidden""
            Presence=@BitPersonaPresenceStatus.None
            ActionIconName=""@BitIconName.Edit""
            OnActionClick=""() => ActionClickCount++""
            ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />
<p>ActionClickCount: @ActionClickCount</p>";
    private readonly string example2CsharpCode = @"
private int ActionClickCount = 0;";

    private readonly string example3RazorCode = @"
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm"" 
            Size=@BitPersonaSize.Size120
            Presence=@BitPersonaPresenceStatus.Online
            HidePersonaDetails=""!IsDetailsHidden""
            OnImageClick=""() => ImageClickCount++""
            ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />
<p>ImageClickCount: @ImageClickCount</p>";
    private readonly string example3CsharpCode = @"
private int ImageClickCount = 0;";

    private readonly string example4RazorCode = @"
<BitPersona PrimaryText=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm""
            Size=@BitPersonaSize.Size120
            ImageUrl=""invalid-src"" />";

    private readonly string example5RazorCode = @"
<BitPersona Dir=""BitDir.Rtl""
            PrimaryText=""صالح یوسف نژاد""
            SecondaryText=""مهندس نرم افزار""
            Size=""@BitPersonaSize.Size56"" />";
}
