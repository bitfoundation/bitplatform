namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Card;

public partial class BitCardDemo
{
    private readonly string example1RazorCode = @"
<BitCard>
    <BitStack HorizontalAlign=""BitAlignment.Start"">
        <BitText Typography=""BitTypography.H4"">bit BlazorUI</BitText>
        <BitText Typography=""BitTypography.Body1"">
            bit BlazorUI components are native, easy-to-customize, and ...
        </BitText>
        <BitLink Href=""https://blazorui.bitplatform.dev"" Target=""_blank"">Learn more</BitLink>
    </BitStack>
</BitCard>";

    private readonly string example2RazorCode = @"
<BitCard Width=""14rem"" Height=""8rem"" ScrollableBody>
    <BitText Typography=""BitTypography.Body2"">
        Width, Height and ScrollableBody: this text is long enough to scroll inside the card
        instead of spilling out of it, so the layout around it stays where it is.
    </BitText>
</BitCard>

<BitCard MinWidth=""10rem"" MaxWidth=""18rem"">
    <BitText Typography=""BitTypography.Body2"">MinWidth and MaxWidth: never narrower than 10rem, never wider than 18rem.</BitText>
</BitCard>


<div style=""display:grid;grid-template-columns:repeat(auto-fit, minmax(10rem, 1fr));gap:1rem"">
    <BitCard FullSize>
        <BitText Typography=""BitTypography.Body2"">FullSize fills its grid cell.</BitText>
    </BitCard>
    <BitCard FullSize>
        <BitText Typography=""BitTypography.Body2"">So a card with more to say makes the whole row taller, and every card in the row follows it.</BitText>
    </BitCard>
    <BitCard FullSize>
        <BitText Typography=""BitTypography.Body2"">One height for all.</BitText>
    </BitCard>
</div>


<BitCard FullWidth>
    <BitText Typography=""BitTypography.Body2"">FullWidth: as wide as its container.</BitText>
</BitCard>";

    private readonly string example3RazorCode = @"
<BitCard Title=""bit BlazorUI"" Subtitle=""Native Blazor components"" Width=""18rem"">
    <BitText Typography=""BitTypography.Body2"">A title and a subtitle.</BitText>
</BitCard>

<BitCard Title=""Deployment"" Subtitle=""Succeeded 2 minutes ago"" IconName=""@BitIconName.Rocket"" HeadingLevel=""3"" Width=""18rem"">
    <BitText Typography=""BitTypography.Body2"">An icon, and a title that is a level-3 heading.</BitText>
</BitCard>

<BitCard Title=""Ada Lovelace"" Subtitle=""Author"" Width=""18rem"">
    <IconTemplate>
        <BitPersona Size=""BitPersonaSize.Size32"" HidePersonaDetails ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />
    </IconTemplate>
    <ChildContent>
        <BitText Typography=""BitTypography.Body2"">An IconTemplate keeps the title and subtitle.</BitText>
    </ChildContent>
</BitCard>

<BitCard Width=""18rem"">
    <HeaderTemplate>
        <BitPersona Size=""BitPersonaSize.Size32"" PrimaryText=""Ada Lovelace"" SecondaryText=""Author"" ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />
    </HeaderTemplate>
    <ChildContent>
        <BitText Typography=""BitTypography.Body2"">A HeaderTemplate replaces the icon, title and subtitle.</BitText>
    </ChildContent>
</BitCard>";

    private readonly string example4RazorCode = @"
<BitCard Title=""Weekly report"" Subtitle=""Updated an hour ago"" Width=""20rem"">
    <Actions>
        <BitButton Variant=""BitVariant.Text"" IconOnly IconName=""@BitIconName.More"" Title=""More"" />
    </Actions>
    <ChildContent>
        <BitText Typography=""BitTypography.Body2"">Actions beside the title, the footer under the body.</BitText>
    </ChildContent>
    <Footer>
        <BitButton Size=""BitSize.Small"">Open</BitButton>
        <BitButton Variant=""BitVariant.Text"" Size=""BitSize.Small"">Share</BitButton>
    </Footer>
</BitCard>

<BitCard Divider Title=""Weekly report"" Subtitle=""Updated an hour ago"" Width=""20rem"">
    <Actions>
        <BitButton Variant=""BitVariant.Text"" IconOnly IconName=""@BitIconName.More"" Title=""More"" />
    </Actions>
    <ChildContent>
        <BitText Typography=""BitTypography.Body2"">The same card with a Divider.</BitText>
    </ChildContent>
    <Footer>
        <BitButton Size=""BitSize.Small"">Open</BitButton>
        <BitButton Variant=""BitVariant.Text"" Size=""BitSize.Small"">Share</BitButton>
    </Footer>
</BitCard>

<BitCard Width=""16rem"">
    <FloatingActions>
        <BitButton Variant=""BitVariant.Text"" IconOnly Title=""@(isStarred ? ""Unstar"" : ""Star"")""
                   Color=""@(isStarred ? BitColor.Warning : BitColor.Tertiary)""
                   IconName=""@(isStarred ? BitIconName.FavoriteStarFill : BitIconName.FavoriteStar)""
                   OnClick=""() => isStarred = !isStarred"" />
    </FloatingActions>
    <ChildContent>
        <BitText Typography=""BitTypography.Body2"">The star floats over the corner. Starred: @isStarred</BitText>
    </ChildContent>
</BitCard>";
    private readonly string example4CsharpCode = @"
private bool isStarred;";

    private readonly string example5RazorCode = @"
<BitCard Title=""Mount Rainier"" Subtitle=""Washington, USA"" Width=""18rem""
         ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg"" ImageHeight=""9rem"" ImagePosition=""top"">
    <BitText Typography=""BitTypography.Body2"">ImageHeight crops it; ImagePosition keeps the top of the frame.</BitText>
</BitCard>

<BitCard Title=""Widescreen"" Subtitle=""A 16 / 9 cover"" Width=""18rem"" ImageLoading=""BitImageLoading.Lazy""
         ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg"" CoverRatio=""16 / 9"">
    <BitText Typography=""BitTypography.Body2"">CoverRatio keeps its ratio at any width.</BitText>
</BitCard>

<BitCard Reversed Title=""Caption first"" Subtitle=""A cover under the content"" Width=""18rem""
         ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img1.jpg"" CoverRatio=""16 / 9"">
    <BitText Typography=""BitTypography.Body2"">Reversed puts the picture last.</BitText>
</BitCard>

<BitCard Title=""Custom cover"" Subtitle=""Any markup you like"" Width=""18rem"">
    <Cover>
        <div style=""height:7rem;display:flex;align-items:center;justify-content:center;color:white;font-size:1.25rem;
                    background:linear-gradient(120deg, var(--bit-clr-pri), var(--bit-clr-inf))"">bit BlazorUI</div>
    </Cover>
    <ChildContent>
        <BitText Typography=""BitTypography.Body2"">The Cover template takes the place of the image.</BitText>
    </ChildContent>
</BitCard>";

    private readonly string example6RazorCode = @"
<BitCard Horizontal Width=""26rem"" Title=""Mount Rainier"" Subtitle=""Washington, USA""
         ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img2.jpg"">
    <BitText Typography=""BitTypography.Body2"">A thumbnail and a summary on one row.</BitText>
</BitCard>

<BitCard Horizontal Width=""26rem"" CoverWidth=""8rem"" Title=""Mount Rainier"" Subtitle=""A narrower cover""
         ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img3.jpg"">
    <BitText Typography=""BitTypography.Body2"">CoverWidth pins the thumbnail width.</BitText>
</BitCard>

<BitCard Horizontal Reversed Width=""26rem"" CoverWidth=""8rem"" Title=""Mount Rainier"" Subtitle=""A trailing cover""
         ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img1.jpg"">
    <BitText Typography=""BitTypography.Body2"">Reversed puts the thumbnail after the words.</BitText>
</BitCard>";

    private readonly string example7RazorCode = @"
<BitCard OnClick=""() => clickCount++"" Title=""Clickable"" Width=""14rem"">
    <Actions>
        <BitButton Variant=""BitVariant.Text"" IconOnly IconName=""@BitIconName.Refresh"" Title=""Reset"" OnClick=""() => clickCount = 0"" />
    </Actions>
    <ChildContent>
        <BitText Typography=""BitTypography.Body2"">Clicked @clickCount times.</BitText>
    </ChildContent>
</BitCard>

<BitCard OnClick=""() => clickCount++"" Disabled Title=""Disabled"" Width=""14rem"">
    <BitText Typography=""BitTypography.Body2"">Answers nothing.</BitText>
</BitCard>

<BitCard @bind-Selected=""isBackupsSelected"" Title=""Backups"" Subtitle=""$4 / month"" Width=""14rem"">
    <BitText Typography=""BitTypography.Body2"">Selected: @isBackupsSelected</BitText>
</BitCard>

<BitCard @bind-Selected=""isMonitoringSelected"" Title=""Monitoring"" Subtitle=""$6 / month"" Width=""14rem"">
    <BitText Typography=""BitTypography.Body2"">Selected: @isMonitoringSelected</BitText>
</BitCard>";
    private readonly string example7CsharpCode = @"
private int clickCount;
private bool isBackupsSelected = true;
private bool isMonitoringSelected;";

    private readonly string example8RazorCode = @"
<BitCard Href=""https://blazorui.bitplatform.dev"" Target=""_blank""
         Title=""bit BlazorUI"" Subtitle=""blazorui.bitplatform.dev"" IconName=""@BitIconName.Globe"" Width=""20rem"">
    <Actions>
        <BitButton Variant=""BitVariant.Text"" IconOnly Title=""@(isPinned ? ""Unpin"" : ""Pin"")""
                   IconName=""@(isPinned ? BitIconName.PinnedFill : BitIconName.Pinned)""
                   OnClick=""() => isPinned = !isPinned"" />
    </Actions>
    <ChildContent>
        <BitText Typography=""BitTypography.Body2"">The whole card is the link; the pin is not. Pinned: @isPinned</BitText>
    </ChildContent>
    <Footer>
        <BitLink Href=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"">GitHub</BitLink>
    </Footer>
</BitCard>";
    private readonly string example8CsharpCode = @"
private bool isPinned;";

    private readonly string example9RazorCode = @"
<BitToggle @bind-Value=""isLoading"" Label=""Loading"" />

<BitCard Loading=""isLoading"" Title=""Weekly report"" Subtitle=""Updated an hour ago"" Width=""20rem"">
    <BitText Typography=""BitTypography.Body2"">Once loaded, this is what the card had to say.</BitText>
</BitCard>

<BitCard Loading=""isLoading"" Title=""Custom placeholder"" Width=""20rem"">
    <LoadingTemplate>
        <BitShimmer Width=""100%"" Height=""4rem"" />
    </LoadingTemplate>
    <ChildContent>
        <BitText Typography=""BitTypography.Body2"">This one brought its own placeholder.</BitText>
    </ChildContent>
</BitCard>";
    private readonly string example9CsharpCode = @"
private bool isLoading = true;";

    private readonly string example10RazorCode = @"
<BitSlider @bind-Value=""elevation"" Min=""0"" Max=""24"" Step=""1"" Label=""Elevation"" />

<BitCard Elevation=""(int)elevation"" Title=""Elevation"" Subtitle=""@($""Level {(int)elevation}"")"" Width=""14rem"" />
<BitCard NoShadow Title=""NoShadow"" Subtitle=""A flat surface"" Width=""14rem"" />
<BitCard Hoverable Title=""Hoverable"" Subtitle=""Hover me"" Width=""14rem"" />";
    private readonly string example10CsharpCode = @"
private double elevation = 4;";

    private readonly string example11RazorCode = @"
<BitCard Outlined Title=""Outlined"" Width=""12rem"" />
<BitCard Border=""BitColorKind.Tertiary"" NoShadow Title=""Border"" Subtitle=""Tertiary, no shadow"" Width=""12rem"" />
<BitCard Square Outlined Title=""Square"" Width=""12rem"" />
<BitCard NoPadding Outlined Width=""12rem"">
    <BitText Typography=""BitTypography.Body2"">NoPadding: the content touches the edges.</BitText>
</BitCard>


<div style=""padding:1rem;background:gray"">
    <BitCard Background=""BitColorKind.Primary"" Title=""Primary"" Width=""12rem"" />
    <BitCard Background=""BitColorKind.Secondary"" Title=""Secondary"" Subtitle=""The default"" Width=""12rem"" />
    <BitCard Background=""BitColorKind.Tertiary"" Title=""Tertiary"" Width=""12rem"" />
    <BitCard Background=""BitColorKind.Transparent"" Outlined Title=""Transparent"" Width=""12rem"" />
</div>";

    private readonly string example12RazorCode = @"
<ul role=""list"" aria-label=""Latest guides"" style=""list-style:none;margin:0;padding:0;display:flex;flex-wrap:wrap;gap:1rem"">
    <li>
        <BitCard Href=""/theming"" Title=""Theming"" Subtitle=""5 min read"" HeadingLevel=""3"" Width=""16rem"">
            <BitText Typography=""BitTypography.Body2"">Tokens, presets and dark mode.</BitText>
        </BitCard>
    </li>
    <li>
        <BitCard Href=""/iconography"" Title=""Iconography"" Subtitle=""3 min read"" HeadingLevel=""3"" Width=""16rem"">
            <BitText Typography=""BitTypography.Body2"">Built-in and external icons.</BitText>
        </BitCard>
    </li>
    <li>
        <BitCard AriaLabel=""Release notes"" Title=""Release notes"" HeadingLevel=""3"" Loading Width=""16rem"" />
    </li>
</ul>";

    private readonly string example13RazorCode = @"
<div style=""--bit-Card-radius: 1rem; --bit-Card-padding: 1.25rem; --bit-Card-shadow: none; --bit-Card-border-width: 1px;
            --bit-Card-border-color: var(--bit-clr-brd-sec); --bit-Card-hover-shadow: var(--bit-shd-md);"">
    <BitCard Hoverable Title=""Soft"" Subtitle=""Rounder, flat, outlined"" Width=""14rem"" />
    <BitCard Hoverable Title=""Same look"" Subtitle=""From the same ancestor"" Width=""14rem"" />
    <BitCard Hoverable Border=""BitColorKind.Primary"" Title=""Border wins"" Subtitle=""A parameter beats the variable"" Width=""14rem"" />
</div>


<div style=""--bit-Card-background: #1e1b4b; --bit-Card-color: #e0e7ff; --bit-Card-subtitle-color: #a5b4fc; --bit-Card-divider-color: #4338ca;
            --bit-Card-selected-color: #fbbf24; --bit-Card-focus-color: #fbbf24; --bit-Card-title-font-weight: 700;
            --bit-Card-hover-background: #312e81; --bit-Card-active-background: #3730a3;"">
    <BitCard Divider Title=""Brand"" Subtitle=""Custom colors"" Width=""14rem"">
        <ChildContent>
            <BitText Typography=""BitTypography.Body2"">A body under a divider.</BitText>
        </ChildContent>
        <Footer>
            <BitText Typography=""BitTypography.Caption1"">A footer</BitText>
        </Footer>
    </BitCard>
    <BitCard @bind-Selected=""isBrandSelected"" Title=""Selectable"" Subtitle=""Amber ring, indigo hover"" Width=""14rem"" />
</div>


<BitCard CoverOverlay Title=""Olympic National Park"" Subtitle=""Washington, USA"" Width=""18rem"" Height=""14rem""
         ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img2.jpg""
         Style=""--bit-Card-scrim: linear-gradient(rgb(0 0 0 / 0.7), rgb(0 0 0 / 0.3)); --bit-Card-color: white; --bit-Card-subtitle-color: rgb(255 255 255 / 0.85);"">
    <ChildContent>
        <BitText Typography=""BitTypography.Body2"">A CoverOverlay card, read against the scrim set on its own Style.</BitText>
    </ChildContent>
    <Footer>
        <BitButton Size=""BitSize.Small"">Explore</BitButton>
    </Footer>
</BitCard>";
    private readonly string example13CsharpCode = @"
private bool isBrandSelected = true;";

    private readonly string example14RazorCode = @"
<BitParams Parameters=""@cardParams"">
    <BitCard Title=""Inbox"" Subtitle=""12 unread"" IconName=""@BitIconName.Mail"">
        <BitText Typography=""BitTypography.Body2"">Outlined, small, divided.</BitText>
    </BitCard>
    <BitCard Title=""Calendar"" Subtitle=""3 events today"" IconName=""@BitIconName.Calendar"">
        <BitText Typography=""BitTypography.Body2"">The same, from BitParams.</BitText>
    </BitCard>
    <BitCard Title=""Tasks"" Subtitle=""5 due"" IconName=""@BitIconName.TaskManager"" Size=""BitSize.Large"">
        <BitText Typography=""BitTypography.Body2"">Keeps its own Size.</BitText>
    </BitCard>
</BitParams>";
    private readonly string example14CsharpCode = @"
private readonly BitCardParams[] cardParams =
[
    new()
    {
        Outlined = true,
        Divider = true,
        Size = BitSize.Small,
        Width = ""14rem"",
        HeadingLevel = 3,
    }
];";

    private readonly string example15RazorCode = @"
@foreach (var color in semanticColors)
{
    <BitCard Color=""color"" Variant=""BitVariant.Fill"" Title=""@color.ToString()"" Subtitle=""Fill"" Width=""12rem"" />
    <BitCard Color=""color"" Variant=""BitVariant.Outline"" Title=""@color.ToString()"" Subtitle=""Outline"" Width=""12rem"" />
    <BitCard Color=""color"" Variant=""BitVariant.Text"" Title=""@color.ToString()"" Subtitle=""Text"" Width=""12rem"" />
}


<div style=""padding:1rem;background:var(--bit-clr-fg-sec)"">
    <BitCard Color=""BitColor.PrimaryBackground"" Title=""PrimaryBackground"" Width=""14rem"" />
    <BitCard Color=""BitColor.SecondaryBackground"" Title=""SecondaryBackground"" Width=""14rem"" />
    <BitCard Color=""BitColor.TertiaryBackground"" Title=""TertiaryBackground"" Width=""14rem"" />
</div>

<BitCard Color=""BitColor.PrimaryForeground"" Title=""PrimaryForeground"" Width=""14rem"" />
<BitCard Color=""BitColor.SecondaryForeground"" Title=""SecondaryForeground"" Width=""14rem"" />
<BitCard Color=""BitColor.TertiaryForeground"" Title=""TertiaryForeground"" Width=""14rem"" />

<BitCard Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Outline"" Title=""PrimaryBorder"" Width=""14rem"" />
<BitCard Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Outline"" Title=""SecondaryBorder"" Width=""14rem"" />
<BitCard Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Outline"" Title=""TertiaryBorder"" Width=""14rem"" />


<BitCard Disabled Color=""BitColor.Primary"" Variant=""BitVariant.Fill"" Title=""Primary"" Subtitle=""Fill"" Width=""12rem"" />
<BitCard Disabled Color=""BitColor.Success"" Variant=""BitVariant.Outline"" Title=""Success"" Subtitle=""Outline"" Width=""12rem"" />
<BitCard Disabled Color=""BitColor.Error"" Variant=""BitVariant.Text"" Title=""Error"" Subtitle=""Text"" Width=""12rem"" />";
    private readonly string example15CsharpCode = @"
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
];";

    private readonly string example16RazorCode = @"
<!-- FontAwesome -->
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitCard Icon=""@(""fa-solid fa-house"")"" Title=""House"" Subtitle=""fa-solid fa-house"" Width=""16rem"" />
<BitCard Icon=""@BitIconInfo.Fa(""brands github"")"" Title=""GitHub"" Subtitle=""fa-brands fa-github"" Width=""16rem"" />
<BitCard Icon=""@BitIconInfo.Fa(""solid rocket"")"" Title=""Rocket"" Subtitle=""fa-solid fa-rocket"" Width=""16rem"" />


<!-- Bootstrap Icons -->
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitCard Icon=""@(""bi bi-house-fill"")"" Title=""House"" Subtitle=""bi bi-house-fill"" Width=""16rem"" />
<BitCard Icon=""@BitIconInfo.Bi(""github"")"" Title=""GitHub"" Subtitle=""bi bi-github"" Width=""16rem"" />
<BitCard Icon=""@BitIconInfo.Bi(""gear-fill"")"" Title=""Gear"" Subtitle=""bi bi-gear-fill"" Width=""16rem"" />";

    private readonly string example17RazorCode = @"
<BitCard Size=""BitSize.Small"" Title=""Small"" Subtitle=""A tight card"" IconName=""@BitIconName.Album"" Width=""16rem"">
    <BitText Typography=""BitTypography.Body2"">Small padding, small type.</BitText>
</BitCard>

<BitCard Size=""BitSize.Medium"" Title=""Medium"" Subtitle=""The default"" IconName=""@BitIconName.Album"" Width=""16rem"">
    <BitText Typography=""BitTypography.Body2"">Medium padding, medium type.</BitText>
</BitCard>

<BitCard Size=""BitSize.Large"" Title=""Large"" Subtitle=""A roomy card"" IconName=""@BitIconName.Album"" Width=""16rem"">
    <BitText Typography=""BitTypography.Body2"">Large padding, large type.</BitText>
</BitCard>";

    private readonly string example18RazorCode = @"
<style>
    .custom-class {
        border-radius: 0.25rem;
        box-shadow: aqua 0 0 0.5rem;
    }

    .custom-root {
        border: 1px solid mediumpurple;
        box-shadow: mediumpurple 0 0 0.5rem;
    }

    .custom-title {
        color: mediumpurple;
    }

    .custom-icon {
        color: mediumpurple;
    }
</style>


<BitCard Style=""border: 2px solid mediumpurple; box-shadow: mediumpurple 0 0 0.5rem;"" Width=""16rem"">
    <BitText Typography=""BitTypography.Body2"">Styled card</BitText>
</BitCard>

<BitCard Class=""custom-class"" Width=""16rem"">
    <BitText Typography=""BitTypography.Body2"">Classed card</BitText>
</BitCard>


<BitCard Title=""Styles"" Subtitle=""Per-part inline styles"" IconName=""@BitIconName.Color"" Width=""18rem""
         Styles=""@(new() { Root = ""border: 1px solid darkcyan"",
                           Header = ""border-bottom: 1px solid darkcyan; padding-bottom: 0.5rem"",
                           Title = ""color: darkcyan"",
                           Icon = ""color: darkcyan"" })"">
    <BitText Typography=""BitTypography.Body2"">Every part can be styled on its own.</BitText>
</BitCard>

<BitCard Title=""Classes"" Subtitle=""Per-part CSS classes"" IconName=""@BitIconName.Color"" Width=""18rem""
         Classes=""@(new() { Root = ""custom-root"", Title = ""custom-title"", Icon = ""custom-icon"" })"">
    <BitText Typography=""BitTypography.Body2"">And every part can take a class.</BitText>
</BitCard>";

    private readonly string example19RazorCode = @"
<BitCard Dir=""BitDir.Rtl"" Title=""کارت"" Subtitle=""یک زیرعنوان"" IconName=""@BitIconName.Album"" Width=""20rem"">
    <Actions>
        <BitButton Variant=""BitVariant.Text"" IconOnly IconName=""@BitIconName.More"" Title=""بیشتر"" />
    </Actions>
    <ChildContent>
        <BitText Typography=""BitTypography.Body2"">بیت بلیزور یو آی، کامپوننت‌های بومی، قابل تنظیم و ...</BitText>
    </ChildContent>
    <Footer>
        <BitButton Size=""BitSize.Small"">باز کردن</BitButton>
    </Footer>
</BitCard>";
}
