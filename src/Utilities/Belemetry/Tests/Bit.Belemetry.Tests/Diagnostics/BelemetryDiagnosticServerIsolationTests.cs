using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Tests.Diagnostics;

/// <summary>
/// A server serves everyone: each circuit has a timeline of its own, holding only what can be attributed to it - by the
/// renderer's synchronization context, or a UI event's activity that started on it - and nothing else, ever.
/// </summary>
[TestClass]
public class BelemetryDiagnosticServerIsolationTests
{
    private sealed class CircuitContext : SynchronizationContext;

    private static T On<T>(SynchronizationContext context, Func<T> action)
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            return action();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private static void On(SynchronizationContext context, Action action) => On(context, () =>
    {
        action();
        return 0;
    });

    [TestMethod]
    public async Task EachCircuit_SeesOnlyItsOwnEvents()
    {
        await using var host = TestHost.Create(o =>
        {
            o.Host = BelemetryHostKind.Server;
            o.Diagnostics.Enabled = true;
        });
        var logger = host.StartLogging().CreateLogger("MyApp.Orders");
        await using var scopeA = host.Services.CreateAsyncScope();
        await using var scopeB = host.Services.CreateAsyncScope();
        var circuitA = new CircuitContext();
        var circuitB = new CircuitContext();

        // Resolved on each circuit's renderer context, as <BelemetryInitializer /> does.
        var a = On(circuitA, () => scopeA.ServiceProvider.GetRequiredService<IBelemetryDiagnostics>());
        var b = On(circuitB, () => scopeB.ServiceProvider.GetRequiredService<IBelemetryDiagnostics>());

        On(circuitA, () => logger.LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("Alice's failure")), "A failed"));
        On(circuitB, () => logger.LogWarning("Bob's warning"));
        logger.LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("a background job")), "Nobody's");

        Assert.AreEqual("Alice's failure", a.GetEvents().Single().Exception!.Message);
        Assert.AreEqual("Bob's warning", b.GetEvents().Single().Message);
        Assert.HasCount(1, a.GetExceptionGroups());
        Assert.IsEmpty(b.GetExceptionGroups());

        var captureB = await On(circuitB, () => b.CaptureAsync().AsTask());
        Assert.IsFalse(captureB.Events.Any(e => e.Exception is not null), "Another circuit's exception is never in this circuit's capture.");
    }

    [TestMethod]
    public async Task WorkThatLeavesTheRenderer_UnderACircuitsEvent_StaysWithThatCircuit()
    {
        await using var host = TestHost.Create(o =>
        {
            o.Host = BelemetryHostKind.Server;
            o.Diagnostics.Enabled = true;
            // No tracer of the app's own in a test: Belemetry may create the framework's activities.
            o.Diagnostics.CreateFrameworkActivities = true;
        });
        var logger = host.StartLogging().CreateLogger("MyApp.Orders");
        await using var scope = host.Services.CreateAsyncScope();
        var circuit = new CircuitContext();
        var diagnostics = On(circuit, () => scope.ServiceProvider.GetRequiredService<IBelemetryDiagnostics>());
        using var source = new ActivitySource(BelemetryDiagnosticEventFactory.ComponentsSource);

        var handle = On(circuit, () => source.StartActivity(BelemetryDiagnosticEventFactory.HandleEventActivity))!;
        handle.SetTag(BelemetryDiagnosticEventFactory.ComponentTypeTag, "MyApp.Pages.Orders");

        // A library's ConfigureAwait(false): no synchronization context any more, the event's activity still current.
        await Task.Run(() => logger.LogWarning("after ConfigureAwait(false)"));
        On(circuit, handle.Stop);

        var events = diagnostics.GetEvents();
        Assert.IsTrue(events.Any(e => e.Message == "after ConfigureAwait(false)"));
        Assert.IsTrue(events.Any(e => e.Name == "handle_event" && e.ComponentType == "MyApp.Pages.Orders"));
    }

    [TestMethod]
    public async Task ADisposedCircuit_IsForgotten()
    {
        await using var host = TestHost.Create(o =>
        {
            o.Host = BelemetryHostKind.Server;
            o.Diagnostics.Enabled = true;
        });
        var logger = host.StartLogging().CreateLogger("App");
        var circuit = new CircuitContext();
        var scope = host.Services.CreateAsyncScope();
        var diagnostics = On(circuit, () => scope.ServiceProvider.GetRequiredService<IBelemetryDiagnostics>());
        await scope.DisposeAsync();

        On(circuit, () => logger.LogWarning("after the circuit ended"));
        Assert.IsEmpty(diagnostics.GetEvents());
        Assert.IsNull(On(circuit, () => host.Pipeline.Diagnostics.Resolve()));
    }

    [TestMethod]
    public async Task ServerErrors_OfAPagesOwnRequest_AreFoundByItsTraceId_OnlyByIt()
    {
        await using var host = TestHost.Create(o =>
        {
            o.Host = BelemetryHostKind.Server;
            o.Diagnostics.Enabled = true;
        });
        var logger = host.StartLogging().CreateLogger("MyApp.Api");
        using var source = new ActivitySource("Test.Requests");
        using var listener = new ActivityListener { ShouldListenTo = _ => true, Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData };
        ActivitySource.AddActivityListener(listener);

        string traceId;
        using (var request = source.StartActivity("request")!)
        {
            traceId = request.TraceId.ToHexString();
            logger.LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("API failed")), "API failed");
        }

        var index = host.Pipeline.Diagnostics.TraceIndex!;
        Assert.HasCount(1, index.Find([traceId]));
        Assert.IsEmpty(index.Find([ActivityTraceId.CreateRandom().ToHexString()]));
    }
}
