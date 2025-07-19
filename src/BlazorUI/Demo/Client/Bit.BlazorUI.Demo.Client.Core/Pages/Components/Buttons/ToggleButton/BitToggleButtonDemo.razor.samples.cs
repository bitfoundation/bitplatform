namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ToggleButton;

public partial class BitToggleButtonDemo
{
    private readonly string example1RazorCode = @"
<BitToggleButton>Microphone</BitToggleButton>";

    private readonly string example2RazorCode = @"
<BitToggleButton Variant=""BitVariant.Fill"">Fill</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"">Outline</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"">Text</BitToggleButton>

<BitToggleButton Variant=""BitVariant.Fill"" IsEnabled=""false"">Fill</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Outline"" IsEnabled=""false"">Outline</BitToggleButton>
<BitToggleButton Variant=""BitVariant.Text"" IsEnabled=""false"">Text</BitToggleButton>";

    private readonly string example3RazorCode = @"
<BitToggleButton Text=""Microphone"" />

<BitToggleButton OnText=""Muted"" OffText=""Unmuted"" />";

    private readonly string example4RazorCode = @"
<BitToggleButton Text=""Microphone"" IconName=""@BitIconName.Microphone"" />

<BitToggleButton OnText=""Muted"" OnIconName=""@BitIconName.MicOff""
                 OffText=""Unmuted"" OffIconName=""@BitIconName.Microphone"" />

<BitToggleButton IconName=""@BitIconName.Microphone"" />
<BitToggleButton OnIconName=""@BitIconName.MicOff"" OffIconName=""@BitIconName.Microphone"" />

<BitToggleButton IconOnly IconName=""@BitIconName.Microphone"" Text=""Microphone"" />
<BitToggleButton IconOnly
                 OnText=""Muted"" OnIconName=""@BitIconName.MicOff"" 
                 OffText=""Unmuted"" OffIconName=""@BitIconName.Microphone"" />";

    private readonly string example5RazorCode = @"
<BitToggleButton DefaultIsChecked=""true""
                 OnText=""Muted"" OnIconName=""@BitIconName.MicOff""
                 OffText=""Unmuted"" OffIconName=""@BitIconName.Microphone"" />

<BitToggleButton @bind-IsChecked=""twoWayBoundValue""
                 Text=""@(twoWayBoundValue ? ""Muted"" : ""Unmuted"")""
                 IconName=""@(twoWayBoundValue ? BitIconName.MicOff : BitIconName.Microphone)"" />
<BitCheckbox Label=""Checked Toggle Button"" @bind-Value=""twoWayBoundValue"" />

<BitToggleButton OnChange=""v => onChangeValue = v""
                 OnText=""Muted"" OnIconName=""@BitIconName.MicOff""
                 OffText=""Unmuted"" OffIconName=""@BitIconName.Microphone"" />
<div>Check status: @onChangeValue</div>";
    private readonly string example5CsharpCode = @"
private bool twoWayBoundValue;
private bool onChangeValue;";

    private readonly string example6RazorCode = @"
<style>
    .custom-template {
        gap: 0.5rem;
        display: flex;
        align-items: center;
    }
</style>


<BitToggleButton>
    <div class=""custom-template"">
        <BitIcon IconName=""@BitIconName.Airplane"" Color=""BitColor.Tertiary"" />
        <span>Custom template</span>
        <BitRollerLoading CustomSize=""20"" Color=""BitColor.Tertiary"" />
    </div>
</BitToggleButton>";

    private readonly string example7RazorCode = @"
<BitToggleButton OnClick=""() => clickCounter++"">
    Click me (@clickCounter)
</BitToggleButton>";
    private readonly string example7CsharpCode = @"
private int clickCounter;";

    private readonly string example8RazorCode = @"
<BitToggleButton Size=""BitSize.Small"" Variant=""BitVariant.Fill"" IconName=""@BitIconName.Microphone"" />
<BitToggleButton Size=""BitSize.Small"" Variant=""BitVariant.Fill"" Text=""Microphone"" />
<BitToggleButton Size=""BitSize.Small"" Variant=""BitVariant.Fill"" IconName=""@BitIconName.Microphone"" Text=""Microphone"" />

<BitToggleButton Size=""BitSize.Small"" Variant=""BitVariant.Outline"" IconName=""@BitIconName.Microphone"" />
<BitToggleButton Size=""BitSize.Small"" Variant=""BitVariant.Outline"" Text=""Microphone"" />
<BitToggleButton Size=""BitSize.Small"" Variant=""BitVariant.Outline"" IconName=""@BitIconName.Microphone"" Text=""Microphone"" />

<BitToggleButton Size=""BitSize.Small"" Variant=""BitVariant.Text"" IconName=""@BitIconName.Microphone"" />
<BitToggleButton Size=""BitSize.Small"" Variant=""BitVariant.Text"" Text=""Microphone"" />
<BitToggleButton Size=""BitSize.Small"" Variant=""BitVariant.Text"" IconName=""@BitIconName.Microphone"" Text=""Microphone"" />


<BitToggleButton Size=""BitSize.Medium"" Variant=""BitVariant.Fill"" IconName=""@BitIconName.Microphone"" />
<BitToggleButton Size=""BitSize.Medium"" Variant=""BitVariant.Fill"" Text=""Microphone"" />
<BitToggleButton Size=""BitSize.Medium"" Variant=""BitVariant.Fill"" IconName=""@BitIconName.Microphone"" Text=""Microphone"" />

<BitToggleButton Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" IconName=""@BitIconName.Microphone"" />
<BitToggleButton Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" Text=""Microphone"" />
<BitToggleButton Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" IconName=""@BitIconName.Microphone"" Text=""Microphone"" />

<BitToggleButton Size=""BitSize.Medium"" Variant=""BitVariant.Text"" IconName=""@BitIconName.Microphone"" />
<BitToggleButton Size=""BitSize.Medium"" Variant=""BitVariant.Text"" Text=""Microphone"" />
<BitToggleButton Size=""BitSize.Medium"" Variant=""BitVariant.Text"" IconName=""@BitIconName.Microphone"" Text=""Microphone"" />


<BitToggleButton Size=""BitSize.Large"" Variant=""BitVariant.Fill"" IconName=""@BitIconName.Microphone"" />
<BitToggleButton Size=""BitSize.Large"" Variant=""BitVariant.Fill"" Text=""Microphone"" />
<BitToggleButton Size=""BitSize.Large"" Variant=""BitVariant.Fill"" IconName=""@BitIconName.Microphone"" Text=""Microphone"" />

<BitToggleButton Size=""BitSize.Large"" Variant=""BitVariant.Outline"" IconName=""@BitIconName.Microphone"" />
<BitToggleButton Size=""BitSize.Large"" Variant=""BitVariant.Outline"" Text=""Microphone"" />
<BitToggleButton Size=""BitSize.Large"" Variant=""BitVariant.Outline"" IconName=""@BitIconName.Microphone"" Text=""Microphone"" />

<BitToggleButton Size=""BitSize.Large"" Variant=""BitVariant.Text"" IconName=""@BitIconName.Microphone"" />
<BitToggleButton Size=""BitSize.Large"" Variant=""BitVariant.Text"" Text=""Microphone"" />
<BitToggleButton Size=""BitSize.Large"" Variant=""BitVariant.Text"" IconName=""@BitIconName.Microphone"" Text=""Microphone"" />";

    private readonly string example9RazorCode = @"
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

    private readonly string example10RazorCode = @"
<BitToggleButton Color=""BitColor.TertiaryBackground"" FixedColor 
                 OnIconName=""@BitIconName.MicOff"" OffIconName=""@BitIconName.Microphone"" />

<BitToggleButton Color=""BitColor.TertiaryBackground"" FixedColor 
                 OnText=""Muted"" OnIconName=""@BitIconName.MicOff""
                 OffText=""Unmuted"" OffIconName=""@BitIconName.Microphone"" />";

    private readonly string example11RazorCode = @"
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
                 OnText=""Styled Button: Muted"" OnIconName=""@BitIconName.MicOff""
                 OffText=""Styled Button: Unmuted"" OffIconName=""@BitIconName.Microphone"" />

<BitToggleButton Class=""custom-class""
                 OnText=""Classed Button: Muted"" OnIconName=""@BitIconName.MicOff"" 
                 OffText=""Classed Button: Unmuted"" OffIconName=""@BitIconName.Microphone"" />


<BitToggleButton OnText=""Styled Button: Muted"" OnIconName=""@BitIconName.MicOff""
                 OffText=""Styled Button: Unmuted"" OffIconName=""@BitIconName.Microphone""
                 Styles=""@(new() { Root = ""--toggle-background: pink; background: var(--toggle-background); border: none;"",
                                   Checked = ""--toggle-background: peachpuff;"",
                                   Icon = ""color: red;"",
                                   Text = ""color: tomato;"" })"" />

<BitToggleButton Variant=""BitVariant.Text""
                 OnText=""Classed Button: Muted"" OnIconName=""@BitIconName.MicOff""
                 OffText=""Classed Button: Unmuted"" OffIconName=""@BitIconName.Microphone""
                 Classes=""@(new() { Root = ""custom-root"",
                                    Checked = ""custom-checked"",
                                    Container = ""custom-content"",
                                    Icon = ""custom-icon"",
                                    Text = ""custom-text"" })"" />";

    private readonly string example12RazorCode = @"
<BitToggleButton Dir=""BitDir.Rtl"" Variant=""BitVariant.Fill""
                 OnText=""صدا قطع"" OnIconName=""@BitIconName.MicOff""
                 OffText=""صدا وصل"" OffIconName=""@BitIconName.Microphone"" />

<BitToggleButton Dir=""BitDir.Rtl"" Variant=""BitVariant.Outline""
                 OnText=""صدا قطع"" OnIconName=""@BitIconName.MicOff""
                 OffText=""صدا وصل"" OffIconName=""@BitIconName.Microphone"" />

<BitToggleButton Dir=""BitDir.Rtl"" Variant=""BitVariant.Text""
                 OnText=""صدا قطع"" OnIconName=""@BitIconName.MicOff""
                 OffText=""صدا وصل"" OffIconName=""@BitIconName.Microphone"" />";
}
