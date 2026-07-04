namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartScatterDemo
{
    private readonly BitChartOptions _bottom = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom, Labels = new BitChartLegendLabelOptions { UsePointStyle = true } }
        },
        Scales =
        {
            ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Linear, Grace = 0.1 },
            ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, Grace = 0.1 }
        }
    };

    private BitChartData ScatterData() => new()
    {
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Group A", BackgroundColor = "#36a2eb",
                Points = new()
                {
                    new(-8, 5), new(-5, 8), new(-3, 2), new(-1, 6), new(0, 9),
                    new(2, 4), new(4, 11), new(6, 7), new(9, 13)
                }
            },
            new BitChartDataset
            {
                Label = "Group B", BackgroundColor = "#ff6384", PointStyle = BitChartPointStyle.Triangle, PointRadius = 5,
                Points = new()
                {
                    new(-7, -3), new(-4, -1), new(-2, -6), new(1, -2), new(3, -7),
                    new(5, -1), new(7, -5), new(10, 0)
                }
            }
        }
    };

    private BitChartData BubbleData() => new()
    {
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Markets", BackgroundColor = ColorUtilAlpha("#4bc0c0"),
                BorderColor = "#4bc0c0",
                Points = new()
                {
                    new(10, 20, 14), new(15, 10, 8), new(20, 30, 22),
                    new(25, 18, 10), new(30, 25, 30), new(35, 12, 6)
                }
            }
        }
    };

    private static string ColorUtilAlpha(string c) => BitChartColorUtil.WithAlpha(c, 0.5);


    private readonly string scatterRazorCode = @"<BitChart Type=""BitChartType.Scatter"" Data=""ScatterData()"" Options=""_bottom"" />";
    private readonly string scatterCsharpCode = @"
private readonly BitChartOptions _bottom = new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom, Labels = new BitChartLegendLabelOptions { UsePointStyle = true } }
    },
    Scales =
    {
        [""x""] = new BitChartScaleOptions { Id = ""x"", Type = BitChartScaleType.Linear, Grace = 0.1 },
        [""y""] = new BitChartScaleOptions { Id = ""y"", Type = BitChartScaleType.Linear, Grace = 0.1 }
    }
};

private BitChartData ScatterData() => new()
{
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Group A"", BackgroundColor = ""#36a2eb"",
            Points = new()
            {
                new(-8, 5), new(-5, 8), new(-3, 2), new(-1, 6), new(0, 9),
                new(2, 4), new(4, 11), new(6, 7), new(9, 13)
            }
        },
        new BitChartDataset
        {
            Label = ""Group B"", BackgroundColor = ""#ff6384"", PointStyle = BitChartPointStyle.Triangle, PointRadius = 5,
            Points = new()
            {
                new(-7, -3), new(-4, -1), new(-2, -6), new(1, -2), new(3, -7),
                new(5, -1), new(7, -5), new(10, 0)
            }
        }
    }
};";

    private readonly string bubbleRazorCode = @"<BitChart Type=""BitChartType.Bubble"" Data=""BubbleData()"" Options=""_bottom"" />";
    private readonly string bubbleCsharpCode = @"
private readonly BitChartOptions _bottom = new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom, Labels = new BitChartLegendLabelOptions { UsePointStyle = true } }
    },
    Scales =
    {
        [""x""] = new BitChartScaleOptions { Id = ""x"", Type = BitChartScaleType.Linear, Grace = 0.1 },
        [""y""] = new BitChartScaleOptions { Id = ""y"", Type = BitChartScaleType.Linear, Grace = 0.1 }
    }
};

private BitChartData BubbleData() => new()
{
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Markets"",
            BackgroundColor = BitChartColorUtil.WithAlpha(""#4bc0c0"", 0.5),
            BorderColor = ""#4bc0c0"",
            // BitChartDataPoint(x, y, r) — r drives the bubble radius
            Points = new()
            {
                new(10, 20, 14), new(15, 10, 8), new(20, 30, 22),
                new(25, 18, 10), new(30, 25, 30), new(35, 12, 6)
            }
        }
    }
};";
}
