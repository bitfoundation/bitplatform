namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartInteractionDemo
{
    private BitChartInteractionMode _mode = BitChartInteractionMode.Index;
    private bool _hasData = true;

    private readonly BitChartOptions _default = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
    };

    private readonly BitChartOptions _intersect = new()
    {
        Interaction = new BitChartInteractionOptions { Intersect = true },
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
    };

    private BitChartOptions ModeOptions() => new()
    {
        Interaction = new BitChartInteractionOptions { Mode = _mode },
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
    };

    private BitChartData Series() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Requests", Data = BitChartSampleData.V(120, 190, 160, 250, 220, 300, 280),
                BorderColor = "#36a2eb", PointRadius = 0, Tension = 0.3 },
            new BitChartDataset { Label = "Errors", Data = BitChartSampleData.V(12, 9, 20, 14, 18, 11, 8),
                BorderColor = "#ff6384", PointRadius = 0, Tension = 0.3 }
        }
    };

    private BitChartData Markers() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Requests", Data = BitChartSampleData.V(120, 190, 160, 250, 220, 300, 280),
                BorderColor = "#36a2eb", PointRadius = 5, Tension = 0.3 },
            new BitChartDataset { Label = "Errors", Data = BitChartSampleData.V(12, 9, 20, 14, 18, 11, 8),
                BorderColor = "#ff6384", PointRadius = 5, Tension = 0.3 }
        }
    };

    private void ToggleEmpty() => _hasData = !_hasData;

    private BitChartData EmptyDemoData() => _hasData ? BitChartSampleData.Revenue() : new BitChartData();


    private readonly string anywhereRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Series()"" Options=""_default"" />";
    private readonly string anywhereCsharpCode = @"
// Intersect is false by default, so no extra configuration is needed.
private readonly BitChartOptions _default = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

private BitChartData Series() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Requests"", Data = new() { 120, 190, 160, 250, 220, 300, 280 },
            BorderColor = ""#36a2eb"", PointRadius = 0, Tension = 0.3 },
        new BitChartDataset { Label = ""Errors"", Data = new() { 12, 9, 20, 14, 18, 11, 8 },
            BorderColor = ""#ff6384"", PointRadius = 0, Tension = 0.3 }
    }
};";

    private readonly string intersectRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Markers()"" Options=""_intersect"" />";
    private readonly string intersectCsharpCode = @"
private readonly BitChartOptions _intersect = new()
{
    Interaction = new BitChartInteractionOptions { Intersect = true },
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};";

    private readonly string modesRazorCode = @"<select @bind=""_mode"">
    <option value=""@BitChartInteractionMode.Nearest"">Nearest</option>
    <option value=""@BitChartInteractionMode.Index"">Index</option>
    <option value=""@BitChartInteractionMode.Dataset"">Dataset</option>
</select>

<BitChart Type=""BitChartType.Bar"" Data=""Revenue()"" Options=""ModeOptions()"" />";
    private readonly string modesCsharpCode = @"
private BitChartInteractionMode _mode = BitChartInteractionMode.Index;

private BitChartOptions ModeOptions() => new()
{
    Interaction = new BitChartInteractionOptions { Mode = _mode },
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};";

    private readonly string emptyRazorCode = @"<BitButton Variant=""BitVariant.Outline"" OnClick=""ToggleEmpty"">Clear the data</BitButton>

<BitChart Type=""BitChartType.Bar"" Data=""EmptyDemoData()"" NoDataText=""No results for this filter"" />";
    private readonly string emptyCsharpCode = @"
private bool _hasData = true;

private void ToggleEmpty() => _hasData = !_hasData;

private BitChartData EmptyDemoData() => _hasData ? Revenue() : new BitChartData();";

    private string? _linked;

    private readonly BitChartOptions _linkedOptions = new()
    {
        Interaction = new BitChartInteractionOptions { Mode = BitChartInteractionMode.Index },
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
    };

    /// <summary>Both charts report through here; a null context means the pointer has left.</summary>
    private void Link(BitChartTooltipContext? context)
        => _linked = context is null
            ? null
            : $"{context.Title}: " + string.Join(", ", context.Points.Select(p => $"{p.Label} {p.FormattedValue}"));

    private readonly string linkedRazorCode = @"<div>@(_linked ?? ""Hover either chart"")</div>

<BitChart Type=""BitChartType.Bar"" Data=""Revenue()"" Options=""_linkedOptions"" OnElementHover=""Link"" />
<BitChart Type=""BitChartType.Line"" Data=""MonthlySales()"" Options=""_linkedOptions"" OnElementHover=""Link"" />";
    private readonly string linkedCsharpCode = @"
private string? _linked;

private readonly BitChartOptions _linkedOptions = new()
{
    Interaction = new BitChartInteractionOptions { Mode = BitChartInteractionMode.Index },
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

// Both charts report through here; a null context means the pointer has left.
private void Link(BitChartTooltipContext? context)
    => _linked = context is null
        ? null
        : $""{context.Title}: "" + string.Join("", "", context.Points.Select(p => $""{p.Label} {p.FormattedValue}""));";
}
