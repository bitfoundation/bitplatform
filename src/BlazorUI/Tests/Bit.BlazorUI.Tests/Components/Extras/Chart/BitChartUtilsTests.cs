using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Chart;

/// <summary>Unit tests for the pure helpers the renderer is built on.</summary>
[TestClass]
public class BitChartUtilsTests
{
    // ---- colors ----

    [TestMethod]
    [DataRow("#369", 51, 102, 153)]
    [DataRow("#336699", 51, 102, 153)]
    [DataRow("rgb(51, 102, 153)", 51, 102, 153)]
    [DataRow("rgba(51,102,153,0.5)", 51, 102, 153)]
    public void ColorUtilShouldParseTheCommonNotations(string color, int r, int g, int b)
    {
        Assert.IsTrue(BitChartColorUtil.TryParse(color, out var pr, out var pg, out var pb, out _));
        Assert.AreEqual(r, pr);
        Assert.AreEqual(g, pg);
        Assert.AreEqual(b, pb);
    }

    [TestMethod]
    public void ColorUtilShouldReadTheAlphaOfAnEightDigitHex()
    {
        Assert.IsTrue(BitChartColorUtil.TryParse("#33669980", out _, out _, out _, out var a));
        Assert.AreEqual(0.5, a, 0.01);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("nonsense")]
    [DataRow("#12345")]
    [DataRow("rgb(1,2)")]
    public void ColorUtilShouldRejectWhatItCannotParse(string color)
    {
        Assert.IsFalse(BitChartColorUtil.TryParse(color, out _, out _, out _, out _));
    }

    [TestMethod]
    public void WithAlphaShouldProduceAnRgbaColor()
    {
        Assert.AreEqual("rgba(51,102,153,0.4)", BitChartColorUtil.WithAlpha("#336699", 0.4));
    }

    [TestMethod]
    public void WithAlphaShouldLeaveUnparseableColorsAlone()
    {
        // CSS variables are handed straight to the browser, so they must survive untouched.
        const string token = "var(--bit-clr-pri, #0078d4)";
        Assert.AreEqual(token, BitChartColorUtil.WithAlpha(token, 0.4));
    }

    [TestMethod]
    public void AdjustShouldLightenAndDarken()
    {
        Assert.IsTrue(BitChartColorUtil.TryParse(BitChartColorUtil.Adjust("#808080", 0.5), out var lr, out _, out _, out _));
        Assert.IsTrue(BitChartColorUtil.TryParse(BitChartColorUtil.Adjust("#808080", -0.5), out var dr, out _, out _, out _));
        Assert.IsTrue(lr > 128);
        Assert.IsTrue(dr < 128);
    }

    [TestMethod]
    public void PaletteShouldCycleAndHandleNegativeIndexes()
    {
        int n = BitChartColorUtil.DefaultPalette.Length;
        Assert.AreEqual(BitChartColorUtil.Palette(0), BitChartColorUtil.Palette(n));
        Assert.AreEqual(BitChartColorUtil.Palette(n - 1), BitChartColorUtil.Palette(-1));
    }

    // ---- text measurement ----

    [TestMethod]
    public void TextMeasureShouldScaleWithLengthAndFontSize()
    {
        double small = BitChartTextMeasure.Width("Hello", 10);
        double big = BitChartTextMeasure.Width("Hello", 20);
        Assert.AreEqual(small * 2, big, 0.001);
        Assert.IsTrue(BitChartTextMeasure.Width("Hello world", 12) > BitChartTextMeasure.Width("Hello", 12));
    }

    [TestMethod]
    public void TextMeasureShouldKnowNarrowGlyphsFromWideOnes()
    {
        Assert.IsTrue(BitChartTextMeasure.Width("iiii", 12) < BitChartTextMeasure.Width("WWWW", 12));
    }

    [TestMethod]
    public void TextMeasureShouldMakeBoldTextWider()
    {
        Assert.IsTrue(BitChartTextMeasure.Width("Total", 12, "bold") > BitChartTextMeasure.Width("Total", 12));
    }

    [TestMethod]
    public void TextMeasureShouldReturnZeroForNothing()
    {
        Assert.AreEqual(0, BitChartTextMeasure.Width(null, 12));
        Assert.AreEqual(0, BitChartTextMeasure.Width("", 12));
    }

    [TestMethod]
    [DataRow("日本語", "Japanese")]
    [DataRow("中文标签", "Chinese")]
    [DataRow("한국어", "Korean")]
    public void TextMeasureShouldTreatFullWidthScriptsAsFullEms(string text, string script)
    {
        // A CJK glyph fills an em; measuring it at the Latin average would reserve far too little
        // room and the axis labels would then overlap.
        Assert.AreEqual(text.Length * 12, BitChartTextMeasure.Width(text, 12), 0.001, script);
    }

    [TestMethod]
    public void AFullWidthLabelShouldMeasureWiderThanTheSameCountOfLatinLetters()
    {
        Assert.IsTrue(BitChartTextMeasure.Width("東京都", 12) > BitChartTextMeasure.Width("abc", 12));
    }

    [TestMethod]
    public void TextMeasureShouldIgnoreCombiningMarks()
    {
        // "e" plus a combining acute is one glyph wide, not two. The mark is an explicit escape so no
        // editor or normalization pass can fold the pair into precomposed "é" and hollow the test out.
        Assert.AreEqual(BitChartTextMeasure.Width("e", 12), BitChartTextMeasure.Width("e\u0301", 12), 0.001);
    }

    [TestMethod]
    public void TextMeasureShouldStillHandleUnknownLatinLikeCharacters()
    {
        // Beyond the table but not full-width: measured at the average rather than dropped.
        Assert.IsTrue(BitChartTextMeasure.Width("Ω", 12) > 0);
    }

    [TestMethod]
    public void MultilineWidthShouldReturnTheWidestLine()
    {
        double expected = BitChartTextMeasure.Width("wide line here", 12);
        Assert.AreEqual(expected, BitChartTextMeasure.MultilineWidth("hi\nwide line here\nyo", 12), 0.001);
    }

    // ---- decimation ----

    [TestMethod]
    public void LttbShouldReduceToTheRequestedSampleCount()
    {
        var data = Enumerable.Range(0, 1000)
            .Select(i => ((double)i, Math.Sin(i / 10.0) * 50 + 50, i, 0d))
            .ToList();

        var sampled = BitChartDecimation.Lttb(data, 100);

        Assert.AreEqual(100, sampled.Count);
    }

    [TestMethod]
    public void LttbShouldKeepTheFirstAndLastPoint()
    {
        var data = Enumerable.Range(0, 500).Select(i => ((double)i, (double)i, i, 0d)).ToList();

        var sampled = BitChartDecimation.Lttb(data, 50);

        Assert.AreEqual(data[0], sampled[0]);
        Assert.AreEqual(data[^1], sampled[^1]);
    }

    [TestMethod]
    public void LttbShouldKeepTheSeriesInOrder()
    {
        var rnd = new Random(3);
        var data = Enumerable.Range(0, 800).Select(i => ((double)i, rnd.NextDouble() * 100, i, 0d)).ToList();

        var sampled = BitChartDecimation.Lttb(data, 120);

        for (int i = 1; i < sampled.Count; i++)
            Assert.IsTrue(sampled[i].x >= sampled[i - 1].x, $"out of order at {i}");
    }

    [TestMethod]
    public void LttbShouldKeepAPeakThatDefinesTheShape()
    {
        var data = Enumerable.Range(0, 300).Select(i => ((double)i, i == 150 ? 1000d : 1d, i, 0d)).ToList();

        var sampled = BitChartDecimation.Lttb(data, 30);

        Assert.IsTrue(sampled.Any(p => p.y >= 1000), "the spike is what the chart is about; it must survive");
    }

    [TestMethod]
    public void LttbShouldPassThroughWhenNoReductionIsNeeded()
    {
        var data = Enumerable.Range(0, 10).Select(i => ((double)i, (double)i, i, 0d)).ToList();

        Assert.AreSame(data, BitChartDecimation.Lttb(data, 50));
        Assert.AreSame(data, BitChartDecimation.Lttb(data, 2));
    }

    // ---- time axis ----

    [TestMethod]
    [DataRow(1, BitChartTimeUnit.Hour)]
    [DataRow(10, BitChartTimeUnit.Day)]
    [DataRow(40, BitChartTimeUnit.Week)]
    [DataRow(200, BitChartTimeUnit.Month)]
    [DataRow(2000, BitChartTimeUnit.Year)]
    public void TimeAxisShouldPickAUnitThatMatchesTheSpan(int days, BitChartTimeUnit expected)
    {
        var start = new DateTime(2026, 1, 1);
        Assert.AreEqual(expected, BitChartTimeAxis.ChooseUnit(start, start.AddDays(days)));
    }

    [TestMethod]
    public void TimeAxisShouldStayWithinTheTickBudget()
    {
        var start = new DateTime(2026, 1, 1);
        var ticks = BitChartTimeAxis.Ticks(start.ToOADate(), start.AddDays(365).ToOADate(),
            BitChartTimeUnit.Day, null, 8);

        Assert.IsTrue(ticks.Count <= 8 * 3, $"got {ticks.Count} ticks");
        Assert.IsTrue(ticks.Count > 1);
    }

    [TestMethod]
    public void TimeAxisTicksShouldRiseAndStayInsideTheRange()
    {
        var start = new DateTime(2026, 3, 5, 4, 0, 0);
        double min = start.ToOADate(), max = start.AddDays(90).ToOADate();

        var ticks = BitChartTimeAxis.Ticks(min, max, BitChartTimeUnit.Auto, null, 10);

        for (int i = 0; i < ticks.Count; i++)
        {
            Assert.IsTrue(ticks[i].Value >= min - 1e-9 && ticks[i].Value <= max + 1e-9);
            if (i > 0) Assert.IsTrue(ticks[i].Value > ticks[i - 1].Value);
        }
    }

    [TestMethod]
    public void TimeAxisShouldUseTheCustomFormatterWhenGiven()
    {
        var start = new DateTime(2026, 1, 1);
        var ticks = BitChartTimeAxis.Ticks(start.ToOADate(), start.AddDays(60).ToOADate(),
            BitChartTimeUnit.Month, d => $"M{d.Month}", 10);

        Assert.IsTrue(ticks.All(t => t.Label.StartsWith("M")), string.Join("|", ticks.Select(t => t.Label)));
    }

    [TestMethod]
    public void TimeAxisShouldAlwaysProduceAtLeastOneTick()
    {
        double value = new DateTime(2026, 5, 5).ToOADate();
        Assert.AreEqual(1, BitChartTimeAxis.Ticks(value, value, BitChartTimeUnit.Year, null, 5).Count);
    }

    [TestMethod]
    public void QuarterFormatShouldNumberTheQuarter()
    {
        Assert.AreEqual("Q2 2026", BitChartTimeAxis.DefaultFormat(new DateTime(2026, 5, 1), BitChartTimeUnit.Quarter));
    }

    [TestMethod]
    public void TimeAxisLabelsShouldFollowTheCultureTheyAreGiven()
    {
        var may = new DateTime(2026, 5, 1);

        Assert.AreEqual("May 2026", BitChartTimeAxis.DefaultFormat(may, BitChartTimeUnit.Month));
        Assert.AreEqual("mai 2026", BitChartTimeAxis.DefaultFormat(may, BitChartTimeUnit.Month, new CultureInfo("fr-FR")));
    }

    [TestMethod]
    public void TimeAxisTicksShouldCarryTheCultureIntoTheirLabels()
    {
        var start = new DateTime(2026, 1, 1);
        var ticks = BitChartTimeAxis.Ticks(start.ToOADate(), start.AddDays(120).ToOADate(),
            BitChartTimeUnit.Month, null, 10, new CultureInfo("fr-FR"));

        Assert.IsTrue(ticks.Any(t => t.Label.StartsWith("janv")), string.Join("|", ticks.Select(t => t.Label)));
    }

    [TestMethod]
    public void TimeAxisShouldStayInvariantWhenNoCultureIsGiven()
    {
        var start = new DateTime(2026, 1, 1);
        var ticks = BitChartTimeAxis.Ticks(start.ToOADate(), start.AddDays(120).ToOADate(),
            BitChartTimeUnit.Month, null, 10);

        Assert.IsTrue(ticks.Any(t => t.Label.StartsWith("Jan")), string.Join("|", ticks.Select(t => t.Label)));
    }

    // ---- point shapes ----

    [TestMethod]
    [DataRow(BitChartPointStyle.Circle, typeof(BitChartSvgCircle))]
    [DataRow(BitChartPointStyle.Rect, typeof(BitChartSvgRect))]
    [DataRow(BitChartPointStyle.RectRounded, typeof(BitChartSvgRect))]
    [DataRow(BitChartPointStyle.Triangle, typeof(BitChartSvgPolygon))]
    [DataRow(BitChartPointStyle.RectRot, typeof(BitChartSvgPolygon))]
    [DataRow(BitChartPointStyle.Star, typeof(BitChartSvgPolygon))]
    [DataRow(BitChartPointStyle.Cross, typeof(BitChartSvgPath))]
    [DataRow(BitChartPointStyle.CrossRot, typeof(BitChartSvgPath))]
    [DataRow(BitChartPointStyle.Dash, typeof(BitChartSvgPath))]
    [DataRow(BitChartPointStyle.Line, typeof(BitChartSvgPath))]
    public void PointShapesShouldBuildTheRightPrimitive(BitChartPointStyle style, Type expected)
    {
        var node = BitChartPointShapes.Build(style, 10, 10, 5, "#f00", "#00f", 1);

        Assert.IsInstanceOfType(node, expected);
    }

    [TestMethod]
    public void PointStyleNoneShouldBuildNothing()
    {
        Assert.IsNull(BitChartPointShapes.Build(BitChartPointStyle.None, 10, 10, 5, "#f00", "#00f", 1));
    }

    [TestMethod]
    public void PointRotationShouldBecomeATransform()
    {
        var node = BitChartPointShapes.Build(BitChartPointStyle.Triangle, 10, 20, 5, "#f00", "#00f", 1, 45);

        StringAssert.Contains(node!.Transform!, "rotate(45 10 20)");
    }

    // ---- number formatting ----

    [TestMethod]
    public void SvgNumbersShouldAlwaysUseTheInvariantForm()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.AreEqual("1.5", BitChartSvg.N(1.5));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [TestMethod]
    public void SvgNumbersShouldNeverEmitNaNOrInfinity()
    {
        Assert.AreEqual("0", BitChartSvg.N(double.NaN));
        Assert.AreEqual("0", BitChartSvg.N(double.PositiveInfinity));
        Assert.AreEqual("0", BitChartSvg.N(double.NegativeInfinity));
    }

    [TestMethod]
    public void DashShouldJoinTheSegmentsAndTolerateNull()
    {
        Assert.AreEqual("", BitChartSvg.Dash(null));
        Assert.AreEqual("6,4", BitChartSvg.Dash(new List<double> { 6, 4 }));
    }

    // ---- padding / corner helpers ----

    [TestMethod]
    public void PaddingShouldConvertFromASingleNumber()
    {
        BitChartPadding p = 8;
        Assert.AreEqual(16, p.Vertical);
        Assert.AreEqual(16, p.Horizontal);
    }

    [TestMethod]
    public void SymmetricPaddingShouldSplitVerticalAndHorizontal()
    {
        var p = BitChartPadding.Symmetric(4, 10);
        Assert.AreEqual(8, p.Vertical);
        Assert.AreEqual(20, p.Horizontal);
    }

    [TestMethod]
    public void CornerRadiusShouldConvertFromASingleNumber()
    {
        BitChartBorderRadiusCorners c = 6;
        Assert.AreEqual(6, c.TopLeft);
        Assert.AreEqual(6, c.BottomRight);

        var top = BitChartBorderRadiusCorners.Top(6);
        Assert.AreEqual(6, top.TopLeft);
        Assert.AreEqual(0, top.BottomRight);
    }

    [TestMethod]
    public void DatasetCountShouldPreferPointsThenRangesThenValues()
    {
        Assert.AreEqual(3, new BitChartDataset { Data = { 1, 2, 3 } }.Count);
        Assert.AreEqual(2, new BitChartDataset { Data = { 1, 2, 3 }, RangeData = [(0, 1), (1, 2)] }.Count);
        Assert.AreEqual(1, new BitChartDataset { Data = { 1, 2, 3 }, Points = [new(0, 0)] }.Count);
    }

    // ---- robustness ----

    [TestMethod]
    [DataRow("#zzzzzz")]
    [DataRow("#12g")]
    [DataRow("#1234567")]
    [DataRow("#")]
    [DataRow("rgb(a,b,c)")]
    public void AMalformedColorShouldBeRejectedRatherThanThrown(string color)
    {
        Assert.IsFalse(BitChartColorUtil.TryParse(color, out _, out _, out _, out _));
        // The helpers hand the value back untouched, so a typo shows up as a color the browser ignores
        // instead of an exception out of the render.
        Assert.AreEqual(color, BitChartColorUtil.WithAlpha(color, 0.5));
        Assert.AreEqual(color, BitChartColorUtil.Adjust(color, 0.5));
    }

    [TestMethod]
    public void TheShortHexFormShouldSupportAnAlphaDigit()
    {
        Assert.IsTrue(BitChartColorUtil.TryParse("#f008", out var r, out var g, out var b, out var a));
        Assert.AreEqual(255, r);
        Assert.AreEqual(0, g);
        Assert.AreEqual(0, b);
        Assert.AreEqual(136 / 255.0, a, 0.001);
    }

    [TestMethod]
    public void AStepSizeFarSmallerThanTheRangeShouldNotGenerateEndlessTicks()
    {
        var options = new BitChartScaleOptions { Id = "y", Min = 0, Max = 10_000_000 };
        options.Ticks.StepSize = 1;
        var scale = new BitChartAxisScale(options, horizontal: false);
        scale.SetDataRange(0, 10_000_000);
        scale.SetPixelRange(600, 0);

        Assert.IsTrue(scale.Ticks.Count <= 100, $"got {scale.Ticks.Count} ticks");
        Assert.IsTrue(scale.Ticks.Count >= 2);
    }

    [TestMethod]
    public void ATimeAxisPointedAtValuesThatAreNotDatesShouldNotThrow()
    {
        foreach (var (min, max) in new[] { (-1e12, 1e12), (0d, 0d), (double.NaN, 5d), (1e9, -1e9) })
        {
            var ticks = BitChartTimeAxis.Ticks(min, max, BitChartTimeUnit.Auto, null, 8);
            Assert.IsTrue(ticks.Count > 0, $"{min}..{max} produced no ticks");
        }
    }

    [TestMethod]
    public void ATimeAxisAtTheEndOfTheCalendarShouldTerminate()
    {
        double end = DateTime.MaxValue.AddDays(-1).ToOADate();
        var ticks = BitChartTimeAxis.Ticks(end, end + 0.5, BitChartTimeUnit.Year, null, 5);

        Assert.IsTrue(ticks.Count > 0);
        Assert.IsTrue(ticks.Count < 100);
    }
}
