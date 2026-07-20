namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartAnnotationsDemo
{
    private BitChartOptions _lines = default!;
    private BitChartOptions _box = default!;

    protected override void OnInitialized()
    {
        _lines = new BitChartOptions
        {
            Plugins = new BitChartPluginOptions
            {
                Legend = new BitChartLegendOptions { Display = false },
                Custom =
                {
                    new BitChartAnnotationPlugin(
                        new BitChartAnnotation { Orientation = BitChartLineOrientation.Horizontal, Value = 80, Color = "#2ecc71", Dash = new() { 6, 4 }, Label = "Target" },
                        new BitChartAnnotation { Kind = BitChartAnnotationKind.Box, YMin = 0, YMax = 30, Color = "#ff6384", FillColor = "rgba(255,99,132,0.10)", LineWidth = 0, Label = "Low", DrawBehindDatasets = true }
                    )
                }
            }
        };

        _box = new BitChartOptions
        {
            Plugins = new BitChartPluginOptions
            {
                Legend = new BitChartLegendOptions { Display = false },
                Custom =
                {
                    new BitChartAnnotationPlugin(
                        new BitChartAnnotation { Kind = BitChartAnnotationKind.Box, XIsIndex = true, XMin = 2, XMax = 4, Color = "#9966ff", FillColor = "rgba(153,102,255,0.12)", LineWidth = 1, DrawBehindDatasets = true },
                        new BitChartAnnotation { Orientation = BitChartLineOrientation.Vertical, XIsIndex = true, Value = 3, Color = "#ff9f40", LineWidth = 2, Label = "Launch" }
                    )
                }
            }
        };
    }

    private BitChartData Series() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets = { new BitChartDataset { Label = "Score", Data = BitChartSampleData.V(20, 45, 60, 35, 75, 90, 70), BorderColor = "#36a2eb", Tension = 0.4 } }
    };

    private BitChartData Bars() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets = { new BitChartDataset { Label = "Sales", Data = BitChartSampleData.V(30, 42, 55, 70, 64, 48, 52), BackgroundColor = "#36a2eb" } }
    };


    private readonly string linesRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Series()"" Options=""_lines"" />";
    private readonly string linesCsharpCode = @"
private BitChartOptions _lines = default!;

protected override void OnInitialized()
{
    _lines = new BitChartOptions
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Display = false },
            Custom =
            {
                new BitChartAnnotationPlugin(
                    new BitChartAnnotation { Orientation = BitChartLineOrientation.Horizontal, Value = 80,
                        Color = ""#2ecc71"", Dash = new() { 6, 4 }, Label = ""Target"" },
                    new BitChartAnnotation { Kind = BitChartAnnotationKind.Box, YMin = 0, YMax = 30,
                        Color = ""#ff6384"", FillColor = ""rgba(255,99,132,0.10)"", LineWidth = 0, Label = ""Low"", DrawBehindDatasets = true }
                )
            }
        }
    };
}

private BitChartData Series() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets = { new BitChartDataset { Label = ""Score"", Data = new() { 20, 45, 60, 35, 75, 90, 70 }, BorderColor = ""#36a2eb"", Tension = 0.4 } }
};";

    private readonly string boxRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Bars()"" Options=""_box"" />";
    private readonly string boxCsharpCode = @"
private BitChartOptions _box = default!;

protected override void OnInitialized()
{
    _box = new BitChartOptions
    {
        Plugins = new BitChartPluginOptions
        {
            Legend = new BitChartLegendOptions { Display = false },
            Custom =
            {
                new BitChartAnnotationPlugin(
                    new BitChartAnnotation { Kind = BitChartAnnotationKind.Box, XIsIndex = true, XMin = 2, XMax = 4,
                        Color = ""#9966ff"", FillColor = ""rgba(153,102,255,0.12)"", LineWidth = 1, DrawBehindDatasets = true },
                    new BitChartAnnotation { Orientation = BitChartLineOrientation.Vertical, XIsIndex = true, Value = 3,
                        Color = ""#ff9f40"", LineWidth = 2, Label = ""Launch"" }
                )
            }
        }
    };
}

private BitChartData Bars() => new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets = { new BitChartDataset { Label = ""Sales"", Data = new() { 30, 42, 55, 70, 64, 48, 52 }, BackgroundColor = ""#36a2eb"" } }
};";
}
