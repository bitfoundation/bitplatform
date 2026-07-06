namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartMultiAxisDemo
{
    private readonly BitChartOptions _combo = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
        Scales =
        {
            ["y"] = new BitChartScaleOptions
            {
                Id = "y", Type = BitChartScaleType.Linear, Position = BitChartPosition.Left, BeginAtZero = true,
                Title = new BitChartScaleTitleOptions { Display = true, Text = "Rainfall (mm)" }
            },
            ["y2"] = new BitChartScaleOptions
            {
                Id = "y2", Type = BitChartScaleType.Linear, Position = BitChartPosition.Right, BeginAtZero = true,
                Title = new BitChartScaleTitleOptions { Display = true, Text = "Temp (°C)" },
                Grid = new BitChartGridOptions { DrawOnChartArea = false },
                Ticks = new BitChartTickOptions { Suffix = "°" }
            }
        }
    };

    private readonly BitChartOptions _dual = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
        Scales =
        {
            ["y"] = new BitChartScaleOptions
            {
                Id = "y", Type = BitChartScaleType.Linear, Position = BitChartPosition.Left,
                Title = new BitChartScaleTitleOptions { Display = true, Text = "Users" }
            },
            ["y2"] = new BitChartScaleOptions
            {
                Id = "y2", Type = BitChartScaleType.Linear, Position = BitChartPosition.Right,
                Title = new BitChartScaleTitleOptions { Display = true, Text = "Latency (ms)" },
                Grid = new BitChartGridOptions { DrawOnChartArea = false }
            }
        }
    };

    private BitChartData DualLine() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Active users", Data = BitChartSampleData.V(1200, 1900, 1700, 2400, 2200, 3000, 2800),
                BorderColor = "#36a2eb", BackgroundColor = "#36a2eb", Tension = 0.4, YAxisID = "y" },
            new BitChartDataset { Label = "Latency", Data = BitChartSampleData.V(120, 95, 110, 80, 90, 70, 65),
                BorderColor = "#ff9f40", BackgroundColor = "#ff9f40", Tension = 0.4, YAxisID = "y2", BorderDash = new() { 5, 4 } }
        }
    };

    private readonly BitChartOptions _dualX = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom, Labels = new BitChartLegendLabelOptions { UsePointStyle = true } } },
        Scales =
        {
            ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Linear, Position = BitChartPosition.Bottom,
                Title = new BitChartScaleTitleOptions { Display = true, Text = "Wavelength (nm)" } },
            ["x2"] = new BitChartScaleOptions { Id = "x2", Type = BitChartScaleType.Linear, Position = BitChartPosition.Top,
                Title = new BitChartScaleTitleOptions { Display = true, Text = "Frequency (THz)" },
                Grid = new BitChartGridOptions { DrawOnChartArea = false } },
            ["y"] = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Linear, Grace = 0.1 }
        }
    };

    private BitChartData DualX() => new()
    {
        Datasets =
        {
            new BitChartDataset
            {
                Label = "Visible", BackgroundColor = "#36a2eb", XAxisID = "x", PointRadius = 6,
                Points = new() { new(450, 20), new(520, 45), new(580, 60), new(640, 38), new(700, 25) }
            },
            new BitChartDataset
            {
                Label = "Frequency band", BackgroundColor = "#ff6384", XAxisID = "x2", PointStyle = BitChartPointStyle.Triangle, PointRadius = 6,
                Points = new() { new(430, 30), new(500, 55), new(600, 40), new(660, 50) }
            }
        }
    };


    private readonly string comboRazorCode = @"@* No top-level Type: each dataset declares its own (Bar/Line). *@
<BitChart Data=""TempAndRainfall()"" Options=""_combo"" />";
    private readonly string comboCsharpCode = @"
private readonly BitChartOptions _combo = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
    Scales =
    {
        [""y""] = new BitChartScaleOptions
        {
            Id = ""y"", Type = BitChartScaleType.Linear, Position = BitChartPosition.Left, BeginAtZero = true,
            Title = new BitChartScaleTitleOptions { Display = true, Text = ""Rainfall (mm)"" }
        },
        [""y2""] = new BitChartScaleOptions
        {
            Id = ""y2"", Type = BitChartScaleType.Linear, Position = BitChartPosition.Right, BeginAtZero = true,
            Title = new BitChartScaleTitleOptions { Display = true, Text = ""Temp (°C)"" },
            Grid = new BitChartGridOptions { DrawOnChartArea = false },
            Ticks = new BitChartTickOptions { Suffix = ""°"" }
        }
    }
};

private BitChartData TempAndRainfall() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Rainfall (mm)"", Type = BitChartType.Bar, Data = new() { 60, 48, 80, 35, 55, 20, 15 },
            BackgroundColor = ""rgba(54,162,235,0.5)"", BorderColor = ""#36a2eb"", YAxisID = ""y"", Order = 1 },
        new BitChartDataset { Label = ""Temp (°C)"", Type = BitChartType.Line, Data = new() { 7, 9, 12, 16, 20, 24, 27 },
            BorderColor = ""#ff6384"", BackgroundColor = ""#ff6384"", Tension = 0.4, YAxisID = ""y2"", Order = 0 }
    }
};";

    private readonly string dualLineRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""DualLine()"" Options=""_dual"" />";
    private readonly string dualLineCsharpCode = @"
private readonly BitChartOptions _dual = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
    Scales =
    {
        [""y""] = new BitChartScaleOptions
        {
            Id = ""y"", Type = BitChartScaleType.Linear, Position = BitChartPosition.Left,
            Title = new BitChartScaleTitleOptions { Display = true, Text = ""Users"" }
        },
        [""y2""] = new BitChartScaleOptions
        {
            Id = ""y2"", Type = BitChartScaleType.Linear, Position = BitChartPosition.Right,
            Title = new BitChartScaleTitleOptions { Display = true, Text = ""Latency (ms)"" },
            Grid = new BitChartGridOptions { DrawOnChartArea = false }
        }
    }
};

private BitChartData DualLine() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Active users"", Data = new() { 1200, 1900, 1700, 2400, 2200, 3000, 2800 },
            BorderColor = ""#36a2eb"", BackgroundColor = ""#36a2eb"", Tension = 0.4, YAxisID = ""y"" },
        new BitChartDataset { Label = ""Latency"", Data = new() { 120, 95, 110, 80, 90, 70, 65 },
            BorderColor = ""#ff9f40"", BackgroundColor = ""#ff9f40"", Tension = 0.4, YAxisID = ""y2"", BorderDash = new() { 5, 4 } }
    }
};";

    private readonly string dualXRazorCode = @"<BitChart Type=""BitChartType.Scatter"" Data=""DualX()"" Options=""_dualX"" />";
    private readonly string dualXCsharpCode = @"
private readonly BitChartOptions _dualX = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom, Labels = new BitChartLegendLabelOptions { UsePointStyle = true } } },
    Scales =
    {
        [""x""] = new BitChartScaleOptions { Id = ""x"", Type = BitChartScaleType.Linear, Position = BitChartPosition.Bottom,
            Title = new BitChartScaleTitleOptions { Display = true, Text = ""Wavelength (nm)"" } },
        [""x2""] = new BitChartScaleOptions { Id = ""x2"", Type = BitChartScaleType.Linear, Position = BitChartPosition.Top,
            Title = new BitChartScaleTitleOptions { Display = true, Text = ""Frequency (THz)"" },
            Grid = new BitChartGridOptions { DrawOnChartArea = false } },
        [""y""] = new BitChartScaleOptions { Id = ""y"", Type = BitChartScaleType.Linear, Grace = 0.1 }
    }
};

private BitChartData DualX() => new()
{
    Datasets =
    {
        new BitChartDataset
        {
            Label = ""Visible"", BackgroundColor = ""#36a2eb"", XAxisID = ""x"", PointRadius = 6,
            Points = new() { new(450, 20), new(520, 45), new(580, 60), new(640, 38), new(700, 25) }
        },
        new BitChartDataset
        {
            Label = ""Frequency band"", BackgroundColor = ""#ff6384"", XAxisID = ""x2"", PointStyle = BitChartPointStyle.Triangle, PointRadius = 6,
            Points = new() { new(430, 30), new(500, 55), new(600, 40), new(660, 50) }
        }
    }
};";
}
