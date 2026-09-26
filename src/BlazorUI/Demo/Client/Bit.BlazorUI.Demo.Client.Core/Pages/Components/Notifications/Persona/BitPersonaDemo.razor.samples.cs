namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.Persona;

public partial class BitPersonaDemo
{
    private readonly string example1RazorCode = @"
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Saleh Khafan""
            SecondaryText=""Developer""
            Size=""BitPersonaSize.Size72"" />

<BitPersona PrimaryText=""+1 (555) 016 7788""
            SecondaryText=""No initials to take""
            Size=""BitPersonaSize.Size72"" />";

    private readonly string example2RazorCode = @"
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Online""
            Presence=""BitPersonaPresence.Online""
            Size=""BitPersonaSize.Size56""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Away""
            Presence=""BitPersonaPresence.Away""
            Size=""BitPersonaSize.Size56""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Busy""
            Presence=""BitPersonaPresence.Busy""
            Size=""BitPersonaSize.Size56""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Dnd (do not disturb)""
            Presence=""BitPersonaPresence.Dnd""
            Size=""BitPersonaSize.Size56""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Offline""
            Presence=""BitPersonaPresence.Offline""
            Size=""BitPersonaSize.Size56""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Blocked""
            Presence=""BitPersonaPresence.Blocked""
            Size=""BitPersonaSize.Size56""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""OutOfOffice""
            Presence=""BitPersonaPresence.OutOfOffice""
            Size=""BitPersonaSize.Size56""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Unknown""
            Presence=""BitPersonaPresence.Unknown""
            Size=""BitPersonaSize.Size56""
            ImageUrl=""/images/persona/persona-female.png"" />


<div>A glyph in the dot (<b>PresenceIconNames</b>), and a single status's glyph (<b>PresenceIconName</b>):</div>

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Online""
            PresenceIconNames=""_iconNames""
            Size=""BitPersonaSize.Size100""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Out of office""
            Presence=""BitPersonaPresence.OutOfOffice""
            PresenceIconName=""@BitIconName.Airplane""
            Size=""BitPersonaSize.Size100""
            ImageUrl=""/images/persona/persona-female.png"" />


<div>Named for the reader (<b>PresenceTitles</b>) - hover the dot:</div>

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Dnd""
            PresenceTitles=""_presenceTitles""
            Size=""BitPersonaSize.Size100""
            ImageUrl=""/images/persona/persona-female.png"" />";
    private readonly string example2CsharpCode = @"
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
    { BitPersonaPresence.Offline, ""Signed out"" },
    { BitPersonaPresence.Online, ""Available"" },
    { BitPersonaPresence.Away, ""Be right back"" },
    { BitPersonaPresence.Dnd, ""Do not disturb"" },
    { BitPersonaPresence.Blocked, ""Blocked"" },
    { BitPersonaPresence.Busy, ""In a call"" },
    { BitPersonaPresence.OutOfOffice, ""Out of office"" },
    { BitPersonaPresence.Unknown, ""Presence unknown"" },
};";

    private readonly string example3RazorCode = @"
<BitPersona PrimaryText=""Saleh Khafan"" SecondaryText=""Two words"" Size=""BitPersonaSize.Size72"" />

<BitPersona PrimaryText=""Ted Alan Randall"" SecondaryText=""Three words - the middle one is skipped"" Size=""BitPersonaSize.Size72"" />

<BitPersona PrimaryText=""Elvia Atkins (Contoso)"" SecondaryText=""The aside is dropped"" Size=""BitPersonaSize.Size72"" />

<BitPersona PrimaryText=""carlos.slattery@@contoso.com"" SecondaryText=""An address, not a name"" Size=""BitPersonaSize.Size72"" />

<BitPersona AllowPhoneInitials PrimaryText=""+1 (555) 016 7788"" SecondaryText=""AllowPhoneInitials"" Size=""BitPersonaSize.Size72"" />

<BitPersona PrimaryText=""Saleh Khafan"" SecondaryText=""ImageInitials"" ImageInitials=""SK!"" Size=""BitPersonaSize.Size72"" />

<BitPersona PrimaryText=""Saleh Khafan"" SecondaryText=""Four letters, stepped down to fit"" ImageInitials=""SKHN"" Size=""BitPersonaSize.Size72"" />

<BitPersona PrimaryText=""Saleh Khafan""
            SecondaryText=""Broken image url""
            Size=""BitPersonaSize.Size72""
            ShowInitialsUntilImageLoads
            ImageUrl=""invalid-src"" />";

    private readonly string example4RazorCode = @"
<BitPersona PrimaryText=""Design Team""
            SecondaryText=""12 members""
            CoinIconName=""@BitIconName.Group""
            Size=""BitPersonaSize.Size72"" />

<BitPersona PrimaryText=""Build Bot""
            SecondaryText=""Service account""
            CoinIconName=""@BitIconName.Robot""
            Size=""BitPersonaSize.Size72"" />

<BitPersona PrimaryText=""Unknown""
            SecondaryText=""Developer""
            Size=""BitPersonaSize.Size72""
            Unknown />

<BitPersona PrimaryText=""Unresolved""
            SecondaryText=""Custom unknown icon""
            Size=""BitPersonaSize.Size72""
            UnknownIconName=""@BitIconName.StatusErrorFull""
            Unknown />";

    private readonly string example5RazorCode = @"
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Circular (default)""
            Presence=""BitPersonaPresence.Online""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona Shape=""BitPersonaShape.Rounded""
            PrimaryText=""Xafan Salina""
            SecondaryText=""Rounded""
            Presence=""BitPersonaPresence.Online""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona Shape=""BitPersonaShape.Square""
            PrimaryText=""Design Team""
            SecondaryText=""Square""
            CoinIconName=""@BitIconName.Group""
            Size=""BitPersonaSize.Size72"" />


<BitPersona PrimaryText=""Saleh Xafan""
            SecondaryText=""Fill (default)""
            Size=""BitPersonaSize.Size72""
            CoinVariant=""BitVariant.Fill"" />

<BitPersona PrimaryText=""Saleh Xafan""
            SecondaryText=""Outline""
            Size=""BitPersonaSize.Size72""
            CoinVariant=""BitVariant.Outline"" />

<BitPersona PrimaryText=""Saleh Xafan""
            SecondaryText=""Text""
            Size=""BitPersonaSize.Size72""
            CoinVariant=""BitVariant.Text"" />";

    private readonly string example6RazorCode = @"
<BitPersona Active
            PrimaryText=""Xafan Salina""
            SecondaryText=""Ring (default)""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona Active
            ActiveAppearance=""BitPersonaActiveAppearance.Shadow""
            PrimaryText=""Xafan Salina""
            SecondaryText=""Shadow""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona Active
            ActiveAppearance=""BitPersonaActiveAppearance.RingShadow""
            PrimaryText=""Xafan Salina""
            SecondaryText=""RingShadow""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona Inactive
            PrimaryText=""Saleh Khafan""
            SecondaryText=""Inactive""
            Size=""BitPersonaSize.Size72"" />


<div>On a tinted surface, with the gap cut in that surface:</div>

<div style=""padding: 1rem; border-radius: 0.5rem; background-color: var(--bit-clr-bg-ter);"">
    <BitPersona Active
                PrimaryText=""Xafan Salina""
                SecondaryText=""Retuned gap color""
                Size=""BitPersonaSize.Size72""
                Style=""--bit-Persona-ring-gap-color: var(--bit-clr-bg-ter);""
                ImageUrl=""/images/persona/persona-female.png"" />
</div>";

    private readonly string example7RazorCode = @"
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm""
            Size=""BitPersonaSize.Size120""
            Presence=""BitPersonaPresence.Online""
            OnActionClick=""() => actionClickCount++""
            ActionIconName=""@BitIconName.CloudUpload""
            ImageUrl=""/images/persona/persona-female.png"" />
<p>Action click count: @actionClickCount</p>

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            Size=""BitPersonaSize.Size120""
            ImageOverlayText=""Change photo""
            OnImageClick=""() => imageClickCount++""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Saleh Khafan""
            SecondaryText=""No picture, same overlay""
            Size=""BitPersonaSize.Size120""
            ImageOverlayText=""Add photo""
            OnImageClick=""() => imageClickCount++"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Empty ImageOverlayText""
            Size=""BitPersonaSize.Size120""
            ImageOverlayText=""""
            OnImageClick=""() => imageClickCount++""
            ImageUrl=""/images/persona/persona-female.png"" />
<p>Image click count: @imageClickCount</p>

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""ActionTemplate""
            Size=""BitPersonaSize.Size120""
            OnActionClick=""() => actionClickCount++""
            ImageUrl=""/images/persona/persona-female.png"">
    <ActionTemplate>
        <BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text"" OnClick=""() => actionClickCount++"">Upload</BitButton>
    </ActionTemplate>
</BitPersona>";
    private readonly string example7CsharpCode = @"
private int imageClickCount = 0;
private int actionClickCount = 0;";

    private readonly string example8RazorCode = @"
<BitPersona Href=""/components/persona""
            PrimaryText=""Xafan Salina""
            SecondaryText=""Opens this page""
            Presence=""BitPersonaPresence.Online""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona Href=""https://github.com/bitfoundation/bitplatform""
            Target=""_blank""
            PrimaryText=""bit platform""
            SecondaryText=""Opens in a new tab""
            CoinIconName=""@BitIconName.Globe""
            Size=""BitPersonaSize.Size72"" />

<BitPersona Href=""/components/persona""
            ImageOverlayText=""View profile""
            PrimaryText=""Xafan Salina""
            SecondaryText=""With a veil""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/images/persona/persona-female.png"" />";

    private readonly string example9RazorCode = @"
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Online""
            Size=""BitPersonaSize.Size72""
            IsEnabled=""false""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Saleh Khafan""
            SecondaryText=""Clickable, disabled""
            Size=""BitPersonaSize.Size72""
            OnImageClick=""() => {}""
            IsEnabled=""false"" />

<BitPersona Href=""/components/persona""
            PrimaryText=""Xafan Salina""
            SecondaryText=""Link, disabled""
            Size=""BitPersonaSize.Size72""
            IsEnabled=""false""
            ImageUrl=""/images/persona/persona-female.png"" />";

    private readonly string example10RazorCode = @"
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Loads, lazily""
            Size=""BitPersonaSize.Size72""
            ImageLoading=""BitImageLoading.Lazy""
            OnImageLoad=""() => imageLoadCount++""
            ImageUrl=""/images/persona/persona-female.png"" />
<p>Image load count: @imageLoadCount</p>

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Fails""
            Size=""BitPersonaSize.Size72""
            OnImageError=""() => imageErrorCount++""
            ImageUrl=""invalid-image-url"" />
<p>Image error count: @imageErrorCount</p>

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Eager, not draggable""
            Size=""BitPersonaSize.Size72""
            ImageLoading=""BitImageLoading.Eager""
            ImageAttributes=""@(new() { { ""draggable"", ""false"" }, { ""decoding"", ""async"" } })""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitButton Size=""BitSize.Small"" Variant=""BitVariant.Outline"" OnClick=""() => isFadeInPersonaShown = !isFadeInPersonaShown"">
    @(isFadeInPersonaShown ? ""Hide"" : ""Show"")
</BitButton>

@if (isFadeInPersonaShown)
{
    <BitPersona ImageFadeIn
                PrimaryText=""Xafan Salina""
                SecondaryText=""Faded in""
                Size=""BitPersonaSize.Size72""
                ImageUrl=""/images/persona/persona-female.png"" />
}";
    private readonly string example10CsharpCode = @"
private int imageLoadCount = 0;
private int imageErrorCount = 0;
private bool isFadeInPersonaShown = true;";

    private readonly string example11RazorCode = @"
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Size=""BitPersonaSize.Size72""
            ImageAlt=""Xafan Salina smiling at the camera""
            ImageUrl=""/images/persona/persona-female.png""
            ImageSizes=""72px""
            ImageSrcSet=""/images/persona/persona-female-72.png 72w, /images/persona/persona-female.png 96w"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Candidates only""
            Size=""BitPersonaSize.Size72""
            ImageSizes=""72px""
            ImageSrcSet=""/images/persona/persona-female-72.png 72w, /images/persona/persona-female.png 96w"" />";

    private readonly string example12RazorCode = @"
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

<BitPersona Size=""BitPersonaSize.Size100"" PrimaryText=""Xafan Salina"" SecondaryText=""CoinTemplate"" Presence=""BitPersonaPresence.Online"" CoinVariant=""BitVariant.Text"">
    <CoinTemplate>
        <img src=""/images/persona/persona-female.png"" width=""100"" height=""100"" class=""custom-coin"" />
    </CoinTemplate>
</BitPersona>";

    private readonly string example13RazorCode = @"
<BitPersona AutoCoinColor PrimaryText=""Xafan Salina"" SecondaryText=""Software Engineer"" Size=""BitPersonaSize.Size72"" />

<BitPersona AutoCoinColor PrimaryText=""Saleh Khafan"" SecondaryText=""Developer"" Size=""BitPersonaSize.Size72"" />

<BitPersona AutoCoinColor PrimaryText=""Ted Randall"" SecondaryText=""Designer"" Size=""BitPersonaSize.Size72"" />


<div>Same seed, same color (<b>CoinColorSeed</b>):</div>

<BitPersona AutoCoinColor CoinColorSeed=""u-1024"" PrimaryText=""Xafan Salina"" SecondaryText=""u-1024"" Size=""BitPersonaSize.Size72"" />

<BitPersona AutoCoinColor CoinColorSeed=""u-1024"" PrimaryText=""X. Salina"" SecondaryText=""u-1024"" Size=""BitPersonaSize.Size72"" />


<div>A palette of your own (<b>AutoCoinColors</b>), and an explicit <b>CoinColor</b>:</div>

<BitPersona AutoCoinColor AutoCoinColors=""_coinColors"" PrimaryText=""Carlos Slattery"" SecondaryText=""Manager"" Size=""BitPersonaSize.Size72"" />

<BitPersona AutoCoinColor AutoCoinColors=""_coinColors"" PrimaryText=""Elvia Atkins"" SecondaryText=""QA Engineer"" Size=""BitPersonaSize.Size72"" />

<BitPersona AutoCoinColor CoinColor=""BitColor.Success"" PrimaryText=""Xafan Salina"" SecondaryText=""Always green"" Size=""BitPersonaSize.Size72"" />";
    private readonly string example13CsharpCode = @"
private readonly BitColor[] _coinColors = [BitColor.Primary, BitColor.Info, BitColor.Tertiary];";

    private readonly string example14RazorCode = @"
<BitPersona Reversed
            PrimaryText=""Xafan Salina""
            SecondaryText=""Reversed""
            Presence=""BitPersonaPresence.Online""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/images/persona/persona-female.png"" />

<div style=""display: flex; gap: 2rem; flex-wrap: wrap;"">
    <BitPersona Vertical
                PrimaryText=""Xafan Salina""
                SecondaryText=""Vertical""
                Presence=""BitPersonaPresence.Online""
                Size=""BitPersonaSize.Size72""
                ImageUrl=""/images/persona/persona-female.png"" />
    <BitPersona Vertical
                Reversed
                PrimaryText=""Saleh Khafan""
                SecondaryText=""Vertical, Reversed""
                Presence=""BitPersonaPresence.Away""
                Size=""BitPersonaSize.Size72"" />
</div>

<div style=""width: 16rem; padding: 0.5rem; border: 1px solid var(--bit-clr-brd-sec);"">
    <BitPersona FullWidth
                PrimaryText=""Xafan Salina Abdollahzadeh Yusefnejad""
                SecondaryText=""Principal Software Engineer, Developer Experience""
                Size=""BitPersonaSize.Size48""
                ImageUrl=""/images/persona/persona-female.png"" />
</div>

<div style=""width: 16rem; padding: 0.5rem; border: 1px solid var(--bit-clr-brd-sec);"">
    <BitPersona FullWidth
                ShowOverflowTooltip=""false""
                PrimaryText=""Xafan Salina Abdollahzadeh Yusefnejad""
                SecondaryText=""No tooltip on hover""
                Size=""BitPersonaSize.Size48""
                ImageUrl=""/images/persona/persona-female.png"" />
</div>";

    private readonly string example15RazorCode = @"
<BitParams Parameters=""@personaParams"">
    <BitPersona PrimaryText=""Xafan Salina"" SecondaryText=""Software Engineer"" Presence=""BitPersonaPresence.Online"" />

    <BitPersona PrimaryText=""Saleh Khafan"" SecondaryText=""Developer"" Presence=""BitPersonaPresence.Away"" />

    <BitPersona PrimaryText=""Ted Randall"" SecondaryText=""Its own Size"" Presence=""BitPersonaPresence.Busy"" Size=""BitPersonaSize.Size72"" />
</BitParams>";
    private readonly string example15CsharpCode = @"
private readonly BitPersonaParams[] personaParams =
[
    new()
    {
        Size = BitPersonaSize.Size40,
        AutoCoinColor = true,
        Shape = BitPersonaShape.Rounded,
        PresenceTitles = new()
        {
            { BitPersonaPresence.Online, ""Available"" },
            { BitPersonaPresence.Away, ""Be right back"" },
            { BitPersonaPresence.Busy, ""In a call"" },
        },
    }
];";

    private readonly string example16RazorCode = @"
<BitPersona PrimaryText=""Primary"" CoinColor=""BitColor.Primary"" />

<BitPersona PrimaryText=""Secondary"" CoinColor=""BitColor.Secondary"" />

<BitPersona PrimaryText=""Tertiary"" CoinColor=""BitColor.Tertiary"" />

<BitPersona PrimaryText=""Info"" SecondaryText=""(default)"" CoinColor=""BitColor.Info"" />

<BitPersona PrimaryText=""Success"" CoinColor=""BitColor.Success"" />

<BitPersona PrimaryText=""Warning"" CoinColor=""BitColor.Warning"" />

<BitPersona PrimaryText=""SevereWarning"" CoinColor=""BitColor.SevereWarning"" />

<BitPersona PrimaryText=""Error"" CoinColor=""BitColor.Error"" />

<div style=""background: var(--bit-clr-fg-ter); padding: 1rem;"">
    <BitPersona PrimaryText=""PrimaryBackground"" CoinColor=""BitColor.PrimaryBackground"" />

    <BitPersona PrimaryText=""SecondaryBackground"" CoinColor=""BitColor.SecondaryBackground"" />

    <BitPersona PrimaryText=""TertiaryBackground"" CoinColor=""BitColor.TertiaryBackground"" />
</div>

<BitPersona PrimaryText=""PrimaryForeground"" CoinColor=""BitColor.PrimaryForeground"" />

<BitPersona PrimaryText=""SecondaryForeground"" CoinColor=""BitColor.SecondaryForeground"" />

<BitPersona PrimaryText=""TertiaryForeground"" CoinColor=""BitColor.TertiaryForeground"" />

<BitPersona PrimaryText=""PrimaryBorder"" CoinColor=""BitColor.PrimaryBorder"" />

<BitPersona PrimaryText=""SecondaryBorder"" CoinColor=""BitColor.SecondaryBorder"" />

<BitPersona PrimaryText=""TertiaryBorder"" CoinColor=""BitColor.TertiaryBorder"" />";

    private readonly string example17RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""FontAwesome action icon""
            Size=""BitPersonaSize.Size100""
            OnActionClick=""() => actionClickCount++""
            ActionIcon=""@BitIconInfo.Fa(""solid camera"")""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Design Team""
            SecondaryText=""FontAwesome coin icon""
            Size=""BitPersonaSize.Size72""
            CoinIcon=""@BitIconInfo.Fa(""solid people-group"")"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Bootstrap Icons presence icons""
            Presence=""BitPersonaPresence.Online""
            PresenceIcons=""_icons""
            Size=""BitPersonaSize.Size100""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Busy""
            Presence=""BitPersonaPresence.Busy""
            PresenceIcons=""_icons""
            Size=""BitPersonaSize.Size100""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Unresolved""
            SecondaryText=""FontAwesome unknown icon""
            Size=""BitPersonaSize.Size72""
            UnknownIcon=""@BitIconInfo.Fa(""solid user-secret"")""
            Unknown />";
    private readonly string example17CsharpCode = @"
private int actionClickCount = 0;

private readonly Dictionary<BitPersonaPresence, BitIconInfo> _icons = new()
{
    { BitPersonaPresence.Offline, BitIconInfo.Bi(""wifi-off"") },
    { BitPersonaPresence.Online, BitIconInfo.Bi(""check-circle-fill"") },
    { BitPersonaPresence.Away, BitIconInfo.Bi(""clock-fill"") },
    { BitPersonaPresence.Dnd, BitIconInfo.Bi(""dash-circle-fill"") },
    { BitPersonaPresence.Blocked, BitIconInfo.Bi(""ban"") },
    { BitPersonaPresence.Busy, BitIconInfo.Bi(""exclamation-circle-fill"") },
};";

    private readonly string example18RazorCode = @"
<BitCheckbox @bind-Value=""isDetailsShown"" Label=""Show details"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Secondary""
            Size=""BitPersonaSize.Size8""
            HidePersonaDetails=""!isDetailsShown""
            Presence=""BitPersonaPresence.Online"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Secondary""
            Size=""BitPersonaSize.Size24""
            HidePersonaDetails=""!isDetailsShown""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Secondary""
            Size=""BitPersonaSize.Size32""
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
            TertiaryText=""In a meeting""
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
            SecondaryText=""CoinSize 150""
            Size=""BitPersonaSize.Size120""
            Presence=""BitPersonaPresence.Online""
            HidePersonaDetails=""!isDetailsShown"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""ShowSecondaryText""
            Size=""BitPersonaSize.Size24""
            ShowSecondaryText
            HidePersonaDetails=""!isDetailsShown""
            ImageUrl=""/images/persona/persona-female.png"" />";
    private readonly string example18CsharpCode = @"
private bool isDetailsShown = true;";

    private readonly string example19RazorCode = @"
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
            Style=""padding: 1rem; background: gray; border-radius: 1rem;"" />

<BitPersona PrimaryText=""Saleh Khafan""
            Size=""BitPersonaSize.Size72""
            Class=""custom-class"" />


<div><b>Styles</b> & <b>Classes</b>:</div>

<BitPersona PrimaryText=""Saleh Khafan""
            Size=""BitPersonaSize.Size72""
            Presence=""BitPersonaPresence.Online""
            Styles=""@(new() { ImageContainer = ""color: #b6ff00; background-color: #00ff90;"",
                              Presence = ""border-color: #b6ff00;"",
                              PrimaryTextContainer = ""color: #ea1919; font-weight: bold; font-style: italic;"" })"" />

<BitPersona PrimaryText=""Saleh Khafan""
            Size=""BitPersonaSize.Size72""
            Classes=""@(new() { ImageContainer = ""custom-img-container"",
                               PrimaryTextContainer = ""custom-primary-text"" })"" />


<div>CSS variables, set once on an ancestor:</div>

<div style=""--bit-Persona-coin-background: #7a2e8e;
            --bit-Persona-coin-color: #fff;
            --bit-Persona-coin-radius: 0.75rem;
            --bit-Persona-gap: 1.5rem;
            --bit-Persona-primary-font-weight: 600;
            --bit-Persona-secondary-color: #7a2e8e;"">
    <BitPersona PrimaryText=""Saleh Khafan"" SecondaryText=""Developer"" Size=""BitPersonaSize.Size56"" />

    <BitPersona Active PrimaryText=""Ted Randall"" SecondaryText=""Designer"" Size=""BitPersonaSize.Size56"" />
</div>";

    private readonly string example20RazorCode = @"
<div dir=""rtl"">
    <BitPersona Dir=""BitDir.Rtl""
                PrimaryText=""صالح یوسف نژاد""
                SecondaryText=""مهندس نرم افزار""
                Presence=""BitPersonaPresence.Online""
                Size=""BitPersonaSize.Size56"" />

    <BitPersona Dir=""BitDir.Rtl""
                PrimaryText=""Saleh Khafan""
                SecondaryText=""یک نام لاتین""
                Presence=""BitPersonaPresence.Online""
                Size=""BitPersonaSize.Size56"" />

    <BitPersona Dir=""BitDir.Rtl""
                PrimaryText=""صالح یوسف نژاد""
                SecondaryText=""مهندس نرم افزار""
                Presence=""BitPersonaPresence.Online""
                Size=""BitPersonaSize.Size56""
                ImageUrl=""/images/persona/persona-female.png"" />
</div>";
}
