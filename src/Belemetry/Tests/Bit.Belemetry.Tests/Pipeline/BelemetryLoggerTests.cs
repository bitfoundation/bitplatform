using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Tests.Pipeline;

[TestClass]
public class BelemetryLoggerTests
{
    [TestMethod]
    public void SeverityMapping()
    {
        Assert.AreEqual(BelemetrySeverity.Trace, BelemetryLogger.ToSeverity(LogLevel.Trace));
        Assert.AreEqual(BelemetrySeverity.Debug, BelemetryLogger.ToSeverity(LogLevel.Debug));
        Assert.AreEqual(BelemetrySeverity.Info, BelemetryLogger.ToSeverity(LogLevel.Information));
        Assert.AreEqual(BelemetrySeverity.Warn, BelemetryLogger.ToSeverity(LogLevel.Warning));
        Assert.AreEqual(BelemetrySeverity.Error, BelemetryLogger.ToSeverity(LogLevel.Error));
        Assert.AreEqual(BelemetrySeverity.Fatal, BelemetryLogger.ToSeverity(LogLevel.Critical));
        Assert.AreEqual(BelemetrySeverity.Unspecified, BelemetryLogger.ToSeverity(LogLevel.None));
    }

    [TestMethod]
    public async Task StructuredState_BecomesAttributes()
    {
        await using var host = TestHost.Create();
        var logger = host.StartLogging().CreateLogger("MyApp.Orders");

        logger.Log(LogLevel.Information, new EventId(7, "Shipped"), "Order {OrderId} shipped to {City}", 42, "Paris");
        logger.LogWarning("Plain warning");

        await host.FlushAsync();
        var logs = host.Memory.Logs.ToList();
        Assert.HasCount(2, logs);

        var log = logs[0];
        Assert.AreEqual("Order 42 shipped to Paris", log.Body);
        Assert.AreEqual(BelemetrySeverity.Info, log.Severity);
        Assert.AreEqual("Information", log.SeverityText);
        Assert.AreEqual("MyApp.Orders", log.Scope.Name);
        Assert.AreEqual(BelemetryFeatures.Logs, log.Feature);
        Assert.AreEqual(42L, log.Attributes["OrderId"]);
        Assert.AreEqual("Paris", log.Attributes["City"]);
        Assert.AreEqual(7L, log.Attributes["log.event.id"]);
        Assert.AreEqual("Shipped", log.Attributes["log.event.name"]);
        Assert.IsFalse(log.Attributes.Contains("{OriginalFormat}"));
        Assert.IsFalse(log.Attributes.Contains("log.record.template"));
        Assert.IsTrue(log.TimeUnixNano > 0);
        Assert.AreEqual(log.TimeUnixNano, log.ObservedTimeUnixNano);

        Assert.AreEqual(BelemetrySeverity.Warn, logs[1].Severity);
        Assert.IsFalse(logs[1].Attributes.Contains("log.event.id"));
    }

    [TestMethod]
    public async Task OriginalFormat_IsKeptAsTemplateWhenAsked()
    {
        await using var host = TestHost.Create(o => o.Logs.IncludeMessageTemplate = true);
        var logger = host.StartLogging().CreateLogger("MyApp");

        logger.LogInformation("Hello {Name}", "Ada");

        await host.FlushAsync();
        var log = host.Memory.Logs.Single();
        Assert.AreEqual("Hello {Name}", log.Attributes["log.record.template"]);
        Assert.IsFalse(log.Attributes.Contains("{OriginalFormat}"));
    }

    [TestMethod]
    public async Task Scopes_BecomeAttributes_WithoutOverridingTheRecord()
    {
        await using var host = TestHost.Create();
        var logger = host.StartLogging().CreateLogger("MyApp");

        using (logger.BeginScope(new Dictionary<string, object?> { ["TenantId"] = "t1", ["City"] = "Scope city" }))
        using (logger.BeginScope("Request {RequestId}", "r-9"))
        {
            logger.LogInformation("In {City}", "Rome");
        }
        logger.LogInformation("Outside");

        await host.FlushAsync();
        var logs = host.Memory.Logs.ToList();
        Assert.AreEqual("t1", logs[0].Attributes["TenantId"]);
        Assert.AreEqual("r-9", logs[0].Attributes["RequestId"]);
        Assert.AreEqual("Rome", logs[0].Attributes["City"]);
        Assert.IsFalse(logs[0].Attributes.Contains("{OriginalFormat}"));
        Assert.IsFalse(logs[1].Attributes.Contains("TenantId"));
    }

    [TestMethod]
    public async Task Scopes_CanBeTurnedOff()
    {
        await using var host = TestHost.Create(o => o.Logs.IncludeScopes = false);
        var logger = host.StartLogging().CreateLogger("MyApp");

        using (logger.BeginScope(new Dictionary<string, object?> { ["TenantId"] = "t1" }))
        {
            logger.LogInformation("x");
        }

        await host.FlushAsync();
        Assert.IsFalse(host.Memory.Logs.Single().Attributes.Contains("TenantId"));
    }

    [TestMethod]
    public async Task Exceptions_AddAttributes_AndCountAsErrors()
    {
        await using var host = TestHost.Create();
        var logger = host.StartLogging().CreateLogger("MyApp");

        var exception = Throw();
        logger.LogWarning(exception, "Recovered");
        logger.LogError("Failed without exception");
        logger.LogCritical("Fatal");

        await host.FlushAsync();
        var logs = host.Memory.Logs.ToList();

        Assert.AreEqual(BelemetryFeatures.Errors, logs[0].Feature, "A log carrying an exception is an error record.");
        Assert.AreEqual(BelemetrySeverity.Warn, logs[0].Severity);
        Assert.AreEqual(typeof(InvalidOperationException).FullName, logs[0].Attributes["exception.type"]);
        Assert.AreEqual("bad state", logs[0].Attributes["exception.message"]);
        StringAssert.Contains((string)logs[0].Attributes["exception.stacktrace"]!, nameof(Throw));
        Assert.IsTrue(logs[0].HasException);

        Assert.AreEqual(BelemetryFeatures.Errors, logs[1].Feature);
        Assert.IsFalse(logs[1].HasException);
        Assert.AreEqual(BelemetrySeverity.Fatal, logs[2].Severity);
        Assert.AreEqual(BelemetryFeatures.Errors, logs[2].Feature);
    }

    [TestMethod]
    public async Task ErrorsAndLogs_AreGatedSeparately()
    {
        await using var host = TestHost.Create(o => o.Features = BelemetryFeatures.Errors);
        var logger = host.StartLogging().CreateLogger("MyApp");

        Assert.IsFalse(logger.IsEnabled(LogLevel.Information));
        Assert.IsTrue(logger.IsEnabled(LogLevel.Error));

        logger.LogInformation("dropped");
        logger.LogError("kept");

        await host.FlushAsync();
        Assert.AreEqual("kept", host.Memory.Logs.Single().Body);
    }

    [TestMethod]
    public async Task MinimumLevel_AndExcludedCategories()
    {
        await using var host = TestHost.Create(o => o.Logs.ExcludedCategories.Add("Noisy."));
        var factory = host.StartLogging();

        var app = factory.CreateLogger("MyApp");
        Assert.IsFalse(app.IsEnabled(LogLevel.Debug), "The default minimum is Information.");
        Assert.IsFalse(app.IsEnabled(LogLevel.None));
        app.LogDebug("dropped");

        var own = factory.CreateLogger(BelemetryLogger.DiagnosticsCategory);
        Assert.IsFalse(own.IsEnabled(LogLevel.Critical), "Belemetry never exports its own diagnostics.");
        own.LogError("dropped");

        var noisy = factory.CreateLogger("Noisy.Component");
        Assert.IsFalse(noisy.IsEnabled(LogLevel.Error));
        noisy.LogError("dropped");

        app.LogInformation("kept");

        await host.FlushAsync();
        Assert.AreEqual("kept", host.Memory.Logs.Single().Body);
    }

    [TestMethod]
    public async Task AppCategories_UnderTheBelemetryNamespace_AreCollected()
    {
        await using var host = TestHost.Create();
        var factory = host.StartLogging();

        // Only the diagnostics category itself is Belemetry's; apps (the demo, the samples) live under Bit.Belemetry.*.
        var demo = factory.CreateLogger("Bit.Belemetry.Demo.Client.Pages.DotnetPage");
        Assert.IsTrue(demo.IsEnabled(LogLevel.Information));
        demo.LogInformation("kept");

        await host.FlushAsync();
        var log = host.Memory.Logs.Single();
        Assert.AreEqual("kept", log.Body);
        Assert.AreEqual("Bit.Belemetry.Demo.Client.Pages.DotnetPage", log.Scope.Name);
    }

    [TestMethod]
    public async Task SuppressedFlows_AreNotLogged()
    {
        await using var host = TestHost.Create();
        var logger = host.StartLogging().CreateLogger("MyApp");

        using (BelemetrySuppression.Begin())
        {
            logger.LogError("inside an export");
        }

        await host.FlushAsync();
        Assert.IsEmpty(host.Memory.Logs);
    }

    [TestMethod]
    public async Task TraceContext_ComesFromActivityCurrent()
    {
        await using var host = TestHost.Create();
        var logger = host.StartLogging().CreateLogger("MyApp");

        string traceId, spanId;
        using (var activity = new Activity("op").SetIdFormat(ActivityIdFormat.W3C))
        {
            activity.ActivityTraceFlags = ActivityTraceFlags.Recorded;
            activity.Start();
            traceId = activity.TraceId.ToHexString();
            spanId = activity.SpanId.ToHexString();
            logger.LogInformation("inside");
        }
        logger.LogInformation("outside");

        await host.FlushAsync();
        var logs = host.Memory.Logs.ToList();
        Assert.AreEqual(traceId, logs[0].TraceId);
        Assert.AreEqual(spanId, logs[0].SpanId);
        Assert.AreEqual(1, logs[0].Flags);
        Assert.IsNull(logs[1].TraceId);
        Assert.IsNull(logs[1].SpanId);
    }

    [TestMethod]
    public async Task NotCollecting_WhenThePipelineDoesNotRun()
    {
        await using var host = TestHost.Create(o => o.Host = BelemetryHostKind.Server);
        var logger = host.StartLogging().CreateLogger("MyApp");

        Assert.IsFalse(host.Pipeline.IsCollecting);
        Assert.IsFalse(logger.IsEnabled(LogLevel.Critical));
    }

    private static InvalidOperationException Throw()
    {
        try
        {
            throw new InvalidOperationException("bad state");
        }
        catch (InvalidOperationException ex)
        {
            return ex;
        }
    }
}
