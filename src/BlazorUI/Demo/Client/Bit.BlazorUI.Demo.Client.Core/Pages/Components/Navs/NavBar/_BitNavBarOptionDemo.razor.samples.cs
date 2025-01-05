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
<BitNavBar Mode=""BitNavMode.Manual"" TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example4RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" IconOnly>
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example5RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" FitWidth>
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example6RazorCode = @"
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
            <div style=""display:flex;flex-direction:column""><b>@option.Text</b><span>🎁</span></div>
        </Template>
    </BitNavBarOption>
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example7RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" OnItemClick=""(BitNavBarOption option) => eventsClickedOption = option"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

Clicked item: @eventsClickedOption?.Text";
    private readonly string example7CsharpCode = @"
private BitNavBarOption? eventsClickedOption;
";

    private readonly string example8RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"" @bind-SelectedItem=""twoWaySelectedOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" @ref=""optionHome"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" @ref=""optionProducts"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" @ref=""optionAcademy"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" @ref=""optionProfile"" />
</BitNavBar>

<BitChoiceGroup Horizontal TItem=""BitChoiceGroupOption<BitNavBarOption>"" TValue=""BitNavBarOption"" @bind-Value=""@twoWaySelectedOption"">
    <BitChoiceGroupOption Text=""Home"" Id=""Home"" Value=""optionHome"" />
    <BitChoiceGroupOption Text=""Products"" Id=""Products"" Value=""optionProducts"" />
    <BitChoiceGroupOption Text=""Academy"" Id=""Academy"" Value=""optionAcademy"" />
    <BitChoiceGroupOption Text=""Profile"" Id=""Profile"" Value=""optionProfile"" />
</BitChoiceGroup>";
    private readonly string example8CsharpCode = @"
private BitNavBarOption? twoWaySelectedOption;

private BitNavBarOption optionHome = default!;
private BitNavBarOption optionProducts = default!;
private BitNavBarOption optionAcademy = default!;
private BitNavBarOption optionProfile = default!;";

    private readonly string example9RazorCode = @"
<BitToggle @bind-Value=""reselectable"" OnText=""Enabled recalling"" OffText=""Disabled recalling"" />
<BitNavBar TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"" OnItemClick=""(BitNavBarOption option) => countClick++"" Reselectable=""reselectable"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
Item click count: @countClick";
    private readonly string example9CsharpCode = @"
private int countClick;
private bool reselectable = true;
";

    private readonly string example10RazorCode = @"
<style>
    .mobile-frame {
        width: 375px;
        height: 712px;
        border: 16px solid #333;
        border-radius: 36px;
        background-color: #fff;
        box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
        position: relative;
        overflow: hidden;
    }

    .mobile-frame::after {
        content: '';
        display: block;
        width: 60px;
        height: 5px;
        background: #333;
        border-radius: 10px;
        position: absolute;
        top: 10px;
        left: 50%;
        transform: translateX(-50%);
    }

    .screen {
        width: 100%;
        height: 100%;
        overflow: auto;
    }

    .nav-menu {
        background-color: #101419;
    }
</style>

<div class=""mobile-frame"">
    <div class=""screen"">
        <BitLayout>
            <Header>
                <BitCard FullWidth>
                    <BitStack Horizontal HorizontalAlign=""BitAlignment.Center"" VerticalAlign=""BitAlignment.Center"">
                        <BitImage Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo.svg"" Width=""50"" />
                        <BitText Typography=""BitTypography.H4"" Color=""BitColor.Info"">
                            bit BlazorUI
                        </BitText>
                    </BitStack>
                </BitCard>
            </Header>
            <Main>
                <BitStack HorizontalAlign=""BitAlignment.Center"" VerticalAlign=""BitAlignment.Center"">
                    <BitText Typography=""BitTypography.H4"" Color=""BitColor.PrimaryForeground"">
                        <BitIcon IconName=""@advancedSelectedOption?.IconName"" Color=""BitColor.PrimaryForeground"" Size=""BitSize.Large"" />
                        <span>@advancedSelectedOption?.Text</span>
                    </BitText>
                </BitStack>
            </Main>
            <Footer>
                <div class=""nav-menu"">
                    <BitNavBar TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"" @bind-SelectedItem=""advancedSelectedOption"">
                        <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
                        <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
                        <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
                        <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
                    </BitNavBar>
                </div>
            </Footer>
        </BitLayout>
    </div>
</div>";
    private readonly string example10CsharpCode = @"
private BitNavBarOption? advancedSelectedOption;
";

    private readonly string example11RazorCode = @"
<BitNavBar Color=""BitColor.Primary"" TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar Color=""BitColor.Secondary"" TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar Color=""BitColor.Tertiary"" TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar Color=""BitColor.Info"" TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar Color=""BitColor.Success"" TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar Color=""BitColor.Warning"" TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar Color=""BitColor.SevereWarning"" TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar Color=""BitColor.Error"" TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar Color=""BitColor.PrimaryBackground"" TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar Color=""BitColor.SecondaryBackground"" TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar Color=""BitColor.TertiaryBackground"" TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>

<BitNavBar Color=""BitColor.PrimaryForeground"" TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar Color=""BitColor.SecondaryForeground"" TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar Color=""BitColor.TertiaryForeground"" TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar Color=""BitColor.PrimaryBorder"" TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar Color=""BitColor.SecondaryBorder"" TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar Color=""BitColor.TertiaryBorder"" TItem=""BitNavBarOption"" Mode=""BitNavMode.Manual"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example12RazorCode = @"
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

<BitNavBar Classes=""@(new() { ItemIcon = ""custom-item-ico"", ItemText = ""custom-item-txt"" })"" TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>
<BitNavBar Styles=""@(new() { ItemIcon = ""color: aqua;"", ItemText = ""color: tomato;"" })"" TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";

    private readonly string example13RazorCode = @"
<BitNavBar Dir=""BitDir.Rtl"" TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""خانه"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""محصولات"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""آکادمی"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""پروفایل"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";
}
