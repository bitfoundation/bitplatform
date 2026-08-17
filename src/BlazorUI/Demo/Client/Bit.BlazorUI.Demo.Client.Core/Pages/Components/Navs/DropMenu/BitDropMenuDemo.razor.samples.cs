namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.DropMenu;

public partial class BitDropMenuDemo
{
    private readonly string example1RazorCode = @"
<BitDropMenu Text=""Basic"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
        <BitButton>Click me</BitButton>
        <BitToggle>Toggle me</BitToggle>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Disabled"" IsEnabled=""false"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
        <BitButton>Click me</BitButton>
        <BitToggle>Toggle me</BitToggle>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Transparent"" Transparent>
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
        <BitButton>Click me</BitButton>
        <BitToggle>Toggle me</BitToggle>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""NoShadow"" NoShadow>
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
        <BitButton>Click me</BitButton>
        <BitToggle>Toggle me</BitToggle>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""FullWidth"" FullWidth>
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
        <BitButton>Click me</BitButton>
        <BitToggle>Toggle me</BitToggle>
    </BitStack>
</BitDropMenu>";

    private readonly string example2RazorCode = @"
<BitChoiceGroup @bind-Value=""backgroundColorKind"" Horizontal
                Label=""Background color kind""
                TItem=""BitChoiceGroupOption<BitColorKind>"" TValue=""BitColorKind"">
    <BitChoiceGroupOption Text=""Primary"" Value=""BitColorKind.Primary"" />
    <BitChoiceGroupOption Text=""Secondary"" Value=""BitColorKind.Secondary"" />
    <BitChoiceGroupOption Text=""Tertiary"" Value=""BitColorKind.Tertiary"" />
    <BitChoiceGroupOption Text=""Transparent"" Value=""BitColorKind.Transparent"" />
</BitChoiceGroup>

<BitDropMenu Text=""Background"" Background=""backgroundColorKind"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
        <BitButton>Click me</BitButton>
        <BitToggle>Toggle me</BitToggle>
    </BitStack>
</BitDropMenu>";
    private readonly string example2CsharpCode = @"
private BitColorKind backgroundColorKind = BitColorKind.Primary;";

    private readonly string example3RazorCode = @"
<BitChoiceGroup @bind-Value=""borderColorKind"" Horizontal
                Label=""Border color kind""
                TItem=""BitChoiceGroupOption<BitColorKind>"" TValue=""BitColorKind"">
    <BitChoiceGroupOption Text=""Primary"" Value=""BitColorKind.Primary"" />
    <BitChoiceGroupOption Text=""Secondary"" Value=""BitColorKind.Secondary"" />
    <BitChoiceGroupOption Text=""Tertiary"" Value=""BitColorKind.Tertiary"" />
    <BitChoiceGroupOption Text=""Transparent"" Value=""BitColorKind.Transparent"" />
</BitChoiceGroup>

<BitDropMenu Text=""Border"" Border=""borderColorKind"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
        <BitButton>Click me</BitButton>
        <BitToggle>Toggle me</BitToggle>
    </BitStack>
</BitDropMenu>";
    private readonly string example3CsharpCode = @"
private BitColorKind borderColorKind = BitColorKind.Primary;";

    private readonly string example4RazorCode = @"
<BitDropMenu Text=""IconName"" IconName=""@BitIconName.Emoji2"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""ChevronDownIconName"" ChevronDownIconName=""@BitIconName.DoubleChevronDown"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""NoChevron"" IconName=""@BitIconName.Emoji2"" NoChevron>
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu IconName=""@BitIconName.More"" NoChevron AriaLabel=""More actions"" Title=""More actions"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">An icon-only drop menu</BitText>
    </BitStack>
</BitDropMenu>";

    private readonly string example5RazorCode = @"
<BitDropMenu Text=""End PanelPosition"" Responsive ScrollContainerId=""sc-con1"" PanelPosition=""BitPanelPosition.End"">
    <div style=""max-width:200px;overflow:auto"" id=""sc-con1"">
        <BitStack FitWidth Gap=""1rem"" Style=""padding:0.5rem"">
            <BitText Typography=""BitTypography.Subtitle1"" NoWrap>This is the content This is the content This is the content</BitText>
            <BitText Typography=""BitTypography.Subtitle1"" NoWrap>This is the content</BitText>
            <BitText Typography=""BitTypography.Subtitle1"" NoWrap>This is the content</BitText>
            <BitText Typography=""BitTypography.Subtitle1"" NoWrap>This is the content</BitText>
        </BitStack>
    </div>
</BitDropMenu>

<BitDropMenu Text=""Start PanelPosition"" Responsive ScrollContainerId=""sc-con2"" PanelPosition=""BitPanelPosition.Start"">
    <div style=""max-width:200px;overflow:auto"" id=""sc-con2"">
        <BitStack FitWidth Gap=""1rem"" Style=""padding:0.5rem"">
            <BitText Typography=""BitTypography.Subtitle1"" NoWrap>This is the content This is the content This is the content</BitText>
            <BitText Typography=""BitTypography.Subtitle1"" NoWrap>This is the content</BitText>
            <BitText Typography=""BitTypography.Subtitle1"" NoWrap>This is the content</BitText>
            <BitText Typography=""BitTypography.Subtitle1"" NoWrap>This is the content</BitText>
        </BitStack>
    </div>
</BitDropMenu>

<BitDropMenu Text=""Top PanelPosition"" Responsive PanelPosition=""BitPanelPosition.Top"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">Swipe up to dismiss it</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Bottom PanelPosition"" Responsive PanelPosition=""BitPanelPosition.Bottom"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">Swipe down to dismiss it</BitText>
    </BitStack>
</BitDropMenu>";

    private readonly string example6RazorCode = @"
<BitDropMenu Text=""Add Icon"" IconName=""@BitIconName.Emoji2"">
    <Template>
        <div style=""display:flex;gap:10px;align-items:center;"">
            <BitIcon IconName=""@BitIconName.Airplane"" Color=""BitColor.Tertiary"" />
            <span>A template</span>
            <BitRippleLoading CustomSize=""20"" Color=""BitColor.Tertiary"" />
        </div>
    </Template>
    <Body>
        <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
            <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
        </BitStack>
    </Body>
</BitDropMenu>";

    private readonly string example7RazorCode = @"
<BitToggle @bind-Value=""isLoading"" Label=""IsLoading"" />

<BitDropMenu Text=""Loading"" IconName=""@BitIconName.Cloud"" IsLoading=""isLoading"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>";
    private readonly string example7CsharpCode = @"
private bool isLoading;";

    private readonly string example8RazorCode = @"
<BitDropMenu Text=""A rather wide drop menu button"" MatchWidth>
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">MatchWidth</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Width"" Width=""16rem"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">A callout of a fixed width</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""MinWidth"" MinWidth=""16rem"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">Short</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""MaxWidth"" MaxWidth=""16rem"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">A rather long piece of content that wraps instead of stretching the callout across the screen</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""MaxHeight"" MaxHeight=""10rem"">
    <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
        @for (var i = 1; i <= 20; i++)
        {
            <BitText Typography=""BitTypography.Subtitle1"">Item @i</BitText>
        }
    </BitStack>
</BitDropMenu>";

    private readonly string example9RazorCode = @"
<BitChoiceGroup @bind-Value=""dropDirection"" Horizontal
                Label=""Drop direction""
                TItem=""BitChoiceGroupOption<BitDropDirection>"" TValue=""BitDropDirection"">
    <BitChoiceGroupOption Text=""TopAndBottom"" Value=""BitDropDirection.TopAndBottom"" />
    <BitChoiceGroupOption Text=""All"" Value=""BitDropDirection.All"" />
</BitChoiceGroup>

<BitDropMenu Text=""DropDirection"" DropDirection=""dropDirection"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>";
    private readonly string example9CsharpCode = @"
private BitDropDirection dropDirection = BitDropDirection.TopAndBottom;";

    private readonly string example10RazorCode = @"
<BitDropMenu Text=""OpenOnHover"" OpenOnHover IconName=""@BitIconName.Globe"">
    <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
        <BitLink Href=""/components/dropmenu"">Products</BitLink>
        <BitLink Href=""/components/dropmenu"">Solutions</BitLink>
        <BitLink Href=""/components/dropmenu"">Pricing</BitLink>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""With delays"" OpenOnHover HoverOpenDelay=""400"" HoverCloseDelay=""600"" IconName=""@BitIconName.Clock"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">Opens after 400ms, closes after 600ms</BitText>
    </BitStack>
</BitDropMenu>";

    private readonly string example11RazorCode = @"
<BitStack Horizontal Wrap Gap=""0.5rem"" FitHeight>
    <BitButton OnClick=""() => isOpen = !isOpen"">@(isOpen ? ""Close"" : ""Open"") the bound one</BitButton>
    <BitButton OnClick=""() => dropMenuRef?.Toggle()"">Toggle through the reference</BitButton>
</BitStack>

<BitDropMenu @bind-IsOpen=""isOpen"" Text=""@($""IsOpen: {isOpen}"")"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu @ref=""dropMenuRef"" Text=""Controlled by the reference"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

@* A drop menu that only needs to start out open uses DefaultIsOpen instead of binding IsOpen. *@
<BitCheckbox @bind-Value=""mountDefaultIsOpen"" Label=""Render a drop menu that starts out open"" />

@if (mountDefaultIsOpen)
{
    <BitDropMenu Text=""DefaultIsOpen"" DefaultIsOpen>
        <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
            <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
        </BitStack>
    </BitDropMenu>
}";
    private readonly string example11CsharpCode = @"
private bool isOpen;
private bool mountDefaultIsOpen;
private BitDropMenu? dropMenuRef;";

    private readonly string example12RazorCode = @"
<BitDropMenu Text=""AutoFocus"" AutoFocus>
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitTextField Label=""Name"" />
        <BitButton>Submit</BitButton>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""TrapFocus"" TrapFocus>
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitTextField Label=""Name"" />
        <BitTextField Label=""Email"" />
        <BitButton>Submit</BitButton>
    </BitStack>
</BitDropMenu>";

    private readonly string example13RazorCode = @"
<BitDropMenu Text=""@($""Click me ({clickCounter})"")""
             OnClick=""() => clickCounter++""
             OnOpen=""() => openCounter++""
             OnDismiss=""() => dismissCounter++"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<div>Clicked: @clickCounter, Opened: @openCounter, Dismissed: @dismissCounter</div>";
    private readonly string example13CsharpCode = @"
private int clickCounter;
private int openCounter;
private int dismissCounter;";

    private readonly string example14RazorCode = @"
<BitDropMenu Text=""@($""AutoClose ({autoCloseAction})"")"" AutoClose IconName=""@BitIconName.More"">
    <BitStack Gap=""0.25rem"" Style=""padding:0.5rem"">
        <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => autoCloseAction = ""Renamed"")"">Rename</BitButton>
        <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => autoCloseAction = ""Duplicated"")"">Duplicate</BitButton>
        <BitButton Variant=""BitVariant.Text"" OnClick=""@(() => autoCloseAction = ""Deleted"")"">Delete</BitButton>
    </BitStack>
</BitDropMenu>

@* Without AutoClose the callout stays open while the content is being used. *@
<BitDropMenu Text=""Without AutoClose"" IconName=""@BitIconName.Filter"">
    <BitStack Gap=""0.5rem"" Style=""padding:0.5rem"">
        <BitCheckbox Label=""Active"" />
        <BitCheckbox Label=""Archived"" />
        <BitCheckbox Label=""Draft"" />
    </BitStack>
</BitDropMenu>";
    private readonly string example14CsharpCode = @"
private string autoCloseAction = ""none"";";

    private readonly string example15RazorCode = @"
<BitChoiceGroup @bind-Value=""variant"" Horizontal
                Label=""Variant""
                TItem=""BitChoiceGroupOption<BitVariant>"" TValue=""BitVariant"">
    <BitChoiceGroupOption Text=""Fill"" Value=""BitVariant.Fill"" />
    <BitChoiceGroupOption Text=""Outline"" Value=""BitVariant.Outline"" />
    <BitChoiceGroupOption Text=""Text"" Value=""BitVariant.Text"" />
</BitChoiceGroup>

<BitDropMenu Text=""Primary"" Variant=""variant"" Color=""BitColor.Primary"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Error"" Variant=""variant"" Color=""BitColor.Error"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Disabled"" Variant=""variant"" Color=""BitColor.Primary"" IsEnabled=""false"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>";
    private readonly string example15CsharpCode = @"
private BitVariant variant = BitVariant.Fill;";

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

<BitDropMenu Text=""@color.ToString()"" Color=""color"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>";
    private readonly string example16CsharpCode = @"
private BitColor color = BitColor.Primary;";

    private readonly string example17RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitDropMenu Text=""House"" Icon=""@(""fa-solid fa-house"")"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Heart"" Icon=""@BitIconInfo.Css(""fa-solid fa-heart"")"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""GitHub"" Icon=""@BitIconInfo.Fa(""fa-brands fa-github"")"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitDropMenu Text=""House"" Icon=""@(""bi bi-house-fill"")"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Gear"" Icon=""@BitIconInfo.Bi(""gear-fill"")""
             ChevronDownIcon=""@BitIconInfo.Bi(""chevron-down"")"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>";

    private readonly string example18RazorCode = @"
<BitDropMenu Text=""Small"" Size=""BitSize.Small"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Medium"" Size=""BitSize.Medium"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Large"" Size=""BitSize.Large"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>";

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

<BitDropMenu Text=""Styled Drop menu"" Style=""background-color: transparent; border-color: blueviolet; color: blueviolet;"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Classed Drop menu"" Class=""custom-class"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Styled Drop menu""
             Styles=""@(new() { Root = ""background-color: peachpuff; border-color: peachpuff; min-width: 6rem;"",
                               Text = ""color: tomato; font-weight: bold;"",
                               Callout = ""border: 2px solid tomato;"",
                               Opened = ""border-color: tomato; background-color: goldenrod;"" })"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""Classed Drop menu""
             Classes=""@(new() { Root = ""custom-root"",
                                Text = ""custom-text"",
                                ChevronDown = ""custom-chevron"",
                                Opened = ""custom-opened"" })"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">This is the content</BitText>
    </BitStack>
</BitDropMenu>";

    private readonly string example20RazorCode = @"
<BitDropMenu Text=""منو"" Dir=""BitDir.Rtl"">
    <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
        <BitText Typography=""BitTypography.Subtitle1"">این یک محتوای تستی می باشد.</BitText>
    </BitStack>
</BitDropMenu>

<BitDropMenu Text=""ریسپانسیو منو در انتها"" Dir=""BitDir.Rtl"" Responsive ScrollContainerId=""sc-con-rtl1"">
    <div style=""max-width:200px;overflow:auto"" id=""sc-con-rtl1"">
        <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
            <BitText Typography=""BitTypography.Subtitle1"" NoWrap>این یک محتوای تستی می باشد این یک محتوای تستی می باشد این یک محتوای تستی می باشد</BitText>
            <BitText Typography=""BitTypography.Subtitle1"" NoWrap>این یک محتوای تستی می باشد</BitText>
            <BitText Typography=""BitTypography.Subtitle1"" NoWrap>این یک محتوای تستی می باشد</BitText>
        </BitStack>
    </div>
</BitDropMenu>

<BitDropMenu Text=""ریسپانسیو منو در ابتدا"" Dir=""BitDir.Rtl"" Responsive ScrollContainerId=""sc-con-rtl2"" PanelPosition=""BitPanelPosition.Start"">
    <div style=""max-width:200px;overflow:auto"" id=""sc-con-rtl2"">
        <BitStack Gap=""1rem"" Style=""padding:0.5rem"">
            <BitText Typography=""BitTypography.Subtitle1"" NoWrap>این یک محتوای تستی می باشد این یک محتوای تستی می باشد این یک محتوای تستی می باشد</BitText>
            <BitText Typography=""BitTypography.Subtitle1"" NoWrap>این یک محتوای تستی می باشد</BitText>
            <BitText Typography=""BitTypography.Subtitle1"" NoWrap>این یک محتوای تستی می باشد</BitText>
        </BitStack>
    </div>
</BitDropMenu>";

}
