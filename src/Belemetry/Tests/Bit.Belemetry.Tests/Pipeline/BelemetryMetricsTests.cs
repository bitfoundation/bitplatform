using System.Diagnostics.Metrics;

namespace Bit.Belemetry.Tests.Pipeline;

[TestClass]
public class BelemetryMetricsTests
{
    private static Meter NewMeter() => new("Tests.Metrics." + Guid.NewGuid().ToString("N"), "1.0");

    private static KeyValuePair<string, object?> Tag(string key, object? value) => new(key, value);

    private static BelemetryInstrumentAggregator Aggregator(Instrument instrument, Action<BelemetryMetricsOptions>? configure = null)
    {
        var options = new BelemetryMetricsOptions();
        configure?.Invoke(options);
        return new BelemetryInstrumentAggregator(instrument, options, startTimeUnixNano: 100);
    }

    [TestMethod]
    public void Counter_SumsCumulativeAndDelta()
    {
        using var meter = NewMeter();
        var counter = meter.CreateCounter<long>("requests", "{request}", "Requests served");
        var aggregator = Aggregator(counter);
        aggregator.IsInteger = true;

        Assert.IsNull(aggregator.Collect(150), "Nothing recorded yet.");

        aggregator.Record(1, 1, [Tag("route", "/a")]);
        aggregator.Record(2, 2, [Tag("route", "/a")]);
        aggregator.Record(-5, -5, [Tag("route", "/a")]); // a counter must not decrease

        var first = aggregator.Collect(200)!;
        Assert.AreEqual("requests", first.Name);
        Assert.AreEqual("{request}", first.Unit);
        Assert.AreEqual("Requests served", first.Description);
        Assert.AreEqual(BelemetryMetricType.Sum, first.Type);
        Assert.IsTrue(first.IsMonotonic);
        Assert.AreEqual(meter.Name, first.Scope.Name);
        Assert.AreEqual("1.0", first.Scope.Version);

        var p = first.Points.Single();
        Assert.IsTrue(p.IsInteger);
        Assert.AreEqual(3L, p.LongValue);
        Assert.AreEqual(3L, p.DeltaLongValue);
        Assert.AreEqual(3.0, p.Value);
        Assert.IsTrue(p.HasDeltaUpdate);
        Assert.AreEqual(100L, p.StartTimeUnixNano);
        Assert.AreEqual(100L, p.DeltaStartTimeUnixNano);
        Assert.AreEqual(200L, p.TimeUnixNano);
        Assert.AreEqual("/a", p.Attributes["route"]);

        var second = aggregator.Collect(300)!.Points.Single();
        Assert.AreEqual(3L, second.LongValue);
        Assert.AreEqual(0L, second.DeltaLongValue);
        Assert.IsFalse(second.HasDeltaUpdate);
        Assert.AreEqual(100L, second.StartTimeUnixNano);
        Assert.AreEqual(200L, second.DeltaStartTimeUnixNano);

        aggregator.Record(4, 4, [Tag("route", "/a")]);
        var third = aggregator.Collect(400)!.Points.Single();
        Assert.AreEqual(7L, third.LongValue);
        Assert.AreEqual(4L, third.DeltaLongValue);
        Assert.IsTrue(third.HasDeltaUpdate);
    }

    [TestMethod]
    public void UpDownCounter_AllowsNegatives()
    {
        using var meter = NewMeter();
        var aggregator = Aggregator(meter.CreateUpDownCounter<double>("balance"));

        aggregator.Record(5, 5, []);
        aggregator.Record(-7.5, -7, []);

        var metric = aggregator.Collect(200)!;
        Assert.AreEqual(BelemetryMetricType.Sum, metric.Type);
        Assert.IsFalse(metric.IsMonotonic);
        Assert.AreEqual(-2.5, metric.Points[0].Value);
        Assert.AreEqual(-2.5, metric.Points[0].DeltaValue);
        Assert.IsFalse(metric.Points[0].IsInteger);
    }

    [TestMethod]
    public void Histogram_ValueEqualToABoundGoesIntoThatBucket()
    {
        using var meter = NewMeter();
        var aggregator = Aggregator(meter.CreateHistogram<double>("latency"), o => o.HistogramBoundaries = [0, 5, 10]);
        CollectionAssert.AreEqual(new[] { 0.0, 5, 10 }, aggregator.Bounds);

        foreach (var value in new[] { -1, 0, 5, 5.0001, 10, 11 }) aggregator.Record(value, (long)value, []);
        aggregator.Record(double.NaN, 0, []);
        aggregator.Record(double.PositiveInfinity, 0, []);

        var p = aggregator.Collect(200)!.Points.Single();
        // (-inf,0] (0,5] (5,10] (10,+inf)
        CollectionAssert.AreEqual(new long[] { 2, 1, 2, 1 }, p.BucketCounts);
        CollectionAssert.AreEqual(new long[] { 2, 1, 2, 1 }, p.DeltaBucketCounts);
        CollectionAssert.AreEqual(new[] { 0.0, 5, 10 }, p.ExplicitBounds);
        Assert.AreEqual(6L, p.Count);
        Assert.AreEqual(30.0001, p.Sum, 1e-9);
        Assert.AreEqual(-1.0, p.Min);
        Assert.AreEqual(11.0, p.Max);
        Assert.AreEqual(6L, p.DeltaCount);

        aggregator.Record(7, 7, []);
        var next = aggregator.Collect(300)!.Points.Single();
        Assert.AreEqual(7L, next.Count);
        Assert.AreEqual(1L, next.DeltaCount);
        Assert.AreEqual(7.0, next.DeltaSum);
        Assert.AreEqual(7.0, next.DeltaMin);
        Assert.AreEqual(7.0, next.DeltaMax);
        Assert.AreEqual(-1.0, next.Min);
        CollectionAssert.AreEqual(new long[] { 0, 0, 1, 0 }, next.DeltaBucketCounts);
        CollectionAssert.AreEqual(new long[] { 2, 1, 3, 1 }, next.BucketCounts);

        var empty = aggregator.Collect(400)!.Points.Single();
        Assert.AreEqual(0L, empty.DeltaCount);
        Assert.IsFalse(empty.HasDeltaUpdate);
    }

#if NET9_0_OR_GREATER
    [TestMethod]
    public void Histogram_UsesAdvisedBounds()
    {
        using var meter = NewMeter();
        var histogram = meter.CreateHistogram<double>("advised", unit: null, description: null, tags: null, advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = [1, 2] });
        CollectionAssert.AreEqual(new[] { 1.0, 2.0 }, Aggregator(histogram).Bounds);
    }

    [TestMethod]
    public void Gauge_KeepsTheLastValue()
    {
        using var meter = NewMeter();
        var aggregator = Aggregator(meter.CreateGauge<int>("temperature"));
        aggregator.IsInteger = true;
        aggregator.Record(20, 20, []);
        aggregator.Record(18, 18, []);

        var metric = aggregator.Collect(200)!;
        Assert.AreEqual(BelemetryMetricType.Gauge, metric.Type);
        Assert.AreEqual(18L, metric.Points[0].LongValue);
    }
#endif

    [TestMethod]
    public void ObservableGauge_AndObservableCounter_ThroughTheCollector()
    {
        using var meter = NewMeter();
        var settings = TestHost.CreateSettings(o => o.Metrics.Meters.Add(meter.Name));
        using var collector = new BelemetryMeterCollector(settings);

        var total = 10L;
        var level = 0.25;
        meter.CreateObservableCounter("bytes", () => new Measurement<long>(total, Tag("dir", "in")));
        meter.CreateObservableGauge("level", () => level);

        var first = collector.Collect().ToDictionary(m => m.Name);
        Assert.AreEqual(BelemetryMetricType.Gauge, first["level"].Type);
        Assert.AreEqual(0.25, first["level"].Points[0].Value);

        var bytes = first["bytes"];
        Assert.AreEqual(BelemetryMetricType.Sum, bytes.Type);
        Assert.IsTrue(bytes.IsMonotonic);
        Assert.AreEqual(10L, bytes.Points[0].LongValue);
        Assert.AreEqual(10L, bytes.Points[0].DeltaLongValue);
        Assert.AreEqual("in", bytes.Points[0].Attributes["dir"]);

        total = 15;
        level = 0.75;
        var second = collector.Collect().ToDictionary(m => m.Name);
        Assert.AreEqual(15L, second["bytes"].Points[0].LongValue);
        Assert.AreEqual(5L, second["bytes"].Points[0].DeltaLongValue, "The delta is computed from the cumulative observations.");
        Assert.IsTrue(second["bytes"].Points[0].HasDeltaUpdate);
        Assert.AreEqual(0.75, second["level"].Points[0].Value);

        var third = collector.Collect().ToDictionary(m => m.Name);
        Assert.AreEqual(0L, third["bytes"].Points[0].DeltaLongValue);
    }

    [TestMethod]
    public void ObservableUpDownCounter_ComputesNegativeDeltas()
    {
        using var meter = NewMeter();
        var settings = TestHost.CreateSettings(o => o.Metrics.Meters.Add(meter.Name));
        using var collector = new BelemetryMeterCollector(settings);

        var value = 10.0;
        meter.CreateObservableUpDownCounter("connections", () => value);

        Assert.AreEqual(10.0, collector.Collect()[0].Points[0].DeltaValue);
        value = 4;
        var metric = collector.Collect()[0];
        Assert.IsFalse(metric.IsMonotonic);
        Assert.AreEqual(4.0, metric.Points[0].Value);
        Assert.AreEqual(-6.0, metric.Points[0].DeltaValue);
    }

    [TestMethod]
    public void CardinalityOverflow_FoldsIntoOnePoint()
    {
        using var meter = NewMeter();
        var aggregator = Aggregator(meter.CreateCounter<long>("c"), o => o.CardinalityLimit = 2);

        aggregator.Record(1, 1, [Tag("k", "a")]);
        aggregator.Record(1, 1, [Tag("k", "b")]);
        aggregator.Record(1, 1, [Tag("k", "c")]);
        aggregator.Record(1, 1, [Tag("k", "d")]);
        aggregator.Record(1, 1, [Tag("k", "a")]); // an existing set still has its own point

        var points = aggregator.Collect(200)!.Points;
        Assert.HasCount(3, points);
        Assert.AreEqual(2.0, points.Single(p => "a".Equals(p.Attributes["k"])).Value);
        var overflow = points.Single(p => p.Attributes.Contains("otel.metric.overflow"));
        Assert.AreEqual(true, overflow.Attributes["otel.metric.overflow"]);
        Assert.AreEqual(1, overflow.Attributes.Count);
        Assert.AreEqual(2.0, overflow.Value);
    }

    [TestMethod]
    public void AttributeOrder_DoesNotSplitPoints()
    {
        using var meter = NewMeter();
        var aggregator = Aggregator(meter.CreateCounter<long>("c"));

        aggregator.Record(1, 1, [Tag("a", 1), Tag("b", "x"), Tag("c", true)]);
        aggregator.Record(1, 1, [Tag("c", true), Tag("a", 1), Tag("b", "x")]);
        aggregator.Record(1, 1, [Tag("b", "x"), Tag("c", true), Tag("a", 1L)]);
        aggregator.Record(1, 1, [Tag("a", 2), Tag("b", "x"), Tag("c", true)]);

        var points = aggregator.Collect(200)!.Points;
        Assert.HasCount(2, points);
        Assert.AreEqual(3.0, points.Single(p => 1L.Equals(p.Attributes["a"])).Value);
    }

    [TestMethod]
    public void Collector_OnlyListensToConfiguredMeters_AndHonorsSuppression()
    {
        using var included = new Meter("Tests.Prefix." + Guid.NewGuid().ToString("N"));
        using var excluded = NewMeter();
        var settings = TestHost.CreateSettings(o => o.Metrics.Meters.Add("Tests.Prefix.*"));
        using var collector = new BelemetryMeterCollector(settings);

        var counter = included.CreateCounter<int>("in");
        excluded.CreateCounter<int>("out").Add(1);
        counter.Add(2);
        using (BelemetrySuppression.Begin())
        {
            counter.Add(100);
        }

        var metrics = collector.Collect().Where(m => m.Scope.Name == included.Name || m.Scope.Name == excluded.Name).ToList();
        Assert.HasCount(1, metrics);
        Assert.AreEqual("in", metrics[0].Name);
        Assert.AreEqual(2L, metrics[0].Points[0].LongValue);
        Assert.IsTrue(metrics[0].Points[0].IsInteger);
    }

    [TestMethod]
    public void Collector_FloatingPointInstrumentsAreNotInteger()
    {
        using var meter = NewMeter();
        var settings = TestHost.CreateSettings(o => o.Metrics.Meters.Add(meter.Name));
        using var collector = new BelemetryMeterCollector(settings);

        meter.CreateCounter<double>("d").Add(1.25);
        meter.CreateCounter<decimal>("m").Add(2.5m);

        var metrics = collector.Collect().ToDictionary(m => m.Name);
        Assert.IsFalse(metrics["d"].Points[0].IsInteger);
        Assert.AreEqual(1.25, metrics["d"].Points[0].Value);
        Assert.AreEqual(2.5, metrics["m"].Points[0].Value);
    }
}
