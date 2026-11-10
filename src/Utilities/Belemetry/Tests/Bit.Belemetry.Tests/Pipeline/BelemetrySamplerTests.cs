namespace Bit.Belemetry.Tests.Pipeline;

[TestClass]
public class BelemetrySamplerTests
{
    [TestMethod]
    public void Bounds()
    {
        var id = ActivityTraceId.CreateRandom();
        Assert.IsTrue(BelemetrySampler.ShouldSample(id, 1));
        Assert.IsTrue(BelemetrySampler.ShouldSample(id, 2));
        Assert.IsFalse(BelemetrySampler.ShouldSample(id, 0));
        Assert.IsFalse(BelemetrySampler.ShouldSample(id, -1));
        Assert.IsTrue(BelemetrySampler.ShouldSample("short", 0.5), "A malformed id is sampled rather than lost.");
    }

    [TestMethod]
    public void IsDeterministic_AndAgreesBetweenOverloads()
    {
        for (var i = 0; i < 200; i++)
        {
            var id = ActivityTraceId.CreateRandom();
            var first = BelemetrySampler.ShouldSample(id, 0.3);
            Assert.AreEqual(first, BelemetrySampler.ShouldSample(id, 0.3));
            Assert.AreEqual(first, BelemetrySampler.ShouldSample(id.ToHexString(), 0.3));
            Assert.AreEqual(first, BelemetrySampler.ShouldSample(id.ToHexString().ToUpperInvariant(), 0.3));
        }
    }

    [TestMethod]
    public void TailSampling_NestsInTheHeadRatio()
    {
        // Why a tail sampler sets SamplingRatio to the smaller ratio and does not multiply: a trace kept at the
        // smaller one is kept at the larger one too, so both keep min(head, tail), not head * tail.
        for (var i = 0; i < 2000; i++)
        {
            var id = ActivityTraceId.CreateRandom();
            if (BelemetrySampler.ShouldSample(id, 0.05)) Assert.IsTrue(BelemetrySampler.ShouldSample(id, 0.25));
        }
    }

    [TestMethod]
    public void IsSessionSampled_DecidesAsTheScriptDoes()
    {
        // isSessionSampled: parseInt(id.slice(0, 8), 16) / 2^32 < ratio.
        Assert.IsTrue(BelemetrySampler.IsSessionSampled("00000000" + new string('a', 24), 0.0001));
        Assert.IsTrue(BelemetrySampler.IsSessionSampled("3fffffff" + new string('a', 24), 0.25));
        Assert.IsFalse(BelemetrySampler.IsSessionSampled("40000000" + new string('a', 24), 0.25));
        Assert.IsTrue(BelemetrySampler.IsSessionSampled("3FFFFFFF", 0.25), "Either case.");
        Assert.IsTrue(BelemetrySampler.IsSessionSampled("1z", 0.25), "The digits up to the first that is not one: 1.");
        Assert.IsFalse(BelemetrySampler.IsSessionSampled("zz", 0.25), "No digit: NaN, never kept.");
        Assert.IsFalse(BelemetrySampler.IsSessionSampled("", 0.25));
        Assert.IsTrue(BelemetrySampler.IsSessionSampled("ffffffff", 1), "A ratio of 1 or more keeps every session.");
        Assert.IsTrue(BelemetrySampler.IsSessionSampled("zz", double.NaN));
        Assert.IsFalse(BelemetrySampler.IsSessionSampled("00000000", 0), "0 or less keeps none.");
    }

    [TestMethod]
    public void UsesTheFirstEightBytes_AsTheOTelSdkDoes()
    {
        // abs(0x0000...) = 0 is below any positive threshold; 0x7fff... is above any threshold below 1.
        Assert.IsTrue(BelemetrySampler.ShouldSample("00000000000000000000000000000001", 0.0001));
        Assert.IsFalse(BelemetrySampler.ShouldSample("7fffffffffffffff0000000000000000", 0.9999));
        // A higher ratio samples a superset of a lower ratio.
        Assert.IsTrue(BelemetrySampler.ShouldSample("3fffffffffffffff0000000000000000", 0.6));
        Assert.IsFalse(BelemetrySampler.ShouldSample("3fffffffffffffff0000000000000000", 0.4));
    }

    [TestMethod]
    [DataRow(0.1)]
    [DataRow(0.5)]
    [DataRow(0.9)]
    public void SamplesApproximatelyTheRatio(double ratio)
    {
        const int n = 20_000;
        var sampled = 0;
        for (var i = 0; i < n; i++)
        {
            if (BelemetrySampler.ShouldSample(ActivityTraceId.CreateRandom(), ratio)) sampled++;
        }

        Assert.AreEqual(ratio, (double)sampled / n, 0.03);
    }
}
