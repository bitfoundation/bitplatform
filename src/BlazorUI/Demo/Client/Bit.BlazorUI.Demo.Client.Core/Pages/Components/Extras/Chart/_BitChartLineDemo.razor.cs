namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartLineDemo
{
    private readonly BitChartOptions _legendBottom = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
    };

    private readonly BitChartOptions _logOptions = new()
    {
        Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Logarithmic } }
    };

    private BitChartData Filled() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Visitors", Data = BitChartSampleData.V(120, 190, 160, 250, 220, 300, 280),
                BorderColor = "#36a2eb", Tension = 0.4, Fill = BitChartFillMode.Origin }
        }
    };

    private BitChartData Stepped() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Plan", Data = BitChartSampleData.V(10, 10, 25, 25, 40, 40, 55),
                BorderColor = "#4bc0c0", Stepped = BitChartSteppedLine.Before }
        }
    };

    private BitChartData Dashed() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Forecast", Data = BitChartSampleData.V(30, 42, null, 55, 48, 67, 70),
                BorderColor = "#9966ff", BorderDash = new() { 6, 4 }, PointStyle = BitChartPointStyle.Star,
                PointRadius = 6, SpanGaps = true }
        }
    };

    private BitChartData Log() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Growth", Data = BitChartSampleData.V(1, 10, 100, 1000, 5000, 20000, 80000),
                BorderColor = "#ff6384", Tension = 0.2 }
        }
    };

    private BitChartData Segmented() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Flow",
                Data = BitChartSampleData.V(40, 55, 48, 70, 62, 80, 72),
                BorderColor = "#36a2eb",
                BorderWidth = 3,
                Segment = new BitChartLineSegmentStyle
                {
                    BorderColor = ctx => ctx.EndValue < ctx.StartValue ? "#ff6384" : "#2ecc71",
                    BorderDash = ctx => ctx.StartIndex >= 4 ? new double[] { 6, 4 } : null
                }
            }
        }
    };

    private BitChartData Monotone() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Cardinal (tension)", Data = BitChartSampleData.V(10, 12, 60, 62, 30, 32, 70),
                BorderColor = "#c9cbcf", Tension = 0.5, PointRadius = 3, PointBackgroundColor = "#c9cbcf" },
            new BitChartDataset { Label = "Monotone", Data = BitChartSampleData.V(10, 12, 60, 62, 30, 32, 70),
                BorderColor = "#36a2eb", CubicInterpolationMode = BitChartCubicInterpolationMode.Monotone,
                PointRadius = 3, PointBackgroundColor = "#36a2eb" }
        }
    };


    private readonly string filledRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Filled()"" Options=""_legendBottom"" />";
    private readonly string filledCsharpCode = @"
private readonly BitChartOptions _legendBottom = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

private BitChartData Filled() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Visitors"", Data = new() { 120, 190, 160, 250, 220, 300, 280 },
            BorderColor = ""#36a2eb"", Tension = 0.4, Fill = BitChartFillMode.Origin }
    }
};";

    private readonly string straightRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""MonthlySales()"" Options=""_legendBottom"" />";
    private readonly string straightCsharpCode = @"
private readonly BitChartOptions _legendBottom = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

private BitChartData MonthlySales() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""2025"", Data = new() { 65, 59, 80, 81, 56, 55, 72 },
            BorderColor = ""#36a2eb"", BackgroundColor = ""#36a2eb"", Tension = 0.4, Fill = BitChartFillMode.None },
        new BitChartDataset { Label = ""2026"", Data = new() { 28, 48, 40, 60, 86, 92, 78 },
            BorderColor = ""#ff6384"", BackgroundColor = ""#ff6384"", Tension = 0.4, Fill = BitChartFillMode.None }
    }
};";

    private readonly string steppedRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Stepped()"" Options=""_legendBottom"" />";
    private readonly string steppedCsharpCode = @"
private readonly BitChartOptions _legendBottom = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

private BitChartData Stepped() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Plan"", Data = new() { 10, 10, 25, 25, 40, 40, 55 },
            BorderColor = ""#4bc0c0"", Stepped = BitChartSteppedLine.Before }
    }
};";

    private readonly string dashedRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Dashed()"" Options=""_legendBottom"" />";
    private readonly string dashedCsharpCode = @"
private readonly BitChartOptions _legendBottom = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

private BitChartData Dashed() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Forecast"", Data = new() { 30, 42, null, 55, 48, 67, 70 },
            BorderColor = ""#9966ff"", BorderDash = new() { 6, 4 }, PointStyle = BitChartPointStyle.Star,
            PointRadius = 6, SpanGaps = true }
    }
};";

    private readonly string logRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Log()"" Options=""_logOptions"" />";
    private readonly string logCsharpCode = @"
private readonly BitChartOptions _logOptions = new()
{
    Scales = { [""y""] = new BitChartScaleOptions { Id = ""y"", Type = BitChartScaleType.Logarithmic } }
};

private BitChartData Log() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Growth"", Data = new() { 1, 10, 100, 1000, 5000, 20000, 80000 },
            BorderColor = ""#ff6384"", Tension = 0.2 }
    }
};";

    private readonly string segmentedRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Segmented()"" Options=""_legendBottom"" />";
    private readonly string segmentedCsharpCode = @"
private readonly BitChartOptions _legendBottom = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

private BitChartData Segmented() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Flow"",
            Data = new() { 40, 55, 48, 70, 62, 80, 72 },
            BorderColor = ""#36a2eb"",
            BorderWidth = 3,
            Segment = new BitChartLineSegmentStyle
            {
                BorderColor = ctx => ctx.EndValue < ctx.StartValue ? ""#ff6384"" : ""#2ecc71"",
                BorderDash = ctx => ctx.StartIndex >= 4 ? new double[] { 6, 4 } : null
            }
        }
    }
};";

    private readonly string monotoneRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Monotone()"" Options=""_legendBottom"" />";
    private readonly string monotoneCsharpCode = @"
private readonly BitChartOptions _legendBottom = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

private BitChartData Monotone() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Cardinal (tension)"", Data = new() { 10, 12, 60, 62, 30, 32, 70 },
            BorderColor = ""#c9cbcf"", Tension = 0.5, PointRadius = 3, PointBackgroundColor = ""#c9cbcf"" },
        new BitChartDataset { Label = ""Monotone"", Data = new() { 10, 12, 60, 62, 30, 32, 70 },
            BorderColor = ""#36a2eb"", CubicInterpolationMode = BitChartCubicInterpolationMode.Monotone,
            PointRadius = 3, PointBackgroundColor = ""#36a2eb"" }
    }
};";
}
