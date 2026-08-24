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


<div>With a glyph inside the dot (PresenceIconNames)</div>
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Online""
            PresenceIconNames=""_iconNames""
            Size=""BitPersonaSize.Size100""
            ImageUrl=""/images/persona/persona-female.png"" />


<div>A single status, without a map of all eight (PresenceIconName)</div>
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Out of office""
            Presence=""BitPersonaPresence.OutOfOffice""
            PresenceIconName=""@BitIconName.Airplane""
            Size=""BitPersonaSize.Size100""
            ImageUrl=""/images/persona/persona-female.png"" />


<div>Named for the reader (PresenceTitles)</div>
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

<BitPersona PrimaryText=""carlos.slattery@contoso.com"" SecondaryText=""An address, not a name"" Size=""BitPersonaSize.Size72"" />

<BitPersona PrimaryText=""Saleh Khafan"" SecondaryText=""ImageInitials"" ImageInitials=""SK!"" Size=""BitPersonaSize.Size72"" />

<BitPersona PrimaryText=""Saleh Khafan"" SecondaryText=""Three letters, stepped down to fit"" ImageInitials=""SKH"" Size=""BitPersonaSize.Size72"" />

<BitPersona PrimaryText=""Saleh Khafan"" SecondaryText=""Four letters, stepped down further"" ImageInitials=""SKHN"" Size=""BitPersonaSize.Size72"" />


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

<BitPersona PrimaryText=""Meeting Room 4""
            SecondaryText=""Second floor""
            CoinColor=""BitColor.Tertiary""
            CoinIconName=""@BitIconName.Room""
            Size=""BitPersonaSize.Size72"" />

<BitPersona PrimaryText=""Build Bot""
            SecondaryText=""Service account""
            Squared
            CoinColor=""BitColor.Success""
            CoinIconName=""@BitIconName.Robot""
            Size=""BitPersonaSize.Size72"" />";

    private readonly string example5RazorCode = @"
<BitPersona PrimaryText=""Unknown""
            SecondaryText=""Developer""
            Size=""BitPersonaSize.Size72""
            Unknown />

<BitPersona PrimaryText=""Unresolved""
            SecondaryText=""Custom unknown icon""
            Size=""BitPersonaSize.Size72""
            UnknownIconName=""@BitIconName.StatusErrorFull""
            Unknown />";

    private readonly string example6RazorCode = @"
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Circle""
            Presence=""BitPersonaPresence.Online""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona Squared
            PrimaryText=""Xafan Salina""
            SecondaryText=""Square""
            Presence=""BitPersonaPresence.Online""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/images/persona/persona-female.png"" />";

    private readonly string example7RazorCode = @"
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

    private readonly string example8RazorCode = @"
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Not active""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/images/persona/persona-female.png"" />

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
            CoinColor=""BitColor.Success""
            PrimaryText=""Xafan Salina""
            SecondaryText=""RingShadow""
            Size=""BitPersonaSize.Size72""
            ImageUrl=""/images/persona/persona-female.png"" />


<div>On a surface of its own, with the gap cut in that surface instead of the page</div>
<div class=""tinted-box"">
    <BitPersona Active
                PrimaryText=""Xafan Salina""
                SecondaryText=""Default gap color""
                Size=""BitPersonaSize.Size72""
                ImageUrl=""/images/persona/persona-female.png"" />

    <BitPersona Active
                PrimaryText=""Xafan Salina""
                SecondaryText=""Retuned gap color""
                Size=""BitPersonaSize.Size72""
                Style=""--bit-prs-ring-gap-clr: var(--bit-clr-bg-ter);""
                ImageUrl=""/images/persona/persona-female.png"" />
</div>";

    private readonly string example9RazorCode = @"
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            TertiaryText=""In a meeting""
            OptionalText=""Available at 4:00pm""
            Size=""BitPersonaSize.Size120""
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
            ImageOverlayText=""Change photo""
            OnImageClick=""() => imageClickCount++""
            ImageUrl=""/images/persona/persona-female.png"" />
<p>Image Click Count: @imageClickCount</p>

<BitPersona PrimaryText=""Saleh Khafan""
            SecondaryText=""Developer""
            Size=""BitPersonaSize.Size120""
            ImageOverlayText=""Add photo""
            OnImageClick=""() => imageClickCount++"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Size=""BitPersonaSize.Size120""
            OnActionClick=""() => actionClickCount++""
            ImageUrl=""/images/persona/persona-female.png"">
    <ActionTemplate>
        <BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text"" OnClick=""() => actionClickCount++"">Upload</BitButton>
    </ActionTemplate>
</BitPersona>";
    private readonly string example9CsharpCode = @"
private int imageClickCount = 0;
private int actionClickCount = 0;";

    private readonly string example10RazorCode = @"
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Online""
            Size=""BitPersonaSize.Size72""
            IsEnabled=""false""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Saleh Khafan""
            SecondaryText=""Developer""
            Size=""BitPersonaSize.Size72""
            IsEnabled=""false"" />";

    private readonly string example11RazorCode = @"
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Loads""
            Size=""BitPersonaSize.Size72""
            OnImageLoad=""() => imageLoadCount++""
            ImageUrl=""/images/persona/persona-female.png"" />
<p>Image Load Count: @imageLoadCount</p>

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Fails""
            Size=""BitPersonaSize.Size72""
            OnImageError=""() => imageErrorCount++""
            ImageUrl=""invalid-image-url"" />
<p>Image Error Count: @imageErrorCount</p>";
    private readonly string example11CsharpCode = @"
private int imageLoadCount = 0;
private int imageErrorCount = 0;";

    private readonly string example12RazorCode = @"
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Lazy loaded""
            Size=""BitPersonaSize.Size72""
            ImageLoading=""BitImageLoading.Lazy""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Eagerly loaded, not draggable""
            Size=""BitPersonaSize.Size72""
            ImageLoading=""BitImageLoading.Eager""
            ImageAttributes=""@(new() { { ""draggable"", ""false"" }, { ""decoding"", ""async"" } })""
            ImageUrl=""/images/persona/persona-female.png"" />";

    private readonly string example13RazorCode = @"
<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Size=""BitPersonaSize.Size72""
            ImageAlt=""Xafan Salina smiling at the camera""
            ImageUrl=""/images/persona/persona-female.png""
            ImageSizes=""72px""
            ImageSrcSet=""/images/persona/persona-female-72.png 72w, /images/persona/persona-female.png 96w"" />


<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Size=""BitPersonaSize.Size72""
            ImageSizes=""72px""
            ImageSrcSet=""/images/persona/persona-female-72.png 72w, /images/persona/persona-female.png 96w"" />";

    private readonly string example14RazorCode = @"
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
        <img src=""/images/persona/persona-female.png"" width=""100"" height=""100"" class=""custom-coin"" />
    </CoinTemplate>
</BitPersona>";

    private readonly string example15RazorCode = @"
<BitPersona AutoCoinColor PrimaryText=""Xafan Salina"" SecondaryText=""Software Engineer"" Size=""BitPersonaSize.Size72"" />
<BitPersona AutoCoinColor PrimaryText=""Saleh Khafan"" SecondaryText=""Developer"" Size=""BitPersonaSize.Size72"" />
<BitPersona AutoCoinColor PrimaryText=""Ted Randall"" SecondaryText=""Designer"" Size=""BitPersonaSize.Size72"" />
<BitPersona AutoCoinColor PrimaryText=""Carlos Slattery"" SecondaryText=""Manager"" Size=""BitPersonaSize.Size72"" />
<BitPersona AutoCoinColor PrimaryText=""Elvia Atkins"" SecondaryText=""QA Engineer"" Size=""BitPersonaSize.Size72"" />


<BitPersona AutoCoinColor CoinColorSeed=""u-1024"" PrimaryText=""Xafan Salina"" SecondaryText=""Software Engineer"" Size=""BitPersonaSize.Size72"" />
<BitPersona AutoCoinColor CoinColorSeed=""u-1024"" PrimaryText=""X. Salina"" SecondaryText=""Same seed, same color"" Size=""BitPersonaSize.Size72"" />


<BitPersona AutoCoinColor CoinColor=""BitColor.Success"" PrimaryText=""Xafan Salina"" SecondaryText=""Always green"" Size=""BitPersonaSize.Size72"" />";

    private readonly string example16RazorCode = @"
<BitPersona Reversed
            PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Presence=""BitPersonaPresence.Online""
            PresenceIconNames=""_iconNames""
            Size=""BitPersonaSize.Size100""
            ImageUrl=""/images/persona/persona-female.png"" />";

    private readonly string example17RazorCode = @"
<style>
    .width-box {
        width: 20rem;
        padding: 0.5rem;
        border: 1px solid gray;
    }
</style>

<div class=""width-box"">
    <BitPersona PrimaryText=""Xafan Salina"" SecondaryText=""Software Engineer"" Size=""BitPersonaSize.Size48""
                ImageUrl=""/images/persona/persona-female.png"" />
</div>

<div class=""width-box"">
    <BitPersona FullWidth PrimaryText=""Xafan Salina"" SecondaryText=""Software Engineer"" Size=""BitPersonaSize.Size48""
                ImageUrl=""/images/persona/persona-female.png"" />
</div>

<div class=""width-box"">
    <BitPersona FullWidth PrimaryText=""Xafan Salina Abdollahzadeh Yusefnejad"" SecondaryText=""Principal Software Engineer, Platform and Developer Experience"" Size=""BitPersonaSize.Size48""
                ImageUrl=""/images/persona/persona-female.png"" />
</div>";

    private readonly string example18RazorCode = @"
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

    private readonly string example19RazorCode = @"
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

<BitPersona PrimaryText=""Design Team""
            SecondaryText=""12 members""
            Size=""BitPersonaSize.Size72""
            CoinIcon=""@BitIconInfo.Fa(""solid people-group"")"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Online""
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

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Away""
            Presence=""BitPersonaPresence.Away""
            PresenceIcons=""_icons""
            Size=""BitPersonaSize.Size100""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Dnd""
            Presence=""BitPersonaPresence.Dnd""
            PresenceIcons=""_icons""
            Size=""BitPersonaSize.Size100""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Offline""
            Presence=""BitPersonaPresence.Offline""
            PresenceIcons=""_icons""
            Size=""BitPersonaSize.Size100""
            ImageUrl=""/images/persona/persona-female.png"" />";
    private readonly string example19CsharpCode = @"
private readonly Dictionary<BitPersonaPresence, BitIconInfo> _icons = new()
{
    { BitPersonaPresence.Offline, BitIconInfo.Bi(""wifi-off"") },
    { BitPersonaPresence.Online, BitIconInfo.Bi(""check-circle-fill"") },
    { BitPersonaPresence.Away, BitIconInfo.Bi(""clock-fill"") },
    { BitPersonaPresence.Dnd, BitIconInfo.Bi(""dash-circle-fill"") },
    { BitPersonaPresence.Blocked, BitIconInfo.Bi(""ban"") },
    { BitPersonaPresence.Busy, BitIconInfo.Bi(""exclamation-circle-fill"") },
};";

    private readonly string example20RazorCode = @"
<BitCheckbox @bind-Value=""isDetailsShown"" Label=""Include BitPersona details"" />

<BitPersona PrimaryText=""Xafan Salina""
            SecondaryText=""Secondary""
            Size=""BitPersonaSize.Size8""
            HidePersonaDetails=""!isDetailsShown""
            Presence=""BitPersonaPresence.Online""
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
            Presence=""BitPersonaPresence.Online""
            HidePersonaDetails=""!isDetailsShown""
            ImageUrl=""/images/persona/persona-female.png"" />

<BitPersona CoinSize=""150""
            PrimaryText=""Xafan Salina""
            SecondaryText=""Software Engineer""
            Size=""BitPersonaSize.Size120""
            HidePersonaDetails=""!isDetailsShown"" />";
    private readonly string example20CsharpCode = @"
private bool isDetailsShown = true;";

    private readonly string example21RazorCode = @"
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
            Presence=""BitPersonaPresence.Online""
            Styles=""@(new() { ImageContainer = ""color: #b6ff00; background-color: #00ff90;"",
                              Presence = ""border-color: #b6ff00;"",
                              PrimaryTextContainer = ""color: #ea1919; font-weight: bold; font-style: italic;"" })"" />

<BitPersona PrimaryText=""Saleh Khafan""
            Size=""BitPersonaSize.Size72""
            Classes=""@(new() { ImageContainer = ""custom-img-container"",
                               PrimaryTextContainer = ""custom-primary-text"" })"" />";

    private readonly string example22RazorCode = @"
<div dir=""rtl"">
    <BitPersona Dir=""BitDir.Rtl""
                PrimaryText=""صالح یوسف نژاد""
                SecondaryText=""مهندس نرم افزار""
                Presence=""BitPersonaPresence.Online""
                Size=""@BitPersonaSize.Size56"" />

    <BitPersona Dir=""BitDir.Rtl""
                PrimaryText=""Saleh Khafan""
                SecondaryText=""یک نام لاتین""
                Presence=""BitPersonaPresence.Online""
                Size=""@BitPersonaSize.Size56"" />

    <BitPersona Dir=""BitDir.Rtl""
                PrimaryText=""صالح یوسف نژاد""
                SecondaryText=""مهندس نرم افزار""
                Presence=""BitPersonaPresence.Online""
                Size=""@BitPersonaSize.Size56""
                ImageUrl=""/images/persona/persona-female.png"" />
</div>";
}
