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
<BitNavBar TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
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
    <BitNavBarOption Text=""/iconography/*"" IconName=""@BitIconName.AppIconDefault"" Url=""/iconography/*"" Match=""BitNavMatch.Wildcard"" IsEnabled=""false"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""^/components/navbar$"" IconName=""@BitIconName.Code"" Url=""^/components/navbar$"" Match=""BitNavMatch.Regex"" IsEnabled=""false"" />
    <BitNavBarOption Text=""^/iconography$"" IconName=""@BitIconName.Code"" Url=""^/iconography$"" Match=""BitNavMatch.Regex"" IsEnabled=""false"" />
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
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" HideUnselectedText Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" InlineText>
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example6RazorCode = @"
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

<BitNavBar TItem=""BitNavBarOption"" Justified>
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products &amp; services"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Me"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Alignment=""BitAlignment.Center"">
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
</BitNavBar>";

    private readonly string example7RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" SelectedIconName=""@BitIconName.HomeSolid"" Key=""home"" />
    <BitNavBarOption Text=""Inbox"" IconName=""@BitIconName.Mail"" SelectedIconName=""@BitIconName.MailSolid"" />
    <BitNavBarOption Text=""Alerts"" IconName=""@BitIconName.Ringer"" SelectedIconName=""@BitIconName.RingerSolid"" />
    <BitNavBarOption Text=""Favorites"" IconName=""@BitIconName.Heart"" SelectedIconName=""@BitIconName.HeartFill"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Filled Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" SelectedIconName=""@BitIconName.HomeSolid"" Key=""home"" />
    <BitNavBarOption Text=""Inbox"" IconName=""@BitIconName.Mail"" SelectedIconName=""@BitIconName.MailSolid"" />
    <BitNavBarOption Text=""Alerts"" IconName=""@BitIconName.Ringer"" SelectedIconName=""@BitIconName.RingerSolid"" />
    <BitNavBarOption Text=""Favorites"" IconName=""@BitIconName.Heart"" SelectedIconName=""@BitIconName.HeartFill"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Indicator=""BitNavBarIndicator.Line"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" SelectedIconName=""@BitIconName.HomeSolid"" Key=""home"" />
    <BitNavBarOption Text=""Inbox"" IconName=""@BitIconName.Mail"" SelectedIconName=""@BitIconName.MailSolid"" />
    <BitNavBarOption Text=""Alerts"" IconName=""@BitIconName.Ringer"" SelectedIconName=""@BitIconName.RingerSolid"" />
    <BitNavBarOption Text=""Favorites"" IconName=""@BitIconName.Heart"" SelectedIconName=""@BitIconName.HeartFill"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Indicator=""BitNavBarIndicator.Pill"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" SelectedIconName=""@BitIconName.HomeSolid"" Key=""home"" />
    <BitNavBarOption Text=""Inbox"" IconName=""@BitIconName.Mail"" SelectedIconName=""@BitIconName.MailSolid"" />
    <BitNavBarOption Text=""Alerts"" IconName=""@BitIconName.Ringer"" SelectedIconName=""@BitIconName.RingerSolid"" />
    <BitNavBarOption Text=""Favorites"" IconName=""@BitIconName.Heart"" SelectedIconName=""@BitIconName.HeartFill"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Indicator=""BitNavBarIndicator.Pill"" Filled Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" SelectedIconName=""@BitIconName.HomeSolid"" Key=""home"" />
    <BitNavBarOption Text=""Inbox"" IconName=""@BitIconName.Mail"" SelectedIconName=""@BitIconName.MailSolid"" />
    <BitNavBarOption Text=""Alerts"" IconName=""@BitIconName.Ringer"" SelectedIconName=""@BitIconName.RingerSolid"" />
    <BitNavBarOption Text=""Favorites"" IconName=""@BitIconName.Heart"" SelectedIconName=""@BitIconName.HeartFill"" />
</BitNavBar>";

    private readonly string example8RazorCode = @"
<BitButton OnClick=""@(() => scrollableSelectedOption = scrollableOptionProfile)"">Select the last item</BitButton>

<BitNavBar TItem=""BitNavBarOption"" Scrollable Mode=""BitNavMode.Manual"" @bind-SelectedItem=""scrollableSelectedOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Inbox"" IconName=""@BitIconName.Mail"" />
    <BitNavBarOption Text=""Alerts"" IconName=""@BitIconName.Ringer"" />
    <BitNavBarOption Text=""Favorites"" IconName=""@BitIconName.Heart"" />
    <BitNavBarOption Text=""Reports"" IconName=""@BitIconName.ReportDocument"" />
    <BitNavBarOption Text=""Settings"" IconName=""@BitIconName.Settings"" />
    <BitNavBarOption Text=""Support"" IconName=""@BitIconName.Help"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" @ref=""scrollableOptionProfile"" />
</BitNavBar>

Selected item: @scrollableSelectedOption?.Text";
    private readonly string example8CsharpCode = @"
private BitNavBarOption? scrollableSelectedOption;
private BitNavBarOption scrollableOptionProfile = default!;";

    private readonly string example9RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"">
    <HeaderTemplate>
        <BitImage Src=""/images/bit-logo.svg"" Width=""32"" Alt=""bit"" />
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
</BitNavBar>";

    private readonly string example10RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" Vertical FitWidth>
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption""
           Vertical
           FitWidth
           InlineText
           Indicator=""BitNavBarIndicator.Line""
           Mode=""BitNavMode.Manual""
           DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Vertical FitWidth IconOnly Alignment=""BitAlignment.Center"">
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
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Scrollable Vertical FitWidth Style=""height:16rem"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Inbox"" IconName=""@BitIconName.Mail"" />
    <BitNavBarOption Text=""Alerts"" IconName=""@BitIconName.Ringer"" />
    <BitNavBarOption Text=""Favorites"" IconName=""@BitIconName.Heart"" />
    <BitNavBarOption Text=""Reports"" IconName=""@BitIconName.ReportDocument"" />
    <BitNavBarOption Text=""Settings"" IconName=""@BitIconName.Settings"" />
    <BitNavBarOption Text=""Support"" IconName=""@BitIconName.Help"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example11RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Inbox"" IconName=""@BitIconName.Mail"" Badge=""12"" />
    <BitNavBarOption Text=""Alerts"" IconName=""@BitIconName.Ringer"" Badge=""99+"" BadgeAriaLabel=""more than 99 unread alerts"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" Dot BadgeAriaLabel=""needs attention"" />
</BitNavBar>";

    private readonly string example12RazorCode = @"
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
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Search"" IconName=""@BitIconName.Search"" />
    <BitNavBarOption Text=""New"" TemplateRenderMode=""BitNavItemTemplateRenderMode.Replace"">
        <Template Context=""option"">
            <BitButton IconOnly Title=""@option.Text"" IconName=""@BitIconName.Add"" Style=""align-self:center"" />
        </Template>
    </BitNavBarOption>
    <BitNavBarOption Text=""Alerts"" IconName=""@BitIconName.Ringer"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example13RazorCode = @"
<BitToggle @bind-Value=""reselectable"" Label=""Reselectable"" Inline />

<BitNavBar TItem=""BitNavBarOption""
           Mode=""BitNavMode.Manual""
           Reselectable=""reselectable""
           OnItemClick=""(BitNavBarOption option) => { eventsClickedOption = option; clickCount++; }""
           OnSelectItem=""(BitNavBarOption option) => { eventsSelectedOption = option; selectCount++; }"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<div>Clicked item: @eventsClickedOption?.Text (@clickCount clicks)</div>
<div>Selected item: @eventsSelectedOption?.Text (@selectCount selections)</div>";
    private readonly string example13CsharpCode = @"
private bool reselectable;
private int clickCount;
private int selectCount;
private BitNavBarOption? eventsClickedOption;
private BitNavBarOption? eventsSelectedOption;";

    private readonly string example14RazorCode = @"
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
    private readonly string example14CsharpCode = @"
private BitNavBarOption? twoWaySelectedOption;
private BitNavBarOption optionHome = default!;
private BitNavBarOption optionProducts = default!;
private BitNavBarOption optionAcademy = default!;
private BitNavBarOption optionProfile = default!;

protected override void OnAfterRender(bool firstRender)
{
    if (firstRender)
    {
        // A reference is only assigned once its option has rendered, so render once more to hand the
        // captured options to the choice group as its values.
        StateHasChanged();
    }

    base.OnAfterRender(firstRender);
}";

    private readonly string example15RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" SingleTabStop Mode=""BitNavMode.Manual"" DefaultSelectedKey=""products"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" Key=""products"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" WrapNavigation SingleTabStop Mode=""BitNavMode.Manual"" DefaultSelectedKey=""products"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" Key=""products"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" SelectOnFocus SingleTabStop Mode=""BitNavMode.Manual"" DefaultSelectedKey=""products"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" Key=""products"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example16RazorCode = @"
<BitStack Horizontal>
    <BitButton OnClick=""AddDynamicOption"">Add item</BitButton>
    <BitButton OnClick=""RemoveDynamicOption"">Remove item</BitButton>
    <BitButton OnClick=""ReverseDynamicOptions"">Reverse items</BitButton>
</BitStack>

<BitToggle @bind-Value=""dynamicAutoReorder"" Label=""AutoReorderOptions"" Inline />

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
    private readonly string example16CsharpCode = @"
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

    private readonly string example17RazorCode = @"
<BitParams Parameters=""navBarParams"">
    <BitNavBar TItem=""BitNavBarOption"" DefaultSelectedKey=""home"">
        <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
        <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
        <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
        <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
    </BitNavBar>

    <BitNavBar TItem=""BitNavBarOption"" Indicator=""BitNavBarIndicator.Line"" DefaultSelectedKey=""home"">
        <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
        <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
        <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
        <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
    </BitNavBar>
</BitParams>

<BitNavBar TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";
    private readonly string example17CsharpCode = @"
private static readonly BitNavBarParams[] navBarParams =
[
    new()
    {
        Mode = BitNavMode.Manual,
        Filled = true,
        Color = BitColor.Info,
        Indicator = BitNavBarIndicator.Pill,
    }
];";

    private readonly string example18RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.Primary"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.Secondary"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.Tertiary"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.Info"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.Success"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.Warning"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.SevereWarning"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.Error"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.PrimaryBackground"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.SecondaryBackground"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.TertiaryBackground"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.PrimaryForeground"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.SecondaryForeground"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.TertiaryForeground"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.PrimaryBorder"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.SecondaryBorder"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Color=""BitColor.TertiaryBorder"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example19RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitNavBar TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""Home"" Icon=""@(""fa-solid fa-house"")"" />
    <BitNavBarOption Text=""Products"" Icon=""@BitIconInfo.Css(""fa-solid fa-box"")"" />
    <BitNavBarOption Text=""Academy"" Icon=""@BitIconInfo.Fa(""solid graduation-cap"")"" />
    <BitNavBarOption Text=""Profile"" Icon=""@BitIconInfo.Fa(""solid user"")"" />
</BitNavBar>";

    private readonly string example20RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" Size=""BitSize.Small"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Size=""BitSize.Medium"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar TItem=""BitNavBarOption"" Size=""BitSize.Large"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example21RazorCode = @"
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

    .floating-navbar {
        margin: 0 0.75rem 0.75rem;
        --bit-NavBar-background: var(--bit-clr-bg-sec);
        --bit-NavBar-border-radius: 999px;
        --bit-NavBar-shadow: 0 4px 16px rgba(0, 0, 0, 0.2);
        --bit-NavBar-padding-block: 4px;
        --bit-NavBar-padding-inline: 8px;
        --bit-NavBar-item-border-radius: 999px;
        --bit-NavBar-selected-color: var(--bit-clr-pri-text);
        --bit-NavBar-selected-background: var(--bit-clr-pri);
    }
</style>

<BitNavBar TItem=""BitNavBarOption"" Style=""border-radius: 1rem; margin: 1rem; box-shadow: tomato 0 0 1rem;"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Class=""custom-class"">
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

<BitNavBar TItem=""BitNavBarOption"" Styles=""@(new() { ItemIcon = ""color: aqua;"", ItemText = ""color: tomato;"", ItemBadge = ""background: darkmagenta;"" })"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Inbox"" IconName=""@BitIconName.Mail"" Badge=""12"" />
    <BitNavBarOption Text=""Alerts"" IconName=""@BitIconName.Ringer"" Badge=""99+"" BadgeAriaLabel=""more than 99 unread alerts"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" Dot BadgeAriaLabel=""needs attention"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Classes=""@(new() { ItemIcon = ""custom-item-ico"", ItemText = ""custom-item-txt"" })"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar TItem=""BitNavBarOption"" Class=""floating-navbar"" Mode=""BitNavMode.Manual"" DefaultSelectedKey=""home"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" Key=""home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example22RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" Dir=""BitDir.Rtl"">
    <BitNavBarOption Text=""خانه"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""محصولات"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""آکادمی"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""پروفایل"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";
}
