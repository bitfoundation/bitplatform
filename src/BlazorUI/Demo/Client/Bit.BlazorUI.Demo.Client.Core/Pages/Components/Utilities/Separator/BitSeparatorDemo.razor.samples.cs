namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Separator;

public partial class BitSeparatorDemo
{
    private readonly string example1RazorCode = @"
<BitSeparator />
<BitSeparator>Text</BitSeparator>
<BitSeparator><BitIcon IconName=""Clock"" /></BitSeparator>";

    private readonly string example2RazorCode = @"
<style>
    .custom-auto-layout {
        gap: 1rem;
        display: flex;
        padding: 1rem 0;
        white-space: nowrap;
    }

    .custom-horizontal-layout {
        gap: 1rem;
        height: 3rem;
        display: flex;
        white-space: nowrap;
        align-items: center;
    }
</style>


<div class=""custom-auto-layout"">
    <span>Item 1</span>
    <BitSeparator Vertical />
    <span>Item 2</span>
    <BitSeparator Vertical />
    <span>Item 3</span>
</div>

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
    <BitSeparator Vertical AlignContent=""@BitSeparatorAlignContent.Start"" ContentOffset=""25%"">25%</BitSeparator>
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


<BitSeparator>Default</BitSeparator>
<BitSeparator Inset=""2rem"">2rem</BitSeparator>
<BitSeparator Inset=""25%"">25%</BitSeparator>

<div class=""custom-horizontal-layout"">
    <span>Item 1</span>
    <BitSeparator Vertical />
    <span>Item 2</span>
    <BitSeparator Vertical Inset=""0.75rem"" />
    <span>Item 3</span>
</div>";

    private readonly string example7RazorCode = @"
<style>
    .custom-horizontal-layout {
        gap: 1rem;
        height: 3rem;
        display: flex;
        white-space: nowrap;
        align-items: center;
    }
</style>


<BitSeparator>Default</BitSeparator>
<BitSeparator Thickness=""3px"">3px</BitSeparator>
<BitSeparator Thickness=""0.5rem"">0.5rem</BitSeparator>

<div class=""custom-horizontal-layout"">
    <span>Item 1</span>
    <BitSeparator Vertical Thickness=""3px"" />
    <span>Item 2</span>
    <BitSeparator Vertical Thickness=""0.5rem"" />
    <span>Item 3</span>
</div>";

    private readonly string example8RazorCode = @"
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
<BitSeparator LineStyle=""BitSeparatorLineStyle.Dotted"" Thickness=""3px"">Dotted</BitSeparator>
<BitSeparator LineStyle=""BitSeparatorLineStyle.Double"" Thickness=""3px"">Double</BitSeparator>

<div class=""custom-horizontal-layout"">
    <span>Item 1</span>
    <BitSeparator Vertical LineStyle=""BitSeparatorLineStyle.Dashed"" />
    <span>Item 2</span>
    <BitSeparator Vertical LineStyle=""BitSeparatorLineStyle.Dotted"" Thickness=""3px"" />
    <span>Item 3</span>
    <BitSeparator Vertical LineStyle=""BitSeparatorLineStyle.Double"" Thickness=""3px"" />
    <span>Item 4</span>
</div>";

    private readonly string example9RazorCode = @"
<BitSeparator />
<BitSeparator Decorative />";

    private readonly string example10RazorCode = @"
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

    private readonly string example11RazorCode = @"
<style>
    .custom-horizontal-layout {
        gap: 1rem;
        height: 3rem;
        display: flex;
        white-space: nowrap;
        align-items: center;
    }
</style>


<BitSeparator Size=""BitSize.Small"">Small</BitSeparator>
<BitSeparator Size=""BitSize.Medium"">Medium</BitSeparator>
<BitSeparator Size=""BitSize.Large"">Large</BitSeparator>

<div class=""custom-horizontal-layout"">
    <span>Small</span>
    <BitSeparator Vertical Size=""BitSize.Small"" />
    <span>Medium</span>
    <BitSeparator Vertical Size=""BitSize.Medium"" />
    <span>Large</span>
    <BitSeparator Vertical Size=""BitSize.Large"" />
    <span>Item</span>
</div>";

    private readonly string example12RazorCode = @"
<style>
    .custom-class::before,
    .custom-class::after {
        border: none;
        height: 3px;
    }

    .custom-class::before {
        background: linear-gradient(to right, transparent, dodgerblue);
    }

    .custom-class::after {
        background: linear-gradient(to right, dodgerblue, transparent);
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

    private readonly string example13RazorCode = @"
<div dir=""rtl"">
    <BitSeparator Dir=""BitDir.Rtl"">جداکننده</BitSeparator>
    <BitSeparator Dir=""BitDir.Rtl"" AlignContent=""@BitSeparatorAlignContent.Start"">ابتدا</BitSeparator>
    <BitSeparator Dir=""BitDir.Rtl"" AlignContent=""@BitSeparatorAlignContent.End"">انتها</BitSeparator>
</div>";
}
