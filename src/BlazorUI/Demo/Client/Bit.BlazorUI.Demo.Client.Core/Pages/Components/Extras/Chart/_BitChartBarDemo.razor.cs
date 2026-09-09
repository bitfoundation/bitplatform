namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartBarDemo
{
    private readonly BitChartOptions _bottom = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
    };

    private readonly BitChartOptions _stacked = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
        Scales =
        {
            ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Category, Stacked = true },
            ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, Stacked = true, BeginAtZero = true }
        }
    };

    private readonly BitChartOptions _horizontal = new()
    {
        IndexAxis = BitChartIndexAxis.Y,
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
    };

    private readonly BitChartOptions _rounded = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
    };

    private readonly BitChartOptions _noLegend = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
    };

    private readonly BitChartOptions _stacked100 = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
        Scales =
        {
            ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Category, Stacked = true },
            ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, Stacked = true, Stacked100 = true, Ticks = new BitChartTickOptions { Suffix = "%" } }
        }
    };

    private readonly BitChartOptions _stackedGroups = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
        Scales =
        {
            ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Category, Stacked = true },
            ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, Stacked = true, BeginAtZero = true }
        }
    };

    private BitChartData Floating() => new()
    {
        Labels = { "Mon", "Tue", "Wed", "Thu", "Fri" },
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Temp range (°C)",
                BackgroundColor = "#ff9f40",
                BorderRadius = 4,
                RangeData = new()
                {
                    (8, 16), (10, 19), (7, 14), (11, 21), (9, 18)
                }
            }
        }
    };

    private BitChartData Patterned() => new()
    {
        Labels = { "Alpha", "Beta", "Gamma", "Delta" },
        Datasets =
        {
            new BitChartDataset { Label = "A", Data = BitChartSampleData.V(40, 55, 35, 60), BorderColor = "#36a2eb", BorderWidth = 1,
                BackgroundPattern = new BitChartFillPattern(BitChartPatternStyle.DiagonalUp, "#36a2eb", "rgba(54,162,235,0.12)") },
            new BitChartDataset { Label = "B", Data = BitChartSampleData.V(28, 42, 50, 33), BorderColor = "#ff6384", BorderWidth = 1,
                BackgroundPattern = new BitChartFillPattern(BitChartPatternStyle.Dots, "#ff6384", "rgba(255,99,132,0.10)") }
        }
    };

    private BitChartData PerColor() => new()
    {
        Labels = { "Red", "Blue", "Teal", "Orange", "Purple" },
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Votes",
                Data = BitChartSampleData.V(12, 19, 7, 15, 9),
                BackgroundColors = new() { "#ff6384", "#36a2eb", "#4bc0c0", "#ff9f40", "#9966ff" },
                BorderRadius = 6
            }
        }
    };

    private BitChartData PerColorRounded() => new()
    {
        Labels = { "Red", "Blue", "Teal", "Orange", "Purple" },
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Votes",
                Data = BitChartSampleData.V(12, 19, 7, 15, 9),
                BackgroundColors = new() { "#ff6384", "#36a2eb", "#4bc0c0", "#ff9f40", "#9966ff" },
                BorderRadius = 16
            }
        }
    };

    private BitChartData TopRounded() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Throughput",
                Data = BitChartSampleData.V(40, 65, 50, 80, 60, 72, 55),
                BackgroundColor = "#36a2eb",
                BorderRadiusCorners = BitChartBorderRadiusCorners.Top(10)
            }
        }
    };

    private BitChartData StackedGroups() => new()
    {
        Labels = { "Q1", "Q2", "Q3", "Q4" },
        Datasets =
        {
            new BitChartDataset { Label = "2025 · New", Stack = "2025", Data = BitChartSampleData.V(20, 28, 24, 32), BackgroundColor = "#36a2eb" },
            new BitChartDataset { Label = "2025 · Renew", Stack = "2025", Data = BitChartSampleData.V(12, 16, 14, 18), BackgroundColor = "#9cd0f5" },
            new BitChartDataset { Label = "2026 · New", Stack = "2026", Data = BitChartSampleData.V(26, 30, 29, 38), BackgroundColor = "#ff6384" },
            new BitChartDataset { Label = "2026 · Renew", Stack = "2026", Data = BitChartSampleData.V(15, 18, 17, 22), BackgroundColor = "#ffb1c1" }
        }
    };


    private readonly string groupedRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Revenue()"" Options=""_bottom"" />";
    private readonly string groupedCsharpCode = @"
private readonly BitChartOptions _bottom = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

private BitChartData Revenue() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Product A"", Data = new() { 12, 19, 14, 22, 18, 25, 20 }, BackgroundColor = ""#36a2eb"" },
        new BitChartDataset { Label = ""Product B"", Data = new() { 8, 11, 17, 9, 14, 12, 19 }, BackgroundColor = ""#ff9f40"" },
        new BitChartDataset { Label = ""Product C"", Data = new() { 5, 7, 9, 12, 8, 10, 14 }, BackgroundColor = ""#4bc0c0"" }
    }
};";

    private readonly string stackedRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Revenue()"" Options=""_stacked"" />";
    private readonly string stackedCsharpCode = @"
private readonly BitChartOptions _stacked = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
    Scales =
    {
        [""x""] = new BitChartScaleOptions { Id = ""x"", Type = BitChartScaleType.Category, Stacked = true },
        [""y""] = new BitChartScaleOptions { Id = ""y"", Type = BitChartScaleType.Linear, Stacked = true, BeginAtZero = true }
    }
};
// Revenue() returns 3 datasets (Product A/B/C) over Jan..Jul.";

    private readonly string horizontalRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""PerColor()"" Options=""_horizontal"" />";
    private readonly string horizontalCsharpCode = @"
private readonly BitChartOptions _horizontal = new()
{
    IndexAxis = BitChartIndexAxis.Y,
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
};

private BitChartData PerColor() => new()
{
    Labels = { ""Red"", ""Blue"", ""Teal"", ""Orange"", ""Purple"" },
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Votes"",
            Data = new() { 12, 19, 7, 15, 9 },
            BackgroundColors = new() { ""#ff6384"", ""#36a2eb"", ""#4bc0c0"", ""#ff9f40"", ""#9966ff"" },
            BorderRadius = 6
        }
    }
};";

    private readonly string roundedRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""PerColorRounded()"" Options=""_rounded"" />";
    private readonly string roundedCsharpCode = @"
private readonly BitChartOptions _rounded = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
};

private BitChartData PerColorRounded() => new()
{
    Labels = { ""Red"", ""Blue"", ""Teal"", ""Orange"", ""Purple"" },
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Votes"",
            Data = new() { 12, 19, 7, 15, 9 },
            BackgroundColors = new() { ""#ff6384"", ""#36a2eb"", ""#4bc0c0"", ""#ff9f40"", ""#9966ff"" },
            BorderRadius = 16
        }
    }
};";

    private readonly string floatingRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Floating()"" Options=""_noLegend"" />";
    private readonly string floatingCsharpCode = @"
private readonly BitChartOptions _noLegend = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
};

private BitChartData Floating() => new()
{
    Labels = { ""Mon"", ""Tue"", ""Wed"", ""Thu"", ""Fri"" },
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Temp range (°C)"",
            BackgroundColor = ""#ff9f40"",
            BorderRadius = 4,
            RangeData = new() { (8, 16), (10, 19), (7, 14), (11, 21), (9, 18) }
        }
    }
};";

    private readonly string stacked100RazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Revenue()"" Options=""_stacked100"" />";
    private readonly string stacked100CsharpCode = @"
private readonly BitChartOptions _stacked100 = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
    Scales =
    {
        [""x""] = new BitChartScaleOptions { Id = ""x"", Type = BitChartScaleType.Category, Stacked = true },
        [""y""] = new BitChartScaleOptions { Id = ""y"", Type = BitChartScaleType.Linear, Stacked = true, Stacked100 = true,
            Ticks = new BitChartTickOptions { Suffix = ""%"" } }
    }
};
// Revenue() returns 3 datasets (Product A/B/C) over Jan..Jul.";

    private readonly string patternedRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Patterned()"" Options=""_bottom"" />";
    private readonly string patternedCsharpCode = @"
private readonly BitChartOptions _bottom = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

private BitChartData Patterned() => new()
{
    Labels = { ""Alpha"", ""Beta"", ""Gamma"", ""Delta"" },
    Datasets =
    {
        new BitChartDataset { Label = ""A"", Data = new() { 40, 55, 35, 60 }, BorderColor = ""#36a2eb"", BorderWidth = 1,
            BackgroundPattern = new BitChartFillPattern(BitChartPatternStyle.DiagonalUp, ""#36a2eb"", ""rgba(54,162,235,0.12)"") },
        new BitChartDataset { Label = ""B"", Data = new() { 28, 42, 50, 33 }, BorderColor = ""#ff6384"", BorderWidth = 1,
            BackgroundPattern = new BitChartFillPattern(BitChartPatternStyle.Dots, ""#ff6384"", ""rgba(255,99,132,0.10)"") }
    }
};";

    private readonly string topRoundedRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""TopRounded()"" Options=""_noLegend"" />";
    private readonly string topRoundedCsharpCode = @"
private readonly BitChartOptions _noLegend = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
};

private BitChartData TopRounded() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Throughput"",
            Data = new() { 40, 65, 50, 80, 60, 72, 55 },
            BackgroundColor = ""#36a2eb"",
            BorderRadiusCorners = BitChartBorderRadiusCorners.Top(10)
        }
    }
};";

    private readonly string rotatedRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Countries()"" Options=""_noLegend"" />";
    private readonly string rotatedCsharpCode = @"
private readonly BitChartOptions _noLegend = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
};

// Long category labels tilt automatically to fit.
private BitChartData Countries() => new()
{
    Labels = { ""United States"", ""United Kingdom"", ""Germany"", ""Netherlands"", ""Switzerland"", ""Australia"", ""New Zealand"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Index"", Data = new() { 72, 65, 80, 58, 91, 67, 74 },
            BackgroundColor = ""#4bc0c0"", BorderRadius = 4 }
    }
};";

    private readonly string stackedGroupsRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""StackedGroups()"" Options=""_stackedGroups"" />";
    private readonly string stackedGroupsCsharpCode = @"
private readonly BitChartOptions _stackedGroups = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
    Scales =
    {
        [""x""] = new BitChartScaleOptions { Id = ""x"", Type = BitChartScaleType.Category, Stacked = true },
        [""y""] = new BitChartScaleOptions { Id = ""y"", Type = BitChartScaleType.Linear, Stacked = true, BeginAtZero = true }
    }
};

private BitChartData StackedGroups() => new()
{
    Labels = { ""Q1"", ""Q2"", ""Q3"", ""Q4"" },
    Datasets =
    {
        new BitChartDataset { Label = ""2025 · New"",   Stack = ""2025"", Data = new() { 20, 28, 24, 32 }, BackgroundColor = ""#36a2eb"" },
        new BitChartDataset { Label = ""2025 · Renew"", Stack = ""2025"", Data = new() { 12, 16, 14, 18 }, BackgroundColor = ""#9cd0f5"" },
        new BitChartDataset { Label = ""2026 · New"",   Stack = ""2026"", Data = new() { 26, 30, 29, 38 }, BackgroundColor = ""#ff6384"" },
        new BitChartDataset { Label = ""2026 · Renew"", Stack = ""2026"", Data = new() { 15, 18, 17, 22 }, BackgroundColor = ""#ffb1c1"" }
    }
};";

    private readonly BitChartOptions _target = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
        Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Title = new BitChartScaleTitleOptions { Display = true, Text = "Units" } } }
    };

    private BitChartData AgainstTarget() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Units vs target",
                Data = BitChartSampleData.V(118, 96, 100.2, 131, 88, 104, 112),
                Base = 100,
                MinBarLength = 6,
                BorderRadius = 3,
                BackgroundColorFn = ctx => ctx.Value >= 100 ? "#2ecc71" : "#ff6384"
            }
        }
    };

    private BitChartData Overlay() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Budget", Grouped = false,
                Data = BitChartSampleData.V(20, 22, 24, 26, 28, 30, 32),
                BackgroundColor = "rgba(120,130,145,0.25)"
            },
            new BitChartDataset
            {
                Label = "Team A", SkipNull = true,
                Data = BitChartSampleData.V(12, 19, 14, 22, 18, 25, 20),
                BackgroundColor = "#36a2eb", BorderRadius = 3
            },
            new BitChartDataset
            {
                Label = "Team B", SkipNull = true,
                Data = BitChartSampleData.V(8, null, 17, null, 14, 12, 19),
                BackgroundColor = "#ff9f40", BorderRadius = 3
            }
        }
    };

    private readonly string minLengthRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""AgainstTarget()"" Options=""_target"" />";
    private readonly string minLengthCsharpCode = @"
private readonly BitChartOptions _target = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } },
    Scales = { [""y""] = new BitChartScaleOptions { Id = ""y"", Title = new BitChartScaleTitleOptions { Display = true, Text = ""Units"" } } }
};

private BitChartData AgainstTarget() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Units vs target"",
            Data = new() { 118, 96, 100.2, 131, 88, 104, 112 },
            Base = 100,          // bars grow from the target instead of zero
            MinBarLength = 6,    // a value that is almost on target still shows
            BorderRadius = 3,
            BackgroundColorFn = ctx => ctx.Value >= 100 ? ""#2ecc71"" : ""#ff6384""
        }
    }
};";

    private readonly string overlayRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Overlay()"" Options=""_bottom"" />";
    private readonly string overlayCsharpCode = @"
private BitChartData Overlay() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        // Grouped = false steps out of the side-by-side layout and keeps the whole band.
        new BitChartDataset
        {
            Label = ""Budget"", Grouped = false,
            Data = new() { 20, 22, 24, 26, 28, 30, 32 },
            BackgroundColor = ""rgba(120,130,145,0.25)""
        },
        new BitChartDataset
        {
            Label = ""Team A"", SkipNull = true,
            Data = new() { 12, 19, 14, 22, 18, 25, 20 },
            BackgroundColor = ""#36a2eb"", BorderRadius = 3
        },
        // The nulls leave no gap: Team A widens over those categories.
        new BitChartDataset
        {
            Label = ""Team B"", SkipNull = true,
            Data = new() { 8, null, 17, null, 14, 12, 19 },
            BackgroundColor = ""#ff9f40"", BorderRadius = 3
        }
    }
};";

    private BitChartData Measured() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Response time (ms)",
                Data = BitChartSampleData.V(120, 138, 131, 156, 149, 162, 158),
                BackgroundColor = "rgba(54,162,235,0.55)",
                BorderColor = "#36a2eb",
                BorderRadius = 4,
                ErrorData = [12, 9, 15, new BitChartErrorBar(6, 24), 11, null, 8],
                ErrorBarColor = "#1f2733"
            }
        }
    };

    private readonly string errorBarsRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Measured()"" Options=""_bottom"" />";
    private readonly string errorBarsCsharpCode = @"
private BitChartData Measured() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Response time (ms)"",
            Data = new() { 120, 138, 131, 156, 149, 162, 158 },
            BackgroundColor = ""rgba(54,162,235,0.55)"",
            BorderColor = ""#36a2eb"",
            BorderRadius = 4,
            // A number is a symmetric interval; BitChartErrorBar(minus, plus) is asymmetric;
            // null leaves that value without a whisker.
            ErrorData = [12, 9, 15, new BitChartErrorBar(6, 24), 11, null, 8],
            ErrorBarColor = ""#1f2733""
        }
    }
};";

    // Each step is a signed change, except the two totals which are drawn from the axis.
    private static readonly (string Label, double Step, bool IsTotal)[] WaterfallSteps =
    [
        ("Opening", 120, true),
        ("New", 46, false),
        ("Upsell", 18, false),
        ("Churn", -32, false),
        ("Discounts", -11, false),
        ("Expansion", 24, false),
        ("Closing", 0, true)
    ];

    private readonly BitChartOptions _waterfall = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Display = false },
            Tooltip = new BitChartTooltipOptions
            {
                Callbacks = new BitChartTooltipCallbacks
                {
                    Label = item =>
                    {
                        var (label, step, isTotal) = WaterfallSteps[item.DataIndex];
                        return isTotal ? $"{label}: {WaterfallTotalAt(item.DataIndex):N0}"
                                       : $"{label}: {step:+#,##0;-#,##0;0}";
                    }
                }
            }
        }
    };

    /// <summary>The running total once the given step has been applied.</summary>
    private static double WaterfallTotalAt(int index)
    {
        double total = 0;
        for (int i = 0; i <= index; i++) total += WaterfallSteps[i].Step;
        return total;
    }

    private BitChartData Waterfall()
    {
        var ranges = new List<(double Low, double High)?>();
        double running = 0;
        foreach (var (_, step, isTotal) in WaterfallSteps)
        {
            if (isTotal)
            {
                running += step;
                ranges.Add((0, running));
                continue;
            }
            double next = running + step;
            ranges.Add((Math.Min(running, next), Math.Max(running, next)));
            running = next;
        }

        return new BitChartData
        {
            Labels = WaterfallSteps.Select(s => s.Label).ToList(),
            Datasets =
            {
                new BitChartDataset
                {
                    Label = "MRR",
                    RangeData = ranges,
                    BorderRadius = 3,
                    BackgroundColorFn = ctx =>
                    {
                        var (_, step, isTotal) = WaterfallSteps[ctx.DataIndex];
                        return isTotal ? "#6b7785" : step >= 0 ? "#2ecc71" : "#ff6384";
                    }
                }
            }
        };
    }

    private readonly string waterfallRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Waterfall()"" Options=""_waterfall"" />";
    private readonly string waterfallCsharpCode = @"
// Each step is a signed change, except the two totals which are drawn from the axis.
private static readonly (string Label, double Step, bool IsTotal)[] Steps =
[
    (""Opening"", 120, true),
    (""New"", 46, false),
    (""Upsell"", 18, false),
    (""Churn"", -32, false),
    (""Discounts"", -11, false),
    (""Expansion"", 24, false),
    (""Closing"", 0, true)
];

private readonly BitChartOptions _waterfall = new()
{
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Display = false },
        Tooltip = new BitChartTooltipOptions
        {
            Callbacks = new BitChartTooltipCallbacks
            {
                Label = item =>
                {
                    var (label, step, isTotal) = Steps[item.DataIndex];
                    return isTotal ? $""{label}: {WaterfallTotalAt(item.DataIndex):N0}""
                                   : $""{label}: {step:+#,##0;-#,##0;0}"";
                }
            }
        }
    }
};

// The running total once the given step has been applied.
private static double WaterfallTotalAt(int index)
{
    double total = 0;
    for (int i = 0; i <= index; i++) total += Steps[i].Step;
    return total;
}

private BitChartData Waterfall()
{
    // RangeData floats each bar between the running total before and after its step.
    var ranges = new List<(double Low, double High)?>();
    double running = 0;
    foreach (var (_, step, isTotal) in Steps)
    {
        if (isTotal) { running += step; ranges.Add((0, running)); continue; }
        double next = running + step;
        ranges.Add((Math.Min(running, next), Math.Max(running, next)));
        running = next;
    }

    return new BitChartData
    {
        Labels = Steps.Select(s => s.Label).ToList(),
        Datasets =
        {
            new BitChartDataset
            {
                Label = ""MRR"",
                RangeData = ranges,
                BorderRadius = 3,
                BackgroundColorFn = ctx =>
                {
                    var (_, step, isTotal) = Steps[ctx.DataIndex];
                    return isTotal ? ""#6b7785"" : step >= 0 ? ""#2ecc71"" : ""#ff6384"";
                }
            }
        }
    };
}";
}
