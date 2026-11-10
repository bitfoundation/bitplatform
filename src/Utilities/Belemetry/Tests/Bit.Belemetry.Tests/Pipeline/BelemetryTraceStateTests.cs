namespace Bit.Belemetry.Tests.Pipeline;

[TestClass]
public class BelemetryTraceStateTests
{
    [TestMethod]
    public void WithSamplingRatio_PutsTheEntryFirst_ReplacingOneItHad()
    {
        Assert.AreEqual("belemetry=sr:0.25", BelemetryTraceState.WithSamplingRatio(null, 0.25));
        Assert.AreEqual("belemetry=sr:0.25", BelemetryTraceState.WithSamplingRatio(" ", 0.25));
        Assert.AreEqual("belemetry=sr:0.1,vendor=1,other=a b", BelemetryTraceState.WithSamplingRatio("vendor=1, other=a b", 0.1));
        Assert.AreEqual("belemetry=sr:0.5,vendor=1", BelemetryTraceState.WithSamplingRatio("vendor=1,belemetry=sr:0.25,,", 0.5));
        Assert.AreEqual("belemetry=sr:1E-05", BelemetryTraceState.WithSamplingRatio(null, 0.00001));
    }

    [TestMethod]
    public void WithSamplingRatio_KeepsTheW3CLimitOf32Members()
    {
        var state = string.Join(",", Enumerable.Range(0, 32).Select(i => $"k{i}=v"));
        var members = BelemetryTraceState.WithSamplingRatio(state, 0.5).Split(',');
        Assert.HasCount(32, members);
        Assert.AreEqual("belemetry=sr:0.5", members[0]);
        Assert.AreEqual("k30=v", members[^1], "The last one is left out.");
    }

    [TestMethod]
    public void SamplingRatio_ReadsTheEntry_And1WithoutOne()
    {
        Assert.AreEqual(0.25, BelemetryTraceState.SamplingRatio("belemetry=sr:0.25"));
        Assert.AreEqual(0.25, BelemetryTraceState.SamplingRatio("vendor=1, belemetry=sr:0.25 ,x=y"));
        Assert.AreEqual(0.1, BelemetryTraceState.SamplingRatio("belemetry=future:1;sr:0.1"), "Fields it does not know are skipped.");
        Assert.AreEqual(0.00001, BelemetryTraceState.SamplingRatio(BelemetryTraceState.WithSamplingRatio(null, 0.00001)));
        Assert.AreEqual(1, BelemetryTraceState.SamplingRatio(null));
        Assert.AreEqual(1, BelemetryTraceState.SamplingRatio(""));
        Assert.AreEqual(1, BelemetryTraceState.SamplingRatio("vendor=1"));
        Assert.AreEqual(1, BelemetryTraceState.SamplingRatio("xbelemetry=sr:0.25"), "Another key.");
        Assert.AreEqual(1, BelemetryTraceState.SamplingRatio("t@belemetry=sr:0.25"), "Another tenant's key.");
        Assert.AreEqual(1, BelemetryTraceState.SamplingRatio("belemetry=sr:0"));
        Assert.AreEqual(1, BelemetryTraceState.SamplingRatio("belemetry=sr:1"));
        Assert.AreEqual(1, BelemetryTraceState.SamplingRatio("belemetry=sr:-0.5"));
        Assert.AreEqual(1, BelemetryTraceState.SamplingRatio("belemetry=sr:NaN"));
        Assert.AreEqual(1, BelemetryTraceState.SamplingRatio("belemetry=sr:abc"));
    }
}
