using System.Numerics;
using Bit.Bmage.Drawing;
using Bit.Bmage.Text;

namespace Bit.Bmage.Tests;

/// <summary>
/// Exercises the TrueType parser and text layout against a small purpose-built font
/// (see Assets/generate-test-font.py). Using a generated font rather than a system one keeps these deterministic on
/// every machine, and its glyphs are simple shapes whose coverage can be asserted exactly.
/// </summary>
[TestClass]
public sealed class FontTests
{
    private const string FontPath = "Assets/BmageTest.ttf";

    private static FontFile LoadFont() => FontFile.Load(File.ReadAllBytes(FontPath));

    [TestMethod]
    public void ParsesFontHeaderAndNames()
    {
        var font = LoadFont();
        Assert.AreEqual(1000, font.UnitsPerEm);
        Assert.AreEqual(800, font.Ascender);
        Assert.AreEqual(-200, font.Descender);
        Assert.AreEqual(4, font.GlyphCount);
        Assert.AreEqual("Bmage Test", font.FamilyName);
        Assert.AreEqual("Bmage Test Regular", font.FullName);
    }

    [TestMethod]
    public void MapsCharactersToGlyphsThroughCmap()
    {
        var font = LoadFont();
        Assert.AreEqual(1, font.GetGlyphIndex('A'));
        Assert.AreEqual(2, font.GetGlyphIndex('B'));
        Assert.AreEqual(3, font.GetGlyphIndex('C'));
        Assert.IsTrue(font.HasGlyph('A'));
        // Unmapped characters fall back to .notdef rather than throwing.
        Assert.AreEqual(0, font.GetGlyphIndex('Z'));
        Assert.IsFalse(font.HasGlyph('Z'));
        Assert.AreEqual(0, font.GetGlyphIndex(0x1F600));
    }

    [TestMethod]
    public void ReadsHorizontalMetrics()
    {
        var font = LoadFont();
        Assert.AreEqual(800, font.GetAdvanceWidth(1));
        Assert.AreEqual(800, font.GetAdvanceWidth(2));
    }

    [TestMethod]
    public void ReadsKerningPairs()
    {
        var font = LoadFont();
        Assert.IsTrue(font.HasKerning);
        Assert.AreEqual(-50, font.GetKerning(1, 2));
        Assert.AreEqual(0, font.GetKerning(2, 1));
        Assert.AreEqual(0, font.GetKerning(3, 3));
    }

    [TestMethod]
    public void ExtractsGlyphOutlines()
    {
        var font = LoadFont();
        var square = font.GetGlyphOutline(1);
        Assert.IsNotNull(square);
        Assert.ContainsSingle(square!.Contours);
        Assert.AreEqual(4, square.Contours[0].Points.Count);

        var ring = font.GetGlyphOutline(3);
        Assert.IsNotNull(ring);
        Assert.AreEqual(2, ring!.Contours.Count);

        // .notdef is empty in this font.
        Assert.IsNull(font.GetGlyphOutline(0));
    }

    [TestMethod]
    public void RejectsDataThatIsNotAFont()
    {
        Assert.ThrowsExactly<ImageDecodeException>(() => FontFile.Load(new byte[64]));
        Assert.ThrowsExactly<ImageDecodeException>(() => FontFile.Load(new byte[4]));
        // A plausible header with nothing behind it must fail cleanly rather than reading out of bounds.
        var fake = new byte[64];
        fake[1] = 1;
        Assert.ThrowsExactly<ImageDecodeException>(() => FontFile.Load(fake));
    }

    [TestMethod]
    public void HandlesTruncatedFontsWithoutCrashing()
    {
        var full = File.ReadAllBytes(FontPath);
        for (var length = 12; length < full.Length; length += 37)
        {
            try
            {
                var font = FontFile.Load(full[..length]);
                // If it parsed, using it must still be safe.
                _ = font.GetGlyphOutline(1);
                _ = font.GetGlyphIndex('A');
            }
            catch (ImageDecodeException)
            {
                // Rejecting truncated data is the expected outcome.
            }
        }
    }
}

[TestClass]
public sealed class FontTextRasterizerTests
{
    private static FontTextRasterizer CreateRasterizer()
    {
        var font = FontFile.Load(File.ReadAllBytes("Assets/BmageTest.ttf"));
        return new FontTextRasterizer().Register("Test", font);
    }

    private static TextStyle Style(float size = 100f) => new()
    {
        FontFamily = "Test",
        FontSize = size,
        FillColor = Rgba32.Black,
    };

    [TestMethod]
    public void MeasuresTextWidthFromAdvances()
    {
        var rasterizer = CreateRasterizer();
        // Advance is 800/1000 em, so at 100px each glyph advances 80px. "AB" also has -50 units of kerning (-5px).
        var metrics = rasterizer.Measure("AB", Style());
        Assert.AreEqual(155f, metrics.Width, 0.5e-1);
        Assert.AreEqual(80f, metrics.Ascent, 0.5e-1);
        Assert.AreEqual(20f, metrics.Descent, 0.5e-1);
        Assert.AreEqual(1, metrics.LineCount);
    }

    [TestMethod]
    public void MeasuresEmptyTextAsZero()
    {
        var metrics = CreateRasterizer().Measure(string.Empty, Style());
        Assert.AreEqual(0, metrics.Width);
        Assert.AreEqual(0, metrics.LineCount);
    }

    [TestMethod]
    public void CountsExplicitLineBreaks()
    {
        var metrics = CreateRasterizer().Measure("A\nB\nC", Style());
        Assert.AreEqual(3, metrics.LineCount);
    }

    [TestMethod]
    public void WrapsAtTheMaximumWidth()
    {
        var rasterizer = CreateRasterizer();
        // Six glyphs at 80px each need 480px; a 200px limit must break them across lines.
        var metrics = rasterizer.Measure("A A A A A A", Style() with { MaxWidth = 200 });
        Assert.IsTrue(metrics.LineCount >= 3, $"Expected wrapping, got {metrics.LineCount} line(s)");
        Assert.IsTrue(metrics.Width <= 200 + 1, $"Wrapped width {metrics.Width} exceeded the limit");
    }

    [TestMethod]
    public void BuildsAPathWithTheRightNumberOfContours()
    {
        var rasterizer = CreateRasterizer();
        // 'A' is one square, 'C' is a square with a hole, so three contours in total.
        var path = rasterizer.GetTextPath("AC", Vector2.Zero, Style());
        Assert.IsNotNull(path);
        Assert.AreEqual(3, path!.Contours.Count);
    }

    [TestMethod]
    public void RendersGlyphsAsSolidShapes()
    {
        var rasterizer = CreateRasterizer();
        using var canvas = ImageCanvas.Create(200, 200, Rgba32.White);
        canvas.DrawText("A", new Vector2(20, 150), Style(), rasterizer);

        // The square glyph spans x 100..700 and y 0..700 in font units, which at 100px is x 30..90 below the baseline.
        Assert.AreEqual(Rgba32.Black, canvas.Buffer[60, 120]);
        // Outside the glyph the canvas is untouched.
        Assert.AreEqual(Rgba32.White, canvas.Buffer[150, 40]);
    }

    [TestMethod]
    public void HonoursAlignment()
    {
        var rasterizer = CreateRasterizer();
        using var left = ImageCanvas.Create(300, 200, Rgba32.White);
        left.DrawText("A", new Vector2(150, 150), Style() with { Alignment = TextAlignment.Left }, rasterizer);
        using var right = ImageCanvas.Create(300, 200, Rgba32.White);
        right.DrawText("A", new Vector2(150, 150), Style() with { Alignment = TextAlignment.Right }, rasterizer);

        static int FirstInkColumn(ImageCanvas c)
        {
            for (var x = 0; x < c.Width; x++)
                for (var y = 0; y < c.Height; y++)
                    if (c.Buffer[x, y].R < 128) return x;
            return -1;
        }

        var leftInk = FirstInkColumn(left);
        var rightInk = FirstInkColumn(right);
        Assert.IsTrue(leftInk > rightInk, $"Right aligned text should start further left: left={leftInk} right={rightInk}");
    }

    [TestMethod]
    public void HonoursBaselineMode()
    {
        var rasterizer = CreateRasterizer();
        static int FirstInkRow(ImageCanvas c)
        {
            for (var y = 0; y < c.Height; y++)
                for (var x = 0; x < c.Width; x++)
                    if (c.Buffer[x, y].R < 128) return y;
            return -1;
        }

        using var alphabetic = ImageCanvas.Create(200, 300, Rgba32.White);
        alphabetic.DrawText("A", new Vector2(20, 150), Style(), rasterizer);
        using var top = ImageCanvas.Create(200, 300, Rgba32.White);
        top.DrawText("A", new Vector2(20, 150), Style() with { Baseline = TextBaseline.Top }, rasterizer);

        // With a Top baseline the glyph sits below the given y; with an alphabetic baseline it sits above it.
        Assert.IsTrue(FirstInkRow(top) > FirstInkRow(alphabetic));
    }

    [TestMethod]
    public void ScalingAStyleScalesTheRenderedSize()
    {
        var rasterizer = CreateRasterizer();
        var small = rasterizer.Measure("ABC", Style(50));
        var large = rasterizer.Measure("ABC", Style(100));
        Assert.AreEqual(large.Width, small.Width * 2, 0.5e-1);
        Assert.AreEqual(large.Width, rasterizer.Measure("ABC", Style(50).ForScale(2)).Width, 0.5e-1);
    }

    [TestMethod]
    public void ReportsAClearErrorWhenNoFontIsRegistered()
    {
        var rasterizer = new FontTextRasterizer();
        Assert.IsFalse(rasterizer.HasFonts);
        var ex = Assert.ThrowsExactly<ImageCapabilityException>(() => rasterizer.GetTextPath("A", Vector2.Zero, Style()));
        Assert.Contains("font", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void FallsBackWhenTheRequestedFamilyIsUnknown()
    {
        var rasterizer = CreateRasterizer();
        // A CSS style family list resolves to the registered font, and an unknown family falls back rather than failing.
        Assert.IsNotNull(rasterizer.ResolveFont(Style() with { FontFamily = "Missing, Test, sans-serif" }));
        Assert.IsNotNull(rasterizer.ResolveFont(Style() with { FontFamily = "Nothing At All" }));
    }

    [TestMethod]
    public void LetterSpacingWidensTheLine()
    {
        var rasterizer = CreateRasterizer();
        var normal = rasterizer.Measure("AAA", Style());
        var spaced = rasterizer.Measure("AAA", Style() with { LetterSpacing = 10 });
        Assert.AreEqual(normal.Width + 20, spaced.Width, 0.5e-1);
    }
}
