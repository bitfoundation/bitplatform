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

    private readonly string example5RazorCode = @"
<BitNavBar TItem=""BitNavBarOption"" IconOnly>
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
        <span style=""font-size:12px"">@option.Text</span>
        <i class=""bit-icon bit-icon--@option.IconName"" />
    </ItemTemplate>
</BitNavBar>";

    private readonly string example7CsharpCode = @"
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

    private readonly string example8CsharpCode = @"
<BitNavBar Dir=""BitDir.Rtl"" TItem=""BitNavBarOption"">
    <BitNavBarOption Text=""خانه"" IconName=""@BitIconName.Home"" />
    <BitNavBarOption Text=""محصولات"" IconName=""@BitIconName.ProductVariant"" />
    <BitNavBarOption Text=""آکادمی"" IconName=""@BitIconName.LearningTools"" />
    <BitNavBarOption Text=""پروفایل"" IconName=""@BitIconName.Contact"" />
</BitNavBar>";
}
