namespace Bit.Belemetry.Tests.AzureMonitor;

/// <summary>Golden checks of the Application Insights mapping documented in CLAUDE.md.</summary>
[TestClass]
public class AzureMonitorEnvelopeSerializerTests
{
    private const string IKey = "11111111-2222-3333-4444-555555555555";
    private const string Time = "2023-11-14T22:13:20.1234567Z";

    private static List<JsonElement> Serialize(BelemetryBatch batch, bool maskIp = true) => Serialize(batch, maskIp, out _);

    private static List<JsonElement> Serialize(BelemetryBatch batch, bool maskIp, out int count)
    {
        var bytes = AzureMonitorEnvelopeSerializer.Serialize(batch, new AzureMonitorEnvelopeSerializer.Context(IKey, maskIp), out count);
        using var doc = JsonDocument.Parse(bytes);
        return doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
    }

    private static JsonElement Single(BelemetrySpan span) => Serialize(new BelemetryBatch(Records.Resource(), [span])).Single();

    private static JsonElement Single(BelemetryLogRecord log) => Serialize(new BelemetryBatch(Records.Resource(), logs: [log])).Single();

    private static JsonElement BaseData(JsonElement envelope) => envelope.GetProperty("data").GetProperty("baseData");

    private static JsonElement Tags(JsonElement envelope) => envelope.GetProperty("tags");

    private static string? Prop(JsonElement envelope, string name) => BaseData(envelope).GetProperty("properties").TryGetProperty(name, out var value) ? value.GetString() : null;

    private static void AssertEnvelope(JsonElement envelope, string type)
    {
        Assert.AreEqual(1, envelope.GetProperty("ver").GetInt32());
        Assert.AreEqual("Microsoft.ApplicationInsights." + type, envelope.GetProperty("name").GetString());
        Assert.AreEqual(Time, envelope.GetProperty("time").GetString());
        Assert.AreEqual(100, envelope.GetProperty("sampleRate").GetInt32());
        Assert.AreEqual(IKey, envelope.GetProperty("iKey").GetString());
        Assert.AreEqual(type + "Data", envelope.GetProperty("data").GetProperty("baseType").GetString());
        Assert.AreEqual(2, BaseData(envelope).GetProperty("ver").GetInt32());
    }

    [TestMethod]
    public void Request_FromServerSpan()
    {
        var span = Records.Span(BelemetrySpanKind.Server, "GET");
        span.Attributes.Set("http.request.method", "GET").Set("http.route", "/orders/{id}").Set("http.response.status_code", 500)
            .Set("url.full", "https://shop.test/orders/1").Set("session.id", "s-1").Set("enduser.pseudo.id", "p-1").Set("user.id", "u-1").Set("custom", 3);

        var e = Single(span);
        AssertEnvelope(e, "Request");
        var data = BaseData(e);
        Assert.AreEqual(Records.SpanId, data.GetProperty("id").GetString());
        Assert.AreEqual("GET /orders/{id}", data.GetProperty("name").GetString());
        Assert.AreEqual("00:00:01.5000000", data.GetProperty("duration").GetString());
        Assert.AreEqual("500", data.GetProperty("responseCode").GetString());
        Assert.IsFalse(data.GetProperty("success").GetBoolean());
        Assert.AreEqual("https://shop.test/orders/1", data.GetProperty("url").GetString());

        var tags = Tags(e);
        Assert.AreEqual("[shop]/checkout", tags.GetProperty("ai.cloud.role").GetString());
        Assert.AreEqual("instance-42", tags.GetProperty("ai.cloud.roleInstance").GetString());
        Assert.AreEqual(Records.TraceId, tags.GetProperty("ai.operation.id").GetString());
        Assert.AreEqual(Records.ParentSpanId, tags.GetProperty("ai.operation.parentId").GetString());
        Assert.AreEqual("GET /orders/{id}", tags.GetProperty("ai.operation.name").GetString());
        Assert.AreEqual("s-1", tags.GetProperty("ai.session.id").GetString());
        Assert.AreEqual("p-1", tags.GetProperty("ai.user.id").GetString());
        Assert.AreEqual("u-1", tags.GetProperty("ai.user.authUserId").GetString());
        Assert.AreEqual("PC", tags.GetProperty("ai.device.type").GetString());
        Assert.AreEqual("0.0.0.0", tags.GetProperty("ai.location.ip").GetString());
        Assert.AreEqual("bitblm:10.6.2", tags.GetProperty("ai.internal.sdkVersion").GetString());
        Assert.AreEqual("2.0.0", tags.GetProperty("ai.application.ver").GetString());

        Assert.AreEqual("3", Prop(e, "custom"));
        Assert.AreEqual("GET", Prop(e, "http.request.method"));
        Assert.IsNull(Prop(e, "session.id"), "Session and user ids travel as tags, not properties.");
        Assert.IsNull(Prop(e, "enduser.pseudo.id"));
        Assert.IsNull(Prop(e, "user.id"));
    }

    [TestMethod]
    public void Request_FromConsumerSpan_BuildsTheUrlFromParts()
    {
        var span = Records.Span(BelemetrySpanKind.Consumer, "process");
        span.Attributes.Set("url.scheme", "https").Set("server.address", "api.test").Set("server.port", 8443).Set("url.path", "/q").Set("url.query", "?a=1")
            .Set("http.response.status_code", 404);

        var e = Single(span);
        AssertEnvelope(e, "Request");
        Assert.AreEqual("https://api.test:8443/q?a=1", BaseData(e).GetProperty("url").GetString());
        Assert.AreEqual("process", BaseData(e).GetProperty("name").GetString());
        Assert.IsTrue(BaseData(e).GetProperty("success").GetBoolean(), "A 4xx is a successful request (the client's fault).");
    }

    [TestMethod]
    public void RemoteDependency_Types()
    {
        var ajax = Records.Span(BelemetrySpanKind.Client, "GET");
        ajax.Scope = new BelemetryScope("bit.belemetry.fetch");
        ajax.Attributes.Set("http.request.method", "GET").Set("url.full", "https://api.test/items?x=1").Set("http.response.status_code", 404);

        var http = Records.Span(BelemetrySpanKind.Client, "POST");
        http.Attributes.Set("http.request.method", "POST").Set("url.full", "https://pay.test:8443/charge").Set("server.address", "pay.test").Set("server.port", 8443)
            .Set("http.response.status_code", 201);

        var inproc = Records.Span(BelemetrySpanKind.Internal, "compute");
        inproc.Status = BelemetryStatusCode.Error;
        inproc.StatusMessage = "overflow";

        var db = Records.Span(BelemetrySpanKind.Client, "SELECT orders");
        db.Attributes.Set("db.system.name", "postgresql").Set("db.query.text", "SELECT * FROM orders").Set("server.address", "db.test").Set("server.port", 5432);

        var producer = Records.Span(BelemetrySpanKind.Producer, "publish");

        var envelopes = Serialize(new BelemetryBatch(Records.Resource(), [ajax, http, inproc, db, producer]));
        Assert.HasCount(5, envelopes);
        foreach (var e in envelopes) AssertEnvelope(e, "RemoteDependency");

        var a = BaseData(envelopes[0]);
        Assert.AreEqual("Ajax", a.GetProperty("type").GetString());
        Assert.AreEqual("404", a.GetProperty("resultCode").GetString());
        Assert.IsFalse(a.GetProperty("success").GetBoolean());
        Assert.AreEqual("https://api.test/items?x=1", a.GetProperty("data").GetString());
        Assert.AreEqual("api.test", a.GetProperty("target").GetString());
        Assert.AreEqual("GET", a.GetProperty("name").GetString());
        Assert.AreEqual("00:00:01.5000000", a.GetProperty("duration").GetString());
        Assert.IsFalse(Tags(envelopes[0]).TryGetProperty("ai.operation.name", out _), "Only requests name the operation.");

        var h = BaseData(envelopes[1]);
        Assert.AreEqual("HTTP", h.GetProperty("type").GetString());
        Assert.IsTrue(h.GetProperty("success").GetBoolean());
        Assert.AreEqual("pay.test:8443", h.GetProperty("target").GetString());

        var i = BaseData(envelopes[2]);
        Assert.AreEqual("InProc", i.GetProperty("type").GetString());
        Assert.IsFalse(i.GetProperty("success").GetBoolean());
        Assert.IsFalse(i.TryGetProperty("resultCode", out _));
        Assert.IsFalse(i.TryGetProperty("data", out _));
        Assert.AreEqual("overflow", Prop(envelopes[2], "message"));

        var d = BaseData(envelopes[3]);
        Assert.AreEqual("postgresql", d.GetProperty("type").GetString());
        Assert.AreEqual("SELECT * FROM orders", d.GetProperty("data").GetString());
        Assert.AreEqual("db.test:5432", d.GetProperty("target").GetString());

        Assert.AreEqual("Producer", BaseData(envelopes[4]).GetProperty("type").GetString());
    }

    [TestMethod]
    public void Exception_FromLogWithExceptionAttributes()
    {
        var log = Records.Log("ignored body", severity: BelemetrySeverity.Fatal);
        log.TraceId = Records.TraceId;
        log.SpanId = Records.SpanId;
        log.Attributes.Set("exception.type", "System.InvalidOperationException").Set("exception.message", "bad").Set("exception.stacktrace", "at X()").Set("order", 1);

        var e = Single(log);
        AssertEnvelope(e, "Exception");
        var ex = BaseData(e).GetProperty("exceptions")[0];
        Assert.AreEqual(1, ex.GetProperty("id").GetInt32());
        Assert.AreEqual(0, ex.GetProperty("outerId").GetInt32());
        Assert.AreEqual("System.InvalidOperationException", ex.GetProperty("typeName").GetString());
        Assert.AreEqual("bad", ex.GetProperty("message").GetString());
        Assert.IsTrue(ex.GetProperty("hasFullStack").GetBoolean());
        Assert.AreEqual("at X()", ex.GetProperty("stack").GetString());
        Assert.AreEqual(4, BaseData(e).GetProperty("severityLevel").GetInt32());
        Assert.AreEqual(Records.TraceId, Tags(e).GetProperty("ai.operation.id").GetString());
        Assert.AreEqual(Records.SpanId, Tags(e).GetProperty("ai.operation.parentId").GetString(), "A log's parent is the span it was written in.");
        Assert.IsNull(Prop(e, "exception.type"));
        Assert.AreEqual("1", Prop(e, "order"));

        var noStack = Records.Log("message from body", severity: BelemetrySeverity.Unspecified);
        noStack.Attributes.Set("exception.type", "Error");
        var e2 = BaseData(Single(noStack));
        Assert.AreEqual("message from body", e2.GetProperty("exceptions")[0].GetProperty("message").GetString());
        Assert.IsFalse(e2.GetProperty("exceptions")[0].GetProperty("hasFullStack").GetBoolean());
        Assert.AreEqual(3, e2.GetProperty("severityLevel").GetInt32());
    }

    [TestMethod]
    public void Exception_FromSpanExceptionEvent()
    {
        var span = Records.Span(BelemetrySpanKind.Server, "GET");
        span.Attributes.Set("http.request.method", "GET").Set("http.route", "/orders/{id}").Set("session.id", "s-1").Set("user.id", "u-1");
        span.Events.Add(new BelemetrySpanEvent { Name = "retry", TimeUnixNano = Records.Start, Attributes = new BelemetryAttributes().Set("attempt", 2) });
        span.Events.Add(new BelemetrySpanEvent
        {
            Name = "exception",
            TimeUnixNano = Records.Start + 1_000_000_000,
            Attributes = new BelemetryAttributes().Set("exception.type", "System.TimeoutException").Set("exception.message", "slow").Set("exception.stacktrace", "at Y()")
                .Set("exception.escaped", true).Set("order", 7),
        });
        span.Events.Add(new BelemetrySpanEvent { Name = "exception", TimeUnixNano = Records.Start, Attributes = new BelemetryAttributes().Set("other", 1) });

        var envelopes = Serialize(new BelemetryBatch(Records.Resource(), [span]), true, out var count);

        Assert.HasCount(3, envelopes, "The exception and the other event are items of their own; an exception event without exception.* is not.");
        Assert.AreEqual(3, count, "The count is the envelopes', the items a 206 response indexes.");
        AssertEnvelope(envelopes[0], "Request");
        Assert.AreEqual("Microsoft.ApplicationInsights.Message", envelopes[1].GetProperty("name").GetString());

        var e = envelopes[2];
        Assert.AreEqual("Microsoft.ApplicationInsights.Exception", e.GetProperty("name").GetString());
        Assert.AreEqual("2023-11-14T22:13:21.1234567Z", e.GetProperty("time").GetString(), "At the event's time, not the span's.");
        Assert.AreEqual(IKey, e.GetProperty("iKey").GetString());
        Assert.AreEqual("ExceptionData", e.GetProperty("data").GetProperty("baseType").GetString());
        var data = BaseData(e);
        var ex = data.GetProperty("exceptions")[0];
        Assert.AreEqual(1, ex.GetProperty("id").GetInt32());
        Assert.AreEqual(0, ex.GetProperty("outerId").GetInt32());
        Assert.AreEqual("System.TimeoutException", ex.GetProperty("typeName").GetString());
        Assert.AreEqual("slow", ex.GetProperty("message").GetString());
        Assert.IsTrue(ex.GetProperty("hasFullStack").GetBoolean());
        Assert.AreEqual("at Y()", ex.GetProperty("stack").GetString());
        Assert.AreEqual(3, data.GetProperty("severityLevel").GetInt32());

        var tags = Tags(e);
        Assert.AreEqual("[shop]/checkout", tags.GetProperty("ai.cloud.role").GetString());
        Assert.AreEqual(Records.TraceId, tags.GetProperty("ai.operation.id").GetString());
        Assert.AreEqual(Records.SpanId, tags.GetProperty("ai.operation.parentId").GetString(), "Its parent is the span it was recorded on.");
        Assert.AreEqual("GET /orders/{id}", tags.GetProperty("ai.operation.name").GetString(), "A request's exception carries the request's operation name.");
        Assert.AreEqual("s-1", tags.GetProperty("ai.session.id").GetString(), "The ids come from the span's attributes.");
        Assert.AreEqual("u-1", tags.GetProperty("ai.user.authUserId").GetString());

        Assert.AreEqual("7", Prop(e, "order"));
        Assert.IsNull(Prop(e, "exception.escaped"));
        Assert.IsNull(Prop(e, "http.route"), "The span's attributes stay on the span's envelope.");
        Assert.IsNull(Prop(e, "session.id"));
    }

    [TestMethod]
    public void Exception_FromActivityAddException()
    {
        using var activity = new Activity("charge").SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        try
        {
            throw new InvalidOperationException("declined");
        }
        catch (InvalidOperationException thrown)
        {
            activity.AddException(thrown);
        }
        activity.Stop();

        var envelopes = Serialize(new BelemetryBatch(Records.Resource(), [BelemetryActivityCollector.Convert(activity, new BelemetryTracingOptions())]));

        Assert.HasCount(2, envelopes);
        Assert.AreEqual("Microsoft.ApplicationInsights.RemoteDependency", envelopes[0].GetProperty("name").GetString());
        var e = envelopes[1];
        Assert.AreEqual("Microsoft.ApplicationInsights.Exception", e.GetProperty("name").GetString());
        var ex = BaseData(e).GetProperty("exceptions")[0];
        Assert.AreEqual("System.InvalidOperationException", ex.GetProperty("typeName").GetString());
        Assert.AreEqual("declined", ex.GetProperty("message").GetString());
        Assert.IsTrue(ex.GetProperty("hasFullStack").GetBoolean());
        Assert.IsTrue(ex.GetProperty("stack").GetString()!.Contains(nameof(Exception_FromActivityAddException), StringComparison.Ordinal));
        Assert.AreEqual(activity.TraceId.ToHexString(), Tags(e).GetProperty("ai.operation.id").GetString());
        Assert.AreEqual(activity.SpanId.ToHexString(), Tags(e).GetProperty("ai.operation.parentId").GetString());
        Assert.IsFalse(Tags(e).TryGetProperty("ai.operation.name", out _), "Only a request's exceptions name the operation.");
    }

    [TestMethod]
    public void Message_FromAnyOtherSpanEvent()
    {
        var span = Records.Span(BelemetrySpanKind.Server, "GET");
        span.Attributes.Set("http.request.method", "GET").Set("http.route", "/orders/{id}").Set("session.id", "s-1");
        span.Events.Add(new BelemetrySpanEvent
        {
            Name = "retry",
            TimeUnixNano = Records.Start + 1_000_000_000,
            Attributes = new BelemetryAttributes().Set("attempt", 2).Set("session.id", "other"),
        });
        var dependency = Records.Span();
        dependency.Events.Add(new BelemetrySpanEvent { Name = "domInteractive", TimeUnixNano = Records.Start });
        dependency.Events.Add(new BelemetrySpanEvent { Name = string.Empty, TimeUnixNano = Records.Start });

        var envelopes = Serialize(new BelemetryBatch(Records.Resource(), [span, dependency]), true, out var count);

        Assert.HasCount(4, envelopes, "An event with no name is not an item.");
        Assert.AreEqual(4, count);
        var e = envelopes[1];
        Assert.AreEqual("Microsoft.ApplicationInsights.Message", e.GetProperty("name").GetString());
        Assert.AreEqual("2023-11-14T22:13:21.1234567Z", e.GetProperty("time").GetString(), "At the event's time, not the span's.");
        Assert.AreEqual("MessageData", e.GetProperty("data").GetProperty("baseType").GetString());
        var data = BaseData(e);
        Assert.AreEqual("retry", data.GetProperty("message").GetString(), "The event's name is the message, as the Azure Monitor exporter writes it.");
        Assert.IsFalse(data.TryGetProperty("severityLevel", out _), "An event is not a log level.");
        Assert.AreEqual("2", Prop(e, "attempt"));
        Assert.IsNull(Prop(e, "http.route"), "The span's attributes stay on the span's envelope.");
        Assert.AreEqual(Records.SpanId, Tags(e).GetProperty("ai.operation.parentId").GetString());
        Assert.AreEqual("GET /orders/{id}", Tags(e).GetProperty("ai.operation.name").GetString());
        Assert.AreEqual("s-1", Tags(e).GetProperty("ai.session.id").GetString(), "The ids come from the span's attributes.");

        Assert.AreEqual("Microsoft.ApplicationInsights.RemoteDependency", envelopes[2].GetProperty("name").GetString());
        Assert.AreEqual("domInteractive", BaseData(envelopes[3]).GetProperty("message").GetString());
        Assert.IsFalse(Tags(envelopes[3]).TryGetProperty("ai.operation.name", out _));
    }

    [TestMethod]
    public void SpanException_LoggedUnderTheSpan_IsTheLogsItemAlone()
    {
        static BelemetrySpan Span(BelemetrySeverity severity, bool isEvent = false, bool limited = false, bool dropped = false)
        {
            var logged = limited ? null : new BelemetryLoggedException("System.TimeoutException", "slow", "at Y()", severity, isEvent);
            if (dropped) logged!.Drop();

            var span = Records.Span();
            span.Events.Add(new BelemetrySpanEvent
            {
                Name = "exception",
                TimeUnixNano = Records.Start,
                Attributes = new BelemetryAttributes().Set("exception.type", "System.TimeoutException").Set("exception.message", "slow"),
                LoggedAs = logged,
                Limited = limited,
            });
            return span;
        }

        static int Items(BelemetrySpan span, AzureMonitorEnvelopeSerializer.Context context)
        {
            var batch = new BelemetryBatch(Records.Resource(), [span]);
            AzureMonitorEnvelopeSerializer.Serialize(batch, context, out var count);
            Assert.AreEqual(count, AzureMonitorEnvelopeSerializer.Count(batch, context), "Count says what Serialize writes.");
            return count;
        }

        var all = new AzureMonitorEnvelopeSerializer.Context(IKey, true);
        Assert.AreEqual(1, Items(Span(BelemetrySeverity.Error), all), "The log record's Exception stands for it.");
        Assert.AreEqual(1, Items(Span(BelemetrySeverity.Error, limited: true), all), "The error limits dropped it as an item of its own.");
        Assert.AreEqual(2, Items(Span(BelemetrySeverity.Error, dropped: true), all), "A log record dropped before the export stands for nothing.");

        Assert.AreEqual(2, Items(Span(BelemetrySeverity.Error), all with { Logs = false }), "An exporter that sends no logs sends it from the span.");
        var errorsOnly = all with { MinimumSeverity = BelemetrySeverity.Error };
        Assert.AreEqual(2, Items(Span(BelemetrySeverity.Warn), errorsOnly), "So does one whose minimum level keeps the log record out.");
        Assert.AreEqual(1, Items(Span(BelemetrySeverity.Error), errorsOnly));
        Assert.AreEqual(1, Items(Span(BelemetrySeverity.Info, isEvent: true), all with { MinimumSeverity = BelemetrySeverity.Fatal }), "TrackException passes any level.");
    }

    [TestMethod]
    public void Count_IsTheEnvelopesSerializeWrites()
    {
        var span = Records.Span(BelemetrySpanKind.Server);
        span.Events.Add(new BelemetrySpanEvent { Name = "retry" });
        span.Events.Add(new BelemetrySpanEvent { Name = "exception", Attributes = new BelemetryAttributes().Set("exception.message", "x") });
        span.Events.Add(new BelemetrySpanEvent { Name = "exception" });
        var counter = new BelemetryMetric { Name = "orders", Type = BelemetryMetricType.Sum, Points = [new() { DeltaLongValue = 2, IsInteger = true }, new()] };
        var histogram = new BelemetryMetric { Name = "latency", Type = BelemetryMetricType.Histogram, Points = [new() { DeltaCount = 1, DeltaSum = 3 }, new()] };
        var gauge = new BelemetryMetric { Name = "queue", Type = BelemetryMetricType.Gauge, Points = [new() { Value = 1 }] };
        var batch = new BelemetryBatch(Records.Resource(), [span, Records.Span()], [Records.Log(), Records.Log()], [counter, histogram, gauge]);
        var context = new AzureMonitorEnvelopeSerializer.Context(IKey, true);

        AzureMonitorEnvelopeSerializer.Serialize(batch, context, out var count);

        Assert.AreEqual(9, count, "2 spans and 2 of their events, 2 logs, and the 3 metric points with something new.");
        Assert.AreEqual(count, AzureMonitorEnvelopeSerializer.Count(batch, context));
    }

    [TestMethod]
    public void Request_WithAnEmptyRoute_IsNamedAfterTheSpan()
    {
        var span = Records.Span(BelemetrySpanKind.Server, "GET");
        span.Attributes.Set("http.request.method", "GET").Set("http.route", string.Empty);

        var e = Single(span);

        Assert.AreEqual("GET", BaseData(e).GetProperty("name").GetString(), "As in TS: an empty route is not known.");
        Assert.AreEqual("GET", Tags(e).GetProperty("ai.operation.name").GetString());
    }

    [TestMethod]
    public void PageView()
    {
        var log = Records.Log(null, "browser.page_view");
        log.SpanId = Records.SpanId;
        log.Attributes.Set("url.full", "https://app.test/home").Set("url.path", "/home").Set("page.title", "Home").Set("session.id", "s-9");

        var e = Single(log);
        AssertEnvelope(e, "PageView");
        var data = BaseData(e);
        Assert.AreEqual("Home", data.GetProperty("name").GetString());
        Assert.AreEqual("https://app.test/home", data.GetProperty("url").GetString());
        Assert.AreEqual(Records.SpanId, data.GetProperty("id").GetString());
        Assert.AreEqual("00:00:00", data.GetProperty("duration").GetString());
        Assert.AreEqual("Home", Tags(e).GetProperty("ai.operation.name").GetString());
        Assert.AreEqual("s-9", Tags(e).GetProperty("ai.session.id").GetString());
        Assert.IsNull(Prop(e, "url.full"));
        Assert.IsNull(Prop(e, "page.title"));
        Assert.AreEqual("/home", Prop(e, "url.path"));

        var untitled = Records.Log(null, "browser.page_view");
        untitled.Attributes.Set("url.path", "/about");
        var u = BaseData(Single(untitled));
        Assert.AreEqual("/about", u.GetProperty("name").GetString());
        Assert.AreEqual(16, u.GetProperty("id").GetString()!.Length, "A page view without a span id gets a random one.");
    }

    [TestMethod]
    public void WebVital_IsAMetric()
    {
        var log = Records.Log(null, "browser.web_vital");
        log.Attributes.Set("web_vital.name", "LCP").Set("web_vital.value", 2512.5).Set("web_vital.rating", "needs-improvement");

        var e = Single(log);
        AssertEnvelope(e, "Metric");
        var metric = BaseData(e).GetProperty("metrics")[0];
        Assert.AreEqual("LCP", metric.GetProperty("name").GetString());
        Assert.AreEqual(0, metric.GetProperty("kind").GetInt32());
        Assert.AreEqual(2512.5, metric.GetProperty("value").GetDouble());
        Assert.AreEqual(1, metric.GetProperty("count").GetInt32());
        Assert.AreEqual("needs-improvement", Prop(e, "web_vital.rating"));
        Assert.IsNull(Prop(e, "web_vital.name"));
        Assert.IsNull(Prop(e, "web_vital.value"));
    }

    [TestMethod]
    public void OtherEvents_AreEvents()
    {
        var log = Records.Log("clicked buy", "browser.user_interaction");
        log.Attributes.Set("target.id", "buy");

        var e = Single(log);
        AssertEnvelope(e, "Event");
        Assert.AreEqual("browser.user_interaction", BaseData(e).GetProperty("name").GetString());
        Assert.AreEqual("buy", Prop(e, "target.id"));
        Assert.AreEqual("clicked buy", Prop(e, "message"));
    }

    [TestMethod]
    public void PageVisitTime_IsTheJsSdkEvent()
    {
        var log = Records.Log("Home page", "browser.page_visit_time");
        log.Attributes.Set("url.full", "https://shop.test/").Set("url.path", "/").Set("page.title", "Home page").Set("page.visit.duration", 1234.5).Set("extra", "y");

        var e = Single(log);
        AssertEnvelope(e, "Event");
        var data = BaseData(e);
        Assert.AreEqual("PageVisitTime", data.GetProperty("name").GetString());
        Assert.AreEqual(1234.5, data.GetProperty("measurements").GetProperty("PageVisitTime").GetDouble());
        Assert.AreEqual("Home page", Prop(e, "PageName"));
        Assert.AreEqual("https://shop.test/", Prop(e, "PageUrl"));
        Assert.AreEqual("y", Prop(e, "extra"));
        Assert.IsNull(Prop(e, "page.visit.duration"), "The consumed attributes are not repeated as properties.");
        Assert.IsNull(Prop(e, "url.path"));

        var untitled = Records.Log(null, "browser.page_visit_time");
        untitled.Attributes.Set("url.path", "/p");
        Assert.AreEqual("/p", Prop(Single(untitled), "PageName"));
        Assert.AreEqual(0, BaseData(Single(untitled)).GetProperty("measurements").GetProperty("PageVisitTime").GetDouble());
    }

    [TestMethod]
    public void OtherLogs_AreMessages()
    {
        var levels = new[]
        {
            (BelemetrySeverity.Trace, 0), (BelemetrySeverity.Debug, 0), (BelemetrySeverity.Info, 1), (BelemetrySeverity.Warn, 2),
            (BelemetrySeverity.Error, 3), (BelemetrySeverity.Fatal, 4),
        };

        foreach (var (severity, level) in levels)
        {
            var e = Single(Records.Log("hello", severity: severity));
            AssertEnvelope(e, "Message");
            Assert.AreEqual("hello", BaseData(e).GetProperty("message").GetString());
            Assert.AreEqual(level, BaseData(e).GetProperty("severityLevel").GetInt32());
            Assert.AreEqual("MyApp.Orders", Prop(e, "CategoryName"));
        }

        Assert.AreEqual(1, AzureMonitorEnvelopeSerializer.ToSeverityLevel((BelemetrySeverity)12));
        Assert.AreEqual(2, AzureMonitorEnvelopeSerializer.ToSeverityLevel((BelemetrySeverity)16));
        Assert.AreEqual(3, AzureMonitorEnvelopeSerializer.ToSeverityLevel((BelemetrySeverity)20));
    }

    [TestMethod]
    public void MetricPoints_UseDeltas()
    {
        var envelopes = Serialize(new BelemetryBatch(Records.Resource(), metrics: Otlp.OtlpProtobufConformanceTests.MetricsSample()), true, out var count);

        Assert.HasCount(4, envelopes, "The idle sum point has nothing to report.");
        Assert.AreEqual(4, count);
        var byName = envelopes.ToDictionary(e => BaseData(e).GetProperty("metrics")[0].GetProperty("name").GetString()!);

        var gauge = byName["queue.length"];
        Assert.AreEqual("Microsoft.ApplicationInsights.Metric", gauge.GetProperty("name").GetString());
        Assert.AreEqual(7.5, BaseData(gauge).GetProperty("metrics")[0].GetProperty("value").GetDouble());
        Assert.AreEqual("MyApp.Meter", BaseData(gauge).GetProperty("metrics")[0].GetProperty("ns").GetString());
        Assert.AreEqual("eu", Prop(gauge, "region"));
        Assert.AreEqual("1970-01-01T00:00:00.0000030Z", gauge.GetProperty("time").GetString());

        var sum = BaseData(byName["requests"]).GetProperty("metrics")[0];
        Assert.AreEqual(4.0, sum.GetProperty("value").GetDouble());
        Assert.AreEqual(1, sum.GetProperty("count").GetInt32());
        Assert.AreEqual(0, sum.GetProperty("kind").GetInt32());

        Assert.AreEqual(-1.25, BaseData(byName["balance"]).GetProperty("metrics")[0].GetProperty("value").GetDouble());

        var histogram = BaseData(byName["latency"]).GetProperty("metrics")[0];
        Assert.AreEqual(1, histogram.GetProperty("kind").GetInt32());
        Assert.AreEqual(30.0, histogram.GetProperty("value").GetDouble());
        Assert.AreEqual(2, histogram.GetProperty("count").GetInt32());
        Assert.AreEqual(10.0, histogram.GetProperty("min").GetDouble());
        Assert.AreEqual(20.0, histogram.GetProperty("max").GetDouble());
    }

    [TestMethod]
    public void ResourceTags_Variants()
    {
        var browser = new BelemetryResource(new BelemetryAttributes()
            .Set("service.name", "web").Set("telemetry.sdk.language", "webjs").Set("telemetry.sdk.version", "1.0.0").Set("user_agent.original", "Mozilla/5.0").Set("browser.language", "de-DE"));
        var tags = Tags(Serialize(new BelemetryBatch(browser, logs: [Records.Log()]), maskIp: false).Single());
        Assert.AreEqual("web", tags.GetProperty("ai.cloud.role").GetString());
        Assert.AreEqual("Browser", tags.GetProperty("ai.device.type").GetString());
        Assert.AreEqual("bitblmjs:1.0.0", tags.GetProperty("ai.internal.sdkVersion").GetString());
        Assert.AreEqual("Mozilla/5.0", tags.GetProperty("ai.user.agent").GetString());
        Assert.AreEqual("de-DE", tags.GetProperty("ai.device.locale").GetString());
        Assert.IsFalse(Tags(Serialize(new BelemetryBatch(Records.Resource(), logs: [Records.Log()])).Single()).TryGetProperty("ai.device.locale", out _), "Only a browser resource knows a locale.");
        Assert.IsFalse(tags.TryGetProperty("ai.location.ip", out _));
        Assert.AreEqual(string.Empty, tags.GetProperty("ai.cloud.roleInstance").GetString());
        Assert.IsFalse(tags.TryGetProperty("ai.application.ver", out _));

        var phone = new BelemetryResource(new BelemetryAttributes().Set("service.name", "app").Set("os.type", "android").Set("host.name", "pixel"));
        var phoneTags = Tags(Serialize(new BelemetryBatch(phone, logs: [Records.Log()])).Single());
        Assert.AreEqual("Phone", phoneTags.GetProperty("ai.device.type").GetString());
        Assert.AreEqual("pixel", phoneTags.GetProperty("ai.cloud.roleInstance").GetString());
    }

    [TestMethod]
    public void Properties_AreStringifiedAndTruncated()
    {
        var log = Records.Log();
        log.Attributes.Set(new string('k', 200), "v").Set("long", new string('v', 9000)).Set("array", new[] { 1L, 2L }).Set("flag", true).Set("d", 0.25);

        var e = Single(log);
        Assert.AreEqual("v", Prop(e, new string('k', 150)));
        Assert.AreEqual(8192, Prop(e, "long")!.Length);
        Assert.AreEqual("[1,2]", Prop(e, "array"));
        Assert.AreEqual("true", Prop(e, "flag"));
        Assert.AreEqual("0.25", Prop(e, "d"));
    }

    [TestMethod]
    public void DurationFormat()
    {
        Assert.AreEqual("00:00:00.0010000", AzureMonitorEnvelopeSerializer.FormatDuration(TimeSpan.FromMilliseconds(1)));
        Assert.AreEqual("1.02:03:04.5000000", AzureMonitorEnvelopeSerializer.FormatDuration(new TimeSpan(1, 2, 3, 4, 500)));
        Assert.AreEqual("00:00:00", AzureMonitorEnvelopeSerializer.FormatDuration(TimeSpan.FromSeconds(-1)));

        var span = Records.Span();
        span.EndTimeUnixNano = span.StartTimeUnixNano - 1;
        Assert.AreEqual(TimeSpan.Zero, span.Duration);
    }
}
