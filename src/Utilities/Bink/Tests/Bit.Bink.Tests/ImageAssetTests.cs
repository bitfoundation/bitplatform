using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>Embedded images written once and referred to: in files, and between collaborators.</summary>
[TestClass]
public sealed class ImageAssetTests
{
    private static BinkImageSource Photo(byte fill = 7, int bytes = 30_000)
    {
        var data = new byte[bytes];
        Array.Fill(data, fill);
        return BinkImageSource.FromBytes(data, "image/png", 64, 48);
    }

    private static int Occurrences(string text, string part)
    {
        int count = 0;
        for (int at = text.IndexOf(part, StringComparison.Ordinal); at >= 0; at = text.IndexOf(part, at + part.Length, StringComparison.Ordinal)) count++;
        return count;
    }

    /// <summary>The base64 payload of a data URI, which is what must not be repeated.</summary>
    private static string Payload(BinkImageSource source) => source.Uri[(source.Uri.IndexOf(',', StringComparison.Ordinal) + 1)..];

    [TestMethod]
    public void An_image_shown_twice_is_saved_once_and_shared_again_on_load()
    {
        var document = new BinkDocument();
        BinkImageSource photo = Photo();
        document.AddElement(new BinkImageElement(photo, new BinkRect(0, 0, 64, 48)));
        document.AddElement(new BinkImageElement(photo, new BinkRect(100, 0, 64, 48)));
        document.AddElement(new BinkImageElement(Photo(fill: 9), new BinkRect(200, 0, 64, 48)));

        string json = BinkSerializer.Serialize(document);

        Assert.AreEqual(1, Occurrences(json, Payload(photo)), "Written once, in the asset table.");
        StringAssert.Contains(json, "\"version\":2");
        StringAssert.Contains(json, "\"assets\"");
        BinkImageElement[] loaded = [.. BinkSerializer.Deserialize(json).AllElements.OfType<BinkImageElement>()];
        Assert.HasCount(3, loaded);
        Assert.AreSame(loaded[0].Source, loaded[1].Source, "The two elements share one picture in memory again.");
        Assert.AreEqual(photo.Uri, loaded[0].Source.Uri);
        Assert.AreEqual(new BinkRect(100, 0, 64, 48), loaded[1].Rect);
        Assert.AreEqual(Photo(fill: 9).Uri, loaded[2].Source.Uri, "A picture shown once stays in its element.");
    }

    [TestMethod]
    public void Without_shared_images_a_drawing_is_still_written_for_earlier_versions()
    {
        var document = new BinkDocument();
        document.AddElement(new BinkImageElement(Photo(), new BinkRect(0, 0, 64, 48)));
        string json = BinkSerializer.Serialize(document);
        StringAssert.Contains(json, "\"version\":1");
        Assert.DoesNotContain("\"assets\"", json);

        // Turned off, every element carries its own picture again, as before the table existed.
        document.AddElement(new BinkImageElement(document.AllElements.OfType<BinkImageElement>().Single().Source, new BinkRect(100, 0, 64, 48)));
        string separate = BinkSerializer.Serialize(document, new BinkSerializerOptions { ShareImageData = false });
        StringAssert.Contains(separate, "\"version\":1");
        Assert.AreEqual(2, Occurrences(separate, Payload(Photo())));
    }

    [TestMethod]
    public void A_copy_of_two_images_of_one_photo_carries_it_once()
    {
        var document = new BinkDocument();
        BinkImageSource photo = Photo();
        var first = new BinkImageElement(photo, new BinkRect(0, 0, 64, 48));
        document.AddElement(first);
        document.DuplicateElements([first.Id], new Vector2(80, 0));

        string payload = BinkClipboard.Serialize(document.AllElements);

        Assert.AreEqual(1, Occurrences(payload, Payload(photo)));
        Assert.IsTrue(BinkClipboard.TryDeserialize(payload, out IReadOnlyList<BinkElement> pasted, out _));
        Assert.AreSame(((BinkImageElement)pasted[0]).Source, ((BinkImageElement)pasted[1]).Source);
    }

    [TestMethod]
    public void A_hostile_asset_table_is_refused_entry_by_entry()
    {
        string png = Photo().Uri;
        string json = $$"""
            {"format":"bink","version":2,"assets":{"good":"{{png}}","script":"javascript:alert(1)","remote":"https://example.com/a.png","number":5},
             "layers":[{"elements":[
               {"type":"image","rect":[0,0,10,10],"asset":"good"},
               {"type":"image","rect":[0,0,10,10],"asset":"script"},
               {"type":"image","rect":[0,0,10,10],"asset":"remote"},
               {"type":"image","rect":[0,0,10,10],"asset":"number"},
               {"type":"image","rect":[0,0,10,10],"asset":"missing"}]}]}
            """;

        BinkDocument document = BinkSerializer.Deserialize(json);

        Assert.AreEqual(1, document.ElementCount, "Only the element whose asset is an embedded raster picture is kept.");
        var limits = new BinkDeserializerOptions { Limits = new BinkDocumentLimits { MaxElements = 2 } };
        Assert.IsFalse(BinkSerializer.TryDeserialize(json, out _, out string? error, limits));
        Assert.Contains("assets", error!);
        Assert.IsFalse(BinkSerializer.TryDeserialize("""{"format":"bink","version":3,"layers":[]}""", out _, out error));
        Assert.Contains("newer version", error!);
    }

    // ---- Collaboration ---------------------------------------------------------------------------------------------

    /// <summary>Two participants who have heard from each other, as any session does within moments of joining.</summary>
    private static async Task<(BinkDocument Alex, BinkDocument Sam, TestTransport AlexWire)> AcquaintedPair()
    {
        var hub = new TestHub();
        var alex = new BinkDocument();
        // The same document, as a participant gets it by joining: its layers have the same ids.
        BinkDocument sam = BinkSerializer.Deserialize(BinkSerializer.Serialize(alex));
        TestTransport alexWire = hub.CreateClient();
        var alexSession = new BinkCollaborationSession(alex, alexWire, "alex");
        var samSession = new BinkCollaborationSession(sam, hub.CreateClient(), "sam");
        await samSession.UpdatePresenceAsync(new BinkPresenceState { ParticipantId = "sam" });
        await alexSession.UpdatePresenceAsync(new BinkPresenceState { ParticipantId = "alex" });
        return (alex, sam, alexWire);
    }

    private static IEnumerable<string> Operations(TestTransport wire)
        => wire.Sent.Where(m => m.Kind == BinkCollaborationMessageKind.Operation).Select(m => m.Operation!);

    [TestMethod]
    public async Task Dragging_an_image_sends_its_picture_once()
    {
        (BinkDocument alex, BinkDocument sam, TestTransport wire) = await AcquaintedPair();
        BinkImageSource photo = Photo();
        var image = new BinkImageElement(photo, new BinkRect(0, 0, 64, 48));
        alex.AddElement(image);
        for (int i = 1; i <= 10; i++) alex.ReplaceElement(alex.GetElement(image.Id) with { Transform = Matrix3x2.CreateTranslation(i * 5, 0) });

        string[] sent = [.. Operations(wire)];
        Assert.HasCount(11, sent);
        Assert.AreEqual(1, sent.Sum(op => Occurrences(op, Payload(photo))), "Only the first change carries the picture.");
        Assert.IsTrue(sent.Skip(1).All(op => op.Length < 1_000), "Each frame of the drag is small.");

        var received = (BinkImageElement)sam.GetElement(image.Id);
        Assert.AreEqual(photo.Uri, received.Source.Uri);
        Assert.AreEqual(50f, received.Transform.M31);
    }

    [TestMethod]
    public async Task Copies_of_one_photo_in_one_change_carry_it_once()
    {
        (BinkDocument alex, BinkDocument sam, TestTransport wire) = await AcquaintedPair();
        BinkImageSource photo = Photo();
        var first = new BinkImageElement(photo, new BinkRect(0, 0, 64, 48));
        var second = new BinkImageElement(photo, new BinkRect(100, 0, 64, 48));
        var third = new BinkImageElement(photo, new BinkRect(200, 0, 64, 48));
        Guid layer = alex.Layers[0].Id;

        alex.Apply(new BinkBatchOperation([new BinkAddElementOperation(layer, first), new BinkAddElementOperation(layer, second), new BinkAddElementOperation(layer, third)], "Paste"));

        Assert.AreEqual(1, Occurrences(Operations(wire).Single(), Payload(photo)));
        Assert.AreEqual(3, sam.AllElements.OfType<BinkImageElement>().Count(e => e.Source.Uri == photo.Uri));
    }

    [TestMethod]
    public async Task With_a_participant_from_before_references_the_picture_is_sent_every_time()
    {
        var hub = new TestHub();
        var alex = new BinkDocument();
        TestTransport alexWire = hub.CreateClient();
        await using var alexSession = new BinkCollaborationSession(alex, alexWire, "alex");
        // A participant of an earlier version: its messages carry no protocol, which reads as 0.
        TestTransport oldWire = hub.CreateClient();
        await oldWire.SendAsync(new BinkCollaborationMessage { Kind = BinkCollaborationMessageKind.Presence, SenderId = "old", Sequence = 1, Presence = new BinkPresenceState { ParticipantId = "old" } });

        BinkImageSource photo = Photo();
        var image = new BinkImageElement(photo, new BinkRect(0, 0, 64, 48));
        alex.AddElement(image);
        alex.ReplaceElement(image with { Transform = Matrix3x2.CreateTranslation(5, 0) });

        Assert.IsTrue(Operations(alexWire).All(op => Occurrences(op, Payload(photo)) == 1), "Every change carries the picture it shows.");

        // Joining, it gets a document it can open: no asset table, even though two elements show one photo now.
        alex.AddElement(new BinkImageElement(photo, new BinkRect(100, 0, 64, 48)));
        await oldWire.SendAsync(new BinkCollaborationMessage { Kind = BinkCollaborationMessageKind.SnapshotRequest, SenderId = "old", Sequence = 2 });
        string snapshot = alexWire.Sent.Last(m => m.Kind == BinkCollaborationMessageKind.Snapshot).Snapshot!;
        StringAssert.Contains(snapshot, "\"version\":1");
        Assert.DoesNotContain("\"assets\"", snapshot);
    }

    [TestMethod]
    public async Task A_change_showing_an_image_this_replica_lacks_waits_for_a_snapshot_that_brings_it()
    {
        (BinkDocument alex, _, TestTransport wire) = await AcquaintedPair();
        var image = new BinkImageElement(Photo(), new BinkRect(0, 0, 64, 48));
        alex.AddElement(image);
        alex.ReplaceElement(image with { Transform = Matrix3x2.CreateTranslation(30, 0) });
        BinkCollaborationMessage move = wire.Sent.Last(m => m.Kind == BinkCollaborationMessageKind.Operation);
        Assert.DoesNotContain("data:", move.Operation!, "The move refers to the picture.");

        // Zoe missed the change that carried the picture, then receives the move.
        var zoe = new BinkDocument();
        var zoeWire = new ManualTransport();
        await using var zoeSession = new BinkCollaborationSession(zoe, zoeWire, "zoe");
        string? rejected = null;
        zoeSession.RemoteOperationRejected += reason => rejected = reason;
        await zoeWire.DeliverAsync(move);

        Assert.IsNull(rejected, "The change is not malformed; it waits.");
        Assert.Contains(m => m.Kind == BinkCollaborationMessageKind.SnapshotRequest, zoeWire.Sent);

        await zoeWire.DeliverAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Snapshot,
            SenderId = "alex",
            RecipientId = "zoe",
            Sequence = move.Sequence + 1,
            Protocol = 1,
            Snapshot = BinkSerializer.Serialize(alex),
        });

        var received = (BinkImageElement)zoe.GetElement(image.Id);
        Assert.AreEqual(image.Source.Uri, received.Source.Uri);
        Assert.AreEqual(30f, received.Transform.M31);
        Assert.IsNull(rejected);
    }

    [TestMethod]
    public async Task References_to_pictures_that_do_not_exist_cannot_make_everyone_send_the_document_again_and_again()
    {
        var zoe = new BinkDocument();
        var wire = new ManualTransport();
        var clock = new ManualClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        await using var session = new BinkCollaborationSession(zoe, wire, "zoe") { Clock = clock };
        var rejected = new List<string>();
        session.RemoteOperationRejected += rejected.Add;
        long sequence = 1;
        Task Bogus() => wire.DeliverAsync(new BinkCollaborationMessage
        {
            Kind = BinkCollaborationMessageKind.Operation,
            SenderId = "mallory",
            Sequence = sequence++,
            Protocol = 1,
            Operation = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                """{{"op":"add","layer":"{0}","element":{{"type":"image","id":"{1}","rect":[0,0,10,10],"asset":"{2}"}}}}""",
                zoe.Layers[0].Id, Guid.NewGuid(), new string('f', 32)),
        });
        int Requests() => wire.Sent.Count(m => m.Kind == BinkCollaborationMessageKind.SnapshotRequest);

        await Bogus();
        Assert.AreEqual(1, Requests());
        // The answer does not bring the picture, because there is none; the change is reported and dropped.
        await wire.DeliverAsync(new BinkCollaborationMessage { Kind = BinkCollaborationMessageKind.Snapshot, SenderId = "sam", RecipientId = "zoe", Sequence = 1, Protocol = 1, Snapshot = BinkSerializer.Serialize(zoe) });
        Assert.HasCount(1, rejected);

        for (int i = 0; i < 5; i++)
        {
            clock.Now += TimeSpan.FromSeconds(1);
            await Bogus();
        }
        Assert.AreEqual(1, Requests(), "Within the interval, no further snapshot is asked for.");

        clock.Now += TimeSpan.FromSeconds(10);
        await Bogus();
        Assert.AreEqual(2, Requests(), "After it, the changes still waiting get one.");
        Assert.AreEqual(0, zoe.ElementCount);
    }
}
