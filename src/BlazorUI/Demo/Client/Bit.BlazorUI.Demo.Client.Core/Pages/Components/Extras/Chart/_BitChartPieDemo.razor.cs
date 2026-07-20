namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartPieDemo
{
    private readonly BitChartOptions _right = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Right } }
    };

    private readonly BitChartOptions _doughnut = new()
    {
        CutoutPercentage = 60,
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Right } }
    };

    private readonly BitChartOptions _gauge = new()
    {
        CutoutPercentage = 65,
        CircumferenceDegrees = 180,
        RotationDegrees = -90,
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
    };

    private readonly BitChartOptions _centerText = new()
    {
        CutoutPercentage = 68,
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Right },
            Custom = { new BitChartCenterTextPlugin("1,340", "sessions") }
        }
    };

    private BitChartData Gauge() => new()
    {
        Labels = { "Used", "Free" },
        Datasets =
        {
            new BitChartDataset { Data = BitChartSampleData.V(68, 32), BackgroundColors = new() { "#ff6384", "#e6e9ef" } }
        }
    };

    private BitChartData MultiRing() => new()
    {
        Labels = { "Mobile", "Desktop", "Tablet" },
        Datasets =
        {
            new BitChartDataset { Label = "2025", Data = BitChartSampleData.V(55, 35, 10) },
            new BitChartDataset { Label = "2026", Data = BitChartSampleData.V(62, 28, 10) }
        }
    };


    private readonly string pieRazorCode = @"<BitChart Type=""BitChartType.Pie"" Data=""Traffic()"" Options=""_right"" />";
    private readonly string pieCsharpCode = @"
private readonly BitChartOptions _right = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Right } }
};

private BitChartData Traffic() => new()
{
    Labels = { ""Direct"", ""Organic"", ""Referral"", ""Social"", ""Email"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Sessions"", Data = new() { 300, 500, 180, 240, 120 } }
    }
};";

    private readonly string doughnutRazorCode = @"<BitChart Type=""BitChartType.Doughnut"" Data=""Traffic()"" Options=""_doughnut"" />";
    private readonly string doughnutCsharpCode = @"
private readonly BitChartOptions _doughnut = new()
{
    CutoutPercentage = 60,
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Right } }
};
// Traffic(): Direct/Organic/Referral/Social/Email = 300/500/180/240/120";

    private readonly string gaugeRazorCode = @"<BitChart Type=""BitChartType.Doughnut"" Data=""Gauge()"" Options=""_gauge"" />";
    private readonly string gaugeCsharpCode = @"
private readonly BitChartOptions _gauge = new()
{
    CutoutPercentage = 65,
    CircumferenceDegrees = 180,
    RotationDegrees = -90,
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

private BitChartData Gauge() => new()
{
    Labels = { ""Used"", ""Free"" },
    Datasets =
    {
        new BitChartDataset { Data = new() { 68, 32 }, BackgroundColors = new() { ""#ff6384"", ""#e6e9ef"" } }
    }
};";

    private readonly string multiRingRazorCode = @"<BitChart Type=""BitChartType.Doughnut"" Data=""MultiRing()"" Options=""_right"" />";
    private readonly string multiRingCsharpCode = @"
private readonly BitChartOptions _right = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Right } }
};

private BitChartData MultiRing() => new()
{
    Labels = { ""Mobile"", ""Desktop"", ""Tablet"" },
    Datasets =
    {
        new BitChartDataset { Label = ""2025"", Data = new() { 55, 35, 10 } },
        new BitChartDataset { Label = ""2026"", Data = new() { 62, 28, 10 } }
    }
};";

    private readonly string centerTextRazorCode = @"<BitChart Type=""BitChartType.Doughnut"" Data=""Traffic()"" Options=""_centerText"" />";
    private readonly string centerTextCsharpCode = @"
private readonly BitChartOptions _centerText = new()
{
    CutoutPercentage = 68,
    Plugins = new BitChartPluginOptions
    {
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Right },
        Custom = { new BitChartCenterTextPlugin(""1,340"", ""sessions"") }
    }
};
// Traffic(): Direct/Organic/Referral/Social/Email = 300/500/180/240/120";
}
