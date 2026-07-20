namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartMixedDemo
{
    private readonly BitChartOptions _bottom = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
    };

    private readonly BitChartOptions _dual = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
        Scales =
        {
            ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, Position = BitChartPosition.Left, BeginAtZero = true,
                Title = new BitChartScaleTitleOptions { Display = true, Text = "Revenue ($k)" } },
            ["y2"] = new BitChartScaleOptions { Id = "y2", Type = BitChartScaleType.Linear, Position = BitChartPosition.Right, BeginAtZero = true,
                Title = new BitChartScaleTitleOptions { Display = true, Text = "Margin %" },
                Grid = new BitChartGridOptions { DrawOnChartArea = false } }
        }
    };

    // Each dataset sets its own Type to mix bars and lines.
    private BitChartData BarLine() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Type = BitChartType.Bar, Label = "Units", Data = BitChartSampleData.V(40, 55, 48, 70, 62, 80, 75), BackgroundColor = "#36a2eb" },
            new BitChartDataset { Type = BitChartType.Line, Label = "Trend", Data = BitChartSampleData.V(38, 50, 52, 64, 68, 76, 80), BorderColor = "#ff6384", Tension = 0.4 }
        }
    };

    private BitChartData DualAxis() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Type = BitChartType.Bar, Label = "Revenue", YAxisID = "y", Data = BitChartSampleData.V(120, 150, 130, 180, 165, 210, 195), BackgroundColor = "#36a2eb" },
            new BitChartDataset { Type = BitChartType.Line, Label = "Margin", YAxisID = "y2", Data = BitChartSampleData.V(18, 22, 20, 26, 24, 30, 28), BorderColor = "#ff9f40", Tension = 0.3 }
        }
    };


    private readonly string barLineRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""BarLine()"" Options=""_bottom"" />";
    private readonly string barLineCsharpCode = @"
private readonly BitChartOptions _bottom = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

// Each dataset sets its own Type to mix bars and lines.
private BitChartData BarLine() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Type = BitChartType.Bar,  Label = ""Units"", Data = new() { 40, 55, 48, 70, 62, 80, 75 }, BackgroundColor = ""#36a2eb"" },
        new BitChartDataset { Type = BitChartType.Line, Label = ""Trend"", Data = new() { 38, 50, 52, 64, 68, 76, 80 }, BorderColor = ""#ff6384"", Tension = 0.4 }
    }
};";

    private readonly string dualAxisRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""DualAxis()"" Options=""_dual"" />";
    private readonly string dualAxisCsharpCode = @"
private readonly BitChartOptions _dual = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
    Scales =
    {
        [""y""] = new BitChartScaleOptions { Id = ""y"", Type = BitChartScaleType.Linear, Position = BitChartPosition.Left, BeginAtZero = true,
            Title = new BitChartScaleTitleOptions { Display = true, Text = ""Revenue ($k)"" } },
        [""y2""] = new BitChartScaleOptions { Id = ""y2"", Type = BitChartScaleType.Linear, Position = BitChartPosition.Right, BeginAtZero = true,
            Title = new BitChartScaleTitleOptions { Display = true, Text = ""Margin %"" },
            Grid = new BitChartGridOptions { DrawOnChartArea = false } }
    }
};

private BitChartData DualAxis() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Type = BitChartType.Bar,  Label = ""Revenue"", YAxisID = ""y"",  Data = new() { 120, 150, 130, 180, 165, 210, 195 }, BackgroundColor = ""#36a2eb"" },
        new BitChartDataset { Type = BitChartType.Line, Label = ""Margin"",  YAxisID = ""y2"", Data = new() { 18, 22, 20, 26, 24, 30, 28 }, BorderColor = ""#ff9f40"", Tension = 0.3 }
    }
};";
}
