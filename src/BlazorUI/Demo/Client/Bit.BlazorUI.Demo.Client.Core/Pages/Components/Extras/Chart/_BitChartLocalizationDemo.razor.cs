using System.Globalization;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartLocalizationDemo
{
    private string _culture = "de-DE";

    private BitChartOptions CultureOptions() => new()
    {
        Culture = CultureInfo.GetCultureInfo(_culture),
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
    };

    private readonly BitChartOptions _currency = new()
    {
        Culture = CultureInfo.GetCultureInfo("en-US"),
        Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
        Scales = { ["y"] = new BitChartScaleOptions { Id = "y", Ticks = new BitChartTickOptions { Format = "C0" } } }
    };

    private readonly BitChartOptions _rtl = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Title = new BitChartTitleOptions { Display = true, Text = "فروش فصلی" },
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom }
        },
        Scales = { ["x"] = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Category, Reverse = true } }
    };

    private BitChartData Revenue() => new()
    {
        Labels = BitChartSampleData.Months.ToList(),
        Datasets =
        {
            new BitChartDataset { Label = "Revenue", Data = BitChartSampleData.V(12500.5, 19300.25, 14100, 22800.75, 18250, 25400, 20900),
                BackgroundColor = "#36a2eb", BorderColor = "#36a2eb", Tension = 0.3 }
        }
    };

    private BitChartData Persian() => new()
    {
        Labels = { "بهار", "تابستان", "پاییز", "زمستان" },
        Datasets =
        {
            new BitChartDataset { Label = "درآمد", Data = BitChartSampleData.V(120, 190, 160, 250), BackgroundColor = "#4bc0c0", BorderRadius = 4 },
            new BitChartDataset { Label = "هزینه", Data = BitChartSampleData.V(80, 120, 110, 150), BackgroundColor = "#ff9f40", BorderRadius = 4 }
        }
    };


    private readonly string cultureRazorCode = @"<BitChart Type=""BitChartType.Bar"" Data=""Revenue()"" Options=""CultureOptions()"" />";
    private readonly string cultureCsharpCode = @"
private string _culture = ""de-DE"";

private BitChartOptions CultureOptions() => new()
{
    Culture = CultureInfo.GetCultureInfo(_culture),
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } }
};";

    private readonly string currencyRazorCode = @"<BitChart Type=""BitChartType.Line"" Data=""Revenue()"" Options=""_currency"" />";
    private readonly string currencyCsharpCode = @"
private readonly BitChartOptions _currency = new()
{
    Culture = CultureInfo.GetCultureInfo(""en-US""),
    Plugins = new BitChartPluginOptions { Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom } },
    Scales = { [""y""] = new BitChartScaleOptions { Id = ""y"", Ticks = new BitChartTickOptions { Format = ""C0"" } } }
};";

    private readonly string rtlRazorCode = @"<BitChart Dir=""BitDir.Rtl"" Type=""BitChartType.Bar"" Data=""Persian()"" Options=""_rtl"" />";
    private readonly string rtlCsharpCode = @"
private readonly BitChartOptions _rtl = new()
{
    Plugins = new BitChartPluginOptions
    {
        Title = new BitChartTitleOptions { Display = true, Text = ""فروش فصلی"" },
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom }
    },
    Scales = { [""x""] = new BitChartScaleOptions { Id = ""x"", Type = BitChartScaleType.Category, Reverse = true } }
};

private BitChartData Persian() => new()
{
    Labels = { ""بهار"", ""تابستان"", ""پاییز"", ""زمستان"" },
    Datasets =
    {
        new BitChartDataset { Label = ""درآمد"", Data = new() { 120, 190, 160, 250 }, BackgroundColor = ""#4bc0c0"", BorderRadius = 4 },
        new BitChartDataset { Label = ""هزینه"", Data = new() { 80, 120, 110, 150 }, BackgroundColor = ""#ff9f40"", BorderRadius = 4 }
    }
};";
}
