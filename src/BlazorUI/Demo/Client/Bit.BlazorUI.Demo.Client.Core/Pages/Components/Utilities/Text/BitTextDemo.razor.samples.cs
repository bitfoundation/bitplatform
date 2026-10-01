namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Text;

public partial class BitTextDemo
{
    private readonly string example1RazorCode = @"
<BitText>Default (Subtitle1)</BitText>

<BitText Typography=""BitTypography.H1"">H1. Heading</BitText>
<BitText Typography=""BitTypography.H2"">H2. Heading</BitText>
<BitText Typography=""BitTypography.H3"">H3. Heading</BitText>
<BitText Typography=""BitTypography.H4"">H4. Heading</BitText>
<BitText Typography=""BitTypography.H5"">H5. Heading</BitText>
<BitText Typography=""BitTypography.H6"">H6. Heading</BitText>

<BitText Typography=""BitTypography.Subtitle1"">Subtitle1. Once upon a time</BitText>
<BitText Typography=""BitTypography.Subtitle2"">Subtitle2. Once upon a time</BitText>

<BitText Typography=""BitTypography.Body1"">Body1. Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.</BitText>
<BitText Typography=""BitTypography.Body2"">Body2. Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.</BitText>

<BitText Typography=""BitTypography.Button"">Button. Click me</BitText>
<BitText Typography=""BitTypography.Caption1"">Caption1. Hello world!</BitText>
<BitText Typography=""BitTypography.Caption2"">Caption2. Hello world!</BitText>
<BitText Typography=""BitTypography.Overline"">Overline. This is overline text</BitText>

<div style=""font-style:italic;font-size:1.25rem;color:tomato"">
    Around it, <BitText Typography=""BitTypography.Inherit"">Inherit</BitText> keeps the paragraph's look.
</div>";

    private readonly string example2RazorCode = @"
<BitText Element=""h2"" Typography=""BitTypography.H4"">An h2 drawn at the size of an h4</BitText>
<BitText Element=""span"" Typography=""BitTypography.H4"">An h4 look with no heading semantics (span)</BitText>
<BitText Element=""strong"" Typography=""BitTypography.Body1"">Strongly emphasized body text</BitText>
<BitText Element=""blockquote"" Typography=""BitTypography.Body1"">A quotation, in a blockquote</BitText>
<BitText Element=""code"" Typography=""BitTypography.Body2"">var text = new BitText();</BitText>
<BitText Element=""not a tag name"" Typography=""BitTypography.Body2"">An invalid tag name falls back to the variant's tag (p)</BitText>
<BitText Element=""hr"">A void element holds no content, so this is not rendered</BitText>";

    private readonly string example3RazorCode = @"
<BitText Weight=""BitFontWeight.Light"">Light weight</BitText>
<BitText Weight=""BitFontWeight.Regular"">Regular weight</BitText>
<BitText Weight=""BitFontWeight.Medium"">Medium weight</BitText>
<BitText Weight=""BitFontWeight.Semibold"">Semibold weight</BitText>
<BitText Weight=""BitFontWeight.Bold"">Bold weight</BitText>

<BitText Italic>Italic</BitText>
<BitText Underline>Underline</BitText>
<BitText Strikethrough>Strikethrough</BitText>
<BitText Underline Strikethrough>Underline and strikethrough</BitText>

<BitText Transform=""BitTextTransform.Uppercase"">Uppercase transform</BitText>
<BitText Transform=""BitTextTransform.Capitalize"">capitalize transform</BitText>
<BitText Typography=""BitTypography.Overline"" Transform=""BitTextTransform.None"">None, undoing the overline's uppercase</BitText>";

    private readonly string example4RazorCode = @"
<BitText Typography=""BitTypography.Caption1"">Default</BitText>
<BitText Typography=""BitTypography.Body1"">1,111.11</BitText>
<BitText Typography=""BitTypography.Body1"">8,888.88</BitText>

<BitText Typography=""BitTypography.Caption1"">Numeric</BitText>
<BitText Typography=""BitTypography.Body1"" Numeric>1,111.11</BitText>
<BitText Typography=""BitTypography.Body1"" Numeric>8,888.88</BitText>

<BitText Typography=""BitTypography.Caption1"">Monospace</BitText>
<BitText Typography=""BitTypography.Body1"" Monospace>1,111.11</BitText>
<BitText Typography=""BitTypography.Body1"" Monospace>8,888.88</BitText>

<BitText Element=""code"" Typography=""BitTypography.Body2"" Monospace>var text = new BitText { Monospace = true };</BitText>";

    private readonly string example5RazorCode = @"
<div style=""width:250px"">
    <BitText Typography=""BitTypography.Caption1"">NoWrap</BitText>
    <BitText NoWrap>Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.</BitText>
</div>

<div style=""width:250px"">
    <BitText Typography=""BitTypography.Caption1"">NoWrap + Block on an inline caption</BitText>
    <BitText Typography=""BitTypography.Caption2"" NoWrap Block>Once upon a time, stories wove connections between people, a symphony of voices.</BitText>
</div>

<div style=""width:250px"">
    <BitText Typography=""BitTypography.Caption1"">LineClamp=""2"", with a title</BitText>
    <BitText LineClamp=""2"" title=""Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams that outlasted every night they were told in."">Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams that outlasted every night they were told in.</BitText>
</div>";

    private readonly string example6RazorCode = @"
<div style=""width:250px"">
    <BitText Typography=""BitTypography.Caption1"">BreakWord</BitText>
    <BitText BreakWord>A path: /a/very/long/path/segment/that/never/breaks/on/its/own/anywhere.txt</BitText>
</div>

<div style=""width:250px"">
    <BitText Typography=""BitTypography.Caption1"">ForceBreak</BitText>
    <BitText ForceBreak>1234567890123456789012345678901234567890123456789012345678901234567890</BitText>
</div>

<div style=""width:250px"">
    <BitText Typography=""BitTypography.Caption1"">Wrap=""Balance""</BitText>
    <BitText Typography=""BitTypography.H5"" Wrap=""BitTextWrap.Balance"">A heading whose lines are balanced against each other</BitText>
</div>

<div style=""width:250px"">
    <BitText Typography=""BitTypography.Caption1"">Hyphenate</BitText>
    <BitText Lang=""en"" Hyphenate>An incomprehensibly complicated internationalization responsibility.</BitText>
</div>

<div style=""width:250px"">
    <BitText Typography=""BitTypography.Caption1"">PreserveWhitespace</BitText>
    <BitText PreserveWhitespace>@(@""Dear reader,

    Blank lines and indents survive,
    and a line this long still wraps."")</BitText>
</div>";

    private readonly string example7RazorCode = @"
<div style=""width:250px"">
    <BitText Align=""BitTextAlign.Start"">Start</BitText>
    <BitText Align=""BitTextAlign.Center"">Center</BitText>
    <BitText Align=""BitTextAlign.End"">End</BitText>
    <BitText Align=""BitTextAlign.Justify"">Justify. Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.</BitText>
</div>";

    private readonly string example8RazorCode = @"
<div style=""border:1px dashed gray"">
    <BitText Typography=""BitTypography.H5"" Gutter>A heading with a gutter</BitText>
    <BitText Typography=""BitTypography.Body1"" Gutter>A paragraph with a smaller gutter</BitText>
    <BitText Typography=""BitTypography.Body1"">A paragraph with none</BitText>
</div>

<div style=""display:flex;gap:2rem;align-items:flex-start"">
    <div style=""border:1px dashed gray""><BitText Typography=""BitTypography.H4"">None</BitText></div>
    <div style=""border:1px dashed gray""><BitText Typography=""BitTypography.H4"" Trim=""BitTextTrim.Start"">Start</BitText></div>
    <div style=""border:1px dashed gray""><BitText Typography=""BitTypography.H4"" Trim=""BitTextTrim.End"">End</BitText></div>
    <div style=""border:1px dashed gray""><BitText Typography=""BitTypography.H4"" Trim=""BitTextTrim.Both"">Both</BitText></div>
</div>";

    private readonly string example9RazorCode = @"
<BitText Foreground=""BitColorKind.Primary"">Primary foreground</BitText>
<BitText Foreground=""BitColorKind.Secondary"">Secondary foreground</BitText>
<BitText Foreground=""BitColorKind.Tertiary"">Tertiary foreground</BitText>

<BitText Typography=""BitTypography.H3"" Gradient=""linear-gradient(90deg, #7c3aed, #06b6d4)"">A gradient headline</BitText>
<BitText Typography=""BitTypography.H4"" Weight=""BitFontWeight.Bold"" Gradient=""radial-gradient(circle at 30% 50%, #06b6d4, #7c3aed)"">A radial gradient</BitText>
<BitText Gradient=""linear-gradient(90deg, var(--bit-clr-fg-pri), transparent)"">Body text fading out into nothing</BitText>";

    private readonly string example10RazorCode = @"
<BitText Element=""div"" Typography=""BitTypography.H5"" AriaLevel=""3"">A div announced as a level 3 heading</BitText>
<BitText Element=""h2"" Typography=""BitTypography.H5"" AriaLevel=""4"">An h2 announced as a level 4 heading</BitText>

<BitText VisuallyHidden>Read out by a screen reader, drawn nowhere.</BitText>
<BitText Element=""a"" VisuallyHidden href=""#example11"">Skip to the next example</BitText>

<BitText Lang=""fr"">Bonjour tout le monde</BitText>

<BitText NoSelect>This text cannot be selected</BitText>";

    private readonly string example11RazorCode = @"
<div>Visible: [<BitText Typography=""BitTypography.Inherit"" Visibility=""BitVisibility.Visible"">Visible text</BitText>]</div>
<div>Hidden: [<BitText Typography=""BitTypography.Inherit"" Visibility=""BitVisibility.Hidden"">Hidden text</BitText>]</div>
<div>Collapsed: [<BitText Typography=""BitTypography.Inherit"" Visibility=""BitVisibility.Collapsed"">Collapsed text</BitText>]</div>

<BitText IsEnabled=""false"">Disabled text</BitText>
<BitText IsEnabled=""false"" Gradient=""linear-gradient(90deg, #7c3aed, #06b6d4)"">Disabled text, keeping its gradient</BitText>";

    private readonly string example12RazorCode = @"
<BitParams Parameters=""@textParams"">
    <BitText>Takes the variant, the weight and the transform from the cascade</BitText>
    <BitText>So does this one</BitText>
    <BitText Transform=""BitTextTransform.None"" Italic>Its own transform, the cascaded variant and weight</BitText>
</BitParams>

<BitText>Outside the cascade</BitText>";
    private readonly string example12CsharpCode = @"
private readonly BitTextParams[] textParams =
[
    new()
    {
        Typography = BitTypography.Body1,
        Weight = BitFontWeight.Semibold,
        Transform = BitTextTransform.Uppercase,
    }
];";

    private readonly string example13RazorCode = @"
<BitText Color=""BitColor.Primary"">Primary</BitText>
<BitText Color=""BitColor.Secondary"">Secondary</BitText>
<BitText Color=""BitColor.Tertiary"">Tertiary</BitText>

<BitText Color=""BitColor.Info"">Info</BitText>
<BitText Color=""BitColor.Success"">Success</BitText>
<BitText Color=""BitColor.Warning"">Warning</BitText>
<BitText Color=""BitColor.SevereWarning"">SevereWarning</BitText>
<BitText Color=""BitColor.Error"">Error</BitText>

<div style=""background:var(--bit-clr-fg-sec);padding:1rem"">
    <BitText Color=""BitColor.PrimaryBackground"">PrimaryBackground</BitText>
    <BitText Color=""BitColor.SecondaryBackground"">SecondaryBackground</BitText>
    <BitText Color=""BitColor.TertiaryBackground"">TertiaryBackground</BitText>
</div>

<BitText Color=""BitColor.PrimaryForeground"">PrimaryForeground</BitText>
<BitText Color=""BitColor.SecondaryForeground"">SecondaryForeground</BitText>
<BitText Color=""BitColor.TertiaryForeground"">TertiaryForeground</BitText>

<BitText Color=""BitColor.PrimaryBorder"">PrimaryBorder</BitText>
<BitText Color=""BitColor.SecondaryBorder"">SecondaryBorder</BitText>
<BitText Color=""BitColor.TertiaryBorder"">TertiaryBorder</BitText>";

    private readonly string example14RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem;
        border: 1px solid gray;
    }
</style>

<BitText Style=""color: tomato; font-weight: bold;"">Styled through Style</BitText>
<BitText Class=""custom-class"">Classed through Class</BitText>

<BitText Align=""BitTextAlign.Center""
         @attributes=""@(new Dictionary<string, object> { [""class""] = ""custom-class"", [""style""] = ""width:250px"" })"">
    Splatted class and style, kept beside the alignment
</BitText>

<div style=""--bit-Text-color: #0f766e;
            --bit-Text-heading-font-family: Georgia, serif;
            --bit-Text-decoration-color: #f59e0b;
            --bit-Text-decoration-thickness: 2px;
            --bit-Text-underline-offset: 0.3em;
            --bit-Text-monospace-font-family: 'Courier New', monospace;"">
    <BitText Typography=""BitTypography.H5"">A heading in a family of its own</BitText>
    <BitText>Color from --bit-Text-color</BitText>
    <BitText Underline>Underline restyled through the decoration variables</BitText>
    <BitText Monospace>Monospace in another family</BitText>
    <BitText Color=""BitColor.Error"">An explicit Color still wins</BitText>
</div>";

    private readonly string example15RazorCode = @"
<BitText Dir=""BitDir.Rtl"" Typography=""BitTypography.H5"">این یک عنوان راست‌چین است</BitText>
<BitText Dir=""BitDir.Rtl"" Align=""BitTextAlign.Start"">این متن از لبه‌ی آغازین چیده شده است.</BitText>
<BitText Dir=""BitDir.Rtl"" Align=""BitTextAlign.End"">این متن از لبه‌ی پایانی چیده شده است.</BitText>";
}
