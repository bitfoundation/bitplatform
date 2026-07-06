namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartScriptableDemo
{
    private readonly BitChartOptions _noLegend = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
    };

    private BitChartData BySign() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Net change",
                Data = BitChartSampleData.V(12, -8, 22, -4, 18, -15, 9),
                BorderRadius = 4,
                BackgroundColorFn = ctx => (ctx.Value ?? 0) < 0 ? "#ff6384" : "#4bc0c0"
            }
        }
    };

    private BitChartData ByThreshold() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Score",
                Data = BitChartSampleData.V(60, 72, 80, 68, 91, 55, 84),
                BorderColor = "#36a2eb",
                Tension = 0.3,
                PointRadius = 5,
                PointBackgroundColorFn = ctx => (ctx.Value ?? 0) >= 75 ? "#ff9f40" : "#36a2eb",
                PointRadiusFn = ctx => (ctx.Value ?? 0) >= 75 ? 7 : 4
            }
        }
    };

    private BitChartData RadiusByValue() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Volume",
                Data = BitChartSampleData.V(10, 35, 22, 60, 48, 80, 30),
                BorderColor = "#9966ff",
                Tension = 0.4,
                PointBackgroundColor = "#9966ff",
                PointRadiusFn = ctx => 3 + (ctx.Value ?? 0) / 12
            }
        }
    };

    private BitChartData StyleByParity() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Signal",
                Data = BitChartSampleData.V(40, 55, 48, 70, 62, 80, 72),
                BorderColor = "#2ecc71",
                Tension = 0.3,
                PointRadius = 5,
                PointBackgroundColor = "#2ecc71",
                PointStyleFn = ctx => ctx.DataIndex % 2 == 0 ? BitChartPointStyle.RectRot : BitChartPointStyle.Triangle
            }
        }
    };


    private readonly string bySignRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""BySign()"" Options=""_noLegend"" />";
    private readonly string bySignCsharpCode = @"
private readonly BitChartOptions _noLegend = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
};

private BitChartData BySign() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Net change"",
            Data = new() { 12, -8, 22, -4, 18, -15, 9 },
            BorderRadius = 4,
            // Scriptable: receives a context per element
            BackgroundColorFn = ctx => (ctx.Value ?? 0) < 0 ? ""#ff6384"" : ""#4bc0c0""
        }
    }
};";

    private readonly string byThresholdRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""ByThreshold()"" Options=""_noLegend"" />";
    private readonly string byThresholdCsharpCode = @"
private readonly BitChartOptions _noLegend = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
};

private BitChartData ByThreshold() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Score"",
            Data = new() { 60, 72, 80, 68, 91, 55, 84 },
            BorderColor = ""#36a2eb"",
            Tension = 0.3,
            PointRadius = 5,
            PointBackgroundColorFn = ctx => (ctx.Value ?? 0) >= 75 ? ""#ff9f40"" : ""#36a2eb"",
            PointRadiusFn = ctx => (ctx.Value ?? 0) >= 75 ? 7 : 4
        }
    }
};";

    private readonly string radiusByValueRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""RadiusByValue()"" Options=""_noLegend"" />";
    private readonly string radiusByValueCsharpCode = @"
private readonly BitChartOptions _noLegend = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
};

private BitChartData RadiusByValue() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Volume"",
            Data = new() { 10, 35, 22, 60, 48, 80, 30 },
            BorderColor = ""#9966ff"",
            Tension = 0.4,
            PointBackgroundColor = ""#9966ff"",
            PointRadiusFn = ctx => 3 + (ctx.Value ?? 0) / 12
        }
    }
};";

    private readonly string styleByParityRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""StyleByParity()"" Options=""_noLegend"" />";
    private readonly string styleByParityCsharpCode = @"
private readonly BitChartOptions _noLegend = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
};

private BitChartData StyleByParity() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Signal"",
            Data = new() { 40, 55, 48, 70, 62, 80, 72 },
            BorderColor = ""#2ecc71"",
            Tension = 0.3,
            PointRadius = 5,
            PointBackgroundColor = ""#2ecc71"",
            PointStyleFn = ctx => ctx.DataIndex % 2 == 0 ? BitChartPointStyle.RectRot : BitChartPointStyle.Triangle
        }
    }
};";
}
