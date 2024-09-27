namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Layouts.Spacer;

public partial class BitSpacerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Width",
            Type = "int?",
            DefaultValue = "null",
            Description = "Gets or sets the width of the spacer (in pixels).",
        },
    ];



    private readonly string example1RazorCode = @"
<div style=""display: flex; width: 100%;"">
    <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.GlobalNavButton"" />
    <BitSpacer />
    <BitText Typography=""BitTypography.H6"">Title</BitText>
    <BitSpacer />
    <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.Contact"" />
</div>";

    private readonly string example2RazorCode = @"
<div style=""display: flex; width: 100%;"">
    <BitProgress Circular Indeterminate />
    <BitSpacer Width=""64"" />
    <BitProgress Circular Indeterminate />
    <BitSpacer Width=""64"" />
    <BitProgress Circular Indeterminate />
</div>";

}
