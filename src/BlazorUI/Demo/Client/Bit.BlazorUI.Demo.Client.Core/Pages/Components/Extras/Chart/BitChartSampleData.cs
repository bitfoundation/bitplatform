namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

/// <summary>Reusable sample datasets shared across the BitChart demo sections.</summary>
public static class BitChartSampleData
{
    public static readonly string[] Months =
        { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul" };

    public static List<double?> V(params double?[] values) => values.ToList();

    /// <summary>A diverging series (positive and negative) for scriptable-color demos.</summary>
    public static BitChartData Diverging() => new()
    {
        Labels = Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Net change", Data = V(12, -8, 22, -4, 18, -15, 9) }
        }
    };

    /// <summary>Two series on different value axes for multi-axis / combo demos.</summary>
    public static BitChartData TempAndRainfall() => new()
    {
        Labels = Months.ToList(),
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Rainfall (mm)", Type = BitChartType.Bar, Data = V(60, 48, 80, 35, 55, 20, 15),
                BackgroundColor = "rgba(54,162,235,0.5)", BorderColor = "#36a2eb", YAxisID = "y", Order = 1
            },
            new BitChartDataset
            {
                Label = "Temp (°C)", Type = BitChartType.Line, Data = V(7, 9, 12, 16, 20, 24, 27),
                BorderColor = "#ff6384", BackgroundColor = "#ff6384", Tension = 0.4, YAxisID = "y2", Order = 0
            }
        }
    };

    /// <summary>Labels long enough to trigger automatic tick-label rotation.</summary>
    public static BitChartData Countries() => new()
    {
        Labels = { "United States", "United Kingdom", "Germany", "Netherlands", "Switzerland", "Australia", "New Zealand" },
        Datasets =
        {
            new BitChartDataset { Label = "Index", Data = V(72, 65, 80, 58, 91, 67, 74),
                BackgroundColor = "#4bc0c0", BorderRadius = 4 }
        }
    };

    public static BitChartData MonthlySales() => new()
    {
        Labels = Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "2025", Data = V(65, 59, 80, 81, 56, 55, 72), BorderColor = "#36a2eb", BackgroundColor = "#36a2eb", Tension = 0.4, Fill = BitChartFillMode.None },
            new BitChartDataset { Label = "2026", Data = V(28, 48, 40, 60, 86, 92, 78), BorderColor = "#ff6384", BackgroundColor = "#ff6384", Tension = 0.4, Fill = BitChartFillMode.None }
        }
    };

    public static BitChartData Revenue() => new()
    {
        Labels = Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Product A", Data = V(12, 19, 14, 22, 18, 25, 20), BackgroundColor = "#36a2eb" },
            new BitChartDataset { Label = "Product B", Data = V(8, 11, 17, 9, 14, 12, 19), BackgroundColor = "#ff9f40" },
            new BitChartDataset { Label = "Product C", Data = V(5, 7, 9, 12, 8, 10, 14), BackgroundColor = "#4bc0c0" }
        }
    };

    public static BitChartData Traffic() => new()
    {
        Labels = { "Direct", "Organic", "Referral", "Social", "Email" },
        Datasets =
        {
            new BitChartDataset { Label = "Sessions", Data = V(300, 500, 180, 240, 120) }
        }
    };

    public static BitChartData Skills() => new()
    {
        Labels = { "Speed", "Power", "Range", "Defense", "Agility", "Stamina" },
        Datasets =
        {
            new BitChartDataset { Label = "Player 1", Data = V(80, 65, 70, 60, 85, 75), BorderColor = "#36a2eb", Fill = BitChartFillMode.Origin },
            new BitChartDataset { Label = "Player 2", Data = V(55, 80, 60, 90, 50, 65), BorderColor = "#ff6384", Fill = BitChartFillMode.Origin }
        }
    };

    /// <summary>A daily time series across roughly two months (x = OADate).</summary>
    public static BitChartData TimeSeries()
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
            Datasets = { new BitChartDataset { Label = "Price", BorderColor = "#36a2eb", Tension = 0.3, Points = pts, PointRadius = 0 } }
        };
    }

    /// <summary>A large noisy series for decimation / zoom demos (x = OADate).</summary>
    public static BitChartData LargeSeries(int count = 5000)
    {
        var start = new DateTime(2026, 1, 1);
        var rnd = new Random(42);
        var pts = new List<BitChartDataPoint>(count);
        double v = 50;
        for (int i = 0; i < count; i++)
        {
            v += rnd.NextDouble() * 6 - 3 + Math.Sin(i / 60.0) * 0.6;
            pts.Add(new BitChartDataPoint(start.AddMinutes(i * 10).ToOADate(), Math.Round(v, 2)));
        }
        return new BitChartData
        {
            Datasets = { new BitChartDataset { Label = $"{count:N0} points", BorderColor = "#4bc0c0", Points = pts, PointRadius = 0, BorderWidth = 1.5 } }
        };
    }
}
