namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartTooltipsDemo
{
    private readonly BitChartOptions _footer = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom },
            Tooltip = new BitChartTooltipOptions
            {
                Mode = BitChartInteractionMode.Index,
                Callbacks = new BitChartTooltipCallbacks
                {
                    Title = items => items.Count > 0 ? $"Month: {items[0].Label}" : null,
                    Footer = items => $"Total: {items.Sum(i => i.Value):N0}"
                }
            }
        }
    };

    private readonly BitChartOptions _currency = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom },
            Tooltip = new BitChartTooltipOptions
            {
                Mode = BitChartInteractionMode.Index,
                Callbacks = new BitChartTooltipCallbacks
                {
                    Label = item => $"{item.DatasetLabel}: {item.Value:C0}"
                }
            }
        }
    };

    private readonly BitChartOptions _afterBody = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom },
            Tooltip = new BitChartTooltipOptions
            {
                Mode = BitChartInteractionMode.Index,
                Callbacks = new BitChartTooltipCallbacks
                {
                    AfterBody = items =>
                    {
                        if (items.Count < 2) return null;
                        double diff = items[0].Value - items[1].Value;
                        return $"Margin: {diff:N0}";
                    }
                }
            }
        }
    };

    private readonly BitChartOptions _styled = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom },
            Tooltip = new BitChartTooltipOptions
            {
                Mode = BitChartInteractionMode.Index,
                BackgroundColor = "#ffffff",
                TitleColor = "#1f2733",
                BodyColor = "#3a4452",
                FooterColor = "#1f2733",
                BorderColor = "#e0e4ea",
                BorderWidth = 1,
                TitleAlign = BitChartAlign.Center,
                Callbacks = new BitChartTooltipCallbacks
                {
                    Footer = items => $"{items.Count} series"
                }
            }
        }
    };

    private BitChartData TwoSeries() => new()
    {
        Labels = { "Q1", "Q2", "Q3", "Q4" },
        Datasets =
        {
            new BitChartDataset { Label = "Revenue", Data = BitChartSampleData.V(1200, 1500, 1700, 2100), BorderColor = "#36a2eb", BackgroundColor = "#36a2eb", Tension = 0.4 },
            new BitChartDataset { Label = "Costs", Data = BitChartSampleData.V(800, 950, 1100, 1300), BorderColor = "#ff6384", BackgroundColor = "#ff6384", Tension = 0.4 }
        }
    };

    private readonly BitChartOptions _pointStyle = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom, Labels = new BitChartLegendLabelOptions { UsePointStyle = true } },
            Tooltip = new BitChartTooltipOptions { Mode = BitChartInteractionMode.Index, UsePointStyle = true }
        }
    };

    private BitChartData Markers() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Triangles", Data = BitChartSampleData.V(20, 35, 28, 45, 38, 52, 44),
                BorderColor = "#4bc0c0", PointBackgroundColor = "#4bc0c0", PointStyle = BitChartPointStyle.Triangle, PointRadius = 5, Tension = 0.3 },
            new BitChartDataset { Label = "Diamonds", Data = BitChartSampleData.V(12, 22, 18, 30, 26, 38, 32),
                BorderColor = "#ff9f40", PointBackgroundColor = "#ff9f40", PointStyle = BitChartPointStyle.RectRot, PointRadius = 5, Tension = 0.3 }
        }
    };


    private readonly string footerRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Revenue()"" Options=""_footer"" />";
    private readonly string footerCsharpCode = @"
private readonly BitChartOptions _footer = new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom },
        Tooltip = new BitChartTooltipOptions
        {
            Mode = BitChartInteractionMode.Index,
            Callbacks = new BitChartTooltipCallbacks
            {
                Title = items => items.Count > 0 ? $""Month: {items[0].Label}"" : null,
                Footer = items => $""Total: {items.Sum(i => i.Value):N0}""
            }
        }
    }
};
// Revenue(): 3 datasets (Product A/B/C) over Jan..Jul";

    private readonly string currencyRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""TwoSeries()"" Options=""_currency"" />";
    private readonly string currencyCsharpCode = @"
private readonly BitChartOptions _currency = new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom },
        Tooltip = new BitChartTooltipOptions
        {
            Mode = BitChartInteractionMode.Index,
            Callbacks = new BitChartTooltipCallbacks
            {
                Label = item => $""{item.DatasetLabel}: {item.Value:C0}""
            }
        }
    }
};

private BitChartData TwoSeries() => new()
{
    Labels = { ""Q1"", ""Q2"", ""Q3"", ""Q4"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Revenue"", Data = new() { 1200, 1500, 1700, 2100 }, BorderColor = ""#36a2eb"", BackgroundColor = ""#36a2eb"", Tension = 0.4 },
        new BitChartDataset { Label = ""Costs"",   Data = new() { 800, 950, 1100, 1300 }, BorderColor = ""#ff6384"", BackgroundColor = ""#ff6384"", Tension = 0.4 }
    }
};";

    private readonly string afterBodyRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""TwoSeries()"" Options=""_afterBody"" />";
    private readonly string afterBodyCsharpCode = @"
private readonly BitChartOptions _afterBody = new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom },
        Tooltip = new BitChartTooltipOptions
        {
            Mode = BitChartInteractionMode.Index,
            Callbacks = new BitChartTooltipCallbacks
            {
                AfterBody = items =>
                {
                    if (items.Count < 2) return null;
                    double diff = items[0].Value - items[1].Value;
                    return $""Margin: {diff:N0}"";
                }
            }
        }
    }
};
// TwoSeries(): Revenue & Costs over Q1..Q4";

    private readonly string styledRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Revenue()"" Options=""_styled"" />";
    private readonly string styledCsharpCode = @"
private readonly BitChartOptions _styled = new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom },
        Tooltip = new BitChartTooltipOptions
        {
            Mode = BitChartInteractionMode.Index,
            BackgroundColor = ""#ffffff"",
            TitleColor = ""#1f2733"",
            BodyColor = ""#3a4452"",
            FooterColor = ""#1f2733"",
            BorderColor = ""#e0e4ea"",
            BorderWidth = 1,
            TitleAlign = BitChartAlign.Center,
            Callbacks = new BitChartTooltipCallbacks { Footer = items => $""{items.Count} series"" }
        }
    }
};
// Revenue(): 3 datasets (Product A/B/C) over Jan..Jul";

    private readonly string pointStyleRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Markers()"" Options=""_pointStyle"" />";
    private readonly string pointStyleCsharpCode = @"
private readonly BitChartOptions _pointStyle = new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom, Labels = new BitChartLegendLabelOptions { UsePointStyle = true } },
        Tooltip = new BitChartTooltipOptions { Mode = BitChartInteractionMode.Index, UsePointStyle = true }
    }
};

private BitChartData Markers() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Triangles"", Data = new() { 20, 35, 28, 45, 38, 52, 44 },
            BorderColor = ""#4bc0c0"", PointBackgroundColor = ""#4bc0c0"", PointStyle = BitChartPointStyle.Triangle, PointRadius = 5, Tension = 0.3 },
        new BitChartDataset { Label = ""Diamonds"", Data = new() { 12, 22, 18, 30, 26, 38, 32 },
            BorderColor = ""#ff9f40"", PointBackgroundColor = ""#ff9f40"", PointStyle = BitChartPointStyle.RectRot, PointRadius = 5, Tension = 0.3 }
    }
};";
}
