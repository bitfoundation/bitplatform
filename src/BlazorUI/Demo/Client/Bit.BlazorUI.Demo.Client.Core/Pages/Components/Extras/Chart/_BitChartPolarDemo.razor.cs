namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartPolarDemo
{
    private readonly BitChartOptions _right = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Right } }
    };

    private readonly BitChartOptions _labels = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
        Scales =
        {
            ["r"] = new BitChartScaleOptions
            {
                Id = "r", Type = BitChartScaleType.RadialLinear,
                PointLabels = new BitChartPointLabelOptions { Color = "#36507a", Font = new BitChartFont { Size = 12, Weight = "bold" } }
            }
        }
    };

    private BitChartData Data() => new()
    {
        Labels = { "Red", "Green", "Yellow", "Grey", "Blue" },
        Datasets =
        {
            new BitChartDataset { Label = "Votes", Data = BitChartSampleData.V(11, 16, 7, 14, 20) }
        }
    };


    private readonly string areaRazorCode = @"<BitChart Type=""BitChartType.PolarArea"" Data=""Data()"" Options=""_right"" />";
    private readonly string areaCsharpCode = @"
private readonly BitChartOptions _right = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Right } }
};

private BitChartData Data() => new()
{
    Labels = { ""Red"", ""Green"", ""Yellow"", ""Grey"", ""Blue"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Votes"", Data = new() { 11, 16, 7, 14, 20 } }
    }
};";

    private readonly string labelsRazorCode = @"<BitChart Type=""BitChartType.PolarArea"" Data=""Data()"" Options=""_labels"" />";
    private readonly string labelsCsharpCode = @"
private readonly BitChartOptions _labels = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
    Scales =
    {
        [""r""] = new BitChartScaleOptions
        {
            Id = ""r"", Type = BitChartScaleType.RadialLinear,
            PointLabels = new BitChartPointLabelOptions { Color = ""#36507a"", Font = new BitChartFont { Size = 12, Weight = ""bold"" } }
        }
    }
};

private BitChartData Data() => new()
{
    Labels = { ""Red"", ""Green"", ""Yellow"", ""Grey"", ""Blue"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Votes"", Data = new() { 11, 16, 7, 14, 20 } }
    }
};";
}
