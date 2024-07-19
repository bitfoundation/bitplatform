namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public partial class BitToggleButtonDemo
{
    private readonly string example1RazorCode = @"
<BitToggleButton OffText=""Unmuted"" OffIconName=""@BitIconName.Microphone""
                 OnText=""Muted"" OnIconName=""@BitIconName.MicOff"" />";

    private readonly string example2RazorCode = @"
<BitToggleButton Variant=""BitVariant.Fill"">Fill</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"">Outline</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"">Text</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" IsEnabled=""false"">Fill</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" IsEnabled=""false"">Outline</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" IsEnabled=""false"">Text</BitToggleButton>";

    private readonly string example3RazorCode = @"
<BitToggleButton Size=""BitSize.Small"" Variant=""BitVariant.Fill"">Small</BitToggleButton>
<BitToggleButton Size=""BitSize.Medium"" Variant=""BitVariant.Fill"">Medium</BitToggleButton>
<BitToggleButton Size=""BitSize.Large"" Variant=""BitVariant.Fill"">Large</BitToggleButton>

<BitToggleButton Size=""BitSize.Small"" Variant=""BitVariant.Outline"">Small</BitToggleButton>
<BitToggleButton Size=""BitSize.Medium"" Variant=""BitVariant.Outline"">Medium</BitToggleButton>
<BitToggleButton Size=""BitSize.Large"" Variant=""BitVariant.Outline"">Large</BitToggleButton>

<BitToggleButton Size=""BitSize.Small"" Variant=""BitVariant.Text"">Small</BitToggleButton>
<BitToggleButton Size=""BitSize.Medium"" Variant=""BitVariant.Text"">Medium</BitToggleButton>
<BitToggleButton Size=""BitSize.Large"" Variant=""BitVariant.Text"">Large</BitToggleButton>";

    private readonly string example4RazorCode = @"
<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.Primary"">Primary</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.Primary"">Primary</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.Primary"">Primary</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.Secondary"">Secondary</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"">Secondary</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.Secondary"">Secondary</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.Tertiary"">Tertiary</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"">Tertiary</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"">Tertiary</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.Info"">Info</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.Info"">Info</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.Info"">Info</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.Success"">Success</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.Success"">Success</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.Success"">Success</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.Warning"">Warning</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.Warning"">Warning</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.Warning"">Warning</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.SevereWarning"">SevereWarning</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.SevereWarning"">SevereWarning</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.SevereWarning"">SevereWarning</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.Error"">Error</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.Error"">Error</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.Error"">Error</BitToggleButton>";

    private readonly string example5RazorCode = @"
<style>
    .custom-class {
        color: aqua;
        border-radius: 1rem;
    }

    .custom-container {
        font-size: 12px;
    }

    .custom-icon {
        color: blue;
    }

    .custom-text {
        color: red;
    }
</style>


<BitToggleButton Style=""color:darkblue; font-weight:bold""
                 OffText=""Styled Button: Unmuted"" OffIconName=""@BitIconName.Microphone""
                 OnText=""Styled Button: Muted"" OnIconName=""@BitIconName.MicOff"" />

<BitToggleButton Class=""custom-class""
                 Variant=""BitVariant.Outline""
                 OffText=""Classed Button: Unmuted"" OffIconName=""@BitIconName.Microphone""
                 OnText=""Classed Button: Muted"" OnIconName=""@BitIconName.MicOff"" />


<BitToggleButton OffText=""Styled Button: Unmuted"" OffIconName=""@BitIconName.Microphone""
                 OnText=""Styled Button: Muted"" OnIconName=""@BitIconName.MicOff""
                 Styles=""@(new() { Container = ""font-size: 18px;"", Icon = ""color: red;"", Text = ""color: blue;"" })"" />

<BitToggleButton Variant=""BitVariant.Outline""
                 OffText=""Classed Button: Unmuted"" OffIconName=""@BitIconName.Microphone""
                 OnText=""Classed Button: Muted"" OnIconName=""@BitIconName.MicOff""
                 Classes=""@(new() { Container = ""custom-container"", Icon = ""custom-icon"", Text = ""custom-text"" })"" />";

    private readonly string example6RazorCode = @"
<BitToggleButton DefaultIsChecked=""true""
                 OffText=""Unmuted"" OnText=""Muted""
                 OffIconName=""@BitIconName.Microphone"" OnIconName=""@BitIconName.MicOff"" />


<BitToggleButton @bind-IsChecked=""example51Value""
                 Text=""@(example51Value ? ""Muted"" : ""Unmuted"")""
                 IconName=""@(example51Value ? BitIconName.MicOff : BitIconName.Microphone)"" />
<BitCheckbox Label=""Checked Toggle Button"" @bind-Value=""example51Value"" />


<BitToggleButton OnChange=""v => example52Value = v""
                 OffText=""Unmuted"" OnText=""Muted""
                 OffIconName=""@BitIconName.Microphone"" OnIconName=""@BitIconName.MicOff"" />
<BitLabel>Check status is: @example52Value</BitLabel>";
    private readonly string example6CsharpCode = @"
private bool example51Value;
private bool example52Value;";

    private readonly string example7RazorCode = @"
<BitToggleButton Dir=""BitDir.Rtl"" Variant=""BitVariant.Fill""
                 OffText=""صدا وصل"" OffIconName=""@BitIconName.Microphone""
                 OnText=""صدا قطع"" OnIconName=""@BitIconName.MicOff"" />

<BitToggleButton Dir=""BitDir.Rtl"" Variant=""BitVariant.Outline""
                 OffText=""صدا وصل"" OffIconName=""@BitIconName.Microphone""
                 OnText=""صدا قطع"" OnIconName=""@BitIconName.MicOff"" />

<BitToggleButton Dir=""BitDir.Rtl"" Variant=""BitVariant.Text""
                 OffText=""صدا وصل"" OffIconName=""@BitIconName.Microphone""
                 OnText=""صدا قطع"" OnIconName=""@BitIconName.MicOff"" />";
}
