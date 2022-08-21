using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Persona;

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
            DefaultValue = "-1",
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
            LinkType = LinkType.Link,
            Href = "#precence-status",
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
            LinkType = LinkType.Link,
            Href = "#bitpersona-size",
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
        new ComponentParameter()
        {
            Name = "ActionIconName",
            Type = "BitIconName",
            DefaultValue = "BitIconName.Edit",
            Description = "Icon name for the icon button of the custom action.",
        },
        new ComponentParameter()
        {
            Name = "OnActionClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Callback for the persona custom action.",
        },
        new ComponentParameter()
        {
            Name = "ActionFragment",
            Type = "RenderFragment?",
            DefaultValue = "",
            Description = "Optional Custom template for the custom action element.",
        },
        new ComponentParameter()
        {
            Name = "OnImageClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the image clicked.",
        },
        new ComponentParameter()
        {
            Name = "ImageOverlayFragment",
            Type = "RenderFragment?",
            DefaultValue = "",
            Description = "Optional Custom template for the image overlay.",
        }
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "precence-status",
            Title = "BitPersonaPresenceStatus enum",
            EnumList = new List<EnumItem>()
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
                    Name = "DND",
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
        new EnumParameter()
        {
            Id = "bitpersona-initial-color",
            Title = "BitPersonaInitialsColor enum",
            EnumList = new List<EnumItem>()
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
        new EnumParameter()
        {
            Id = "bitpersona-size",
            Title = "BitPersonaSize class",
            EnumList = new List<EnumItem>()
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

    private int _actionClickCount = 0;

    private void HandleActionClick()
    {
        _actionClickCount++;
    }

    private int _imageClickCount = 0;

    private void HandleImageClick()
    {
        _imageClickCount++;
    }

    private static readonly string example1CSharpCode = @"
public bool IsHideDetails { get; set; } = true;";

    private static readonly string example1HtmlCode = @"<BitCheckbox @bind-Value=""IsHideDetails"" 
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
            OptionalText=""Available at 4:00pm""></BitPersona>";

    private static readonly string example2CSharpCode = @"
private int _actionClickCount = 0;

private void HandleActionClick()
{
    _actionClickCount++;
}";

    private static readonly string example2HtmlCode = @"
<BitPersona ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png""
            Presence=@BitPersonaPresenceStatus.None
            Size=@BitPersonaSize.Size120
            OnActionClick=""HandleActionClick""
            ActionIconName=""BitIconName.Edit""
            HidePersonaDetails=""!IsHideDetails""
            Text=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm""></BitPersona>
<p>ActionClickCount: @_actionClickCount</p>";

    private static readonly string example3CSharpCode = @"
private int _imageClickCount = 0;

private void HandleImageClick()
{
    _imageClickCount++;
}";

    private static readonly string example3HtmlCode = @"
<BitPersona ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png""
            Presence=@BitPersonaPresenceStatus.Online
            Size=@BitPersonaSize.Size120
            OnImageClick=""HandleImageClick""
            HidePersonaDetails=""!IsHideDetails""
            Text=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm""></BitPersona>
<p>ImageClickCount: @_imageClickCount</p>";

    private static readonly string example4HtmlCode = @"
<BitPersona ImageUrl=""invalid-src""
            Size=@BitPersonaSize.Size120
            Text=""Annie Lindqvist""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm""></BitPersona>";
}
