namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartLegendDemo
{
    private BitChartPosition _position = BitChartPosition.Top;
    private BitChartAlign _align = BitChartAlign.Center;
    private bool _usePointStyle;
    private bool _reverse;

    private readonly BitChartOptions _titled = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom, Title = "Product lines" }
        }
    };

    private readonly BitChartOptions _pointStyle = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom, Labels = new BitChartLegendLabelOptions { UsePointStyle = true } }
        }
    };

    private BitChartOptions Live() => new()
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions
            {
                Position = _position, Align = _align, Reverse = _reverse,
                Labels = new BitChartLegendLabelOptions { UsePointStyle = _usePointStyle }
            }
        }
    };

    private BitChartData MultiSeries() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Alpha", Data = BitChartSampleData.V(65, 59, 80, 81, 56, 55, 72), BorderColor = "#36a2eb", BackgroundColor = "#36a2eb", Tension = 0.3 },
            new BitChartDataset { Label = "Beta", Data = BitChartSampleData.V(28, 48, 40, 60, 86, 92, 78), BorderColor = "#ff6384", BackgroundColor = "#ff6384", Tension = 0.3 },
            new BitChartDataset { Label = "Gamma", Data = BitChartSampleData.V(40, 35, 60, 45, 70, 50, 65), BorderColor = "#4bc0c0", BackgroundColor = "#4bc0c0", Tension = 0.3 }
        }
    };

    private BitChartData Markers() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Stars", Data = BitChartSampleData.V(20, 35, 28, 45, 38, 52, 44), BorderColor = "#ff9f40", PointBackgroundColor = "#ff9f40", PointStyle = BitChartPointStyle.Star, PointRadius = 6, Tension = 0.3 },
            new BitChartDataset { Label = "Triangles", Data = BitChartSampleData.V(12, 22, 18, 30, 26, 38, 32), BorderColor = "#9966ff", PointBackgroundColor = "#9966ff", PointStyle = BitChartPointStyle.Triangle, PointRadius = 6, Tension = 0.3 }
        }
    };


    private readonly string liveRazorCode = @"@* Bind controls to legend options; the chart recomputes on each change. *@
<select @bind=""_position"">...Top/Bottom/Left/Right...</select>
<select @bind=""_align"">...Start/Center/End...</select>
<input type=""checkbox"" @bind=""_usePointStyle"" /> Point style
<input type=""checkbox"" @bind=""_reverse"" /> Reverse

<BitChart Type=""BitChartType.Line"" Data=""MultiSeries()"" Options=""Live()"" />";
    private readonly string liveCsharpCode = @"
private BitChartPosition _position = BitChartPosition.Top;
private BitChartAlign _align = BitChartAlign.Center;
private bool _usePointStyle;
private bool _reverse;

private BitChartOptions Live() => new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions
        {
            Position = _position, Align = _align, Reverse = _reverse,
            Labels = new BitChartLegendLabelOptions { UsePointStyle = _usePointStyle }
        }
    }
};

private BitChartData MultiSeries() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Alpha"", Data = new() { 65, 59, 80, 81, 56, 55, 72 }, BorderColor = ""#36a2eb"", BackgroundColor = ""#36a2eb"", Tension = 0.3 },
        new BitChartDataset { Label = ""Beta"",  Data = new() { 28, 48, 40, 60, 86, 92, 78 }, BorderColor = ""#ff6384"", BackgroundColor = ""#ff6384"", Tension = 0.3 },
        new BitChartDataset { Label = ""Gamma"", Data = new() { 40, 35, 60, 45, 70, 50, 65 }, BorderColor = ""#4bc0c0"", BackgroundColor = ""#4bc0c0"", Tension = 0.3 }
    }
};";

    private readonly string titledRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Revenue()"" Options=""_titled"" />";
    private readonly string titledCsharpCode = @"
private readonly BitChartOptions _titled = new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom, Title = ""Product lines"" }
    }
};
// Revenue(): 3 datasets (Product A/B/C) over Jan..Jul";

    private readonly string pointStyleRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Markers()"" Options=""_pointStyle"" />";
    private readonly string pointStyleCsharpCode = @"
private readonly BitChartOptions _pointStyle = new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom, Labels = new BitChartLegendLabelOptions { UsePointStyle = true } }
    }
};

private BitChartData Markers() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Stars"", Data = new() { 20, 35, 28, 45, 38, 52, 44 }, BorderColor = ""#ff9f40"",
            PointBackgroundColor = ""#ff9f40"", PointStyle = BitChartPointStyle.Star, PointRadius = 6, Tension = 0.3 },
        new BitChartDataset { Label = ""Triangles"", Data = new() { 12, 22, 18, 30, 26, 38, 32 }, BorderColor = ""#9966ff"",
            PointBackgroundColor = ""#9966ff"", PointStyle = BitChartPointStyle.Triangle, PointRadius = 6, Tension = 0.3 }
    }
};";
}
