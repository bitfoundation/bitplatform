using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>A clock the test moves by hand.</summary>
internal sealed class ManualClock(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = start;

    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>A transport whose sends wait until the test opens a gate, so work can happen while a send is in flight.</summary>
internal sealed class GatedTransport : IBinkCollaborationTransport
{
    private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsConnected { get; set; } = true;

    public List<BinkCollaborationMessage> Sent { get; } = new();

    public event Func<BinkCollaborationMessage, Task>? MessageReceived { add { } remove { } }

    public event Action<bool>? ConnectionChanged { add { } remove { } }

    public void Open() => _gate.TrySetResult();

    public async Task SendAsync(BinkCollaborationMessage message, CancellationToken cancellationToken = default)
    {
        Sent.Add(message);
        await _gate.Task.ConfigureAwait(false);
    }
}

/// <summary>An element type the serializer does not know, as an application's own element would be.</summary>
internal sealed record UnserializableElement : BinkElement
{
    public override BinkRect LocalBounds => new(0, 0, 10, 10);

    public override string TypeName => "Custom";

    protected override void DrawCore(IBinkRenderContext context) { }

    protected override bool HitTestCore(Vector2 localPoint, float tolerance) => false;
}

/// <summary>
/// Regressions where participants ended up with different documents, lost each other's changes, or let a peer
/// crash their receive loop.
/// </summary>
[TestClass]
public class CollaborationConvergenceTests
{
    private static BinkDocument Copy(BinkDocument document) => BinkSerializer.Deserialize(BinkSerializer.Serialize(document));

    private static BinkStroke Stroke(int i) => TestData.Stroke(new Vector2(i * 30, 0), new Vector2(i * 30 + 20, 20));

    private static BinkCollaborationMessage Operation(string sender, long sequence, BinkDocument document, BinkDrawingOperation operation) => new()
    {
        Kind = BinkCollaborationMessageKind.Operation,
        SenderId = sender,
        DocumentId = document.Id,
        Sequence = sequence,
        Operation = BinkOperationCodec.Serialize(operation),
    };

    private static Guid[] Ids(BinkDocument document) => document.AllElements.Select(e => e.Id).ToArray();

    [TestMethod]
    public async Task Clearing_the_page_while_someone_deletes_one_stroke_empties_both_pages()
    {
        // The clear arrived as one batch whose second removal was already done on sam's side. A batch is checked by
        // its first operation only, so it was applied, failed on the second, rolled back, and sam kept two strokes
        // nobody else had.
        var alex = new BinkDocument();
        BinkStroke[] strokes = [Stroke(0), Stroke(1), Stroke(2)];
        foreach (BinkStroke stroke in strokes) alex.AddElement(stroke);
        BinkDocument sam = Copy(alex);
        ManualTransport ta = new(), ts = new();
        await using var sa = new BinkCollaborationSession(alex, ta, "alex");
        await using var ss = new BinkCollaborationSession(sam, ts, "sam");

        alex.Clear();
        sam.RemoveElement(strokes[1].Id);
        int changes = 0;
        sam.Changed += (_, _) => changes++;

        await ts.DeliverAsync(Assert.ContainsSingle(ta.Sent));
        await ta.DeliverAsync(Assert.ContainsSingle(ts.Sent));

        Assert.AreEqual(0, alex.ElementCount);
        Assert.AreEqual(0, sam.ElementCount);
        // What is left of the batch still lands as one change.
        Assert.AreEqual(1, changes);
    }

    [TestMethod]
    public async Task A_stroke_with_a_huge_time_can_still_be_edited_and_served()
    {
        // The encoder threw on a time delta past 32 bits. Moving the stroke threw out of the document's Changed
        // event and nothing was sent; every snapshot request then threw out of the receive handler.
        var document = new BinkDocument();
        var stroke = new BinkStroke(BinkStrokePoints.FromArrays([0f, 10f], [0f, 10f], [0.5f, 0.5f], [0f, 3_000_000_000f]), new BinkPenBrush());
        document.AddElement(stroke);
        var transport = new ManualTransport();
        await using var session = new BinkCollaborationSession(document, transport, "alex");
        var rejections = new List<string>();
        session.RemoteOperationRejected += rejections.Add;

        document.ReplaceElement(document.GetElement(stroke.Id) with { Transform = Matrix3x2.CreateTranslation(5, 5) });
        await transport.DeliverAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.SnapshotRequest,
            SenderId = "zoe",
            DocumentId = document.Id,
            Sequence = 1,
        });

        Assert.ContainsSingle(transport.Sent.Where(m => m.Kind == BinkCollaborationMessageKind.Operation));
        BinkCollaborationMessage answer = Assert.ContainsSingle(transport.Sent.Where(m => m.Kind == BinkCollaborationMessageKind.Snapshot));
        Assert.AreEqual(1, BinkSerializer.Deserialize(answer.Snapshot!).ElementCount);
        Assert.IsEmpty(rejections);
    }

    [TestMethod]
    public async Task A_document_that_cannot_be_serialized_is_reported_instead_of_crashing_the_receive_loop()
    {
        var document = new BinkDocument();
        document.AddElement(new UnserializableElement());
        var transport = new ManualTransport();
        await using var session = new BinkCollaborationSession(document, transport, "alex");
        var rejections = new List<string>();
        session.RemoteOperationRejected += rejections.Add;

        await transport.DeliverAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.SnapshotRequest,
            SenderId = "zoe",
            DocumentId = document.Id,
            Sequence = 1,
        });

        Assert.IsEmpty(transport.Sent);
        Assert.ContainsSingle(rejections);
    }

    [TestMethod]
    public async Task A_participant_that_reloads_without_leaving_is_heard_again()
    {
        // Every session numbered its messages from 1. After a reload the others still held the old high-water mark,
        // so everything the new session sent was discarded as a retransmission.
        var hub = new TestHub();
        var alex = new BinkDocument();
        BinkDocument sam = Copy(alex);
        await using var sa = new BinkCollaborationSession(alex, hub.CreateClient(), "alex");
        TestTransport first = hub.CreateClient();
        var crashed = new BinkCollaborationSession(sam, first, "sam");
        for (int i = 0; i < 10; i++) sam.AddElement(Stroke(i));
        Assert.AreEqual(10, alex.ElementCount);

        // The page reloads: the old session disappears without a Leave and a new one starts under the same id.
        first.SetConnected(false);
        BinkDocument reloaded = Copy(sam);
        await using var restarted = new BinkCollaborationSession(reloaded, hub.CreateClient(), "sam");
        for (int i = 10; i < 15; i++) reloaded.AddElement(Stroke(i));

        Assert.AreEqual(15, alex.ElementCount);
        GC.KeepAlive(crashed);
    }

    [TestMethod]
    public async Task Message_numbers_start_from_the_clock_and_stay_exact_in_json()
    {
        // A reload in a new process cannot continue the old counter. Starting from the clock keeps the new session
        // above the old one, and below 2^53 so a JSON or JavaScript transport carries the number exactly.
        var document = new BinkDocument();
        var transport = new ManualTransport();
        await using var session = new BinkCollaborationSession(document, transport, "alex");
        document.AddElement(Stroke(0));

        long sequence = Assert.ContainsSingle(transport.Sent).Sequence;
        Assert.IsGreaterThan(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds() * 1000, sequence);
        Assert.IsLessThan(1L << 53, sequence);
    }

    [TestMethod]
    public async Task A_snapshot_request_below_the_cursor_comes_from_a_restarted_sender()
    {
        // The same participant rejoined from a device whose clock is behind the one it used before: its numbers
        // start below the cursor the others hold for it.
        var document = new BinkDocument();
        Guid layer = document.Layers[0].Id;
        var transport = new ManualTransport();
        await using var session = new BinkCollaborationSession(document, transport, "alex");

        await transport.DeliverAsync(Operation("sam", 9_000_000, document, new BinkAddElementOperation(layer, Stroke(0))));
        await transport.DeliverAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.SnapshotRequest,
            SenderId = "sam",
            DocumentId = document.Id,
            Sequence = 5,
        });
        await transport.DeliverAsync(Operation("sam", 6, document, new BinkAddElementOperation(layer, Stroke(1))));

        Assert.ContainsSingle(transport.Sent.Where(m => m.Kind == BinkCollaborationMessageKind.Snapshot && m.RecipientId == "sam"));
        Assert.AreEqual(2, document.ElementCount);
    }

    [TestMethod]
    public async Task A_stroke_drawn_while_the_answer_is_on_its_way_survives_it()
    {
        // The answer was serialized before zoe's stroke reached alex. Applying it removed the stroke on zoe's side
        // only, so alex had two strokes and zoe one.
        var alex = new BinkDocument();
        BinkDocument zoe = Copy(alex);
        alex.AddElement(Stroke(0));
        ManualTransport ta = new(), tz = new();
        await using var sa = new BinkCollaborationSession(alex, ta, "alex");
        await using var sz = new BinkCollaborationSession(zoe, tz, "zoe");
        using var history = new BinkHistoryManager(zoe);

        await sz.RequestSnapshotAsync();
        await ta.DeliverAsync(tz.Sent[0]);
        BinkCollaborationMessage answer = Assert.ContainsSingle(ta.Sent);

        zoe.AddElement(Stroke(1));
        await ta.DeliverAsync(tz.Sent[1]);
        int sent = tz.Sent.Count;
        await tz.DeliverAsync(answer);

        Assert.AreEqual(2, alex.ElementCount);
        Assert.AreSequenceEqual(Ids(alex), Ids(zoe));
        // The replay is a remote change: it is not sent again and does not enter the undo history.
        Assert.AreEqual(sent, tz.Sent.Count);
        Assert.AreEqual(1, history.UndoCount);
    }

    [TestMethod]
    public async Task A_third_participants_change_that_beats_the_answer_survives_it()
    {
        var alex = new BinkDocument();
        BinkDocument zoe = Copy(alex);
        alex.AddElement(Stroke(0));
        BinkDocument sam = Copy(alex);
        ManualTransport ta = new(), ts = new(), tz = new();
        await using var sa = new BinkCollaborationSession(alex, ta, "alex");
        await using var ss = new BinkCollaborationSession(sam, ts, "sam");
        await using var sz = new BinkCollaborationSession(zoe, tz, "zoe");

        await sz.RequestSnapshotAsync();
        await ta.DeliverAsync(Assert.ContainsSingle(tz.Sent));
        BinkCollaborationMessage answer = Assert.ContainsSingle(ta.Sent);

        // Sam's stroke reaches zoe before alex's answer, and alex only after he wrote it.
        sam.AddElement(Stroke(1));
        BinkCollaborationMessage fromSam = Assert.ContainsSingle(ts.Sent);
        await tz.DeliverAsync(fromSam);
        await ta.DeliverAsync(fromSam);
        await tz.DeliverAsync(answer);

        Assert.AreEqual(2, zoe.ElementCount);
        Assert.AreSequenceEqual(Ids(alex), Ids(zoe));
        Assert.AreSequenceEqual(Ids(sam), Ids(zoe));
    }

    [TestMethod]
    public async Task A_snapshot_delivered_twice_does_not_undo_a_later_edit()
    {
        // Snapshots skipped the sequence check, so a retried delivery after a later edit replaced the document again.
        var alex = new BinkDocument();
        BinkDocument sam = Copy(alex);
        ManualTransport ta = new(), ts = new();
        await using var sa = new BinkCollaborationSession(alex, ta, "alex");
        await using var ss = new BinkCollaborationSession(sam, ts, "sam");

        await sa.SendSnapshotAsync();
        alex.AddElement(Stroke(0));
        BinkCollaborationMessage snapshot = ta.Sent[0];
        BinkCollaborationMessage edit = ta.Sent[1];

        await ts.DeliverAsync(snapshot);
        await ts.DeliverAsync(edit);
        await ts.DeliverAsync(snapshot);

        Assert.AreEqual(1, sam.ElementCount);
    }

    [TestMethod]
    public async Task An_edit_that_overtakes_the_addition_it_edits_waits_for_it()
    {
        // Over a mesh, each sender's messages arrive in order but different senders' do not. Sam's edit of alex's
        // new stroke reached zoe first, was taken for stale and dropped, and zoe kept the unedited stroke.
        var alex = new BinkDocument();
        BinkDocument sam = Copy(alex), zoe = Copy(alex);
        ManualTransport ta = new(), ts = new(), tz = new();
        await using var sa = new BinkCollaborationSession(alex, ta, "alex");
        await using var ss = new BinkCollaborationSession(sam, ts, "sam");
        await using var sz = new BinkCollaborationSession(zoe, tz, "zoe");

        BinkStroke stroke = Stroke(0);
        alex.AddElement(stroke);
        BinkCollaborationMessage added = Assert.ContainsSingle(ta.Sent);
        await ts.DeliverAsync(added);
        sam.ReplaceElement(sam.GetElement(stroke.Id) with { Description = "edited by sam" });
        BinkCollaborationMessage edited = Assert.ContainsSingle(ts.Sent);

        await tz.DeliverAsync(edited);
        Assert.AreEqual(0, zoe.ElementCount);
        await tz.DeliverAsync(added);

        Assert.AreEqual("edited by sam", zoe.GetElement(stroke.Id).Description);
        Assert.AreEqual(sam.GetElement(stroke.Id).Version, zoe.GetElement(stroke.Id).Version);
    }

    [TestMethod]
    public async Task A_deletion_that_overtakes_the_addition_still_deletes()
    {
        var alex = new BinkDocument();
        BinkDocument sam = Copy(alex), zoe = Copy(alex);
        ManualTransport ta = new(), ts = new(), tz = new();
        await using var sa = new BinkCollaborationSession(alex, ta, "alex");
        await using var ss = new BinkCollaborationSession(sam, ts, "sam");
        await using var sz = new BinkCollaborationSession(zoe, tz, "zoe");

        BinkStroke stroke = Stroke(0);
        alex.AddElement(stroke);
        BinkCollaborationMessage added = Assert.ContainsSingle(ta.Sent);
        await ts.DeliverAsync(added);
        sam.RemoveElement(stroke.Id);

        await tz.DeliverAsync(Assert.ContainsSingle(ts.Sent));
        await tz.DeliverAsync(added);

        Assert.AreEqual(0, zoe.ElementCount);
    }

    [TestMethod]
    public async Task An_operation_whose_dependency_never_arrives_expires()
    {
        var alex = new BinkDocument();
        BinkDocument sam = Copy(alex), zoe = Copy(alex);
        ManualTransport ta = new(), ts = new(), tz = new();
        var clock = new ManualClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        await using var sa = new BinkCollaborationSession(alex, ta, "alex");
        await using var ss = new BinkCollaborationSession(sam, ts, "sam");
        await using var sz = new BinkCollaborationSession(zoe, tz, "zoe") { Clock = clock };

        BinkStroke stroke = Stroke(0);
        alex.AddElement(stroke);
        BinkCollaborationMessage added = Assert.ContainsSingle(ta.Sent);
        await ts.DeliverAsync(added);
        sam.ReplaceElement(sam.GetElement(stroke.Id) with { Description = "edited by sam" });

        await tz.DeliverAsync(Assert.ContainsSingle(ts.Sent));
        clock.Now += TimeSpan.FromMinutes(5);
        await tz.DeliverAsync(added);

        Assert.IsNull(zoe.GetElement(stroke.Id).Description);
    }

    [TestMethod]
    public async Task A_late_edit_of_a_deleted_element_is_dropped_rather_than_held()
    {
        // Held, it would land if the deletion were undone, on this replica alone: everyone else applied it before
        // the deletion and then took the undone stroke as it was before the edit.
        var alex = new BinkDocument();
        BinkStroke stroke = Stroke(0);
        alex.AddElement(stroke);
        BinkDocument sam = Copy(alex), zoe = Copy(alex);
        ManualTransport ta = new(), ts = new(), tz = new();
        await using var sa = new BinkCollaborationSession(alex, ta, "alex");
        await using var ss = new BinkCollaborationSession(sam, ts, "sam");
        await using var sz = new BinkCollaborationSession(zoe, tz, "zoe");
        using var history = new BinkHistoryManager(zoe);

        zoe.RemoveElement(stroke.Id);
        sam.ReplaceElement(sam.GetElement(stroke.Id) with { Description = "edited by sam" });
        await tz.DeliverAsync(Assert.ContainsSingle(ts.Sent));
        Assert.IsTrue(history.Undo());

        // Any remote change makes waiting operations retry.
        alex.AddElement(Stroke(1));
        await tz.DeliverAsync(Assert.ContainsSingle(ta.Sent));
        foreach (BinkCollaborationMessage message in tz.Sent.ToArray()) await ts.DeliverAsync(message);

        Assert.IsNull(zoe.GetElement(stroke.Id).Description);
        Assert.AreEqual(sam.GetElement(stroke.Id).Description, zoe.GetElement(stroke.Id).Description);
    }

    [TestMethod]
    public async Task Presence_is_kept_under_the_senders_id_and_this_replicas_clock()
    {
        // Expiry compared the sender's timestamp with this replica's clock, so a peer whose clock ran behind
        // vanished at once and one that claimed a far-future time never expired; and the id inside the state was
        // stored as sent, so a peer could speak for someone else.
        var document = new BinkDocument();
        var transport = new ManualTransport();
        var clock = new ManualClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        await using var session = new BinkCollaborationSession(document, transport, "alex") { Clock = clock };
        BinkPresenceState? reported = null;
        session.PresenceChanged += p => reported = p;

        await transport.DeliverAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Presence,
            SenderId = "sam",
            DocumentId = document.Id,
            Sequence = 1,
            Presence = new BinkPresenceState { ParticipantId = "alex", Name = "Sam", Timestamp = DateTimeOffset.MinValue },
        });

        BinkPresenceState stored = Assert.ContainsSingle(session.Participants);
        Assert.AreEqual("sam", stored.ParticipantId);
        Assert.AreEqual(clock.Now, stored.Timestamp);
        Assert.AreEqual("sam", reported?.ParticipantId);

        await transport.DeliverAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Presence,
            SenderId = "sam",
            DocumentId = document.Id,
            Sequence = 2,
            Presence = new BinkPresenceState { ParticipantId = "sam", Timestamp = DateTimeOffset.MaxValue },
        });
        clock.Now += TimeSpan.FromMinutes(1);

        Assert.IsEmpty(session.Participants);
    }

    [TestMethod]
    public async Task Presence_live_strokes_and_selections_are_bounded_and_finite()
    {
        var document = new BinkDocument();
        var transport = new ManualTransport();
        var options = new BinkCollaborationOptions { Limits = BinkDocumentLimits.Default with { MaxElements = 50 } };
        await using var session = new BinkCollaborationSession(document, transport, "alex", options);

        var live = new List<Vector2>();
        for (int i = 0; i < 25_000; i++) live.Add(i % 100 == 0 ? new Vector2(float.NaN, 0) : new Vector2(i % 500, i % 300));
        await transport.DeliverAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Presence,
            SenderId = "sam",
            DocumentId = document.Id,
            Sequence = 1,
            Presence = new BinkPresenceState
            {
                ParticipantId = "sam",
                Cursor = new Vector2(float.PositiveInfinity, 0),
                LiveStroke = live,
                LiveStrokeWidth = float.NaN,
                Selection = Enumerable.Range(0, 200).Select(_ => Guid.NewGuid()).ToArray(),
            },
        });

        BinkPresenceState stored = Assert.ContainsSingle(session.Participants);
        Assert.IsLessThanOrEqualTo(10_000, stored.LiveStroke!.Count);
        Assert.IsTrue(stored.LiveStroke.All(p => float.IsFinite(p.X) && float.IsFinite(p.Y)));
        Assert.AreEqual(50, stored.Selection!.Count);
        Assert.IsNull(stored.Cursor);
        Assert.IsTrue(float.IsFinite(stored.LiveStrokeBounds.Width));
    }

    [TestMethod]
    public async Task A_snapshot_empties_each_layer_from_the_top_down()
    {
        // Removing bottom-first made every removal scan and shift the whole layer: 20,000 strokes took two seconds.
        var alex = new BinkDocument();
        BinkDocument sam = Copy(alex);
        for (int i = 0; i < 6; i++) sam.AddElement(Stroke(i));
        Guid[] expected = sam.Layers[0].Elements.Select(e => e.Id).Reverse().ToArray();
        ManualTransport ta = new(), ts = new();
        await using var sa = new BinkCollaborationSession(alex, ta, "alex");
        await using var ss = new BinkCollaborationSession(sam, ts, "sam");
        BinkDrawingOperation? applied = null;
        sam.Changed += (_, e) => applied ??= e.Operation;

        await sa.SendSnapshotAsync();
        await ts.DeliverAsync(Assert.ContainsSingle(ta.Sent));

        Guid[] removed = ((BinkBatchOperation)applied!).Operations.OfType<BinkRemoveElementOperation>().Select(r => r.ElementId).ToArray();
        Assert.AreSequenceEqual(expected, removed);
        Assert.AreEqual(0, sam.ElementCount);
    }

    [TestMethod]
    public async Task Editing_a_stroke_with_an_unregistered_brush_sends_its_kind_unchanged()
    {
        // The stroke fell back to a pen, so moving it told everyone, including clients that have the brush, that it
        // was drawn with a pen.
        var original = new BinkDocument();
        original.AddElement(Stroke(0));
        string json = BinkSerializer.Serialize(original).Replace("\"kind\":\"pen\"", "\"kind\":\"glitter-2030\"", StringComparison.Ordinal);
        BinkDocument document = BinkSerializer.Deserialize(json);
        var transport = new ManualTransport();
        await using var session = new BinkCollaborationSession(document, transport, "alex");

        BinkElement stroke = document.AllElements.Single();
        document.ReplaceElement(stroke with { Transform = Matrix3x2.CreateTranslation(3, 4) });

        var received = (BinkReplaceElementOperation)BinkOperationCodec.Deserialize(Assert.ContainsSingle(transport.Sent).Operation!);
        Assert.AreEqual("glitter-2030", ((BinkStroke)received.Element).Brush.Kind);
    }

    [TestMethod]
    public async Task A_message_trimmed_from_the_queue_while_in_flight_does_not_take_an_unsent_one_with_it()
    {
        // The flush peeked a message, awaited its send, then dequeued whatever was at the head. A change made
        // meanwhile trimmed the in-flight message from a full queue, so the flush dequeued the next one unsent.
        var document = new BinkDocument();
        var transport = new GatedTransport { IsConnected = false };
        await using var session = new BinkCollaborationSession(document, transport, "alex", new BinkCollaborationOptions { MaxOfflineQueue = 3 });
        for (int i = 0; i < 3; i++) document.AddElement(Stroke(i));
        Assert.AreEqual(3, session.PendingCount);

        transport.IsConnected = true;
        Task flush = session.FlushAsync();
        document.AddElement(Stroke(3));
        transport.Open();
        await flush;
        await session.FlushAsync();

        long[] sent = transport.Sent.Select(m => m.Sequence).ToArray();
        Assert.HasCount(4, sent);
        Assert.AreSequenceEqual(sent.Order().ToArray(), sent);
        Assert.AreEqual(4, sent.Distinct().Count());
        Assert.AreEqual(0, session.PendingCount);
    }

    [TestMethod]
    public async Task Concurrent_reorders_do_not_converge()
    {
        // Documents a limit, not a guarantee: z-order is index-based, so two concurrent reorders can leave the
        // replicas in different orders. If ordering is ever made convergent, invert this test and the README.
        var alex = new BinkDocument();
        BinkStroke[] strokes = [Stroke(0), Stroke(1), Stroke(2)];
        foreach (BinkStroke stroke in strokes) alex.AddElement(stroke);
        BinkDocument sam = Copy(alex);
        Guid layer = alex.Layers[0].Id;
        ManualTransport ta = new(), ts = new();
        await using var sa = new BinkCollaborationSession(alex, ta, "alex");
        await using var ss = new BinkCollaborationSession(sam, ts, "sam");

        alex.Apply(new BinkReorderElementOperation(strokes[0].Id, layer, 1));
        sam.Apply(new BinkReorderElementOperation(strokes[2].Id, layer, 0));
        await ts.DeliverAsync(Assert.ContainsSingle(ta.Sent));
        await ta.DeliverAsync(Assert.ContainsSingle(ts.Sent));

        CollectionAssert.AreNotEqual(Ids(alex), Ids(sam));
    }
}
