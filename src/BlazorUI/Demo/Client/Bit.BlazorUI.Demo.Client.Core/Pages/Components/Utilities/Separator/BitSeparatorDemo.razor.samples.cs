namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Separator;

public partial class BitSeparatorDemo
{
    private readonly string example1RazorCode = @"
<BitSeparator />
<BitSeparator>Text</BitSeparator>
<BitSeparator><BitIcon IconName=""@BitIconName.Clock"" /></BitSeparator>
<BitSeparator IsEnabled=""false"">Disabled</BitSeparator>";

    private readonly string example2RazorCode = @"
<style>
    .row {
        gap: 1rem;
        display: flex;
        white-space: nowrap;
        align-items: center;
    }

    .tall {
        height: 6rem;
    }
</style>


<div class=""row"">
    <span>Item 1</span>
    <BitSeparator Vertical />
    <span>Item 2</span>
    <BitSeparator Vertical />
    <span>Item 3</span>
</div>

<div class=""row tall"">
    <span>Sign in with a password</span>
    <BitSeparator Vertical>OR</BitSeparator>
    <span>Sign in with a passkey</span>
</div>";

    private readonly string example3RazorCode = @"
<style>
    .row {
        gap: 2rem;
        height: 12rem;
        display: flex;
    }
</style>


<BitSeparator AlignContent=""BitSeparatorAlignContent.Start"">Start</BitSeparator>
<BitSeparator AlignContent=""BitSeparatorAlignContent.Center"">Center</BitSeparator>
<BitSeparator AlignContent=""BitSeparatorAlignContent.End"">End</BitSeparator>
<BitSeparator AlignContent=""BitSeparatorAlignContent.Start"" ContentOffset=""2rem"">Start, 2rem</BitSeparator>
<BitSeparator AlignContent=""BitSeparatorAlignContent.End"" ContentOffset=""10%"">End, 10%</BitSeparator>

<div class=""row"">
    <BitSeparator Vertical AlignContent=""BitSeparatorAlignContent.Start"">Start</BitSeparator>
    <BitSeparator Vertical AlignContent=""BitSeparatorAlignContent.Center"">Center</BitSeparator>
    <BitSeparator Vertical AlignContent=""BitSeparatorAlignContent.End"">End</BitSeparator>
    <BitSeparator Vertical AlignContent=""BitSeparatorAlignContent.Start"" ContentOffset=""25%"">25%</BitSeparator>
</div>";

    private readonly string example4RazorCode = @"
<style>
    .row {
        gap: 1rem;
        height: 3rem;
        display: flex;
        white-space: nowrap;
        align-items: center;
    }
</style>


<BitSeparator>Full length</BitSeparator>
<BitSeparator Inset=""2rem"">Inset 2rem</BitSeparator>
<BitSeparator Inset=""25% 0"">Inset 25% at the start</BitSeparator>

<div class=""row"">
    <span>Item 1</span>
    <BitSeparator Vertical />
    <span>Item 2</span>
    <BitSeparator Vertical Inset=""0.75rem"" />
    <span>Item 3</span>
</div>

<BitSeparator AutoSize Style=""margin-inline: 4rem"">AutoSize, with a margin</BitSeparator>";

    private readonly string example5RazorCode = @"
<style>
    .row {
        gap: 1rem;
        height: 3rem;
        display: flex;
        white-space: nowrap;
        align-items: center;
    }
</style>


<BitSeparator LineStyle=""BitSeparatorLineStyle.Solid"">Solid</BitSeparator>
<BitSeparator LineStyle=""BitSeparatorLineStyle.Dashed"">Dashed</BitSeparator>
<BitSeparator LineStyle=""BitSeparatorLineStyle.Dotted"" Thickness=""3px"">Dotted, 3px</BitSeparator>
<BitSeparator LineStyle=""BitSeparatorLineStyle.Double"" Thickness=""4px"">Double, 4px</BitSeparator>
<BitSeparator Thickness=""0.5rem"">Solid, 0.5rem</BitSeparator>

<div class=""row"">
    <span>Item 1</span>
    <BitSeparator Vertical LineStyle=""BitSeparatorLineStyle.Dashed"" />
    <span>Item 2</span>
    <BitSeparator Vertical LineStyle=""BitSeparatorLineStyle.Dotted"" Thickness=""3px"" />
    <span>Item 3</span>
    <BitSeparator Vertical LineStyle=""BitSeparatorLineStyle.Double"" Thickness=""4px"" />
    <span>Item 4</span>
</div>";

    private readonly string example6RazorCode = @"
<style>
    .list {
        margin: 0;
        padding: 0;
        max-width: 20rem;
        list-style: none;
    }

    .list > li:not([role]) {
        padding: 0.5rem 0;
    }
</style>


<ul class=""list"" aria-label=""Contacts"">
    <li><BitPersona PrimaryText=""Annie Lindqvist"" Size=""BitPersonaSize.Size32"" /></li>
    <BitSeparator Element=""li"" Inset=""2.5rem 0"" />
    <li><BitPersona PrimaryText=""Aaron Reid"" Size=""BitPersonaSize.Size32"" /></li>
    <BitSeparator Element=""li"" Inset=""2.5rem 0"" />
    <li><BitPersona PrimaryText=""Alex Lundberg"" Size=""BitPersonaSize.Size32"" /></li>
</ul>";

    private readonly string example7RazorCode = @"
<BitSeparator>Named by its content</BitSeparator>
<BitSeparator AriaLabel=""End of the shipping details"" />
<BitSeparator Decorative />
<BitSeparator Decorative>Read as plain text</BitSeparator>";

    private readonly string example8RazorCode = @"
<div style=""--bit-Separator-color: var(--bit-clr-pri); --bit-Separator-line-style: dashed; --bit-Separator-content-color: var(--bit-clr-fg-sec); --bit-Separator-content-font-size: 0.75rem;"">
    <BitSeparator>Set on the container</BitSeparator>
    <BitSeparator AlignContent=""BitSeparatorAlignContent.Start"">for every separator inside it</BitSeparator>
    <BitSeparator Color=""BitColor.Error"">Color wins over the variable</BitSeparator>
</div>

<BitSeparator Style=""--bit-Separator-thickness: 4px; --bit-Separator-spacing: 1rem; --bit-Separator-content-gap: 0.25rem; --bit-Separator-content-font-weight: 600;"">
    Set on one separator
</BitSeparator>";

    private readonly string example9RazorCode = @"
<BitParams Parameters=""separatorParams"">
    <BitSeparator>Account</BitSeparator>
    <BitSeparator>Billing</BitSeparator>
    <BitSeparator Color=""BitColor.Error"" LineStyle=""BitSeparatorLineStyle.Solid"">Danger zone</BitSeparator>
</BitParams>";
    private readonly string example9CsharpCode = @"
private readonly BitSeparatorParams[] separatorParams =
[
    new()
    {
        Color = BitColor.Primary,
        LineStyle = BitSeparatorLineStyle.Dashed,
        AlignContent = BitSeparatorAlignContent.Start,
    }
];";

    private readonly string example10RazorCode = @"
<BitSeparator Color=""BitColor.Primary"">Primary</BitSeparator>
<BitSeparator Color=""BitColor.Secondary"">Secondary</BitSeparator>
<BitSeparator Color=""BitColor.Tertiary"">Tertiary</BitSeparator>
<BitSeparator Color=""BitColor.Info"">Info</BitSeparator>
<BitSeparator Color=""BitColor.Success"">Success</BitSeparator>
<BitSeparator Color=""BitColor.Warning"">Warning</BitSeparator>
<BitSeparator Color=""BitColor.SevereWarning"">SevereWarning</BitSeparator>
<BitSeparator Color=""BitColor.Error"">Error</BitSeparator>

<BitSeparator Border=""BitColorKind.Primary"">Primary border</BitSeparator>
<BitSeparator Border=""BitColorKind.Secondary"">Secondary border</BitSeparator>
<BitSeparator Border=""BitColorKind.Tertiary"">Tertiary border</BitSeparator>
<BitSeparator Border=""BitColorKind.Transparent"">Transparent border</BitSeparator>

<BitSeparator Background=""BitColorKind.Primary"">Primary background</BitSeparator>
<BitSeparator Background=""BitColorKind.Secondary"">Secondary background</BitSeparator>
<BitSeparator Background=""BitColorKind.Tertiary"">Tertiary background</BitSeparator>
<BitSeparator Background=""BitColorKind.Transparent"">Transparent background</BitSeparator>";

    private readonly string example11RazorCode = @"
<style>
    .row {
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

<div class=""row"">
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

<BitSeparator Styles=""@(new() { Root = ""text-transform: uppercase;"", Content = ""color: dodgerblue; font-weight: 600;"" })"">
    Styles
</BitSeparator>
<BitSeparator Classes=""@(new() { Content = ""custom-content"" })"">Classes</BitSeparator>";

    private readonly string example13RazorCode = @"
<BitSeparator Dir=""BitDir.Rtl"">جداکننده</BitSeparator>
<BitSeparator Dir=""BitDir.Rtl"" AlignContent=""BitSeparatorAlignContent.Start"">ابتدا</BitSeparator>
<BitSeparator Dir=""BitDir.Rtl"" AlignContent=""BitSeparatorAlignContent.End"">انتها</BitSeparator>";
}
