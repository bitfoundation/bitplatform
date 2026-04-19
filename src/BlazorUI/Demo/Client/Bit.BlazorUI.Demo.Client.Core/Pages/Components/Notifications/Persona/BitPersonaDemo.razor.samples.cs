namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.Persona;

public partial class BitPersonaDemo
{
    private readonly string example1RazorCode = @"
<BitPersona PrimaryText=""Saleh Khafan"" Size=""BitPersonaSize.Size72"" />

<BitPersona PrimaryText=""Saleh Khafan""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Unknown""
            SecondaryText=""Developer""
            Size=""BitPersonaSize.Size72""
            Unknown />";

    private readonly string example2RazorCode = @"
<BitPersona PrimaryText=""Saleh Xafan""
            SecondaryText=""Developer""
            Size=""BitPersonaSize.Size72""
            CoinVariant=""BitVariant.Fill"" />

<BitPersona PrimaryText=""Saleh Xafan""
            SecondaryText=""Developer""
            Size=""BitPersonaSize.Size72""
            CoinVariant=""BitVariant.Outline"" />

<BitPersona PrimaryText=""Saleh Xafan""
            SecondaryText=""Developer""
            Size=""BitPersonaSize.Size72""
            CoinVariant=""BitVariant.Text"" />";

    private readonly string example3RazorCode = @"
<BitPersona PrimaryText=""Saleh Xafan""
            SecondaryText=""Circle""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />

<BitPersona Squared
            PrimaryText=""Saleh Xafan""
            SecondaryText=""Square""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />";

    private readonly string example4RazorCode = @"
<div>None</div>
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.None""
            PresenceIconNames=""_iconNames""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/images/persona/persona-female.png"" />

<div>Offline</div>
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Offline""
            PresenceIconNames=""_iconNames""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/images/persona/persona-female.png"" />

<div>Online</div>
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Online""
            PresenceIconNames=""_iconNames""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/images/persona/persona-female.png"" />

<div>Away</div>
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Away""
            PresenceIconNames=""_iconNames""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/images/persona/persona-female.png"" />

<div>Do not Disturb (Dnd)</div>
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Dnd""
            PresenceIconNames=""_iconNames""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/images/persona/persona-female.png"" />

<div>Blocked</div>
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Blocked""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/images/persona/persona-female.png"" />

<div>Busy</div>
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Busy""
            PresenceIconNames=""_iconNames""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/images/persona/persona-female.png"" />";
    private readonly string example4CsharpCode = @"
private readonly Dictionary<BitPersonaPresence, string> _iconNames = new()
{
    {BitPersonaPresence.Offline, BitIconName.UnavailableOffline},
    {BitPersonaPresence.Online, BitIconName.SkypeCheck},
    {BitPersonaPresence.Away, BitIconName.SkypeClock},
    {BitPersonaPresence.Dnd, BitIconName.SkypeMinus},
    {BitPersonaPresence.Blocked, BitIconName.BlockedSolid},
    {BitPersonaPresence.Busy, BitIconName.Blocked2Solid}
};";

    private readonly string example5RazorCode = @"
<BitCheckbox @bind-Value=""isDetailsShown"" Label=""Include BitPersona details"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Secondary""
            Size=""BitPersonaSize.Size8""
            HidePersonaDetails=""!isDetailsShown""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Secondary""
            Size=""BitPersonaSize.Size24""
            HidePersonaDetails=""!isDetailsShown""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Secondary""
            Size=@BitPersonaSize.Size32
            HidePersonaDetails=""!isDetailsShown""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Size=""BitPersonaSize.Size40""
            HidePersonaDetails=""!isDetailsShown""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Size=""BitPersonaSize.Size48""
            HidePersonaDetails=""!isDetailsShown""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Size=""BitPersonaSize.Size56""
            HidePersonaDetails=""!isDetailsShown""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            Size=""BitPersonaSize.Size72""
            HidePersonaDetails=""!isDetailsShown""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            TertiaryText=""Off""
            OptionalText=""Available at 4:00pm""
            Size=""BitPersonaSize.Size100""
            HidePersonaDetails=""!isDetailsShown""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm""
            Size=""BitPersonaSize.Size120""
            HidePersonaDetails=""!isDetailsShown""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona CoinSize=""150""
            PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm""
            Size=""BitPersonaSize.Size120""
            HidePersonaDetails=""!isDetailsShown""
            ImageUrl=""/images/persona/persona-female.png"" />";
    private readonly string example5CsharpCode = @"
private bool isDetailsShown = true;";

    private readonly string example6RazorCode = @"
<BitPersona Reversed
            PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Online""
            PresenceIconNames=""_iconNames""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />";
    private readonly string example6CsharpCode = @"
private readonly Dictionary<BitPersonaPresence, string> _iconNames = new()
{
    {BitPersonaPresence.Offline, BitIconName.UnavailableOffline},
    {BitPersonaPresence.Online, BitIconName.SkypeCheck},
    {BitPersonaPresence.Away, BitIconName.SkypeClock},
    {BitPersonaPresence.Dnd, BitIconName.SkypeMinus},
    {BitPersonaPresence.Blocked, BitIconName.BlockedSolid},
    {BitPersonaPresence.Busy, BitIconName.Blocked2Solid}
};";

    private readonly string example7RazorCode = @"
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm""
            Size=""BitPersonaSize.Size120""
            Presence=""BitPersonaPresence.None""
            OnActionClick=""() => actionClickCount++""
            ActionIconName=""@BitIconName.CloudUpload""
            ImageUrl=""/images/persona/persona-female.png"" />
<p>Action Click Count: @actionClickCount</p>

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm""
            Size=""BitPersonaSize.Size120""
            Presence=""BitPersonaPresence.Online""
            OnImageClick=""() => imageClickCount++""
            ImageUrl=""/images/persona/persona-female.png"" />
<p>Image Click Count: @imageClickCount</p>";
    private readonly string example7CsharpCode = @"
private int imageClickCount = 0;
private int actionClickCount = 0;";

    private readonly string example8RazorCode = @"
<BitPersona PrimaryText=""Saleh Khafan""
            Size=""BitPersonaSize.Size72""
            ShowInitialsUntilImageLoads
            ImageUrl=""invalid-src"" />

<BitPersona Size=""BitPersonaSize.Size72"" PrimaryText=""Saleh Xafan"" />

<BitPersona Size=""BitPersonaSize.Size72"" PrimaryText=""Saleh Khafan"" ImageInitials=""S"" />";

    private readonly string example9RazorCode = @"
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

<BitPersona Size=""BitPersonaSize.Size100"" ImageUrl=""/images/persona/persona-female.png"" OnImageClick=""() => {}"">
    <PrimaryTextTemplate>
        <BitIcon IconName=""@BitIconName.Contact"" Class=""custom-ico"" />
        Xafan Salina
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


<BitPersona Size=""BitPersonaSize.Size100"" PrimaryText=""Xafan Salina"" SecondaryText=""Software Engineer"" Presence=""BitPersonaPresence.Online"" CoinVariant=""BitVariant.Text"">
    <CoinTemplate>
        <img src=""/images/persona/persona-female.png"" width=""100px"" height=""100px"" class=""custom-coin"" />
    </CoinTemplate>
</BitPersona>";

    private readonly string example10RazorCode = @"
<BitPersona PrimaryText=""Primary"" CoinColor=""BitColor.Primary"" />
<BitPersona PrimaryText=""Secondary"" CoinColor=""BitColor.Secondary"" />
<BitPersona PrimaryText=""Tertiary"" CoinColor=""BitColor.Tertiary"" />
<BitPersona PrimaryText=""Info"" SecondaryText=""(default)"" CoinColor=""BitColor.Info"" />
<BitPersona PrimaryText=""Success"" CoinColor=""BitColor.Success"" />
<BitPersona PrimaryText=""Warning"" CoinColor=""BitColor.Warning"" />
<BitPersona PrimaryText=""SevereWarning"" CoinColor=""BitColor.SevereWarning"" />
<BitPersona PrimaryText=""Error"" CoinColor=""BitColor.Error"" />

<BitPersona PrimaryText=""PrimaryBackground"" CoinColor=""BitColor.PrimaryBackground"" />
<BitPersona PrimaryText=""SecondaryBackground"" CoinColor=""BitColor.SecondaryBackground"" />
<BitPersona PrimaryText=""TertiaryBackground"" CoinColor=""BitColor.TertiaryBackground"" />

<BitPersona PrimaryText=""PrimaryForeground"" CoinColor=""BitColor.PrimaryForeground"" />
<BitPersona PrimaryText=""SecondaryForeground"" CoinColor=""BitColor.SecondaryForeground"" />
<BitPersona PrimaryText=""TertiaryForeground"" CoinColor=""BitColor.TertiaryForeground"" />
<BitPersona PrimaryText=""PrimaryBorder"" CoinColor=""BitColor.PrimaryBorder"" />
<BitPersona PrimaryText=""SecondaryBorder"" CoinColor=""BitColor.SecondaryBorder"" />
<BitPersona PrimaryText=""TertiaryBorder"" CoinColor=""BitColor.TertiaryBorder"" />";

    private readonly string example11RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm""
            Size=""BitPersonaSize.Size120""
            Presence=""BitPersonaPresence.None""
            OnActionClick=""() => actionClickCount++""
            ActionIcon=""@BitIconInfo.Fa(""solid camera"")""
            ImageUrl=""/images/persona/persona-female.png"" />
<p>Action Click Count: @actionClickCount</p>


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Online""
            PresenceIcons=""_icons""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Busy""
            PresenceIcons=""_icons""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Away""
            PresenceIcons=""_icons""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Dnd""
            PresenceIcons=""_icons""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Offline""
            PresenceIcons=""_icons""
            Size=""BitPersonaSize.Size120""
            ImageUrl=""/images/persona/persona-female.png"" />";
    private readonly string example11CsharpCode = @"
private readonly Dictionary<BitPersonaPresence, BitIconInfo> _icons = new()
{
    {BitPersonaPresence.Offline, BitIconInfo.Bi(""wifi-off"")},
    {BitPersonaPresence.Online, BitIconInfo.Bi(""check-circle-fill"")},
    {BitPersonaPresence.Away, BitIconInfo.Bi(""clock-fill"")},
    {BitPersonaPresence.Dnd, BitIconInfo.Bi(""dash-circle-fill"")},
    {BitPersonaPresence.Blocked, BitIconInfo.Bi(""ban"")},
    {BitPersonaPresence.Busy, BitIconInfo.Bi(""exclamation-circle-fill"")}
};";

    private readonly string example12RazorCode = @"
<BitPersona AutoCoinColor
            PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Size=""BitPersonaSize.Size72"" />

<BitPersona AutoCoinColor
            PrimaryText=""Saleh Khafan""
            SecondaryText=""Developer""
            Size=""BitPersonaSize.Size72"" />

<BitPersona AutoCoinColor
            PrimaryText=""Ted Randall""
            SecondaryText=""Designer""
            Size=""BitPersonaSize.Size72"" />

<BitPersona AutoCoinColor
            PrimaryText=""Carlos Slattery""
            SecondaryText=""Manager""
            Size=""BitPersonaSize.Size72"" />

<BitPersona AutoCoinColor
            PrimaryText=""Elvia Atkins""
            SecondaryText=""QA Engineer""
            Size=""BitPersonaSize.Size72"" />

<BitPersona AutoCoinColor
            ImageInitials=""ZZ""
            PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Size=""BitPersonaSize.Size72"" />

<BitPersona AutoCoinColor
            CoinColor=""BitColor.Success""
            PrimaryText=""Xafan Salina""
            SecondaryText=""Always green""
            Size=""BitPersonaSize.Size72"" />";

    private readonly string example13RazorCode = @"
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Size=""BitPersonaSize.Size72""
            OnImageLoad=""() => imageLoadCount++""
            ImageUrl=""/images/persona/persona-female.png"" />
<p>Image Load Count: @imageLoadCount</p>

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Size=""BitPersonaSize.Size72""
            OnImageError=""() => imageErrorCount++""
            ImageUrl=""invalid-image-url"" />
<p>Image Error Count: @imageErrorCount</p>";
    private readonly string example13CsharpCode = @"
private int imageLoadCount = 0;
private int imageErrorCount = 0;";

    private readonly string example14RazorCode = @"
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Lazy loaded""
            Size=""BitPersonaSize.Size72""
            ImageLoading=""BitImageLoading.Lazy""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Eagerly loaded""
            Size=""BitPersonaSize.Size72""
            ImageLoading=""BitImageLoading.Eager""
            ImageUrl=""/images/persona/persona-female.png"" />";

    private readonly string example15RazorCode = @"
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/images/persona/persona-female.png""
            ImageSrcSet=""/images/persona/persona-female.png 1x, /images/persona/persona-female.png 2x"" />";

    private readonly string example16RazorCode = @"
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

    private readonly string example17RazorCode = @"
<BitPersona Dir=""BitDir.Rtl""
            PrimaryText=""صالح یوسف نژاد""
            SecondaryText=""مهندس نرم افزار""
            Size=""@BitPersonaSize.Size56"" />";
}
