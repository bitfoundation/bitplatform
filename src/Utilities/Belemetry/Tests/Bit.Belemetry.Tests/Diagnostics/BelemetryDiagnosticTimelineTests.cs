using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Tests.Diagnostics;

/// <summary>What reaches the diagnostic timeline, and what never does.</summary>
[TestClass]
public class BelemetryDiagnosticTimelineTests
{
    [TestMethod]
    public async Task Disabled_RecordsNothing_AndACaptureSaysWhy()
    {
        await using var host = DiagnosticsTestHost.Create(o => o.Diagnostics.Enabled = false);
        host.Factory.CreateLogger("App").LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("boom")), "Failed");

        Assert.IsFalse(host.Diagnostics.IsEnabled);
        Assert.IsEmpty(host.Diagnostics.GetEvents());
        var capture = await host.Diagnostics.CaptureAsync(cancellationToken: TestContext.CancellationToken);
        Assert.IsFalse(capture.Succeeded);
        Assert.Contains("not enabled", capture.Error!);
        Assert.Throws<InvalidOperationException>(() => capture.Confirm());
    }

    [TestMethod]
    public async Task LoggedExceptions_AreRecorded_WithTheirStructure_AfterPrivacy()
    {
        await using var host = DiagnosticsTestHost.Create();
        var inner = DiagnosticsTestHost.Thrown(() => new TimeoutException("db timeout for jane@example.com"));
        var ex = DiagnosticsTestHost.Thrown(() => new InvalidOperationException("Order 10423 failed for jane@example.com", inner));

        host.Factory.CreateLogger("MyApp.Orders").LogError(ex, "Saving order {OrderId} for {Email} failed", 10423, "jane@example.com");

        var e = host.Diagnostics.GetEvents().Single();
        Assert.AreEqual(BelemetryDiagnosticCategories.Exception, e.Category);
        Assert.AreEqual(BelemetryDiagnosticOutcome.Failure, e.Outcome);
        Assert.AreEqual("Saving order {OrderId} for {Email} failed", e.Message, "The template, not the rendered message.");
        Assert.AreEqual("System.InvalidOperationException", e.Exception!.Type);
        Assert.AreEqual("Order 10423 failed for [email]", e.Exception.Message);
        Assert.AreEqual("Order {n} failed for [email]", e.Exception.MessageTemplate);
        Assert.AreEqual("System.TimeoutException", e.Exception.InnerExceptions.Single().Type);
        Assert.AreEqual("db timeout for [email]", e.Exception.InnerExceptions.Single().Message);
        Assert.AreEqual("log", e.Exception.Origin);
        Assert.AreEqual("MyApp.Orders", e.Attributes["log.category"]);
        Assert.IsFalse(e.Attributes.ContainsKey("OrderId"), "Structured values are not kept unless allowlisted.");
        Assert.IsFalse(e.Attributes.ContainsKey("Email"));

        var group = host.Diagnostics.GetExceptionGroups().Single();
        Assert.AreEqual(e.Exception.Fingerprint, group.Fingerprint);
        Assert.AreEqual(1, group.Count);
        Assert.AreSequenceEqual(new[] { "1.2.3" }, group.Releases.ToArray());
    }

    [TestMethod]
    public async Task ExceptionMessages_CanBeLeftOut_WithTheirTemplates_StillGrouped()
    {
        await using var host = DiagnosticsTestHost.Create(o => o.Diagnostics.IncludeExceptionMessages = false);
        host.Factory.CreateLogger("App").LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("Customer 'Jane Doe' not found")), "Failed");

        var exception = host.Diagnostics.GetEvents().Single().Exception!;
        Assert.IsNull(exception.Message);
        Assert.IsNull(exception.MessageTemplate, "A template keeps quoted text whole when there is no stack.");
        Assert.DoesNotContain("Jane Doe", exception.StackTrace ?? string.Empty, "Nor in the stack text, which starts with it.");
        Assert.AreEqual(24, exception.Fingerprint.Length);
    }

    [TestMethod]
    public async Task ExporterlessApps_StillRecord()
    {
        await using var host = DiagnosticsTestHost.Create(o => o.Exporters.Clear());
        Assert.IsFalse(host.Host.Settings.HasExporters);

        host.Factory.CreateLogger("App").LogWarning("Cart is slow");

        Assert.ContainsSingle(host.Diagnostics.GetEvents());
    }

    [TestMethod]
    public async Task TheTimeline_HasItsOwnLevel_BelowTheExport()
    {
        await using var host = DiagnosticsTestHost.Create(o => o.Logs.MinimumLevel = LogLevel.Error);
        var logger = host.Factory.CreateLogger("App");

        logger.LogWarning("Only on the timeline");
        logger.LogInformation("Nowhere: below the timeline's Warning");

        await host.Host.FlushAsync();
        Assert.IsEmpty(host.Host.Memory.Logs, "Below the export's level: not exported.");
        Assert.AreEqual("Only on the timeline", host.Diagnostics.GetEvents().Single().Message);
    }

    [TestMethod]
    public async Task UndecidedConsent_IsHeld_ThenOnlyTheGrantedIsCaptured_AndARevokePurges()
    {
        await using var host = DiagnosticsTestHost.Create(consentDefault: BelemetryFeatures.None);
        var logger = host.Factory.CreateLogger("App");
        logger.LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("e")), "error");
        logger.LogWarning("warning");

        Assert.IsEmpty(host.Diagnostics.GetEvents(), "Nothing granted yet: nothing capturable.");

        host.Host.Consent.Decide(BelemetryFeatures.Errors);
        var events = host.Diagnostics.GetEvents();
        Assert.ContainsSingle(events, "The decision drops what was not granted.");
        Assert.AreEqual(BelemetryFeatures.Errors, events[0].Feature);

        host.Host.Consent.Decide(BelemetryFeatures.None);
        Assert.IsEmpty(host.Diagnostics.GetEvents());
        Assert.IsEmpty(host.Diagnostics.GetExceptionGroups(), "A revoke of Errors forgets the groups too.");

        logger.LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("e")), "after the revoke");
        Assert.IsEmpty(host.Hub.Global!.Events(), "Not recorded at all once revoked.");
    }

    [TestMethod]
    public async Task AProcessorThatDropsARecord_KeepsItOffTheTimelineToo()
    {
        await using var host = DiagnosticsTestHost.Create(o => o.AddProcessor(new DropSecretLogs()));
        var logger = host.Factory.CreateLogger("App");
        logger.LogWarning("secret thing");
        logger.LogWarning("public thing");

        Assert.AreEqual("public thing", host.Diagnostics.GetEvents().Single().Message);
    }

    [TestMethod]
    public async Task RelayedBrowserRecords_AreNotTheTimelinesSource()
    {
        await using var host = DiagnosticsTestHost.Create();
        host.Host.Pipeline.Enqueue(new BelemetryLogRecord { Body = "js", Feature = BelemetryFeatures.Errors, Severity = BelemetrySeverity.Error, TimeUnixNano = Records.Start }, relayed: true);
        Assert.IsEmpty(host.Diagnostics.GetEvents(), "The browser's own timeline is read at capture time instead.");
    }

    [TestMethod]
    public async Task HttpSpans_KeepMetadataOnly()
    {
        await using var host = DiagnosticsTestHost.Create();
        var span = Records.Span();
        span.Status = BelemetryStatusCode.Error;
        span.Attributes.Set(BelemetrySemanticConventions.HttpRequestMethod, "POST");
        span.Attributes.Set(BelemetrySemanticConventions.HttpResponseStatusCode, 503);
        span.Attributes.Set(BelemetrySemanticConventions.UrlFull, "https://api.shop.test/orders/42?token=abc&page=2");
        span.Attributes.Set("http.request.header.authorization", "Bearer xyz");
        span.Attributes.Set("http.request.body", "{\"card\":\"4111111111111111\"}");

        host.Host.Pipeline.Enqueue(span);

        var e = host.Diagnostics.GetEvents().Single();
        Assert.AreEqual(BelemetryDiagnosticCategories.Http, e.Category);
        Assert.AreEqual("POST", e.Name);
        Assert.AreEqual(BelemetryDiagnosticOutcome.Failure, e.Outcome);
        Assert.AreEqual(503, Convert.ToInt32(e.Attributes[BelemetrySemanticConventions.HttpResponseStatusCode]));
        Assert.AreEqual("https://api.shop.test/orders/{n}", e.Attributes["url.template"]);
        Assert.DoesNotContain(k => k.Contains("header") || k.Contains("body"), e.Attributes.Keys);
        Assert.AreEqual(Records.TraceId, e.TraceId);
        Assert.AreEqual(Records.ParentSpanId, e.ParentSpanId);
    }

    [TestMethod]
    public async Task FrameworkActivities_NameTheComponent_AndTheRouteTemplate_ExportedOrNot()
    {
        await using var host = DiagnosticsTestHost.Create();
        using var source = new ActivitySource(BelemetryDiagnosticEventFactory.ComponentsSource);

        using (var navigate = source.StartActivity(BelemetryDiagnosticEventFactory.NavigateActivity))
        {
            Assert.IsNotNull(navigate, "The hub's listener makes the framework create its activities.");
            navigate.SetTag(BelemetryDiagnosticEventFactory.RouteTag, "/orders/{id}");
            navigate.SetTag(BelemetryDiagnosticEventFactory.ComponentTypeTag, "MyApp.Pages.Orders");
            Assert.IsFalse(navigate.Recorded, "Not sampled for export by diagnostics.");
        }

        Exception thrown;
        string traceId;
        using (var handle = source.StartActivity(BelemetryDiagnosticEventFactory.HandleEventActivity)!)
        {
            handle.SetTag(BelemetryDiagnosticEventFactory.ComponentTypeTag, "MyApp.Pages.Orders");
            handle.SetTag(BelemetryDiagnosticEventFactory.MethodTag, "Save");
            handle.SetTag(BelemetryDiagnosticEventFactory.AttributeNameTag, "onclick");
            traceId = handle.TraceId.ToHexString();
            thrown = DiagnosticsTestHost.Thrown(() => new InvalidOperationException("save failed"));
            host.Factory.CreateLogger("Microsoft.AspNetCore.Components.WebAssembly.Rendering.WebAssemblyRenderer").LogCritical(thrown, "Unhandled exception rendering component: {Message}", thrown.Message);
            handle.SetTag("error.type", thrown.GetType().FullName);
        }

        var events = host.Diagnostics.GetEvents();
        var navigation = events.Single(e => e.Category == BelemetryDiagnosticCategories.Navigation);
        Assert.AreEqual("/orders/{id}", navigation.Route);
        Assert.AreEqual("framework", navigation.RouteSource);
        Assert.AreEqual(BelemetryFeatures.PageViews, navigation.Feature);

        var handler = events.Single(e => e.Name == "handle_event");
        Assert.AreEqual("MyApp.Pages.Orders", handler.ComponentType);
        Assert.AreEqual("Save", handler.Attributes[BelemetryDiagnosticEventFactory.MethodTag]);
        Assert.AreEqual(BelemetryDiagnosticOutcome.Failure, handler.Outcome);

        var exception = events.Single(e => e.Exception is not null);
        Assert.AreEqual("unhandled", exception.Exception!.Origin);
        Assert.IsFalse(exception.Exception.Handled);
        Assert.AreEqual(traceId, exception.TraceId, "Logged inside the handler: the same trace.");

        var capture = await host.Diagnostics.CaptureAsync(cancellationToken: TestContext.CancellationToken);
        var report = capture.Report;
        Assert.AreEqual("onclick on MyApp.Pages.Orders.Save", report.TriggeringAction);
        Assert.AreEqual("/orders/{id}", report.Route);
        Assert.Contains(f => f.Kind == BelemetryIssueFindingKind.Observed && f.Text.Contains("handled onclick") && f.Text.Contains(traceId), report.Findings);
        Assert.AreEqual("trace", capture.Components.First().Association);
        Assert.AreEqual("MyApp.Pages.Orders", capture.Components.First().Type);
    }

    [TestMethod]
    public async Task OnlyTheConfiguredCategories_AreRecorded()
    {
        await using var host = DiagnosticsTestHost.Create(o => o.Diagnostics.Categories = BelemetryDiagnosticCategories.Exception);
        var logger = host.Factory.CreateLogger("App");
        logger.LogWarning("a warning");
        logger.LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("e")), "an error");

        Assert.AreEqual(BelemetryDiagnosticCategories.Exception, host.Diagnostics.GetEvents().Single().Category);
    }

    [TestMethod]
    public async Task TheRings_AreBounded_AndFailuresSurviveChatter()
    {
        await using var host = DiagnosticsTestHost.Create(o =>
        {
            o.Diagnostics.MaxEvents = 10;
            o.Diagnostics.MaxFailureEvents = 3;
        });
        var logger = host.Factory.CreateLogger("App");
        logger.LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("the one that matters")), "failure");
        for (var i = 0; i < 100; i++) logger.LogWarning("chatter {I}", i);

        var events = host.Hub.Global!.Events();
        Assert.HasCount(11, events);
        Assert.IsNotNull(events.Single(e => e.Exception is not null), "Kept: failures have a ring of their own.");
        Assert.AreEqual(90, host.Hub.Global.Dropped()["activity_limit"]);

        for (var i = 0; i < 5; i++) logger.LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("e" + i)), "failure");
        Assert.AreEqual(3, host.Hub.Global.Events().Count(e => e.IsFailure));
    }

    [TestMethod]
    public async Task TheMemoryBudget_EvictsChatterFirst()
    {
        await using var host = DiagnosticsTestHost.Create(o =>
        {
            o.Diagnostics.MaxMemoryBytes = 16 * 1024;
            o.Diagnostics.IncludeLogMessages = true;
        });
        var logger = host.Factory.CreateLogger("App");
        logger.LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("kept")), "failure");
        for (var i = 0; i < 50; i++) logger.LogWarning("{Big}", new string('x', 1000));

        var events = host.Hub.Global!.Events();
        Assert.IsLessThan(50, events.Count);
        Assert.Contains(e => e.Exception is not null, events);
        Assert.IsGreaterThan(0, host.Hub.Global.Dropped()["memory_limit"]);
    }

    [TestMethod]
    public async Task ExceptionGroups_AreBounded_LeastRecentlySeenFirst()
    {
        await using var host = DiagnosticsTestHost.Create(o => o.Diagnostics.MaxExceptionGroups = 2);
        var logger = host.Factory.CreateLogger("App");
        logger.LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("a")), "a");
        logger.LogError(DiagnosticsTestHost.Thrown(() => new ArgumentException("b")), "b");
        logger.LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("a")), "a again");
        logger.LogError(DiagnosticsTestHost.Thrown(() => new TimeoutException("c")), "c");

        var groups = host.Diagnostics.GetExceptionGroups();
        Assert.AreSequenceEqual(new[] { "System.TimeoutException", "System.InvalidOperationException" }, groups.Select(g => g.ExceptionType).ToArray(), Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
        Assert.AreEqual(2, groups.Single(g => g.ExceptionType == "System.InvalidOperationException").Count);
    }

    [TestMethod]
    public async Task AClassifier_CanCategorize_OverrideTheGroup_OrIgnore()
    {
        await using var host = DiagnosticsTestHost.Create(configureServices: s => s.AddSingleton<IBelemetryExceptionClassifier, TestClassifier>());
        var logger = host.Factory.CreateLogger("App");
        logger.LogError(DiagnosticsTestHost.Thrown(() => new TaskCanceledException("expected")), "ignored");
        logger.LogError(DiagnosticsTestHost.Thrown(() => new HttpRequestException("one")), "network 1");
        logger.LogError(DiagnosticsTestHost.Thrown(() => new HttpRequestException("two", null, System.Net.HttpStatusCode.BadGateway)), "network 2");

        var events = host.Diagnostics.GetEvents();
        Assert.HasCount(2, events);
        Assert.IsTrue(events.All(e => e.Exception!.Classification == "network"));
        Assert.AreEqual(BelemetryExceptionFingerprint.FromKey("network"), events[0].Exception!.Fingerprint);
        var group = host.Diagnostics.GetExceptionGroups().Single();
        Assert.AreEqual("custom", group.FingerprintVersion);
        Assert.AreEqual(2, group.Count);
    }

    [TestMethod]
    public async Task TheAppsOwnEvents_AreSanitizedLikeTheRest()
    {
        await using var host = DiagnosticsTestHost.Create();
        host.Diagnostics.Record("checkout.submitted", new Dictionary<string, object?> { ["items"] = 3, ["email"] = "jane@example.com", ["password"] = "x", ["card"] = new object() });

        var e = host.Diagnostics.GetEvents().Single();
        Assert.AreEqual(BelemetryDiagnosticCategories.Custom, e.Category);
        Assert.AreEqual(BelemetryDiagnosticSource.Application, e.Source);
        Assert.AreEqual(3, e.Attributes["items"]);
        Assert.AreEqual("[email]", e.Attributes["email"]);
        Assert.AreEqual("REDACTED", e.Attributes["password"]);
        Assert.IsFalse(e.Attributes.ContainsKey("card"), "An object is never serialized.");
    }

    [TestMethod]
    public async Task Clear_ForgetsEverything()
    {
        await using var host = DiagnosticsTestHost.Create();
        host.Factory.CreateLogger("App").LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("e")), "e");
        await host.Diagnostics.ClearAsync(TestContext.CancellationToken);
        Assert.IsEmpty(host.Diagnostics.GetEvents());
        Assert.IsEmpty(host.Diagnostics.GetExceptionGroups());
    }

    private sealed class DropSecretLogs : IBelemetryProcessor
    {
        public bool OnLog(BelemetryLogRecord log) => log.Body?.Contains("secret") is not true;
    }

    private sealed class TestClassifier : IBelemetryExceptionClassifier
    {
        public BelemetryExceptionClassification? Classify(BelemetryDiagnosticException exception) => exception.Type switch
        {
            "System.Threading.Tasks.TaskCanceledException" => new BelemetryExceptionClassification { Ignore = true },
            "System.Net.Http.HttpRequestException" => new BelemetryExceptionClassification { Category = "network", Fingerprint = "network" },
            _ => null,
        };
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
