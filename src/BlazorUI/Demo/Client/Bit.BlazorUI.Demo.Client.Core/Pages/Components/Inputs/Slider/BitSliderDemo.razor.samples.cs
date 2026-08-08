namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Slider;

public partial class BitSliderDemo
{
    private readonly string example1RazorCode = @"
<BitSlider Label=""Basic slider"" />

<BitSlider Label=""Volume"" DefaultValue=""7"" />

<BitSlider Label=""Percentage"" Min=""0"" Max=""100"" Step=""10"" DefaultValue=""40"" />

<BitSlider Label=""Disabled slider"" DefaultValue=""5"" IsEnabled=""false"" />

<BitSlider Label=""ReadOnly slider"" DefaultValue=""6"" ReadOnly />";

    private readonly string example2RazorCode = @"
<BitSlider Label=""Basic range"" IsRanged />

<BitSlider Label=""Price"" IsRanged Min=""0"" Max=""1000"" Step=""50""
           ValueFormat=""C0""
           DefaultLowerValue=""200""
           DefaultUpperValue=""750"" />

<BitSlider Label=""Age band"" IsRanged Min=""0"" Max=""100"" Step=""5"" @bind-RangeValue=""ageBand"" />
<BitLabel>From @ageBand.Lower to @ageBand.Upper (@ageBand.Length years wide)</BitLabel>

<BitSlider Label=""Disabled range"" IsRanged DefaultLowerValue=""2"" DefaultUpperValue=""5"" IsEnabled=""false"" />";
    private readonly string example2CsharpCode = @"
private BitSliderRangeValue ageBand = new(25, 45);";

    private readonly string example3RazorCode = @"
<BitSlider Label=""Steps of one"" ShowMarks DefaultValue=""6"" />

<BitSlider Label=""Rating"" ShowMarks ShowMarkLabels Max=""5"" DefaultValue=""3"" />

<BitSlider Label=""Budget"" ShowMarks ShowMarkLabels
           Min=""0"" Max=""1000"" Step=""10"" MarkStep=""200""
           ValueFormat=""C0""
           DefaultValue=""400"" />

<BitSlider Label=""Quality"" Max=""4"" Marks=""qualityMarks"" DefaultValue=""2"" ShowValue=""false"" />

<BitSlider Label=""Storage"" RestrictToMarks
           Min=""0"" Max=""2000"" Step=""1""
           Marks=""storageMarks""
           ShowValue=""false""
           @bind-Value=""storageValue"" />
<BitLabel>Value: @storageValue GB</BitLabel>

<BitSlider Label=""Working hours"" IsRanged ShowMarks ShowMarkLabels
           Min=""0"" Max=""24"" Step=""1"" MarkStep=""6""
           ValueFormat=""0'h'""
           DefaultLowerValue=""9""
           DefaultUpperValue=""17"" />";

    private readonly string example3CsharpCode = @"
private readonly List<BitSliderMark> qualityMarks =
[
    new(0, ""Draft""),
    new(1, ""Low""),
    new(2, ""Medium""),
    new(3, ""High""),
    new(4, ""Lossless"")
];

private readonly List<BitSliderMark> storageMarks =
[
    new(0, ""0""),
    new(64, ""64""),
    new(128, ""128""),
    new(256, ""256""),
    new(512, ""512""),
    new(1000, ""1 TB""),
    new(2000, ""2 TB"")
];
private double storageValue = 256;";

    private readonly string example4RazorCode = @"
<BitSlider Label=""Basic"" IsVertical DefaultValue=""4"" />

<BitSlider Label=""Disabled"" IsVertical DefaultValue=""4"" IsEnabled=""false"" />

<BitSlider Label=""Formatted"" IsVertical DefaultValue=""2"" ValueFormat=""0 cm"" />

<BitSlider Label=""Ranged"" IsVertical IsRanged DefaultLowerValue=""2"" DefaultUpperValue=""7"" />

<BitSlider Label=""Marks"" IsVertical ShowMarks ShowMarkLabels MarkStep=""2"" DefaultValue=""6"" />

<BitSlider Label=""Taller"" IsVertical Style=""--bit-sld-length: 18rem"" DefaultValue=""8"" />";

    private readonly string example5RazorCode = @"
<BitSlider Label=""Balance"" Min=""-5"" Max=""5"" DefaultValue=""3"" IsOriginFromZero />

<BitSlider Label=""Temperature"" Min=""-2"" Max=""8"" DefaultValue=""-1"" IsOriginFromZero ValueFormat=""0 °C"" />

<BitSlider Label=""Against target"" Min=""0"" Max=""100"" Step=""5"" Origin=""60"" DefaultValue=""85"" ValueFormat=""0'%'"" />

<BitSlider Label=""Remaining"" Max=""100"" Step=""5"" Inverted DefaultValue=""30"" ValueFormat=""0'%'"" />

<BitSlider Label=""Excluded"" IsRanged Inverted Min=""0"" Max=""100"" Step=""5""
           DefaultLowerValue=""35""
           DefaultUpperValue=""65"" />

<BitSlider Label=""Position"" NoFill ShowMarks Max=""100"" Step=""5"" MarkStep=""10"" DefaultValue=""45"" />

<BitSlider Label=""Bounds"" NoFill IsRanged Max=""100"" Step=""5""
           DefaultLowerValue=""25""
           DefaultUpperValue=""75"" />";

    private readonly string example6RazorCode = @"
<BitSlider Label=""Auto"" ThumbLabel=""BitSliderThumbLabel.Auto"" ShowValue=""false"" DefaultValue=""4"" />

<BitSlider Label=""On"" ThumbLabel=""BitSliderThumbLabel.On"" ShowValue=""false"" Max=""100"" Step=""5"" DefaultValue=""35"" ValueFormat=""0'%'"" />

<BitSlider Label=""Range"" IsRanged ThumbLabel=""BitSliderThumbLabel.On"" ShowValue=""false""
           Min=""0"" Max=""1000"" Step=""50""
           ValueFormat=""C0""
           DefaultLowerValue=""250""
           DefaultUpperValue=""700"" />

<BitSlider Label=""Gain"" IsVertical ThumbLabel=""BitSliderThumbLabel.On"" ShowValue=""false"" DefaultValue=""6"" ValueFormat=""0 dB"" />

<BitSlider Label=""Volume"" ThumbLabel=""BitSliderThumbLabel.On"" ShowValue=""false"" Max=""100"" Step=""5"" DefaultValue=""60"">
    <ThumbLabelTemplate Context=""value"">
        <span class=""thumb-template"">
            <BitIcon IconName=""@GetVolumeIcon(value)"" Size=""BitSize.Small"" Style=""color:inherit"" />
            @value.ToString(""0"")%
        </span>
    </ThumbLabelTemplate>
</BitSlider>";
    private readonly string example6CsharpCode = @"
private static string GetVolumeIcon(double value) => value switch
{
    <= 0 => BitIconName.VolumeDisabled,
    < 34 => BitIconName.Volume1,
    < 67 => BitIconName.Volume2,
    _ => BitIconName.Volume3
};";

    private readonly string example7RazorCode = @"
<BitSlider IsRanged Max=""100"" Step=""5"" @bind-RangeValue=""freeRange"" />
<BitLabel>@freeRange.Lower - @freeRange.Upper</BitLabel>

<BitSlider IsRanged NoSwap Max=""100"" Step=""5"" DefaultLowerValue=""30"" DefaultUpperValue=""70"" />

<BitSlider IsRanged MinRange=""20"" Max=""100"" Step=""5"" DefaultLowerValue=""20"" DefaultUpperValue=""60"" />

<BitSlider IsRanged MaxRange=""30"" Max=""100"" Step=""5"" DefaultLowerValue=""30"" DefaultUpperValue=""50"" />

<BitSlider IsRanged MinRange=""10"" MaxRange=""40"" Max=""100"" Step=""5"" DefaultLowerValue=""30"" DefaultUpperValue=""50"" />

<BitSlider IsRanged Pushable MinRange=""20"" Max=""100"" Step=""5"" @bind-RangeValue=""pushableRange"" />
<BitLabel>@pushableRange.Lower - @pushableRange.Upper (@pushableRange.Length wide)</BitLabel>

<BitSlider IsRanged Pushable NoSwap Max=""100"" Step=""5"" DefaultLowerValue=""30"" DefaultUpperValue=""70"" />";
    private readonly string example7CsharpCode = @"
private BitSliderRangeValue freeRange = new(30, 70);
private BitSliderRangeValue pushableRange = new(20, 40);";

    private readonly string example8RazorCode = @"
<BitSlider Label=""Currency"" Min=""0"" Max=""1000"" Step=""50"" ValueFormat=""C0"" DefaultValue=""450"" />
<BitSlider Label=""Percentage"" Max=""1"" Step=""0.01"" ValueFormat=""0 %"" DefaultValue=""0.69"" />
<BitSlider Label=""Fixed decimals"" Max=""5"" Step=""0.1"" ValueFormat=""0.0 rem"" DefaultValue=""2.5"" />

<BitSlider Label=""Meeting time""
           Min=""480"" Max=""1080"" Step=""30"" MarkStep=""120""
           ShowMarks ShowMarkLabels
           GetValueText=""GetTimeText""
           DefaultValue=""750"" />

<BitSlider Label=""No value label"" ShowValue=""false"" DefaultValue=""4"" />

<BitSlider DefaultValue=""6"" Max=""10"">
    <LabelTemplate>
        <span class=""template-label"">
            <BitIcon IconName=""@BitIconName.Volume3"" />
            Master volume
        </span>
    </LabelTemplate>
</BitSlider>";
    private readonly string example8CsharpCode = @"
private static string GetTimeText(double minutes)
{
    return TimeSpan.FromMinutes(minutes).ToString(@""hh\:mm"", CultureInfo.InvariantCulture);
}";

    private readonly string example9RazorCode = @"
<BitSlider Label=""One-way"" Value=""oneWayBinding"" />
<BitRating Max=""10"" @bind-Value=""oneWayBinding"" />

<BitSlider Label=""Two-way"" @bind-Value=""twoWayBinding"" />
<BitRating Max=""10"" @bind-Value=""twoWayBinding"" />

<BitSlider Label=""Range"" IsRanged Max=""100"" Step=""5""
           @bind-LowerValue=""boundLower""
           @bind-UpperValue=""boundUpper"" />
<BitLabel>LowerValue: @boundLower &nbsp; UpperValue: @boundUpper</BitLabel>";
    private readonly string example9CsharpCode = @"
private double oneWayBinding = 3;
private double twoWayBinding = 5;
private double boundLower = 25;
private double boundUpper = 75;";

    private readonly string example10RazorCode = @"
<BitSlider Label=""Drag me"" Max=""100""
           DefaultValue=""20""
           OnChange=""v => onChangeCount++""
           OnChangeEnd=""v => onChangeEndValue = v"" />
<BitLabel>OnChange fired @onChangeCount times &nbsp; · &nbsp; OnChangeEnd committed @onChangeEndValue</BitLabel>

<BitSlider Label=""Drag me too"" IsRanged Max=""100""
           DefaultLowerValue=""20""
           DefaultUpperValue=""60""
           OnRangeChange=""r => onRangeChangeCount++""
           OnRangeChangeEnd=""r => onRangeChangeEndValue = r"" />
<BitLabel>
    OnRangeChange fired @onRangeChangeCount times &nbsp; · &nbsp;
    OnRangeChangeEnd committed @onRangeChangeEndValue?.Lower - @onRangeChangeEndValue?.Upper
</BitLabel>

<BitSlider Label=""Focus me"" IsRanged Max=""100"" Step=""5""
           DefaultLowerValue=""30""
           DefaultUpperValue=""70""
           OnFocusIn=""() => focusState = focusedText""
           OnFocusOut=""() => focusState = blurredText"" />
<BitLabel>The slider is @focusState</BitLabel>";
    private readonly string example10CsharpCode = @"
private int onChangeCount;
private double? onChangeEndValue;
private int onRangeChangeCount;
private BitSliderRangeValue? onRangeChangeEndValue;

private const string focusedText = ""focused"";
private const string blurredText = ""blurred"";
private string focusState = blurredText;";

    private readonly string example11RazorCode = @"
<EditForm Model=""ValidationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">

    <DataAnnotationsValidator />

    <BitSlider Required
               Name=""rate""
               Label=""How many days a week?""
               Max=""7""
               ShowMarks
               ShowMarkLabels
               @bind-Value=""ValidationModel.Days"" />
    <ValidationMessage For=""@(() => ValidationModel.Days)"" />

    <BitSlider IsRanged
               Label=""Budget bracket""
               Max=""100""
               Step=""5""
               @bind-RangeValue=""ValidationModel.Budget"" />
    <ValidationMessage For=""@(() => ValidationModel.Budget)"" />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example11CsharpCode = @"
public class BitSliderDemoFormModel
{
    [Range(typeof(double), ""4"", ""7"", ErrorMessage = ""Pick at least {1} days a week"")]
    public double Days { get; set; }

    [MinimumRangeLength(20, ErrorMessage = ""Cover a span of at least 20"")]
    public BitSliderRangeValue? Budget { get; set; } = new(40, 50);
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class MinimumRangeLengthAttribute(double minimum) : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value is BitSliderRangeValue range && range.Length >= minimum;
    }
}

public BitSliderDemoFormModel ValidationModel = new();
public string? SuccessMessage;

private async Task HandleValidSubmit()
{
    SuccessMessage = ""Form Submitted Successfully!"";
    await Task.Delay(2000);
    SuccessMessage = string.Empty;
    ValidationModel.Days = default;
    ValidationModel.Budget = new(40, 50);
    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    SuccessMessage = string.Empty;
}";

    private readonly string example12RazorCode = @"
<BitSlider Label=""Quality""
           Max=""4""
           ShowMarks
           AriaValueText=""GetQualityText""
           DefaultValue=""2"" />

<BitSlider IsRanged
           AriaLabel=""Price range""
           LowerAriaLabel=""Minimum price""
           UpperAriaLabel=""Maximum price""
           Min=""0"" Max=""1000"" Step=""50""
           ValueFormat=""C0""
           DefaultLowerValue=""150""
           DefaultUpperValue=""800"" />

<BitSlider IsRanged Max=""10"" DefaultLowerValue=""3"" DefaultUpperValue=""7"">
    <LabelTemplate>
        <span class=""template-label"">
            <BitIcon IconName=""@BitIconName.Equalizer"" />
            Frequency band
        </span>
    </LabelTemplate>
</BitSlider>";
    private readonly string example12CsharpCode = @"
private static readonly string[] qualityWords = [""Draft"", ""Low"", ""Medium"", ""High"", ""Lossless""];

private static string GetQualityText(double value)
{
    var index = (int)Math.Clamp(value, 0, qualityWords.Length - 1);

    return qualityWords[index];
}";

    private readonly string example13RazorCode = @"
<BitSlider Color=""BitColor.Primary"" Label=""Primary"" DefaultValue=""6"" />
<BitSlider Color=""BitColor.Secondary"" Label=""Secondary"" DefaultValue=""6"" />
<BitSlider Color=""BitColor.Tertiary"" Label=""Tertiary"" DefaultValue=""6"" />
<BitSlider Color=""BitColor.Info"" Label=""Info"" DefaultValue=""6"" />
<BitSlider Color=""BitColor.Success"" Label=""Success"" DefaultValue=""6"" />
<BitSlider Color=""BitColor.Warning"" Label=""Warning"" DefaultValue=""6"" />
<BitSlider Color=""BitColor.SevereWarning"" Label=""SevereWarning"" DefaultValue=""6"" />
<BitSlider Color=""BitColor.Error"" Label=""Error"" DefaultValue=""6"" />

<BitSlider Color=""BitColor.PrimaryBackground"" Label=""PrimaryBackground"" DefaultValue=""6"" />
<BitSlider Color=""BitColor.SecondaryBackground"" Label=""SecondaryBackground"" DefaultValue=""6"" />
<BitSlider Color=""BitColor.TertiaryBackground"" Label=""TertiaryBackground"" DefaultValue=""6"" />

<BitSlider Color=""BitColor.PrimaryForeground"" Label=""PrimaryForeground"" DefaultValue=""6"" />
<BitSlider Color=""BitColor.SecondaryForeground"" Label=""SecondaryForeground"" DefaultValue=""6"" />
<BitSlider Color=""BitColor.TertiaryForeground"" Label=""TertiaryForeground"" DefaultValue=""6"" />
<BitSlider Color=""BitColor.PrimaryBorder"" Label=""PrimaryBorder"" DefaultValue=""6"" />
<BitSlider Color=""BitColor.SecondaryBorder"" Label=""SecondaryBorder"" DefaultValue=""6"" />
<BitSlider Color=""BitColor.TertiaryBorder"" Label=""TertiaryBorder"" DefaultValue=""6"" />";

    private readonly string example14RazorCode = @"
<BitSlider Size=""BitSize.Small"" ShowMarks DefaultValue=""6"" />

<BitSlider Size=""BitSize.Medium"" ShowMarks DefaultValue=""6"" />

<BitSlider Size=""BitSize.Large"" ShowMarks DefaultValue=""6"" />

<BitSlider Size=""BitSize.Small"" IsVertical DefaultValue=""6"" />
<BitSlider Size=""BitSize.Medium"" IsVertical DefaultValue=""6"" />
<BitSlider Size=""BitSize.Large"" IsVertical DefaultValue=""6"" />";

    private readonly string example15RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem 1rem;
        border-radius: 0.25rem;
        border: 1px solid dodgerblue;
        box-shadow: dodgerblue 0 0 1rem;
    }

    .custom-fill {
        background: linear-gradient(90deg, mediumseagreen, seagreen);
    }

    .custom-mark {
        border-radius: 0;
        background-color: seagreen;
    }
</style>


<BitSlider DefaultValue=""6"" Label=""Style"" Style=""padding: 0.5rem 1rem; border-radius: 0.5rem; box-shadow: tomato 0 0 1rem;"" />
<BitSlider DefaultValue=""6"" Label=""Class"" Class=""custom-class"" />

<BitSlider DefaultValue=""6"" Label=""Chunky"" Style=""--bit-sld-thumb: 2rem; --bit-sld-rail: 1rem;"" />
<BitSlider DefaultValue=""6"" Label=""Hairline"" Style=""--bit-sld-thumb: 0.75rem; --bit-sld-rail: 0.125rem;"" />

<BitSlider DefaultValue=""6""
           Label=""Custom styles""
           Styles=""@(new() { Label = ""font-weight: 900; font-size: 1.25rem;"",
                             Fill = ""background: linear-gradient(90deg, gold, orangered);"",
                             ValueLabel = ""color: orangered;"" })"" />

<BitSlider DefaultValue=""6""
           ShowMarks
           Label=""Custom classes""
           Classes=""@(new() { Fill = ""custom-fill"", Mark = ""custom-mark"" })"" />

<BitSlider IsRanged
           Label=""Two-tone thumbs""
           DefaultLowerValue=""3""
           DefaultUpperValue=""7""
           Styles=""@(new() { Thumb = ""border-radius: 0.25rem;"",
                             LowerThumb = ""border-color: mediumseagreen;"",
                             UpperThumb = ""border-color: orangered;"" })"" />";

    private readonly string example16RazorCode = @"
<BitSlider Dir=""BitDir.Rtl"" Label=""اسلایدر ساده"" DefaultValue=""4"" />

<BitSlider Dir=""BitDir.Rtl"" Label=""با علامت‌ها"" ShowMarks ShowMarkLabels Max=""5"" DefaultValue=""3"" />

<BitSlider Dir=""BitDir.Rtl"" Label=""بازه"" IsRanged DefaultLowerValue=""2"" DefaultUpperValue=""7"" />";
}
