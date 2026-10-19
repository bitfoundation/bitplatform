using Microsoft.Extensions.Time.Testing;

namespace Bit.Bcrdt.Tests;

/// <summary>
/// Presence has different rules from document state, and the differences are the point.
/// </summary>
/// <remarks>
/// Awareness is last-writer-wins per client, expires on silence, and is never persisted. Each of
/// those is a deliberate departure from how the document behaves, and each is tested here.
/// </remarks>
[TestClass]
public class AwarenessTests
{
    private static Dictionary<string, object?> State(params (string Key, object? Value)[] fields)
    {
        var state = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach ((string key, object? value) in fields) state[key] = value;
        return state;
    }

    [TestMethod]
    public void Local_state_is_published_and_readable()
    {
        using var awareness = new BcrdtAwarenessStore(1);
        awareness.SetLocalState(State(("name", "Ada")));

        Assert.AreEqual("Ada", awareness.GetField(1, "name"));
        Assert.AreEqual(1, awareness.OnlineCount);
    }

    [TestMethod]
    public void An_update_from_a_peer_adds_them_to_the_participant_list()
    {
        using var a = new BcrdtAwarenessStore(1);
        using var b = new BcrdtAwarenessStore(2);

        a.SetLocalState(State(("name", "Ada")));
        b.SetLocalState(State(("name", "Grace")));
        a.ApplyUpdate(b.EncodeAll());

        Assert.AreEqual(2, a.OnlineCount);
        Assert.AreEqual("Grace", a.GetField(2, "name"));
    }

    [TestMethod]
    public void Redelivering_an_awareness_update_raises_no_further_change()
    {
        using var a = new BcrdtAwarenessStore(1);
        using var b = new BcrdtAwarenessStore(2);

        b.SetLocalState(State(("name", "Grace")));
        byte[] update = b.EncodeAll();

        int changes = 0;
        a.Changed += _ => changes++;

        a.ApplyUpdate(update);
        a.ApplyUpdate(update);
        a.ApplyUpdate(update);

        Assert.AreEqual(1, changes);
    }

    [TestMethod]
    public void A_stale_update_is_ignored_in_favour_of_newer_state()
    {
        using var a = new BcrdtAwarenessStore(1);
        using var b = new BcrdtAwarenessStore(2);

        b.SetLocalState(State(("cursor", 1)));
        byte[] stale = b.EncodeAll();

        b.SetLocalState(State(("cursor", 2)));
        a.ApplyUpdate(b.EncodeAll());

        // Reordered delivery must not resurrect the earlier cursor position.
        a.ApplyUpdate(stale);
        Assert.AreEqual(2L, a.GetField(2, "cursor"));
    }

    [TestMethod]
    public void Setting_null_state_reports_the_client_as_gone()
    {
        using var a = new BcrdtAwarenessStore(1);
        using var b = new BcrdtAwarenessStore(2);

        a.SetLocalState(State(("name", "Ada")));
        b.SetLocalState(State(("name", "Grace")));
        a.ApplyUpdate(b.EncodeAll());

        List<ulong> removed = [];
        a.Changed += args => removed.AddRange(args.Removed);

        b.SetLocalState(null);
        a.ApplyUpdate(b.EncodeAll());

        Assert.Contains(2UL, removed);
        Assert.AreEqual(1, a.OnlineCount);
    }

    [TestMethod]
    public void A_client_that_stops_refreshing_is_dropped_after_the_timeout()
    {
        var time = new FakeTimeProvider();
        using var a = new BcrdtAwarenessStore(1, TimeSpan.FromSeconds(30), time);
        using var b = new BcrdtAwarenessStore(2);

        a.SetLocalState(State(("name", "me")));
        b.SetLocalState(State(("name", "them")));
        a.ApplyUpdate(b.EncodeAll());
        Assert.AreEqual(2, a.OnlineCount);

        List<ulong> removed = [];
        a.Changed += args => removed.AddRange(args.Removed);

        // The peer's browser tab was closed without a chance to say goodbye. Nothing announces that;
        // the only evidence is that it stopped refreshing.
        time.Advance(TimeSpan.FromSeconds(31));
        a.Heartbeat();

        Assert.Contains(2UL, removed);
        Assert.AreEqual(1, a.OnlineCount);
    }

    [TestMethod]
    public void A_client_that_keeps_refreshing_is_not_dropped()
    {
        var time = new FakeTimeProvider();
        using var a = new BcrdtAwarenessStore(1, TimeSpan.FromSeconds(30), time);
        using var b = new BcrdtAwarenessStore(2);

        b.SetLocalState(State(("name", "them")));

        // The peer keeps announcing itself, which is all that stands between it and expiry.
        for (int tick = 0; tick < 10; tick++)
        {
            time.Advance(TimeSpan.FromSeconds(10));
            a.ApplyUpdate(b.EncodeUpdate([2]));
            a.Heartbeat();
        }

        Assert.AreEqual(1, a.OnlineCount);
        Assert.IsTrue(a.Entries[2].State is not null);
    }

    [TestMethod]
    public void An_impostor_using_our_client_id_does_not_overwrite_our_state()
    {
        using var mine = new BcrdtAwarenessStore(1);
        using var impostor = new BcrdtAwarenessStore(1);

        mine.SetLocalState(State(("name", "real")));
        impostor.SetLocalState(State(("name", "fake")));
        impostor.SetLocalState(State(("name", "fake")));

        mine.ApplyUpdate(impostor.EncodeAll());

        Assert.AreEqual("real", mine.GetField(1, "name"));
    }

    [TestMethod]
    public void Presence_exposes_users_cursors_and_activity()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var awareness = new BcrdtAwarenessStore(1);
        using var presence = new BcrdtPresenceService(awareness, doc);

        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello world");

        presence.SetUser(new BcrdtCollaboratorInfo("u1", "Ada", "#ff0000"));
        presence.SetCaret("body", text, 6);
        presence.SetActivity("typing");

        BcrdtCollaborator me = Assert.ContainsSingle(presence.Participants);
        Assert.AreEqual("Ada", me.User.Name);
        Assert.AreEqual("typing", me.Activity);
        Assert.IsNotNull(me.Selection);
        Assert.AreEqual((6, 6), me.Selection!.Resolve(doc));
    }

    [TestMethod]
    public void A_remote_cursor_stays_on_its_character_when_text_is_inserted_before_it()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var awareness = new BcrdtAwarenessStore(1);
        using var presence = new BcrdtPresenceService(awareness, doc);

        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello world");

        presence.SetUser(new BcrdtCollaboratorInfo("u1", "Ada", "#ff0000"));
        presence.SetCaret("body", text, 6);

        // Somebody types at the start. An index-based cursor would now be pointing six characters
        // into the wrong word.
        text.Insert(0, ">>>> ");

        BcrdtCollaborator me = Assert.ContainsSingle(presence.Participants);
        (int Start, int End)? resolved = me.Selection!.Resolve(doc);
        Assert.AreEqual(11, resolved!.Value.Start);
        Assert.AreEqual('w', text.ToString()[resolved.Value.Start]);
    }

    [TestMethod]
    public void A_cursor_resolves_sensibly_when_its_anchor_is_deleted()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var awareness = new BcrdtAwarenessStore(1);
        using var presence = new BcrdtPresenceService(awareness, doc);

        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello world");
        presence.SetUser(new BcrdtCollaboratorInfo("u1", "Ada", "#ff0000"));
        presence.SetCaret("body", text, 6);

        text.Delete(5, 6);

        BcrdtCollaborator me = Assert.ContainsSingle(presence.Participants);
        (int Start, int End)? resolved = me.Selection!.Resolve(doc);

        Assert.IsNotNull(resolved);
        Assert.IsInRange(0, text.Count, resolved!.Value.Start);
    }

    [TestMethod]
    public void A_cursor_anchored_to_content_we_have_not_received_resolves_to_nothing()
    {
        using var author = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var observer = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });

        BcrdtText text = author.GetText("body");
        text.Insert(0, "hello");

        BcrdtRelativePosition position = BcrdtRelativePosition.FromIndex(text, 3);

        // The observer has not yet received the text this cursor is anchored to. Reporting a
        // position anyway would draw somebody's cursor at a place that does not exist.
        Assert.IsNull(position.Resolve(observer));

        observer.ApplyUpdate(author.EncodeStateAsUpdate());
        Assert.AreEqual(3, position.Resolve(observer)!.Value.Index);
    }

    [TestMethod]
    public void Relative_positions_round_trip_through_their_encoding()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello world");

        foreach (int index in new[] { 0, 1, 5, 11 })
        {
            BcrdtRelativePosition original = BcrdtRelativePosition.FromIndex(text, index);
            BcrdtRelativePosition decoded = BcrdtRelativePosition.Decode(original.Encode());

            Assert.AreEqual(original.Resolve(doc)?.Index, decoded.Resolve(doc)?.Index);
        }
    }

    [TestMethod]
    public void Suggested_colours_are_stable_for_a_user_and_spread_across_the_palette()
    {
        Assert.AreEqual(BcrdtPresenceService.SuggestColor("alice"), BcrdtPresenceService.SuggestColor("alice"));

        var colours = Enumerable.Range(0, 50)
            .Select(i => BcrdtPresenceService.SuggestColor($"user-{i}"))
            .Distinct()
            .Count();

        Assert.IsTrue(colours >= 5, $"Only {colours} distinct colours across 50 users; the palette is not spreading.");
    }
}
