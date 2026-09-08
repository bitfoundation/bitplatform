using System.Globalization;
using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Export helpers. The chart is already a live SVG element, so exporting is a matter of serializing it
/// (SVG), rasterizing that serialization (PNG), or writing the underlying values out (CSV).
/// </summary>
public partial class BitChart
{
    /// <summary>
    /// Downloads the chart as a standalone <c>.svg</c> file. The theme tokens the chart references are
    /// resolved into the exported file so it looks the same outside the app.
    /// </summary>
    /// <param name="fileName">File name to save as; defaults to <c>chart.svg</c>.</param>
    /// <param name="backgroundColor">Optional background painted behind the chart (SVG is transparent by default).</param>
    /// <returns>True when the file was produced.</returns>
    public async Task<bool> ExportSvgAsync(string? fileName = null, string? backgroundColor = null)
    {
        try
        {
            return await JS.BitChartExportSvg(_plotEl, fileName ?? "chart.svg", backgroundColor);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Downloads the chart as a <c>.png</c> image, rasterized from the live SVG.
    /// </summary>
    /// <param name="fileName">File name to save as; defaults to <c>chart.png</c>.</param>
    /// <param name="scale">Pixel ratio; 2 (the default) produces a crisp image on high-density displays.</param>
    /// <param name="backgroundColor">Background painted behind the chart; PNG is transparent without it.</param>
    /// <returns>True when the file was produced.</returns>
    public async Task<bool> ExportPngAsync(string? fileName = null, double scale = 2, string? backgroundColor = "#ffffff")
    {
        try
        {
            return await JS.BitChartExportPng(_plotEl, fileName ?? "chart.png", scale <= 0 ? 1 : scale, backgroundColor);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns the chart as standalone SVG markup instead of downloading it - for embedding it in a
    /// report, mailing it, or storing it - with the theme tokens it references resolved into the markup
    /// so it looks the same outside the app.
    /// </summary>
    /// <param name="backgroundColor">Optional background painted behind the chart (SVG is transparent by default).</param>
    /// <returns>The SVG markup, or null when the chart has not been rendered in a browser yet.</returns>
    public async Task<string?> ToSvgStringAsync(string? backgroundColor = null)
    {
        try
        {
            return await JS.BitChartToSvgString(_plotEl, backgroundColor);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns the chart as a rasterized <c>data:</c> URL - the same picture <see cref="ExportPngAsync"/>
    /// downloads - ready to drop into an <c>img</c> src or a PDF. Mirrors Chart.js's <c>toBase64Image</c>.
    /// </summary>
    /// <param name="mimeType">Image type to encode; <c>image/png</c> by default (<c>image/jpeg</c> and <c>image/webp</c> also work).</param>
    /// <param name="scale">Pixel ratio; 2 (the default) produces a crisp image on high-density displays.</param>
    /// <param name="backgroundColor">Background painted behind the chart; PNG is transparent without it.</param>
    /// <returns>The data URL, or null when the chart has not been rendered in a browser yet.</returns>
    public async Task<string?> ToBase64ImageAsync(string mimeType = "image/png", double scale = 2,
        string? backgroundColor = "#ffffff")
    {
        try
        {
            return await JS.BitChartToDataUrl(_plotEl, mimeType, scale <= 0 ? 1 : scale, backgroundColor);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Downloads the chart's data as a <c>.csv</c> file.</summary>
    /// <param name="fileName">File name to save as; defaults to <c>chart.csv</c>.</param>
    /// <returns>True when the file was produced.</returns>
    public async Task<bool> ExportCsvAsync(string? fileName = null)
    {
        try
        {
            await JS.BitChartDownloadText(fileName ?? "chart.csv", ToCsv(), "text/csv;charset=utf-8");
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Renders the chart's data as CSV. Value datasets become one row per series with a column per
    /// label; point datasets (scatter/bubble) become one row per point.
    /// </summary>
    public string ToCsv()
    {
        var data = _config.Data;
        var culture = Culture;
        var sb = new StringBuilder();

        if (HasPointData)
        {
            sb.AppendLine("Series,X,Y,R");
            foreach (var ds in data.Datasets)
            {
                if (ds.Points is not { } pts) continue;
                foreach (var p in pts)
                    sb.Append(Csv(ds.Label ?? "Series")).Append(',')
                      .Append(Csv(p.X.ToString(culture))).Append(',')
                      .Append(Csv(p.Y.ToString(culture))).Append(',')
                      .AppendLine(p.R is { } r ? Csv(r.ToString(culture)) : "");
            }
            return sb.ToString();
        }

        sb.Append("Series");
        foreach (var label in data.Labels) sb.Append(',').Append(Csv(label));
        sb.AppendLine();

        foreach (var ds in data.Datasets)
        {
            sb.Append(Csv(ds.Label ?? "Series"));
            if (ds.RangeData is { } ranges)
            {
                foreach (var r in ranges)
                    sb.Append(',').Append(r is { } rr ? Csv($"{rr.Low.ToString(culture)} - {rr.High.ToString(culture)}") : "");
            }
            else
            {
                foreach (var v in ds.Data)
                    sb.Append(',').Append(v is { } vv ? Csv(vv.ToString(culture)) : "");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>Quotes a CSV field when it contains a separator, quote or newline.</summary>
    private static string Csv(string value)
    {
        if (value.IndexOfAny([',', '"', '\n', '\r']) < 0) return value;
        return '"' + value.Replace("\"", "\"\"") + '"';
    }
}
