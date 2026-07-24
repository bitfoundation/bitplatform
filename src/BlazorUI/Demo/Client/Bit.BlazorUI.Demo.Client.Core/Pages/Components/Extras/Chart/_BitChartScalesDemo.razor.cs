namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartScalesDemo
{
    private readonly BitChartOptions _minMax = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
        Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, Min = 0, Max = 100 } }
    };

    private readonly BitChartOptions _suggested = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
        Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, SuggestedMin = 0, SuggestedMax = 100 } }
    };

    private readonly BitChartOptions _step = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
        Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, BeginAtZero = true, Ticks = new BitChartTickOptions { StepSize = 25 } } }
    };

    private readonly BitChartOptions _log = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
        Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Logarithmic } }
    };

    private readonly BitChartOptions _reverse = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
        Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, Reverse = true, Min = 1, Max = 10 } }
    };

    private readonly BitChartOptions _grace = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
        Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, Grace = 0.1 } }
    };

    private BitChartData Series() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets = { new BitChartDataset { Label = "Value", Data = BitChartSampleData.V(35, 52, 48, 70, 60, 78, 66), BorderColor = "#36a2eb", BackgroundColor = "#36a2eb", Tension = 0.3 } }
    };

    private BitChartData Rank() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets = { new BitChartDataset { Label = "Rank", Data = BitChartSampleData.V(8, 6, 7, 3, 4, 2, 1), BorderColor = "#9966ff", BackgroundColor = "#9966ff", Tension = 0.3 } }
    };

    private BitChartData LogData() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets = { new BitChartDataset { Label = "Growth", Data = BitChartSampleData.V(1, 10, 100, 1000, 5000, 20000, 80000), BorderColor = "#ff6384", BackgroundColor = "#ff6384", Tension = 0.2 } }
    };


    private readonly string minMaxRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Series()"" Options=""_minMax"" />";
    private readonly string minMaxCsharpCode = @"
private readonly BitChartOptions _minMax = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
    Scales = { [""y""] = new BitChartScaleOptions { Id = ""y"", Type = BitChartScaleType.Linear, Min = 0, Max = 100 } }
};

private BitChartData Series() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets = { new BitChartDataset { Label = ""Value"", Data = new() { 35, 52, 48, 70, 60, 78, 66 },
        BorderColor = ""#36a2eb"", BackgroundColor = ""#36a2eb"", Tension = 0.3 } }
};";

    private readonly string suggestedRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Series()"" Options=""_suggested"" />";
    private readonly string suggestedCsharpCode = @"
// SuggestedMin/Max only expand the range; data outside still shows.
private readonly BitChartOptions _suggested = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
    Scales = { [""y""] = new BitChartScaleOptions { Id = ""y"", Type = BitChartScaleType.Linear, SuggestedMin = 0, SuggestedMax = 100 } }
};
// Series(): Jan..Jul = 35/52/48/70/60/78/66";

    private readonly string stepRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Series()"" Options=""_step"" />";
    private readonly string stepCsharpCode = @"
private readonly BitChartOptions _step = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
    Scales = { [""y""] = new BitChartScaleOptions { Id = ""y"", Type = BitChartScaleType.Linear, BeginAtZero = true,
        Ticks = new BitChartTickOptions { StepSize = 25 } } }
};
// Series(): Jan..Jul = 35/52/48/70/60/78/66";

    private readonly string logRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""LogData()"" Options=""_log"" />";
    private readonly string logCsharpCode = @"
private readonly BitChartOptions _log = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
    Scales = { [""y""] = new BitChartScaleOptions { Id = ""y"", Type = BitChartScaleType.Logarithmic } }
};

private BitChartData LogData() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets = { new BitChartDataset { Label = ""Growth"", Data = new() { 1, 10, 100, 1000, 5000, 20000, 80000 },
        BorderColor = ""#ff6384"", BackgroundColor = ""#ff6384"", Tension = 0.2 } }
};";

    private readonly string reverseRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Rank()"" Options=""_reverse"" />";
    private readonly string reverseCsharpCode = @"
private readonly BitChartOptions _reverse = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
    Scales = { [""y""] = new BitChartScaleOptions { Id = ""y"", Type = BitChartScaleType.Linear, Reverse = true, Min = 1, Max = 10 } }
};

private BitChartData Rank() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets = { new BitChartDataset { Label = ""Rank"", Data = new() { 8, 6, 7, 3, 4, 2, 1 },
        BorderColor = ""#9966ff"", BackgroundColor = ""#9966ff"", Tension = 0.3 } }
};";

    private readonly string graceRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Series()"" Options=""_grace"" />";
    private readonly string graceCsharpCode = @"
private readonly BitChartOptions _grace = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
    Scales = { [""y""] = new BitChartScaleOptions { Id = ""y"", Type = BitChartScaleType.Linear, Grace = 0.1 } }
};
// Series(): Jan..Jul = 35/52/48/70/60/78/66";
}
