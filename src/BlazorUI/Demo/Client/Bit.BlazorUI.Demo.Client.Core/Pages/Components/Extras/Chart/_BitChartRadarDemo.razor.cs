namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartRadarDemo
{
    private readonly BitChartOptions _bottom = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
    };

    private readonly BitChartOptions _circular = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
        Scales =
        {
            ["r"] = new BitChartScaleOptions
            {
                Id = "r", Type = BitChartScaleType.RadialLinear, BeginAtZero = true,
                Grid = new BitChartGridOptions { Circular = true },
                AngleLineColor = "rgba(0,0,0,0.15)",
                PointLabels = new BitChartPointLabelOptions { Color = "#36a2eb", Font = new BitChartFont { Size = 12, Weight = "bold" } }
            }
        }
    };

    private BitChartData Outline() => new()
    {
        Labels = { "HTML", "CSS", "JS", "C#", "SQL", "DevOps" },
        Datasets =
        {
            new BitChartDataset { Label = "Team A", Data = BitChartSampleData.V(90, 75, 80, 95, 60, 70), BorderColor = "#36a2eb", Fill = BitChartFillMode.None, BorderWidth = 2 },
            new BitChartDataset { Label = "Team B", Data = BitChartSampleData.V(70, 85, 65, 60, 90, 80), BorderColor = "#ff9f40", Fill = BitChartFillMode.None, BorderWidth = 2 }
        }
    };

    private BitChartData Skip() => new()
    {
        Labels = { "Speed", "Power", "Range", "Defense", "Agility", "Stamina" },
        Datasets =
        {
            new BitChartDataset { Label = "Scouted", Data = BitChartSampleData.V(80, 65, null, 60, 85, null), BorderColor = "#9966ff", Fill = BitChartFillMode.Origin }
        }
    };


    private readonly string twoPlayersRazorCode = @"<BitChart Type=""BitChartType.Radar"" Data=""Skills()"" Options=""_bottom"" />";
    private readonly string twoPlayersCsharpCode = @"
private readonly BitChartOptions _bottom = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

private BitChartData Skills() => new()
{
    Labels = { ""Speed"", ""Power"", ""Range"", ""Defense"", ""Agility"", ""Stamina"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Player 1"", Data = new() { 80, 65, 70, 60, 85, 75 },
            BorderColor = ""#36a2eb"", Fill = BitChartFillMode.Origin },
        new BitChartDataset { Label = ""Player 2"", Data = new() { 55, 80, 60, 90, 50, 65 },
            BorderColor = ""#ff6384"", Fill = BitChartFillMode.Origin }
    }
};";

    private readonly string outlineRazorCode = @"<BitChart Type=""BitChartType.Radar"" Data=""Outline()"" Options=""_bottom"" />";
    private readonly string outlineCsharpCode = @"
private readonly BitChartOptions _bottom = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

private BitChartData Outline() => new()
{
    Labels = { ""HTML"", ""CSS"", ""JS"", ""C#"", ""SQL"", ""DevOps"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Team A"", Data = new() { 90, 75, 80, 95, 60, 70 }, BorderColor = ""#36a2eb"",
            Fill = BitChartFillMode.None, BorderWidth = 2 },
        new BitChartDataset { Label = ""Team B"", Data = new() { 70, 85, 65, 60, 90, 80 }, BorderColor = ""#ff9f40"",
            Fill = BitChartFillMode.None, BorderWidth = 2 }
    }
};";

    private readonly string circularRazorCode = @"<BitChart Type=""BitChartType.Radar"" Data=""Skills()"" Options=""_circular"" />";
    private readonly string circularCsharpCode = @"
private readonly BitChartOptions _circular = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
    Scales =
    {
        [""r""] = new BitChartScaleOptions
        {
            Id = ""r"", Type = BitChartScaleType.RadialLinear, BeginAtZero = true,
            Grid = new BitChartGridOptions { Circular = true },
            AngleLineColor = ""rgba(0,0,0,0.15)"",
            PointLabels = new BitChartPointLabelOptions { Color = ""#36a2eb"", Font = new BitChartFont { Size = 12, Weight = ""bold"" } }
        }
    }
};
// Skills(): Speed/Power/Range/Defense/Agility/Stamina for Player 1 & 2";

    private readonly string skipRazorCode = @"<BitChart Type=""BitChartType.Radar"" Data=""Skip()"" Options=""_bottom"" />";
    private readonly string skipCsharpCode = @"
private readonly BitChartOptions _bottom = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

// null values are skipped, not treated as zero
private BitChartData Skip() => new()
{
    Labels = { ""Speed"", ""Power"", ""Range"", ""Defense"", ""Agility"", ""Stamina"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Scouted"", Data = new() { 80, 65, null, 60, 85, null },
            BorderColor = ""#9966ff"", Fill = BitChartFillMode.Origin }
    }
};";
}
