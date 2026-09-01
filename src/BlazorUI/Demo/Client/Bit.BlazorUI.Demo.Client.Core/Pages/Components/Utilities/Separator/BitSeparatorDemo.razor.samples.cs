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
<BitSeparator AlignContent=""@BitSeparatorAlignContent.Start"" ContentOffset=""2rem"">Start, 2rem</BitSeparator>
<BitSeparator AlignContent=""@BitSeparatorAlignContent.End"" ContentOffset=""10%"">End, 10%</BitSeparator>

<div style=""height: 13rem"">
    <BitSeparator Vertical AlignContent=""@BitSeparatorAlignContent.Start"" ContentOffset=""2rem"">Start</BitSeparator>
    <BitSeparator Vertical AlignContent=""@BitSeparatorAlignContent.End"" ContentOffset=""2rem"">End</BitSeparator>
</div>";

    private readonly string example5RazorCode = @"
<div style=""display:flex;flex-direction:column;align-items:center"">
    <BitSeparator>Default</BitSeparator>
    <BitSeparator AutoSize>AutoSize</BitSeparator>
</div>";

    private readonly string example6RazorCode = @"
<style>
    .custom-horizontal-layout {
        gap: 1rem;
        height: 3rem;
        display: flex;
        white-space: nowrap;
        align-items: center;
    }
</style>


<BitSeparator>Solid</BitSeparator>
<BitSeparator LineStyle=""BitSeparatorLineStyle.Dashed"">Dashed</BitSeparator>
<BitSeparator LineStyle=""BitSeparatorLineStyle.Dotted"">Dotted</BitSeparator>

<div class=""custom-horizontal-layout"">
    <span>Item 1</span>
    <BitSeparator Vertical LineStyle=""BitSeparatorLineStyle.Dashed"" />
    <span>Item 2</span>
    <BitSeparator Vertical LineStyle=""BitSeparatorLineStyle.Dotted"" />
    <span>Item 3</span>
</div>";

    private readonly string example7RazorCode = @"
<BitSeparator>Default</BitSeparator>
<BitSeparator Thickness=""3px"">3px</BitSeparator>
<BitSeparator Thickness=""0.5rem"">0.5rem</BitSeparator>
<BitSeparator Thickness=""3px"" LineStyle=""BitSeparatorLineStyle.Dotted"">Dotted 3px</BitSeparator>";

    private readonly string example8RazorCode = @"
<BitSeparator />
<BitSeparator Decorative />";

    private readonly string example9RazorCode = @"
<BitSeparator Color=""BitColor.Primary"">Primary</BitSeparator>
<BitSeparator Color=""BitColor.Secondary"">Secondary</BitSeparator>
<BitSeparator Color=""BitColor.Tertiary"">Tertiary</BitSeparator>
<BitSeparator Color=""BitColor.Info"">Info</BitSeparator>
<BitSeparator Color=""BitColor.Success"">Success</BitSeparator>
<BitSeparator Color=""BitColor.Warning"">Warning</BitSeparator>
<BitSeparator Color=""BitColor.SevereWarning"">SevereWarning</BitSeparator>
<BitSeparator Color=""BitColor.Error"">Error</BitSeparator>

<BitSeparator Border=""BitColorKind.Primary"">Primary</BitSeparator>
<BitSeparator Border=""BitColorKind.Secondary"">Secondary</BitSeparator>
<BitSeparator Border=""BitColorKind.Tertiary"">Tertiary</BitSeparator>
<BitSeparator Border=""BitColorKind.Transparent"">Transparent</BitSeparator>

<BitSeparator Background=""BitColorKind.Primary"">Primary</BitSeparator>
<BitSeparator Background=""BitColorKind.Secondary"">Secondary</BitSeparator>
<BitSeparator Background=""BitColorKind.Tertiary"">Tertiary</BitSeparator>
<BitSeparator Background=""BitColorKind.Transparent"">Transparent</BitSeparator>";

    private readonly string example10RazorCode = @"
<style>
    .custom-class::before {
        border: none;
        height: 3px;
        background: linear-gradient(90deg, transparent, dodgerblue, transparent);
    }

    .custom-content {
        color: white;
        padding: 0.25rem 1rem;
        border-radius: 1rem;
        background: linear-gradient(90deg, mediumorchid, dodgerblue);
    }
</style>


<BitSeparator Style=""max-width: 20rem; margin: auto;"">Styled</BitSeparator>
<BitSeparator Class=""custom-class"">Classed</BitSeparator>

<BitSeparator Styles=""@(new() { Root = ""text-transform: uppercase;"",
                                Content = ""color: dodgerblue; font-weight: 600;"" })"">
    Styles
</BitSeparator>
<BitSeparator Classes=""@(new() { Content = ""custom-content"" })"">Classes</BitSeparator>";

    private readonly string example11RazorCode = @"
<div dir=""rtl"">
    <BitSeparator Dir=""BitDir.Rtl"">جداکننده</BitSeparator>
    <BitSeparator Dir=""BitDir.Rtl"" AlignContent=""@BitSeparatorAlignContent.Start"">ابتدا</BitSeparator>
    <BitSeparator Dir=""BitDir.Rtl"" AlignContent=""@BitSeparatorAlignContent.End"">انتها</BitSeparator>
</div>";
}
