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
}
