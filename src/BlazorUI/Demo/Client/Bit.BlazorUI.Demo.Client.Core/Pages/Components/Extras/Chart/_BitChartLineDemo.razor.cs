namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartLineDemo
{
    private readonly BitChartOptions _legendBottom = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
    };

    /// <summary>
    /// A sparkline drops the chrome, and turning off MaintainAspectRatio lets it take the tile's own
    /// height instead of a width-derived one.
    /// </summary>
    private readonly BitChartOptions _sparkline = new()
    {
        Sparkline = true,
        MaintainAspectRatio = false,
        Layout = new BitChartLayoutOptions { Padding = 2 }
    };

    private sealed record SparklineTile(string Caption, string Value, BitChartType Type, BitChartData Data);

    private readonly List<SparklineTile> _tiles =
    [
        new("Sessions", "12,480", BitChartType.Line, Spark("#36a2eb", true, 30, 34, 31, 40, 44, 41, 52, 58, 55, 64)),
        new("Signups", "934", BitChartType.Bar, Spark("#4bc0c0", false, 12, 18, 15, 22, 19, 26, 24, 31, 28, 35)),
        new("Errors", "17", BitChartType.Line, Spark("#ff6384", true, 22, 19, 24, 16, 14, 18, 11, 9, 12, 7))
    ];

    private static BitChartData Spark(string color, bool line, params double?[] values) => new()
    {
        // A category axis spans the labels, so a sparkline still needs one per value - blank, since the
        // tile shows no axis - or every point lands on the same x.
        Labels = [.. values.Select(_ => "")],
        Datasets =
        {
            new BitChartDataset
            {
                Data = [.. values],
                BorderColor = color,
                BackgroundColor = color,
                PointRadius = 0,
                BorderWidth = 2,
                Tension = 0.35,
                Fill = line ? BitChartFillMode.Origin : BitChartFillMode.None,
                FillColor = line ? BitChartColorUtil.WithAlpha(color, 0.18) : null,
                BorderRadius = 2
            }
        }
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

    private readonly string sparklineRazorCode = @"@foreach (var tile in _tiles)
{
    <div class=""sparkline-tile"">
        <div class=""sparkline-caption"">@tile.Caption</div>
        <div class=""sparkline-value"">@tile.Value</div>
        <BitChart Type=""tile.Type"" Data=""tile.Data"" Options=""_sparkline"" Height=""48px"" />
    </div>
}";
    private readonly string sparklineCsharpCode = @"
// Sparkline hides the axes, grid, legend and title; turning off MaintainAspectRatio lets the
// chart take the tile's own height instead of one derived from its width.
private readonly BitChartOptions _sparkline = new()
{
    Sparkline = true,
    MaintainAspectRatio = false,
    Layout = new BitChartLayoutOptions { Padding = 2 }
};

private sealed record SparklineTile(string Caption, string Value, BitChartType Type, BitChartData Data);

private readonly List<SparklineTile> _tiles =
[
    new(""Sessions"", ""12,480"", BitChartType.Line, Spark(""#36a2eb"", true, 30, 34, 31, 40, 44, 41, 52, 58, 55, 64)),
    new(""Signups"", ""934"", BitChartType.Bar, Spark(""#4bc0c0"", false, 12, 18, 15, 22, 19, 26, 24, 31, 28, 35)),
    new(""Errors"", ""17"", BitChartType.Line, Spark(""#ff6384"", true, 22, 19, 24, 16, 14, 18, 11, 9, 12, 7))
];

private static BitChartData Spark(string color, bool line, params double?[] values) => new()
{
    // A category axis spans the labels, so a sparkline still needs one per value - blank, since the
    // tile shows no axis - or every point lands on the same x.
    Labels = [.. values.Select(_ => """")],
    Datasets =
    {
        new BitChartDataset
        {
            Data = [.. values],
            BorderColor = color,
            BackgroundColor = color,
            PointRadius = 0,
            BorderWidth = 2,
            Tension = 0.35,
            Fill = line ? BitChartFillMode.Origin : BitChartFillMode.None,
            FillColor = line ? BitChartColorUtil.WithAlpha(color, 0.18) : null,
            BorderRadius = 2
        }
    }
};";
}
