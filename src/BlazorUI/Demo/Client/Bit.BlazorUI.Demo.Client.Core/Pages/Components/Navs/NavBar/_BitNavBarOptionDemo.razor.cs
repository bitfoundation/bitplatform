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
}
