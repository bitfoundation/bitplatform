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
<BitNavBar TItem=""BitNavBarOption"">
    <Options>
        <BitNavBarOption Text=""Home"" IconName=""@BitIconName.Home"" />
        <BitNavBarOption Text=""Products"" IconName=""@BitIconName.ProductVariant"" />
        <BitNavBarOption Text=""Academy"" IconName=""@BitIconName.LearningTools"" />
        <BitNavBarOption Text=""Profile"" IconName=""@BitIconName.Contact"" />
    </Options>
    <ItemTemplate Context=""option"">
        <span style=""font-size:12px"">@option.Text</span>
        <i class=""bit-icon bit-icon--@option.IconName"" />
    </ItemTemplate>
</BitNavBar>";
}
