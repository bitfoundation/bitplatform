namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartZoomDemo
{
    private const int _count = 5000;
    private const int _samples = 250;
    private BitChartData _data = default!;

    private readonly BitChartOptions _options = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Display = false },
            Decimation = new BitChartDecimationOptions { Enabled = true, Samples = _samples, Threshold = 400 },
            Tooltip = new BitChartTooltipOptions { Enabled = false }
        },
        Zoom = new BitChartZoomOptions { Enabled = true, Mode = BitChartZoomMode.X },
        Scales = { ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Time } }
    };

    private readonly BitChartOptions _xy = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
        Zoom = new BitChartZoomOptions { Enabled = true, Mode = BitChartZoomMode.XY }
    };

    private readonly BitChartOptions _drag = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
        Zoom = new BitChartZoomOptions { Enabled = true, Mode = BitChartZoomMode.XY, DragZoom = true, Wheel = false }
    };

    private readonly BitChartOptions _catZoom = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
        Zoom = new BitChartZoomOptions { Enabled = true, Mode = BitChartZoomMode.X }
    };

    protected override void OnInitialized() => _data = BitChartSampleData.LargeSeries(_count);

    private BitChartData Scatter()
    {
        var rnd = new Random(3);
        var pts = new List<BitChartDataPoint>();
        for (int i = 0; i < 400; i++)
            pts.Add(new BitChartDataPoint(rnd.NextDouble() * 100 - 50, rnd.NextDouble() * 100 - 50));
        return new BitChartData { Datasets = { new BitChartDataset { Label = "Cloud", BackgroundColor = "rgba(54,162,235,0.6)", Points = pts } } };
    }

    private BitChartData ManyBars()
    {
        var rnd = new Random(11);
        var labels = Enumerable.Range(1, 40).Select(i => $"D{i}").ToList();
        var data = Enumerable.Range(0, 40).Select(_ => (double?)Math.Round(rnd.NextDouble() * 80 + 20, 0)).ToList();
        return new BitChartData
        {
            Labels = labels,
            Datasets = { new BitChartDataset { Label = "Daily", Data = data, BackgroundColor = "#4bc0c0" } }
        };
    }


    private readonly string decimationRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""_data"" Options=""_options"" />";
    private readonly string decimationCsharpCode = @"
private const int _count = 5000;
private const int _samples = 250;
private BitChartData _data = default!;

private readonly BitChartOptions _options = new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Display = false },
        // LTTB downsampling keeps thousands of points smooth.
        Decimation = new BitChartDecimationOptions { Enabled = true, Samples = _samples, Threshold = 400 },
        Tooltip = new BitChartTooltipOptions { Enabled = false }
    },
    Zoom = new BitChartZoomOptions { Enabled = true, Mode = BitChartZoomMode.X },
    Scales = { [""x""] = new BitChartScaleOptions { Id = ""x"", Type = BitChartScaleType.Time } }
};

protected override void OnInitialized() => _data = BuildLargeSeries(_count);";

    private readonly string xyRazorCode = @"<BitChart Type=""BitChartType.Scatter"" Data=""Scatter()"" Options=""_xy"" />";
    private readonly string xyCsharpCode = @"
private readonly BitChartOptions _xy = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
    Zoom = new BitChartZoomOptions { Enabled = true, Mode = BitChartZoomMode.XY }
};

private BitChartData Scatter()
{
    var rnd = new Random(3);
    var pts = new List<BitChartDataPoint>();
    for (int i = 0; i < 400; i++)
        pts.Add(new BitChartDataPoint(rnd.NextDouble() * 100 - 50, rnd.NextDouble() * 100 - 50));
    return new BitChartData { Datasets = { new BitChartDataset { Label = ""Cloud"", BackgroundColor = ""rgba(54,162,235,0.6)"", Points = pts } } };
}";

    private readonly string dragRazorCode = @"<BitChart Type=""BitChartType.Scatter"" Data=""Scatter()"" Options=""_drag"" />";
    private readonly string dragCsharpCode = @"
// DragZoom + Wheel = false: drag a rectangle to zoom into it.
private readonly BitChartOptions _drag = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
    Zoom = new BitChartZoomOptions { Enabled = true, Mode = BitChartZoomMode.XY, DragZoom = true, Wheel = false }
};
// Scatter(): 400-point random cloud (see ""Zoom both axes"" card)";

    private readonly string catZoomRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""ManyBars()"" Options=""_catZoom"" />";
    private readonly string catZoomCsharpCode = @"
private readonly BitChartOptions _catZoom = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
    Zoom = new BitChartZoomOptions { Enabled = true, Mode = BitChartZoomMode.X }
};

private BitChartData ManyBars()
{
    var rnd = new Random(11);
    var labels = Enumerable.Range(1, 40).Select(i => $""D{i}"").ToList();
    var data = Enumerable.Range(0, 40).Select(_ => (double?)Math.Round(rnd.NextDouble() * 80 + 20, 0)).ToList();
    return new BitChartData
    {
        Labels = labels,
        Datasets = { new BitChartDataset { Label = ""Daily"", Data = data, BackgroundColor = ""#4bc0c0"" } }
    };
}";
}
