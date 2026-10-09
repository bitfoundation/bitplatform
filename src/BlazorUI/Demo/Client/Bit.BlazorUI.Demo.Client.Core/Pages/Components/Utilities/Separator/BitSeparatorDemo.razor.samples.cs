namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Separator;

public partial class BitSeparatorDemo
{
    private readonly string example1RazorCode = @"
<BitSeparator />
<BitSeparator>Text</BitSeparator>
<BitSeparator><BitIcon IconName=""@BitIconName.Clock"" /></BitSeparator>
<BitSeparator Disabled>Disabled</BitSeparator>";

    private readonly string example2RazorCode = @"
<style>
    .row {
        gap: 1rem;
        display: flex;
        white-space: nowrap;
        align-items: center;
    }

    .medium {
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

<div class=""row medium"">
    <span>Sign in with a password</span>
    <BitSeparator Vertical>OR</BitSeparator>
    <span>Sign in with a passkey</span>
</div>

<div>Docs <BitSeparator Vertical /> Blog <BitSeparator Vertical /> GitHub</div>";

    private readonly string example3RazorCode = @"
<style>
    .row {
        gap: 1rem;
        display: flex;
        white-space: nowrap;
        align-items: center;
    }

    .taller {
        gap: 2rem;
        height: 12rem;
    }
</style>


<BitSeparator AlignContent=""BitPlacement.Start"">Start</BitSeparator>
<BitSeparator AlignContent=""BitPlacement.Center"">Center</BitSeparator>
<BitSeparator AlignContent=""BitPlacement.End"">End</BitSeparator>
<BitSeparator AlignContent=""BitPlacement.Start"" ContentOffset=""2rem"">Start, 2rem</BitSeparator>
<BitSeparator AlignContent=""BitPlacement.End"" ContentOffset=""10%"">End, 10%</BitSeparator>

<div class=""row taller"">
    <BitSeparator Vertical AlignContent=""BitPlacement.Start"">Start</BitSeparator>
    <BitSeparator Vertical AlignContent=""BitPlacement.Center"">Center</BitSeparator>
    <BitSeparator Vertical AlignContent=""BitPlacement.End"">End</BitSeparator>
    <BitSeparator Vertical AlignContent=""BitPlacement.Start"" ContentOffset=""25%"">25%</BitSeparator>
</div>";

    private readonly string example4RazorCode = @"
<style>
    .row {
        gap: 1rem;
        display: flex;
        white-space: nowrap;
        align-items: center;
    }

    .tall {
        height: 3rem;
    }
</style>


<BitSeparator>Full length</BitSeparator>
<BitSeparator Inset=""2rem"">Inset 2rem</BitSeparator>
<BitSeparator Inset=""25% 0"">Inset 25% at the start</BitSeparator>

<div class=""row tall"">
    <span>Item 1</span>
    <BitSeparator Vertical />
    <span>Item 2</span>
    <BitSeparator Vertical Inset=""0.75rem"" />
    <span>Item 3</span>
    <BitSeparator Vertical AutoSize />
    <span>Item 4</span>
</div>";

    private readonly string example5RazorCode = @"
<style>
    .row {
        gap: 1rem;
        display: flex;
        white-space: nowrap;
        align-items: center;
    }

    .tall {
        height: 3rem;
    }
</style>


<BitSeparator LineStyle=""BitLineStyle.Solid"">Solid</BitSeparator>
<BitSeparator LineStyle=""BitLineStyle.Dashed"">Dashed</BitSeparator>
<BitSeparator LineStyle=""BitLineStyle.Dotted"" Thickness=""3px"">Dotted, 3px</BitSeparator>
<BitSeparator LineStyle=""BitLineStyle.Double"" Thickness=""4px"">Double, 4px</BitSeparator>
<BitSeparator Thickness=""0.5rem"">Solid, 0.5rem</BitSeparator>

<div class=""row tall"">
    <span>Item 1</span>
    <BitSeparator Vertical LineStyle=""BitLineStyle.Dashed"" />
    <span>Item 2</span>
    <BitSeparator Vertical LineStyle=""BitLineStyle.Dotted"" Thickness=""3px"" />
    <span>Item 3</span>
    <BitSeparator Vertical LineStyle=""BitLineStyle.Double"" Thickness=""4px"" />
    <span>Item 4</span>
</div>";

    private readonly string example6RazorCode = @"
<BitSeparator>Named by its content</BitSeparator>
<BitSeparator AriaLabel=""End of the shipping details"" />
<BitSeparator Decorative />
<BitSeparator Decorative>Read as plain text</BitSeparator>";

    private readonly string example7RazorCode = @"
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
    <BitSeparator Element=""li"" Inset=""2.5rem 0"" Decorative />
    <li><BitPersona PrimaryText=""Aaron Reid"" Size=""BitPersonaSize.Size32"" /></li>
    <BitSeparator Element=""li"" Inset=""2.5rem 0"" Decorative />
    <li><BitPersona PrimaryText=""Alex Lundberg"" Size=""BitPersonaSize.Size32"" /></li>
</ul>";

    private readonly string example8RazorCode = @"
<BitParams Parameters=""separatorParams"">
    <BitSeparator>Account</BitSeparator>
    <BitSeparator>Billing</BitSeparator>
    <BitSeparator LineStyle=""BitLineStyle.Solid"">Danger zone</BitSeparator>
</BitParams>";
    private readonly string example8CsharpCode = @"
private readonly BitSeparatorParams[] separatorParams =
[
    new()
    {
        Thickness = ""2px"",
        LineStyle = BitLineStyle.Dashed,
        AlignContent = BitPlacement.Start,
    }
];";

    private readonly string example9RazorCode = @"
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

    private readonly string example10RazorCode = @"
<style>
    .row {
        gap: 1rem;
        display: flex;
        white-space: nowrap;
        align-items: center;
    }

    .tall {
        height: 3rem;
    }
</style>


<BitSeparator Size=""BitSize.Small"">Small</BitSeparator>
<BitSeparator Size=""BitSize.Medium"">Medium</BitSeparator>
<BitSeparator Size=""BitSize.Large"">Large</BitSeparator>

<div class=""row tall"">
    <span>Small</span>
    <BitSeparator Vertical Size=""BitSize.Small"" />
    <span>Medium</span>
    <BitSeparator Vertical Size=""BitSize.Medium"" />
    <span>Large</span>
    <BitSeparator Vertical Size=""BitSize.Large"" />
    <span>Item</span>
</div>";

    private readonly string example11RazorCode = @"
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

    private readonly string example12RazorCode = @"
<BitSeparator Dir=""BitDir.Rtl"">جداکننده</BitSeparator>
<BitSeparator Dir=""BitDir.Rtl"" AlignContent=""BitPlacement.Start"">ابتدا</BitSeparator>
<BitSeparator Dir=""BitDir.Rtl"" AlignContent=""BitPlacement.End"">انتها</BitSeparator>";
}
