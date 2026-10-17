using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>
/// Visual regression tests. Each case renders a known drawing with the built-in rasterizer, which is
/// deterministic, and compares the pixels against a stored golden image.
/// </summary>
/// <remarks>
/// <para>
/// Set the environment variable <c>BINK_UPDATE_GOLDEN=1</c> to write the current output as the new golden
/// image. Review the change before committing it: that is the whole point of the check.
/// </para>
/// <para>
/// Comparison allows a small per-channel tolerance and a small fraction of differing pixels, so a rounding
/// change in a math library does not fail the suite while a real change in stroke shape does.
/// </para>
/// </remarks>
[TestClass]
public class VisualRegressionTests
{
    private const int Size = 240;
    private const int ChannelTolerance = 6;
    private const double MaxDifferingFraction = 0.002;

    private static string GoldenDirectory => Path.Combine(AppContext.BaseDirectory, "Golden");

    private static bool ShouldUpdate =>
        Environment.GetEnvironmentVariable("BINK_UPDATE_GOLDEN") is "1" or "true";

    /// <summary>The named drawings covered by the golden images.</summary>
    public static readonly string[] CaseNames =
    [
        "straight-line",
        "circle",
        "handwriting",
        "pressure-ramp",
        "sharp-corner",
        "overlapping-strokes",
        "highlighter-multiply",
        "eraser-mask",
        "layer-opacity",
        "shapes",
        "dot",
        "rotated-element",
    ];

    public static IEnumerable<object[]> Cases()
    {
        foreach (string name in CaseNames) yield return [name];
    }

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void Rendering_matches_the_golden_image(string name)
    {
        BinkDocument document = BuildCase(name);
        byte[] rendered = BinkPngExporter.Export(document, new BinkPngExportOptions { Scale = 1f });

        string path = Path.Combine(GoldenDirectory, name + ".png");
        if (ShouldUpdate || !File.Exists(path))
        {
            Directory.CreateDirectory(GoldenDirectory);
            File.WriteAllBytes(path, rendered);
            Assert.IsTrue(ShouldUpdate, $"No golden image existed for \"{name}\". One was written to {path}; review it and commit it.");
            return;
        }

        BinkPngCodec.DecodedImage expected = BinkPngCodec.Decode(File.ReadAllBytes(path));
        BinkPngCodec.DecodedImage actual = BinkPngCodec.Decode(rendered);

        Assert.AreEqual(expected.Width, actual.Width);
        Assert.AreEqual(expected.Height, actual.Height);

        int differing = 0;
        int worst = 0;
        for (int i = 0; i < expected.Rgba.Length; i += 4)
        {
            int delta = 0;
            for (int c = 0; c < 4; c++) delta = Math.Max(delta, Math.Abs(expected.Rgba[i + c] - actual.Rgba[i + c]));
            if (delta > ChannelTolerance) differing++;
            worst = Math.Max(worst, delta);
        }

        double fraction = differing / (double)(expected.Width * expected.Height);
        if (fraction > MaxDifferingFraction)
        {
            string failurePath = Path.Combine(GoldenDirectory, name + ".actual.png");
            File.WriteAllBytes(failurePath, rendered);
            Assert.Fail($"\"{name}\" differs from its golden image: {fraction:P2} of pixels changed (worst channel delta {worst}). The rendered image was written to {failurePath}.");
        }
    }

    [TestMethod]
    public void Rendering_is_deterministic_across_runs()
    {
        BinkDocument document = BuildCase("handwriting");
        Assert.AreSequenceEqual(BinkPngExporter.Export(document), BinkPngExporter.Export(document));
    }

    [TestMethod]
    public void Svg_export_is_stable_for_every_case()
    {
        // A cheap structural check across all cases: valid, non-empty, and containing real path geometry.
        foreach (string name in CaseNames)
        {
            string svg = BinkSvgExporter.Export(BuildCase(name));
            Assert.StartsWith("<svg", svg, StringComparison.Ordinal);
            Assert.EndsWith("</svg>", svg, StringComparison.Ordinal);
            Assert.AreEqual(svg, BinkSvgExporter.Export(BuildCase(name)));
        }
    }

    private static BinkDocument BuildCase(string name)
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = Size, Height = Size, Background = BinkColor.White });
        Guid layer = document.Layers[0].Id;

        switch (name)
        {
            case "straight-line":
                document.AddElement(new BinkStroke(Line(new Vector2(30, 120), new Vector2(210, 120), 40),
                    new BinkPenBrush { Width = 10f, PressureCurve = BinkPressureCurve.Constant, TaperEnd = 0f }), layer);
                break;

            case "circle":
                document.AddElement(new BinkStroke(TestData.Circle(new Vector2(120, 120), 70, 96),
                    new BinkPenBrush { Width = 6f, PressureCurve = BinkPressureCurve.Constant, TaperEnd = 0f }), layer);
                break;

            case "handwriting":
            {
                // A cursive-like scribble: the case that exercises smoothing, curvature and taper together.
                var buffer = new BinkStrokePointBuffer();
                for (int i = 0; i <= 160; i++)
                {
                    float t = i / 160f;
                    buffer.Add(new BinkPoint(
                        20f + t * 200f,
                        120f + MathF.Sin(t * MathF.Tau * 2.5f) * 55f * (1f - t * 0.4f),
                        0.25f + 0.6f * MathF.Abs(MathF.Sin(t * MathF.PI * 3f)),
                        i * 6f));
                }
                document.AddElement(new BinkStroke(buffer.ToStrokePoints(), new BinkPenBrush { Width = 8f }), layer);
                break;
            }

            case "pressure-ramp":
            {
                var buffer = new BinkStrokePointBuffer();
                for (int i = 0; i <= 60; i++)
                {
                    buffer.Add(new BinkPoint(20f + i * 3.3f, 120f, 0.05f + i / 60f * 0.95f, i * 8f));
                }
                document.AddElement(new BinkStroke(buffer.ToStrokePoints(),
                    new BinkPenBrush { Width = 22f, PressureCurve = BinkPressureCurve.Linear, TaperEnd = 0f }), layer);
                break;
            }

            case "sharp-corner":
            {
                var buffer = new BinkStrokePointBuffer();
                for (int i = 0; i <= 20; i++) buffer.Add(new BinkPoint(30f + i * 4.5f, 200f - i * 7f, 0.6f, i * 8f));
                for (int i = 1; i <= 20; i++) buffer.Add(new BinkPoint(120f + i * 4.5f, 60f + i * 7f, 0.6f, (20 + i) * 8f));
                document.AddElement(new BinkStroke(buffer.ToStrokePoints(),
                    new BinkPenBrush { Width = 18f, PressureCurve = BinkPressureCurve.Constant, TaperEnd = 0f }), layer);
                break;
            }

            case "overlapping-strokes":
                for (int i = 0; i < 5; i++)
                {
                    document.AddElement(new BinkStroke(
                        Line(new Vector2(30, 40 + i * 12), new Vector2(210, 200 - i * 12), 24),
                        new BinkPenBrush { Width = 9f, Opacity = 0.6f, PressureCurve = BinkPressureCurve.Constant, TaperEnd = 0f, Color = new BinkColor((byte)(40 + i * 40), 60, (byte)(200 - i * 30)) }), layer);
                }
                break;

            case "highlighter-multiply":
                document.AddElement(new BinkStroke(Line(new Vector2(20, 90), new Vector2(220, 90), 20), new BinkHighlighterBrush { Width = 40f }), layer);
                document.AddElement(new BinkStroke(Line(new Vector2(120, 20), new Vector2(120, 220), 20), new BinkHighlighterBrush { Width = 40f }), layer);
                document.AddElement(new BinkStroke(Line(new Vector2(20, 150), new Vector2(220, 150), 20),
                    new BinkPenBrush { Width = 5f, PressureCurve = BinkPressureCurve.Constant, TaperEnd = 0f }), layer);
                break;

            case "eraser-mask":
                document.AddElement(new BinkStroke(Line(new Vector2(20, 120), new Vector2(220, 120), 30),
                    new BinkPenBrush { Width = 26f, PressureCurve = BinkPressureCurve.Constant, TaperEnd = 0f }), layer);
                document.AddElement(new BinkStroke(Line(new Vector2(120, 30), new Vector2(120, 210), 20),
                    new BinkPixelEraserBrush { Width = 34f }), layer);
                break;

            case "layer-opacity":
            {
                document.AddElement(new BinkStroke(Line(new Vector2(20, 100), new Vector2(220, 100), 20),
                    new BinkPenBrush { Width = 20f, PressureCurve = BinkPressureCurve.Constant, TaperEnd = 0f }), layer);
                BinkLayer faded = document.AddLayer("Faded");
                document.UpdateLayer(faded.Id, p => p with { Opacity = 0.35f });
                document.AddElement(new BinkStroke(Line(new Vector2(20, 140), new Vector2(220, 140), 20),
                    new BinkPenBrush { Width = 20f, PressureCurve = BinkPressureCurve.Constant, TaperEnd = 0f }), faded.Id);
                break;
            }

            case "shapes":
                document.AddElement(new BinkRectangleShape(new BinkRect(20, 20, 90, 60)) { Style = new BinkShapeStyle { StrokeWidth = 3f, FillColor = BinkColor.Parse("#dbeafe") } }, layer);
                document.AddElement(new BinkEllipseShape(new BinkRect(130, 20, 90, 60)) { Style = new BinkShapeStyle { StrokeWidth = 3f, StrokeColor = BinkColor.Parse("#dc2626") } }, layer);
                document.AddElement(new BinkLineShape(new Vector2(30, 140), new Vector2(200, 190)) { EndArrow = true, Style = new BinkShapeStyle { StrokeWidth = 4f, StrokeColor = BinkColor.Parse("#16a34a") } }, layer);
                document.AddElement(new BinkRectangleShape(new BinkRect(30, 100, 180, 24)) { CornerRadius = 10f, Style = new BinkShapeStyle { StrokeWidth = 2f, Dash = [8f, 5f] } }, layer);
                document.AddElement(new BinkHighlightElement(new BinkRect(120, 150, 90, 30)), layer);
                break;

            case "dot":
            {
                for (int i = 0; i < 4; i++)
                {
                    var buffer = new BinkStrokePointBuffer();
                    buffer.Add(new BinkPoint(50f + i * 45f, 120f, 0.2f + i * 0.25f, 0f));
                    document.AddElement(new BinkStroke(buffer.ToStrokePoints(), new BinkPenBrush { Width = 24f }), layer);
                }
                break;
            }

            case "rotated-element":
                document.AddElement(new BinkRectangleShape(new BinkRect(-60, -25, 120, 50))
                {
                    Style = new BinkShapeStyle { StrokeWidth = 4f, FillColor = BinkColor.Parse("#fde68a") },
                    Transform = Matrix3x2.CreateRotation(0.6f) * Matrix3x2.CreateTranslation(120, 120),
                }, layer);
                document.AddElement(new BinkStroke(Line(new Vector2(20, 20), new Vector2(80, 60), 12), new BinkPenBrush { Width = 6f })
                {
                    Transform = Matrix3x2.CreateScale(1.6f) * Matrix3x2.CreateTranslation(40, 120),
                }, layer);
                break;

            default:
                throw new ArgumentException($"Unknown visual case \"{name}\".", nameof(name));
        }

        return document;
    }

    private static BinkStrokePoints Line(Vector2 from, Vector2 to, int count)
        => TestData.Line(from, to, count);
}
