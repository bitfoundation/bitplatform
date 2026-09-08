namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartDataLabelsDemo
{
    private readonly BitChartOptions _outside = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Display = false },
            DataLabels = new BitChartDataLabelOptions
            {
                Display = true,
                Anchor = BitChartAlign.End,
                Align = BitChartAlign.End,
                Font = new BitChartFont { Weight = "bold" }
            }
        },
        Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Display = false, Grace = 0.15 } }
    };

    private readonly BitChartOptions _inside = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Display = false },
            DataLabels = new BitChartDataLabelOptions
            {
                Display = true,
                Anchor = BitChartAlign.End,
                Align = BitChartAlign.Start,
                Color = "#fff",
                BackgroundColor = "rgba(0,0,0,0.35)",
                Padding = 3,
                Font = new BitChartFont { Weight = "bold" }
            }
        }
    };

    private readonly BitChartOptions _line = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Display = false },
            DataLabels = new BitChartDataLabelOptions
            {
                Display = true,
                Offset = 6,
                Formatter = v => $"{v:N0} k",
                DisplayFn = (v, _, _) => v > 250
            }
        }
    };

    private readonly BitChartOptions _doughnut = new()
    {
        CutoutPercentage = 55,
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Right },
            DataLabels = new BitChartDataLabelOptions
            {
                Display = true,
                Anchor = BitChartAlign.Center,
                Color = "#fff",
                Font = new BitChartFont { Weight = "bold" },
                FormatterCtx = (v, _, _) => $"{v / 1340d * 100:N0}%"
            }
        }
    };

    private BitChartData Sales() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Units", Data = BitChartSampleData.V(12, 19, 14, 22, 18, 25, 20),
                BackgroundColor = "#36a2eb", BorderRadius = 6 }
        }
    };

    private BitChartData Traffic() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Visits", Data = BitChartSampleData.V(120, 190, 260, 250, 320, 300, 280),
                BorderColor = "#4bc0c0", Tension = 0.35, PointRadius = 4 }
        }
    };


    private readonly string barsRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Sales()"" Options=""_outside"" />";
    private readonly string barsCsharpCode = @"
private readonly BitChartOptions _outside = new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Display = false },
        DataLabels = new BitChartDataLabelOptions
        {
            Display = true,
            Anchor = BitChartAlign.End,
            Align = BitChartAlign.End,
            Font = new BitChartFont { Weight = ""bold"" }
        }
    },
    Scales = { [""y""] = new BitChartScaleOptions { Id = ""y"", Display = false, Grace = 0.15 } }
};

private BitChartData Sales() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Units"", Data = new() { 12, 19, 14, 22, 18, 25, 20 },
            BackgroundColor = ""#36a2eb"", BorderRadius = 6 }
    }
};";

    private readonly string insideRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Sales()"" Options=""_inside"" />";
    private readonly string insideCsharpCode = @"
private readonly BitChartOptions _inside = new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Display = false },
        DataLabels = new BitChartDataLabelOptions
        {
            Display = true,
            Anchor = BitChartAlign.End,
            Align = BitChartAlign.Start,
            Color = ""#fff"",
            BackgroundColor = ""rgba(0,0,0,0.35)"",
            Padding = 3,
            Font = new BitChartFont { Weight = ""bold"" }
        }
    }
};";

    private readonly string lineRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Traffic()"" Options=""_line"" />";
    private readonly string lineCsharpCode = @"
private readonly BitChartOptions _line = new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Display = false },
        DataLabels = new BitChartDataLabelOptions
        {
            Display = true,
            Offset = 6,
            Formatter = v => $""{v:N0} k"",
            DisplayFn = (v, _, _) => v > 250
        }
    }
};

private BitChartData Traffic() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Visits"", Data = new() { 120, 190, 260, 250, 320, 300, 280 },
            BorderColor = ""#4bc0c0"", Tension = 0.35, PointRadius = 4 }
    }
};";

    private readonly string doughnutRazorCode = @"<BitChart Type=""BitChartType.Doughnut"" Data=""BitChartSampleData.Traffic()"" Options=""_doughnut"" />";
    private readonly string doughnutCsharpCode = @"
private readonly BitChartOptions _doughnut = new()
{
    CutoutPercentage = 55,
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Right },
        DataLabels = new BitChartDataLabelOptions
        {
            Display = true,
            Anchor = BitChartAlign.Center,
            Color = ""#fff"",
            Font = new BitChartFont { Weight = ""bold"" },
            FormatterCtx = (v, _, _) => $""{v / 1340d * 100:N0}%""
        }
    }
};";
}
