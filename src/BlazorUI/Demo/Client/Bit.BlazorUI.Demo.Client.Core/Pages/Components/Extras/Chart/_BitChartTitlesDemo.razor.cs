namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartTitlesDemo
{
    private BitChartPosition _position = BitChartPosition.Left;
    private BitChartAlign _align = BitChartAlign.Center;

    private readonly BitChartOptions _titled = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Title = new BitChartTitleOptions { Display = true, Text = "Revenue by product" },
            Subtitle = new BitChartTitleOptions
            {
                Display = true,
                Text = "Thousands of euro, first seven months",
                Font = new BitChartFont { Size = 12 },
                Color = "var(--bit-clr-fg-sec)"
            },
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom }
        }
    };

    private BitChartOptions Placement() => new()
    {
        Plugins = new BitChartPluginOptions
        {
            Title = new BitChartTitleOptions { Display = true, Text = "Monthly sales", Position = _position, Align = _align },
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom }
        }
    };

    private readonly BitChartOptions _axisTitles = new()
    {
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
        Scales =
        {
            ["x"] = new BitChartScaleOptions
            {
                Id = "x", Type = BitChartScaleType.Category,
                Title = new BitChartScaleTitleOptions { Display = true, Text = "Month" }
            },
            ["y"] = new BitChartScaleOptions
            {
                Id = "y", Type = BitChartScaleType.Linear, BeginAtZero = true,
                Title = new BitChartScaleTitleOptions { Display = true, Text = "Units sold" }
            }
        }
    };


    private readonly string basicRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Revenue()"" Options=""_titled"" />";
    private readonly string basicCsharpCode = @"
private readonly BitChartOptions _titled = new()
{
    Plugins = new BitChartPluginOptions
    {
        Title = new BitChartTitleOptions { Display = true, Text = ""Revenue by product"" },
        Subtitle = new BitChartTitleOptions
        {
            Display = true,
            Text = ""Thousands of euro, first seven months"",
            Font = new BitChartFont { Size = 12 },
            Color = ""var(--bit-clr-fg-sec)""
        },
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom }
    }
};";

    private readonly string placementRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""MonthlySales()"" Options=""Placement()"" />";
    private readonly string placementCsharpCode = @"
private BitChartPosition _position = BitChartPosition.Left;
private BitChartAlign _align = BitChartAlign.Center;

private BitChartOptions Placement() => new()
{
    Plugins = new BitChartPluginOptions
    {
        Title = new BitChartTitleOptions { Display = true, Text = ""Monthly sales"", Position = _position, Align = _align },
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom }
    }
};";

    private readonly string axisRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""MonthlySales()"" Options=""_axisTitles"" />";
    private readonly string axisCsharpCode = @"
private readonly BitChartOptions _axisTitles = new()
{
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
    Scales =
    {
        [""x""] = new BitChartScaleOptions
        {
            Id = ""x"", Type = BitChartScaleType.Category,
            Title = new BitChartScaleTitleOptions { Display = true, Text = ""Month"" }
        },
        [""y""] = new BitChartScaleOptions
        {
            Id = ""y"", Type = BitChartScaleType.Linear, BeginAtZero = true,
            Title = new BitChartScaleTitleOptions { Display = true, Text = ""Units sold"" }
        }
    }
};";
}
