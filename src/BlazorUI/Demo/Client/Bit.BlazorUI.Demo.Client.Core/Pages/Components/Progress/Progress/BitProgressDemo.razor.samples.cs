namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Progress.Progress;

public partial class BitProgressDemo
{
    private readonly string example1RazorCode = @"
<BitProgress Label=""Basic Progress""
             Description=""Example description""
             Percent=""42"" />";

    private readonly string example2RazorCode = @"
<BitProgress Circular
             Label=""Basic Circular Progress""
             Description=""Example description""
             Percent=""42"" />";

    private readonly string example3RazorCode = @"
<BitProgress Label=""Uploading files""
             Description=""3 of 10 files""
             AriaValueText=""3 of 10 files""
             Value=""3""
             Max=""10"" />

<BitProgress Label=""Temperature""
             Description=""Value 32 of a 20 to 40 range""
             Min=""20""
             Max=""40""
             Value=""32""
             ShowPercentNumber />

<BitProgress Circular
             Label=""Steps""
             Value=""7""
             Max=""12""
             ShowPercentNumber />";

    private readonly string example4RazorCode = @"
<BitProgress Label=""Show Percent Number""
             Percent=""85.69""
             ShowPercentNumber />

<BitProgress Label=""Percent Number Format""
             Percent=""85.69""
             PercentNumberFormat=""{0:F2} %""
             ShowPercentNumber />

<BitProgress Label=""Percent Number Template"" Percent=""85.69"">
    <PercentNumberTemplate Context=""percent"">
        <BitText Color=""BitColor.Success"">@($""{percent:F0}% done"")</BitText>
    </PercentNumberTemplate>
</BitProgress>

<BitProgress Label=""Start"" Percent=""42"" ShowPercentNumber
             PercentNumberPosition=""BitProgressPercentPosition.Start"" />

<BitProgress Label=""Center"" Percent=""42"" ShowPercentNumber
             PercentNumberPosition=""BitProgressPercentPosition.Center"" />

<BitProgress Label=""Inside"" Percent=""42"" Thickness=""20"" Rounded ShowPercentNumber
             PercentNumberPosition=""BitProgressPercentPosition.Inside"" />

<BitProgress Circular
             Label=""Show Percent Number""
             Percent=""85.69""
             ShowPercentNumber />

<BitProgress Circular
             Label=""Percent Number Format""
             Percent=""85.69""
             PercentNumberFormat=""{0:F2} %""
             ShowPercentNumber />";

    private readonly string example5RazorCode = @"
<BitSlider @bind-Value=""barThickness"" Max=""50"" />

<BitProgress ShowPercentNumber Percent=""69"" Thickness=""(int)barThickness"" />

<BitProgress Circular ShowPercentNumber Percent=""69"" Thickness=""(int)barThickness"" />";
    private readonly string example5CsharpCode = @"
private double barThickness = 10;";

    private readonly string example6RazorCode = @"
<BitProgress Circular Percent=""69"" Diameter=""24"" />
<BitProgress Circular Percent=""69"" Diameter=""48"" />
<BitProgress Circular Percent=""69"" Diameter=""96"" ShowPercentNumber />
<BitProgress Circular Indeterminate Diameter=""128"" Thickness=""6"" />";

    private readonly string example7RazorCode = @"
<BitProgress Indeterminate Label=""Preparing your export"" />

<BitProgress Circular Indeterminate />";

    private readonly string example8RazorCode = @"
<BitProgress Circular Indeterminate Size=""BitSize.Small"" AriaLabel=""Loading"" />
<BitProgress Circular Indeterminate Size=""BitSize.Medium"" AriaLabel=""Loading"" />
<BitProgress Circular Indeterminate Size=""BitSize.Large"" AriaLabel=""Loading"" />

<BitProgress Circular Indeterminate Color=""BitColor.Success"" AriaLabel=""Loading"" />
<BitProgress Circular Indeterminate Color=""BitColor.Warning"" AriaLabel=""Loading"" />
<BitProgress Circular Indeterminate Color=""BitColor.Error"" AriaLabel=""Loading"" />";

    private readonly string example9RazorCode = @"
<BitProgress Label=""Playing"" Percent=""35"" Buffer=""62"" ShowPercentNumber />

<BitProgress Label=""Processing"" Value=""4"" Max=""10"" Buffer=""8"" />

<BitProgress Circular Percent=""35"" Buffer=""62"" Thickness=""6"" ShowPercentNumber />

<BitSlider Label=""Progress"" @bind-Value=""bufferPercent"" Max=""100"" />
<BitProgress Percent=""bufferPercent"" Buffer=""Math.Min(100, bufferPercent + 25)"" Thickness=""8"" Rounded />";
    private readonly string example9CsharpCode = @"
private double bufferPercent = 40;";

    private readonly string example10RazorCode = @"
<BitProgress Rounded Percent=""42"" Thickness=""10"" />

<BitProgress Rounded Indeterminate Thickness=""10"" />

<BitProgress Circular Rounded Percent=""42"" Thickness=""8"" />
<BitProgress Circular Rounded Indeterminate Thickness=""8"" />";

    private readonly string example11RazorCode = @"
<BitProgress Striped Percent=""42"" Thickness=""12"" />

<BitProgress Striped StripedAnimation Percent=""42"" Thickness=""12"" />

<BitProgress Striped StripedAnimation Rounded Percent=""72"" Thickness=""12"" Color=""BitColor.Success"" />";

    private readonly string example12RazorCode = @"
<BitProgress Reversed Percent=""42"" Thickness=""10"" ShowPercentNumber />

<BitProgress Reversed Indeterminate Thickness=""10"" />

<BitProgress Circular Reversed Percent=""42"" Thickness=""6"" />
<BitProgress Circular Reversed Indeterminate Thickness=""6"" />";

    private readonly string example13RazorCode = @"
<BitSlider @bind-Value=""gaugeValue"" Max=""100"" />

<BitProgress Circular Rounded ShowPercentNumber
             GapDegree=""90""
             Diameter=""120""
             Thickness=""10""
             Percent=""gaugeValue"" />

<BitProgress Circular Rounded ShowPercentNumber
             GapDegree=""180""
             Diameter=""120""
             Thickness=""10""
             Color=""BitColor.Success""
             Percent=""gaugeValue"" />

<BitProgress Circular Rounded
             GapDegree=""120""
             Diameter=""120""
             Thickness=""14""
             Color=""BitColor.Warning""
             Min=""20""
             Max=""40""
             Value=""20 + gaugeValue / 5""
             Buffer=""38""
             AriaValueText=""@($""{20 + gaugeValue / 5:F0} degrees"")"" />

<BitProgress Circular Rounded GapDegree=""120"" Diameter=""80"" Thickness=""8"" Percent=""gaugeValue""
             GapPosition=""BitProgressGapPosition.Top"" />
<BitProgress Circular Rounded GapDegree=""120"" Diameter=""80"" Thickness=""8"" Percent=""gaugeValue""
             GapPosition=""BitProgressGapPosition.Start"" />
<BitProgress Circular Rounded GapDegree=""120"" Diameter=""80"" Thickness=""8"" Percent=""gaugeValue""
             GapPosition=""BitProgressGapPosition.End"" />";
    private readonly string example13CsharpCode = @"
private double gaugeValue = 65;";

    private readonly string example14RazorCode = @"
<BitProgress Vertical Percent=""42"" Thickness=""12"" Rounded />
<BitProgress Vertical Percent=""42"" Thickness=""12"" Length=""6rem"" Color=""BitColor.Success"" />
<BitProgress Vertical Reversed Percent=""42"" Thickness=""12"" Color=""BitColor.Warning"" />
<BitProgress Vertical Percent=""42"" Buffer=""75"" Thickness=""16"" Rounded Color=""BitColor.Info"" />
<BitProgress Vertical Striped StripedAnimation Percent=""60"" Thickness=""16"" />
<BitProgress Vertical Segments=""4"" Percent=""60"" Thickness=""16"" />
<BitProgress Vertical Indeterminate Thickness=""12"" />";

    private readonly string example15RazorCode = @"
<BitSlider @bind-Value=""segmentedPercent"" Max=""100"" />

<BitProgress Segments=""5"" Percent=""segmentedPercent"" Thickness=""12"" ShowPercentNumber />

<BitProgress Segments=""10"" SegmentGap=""2"" Percent=""segmentedPercent"" Thickness=""8"" Color=""BitColor.Success"" />

<BitProgress Segments=""4"" SegmentGap=""8"" Rounded Percent=""segmentedPercent"" Thickness=""14"" Buffer=""100"" />";
    private readonly string example15CsharpCode = @"
private double segmentedPercent = 45;";

    private readonly string example16RazorCode = @"
<BitSlider @bind-Value=""announcedPercent"" Max=""100"" />

<BitProgress AnnounceProgress
             Label=""Importing rows""
             Percent=""announcedPercent""
             ShowPercentNumber />

<BitProgress AnnounceProgress
             AnnounceStep=""10""
             Label=""Uploading""
             Percent=""announcedPercent""
             Thickness=""8"" />";
    private readonly string example16CsharpCode = @"
private double announcedPercent = 0;";

    private readonly string example17RazorCode = @"
<BitProgress Color=""BitColor.Primary"" Percent=""69"" />
<BitProgress Color=""BitColor.Secondary"" Percent=""69"" />
<BitProgress Color=""BitColor.Tertiary"" Percent=""69"" />
<BitProgress Color=""BitColor.Info"" Percent=""69"" />
<BitProgress Color=""BitColor.Success"" Percent=""69"" />
<BitProgress Color=""BitColor.Warning"" Percent=""69"" />
<BitProgress Color=""BitColor.SevereWarning"" Percent=""69"" />
<BitProgress Color=""BitColor.Error"" Percent=""69"" />

<BitProgress Color=""BitColor.Primary"" Circular Percent=""69"" />
<BitProgress Color=""BitColor.Secondary"" Circular Percent=""69"" />
<BitProgress Color=""BitColor.Tertiary"" Circular Percent=""69"" />
<BitProgress Color=""BitColor.Info"" Circular Percent=""69"" />
<BitProgress Color=""BitColor.Success"" Circular Percent=""69"" />
<BitProgress Color=""BitColor.Warning"" Circular Percent=""69"" />
<BitProgress Color=""BitColor.SevereWarning"" Circular Percent=""69"" />
<BitProgress Color=""BitColor.Error"" Circular Percent=""69"" />";

    private readonly string example18RazorCode = @"
<BitProgress Size=""BitSize.Small"" Label=""Small"" Percent=""69"" ShowPercentNumber />
<BitProgress Size=""BitSize.Medium"" Label=""Medium"" Percent=""69"" ShowPercentNumber />
<BitProgress Size=""BitSize.Large"" Label=""Large"" Percent=""69"" ShowPercentNumber />

<BitProgress Size=""BitSize.Small"" Circular Percent=""69"" ShowPercentNumber />
<BitProgress Size=""BitSize.Medium"" Circular Percent=""69"" ShowPercentNumber />
<BitProgress Size=""BitSize.Large"" Circular Percent=""69"" ShowPercentNumber />";

    private readonly string example19RazorCode = @"
<style>
    .custom-class {
        padding: 0.2rem;
        margin-bottom: 1rem;
        border-radius: 0.5rem;
        background-color: darkred;
    }

    .custom-track {
        background-color: #ff6a00;
    }

    .custom-buffer {
        background-color: #ffb680;
    }

    .custom-bar {
        background-color: #ff2700;
    }

    .custom-circle-track {
        stroke: #ff6a00;
    }

    .custom-circle-bar {
        stroke: #ff2700;
    }
</style>


<BitProgress Indeterminate Style=""background-color: #e687dc; border-radius: 0.5rem; padding: 0.2rem;"" Thickness=""10"" />

<BitProgress Class=""custom-class""
             Percent=""69""
             Thickness=""10"" />


<BitProgress Circular Indeterminate Style=""background-color: #e687dc; border-radius: 0.5rem; padding: 0.2rem;"" Thickness=""10"" />

<BitProgress Circular
             Class=""custom-class""
             Percent=""69""
             Thickness=""10"" />


<BitProgress Indeterminate
             Thickness=""10""
             Styles=""@(new() { Bar = ""background: linear-gradient(to right, green 0%, yellow 50%, green 100%);"",
                               Track = ""background-color: green;"" })"" />

<BitProgress Classes=""@(new() { Bar = ""custom-bar"",
                                Track = ""custom-track"",
                                Buffer = ""custom-buffer"" })""
             Percent=""45""
             Buffer=""80""
             Thickness=""10"" />


<BitProgress Circular Indeterminate
             Thickness=""10""
             Styles=""@(new() { Bar = ""stroke: greenyellow;"",
                               Track = ""stroke: green;"" })"" />

<BitProgress Circular
             Percent=""69""
             Thickness=""10""
             Classes=""@(new() { Bar = ""custom-circle-bar"",
                                Track = ""custom-circle-track"" })"" />";

    private readonly string example20RazorCode = @"
<BitProgress Dir=""BitDir.Rtl""
             Thickness=""10""
             Indeterminate />

<BitProgress Label=""لیبل تست""
             Description=""توضیحات تست""
             Dir=""BitDir.Rtl""
             Percent=""69""
             Thickness=""10""
             ShowPercentNumber />

<BitProgress Circular
             Dir=""BitDir.Rtl""
             Thickness=""10""
             Indeterminate />

<BitProgress Circular
             Label=""لیبل تست""
             Description=""توضیحات تست""
             Dir=""BitDir.Rtl""
             Percent=""69""
             Thickness=""10""
             ShowPercentNumber />";
}
