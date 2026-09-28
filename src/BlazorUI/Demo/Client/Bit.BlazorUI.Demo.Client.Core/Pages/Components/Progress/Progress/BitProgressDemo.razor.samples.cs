namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Progress.Progress;

public partial class BitProgressDemo
{
    private readonly string example1RazorCode = @"
<BitProgress Label=""Uploading report.pdf"" Description=""4.2 MB of 10 MB"" Percent=""42"" />

<BitProgress Circular Label=""Uploading report.pdf"" Description=""4.2 MB of 10 MB"" Percent=""42"" />";

    private readonly string example2RazorCode = @"
<BitProgress Indeterminate Label=""Preparing your export"" />

<BitProgress Circular Indeterminate AriaLabel=""Loading"" />

<BitButton OnClick=""() => isLoading = !isLoading"">@(isLoading ? ""Stop"" : ""Start"") loading</BitButton>

@if (isLoading)
{
    <BitProgress Indeterminate Delay=""800"" Label=""Shown after 800 ms"" />
}";
    private readonly string example2CsharpCode = @"
private bool isLoading;";

    private readonly string example3RazorCode = @"
<BitSlider Label=""Thickness"" @bind-Value=""barThickness"" Min=""1"" Max=""30"" />

<BitProgress AriaLabel=""Thick bar"" Percent=""69"" Thickness=""(int)barThickness"" />

<div style=""display: flex; gap: 1rem; align-items: center; flex-wrap: wrap;"">
    <BitProgress Circular AriaLabel=""Thick ring"" Percent=""69"" Thickness=""(int)barThickness"" Diameter=""96"" />
    <BitProgress Circular AriaLabel=""24 pixel ring"" Percent=""69"" Diameter=""24"" />
    <BitProgress Circular AriaLabel=""48 pixel ring"" Percent=""69"" Diameter=""48"" />
    <BitProgress Circular Indeterminate AriaLabel=""72 pixel spinner"" Diameter=""72"" Thickness=""6"" />
</div>";
    private readonly string example3CsharpCode = @"
private double barThickness = 10;";

    private readonly string example4RazorCode = @"
<BitProgress Label=""End"" Percent=""85.69"" ShowPercentNumber />

<BitProgress Label=""Format"" Percent=""85.69"" PercentNumberFormat=""{0:F2} %"" ShowPercentNumber />

<BitProgress Label=""Template"" Percent=""85.69"">
    <PercentNumberTemplate Context=""percent"">
        <b>@($""{percent:F0}% done"")</b>
    </PercentNumberTemplate>
</BitProgress>

<BitProgress Label=""Start"" Percent=""42"" ShowPercentNumber PercentNumberPosition=""BitProgressPercentPosition.Start"" />

<BitProgress Label=""Center"" Percent=""42"" ShowPercentNumber PercentNumberPosition=""BitProgressPercentPosition.Center"" />

<BitProgress Label=""Top"" Percent=""42"" ShowPercentNumber PercentNumberPosition=""BitProgressPercentPosition.Top"" />

<BitProgress Label=""Inside"" Percent=""42"" Thickness=""20"" ShowPercentNumber PercentNumberPosition=""BitProgressPercentPosition.Inside"" />

<div style=""display: flex; gap: 1rem; align-items: center; flex-wrap: wrap;"">
    <BitProgress Circular AriaLabel=""Ring readout"" Percent=""85.69"" ShowPercentNumber />
    <BitProgress Circular AriaLabel=""Large ring readout"" Percent=""85.69"" ShowPercentNumber Diameter=""96"" Thickness=""8"" PercentNumberFormat=""{0:F1} %"" />
</div>";

    private readonly string example5RazorCode = @"
<BitProgress Label=""Uploading files""
             Description=""3 of 10 files""
             AriaValueText=""3 of 10 files""
             Value=""3""
             Max=""10"" />

<BitProgress Label=""Re-indexing rows""
             AriaValueText=""row 3,200 of 5,000""
             Min=""1000""
             Max=""5000""
             Value=""3200""
             ShowPercentNumber />

<BitProgress Circular Label=""Steps"" Value=""7"" Max=""12"" ShowPercentNumber />";

    private readonly string example6RazorCode = @"
<BitSlider Label=""Played"" @bind-Value=""bufferPercent"" Max=""100"" />

<BitProgress AriaLabel=""Playing"" Percent=""bufferPercent"" Buffer=""Math.Min(100, bufferPercent + 25)"" Thickness=""6"" />

<BitProgress Label=""Processing"" Value=""4"" Max=""10"" Buffer=""8"" ShowPercentNumber />

<BitProgress Circular AriaLabel=""Playing"" Percent=""bufferPercent"" Buffer=""Math.Min(100, bufferPercent + 25)"" Thickness=""6"" Diameter=""64"" />";
    private readonly string example6CsharpCode = @"
private double bufferPercent = 40;";

    private readonly string example7RazorCode = @"
<BitProgress Rounded AriaLabel=""Rounded bar"" Percent=""42"" Thickness=""10"" />

<BitProgress Rounded Indeterminate AriaLabel=""Rounded indeterminate bar"" Thickness=""10"" />

<BitProgress Striped AriaLabel=""Striped bar"" Percent=""42"" Thickness=""12"" />

<BitProgress Striped StripedAnimation Rounded AriaLabel=""Rounded bar with travelling stripes"" Percent=""72"" Thickness=""12"" />

<div style=""display: flex; gap: 1rem; align-items: center;"">
    <BitProgress Circular Rounded AriaLabel=""Rounded ring"" Percent=""42"" Thickness=""8"" Diameter=""48"" />
    <BitProgress Circular Rounded Indeterminate AriaLabel=""Rounded spinner"" Thickness=""8"" Diameter=""48"" />
</div>";

    private readonly string example8RazorCode = @"
<BitProgress Reversed AriaLabel=""Reversed bar"" Percent=""42"" Thickness=""10"" ShowPercentNumber />

<BitProgress Reversed Indeterminate AriaLabel=""Reversed indeterminate bar"" Thickness=""10"" />

<div style=""display: flex; gap: 1rem; align-items: center;"">
    <BitProgress Circular Reversed AriaLabel=""Reversed ring"" Percent=""42"" Thickness=""6"" Diameter=""48"" />
    <BitProgress Circular Reversed Indeterminate AriaLabel=""Reversed spinner"" Thickness=""6"" Diameter=""48"" />
</div>";

    private readonly string example9RazorCode = @"
<BitSlider Label=""Progress"" @bind-Value=""segmentedPercent"" Max=""100"" />

<BitProgress AriaLabel=""Five segments"" Segments=""5"" Percent=""segmentedPercent"" Thickness=""12"" ShowPercentNumber />

<BitProgress AriaLabel=""Ten segments"" Segments=""10"" SegmentGap=""2"" Percent=""segmentedPercent"" Thickness=""8"" />

<BitProgress AriaLabel=""Four rounded segments"" Segments=""4"" SegmentGap=""8"" Rounded Percent=""segmentedPercent"" Thickness=""14"" Buffer=""100"" />";
    private readonly string example9CsharpCode = @"
private double segmentedPercent = 45;";

    private readonly string example10RazorCode = @"
<div style=""display: flex; gap: 1rem; align-items: center; flex-wrap: wrap;"">
    <BitProgress Vertical AriaLabel=""Vertical bar"" Percent=""42"" Thickness=""12"" Rounded />
    <BitProgress Vertical AriaLabel=""Short vertical bar"" Percent=""42"" Thickness=""12"" Length=""6rem"" />
    <BitProgress Vertical Reversed AriaLabel=""Reversed vertical bar"" Percent=""42"" Thickness=""12"" />
    <BitProgress Vertical AriaLabel=""Vertical bar with a buffer"" Percent=""42"" Buffer=""75"" Thickness=""16"" Rounded />
    <BitProgress Vertical Striped StripedAnimation AriaLabel=""Striped vertical bar"" Percent=""60"" Thickness=""16"" />
    <BitProgress Vertical AriaLabel=""Segmented vertical bar"" Segments=""4"" Percent=""60"" Thickness=""16"" />
    <BitProgress Vertical Indeterminate AriaLabel=""Vertical indeterminate bar"" Thickness=""12"" />
</div>";

    private readonly string example11RazorCode = @"
<BitSlider Label=""Value"" @bind-Value=""gaugeValue"" Max=""100"" />

<div style=""display: flex; gap: 1rem; align-items: center; flex-wrap: wrap;"">
    <BitProgress Circular Rounded ShowPercentNumber AriaLabel=""Gauge with a 90 degree gap""
                 GapDegree=""90"" Diameter=""120"" Thickness=""10"" Percent=""gaugeValue"" />
    <BitProgress Circular Rounded ShowPercentNumber AriaLabel=""Gauge with a 180 degree gap""
                 GapDegree=""180"" Diameter=""120"" Thickness=""10"" Percent=""gaugeValue"" />
</div>

<div style=""display: flex; gap: 1rem; align-items: center; flex-wrap: wrap;"">
    <BitProgress Circular Rounded AriaLabel=""Gap at the top"" GapDegree=""120"" Diameter=""80"" Thickness=""8"" Percent=""gaugeValue""
                 GapPosition=""BitProgressGapPosition.Top"" />
    <BitProgress Circular Rounded AriaLabel=""Gap at the start"" GapDegree=""120"" Diameter=""80"" Thickness=""8"" Percent=""gaugeValue""
                 GapPosition=""BitProgressGapPosition.Start"" />
    <BitProgress Circular Rounded AriaLabel=""Gap at the end"" GapDegree=""120"" Diameter=""80"" Thickness=""8"" Percent=""gaugeValue""
                 GapPosition=""BitProgressGapPosition.End"" />
</div>";
    private readonly string example11CsharpCode = @"
private double gaugeValue = 65;";

    private readonly string example12RazorCode = @"
<BitSlider Label=""Used"" @bind-Value=""meterValue"" Max=""100"" />

<BitProgress Meter
             Label=""Disk usage""
             Description=""@($""{meterValue:F0} GB of 100 GB used"")""
             AriaValueText=""@($""{meterValue:F0} of 100 gigabytes used"")""
             Value=""meterValue""
             Thickness=""10""
             Rounded
             ShowPercentNumber />

<BitProgress Meter Circular Rounded
             Label=""Temperature""
             AriaValueText=""@($""{20 + meterValue / 5:F0} degrees"")""
             Min=""20""
             Max=""40""
             Value=""20 + meterValue / 5""
             GapDegree=""120""
             Diameter=""120""
             Thickness=""12"">
    <PercentNumberTemplate>@($""{20 + meterValue / 5:F0} °C"")</PercentNumberTemplate>
</BitProgress>";
    private readonly string example12CsharpCode = @"
private double meterValue = 62;";

    private readonly string example13RazorCode = @"
<BitSlider Label=""Progress"" @bind-Value=""announcedPercent"" Max=""100"" />

<BitProgress AnnounceProgress Label=""Importing rows"" Percent=""announcedPercent"" ShowPercentNumber />

<BitProgress AnnounceProgress AnnounceStep=""10"" Label=""Uploading"" Percent=""announcedPercent"" />";
    private readonly string example13CsharpCode = @"
private double announcedPercent = 20;";

    private readonly string example14RazorCode = @"
<BitProgress Label=""Brand"" BarColor=""#8b5cf6"" TrackColor=""#e9d5ff"" Percent=""62"" Thickness=""10"" Rounded ShowPercentNumber />

<BitProgress Label=""Buffered"" BarColor=""darkcyan"" TrackColor=""#e0f2f1"" Percent=""45"" Buffer=""78"" Thickness=""10"" Rounded />

<BitProgress Label=""Striped"" BarColor=""tomato"" Striped StripedAnimation Percent=""72"" Thickness=""14"" Rounded />

<BitProgress Label=""Sweeping"" BarColor=""#8b5cf6"" TrackColor=""#e9d5ff"" Indeterminate Thickness=""10"" Rounded />

<div style=""display: flex; gap: 1rem; align-items: center;"">
    <BitProgress Circular Rounded AriaLabel=""Brand ring"" ShowPercentNumber BarColor=""#8b5cf6"" TrackColor=""#e9d5ff"" Percent=""62"" Diameter=""80"" Thickness=""8"" />
    <BitProgress Circular Rounded AriaLabel=""Sweeping ring"" BarColor=""tomato"" TrackColor=""#ffe0d6"" Indeterminate Diameter=""80"" Thickness=""8"" />
</div>";

    private readonly string example15RazorCode = @"
<BitParams Parameters=""@progressParams"">
    <BitProgress Label=""CPU"" Percent=""34"" />
    <BitProgress Label=""Memory"" Percent=""71"" />
    <BitProgress Label=""Disk"" Percent=""92"" Rounded=""false"" />
</BitParams>

<BitProgress Label=""Outside the cascade"" Percent=""50"" />";
    private readonly string example15CsharpCode = @"
private readonly BitProgressParams[] progressParams =
[
    new()
    {
        Thickness = 8,
        Rounded = true,
        ShowPercentNumber = true,
        PercentNumberPosition = BitProgressPercentPosition.Top,
    }
];";

    private readonly string example16RazorCode = @"
<BitProgress AriaLabel=""Primary"" Color=""BitColor.Primary"" Percent=""69"" Thickness=""4"" />
<BitProgress AriaLabel=""Secondary"" Color=""BitColor.Secondary"" Percent=""69"" Thickness=""4"" />
<BitProgress AriaLabel=""Tertiary"" Color=""BitColor.Tertiary"" Percent=""69"" Thickness=""4"" />
<BitProgress AriaLabel=""Info"" Color=""BitColor.Info"" Percent=""69"" Thickness=""4"" />
<BitProgress AriaLabel=""Success"" Color=""BitColor.Success"" Percent=""69"" Thickness=""4"" />
<BitProgress AriaLabel=""Warning"" Color=""BitColor.Warning"" Percent=""69"" Thickness=""4"" />
<BitProgress AriaLabel=""Severe warning"" Color=""BitColor.SevereWarning"" Percent=""69"" Thickness=""4"" />
<BitProgress AriaLabel=""Error"" Color=""BitColor.Error"" Percent=""69"" Thickness=""4"" />

<BitProgress AriaLabel=""Primary ring"" Color=""BitColor.Primary"" Circular Percent=""69"" />
<BitProgress AriaLabel=""Secondary ring"" Color=""BitColor.Secondary"" Circular Percent=""69"" />
<BitProgress AriaLabel=""Tertiary ring"" Color=""BitColor.Tertiary"" Circular Percent=""69"" />
<BitProgress AriaLabel=""Info ring"" Color=""BitColor.Info"" Circular Percent=""69"" />
<BitProgress AriaLabel=""Success ring"" Color=""BitColor.Success"" Circular Percent=""69"" />
<BitProgress AriaLabel=""Warning ring"" Color=""BitColor.Warning"" Circular Percent=""69"" />
<BitProgress AriaLabel=""Severe warning ring"" Color=""BitColor.SevereWarning"" Circular Percent=""69"" />
<BitProgress AriaLabel=""Error ring"" Color=""BitColor.Error"" Circular Percent=""69"" />
<BitProgress AriaLabel=""Loading"" Color=""BitColor.Success"" Circular Indeterminate />


<BitProgress Label=""Uploading report.pdf"" Description=""Upload complete"" Color=""BitColor.Success"" Percent=""100"" Thickness=""8"" Rounded>
    <PercentNumberTemplate>
        <BitIcon IconName=""@BitIconName.CompletedSolid"" Color=""BitColor.Success"" />
    </PercentNumberTemplate>
</BitProgress>

<BitProgress Label=""Uploading report.pdf"" Description=""Upload failed - the connection was lost"" Color=""BitColor.Error"" Percent=""70"" Thickness=""8"" Rounded>
    <PercentNumberTemplate>
        <BitIcon IconName=""@BitIconName.StatusCircleErrorX"" Color=""BitColor.Error"" />
    </PercentNumberTemplate>
</BitProgress>

<BitProgress Circular Rounded Thickness=""6"" Diameter=""64"" Color=""BitColor.Success"" Percent=""100"" AriaLabel=""Upload complete"">
    <PercentNumberTemplate>
        <BitIcon IconName=""@BitIconName.CompletedSolid"" Color=""BitColor.Success"" />
    </PercentNumberTemplate>
</BitProgress>

<BitProgress Circular Rounded Thickness=""6"" Diameter=""64"" Color=""BitColor.Error"" Percent=""70"" AriaLabel=""Upload failed"">
    <PercentNumberTemplate>
        <BitIcon IconName=""@BitIconName.StatusCircleErrorX"" Color=""BitColor.Error"" />
    </PercentNumberTemplate>
</BitProgress>";

    private readonly string example17RazorCode = @"
<BitProgress Size=""BitSize.Small"" Label=""Small"" Percent=""69"" ShowPercentNumber />

<BitProgress Size=""BitSize.Medium"" Label=""Medium"" Percent=""69"" ShowPercentNumber />

<BitProgress Size=""BitSize.Large"" Label=""Large"" Percent=""69"" ShowPercentNumber />

<BitProgress Size=""BitSize.Small"" AriaLabel=""Small ring"" Circular Percent=""69"" />
<BitProgress Size=""BitSize.Medium"" AriaLabel=""Medium ring"" Circular Percent=""69"" />
<BitProgress Size=""BitSize.Large"" AriaLabel=""Large ring"" Circular Percent=""69"" ShowPercentNumber />
<BitProgress Size=""BitSize.Small"" AriaLabel=""Small spinner"" Circular Indeterminate />
<BitProgress Size=""BitSize.Medium"" AriaLabel=""Medium spinner"" Circular Indeterminate />
<BitProgress Size=""BitSize.Large"" AriaLabel=""Large spinner"" Circular Indeterminate />";

    private readonly string example18RazorCode = @"
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

<BitProgress Class=""custom-class"" AriaLabel=""Bar with a custom class"" Percent=""69"" Thickness=""10"" />


<BitProgress Indeterminate AriaLabel=""Bar with styled parts""
             Thickness=""10""
             Styles=""@(new() { Bar = ""background: linear-gradient(to right, green 0%, yellow 50%, green 100%);"",
                               Track = ""background-color: green;"" })"" />

<BitProgress AriaLabel=""Bar with classed parts""
             Percent=""45""
             Buffer=""80""
             Thickness=""10""
             Classes=""@(new() { Bar = ""custom-bar"",
                                Track = ""custom-track"",
                                Buffer = ""custom-buffer"" })"" />

<BitProgress Circular Indeterminate AriaLabel=""Ring with styled parts""
             Thickness=""6""
             Diameter=""48""
             Styles=""@(new() { Bar = ""stroke: greenyellow;"", Track = ""stroke: green;"" })"" />

<BitProgress Circular AriaLabel=""Ring with classed parts""
             Percent=""69""
             Thickness=""6""
             Diameter=""48""
             Classes=""@(new() { Bar = ""custom-circle-bar"", Track = ""custom-circle-track"" })"" />


<BitProgress Label=""Softer corners, own palette"" Percent=""58"" ShowPercentNumber
             Style=""--bit-Progress-thickness: 12px; --bit-Progress-radius: 4px; --bit-Progress-bar-color: var(--bit-clr-suc); --bit-Progress-track-color: var(--bit-clr-bg-ter);"" />

<BitProgress Label=""Wide, bright stripes"" Percent=""72"" Striped StripedAnimation
             Style=""--bit-Progress-thickness: 16px; --bit-Progress-stripe-size: 2rem; --bit-Progress-stripe-color: rgba(255, 255, 255, 0.45);"" />

<BitProgress Circular AriaLabel=""Ring with a bold readout"" Percent=""42"" ShowPercentNumber
             Style=""--bit-Progress-diameter: 88px; --bit-Progress-thickness: 8px; --bit-Progress-percent-font-size: 1.5rem; --bit-Progress-percent-color: var(--bit-clr-pri);"" />

<div style=""--bit-Progress-thickness: 6px; --bit-Progress-radius: 999px; --bit-Progress-label-font-weight: 600; --bit-Progress-description-color: var(--bit-clr-fg-pri);"">
    <BitProgress Label=""Documents"" Description=""2.1 GB"" Percent=""21"" />
    <BitProgress Label=""Photos"" Description=""5.4 GB"" Percent=""54"" />
    <BitProgress Label=""Videos"" Description=""8.8 GB"" Percent=""88"" />
</div>";

    private readonly string example19RazorCode = @"
<BitProgress Dir=""BitDir.Rtl"" AriaLabel=""در حال بارگذاری"" Thickness=""10"" Indeterminate />

<BitProgress Dir=""BitDir.Rtl"" Label=""در حال بارگذاری"" Description=""۴.۲ مگابایت از ۱۰ مگابایت"" Percent=""69"" Thickness=""10"" ShowPercentNumber />

<div dir=""rtl"" style=""display: flex; gap: 1rem; align-items: center;"">
    <BitProgress Dir=""BitDir.Rtl"" Circular AriaLabel=""در حال بارگذاری"" Thickness=""6"" Diameter=""48"" Indeterminate />
    <BitProgress Dir=""BitDir.Rtl"" Circular AriaLabel=""در حال بارگذاری"" Thickness=""6"" Diameter=""64"" Percent=""69"" ShowPercentNumber />
</div>";
}
