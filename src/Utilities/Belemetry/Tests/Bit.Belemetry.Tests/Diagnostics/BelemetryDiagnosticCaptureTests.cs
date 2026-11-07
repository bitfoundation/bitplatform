using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Tests.Diagnostics;

/// <summary>Captures: preview, narrowing, confirmation, export, snapshots, components, automatic triggers.</summary>
[TestClass]
public class BelemetryDiagnosticCaptureTests
{
    private static async Task<DiagnosticsTestHost> WithAFailureAsync(Action<BelemetryOptions>? configure = null, Action<IServiceCollection>? services = null)
    {
        var host = DiagnosticsTestHost.Create(o =>
        {
            o.Environment = "Production";
            o.Diagnostics.Release.Commit = "abc1234";
            o.Diagnostics.Release.Metadata["ring"] = "canary";
            configure?.Invoke(o);
        }, configureServices: services);
        var logger = host.Factory.CreateLogger("MyApp.Orders");
        logger.LogWarning("Inventory is slow");
        logger.LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("Order 42 failed")), "Saving failed");
        await Task.Yield();
        return host;
    }

    [TestMethod]
    public async Task ACapture_IsOnlyExported_AfterConfirm_AndAChangeWithdrawsTheConfirmation()
    {
        await using var host = await WithAFailureAsync();
        var capture = await host.Diagnostics.CaptureAsync(new() { Reason = "user-report" });

        Assert.IsTrue(capture.Succeeded);
        Assert.AreEqual(16, capture.Id.Length);
        Assert.AreEqual(BelemetryCaptureTriggerKind.Manual, capture.Trigger.Kind);
        await Assert.ThrowsAsync<InvalidOperationException>(() => capture.ExportAsync());

        capture.Confirm();
        Assert.IsTrue(capture.IsConfirmed);
        capture.ExcludeSection(BelemetryBundleSections.Release);
        Assert.IsFalse(capture.IsConfirmed, "What is confirmed is what is exported.");
        await Assert.ThrowsAsync<InvalidOperationException>(() => capture.ExportAsync());

        capture.Confirm();
        var bytes = await capture.ExportAsync();
        Assert.IsGreaterThan(0, bytes.Length);
    }

    [TestMethod]
    public async Task TheBundle_RoundTrips_AndValidates()
    {
        await using var host = await WithAFailureAsync();
        var capture = await host.Diagnostics.CaptureAsync(new() { Reason = "user-report", Description = "I clicked Save and it broke. Reach me at jane@example.com" });
        capture.Confirm();

        using var stream = new MemoryStream();
        await capture.ExportAsync(stream);
        stream.Position = 0;
        var read = await BelemetrySupportBundle.ReadAsync(stream);

        Assert.IsTrue(read.IsValid, string.Join("; ", read.Errors));
        Assert.AreEqual(BelemetrySupportBundle.SchemaVersion, read.SchemaVersion);
        Assert.AreEqual(capture.Id, read.CaptureId);
        Assert.IsTrue(read.Generator!.StartsWith("Bit.Belemetry", StringComparison.Ordinal));
        CollectionAssert.IsSubsetOf(new[] { "manifest.json", "report.json", "report.md", "release.json", "exceptions.json", "timeline.json", "components.json" }, read.Files.Keys.ToArray());

        var report = read.Report!;
        Assert.AreEqual("System.InvalidOperationException", report.ExceptionType);
        Assert.AreEqual("Order 42 failed", report.ExceptionMessage);
        Assert.AreEqual("1.2.3", report.Release);
        Assert.AreEqual("Production", report.Environment);
        Assert.AreEqual("abc1234", report.Commit);
        Assert.AreEqual("I clicked Save and it broke. Reach me at [email]", report.UserDescription, "User text goes through the privacy patterns too.");
        Assert.IsTrue(report.Findings.Any(f => f.Kind == BelemetryIssueFindingKind.UserProvided));
        Assert.IsTrue(report.Findings.Any(f => f.Kind == BelemetryIssueFindingKind.Observed && f.Text.StartsWith("System.InvalidOperationException was logged", StringComparison.Ordinal)));
        Assert.HasCount(2, read.Events);
        Assert.AreEqual(1, read.ExceptionGroups.Single().Count);
        Assert.Contains("# InvalidOperationException", read.ReportMarkdown!);
    }

    [TestMethod]
    public async Task TheExport_IsDeterministic()
    {
        await using var host = await WithAFailureAsync();
        var capture = await host.Diagnostics.CaptureAsync();
        capture.Confirm();
        CollectionAssert.AreEqual(await capture.ExportAsync(), await capture.ExportAsync());
    }

    [TestMethod]
    public async Task ThePreview_NarrowsTheExport_AndTheManifestSaysHow()
    {
        await using var host = await WithAFailureAsync(o => o.Diagnostics.AllowedAttributes.Add("tenant"));
        host.Diagnostics.Record("tenant.switched", new Dictionary<string, object?> { ["tenant"] = "contoso" });
        var capture = await host.Diagnostics.CaptureAsync();

        var warning = capture.Events.Single(e => e.Message == "Inventory is slow");
        Assert.IsTrue(capture.ExcludeEvent(warning));
        capture.RedactAttribute("tenant");
        capture.ExcludeSection(BelemetryBundleSections.Release);
        capture.SetDescription("It broke");
        Assert.IsFalse(capture.Events.Contains(warning));
        Assert.IsNull(capture.Release);
        Assert.AreEqual("excluded", capture.Sections.Single(s => s.Section == BelemetryBundleSections.Release).Status);

        capture.Confirm();
        var read = await BelemetrySupportBundle.ReadAsync(new MemoryStream(await capture.ExportAsync()));
        Assert.IsTrue(read.IsValid, string.Join("; ", read.Errors));
        Assert.IsFalse(read.Files.ContainsKey("release.json"));
        Assert.IsFalse(read.Events.Any(e => e.Message == "Inventory is slow"));
        Assert.AreEqual("REDACTED", read.Events.Single(e => e.Name == "tenant.switched").Attributes["tenant"]);

        using var zip = new ZipArchive(new MemoryStream(await capture.ExportAsync()));
        using var manifest = JsonDocument.Parse(zip.GetEntry("manifest.json")!.Open());
        var changes = manifest.RootElement.GetProperty("privacy").GetProperty("userChanges").EnumerateArray().Select(c => c.GetString()).ToList();
        CollectionAssert.AreEqual(new[] { $"excluded event {BelemetryDiagnosticJson.EventId(warning)}", "redacted attribute tenant", "excluded section Release", "set description" }, changes);
        Assert.IsTrue(manifest.RootElement.GetProperty("missingEvidence").EnumerateArray().Any(m => m.GetString()!.Contains("narrowed the bundle")));
    }

    [TestMethod]
    public async Task ARevokeAfterACapture_LeavesThePendingOnesBehind()
    {
        await using var host = await WithAFailureAsync(o => o.Diagnostics.AutoCaptureTriggers = BelemetryDiagnosticTriggers.AllExceptions);
        await WaitForAsync(() => host.Diagnostics.PendingCaptures.Count == 1);

        host.Host.Consent.Decide(BelemetryFeatures.All & ~BelemetryFeatures.Errors);
        Assert.IsEmpty(host.Diagnostics.PendingCaptures, "Made under the old grant: it holds what the user took back.");
    }

    [TestMethod]
    public async Task SnapshotProviders_AreIsolated_AndTheirDataSanitized()
    {
        await using var host = await WithAFailureAsync(services: s =>
        {
            s.AddBitBelemetrySnapshotProvider<CartSnapshot>();
            s.AddBitBelemetrySnapshotProvider<ThrowingSnapshot>();
            s.AddBitBelemetrySnapshotProvider<SlowSnapshot>();
        });
        host.Host.Settings.Options.Diagnostics.Snapshots.Timeout = TimeSpan.FromMilliseconds(200);

        var withoutSnapshots = await host.Diagnostics.CaptureAsync();
        Assert.IsEmpty(withoutSnapshots.Snapshots, "Application state is opt-in per capture.");

        var capture = await host.Diagnostics.CaptureAsync(new() { IncludeSnapshots = true });
        var cart = capture.Snapshots.Single(s => s.Name == "cart");
        Assert.AreEqual(BelemetrySnapshotStatus.Captured, cart.Status);
        Assert.AreEqual(3, cart.Data!["items"]);
        Assert.AreEqual("REDACTED", cart.Data["couponSecret"]);
        Assert.AreEqual("[email]", cart.Data["owner"]);
        Assert.AreEqual(BelemetrySnapshotStatus.Failed, capture.Snapshots.Single(s => s.Name == "throws").Status);
        Assert.AreEqual("System.InvalidOperationException", capture.Snapshots.Single(s => s.Name == "throws").Error);
        Assert.AreEqual(BelemetrySnapshotStatus.TimedOut, capture.Snapshots.Single(s => s.Name == "slow").Status);
        Assert.IsTrue(capture.Report.Prerequisites.Single().StartsWith("Snapshot cart", StringComparison.Ordinal));

        capture.ExcludeSnapshot("cart");
        Assert.AreEqual(BelemetrySnapshotStatus.Excluded, capture.Snapshots.Single(s => s.Name == "cart").Status);
        Assert.IsNull(capture.Snapshots.Single(s => s.Name == "cart").Data);
        capture.Confirm();
        using var zip = new ZipArchive(new MemoryStream(await capture.ExportAsync()));
        var snapshots = new StreamReader(zip.GetEntry("snapshots.json")!.Open()).ReadToEnd();
        Assert.DoesNotContain("\"items\"", snapshots);
        Assert.DoesNotContain("hunter", snapshots);
    }

    [TestMethod]
    public async Task RecordedSnapshots_GiveTheStateBeforeAnOperation()
    {
        await using var host = await WithAFailureAsync(services: s => s.AddBitBelemetrySnapshotProvider<CartSnapshot>());
        await host.Diagnostics.RecordSnapshotAsync("before-checkout");

        var capture = await host.Diagnostics.CaptureAsync(new() { IncludeSnapshots = true });
        CollectionAssert.AreEqual(new string?[] { "before-checkout", null }, capture.Snapshots.Select(s => s.Label).ToArray());
        Assert.IsTrue(capture.Events.Any(e => e.Name == "snapshot.before-checkout"));
    }

    [TestMethod]
    public async Task AComponentContextProvider_AddsTheTree_AndAFailingOneIsReported()
    {
        await using var host = await WithAFailureAsync(services: s =>
        {
            s.AddSingleton<IBelemetryComponentContextProvider, TreeProvider>();
            s.AddSingleton<IBelemetryComponentContextProvider, BrokenProvider>();
        });
        host.Diagnostics.Record("noted", category: BelemetryDiagnosticCategories.Custom);
        var source = new ActivitySource(BelemetryDiagnosticEventFactory.ComponentsSource);
        using (var handle = source.StartActivity(BelemetryDiagnosticEventFactory.HandleEventActivity)!) handle.SetTag(BelemetryDiagnosticEventFactory.ComponentTypeTag, "MyApp.Pages.Orders");

        var capture = await host.Diagnostics.CaptureAsync();
        var orders = capture.Components.Single(c => c.Type == "MyApp.Pages.Orders");
        CollectionAssert.AreEqual(new[] { "MyApp.App", "MyApp.Layout.MainLayout" }, orders.Ancestors!.ToArray());
        Assert.AreEqual(7, orders.RenderCount);
        Assert.AreEqual("tree", orders.Source);
        StringAssert.StartsWith(orders.InstanceKey, "c", "A capture-local key, never the renderer's id.");
        Assert.IsTrue(capture.MissingEvidence.Any(m => m.Contains("'broken' failed")));
    }

    [TestMethod]
    public async Task WithoutAProvider_TheHierarchyIsReportedMissing_NotInvented()
    {
        await using var host = await WithAFailureAsync();
        var capture = await host.Diagnostics.CaptureAsync();
        Assert.AreEqual("component hierarchy unavailable (no IBelemetryComponentContextProvider)", capture.Sections.Single(s => s.Section == BelemetryBundleSections.Components).Note);
        Assert.IsTrue(capture.MissingEvidence.Any(m => m.Contains("Browser evidence")), "No browser in a test host: said, not hidden.");
    }

    [TestMethod]
    public async Task AnExceptionInHand_BecomesTheSubject()
    {
        await using var host = await WithAFailureAsync();
        var boundary = DiagnosticsTestHost.Thrown(() => new ArgumentException("bad quantity"));

        var capture = await host.Diagnostics.CaptureAsync(new() { Exception = boundary, Reason = "error-boundary" });

        Assert.AreEqual("System.ArgumentException", capture.Subject!.Exception!.Type);
        Assert.AreEqual("tracked", capture.Subject.Exception.Origin);
        Assert.AreEqual("System.ArgumentException", capture.Report.ExceptionType);
        Assert.AreEqual(2, capture.ExceptionGroups.Count);
    }

    [TestMethod]
    public async Task TheBundleSizeLimit_DropsOldActivityFirst_AndSaysSo()
    {
        await using var host = DiagnosticsTestHost.Create(o =>
        {
            o.Diagnostics.MaxBundleBytes = 64 * 1024;
            o.Diagnostics.IncludeLogMessages = true;
            o.Diagnostics.MaxEvents = 1000;
            o.Diagnostics.MaxMemoryBytes = 64 * 1024 * 1024;
        });
        var logger = host.Factory.CreateLogger("App");
        for (var i = 0; i < 200; i++) logger.LogWarning("{Big}", new string('x', 1000));
        logger.LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("kept")), "failure");

        var capture = await host.Diagnostics.CaptureAsync();
        Assert.IsLessThan(200, capture.Events.Count);
        Assert.IsNotNull(capture.Subject, "The failure is kept.");
        Assert.IsTrue(capture.MissingEvidence.Any(m => m.Contains("bundle size limit")));
        Assert.AreEqual("truncated", capture.Sections.Single(s => s.Section == BelemetryBundleSections.Timeline).Status);
    }

    [TestMethod]
    public async Task AnUnhandledException_TriggersOneAutomaticCapture_PerCooldown()
    {
        await using var host = DiagnosticsTestHost.Create(o => o.Diagnostics.AutoCaptureTriggers = BelemetryDiagnosticTriggers.Default);
        var available = new List<BelemetryDiagnosticCapture>();
        host.Diagnostics.CaptureAvailable += available.Add;
        var renderer = host.Factory.CreateLogger("Microsoft.AspNetCore.Components.WebAssembly.Rendering.WebAssemblyRenderer");
        var app = host.Factory.CreateLogger("App");

        app.LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("handled by the app")), "logged");
        await Task.Delay(50);
        Assert.IsEmpty(host.Diagnostics.PendingCaptures, "Default triggers: a logged, handled error is not one.");

        var thrown = DiagnosticsTestHost.Thrown(() => new NullReferenceException("render failed"));
        renderer.LogCritical(thrown, "Unhandled exception rendering component: {Message}", thrown.Message);
        renderer.LogCritical(thrown, "Unhandled exception rendering component: {Message}", thrown.Message);
        await WaitForAsync(() => host.Diagnostics.PendingCaptures.Count == 1);
        await Task.Delay(50);

        var capture = host.Diagnostics.PendingCaptures.Single();
        Assert.AreEqual(BelemetryCaptureTriggerKind.Automatic, capture.Trigger.Kind);
        Assert.AreEqual("unhandled-exception", capture.Trigger.Reason);
        Assert.AreEqual("System.NullReferenceException", capture.Subject!.Exception!.Type);
        Assert.HasCount(1, available);
        Assert.IsFalse(capture.IsConfirmed, "An automatic capture waits for review; it is never exported on its own.");

        Assert.IsTrue(host.Diagnostics.Dismiss(capture));
        Assert.IsEmpty(host.Diagnostics.PendingCaptures);
    }

    [TestMethod]
    public async Task ACriticalClassification_Triggers()
    {
        await using var host = DiagnosticsTestHost.Create(o => o.Diagnostics.AutoCaptureTriggers = BelemetryDiagnosticTriggers.Critical,
            configureServices: s => s.AddSingleton<IBelemetryExceptionClassifier, PaymentsAreCritical>());
        host.Factory.CreateLogger("App").LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("payment declined")), "payment");
        await WaitForAsync(() => host.Diagnostics.PendingCaptures.Count == 1);
        Assert.AreEqual("critical-exception", host.Diagnostics.PendingCaptures.Single().Trigger.Reason);
    }

    internal static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 5000)
    {
        var watch = Stopwatch.StartNew();
        while (condition() is false)
        {
            if (watch.ElapsedMilliseconds > timeoutMs) Assert.Fail("Timed out waiting for the condition.");
            await Task.Delay(10);
        }
    }

    private sealed class CartSnapshot : IBelemetrySnapshotProvider
    {
        public string Name => "cart";

        public ValueTask<IReadOnlyDictionary<string, object?>?> CaptureAsync(BelemetrySnapshotContext context, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyDictionary<string, object?>?>(new Dictionary<string, object?> { ["items"] = 3, ["couponSecret"] = "hunter2", ["owner"] = "jane@example.com" });
    }

    private sealed class ThrowingSnapshot : IBelemetrySnapshotProvider
    {
        public string Name => "throws";

        public ValueTask<IReadOnlyDictionary<string, object?>?> CaptureAsync(BelemetrySnapshotContext context, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("state of jane@example.com");
    }

    private sealed class SlowSnapshot : IBelemetrySnapshotProvider
    {
        public string Name => "slow";

        public async ValueTask<IReadOnlyDictionary<string, object?>?> CaptureAsync(BelemetrySnapshotContext context, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            return null;
        }
    }

    private sealed class TreeProvider : IBelemetryComponentContextProvider
    {
        public string Name => "tree";

        public ValueTask<IReadOnlyList<BelemetryComponentInfo>> DescribeAsync(BelemetryComponentContextRequest request, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<BelemetryComponentInfo>>(request.ComponentTypes.Select(t => new BelemetryComponentInfo { Type = t, Ancestors = ["MyApp.App", "MyApp.Layout.MainLayout"], RenderCount = 7 }).ToList());
    }

    private sealed class BrokenProvider : IBelemetryComponentContextProvider
    {
        public string Name => "broken";

        public ValueTask<IReadOnlyList<BelemetryComponentInfo>> DescribeAsync(BelemetryComponentContextRequest request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("broken");
    }

    private sealed class PaymentsAreCritical : IBelemetryExceptionClassifier
    {
        public BelemetryExceptionClassification? Classify(BelemetryDiagnosticException exception) =>
            exception.Message?.Contains("payment", StringComparison.Ordinal) is true ? new BelemetryExceptionClassification { Category = "payments", Critical = true } : null;
    }
}
