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

    private BitChart? _imageChart;
    private string? _dataUrl;
    private string? _markup;

    private readonly BitChartOptions _imageOptions = new()
    {
        CutoutPercentage = 55,
        Plugins = new BitChartPluginOptions
        {
            Title = new BitChartTitleOptions { Display = true, Text = "Traffic by source" },
            Legend = new BitChartLegendOptions { Position = BitChartPosition.Right }
        }
    };

    private async Task ShowImage()
    {
        if (_imageChart is null) return;
        _dataUrl = await _imageChart.ToBase64ImageAsync(scale: 1);
    }

    private async Task ShowMarkup()
    {
        if (_imageChart is null) return;
        _markup = await _imageChart.ToSvgStringAsync();
    }


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

    private readonly string imageRazorCode = @"<BitButton Variant=""BitVariant.Outline"" OnClick=""ShowImage"">Render to a data URL</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""ShowMarkup"">Show the SVG markup</BitButton>

@if (_dataUrl is not null)
{
    <img src=""@_dataUrl"" alt=""A snapshot of the chart"" />
}
@if (_markup is not null)
{
    <pre>@_markup</pre>
}

<BitChart @ref=""_imageChart"" Type=""BitChartType.Doughnut"" Data=""Traffic()"" Options=""_imageOptions"" />";
    private readonly string imageCsharpCode = @"
private BitChart? _imageChart;
private string? _dataUrl;
private string? _markup;

// scale 1 matches the on-screen size; pass 2 for a high-density snapshot.
private async Task ShowImage() => _dataUrl = await _imageChart!.ToBase64ImageAsync(scale: 1);

// The theme tokens the chart references are resolved into the markup, so it stands alone.
private async Task ShowMarkup() => _markup = await _imageChart!.ToSvgStringAsync();";
}
