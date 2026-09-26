namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.DropMenu;

public partial class BitDropMenuDemo
{
    private readonly string example1RazorCode = @"
<BitDropMenu Text=""Quick settings"">
    <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
        <BitToggle Label=""Notifications"" />
        <BitCheckbox Label=""Weekly digest"" />
        <BitSeparator />
        <BitButton Size=""BitSize.Small"">Save</BitButton>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Disabled"" IsEnabled=""false"">
    <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
        <BitToggle Label=""Notifications"" />
        <BitCheckbox Label=""Weekly digest"" />
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""FullWidth"" FullWidth>
    <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
        <BitToggle Label=""Notifications"" />
        <BitCheckbox Label=""Weekly digest"" />
    </BitStack>
</BitDropMenu>";

    private readonly string example2RazorCode = @"
<BitStack Horizontal Wrap Gap=""1rem"" FitHeight>
    <BitDropMenu Text=""Share"" IconName=""@BitIconName.Share"">
        <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Mail"">Send by email</BitButton>
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Link"">Copy link</BitButton>
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""Filters"" ChevronDownIconName=""@BitIconName.DoubleChevronDown"">
        <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
            <BitCheckbox Label=""Active"" />
            <BitCheckbox Label=""Archived"" />
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""Jane Cooper"" IconName=""@BitIconName.Contact"" NoChevron>
        <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Settings"">Settings</BitButton>
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.SignOut"">Sign out</BitButton>
        </BitStack>
    </BitDropMenu>
    <BitDropMenu IconName=""@BitIconName.More"" NoChevron AriaLabel=""More actions"" Title=""More actions"">
        <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Edit"">Rename</BitButton>
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Delete"">Delete</BitButton>
        </BitStack>
    </BitDropMenu>
</BitStack>";

    private readonly string example3RazorCode = @"
<BitDropMenu>
    <Template>
        <div style=""display:flex;gap:10px;align-items:center;"">
            <BitIcon IconName=""@BitIconName.Airplane"" Color=""BitColor.Tertiary"" />
            <span>Flight BA 117</span>
            <BitRippleLoading CustomSize=""20"" Color=""BitColor.Tertiary"" />
        </div>
    </Template>
    <Body>
        <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
            <BitText Typography=""BitTypography.Body2"">Departs 18:40 from gate B22</BitText>
            <BitSeparator />
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Info"">Details</BitButton>
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Calendar"">Change the date</BitButton>
        </BitStack>
    </Body>
</BitDropMenu>";

    private readonly string example4RazorCode = @"
<BitChoiceGroup @bind-Value=""variant"" Horizontal
                Label=""Variant""
                TItem=""BitChoiceGroupOption<BitVariant>"" TValue=""BitVariant"">
    <BitChoiceGroupOption Text=""Fill"" Value=""BitVariant.Fill"" />
    <BitChoiceGroupOption Text=""Outline"" Value=""BitVariant.Outline"" />
    <BitChoiceGroupOption Text=""Text"" Value=""BitVariant.Text"" />
</BitChoiceGroup>

<BitStack Horizontal Wrap Gap=""1rem"" FitHeight>
    <BitDropMenu Text=""Primary"" Variant=""variant"" Color=""BitColor.Primary"">
        <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Edit"">Rename</BitButton>
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Copy"">Duplicate</BitButton>
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""Error"" Variant=""variant"" Color=""BitColor.Error"">
        <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Edit"">Rename</BitButton>
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Copy"">Duplicate</BitButton>
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""Transparent"" Variant=""variant"" Color=""BitColor.Primary"" Transparent>
        <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Edit"">Rename</BitButton>
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Copy"">Duplicate</BitButton>
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""Disabled"" Variant=""variant"" Color=""BitColor.Primary"" IsEnabled=""false"">
        <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Edit"">Rename</BitButton>
        </BitStack>
    </BitDropMenu>
</BitStack>";
    private readonly string example4CsharpCode = @"
private BitVariant variant = BitVariant.Fill;";

    private readonly string example5RazorCode = @"
<BitToggle @bind-Value=""isLoading"" Label=""IsLoading"" />

<BitStack Horizontal Wrap Gap=""1rem"" FitHeight>
    <BitDropMenu Text=""Cloud sync"" IconName=""@BitIconName.Cloud"" IsLoading=""isLoading"">
        <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
            <BitText Typography=""BitTypography.Body2"">Last synced 5 minutes ago</BitText>
            <BitToggle Label=""Sync on cellular data"" />
            <BitButton Size=""BitSize.Small"" IconName=""@BitIconName.Download"">Sync now</BitButton>
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""LazyRender"" LazyRender OnOpen=""@(() => lazyOpenedAt ??= DateTime.Now.ToString(""T""))"">
        <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
            <BitText Typography=""BitTypography.Body2"">Rendered on the first opening, at @lazyOpenedAt</BitText>
            <BitCheckbox Label=""Keeps its state after a close"" />
        </BitStack>
    </BitDropMenu>
</BitStack>";
    private readonly string example5CsharpCode = @"
private bool isLoading;
private string? lazyOpenedAt;";

    private readonly string example6RazorCode = @"
<BitChoiceGroup @bind-Value=""backgroundColorKind"" Horizontal
                Label=""Background""
                TItem=""BitChoiceGroupOption<BitColorKind>"" TValue=""BitColorKind"">
    <BitChoiceGroupOption Text=""Primary"" Value=""BitColorKind.Primary"" />
    <BitChoiceGroupOption Text=""Secondary"" Value=""BitColorKind.Secondary"" />
    <BitChoiceGroupOption Text=""Tertiary"" Value=""BitColorKind.Tertiary"" />
    <BitChoiceGroupOption Text=""Transparent"" Value=""BitColorKind.Transparent"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""borderColorKind"" Horizontal
                Label=""Border""
                TItem=""BitChoiceGroupOption<BitColorKind>"" TValue=""BitColorKind"">
    <BitChoiceGroupOption Text=""Primary"" Value=""BitColorKind.Primary"" />
    <BitChoiceGroupOption Text=""Secondary"" Value=""BitColorKind.Secondary"" />
    <BitChoiceGroupOption Text=""Tertiary"" Value=""BitColorKind.Tertiary"" />
    <BitChoiceGroupOption Text=""Transparent"" Value=""BitColorKind.Transparent"" />
</BitChoiceGroup>

<BitStack Horizontal Wrap Gap=""1rem"" FitHeight>
    <BitDropMenu Text=""Background"" Background=""backgroundColorKind"">
        <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
            <BitToggle Label=""Notifications"" />
            <BitCheckbox Label=""Weekly digest"" />
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""Border, NoShadow"" Border=""borderColorKind"" NoShadow>
        <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
            <BitToggle Label=""Notifications"" />
            <BitCheckbox Label=""Weekly digest"" />
        </BitStack>
    </BitDropMenu>
</BitStack>";
    private readonly string example6CsharpCode = @"
private BitColorKind backgroundColorKind = BitColorKind.Primary;
private BitColorKind borderColorKind = BitColorKind.Primary;";

    private readonly string example7RazorCode = @"
<BitDropMenu Text=""A rather wide drop menu button"" MatchWidth>
    <BitText Style=""padding:0.5rem"">MatchWidth</BitText>
</BitDropMenu>

<BitStack Horizontal Wrap Gap=""1rem"" FitHeight>
    <BitDropMenu Text=""Width"" Width=""16rem"">
        <BitText Style=""padding:0.5rem"">A callout 16rem wide</BitText>
    </BitDropMenu>
    <BitDropMenu Text=""MinWidth"" MinWidth=""16rem"">
        <BitText Style=""padding:0.5rem"">Short</BitText>
    </BitDropMenu>
    <BitDropMenu Text=""MaxWidth"" MaxWidth=""16rem"">
        <BitText Style=""padding:0.5rem"">A rather long piece of content that wraps instead of stretching the callout across the screen</BitText>
    </BitDropMenu>
    <BitDropMenu Text=""MaxHeight"" MaxHeight=""10rem"">
        <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
            @for (var i = 1; i <= 20; i++)
            {
                <BitText>Item @i</BitText>
            }
        </BitStack>
    </BitDropMenu>
</BitStack>";

    private readonly string example8RazorCode = @"
<BitChoiceGroup @bind-Value=""dropDirection"" Horizontal
                Label=""DropDirection""
                TItem=""BitChoiceGroupOption<BitDropDirection>"" TValue=""BitDropDirection"">
    <BitChoiceGroupOption Text=""TopAndBottom"" Value=""BitDropDirection.TopAndBottom"" />
    <BitChoiceGroupOption Text=""All"" Value=""BitDropDirection.All"" />
</BitChoiceGroup>

<BitDropMenu Text=""Categories"" DropDirection=""dropDirection"">
    <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
        @for (var i = 1; i <= 15; i++)
        {
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Tag"">Category @i</BitButton>
        }
    </BitStack>
</BitDropMenu>

<BitChoiceGroup @bind-Value=""alignment"" Horizontal
                Label=""Alignment""
                TItem=""BitChoiceGroupOption<BitCalloutAlignment>"" TValue=""BitCalloutAlignment"">
    <BitChoiceGroupOption Text=""Start"" Value=""BitCalloutAlignment.Start"" />
    <BitChoiceGroupOption Text=""Center"" Value=""BitCalloutAlignment.Center"" />
    <BitChoiceGroupOption Text=""End"" Value=""BitCalloutAlignment.End"" />
</BitChoiceGroup>

<BitDropMenu Text=""Account"" IconName=""@BitIconName.Contact"" Alignment=""alignment"" FullWidth>
    <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
        <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Settings"">Settings</BitButton>
        <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.SignOut"">Sign out</BitButton>
    </BitStack>
</BitDropMenu>";
    private readonly string example8CsharpCode = @"
private BitDropDirection dropDirection = BitDropDirection.TopAndBottom;
private BitCalloutAlignment alignment = BitCalloutAlignment.Start;";

    private readonly string example9RazorCode = @"
<BitChoiceGroup @bind-Value=""panelPosition"" Horizontal
                Label=""PanelPosition""
                TItem=""BitChoiceGroupOption<BitPanelPosition>"" TValue=""BitPanelPosition"">
    <BitChoiceGroupOption Text=""Start"" Value=""BitPanelPosition.Start"" />
    <BitChoiceGroupOption Text=""End"" Value=""BitPanelPosition.End"" />
    <BitChoiceGroupOption Text=""Top"" Value=""BitPanelPosition.Top"" />
    <BitChoiceGroupOption Text=""Bottom"" Value=""BitPanelPosition.Bottom"" />
</BitChoiceGroup>

<BitDropMenu Text=""Responsive"" Responsive PanelPosition=""panelPosition"" ScrollContainerId=""responsive-list"">
    <div id=""responsive-list"" style=""max-height:60vh;overflow:auto"">
        <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
            <BitText Typography=""BitTypography.Subtitle1"">Swipe to dismiss</BitText>
            <BitCheckbox Label=""Active"" />
            <BitCheckbox Label=""Archived"" />
            <BitCheckbox Label=""Draft"" />
            <BitCheckbox Label=""Scheduled"" />
            <BitCheckbox Label=""Deleted"" />
        </BitStack>
    </div>
</BitDropMenu>";
    private readonly string example9CsharpCode = @"
private BitPanelPosition panelPosition = BitPanelPosition.End;";

    private readonly string example10RazorCode = @"
<BitStack Horizontal Wrap Gap=""1rem"" FitHeight>
    <BitDropMenu Text=""Products"" OpenOnHover IconName=""@BitIconName.Globe"">
        <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
            <BitLink Href=""/components/dropmenu"">Platform</BitLink>
            <BitLink Href=""/components/dropmenu"">Solutions</BitLink>
            <BitLink Href=""/components/dropmenu"">Pricing</BitLink>
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""With delays"" OpenOnHover HoverOpenDelay=""400"" HoverCloseDelay=""600"" IconName=""@BitIconName.Clock"">
        <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
            <BitText Typography=""BitTypography.Body2"">Opens after 400ms, closes after 600ms</BitText>
            <BitLink Href=""/components/dropmenu"">Documentation</BitLink>
            <BitLink Href=""/components/dropmenu"">Release notes</BitLink>
        </BitStack>
    </BitDropMenu>
</BitStack>";

    private readonly string example11RazorCode = @"
<BitStack Horizontal Wrap Gap=""0.5rem"" FitHeight>
    <BitButton OnClick=""() => isOpen = !isOpen"">@(isOpen ? ""Close"" : ""Open"") the bound one</BitButton>
    <BitButton OnClick=""() => dropMenuRef?.Toggle()"">Toggle through the reference</BitButton>
</BitStack>

<BitStack Horizontal Wrap Gap=""1rem"" FitHeight>
    <BitDropMenu @bind-IsOpen=""isOpen"" Text=""@($""IsOpen: {isOpen}"")"">
        <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
            <BitCheckbox Label=""Active"" />
            <BitCheckbox Label=""Archived"" />
        </BitStack>
    </BitDropMenu>
    <BitDropMenu @ref=""dropMenuRef"" Text=""Controlled by the reference"">
        <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Edit"">Rename</BitButton>
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Copy"">Duplicate</BitButton>
        </BitStack>
    </BitDropMenu>
</BitStack>

<BitCheckbox @bind-Value=""mountDefaultIsOpen"" Label=""Render a drop menu that starts out open"" />

@if (mountDefaultIsOpen)
{
    <BitDropMenu Text=""DefaultIsOpen"" DefaultIsOpen>
        <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Settings"">Settings</BitButton>
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.SignOut"">Sign out</BitButton>
        </BitStack>
    </BitDropMenu>
}";
    private readonly string example11CsharpCode = @"
private bool isOpen;
private bool mountDefaultIsOpen;
private BitDropMenu? dropMenuRef;";

    private readonly string example12RazorCode = @"
<BitDropMenu Text=""@($""Click me ({clickCounter})"")""
             OnClick=""() => clickCounter++""
             OnOpen=""() => openCounter++""
             OnDismiss=""() => dismissCounter++"">
    <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
        <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Edit"">Rename</BitButton>
        <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Copy"">Duplicate</BitButton>
    </BitStack>
</BitDropMenu>

<BitText>Clicked: @clickCounter, Opened: @openCounter, Dismissed: @dismissCounter</BitText>";
    private readonly string example12CsharpCode = @"
private int clickCounter;
private int openCounter;
private int dismissCounter;";

    private readonly string example13RazorCode = @"
<BitStack Horizontal Wrap Gap=""1rem"" FitHeight>
    <BitDropMenu Text=""@($""AutoClose ({autoCloseAction})"")"" AutoClose IconName=""@BitIconName.More"">
        <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => autoCloseAction = ""Renamed"")"">Rename</BitButton>
            <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => autoCloseAction = ""Duplicated"")"">Duplicate</BitButton>
            <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => autoCloseAction = ""Deleted"")"">Delete</BitButton>
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""Without AutoClose"" IconName=""@BitIconName.Filter"">
        <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
            <BitCheckbox Label=""Active"" />
            <BitCheckbox Label=""Archived"" />
        </BitStack>
    </BitDropMenu>
</BitStack>";
    private readonly string example13CsharpCode = @"
private string autoCloseAction = ""none"";";

    private readonly string example14RazorCode = @"
<BitStack Horizontal Wrap Gap=""1rem"" FitHeight>
    <BitDropMenu Text=""AutoFocus"" AutoFocus>
        <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
            <BitTextField Label=""Search"" />
            <BitButton>Go</BitButton>
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""TrapFocus"" TrapFocus>
        <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
            <BitTextField Label=""Email"" />
            <BitDropdown Label=""Frequency"" TItem=""BitDropdownOption<string>"" TValue=""string"" DefaultValue=""@(""weekly"")"">
                <BitDropdownOption Text=""Daily"" Value=""@(""daily"")"" />
                <BitDropdownOption Text=""Weekly"" Value=""@(""weekly"")"" />
                <BitDropdownOption Text=""Monthly"" Value=""@(""monthly"")"" />
            </BitDropdown>
            <BitButton>Subscribe</BitButton>
        </BitStack>
    </BitDropMenu>
    <BitDropMenu IconName=""@BitIconName.Filter"" NoChevron
                 AriaLabel=""Filters""
                 AriaDescription=""Narrows the list below""
                 Title=""Filters"">
        <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
            <BitCheckbox Label=""Active"" />
            <BitCheckbox Label=""Archived"" />
        </BitStack>
    </BitDropMenu>
</BitStack>";

    private readonly string example15RazorCode = @"
<BitParams Parameters=""toolbarDropMenuParams"">
    <BitStack Horizontal Wrap Gap=""0.5rem"" FitHeight>
        <BitDropMenu Text=""File"">
            <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
                <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Add"">New</BitButton>
                <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Save"">Save</BitButton>
            </BitStack>
        </BitDropMenu>
        <BitDropMenu Text=""Edit"">
            <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
                <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Undo"">Undo</BitButton>
                <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Redo"">Redo</BitButton>
            </BitStack>
        </BitDropMenu>
        <BitDropMenu Text=""Share"" Variant=""BitVariant.Fill"">
            <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
                <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Mail"">Email</BitButton>
                <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Link"">Copy link</BitButton>
            </BitStack>
        </BitDropMenu>
    </BitStack>
</BitParams>";
    private readonly string example15CsharpCode = @"
private readonly BitDropMenuParams[] toolbarDropMenuParams =
[
    new()
    {
        Size = BitSize.Small,
        Color = BitColor.Primary,
        Variant = BitVariant.Outline,
        AutoClose = true
    }
];";

    private readonly string example16RazorCode = @"
<BitChoiceGroup @bind-Value=""color"" Horizontal
                Label=""Color""
                TItem=""BitChoiceGroupOption<BitColor>"" TValue=""BitColor"">
    <BitChoiceGroupOption Text=""Primary"" Value=""BitColor.Primary"" />
    <BitChoiceGroupOption Text=""Secondary"" Value=""BitColor.Secondary"" />
    <BitChoiceGroupOption Text=""Tertiary"" Value=""BitColor.Tertiary"" />
    <BitChoiceGroupOption Text=""Info"" Value=""BitColor.Info"" />
    <BitChoiceGroupOption Text=""Success"" Value=""BitColor.Success"" />
    <BitChoiceGroupOption Text=""Warning"" Value=""BitColor.Warning"" />
    <BitChoiceGroupOption Text=""SevereWarning"" Value=""BitColor.SevereWarning"" />
    <BitChoiceGroupOption Text=""Error"" Value=""BitColor.Error"" />
    <BitChoiceGroupOption Text=""PrimaryBackground"" Value=""BitColor.PrimaryBackground"" />
    <BitChoiceGroupOption Text=""SecondaryBackground"" Value=""BitColor.SecondaryBackground"" />
    <BitChoiceGroupOption Text=""TertiaryBackground"" Value=""BitColor.TertiaryBackground"" />
    <BitChoiceGroupOption Text=""PrimaryForeground"" Value=""BitColor.PrimaryForeground"" />
    <BitChoiceGroupOption Text=""SecondaryForeground"" Value=""BitColor.SecondaryForeground"" />
    <BitChoiceGroupOption Text=""TertiaryForeground"" Value=""BitColor.TertiaryForeground"" />
    <BitChoiceGroupOption Text=""PrimaryBorder"" Value=""BitColor.PrimaryBorder"" />
    <BitChoiceGroupOption Text=""SecondaryBorder"" Value=""BitColor.SecondaryBorder"" />
    <BitChoiceGroupOption Text=""TertiaryBorder"" Value=""BitColor.TertiaryBorder"" />
</BitChoiceGroup>

<BitStack Horizontal Wrap Gap=""1rem"" FitHeight>
    <BitDropMenu Text=""@color.ToString()"" Color=""color"">
        <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Settings"">Settings</BitButton>
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.SignOut"">Sign out</BitButton>
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""Outline"" Color=""color"" Variant=""BitVariant.Outline"">
        <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Settings"">Settings</BitButton>
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.SignOut"">Sign out</BitButton>
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""Text"" Color=""color"" Variant=""BitVariant.Text"">
        <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Settings"">Settings</BitButton>
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.SignOut"">Sign out</BitButton>
        </BitStack>
    </BitDropMenu>
</BitStack>";
    private readonly string example16CsharpCode = @"
private BitColor color = BitColor.Primary;";

    private readonly string example17RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />
<BitStack Horizontal Wrap Gap=""1rem"" FitHeight>
    <BitDropMenu Text=""House"" Icon=""@(""fa-solid fa-house"")"">
        <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
            <BitLink Href=""/components/dropmenu"">Home</BitLink>
            <BitLink Href=""/components/dropmenu"">Dashboard</BitLink>
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""GitHub"" Icon=""@BitIconInfo.Fa(""fa-brands fa-github"")"">
        <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Copy"">Clone</BitButton>
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Download"">Download ZIP</BitButton>
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""Gear"" Icon=""@BitIconInfo.Bi(""gear-fill"")"" ChevronDownIcon=""@BitIconInfo.Bi(""chevron-down"")"">
        <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
            <BitToggle Label=""Dark theme"" />
            <BitToggle Label=""Compact rows"" />
        </BitStack>
    </BitDropMenu>
</BitStack>";

    private readonly string example18RazorCode = @"
<BitStack Horizontal Wrap Gap=""1rem"" FitHeight VerticalAlign=""BitAlignment.Center"">
    <BitDropMenu Text=""Small"" Size=""BitSize.Small"" IconName=""@BitIconName.Filter"">
        <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
            <BitCheckbox Label=""Active"" />
            <BitCheckbox Label=""Archived"" />
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""Medium"" Size=""BitSize.Medium"" IconName=""@BitIconName.Filter"">
        <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
            <BitCheckbox Label=""Active"" />
            <BitCheckbox Label=""Archived"" />
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""Large"" Size=""BitSize.Large"" IconName=""@BitIconName.Filter"">
        <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
            <BitCheckbox Label=""Active"" />
            <BitCheckbox Label=""Archived"" />
        </BitStack>
    </BitDropMenu>
</BitStack>";

    private readonly string example19RazorCode = @"
<style>
    .custom-class {
        border-radius: 1rem;
        border-color: blueviolet;
        transition: background-color 1s;
        background: linear-gradient(90deg, magenta, transparent) blue;
    }

    .custom-class:hover {
        border-color: magenta;
        background-color: magenta;
    }

    .custom-root {
        color: aqua;
        min-width: 7.2rem;
        font-weight: bold;
        border-color: aqua;
        border-radius: 1rem;
        box-shadow: aqua 0 0 0.5rem;
    }

    .custom-root:hover {
        background-color: gray;
    }

    .custom-text {
        text-shadow: tomato 0 0 0.5rem;
    }

    .custom-chevron {
        color: tomato;
    }

    .custom-opened {
        color: green;
    }
</style>

<BitStack Horizontal Wrap Gap=""1rem"" FitHeight>
    <BitDropMenu Text=""Styled"" Style=""background-color: transparent; border-color: blueviolet; color: blueviolet;"">
        <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Edit"">Rename</BitButton>
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Copy"">Duplicate</BitButton>
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""Classed"" Class=""custom-class"">
        <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Edit"">Rename</BitButton>
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Copy"">Duplicate</BitButton>
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""Styles""
                 Styles=""@(new() { Root = ""background-color: peachpuff; border-color: peachpuff;"",
                                   Text = ""color: tomato; font-weight: bold;"",
                                   Callout = ""border: 2px solid tomato;"",
                                   Opened = ""border-color: tomato; background-color: goldenrod;"" })"">
        <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Edit"">Rename</BitButton>
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Copy"">Duplicate</BitButton>
        </BitStack>
    </BitDropMenu>
    <BitDropMenu Text=""Classes""
                 Classes=""@(new() { Root = ""custom-root"",
                                    Text = ""custom-text"",
                                    ChevronDown = ""custom-chevron"",
                                    Opened = ""custom-opened"" })"">
        <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Edit"">Rename</BitButton>
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Copy"">Duplicate</BitButton>
        </BitStack>
    </BitDropMenu>
</BitStack>

<BitStack Horizontal Wrap Gap=""1rem"" FitHeight>
    <BitDropMenu Text=""Pill, soft callout"" IconName=""@BitIconName.Emoji2""
                 Style=""--bit-DropMenu-radius: 999px;
                        --bit-DropMenu-callout-radius: 1rem;
                        --bit-DropMenu-callout-padding: 0.5rem;
                        --bit-DropMenu-callout-background: var(--bit-clr-bg-sec);"">
        <BitStack Gap=""0.25rem"">
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Edit"">Rename</BitButton>
            <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Copy"">Duplicate</BitButton>
        </BitStack>
    </BitDropMenu>
    <div style=""display:flex;gap:0.5rem;
                --bit-DropMenu-min-height: 2.5rem;
                --bit-DropMenu-active-color: var(--bit-clr-pri-text);
                --bit-DropMenu-active-background: var(--bit-clr-pri);"">
        <BitDropMenu Text=""Inherited"" Variant=""BitVariant.Outline"">
            <BitText Style=""padding:0.5rem"">Taller, and primary while open</BitText>
        </BitDropMenu>
        <BitDropMenu Text=""From the ancestor"" Variant=""BitVariant.Outline"">
            <BitText Style=""padding:0.5rem"">Taller, and primary while open</BitText>
        </BitDropMenu>
    </div>
</BitStack>";

    private readonly string example20RazorCode = @"
<div dir=""rtl"">
    <BitStack Horizontal Wrap Gap=""1rem"" FitHeight>
        <BitDropMenu Text=""منو"" Dir=""BitDir.Rtl"">
            <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
                <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Settings"">تنظیمات</BitButton>
                <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.SignOut"">خروج</BitButton>
            </BitStack>
        </BitDropMenu>
        <BitDropMenu Text=""ریسپانسیو"" Dir=""BitDir.Rtl"" Responsive PanelPosition=""BitPanelPosition.Start"">
            <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
                <BitText Typography=""BitTypography.Subtitle1"">پنل از ابتدا</BitText>
                <BitCheckbox Label=""فعال"" />
                <BitCheckbox Label=""بایگانی شده"" />
                <BitCheckbox Label=""پیش نویس"" />
            </BitStack>
        </BitDropMenu>
    </BitStack>
</div>";
}
