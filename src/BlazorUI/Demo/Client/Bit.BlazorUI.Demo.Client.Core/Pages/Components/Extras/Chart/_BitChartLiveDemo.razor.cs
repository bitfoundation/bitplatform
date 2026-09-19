namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartLiveDemo
{
    private const int WindowSize = 40;

    private BitChart? _live;
    private BitChart? _visibility;
    private System.Timers.Timer? _timer;
    private readonly Random _random = new(11);
    private double _value = 50;

    /// <summary>The one data object the feed appends to; the chart is told to re-read it by Refresh().</summary>
    private readonly BitChartData _stream = new()
    {
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Throughput",
                BorderColor = "#36a2eb",
                FillColor = "rgba(54,162,235,0.15)",
                Fill = BitChartFillMode.Origin,
                PointRadius = 0,
                BorderWidth = 2,
                Tension = 0.3
            }
        }
    };

    private readonly BitChartOptions _streamOptions = new()
    {
        Animation = new BitChartAnimationOptions { Animate = false },
        Scales =
        {
            ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, BeginAtZero = true, SuggestedMax = 100 }
        },
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
    };

    private readonly BitChartData _revenue = BitChartSampleData.Revenue();

    private readonly BitChartOptions _visibilityOptions = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
    };

    protected override void OnInitialized()
    {
        for (int i = 0; i < WindowSize; i++) Append();
    }

    private void Toggle()
    {
        if (_timer is not null) { Stop(); return; }
        _timer = new System.Timers.Timer(700) { AutoReset = true };
        _timer.Elapsed += async (_, _) => await InvokeAsync(AddReading);
        _timer.Start();
    }

    private void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    /// <summary>Appends one reading, drops the oldest, then asks the chart to redraw itself.</summary>
    private void AddReading()
    {
        Append();
        _live?.Refresh();
        StateHasChanged();
    }

    private void Append()
    {
        _value = Math.Clamp(_value + _random.NextDouble() * 18 - 9, 5, 95);
        var ds = _stream.Datasets[0];
        ds.Data.Add(Math.Round(_value, 1));
        _stream.Labels.Add(DateTime.Now.ToString("HH:mm:ss"));
        if (ds.Data.Count <= WindowSize) return;
        ds.Data.RemoveAt(0);
        _stream.Labels.RemoveAt(0);
    }

    public void Dispose() => Stop();


    private readonly string streamRazorCode = @"<BitButton Variant=""BitVariant.Outline"" OnClick=""Toggle"">@(_timer is null ? ""Start"" : ""Stop"")</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""AddReading"">Add one reading</BitButton>

<BitChart @ref=""_live"" Type=""BitChartType.Line"" Data=""_stream"" Options=""_streamOptions"" />";
    private readonly string streamCsharpCode = @"
private const int WindowSize = 40;

private BitChart? _live;
private System.Timers.Timer? _timer;

// One data object, appended to in place - Refresh() is what makes the chart re-read it.
private readonly BitChartData _stream = new()
{
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Throughput"",
            BorderColor = ""#36a2eb"",
            FillColor = ""rgba(54,162,235,0.15)"",
            Fill = BitChartFillMode.Origin,
            PointRadius = 0,
            Tension = 0.3
        }
    }
};

// A live feed redraws several times a second, so the entry animation is turned off.
private readonly BitChartOptions _streamOptions = new()
{
    Animation = new BitChartAnimationOptions { Animate = false },
    Scales =
    {
        [""y""] = new BitChartScaleOptions { Id = ""y"", Type = BitChartScaleType.Linear, BeginAtZero = true, SuggestedMax = 100 }
    },
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
};

private void Toggle()
{
    if (_timer is not null) { Stop(); return; }
    _timer = new System.Timers.Timer(700) { AutoReset = true };
    _timer.Elapsed += async (_, _) => await InvokeAsync(AddReading);
    _timer.Start();
}

private void AddReading()
{
    Append();          // append one value, drop the oldest
    _live?.Refresh();  // rebuild the scene from the data as it now stands
    StateHasChanged();
}";

    private readonly string visibilityRazorCode = @"@for (int i = 0; i < _revenue.Datasets.Count; i++)
{
    var index = i;
    <label>
        <input type=""checkbox"" checked=""@(_visibility?.IsDatasetVisible(index) ?? true)""
               @onchange=""() => _visibility?.ToggleDataset(index)"" />
        @_revenue.Datasets[index].Label
    </label>
}
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => _visibility?.ResetVisibility()"">Show all</BitButton>

<BitChart @ref=""_visibility"" Type=""BitChartType.Bar"" Data=""_revenue"" Options=""_visibilityOptions"" />";
    private readonly string visibilityCsharpCode = @"
private BitChart? _visibility;

private readonly BitChartData _revenue = Revenue();

private readonly BitChartOptions _visibilityOptions = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

// The same state the legend drives:
//   chart.ToggleDataset(i) / SetDatasetVisible(i, visible) / IsDatasetVisible(i)
//   chart.ToggleDataIndex(i) for one pie/doughnut/polar-area slice
//   chart.ResetVisibility()  to bring everything back";
}
