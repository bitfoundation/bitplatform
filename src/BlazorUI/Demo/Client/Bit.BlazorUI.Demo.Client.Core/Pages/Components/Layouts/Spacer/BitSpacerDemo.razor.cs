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
    <BitIconButton IconName=""@BitIconName.GlobalNavButton"" />
    <BitSpacer />
    <BitTypography Variant=""BitTypographyVariant.H6"">Title</BitTypography>
    <BitSpacer />
    <BitIconButton IconName=""@BitIconName.Contact"" />
</div>";

    private readonly string example2RazorCode = @"
<div style=""display: flex; width: 100%;"">
    <BitSpinner />
    <BitSpacer Width=""64"" />
    <BitSpinner />
    <BitSpacer Width=""64"" />
    <BitSpinner />
</div>";

}
