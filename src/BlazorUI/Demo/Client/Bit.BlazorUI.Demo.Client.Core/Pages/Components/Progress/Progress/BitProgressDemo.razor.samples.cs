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

<BitProgress Label=""Re-indexing rows""
             Description=""Row 3,200 of the 1,000 to 5,000 range""
             AriaValueText=""row 3,200 of 5,000""
             Min=""1000""
             Max=""5000""
             Value=""3200""
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

<BitProgress Label=""Top"" Percent=""42"" ShowPercentNumber
             PercentNumberPosition=""BitProgressPercentPosition.Top"" />

<BitProgress Percent=""42"" ShowPercentNumber AriaLabel=""Progress without a label""
             PercentNumberPosition=""BitProgressPercentPosition.Top"" />

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

<BitProgress AriaLabel=""Thickness example"" ShowPercentNumber Percent=""69"" Thickness=""(int)barThickness"" />

<BitProgress Circular AriaLabel=""Circular thickness example"" ShowPercentNumber Percent=""69"" Thickness=""(int)barThickness"" />";
    private readonly string example5CsharpCode = @"
private double barThickness = 10;";

    private readonly string example6RazorCode = @"
<BitProgress Circular AriaLabel=""24 pixel ring"" Percent=""69"" Diameter=""24"" />
<BitProgress Circular AriaLabel=""48 pixel ring"" Percent=""69"" Diameter=""48"" />
<BitProgress Circular AriaLabel=""96 pixel ring"" Percent=""69"" Diameter=""96"" ShowPercentNumber />
<BitProgress Circular Indeterminate AriaLabel=""128 pixel spinner"" Diameter=""128"" Thickness=""6"" />";

    private readonly string example7RazorCode = @"
<BitProgress Indeterminate Label=""Preparing your export"" />

<BitProgress Circular Indeterminate AriaLabel=""Preparing your export"" />";

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

<BitProgress Circular AriaLabel=""Playing"" Percent=""35"" Buffer=""62"" Thickness=""6"" ShowPercentNumber />

<BitSlider Label=""Progress"" @bind-Value=""bufferPercent"" Max=""100"" />
<BitProgress AriaLabel=""Playing"" Percent=""bufferPercent"" Buffer=""Math.Min(100, bufferPercent + 25)"" Thickness=""8"" Rounded />";
    private readonly string example9CsharpCode = @"
private double bufferPercent = 40;";

    private readonly string example10RazorCode = @"
<BitProgress Rounded AriaLabel=""Rounded bar"" Percent=""42"" Thickness=""10"" />

<BitProgress Rounded Indeterminate AriaLabel=""Rounded indeterminate bar"" Thickness=""10"" />

<BitProgress Circular Rounded AriaLabel=""Rounded ring"" Percent=""42"" Thickness=""8"" />
<BitProgress Circular Rounded Indeterminate AriaLabel=""Rounded spinner"" Thickness=""8"" />";

    private readonly string example11RazorCode = @"
<BitProgress Striped AriaLabel=""Striped bar"" Percent=""42"" Thickness=""12"" />

<BitProgress Striped StripedAnimation AriaLabel=""Striped bar with travelling stripes"" Percent=""42"" Thickness=""12"" />

<BitProgress Striped StripedAnimation Rounded AriaLabel=""Rounded striped bar"" Percent=""72"" Thickness=""12"" Color=""BitColor.Success"" />";

    private readonly string example12RazorCode = @"
<BitProgress Reversed AriaLabel=""Reversed bar"" Percent=""42"" Thickness=""10"" ShowPercentNumber />

<BitProgress Reversed Indeterminate AriaLabel=""Reversed indeterminate bar"" Thickness=""10"" />

<BitProgress Circular Reversed AriaLabel=""Reversed ring"" Percent=""42"" Thickness=""6"" />
<BitProgress Circular Reversed Indeterminate AriaLabel=""Reversed spinner"" Thickness=""6"" />";

    private readonly string example13RazorCode = @"
<BitSlider @bind-Value=""gaugeValue"" Max=""100"" />

<BitProgress Circular Rounded ShowPercentNumber AriaLabel=""Gauge with a 90 degree gap""
             GapDegree=""90""
             Diameter=""120""
             Thickness=""10""
             Percent=""gaugeValue"" />

<BitProgress Circular Rounded ShowPercentNumber AriaLabel=""Gauge with a 180 degree gap""
             GapDegree=""180""
             Diameter=""120""
             Thickness=""10""
             Color=""BitColor.Success""
             Percent=""gaugeValue"" />

<BitProgress Circular Rounded Meter AriaLabel=""Temperature""
             GapDegree=""120""
             Diameter=""120""
             Thickness=""14""
             Color=""BitColor.Warning""
             Min=""20""
             Max=""40""
             Value=""20 + gaugeValue / 5""
             Buffer=""38""
             AriaValueText=""@($""{20 + gaugeValue / 5:F0} degrees"")"" />

<BitProgress Circular Rounded AriaLabel=""Gauge with the gap at the top"" GapDegree=""120"" Diameter=""80"" Thickness=""8"" Percent=""gaugeValue""
             GapPosition=""BitProgressGapPosition.Top"" />
<BitProgress Circular Rounded AriaLabel=""Gauge with the gap at the start"" GapDegree=""120"" Diameter=""80"" Thickness=""8"" Percent=""gaugeValue""
             GapPosition=""BitProgressGapPosition.Start"" />
<BitProgress Circular Rounded AriaLabel=""Gauge with the gap at the end"" GapDegree=""120"" Diameter=""80"" Thickness=""8"" Percent=""gaugeValue""
             GapPosition=""BitProgressGapPosition.End"" />";
    private readonly string example13CsharpCode = @"
private double gaugeValue = 65;";

    private readonly string example14RazorCode = @"
<BitSlider @bind-Value=""meterValue"" Min=""0"" Max=""100"" />

<BitProgress Meter
             Label=""Disk usage""
             Description=""@($""{meterValue:F0} GB of 100 GB used"")""
             AriaValueText=""@($""{meterValue:F0} of 100 gigabytes used"")""
             Value=""meterValue""
             Max=""100""
             Thickness=""10""
             Rounded
             ShowPercentNumber
             Color=""@(meterValue > 90 ? BitColor.Error : meterValue > 70 ? BitColor.Warning : BitColor.Success)"" />

<BitProgress Meter Circular Rounded ShowPercentNumber
             GapDegree=""120""
             Diameter=""120""
             Thickness=""10""
             Label=""Signal""
             Percent=""meterValue"" />";
    private readonly string example14CsharpCode = @"
private double meterValue = 62;";

    private readonly string example15RazorCode = @"
<BitProgress Vertical AriaLabel=""Vertical bar"" Percent=""42"" Thickness=""12"" Rounded />
<BitProgress Vertical AriaLabel=""Vertical bar with a set length"" Percent=""42"" Thickness=""12"" Length=""6rem"" Color=""BitColor.Success"" />
<BitProgress Vertical Reversed AriaLabel=""Reversed vertical bar"" Percent=""42"" Thickness=""12"" Color=""BitColor.Warning"" />
<BitProgress Vertical AriaLabel=""Vertical bar with a buffer"" Percent=""42"" Buffer=""75"" Thickness=""16"" Rounded Color=""BitColor.Info"" />
<BitProgress Vertical Striped StripedAnimation AriaLabel=""Striped vertical bar"" Percent=""60"" Thickness=""16"" />
<BitProgress Vertical AriaLabel=""Segmented vertical bar"" Segments=""4"" Percent=""60"" Thickness=""16"" />
<BitProgress Vertical Indeterminate AriaLabel=""Vertical indeterminate bar"" Thickness=""12"" />";

    private readonly string example16RazorCode = @"
<BitSlider @bind-Value=""segmentedPercent"" Max=""100"" />

<BitProgress AriaLabel=""Five segments"" Segments=""5"" Percent=""segmentedPercent"" Thickness=""12"" ShowPercentNumber />

<BitProgress AriaLabel=""Ten segments"" Segments=""10"" SegmentGap=""2"" Percent=""segmentedPercent"" Thickness=""8"" Color=""BitColor.Success"" />

<BitProgress AriaLabel=""Four rounded segments"" Segments=""4"" SegmentGap=""8"" Rounded Percent=""segmentedPercent"" Thickness=""14"" Buffer=""100"" />";
    private readonly string example16CsharpCode = @"
private double segmentedPercent = 45;";

    private readonly string example17RazorCode = @"
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
    private readonly string example17CsharpCode = @"
private double announcedPercent = 0;";

    private readonly string example18RazorCode = @"
<BitProgress Label=""Uploading report.pdf""
             Description=""Uploading - 4.2 MB of 6.0 MB""
             Percent=""70""
             Thickness=""8""
             Rounded
             ShowPercentNumber />

<BitProgress Label=""Uploading report.pdf""
             Description=""Upload complete""
             Color=""BitColor.Success""
             Percent=""100""
             Thickness=""8""
             Rounded>
    <PercentNumberTemplate>
        <BitIcon IconName=""@BitIconName.CompletedSolid"" Color=""BitColor.Success"" />
    </PercentNumberTemplate>
</BitProgress>

<BitProgress Label=""Uploading report.pdf""
             Description=""Upload failed - the connection was lost""
             Color=""BitColor.Error""
             Percent=""70""
             Thickness=""8""
             Rounded>
    <PercentNumberTemplate>
        <BitIcon IconName=""@BitIconName.StatusCircleErrorX"" Color=""BitColor.Error"" />
    </PercentNumberTemplate>
</BitProgress>

<BitProgress Circular Rounded Thickness=""6"" Diameter=""64"" Color=""BitColor.Success"" Percent=""100""
             AriaLabel=""Upload complete"">
    <PercentNumberTemplate>
        <BitIcon IconName=""@BitIconName.CompletedSolid"" Color=""BitColor.Success"" />
    </PercentNumberTemplate>
</BitProgress>

<BitProgress Circular Rounded Thickness=""6"" Diameter=""64"" Color=""BitColor.Error"" Percent=""70""
             AriaLabel=""Upload failed"">
    <PercentNumberTemplate>
        <BitIcon IconName=""@BitIconName.StatusCircleErrorX"" Color=""BitColor.Error"" />
    </PercentNumberTemplate>
</BitProgress>";

    private readonly string example19RazorCode = @"
<BitProgress Label=""Brand"" BarColor=""#8b5cf6"" TrackColor=""#e9d5ff"" Percent=""62"" Thickness=""10"" Rounded ShowPercentNumber />

<BitProgress Label=""Buffered"" BarColor=""darkcyan"" TrackColor=""#e0f2f1"" Percent=""45"" Buffer=""78"" Thickness=""10"" Rounded />

<BitProgress Label=""Striped"" BarColor=""tomato"" Striped StripedAnimation Percent=""72"" Thickness=""14"" Rounded />

<BitProgress Label=""Sweeping"" BarColor=""#8b5cf6"" TrackColor=""#e9d5ff"" Indeterminate Thickness=""10"" Rounded />

<BitProgress Circular Rounded AriaLabel=""Brand ring"" ShowPercentNumber BarColor=""#8b5cf6"" TrackColor=""#e9d5ff"" Percent=""62"" Diameter=""80"" Thickness=""8"" />
<BitProgress Circular Rounded AriaLabel=""Sweeping ring"" BarColor=""tomato"" TrackColor=""#ffe0d6"" Indeterminate Diameter=""80"" Thickness=""8"" />";

    private readonly string example20RazorCode = @"
<BitProgress AriaLabel=""Primary"" Color=""BitColor.Primary"" Percent=""69"" />
<BitProgress AriaLabel=""Secondary"" Color=""BitColor.Secondary"" Percent=""69"" />
<BitProgress AriaLabel=""Tertiary"" Color=""BitColor.Tertiary"" Percent=""69"" />
<BitProgress AriaLabel=""Info"" Color=""BitColor.Info"" Percent=""69"" />
<BitProgress AriaLabel=""Success"" Color=""BitColor.Success"" Percent=""69"" />
<BitProgress AriaLabel=""Warning"" Color=""BitColor.Warning"" Percent=""69"" />
<BitProgress AriaLabel=""Severe warning"" Color=""BitColor.SevereWarning"" Percent=""69"" />
<BitProgress AriaLabel=""Error"" Color=""BitColor.Error"" Percent=""69"" />

<BitProgress AriaLabel=""Primary ring"" Color=""BitColor.Primary"" Circular Percent=""69"" />
<BitProgress AriaLabel=""Secondary ring"" Color=""BitColor.Secondary"" Circular Percent=""69"" />
<BitProgress AriaLabel=""Tertiary ring"" Color=""BitColor.Tertiary"" Circular Percent=""69"" />
<BitProgress AriaLabel=""Info ring"" Color=""BitColor.Info"" Circular Percent=""69"" />
<BitProgress AriaLabel=""Success ring"" Color=""BitColor.Success"" Circular Percent=""69"" />
<BitProgress AriaLabel=""Warning ring"" Color=""BitColor.Warning"" Circular Percent=""69"" />
<BitProgress AriaLabel=""Severe warning ring"" Color=""BitColor.SevereWarning"" Circular Percent=""69"" />
<BitProgress AriaLabel=""Error ring"" Color=""BitColor.Error"" Circular Percent=""69"" />";

    private readonly string example21RazorCode = @"
<BitProgress Size=""BitSize.Small"" Label=""Small"" Percent=""69"" ShowPercentNumber />
<BitProgress Size=""BitSize.Medium"" Label=""Medium"" Percent=""69"" ShowPercentNumber />
<BitProgress Size=""BitSize.Large"" Label=""Large"" Percent=""69"" ShowPercentNumber />

<BitProgress Size=""BitSize.Small"" AriaLabel=""Small ring"" Circular Percent=""69"" ShowPercentNumber />
<BitProgress Size=""BitSize.Medium"" AriaLabel=""Medium ring"" Circular Percent=""69"" ShowPercentNumber />
<BitProgress Size=""BitSize.Large"" AriaLabel=""Large ring"" Circular Percent=""69"" ShowPercentNumber />";

    private readonly string example22RazorCode = @"
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


<BitProgress Indeterminate AriaLabel=""Bar with a custom style"" Style=""background-color: #e687dc; border-radius: 0.5rem; padding: 0.2rem;"" Thickness=""10"" />

<BitProgress Class=""custom-class"" AriaLabel=""Bar with a custom class""
             Percent=""69""
             Thickness=""10"" />


<BitProgress Circular Indeterminate AriaLabel=""Ring with a custom style"" Style=""background-color: #e687dc; border-radius: 0.5rem; padding: 0.2rem;"" Thickness=""10"" />

<BitProgress Circular AriaLabel=""Ring with a custom class""
             Class=""custom-class""
             Percent=""69""
             Thickness=""10"" />


<BitProgress Indeterminate AriaLabel=""Bar with styled parts""
             Thickness=""10""
             Styles=""@(new() { Bar = ""background: linear-gradient(to right, green 0%, yellow 50%, green 100%);"",
                               Track = ""background-color: green;"" })"" />

<BitProgress Classes=""@(new() { Bar = ""custom-bar"",
                                Track = ""custom-track"",
                                Buffer = ""custom-buffer"" })"" AriaLabel=""Bar with classed parts""
             Percent=""45""
             Buffer=""80""
             Thickness=""10"" />


<BitProgress Circular Indeterminate AriaLabel=""Ring with styled parts""
             Thickness=""10""
             Styles=""@(new() { Bar = ""stroke: greenyellow;"",
                               Track = ""stroke: green;"" })"" />

<BitProgress Circular AriaLabel=""Ring with classed parts""
             Percent=""69""
             Thickness=""10""
             Classes=""@(new() { Bar = ""custom-circle-bar"",
                                Track = ""custom-circle-track"" })"" />";

    private readonly string example23RazorCode = @"
<BitProgress Dir=""BitDir.Rtl"" AriaLabel=""در حال بارگذاری""
             Thickness=""10""
             Indeterminate />

<BitProgress Label=""لیبل تست""
             Description=""توضیحات تست""
             Dir=""BitDir.Rtl""
             Percent=""69""
             Thickness=""10""
             ShowPercentNumber />

<BitProgress Circular AriaLabel=""در حال بارگذاری""
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
