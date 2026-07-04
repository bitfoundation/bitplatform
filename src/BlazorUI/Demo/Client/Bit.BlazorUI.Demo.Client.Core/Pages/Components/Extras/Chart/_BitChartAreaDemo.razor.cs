namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartAreaDemo
{
    private readonly BitChartOptions _noLegend = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
    };

    private readonly BitChartOptions _legendBottom = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
    };

    private readonly BitChartOptions _stacked = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
        Scales =
        {
            ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Category },
            ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, Stacked = true, BeginAtZero = true }
        }
    };

    private BitChartData Gradient() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Sessions",
                Data = BitChartSampleData.V(120, 190, 160, 250, 220, 300, 280),
                BorderColor = "#36a2eb",
                Tension = 0.4,
                Fill = BitChartFillMode.Origin,
                FillGradient = BitChartLinearGradient.Vertical2("rgba(54,162,235,0.55)", "rgba(54,162,235,0.02)")
            }
        }
    };

    private BitChartData Stacked() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Organic", Data = BitChartSampleData.V(30, 40, 35, 50, 45, 60, 55), BorderColor = "#36a2eb", Fill = BitChartFillMode.Stack },
            new BitChartDataset { Label = "Paid", Data = BitChartSampleData.V(20, 25, 30, 28, 35, 30, 40), BorderColor = "#ff6384", Fill = BitChartFillMode.Stack },
            new BitChartDataset { Label = "Referral", Data = BitChartSampleData.V(10, 12, 15, 18, 14, 20, 22), BorderColor = "#4bc0c0", Fill = BitChartFillMode.Stack }
        }
    };

    private BitChartData Range() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Max", Data = BitChartSampleData.V(28, 32, 30, 36, 40, 44, 42), BorderColor = "#ff9f40", Fill = BitChartFillMode.Dataset, FillTargetIndex = 1, FillColor = "rgba(255,159,64,0.18)" },
            new BitChartDataset { Label = "Min", Data = BitChartSampleData.V(14, 16, 15, 20, 24, 26, 23), BorderColor = "#36a2eb", Fill = BitChartFillMode.None }
        }
    };

    private BitChartData Boundaries() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Origin", Data = BitChartSampleData.V(10, -20, 30, -10, 25, -5, 15), BorderColor = "#36a2eb", Fill = BitChartFillMode.Origin, FillColor = "rgba(54,162,235,0.15)", Tension = 0.3 },
            new BitChartDataset { Label = "Start (top)", Data = BitChartSampleData.V(40, 35, 45, 38, 50, 44, 55), BorderColor = "#ff6384", Fill = BitChartFillMode.Start, FillColor = "rgba(255,99,132,0.12)", Tension = 0.3 }
        }
    };

    private BitChartData RadialFill() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Signal",
                Data = BitChartSampleData.V(120, 190, 160, 250, 220, 300, 280),
                BorderColor = "#9966ff",
                Tension = 0.4,
                Fill = BitChartFillMode.Origin,
                FillGradient = BitChartRadialGradient.Center2("rgba(153,102,255,0.55)", "rgba(153,102,255,0.03)")
            }
        }
    };


    private readonly string gradientRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Gradient()"" Options=""_noLegend"" />";
    private readonly string gradientCsharpCode = @"
private readonly BitChartOptions _noLegend = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
};

private BitChartData Gradient() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Sessions"",
            Data = new() { 120, 190, 160, 250, 220, 300, 280 },
            BorderColor = ""#36a2eb"",
            Tension = 0.4,
            Fill = BitChartFillMode.Origin,
            FillGradient = BitChartLinearGradient.Vertical2(""rgba(54,162,235,0.55)"", ""rgba(54,162,235,0.02)"")
        }
    }
};";

    private readonly string stackedRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Stacked()"" Options=""_stacked"" />";
    private readonly string stackedCsharpCode = @"
private readonly BitChartOptions _stacked = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
    Scales =
    {
        [""x""] = new BitChartScaleOptions { Id = ""x"", Type = BitChartScaleType.Category },
        [""y""] = new BitChartScaleOptions { Id = ""y"", Type = BitChartScaleType.Linear, Stacked = true, BeginAtZero = true }
    }
};

private BitChartData Stacked() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Organic"",  Data = new() { 30, 40, 35, 50, 45, 60, 55 }, BorderColor = ""#36a2eb"", Fill = BitChartFillMode.Stack },
        new BitChartDataset { Label = ""Paid"",     Data = new() { 20, 25, 30, 28, 35, 30, 40 }, BorderColor = ""#ff6384"", Fill = BitChartFillMode.Stack },
        new BitChartDataset { Label = ""Referral"", Data = new() { 10, 12, 15, 18, 14, 20, 22 }, BorderColor = ""#4bc0c0"", Fill = BitChartFillMode.Stack }
    }
};";

    private readonly string rangeRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Range()"" Options=""_legendBottom"" />";
    private readonly string rangeCsharpCode = @"
private readonly BitChartOptions _legendBottom = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

private BitChartData Range() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Max"", Data = new() { 28, 32, 30, 36, 40, 44, 42 }, BorderColor = ""#ff9f40"",
            Fill = BitChartFillMode.Dataset, FillTargetIndex = 1, FillColor = ""rgba(255,159,64,0.18)"" },
        new BitChartDataset { Label = ""Min"", Data = new() { 14, 16, 15, 20, 24, 26, 23 }, BorderColor = ""#36a2eb"",
            Fill = BitChartFillMode.None }
    }
};";

    private readonly string boundariesRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Boundaries()"" Options=""_legendBottom"" />";
    private readonly string boundariesCsharpCode = @"
private readonly BitChartOptions _legendBottom = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

private BitChartData Boundaries() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Origin"", Data = new() { 10, -20, 30, -10, 25, -5, 15 }, BorderColor = ""#36a2eb"",
            Fill = BitChartFillMode.Origin, FillColor = ""rgba(54,162,235,0.15)"", Tension = 0.3 },
        new BitChartDataset { Label = ""Start (top)"", Data = new() { 40, 35, 45, 38, 50, 44, 55 }, BorderColor = ""#ff6384"",
            Fill = BitChartFillMode.Start, FillColor = ""rgba(255,99,132,0.12)"", Tension = 0.3 }
    }
};";

    private readonly string radialFillRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""RadialFill()"" Options=""_noLegend"" />";
    private readonly string radialFillCsharpCode = @"
private readonly BitChartOptions _noLegend = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
};

private BitChartData RadialFill() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Signal"",
            Data = new() { 120, 190, 160, 250, 220, 300, 280 },
            BorderColor = ""#9966ff"",
            Tension = 0.4,
            Fill = BitChartFillMode.Origin,
            FillGradient = BitChartRadialGradient.Center2(""rgba(153,102,255,0.55)"", ""rgba(153,102,255,0.03)"")
        }
    }
};";
}
