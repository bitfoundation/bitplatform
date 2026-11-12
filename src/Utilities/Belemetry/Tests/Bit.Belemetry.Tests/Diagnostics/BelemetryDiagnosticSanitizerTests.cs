namespace Bit.Belemetry.Tests.Diagnostics;

[TestClass]
public class BelemetryDiagnosticSanitizerTests
{
    private static BelemetryDiagnosticSanitizer Create(Action<BelemetryOptions>? configure = null, params IBelemetryDiagnosticSanitizer[] custom)
    {
        var options = new BelemetryOptions();
        configure?.Invoke(options);
        return new BelemetryDiagnosticSanitizer(options, custom);
    }

    [TestMethod]
    public void Credentials_AndBearerSecrets_AreRedactedByKey()
    {
        var sanitizer = Create();
        var counters = new BelemetrySanitizeCounters();

        Assert.AreEqual("REDACTED", sanitizer.Scalar("password", "hunter2", ref counters));
        Assert.AreEqual("REDACTED", sanitizer.Scalar("http.request.header.authorization", "Bearer abc", ref counters));
        Assert.AreEqual("REDACTED", sanitizer.Scalar("CircuitId", "CfDJ8...", ref counters), "A circuit id is a bearer secret.");
        Assert.AreEqual("REDACTED", sanitizer.Scalar("aspnetcore.components.circuit.id", "CfDJ8...", ref counters));
        Assert.AreEqual(4, counters.Redactions);
    }

    [TestMethod]
    public void Patterns_AreScrubbed_EvenWhenTheExportDoesNotScrub()
    {
        var sanitizer = Create(o => o.Privacy.ScrubPatterns = false);
        var counters = new BelemetrySanitizeCounters();

        var text = sanitizer.Text("Mail jane@example.com with Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.c2lnbmF0dXJl, card 4111 1111 1111 1111", 1000, ref counters)!;

        Assert.DoesNotContain("jane@example.com", text);
        Assert.DoesNotContain("eyJ", text);
        Assert.DoesNotContain("4111 1111 1111 1111", text);
        Assert.IsGreaterThan(0, counters.Redactions);
    }

    [TestMethod]
    public void Urls_AreTemplated_AndKeepOnlyAllowlistedQueryParameters()
    {
        var sanitizer = Create(o => o.Diagnostics.AllowedQueryParameters.Add("page"));
        var counters = new BelemetrySanitizeCounters();

        Assert.AreEqual("https://api.shop.test/orders/{n}/lines/{guid}?page=2",
            sanitizer.Url("https://jane:secret@api.shop.test/orders/10423/lines/3f2a8c1e-0b6d-4f3a-9c1e-2b6d4f3a9c1e?page=2&token=abc&email=jane%40x.io#frag", ref counters));
        Assert.AreEqual("/users/{value}/profile", sanitizer.Url("/users/jane@example.com/profile", ref counters));
    }

    [TestMethod]
    public void Trees_KeepSupportedValuesOnly_NeverWalkAnObject()
    {
        var sanitizer = Create();
        var counters = new BelemetrySanitizeCounters();
        var data = new Dictionary<string, object?>
        {
            ["items"] = 3,
            ["total"] = 12.5m,
            ["step"] = DayOfWeek.Monday,
            ["at"] = new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.Zero),
            ["owner"] = new { Name = "Jane", Password = "hunter2" },
            ["apiKey"] = "k-123",
            ["tags"] = new[] { "a", "b" },
            ["nested"] = new Dictionary<string, object?> { ["email"] = "jane@example.com" },
        };

        var tree = sanitizer.Tree(data, new BelemetrySnapshotOptions(), ref counters);

        Assert.AreEqual(3, tree["items"]);
        Assert.AreEqual(12.5, tree["total"]);
        Assert.AreEqual("Monday", tree["step"]);
        Assert.AreEqual("2026-11-04T10:00:00.0000000+00:00", tree["at"]);
        Assert.AreEqual(BelemetryDiagnosticSanitizer.Unsupported, tree["owner"], "An object is never serialized.");
        Assert.AreEqual("REDACTED", tree["apiKey"]);
        Assert.AreSequenceEqual(new object[] { "a", "b" }, ((List<object?>)tree["tags"]!).ToArray());
        Assert.AreEqual("[email]", ((Dictionary<string, object?>)tree["nested"]!)["email"]);
        Assert.AreEqual(1, counters.Omissions);
    }

    [TestMethod]
    public void ALongKey_IsRedactedByItsWholeName_NotByItsCutOne()
    {
        var sanitizer = Create();
        var counters = new BelemetrySanitizeCounters();
        var key = new string('x', 130) + "Password";

        var tree = sanitizer.Tree(new Dictionary<string, object?> { [key] = "hunter2" }, new BelemetrySnapshotOptions(), ref counters);

        Assert.AreEqual("REDACTED", tree[key[..128]], "The cut takes off the part that names the secret.");
    }

    [TestMethod]
    public void Trees_AreBounded_InDepthItemsLengthAndSize()
    {
        var sanitizer = Create();
        var limits = new BelemetrySnapshotOptions { MaxDepth = 2, MaxItems = 3, MaxStringLength = 5, MaxBytes = 300 };

        // A cycle is bounded by the depth limit.
        var cyclic = new Dictionary<string, object?>();
        cyclic["self"] = cyclic;
        var counters = new BelemetrySanitizeCounters();
        var tree = sanitizer.Tree(cyclic, limits, ref counters);
        Assert.AreEqual(BelemetryDiagnosticSanitizer.Truncated, ((Dictionary<string, object?>)tree["self"]!)["self"]);

        counters = new BelemetrySanitizeCounters();
        var wide = Enumerable.Range(0, 10).ToDictionary(i => "k" + i, i => (object?)"abcdefgh");
        tree = sanitizer.Tree(wide, limits, ref counters);
        Assert.HasCount(4, tree, "Three items and a truncation marker.");
        Assert.AreEqual("abcde", tree["k0"]);
        Assert.IsGreaterThan(0, counters.Truncations);

        counters = new BelemetrySanitizeCounters();
        var big = Enumerable.Range(0, 3).ToDictionary(i => "k" + i, i => (object?)new string('x', 4000));
        tree = sanitizer.Tree(big, new BelemetrySnapshotOptions { MaxBytes = 300, MaxStringLength = 100 }, ref counters);
        Assert.Contains(v => Equals(v, BelemetryDiagnosticSanitizer.Truncated), tree.Values, "The byte budget stops the tree.");
    }

    [TestMethod]
    public void ApplicationSanitizers_Run_AndAFailingOne_ExcludesTheValue()
    {
        var sanitizer = Create(null, new MaskTenant(), new Throwing());
        var counters = new BelemetrySanitizeCounters();

        var tree = sanitizer.Tree(new Dictionary<string, object?> { ["tenant"] = "contoso", ["boom"] = "value", ["ok"] = "fine" }, new BelemetrySnapshotOptions(), ref counters);

        Assert.AreEqual("t-***", tree["tenant"]);
        Assert.IsNull(tree["boom"], "Never kept as it was because the sanitizer failed.");
        Assert.AreEqual("fine", tree["ok"]);
        Assert.AreEqual(1, counters.Omissions);
    }

    private sealed class MaskTenant : IBelemetryDiagnosticSanitizer
    {
        public bool TrySanitize(string path, object? value, out object? sanitized)
        {
            sanitized = path == "tenant" ? "t-***" : value;
            return path == "tenant";
        }
    }

    private sealed class Throwing : IBelemetryDiagnosticSanitizer
    {
        public bool TrySanitize(string path, object? value, out object? sanitized)
        {
            if (path == "boom") throw new InvalidOperationException("sanitizer bug");
            sanitized = value;
            return false;
        }
    }
}
