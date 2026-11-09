using System.Text;

namespace Bit.Belemetry.Tests.Sentry;

/// <summary>Golden checks of the Sentry mapping documented in CLAUDE.md ("Sentry mapping").</summary>
[TestClass]
public class SentryEnvelopeSerializerTests
{
    private const string Dsn = "https://abc123@o1.ingest.sentry.io/42";

    private static SentryEnvelopeSerializer.Context Context(BelemetrySeverity? minimum = BelemetrySeverity.Error, string? release = null, bool includeUnhandled = true, bool maskIp = true) =>
        new(SentryDsn.Parse(Dsn), release, minimum, includeUnhandled, maskIp);

    private sealed record Envelope(JsonElement Header, JsonElement Item, JsonElement Event, int EventBytes);

    private static Envelope Parse(byte[] body)
    {
        var text = Encoding.UTF8.GetString(body);
        Assert.IsTrue(text.EndsWith('\n'), "An envelope ends with a newline.");
        var lines = text.Split('\n');
        Assert.HasCount(4, lines);
        using var header = JsonDocument.Parse(lines[0]);
        using var item = JsonDocument.Parse(lines[1]);
        using var e = JsonDocument.Parse(lines[2]);
        return new(header.RootElement.Clone(), item.RootElement.Clone(), e.RootElement.Clone(), Encoding.UTF8.GetByteCount(lines[2]));
    }

    private static List<JsonElement> Events(BelemetryBatch batch, SentryEnvelopeSerializer.Context? context = null) =>
        SentryEnvelopeSerializer.Serialize(batch, context ?? Context()).Select(b => Parse(b).Event).ToList();

    private static JsonElement Single(BelemetryLogRecord log, SentryEnvelopeSerializer.Context? context = null, BelemetryResource? resource = null) =>
        Events(new BelemetryBatch(resource ?? Records.Resource(), logs: [log]), context).Single();

    private static BelemetryLogRecord Error(Exception? exception = null, string scope = "MyApp.Orders", string? body = "Order failed")
    {
        var log = Records.Log(body, severity: BelemetrySeverity.Error);
        log.Scope = new BelemetryScope(scope);
        log.Feature = BelemetryFeatures.Errors;
        log.TraceId = Records.TraceId;
        log.SpanId = Records.SpanId;
        var ex = exception ?? SentryStackTraceTests.Thrower.Catch(SentryStackTraceTests.Thrower.Wrapped);
        log.Exception = ex;
        log.Attributes.Set(BelemetrySemanticConventions.ExceptionType, ex.GetType().FullName)
                      .Set(BelemetrySemanticConventions.ExceptionMessage, ex.Message)
                      .Set(BelemetrySemanticConventions.ExceptionStacktrace, ex.ToString());
        return log;
    }

    private static string? Str(JsonElement e, params string[] path)
    {
        foreach (var name in path)
        {
            if (e.ValueKind != JsonValueKind.Object || e.TryGetProperty(name, out e) is false) return null;
        }
        return e.ValueKind == JsonValueKind.String ? e.GetString() : e.GetRawText();
    }

    [TestMethod]
    public void TheEnvelope_HoldsOneEvent_WithItsLengthInBytes()
    {
        var log = Error();
        log.Attributes.Set(BelemetrySemanticConventions.ExceptionMessage, "café ☕");

        var envelope = Parse(SentryEnvelopeSerializer.Serialize(new BelemetryBatch(Records.Resource(), logs: [log]), Context()).Single());

        Assert.AreEqual(Str(envelope.Event, "event_id"), Str(envelope.Header, "event_id"));
        StringAssert.Matches(Str(envelope.Header, "event_id"), new System.Text.RegularExpressions.Regex("^[0-9a-f]{32}$"));
        Assert.AreEqual(Dsn, Str(envelope.Header, "dsn"));
        Assert.AreEqual("bit.belemetry.dotnet", Str(envelope.Header, "sdk", "name"));
        Assert.AreEqual("10.6.2", Str(envelope.Header, "sdk", "version"));
        Assert.AreEqual("event", Str(envelope.Item, "type"));
        Assert.AreEqual(envelope.EventBytes, envelope.Item.GetProperty("length").GetInt32());
        Assert.AreEqual("café ☕", envelope.Event.GetProperty("exception").GetProperty("values")[1].GetProperty("value").GetString());
    }

    [TestMethod]
    public void ALoggedException_IsAnErrorEvent_WithItsChainTraceUserAndSymbolicationData()
    {
        var log = Error();
        log.Attributes.Set("session.id", "s-1").Set("enduser.pseudo.id", "p-1").Set("user.id", "u-1").Set("tenant", "contoso").Set("retries", 2L)
                      .Set("ratio", 0.5).Set("tags", new[] { "a", "b" });

        var e = Single(log, resource: new BelemetryResource(Records.Resource().Attributes.Clone()
            .Set(BelemetrySemanticConventions.DeploymentEnvironmentName, "Production")
            .Set(BelemetrySemanticConventions.HostName, "web-01")
            .Set(BelemetrySemanticConventions.ProcessRuntimeName, ".NET")
            .Set(BelemetrySemanticConventions.ProcessRuntimeVersion, "11.0.0")
            .Set(BelemetrySemanticConventions.OsDescription, "Microsoft Windows 10.0.26100")));

        Assert.AreEqual(1_700_000_000.123, e.GetProperty("timestamp").GetDouble());
        Assert.AreEqual("csharp", Str(e, "platform"));
        Assert.AreEqual("error", Str(e, "level"));
        Assert.AreEqual("MyApp.Orders", Str(e, "logger"));
        Assert.AreEqual("checkout@2.0.0", Str(e, "release"));
        Assert.AreEqual("Production", Str(e, "environment"));
        Assert.AreEqual("web-01", Str(e, "server_name"));
        Assert.AreEqual("Order failed", Str(e, "message", "formatted"));
        Assert.IsFalse(e.TryGetProperty("transaction", out _));

        // Oldest first: the inner exception (the cause), then the outer one, which carries the mechanism.
        var values = e.GetProperty("exception").GetProperty("values").EnumerateArray().ToList();
        Assert.HasCount(2, values);
        Assert.AreEqual("System.TimeoutException", Str(values[0], "type"));
        Assert.AreEqual("late", Str(values[0], "value"), "The record's stack text holds the inner message as it is.");
        Assert.IsFalse(values[0].TryGetProperty("mechanism", out _));
        Assert.AreEqual("System.InvalidOperationException", Str(values[1], "type"));
        Assert.AreEqual("wrapped", Str(values[1], "value"));
        Assert.AreEqual("generic", Str(values[1], "mechanism", "type"));
        Assert.IsTrue(values[1].GetProperty("mechanism").GetProperty("handled").GetBoolean());

        var frame = values[0].GetProperty("stacktrace").GetProperty("frames").EnumerateArray().Last();
        Assert.AreEqual("Fail", Str(frame, "function"));
        Assert.AreEqual(typeof(SentryStackTraceTests.Thrower).FullName!.Replace('+', '.'), Str(frame, "module"));
        Assert.IsTrue(frame.GetProperty("in_app").GetBoolean());
        StringAssert.StartsWith(Str(frame, "instruction_addr"), "0x");
        StringAssert.StartsWith(Str(frame, "function_id"), "0x");
        StringAssert.StartsWith(Str(frame, "addr_mode"), "rel:");

        var images = e.GetProperty("debug_meta").GetProperty("images").EnumerateArray().ToList();
        var image = images[int.Parse(Str(frame, "addr_mode")!["rel:".Length..])];
        Assert.AreEqual("pe_dotnet", Str(image, "type"));
        Assert.AreEqual(typeof(SentryStackTraceTests.Thrower).Assembly.Location, Str(image, "code_file"));
        Assert.AreEqual(SentryDebugImage.Of(typeof(SentryStackTraceTests.Thrower).Module)!.DebugId, Str(image, "debug_id"));

        var contexts = e.GetProperty("contexts");
        Assert.AreEqual(Records.TraceId, Str(contexts, "trace", "trace_id"));
        Assert.AreEqual(Records.SpanId, Str(contexts, "trace", "span_id"));
        Assert.AreEqual(".NET", Str(contexts, "runtime", "name"));
        Assert.AreEqual("11.0.0", Str(contexts, "runtime", "version"));
        Assert.AreEqual("windows", Str(contexts, "os", "name"));
        Assert.AreEqual("Microsoft Windows 10.0.26100", Str(contexts, "os", "raw_description"));
        Assert.AreEqual("instance-42", Str(contexts, "otel", "resource", "service.instance.id"));

        Assert.AreEqual("s-1", Str(e, "tags", "session.id"));
        Assert.AreEqual("u-1", Str(e, "user", "id"));
        var extra = e.GetProperty("extra");
        CollectionAssert.AreEquivalent(new[] { "enduser.pseudo.id", "tenant", "retries", "ratio", "tags" }, extra.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.AreEqual(2, extra.GetProperty("retries").GetInt64());
        Assert.AreEqual(0.5, extra.GetProperty("ratio").GetDouble());
        Assert.AreEqual("b", extra.GetProperty("tags")[1].GetString());

        Assert.AreEqual("bit.belemetry.dotnet", Str(e, "sdk", "name"));
        Assert.AreEqual("never", Str(e, "sdk", "settings", "infer_ip"));
        Assert.IsFalse(e.TryGetProperty("request", out _), "No user agent in a .NET resource.");
    }

    [TestMethod]
    public void TheRecordsAttributes_WinOverTheExceptionObject()
    {
        // Privacy and processors work on the attributes: a removed stack trace is not sent, not even from the object.
        var log = Error();
        log.Attributes.Set(BelemetrySemanticConventions.ExceptionMessage, "[REDACTED]");
        log.Attributes.Remove(BelemetrySemanticConventions.ExceptionStacktrace);

        var e = Single(log);
        var value = e.GetProperty("exception").GetProperty("values").EnumerateArray().Single();
        Assert.AreEqual("[REDACTED]", Str(value, "value"));
        Assert.IsFalse(value.TryGetProperty("stacktrace", out _));
        Assert.IsFalse(e.TryGetProperty("debug_meta", out _));

        // An inner message the privacy rules changed in the stack text is not taken from the object.
        var scrubbed = Error();
        scrubbed.Attributes.Set(BelemetrySemanticConventions.ExceptionStacktrace, scrubbed.Exception!.ToString().Replace("late", "{scrubbed}"));
        var inner = Single(scrubbed).GetProperty("exception").GetProperty("values")[0];
        Assert.AreEqual("System.TimeoutException", Str(inner, "type"));
        Assert.IsFalse(inner.TryGetProperty("value", out _));
    }

    [TestMethod]
    public void WithoutTheObject_TheStackTextIsParsed()
    {
        var log = Error();
        log.Exception = null;

        var values = Single(log).GetProperty("exception").GetProperty("values").EnumerateArray().ToList();
        Assert.HasCount(2, values);
        Assert.AreEqual("System.TimeoutException", Str(values[0], "type"));
        Assert.AreEqual("late", Str(values[0], "value"));
        var frame = values[0].GetProperty("stacktrace").GetProperty("frames").EnumerateArray().Last();
        Assert.AreEqual("Fail", Str(frame, "function"));
        Assert.IsFalse(frame.TryGetProperty("function_id", out _), "Only the object has the method token.");
    }

    [TestMethod]
    public void UnhandledExceptions_AreFatalAndUnhandled_AndCanBeLeftToASentrySdk()
    {
        var crash = Error(scope: BelemetryUnhandledExceptions.ScopeName, body: "An unhandled exception ended the process.");
        crash.Severity = BelemetrySeverity.Fatal;
        crash.EventName = BelemetrySemanticConventions.ExceptionEvent;

        var e = Single(crash);
        Assert.AreEqual("fatal", Str(e, "level"));
        Assert.IsFalse(e.GetProperty("exception").GetProperty("values")[1].GetProperty("mechanism").GetProperty("handled").GetBoolean());

        Assert.IsEmpty(Events(new BelemetryBatch(Records.Resource(), logs: [crash]), Context(includeUnhandled: false)));

        // What the framework's renderer logs nobody handled; the app's own PdfRenderer handled it.
        Assert.IsFalse(Single(Error(scope: "Microsoft.AspNetCore.Components.RenderTree.Renderer")).GetProperty("exception").GetProperty("values")[1].GetProperty("mechanism").GetProperty("handled").GetBoolean());
        Assert.IsTrue(Single(Error(scope: "MyApp.PdfRenderer")).GetProperty("exception").GetProperty("values")[1].GetProperty("mechanism").GetProperty("handled").GetBoolean());
    }

    [TestMethod]
    public void PlainLogs_AtTheLevel_AreMessageEvents_AndNothingElseIs()
    {
        var error = Records.Log("Payment declined", severity: BelemetrySeverity.Error);
        var warning = Records.Log("Slow checkout", severity: BelemetrySeverity.Warn);
        var pageView = Records.Log("Home", BelemetrySemanticConventions.PageViewEvent, BelemetrySeverity.Error);
        var empty = Records.Log(null, severity: BelemetrySeverity.Error);
        var batch = new BelemetryBatch(Records.Resource(), logs: [error, warning, pageView, empty]);

        var events = Events(batch);
        Assert.HasCount(1, events);
        Assert.AreEqual("Payment declined", Str(events[0], "message", "formatted"));
        Assert.IsFalse(events[0].TryGetProperty("exception", out _));
        Assert.AreEqual(1, SentryEnvelopeSerializer.Count(batch, Context()));

        Assert.HasCount(2, Events(batch, Context(BelemetrySeverity.Warn)));
        Assert.AreEqual("warning", Str(Events(batch, Context(BelemetrySeverity.Warn))[1], "level"));
        Assert.HasCount(2, Events(batch, Context(minimum: null)), "Everything with a body that is not another event.");
        Assert.IsEmpty(Events(new BelemetryBatch(Records.Resource(), logs: [Error()]), Context(BelemetrySeverity.Fatal)));
    }

    [TestMethod]
    public void AnExceptionRecordedOnASpan_IsAnEvent_UnlessLimitedOrLogged()
    {
        var span = Records.Span(BelemetrySpanKind.Server, "GET /orders/{id}");
        span.Scope = new BelemetryScope("Microsoft.AspNetCore");
        span.Attributes.Set("session.id", "s-1").Set("user.id", "u-1");
        BelemetrySpanEvent Exception(string message) => new()
        {
            Name = BelemetrySemanticConventions.ExceptionEvent,
            TimeUnixNano = Records.Start + 1_000_000,
            Attributes = new BelemetryAttributes().Set(BelemetrySemanticConventions.ExceptionType, "System.Exception").Set(BelemetrySemanticConventions.ExceptionMessage, message)
                .Set(BelemetrySemanticConventions.ExceptionStacktrace, $"System.Exception: {message}\n   at Shop.Api.Get() in /src/Api.cs:line 7"),
        };

        var plain = Exception("plain");
        var limited = Exception("limited");
        limited.Limited = true;
        var logged = Exception("logged");
        logged.LoggedAs = new BelemetryLoggedException("System.Exception", "logged", "stack", BelemetrySeverity.Error, isEvent: false);
        var loggedBelowTheLevel = Exception("logged as a warning");
        loggedBelowTheLevel.LoggedAs = new BelemetryLoggedException("System.Exception", "logged as a warning", "stack", BelemetrySeverity.Warn, isEvent: false);
        var loggedAndDropped = Exception("logged and dropped");
        loggedAndDropped.LoggedAs = new BelemetryLoggedException("System.Exception", "logged and dropped", "stack", BelemetrySeverity.Error, isEvent: false);
        loggedAndDropped.LoggedAs.Drop();
        span.Events.AddRange([plain, limited, logged, loggedBelowTheLevel, loggedAndDropped, new BelemetrySpanEvent { Name = "retry" }]);
        var batch = new BelemetryBatch(Records.Resource(), [span]);

        var events = Events(batch);
        CollectionAssert.AreEqual(new[] { "plain", "logged as a warning", "logged and dropped" },
                                  events.Select(e => Str(e.GetProperty("exception").GetProperty("values")[0], "value")).ToArray());
        Assert.AreEqual(3, SentryEnvelopeSerializer.Count(batch, Context()));

        var e = events[0];
        Assert.AreEqual("GET /orders/{id}", Str(e, "transaction"));
        Assert.AreEqual("Microsoft.AspNetCore", Str(e, "logger"));
        Assert.AreEqual("error", Str(e, "level"));
        Assert.AreEqual(1_700_000_000.124, e.GetProperty("timestamp").GetDouble());
        Assert.AreEqual(Records.ParentSpanId, Str(e, "contexts", "trace", "parent_span_id"));
        Assert.AreEqual("s-1", Str(e, "tags", "session.id"));
        Assert.AreEqual("u-1", Str(e, "user", "id"));
        Assert.IsFalse(e.TryGetProperty("extra", out _), "The span's attributes are not the exception's.");
        var frame = e.GetProperty("exception").GetProperty("values")[0].GetProperty("stacktrace").GetProperty("frames")[0];
        Assert.AreEqual("Shop.Api", Str(frame, "module"));
        Assert.AreEqual("7", Str(frame, "lineno"));
    }

    [TestMethod]
    public void ARelayedBrowserError_IsTheEventTheScriptWrites()
    {
        // The same record and expectations as sentry.test.js ("writes a JS error as an unhandled error event").
        var resource = new BelemetryResource(new BelemetryAttributes()
            .Set("service.name", "shop").Set("service.version", "2.0.0").Set("service.instance.id", "inst-1").Set("deployment.environment.name", "Production")
            .Set("telemetry.sdk.language", "webjs").Set("telemetry.sdk.version", "10.6.2").Set("user_agent.original", "UA/1.0").Set("browser.language", "en-US"));
        var log = new BelemetryLogRecord
        {
            TimeUnixNano = 1_700_000_000_123_456_000,
            Severity = BelemetrySeverity.Error,
            Body = "x is undefined",
            EventName = "exception",
            Scope = new BelemetryScope("bit.belemetry.errors"),
            Feature = BelemetryFeatures.Errors,
            TraceId = "0af7651916cd43dd8448eb211c80319c",
            SpanId = "b7ad6b7169203331",
        };
        log.Attributes.Set("exception.type", "TypeError").Set("exception.message", "x is undefined")
            .Set("exception.stacktrace", "TypeError: x is undefined\n    at render (https://shop.test/app.js:10:15)")
            .Set("session.id", new string('s', 32)).Set("enduser.pseudo.id", new string('p', 32)).Set("user.id", "alice").Set("tenant", "contoso").Set("retries", 2L);

        var e = Single(log, resource: resource);
        var expected = $$"""
            {
              "event_id": "{{Str(e, "event_id")}}",
              "timestamp": 1700000000.123,
              "platform": "javascript",
              "level": "error",
              "logger": "bit.belemetry.errors",
              "release": "shop@2.0.0",
              "environment": "Production",
              "exception": { "values": [ {
                "type": "TypeError", "value": "x is undefined",
                "stacktrace": { "frames": [ { "function": "render", "filename": "https://shop.test/app.js", "abs_path": "https://shop.test/app.js", "lineno": 10, "colno": 15, "in_app": true } ] },
                "mechanism": { "type": "generic", "handled": false } } ] },
              "contexts": {
                "trace": { "trace_id": "0af7651916cd43dd8448eb211c80319c", "span_id": "b7ad6b7169203331", "type": "trace" },
                "culture": { "locale": "en-US", "type": "culture" },
                "otel": { "resource": { "service.name": "shop", "service.version": "2.0.0", "service.instance.id": "inst-1", "deployment.environment.name": "Production",
                                        "telemetry.sdk.language": "webjs", "telemetry.sdk.version": "10.6.2", "user_agent.original": "UA/1.0", "browser.language": "en-US" }, "type": "otel" }
              },
              "tags": { "session.id": "{{new string('s', 32)}}" },
              "user": { "id": "alice" },
              "request": { "headers": { "User-Agent": "UA/1.0" } },
              "extra": { "enduser.pseudo.id": "{{new string('p', 32)}}", "tenant": "contoso", "retries": 2 },
              "sdk": { "name": "bit.belemetry.javascript", "version": "10.6.2", "settings": { "infer_ip": "never" } }
            }
            """;
        using var document = JsonDocument.Parse(expected);
        Assert.IsTrue(JsonElement.DeepEquals(document.RootElement, e), e.GetRawText());
    }

    [TestMethod]
    public void ADotnetExceptionTheScriptRecorded_HasDotnetFrames()
    {
        // A Blazor Server component's TrackException goes to the page's script, which relays it: the same event as
        // sentry.test.js ("make a csharp event when the script recorded a .NET exception").
        var resource = new BelemetryResource(new BelemetryAttributes().Set("service.name", "shop").Set("telemetry.sdk.language", "webjs"));
        var exception = SentryStackTraceTests.Thrower.Catch(SentryStackTraceTests.Thrower.Wrapped);
        var log = Records.Log("Order failed", BelemetrySemanticConventions.ExceptionEvent, BelemetrySeverity.Error);
        log.Scope = new BelemetryScope("Bit.Belemetry.Api");
        log.Attributes.Set("exception.type", exception.GetType().FullName).Set("exception.message", exception.Message).Set("exception.stacktrace", exception.ToString());

        var e = Single(log, resource: resource);
        Assert.AreEqual("csharp", Str(e, "platform"));
        Assert.AreEqual("bit.belemetry.javascript", Str(e, "sdk", "name"), "Who recorded it.");
        var values = e.GetProperty("exception").GetProperty("values").EnumerateArray().ToList();
        CollectionAssert.AreEqual(new[] { "System.TimeoutException", "System.InvalidOperationException" }, values.Select(v => Str(v, "type")).ToArray());
        Assert.AreEqual("late", Str(values[0], "value"));
        var frame = values[0].GetProperty("stacktrace").GetProperty("frames").EnumerateArray().Last();
        Assert.AreEqual("Fail", Str(frame, "function"));
        Assert.AreEqual(typeof(SentryStackTraceTests.Thrower).FullName!.Replace('+', '.'), Str(frame, "module"));
        Assert.IsTrue(values[1].GetProperty("mechanism").GetProperty("handled").GetBoolean());
    }

    [TestMethod]
    public void TheReleaseAndTheIpInference_AreConfigurable()
    {
        var e = Single(Error(), Context(release: "web@9", maskIp: false));
        Assert.AreEqual("web@9", Str(e, "release"));
        Assert.AreEqual("auto", Str(e, "sdk", "settings", "infer_ip"));

        var withoutVersion = new BelemetryResource(new BelemetryAttributes().Set(BelemetrySemanticConventions.ServiceName, "checkout"));
        Assert.IsFalse(Single(Error(), resource: withoutVersion).TryGetProperty("release", out _));
    }

    [TestMethod]
    public void TheExporter_PostsOneRequestPerEvent()
    {
        var exporter = new BelemetrySentryExporter(new BelemetrySentryExporterOptions { Dsn = "https://abc123:s3cr3t@o1.ingest.sentry.io/42", Compression = BelemetryCompression.Gzip }, "sentry");
        var batch = new BelemetryBatch(Records.Resource(), logs: [Error(), Records.Log(), Error(body: "second")]);

        var requests = exporter.CreateRequests(BelemetryExporterSet.WithMinimumSeverity(batch, exporter.MinimumSeverity)).ToList();
        Assert.HasCount(2, requests);
        Assert.AreEqual(2, exporter.CountItems(batch));
        foreach (var request in requests)
        {
            Assert.AreEqual("sentry", request.Exporter);
            Assert.AreEqual("https://o1.ingest.sentry.io/api/42/envelope/?sentry_key=abc123&sentry_version=7", request.Url);
            Assert.AreEqual("application/x-sentry-envelope", request.ContentType);
            Assert.AreEqual("gzip", request.ContentEncoding);
            Assert.AreEqual(1, request.Count);
            Assert.DoesNotContain("s3cr3t", Encoding.UTF8.GetString(request.Body), "A legacy secret key is never sent.");
            Assert.IsTrue(exporter.PostsTo(request.Url));
        }
        Assert.AreEqual("second", Str(Parse(requests[1].Body).Event, "message", "formatted"));
        Assert.AreNotEqual(Str(Parse(requests[0].Body).Event, "event_id"), Str(Parse(requests[1].Body).Event, "event_id"));
        Assert.IsEmpty(exporter.Headers);

        Assert.IsTrue(exporter.IsRetryable(429));
        Assert.IsTrue(exporter.IsRetryable(503));
        Assert.IsFalse(exporter.IsRetryable(400));
        Assert.IsFalse(exporter.IsRetryable(413));
        Assert.AreEqual(BelemetrySeverity.Error, exporter.MinimumSeverity);
        Assert.IsNull(new BelemetrySentryExporter(new() { Dsn = Dsn, MinimumEventLevel = Microsoft.Extensions.Logging.LogLevel.Trace }, "sentry").MinimumSeverity);
    }

    [TestMethod]
    public void Dsn_NamesTheEnvelopeEndpoint()
    {
        var dsn = SentryDsn.Parse(Dsn);
        Assert.AreEqual("abc123", dsn.PublicKey);
        Assert.AreEqual("42", dsn.ProjectId);
        Assert.AreEqual("https://o1.ingest.sentry.io/api/42/envelope/", dsn.EnvelopeEndpoint);
        Assert.AreEqual("https://o1.ingest.sentry.io/api/42/envelope/?sentry_key=abc123&sentry_version=7", dsn.EnvelopeUrl);
        Assert.AreEqual(Dsn, dsn.PublicDsn);

        var selfHosted = SentryDsn.Parse("http://abc:s3cr3t@sentry.local:9000/sentry/7/");
        Assert.AreEqual("http://sentry.local:9000/sentry/api/7/envelope/", selfHosted.EnvelopeEndpoint);
        Assert.AreEqual("http://abc@sentry.local:9000/sentry/7", selfHosted.PublicDsn, "A legacy secret key is dropped.");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("not a url")]
    [DataRow("https://o1.ingest.sentry.io/42")]
    [DataRow("https://abc@o1.ingest.sentry.io")]
    [DataRow("ftp://abc@host/1")]
    public void Dsn_RejectsWhatIsNotOne(string value) => Assert.ThrowsExactly<FormatException>(() => SentryDsn.Parse(value));
}
