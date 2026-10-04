namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartCustomizationDemo
{
    private readonly BitChartData _data = new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Visitors", Data = BitChartSampleData.V(120, 190, 160, 250, 220, 300, 280), Tension = 0.4 },
            new BitChartDataset { Label = "Signups", Data = BitChartSampleData.V(40, 70, 55, 90, 85, 120, 110), Tension = 0.4 }
        }
    };

    private readonly BitChartOptions _options = new()
    {
        Interaction = new BitChartInteractionOptions { Mode = BitChartInteractionMode.Index },
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
    };

    private const string _brandStyle =
        "--bit-Chart-series-color-1: #7c3aed;" +
        "--bit-Chart-series-color-2: #f59e0b;" +
        "--bit-Chart-grid-color: rgba(124, 58, 237, 0.15);" +
        "--bit-Chart-tick-color: #7c3aed;" +
        "--bit-Chart-tooltip-background: #2e1065;" +
        "--bit-Chart-tooltip-color: #f5f3ff;" +
        "--bit-Chart-tooltip-radius: 12px;" +
        "--bit-Chart-font-family: Georgia, serif;";

    // One shared options instance: the renderer never writes back into it, so every chart can read it.
    private static readonly BitChartOptions _sharedOptions = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
    };

    private readonly BitChartParams[] _chartParams =
    [
        new()
        {
            Type = BitChartType.Bar,
            Options = _sharedOptions,
            Height = "160px",
            Styles = new() { Root = "--bit-Chart-series-color-1: #0f766e; padding: 0.5rem; border-radius: 6px; background: var(--bit-clr-bg-sec);" }
        }
    ];

    private readonly BitChartData _north = Region("North", 42, 51, 48, 60);
    private readonly BitChartData _south = Region("South", 35, 39, 46, 52);
    private readonly BitChartData _west = Region("West", 28, 33, 41, 38);

    private static BitChartData Region(string label, params double?[] values) => new()
    {
        Labels = { "Q1", "Q2", "Q3", "Q4" },
        Datasets = { new BitChartDataset { Label = label, Data = [.. values] } }
    };

    private readonly BitChartOptions _titled = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Title = new BitChartTitleOptions { Display = true, Text = "Traffic" },
            Subtitle = new BitChartTitleOptions { Display = true, Text = "First half of the year" },
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom }
        }
    };

    private readonly BitChartClassStyles _styles = new()
    {
        Root = "padding: 1rem; border-radius: 8px; border: 1px solid var(--bit-clr-brd-sec);",
        Title = "text-transform: uppercase; letter-spacing: 0.08em;",
        Subtitle = "font-style: italic;",
        LegendItem = "border: 1px solid var(--bit-clr-brd-sec); border-radius: 999px; padding: 2px 10px;",
        Tooltip = "border: 1px solid var(--bit-clr-brd-sec);"
    };


    private readonly string cssVariablesRazorCode = @"
<BitChart Type=""BitChartType.Line"" Data=""_data"" Options=""_options"" Style=""@_brandStyle"" />";
    private readonly string cssVariablesCsharpCode = @"
private const string _brandStyle =
    ""--bit-Chart-series-color-1: #7c3aed;"" +
    ""--bit-Chart-series-color-2: #f59e0b;"" +
    ""--bit-Chart-grid-color: rgba(124, 58, 237, 0.15);"" +
    ""--bit-Chart-tick-color: #7c3aed;"" +
    ""--bit-Chart-tooltip-background: #2e1065;"" +
    ""--bit-Chart-tooltip-color: #f5f3ff;"" +
    ""--bit-Chart-tooltip-radius: 12px;"" +
    ""--bit-Chart-font-family: Georgia, serif;"";

private readonly BitChartOptions _options = new()
{
    Interaction = new BitChartInteractionOptions { Mode = BitChartInteractionMode.Index },
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};

// No colors in the data: the series take the palette, which the variables re-skin.
private readonly BitChartData _data = new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Visitors"", Data = { 120, 190, 160, 250, 220, 300, 280 }, Tension = 0.4 },
        new BitChartDataset { Label = ""Signups"", Data = { 40, 70, 55, 90, 85, 120, 110 }, Tension = 0.4 }
    }
};";

    private readonly string cascadingRazorCode = @"
<BitParams Parameters=""@_chartParams"">
    <BitChart Data=""_north"" AriaLabel=""North"" />
    <BitChart Data=""_south"" AriaLabel=""South"" />
    <BitChart Data=""_west"" AriaLabel=""West"" Type=""BitChartType.Line"" />
</BitParams>";
    private readonly string cascadingCsharpCode = @"
// One shared options instance: the renderer never writes back into it, so every chart can read it.
private static readonly BitChartOptions _sharedOptions = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Display = false } }
};

private readonly BitChartParams[] _chartParams =
[
    new()
    {
        Type = BitChartType.Bar,
        Options = _sharedOptions,
        Height = ""160px"",
        Styles = new() { Root = ""--bit-Chart-series-color-1: #0f766e; padding: 0.5rem; border-radius: 6px; background: var(--bit-clr-bg-sec);"" }
    }
];

private readonly BitChartData _north = Region(""North"", 42, 51, 48, 60);
private readonly BitChartData _south = Region(""South"", 35, 39, 46, 52);
private readonly BitChartData _west = Region(""West"", 28, 33, 41, 38);

private static BitChartData Region(string label, params double?[] values) => new()
{
    Labels = { ""Q1"", ""Q2"", ""Q3"", ""Q4"" },
    Datasets = { new BitChartDataset { Label = label, Data = [.. values] } }
};";

    private readonly string styleClassRazorCode = @"
<style>
    .custom-chart {
        background: linear-gradient(180deg, var(--bit-clr-bg-sec), transparent);
    }
</style>

<BitChart Type=""BitChartType.Bar""
          Data=""_data""
          Options=""_titled""
          Class=""custom-chart""
          Styles=""@_styles"" />";
    private readonly string styleClassCsharpCode = @"
private readonly BitChartOptions _titled = new()
{
    Plugins = new BitChartPluginOptions
    {
        Title = new BitChartTitleOptions { Display = true, Text = ""Traffic"" },
        Subtitle = new BitChartTitleOptions { Display = true, Text = ""First half of the year"" },
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom }
    }
};

private readonly BitChartClassStyles _styles = new()
{
    Root = ""padding: 1rem; border-radius: 8px; border: 1px solid var(--bit-clr-brd-sec);"",
    Title = ""text-transform: uppercase; letter-spacing: 0.08em;"",
    Subtitle = ""font-style: italic;"",
    LegendItem = ""border: 1px solid var(--bit-clr-brd-sec); border-radius: 999px; padding: 2px 10px;"",
    Tooltip = ""border: 1px solid var(--bit-clr-brd-sec);""
};

private readonly BitChartData _data = new()
{
    Labels = { ""Jan"", ""Feb"", ""Mar"", ""Apr"", ""May"", ""Jun"", ""Jul"" },
    Datasets =
    {
        new BitChartDataset { Label = ""Visitors"", Data = { 120, 190, 160, 250, 220, 300, 280 }, Tension = 0.4 },
        new BitChartDataset { Label = ""Signups"", Data = { 40, 70, 55, 90, 85, 120, 110 }, Tension = 0.4 }
    }
};";
}
