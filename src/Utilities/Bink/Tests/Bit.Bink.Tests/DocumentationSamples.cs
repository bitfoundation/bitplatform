using System.IO.Compression;
using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>
/// The code the documentation tells people to write, compiled and run. Documentation that no longer matches the
/// API is a bug, and the only way to keep it from drifting silently is to make the compiler read it too. Each
/// method mirrors one section of one document; when an API changes, this file fails to build and says which page
/// has to be updated.
/// </summary>
[TestClass]
public class DocumentationSamples
{
    private static BinkDocument Sample() => TestData.SampleDocument();

    [TestMethod]
    public void Export_document_svg_and_png()
    {
        BinkDocument document = Sample();

        // docs/export.md - SVG
        string svg = BinkSvgExporter.Export(document);
        byte[] bytes = BinkSvgExporter.ExportToUtf8Bytes(document);
        using var stream = new MemoryStream();
        BinkSvgExporter.Export(document, stream);

        Assert.StartsWith("<svg", svg, StringComparison.Ordinal);
        Assert.IsNotEmpty(bytes);
        Assert.IsTrue(stream.Length > 0);

        // docs/export.md - SVG options
        var svgOptions = new BinkSvgExportOptions
        {
            Region = BinkExportRegion.Content,
            Padding = 12f,
            Background = BinkColor.Transparent,
            Decimals = 2,
            IncludeXmlDeclaration = true,
            Title = "Signature",
            IncludeElementDescriptions = true,
            SelectedIds = document.AllElements.Select(e => e.Id).ToArray(),
        };
        Assert.StartsWith("<?xml", BinkSvgExporter.Export(document, svgOptions), StringComparison.Ordinal);

        // docs/export.md - PNG
        byte[] png = BinkPngExporter.Export(document, new BinkPngExportOptions { Scale = 2f });
        Assert.IsNotEmpty(png);

        BinkPngExportResult result = BinkPngExporter.ExportWithDetails(document);
        if (!result.IsComplete)
        {
            Assert.IsTrue(result.SkippedTextElements >= 0 && result.SkippedImages >= 0);
        }

        // docs/export.md - PNG options
        var pngOptions = new BinkPngExportOptions
        {
            Region = BinkExportRegion.Content,
            Padding = 8f,
            Scale = 3f,
            Background = BinkColor.Transparent,
            PixelsPerMeter = 11811,
            MaxPixels = 80_000_000,
            Compression = CompressionLevel.SmallestSize,
        };
        Assert.IsNotEmpty(BinkPngExporter.Export(document, pngOptions));
    }

    [TestMethod]
    public async Task Serialization_round_trip_and_limits()
    {
        BinkDocument document = Sample();

        // docs/serialization.md
        string json = BinkSerializer.Serialize(document);
        BinkDocument reloaded = BinkSerializer.Deserialize(json);

        byte[] utf8 = BinkSerializer.SerializeToUtf8Bytes(document);
        using var stream = new MemoryStream();
        await BinkSerializer.SerializeAsync(document, stream);
        stream.Position = 0;
        BinkDocument loaded = await BinkSerializer.DeserializeAsync(stream);

        Assert.IsTrue(BinkSerializer.TryDeserialize(json, out BinkDocument? d, out string? error));
        Assert.IsNotNull(d);
        Assert.IsNull(error);
        Assert.IsNotEmpty(utf8);
        Assert.AreEqual(document.ElementCount, reloaded.ElementCount);
        Assert.AreEqual(document.ElementCount, loaded.ElementCount);

        string readable = BinkSerializer.Serialize(document, BinkSerializerOptions.Readable);
        Assert.Contains("\n", readable, StringComparison.Ordinal);

        var options = new BinkDeserializerOptions
        {
            Limits = new BinkDocumentLimits { MaxElements = 5_000, MaxPointsPerStroke = 20_000 },
            AllowRemoteImages = false,
            AllowSvgImages = false,
        };
        Assert.IsNotNull(BinkSerializer.Deserialize(json, options));
    }

    [TestMethod]
    public void Metadata_and_descriptions()
    {
        BinkDocument document = Sample();
        BinkStroke stroke = document.AllElements.OfType<BinkStroke>().First();

        // docs/serialization.md and docs/accessibility.md
        document.SetMetadata("case-id", "2026-114");
        document.ReplaceElement(stroke with
        {
            Description = "Signature of Jane Doe",
            Metadata = new Dictionary<string, string> { ["signer"] = "jane" },
        });
        document.AddElement(new BinkLineShape(new Vector2(0, 0), new Vector2(10, 10)) { EndArrow = true, Description = "Arrow pointing at the total" });

        Assert.AreEqual("2026-114", document.Metadata["case-id"]);
        Assert.AreEqual("Signature of Jane Doe", document.GetElement(stroke.Id).GetAccessibleDescription());
    }

    [TestMethod]
    public void Selection_queries_and_layers()
    {
        BinkDocument document = Sample();
        using var selection = new BinkSelection(document);
        Guid elementId = document.AllElements.First().Id;
        Guid otherId = document.AllElements.Skip(1).First().Id;
        Guid thirdId = document.AllElements.Skip(2).First().Id;

        // docs/selection-and-layers.md
        selection.Set(elementId);
        selection.Add(otherId);
        selection.Toggle(thirdId);
        BinkRect bounds = selection.Bounds;
        foreach (BinkElement element in selection.Elements) Assert.IsNotNull(element);
        selection.SelectAll();
        selection.Clear();
        Assert.IsFalse(bounds.IsUnset);

        var point = new Vector2(30, 30);
        var rect = new BinkRect(0, 0, 400, 300);
        BinkElement? hit = BinkDocumentQuery.HitTest(document, point, tolerance: 4f);
        List<BinkElement> all = BinkDocumentQuery.HitTestAll(document, point);
        List<BinkElement> inside = BinkDocumentQuery.FindInRect(document, rect, BinkRectSelectionMode.Contain);
        Assert.IsNotNull(all);
        Assert.IsNotNull(inside);
        _ = hit;

        BinkLayer annotations = document.AddLayer("Annotations");
        document.UpdateLayer(annotations.Id, p => p with { IsVisible = false });
        document.UpdateLayer(annotations.Id, p => p with { IsLocked = true });
        document.UpdateLayer(annotations.Id, p => p with { Opacity = 0.5f });
        document.UpdateLayer(annotations.Id, p => p with { Name = "Review notes" });
        document.ReorderLayer(annotations.Id, index: 0);
        document.RemoveLayer(annotations.Id);
        Assert.IsFalse(document.TryGetLayer(annotations.Id, out _));
    }

    [TestMethod]
    public void Brushes_tools_and_erasing()
    {
        // docs/drawing-and-brushes.md
        var pen = new BinkPenBrush
        {
            Color = BinkColor.Parse("#1d4ed8"),
            Width = 6f,
            Opacity = 1f,
            Smoothing = 0.5f,
            Spacing = 0.25f,
            SimulatePressure = true,
            TaperStart = 0f,
            TaperEnd = 6f,
            PressureCurve = BinkPressureCurve.Pen,
        };
        BinkBrush brush = pen with { Color = BinkColor.Parse("#dc2626") };
        var writing = new BinkPenBrush { Width = 3f, Smoothing = 0.35f, Spacing = 0.12f, TaperEnd = 5f };
        Assert.AreEqual(BinkColor.Parse("#dc2626"), brush.Color);
        Assert.AreEqual(3f, writing.Width);

        BinkBrush[] builtIn = [new BinkPenBrush(), new BinkPencilBrush(), new BinkMarkerBrush(), new BinkHighlighterBrush(), new BinkPixelEraserBrush()];
        Assert.AreEqual(5, builtIn.Length);

        _ = BinkPressureCurve.Pen;
        _ = BinkPressureCurve.Linear;
        _ = BinkPressureCurve.Constant;
        _ = new BinkPressureCurve(BinkPressureCurveKind.EaseIn, minFactor: 0.1f, maxFactor: 1f);
        _ = BinkPressureCurve.FromSamples([0f, 0.1f, 0.45f, 0.8f, 1f]);

        // docs/getting-started.md and docs/erasing.md
        IBinkTool tool = new BinkDrawTool { SimplifyTolerance = 0.12f };
        tool = new BinkEraserTool { Mode = BinkEraserMode.Stroke, Radius = 12f };
        tool = new BinkSelectTool();
        tool = new BinkShapeTool(BinkShapeKind.Arrow);
        Assert.AreEqual("shape-arrow", tool.Id);
        _ = new BinkDrawTool { HonorPenEraserButton = false };

        // docs/erasing.md - the geometric primitive
        var stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(100, 0));
        var fragments = new List<BinkStroke>();
        bool changed = BinkStrokeEditor.EraseSegments(stroke, [new Vector2(50, 0)], 10f, fragments);
        Assert.IsTrue(changed);
        Assert.AreEqual(2, fragments.Count);

        // docs/mobile-and-stylus.md
        var pan = new BinkPanTool { MinZoom = 0.1f, MaxZoom = 32f, EnablePinchZoom = true };
        Assert.AreEqual(0.1f, pan.MinZoom);
    }

    [TestMethod]
    public void Annotations_text_and_images()
    {
        var document = new BinkDocument();

        // docs/annotations-and-images.md
        document.AddElement(new BinkRectangleShape(new BinkRect(20, 20, 200, 120))
        {
            Style = new BinkShapeStyle { StrokeColor = BinkColor.Parse("#dc2626"), StrokeWidth = 3f },
            CornerRadius = 6f,
        });
        document.AddElement(new BinkEllipseShape(new BinkRect(60, 60, 120, 120)));
        document.AddElement(new BinkLineShape(new Vector2(20, 20), new Vector2(200, 90))
        {
            EndArrow = true,
            Style = new BinkShapeStyle { StrokeColor = BinkColor.Parse("#2563eb"), StrokeWidth = 2f },
        });

        BinkRect[] lineRects = [new BinkRect(10, 10, 80, 14), new BinkRect(10, 26, 60, 14)];
        document.AddElement(new BinkHighlightElement(lineRects)
        {
            Color = BinkColor.Parse("#fde047"),
            Opacity = 0.5f,
            Description = "Highlighted: payment terms",
        });

        document.AddElement(new BinkCalloutElement(
            box: new BinkRect(320, 100, 200, 70),
            anchor: new Vector2(280, 130),
            text: "Please confirm this clause.")
        {
            Style = new BinkShapeStyle { FillColor = BinkColor.Parse("#fff9c4"), StrokeColor = BinkColor.Parse("#785a00") },
        });

        document.AddElement(new BinkTextElement(new Vector2(40, 40), "Approved")
        {
            Style = BinkTextStyle.Default with { FontSize = 20f, Bold = true, Color = BinkColor.Parse("#15803d") },
            Background = BinkColor.Parse("#dcfce7"),
            Padding = 6f,
        });

        byte[] pngBytes = BinkPngExporter.Export(new BinkDocument(new BinkDocumentSettings { Width = 4, Height = 4 }));
        BinkImageSource source = BinkImageSource.FromBytes(pngBytes, "image/png", width: 640, height: 400);
        document.AddElement(new BinkImageElement(source, new BinkRect(0, 0, 640, 400))
        {
            Crop = new BinkRect(100, 50, 400, 300),
            Opacity = 0.9f,
            Description = "Screenshot of the checkout page",
        });

        Guid imageLayer = document.Layers[0].Id;
        document.UpdateLayer(imageLayer, p => p with { IsLocked = true, Name = "Image" });
        Guid annotations = document.AddLayer("Annotations").Id;
        Assert.IsTrue(document.TryGetLayer(annotations, out _));

        Assert.IsTrue(BinkImageSource.TryCreate(source.Uri, 640, 400, allowSvg: false, out BinkImageSource? parsed));
        Assert.IsNotNull(parsed);
        Assert.IsFalse(BinkImageSource.TryCreate("javascript:alert(1)", 1, 1, allowSvg: false, out _));
    }

    /// <summary>docs/extending.md - a custom brush registered with the registry.</summary>
    public sealed record DashedBrush : BinkBrush
    {
        public const string KindName = "dashed";

        public override string Kind => KindName;

        /// <summary>Length of each dash in document units.</summary>
        public float DashLength { get; init; } = 6f;

        public override BinkStrokeGeometry BuildGeometry(BinkStrokePointsView points, BinkPathBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.Reset();
            for (int i = 1; i < points.Count; i++)
            {
                if (i % 2 == 0) continue;
                builder.MoveTo(points.PositionAt(i - 1));
                builder.LineTo(points.PositionAt(i));
            }
            return new BinkStrokeGeometry(
                builder.ToGeometry(),
                BinkStrokeGeometryMode.Centerline,
                Width,
                BinkLineCap.Round,
                BinkLineJoin.Round,
                points.Bounds.Inflate(Width));
        }
    }

    /// <summary>docs/extending.md - a custom tool.</summary>
    public sealed class DotTool : BinkToolBase
    {
        public override string Id => "dot";

        public override string DisplayName => "Dot";

        public override string Cursor => "crosshair";

        public override bool OnPointerDown(IBinkToolContext context, in BinkInputSample sample)
        {
            ArgumentNullException.ThrowIfNull(context);
            Vector2 point = context.Viewport.ToDocument(sample.Position);
            context.Document.AddElement(
                new BinkEllipseShape(BinkRect.FromCenter(point, 12f, 12f))
                {
                    Style = new BinkShapeStyle { FillColor = context.ShapeStyle.StrokeColor, StrokeWidth = 0f },
                },
                context.ActiveLayerId);
            context.InvalidateDocument();
            return true;
        }
    }

    /// <summary>docs/extending.md - a text rasterizer for the .NET PNG exporter.</summary>
    public sealed class NullTextRasterizer : IBinkTextRasterizer
    {
        public void DrawText(BinkRasterSurface surface, string text, Vector2 origin, in BinkTextStyle style, float scale, float opacity, BinkRect clip)
        {
        }

        public Vector2? Measure(string text, in BinkTextStyle style) => null;
    }

    [TestMethod]
    public void Extending_the_framework()
    {
        var registry = new BinkBrushRegistry();
        registry.Register<DashedBrush>();
        Assert.IsTrue(registry.TryCreate(DashedBrush.KindName, out BinkBrush? created));
        Assert.IsExactInstanceOfType<DashedBrush>(created);

        var stroke = new BinkStroke(TestData.Line(new Vector2(0, 0), new Vector2(60, 60), 8), new DashedBrush { Width = 3f });
        Assert.IsFalse(stroke.Geometry.IsEmpty);

        using var context = new TestToolContext();
        var tool = new DotTool();
        tool.Activate(context);
        Assert.IsTrue(tool.OnPointerDown(context, new BinkInputSample { X = 50, Y = 50, Pressure = 0.5f, Buttons = 1 }));
        Assert.AreEqual(1, context.Document.ElementCount);

        Assert.IsNotEmpty(BinkPngExporter.Export(context.Document, new BinkPngExportOptions { TextRasterizer = new NullTextRasterizer() }));
        BinkDocumentRenderer.Render(context.Document, new BinkSvgRenderContext(TextWriter.Null));
    }

    [TestMethod]
    public void Collaboration_surface()
    {
        // docs/collaboration.md - the shapes the page documents, without a transport.
        var presence = new BinkPresenceState
        {
            ParticipantId = "alex",
            Name = "Alex",
            Color = "#2563eb",
            Cursor = new Vector2(10, 20),
            LiveStroke = [new Vector2(0, 0), new Vector2(5, 5)],
            LiveStrokeColor = "#2563eb",
            LiveStrokeWidth = 4f,
        };
        Assert.AreEqual("alex", presence.ParticipantId);

        var document = new BinkDocument();
        var stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10));
        BinkDrawingOperation operation = new BinkAddElementOperation(document.Layers[0].Id, stroke);
        string json = BinkOperationCodec.Serialize(operation);
        BinkDrawingOperation received = BinkOperationCodec.Deserialize(json);
        Assert.IsExactInstanceOfType<BinkAddElementOperation>(received);
    }
}
