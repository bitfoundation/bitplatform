namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartTimeDemo
{
    private readonly BitChartOptions _time = new()
    {
        Animation = new BitChartAnimationOptions { Progressive = true, Duration = 1500 },
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
        Scales =
        {
            ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Time },
            ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear }
        }
    };

    private readonly BitChartOptions _month = new()
    {
        Animation = new BitChartAnimationOptions { Progressive = true, Duration = 1500 },
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
        Scales =
        {
            ["x"] = new BitChartScaleOptions
            {
                Id = "x", Type = BitChartScaleType.Time, TimeUnit = BitChartTimeUnit.Week,
                TimeFormat = d => d.ToString("d MMM")
            },
            ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear }
        }
    };


    private readonly string dailyRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""TimeSeries()"" Options=""_time"" />";
    private readonly string dailyCsharpCode = @"
private readonly BitChartOptions _time = new()
{
    Animation = new BitChartAnimationOptions { Progressive = true, Duration = 1500 },
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
    Scales =
    {
        // A Time scale auto-selects the unit (hour/day/week/month/year) for the range.
        [""x""] = new BitChartScaleOptions { Id = ""x"", Type = BitChartScaleType.Time },
        [""y""] = new BitChartScaleOptions { Id = ""y"", Type = BitChartScaleType.Linear }
    }
};

// Points carry x as a date (OADate). e.g. 60 daily points:
private BitChartData TimeSeries()
{
    var start = new DateTime(2026, 1, 1);
    var rnd = new Random(7);
    var pts = new List<BitChartDataPoint>();
    double v = 100;
    for (int i = 0; i < 60; i++)
    {
        v += rnd.NextDouble() * 20 - 9;
        pts.Add(new BitChartDataPoint(start.AddDays(i).ToOADate(), Math.Round(v, 1)));
    }
    return new BitChartData
    {
        Datasets = { new BitChartDataset { Label = ""Price"", BorderColor = ""#36a2eb"", Tension = 0.3, Points = pts, PointRadius = 0 } }
    };
}";

    private readonly string monthRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""TimeSeries()"" Options=""_month"" />";
    private readonly string monthCsharpCode = @"
private readonly BitChartOptions _month = new()
{
    Animation = new BitChartAnimationOptions { Progressive = true, Duration = 1500 },
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
    Scales =
    {
        [""x""] = new BitChartScaleOptions
        {
            Id = ""x"", Type = BitChartScaleType.Time, TimeUnit = BitChartTimeUnit.Week,
            TimeFormat = d => d.ToString(""d MMM"")   // custom tick label
        },
        [""y""] = new BitChartScaleOptions { Id = ""y"", Type = BitChartScaleType.Linear }
    }
};
// TimeSeries(): 60 daily points starting 2026-01-01 (see previous card)";
}
