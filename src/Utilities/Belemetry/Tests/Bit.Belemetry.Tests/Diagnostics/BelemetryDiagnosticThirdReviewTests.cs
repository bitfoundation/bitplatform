using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Tests.Diagnostics;

/// <summary>The third review of production diagnostics: occurrences, the whole bundle's size, providers that hold a capture, a tool's own UI.</summary>
[TestClass]
public class BelemetryDiagnosticThirdReviewTests
{
    [TestMethod]
    public async Task TheExceptionInHand_IsItsOwnOccurrence_NotTheLatestOfItsGroup()
    {
        await using var host = DiagnosticsTestHost.Create();
        var logger = host.Factory.CreateLogger("App");
        static Exception Fail(int order) => DiagnosticsTestHost.Thrown(() => new InvalidOperationException($"Order {order} failed"));
        var first = Fail(10423);
        logger.LogError(first, "Saving failed");
        logger.LogError(Fail(99871), "Saving failed");
        var recorded = host.Diagnostics.GetEvents().Where(e => e.Exception is not null).ToList();
        Assert.HasCount(2, recorded);
        Assert.AreEqual(recorded[0].Exception!.Fingerprint, recorded[1].Exception!.Fingerprint, "One group.");

        var capture = await host.Diagnostics.CaptureAsync(new() { Exception = first });

        Assert.AreEqual(recorded[0].Sequence, capture.Subject!.Sequence, "The occurrence that logged this object, not the latest of its group.");
        Assert.AreEqual("Order 10423 failed", capture.Subject.Exception!.Message);
        Assert.AreEqual(2, capture.Events.Count(e => e.Exception is not null), "Not a third report of one failure.");
    }

    [TestMethod]
    public async Task WithoutExceptionMessages_TheStackLosesItsMessageLine_NotItsFingerprint()
    {
        await using var withMessages = DiagnosticsTestHost.Create();
        await using var without = DiagnosticsTestHost.Create(o => o.Diagnostics.IncludeExceptionMessages = false);
        var thrown = DiagnosticsTestHost.Thrown(() => new InvalidOperationException("Customer 'Jane Doe' not found"));
        withMessages.Factory.CreateLogger("App").LogError(thrown, "Failed");
        without.Factory.CreateLogger("App").LogError(thrown, "Failed");

        var kept = withMessages.Diagnostics.GetEvents().Single().Exception!;
        var left = without.Diagnostics.GetEvents().Single().Exception!;

        Assert.Contains("Jane Doe", kept.StackTrace!, "A .NET exception's text starts with its message.");
        Assert.DoesNotContain("Jane Doe", left.StackTrace!);
        Assert.Contains("at ", left.StackTrace!, "The frames stay.");
        Assert.AreEqual(kept.Fingerprint, left.Fingerprint);
    }

    [TestMethod]
    public async Task TheBundleSizeLimit_CountsSnapshots_AndKeepsTheWholeBundleUnderIt()
    {
        await using var host = DiagnosticsTestHost.Create(o => o.Diagnostics.MaxBundleBytes = 64 * 1024, configureServices: s => s.AddBitBelemetrySnapshotProvider<BigSnapshot>());
        var logger = host.Factory.CreateLogger("App");
        for (var i = 0; i < 50; i++) logger.LogWarning("Step {Step} is slow", i);
        logger.LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("kept")), "failure");
        for (var i = 0; i < 5; i++) await host.Diagnostics.RecordSnapshotAsync("step-" + i);

        var capture = await host.Diagnostics.CaptureAsync(new() { IncludeSnapshots = true });
        capture.Confirm();
        using var zip = new ZipArchive(new MemoryStream(await capture.ExportAsync()));

        Assert.IsLessThanOrEqualTo(64L * 1024, zip.Entries.Sum(e => e.Length), "Every file of the bundle, the snapshots included.");
        Assert.AreEqual("kept", capture.Subject?.Exception?.Message, "The failure is kept.");
        Assert.IsTrue(capture.Dropped.ContainsKey("snapshot_size_limit"));
        Assert.IsTrue(capture.MissingEvidence.Any(m => m.Contains("snapshots were left out at the bundle size limit")));
        Assert.IsNotEmpty(capture.Snapshots, "As many as fit stay.");
    }

    [TestMethod]
    public async Task ASnapshotProvider_ThatBlocks_OrGivesUpAtItsToken_TimesOut()
    {
        await using var host = DiagnosticsTestHost.Create(o => o.Diagnostics.Snapshots.Timeout = TimeSpan.FromMilliseconds(100), configureServices: s =>
        {
            s.AddBitBelemetrySnapshotProvider<BlockingSnapshot>();
            s.AddBitBelemetrySnapshotProvider<CooperativeSnapshot>();
        });

        var capture = await host.Diagnostics.CaptureAsync(new() { IncludeSnapshots = true });

        var blocking = capture.Snapshots.Single(s => s.Name == "blocking");
        Assert.AreEqual(BelemetrySnapshotStatus.TimedOut, blocking.Status, "It returned, but only after the timeout.");
        Assert.IsNull(blocking.Data);
        Assert.AreEqual(BelemetrySnapshotStatus.TimedOut, capture.Snapshots.Single(s => s.Name == "cooperative").Status, "Gave up as asked: not a failure.");
    }

    [TestMethod]
    public async Task AComponentProvider_ThatIgnoresItsToken_CannotHoldTheCapture()
    {
        await using var host = DiagnosticsTestHost.Create(configureServices: s => s.AddSingleton<IBelemetryComponentContextProvider, HangingProvider>());
        host.Factory.CreateLogger("App").LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("e")), "e");

        var capture = await host.Diagnostics.CaptureAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(30));

        Assert.IsTrue(capture.MissingEvidence.Any(m => m.Contains("'hanging' timed out")), string.Join("; ", capture.MissingEvidence));
    }

    [TestMethod]
    public async Task AToolsOwnComponents_StayOffTheTimeline()
    {
        await using var host = DiagnosticsTestHost.Create(configureServices: s => s.AddSingleton<IBelemetryDiagnosticComponentFilter, ToolFilter>());
        using var source = new ActivitySource(BelemetryDiagnosticEventFactory.ComponentsSource);

        foreach (var type in new[] { "Tool.Panel", "MyApp.Pages.Orders" })
        {
            using var handle = source.StartActivity(BelemetryDiagnosticEventFactory.HandleEventActivity)!;
            handle.SetTag(BelemetryDiagnosticEventFactory.ComponentTypeTag, type);
        }

        Assert.AreEqual("MyApp.Pages.Orders", host.Diagnostics.GetEvents().Single(e => e.Name == "handle_event").ComponentType);
    }

    private sealed class BigSnapshot : IBelemetrySnapshotProvider
    {
        public string Name => "big";

        public ValueTask<IReadOnlyDictionary<string, object?>?> CaptureAsync(BelemetrySnapshotContext context, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyDictionary<string, object?>?>(Enumerable.Range(0, 40).ToDictionary(i => "line" + i, i => (object?)new string('x', 1000)));
    }

    private sealed class BlockingSnapshot : IBelemetrySnapshotProvider
    {
        public string Name => "blocking";

        public ValueTask<IReadOnlyDictionary<string, object?>?> CaptureAsync(BelemetrySnapshotContext context, CancellationToken cancellationToken)
        {
            // What the contract forbids: blocking before the task is returned, where no race can see it.
            Thread.Sleep(300);
            return ValueTask.FromResult<IReadOnlyDictionary<string, object?>?>(new Dictionary<string, object?> { ["late"] = true });
        }
    }

    private sealed class CooperativeSnapshot : IBelemetrySnapshotProvider
    {
        public string Name => "cooperative";

        public async ValueTask<IReadOnlyDictionary<string, object?>?> CaptureAsync(BelemetrySnapshotContext context, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return null;
        }
    }

    private sealed class HangingProvider : IBelemetryComponentContextProvider
    {
        public string Name => "hanging";

        public ValueTask<IReadOnlyList<BelemetryComponentInfo>> DescribeAsync(BelemetryComponentContextRequest request, CancellationToken cancellationToken) =>
            new(new TaskCompletionSource<IReadOnlyList<BelemetryComponentInfo>>().Task);
    }

    private sealed class ToolFilter : IBelemetryDiagnosticComponentFilter
    {
        public bool IsExcluded(string componentType) => componentType.StartsWith("Tool.", StringComparison.Ordinal);
    }
}
