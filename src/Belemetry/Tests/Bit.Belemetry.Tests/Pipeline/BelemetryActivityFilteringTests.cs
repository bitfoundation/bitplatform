namespace Bit.Belemetry.Tests.Pipeline;

/// <summary>The two cheap filters that need no processor: ignored activity names and ignored request paths.</summary>
[TestClass]
public class BelemetryActivityFilteringTests
{
    private static string NewSourceName() => "Tests.Filtering." + Guid.NewGuid().ToString("N");

    [TestMethod]
    public async Task IgnoredActivityNames_AreNeverCreated_TheirChildrenTakeTheParent()
    {
        using var source = new ActivitySource(NewSourceName());
        await using var host = TestHost.Create(o =>
        {
            o.Traces.Sources.Add(source.Name);
            o.Traces.IgnoredActivityNames.Add("Microsoft.AspNetCore.Components.HandleEvent");
            o.Traces.IgnoredActivityNames.Add("Noise.*");
        });
        using var collector = new BelemetryActivityCollector(host.Settings, host.Pipeline);

        using (var root = source.StartActivity("Root"))
        {
            Assert.IsNotNull(root);
            using (var ignored = source.StartActivity("Microsoft.AspNetCore.Components.HandleEvent"))
            {
                Assert.IsNull(ignored, "An ignored activity is not created at all.");
                Assert.AreSame(root, Activity.Current);
                using var child = source.StartActivity("Child");
                Assert.AreEqual(root.SpanId, child!.ParentSpanId, "The child of an ignored activity attaches to the ignored activity's parent.");
            }
            Assert.IsNull(source.StartActivity("Noise.Tick"), "A trailing * ignores a prefix.");
            Assert.IsNull(source.StartActivity("noise.tick"), "Case-insensitive, as the sources list is.");
            Assert.IsNotNull(source.StartActivity("NoiseNot")?.Also(a => a.Stop()));
        }

        await host.FlushAsync();
        CollectionAssert.AreEquivalent(new[] { "Root", "Child", "NoiseNot" }, host.Memory.Spans.Select(s => s.Name).ToArray());
    }

    [TestMethod]
    public async Task IgnoredRequestPaths_DropTheServerSpanByPathOrRoute()
    {
        using var source = new ActivitySource("Microsoft.AspNetCore");
        await using var host = TestHost.Create(o =>
        {
            o.Host = BelemetryHostKind.Server;
            o.Server.CollectDotnetTelemetry = true;
            o.Server.IgnoredRequestPaths.Add("/health");
            o.Server.IgnoredRequestPaths.Add("/_framework");
        });
        using var collector = new BelemetryActivityCollector(host.Settings, host.Pipeline);

        Request("url.path", "/health");
        Request("url.path", "/HEALTH/ready");
        Request("http.route", "/health");
        Request("url.path", "/_framework/blazor.web.js");
        Request("url.path", "/api/orders");
        Request("http.route", "/api/orders/{id}");
        Request("custom", "/health");

        await host.FlushAsync();
        CollectionAssert.AreEquivalent(new[] { "/api/orders", "/api/orders/{id}", "/health" },
            host.Memory.Spans.Select(s => s.Attributes.GetString("url.path") ?? s.Attributes.GetString("http.route") ?? s.Attributes.GetString("custom")).ToArray());

        void Request(string tag, string value)
        {
            using var activity = source.StartActivity(BelemetryAspNetCoreRequests.ActivityName, ActivityKind.Server);
            Assert.IsNotNull(activity);
            activity.SetTag(tag, value);
        }
    }

    [TestMethod]
    public async Task IgnoredRequestPaths_OnlyApplyToRequestSpans()
    {
        using var source = new ActivitySource(NewSourceName());
        await using var host = TestHost.Create(o =>
        {
            o.Host = BelemetryHostKind.Server;
            o.Server.CollectDotnetTelemetry = true;
            o.Server.IgnoredRequestPaths.Add("/health");
            o.Traces.Sources.Add(source.Name);
        });
        using var collector = new BelemetryActivityCollector(host.Settings, host.Pipeline);

        using (var activity = source.StartActivity("HealthCheck"))
        {
            activity!.SetTag("url.path", "/health");
        }

        await host.FlushAsync();
        Assert.AreEqual("HealthCheck", host.Memory.Spans.Single().Name);
    }

    [TestMethod]
    public void Sampler_IsPublicAndDeterministic()
    {
        var traceId = ActivityTraceId.CreateFromString(Records.TraceId);
        Assert.AreEqual(BelemetrySampler.ShouldSample(traceId, 0.05), BelemetrySampler.ShouldSample(Records.TraceId, 0.05));
        Assert.IsTrue(BelemetrySampler.ShouldSample(Records.TraceId, 1));
        Assert.IsFalse(BelemetrySampler.ShouldSample(Records.TraceId, 0));
        Assert.IsTrue(BelemetrySampler.ShouldSample("not-a-trace-id", 0.01), "What cannot be hashed is kept.");
    }
}

file static class ActivityExtensions
{
    public static Activity Also(this Activity activity, Action<Activity> action)
    {
        action(activity);
        return activity;
    }
}
