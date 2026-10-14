namespace Bit.Belemetry.Tests.Model;

[TestClass]
public class BelemetryAttributesTests
{
    private enum Color { Red }

    [TestMethod]
    public void Normalize_MapsEveryTypeToAnAnyValueType()
    {
        var guid = Guid.NewGuid();
        var utc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        Assert.AreEqual("s", BelemetryAttributes.Normalize("s"));
        Assert.AreEqual(true, BelemetryAttributes.Normalize(true));
        Assert.AreEqual(5L, BelemetryAttributes.Normalize(5L));
        Assert.AreEqual(5L, BelemetryAttributes.Normalize(5));
        Assert.AreEqual(5L, BelemetryAttributes.Normalize((short)5));
        Assert.AreEqual(5L, BelemetryAttributes.Normalize((byte)5));
        Assert.AreEqual(-5L, BelemetryAttributes.Normalize((sbyte)-5));
        Assert.AreEqual(5L, BelemetryAttributes.Normalize((ushort)5));
        Assert.AreEqual(5L, BelemetryAttributes.Normalize(5u));
        Assert.AreEqual(5L, BelemetryAttributes.Normalize(5UL));
        Assert.AreEqual(ulong.MaxValue.ToString(), BelemetryAttributes.Normalize(ulong.MaxValue));
        Assert.AreEqual(1.5, BelemetryAttributes.Normalize(1.5));
        Assert.AreEqual(1.5, BelemetryAttributes.Normalize(1.5f));
        Assert.AreEqual(1.5, BelemetryAttributes.Normalize(1.5m));
        Assert.AreEqual("c", BelemetryAttributes.Normalize('c'));
        Assert.AreEqual("Red", BelemetryAttributes.Normalize(Color.Red));
        Assert.AreEqual("2026-01-02T03:04:05.0000000Z", BelemetryAttributes.Normalize(utc));
        Assert.AreEqual("2026-01-02T03:04:05.0000000+00:00", BelemetryAttributes.Normalize(new DateTimeOffset(utc)));
        Assert.AreEqual(1500.0, BelemetryAttributes.Normalize(TimeSpan.FromSeconds(1.5)));
        Assert.AreEqual(guid.ToString(), BelemetryAttributes.Normalize(guid));
        Assert.AreEqual("https://example.com/", BelemetryAttributes.Normalize(new Uri("https://example.com")));
        Assert.IsNull(BelemetryAttributes.Normalize(null));

        var bytes = new byte[] { 1 };
        Assert.AreSame(bytes, BelemetryAttributes.Normalize(bytes));
        CollectionAssert.AreEqual(new[] { "a" }, (string[])BelemetryAttributes.Normalize(new[] { "a" })!);
        CollectionAssert.AreEqual(new[] { 1L }, (long[])BelemetryAttributes.Normalize(new[] { 1L })!);
        CollectionAssert.AreEqual(new[] { 1.5 }, (double[])BelemetryAttributes.Normalize(new[] { 1.5 })!);
        CollectionAssert.AreEqual(new[] { true }, (bool[])BelemetryAttributes.Normalize(new[] { true })!);
        CollectionAssert.AreEqual(new[] { 1L, 2L }, (long[])BelemetryAttributes.Normalize(new[] { 1, 2 })!);
        CollectionAssert.AreEqual(new[] { 0.5 }, (double[])BelemetryAttributes.Normalize(new[] { 0.5f })!);
        CollectionAssert.AreEqual(new[] { "x", "y" }, (string[])BelemetryAttributes.Normalize(new List<string> { "x", "y" })!);
        CollectionAssert.AreEqual(new[] { "1", "true" }, (string[])BelemetryAttributes.Normalize(new List<object> { 1, true })!);
        Assert.AreEqual("custom", BelemetryAttributes.Normalize(new Custom()));
    }

    [TestMethod]
    public void ToInvariantString_FormatsEveryType()
    {
        Assert.AreEqual(string.Empty, BelemetryAttributes.ToInvariantString(null));
        Assert.AreEqual("true", BelemetryAttributes.ToInvariantString(true));
        Assert.AreEqual("1.5", BelemetryAttributes.ToInvariantString(1.5));
        Assert.AreEqual("-3", BelemetryAttributes.ToInvariantString(-3L));
        Assert.AreEqual("AQI=", BelemetryAttributes.ToInvariantString(new byte[] { 1, 2 }));
        Assert.AreEqual("[a,b]", BelemetryAttributes.ToInvariantString(new[] { "a", "b" }));
        Assert.AreEqual("[1,2]", BelemetryAttributes.ToInvariantString(new[] { 1L, 2L }));
        Assert.AreEqual("[0.5]", BelemetryAttributes.ToInvariantString(new[] { 0.5 }));
        Assert.AreEqual("[true,false]", BelemetryAttributes.ToInvariantString(new[] { true, false }));
    }

    [TestMethod]
    public void SetReplaceRemove()
    {
        var attributes = new BelemetryAttributes().Set("a", 1).Set("b", "x");
        attributes.Set("a", 2);
        Assert.AreEqual(2, attributes.Count);
        Assert.AreEqual(2L, attributes["a"]);
        CollectionAssert.AreEqual(new[] { "a", "b" }, attributes.Select(kv => kv.Key).ToArray(), "Replacing keeps the position.");

        attributes.SetIfAbsent("a", 3).SetIfAbsent("c", true);
        Assert.AreEqual(2L, attributes["a"]);
        Assert.AreEqual(true, attributes["c"]);

        attributes.Set("b", null);
        Assert.IsFalse(attributes.Contains("b"));
        attributes["c"] = null;
        Assert.IsFalse(attributes.Contains("c"));
        Assert.IsFalse(attributes.Remove("missing"));
        Assert.IsTrue(attributes.Remove("a"));
        Assert.AreEqual(0, attributes.Count);

        attributes.Set(string.Empty, "ignored");
        Assert.AreEqual(0, attributes.Count);

        attributes.SetRange([new("x", 1), new("y", "2"), new("z", null)]);
        Assert.AreEqual(2, attributes.Count);
        Assert.AreEqual(1, attributes.RemoveWhere((k, _) => k == "x"));
        Assert.AreEqual(2L, attributes.GetInt64("y"));
        Assert.AreEqual(2.0, attributes.GetDouble("y"));
        Assert.AreEqual("2", attributes.GetString("y"));
        Assert.IsNull(attributes.GetInt64("missing"));
        Assert.IsNull(attributes.GetString("missing"));
    }

    [TestMethod]
    public void Getters_ConvertNumbers()
    {
        var attributes = new BelemetryAttributes().Set("l", 7).Set("d", 7.9).Set("s", "abc").Set("b", true);
        Assert.AreEqual(7L, attributes.GetInt64("l"));
        Assert.AreEqual(7.0, attributes.GetDouble("l"));
        Assert.AreEqual(7L, attributes.GetInt64("d"));
        Assert.AreEqual(7.9, attributes.GetDouble("d"));
        Assert.IsNull(attributes.GetInt64("s"));
        Assert.IsNull(attributes.GetDouble("b"));
        Assert.AreEqual("true", attributes.GetString("b"));
    }

    [TestMethod]
    public void Limit_CountsDroppedAttributes()
    {
        var attributes = new BelemetryAttributes(2).Set("a", 1).Set("b", 2).Set("c", 3).Set("d", 4);
        Assert.AreEqual(2, attributes.Count);
        Assert.AreEqual(2, attributes.DroppedCount);

        // Replacing an existing key is not a new attribute, so it is never dropped.
        attributes.Set("a", 10);
        Assert.AreEqual(10L, attributes["a"]);
        Assert.AreEqual(2, attributes.DroppedCount);

        var clone = attributes.Clone();
        Assert.AreEqual(2, clone.DroppedCount);
        clone.Set("a", 99);
        Assert.AreEqual(10L, attributes["a"], "A clone is independent.");

        attributes.Clear();
        Assert.AreEqual(0, attributes.Count);
        Assert.AreEqual(0, attributes.DroppedCount);
    }

    [TestMethod]
    public void DefaultLimit_Is128()
    {
        var attributes = new BelemetryAttributes();
        for (var i = 0; i < 130; i++) attributes.Set("k" + i, i);
        Assert.AreEqual(BelemetryAttributes.DefaultLimit, attributes.Count);
        Assert.AreEqual(2, attributes.DroppedCount);

        // A non-positive limit falls back to the default.
        var fallback = new BelemetryAttributes(0);
        for (var i = 0; i < 130; i++) fallback.Set("k" + i, i);
        Assert.AreEqual(BelemetryAttributes.DefaultLimit, fallback.Count);
    }

    [TestMethod]
    public void Resource_MergeOverlays()
    {
        var resource = new BelemetryResource(new BelemetryAttributes().Set("service.name", "a").Set("x", 1));
        var merged = resource.Merge(new BelemetryAttributes().Set("x", 2).Set("y", 3));
        Assert.AreEqual("a", merged.ServiceName);
        Assert.AreEqual(2L, merged.Attributes["x"]);
        Assert.AreEqual(3L, merged.Attributes["y"]);
        Assert.AreEqual(1L, resource.Attributes["x"]);
        Assert.AreEqual("unknown_service", new BelemetryResource().ServiceName);
    }

    private sealed class Custom
    {
        public override string ToString() => "custom";
    }
}
