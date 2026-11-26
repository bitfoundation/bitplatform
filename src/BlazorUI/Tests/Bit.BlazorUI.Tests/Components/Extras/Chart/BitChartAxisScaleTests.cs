using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Chart;

/// <summary>Value-to-pixel mapping and tick generation, the arithmetic every chart type sits on.</summary>
[TestClass]
public class BitChartAxisScaleTests
{
    private static BitChartAxisScale Vertical(BitChartScaleOptions options, double dataMin, double dataMax,
        double bottom = 300, double top = 0)
    {
        var scale = new BitChartAxisScale(options, horizontal: false);
        scale.SetDataRange(dataMin, dataMax);
        scale.SetPixelRange(bottom, top);
        return scale;
    }

    private static BitChartScaleOptions Linear(string id = "y") => new() { Id = id, Type = BitChartScaleType.Linear };

    [TestMethod]
    public void ALinearScaleShouldMapItsEndsToThePixelEnds()
    {
        var scale = Vertical(Linear(), 0, 100);

        Assert.AreEqual(300, scale.PixelFor(scale.Min), 0.001);
        Assert.AreEqual(0, scale.PixelFor(scale.Max), 0.001);
    }

    [TestMethod]
    public void ReverseShouldFlipThePixelMapping()
    {
        var options = Linear();
        options.Reverse = true;
        var scale = Vertical(options, 0, 100);

        Assert.IsTrue(scale.PixelFor(scale.Min) < scale.PixelFor(scale.Max));
    }

    [TestMethod]
    public void ExplicitMinMaxShouldWinOverTheData()
    {
        var options = Linear();
        options.Min = -10;
        options.Max = 10;
        var scale = Vertical(options, 0, 100);

        Assert.AreEqual(-10, scale.Min, 1e-9);
        Assert.AreEqual(10, scale.Max, 1e-9);
    }

    [TestMethod]
    public void SuggestedBoundsShouldOnlyEverWidenTheRange()
    {
        var options = Linear();
        options.SuggestedMin = -50;
        options.SuggestedMax = 10;
        var scale = Vertical(options, 0, 100);

        Assert.IsTrue(scale.Min <= -50);
        Assert.IsTrue(scale.Max >= 100, "a suggestion below the data must not clamp it");
    }

    [TestMethod]
    public void BeginAtZeroShouldPullTheRangeToTheOrigin()
    {
        var options = Linear();
        options.BeginAtZero = true;
        var scale = Vertical(options, 40, 100);

        Assert.IsTrue(scale.Min <= 0);
    }

    [TestMethod]
    public void GraceShouldPadBothEndsOfTheRange()
    {
        var plain = Vertical(Linear(), 0, 100);
        var options = Linear();
        options.Grace = 0.1;
        var graced = Vertical(options, 0, 100);

        Assert.IsTrue(graced.Min < plain.Min || graced.Max > plain.Max);
    }

    [TestMethod]
    public void AFlatSeriesShouldStillGetAUsableRange()
    {
        var scale = Vertical(Linear(), 5, 5);
        Assert.IsTrue(scale.Max > scale.Min);

        var zero = Vertical(Linear(), 0, 0);
        Assert.IsTrue(zero.Max > zero.Min);
    }

    [TestMethod]
    public void StepSizeShouldSpaceTheTicksExactly()
    {
        var options = Linear();
        options.Min = 0;
        options.Max = 100;
        options.Ticks.StepSize = 25;
        var scale = Vertical(options, 0, 100, bottom: 600);

        var values = scale.Ticks.Select(t => t.Value).ToList();
        CollectionAssert.AreEqual(new[] { 0d, 25, 50, 75, 100 }, values);
    }

    [TestMethod]
    public void MaxTicksLimitShouldCapTheTickCount()
    {
        var options = Linear();
        options.Ticks.MaxTicksLimit = 3;
        var scale = Vertical(options, 0, 1000, bottom: 900);

        Assert.IsTrue(scale.Ticks.Count <= 4, $"got {scale.Ticks.Count}");
    }

    [TestMethod]
    public void TheTickCountShouldFollowTheSpaceAvailable()
    {
        int Count(double height)
        {
            var scale = Vertical(Linear(), 0, 100, bottom: height);
            return scale.Ticks.Count;
        }

        Assert.IsTrue(Count(60) < Count(600), "a short axis cannot carry as many labels as a tall one");
    }

    [TestMethod]
    public void TurningOffAutoSkipTicksShouldRestoreTheRequestedCount()
    {
        var options = Linear();
        options.AutoSkipTicks = false;
        var scale = Vertical(options, 0, 100, bottom: 40);

        Assert.IsTrue(scale.Ticks.Count > 5, $"got {scale.Ticks.Count}");
    }

    [TestMethod]
    public void TickCallbackShouldReplaceTheLabel()
    {
        var options = Linear();
        options.Ticks.Callback = (v, _) => $"[{v}]";
        var scale = Vertical(options, 0, 100);

        Assert.IsTrue(scale.Ticks.All(t => t.Label.StartsWith("[")));
    }

    [TestMethod]
    public void PrefixAndSuffixShouldWrapTheLabel()
    {
        var options = Linear();
        options.Ticks.Prefix = "$";
        options.Ticks.Suffix = "k";
        var scale = Vertical(options, 0, 100);

        Assert.IsTrue(scale.Ticks.All(t => t.Label.StartsWith("$") && t.Label.EndsWith("k")),
            string.Join("|", scale.Ticks.Select(t => t.Label)));
    }

    [TestMethod]
    public void PrecisionShouldFixTheDecimalPlaces()
    {
        var options = Linear();
        options.Ticks.Precision = 2;
        var scale = Vertical(options, 0, 1);

        Assert.IsTrue(scale.Ticks.All(t => t.Label.Split('.').Last().Length == 2),
            string.Join("|", scale.Ticks.Select(t => t.Label)));
    }

    [TestMethod]
    public void CultureShouldDecideTheDecimalAndGroupSeparators()
    {
        var scale = new BitChartAxisScale(Linear(), horizontal: false) { Culture = new CultureInfo("de-DE") };
        scale.SetDataRange(0, 5000);
        scale.SetPixelRange(600, 0);

        Assert.IsTrue(scale.Ticks.Any(t => t.Label.Contains('.')), string.Join("|", scale.Ticks.Select(t => t.Label)));
    }

    // ---- logarithmic ----

    [TestMethod]
    public void ALogScaleShouldPlaceDecadesEvenly()
    {
        var options = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Logarithmic };
        var scale = Vertical(options, 1, 1000);

        double p1 = scale.PixelFor(1), p10 = scale.PixelFor(10), p100 = scale.PixelFor(100);
        Assert.AreEqual(p1 - p10, p10 - p100, 0.5, "each decade must take the same amount of space");
    }

    [TestMethod]
    public void ALogScaleShouldEmitMajorAndMinorTicks()
    {
        var options = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Logarithmic };
        var scale = Vertical(options, 1, 1000);

        Assert.IsTrue(scale.Ticks.Any(t => !t.Minor && t.Label.Length > 0));
        Assert.IsTrue(scale.Ticks.Any(t => t.Minor), "the in-between gridlines are what make a log axis readable");
    }

    [TestMethod]
    public void ALogScaleShouldNotTakeANonPositiveMinimum()
    {
        var options = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Logarithmic };
        var scale = Vertical(options, 0, 100);

        Assert.IsTrue(scale.Min > 0);
    }

    // ---- category ----

    [TestMethod]
    public void ACategoryScaleShouldCenterIndexesInTheirBand()
    {
        var options = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Category };
        var categories = new List<string> { "A", "B", "C", "D" };
        var scale = new BitChartAxisScale(options, horizontal: true, categories);
        scale.SetDataRange(0, 3);
        scale.SetPixelRange(0, 400);

        Assert.AreEqual(50, scale.PixelForIndex(0, centered: true), 0.001);
        Assert.AreEqual(350, scale.PixelForIndex(3, centered: true), 0.001);
        Assert.AreEqual(100, scale.BandWidth(), 0.001);
    }

    [TestMethod]
    public void ACategoryScaleWithoutOffsetShouldPinTheEndsToTheAxis()
    {
        var options = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Category };
        var categories = new List<string> { "A", "B", "C" };
        var scale = new BitChartAxisScale(options, horizontal: true, categories);
        scale.SetDataRange(0, 2);
        scale.SetPixelRange(0, 400);

        Assert.AreEqual(0, scale.PixelForIndex(0, centered: false), 0.001);
        Assert.AreEqual(400, scale.PixelForIndex(2, centered: false), 0.001);
    }

    [TestMethod]
    public void ACategoryScaleShouldSkipLabelsItCannotFit()
    {
        var options = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Category };
        options.Ticks.MaxTicksLimit = 5;
        var categories = Enumerable.Range(0, 40).Select(i => $"C{i}").ToList();
        var scale = new BitChartAxisScale(options, horizontal: true, categories);
        scale.SetDataRange(0, 39);
        scale.SetPixelRange(0, 800);

        Assert.IsTrue(scale.Ticks.Count <= 6, $"got {scale.Ticks.Count}");
    }

    [TestMethod]
    [DataRow(30, 8, 900)]
    [DataRow(60, 6, 900)]
    [DataRow(40, 5, 800)]
    [DataRow(31, 7, 900)]
    [DataRow(100, 11, 600)]
    public void ACategoryScaleShouldKeepTheFirstAndLastLabelsWithoutCrowdingEither(int n, int limit, double width)
    {
        var options = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Category };
        options.Ticks.MaxTicksLimit = limit;
        var categories = Enumerable.Range(0, n).Select(i => $"C{i}").ToList();
        var scale = new BitChartAxisScale(options, horizontal: true, categories);
        scale.SetDataRange(0, n - 1);
        scale.SetPixelRange(0, width);

        var indexes = scale.Ticks.Select(t => (int)t.Value).ToList();
        string got = string.Join(",", indexes);
        Assert.IsTrue(indexes.Count <= limit, $"more labels than the limit: {got}");
        Assert.AreEqual(0, indexes[0], got);
        Assert.AreEqual(n - 1, indexes[^1], got);

        // The labels before the last one fall on one regular stride - at least what the limit asks for - and the last
        // label is never closer to the one before it than that stride, nor so far that another label would fit between.
        var gaps = indexes.Zip(indexes.Skip(1), (a, b) => b - a).ToList();
        int stride = gaps[0];
        Assert.IsTrue(stride >= (int)System.Math.Ceiling((double)n / limit), $"the labels are closer than the limit allows: {got}");
        Assert.IsTrue(gaps.Take(gaps.Count - 1).All(g => g == stride), $"the labels must fall on a regular stride: {got}");
        Assert.IsTrue(gaps[^1] >= stride, $"the last label must not crowd the one before it: {got}");
        Assert.IsTrue(gaps[^1] < 2 * stride, $"a label is missing before the last one: {got}");
    }

    [TestMethod]
    public void ACategoryScaleShouldKeepItsLabelsInPlaceWhileTheEndOfTheRangeMoves()
    {
        var options = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Category };
        options.Ticks.MaxTicksLimit = 6;
        var categories = Enumerable.Range(0, 60).Select(i => $"C{i}").ToList();
        var scale = new BitChartAxisScale(options, horizontal: true, categories);

        scale.SetDataRange(0, 59);
        scale.SetPixelRange(0, 900);
        var before = scale.Ticks.Select(t => (int)t.Value).SkipLast(1).ToList();

        // A zoom from the left edge: only the end of the range moves.
        scale.Forced = (0, 57);
        scale.SetDataRange(0, 59);
        scale.SetPixelRange(0, 900);
        var after = scale.Ticks.Select(t => (int)t.Value).ToList();

        CollectionAssert.IsSubsetOf(before, after, $"before: {string.Join(",", before)}; after: {string.Join(",", after)}");
    }

    [TestMethod]
    public void ACategoryScaleLimitedToOneLabelShouldKeepTheFirstOnly()
    {
        var options = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Category };
        options.Ticks.MaxTicksLimit = 1;
        var categories = Enumerable.Range(0, 12).Select(i => $"C{i}").ToList();
        var scale = new BitChartAxisScale(options, horizontal: true, categories);
        scale.SetDataRange(0, 11);
        scale.SetPixelRange(0, 900);

        CollectionAssert.AreEqual(new[] { "C0" }, scale.Ticks.Select(t => t.Label).ToArray());
    }

    [TestMethod]
    public void TickCallbackIndexShouldSkipTheMinorTicksOfALogScale()
    {
        var options = new BitChartScaleOptions { Id = "y", Type = BitChartScaleType.Logarithmic };
        var indexes = new List<int>();
        options.Ticks.Callback = (v, i) => { indexes.Add(i); return v.ToString(CultureInfo.InvariantCulture); };
        var scale = new BitChartAxisScale(options, horizontal: false);
        scale.SetDataRange(1, 1000);
        scale.SetPixelRange(400, 0);

        var majors = scale.Ticks.Count(t => !t.Minor);
        CollectionAssert.AreEqual(Enumerable.Range(0, majors).ToList(), indexes.TakeLast(majors).ToList());
    }

    [TestMethod]
    public void ACategoryScaleShouldKeepEveryLabelWithoutAutoSkip()
    {
        var options = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Category };
        options.Ticks.AutoSkip = false;
        options.Ticks.MaxTicksLimit = 5;
        var categories = Enumerable.Range(0, 30).Select(i => $"C{i}").ToList();
        var scale = new BitChartAxisScale(options, horizontal: true, categories);
        scale.SetDataRange(0, 29);
        scale.SetPixelRange(0, 900);

        Assert.AreEqual(30, scale.Ticks.Count);
    }

    [TestMethod]
    public void TickCallbackShouldReplaceACategoryLabel()
    {
        var options = new BitChartScaleOptions { Id = "x", Type = BitChartScaleType.Category };
        options.Ticks.AutoSkip = false;
        var categories = Enumerable.Range(0, 10).Select(i => $"C{i}").ToList();
        options.Ticks.Callback = (v, i) => (9 - (int)v) % 3 == 0 ? $"{categories[(int)v]}#{i}" : "";
        var scale = new BitChartAxisScale(options, horizontal: true, categories);
        scale.SetDataRange(0, 9);
        scale.SetPixelRange(0, 900);

        CollectionAssert.AreEqual(new[] { "C0#0", "", "", "C3#3", "", "", "C6#6", "", "", "C9#9" },
            scale.Ticks.Select(t => t.Label).ToArray());
    }

    // ---- zoom ----

    [TestMethod]
    public void AForcedRangeShouldOverrideTheData()
    {
        var scale = new BitChartAxisScale(Linear(), horizontal: false) { Forced = (25, 75) };
        scale.SetDataRange(0, 100);
        scale.SetPixelRange(300, 0);

        Assert.AreEqual(25, scale.Min, 1e-9);
        Assert.AreEqual(75, scale.Max, 1e-9);
        Assert.AreEqual(300, scale.PixelFor(25), 0.001);
    }

    [TestMethod]
    public void AForcedRangeShouldNotBeRoundedToNiceNumbers()
    {
        var scale = new BitChartAxisScale(Linear(), horizontal: false) { Forced = (13.7, 41.3) };
        scale.SetDataRange(0, 100);
        scale.SetPixelRange(300, 0);

        Assert.AreEqual(13.7, scale.Min, 1e-9);
        Assert.AreEqual(41.3, scale.Max, 1e-9);
    }

    // ---- nice numbers ----

    [TestMethod]
    [DataRow(0.0, 1.0)]
    [DataRow(-5.0, 1.0)]
    [DataRow(1.1, 1.0)]
    [DataRow(2.4, 2.0)]
    [DataRow(6.0, 5.0)]
    [DataRow(9.0, 10.0)]
    public void NiceNumberShouldRoundToAFriendlyStep(double value, double expected)
    {
        Assert.AreEqual(expected, BitChartAxisScale.NiceNumber(value, round: true), 1e-9);
    }
}
