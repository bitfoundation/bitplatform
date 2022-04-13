using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Persona;

public partial class BitPersonaDemo
{
    public bool IsHideDetails { get; set; } = true;
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AllowPhoneInitials",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether initials are calculated for phone numbers and number sequences.",
        },
         new ComponentParameter()
        {
            Name = "CoinSize",
            Type = "int",
            DefaultValue = " -1",
            Description = "Optional custom persona coin size in pixel.",
        },
        new ComponentParameter()
        {
            Name = "HidePersonaDetails",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to not render persona details, and just render the persona image/initials.",
        },
        new ComponentParameter()
        {
            Name = "IsOutOfOffice",
            Type = "bool",
            DefaultValue = "false",
            Description = "This flag can be used to signal the persona is out of office. This will change the way the presence icon looks for statuses that support dual-presence.",
        },
        new ComponentParameter()
        {
            Name = "ImageUrl",
            Type = "string?",
            DefaultValue = "",
            Description = "Url to the image to use, should be a square aspect ratio and big enough to fit in the image area.",
        },
        new ComponentParameter()
        {
            Name = "ImageAlt",
            Type = "string?",
            DefaultValue = "",
            Description = "Alt text for the image to use. default is empty string.",
        },
        new ComponentParameter()
        {
            Name = "ImageInitials",
            Type = "string?",
            DefaultValue = "",
            Description = "The user's initials to display in the image area when there is no image.",
        },
        new ComponentParameter()
        {
            Name = "InitialsColor",
            Type = "BitPersonaInitialsColor?",
             LinkType = LinkType.Link,
            Href = "#bitpersona-initial-color",
            DefaultValue = "0",
            Description = "The background color when the user's initials are displayed.",
        },
        new ComponentParameter()
        {
            Name = "OptionalText",
            Type = "string?",
            DefaultValue = "",
            Description = "Optional text to display, usually a custom message set. The optional text will only be shown when using size100.",
        },
        new ComponentParameter()
        {
            Name = "Presence",
            Type = "BitPersonaPresenceStatus",
            LinkType=LinkType.Link,
            Href="#precence-status",
            DefaultValue = "BitPersonaPresenceStatus.None",
            Description = "Presence of the person to display - will not display presence if undefined.",
        },
        new ComponentParameter()
        {
            Name = "PresenceTitle",
            Type = "string?",
            DefaultValue = "",
            Description = "Presence title to be shown as a tooltip on hover over the presence icon.",
        },
        new ComponentParameter()
        {
            Name = "SecondaryText",
            Type = "string?",
            DefaultValue = "",
            Description = "Secondary text to display, usually the role of the user.",
        },
        new ComponentParameter()
        {
            Name = "ShowInitialsUntilImageLoads",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true renders the initials while the image is loading. This only applies when an imageUrl is provided.",
        },
        new ComponentParameter()
        {
            Name = "ShowUnknownPersonaCoin",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, show the special coin for unknown persona. It has '?' in place of initials, with static font and background colors.",
        },
        new ComponentParameter()
        {
            Name = "Size",
            Type = "string?",
            DefaultValue = "",
            LinkType=LinkType.Link,
            Href="#bitpersona-size",
            Description = "Decides the size of the control.",
        },
        new ComponentParameter()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "",
            Description = "Primary text to display, usually the name of the person.",
        },
         new ComponentParameter()
        {
            Name = "TertiaryText",
            Type = "string?",
            DefaultValue = "",
            Description = "Tertiary text to display, usually the status of the user. The tertiary text will only be shown when using size72 or size100.",
        },
    };

    private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "precence-status",
                Title = "BitPersonaPresence enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new()
                    {
                        Name = "Away",
                        Description = "",
                        Value = "3",
                    },
                    new()
                    {
                        Name = "Blocked",
                        Description = "",
                        Value = "5",
                    },
                    new()
                    {
                        Name = "Busy",
                        Description = "",
                        Value = "6",
                    },
                    new()
                    {
                        Name = "DND",
                        Description = "",
                        Value = "4",
                    },
                    new()
                    {
                        Name = "None",
                        Description = "",
                        Value = "0",
                    },
                    new()
                    {
                        Name = "Offline",
                        Description = "",
                        Value = "1",
                    },
                    new()
                    {
                        Name = "Online",
                        Description = "",
                        Value = "2",
                    },
                }
            },
            new EnumParameter()
            {
                Id = "bitpersona-initial-color",
                Title = "BitPersonaInitialsColor enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new()
                    {
                        Name = "Black",
                        Description = "",
                        Value = "Black=11",
                    },
                    new()
                    {
                        Name = "Blue",
                        Description = "",
                        Value = "Blue=1",
                    },
                    new()
                    {
                        Name = "Burgundy",
                        Description = "",
                        Value = "Burgundy=19",
                    },
                    new()
                    {
                        Name = "CoolGray",
                        Description = "",
                        Value = "CoolGray=21",
                    },
                    new()
                    {
                        Name = "Cyan",
                        Description = "",
                        Value = "Cyan=23",
                    },
                    new()
                    {
                        Name = "DarkBlue",
                        Description = "",
                        Value = "DarkBlue=2",
                    },
                    new()
                    {
                        Name = "DarkGreen",
                        Description = "",
                        Value = "DarkGreen=6",
                    },
                    new()
                    {
                        Name = "DarkRed",
                        Description = "",
                        Value = "DarkRed=14",
                    },
                    new()
                    {
                        Name = "Gold",
                        Description = "",
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
                        Description = "",
                        Value = "Green=5",
                    },
                    new()
                    {
                        Name = "LightBlue",
                        Description = "",
                        Value = "LightBlue=0",
                    },
                    new()
                    {
                        Name = "LightGreen",
                        Description = "",
                        Value = "LightGreen=4",
                    },
                    new()
                    {
                        Name = "LightPink",
                        Description = "",
                        Value = "LightPink=7",
                    },
                    new()
                    {
                        Name = "LightRed",
                        Description = "",
                        Value = "LightRed=17",
                    },
                    new()
                    {
                        Name = "Magenta",
                        Description = "",
                        Value = "Magenta=9",
                    },
                    new()
                    {
                        Name = "Orange",
                        Description = "",
                        Value = "Orange=12",
                    },
                    new()
                    {
                        Name = "Pink",
                        Description = "",
                        Value = "Pink=8",
                    },
                    new()
                    {
                        Name = "Purple",
                        Description = "",
                        Value = "Purple=10",
                    },
                    new()
                    {
                        Name = "Red",
                        Description = "",
                        Value = "Red=13",
                    },
                    new()
                    {
                        Name = "Rust",
                        Description = "",
                        Value = "Rust=24",
                    },
                    new()
                    {
                        Name = "Teal",
                        Description = "",
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
                        Description = "",
                        Value = "Violet=16",
                    },
                    new()
                    {
                        Name = "WarmGray",
                        Description = "",
                        Value = "WarmGray=20",
                    },
                },

            },
            new EnumParameter()
            {
                Id = "bitpersona-size",
                Title = "BitPersonaPresence enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new()
                    {
                        Name = "Size8",
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

    private static string example1CSharpCode = @"
public bool IsHideDetails { get; set; } = true;";

    private static string example1HTMLCode = @"<BitCheckbox @bind-Value=""IsHideDetails"" 
             OnClick=""()=>IsHideDetails=!IsHideDetails"">Include BitPersona details</BitCheckbox>

<div class=""title"">Size 24 BitPersona</div>
<BitPersona ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png""
            Presence=@BitPersonaPresenceStatus.None
            Size=@BitPersonaSize.Size24
            HidePersonaDetails=""!IsHideDetails""
            Text=""Annie Lindqvist""
            SecondaryText=""secondry-text""></BitPersona>

<div class=""title"">Size 32 BitPersona</div>
<BitPersona ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png""
            Presence=@BitPersonaPresenceStatus.Busy
            Size=@BitPersonaSize.Size32
            HidePersonaDetails=""!IsHideDetails""
            Text=""Annie Lindqvist""
            SecondaryText=""secondry-text""></BitPersona>

<div class=""title"">Size 40 BitPersona</div>
<BitPersona ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png""
            Presence=@BitPersonaPresenceStatus.Away
            Size=@BitPersonaSize.Size40
            HidePersonaDetails=""!IsHideDetails""
            Text=""Annie Lindqvist""
            SecondaryText=""Software Engineer""></BitPersona>

<div class=""title"">Size 48 BitPersona</div>
<BitPersona ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png""
            Presence=@BitPersonaPresenceStatus.Blocked
            Size=@BitPersonaSize.Size48
            HidePersonaDetails=""!IsHideDetails""
            Text=""Annie Lindqvist""
            SecondaryText=""Software Engineer""></BitPersona>

<div class=""title"">Size 56 BitPersona</div>
<BitPersona ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png""
            Presence=@BitPersonaPresenceStatus.Online
            Size=@BitPersonaSize.Size56
            HidePersonaDetails=""!IsHideDetails""
            Text=""Annie Lindqvist""
            SecondaryText=""Software Engineer""></BitPersona>

<div class=""title"">Size 72 BitPersona</div>
<BitPersona ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png""
            Presence=@BitPersonaPresenceStatus.Busy
            Size=@BitPersonaSize.Size72
            HidePersonaDetails=""!IsHideDetails""
            Text=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""></BitPersona>

<div class=""title"">Size 100 BitPersona</div>
<BitPersona ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png""
            Presence=@BitPersonaPresenceStatus.Offline
            Size=@BitPersonaSize.Size100
            HidePersonaDetails=""!IsHideDetails""
            Text=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm""></BitPersona>

<div class=""title"">Size 120 BitPersona</div>
<BitPersona ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png""
            Presence=@BitPersonaPresenceStatus.DND
            Size=@BitPersonaSize.Size120
            HidePersonaDetails=""!IsHideDetails""
            Text=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm""></BitPersona>

<style>
   .title{
    font-weight:600;
    margin:20px 0 ;
}
</style>";
}
