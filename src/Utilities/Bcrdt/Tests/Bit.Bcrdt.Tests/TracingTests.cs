using System.Collections.Concurrent;
using System.Diagnostics;
using Bit.Bcrdt.Server;

namespace Bit.Bcrdt.Tests;

/// <summary>
/// The spans on <see cref="BcrdtDiagnostics.ActivitySourceName"/>: what each covers, what it is tagged with, that
/// long-lived ones end on every path, that none carries an identifier, and that nothing is created unasked.
/// </summary>
/// <remarks>
/// Other test classes run alongside these in the same process and trace too while a listener is attached, so
/// every assertion is scoped to the spans its own test caused: those under the test's own parent span, or the
/// ones its store saw as current.
/// </remarks>
[TestClass]
public class TracingTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(15);

    private const string TestSourceName = "Bit.Bcrdt.Tests.Tracing";

    private static readonly ActivitySource TestSource = new(TestSourceName);

    // Distinctive enough that no size or count can collide with them.
    private const string DocumentId = "doc-7f3a9c-confidential";
    private const string Room = "room-7f3a9c-confidential";
    private const ulong FirstClient = 917_364_528_103;
    private const ulong SecondClient = 917_364_528_207;
    private const string UserId = "user-7f3a9c-ada";
    private const string ConnectionId = "connection-7f3a9c";

    private static readonly string[] Identifiers =
        [DocumentId, Room, FirstClient.ToString(), SecondClient.ToString(), UserId, ConnectionId, "7f3a9c"];

    private static BcrdtSyncProviderOptions Options(TimeSpan handshakeTimeout) => new()
    {
        HeartbeatInterval = TimeSpan.Zero,
        HandshakeTimeout = handshakeTimeout,
        Reconnect = new BcrdtReconnectPolicy { BaseDelay = TimeSpan.FromMilliseconds(5), MaxDelay = TimeSpan.FromMilliseconds(20) },
    };

    /// <summary>Built on the pool, so the provider captures no synchronization context.</summary>
    private static Task<BcrdtSyncProvider> ProviderAsync(BcrdtDocument doc, IBcrdtTransport transport, TimeSpan handshakeTimeout) =>
        Task.Run(() => new BcrdtSyncProvider(doc, transport, options: Options(handshakeTimeout)));

    [TestMethod]
    public async Task A_provider_handshake_is_one_span_from_announcement_to_synced()
    {
        using var spans = new SpanRecorder();
        using Activity parent = TestSource.StartActivity("handshake-test")!;

        var network = new BcrdtInMemoryNetwork();
        using var firstDoc = new BcrdtDocument(new BcrdtDocumentOptions { Id = DocumentId, ClientId = FirstClient });
        using var secondDoc = new BcrdtDocument(new BcrdtDocumentOptions { Id = DocumentId, ClientId = SecondClient });
        firstDoc.GetText("body").Insert(0, "hello");

        await using BcrdtSyncProvider first = await ProviderAsync(firstDoc, network.CreateTransport(Room), TimeSpan.FromMilliseconds(100));
        await using BcrdtSyncProvider second = await ProviderAsync(secondDoc, network.CreateTransport(Room), TimeSpan.FromMinutes(5));

        // Alone in the room, the first concludes that nobody is there; the second is answered by the first.
        await first.ConnectAsync(TestContext.CancellationToken);
        Activity alone = await spans.WaitForAsync(parent, BcrdtDiagnostics.HandshakeSpan);
        await second.ConnectAsync(TestContext.CancellationToken);
        await EventuallyAsync(() => second.IsSynced, "the second provider should sync");

        Activity answered = await spans.WaitForAsync(parent, BcrdtDiagnostics.HandshakeSpan, span => !ReferenceEquals(span, alone));
        Assert.AreEqual("timeout", alone.GetTagItem("bcrdt.outcome"));
        Assert.AreEqual("synced", answered.GetTagItem("bcrdt.outcome"));
        Assert.AreEqual(0, answered.GetTagItem("bcrdt.handshake.retries"));
        Assert.AreEqual(ActivityStatusCode.Unset, answered.Status);
        Assert.AreEqual(parent.SpanId, answered.ParentSpanId);

        // What the first sent in answer was applied as part of the second's handshake.
        Activity applied = await spans.WaitForAsync(parent, BcrdtDiagnostics.UpdateApplySpan, span => span.ParentSpanId == answered.SpanId);
        Assert.AreEqual("sync_step2", applied.GetTagItem("bcrdt.message.kind"));
        Assert.IsGreaterThan(0, (int)applied.GetTagItem("bcrdt.message.size")!);

        // Long-lived spans are never left as the ambient one.
        Assert.AreSame(parent, Activity.Current);
        AssertCarriesNoIdentifier(spans.InTrace(parent));
    }

    [TestMethod]
    public async Task A_remote_change_is_applied_under_a_span_with_its_kind_and_size()
    {
        using var spans = new SpanRecorder();
        var network = new BcrdtInMemoryNetwork();
        using var firstDoc = new BcrdtDocument(new BcrdtDocumentOptions { Id = DocumentId, ClientId = FirstClient });
        using var secondDoc = new BcrdtDocument(new BcrdtDocumentOptions { Id = DocumentId, ClientId = SecondClient });
        await using BcrdtSyncProvider first = await ProviderAsync(firstDoc, network.CreateTransport(Room), TimeSpan.FromMilliseconds(50));
        await using BcrdtSyncProvider second = await ProviderAsync(secondDoc, network.CreateTransport(Room), TimeSpan.FromMilliseconds(50));
        await first.ConnectAsync(TestContext.CancellationToken);
        await second.ConnectAsync(TestContext.CancellationToken);
        await EventuallyAsync(() => first.IsSynced && second.IsSynced, "both should sync");

        // The in-memory network delivers on the sender's flow, so the change is applied under the test's span.
        using (Activity parent = TestSource.StartActivity("apply-test")!)
        {
            firstDoc.GetText("body").Insert(0, "a remote change");
            await EventuallyAsync(() => secondDoc.GetText("body").ToString() == "a remote change", "the change should arrive");

            Activity applied = await spans.WaitForAsync(parent, BcrdtDiagnostics.UpdateApplySpan);
            Assert.AreEqual("update", applied.GetTagItem("bcrdt.message.kind"));
            Assert.IsGreaterThan("a remote change".Length, (int)applied.GetTagItem("bcrdt.message.size")!);
            Assert.AreEqual(ActivityStatusCode.Unset, applied.Status);
            Assert.AreEqual(parent.SpanId, applied.ParentSpanId);
            Assert.AreSame(parent, Activity.Current);
            AssertCarriesNoIdentifier(spans.InTrace(parent));
        }
    }

    [TestMethod]
    public async Task A_handshake_span_ends_when_the_connection_drops_and_its_reconnection_is_traced()
    {
        using var spans = new SpanRecorder();
        using Activity parent = TestSource.StartActivity("drop-test")!;

        var network = new BcrdtInMemoryNetwork();
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { Id = DocumentId, ClientId = FirstClient });
        BcrdtInMemoryTransport transport = network.CreateTransport(Room);
        Activity reconnected;
        Activity resumed;
        await using (BcrdtSyncProvider provider = await ProviderAsync(doc, transport, TimeSpan.FromMinutes(5)))
        {
            // Nobody answers and the timeout is long: the handshake is still under way when the connection drops.
            await provider.ConnectAsync(TestContext.CancellationToken);
            transport.SimulateDrop(new IOException($"Lost '{DocumentId}'."));

            Activity dropped = await spans.WaitForAsync(parent, BcrdtDiagnostics.HandshakeSpan);
            Assert.AreEqual("disconnected", dropped.GetTagItem("bcrdt.outcome"));
            Assert.AreEqual(ActivityStatusCode.Error, dropped.Status);
            Assert.AreEqual(nameof(IOException), dropped.GetTagItem("error.type"));
            Assert.AreEqual(nameof(IOException), dropped.StatusDescription, "the message names the document; the type does not");

            reconnected = await spans.WaitForAsync(parent, BcrdtDiagnostics.ReconnectSpan);
            Assert.AreEqual(1, reconnected.GetTagItem("bcrdt.reconnect.attempt"));
            Assert.AreEqual("connected", reconnected.GetTagItem("bcrdt.outcome"));
            await EventuallyAsync(() => provider.Status == BcrdtConnectionStatus.Handshaking, "the provider should be handshaking again");
            Assert.DoesNotContain(span => span.OperationName == BcrdtDiagnostics.HandshakeSpan && !ReferenceEquals(span, dropped), spans.InTrace(parent),
                "the second handshake is still under way");
        }

        // Disposal ends the handshake that was still waiting for an answer.
        resumed = await spans.WaitForAsync(parent, BcrdtDiagnostics.HandshakeSpan, span => (string?)span.GetTagItem("bcrdt.outcome") == "disposed");
        Assert.AreEqual(reconnected.SpanId, resumed.ParentSpanId, "the handshake a reconnection opens belongs to it");
        Assert.AreEqual(ActivityStatusCode.Unset, resumed.Status);
        Assert.AreSame(parent, Activity.Current);
        AssertCarriesNoIdentifier(spans.InTrace(parent));
    }

    [TestMethod]
    public void A_long_lived_span_never_becomes_or_disturbs_the_ambient_one()
    {
        using var spans = new SpanRecorder();
        using Activity parent = TestSource.StartActivity("ambient-test")!;

        // Started and stopped from synchronous code, where a change to the ambient span would stick.
        Activity? handshake = BcrdtDiagnostics.StartDetached(BcrdtDiagnostics.HandshakeSpan);
        Assert.IsNotNull(handshake);
        Assert.AreSame(parent, Activity.Current);
        Assert.AreEqual(parent.SpanId, handshake.ParentSpanId);

        using (Activity unrelated = TestSource.StartActivity("unrelated")!)
        {
            BcrdtDiagnostics.EndHandshake(ref handshake, "synced", retries: 0);
            Assert.IsNull(handshake);
            Assert.AreSame(unrelated, Activity.Current, "ending a span elsewhere must not make its parent current there");

            // Ending it again, from another path, is harmless.
            BcrdtDiagnostics.EndHandshake(ref handshake, "disposed", retries: 0);
        }

        Assert.AreSame(parent, Activity.Current);
        Assert.ContainsSingle(span => span.OperationName == BcrdtDiagnostics.HandshakeSpan, spans.InTrace(parent));
    }

    [TestMethod]
    public async Task A_handshake_span_ends_on_a_deliberate_disconnection()
    {
        using var spans = new SpanRecorder();
        using Activity parent = TestSource.StartActivity("disconnect-test")!;

        var network = new BcrdtInMemoryNetwork();
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { Id = DocumentId, ClientId = FirstClient });
        await using BcrdtSyncProvider provider = await ProviderAsync(doc, network.CreateTransport(Room), TimeSpan.FromMinutes(5));

        await provider.ConnectAsync(TestContext.CancellationToken);
        await provider.DisconnectAsync(TestContext.CancellationToken);

        Activity handshake = await spans.WaitForAsync(parent, BcrdtDiagnostics.HandshakeSpan);
        Assert.AreEqual("disconnected", handshake.GetTagItem("bcrdt.outcome"));
        Assert.AreEqual(ActivityStatusCode.Unset, handshake.Status, "disconnecting on purpose is no failure");
        Assert.AreSame(parent, Activity.Current);
    }

    [TestMethod]
    public async Task Persistence_traces_loading_writing_and_compaction_as_traces_of_their_own()
    {
        using var spans = new SpanRecorder();
        var store = new TracingStore();
        await store.AppendUpdateAsync(DocumentId, Seed(), TestContext.CancellationToken);

        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { Id = DocumentId, ClientId = FirstClient });
        Activity loaded;
        BcrdtDocumentPersistence persistence;
        using (Activity parent = TestSource.StartActivity("persistence-test")!)
        {
            persistence = await BcrdtDocumentPersistence.AttachAsync(doc, store, options: new BcrdtDocumentPersistenceOptions { CompactionThreshold = 3 }, cancellationToken: TestContext.CancellationToken);
            loaded = await spans.WaitForAsync(parent, BcrdtDiagnostics.PersistenceLoadSpan);
            Assert.AreSame(parent, Activity.Current);
        }

        await using (persistence)
        {
            Assert.AreEqual(1, loaded.GetTagItem("bcrdt.persistence.records"));
            Assert.IsGreaterThan(0, (long)loaded.GetTagItem("bcrdt.persistence.size")!);

            for (int i = 0; i < 3; i++)
            {
                doc.GetText("body").Insert(0, "x");
                await persistence.FlushAsync();
            }

            Activity compaction = await store.Compacted.Task.WaitAsync(Patience, TestContext.CancellationToken);
            await EventuallyAsync(() => compaction.IsStopped, "the compaction span should end");
            Assert.AreEqual(BcrdtDiagnostics.PersistenceCompactSpan, compaction.OperationName);
            Assert.AreEqual(3, compaction.GetTagItem("bcrdt.persistence.records"));
            Assert.AreEqual(ActivityStatusCode.Unset, compaction.Status);
            Assert.AreEqual(default, compaction.ParentSpanId, "background writes do not join whatever trace attached the binding");

            Activity append = store.Appends.First();
            Assert.AreEqual(BcrdtDiagnostics.PersistenceAppendSpan, append.OperationName);
            Assert.AreEqual(1, append.GetTagItem("bcrdt.persistence.updates"));
            Assert.IsGreaterThan(0, (long)append.GetTagItem("bcrdt.persistence.size")!);
            AssertCarriesNoIdentifier([loaded, compaction, .. store.Appends]);
        }
    }

    [TestMethod]
    public async Task A_failed_compaction_is_an_error_span_named_by_its_exception_type_alone()
    {
        using var spans = new SpanRecorder();
        var store = new TracingStore { FailCompaction = true };
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { Id = DocumentId, ClientId = FirstClient });
        await using BcrdtDocumentPersistence persistence =
            await BcrdtDocumentPersistence.AttachAsync(doc, store, options: new BcrdtDocumentPersistenceOptions { CompactionThreshold = 1 }, cancellationToken: TestContext.CancellationToken);

        doc.GetText("body").Insert(0, "x");
        await persistence.FlushAsync();

        Activity compaction = await store.Compacted.Task.WaitAsync(Patience, TestContext.CancellationToken);
        await EventuallyAsync(() => compaction.IsStopped, "the compaction span should end");
        Assert.AreEqual(ActivityStatusCode.Error, compaction.Status);
        Assert.AreEqual(nameof(IOException), compaction.GetTagItem("error.type"));
        Assert.AreEqual(nameof(IOException), compaction.StatusDescription);
        Assert.IsEmpty(compaction.Events, "an exception event would carry the message, and the message names the document");
        AssertCarriesNoIdentifier([compaction]);
    }

    [TestMethod]
    public async Task A_document_host_traces_loading_applying_answering_and_unloading()
    {
        using var spans = new SpanRecorder();
        var store = new TracingStore();
        var host = new BcrdtDocumentHost(new BcrdtDocumentHostOptions { Store = store, ValidateUpdate = _ => ValueTask.FromResult(true) });
        var connection = new TestConnection(BcrdtDocumentAccess.ReadWrite);

        using (Activity parent = TestSource.StartActivity("host-test")!)
        {
            using BcrdtHostSession session = await host.JoinAsync(DocumentId, connection, TestContext.CancellationToken);
            Activity load = await spans.WaitForAsync(parent, BcrdtDiagnostics.HostLoadSpan);
            Activity storeLoad = await spans.WaitForAsync(parent, BcrdtDiagnostics.PersistenceLoadSpan);
            Assert.AreEqual(load.SpanId, storeLoad.ParentSpanId);

            Assert.IsTrue(await session.ReceiveAsync(BcrdtSyncProtocol.EncodeUpdate(Seed())));
            Activity apply = await spans.WaitForAsync(parent, BcrdtDiagnostics.HostApplySpan);
            Assert.AreEqual("update", apply.GetTagItem("bcrdt.message.kind"));
            Assert.AreEqual("applied", apply.GetTagItem("bcrdt.outcome"));
            Assert.AreEqual("accepted", apply.GetTagItem("bcrdt.validation"));
            Assert.IsNull(apply.GetTagItem("bcrdt.reason"));
            Assert.AreEqual(parent.SpanId, apply.ParentSpanId);

            Assert.IsFalse(await session.ReceiveAsync(BcrdtSyncProtocol.EncodeSyncStep1(new BcrdtStateVector())));
            Activity answer = await spans.WaitForAsync(parent, BcrdtDiagnostics.HostHandshakeSpan);
            Assert.AreEqual("sync_step1", answer.GetTagItem("bcrdt.message.kind"));
            Assert.AreEqual("answered", answer.GetTagItem("bcrdt.outcome"));
            Assert.IsGreaterThan(0, (long)answer.GetTagItem("bcrdt.reply.size")!);

            Assert.AreSame(parent, Activity.Current);
            AssertCarriesNoIdentifier(spans.InTrace(parent));
        }

        await host.DisposeAsync();
        Activity unload = await store.Detached.Task.WaitAsync(Patience, TestContext.CancellationToken);
        await EventuallyAsync(() => unload.IsStopped, "the unload span should end");
        Assert.AreEqual(BcrdtDiagnostics.HostUnloadSpan, unload.OperationName);
        Assert.AreEqual(default, unload.ParentSpanId);
        AssertCarriesNoIdentifier([unload]);
    }

    [TestMethod]
    public async Task A_document_host_records_why_it_dropped_a_change()
    {
        using var spans = new SpanRecorder();
        int calls = 0;
        await using var host = new BcrdtDocumentHost(new BcrdtDocumentHostOptions
        {
            // The first change is rejected, the second fails to validate at all.
            ValidateUpdate = _ => Interlocked.Increment(ref calls) == 1
                ? ValueTask.FromResult(false)
                : throw new InvalidOperationException($"Validation of '{DocumentId}' broke."),
        });
        var writer = new TestConnection(BcrdtDocumentAccess.ReadWrite);
        var reader = new TestConnection(BcrdtDocumentAccess.ReadOnly);
        using BcrdtHostSession writerSession = await host.JoinAsync(DocumentId, writer, TestContext.CancellationToken);
        using BcrdtHostSession readerSession = await host.JoinAsync(DocumentId, reader, TestContext.CancellationToken);

        using Activity parent = TestSource.StartActivity("drop-test")!;

        Assert.IsFalse(await readerSession.ReceiveAsync(BcrdtSyncProtocol.EncodeUpdate(Seed())));
        Activity readOnly = await spans.WaitForAsync(parent, BcrdtDiagnostics.HostApplySpan, span => (string?)span.GetTagItem("bcrdt.reason") == "read_only");
        Assert.AreEqual("dropped", readOnly.GetTagItem("bcrdt.outcome"));
        Assert.IsNull(readOnly.GetTagItem("bcrdt.validation"), "a reader's change never reaches the validator");
        Assert.AreEqual(ActivityStatusCode.Unset, readOnly.Status, "refusing a client is the host doing its job, not failing");

        Assert.IsFalse(await writerSession.ReceiveAsync(BcrdtSyncProtocol.EncodeUpdate(Seed())));
        Activity rejected = await spans.WaitForAsync(parent, BcrdtDiagnostics.HostApplySpan, span => (string?)span.GetTagItem("bcrdt.reason") == "rejected");
        Assert.AreEqual("rejected", rejected.GetTagItem("bcrdt.validation"));
        Assert.AreEqual("dropped", rejected.GetTagItem("bcrdt.outcome"));
        Assert.AreEqual(ActivityStatusCode.Unset, rejected.Status);

        var another = new TestConnection(BcrdtDocumentAccess.ReadWrite);
        using BcrdtHostSession anotherSession = await host.JoinAsync(DocumentId, another, TestContext.CancellationToken);
        Assert.IsFalse(await anotherSession.ReceiveAsync(BcrdtSyncProtocol.EncodeUpdate(Seed())));
        Activity failed = await spans.WaitForAsync(parent, BcrdtDiagnostics.HostApplySpan, span => (string?)span.GetTagItem("bcrdt.validation") == "failed");
        Assert.AreEqual("rejected", failed.GetTagItem("bcrdt.reason"));
        Assert.AreEqual(ActivityStatusCode.Error, failed.Status);
        Assert.AreEqual(nameof(InvalidOperationException), failed.GetTagItem("error.type"));

        // A change in a protocol version nobody speaks is dropped before it reaches the validator.
        Assert.IsFalse(await anotherSession.ReceiveAsync(new byte[] { 0xEE, (byte)BcrdtSyncMessageKind.Update, 0 }));
        Activity malformed = await spans.WaitForAsync(parent, BcrdtDiagnostics.HostApplySpan, span => (string?)span.GetTagItem("bcrdt.reason") == "malformed");
        Assert.AreEqual("dropped", malformed.GetTagItem("bcrdt.outcome"));
        Assert.AreEqual(2, Volatile.Read(ref calls));

        AssertCarriesNoIdentifier(spans.InTrace(parent));
    }

    /// <summary>
    /// Runs alone, after the parallel tests: while it runs, nothing listens to the source but the listener it
    /// adds, which declines every span. That is what an application that does not trace amounts to.
    /// </summary>
    [TestMethod]
    [DoNotParallelize]
    public async Task Nothing_is_created_while_nobody_listens()
    {
        Assert.IsNull(BcrdtDiagnostics.Start(BcrdtDiagnostics.HostLoadSpan));
        Assert.IsNull(BcrdtDiagnostics.StartDetached(BcrdtDiagnostics.HandshakeSpan));
        Assert.IsNull(BcrdtDiagnostics.StartHostMessage(BcrdtSyncProtocol.EncodeUpdate(Seed())));

        int asked = 0;
        int created = 0;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == BcrdtDiagnostics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
            {
                Interlocked.Increment(ref asked);
                return ActivitySamplingResult.None;
            },
            ActivityStarted = _ => Interlocked.Increment(ref created),
        };
        ActivitySource.AddActivityListener(listener);

        var network = new BcrdtInMemoryNetwork();
        using var firstDoc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = FirstClient });
        using var secondDoc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = SecondClient });
        await using (BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(
            firstDoc, new BcrdtInMemoryDocumentStore(), options: new BcrdtDocumentPersistenceOptions { CompactionThreshold = 1 }, cancellationToken: TestContext.CancellationToken))
        await using (BcrdtSyncProvider first = await ProviderAsync(firstDoc, network.CreateTransport(Room), TimeSpan.FromMilliseconds(50)))
        await using (BcrdtSyncProvider second = await ProviderAsync(secondDoc, network.CreateTransport(Room), TimeSpan.FromMilliseconds(50)))
        {
            await first.ConnectAsync(TestContext.CancellationToken);
            await second.ConnectAsync(TestContext.CancellationToken);
            await EventuallyAsync(() => first.IsSynced && second.IsSynced, "both should sync");
            firstDoc.GetText("body").Insert(0, "untraced");
            await EventuallyAsync(() => secondDoc.GetText("body").ToString() == "untraced", "the change should arrive");
            await persistence.FlushAsync();
        }

        await using (var host = new BcrdtDocumentHost())
        {
            using BcrdtHostSession session = await host.JoinAsync(DocumentId, new TestConnection(BcrdtDocumentAccess.ReadWrite), TestContext.CancellationToken);
            Assert.IsTrue(await session.ReceiveAsync(BcrdtSyncProtocol.EncodeUpdate(Seed())));
        }

        Assert.IsGreaterThan(0, Volatile.Read(ref asked), "the instrumentation was reached");
        Assert.AreEqual(0, Volatile.Read(ref created));
        Assert.IsNull(Activity.Current);
    }

    /// <summary>One small change, as an update.</summary>
    private static byte[] Seed()
    {
        using var author = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = SecondClient });
        author.GetText("body").Insert(0, "seed");
        return author.EncodeStateAsUpdate();
    }

    /// <summary>
    /// Fails if any span carries an identifier anywhere an exporter would read: its name, status, tags or events.
    /// </summary>
    private static void AssertCarriesNoIdentifier(IEnumerable<Activity> activities)
    {
        int count = 0;
        foreach (Activity activity in activities)
        {
            count++;
            List<string?> values = [activity.DisplayName, activity.OperationName, activity.StatusDescription];
            values.AddRange(activity.TagObjects.Select(tag => tag.Value?.ToString()));
            values.AddRange(activity.TagObjects.Select(tag => tag.Key));
            foreach (ActivityEvent e in activity.Events)
            {
                values.Add(e.Name);
                values.AddRange(e.Tags.Select(tag => tag.Value?.ToString()));
            }
            values.AddRange(activity.Baggage.Select(item => item.Value));

            foreach (string? value in values)
            {
                if (value is null) continue;
                foreach (string identifier in Identifiers)
                {
                    Assert.IsFalse(value.Contains(identifier, StringComparison.Ordinal), $"{activity.OperationName} carries '{value}'");
                }
            }
        }
        Assert.IsGreaterThan(0, count, "there should be spans to check");
    }

    private static async Task EventuallyAsync(Func<bool> condition, string because)
    {
        var clock = Stopwatch.StartNew();
        while (!condition())
        {
            Assert.IsLessThan(Patience, clock.Elapsed, because);
            await Task.Delay(10);
        }
    }

    /// <summary>Records every bcrdt span that ends while it is attached, and samples the tests' own parent spans.</summary>
    private sealed class SpanRecorder : IDisposable
    {
        private readonly ConcurrentQueue<Activity> _stopped = new();
        private readonly ActivityListener _listener;

        public SpanRecorder()
        {
            _listener = new ActivityListener
            {
                // By name: the tests' own source may be created after the listener, while its field is still unset.
                ShouldListenTo = source => source.Name is BcrdtDiagnostics.ActivitySourceName or TestSourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity =>
                {
                    if (activity.Source.Name == BcrdtDiagnostics.ActivitySourceName) _stopped.Enqueue(activity);
                },
            };
            ActivitySource.AddActivityListener(_listener);
        }

        /// <summary>The ended spans in the parent's trace: the ones this test caused.</summary>
        public IEnumerable<Activity> InTrace(Activity parent) => _stopped.Where(span => span.TraceId == parent.TraceId);

        public async Task<Activity> WaitForAsync(Activity parent, string name, Func<Activity, bool>? where = null)
        {
            var clock = Stopwatch.StartNew();
            while (true)
            {
                Activity? found = InTrace(parent).FirstOrDefault(span => span.OperationName == name && (where?.Invoke(span) ?? true));
                if (found is not null) return found;
                Assert.IsLessThan(Patience, clock.Elapsed, $"no {name} span ended");
                await Task.Delay(10);
            }
        }

        public void Dispose() => _listener.Dispose();
    }

    /// <summary>An in-memory store that remembers which span was current when it was written, compacted and let go.</summary>
    private sealed class TracingStore : IBcrdtDocumentStore, IBcrdtAttachedDocumentTracker
    {
        private readonly BcrdtInMemoryDocumentStore _inner = new();

        public bool FailCompaction { get; init; }

        public ConcurrentQueue<Activity> Appends { get; } = new();

        public TaskCompletionSource<Activity> Compacted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<Activity> Detached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default) =>
            _inner.LoadAsync(documentId, cancellationToken);

        public ValueTask AppendUpdateAsync(string documentId, ReadOnlyMemory<byte> update, CancellationToken cancellationToken = default)
        {
            if (Activity.Current is { } current) Appends.Enqueue(current);
            return _inner.AppendUpdateAsync(documentId, update, cancellationToken);
        }

        public ValueTask CompactAsync(string documentId, CancellationToken cancellationToken = default)
        {
            if (Activity.Current is { } current) Compacted.TrySetResult(current);
            if (FailCompaction) throw new IOException($"Could not compact '{documentId}'.");
            return _inner.CompactAsync(documentId, cancellationToken);
        }

        public ValueTask DeleteAsync(string documentId, CancellationToken cancellationToken = default) =>
            _inner.DeleteAsync(documentId, cancellationToken);

        public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default) =>
            _inner.ListAsync(cancellationToken);

        public void OnAttached(string documentId) => ((IBcrdtAttachedDocumentTracker)_inner).OnAttached(documentId);

        public void OnDetached(string documentId)
        {
            if (Activity.Current is { } current) Detached.TrySetResult(current);
            ((IBcrdtAttachedDocumentTracker)_inner).OnDetached(documentId);
        }
    }

    /// <summary>A host connection with identifiers worth leaking, and no relay behind it.</summary>
    private sealed class TestConnection(BcrdtDocumentAccess access) : IBcrdtHostConnection
    {
        public string Id => ConnectionId;

        public BcrdtDocumentAccess Access => access;

        public string? UserId => TracingTests.UserId;

        public string Relay => "test";

        public void Send(byte[] message) { }

        public void Refuse(string reason) { }
    }

    public TestContext TestContext { get; set; }
}
