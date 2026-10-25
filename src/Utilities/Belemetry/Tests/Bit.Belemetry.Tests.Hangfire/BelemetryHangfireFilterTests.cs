using Bit.Belemetry.Hangfire;
using Hangfire;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.InMemory;
using Hangfire.States;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Tests.Hangfire;

[TestClass]
public class BelemetryHangfireFilterTests
{
    /// <summary>The jobs under test. Static, as Hangfire jobs are, and they report back through a completion source.</summary>
    public static class Jobs
    {
        public static TaskCompletionSource<string?> Completed { get; set; } = new();

        public static void Succeed(string marker) => Completed.TrySetResult(Activity.Current?.Id);

        [AutomaticRetry(Attempts = 0)]
        public static void Fail(string marker)
        {
            Completed.TrySetResult(Activity.Current?.Id);
            throw new InvalidOperationException("job failed: " + marker);
        }

        public static void Skipped() => Completed.TrySetResult(Activity.Current?.Id);
    }

    private static readonly ActivitySource _tests = new("Tests.Hangfire");
    private static ServiceProvider _services = null!;
    private static BelemetryInMemoryExporter _memory = null!;
    private static BackgroundJobServer _server = null!;

    [ClassInitialize]
    public static void StartServer(TestContext testContext)
    {
        _memory = new BelemetryInMemoryExporter();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBitBelemetryServices(o =>
        {
            o.ServiceName = "hangfire-tests";
            o.UseEnvironmentVariables = false;
            o.Consent.Default = BelemetryFeatures.All;
            o.Consent.Persist = false;
            o.Persistence.Enabled = false;
            o.Batch.FlushInterval = TimeSpan.FromHours(1);
            o.Metrics.ExportInterval = TimeSpan.FromHours(1);
            o.Metrics.Meters.Clear();
            o.Traces.Sources.Add(_tests.Name);
            o.AddExporter(_memory);
        });
        _services = services.BuildServiceProvider();
        _ = _services.GetRequiredService<ILoggerFactory>();

        GlobalConfiguration.Configuration
            .UseInMemoryStorage()
            .UseBitBelemetry(o => o.Filter = job => job.Method.Name != nameof(Jobs.Skipped));
        _server = new BackgroundJobServer(new BackgroundJobServerOptions { WorkerCount = 1, Queues = ["default", "critical"], SchedulePollingInterval = TimeSpan.FromMilliseconds(100) });
    }

    [ClassCleanup]
    public static async Task StopServer()
    {
        _server.Dispose();
        await _services.DisposeAsync();
    }

    private sealed record Run(List<BelemetrySpan> Spans, string ParentSpanId, string TraceId);

    private static async Task<Run> RunAsync(Action enqueue)
    {
        Jobs.Completed = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _memory.Clear();

        string parentSpanId, traceId;
        using (var parent = _tests.StartActivity("Enqueue"))
        {
            Assert.IsNotNull(parent);
            parentSpanId = parent.SpanId.ToHexString();
            traceId = parent.TraceId.ToHexString();
            enqueue();
        }

        var completed = await Task.WhenAny(Jobs.Completed.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        Assert.AreSame(Jobs.Completed.Task, completed, "The job did not run in time.");
        // The filter's OnPerformed runs after the job method returns; give the worker a moment to stop the span.
        await Task.Delay(200);

        await using var scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IBelemetry>().FlushAsync();
        return new Run(_memory.Spans.Where(s => s.Scope.Name == BelemetryHangfireFilter.SourceName).ToList(), parentSpanId, traceId);
    }

    [TestMethod]
    public async Task AnExecution_IsAConsumerSpan_ParentedToTheEnqueue()
    {
        var (spans, parentSpanId, traceId) = await RunAsync(() => BackgroundJob.Enqueue("critical", () => Jobs.Succeed("ok")));

        var span = spans.Single();
        Assert.AreEqual("JOB Jobs.Succeed", span.Name);
        Assert.AreEqual(BelemetrySpanKind.Consumer, span.Kind);
        Assert.AreEqual(traceId, span.TraceId, "The enqueue and the execution share a trace.");
        Assert.AreEqual(parentSpanId, span.ParentSpanId, "The enqueue is the execution's parent.");
        Assert.AreEqual(BelemetryStatusCode.Unset, span.Status);
        Assert.IsNotNull(span.Attributes.GetString("job.id"));
        Assert.AreEqual(typeof(Jobs).FullName, span.Attributes.GetString("job.type"));
        Assert.AreEqual("Succeed", span.Attributes.GetString("job.method"));
        Assert.AreEqual("critical", span.Attributes.GetString("job.queue"), "The queue the job was enqueued to; a job on the default queue carries none.");
        Assert.IsNotNull(span.Attributes.GetString("job.created_at"));
        Assert.IsNotNull(span.Scope.Version);

        var insideJob = await Jobs.Completed.Task;
        StringAssert.Contains(insideJob, span.SpanId, "The span is Activity.Current while the job runs, so its own telemetry nests under it.");
    }

    [TestMethod]
    public async Task AFailedExecution_IsAnErrorSpan()
    {
        var (spans, _, _) = await RunAsync(() => BackgroundJob.Enqueue(() => Jobs.Fail("boom")));

        var span = spans.Single();
        Assert.AreEqual("JOB Jobs.Fail", span.Name);
        Assert.AreEqual(BelemetryStatusCode.Error, span.Status);
        Assert.AreEqual(typeof(InvalidOperationException).FullName, span.Attributes.GetString("error.type"));
        StringAssert.Contains(span.StatusMessage, "boom");
    }

    [TestMethod]
    public async Task Filter_SkipsJobs()
    {
        var (spans, _, _) = await RunAsync(() => BackgroundJob.Enqueue(() => Jobs.Skipped()));
        Assert.IsEmpty(spans);
        Assert.IsNull(await Jobs.Completed.Task, "No span was current in the skipped job.");
    }

    [TestMethod]
    public void OnCreating_StoresTheTraceContext_UnlessPropagationIsOff()
    {
        using var connection = JobStorage.Current.GetConnection();
        var job = Job.FromExpression(() => Jobs.Succeed("x"));

        using var activity = _tests.StartActivity("Enqueue");
        Assert.IsNotNull(activity);
        activity.TraceStateString = "vendor=1";

        var propagating = new CreatingContext(new CreateContext(JobStorage.Current, connection, job, new EnqueuedState()));
        new BelemetryHangfireFilter().OnCreating(propagating);
        Assert.AreEqual(activity.Id, propagating.Parameters["traceparent"]);
        Assert.AreEqual("vendor=1", propagating.Parameters["tracestate"]);

        var silent = new CreatingContext(new CreateContext(JobStorage.Current, connection, job, new EnqueuedState()));
        new BelemetryHangfireFilter(new BelemetryHangfireOptions { PropagateTraceContext = false }).OnCreating(silent);
        Assert.IsFalse(silent.Parameters.ContainsKey("traceparent"));

        var filtered = new CreatingContext(new CreateContext(JobStorage.Current, connection, job, new EnqueuedState()));
        new BelemetryHangfireFilter(new BelemetryHangfireOptions { Filter = _ => false }).OnCreating(filtered);
        Assert.IsFalse(filtered.Parameters.ContainsKey("traceparent"));
    }
}
