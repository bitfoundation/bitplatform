using System.Text;
using System.Numerics;
using System.Text.Json;
using Bit.Bink.Demo.Server.Dtos;

namespace Bit.Bink.Demo.Server.Services;

/// <summary>
/// Runs a Bink document through the library's own serializer and exporters, so "is this document
/// valid, what is in it, and what will an export of it look like?" is answered by the code that
/// decides it at runtime rather than by a reading of the format.
/// <para>
/// The document is read exactly the way an application reads one it did not write: with
/// <see cref="BinkSerializer.TryDeserialize(string, out BinkDocument?, out string?, BinkDeserializerOptions?)"/>
/// and the default, untrusted-input limits. Everything the report says about the loaded document -
/// its layers, its element types, its bounds, its round trip, the pixels a PNG export has and what
/// it has to leave out - is the library's answer, computed on the spot.
/// </para>
/// </summary>
public static class BinkDocumentLab
{
    /// <summary>
    /// The largest document this tool reads. Far below the library's own limit: the document arrives
    /// as a tool argument, and a drawing that size is measured in megabytes of points, not in the
    /// structure a question is about.
    /// </summary>
    public const int MaxInputBytes = 2 * 1024 * 1024;

    /// <summary>The longest SVG handed back; longer exports are cut at an element boundary.</summary>
    public const int MaxSvgLength = 30_000;

    /// <summary>
    /// The pixel budget of the PNG check. Which text and images the server-side rasterizer has to
    /// skip does not depend on the scale, so it renders a scaled-down copy rather than the full page.
    /// </summary>
    private const long PngCheckPixels = 250_000;

    public static readonly string[] Regions = ["Page", "Content"];

    private static readonly Lazy<string> _sample = new(BuildSample);

    /// <summary>A small document written by the library itself with readable points.</summary>
    public static string Sample => _sample.Value;

    public static BinkDocumentReportDto Inspect(string? json, bool includeSvg, string? region)
    {
        var supported = FormattableString.Invariant($"{BinkSerializer.MinimumSupportedVersion}..{BinkSerializer.CurrentVersion}");

        if (string.IsNullOrWhiteSpace(json))
        {
            return new BinkDocumentReportDto
            {
                Valid = false,
                SupportedVersions = supported,
                InputBytes = 0,
                Sample = Sample,
                Notes =
                [
                    "No document was passed, so nothing was inspected. Sample is a small document BinkSerializer wrote with " +
                    "BinkSerializerOptions.Readable (JSON points, indented) - pass it, or one of your own, as json to see the report. " +
                    "BinkSerializer.Serialize writes points as compact binary (\"encoding\": \"b64\") by default; both encodings load."
                ]
            };
        }

        var notes = new List<string>();
        var inputBytes = Encoding.UTF8.GetByteCount(json);
        var raw = ReadRaw(json);

        var options = BinkDeserializerOptions.Default with { MaxInputBytes = MaxInputBytes };

        if (BinkSerializer.TryDeserialize(json, out var document, out var error, options) is false || document is null)
        {
            if (inputBytes > MaxInputBytes)
            {
                notes.Add($"This tool reads documents of up to {MaxInputBytes} bytes; the library's own default limit (BinkDeserializerOptions.MaxInputBytes) is far higher.");
            }
            else
            {
                notes.Add("This is the message BinkSerializer.TryDeserialize gives an application for the same input, under the default untrusted-input limits.");
            }

            return new BinkDocumentReportDto
            {
                Valid = false,
                Error = error,
                Format = raw?.Format,
                SchemaVersion = raw?.Version,
                SupportedVersions = supported,
                InputBytes = inputBytes,
                Notes = [.. notes]
            };
        }

        var settings = document.Settings;
        var elements = document.AllElements.ToArray();

        var report = new BinkDocumentReportDto
        {
            Valid = true,
            Format = raw?.Format,
            SchemaVersion = raw?.Version,
            SupportedVersions = supported,
            Id = document.Id.ToString(),
            Width = settings.Width,
            Height = settings.Height,
            Background = settings.Background.ToCss(),
            MetadataEntries = document.Metadata.Count,
            Layers = [.. document.Layers.Select(layer => new BinkLayerReportDto
            {
                Id = layer.Id.ToString(),
                Name = layer.Name,
                Visible = layer.IsVisible,
                Locked = layer.IsLocked,
                Opacity = layer.Opacity,
                Elements = CountBy(layer.Elements.Select(element => element.TypeName))
            })],
            Elements = CountBy(elements.Select(element => element.TypeName)),
            TotalPoints = document.TotalPointCount,
            ContentBounds = document.ContentBounds is { IsEmpty: false } bounds ? ToDto(bounds) : null,
            BrushKinds = raw is null ? null : CountBy(raw.BrushKinds),
            PointEncodings = raw is null ? null : CountBy(raw.PointEncodings),
            InputBytes = inputBytes,
            Notes = []
        };

        if (elements.Length == 0) notes.Add("The document has no elements.");

        // ---- Round trip --------------------------------------------------------

        try
        {
            var compact = BinkSerializer.Serialize(document);
            var readable = BinkSerializer.Serialize(document, BinkSerializerOptions.Readable);
            var again = BinkSerializer.Serialize(BinkSerializer.Deserialize(compact));

            report = report with
            {
                CompactBytes = Encoding.UTF8.GetByteCount(compact),
                ReadableBytes = Encoding.UTF8.GetByteCount(readable),
                RoundTripStable = string.Equals(compact, again, StringComparison.Ordinal)
            };

            if (report.RoundTripStable is false)
            {
                notes.Add("Saving the loaded document, loading that and saving it again does not give the same text - a document that changes every time it is saved makes every save look like an edit.");
            }
        }
        catch (Exception exception) when (exception is BinkFormatException or InvalidOperationException or ArgumentException)
        {
            notes.Add($"The document loads, but saving it back fails: {exception.Message}");
        }

        // ---- What the document contains that an application has to know about ---

        if (raw is not null)
        {
            foreach (var kind in raw.BrushKinds.Distinct(StringComparer.Ordinal).Where(kind => BinkBrushRegistry.Default.Contains(kind) is false))
            {
                notes.Add($"Brush kind '{kind}' is not registered in BinkBrushRegistry.Default, so its strokes load as a BinkPenBrush with the stored colour, " +
                          "width and pressure, and are saved back as 'pen'. Register the brush (BinkBrushRegistry.Default.Register<TBrush>(), or a registry " +
                          "passed as BinkDeserializerOptions.BrushRegistry) before loading.");
            }
        }

        foreach (var unknown in elements.OfType<BinkUnknownElement>().GroupBy(element => element.SerializedTypeName, StringComparer.Ordinal))
        {
            notes.Add($"{unknown.Count()} element(s) of type '{unknown.Key}' are unknown to this build: they are kept verbatim as BinkUnknownElement, so saving " +
                      "does not lose them, but they are not drawn, exported or selectable.");
        }

        var hidden = document.Layers.Where(layer => layer.IsVisible is false).Select(layer => $"'{layer.Name}'").ToArray();
        if (hidden.Length > 0)
        {
            notes.Add($"Hidden layer(s) {string.Join(", ", hidden)} are left out of SVG and PNG exports unless BinkExportOptions.IncludeHiddenLayers is set.");
        }

        if (settings.HasFixedSize is false)
        {
            notes.Add("The page is unbounded (width or height is 0), so a Page-region export covers the content bounds plus Padding instead of a fixed page.");
        }
        else
        {
            var page = settings.PageRect;
            var outside = elements.Count(element => element is not BinkUnknownElement && element.Bounds.Intersects(page) is false);

            if (outside > 0)
            {
                notes.Add(FormattableString.Invariant($"{outside} element(s) lie entirely outside the {settings.Width} x {settings.Height} page, so a Page-region export (the default) does not show them. Export with Region = BinkExportRegion.Content to crop to the ink instead."));
            }
        }

        var remoteImages = elements.OfType<BinkImageElement>().Count(image => image.Source.IsEmbedded is false);
        if (remoteImages > 0)
        {
            notes.Add($"{remoteImages} image(s) reference a URL rather than embedding their data: the browser fetches them, and a document from an untrusted source can be loaded with BinkDeserializerOptions.AllowRemoteImages = false to refuse them.");
        }

        if (raw is not null && raw.PointEncodings.Any(encoding => encoding == "json"))
        {
            notes.Add("Some strokes store their points as JSON arrays. That loads fine; saving with the default options writes compact binary points (CompactBytes) instead, and BinkSerializerOptions.Readable keeps them readable (ReadableBytes).");
        }

        // ---- Export ----------------------------------------------------------------

        var png = CheckPng(document, notes);
        if (png is not null) report = report with { Png = png };

        if (includeSvg)
        {
            var exportRegion = string.Equals(region, "Content", StringComparison.OrdinalIgnoreCase) ? BinkExportRegion.Content : BinkExportRegion.Page;

            if (region is not null && Regions.Contains(region, StringComparer.OrdinalIgnoreCase) is false)
            {
                notes.Add($"'{region}' is not a region this tool exports, so Page was used. Use one of: {string.Join(", ", Regions)}.");
            }

            try
            {
                var svg = BinkSvgExporter.Export(document, new BinkSvgExportOptions { Region = exportRegion });

                if (svg.Length > MaxSvgLength)
                {
                    notes.Add($"The SVG is {svg.Length} characters long; only the first {MaxSvgLength} are returned.");
                    var cut = svg.LastIndexOf('>', MaxSvgLength - 1);
                    svg = svg[..(cut > 0 ? cut + 1 : MaxSvgLength)];
                }

                report = report with { Svg = svg };
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                notes.Add($"BinkSvgExporter could not export the document: {exception.Message}");
            }
        }

        return report with { Notes = [.. notes] };
    }

    /// <summary>
    /// What BinkPngExporter - the pure .NET path a server or a test uses - makes of the page: the
    /// size of a scale-1 export, and the text and images it cannot draw, found by rendering it.
    /// </summary>
    private static BinkPngReportDto? CheckPng(BinkDocument document, List<string> notes)
    {
        try
        {
            var region = BinkPngExportOptions.Default.ResolveRegion(document);
            if (region.IsEmpty) region = new BinkRect(region.X, region.Y, MathF.Max(region.Width, 1f), MathF.Max(region.Height, 1f));

            // The same arithmetic BinkPngExporter uses for the output size at scale 1.
            var width = Math.Max((int)MathF.Ceiling(region.Width), 1);
            var height = Math.Max((int)MathF.Ceiling(region.Height), 1);

            var scale = (float)Math.Min(1d, Math.Sqrt(PngCheckPixels / ((double)width * height)));

            BinkPngExporter.Render(document, BinkPngExportOptions.Default with { Scale = scale }, out var skippedText, out var skippedImages);

            if (skippedText > 0)
            {
                notes.Add($"BinkPngExporter skipped {skippedText} text element(s): it ships no font engine and draws text only when BinkPngExportOptions.TextRasterizer is set. " +
                          "BinkCanvas.ExportPngAsync renders in the browser and includes them.");
            }

            if (skippedImages > 0)
            {
                notes.Add($"BinkPngExporter skipped {skippedImages} image(s): the server-side path decodes only embedded PNG data URIs. BinkCanvas.ExportPngAsync renders in the browser and draws every format the browser decodes.");
            }

            return new BinkPngReportDto
            {
                Width = width,
                Height = height,
                SkippedTextElements = skippedText,
                SkippedImages = skippedImages
            };
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            notes.Add($"BinkPngExporter could not render the document: {exception.Message}");
            return null;
        }
    }

    private sealed record RawDocument(string? Format, int? Version, List<string> BrushKinds, List<string> PointEncodings);

    /// <summary>
    /// What the document says about itself before the library interprets it: the declared format and
    /// version, and the brush kinds and point encodings as written - an unregistered brush kind is
    /// loaded as a pen, so the loaded document alone can no longer tell which kind it was.
    /// </summary>
    private static RawDocument? ReadRaw(string json)
    {
        try
        {
            using var parsed = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });

            var root = parsed.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            var format = root.TryGetProperty("format", out var formatElement) && formatElement.ValueKind == JsonValueKind.String ? formatElement.GetString() : null;
            int? version = root.TryGetProperty("version", out var versionElement) && versionElement.ValueKind == JsonValueKind.Number && versionElement.TryGetInt32(out var number) ? number : null;

            var brushKinds = new List<string>();
            var encodings = new List<string>();

            if (root.TryGetProperty("layers", out var layers) && layers.ValueKind == JsonValueKind.Array)
            {
                foreach (var layer in layers.EnumerateArray())
                {
                    if (layer.ValueKind != JsonValueKind.Object || layer.TryGetProperty("elements", out var items) is false || items.ValueKind != JsonValueKind.Array) continue;

                    foreach (var item in items.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Object || item.TryGetProperty("type", out var type) is false || type.ValueKind != JsonValueKind.String || type.GetString() != "stroke") continue;

                        // The same defaults the serializer reads with: no brush is a pen, no encoding is JSON.
                        brushKinds.Add(item.TryGetProperty("brush", out var brush) && brush.ValueKind == JsonValueKind.Object &&
                                       brush.TryGetProperty("kind", out var kind) && kind.ValueKind == JsonValueKind.String
                            ? kind.GetString() ?? BinkPenBrush.KindName
                            : BinkPenBrush.KindName);

                        encodings.Add(item.TryGetProperty("points", out var points) && points.ValueKind == JsonValueKind.Object &&
                                      points.TryGetProperty("encoding", out var encoding) && encoding.ValueKind == JsonValueKind.String
                            ? encoding.GetString() ?? "json"
                            : "json");
                    }
                }
            }

            return new RawDocument(format, version, brushKinds, encodings);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static BinkCountDto[] CountBy(IEnumerable<string> names)
    {
        return [.. names.GroupBy(name => name, StringComparer.Ordinal)
                        .Select(group => new BinkCountDto { Name = group.Key, Count = group.Count() })
                        .OrderByDescending(count => count.Count)
                        .ThenBy(count => count.Name, StringComparer.Ordinal)];
    }

    private static BinkRectDto ToDto(BinkRect rect) => new()
    {
        X = Round(rect.X),
        Y = Round(rect.Y),
        Width = Round(rect.Width),
        Height = Round(rect.Height)
    };

    private static float Round(float value) => MathF.Round(value, 2);

    /// <summary>
    /// The sample: a page, two strokes with different brushes, a shape and a text element - one of
    /// each thing a typical drawing holds - written with readable points so the format can be read.
    /// It is built once per process, so every call answers with the same text.
    /// </summary>
    private static string BuildSample()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 320, Height = 200 });

        document.AddElement(new BinkStroke(
            BinkStrokePoints.FromPositions([new Vector2(20, 120), new Vector2(60, 80), new Vector2(100, 110), new Vector2(140, 70)]),
            new BinkPenBrush { Color = BinkColor.Parse("#1d4ed8") }));

        document.AddElement(new BinkStroke(
            BinkStrokePoints.FromPositions([new Vector2(20, 160), new Vector2(180, 160)]),
            new BinkHighlighterBrush()));

        document.AddElement(new BinkRectangleShape(new BinkRect(200, 40, 90, 60)));

        document.AddElement(new BinkTextElement(new Vector2(200, 130), "Hello"));

        return BinkSerializer.Serialize(document, BinkSerializerOptions.Readable);
    }
}
