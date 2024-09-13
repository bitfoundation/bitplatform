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
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.Error"">Error</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBackground"">PrimaryBackground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBackground"">PrimaryBackground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"">PrimaryBackground</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBackground"">SecondaryBackground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBackground"">SecondaryBackground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBackground"">SecondaryBackground</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBackground"">TertiaryBackground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"">TertiaryBackground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground"">TertiaryBackground</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryForeground"">PrimaryForeground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryForeground"">PrimaryForeground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.PrimaryForeground"">PrimaryForeground</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryForeground"">SecondaryForeground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryForeground"">SecondaryForeground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.SecondaryForeground"">SecondaryForeground</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryForeground"">TertiaryForeground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryForeground"">TertiaryForeground</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.TertiaryForeground"">TertiaryForeground</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBorder"">PrimaryBorder</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBorder"">PrimaryBorder</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBorder"">PrimaryBorder</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBorder"">SecondaryBorder</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBorder"">SecondaryBorder</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBorder"">SecondaryBorder</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBorder"">TertiaryBorder</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBorder"">TertiaryBorder</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBorder"">TertiaryBorder</BitToggleButton>";

    private readonly string example5RazorCode = @"
<style>
    .custom-class {
        border-radius: 1rem;
        border-color: blueviolet;
        transition: background-color 1s;
        background: linear-gradient(90deg, magenta, transparent) blue;
    }

    .custom-class:hover {
        border-color: magenta;
        background-color: magenta;
    }


    .custom-root {
        border: none;
        color: blueviolet;
        background: transparent;
    }

    .custom-text {
        position: relative;
    }

    .custom-root:hover .custom-text {
        color: darkviolet;
    }

    .custom-text::after {
        content: '';
        left: 0;
        width: 0;
        height: 2px;
        bottom: -6px;
        position: absolute;
        transition: 0.3s ease;
        background: linear-gradient(90deg, #ff00cc, #3333ff);
    }

    .custom-icon {
        color: hotpink;
    }

    .custom-checked {
        border: none;
        background-color: transparent;
    }

    .custom-checked .custom-text::after {
        width: 100%;
    }

    .custom-checked .custom-icon {
        color: hotpink;
    }
</style>


<BitToggleButton Style=""background-color: transparent; border-color: blueviolet; color: blueviolet;""
                 Variant=""BitVariant.Outline""
                 OffText=""Styled Button: Unmuted"" OffIconName=""@BitIconName.Microphone""
                 OnText=""Styled Button: Muted"" OnIconName=""@BitIconName.MicOff"" />

<BitToggleButton Class=""custom-class""
                 OffText=""Classed Button: Unmuted"" OffIconName=""@BitIconName.Microphone""
                 OnText=""Classed Button: Muted"" OnIconName=""@BitIconName.MicOff"" />


<BitToggleButton OffText=""Styled Button: Unmuted"" OffIconName=""@BitIconName.Microphone""
                 OnText=""Styled Button: Muted"" OnIconName=""@BitIconName.MicOff""
                 Styles=""@(new() { Root = ""--toggle-background: pink; background: var(--toggle-background); border: none;"",
                                   Checked = ""--toggle-background: peachpuff;"",
                                   Icon = ""color: red;"",
                                   Text = ""color: tomato;"" })"" />

<BitToggleButton Variant=""BitVariant.Text""
                 OffText=""Classed Button: Unmuted"" OffIconName=""@BitIconName.Microphone""
                 OnText=""Classed Button: Muted"" OnIconName=""@BitIconName.MicOff""
                 Classes=""@(new() { Root = ""custom-root"",
                                    Checked = ""custom-checked"",
                                    Container = ""custom-content"",
                                    Icon = ""custom-icon"",
                                    Text = ""custom-text"" })"" />";

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
<style>
    .custom-content {
        gap: 0.5rem;
        display: flex;
        align-items: center;
    }
</style>


<BitToggleButton Class=""custom-content"">
    <BitIcon IconName=""@BitIconName.Airplane"" />
    <span>Custom template</span>
    <BitRollerLoading CustomSize=""20"" Color=""BitColor.Tertiary"" />
</BitToggleButton>";

    private readonly string example8RazorCode = @"
<BitToggleButton OnClick=""() => clickCounter++"">Click me (@clickCounter)</BitToggleButton>";
    private readonly string example8CsharpCode = @"
private int clickCounter;";

    private readonly string example9RazorCode = @"
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
