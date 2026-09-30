namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Label;

public partial class BitLabelDemo
{
    private readonly string example1RazorCode = @"
<BitLabel>I'm a Label</BitLabel>
<BitLabel IsEnabled=""false"">I'm a disabled Label</BitLabel>";

    private readonly string example2RazorCode = @"
<BitLabel For=""label-input"">A Label for an input</BitLabel>
<input type=""text"" id=""label-input"" />

<BitLabel><input type=""checkbox"" /> A Label wrapping its own control</BitLabel>";

    private readonly string example3RazorCode = @"
<BitLabel For=""required-input"" Required>Email</BitLabel>
<input type=""email"" id=""required-input"" required />

<BitLabel Required RequiredText=""(required)"">A word instead of the asterisk</BitLabel>

<BitLabel Required>
    <ChildContent>A custom required template</ChildContent>
    <RequiredTemplate>
        <BitIcon IconName=""@BitIconName.Important"" Color=""BitColor.Error"" Size=""BitSize.Small"" />
    </RequiredTemplate>
</BitLabel>

<BitLabel Required IsEnabled=""false"">A disabled Label dims its mark too</BitLabel>

<BitLabel Optional>I'm an optional Label</BitLabel>

<BitLabel Optional OptionalText=""- if you have one"">Its own optional text</BitLabel>

<BitLabel Optional>
    <ChildContent>A custom optional template</ChildContent>
    <OptionalTemplate>
        <BitTag Text=""optional"" Size=""BitSize.Small"" Color=""BitColor.Tertiary"" />
    </OptionalTemplate>
</BitLabel>";

    private readonly string example4RazorCode = @"
<BitLabel Element=""div"" Id=""favorite-color-label"" Required>Favorite color</BitLabel>
<div role=""radiogroup"" aria-labelledby=""favorite-color-label"" aria-required=""true"">
    <label><input type=""radio"" name=""favorite-color"" /> Red</label>
    <label><input type=""radio"" name=""favorite-color"" /> Green</label>
    <label><input type=""radio"" name=""favorite-color"" /> Blue</label>
</div>

<fieldset>
    <BitLabel Element=""legend"" Optional>Delivery notes</BitLabel>
    <label><input type=""checkbox"" /> Leave with a neighbour</label>
    <label><input type=""checkbox"" /> Ring the doorbell</label>
</fieldset>";

    private readonly string example5RazorCode = @"
<BitLabel Style=""width:220px"">A caption long enough to need more than one line at this width</BitLabel>
<BitLabel Style=""width:220px"" NoWrap Required title=""A caption long enough to need more than one line at this width"">
    A caption long enough to need more than one line at this width
</BitLabel>

<BitLabel><input type=""checkbox"" /> Selectable caption</BitLabel>
<BitLabel NoSelect><input type=""checkbox"" /> Unselectable caption</BitLabel>";

    private readonly string example6RazorCode = @"
<BitLabel VisuallyHidden For=""search-input"">Search the documentation</BitLabel>
<input type=""search"" id=""search-input"" placeholder=""Search..."" />

<BitLabel VisuallyHidden><input type=""checkbox"" /> Shown only while its checkbox has the focus</BitLabel>

<div style=""display:flex;align-items:center;gap:0.25rem"">Visible: [ <BitLabel Visibility=""BitVisibility.Visible"">Visible Label</BitLabel> ]</div>
<div style=""display:flex;align-items:center;gap:0.25rem"">Hidden: [ <BitLabel Visibility=""BitVisibility.Hidden"">Hidden Label</BitLabel> ]</div>
<div style=""display:flex;align-items:center;gap:0.25rem"">Collapsed: [ <BitLabel Visibility=""BitVisibility.Collapsed"">Collapsed Label</BitLabel> ]</div>";

    private readonly string example7RazorCode = @"
<div style=""--bit-Label-font-weight: 400; --bit-Label-required-color: var(--bit-clr-pri); --bit-Label-indicator-gap: 2px;"">
    <BitLabel For=""css-name"" Required>Name (set on the container)</BitLabel>
    <input type=""text"" id=""css-name"" required />
    <BitLabel For=""css-nickname"" Optional>Nickname (set on the container)</BitLabel>
    <input type=""text"" id=""css-nickname"" />
</div>

<BitLabel Style=""--bit-Label-font-size: 1.25rem; --bit-Label-color: var(--bit-clr-sec-fg); --bit-Label-padding: 0 0 8px;"">
    Set on one label
</BitLabel>

<div style=""--bit-tpg-field-label-font-weight: 400;"">
    <BitLabel For=""css-city"">City (a BitLabel)</BitLabel>
    <input type=""text"" id=""css-city"" />
    <BitTextField Label=""Country (a BitTextField's own label)"" />
</div>";

    private readonly string example8RazorCode = @"
<BitParams Parameters=""labelParams"">
    <BitLabel>First name</BitLabel>
    <BitLabel>Last name</BitLabel>
    <BitLabel Required=""false"" Optional>Middle name</BitLabel>
</BitParams>";
    private readonly string example8CsharpCode = @"
private readonly BitLabelParams[] labelParams =
[
    new()
    {
        Required = true,
        RequiredText = ""(required)"",
    }
];";

    private readonly string example9RazorCode = @"
<BitLabel Color=""BitColor.Primary"">Primary</BitLabel>
<BitLabel Color=""BitColor.Secondary"">Secondary</BitLabel>
<BitLabel Color=""BitColor.Tertiary"">Tertiary</BitLabel>
<BitLabel Color=""BitColor.Info"">Info</BitLabel>
<BitLabel Color=""BitColor.Success"">Success</BitLabel>
<BitLabel Color=""BitColor.Warning"">Warning</BitLabel>
<BitLabel Color=""BitColor.SevereWarning"">SevereWarning</BitLabel>
<BitLabel Color=""BitColor.Error"" Required>Error</BitLabel>
<BitLabel Color=""BitColor.PrimaryForeground"">PrimaryForeground</BitLabel>
<BitLabel Color=""BitColor.SecondaryForeground"">SecondaryForeground</BitLabel>
<BitLabel Color=""BitColor.TertiaryForeground"">TertiaryForeground</BitLabel>
<BitLabel Color=""BitColor.PrimaryBorder"">PrimaryBorder</BitLabel>
<BitLabel Color=""BitColor.SecondaryBorder"">SecondaryBorder</BitLabel>
<BitLabel Color=""BitColor.TertiaryBorder"">TertiaryBorder</BitLabel>";

    private readonly string example10RazorCode = @"
<BitLabel Size=""BitSize.Small"" Required>Small</BitLabel>
<BitLabel Size=""BitSize.Medium"" Required>Medium</BitLabel>
<BitLabel Size=""BitSize.Large"" Required>Large</BitLabel>";

    private readonly string example11RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem;
        border: 1px solid red;
        max-width: max-content;
    }

    .custom-root {
        text-transform: uppercase;
        letter-spacing: 0.05rem;
    }

    .custom-optional {
        color: mediumseagreen;
        font-style: italic;
    }
</style>

<BitLabel Style=""color: dodgerblue; font-weight: bold"">I'm a Label with Style</BitLabel>
<BitLabel Class=""custom-class"">I'm a Label with Class</BitLabel>

<BitLabel Required Styles=""@(new() { Root = ""font-style: italic"", RequiredIndicator = ""color: blueviolet; font-size: 1rem"" })"">
    I'm a Label with Styles
</BitLabel>

<BitLabel Optional Classes=""@(new() { Root = ""custom-root"", OptionalIndicator = ""custom-optional"" })"">
    I'm a Label with Classes
</BitLabel>";

    private readonly string example12RazorCode = @"
<BitLabel Dir=""BitDir.Rtl"">من یک برچسب هستم</BitLabel>
<BitLabel Dir=""BitDir.Rtl"" Required>من یک برچسب الزامی هستم</BitLabel>
<BitLabel Dir=""BitDir.Rtl"" Optional OptionalText=""(اختیاری)"">من یک برچسب اختیاری هستم</BitLabel>";
}
