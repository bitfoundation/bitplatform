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
