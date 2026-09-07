namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartExportDemo
{
    private BitChart? _chart;
    private BitChart? _csvChart;
    private string? _csv;

    private readonly BitChartOptions _options = new()
    {
        Plugins = new BitChartPluginOptions
        {
            Title = new BitChartTitleOptions { Display = true, Text = "Revenue by product" },
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom }
        }
    };

    private Task ExportSvg() => _chart?.ExportSvgAsync("revenue.svg") ?? Task.FromResult(false);

    private Task ExportPng() => _chart?.ExportPngAsync("revenue.png", scale: 2) ?? Task.FromResult(false);

    private Task ExportCsv() => _chart?.ExportCsvAsync("revenue.csv") ?? Task.FromResult(false);

    private void ShowCsv() => _csv = _csvChart?.ToCsv();


    private readonly string exportRazorCode = @"<BitButton Variant=""BitVariant.Outline"" OnClick=""ExportSvg"">Download SVG</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""ExportPng"">Download PNG</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""ExportCsv"">Download CSV</BitButton>

<BitChart @ref=""_chart"" Type=""BitChartType.Bar"" Data=""Revenue()"" Options=""_options"" />";
    private readonly string exportCsharpCode = @"
private BitChart? _chart;

private readonly BitChartOptions _options = new()
{
    Plugins = new BitChartPluginOptions
    {
        Title = new BitChartTitleOptions { Display = true, Text = ""Revenue by product"" },
        Legend = new BitChartLegendOptions { Position = BitChartPosition.Bottom }
    }
};

private Task ExportSvg() => _chart!.ExportSvgAsync(""revenue.svg"");

// scale 2 renders at twice the on-screen size, which stays crisp on high-density displays.
private Task ExportPng() => _chart!.ExportPngAsync(""revenue.png"", scale: 2);

private Task ExportCsv() => _chart!.ExportCsvAsync(""revenue.csv"");";

    private readonly string csvRazorCode = @"<BitButton Variant=""BitVariant.Outline"" OnClick=""ShowCsv"">Show the CSV</BitButton>

@if (_csv is not null)
{
    <pre>@_csv</pre>
}

<BitChart @ref=""_csvChart"" Type=""BitChartType.Line"" Data=""MonthlySales()"" Options=""_options"" />";
    private readonly string csvCsharpCode = @"
private BitChart? _csvChart;
private string? _csv;

private void ShowCsv() => _csv = _csvChart!.ToCsv();";
}
