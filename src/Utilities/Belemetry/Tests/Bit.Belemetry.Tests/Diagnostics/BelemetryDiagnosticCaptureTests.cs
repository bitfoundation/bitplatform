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
        var capture = await host.Diagnostics.CaptureAsync(new() { Reason = "user-report" }, TestContext.CancellationToken);

        Assert.IsTrue(capture.Succeeded);
        Assert.AreEqual(16, capture.Id.Length);
        Assert.AreEqual(BelemetryCaptureTriggerKind.Manual, capture.Trigger.Kind);
        await Assert.ThrowsAsync<InvalidOperationException>(() => capture.ExportAsync(TestContext.CancellationToken));

        capture.Confirm();
        Assert.IsTrue(capture.IsConfirmed);
        capture.ExcludeSection(BelemetryBundleSections.Release);
        Assert.IsFalse(capture.IsConfirmed, "What is confirmed is what is exported.");
        await Assert.ThrowsAsync<InvalidOperationException>(() => capture.ExportAsync(TestContext.CancellationToken));

        capture.Confirm();
        var bytes = await capture.ExportAsync(TestContext.CancellationToken);
        Assert.IsGreaterThan(0, bytes.Length);
    }

    [TestMethod]
    public async Task TheBundle_RoundTrips_AndValidates()
    {
        await using var host = await WithAFailureAsync();
        var capture = await host.Diagnostics.CaptureAsync(new() { Reason = "user-report", Description = "I clicked Save and it broke. Reach me at jane@example.com" }, TestContext.CancellationToken);
        capture.Confirm();

        using var stream = new MemoryStream();
        await capture.ExportAsync(stream, TestContext.CancellationToken);
        stream.Position = 0;
        var read = await BelemetrySupportBundle.ReadAsync(stream, TestContext.CancellationToken);

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
        Assert.Contains(f => f.Kind == BelemetryIssueFindingKind.UserProvided, report.Findings);
        Assert.Contains(f => f.Kind == BelemetryIssueFindingKind.Observed && f.Text.StartsWith("System.InvalidOperationException was logged", StringComparison.Ordinal), report.Findings);
        Assert.HasCount(2, read.Events);
        Assert.AreEqual(1, read.ExceptionGroups.Single().Count);
        Assert.Contains("# InvalidOperationException", read.ReportMarkdown!);
    }

    [TestMethod]
    public async Task TheExport_IsDeterministic()
    {
        await using var host = await WithAFailureAsync();
        var capture = await host.Diagnostics.CaptureAsync(cancellationToken: TestContext.CancellationToken);
        capture.Confirm();
        Assert.AreSequenceEqual(await capture.ExportAsync(TestContext.CancellationToken), await capture.ExportAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task ThePreview_NarrowsTheExport_AndTheManifestSaysHow()
    {
        await using var host = await WithAFailureAsync(o => o.Diagnostics.AllowedAttributes.Add("tenant"));
        host.Diagnostics.Record("tenant.switched", new Dictionary<string, object?> { ["tenant"] = "contoso" });
        var capture = await host.Diagnostics.CaptureAsync(cancellationToken: TestContext.CancellationToken);

        var warning = capture.Events.Single(e => e.Message == "Inventory is slow");
        Assert.IsTrue(capture.ExcludeEvent(warning));
        capture.RedactAttribute("tenant");
        capture.ExcludeSection(BelemetryBundleSections.Release);
        capture.SetDescription("It broke");
        Assert.DoesNotContain(warning, capture.Events);
        Assert.IsNull(capture.Release);
        Assert.AreEqual("excluded", capture.Sections.Single(s => s.Section == BelemetryBundleSections.Release).Status);

        capture.Confirm();
        var read = await BelemetrySupportBundle.ReadAsync(new MemoryStream(await capture.ExportAsync(TestContext.CancellationToken)), TestContext.CancellationToken);
        Assert.IsTrue(read.IsValid, string.Join("; ", read.Errors));
        Assert.IsFalse(read.Files.ContainsKey("release.json"));
        Assert.DoesNotContain(e => e.Message == "Inventory is slow", read.Events);
        Assert.AreEqual("REDACTED", read.Events.Single(e => e.Name == "tenant.switched").Attributes["tenant"]);

        using var zip = new ZipArchive(new MemoryStream(await capture.ExportAsync(TestContext.CancellationToken)));
        using var manifest = JsonDocument.Parse(zip.GetEntry("manifest.json")!.Open());
        var changes = manifest.RootElement.GetProperty("privacy").GetProperty("userChanges").EnumerateArray().Select(c => c.GetString()).ToList();
        Assert.AreSequenceEqual(new[] { $"excluded event {BelemetryDiagnosticJson.EventId(warning)}", "redacted attribute tenant", "excluded section Release", "set description" }, changes);
        Assert.Contains(m => m.GetString()!.Contains("narrowed the bundle"), manifest.RootElement.GetProperty("missingEvidence").EnumerateArray());
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

        var withoutSnapshots = await host.Diagnostics.CaptureAsync(cancellationToken: TestContext.CancellationToken);
        Assert.IsEmpty(withoutSnapshots.Snapshots, "Application state is opt-in per capture.");

        var capture = await host.Diagnostics.CaptureAsync(new() { IncludeSnapshots = true }, TestContext.CancellationToken);
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
        using var zip = new ZipArchive(new MemoryStream(await capture.ExportAsync(TestContext.CancellationToken)));
        var snapshots = new StreamReader(zip.GetEntry("snapshots.json")!.Open()).ReadToEnd();
        Assert.DoesNotContain("\"items\"", snapshots);
        Assert.DoesNotContain("hunter", snapshots);
    }

    [TestMethod]
    public async Task RecordedSnapshots_GiveTheStateBeforeAnOperation()
    {
        await using var host = await WithAFailureAsync(services: s => s.AddBitBelemetrySnapshotProvider<CartSnapshot>());
        await host.Diagnostics.RecordSnapshotAsync("before-checkout", TestContext.CancellationToken);

        var capture = await host.Diagnostics.CaptureAsync(new() { IncludeSnapshots = true }, TestContext.CancellationToken);
        Assert.AreSequenceEqual(new string?[] { "before-checkout", null }, capture.Snapshots.Select(s => s.Label).ToArray());
        Assert.Contains(e => e.Name == "snapshot.before-checkout", capture.Events);
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

        var capture = await host.Diagnostics.CaptureAsync(cancellationToken: TestContext.CancellationToken);
        var orders = capture.Components.Single(c => c.Type == "MyApp.Pages.Orders");
        Assert.AreSequenceEqual(new[] { "MyApp.App", "MyApp.Layout.MainLayout" }, orders.Ancestors!.ToArray());
        Assert.AreEqual(7, orders.RenderCount);
        Assert.AreEqual("tree", orders.Source);
        Assert.StartsWith("c", orders.InstanceKey, "A capture-local key, never the renderer's id.");
        Assert.Contains(m => m.Contains("'broken' failed"), capture.MissingEvidence);
    }

    [TestMethod]
    public async Task WithoutAProvider_TheHierarchyIsReportedMissing_NotInvented()
    {
        await using var host = await WithAFailureAsync();
        var capture = await host.Diagnostics.CaptureAsync(cancellationToken: TestContext.CancellationToken);
        Assert.AreEqual("component hierarchy unavailable (no IBelemetryComponentContextProvider)", capture.Sections.Single(s => s.Section == BelemetryBundleSections.Components).Note);
        Assert.Contains(m => m.Contains("Browser evidence"), capture.MissingEvidence, "No browser in a test host: said, not hidden.");
    }

    [TestMethod]
    public async Task AnExceptionInHand_BecomesTheSubject()
    {
        await using var host = await WithAFailureAsync();
        var boundary = DiagnosticsTestHost.Thrown(() => new ArgumentException("bad quantity"));

        var capture = await host.Diagnostics.CaptureAsync(new() { Exception = boundary, Reason = "error-boundary" }, TestContext.CancellationToken);

        Assert.AreEqual("System.ArgumentException", capture.Subject!.Exception!.Type);
        Assert.AreEqual("tracked", capture.Subject.Exception.Origin);
        Assert.AreEqual("System.ArgumentException", capture.Report.ExceptionType);
        Assert.HasCount(2, capture.ExceptionGroups);
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

        var capture = await host.Diagnostics.CaptureAsync(cancellationToken: TestContext.CancellationToken);
        Assert.IsLessThan(200, capture.Events.Count);
        Assert.IsNotNull(capture.Subject, "The failure is kept.");
        Assert.Contains(m => m.Contains("bundle size limit"), capture.MissingEvidence);
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
        await Task.Delay(50, TestContext.CancellationToken);
        Assert.IsEmpty(host.Diagnostics.PendingCaptures, "Default triggers: a logged, handled error is not one.");

        var thrown = DiagnosticsTestHost.Thrown(() => new NullReferenceException("render failed"));
        renderer.LogCritical(thrown, "Unhandled exception rendering component: {Message}", thrown.Message);
        renderer.LogCritical(thrown, "Unhandled exception rendering component: {Message}", thrown.Message);
        await WaitForAsync(() => host.Diagnostics.PendingCaptures.Count == 1);
        await Task.Delay(50, TestContext.CancellationToken);

        var capture = host.Diagnostics.PendingCaptures.Single();
        Assert.AreEqual(BelemetryCaptureTriggerKind.Automatic, capture.Trigger.Kind);
        Assert.AreEqual("unhandled-exception", capture.Trigger.Reason);
        Assert.AreEqual("System.NullReferenceException", capture.Subject!.Exception!.Type);
        Assert.ContainsSingle(available);
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

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
