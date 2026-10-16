using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>An in-memory transport that connects sessions directly, so the tests exercise real synchronization.</summary>
internal sealed class TestHub
{
    private readonly List<TestTransport> _clients = new();

    public TestTransport CreateClient()
    {
        var client = new TestTransport(this);
        _clients.Add(client);
        return client;
    }

    internal async Task BroadcastAsync(TestTransport sender, BinkCollaborationMessage message)
    {
        foreach (TestTransport client in _clients.ToArray())
        {
            if (!ReferenceEquals(client, sender)) await client.ReceiveAsync(message).ConfigureAwait(false);
        }
    }
}

internal sealed class TestTransport(TestHub hub) : IBinkCollaborationTransport
{
    private bool _connected = true;

    public bool IsConnected => _connected;

    public int SentCount { get; private set; }

    public event Func<BinkCollaborationMessage, Task>? MessageReceived;

    public event Action<bool>? ConnectionChanged;

    public void SetConnected(bool connected)
    {
        if (_connected == connected) return;
        _connected = connected;
        ConnectionChanged?.Invoke(connected);
    }

    public async Task SendAsync(BinkCollaborationMessage message, CancellationToken cancellationToken = default)
    {
        if (!_connected) throw new InvalidOperationException("Offline.");
        SentCount++;
        await hub.BroadcastAsync(this, message).ConfigureAwait(false);
    }

    internal Task ReceiveAsync(BinkCollaborationMessage message)
        => _connected && MessageReceived is not null ? MessageReceived(message) : Task.CompletedTask;
}

[TestClass]
public class OperationCodecTests
{
    [TestMethod]
    public void Round_trips_every_operation_kind()
    {
        var document = new BinkDocument();
        Guid layer = document.Layers[0].Id;
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(50, 50));

        BinkDrawingOperation[] operations =
        [
            new BinkAddElementOperation(layer, stroke, 0),
            new BinkRemoveElementOperation(stroke.Id),
            new BinkReplaceElementOperation(stroke with { Description = "edited" }),
            new BinkReorderElementOperation(stroke.Id, layer, 2),
            new BinkAddLayerOperation(Guid.NewGuid(), new BinkLayerProperties { Name = "New", Opacity = 0.4f }, 1, [stroke]),
            new BinkRemoveLayerOperation(layer),
            new BinkUpdateLayerOperation(layer, new BinkLayerProperties { Name = "Renamed", IsLocked = true }),
            new BinkReorderLayerOperation(layer, 0),
            new BinkUpdateSettingsOperation(new BinkDocumentSettings { Width = 123, Height = 456, Background = BinkColor.Parse("#abcdef") }),
            new BinkSetMetadataOperation("key", "value"),
            new BinkSetMetadataOperation("key", null),
            new BinkBatchOperation([new BinkRemoveElementOperation(stroke.Id), new BinkSetMetadataOperation("a", "b")], "Group"),
        ];

        foreach (BinkDrawingOperation operation in operations)
        {
            string json = BinkOperationCodec.Serialize(operation);
            BinkDrawingOperation decoded = BinkOperationCodec.Deserialize(json);
            Assert.AreEqual(operation.GetType(), decoded.GetType());
            Assert.AreEqual(json, BinkOperationCodec.Serialize(decoded));
        }
    }

    [TestMethod]
    public void An_add_carries_the_stroke_points()
    {
        var document = new BinkDocument();
        BinkStroke stroke = new(TestData.PressureRamp(new Vector2(5, 5), new Vector2(120, 90), 40), new BinkPenBrush { Width = 7f });
        string json = BinkOperationCodec.Serialize(new BinkAddElementOperation(document.Layers[0].Id, stroke));

        var decoded = (BinkAddElementOperation)BinkOperationCodec.Deserialize(json);
        var received = (BinkStroke)decoded.Element;
        Assert.AreEqual(stroke.Id, received.Id);
        Assert.AreEqual(stroke.Points.Count, received.Points.Count);
        Assert.AreEqual(7f, received.Brush.Width, 5e-4);
    }

    [TestMethod]
    public void A_stroke_operation_is_small()
    {
        var document = new BinkDocument();
        BinkStroke stroke = new(TestData.PressureRamp(new Vector2(0, 0), new Vector2(200, 150), 120), new BinkPenBrush());
        string json = BinkOperationCodec.Serialize(new BinkAddElementOperation(document.Layers[0].Id, stroke));

        // Roughly ten bytes per point on the wire, including base64 expansion and all the JSON around it.
        double perPoint = json.Length / 120.0;
        Assert.IsTrue(perPoint < 14, $"a stroke cost {perPoint:0.0} bytes per point on the wire ({json.Length} bytes total)");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("{}")]
    [DataRow("not json")]
    [DataRow("{\"op\":\"nonsense\"}")]
    [DataRow("{\"op\":\"add\"}")]
    [DataRow("{\"op\":\"remove\",\"element\":\"not-a-guid\"}")]
    // Values of the wrong JSON kind used to escape as InvalidOperationException, which the session does not catch.
    [DataRow("{\"op\":5}")]
    [DataRow("{\"op\":\"metadata\",\"key\":5}")]
    [DataRow("{\"op\":\"batch\",\"label\":5,\"operations\":[]}")]
    [DataRow("{\"op\":\"settings\",\"background\":5}")]
    [DataRow("{\"op\":\"updateLayer\",\"layer\":\"00000000-0000-0000-0000-000000000001\",\"properties\":{\"name\":5}}")]
    [DataRow("{\"op\":\"updateLayer\",\"layer\":\"00000000-0000-0000-0000-000000000001\",\"properties\":{\"opacity\":\"x\"}}")]
    public void Malformed_operations_are_rejected(string json)
    {
        Assert.ThrowsExactly<BinkFormatException>(() => BinkOperationCodec.Deserialize(json));
    }

    [TestMethod]
    public void Layer_metadata_travels_with_layer_operations()
    {
        // An update replaces the whole property set, so metadata missing from the wire was erased on every other
        // replica by something as small as renaming the layer.
        var properties = new BinkLayerProperties { Name = "Notes", Metadata = new Dictionary<string, string> { ["role"] = "review" } };
        var decoded = (BinkUpdateLayerOperation)BinkOperationCodec.Deserialize(BinkOperationCodec.Serialize(new BinkUpdateLayerOperation(Guid.NewGuid(), properties)));
        Assert.AreEqual("review", decoded.Properties.Metadata?["role"]);

        var added = (BinkAddLayerOperation)BinkOperationCodec.Deserialize(BinkOperationCodec.Serialize(new BinkAddLayerOperation(Guid.NewGuid(), properties)));
        Assert.AreEqual("review", added.Properties.Metadata?["role"]);
    }
}

[TestClass]
public class CollaborationSessionTests
{
    private static (BinkDocument A, BinkDocument B, BinkCollaborationSession SessionA, BinkCollaborationSession SessionB, TestTransport TransportA, TestTransport TransportB) CreatePair()
    {
        var hub = new TestHub();
        var a = new BinkDocument(new BinkDocumentSettings { Width = 400, Height = 300 });
        BinkDocument b = BinkSerializer.Deserialize(BinkSerializer.Serialize(a));

        TestTransport transportA = hub.CreateClient();
        TestTransport transportB = hub.CreateClient();
        return (a, b, new BinkCollaborationSession(a, transportA, "alex"), new BinkCollaborationSession(b, transportB, "sam"), transportA, transportB);
    }

    [TestMethod]
    public async Task A_stroke_drawn_by_one_participant_appears_for_the_other()
    {
        (BinkDocument a, BinkDocument b, BinkCollaborationSession sa, BinkCollaborationSession sb, _, _) = CreatePair();
        await using (sa)
        await using (sb)
        {
            BinkStroke stroke = TestData.Stroke(new Vector2(10, 10), new Vector2(90, 60));
            a.AddElement(stroke);
            await Task.Yield();

            Assert.AreEqual(1, b.ElementCount);
            Assert.AreEqual(stroke.Id, b.Layers[0].Elements[0].Id);
        }
    }

    [TestMethod]
    public async Task Remote_changes_do_not_enter_the_local_undo_history()
    {
        (BinkDocument a, BinkDocument b, BinkCollaborationSession sa, BinkCollaborationSession sb, _, _) = CreatePair();
        await using (sa)
        await using (sb)
        {
            using var historyB = new BinkHistoryManager(b);
            a.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(50, 50)));
            await Task.Yield();

            Assert.AreEqual(1, b.ElementCount);
            Assert.IsFalse(historyB.CanUndo);
        }
    }

    [TestMethod]
    public async Task Undo_is_broadcast_as_an_ordinary_edit()
    {
        (BinkDocument a, BinkDocument b, BinkCollaborationSession sa, BinkCollaborationSession sb, _, _) = CreatePair();
        await using (sa)
        await using (sb)
        {
            using var historyA = new BinkHistoryManager(a);
            a.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(50, 50)));
            await Task.Yield();
            Assert.AreEqual(1, b.ElementCount);

            historyA.Undo();
            await Task.Yield();

            Assert.AreEqual(0, b.ElementCount);
        }
    }

    [TestMethod]
    public async Task Two_participants_drawing_at_once_keep_both_strokes()
    {
        (BinkDocument a, BinkDocument b, BinkCollaborationSession sa, BinkCollaborationSession sb, _, _) = CreatePair();
        await using (sa)
        await using (sb)
        {
            a.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(40, 40)));
            b.AddElement(TestData.Stroke(new Vector2(100, 100), new Vector2(140, 140)));
            await Task.Yield();

            Assert.AreEqual(2, a.ElementCount);
            Assert.AreEqual(2, b.ElementCount);
        }
    }

    [TestMethod]
    public async Task Editing_the_same_element_resolves_the_same_way_on_both_sides()
    {
        (BinkDocument a, BinkDocument b, BinkCollaborationSession sa, BinkCollaborationSession sb, TestTransport ta, TestTransport tb) = CreatePair();
        await using (sa)
        await using (sb)
        {
            BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(50, 50));
            a.AddElement(stroke);
            await Task.Yield();

            // Both edit the same element while disconnected, then reconnect.
            ta.SetConnected(false);
            tb.SetConnected(false);
            a.ReplaceElement(a.GetElement(stroke.Id) with { Description = "from alex" });
            b.ReplaceElement(b.GetElement(stroke.Id) with { Description = "from sam" });

            ta.SetConnected(true);
            tb.SetConnected(true);
            await sa.FlushAsync();
            await sb.FlushAsync();
            await Task.Yield();

            // Whatever the outcome, the replicas must agree.
            Assert.AreEqual(a.GetElement(stroke.Id).Description, b.GetElement(stroke.Id).Description);
        }
    }

    [TestMethod]
    public async Task Changes_made_offline_are_queued_and_delivered_on_reconnect()
    {
        (BinkDocument a, BinkDocument b, BinkCollaborationSession sa, BinkCollaborationSession sb, TestTransport ta, _) = CreatePair();
        await using (sa)
        await using (sb)
        {
            ta.SetConnected(false);
            for (int i = 0; i < 5; i++) a.AddElement(TestData.Stroke(new Vector2(i * 10, 0), new Vector2(i * 10 + 5, 20)));
            Assert.AreEqual(0, b.ElementCount);
            Assert.AreEqual(5, sa.PendingCount);

            ta.SetConnected(true);
            await sa.FlushAsync();
            await Task.Yield();

            Assert.AreEqual(5, b.ElementCount);
            Assert.IsTrue(sa.IsSynchronized);
        }
    }

    [TestMethod]
    public async Task A_snapshot_brings_a_joining_participant_up_to_date()
    {
        (BinkDocument a, BinkDocument b, BinkCollaborationSession sa, BinkCollaborationSession sb, TestTransport ta, _) = CreatePair();
        await using (sa)
        await using (sb)
        {
            ta.SetConnected(false);
            for (int i = 0; i < 4; i++) a.AddElement(TestData.Stroke(new Vector2(i * 20, 0), new Vector2(i * 20 + 10, 30)));
            ta.SetConnected(true);
            sa.ClearPending();

            await sb.RequestSnapshotAsync();
            await Task.Yield();

            Assert.AreEqual(4, b.ElementCount);
        }
    }

    [TestMethod]
    public async Task A_snapshot_that_cannot_be_applied_leaves_the_document_intact()
    {
        var hub = new TestHub();
        var local = new BinkDocument(new BinkDocumentSettings { Width = 200, Height = 200 }, BinkDocumentLimits.Default with { MaxElements = 3 });
        for (int i = 0; i < 3; i++) local.AddElement(TestData.Stroke(new Vector2(i * 10, 0), new Vector2(i * 10 + 5, 20)));

        TestTransport transport = hub.CreateClient();
        await using var session = new BinkCollaborationSession(local, transport, "local");

        // A peer sends a document with more elements than this participant allows.
        var remote = BinkSerializer.Deserialize(BinkSerializer.Serialize(local));
        var oversized = new BinkDocument(remote.Settings) { Id = remote.Id };
        for (int i = 0; i < 20; i++) oversized.AddElement(TestData.Stroke(new Vector2(i, 0), new Vector2(i + 1, 1)));

        TestTransport peer = hub.CreateClient();
        await peer.SendAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Snapshot,
            SenderId = "peer",
            DocumentId = local.Id,
            Sequence = 1,
            Snapshot = BinkSerializer.Serialize(oversized),
        });
        await Task.Yield();

        // The local drawing survives: a refused snapshot must not destroy it.
        Assert.AreEqual(3, local.ElementCount);
    }

    [TestMethod]
    public async Task Presence_is_reported_and_never_touches_the_document()
    {
        (BinkDocument a, BinkDocument b, BinkCollaborationSession sa, BinkCollaborationSession sb, _, _) = CreatePair();
        await using (sa)
        await using (sb)
        {
            BinkPresenceState? received = null;
            sb.PresenceChanged += p => received = p;

            await sa.UpdatePresenceAsync(new BinkPresenceState
            {
                ParticipantId = "alex",
                Name = "Alex",
                Cursor = new Vector2(30, 40),
                LiveStroke = [new Vector2(0, 0), new Vector2(10, 10)],
                LiveStrokeWidth = 4f,
            });
            await Task.Yield();

            Assert.IsNotNull(received);
            Assert.AreEqual("Alex", received!.Name);
            Assert.AreEqual(0, b.ElementCount);
            Assert.ContainsSingle(sb.Participants);
        }
    }

    [TestMethod]
    public async Task A_hostile_operation_is_rejected_without_disturbing_the_document()
    {
        var hub = new TestHub();
        var document = new BinkDocument();
        document.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10)));

        TestTransport transport = hub.CreateClient();
        await using var session = new BinkCollaborationSession(document, transport, "local");
        string? rejection = null;
        session.RemoteOperationRejected += reason => rejection = reason;

        TestTransport peer = hub.CreateClient();
        await peer.SendAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Operation,
            SenderId = "peer",
            DocumentId = document.Id,
            Sequence = 1,
            Operation = "{\"op\":\"add\",\"layer\":\"not-a-guid\"}",
        });
        await Task.Yield();

        Assert.AreEqual(1, document.ElementCount);
        Assert.IsNotNull(rejection);
    }

    [TestMethod]
    public async Task A_duplicate_message_is_ignored()
    {
        var hub = new TestHub();
        var document = new BinkDocument();
        TestTransport transport = hub.CreateClient();
        await using var session = new BinkCollaborationSession(document, transport, "local");

        TestTransport peer = hub.CreateClient();
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(20, 20));
        var message = new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Operation,
            SenderId = "peer",
            DocumentId = document.Id,
            Sequence = 1,
            Operation = BinkOperationCodec.Serialize(new BinkAddElementOperation(document.Layers[0].Id, stroke)),
        };

        await peer.SendAsync(message);
        await peer.SendAsync(message);
        await Task.Yield();

        Assert.AreEqual(1, document.ElementCount);
    }

    [TestMethod]
    public async Task A_participant_that_leaves_is_forgotten_and_can_rejoin_with_the_same_id()
    {
        var hub = new TestHub();
        var document = new BinkDocument();
        TestTransport transport = hub.CreateClient();
        await using var session = new BinkCollaborationSession(document, transport, "local");
        TestTransport peer = hub.CreateClient();

        await peer.SendAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Presence,
            SenderId = "peer",
            DocumentId = document.Id,
            Sequence = 7,
            Presence = new BinkPresenceState { ParticipantId = "peer", Name = "Peer" },
        });
        await Task.Yield();
        Assert.ContainsSingle(session.Participants);

        await peer.SendAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Leave,
            SenderId = "peer",
            DocumentId = document.Id,
            Sequence = 8,
        });
        await Task.Yield();
        Assert.IsEmpty(session.Participants);

        // Rejoining restarts at sequence 1. If the de-duplication cursor had survived the leave, every message
        // from the returning participant would be discarded as a retransmission and they would appear frozen.
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(20, 20));
        await peer.SendAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Operation,
            SenderId = "peer",
            DocumentId = document.Id,
            Sequence = 1,
            Operation = BinkOperationCodec.Serialize(new BinkAddElementOperation(document.Layers[0].Id, stroke)),
        });
        await Task.Yield();

        Assert.AreEqual(1, document.ElementCount);
    }

    [TestMethod]
    public async Task A_change_made_while_the_queue_is_draining_does_not_overtake_it()
    {
        var hub = new TestHub();
        var document = new BinkDocument();
        TestTransport transport = hub.CreateClient();
        var received = new List<BinkCollaborationMessage>();

        TestTransport peer = hub.CreateClient();
        peer.MessageReceived += m => { received.Add(m); return Task.CompletedTask; };

        await using var session = new BinkCollaborationSession(document, transport, "local");

        transport.SetConnected(false);
        document.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10)));
        document.AddElement(TestData.Stroke(new Vector2(20, 0), new Vector2(30, 10)));
        Assert.AreEqual(2, session.PendingCount);

        transport.SetConnected(true);
        document.AddElement(TestData.Stroke(new Vector2(40, 0), new Vector2(50, 10)));
        await session.FlushAsync();
        await Task.Yield();

        Assert.AreEqual(0, session.PendingCount);
        long[] sequences = received
            .Where(m => m.Kind == BinkCollaborationMessageKind.Operation)
            .Select(m => m.Sequence)
            .ToArray();
        Assert.AreEqual(3, sequences.Length);
        Assert.AreSequenceEqual(sequences.OrderBy(x => x).ToArray(), sequences);
    }
}

/// <summary>A transport whose messages the test delivers by hand, so concurrent edits can be interleaved exactly.</summary>
internal sealed class ManualTransport : IBinkCollaborationTransport
{
    public bool IsConnected { get; set; } = true;

    /// <summary>Makes the next operation send fail, as a dropped connection would.</summary>
    public bool FailNextOperation { get; set; }

    public List<BinkCollaborationMessage> Sent { get; } = new();

    public event Func<BinkCollaborationMessage, Task>? MessageReceived;

    public event Action<bool>? ConnectionChanged;

    public Task SendAsync(BinkCollaborationMessage message, CancellationToken cancellationToken = default)
    {
        if (FailNextOperation && message.Kind == BinkCollaborationMessageKind.Operation)
        {
            FailNextOperation = false;
            throw new InvalidOperationException("Connection dropped.");
        }
        Sent.Add(message);
        return Task.CompletedTask;
    }

    public Task DeliverAsync(BinkCollaborationMessage message) => MessageReceived?.Invoke(message) ?? Task.CompletedTask;

    public void RaiseConnectionChanged() => ConnectionChanged?.Invoke(IsConnected);
}

[TestClass]
public class CollaborationRegressionTests
{
    /// <summary>
    /// Three participants: alex and sam already share two strokes, sam has one more that alex has not seen, and zoe
    /// joins empty.
    /// </summary>
    private static (BinkDocument Alex, BinkDocument Sam, BinkDocument Zoe, TestHub Hub) CreateJoiningTrio()
    {
        var alex = new BinkDocument(new BinkDocumentSettings { Width = 400, Height = 300 });
        BinkDocument zoe = BinkSerializer.Deserialize(BinkSerializer.Serialize(alex));
        alex.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(20, 20)));
        alex.AddElement(TestData.Stroke(new Vector2(40, 0), new Vector2(60, 20)));
        BinkDocument sam = BinkSerializer.Deserialize(BinkSerializer.Serialize(alex));
        sam.AddElement(TestData.Stroke(new Vector2(80, 0), new Vector2(100, 20)));
        return (alex, sam, zoe, new TestHub());
    }

    [TestMethod]
    public async Task A_snapshot_answer_is_applied_only_by_the_participant_that_asked_and_only_once()
    {
        (BinkDocument alex, BinkDocument sam, BinkDocument zoe, TestHub hub) = CreateJoiningTrio();
        await using var sa = new BinkCollaborationSession(alex, hub.CreateClient(), "alex");
        await using var ss = new BinkCollaborationSession(sam, hub.CreateClient(), "sam");
        await using var sz = new BinkCollaborationSession(zoe, hub.CreateClient(), "zoe");

        // Both alex and sam answer. Every participant used to apply every answer: sam took alex's copy and lost
        // the stroke only sam had, and zoe applied both answers, the second overwriting the first.
        await sz.RequestSnapshotAsync();
        await Task.Yield();

        Assert.AreEqual(2, alex.ElementCount);
        Assert.AreEqual(3, sam.ElementCount);
        // Alex answered first; that answer is the one zoe kept.
        Assert.AreSequenceEqual(alex.AllElements.Select(e => e.Id), zoe.AllElements.Select(e => e.Id));
    }

    [TestMethod]
    public async Task A_snapshot_sent_to_everyone_still_resets_every_participant()
    {
        (BinkDocument alex, BinkDocument sam, BinkDocument zoe, TestHub hub) = CreateJoiningTrio();
        await using var sa = new BinkCollaborationSession(alex, hub.CreateClient(), "alex");
        await using var ss = new BinkCollaborationSession(sam, hub.CreateClient(), "sam");
        await using var sz = new BinkCollaborationSession(zoe, hub.CreateClient(), "zoe");

        await sa.SendSnapshotAsync();
        await Task.Yield();

        Assert.AreSequenceEqual(alex.AllElements.Select(e => e.Id), sam.AllElements.Select(e => e.Id));
        Assert.AreSequenceEqual(alex.AllElements.Select(e => e.Id), zoe.AllElements.Select(e => e.Id));
    }

    [TestMethod]
    public async Task An_answer_nobody_asked_for_is_ignored()
    {
        var local = new BinkDocument(new BinkDocumentSettings { Width = 200, Height = 200 });
        local.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(20, 20)));
        var other = new BinkDocument(local.Settings) { Id = local.Id };
        var transport = new ManualTransport();
        await using var session = new BinkCollaborationSession(local, transport, "local");

        await transport.DeliverAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Snapshot,
            SenderId = "peer",
            RecipientId = "local",
            DocumentId = local.Id,
            Sequence = 1,
            Snapshot = BinkSerializer.Serialize(other),
        });

        Assert.AreEqual(1, local.ElementCount);
    }

    [TestMethod]
    public async Task Three_participants_agree_on_the_winner_of_a_tied_edit()
    {
        // The tie used to be settled between the receiving replica's own id and the sender's. A bystander holding
        // alex's version then judged sam's version against "zoe", kept alex's and disagreed with alex and sam,
        // who had both settled on sam's.
        var a = new BinkDocument();
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(50, 50));
        a.AddElement(stroke);
        BinkDocument b = BinkSerializer.Deserialize(BinkSerializer.Serialize(a));
        BinkDocument c = BinkSerializer.Deserialize(BinkSerializer.Serialize(a));

        ManualTransport ta = new(), tb = new(), tc = new();
        await using var sa = new BinkCollaborationSession(a, ta, "alex");
        await using var sb = new BinkCollaborationSession(b, tb, "sam");
        await using var sc = new BinkCollaborationSession(c, tc, "zoe");

        a.ReplaceElement(a.GetElement(stroke.Id) with { Description = "from alex" });
        b.ReplaceElement(b.GetElement(stroke.Id) with { Description = "from sam" });
        BinkCollaborationMessage fromAlex = Assert.ContainsSingle(ta.Sent);
        BinkCollaborationMessage fromSam = Assert.ContainsSingle(tb.Sent);

        await tb.DeliverAsync(fromAlex);
        await tc.DeliverAsync(fromAlex);
        await ta.DeliverAsync(fromSam);
        await tc.DeliverAsync(fromSam);

        string? winner = a.GetElement(stroke.Id).Description;
        Assert.AreEqual(winner, b.GetElement(stroke.Id).Description);
        Assert.AreEqual(winner, c.GetElement(stroke.Id).Description);

        // The replica that took the winning edit stores it at the version it was sent with. Bumping it left that
        // replica a version ahead of the others, so the next edit from anyone was ordered differently there.
        Assert.AreEqual(a.GetElement(stroke.Id).Version, b.GetElement(stroke.Id).Version);
        Assert.AreEqual(a.GetElement(stroke.Id).Version, c.GetElement(stroke.Id).Version);
    }

    [TestMethod]
    public async Task Presence_does_not_overtake_queued_operations()
    {
        // Presence and operations share the sender's sequence. A presence update sent past a queued operation
        // made the receiver discard that operation as a late arrival once the queue drained.
        var local = new BinkDocument();
        BinkDocument remote = BinkSerializer.Deserialize(BinkSerializer.Serialize(local));
        var transport = new ManualTransport { FailNextOperation = true };
        var peer = new ManualTransport();
        await using var session = new BinkCollaborationSession(local, transport, "local");
        await using var receiver = new BinkCollaborationSession(remote, peer, "remote");

        local.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(20, 20)));
        Assert.AreEqual(1, session.PendingCount);

        await session.UpdatePresenceAsync(new BinkPresenceState { ParticipantId = "local", Cursor = new Vector2(5, 5) });
        await session.FlushAsync();
        Assert.AreEqual(0, session.PendingCount);

        foreach (BinkCollaborationMessage message in transport.Sent.ToArray()) await peer.DeliverAsync(message);

        Assert.AreEqual(1, remote.ElementCount);
    }

    [TestMethod]
    public async Task A_snapshot_brings_settings_metadata_and_layer_order_along()
    {
        // Only layers and elements were synchronized: a joining participant kept its own page and metadata, and
        // layers it already had stayed in its own order.
        var a = new BinkDocument(new BinkDocumentSettings { Width = 400, Height = 300 });
        BinkLayer top = a.AddLayer("Top");
        BinkDocument b = BinkSerializer.Deserialize(BinkSerializer.Serialize(a));
        a.ReorderLayer(top.Id, 0);
        a.UpdateSettings(new BinkDocumentSettings { Width = 1000, Height = 700, Background = BinkColor.Parse("#123456") });
        a.SetMetadata("case", "42");
        b.SetMetadata("stale", "x");

        var hub = new TestHub();
        await using var sa = new BinkCollaborationSession(a, hub.CreateClient(), "alex");
        await using var sb = new BinkCollaborationSession(b, hub.CreateClient(), "sam");
        await sb.RequestSnapshotAsync();
        await Task.Yield();

        Assert.AreSequenceEqual(a.Layers.Select(l => l.Id), b.Layers.Select(l => l.Id));
        Assert.AreEqual(a.Settings, b.Settings);
        Assert.AreEqual("42", b.Metadata["case"]);
        Assert.IsFalse(b.Metadata.ContainsKey("stale"));
    }
}
