namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Separator;

public partial class BitSeparatorDemo
{
    private readonly string example1RazorCode = @"
<BitSeparator />
<BitSeparator>Text</BitSeparator>
<BitSeparator><BitIcon IconName=""Clock"" /></BitSeparator>";

    private readonly string example2RazorCode = @"
<style>
    .custom-horizontal-layout {
        gap: 1rem;
        height: 3rem;
        display: flex;
        white-space: nowrap;
        align-items: center;
    }
</style>


<div class=""custom-horizontal-layout"">
    <span>Item 1</span>
    <BitSeparator Vertical />
    <span>Item 2</span>
    <BitSeparator Vertical />
    <span>Item 3</span>
    <BitSeparator Vertical />
    <span>Item 4</span>
    <BitSeparator Vertical />
    <span>Item 5</span>
</div>
";

    private readonly string example3RazorCode = @"
<BitSeparator AlignContent=""@BitSeparatorAlignContent.Center"">Center</BitSeparator>
<BitSeparator AlignContent=""@BitSeparatorAlignContent.Start"">Start</BitSeparator>
<BitSeparator AlignContent=""@BitSeparatorAlignContent.End"">End</BitSeparator>

<div style=""height: 13rem"">
    <BitSeparator Vertical AlignContent=""@BitSeparatorAlignContent.Center"">Center</BitSeparator>
    <BitSeparator Vertical AlignContent=""@BitSeparatorAlignContent.Start"">Start</BitSeparator>
    <BitSeparator Vertical AlignContent=""@BitSeparatorAlignContent.End"">End</BitSeparator>
</div>";

    private readonly string example4RazorCode = @"
<div style=""display:flex;flex-direction:column;align-items:center"">
    <BitSeparator>Default</BitSeparator>
    <BitSeparator AutoSize>AutoSize</BitSeparator>
</div>";

    private readonly string example5RazorCode = @"
<BitSeparator Background=""BitColorKind.Primary"">Primary</BitSeparator>
<BitSeparator Background=""BitColorKind.Secondary"">Secondary</BitSeparator>
<BitSeparator Background=""BitColorKind.Tertiary"">Tertiary</BitSeparator>
<BitSeparator Background=""BitColorKind.Transparent"">Transparent</BitSeparator>

<BitSeparator Border=""BitColorKind.Primary"">Primary</BitSeparator>
<BitSeparator Border=""BitColorKind.Secondary"">Secondary</BitSeparator>
<BitSeparator Border=""BitColorKind.Tertiary"">Tertiary</BitSeparator>
<BitSeparator Border=""BitColorKind.Transparent"">Transparent</BitSeparator>";
}
