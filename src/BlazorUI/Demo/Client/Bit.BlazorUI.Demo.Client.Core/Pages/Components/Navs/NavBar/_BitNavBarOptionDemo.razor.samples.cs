namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.NavBar;

public partial class _BitNavBarOptionDemo
{
    private readonly string example1RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example2RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" IsEnabled=""false"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" IsEnabled=""false"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example3RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example4RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""NavBar"" IconName=""@BitIconName.GlobalNavButton"" Url=""/components/navbar"" />
    <BitNavBarOption Text=""Nav"" IconName=""@BitIconName.BulletedList"" Url=""/components/nav"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Match=""BitNavMatch.Prefix"">
    <BitNavBarOption Text=""Components"" IconName=""@BitIconName.F12DevTools"" Url=""/components"" />
    <BitNavBarOption Text=""Iconography"" IconName=""@BitIconName.AppIconDefault"" Url=""/iconography"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""/components/*"" IconName=""@BitIconName.F12DevTools"" Url=""/components/*"" Match=""BitNavMatch.Wildcard"" IsEnabled=""false"" />
    <BitNavBarOption Text=""^/components/b"" IconName=""@BitIconName.Code"" Url=""^/components/b"" Match=""BitNavMatch.Regex"" IsEnabled=""false"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""Navs"" IconName=""@BitIconName.GlobalNavButton"" Url=""/components/nav"" AdditionalUrls=""@([""/components/navbar"", ""/components/breadcrumb""])"" />
    <BitNavBarOption Text=""Buttons"" IconName=""@BitIconName.ButtonControl"" Url=""/components/button"" AdditionalUrls=""@([""/components/togglebutton""])"" />
</BitNavBar>";

    private readonly string example5RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" IconOnly>
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example6RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" HideUnselectedText Mode=""BitNavMode.Manual"" @bind-SelectedItem=""hideTextSelectedOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" @ref=""hideTextOptionHome"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";
    private readonly string example6CsharpCode = @"
// An option only exists once it has rendered, so the selection is handed the captured reference
// after the first render instead of through a DefaultSelectedItem.
private BitNavBarOption? hideTextSelectedOption;
private BitNavBarOption hideTextOptionHome = default!;

protected override void OnAfterRender(bool firstRender)
{
    if (firstRender)
    {
        hideTextSelectedOption ??= hideTextOptionHome;
        StateHasChanged();
    }

    base.OnAfterRender(firstRender);
}";

    private readonly string example7RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" InlineText>
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Vertical FitWidth>
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Vertical InlineText FitWidth>
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example8RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" FitWidth>
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" FullWidth>
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products & services"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Me"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Justified>
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products & services"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Me"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example9RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Inbox"" IconName=""@BitIconName.Mail"" Badge=""12"" />
    <BitNavBarOption Text=""Alerts"" IconName=""@BitIconName.Ringer"" Badge=""99+"" BadgeAriaLabel=""more than 99 unread alerts"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" Dot BadgeAriaLabel=""needs attention"" />
</BitNavBar>";

    private readonly string example10RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" Accent=""BitColor.Primary"" Mode=""BitNavMode.Manual"" @bind-SelectedItem=""accentSelectedOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" @ref=""accentOptionHome"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Accent=""BitColor.Success"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Accent=""BitColor.Error"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Accent=""BitColor.SecondaryBackground"" Color=""BitColor.Info"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";
    private readonly string example10CsharpCode = @"
private BitNavBarOption? accentSelectedOption;
private BitNavBarOption accentOptionHome = default!;

protected override void OnAfterRender(bool firstRender)
{
    if (firstRender)
    {
        accentSelectedOption ??= accentOptionHome;
        StateHasChanged();
    }

    base.OnAfterRender(firstRender);
}";

    private readonly string example11RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"">
    <Options>
        <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
        <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
        <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
        <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
    </Options>
    <ItemTemplate Context=""option"">
        <BitText Typography=""BitTypography.Caption1"" Color=""BitColor.Warning"">@option.Text</BitText>
        <BitIcon IconName=""@option.IconName"" Color=""BitColor.Success"" />
    </ItemTemplate>
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"">
        <Template Context=""option"">
            <div style=""display:flex;flex-direction:column""><b>@option.Text</b><span>&#127873;</span></div>
        </Template>
    </BitNavBarOption>
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example12RazorCode = @"
<BitNavBar TItem=""BitNavBarOption""
           Mode=""BitNavMode.Manual""
           OnItemClick=""(BitNavBarOption option) => eventsClickedOption = option""
           OnSelectItem=""(BitNavBarOption option) => eventsSelectedOption = option"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

Clicked item: @eventsClickedOption?.Text
Selected item: @eventsSelectedOption?.Text";
    private readonly string example12CsharpCode = @"
private BitNavBarOption? eventsClickedOption;
private BitNavBarOption? eventsSelectedOption;";

    private readonly string example13RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"" @bind-SelectedItem=""bindingSelectedOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" @ref=""bindingOptionProducts"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

Selected item: @bindingSelectedOption?.Text


<BitNavBar TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"" @bind-SelectedItem=""twoWaySelectedOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" @ref=""optionHome"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" @ref=""optionProducts"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" @ref=""optionAcademy"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" @ref=""optionProfile"" />
</BitNavBar>

Selected item: @twoWaySelectedOption?.Text

<BitChoiceGroup Horizontal TItem=""BitChoiceGroupOption<BitNavBarOption>"" TValue=""BitNavBarOption"" @bind-Value=""@twoWaySelectedOption"">
    <BitChoiceGroupOption Text=""Home"" Id=""Home"" Value=""optionHome"" />
    <BitChoiceGroupOption Text=""Products"" Id=""Products"" Value=""optionProducts"" />
    <BitChoiceGroupOption Text=""Academy"" Id=""Academy"" Value=""optionAcademy"" />
    <BitChoiceGroupOption Text=""Profile"" Id=""Profile"" Value=""optionProfile"" />
</BitChoiceGroup>";
    private readonly string example13CsharpCode = @"
private BitNavBarOption? bindingSelectedOption;
private BitNavBarOption bindingOptionProducts = default!;

private BitNavBarOption? twoWaySelectedOption;

private BitNavBarOption optionHome = default!;
private BitNavBarOption optionProducts = default!;
private BitNavBarOption optionAcademy = default!;
private BitNavBarOption optionProfile = default!;

protected override void OnAfterRender(bool firstRender)
{
    if (firstRender)
    {
        // An option only exists once it has rendered, so the navbar that has to open on one is handed
        // its reference here, where the reference has been assigned.
        bindingSelectedOption ??= bindingOptionProducts;

        StateHasChanged();
    }

    base.OnAfterRender(firstRender);
}";

    private readonly string example14RazorCode = @"
<BitToggle @bind-Value=""reselectable"" OnText=""Enabled recalling"" OffText=""Disabled recalling"" />

<BitNavBar TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"" OnItemClick=""(BitNavBarOption option) => countClick++"" Reselectable=""reselectable"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

Item click count: @countClick";
    private readonly string example14CsharpCode = @"
private int countClick;
private bool reselectable = true;";

    private readonly string example15RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" SingleTabStop Mode=""BitNavMode.Manual"" @bind-SelectedItem=""tabStopSelectedOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" @ref=""tabStopOptionProducts"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" WrapNavigation SingleTabStop Mode=""BitNavMode.Manual"" @bind-SelectedItem=""wrapSelectedOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" @ref=""wrapOptionProducts"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";
    private readonly string example15CsharpCode = @"
private BitNavBarOption? tabStopSelectedOption;
private BitNavBarOption tabStopOptionProducts = default!;

private BitNavBarOption? wrapSelectedOption;
private BitNavBarOption wrapOptionProducts = default!;

protected override void OnAfterRender(bool firstRender)
{
    if (firstRender)
    {
        tabStopSelectedOption ??= tabStopOptionProducts;
        wrapSelectedOption ??= wrapOptionProducts;
        StateHasChanged();
    }

    base.OnAfterRender(firstRender);
}";

    private readonly string example16RazorCode = @"
<div class=""mobile-frame"">
    <div class=""screen"">
        <BitSticky Top=""0"">
            <BitCard FullWidth>
                <BitStack Horizontal HorizontalAlign=""BitAlignment.Center"" VerticalAlign=""BitAlignment.Center"">
                    <BitImage Src=""/images/bit-logo.svg"" Width=""50"" />
                    <BitText Typography=""BitTypography.H4"" Color=""BitColor.Info"">bit BlazorUI</BitText>
                </BitStack>
            </BitCard>
        </BitSticky>
        <BitStack Alignment=""BitAlignment.Center"" AutoHeight Grows>
            <BitText Typography=""BitTypography.H4"" Color=""BitColor.PrimaryForeground"">
                <BitIcon IconName=""@advancedSelectedOption?.IconName"" Color=""BitColor.PrimaryForeground"" Size=""BitSize.Large"" />
                <span>@advancedSelectedOption?.Text</span>
            </BitText>
        </BitStack>
        <BitSticky Bottom=""0"">
            <BitCard FullWidth Style=""padding:2px"">
                <BitNavBar TItem=""BitNavBarOption"" SafeArea FullWidth Accent=""BitColor.Primary"" Mode=""BitNavMode.Manual"" @bind-SelectedItem=""advancedSelectedOption"">
                    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
                    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" @ref=""advancedOptionProducts"" />
                    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
                    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
                </BitNavBar>
            </BitCard>
        </BitSticky>
    </div>
</div>";
    private readonly string example16CsharpCode = @"
private BitNavBarOption? advancedSelectedOption;
private BitNavBarOption advancedOptionProducts = default!;

protected override void OnAfterRender(bool firstRender)
{
    if (firstRender)
    {
        advancedSelectedOption ??= advancedOptionProducts;
        StateHasChanged();
    }

    base.OnAfterRender(firstRender);
}";

    private readonly string example17RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"" @bind-SelectedItem=""selectedIconSelectedOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" SelectedIconName=""@BitIconName.HomeSolid"" @ref=""selectedIconOptionHome"" />
    <BitNavBarOption Text=""Inbox"" IconName=""@BitIconName.Mail"" SelectedIconName=""@BitIconName.MailSolid"" />
    <BitNavBarOption Text=""Alerts"" IconName=""@BitIconName.Ringer"" SelectedIconName=""@BitIconName.RingerSolid"" />
    <BitNavBarOption Text=""Favorites"" IconName=""@BitIconName.Heart"" SelectedIconName=""@BitIconName.HeartFill"" />
</BitNavBar>";
    private readonly string example17CsharpCode = @"
private BitNavBarOption? selectedIconSelectedOption;
private BitNavBarOption selectedIconOptionHome = default!;

protected override void OnAfterRender(bool firstRender)
{
    if (firstRender)
    {
        selectedIconSelectedOption ??= selectedIconOptionHome;
        StateHasChanged();
    }

    base.OnAfterRender(firstRender);
}";

    private readonly string example18RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" Alignment=""BitAlignment.Start"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Alignment=""BitAlignment.Center"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Alignment=""BitAlignment.End"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Alignment=""BitAlignment.SpaceBetween"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Vertical FitWidth Alignment=""BitAlignment.Center"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example19RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"">
    <HeaderTemplate>
        <BitImage Src=""/images/bit-logo.svg"" Width=""32"" />
    </HeaderTemplate>
    <Options>
        <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
        <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
        <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
        <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
    </Options>
    <FooterTemplate>
        <BitButton IconOnly Title=""More"" Variant=""BitVariant.Text"" IconName=""@BitIconName.More"" />
    </FooterTemplate>
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Vertical FitWidth IconOnly>
    <HeaderTemplate>
        <BitButton IconOnly Title=""New"" IconName=""@BitIconName.Add"" />
    </HeaderTemplate>
    <Options>
        <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
        <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
        <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
        <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
    </Options>
    <FooterTemplate>
        <BitButton IconOnly Title=""Settings"" Variant=""BitVariant.Text"" IconName=""@BitIconName.Settings"" />
    </FooterTemplate>
</BitNavBar>";

    private readonly string example20RazorCode = @"
<BitStack Horizontal>
    <BitButton OnClick=""AddDynamicOption"">Add item</BitButton>
    <BitButton OnClick=""RemoveDynamicOption"">Remove item</BitButton>
    <BitButton OnClick=""ReverseDynamicOptions"">Reverse items</BitButton>
</BitStack>

<BitToggle @bind-Value=""dynamicAutoReorder"" OnText=""AutoReorderOptions"" OffText=""AutoReorderOptions"" />

<BitNavBar TItem=""BitNavBarOption""
           Mode=""BitNavMode.Manual""
           AutoReorderOptions=""dynamicAutoReorder""
           @bind-SelectedItem=""dynamicSelectedOption"">
    @foreach (var option in dynamicOptions)
    {
        <BitNavBarOption @key=""option"" Text=""@option.Text"" IconName=""@option.IconName"" />
    }
</BitNavBar>

Selected item: @dynamicSelectedOption?.Text";
    private readonly string example20CsharpCode = @"
private bool dynamicAutoReorder = true;
private int dynamicOptionsCount = 3;
private BitNavBarOption? dynamicSelectedOption;
private readonly List<DynamicOption> dynamicOptions =
[
    new(""Home"", BitIconName.Home),
    new(""Products"", BitIconName.ProductVariant),
    new(""Profile"", BitIconName.Contact),
];

private void AddDynamicOption()
{
    dynamicOptionsCount++;
    dynamicOptions.Add(new($""Item {dynamicOptionsCount}"", BitIconName.Tag));
}

private void RemoveDynamicOption()
{
    if (dynamicOptions.Count == 0) return;

    dynamicOptions.RemoveAt(dynamicOptions.Count - 1);
}

private void ReverseDynamicOptions() => dynamicOptions.Reverse();

private record DynamicOption(string Text, string IconName);";

    private readonly string example21RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.Primary"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.Secondary"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.Tertiary"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.Info"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.Success"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.Warning"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.SevereWarning"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.Error"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.PrimaryBackground"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.SecondaryBackground"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.TertiaryBackground"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.PrimaryForeground"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.SecondaryForeground"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.TertiaryForeground"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.PrimaryBorder"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.SecondaryBorder"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.TertiaryBorder"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example22RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitNavBar TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""Home"" Icon=""@(""fa-solid fa-house"")"" />
    <BitNavBarOption Text=""Products"" Icon=""@BitIconInfo.Css(""fa-solid fa-box"")"" />
    <BitNavBarOption Text=""Academy"" Icon=""@BitIconInfo.Fa(""solid graduation-cap"")"" />
    <BitNavBarOption Text=""Profile"" Icon=""@BitIconInfo.Fa(""solid user"")"" />
</BitNavBar>";

    private readonly string example23RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" Size=""BitSize.Small"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Size=""BitSize.Medium"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Size=""BitSize.Large"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example24RazorCode = @"
<style>
    .custom-class {
        margin: 1rem;
        border-radius: 1rem;
        box-shadow: aqua 0 0 1rem;
        background: linear-gradient(90deg, magenta, transparent) blue;
    }

    .custom-item {
        color: #ff7800;
        font-weight: 600;
    }

    .custom-item-ico {
        font-weight: bold;
        color: darkmagenta;
    }

    .custom-item-txt {
        font-weight: bold;
        font-style: italic;
    }
</style>

<BitNavBar Style=""border-radius: 1rem; margin: 1rem; box-shadow: tomato 0 0 1rem;"" TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar Class=""custom-class"" TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" Class=""custom-item"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" Style=""color: #b6ff00;font-weight: 600;"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar Styles=""@(new() { ItemIcon = ""color: aqua;"", ItemText = ""color: tomato;"", ItemBadge = ""background: darkmagenta;"" })"" TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Inbox"" IconName=""@BitIconName.Mail"" Badge=""12"" />
    <BitNavBarOption Text=""Alerts"" IconName=""@BitIconName.Ringer"" Badge=""99+"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" Dot />
</BitNavBar>

<BitNavBar Classes=""@(new() { ItemIcon = ""custom-item-ico"", ItemText = ""custom-item-txt"" })"" TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example25RazorCode = @"
<BitNavBar Dir=""BitDir.Rtl"" TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""خانه"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""محصولات"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""آکادمی"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""پروفایل"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";
}
