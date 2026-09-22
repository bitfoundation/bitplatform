namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.NumberField;

public partial class BitNumberFieldDemo
{
    private readonly string example1RazorCode = @"
<BitNumberField Label=""Basic"" TValue=""int?"" />

<BitNumberField Label=""Disabled & DefaultValue"" DefaultValue=""1363"" IsEnabled=""false"" />

<BitNumberField Label=""Placeholder"" TValue=""int?"" Placeholder=""Enter a number..."" />

<BitNumberField Label=""Required"" TValue=""int?"" Required />

<BitNumberField Label=""NoSelectOnFocus"" NoSelectOnFocus DefaultValue=""1363"" TValue=""int?"" />";

    private readonly string example2RazorCode = @"
<BitNumberField Label=""One-way"" Value=""oneWayValue"" />
<BitRating @bind-Value=""oneWayValue"" />

<BitNumberField Label=""Two-way"" @bind-Value=""twoWayValue"" />
<BitRating @bind-Value=""twoWayValue"" />

<BitNumberField Label=""Uncontrolled (DefaultValue & OnChange)"" TValue=""int?""
                DefaultValue=""7""
                OnChange=""(int? v) => uncontrolledValue = v"" />
<div>last OnChange value: [@uncontrolledValue]</div>";
    private readonly string example2CsharpCode = @"
private double oneWayValue;
private double twoWayValue;
private int? uncontrolledValue;";

    private readonly string example3RazorCode = @"
<BitNumberField Label=""Compact"" TValue=""int"" Mode=""BitSpinButtonMode.Compact"" />

<BitNumberField Label=""Inline"" TValue=""int"" Mode=""BitSpinButtonMode.Inline"" />

<BitNumberField Label=""Spread"" TValue=""int"" Mode=""BitSpinButtonMode.Spread"" />

<BitNumberField Label=""Custom icons & titles"" TValue=""int""
                Mode=""BitSpinButtonMode.Compact""
                IncrementTitle=""Like it more""
                DecrementTitle=""Like it less""
                IncrementIconName=""@BitIconName.LikeSolid""
                DecrementIconName=""@BitIconName.DislikeSolid"" />

<BitNumberField Label=""Fast continuous spin (hold a button)"" TValue=""int""
                Mode=""BitSpinButtonMode.Spread""
                ContinuousSpinDelay=""200""
                ContinuousSpinInterval=""25""
                IncrementIconName=""@BitIconName.CalculatorAddition""
                DecrementIconName=""@BitIconName.CalculatorSubtract"" />

<BitNumberField HideInput
                @bind-Value=""hideInputValue""
                Mode=""BitSpinButtonMode.Inline""
                Label=""@($""HideInput: {hideInputValue}"")"" />";
    private readonly string example3CsharpCode = @"
private int hideInputValue;";

    private readonly string example4RazorCode = @"
<BitNumberField Label=""Min = 0"" Min=""0"" Mode=""BitSpinButtonMode.Compact"" @bind-Value=""minValue"" />
<div>value: [@minValue]</div>

<BitNumberField Label=""Min & Max (-10, 10)"" Min=""-10"" Max=""10"" Mode=""BitSpinButtonMode.Compact"" @bind-Value=""minMaxValue"" />
<div>value: [@minMaxValue]</div>

<BitNumberField Label=""Clamping (default): typing 500 commits 100"" Min=""0"" Max=""100"" Mode=""BitSpinButtonMode.Compact"" @bind-Value=""clampValue"" />
<div>value: [@clampValue]</div>

<BitNumberField Label=""NoClamp: typing 500 commits 500"" NoClamp Min=""0"" Max=""100"" Mode=""BitSpinButtonMode.Compact"" @bind-Value=""noClampValue"" />
<div>value: [@noClampValue]</div>";
    private readonly string example4CsharpCode = @"
private int minValue;
private int minMaxValue;
private int clampValue;
private int noClampValue;";

    private readonly string example5RazorCode = @"
<BitNumberField Label=""Step = 2"" Step=""2"" Mode=""BitSpinButtonMode.Compact"" @bind-Value=""stepValue"" />
<div>value: [@stepValue]</div>

<BitNumberField Label=""Fractional step (0.1)"" Step=""0.1"" Mode=""BitSpinButtonMode.Compact"" @bind-Value=""fractionalStepValue"" />
<div>value: [@fractionalStepValue]</div>

<BitNumberField Label=""PageStep = 20 (press PageUp/PageDown)"" PageStep=""20"" Mode=""BitSpinButtonMode.Compact"" @bind-Value=""pageStepValue"" />
<div>value: [@pageStepValue]</div>

<BitNumberField Label=""Precision = -2 (rounds to the nearest hundred)"" Precision=""-2"" TValue=""double"" Mode=""BitSpinButtonMode.Compact"" @bind-Value=""negativePrecisionInputValue"" />
<div>value: [@negativePrecisionInputValue]</div>

<BitNumberField Label=""SnapToStep, Min = 2 & Step = 3 (typing 7 commits 8)"" SnapToStep Min=""2"" Step=""3"" Mode=""BitSpinButtonMode.Compact"" @bind-Value=""snapAnchoredValue"" />
<div>value: [@snapAnchoredValue]</div>

<BitNumberField Label=""SnapToStep & fractional step (typing 0.3 commits 0.25)"" SnapToStep Step=""0.25"" Mode=""BitSpinButtonMode.Compact"" @bind-Value=""snapFractionValue"" />
<div>value: [@snapFractionValue]</div>";
    private readonly string example5CsharpCode = @"
private int stepValue;
private double fractionalStepValue;
private int pageStepValue;
private double negativePrecisionInputValue;
private int snapAnchoredValue = 2;
private double snapFractionValue;";

    private readonly string example6RazorCode = @"
<BitNumberField Label=""byte"" Mode=""BitSpinButtonMode.Compact"" @bind-Value=""byteValue"" />
<div>value: [@byteValue]</div>

<BitNumberField Label=""long"" Mode=""BitSpinButtonMode.Compact"" @bind-Value=""longValue"" />
<div>value: [@longValue]</div>

<BitNumberField Label=""decimal"" Mode=""BitSpinButtonMode.Compact"" @bind-Value=""decimalValue"" Step=""0.01"" />
<div>value: [@decimalValue]</div>

<BitNumberField Label=""Negative values on touch (InputMode.Text)"" Mode=""BitSpinButtonMode.Compact""
                InputMode=""BitInputMode.Text"" Min=""-100"" Max=""100"" @bind-Value=""signedValue"" />
<div>value: [@signedValue]</div>";
    private readonly string example6CsharpCode = @"
private byte byteValue = 5;
private long longValue = 1_000_000_000_000;
private decimal decimalValue = 0.05m;
private int signedValue = -5;";

    private readonly string example7RazorCode = @"
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

    private readonly string example8RazorCode = @"
<BitNumberField Label=""Quantity"" TValue=""int"" Min=""1"" Max=""99"" Mode=""BitSpinButtonMode.Compact""
                Description=""Between 1 and 99 items per order."" />

<BitNumberField Label=""Seats"" TValue=""int"" Mode=""BitSpinButtonMode.Compact""
                @bind-Value=""seatsValue""
                Description=""We hold the room for 15 minutes.""
                ErrorMessage=""@(seatsValue > 4 ? ""Only 4 seats are left for this show."" : null)"" />

<BitNumberField Label=""Invalid (no message of its own)"" TValue=""int"" Invalid DefaultValue=""42"" />

<BitToggle @bind-Value=""loadingValue"" Text=""Loading"" />
<BitNumberField Label=""Price"" TValue=""double"" Loading=""loadingValue""
                LoadingAriaLabel=""Recalculating the price"" DefaultValue=""19.99"" />

<BitNumberField Label=""Discount"" TValue=""int"" Min=""0"" Max=""100"" Mode=""BitSpinButtonMode.Inline"">
    <DescriptionTemplate>
        <b style=""color:darkorange"">Anything above 50% needs a manager's approval.</b>
    </DescriptionTemplate>
</BitNumberField>

<BitNumberField Label=""Label on the side"" TValue=""int""
                LabelPosition=""BitLabelPosition.Start""
                Description=""The hint wraps onto a line of its own."" />";
    private readonly string example8CsharpCode = @"
private int seatsValue = 2;
private bool loadingValue;";

    private readonly string example9RazorCode = @"
<BitNumberField Label=""Underlined"" TValue=""int"" Underlined Mode=""BitSpinButtonMode.Compact"" />

<BitNumberField Label=""Underlined & Required"" TValue=""int"" Underlined Required />

<BitNumberField Label=""NoBorder"" TValue=""int"" NoBorder Mode=""BitSpinButtonMode.Compact"" />

<div style=""display:flex"">
    <BitNumberField Label=""FullWidth (inside a flex container)"" TValue=""int"" FullWidth Mode=""BitSpinButtonMode.Compact"" />
</div>";

    private readonly string example10RazorCode = @"
<BitNumberField TValue=""int"" Label=""Icon"" IconName=""@BitIconName.Lightbulb"" />

<BitNumberField TValue=""int"" Label=""Prefix & Suffix"" Prefix=""Distance:"" Suffix=""km"" Mode=""BitSpinButtonMode.Compact"" />

<BitNumberField TValue=""int"" Label=""Disabled"" Prefix=""Distance:"" Suffix=""km"" IsEnabled=""false"" />

<BitNumberField TValue=""double"" Label=""Price"" Mode=""BitSpinButtonMode.Compact"" Step=""0.5"">
    <PrefixTemplate>
        <BitIcon IconName=""@BitIconName.Money"" Style=""padding-inline: 0.5rem;"" />
    </PrefixTemplate>
    <SuffixTemplate>
        <BitTag Color=""BitColor.Info"" Style=""margin-inline-end: 0.25rem;"">USD</BitTag>
    </SuffixTemplate>
</BitNumberField>";

    private readonly string example11RazorCode = @"
<BitNumberField Label=""Age"" TValue=""int?"" ShowClearButton DefaultValue=""28"" />

<BitNumberField Label=""Custom icon"" TValue=""int?"" ShowClearButton ClearButtonIconName=""@BitIconName.Delete"" DefaultValue=""28"" />

<BitNumberField Label=""OnClear & Escape (press Escape to clear)"" TValue=""int?"" ShowClearButton
                DefaultValue=""28"" OnClear=""() => clearedCounter++"" />
<div>cleared: @clearedCounter time(s)</div>";
    private readonly string example11CsharpCode = @"
private int clearedCounter;";

    private readonly string example12RazorCode = @"
<BitNumberField Label=""N0"" DefaultValue=""1234567890d"" NumberFormat=""N0"" />

<BitNumberField Label=""C0"" DefaultValue=""150"" NumberFormat=""C0"" />

<BitNumberField Label=""C2 (try typing a negative amount)"" DefaultValue=""1234.5"" NumberFormat=""C2"" />

<BitNumberField Label=""000000"" DefaultValue=""1363"" NumberFormat=""000000"" />";

    private readonly string example13RazorCode = @"
<BitNumberField Label=""Immediate"" TValue=""int?"" @bind-Value=""immediateValue"" Immediate />
<div>Value: [@immediateValue]</div>

<BitNumberField Label=""Immediate & decimals (try typing 1.25)"" TValue=""double?"" @bind-Value=""immediateDecimalValue"" Immediate />
<div>Value: [@immediateDecimalValue]</div>

<BitNumberField Label=""Immediate & DebounceTime (300ms)"" TValue=""int?"" @bind-Value=""debounceValue"" Immediate DebounceTime=""300"" />
<div>Value: [@debounceValue]</div>

<BitNumberField Label=""ReadOnly"" ReadOnly Mode=""BitSpinButtonMode.Compact"" @bind-Value=""readOnlyValue"" />

<BitNumberField Label=""IsInputReadOnly"" IsInputReadOnly Mode=""BitSpinButtonMode.Compact"" @bind-Value=""inputReadOnlyValue"" />

<BitToggle @bind-Value=""invertMouseWheel"" Text=""Invert Mouse Wheel"" />

<BitNumberField InvertMouseWheel=""invertMouseWheel"" Label=""Click to focus, then Shift + Mouse Wheel"" TValue=""int"" />

<BitNumberField NoMouseWheel Label=""NoMouseWheel (the wheel never changes it)"" TValue=""int"" Mode=""BitSpinButtonMode.Compact"" />";
    private readonly string example13CsharpCode = @"
private int? immediateValue;
private double? immediateDecimalValue;
private int? debounceValue;
private int readOnlyValue = 10;
private int inputReadOnlyValue = 10;
private bool invertMouseWheel;";

    private readonly string example14RazorCode = @"
<BitNumberField Label=""Without NormalizeDigits"" @bind-Value=""normalizeOffValue"" Placeholder=""۱۲۳"" />
<div>Value: @normalizeOffValue</div>

<BitNumberField Label=""With NormalizeDigits (۱۲۳)"" @bind-Value=""normalizeOnValue"" NormalizeDigits Placeholder=""۱۲۳"" />
<div>Value: @normalizeOnValue</div>

<BitNumberField Label=""Fractioned NormalizeDigits (۱۲٫۵)"" @bind-Value=""normalizeDecimalValue"" NormalizeDigits Placeholder=""۱۲٫۵"" Precision=""2"" />
<div>Value: @normalizeDecimalValue</div>

<BitNumberField Label=""Custom DigitsNormalizer (۱٬۲۳۴)"" @bind-Value=""customNormalizerValue"" DigitsNormalizer=""CustomDigitsNormalizer"" Placeholder=""۱٬۲۳۴"" />
<div>Value: @customNormalizerValue</div>";
    private readonly string example14CsharpCode = @"
private int? normalizeOffValue;
private int? normalizeOnValue;
private double? normalizeDecimalValue;
private int? customNormalizerValue;

// Maps any Unicode decimal digit to its Latin equivalent and strips spaces
// and thousand separators (Latin ',' and Persian '٬').
private string? CustomDigitsNormalizer(string? value)
{
    if (string.IsNullOrEmpty(value)) return value;

    var sb = new System.Text.StringBuilder(value.Length);
    foreach (var c in value)
    {
        if (c is ' ' or ',' or '٬') continue;

        var digit = System.Globalization.CharUnicodeInfo.GetDecimalDigitValue(c);
        sb.Append(digit >= 0 ? (char)('0' + digit) : c);
    }

    return sb.ToString();
}";

    private readonly string example15RazorCode = @"
<BitNumberField Label=""OnIncrement & OnDecrement"" Mode=""BitSpinButtonMode.Compact""
                OnIncrement=""(double v) => onIncrementCounter++""
                OnDecrement=""(double v) => onDecrementCounter++"" />
<div>OnIncrement Counter: @onIncrementCounter</div>
<div>OnDecrement Counter: @onDecrementCounter</div>

<BitNumberField Label=""OnChange"" OnChange=""(double v) => onChangeCounter++"" />
<div>OnChange Counter: @onChangeCounter</div>

<BitNumberField Label=""OnMinReached & OnMaxReached (0 to 3)"" TValue=""int""
                Min=""0"" Max=""3"" Mode=""BitSpinButtonMode.Compact""
                OnMinReached=""HandleMinReached""
                OnMaxReached=""HandleMaxReached"" />
<div>[@boundMessage]</div>

<BitNumberField Label=""OnEnter (type a number and press Enter)"" TValue=""int?""
                @bind-Value=""enterValue""
                OnEnter=""HandleEnter"" />
<div>[@enterMessage]</div>

<BitNumberField Label=""OnKeyDown, OnKeyUp & OnClick"" TValue=""int?""
                OnClick=""() => onClickCounter++""
                OnKeyUp=""() => onKeyUpCounter++""
                OnKeyDown=""(KeyboardEventArgs e) => lastKey = e.Key"" />
<div>Last key down: [@lastKey]</div>
<div>OnKeyUp Counter: @onKeyUpCounter</div>
<div>OnClick Counter: @onClickCounter</div>";
    private readonly string example15CsharpCode = @"
private int onIncrementCounter;
private int onDecrementCounter;
private int onChangeCounter;
private int onKeyUpCounter;
private int onClickCounter;
private string? lastKey;
private string? boundMessage;
private string? enterMessage;
private int? enterValue;

private void HandleMinReached(int value)
{
    boundMessage = $""Reached the minimum ({value})."";
}

private void HandleMaxReached(int value)
{
    boundMessage = $""Reached the maximum ({value})."";
}

private void HandleEnter()
{
    enterMessage = $""Submitted: {enterValue}."";
}";

    private readonly string example16RazorCode = @"
<BitNumberField @ref=""apiNumberField"" Label=""Driven from the outside"" TValue=""int?""
                Min=""0"" Max=""10"" Step=""2"" @bind-Value=""apiValue"" />
<div>value: [@apiValue]</div>

<BitStack Horizontal Gap=""0.5rem"">
    <BitButton OnClick=""() => apiNumberField?.DecrementAsync()"">Decrement</BitButton>
    <BitButton OnClick=""() => apiNumberField?.IncrementAsync()"">Increment</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => apiNumberField?.ClearAsync()"">Clear</BitButton>
    <BitButton Variant=""BitVariant.Text"" OnClick=""() => apiNumberField?.FocusAsync()"">Focus</BitButton>
</BitStack>";
    private readonly string example16CsharpCode = @"
private int? apiValue = 4;
private BitNumberField<int?>? apiNumberField;";

    private readonly string example17RazorCode = @"
@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""@validationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">
        <DataAnnotationsValidator />

        <BitNumberField Label=""@($""Age: [{validationModel.Age}]"")"" @bind-Value=""validationModel.Age"" />
        <ValidationMessage For=""@(() => validationModel.Age)"" />

        <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
    </EditForm>
}
else
{
    <BitMessage Color=""BitColor.Success"">@SuccessMessage</BitMessage>
}

<EditForm Model=""@validationModel"">
    <DataAnnotationsValidator />

    <BitNumberField Label=""Custom ParsingErrorMessage (try typing letters)""
                    DisplayName=""Weight""
                    ParsingErrorMessage=""{0} must be a number.""
                    @bind-Value=""parsingErrorValue"" />
    <ValidationMessage For=""@(() => parsingErrorValue)"" />
</EditForm>

<EditForm Model=""@rangeModel"">
    <DataAnnotationsValidator />

    <BitNumberField Label=""NoClamp & [Range(0, 100)]"" NoClamp Min=""0"" Max=""100"" @bind-Value=""rangeModel.Percentage"" />
    <ValidationMessage For=""@(() => rangeModel.Percentage)"" />
</EditForm>";
    private readonly string example17CsharpCode = @"
public class BitNumberFieldValidationModel
{
    [Required(ErrorMessage = ""Enter an age"")]
    [Range(1, 150, ErrorMessage = ""Nobody is that old"")]
    public int? Age { get; set; }
}

public class RangeModel
{
    [Range(0, 100, ErrorMessage = ""The percentage must be between 0 and 100"")]
    public int Percentage { get; set; }
}

private string SuccessMessage = string.Empty;
private BitNumberFieldValidationModel validationModel = new();
private RangeModel rangeModel = new();
private double? parsingErrorValue;

private async Task HandleValidSubmit()
{
    SuccessMessage = ""Form Submitted Successfully!"";
    await Task.Delay(3000);
    SuccessMessage = string.Empty;
    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    SuccessMessage = string.Empty;
}";

    private readonly string example18RazorCode = @"
<BitNumberField Label=""Volume"" TValue=""int"" Min=""0"" Max=""10"" Mode=""BitSpinButtonMode.Compact""
                AriaDescription=""Use the up and down arrow keys to adjust the volume between 0 and 10.""
                IncrementAriaLabel=""Louder""
                DecrementAriaLabel=""Quieter"" />

<BitNumberField Label=""Rating"" TValue=""int"" Min=""1"" Max=""5"" DefaultValue=""3"" Mode=""BitSpinButtonMode.Compact""
                AriaValueText=""3 out of 5 stars"" />

<BitNumberField Label=""Quantity (2 of 3)"" TValue=""int?"" ShowClearButton DefaultValue=""1""
                AriaSetSize=""3"" AriaPositionInSet=""2""
                ClearButtonAriaLabel=""Remove the quantity"" />";

    private readonly string example19RazorCode = @"
<BitParams Parameters=""@numberFieldParams"">
    <BitNumberField Label=""Width"" TValue=""int"" />
    <BitNumberField Label=""Height"" TValue=""int"" />
    <BitNumberField Label=""Depth (its own Suffix)"" TValue=""int"" Suffix=""m"" />
</BitParams>

<BitNumberField Label=""Outside the cascade"" TValue=""int"" />";
    private readonly string example19CsharpCode = @"
private readonly BitNumberFieldParams[] numberFieldParams =
[
    new()
    {
        Min = ""0"",
        Max = ""500"",
        Step = ""5"",
        Suffix = ""cm"",
        Underlined = true,
        ShowClearButton = true,
        Mode = BitSpinButtonMode.Compact,
        Description = ""Between 0 and 500, in steps of 5.""
    }
];";

    private readonly string example20RazorCode = @"
<BitNumberField Label=""Primary (default)"" TValue=""int"" Background=""BitColorKind.Primary"" Mode=""BitSpinButtonMode.Compact"" />

<BitNumberField Label=""Secondary"" TValue=""int"" Background=""BitColorKind.Secondary"" Mode=""BitSpinButtonMode.Compact"" />

<BitNumberField Label=""Tertiary"" TValue=""int"" Background=""BitColorKind.Tertiary"" Mode=""BitSpinButtonMode.Compact"" />

<BitNumberField Label=""Transparent"" TValue=""int"" Background=""BitColorKind.Transparent"" Mode=""BitSpinButtonMode.Compact"" />";

    private readonly string example21RazorCode = @"
<BitNumberField Label=""Primary (default)"" TValue=""int"" Border=""BitColorKind.Primary"" Mode=""BitSpinButtonMode.Compact"" />

<BitNumberField Label=""Secondary"" TValue=""int"" Border=""BitColorKind.Secondary"" Mode=""BitSpinButtonMode.Compact"" />

<BitNumberField Label=""Tertiary"" TValue=""int"" Border=""BitColorKind.Tertiary"" Mode=""BitSpinButtonMode.Compact"" />

<BitNumberField Label=""Transparent"" TValue=""int"" Border=""BitColorKind.Transparent"" Mode=""BitSpinButtonMode.Compact"" />";

    private readonly string example22RazorCode = @"
<BitNumberField Label=""Primary (default)"" TValue=""int"" Accent=""BitColor.Primary"" IconName=""@BitIconName.Money"" Prefix=""$"" />

<BitNumberField Label=""Secondary"" TValue=""int"" Accent=""BitColor.Secondary"" IconName=""@BitIconName.Money"" Prefix=""$"" />

<BitNumberField Label=""Tertiary"" TValue=""int"" Accent=""BitColor.Tertiary"" IconName=""@BitIconName.Money"" Prefix=""$"" />

<BitNumberField Label=""Info"" TValue=""int"" Accent=""BitColor.Info"" IconName=""@BitIconName.Money"" Prefix=""$"" />

<BitNumberField Label=""Success"" TValue=""int"" Accent=""BitColor.Success"" IconName=""@BitIconName.Money"" Prefix=""$"" />

<BitNumberField Label=""Warning"" TValue=""int"" Accent=""BitColor.Warning"" IconName=""@BitIconName.Money"" Prefix=""$"" />

<BitNumberField Label=""SevereWarning"" TValue=""int"" Accent=""BitColor.SevereWarning"" IconName=""@BitIconName.Money"" Prefix=""$"" />

<BitNumberField Label=""Error"" TValue=""int"" Accent=""BitColor.Error"" IconName=""@BitIconName.Money"" Prefix=""$"" />

<BitNumberField Label=""PrimaryBackground"" TValue=""int"" Accent=""BitColor.PrimaryBackground"" IconName=""@BitIconName.Money"" Prefix=""$"" />

<BitNumberField Label=""SecondaryBackground"" TValue=""int"" Accent=""BitColor.SecondaryBackground"" IconName=""@BitIconName.Money"" Prefix=""$"" />

<BitNumberField Label=""TertiaryBackground"" TValue=""int"" Accent=""BitColor.TertiaryBackground"" IconName=""@BitIconName.Money"" Prefix=""$"" />

<BitNumberField Label=""PrimaryForeground"" TValue=""int"" Accent=""BitColor.PrimaryForeground"" IconName=""@BitIconName.Money"" Prefix=""$"" />

<BitNumberField Label=""SecondaryForeground"" TValue=""int"" Accent=""BitColor.SecondaryForeground"" IconName=""@BitIconName.Money"" Prefix=""$"" />

<BitNumberField Label=""TertiaryForeground"" TValue=""int"" Accent=""BitColor.TertiaryForeground"" IconName=""@BitIconName.Money"" Prefix=""$"" />

<BitNumberField Label=""PrimaryBorder"" TValue=""int"" Accent=""BitColor.PrimaryBorder"" IconName=""@BitIconName.Money"" Prefix=""$"" />

<BitNumberField Label=""SecondaryBorder"" TValue=""int"" Accent=""BitColor.SecondaryBorder"" IconName=""@BitIconName.Money"" Prefix=""$"" />

<BitNumberField Label=""TertiaryBorder"" TValue=""int"" Accent=""BitColor.TertiaryBorder"" IconName=""@BitIconName.Money"" Prefix=""$"" />";

    private readonly string example23RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitNumberField Label=""Icon with a plain class string"" TValue=""int"" Icon=""@(""fa-solid fa-calculator"")"" />

<BitNumberField Label=""Icon with BitIconInfo.Fa"" TValue=""int"" Icon=""@BitIconInfo.Fa(""solid lightbulb"")"" />

<BitNumberField Label=""Icon with BitIconInfo.Bi"" TValue=""int"" Icon=""@BitIconInfo.Bi(""calculator"")"" />

<BitNumberField Label=""FontAwesome spin buttons"" TValue=""int"" Mode=""BitSpinButtonMode.Compact""
                IncrementIcon=""@BitIconInfo.Fa(""solid plus"")""
                DecrementIcon=""@BitIconInfo.Fa(""solid minus"")"" />

<BitNumberField Label=""Bootstrap spin buttons"" TValue=""int"" Mode=""BitSpinButtonMode.Spread""
                IncrementIcon=""@BitIconInfo.Bi(""plus-circle-fill"")""
                DecrementIcon=""@BitIconInfo.Bi(""dash-circle-fill"")"" />";

    private readonly string example24RazorCode = @"
<BitNumberField Label=""Small"" TValue=""int"" Size=""BitSize.Small"" Mode=""BitSpinButtonMode.Compact"" IconName=""@BitIconName.Money"" Suffix=""kg"" />

<BitNumberField Label=""Medium (default)"" TValue=""int"" Size=""BitSize.Medium"" Mode=""BitSpinButtonMode.Compact"" IconName=""@BitIconName.Money"" Suffix=""kg"" />

<BitNumberField Label=""Large"" TValue=""int"" Size=""BitSize.Large"" Mode=""BitSpinButtonMode.Compact"" IconName=""@BitIconName.Money"" Suffix=""kg"" />";

    private readonly string example25RazorCode = @"
<BitNumberField TValue=""int"" DefaultValue=""12"" Label=""On the instance"" Mode=""BitSpinButtonMode.Inline"" Suffix=""cm""
                Style=""--bit-NumberField-radius: 1rem; --bit-NumberField-affix-background: transparent; --bit-NumberField-label-font-weight: 400; --bit-NumberField-border-width: 2px;"" />

<div class=""pill-field"">
    <BitNumberField TValue=""int"" DefaultValue=""3"" Label=""On an ancestor (a whole section)"" Mode=""BitSpinButtonMode.Spread"" />

    <BitNumberField TValue=""int"" DefaultValue=""7"" Label=""Every field under it follows"" Mode=""BitSpinButtonMode.Spread"" />
</div>

<BitNumberField DefaultValue=""10"" Style=""box-shadow: aqua 0 0 1rem; margin-inline: 1rem;"" />

<BitNumberField DefaultValue=""20"" Class=""custom-class"" />

<BitNumberField DefaultValue=""1""
                Label=""Styles""
                Description=""The description is styleable too.""
                Styles=""@(new() { Root = ""margin-inline: 1rem;"",
                                  Focused = ""--focused-background: #b2b2b25a;"",
                                  InputContainer = ""background: var(--focused-background);"",
                                  Label = ""text-shadow: aqua 0 0 1rem; font-weight: 900; font-size: 1.25rem;"",
                                  Description = ""color: darkviolet; font-style: italic;"",
                                  Input = ""padding: 0.5rem;"" })"" />

<BitNumberField TValue=""int?""
                Label=""Classes""
                @bind-Value=""classesValue""
                Classes=""@(new() { Root = ""custom-root"",
                                   InputContainer = ""custom-input-wrapper"",
                                   Focused = ""custom-focus"",
                                   Input = ""custom-input"",
                                   Label = $""custom-label{(classesValue is null ? string.Empty : "" custom-label-top"")}"" })"" />";
    private readonly string example25CsharpCode = @"
private int? classesValue;";
    private const string example25ScssCode = @"
.pill-field {
    // The variables inherit, so one block on an ancestor re-skins every field underneath it.
    --bit-NumberField-radius: 999px;
    --bit-NumberField-height: 2.75rem;
    --bit-NumberField-background: #f3f1ff;
    --bit-NumberField-border-color: #7c5cff;
    --bit-NumberField-focus-color: #7c5cff;
    --bit-NumberField-button-hover-background: #e6e0ff;
    --bit-NumberField-label-color: #7c5cff;
}

::deep {
    .custom-class {
        overflow: hidden;
        margin-inline: 1rem;
        border-radius: 1rem;
        border: 2px solid brown;
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

    .custom-input-wrapper {
        border-radius: 0;
        position: relative;
        border-width: 0 0 1px 0;
    }

    .custom-focus .custom-label {
        color: blueviolet;
        transform: translate(0, 1.5px) scale(0.75);
    }
}";
    private readonly DemoCodeFile[] example25CodeFiles =
    [
        new("BitNumberFieldDemo.razor.scss", example25ScssCode),
    ];

    private readonly string example26RazorCode = @"
<CascadingValue Value=""BitDir.Rtl"">

    <BitNumberField Label=""برچسب در بالا"" TValue=""int"" Mode=""BitSpinButtonMode.Compact"" />

    <BitNumberField Label=""برچسب در کنار"" TValue=""int"" LabelPosition=""BitLabelPosition.Start"" />

    <BitNumberField TValue=""int"" Required />

    <BitNumberField Label=""الزامی"" TValue=""int"" Required Suffix=""کیلوگرم"" Mode=""BitSpinButtonMode.Spread"" />

</CascadingValue>";
}
