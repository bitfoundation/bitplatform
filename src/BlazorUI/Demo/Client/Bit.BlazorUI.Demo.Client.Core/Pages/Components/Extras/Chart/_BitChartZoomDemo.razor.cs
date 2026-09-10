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

    protected override void OnInitialized()
    {
        _data = BitChartSampleData.LargeSeries(_count);
        _brushData = BitChartSampleData.LargeSeries(2000);
    }

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

    // ---- overview and detail ----

    private BitChart? _brush;
    private BitChartData _brushData = default!;
    private string _window = "the whole series";

    private readonly BitChartOptions _brushOptions = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Display = false },
            Decimation = new BitChartDecimationOptions { Enabled = true, Samples = 250, Threshold = 400 }
        },
        Zoom = new BitChartZoomOptions { Enabled = true, Mode = BitChartZoomMode.X },
        Scales = { ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Time } }
    };

    /// <summary>The strip under the detail chart: the same series with nothing but the line.</summary>
    private readonly BitChartOptions _overview = new()
    {
        Sparkline = true,
        MaintainAspectRatio = false,
        Plugins = new BitChartPluginOptions
        {
            Decimation = new BitChartDecimationOptions { Enabled = true, Samples = 250, Threshold = 400 },
            Tooltip = new BitChartTooltipOptions { Enabled = false }
        },
        Scales = { ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Time } }
    };

    /// <summary>Zooms the detail chart to a fraction of the full series.</summary>
    private void ShowWindow(double from, double to)
    {
        if (_brush is null) return;
        var full = FullRange();
        double span = full.Max - full.Min;
        _brush.ZoomTo("x", full.Min + span * from, full.Min + span * to);
    }

    private (double Min, double Max) FullRange()
    {
        var points = _brushData.Datasets[0].Points!;
        return (points[0].X, points[^1].X);
    }

    private void ReadWindow()
    {
        // No range means nothing is zoomed - a reset leaves the readout claiming the old window otherwise.
        if (_brush?.GetAxisRange("x") is not { } range)
        {
            _window = "the whole series";
            return;
        }
        _window = $"{DateTime.FromOADate(range.Min):MMM d, HH:mm} to {DateTime.FromOADate(range.Max):MMM d, HH:mm}";
    }

    private readonly string brushRazorCode = @"<BitButton OnClick=""() => ShowWindow(0, 0.25)"">First quarter</BitButton>
<BitButton OnClick=""() => _brush?.ResetZoom()"">Whole series</BitButton>

<BitChart @ref=""_brush"" Type=""BitChartType.Line"" Data=""_brushData"" Options=""_brushOptions"" OnZoomChange=""ReadWindow"" />
<div>Showing @_window</div>

@* The overview strip: the same series with nothing but the line. *@
<BitChart Type=""BitChartType.Line"" Data=""_brushData"" Options=""_overview"" Height=""56px"" />";
    private readonly string brushCsharpCode = @"
private BitChart? _brush;
private BitChartData _brushData = default!;
private string _window = ""the whole series"";

private readonly BitChartOptions _brushOptions = new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Display = false },
        Decimation = new BitChartDecimationOptions { Enabled = true, Samples = 250, Threshold = 400 }
    },
    Zoom = new BitChartZoomOptions { Enabled = true, Mode = BitChartZoomMode.X },
    Scales = { [""x""] = new BitChartScaleOptions { Id = ""x"", Type = BitChartScaleType.Time } }
};

private readonly BitChartOptions _overview = new()
{
    Sparkline = true,
    MaintainAspectRatio = false,
    Plugins = new BitChartPluginOptions
    {
        Decimation = new BitChartDecimationOptions { Enabled = true, Samples = 250, Threshold = 400 },
        Tooltip = new BitChartTooltipOptions { Enabled = false }
    },
    Scales = { [""x""] = new BitChartScaleOptions { Id = ""x"", Type = BitChartScaleType.Time } }
};

// Zooms the detail chart to a fraction of the full series.
private void ShowWindow(double from, double to)
{
    var points = _brushData.Datasets[0].Points!;
    double min = points[0].X, span = points[^1].X - min;
    _brush!.ZoomTo(""x"", min + span * from, min + span * to);
}

// OnZoomChange fires after every wheel, drag and ZoomTo, so the readout always matches the view.
private void ReadWindow()
{
    // No range means nothing is zoomed - a reset leaves the readout claiming the old window otherwise.
    if (_brush?.GetAxisRange(""x"") is not { } range)
    {
        _window = ""the whole series"";
        return;
    }
    _window = $""{DateTime.FromOADate(range.Min):MMM d, HH:mm} to {DateTime.FromOADate(range.Max):MMM d, HH:mm}"";
}";
}
