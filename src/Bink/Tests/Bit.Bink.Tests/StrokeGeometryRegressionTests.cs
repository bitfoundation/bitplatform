
namespace Bit.Bink.Tests;

/// <summary>
/// Regressions in the outline the brushes generate: the shape of a committed stroke must not depend on how many
/// points simplification happened to keep.
/// </summary>
[TestClass]
public class StrokeGeometryRegressionTests(TestContext output)
{
    /// <summary>
    /// Simplification is what makes this a regression rather than a curiosity: a straight stroke is committed as
    /// two points hundreds of units apart, and the taper was evaluated only at those. A taper meant for the last
    /// few units of the stroke became a wedge down its whole length, so a line looked right while it was being
    /// drawn and changed shape the moment it was committed.
    /// </summary>
    [TestMethod]
    [DataRow(2)]
    [DataRow(6)]
    [DataRow(60)]
    public void A_taper_covers_the_same_length_however_few_points_the_stroke_kept(int count)
    {
        var buffer = new BinkStrokePointBuffer(count);
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            buffer.Add(new BinkPoint(80 + t * 480, 200, 0.8f, i * 8f));
        }
        var stroke = new BinkStroke(buffer.ToStrokePoints(), new BinkPenBrush { Width = 6f, TaperEnd = 5f });

        var document = new BinkDocument(new BinkDocumentSettings { Width = 700, Height = 400, Background = BinkColor.Transparent });
        document.AddElement(stroke);
        BinkRasterSurface surface = BinkPngExporter.Render(document,
            new BinkPngExportOptions { Region = BinkExportRegion.Custom, CustomRegion = new BinkRect(0, 0, 700, 400), Background = BinkColor.Transparent },
            out _, out _);
        byte[] rgba = surface.ToRgba32();

        float Coverage(int x)
        {
            long alpha = 0;
            for (int y = 0; y < surface.Height; y++) alpha += rgba[(y * surface.Width + x) * 4 + 3];
            return alpha / 255f;
        }

        // The taper zone is the last 5 units, so everything from well after the start to well before the end must
        // be at full width whatever the point count is.
        float[] samples = [Coverage(120), Coverage(300), Coverage(500), Coverage(550)];
        output.WriteLine($"{count} points -> " + string.Join(" ", samples.Select(v => v.ToString("0.00"))));
        foreach (float coverage in samples)
        {
            Assert.IsTrue(coverage > 4.5f, $"a {count}-point stroke tapered outside its taper zone: {string.Join(" ", samples)}");
        }
    }
}
