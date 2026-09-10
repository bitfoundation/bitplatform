namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartTrendlinesDemo
{
    private static readonly string[] Weeks =
        ["W1", "W2", "W3", "W4", "W5", "W6", "W7", "W8", "W9", "W10", "W11", "W12"];

    private readonly BitChartOptions _linear = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom },
            Custom = { new BitChartTrendlinePlugin(new BitChartTrendline { DatasetIndex = 0 }) }
        }
    };

    private readonly BitChartOptions _extended = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom },
            Custom =
            {
                new BitChartTrendlinePlugin(new BitChartTrendline
                {
                    DatasetIndex = 0,
                    Extend = true,
                    Color = "#ff6384",
                    Label = "trend"
                })
            }
        }
    };

    private readonly BitChartOptions _averages = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom },
            Custom =
            {
                new BitChartTrendlinePlugin(
                    new BitChartTrendline
                    {
                        DatasetIndex = 0,
                        Kind = BitChartTrendlineKind.MovingAverage,
                        Period = 4,
                        Color = "#9966ff",
                        Dash = null,
                        Label = "4-week average"
                    },
                    new BitChartTrendline
                    {
                        DatasetIndex = 0,
                        Kind = BitChartTrendlineKind.Average,
                        Color = "#c9cbcf",
                        LineWidth = 1.5
                    })
            }
        }
    };

    private BitChartData Noisy() => new()
    {
        Labels = [.. Weeks],
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Tickets closed",
                Data = BitChartSampleData.V(18, 26, 21, 33, 28, 39, 31, 44, 38, 51, 46, 57),
                BackgroundColor = "rgba(54,162,235,0.55)",
                BorderColor = "#36a2eb",
                BorderRadius = 4,
                Tension = 0.3
            }
        }
    };

    private BitChartData Seasonal() => new()
    {
        Labels = [.. Weeks],
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Orders",
                Data = BitChartSampleData.V(42, 61, 38, 70, 45, 78, 52, 84, 49, 90, 58, 96),
                BorderColor = "#4bc0c0",
                BackgroundColor = "#4bc0c0",
                PointRadius = 3
            }
        }
    };


    private readonly string linearRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Noisy()"" Options=""_linear"" />";
    private readonly string linearCsharpCode = @"
private readonly BitChartOptions _linear = new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom },
        Custom = { new BitChartTrendlinePlugin(new BitChartTrendline { DatasetIndex = 0 }) }
    }
};

private BitChartData Noisy() => new()
{
    Labels = { ""W1"", ""W2"", ""W3"", ""W4"", ""W5"", ""W6"", ""W7"", ""W8"", ""W9"", ""W10"", ""W11"", ""W12"" },
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Tickets closed"",
            Data = new() { 18, 26, 21, 33, 28, 39, 31, 44, 38, 51, 46, 57 },
            BackgroundColor = ""rgba(54,162,235,0.55)"",
            BorderColor = ""#36a2eb"",
            BorderRadius = 4
        }
    }
};";

    private readonly string extendRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Noisy()"" Options=""_extended"" />";
    private readonly string extendCsharpCode = @"
private readonly BitChartOptions _extended = new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom },
        Custom =
        {
            new BitChartTrendlinePlugin(new BitChartTrendline
            {
                DatasetIndex = 0,
                Extend = true,        // run the fit out to both edges of the plot
                Color = ""#ff6384"",
                Label = ""trend""
            })
        }
    }
};";

    private readonly string averagesRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Seasonal()"" Options=""_averages"" />";
    private readonly string averagesCsharpCode = @"
private readonly BitChartOptions _averages = new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom },
        Custom =
        {
            new BitChartTrendlinePlugin(
                new BitChartTrendline
                {
                    DatasetIndex = 0,
                    Kind = BitChartTrendlineKind.MovingAverage,
                    Period = 4,
                    Color = ""#9966ff"",
                    Dash = null,       // solid, so it reads as the smoothed series
                    Label = ""4-week average""
                },
                new BitChartTrendline
                {
                    DatasetIndex = 0,
                    Kind = BitChartTrendlineKind.Average,
                    Color = ""#c9cbcf"",
                    LineWidth = 1.5
                })
        }
    }
};

private BitChartData Seasonal() => new()
{
    Labels = { ""W1"", ""W2"", ""W3"", ""W4"", ""W5"", ""W6"", ""W7"", ""W8"", ""W9"", ""W10"", ""W11"", ""W12"" },
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Orders"",
            Data = new() { 42, 61, 38, 70, 45, 78, 52, 84, 49, 90, 58, 96 },
            BorderColor = ""#4bc0c0"",
            BackgroundColor = ""#4bc0c0"",
            PointRadius = 3
        }
    }
};";
}
