using System.Diagnostics.Metrics;

namespace Bit.Belemetry.Tests.Pipeline;

[TestClass]
public class BelemetryActivityCollectorTests
{
    private static string NewSourceName() => "Tests.Traces." + Guid.NewGuid().ToString("N");

    private static (TestHost Host, BelemetryActivityCollector Collector, ActivitySource Source) Create(
        double ratio = 1, BelemetryFeatures consent = BelemetryFeatures.All, Action<BelemetryOptions>? configure = null)
    {
        var source = new ActivitySource(NewSourceName(), "4.5.6");
        var host = TestHost.Create(o =>
        {
            o.Traces.Sources.Add(source.Name);
            o.Traces.SamplingRatio = ratio;
            configure?.Invoke(o);
        }, consent);
        return (host, new BelemetryActivityCollector(host.Settings, host.Pipeline), source);
    }

    [TestMethod]
    public async Task ConvertsAStoppedActivity()
    {
        var (host, collector, source) = Create();
        await using var _ = host;
        using var __ = collector;
        using var ___ = source;

        var linked = new ActivityContext(ActivityTraceId.CreateFromString(Records.LinkTraceId), ActivitySpanId.CreateFromString(Records.LinkSpanId), ActivityTraceFlags.Recorded, "lk=lv");
        var links = new[] { new ActivityLink(linked, new ActivityTagsCollection { ["link.attr"] = 1 }) };

        string traceId, spanId;
        DateTime startUtc;
        using (var activity = source.StartActivity("GET /orders", ActivityKind.Client, parentContext: default, tags: null, links: links)!)
        {
            Assert.IsNotNull(activity);
            Assert.IsTrue(activity.Recorded);
            activity.TraceStateString = "vendor=1";
            activity.SetTag("http.request.method", "GET");
            activity.SetTag("http.response.status_code", 503);
            activity.SetTag("ratio", 0.5);
            activity.SetTag("ok", false);
            activity.SetTag("list", new[] { "a", "b" });
            activity.AddEvent(new ActivityEvent("retry", DateTimeOffset.UtcNow, new ActivityTagsCollection { ["attempt"] = 2 }));
            activity.SetStatus(ActivityStatusCode.Error, "unavailable");
            traceId = activity.TraceId.ToHexString();
            spanId = activity.SpanId.ToHexString();
            startUtc = activity.StartTimeUtc;
        }

        await host.FlushAsync();
        var span = host.Memory.Spans.Single();

        Assert.AreEqual(traceId, span.TraceId);
        Assert.AreEqual(spanId, span.SpanId);
        Assert.IsNull(span.ParentSpanId);
        Assert.AreEqual("vendor=1", span.TraceState);
        Assert.AreEqual(1, span.Flags);
        Assert.AreEqual("GET /orders", span.Name);
        Assert.AreEqual(BelemetrySpanKind.Client, span.Kind);
        Assert.AreEqual(BelemetryTime.ToUnixNano(startUtc), span.StartTimeUnixNano);
        Assert.IsTrue(span.EndTimeUnixNano >= span.StartTimeUnixNano);
        Assert.AreEqual(source.Name, span.Scope.Name);
        Assert.AreEqual("4.5.6", span.Scope.Version);
        Assert.AreEqual(BelemetryFeatures.Traces, span.Feature);

        Assert.AreEqual("GET", span.Attributes["http.request.method"]);
        Assert.AreEqual(503L, span.Attributes["http.response.status_code"]);
        Assert.AreEqual(0.5, span.Attributes["ratio"]);
        Assert.AreEqual(false, span.Attributes["ok"]);
        CollectionAssert.AreEqual(new[] { "a", "b" }, (string[])span.Attributes["list"]!);

        Assert.AreEqual("retry", span.Events.Single().Name);
        Assert.AreEqual(2L, span.Events[0].Attributes["attempt"]);
        Assert.IsTrue(span.Events[0].TimeUnixNano > 0);

        var link = span.Links.Single();
        Assert.AreEqual(Records.LinkTraceId, link.TraceId);
        Assert.AreEqual(Records.LinkSpanId, link.SpanId);
        Assert.AreEqual("lk=lv", link.TraceState);
        Assert.AreEqual(1L, link.Attributes["link.attr"]);

        Assert.AreEqual(BelemetryStatusCode.Error, span.Status);
        Assert.AreEqual("unavailable", span.StatusMessage);
    }

    [TestMethod]
    public void Convert_MapsKindsAndChildParent()
    {
        using var source = new ActivitySource(NewSourceName());
        using var listener = RecordEverything(source.Name);

        foreach (var (kind, expected) in new[]
        {
            (ActivityKind.Internal, BelemetrySpanKind.Internal), (ActivityKind.Server, BelemetrySpanKind.Server),
            (ActivityKind.Client, BelemetrySpanKind.Client), (ActivityKind.Producer, BelemetrySpanKind.Producer),
            (ActivityKind.Consumer, BelemetrySpanKind.Consumer),
        })
        {
            using var parent = source.StartActivity("parent")!;
            var activity = source.StartActivity("op", kind)!;
            activity.Stop();

            var span = BelemetryActivityCollector.Convert(activity, new BelemetryTracingOptions());
            Assert.AreEqual(expected, span.Kind);
            Assert.AreEqual(parent.SpanId.ToHexString(), span.ParentSpanId);
            Assert.AreEqual(parent.TraceId.ToHexString(), span.TraceId);
        }
    }

    private static ActivityListener RecordEverything(string sourceName)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    [TestMethod]
    public void Convert_ReadsOtelStatusTags_AndLimitsEventsAndLinks()
    {
        using var activity = new Activity("op").SetIdFormat(ActivityIdFormat.W3C);
        activity.SetTag("otel.status_code", "ERROR");
        activity.SetTag("otel.status_description", "from tags");
        activity.SetTag("keep", "me");
        for (var i = 0; i < 5; i++) activity.AddEvent(new ActivityEvent("e" + i));
        activity.Start();
        activity.Stop();

        var span = BelemetryActivityCollector.Convert(activity, new BelemetryTracingOptions { MaxEventsPerSpan = 3 });
        Assert.AreEqual(BelemetryStatusCode.Error, span.Status);
        Assert.AreEqual("from tags", span.StatusMessage);
        Assert.IsFalse(span.Attributes.Contains("otel.status_code"));
        Assert.IsFalse(span.Attributes.Contains("otel.status_description"));
        Assert.AreEqual("me", span.Attributes["keep"]);
        Assert.HasCount(3, span.Events);

        using var ok = new Activity("ok").SetIdFormat(ActivityIdFormat.W3C);
        ok.SetTag("otel.status_code", "OK");
        ok.Start();
        ok.Stop();
        Assert.AreEqual(BelemetryStatusCode.Ok, BelemetryActivityCollector.Convert(ok, new BelemetryTracingOptions()).Status);

        // Activity.SetStatus wins over the tags; a non-error status carries no message.
        using var both = new Activity("both").SetIdFormat(ActivityIdFormat.W3C);
        both.SetTag("otel.status_code", "ERROR");
        both.SetTag("otel.status_description", "tag");
        both.SetStatus(ActivityStatusCode.Ok);
        both.Start();
        both.Stop();
        var bothSpan = BelemetryActivityCollector.Convert(both, new BelemetryTracingOptions());
        Assert.AreEqual(BelemetryStatusCode.Ok, bothSpan.Status);
        Assert.IsNull(bothSpan.StatusMessage);
    }

    [TestMethod]
    public async Task ParentBased_FollowsTheParentDecision()
    {
        var (host, collector, source) = Create(ratio: 0);
        await using var _ = host;
        using var __ = collector;
        using var ___ = source;

        var sampledParent = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded, isRemote: true);
        using (var child = source.StartActivity("child-of-sampled", ActivityKind.Server, sampledParent))
        {
            Assert.IsNotNull(child);
            Assert.IsTrue(child.Recorded, "A sampled parent is followed even at ratio 0.");
        }

        var unsampledParent = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None, isRemote: true);
        using (var child = source.StartActivity("child-of-unsampled", ActivityKind.Server, unsampledParent))
        {
            Assert.IsNotNull(child, "The context still propagates.");
            Assert.IsFalse(child.Recorded);
            Assert.AreEqual(unsampledParent.TraceId, child.TraceId);
        }

        using (var root = source.StartActivity("root"))
        {
            Assert.IsNotNull(root);
            Assert.IsFalse(root.Recorded, "Ratio 0 samples no new trace.");
        }

        await host.FlushAsync();
        Assert.AreEqual("child-of-sampled", host.Memory.Spans.Single().Name);
    }

    [TestMethod]
    public async Task RatioSampling_IsDecidedByTheTraceId()
    {
        var (host, collector, source) = Create(ratio: 0.5);
        await using var _ = host;
        using var __ = collector;
        using var ___ = source;

        var expected = new List<string>();
        for (var i = 0; i < 200; i++)
        {
            Activity.Current = null;
            using var root = source.StartActivity("root");
            Assert.IsNotNull(root);
            Assert.AreEqual(BelemetrySampler.ShouldSample(root.TraceId, 0.5), root.Recorded);
            if (root.Recorded) expected.Add(root.SpanId.ToHexString());
        }

        await host.FlushAsync();
        CollectionAssert.AreEquivalent(expected, host.Memory.Spans.Select(s => s.SpanId).ToList());
        Assert.IsTrue(expected.Count is > 50 and < 150);
    }

    [TestMethod]
    public async Task Suppression_CreatesNoActivity()
    {
        var (host, collector, source) = Create();
        await using var _ = host;
        using var __ = collector;
        using var ___ = source;

        using (BelemetrySuppression.Begin())
        {
            Assert.IsTrue(BelemetrySuppression.IsActive);
            Assert.IsNull(source.StartActivity("export call"));
        }

        Assert.IsFalse(BelemetrySuppression.IsActive);
        using var after = source.StartActivity("after");
        Assert.IsNotNull(after);
    }

    [TestMethod]
    public async Task WithoutTracesConsent_OnlyPropagates()
    {
        var (host, collector, source) = Create(consent: BelemetryFeatures.None);
        await using var _ = host;
        using var __ = collector;
        using var ___ = source;

        using (var activity = source.StartActivity("not consented"))
        {
            Assert.IsNotNull(activity);
            Assert.IsFalse(activity.Recorded);
            Assert.IsFalse(activity.IsAllDataRequested);
        }

        host.Consent.Decide(BelemetryFeatures.Traces);
        using (var activity = source.StartActivity("consented"))
        {
            Assert.IsTrue(activity!.Recorded);
        }

        await host.FlushAsync();
        Assert.AreEqual("consented", host.Memory.Spans.Single().Name);
    }

    [TestMethod]
    public async Task UnconfiguredSources_AreNotListenedTo()
    {
        var (host, collector, source) = Create(configure: o => o.Traces.Sources.Add("Tests.Wildcard.*"));
        await using var _ = host;
        using var __ = collector;
        using var ___ = source;

        using var other = new ActivitySource(NewSourceName());
        Assert.IsNull(other.StartActivity("ignored"));

        using var wildcard = new ActivitySource("Tests.Wildcard." + Guid.NewGuid().ToString("N"));
        using var matched = wildcard.StartActivity("matched");
        Assert.IsNotNull(matched);
    }

    [TestMethod]
    public async Task AspNetCoreRequestSpans_GetTheHttpConventions_FromTheHostingMetric()
    {
        await using var host = TestHost.Create(o =>
        {
            o.Host = BelemetryHostKind.Server;
            o.Server.CollectDotnetTelemetry = true;
        });
        host.StartLogging();

        // Stand-ins for what ASP.NET Core's hosting layer creates: same source, activity, meter and instrument names.
        using var source = new ActivitySource("Microsoft.AspNetCore");
        using var meter = new Meter(BelemetryAspNetCoreRequests.MeterName);
        var duration = meter.CreateHistogram<double>(BelemetryAspNetCoreRequests.DurationInstrument, "s");

        void Request(string method, string? route, int status, string? appName = null)
        {
            using var activity = source.StartActivity(BelemetryAspNetCoreRequests.ActivityName, ActivityKind.Server)!;
            Assert.IsNotNull(activity);
            if (appName is not null) activity.DisplayName = appName;
            activity.SetTag(BelemetrySemanticConventions.UrlPath, "/already/set");

            var tags = new TagList
            {
                { BelemetrySemanticConventions.UrlScheme, "https" },
                { BelemetrySemanticConventions.HttpRequestMethod, method },
                { BelemetrySemanticConventions.HttpResponseStatusCode, status },
                { BelemetrySemanticConventions.UrlPath, "/from/the/metric" },
            };
            if (route is not null) tags.Add(BelemetrySemanticConventions.HttpRoute, route);
            duration.Record(0.01, tags);
        }

        Request("GET", "/orders/{id}", 200);
        Request("POST", null, 503);
        Request("GET", "/health", 404, appName: "health probe");

        await host.FlushAsync();
        var spans = host.Memory.Spans.ToList();
        Assert.HasCount(3, spans);

        Assert.AreEqual("GET /orders/{id}", spans[0].Name);
        Assert.AreEqual(BelemetrySpanKind.Server, spans[0].Kind);
        Assert.AreEqual("GET", spans[0].Attributes.GetString(BelemetrySemanticConventions.HttpRequestMethod));
        Assert.AreEqual("https", spans[0].Attributes.GetString(BelemetrySemanticConventions.UrlScheme));
        Assert.AreEqual("/orders/{id}", spans[0].Attributes.GetString(BelemetrySemanticConventions.HttpRoute));
        Assert.AreEqual(200L, spans[0].Attributes.GetInt64(BelemetrySemanticConventions.HttpResponseStatusCode));
        Assert.AreEqual("/already/set", spans[0].Attributes.GetString(BelemetrySemanticConventions.UrlPath), "A tag already on the activity wins.");
        Assert.AreEqual(BelemetryStatusCode.Unset, spans[0].Status);

        Assert.AreEqual("POST", spans[1].Name, "Without a route the name is the method alone.");
        Assert.AreEqual(BelemetryStatusCode.Error, spans[1].Status, "A 5xx fails a server span.");

        Assert.AreEqual("health probe", spans[2].Name, "A name the app gave the activity is kept.");
        Assert.AreEqual(BelemetryStatusCode.Unset, spans[2].Status, "A 4xx is the client's error, not the server span's.");
    }

    [TestMethod]
    public void AspNetCoreRequests_OnlyEnrichTheLiveRequestActivity()
    {
        using var source = new ActivitySource(NewSourceName());
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s == source,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);

        KeyValuePair<string, object?>[] tags = [new(BelemetrySemanticConventions.HttpRoute, "/x")];

        using var other = source.StartActivity("SomethingElse")!;
        BelemetryAspNetCoreRequests.Enrich(other, tags);
        Assert.IsNull(other.GetTagItem(BelemetrySemanticConventions.HttpRoute));

        var stopped = source.StartActivity(BelemetryAspNetCoreRequests.ActivityName)!;
        stopped.Stop();
        BelemetryAspNetCoreRequests.Enrich(stopped, tags);
        Assert.IsNull(stopped.GetTagItem(BelemetrySemanticConventions.HttpRoute));

        BelemetryAspNetCoreRequests.Enrich(null, tags);
    }

    [TestMethod]
    public void AspNetCoreRequests_SpanName()
    {
        Assert.AreEqual("GET /orders/{id}", BelemetryAspNetCoreRequests.GetSpanName("GET", "/orders/{id}"));
        Assert.AreEqual("GET", BelemetryAspNetCoreRequests.GetSpanName("GET", null));
        Assert.AreEqual("HTTP", BelemetryAspNetCoreRequests.GetSpanName("_OTHER", ""));
        Assert.IsNull(BelemetryAspNetCoreRequests.GetSpanName(null, "/x"));
    }

    [TestMethod]
    public void Matches_ExactPrefixAndStar()
    {
        Assert.IsTrue(BelemetrySettings.Matches("System.Net.Http", ["system.net.http"]));
        Assert.IsTrue(BelemetrySettings.Matches("Microsoft.AspNetCore.Components.Server", ["Microsoft.AspNetCore.Components*"]));
        Assert.IsTrue(BelemetrySettings.Matches("anything", ["*"]));
        Assert.IsFalse(BelemetrySettings.Matches("System.Net.Http.Other", ["System.Net.Http"]));
        Assert.IsFalse(BelemetrySettings.Matches("x", []));
    }
}
