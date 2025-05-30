namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.NumberField;

public partial class BitNumberFieldDemo
{
    private readonly string example1RazorCode = @"
<BitNumberField Label=""Basic"" TValue=""int?"" />

<BitNumberField Label=""Disabled & DefaultValue"" DefaultValue=""1363"" IsEnabled=""false"" />

<BitNumberField Label=""Placeholder"" TValue=""int?"" Placeholder=""Enter a number..."" />

<BitNumberField Label=""Required"" TValue=""int?"" Required />";

    private readonly string example2RazorCode = @"
<BitNumberField Label=""Top (default)"" TValue=""int"" />

<BitNumberField Label=""Start"" LabelPosition=""BitLabelPosition.Start"" TValue=""int"" />

<BitNumberField Label=""End"" LabelPosition=""BitLabelPosition.End"" TValue=""int"" />

<BitNumberField Label=""Bottom"" LabelPosition=""BitLabelPosition.Bottom"" TValue=""int"" />

<BitNumberField TValue=""int"">
    <LabelTemplate>
        <div style=""display:flex;align-items:center;gap:10px"">
            <BitLabel Style=""color:green;"">This is custom Label</BitLabel>
            <BitIcon IconName=""@BitIconName.Filter"" Style=""font-size:18px;"" />
        </div>
    </LabelTemplate>
</BitNumberField>";

    private readonly string example3RazorCode = @"
<BitNumberField Label=""Label & Icon"" TValue=""int""
                IconName=""@BitIconName.Lightbulb"" />

<BitNumberField Label=""Compact mode"" TValue=""int""
                Mode=""BitSpinButtonMode.Compact""
                IncrementIconName=""@BitIconName.LikeSolid""
                DecrementIconName=""@BitIconName.DislikeSolid"" />

<BitNumberField Label=""Inline mode"" TValue=""int""
                Mode=""BitSpinButtonMode.Inline""
                IncrementIconName=""@BitIconName.Forward""
                DecrementIconName=""@BitIconName.Back"" />

<BitNumberField Label=""Spread mode"" TValue=""int""
                Mode=""BitSpinButtonMode.Spread""
                IncrementIconName=""@BitIconName.CalculatorAddition""
                DecrementIconName=""@BitIconName.CalculatorSubtract"" />";

    private readonly string example4RazorCode = @"
<BitNumberField Label=""Age"" TValue=""int?"" ShowClearButton DefaultValue=""28"" />";

    private readonly string example5RazorCode = @"
<BitNumberField Label=""N0"" DefaultValue=""1234567890d"" NumberFormat=""N0"" />

<BitNumberField Label=""C0"" DefaultValue=""150"" NumberFormat=""C0"" />

<BitNumberField Label=""000000"" DefaultValue=""1363"" NumberFormat=""000000"" />";

    private readonly string example6RazorCode = @"
<BitNumberField TValue=""int"" Label=""Prefix"" Prefix=""Distance:"" />

<BitNumberField TValue=""int"" Label=""Suffix"" Suffix=""km"" />

<BitNumberField TValue=""int"" Label=""Prefix & Suffix"" Prefix=""Distance:"" Suffix=""km"" />

<BitNumberField TValue=""int"" Label=""With buttons"" Prefix=""Distance:"" Suffix=""km"" ShowButtons=""true"" />

<BitNumberField TValue=""int"" Label=""Disabled"" Prefix=""Distance:"" Suffix=""km"" IsEnabled=""false"" />";

    private readonly string example7RazorCode = @"
<BitNumberField Label=""One-way"" Value=""oneWayValue"" />
<BitRating @bind-Value=""oneWayValue"" />

<BitNumberField Label=""Two-way"" @bind-Value=""twoWayValue"" />
<BitRating @bind-Value=""twoWayValue"" />";
    private readonly string example7CsharpCode = @"
private double oneWayValue;
private double twoWayValue;
";

    private readonly string example8RazorCode = @"
<BitNumberField Label=""Min = 0"" Min=""0"" @bind-Value=""minValue"" />
<div>value: [@minValue]</div>

<BitNumberField Label=""Max = 100"" Max=""100"" @bind-Value=""maxValue"" />
<div>value: [@maxValue]</div>

<BitNumberField Label=""Min & Max (-10, 10)"" Min=""-10"" Max=""10"" @bind-Value=""minMaxValue"" />
<div>value: [@minMaxValue]</div>";
    private readonly string example8CsharpCode = @"
private int minValue;
private int maxValue;
private int minMaxValue;";

    private readonly string example9RazorCode = @"
<BitNumberField Precision=""2"" @bind-Value=""precisionInputValue"" Label=""Rounding to 2 Decimal Places"" />";
    private readonly string example9CsharpCode = @"
    private double precisionInputValue = 3.1415;";

    private readonly string example10RazorCode = @"
<BitNumberField HideInput
                @bind-Value=""hideInputValue""
                Mode=""BitSpinButtonMode.Inline""
                Label=""@hideInputValue.ToString()"" />";
    private readonly string example10CsharpCode = @"
private int hideInputValue;";

    private readonly string example11RazorCode = @"
<BitToggle @bind-Value=""invertMouseWheel"" Text=""Invert Mouse Wheel"" />
<BitNumberField InvertMouseWheel=""invertMouseWheel"" Label=""Use Shift + Mouse Wheel"" TValue=""int"" />";
    private readonly string example11CsharpCode = @"
private bool invertMouseWheel;";

    private readonly string example12RazorCode = @"
<BitNumberField Label=""OnIncrement & OnDecrement"" ShowButtons=""true""
                OnIncrement=""(double v) => onIncrementCounter++""
                OnDecrement=""(double v) => onDecrementCounter++"" />
<div>OnIncrement Counter: @onIncrementCounter</div>
<div>OnDecrement Counter: @onDecrementCounter</div>

<BitNumberField Label=""OnChange"" OnChange=""(double v) => onChangeCounter++"" />
<div>OnChange Counter: @onChangeCounter</div>";
    private readonly string example12CsharpCode = @"
private int onIncrementCounter;
private int onDecrementCounter;
private int onChangeCounter;";

    private readonly string example13RazorCode = @"
<EditForm Model=""@validationModel"">
    <DataAnnotationsValidator />

    <BitNumberField Label=""@($""Age: [{validationModel.Age}]"")"" @bind-Value=""validationModel.Age"" />
    <ValidationMessage For=""@(() => validationModel.Age)"" />
    <br />
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example13CsharpCode = @"
public class BitNumberFieldValidationModel
{
    [Required(ErrorMessage = ""Enter an age"")]
    [Range(1, 150, ErrorMessage = ""Nobody is that old"")]
    public int? Age { get; set; }
}

private BitNumberFieldValidationModel validationModel = new();";

    private readonly string example14RazorCode = @"
<style>
    .custom-class {
        overflow: hidden;
        margin-inline: 1rem;
        border-radius: 1rem;
        border: 2px solid brown;
    }

    .custom-class *, .custom-class *::after {
        border: none;
    }


    .custom-root {
        height: 3rem;
        display: flex;
        align-items: end;
        position: relative;
        margin-inline: 1rem;
    }

    .custom-label {
        top: 0;
        left: 0;
        z-index: 1;
        padding: 0;
        font-size: 1rem;
        color: darkgray;
        position: absolute;
        transform-origin: top left;
        transform: translate(0, 22px) scale(1);
        transition: color 200ms cubic-bezier(0, 0, 0.2, 1) 0ms, transform 200ms cubic-bezier(0, 0, 0.2, 1) 0ms;
    }

    .custom-label-top {
        transform: translate(0, 1.5px) scale(0.75);
    }

    .custom-input {
        padding: 0;
        font-size: 1rem;
        font-weight: 900;
    }

    .custom-input-wrapper {
        border-radius: 0;
        position: relative;
        border-width: 0 0 1px 0;
    }

    .custom-input-wrapper::after {
        content: '';
        width: 0;
        height: 2px;
        border: none;
        position: absolute;
        inset: 100% 0 0 50%;
        background-color: blueviolet;
        transition: width 0.3s ease, left 0.3s ease;
    }

    .custom-focus .custom-input-wrapper::after {
        left: 0;
        width: 100%;
    }

    .custom-focus .custom-label {
        color: blueviolet;
        transform: translate(0, 1.5px) scale(0.75);
    }
</style>


<BitNumberField DefaultValue=""10"" Style=""box-shadow: aqua 0 0 1rem; margin-inline: 1rem;"" />

<BitNumberField DefaultValue=""20"" Class=""custom-class"" />


<BitNumberField DefaultValue=""1""
                Label=""Styles""
                Styles=""@(new() { Root = ""margin-inline: 1rem;"",
                                  Focused = ""--focused-background: #b2b2b25a;"",
                                  InputWrapper = ""background: var(--focused-background);"",
                                  Label = ""text-shadow: aqua 0 0 1rem; font-weight: 900; font-size: 1.25rem;"",
                                  Input = ""padding: 0.5rem;"" })"" />

<BitNumberField TValue=""int?""
                Label=""Classes""
                @bind-Value=""classesValue""
                Classes=""@(new() { Root = ""custom-root"",
                                 InputWrapper = ""custom-input-wrapper"",
                                 Focused = ""custom-focus"",
                                 Input = ""custom-input"",
                                 Label = $""custom-label{(classesValue is null ? string.Empty : "" custom-label-top"")}"" })"" />";

    private readonly string example15RazorCode = @"
<CascadingValue Value=""BitDir.Rtl"">

    <BitNumberField Label=""برچسب در بالا"" TValue=""int"" ShowButtons />

    <BitNumberField Label=""برچسب درخط"" TValue=""int"" InlineLabel />

    <BitNumberField TValue=""int"" Required />

    <BitNumberField Label=""الزامی"" TValue=""int"" Required />

</CascadingValue>";
}
