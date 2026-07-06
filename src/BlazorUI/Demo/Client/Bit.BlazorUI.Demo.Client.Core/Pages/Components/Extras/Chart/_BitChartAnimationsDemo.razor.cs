namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartAnimationsDemo
{
    private readonly Random _rnd = new();

    // ---- Bars sample state (independent) ----
    private int _barsDuration = 800;
    private string _barsEasing = "cubic-bezier(.34,1.56,.64,1)";
    private int _barsStagger = 60;
    private BitChartData _barsData = BuildBars();

    private BitChartOptions BarsOpts() => new()
    {
        Animation = new BitChartAnimationOptions { Duration = _barsDuration, Easing = _barsEasing, DelayBetween = _barsStagger },
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
    };

    private static BitChartData BuildBars() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Value", Data = BitChartSampleData.V(40, 65, 50, 80, 60, 72, 55),
                BackgroundColor = "#36a2eb", BorderRadius = 6 }
        }
    };

    private void RandomizeBars()
    {
        var d = _barsData.Datasets[0].Data;
        for (int i = 0; i < d.Count; i++) d[i] = _rnd.Next(20, 100);
        _barsData = new BitChartData { Labels = _barsData.Labels, Datasets = { _barsData.Datasets[0] } };
    }

    // ---- Line draw-on sample state (independent) ----
    private int _lineDuration = 800;
    private string _lineEasing = "cubic-bezier(.34,1.56,.64,1)";
    private int _lineStagger = 60;
    private BitChartData _lineData = BuildLine();

    private BitChartOptions LineOpts() => new()
    {
        Animation = new BitChartAnimationOptions { Duration = _lineDuration, Easing = _lineEasing, DelayBetween = _lineStagger },
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
    };

    private static BitChartData BuildLine() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Trend", Data = BitChartSampleData.V(40, 65, 50, 80, 60, 72, 55),
                BorderColor = "#9966ff", Tension = 0.4, PointRadius = 4, PointBackgroundColor = "#9966ff" }
        }
    };

    private void RandomizeLine()
    {
        var d = _lineData.Datasets[0].Data;
        for (int i = 0; i < d.Count; i++) d[i] = _rnd.Next(20, 100);
        _lineData = new BitChartData { Labels = _lineData.Labels, Datasets = { _lineData.Datasets[0] } };
    }


    private readonly string barsRazorCode = @"
<div class=""controls"">
    <label>Duration <input type=""range"" min=""0"" max=""2000"" step=""100"" @bind=""_duration"" @bind:event=""oninput"" /> <span>@_duration ms</span></label>
    <label>Easing
        <select @bind=""_easing"">
            <option value=""ease-out"">ease-out</option>
            <option value=""ease-in-out"">ease-in-out</option>
            <option value=""cubic-bezier(.34,1.56,.64,1)"">spring</option>
            <option value=""linear"">linear</option>
        </select>
    </label>
    <label>Stagger <input type=""range"" min=""0"" max=""120"" step=""10"" @bind=""_stagger"" @bind:event=""oninput"" /> <span>@_stagger ms/el</span></label>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""Randomize"">Randomize data</BitButton>
</div>

<BitChart Type=""BitChartType.Bar"" Data=""_data"" Options=""Opts()"" />";
    private readonly string barsCsharpCode = @"
private int _duration = 800;
private string _easing = ""cubic-bezier(.34,1.56,.64,1)"";
private int _stagger = 60;
private BitChartData _data = Build();
private readonly Random _rnd = new();

// Animation maps to CSS transitions in the browser.
private BitChartOptions Opts() => new()
{
    Animation = new BitChartAnimationOptions { Duration = _duration, Easing = _easing, DelayBetween = _stagger },
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
};

private static BitChartData Build() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Value"", Data = new() { 40, 65, 50, 80, 60, 72, 55 },
            BackgroundColor = ""#36a2eb"", BorderRadius = 6 }
    }
};

private void Randomize()
{
    var d = _data.Datasets[0].Data;
    for (int i = 0; i < d.Count; i++) d[i] = _rnd.Next(20, 100);
    _data = new BitChartData { Labels = _data.Labels, Datasets = { _data.Datasets[0] } };
}";

    private readonly string lineDrawRazorCode = @"
<div class=""controls"">
    <label>Duration <input type=""range"" min=""0"" max=""2000"" step=""100"" @bind=""_duration"" @bind:event=""oninput"" /> <span>@_duration ms</span></label>
    <label>Easing
        <select @bind=""_easing"">
            <option value=""ease-out"">ease-out</option>
            <option value=""ease-in-out"">ease-in-out</option>
            <option value=""cubic-bezier(.34,1.56,.64,1)"">spring</option>
            <option value=""linear"">linear</option>
        </select>
    </label>
    <label>Stagger <input type=""range"" min=""0"" max=""120"" step=""10"" @bind=""_stagger"" @bind:event=""oninput"" /> <span>@_stagger ms/el</span></label>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""Randomize"">Randomize data</BitButton>
</div>

<BitChart Type=""BitChartType.Line"" Data=""_data"" Options=""Opts()"" />";
    private readonly string lineDrawCsharpCode = @"
private int _duration = 800;
private string _easing = ""cubic-bezier(.34,1.56,.64,1)"";
private int _stagger = 60;
private BitChartData _data = Build();
private readonly Random _rnd = new();

private BitChartOptions Opts() => new()
{
    Animation = new BitChartAnimationOptions { Duration = _duration, Easing = _easing, DelayBetween = _stagger },
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
};

private static BitChartData Build() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Trend"", Data = new() { 40, 65, 50, 80, 60, 72, 55 },
            BorderColor = ""#9966ff"", Tension = 0.4, PointRadius = 4, PointBackgroundColor = ""#9966ff"" }
    }
};

private void Randomize()
{
    var d = _data.Datasets[0].Data;
    for (int i = 0; i < d.Count; i++) d[i] = _rnd.Next(20, 100);
    _data = new BitChartData { Labels = _data.Labels, Datasets = { _data.Datasets[0] } };
}";
}
