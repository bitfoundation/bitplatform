using System.Buffers.Binary;
using System.Collections.Concurrent;
using Microsoft.Extensions.Time.Testing;

namespace Bit.Bcrdt.Tests;

/// <summary>
/// Regression tests for defects found by review in the application layers: adapters, awareness,
/// collaboration, persistence and the text-control bridge.
/// </summary>
[TestClass]
public class AppLayerRegressionTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "bit-bcrdt-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string because)
    {
        DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) Assert.Fail($"Timed out waiting until {because}.");
            await Task.Delay(10);
        }
    }

    private static Dictionary<string, object?> State(params (string Key, object? Value)[] fields)
    {
        var state = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach ((string key, object? value) in fields) state[key] = value;
        return state;
    }

    // ---------------------------------------------------------------- BcrdtTextSynchronizer

    [TestMethod]
    [DataRow("x😀", "xa😀", 1, 0, "a")]               // typing before an emoji
    [DataRow("ab😀", "a😀", 1, 1, "")]                // backspacing before an emoji
    [DataRow("😀", "🈀", 0, 2, "🈀")]       // same low surrogate, new high
    [DataRow("😀", "😁", 0, 2, "😁")]       // same high surrogate, new low
    public void The_diff_never_splits_a_surrogate_pair(string current, string desired, int index, int deleted, string inserted)
    {
        BcrdtTextEdit edit = BcrdtTextSynchronizer.Diff(current, desired);

        Assert.AreEqual(new BcrdtTextEdit(index, deleted, inserted), edit);
    }

    [TestMethod]
    public void Typing_before_an_emoji_does_not_leave_half_of_it_behind_a_concurrent_delete()
    {
        using var a = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var b = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        a.GetText("t").Insert(0, "x😀");
        b.ApplyUpdate(a.EncodeStateAsUpdate());

        BcrdtTextSynchronizer.Apply(a.GetText("t"), "xa😀");
        b.GetText("t").Delete(1, 2);

        a.ApplyUpdate(b.EncodeStateAsUpdate(a.GetStateVector()));
        b.ApplyUpdate(a.EncodeStateAsUpdate(b.GetStateVector()));

        Assert.AreEqual("xa", a.GetText("t").ToString());
        BcrdtConvergenceHarness.AssertConverged(a, b);
    }

    [TestMethod]
    [DataRow("aaa", "aaaa", 2, 1, 0, "a")]   // typed after the first character
    [DataRow("aaa", "aa", 1, 1, 1, "")]      // backspaced the second character
    [DataRow("abcd", "acd", 1, 1, 1, "")]    // forward-deleted 'b'
    [DataRow("abcd", "aXd", 2, 1, 2, "X")]   // replaced a selection
    [DataRow("abc", "abXc", 0, 2, 0, "X")]   // an inconsistent hint is ignored
    public void The_caret_resolves_an_ambiguous_diff(
        string current, string desired, int caret, int index, int deleted, string inserted)
    {
        Assert.AreEqual(new BcrdtTextEdit(index, deleted, inserted), BcrdtTextSynchronizer.Diff(current, desired, caret));
    }

    [TestMethod]
    public void Applying_a_value_to_text_with_an_embed_edits_the_right_characters_and_keeps_the_embed()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("t");
        text.Insert(0, "abcdef");
        text.InsertEmbed(0, new Dictionary<string, object?> { ["image"] = "x.png" });

        BcrdtTextSynchronizer.Apply(text, "abcXdef");

        Assert.AreEqual("abcXdef", text.ToString());
        Assert.AreEqual(8, text.Count);
        Assert.IsNotExactInstanceOfType<string>(text.ToDelta()[0].Content);
    }

    [TestMethod]
    public void A_projection_presents_line_endings_as_a_textarea_does_and_maps_positions_back()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("t");
        text.Insert(0, "a\r\nb\rc");

        BcrdtTextProjection projection = BcrdtTextProjection.Create(text, normalizeLineEndings: true);

        Assert.AreEqual("a\nb\nc", projection.Display);
        Assert.AreEqual(3, projection.ToDocument(2));
        Assert.AreEqual(2, projection.ToDisplay(3));
        Assert.AreEqual((1, 2), Assert.ContainsSingle(projection.ToDocumentRanges(1, 2)));
        Assert.AreEqual("a\r\nb\rc", BcrdtTextProjection.Create(text).Display);
    }

    // ---------------------------------------------------------------- BcrdtTextInputBinding

    [TestMethod]
    public void Input_computed_against_an_unrendered_remote_change_does_not_erase_it()
    {
        using var a = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var b = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        BcrdtText text = a.GetText("body");
        text.Insert(0, "hello");
        b.ApplyUpdate(a.EncodeStateAsUpdate());

        using var binding = new BcrdtTextInputBinding(text);

        // A collaborator's insertion reaches the document before the control has been updated.
        b.GetText("body").Insert(0, "X");
        a.ApplyUpdate(b.EncodeStateAsUpdate(a.GetStateVector()));
        Assert.IsTrue(binding.HasPendingChanges);
        Assert.AreEqual("hello", binding.Value);

        // The user, still looking at "hello", types "!" at the end.
        binding.ApplyInput("hello!", 6, 6);

        Assert.AreEqual("Xhello!", text.ToString());

        BcrdtTextControlUpdate update = binding.CreateUpdate();
        Assert.AreEqual("hello!", update.Expected);
        Assert.AreEqual("Xhello!", update.Value);
        Assert.AreEqual((7, 7), (update.SelectionStart, update.SelectionEnd));

        binding.Acknowledge(update);
        Assert.IsFalse(binding.HasPendingChanges);

        binding.ApplyInput("Xhello!?", 8, 8);
        Assert.AreEqual("Xhello!?", text.ToString());
    }

    [TestMethod]
    public void Editing_text_a_collaborator_already_deleted_does_not_fail_or_touch_their_work()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello world");
        using var binding = new BcrdtTextInputBinding(text);

        text.Delete(6, 5);   // somebody else removed "world"

        // The control still shows "world"; the user retypes its first letter.
        binding.ApplyInput("hello World", 7, 7);

        Assert.AreEqual("hello W", text.ToString());
    }

    [TestMethod]
    public void A_keystroke_in_text_with_windows_line_endings_is_a_one_character_edit()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "line1\r\nline2\r\nline3");
        using var binding = new BcrdtTextInputBinding(text);
        Assert.AreEqual("line1\nline2\nline3", binding.Value);

        List<BcrdtDeltaOperation> delta = [];
        using IDisposable subscription = text.Observe((e, _) => delta.AddRange(((BcrdtTextEvent)e).Delta));

        binding.ApplyInput("line1\nline2\nline3!", 18, 18);

        Assert.AreEqual("line1\r\nline2\r\nline3!", text.ToString());
        Assert.DoesNotContain(operation => operation.Kind == BcrdtDeltaKind.Delete, delta);

        // Deleting a displayed line break removes the whole "\r\n".
        binding.ApplyInput("line1line2\nline3!", 5, 5);
        Assert.AreEqual("line1line2\r\nline3!", text.ToString());
    }

    [TestMethod]
    public void Embeds_are_hidden_from_the_control_and_survive_its_edits()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "abcdef");
        text.InsertEmbed(3, new Dictionary<string, object?> { ["image"] = "x.png" });
        using var binding = new BcrdtTextInputBinding(text);

        Assert.AreEqual("abcdef", binding.Value);
        binding.ApplyInput("abcXdef", 4, 4);

        Assert.AreEqual("abcXdef", text.ToString());
        Assert.AreEqual(8, text.Count);
    }

    [TestMethod]
    public void A_selection_survives_a_remote_change_as_a_selection()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello world");
        using var binding = new BcrdtTextInputBinding(text);
        binding.SetSelection(6, 11);

        text.Insert(0, ">> ");

        BcrdtTextControlUpdate update = binding.CreateUpdate();
        Assert.AreEqual((9, 14), (update.SelectionStart, update.SelectionEnd));
        Assert.AreEqual("world", update.Value[update.SelectionStart..update.SelectionEnd]);
    }

    [TestMethod]
    public void A_change_arriving_while_an_update_is_in_flight_is_not_lost()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "abc");
        using var binding = new BcrdtTextInputBinding(text);

        text.Insert(0, "1");
        BcrdtTextControlUpdate first = binding.CreateUpdate();
        text.Insert(4, "2");   // arrives before the control confirms the first update
        binding.Acknowledge(first);

        Assert.AreEqual("1abc", binding.Value);
        Assert.IsTrue(binding.HasPendingChanges);

        // Input against what the control shows still lands correctly.
        binding.ApplyInput("1aXbc", 3, 3);
        Assert.AreEqual("1aXbc2", text.ToString());
    }

    [TestMethod]
    public void A_refused_update_is_rebuilt_from_the_users_newer_input()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "abc");
        using var binding = new BcrdtTextInputBinding(text);

        text.Insert(0, ">");
        BcrdtTextControlUpdate stale = binding.CreateUpdate();
        binding.ApplyInput("abcd", 4, 4);   // the user typed before the update reached the control
        binding.Reject(stale);

        BcrdtTextControlUpdate fresh = binding.CreateUpdate();
        Assert.AreEqual("abcd", fresh.Expected);
        Assert.AreEqual(">abcd", fresh.Value);
        Assert.AreEqual(">abcd", text.ToString());
    }

    [TestMethod]
    public void Edits_made_through_the_binding_are_undoable_by_a_default_undo_manager()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello");
        using var undo = new BcrdtUndoManager(text);
        using var binding = new BcrdtTextInputBinding(text);

        binding.ApplyInput("hello world", 11, 11);
        Assert.IsTrue(undo.CanUndo);

        undo.Undo();
        Assert.AreEqual("hello", text.ToString());
    }

    // ---------------------------------------------------------------- Bindings

    private enum Kind { A, B }

    private sealed record Shape(string Name, int Sides);

    private sealed class Model
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public Kind Kind { get; set; }
        public List<string>? Tags { get; set; }
        public TimeSpan Duration { get; set; }
        public Shape? Shape { get; set; }
    }

    private sealed class Indexed
    {
        private readonly Dictionary<int, string> _values = [];
        public string? Title { get; set; }
        public string this[int index] { get => _values[index]; set => _values[index] = value; }
    }

    [TestMethod]
    public void An_object_binding_reads_back_every_property_it_wrote()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var binding = new BcrdtObjectBinding<Model>(doc.GetMap("m"));

        binding.FromObject(new Model
        {
            Name = "Ada",
            Kind = Kind.B,
            Tags = ["a", "b"],
            Duration = TimeSpan.FromMinutes(1),
            Shape = new Shape("square", 4),
        });

        Model back = binding.ToObject();
        Assert.AreEqual("Ada", back.Name);
        Assert.AreEqual(Kind.B, back.Kind);
        Assert.AreSequenceEqual(new[] { "a", "b" }, back.Tags);
        Assert.AreEqual(TimeSpan.FromMinutes(1), back.Duration);
        Assert.AreEqual(new Shape("square", 4), back.Shape);
    }

    [TestMethod]
    public void A_value_this_version_cannot_read_is_skipped_rather_than_failing_the_read()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtMap map = doc.GetMap("m");
        using var binding = new BcrdtObjectBinding<Model>(map);

        map.Set("Name", "Ada");
        map.Set("Kind", "Hexagonal");   // an enum member added by a newer version
        map.Set("Tags", 42L);

        Model back = binding.ToObject();
        Assert.AreEqual("Ada", back.Name);
        Assert.AreEqual(Kind.A, back.Kind);
        Assert.IsNull(back.Tags);

        // Saving the form must not overwrite the unreadable value with a default either.
        binding.FromObject(back);
        Assert.AreEqual("Hexagonal", map.Get("Kind"));
    }

    [TestMethod]
    public void Saving_a_form_writes_only_what_this_user_changed()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtMap map = doc.GetMap("m");
        map.Set("Name", "Ada");
        map.Set("Email", "ada@old.example");

        using var bob = new BcrdtObjectBinding<Model>(map);
        Model form = bob.ToObject();

        // Somebody else changes the email while Bob has the form open.
        map.Set("Email", "ada@new.example");

        form.Name = "Ada Lovelace";
        bob.FromObject(form);

        Assert.AreEqual("Ada Lovelace", map.Get("Name"));
        Assert.AreEqual("ada@new.example", map.Get("Email"));
    }

    [TestMethod]
    public void A_type_with_an_indexer_can_be_bound()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var binding = new BcrdtObjectBinding<Indexed>(doc.GetMap("m"));

        binding.FromObject(new Indexed { Title = "x" });

        Assert.AreEqual("x", binding.ToObject().Title);
    }

    [TestMethod]
    public void A_list_binding_keeps_every_index_aligned_with_its_source()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtArray array = doc.GetArray("a");
        array.Push("a", null, "b");
        using var strings = new BcrdtListBinding<string>(array);

        Assert.AreEqual(3, strings.Count);
        Assert.IsNull(strings[1]);
        Assert.AreEqual("b", strings[2]);

        BcrdtArray numbers = doc.GetArray("n");
        numbers.Push(1L, 2L);
        using var ints = new BcrdtListBinding<int>(numbers);

        // A peer stores something that is not a number. The edit must not fail because of the binding.
        numbers.Push("oops");

        Assert.AreSequenceEqual(new[] { 1, 2, 0 }, ints);
    }

    // ---------------------------------------------------------------- Awareness

    [TestMethod]
    public void A_client_timed_out_by_mistake_reappears_with_its_next_update()
    {
        var time = new FakeTimeProvider();
        using var observer = new BcrdtAwarenessStore(100, TimeSpan.FromSeconds(30), time);
        using var remote = new BcrdtAwarenessStore(200, TimeSpan.FromSeconds(30), time);
        observer.SetLocalField("user", "me");

        remote.SetLocalField("user", "bob");
        observer.ApplyUpdate(remote.EncodeUpdate([200]));

        time.Advance(TimeSpan.FromSeconds(31));
        observer.Heartbeat();
        Assert.AreEqual(1, observer.OnlineCount);

        // It was only slow, and never heard about its own removal.
        remote.SetLocalField("user", "bob, again");
        observer.ApplyUpdate(remote.EncodeUpdate([200]));

        Assert.AreEqual(2, observer.OnlineCount);
        Assert.AreEqual("bob, again", observer.GetField(200, "user"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Peers_agree_on_presence_whatever_order_a_timeout_and_a_refresh_arrive_in(bool removalFirst)
    {
        var time = new FakeTimeProvider();
        using var a = new BcrdtAwarenessStore(1, TimeSpan.FromSeconds(30), time);
        using var b = new BcrdtAwarenessStore(2, TimeSpan.FromSeconds(30), time);
        using var c = new BcrdtAwarenessStore(3, TimeSpan.FromSeconds(30), time);

        byte[]? removal = null;
        a.Updated += update => removal = update;

        c.SetLocalField("user", "carol");
        byte[] first = c.EncodeUpdate([3]);
        a.ApplyUpdate(first);
        b.ApplyUpdate(first);

        time.Advance(TimeSpan.FromSeconds(31));
        a.Heartbeat();                                // a times c out and announces it
        Assert.IsNotNull(removal);

        c.SetLocalField("user", "carol");            // c is alive and refreshes
        byte[] refresh = c.EncodeUpdate([3]);

        if (removalFirst) { b.ApplyUpdate(removal!); b.ApplyUpdate(refresh); }
        else { b.ApplyUpdate(refresh); b.ApplyUpdate(removal!); }
        a.ApplyUpdate(refresh);

        Assert.IsNotNull(a.Entries[3].State);
        Assert.IsNotNull(b.Entries[3].State);
    }

    [TestMethod]
    public void A_client_told_it_has_gone_while_still_here_republishes_at_once()
    {
        var time = new FakeTimeProvider();
        using var me = new BcrdtAwarenessStore(1, TimeSpan.FromSeconds(30), time);
        using var peer = new BcrdtAwarenessStore(2, TimeSpan.FromSeconds(30), time);

        byte[]? republished = null;
        me.SetLocalField("user", "me");
        peer.ApplyUpdate(me.EncodeUpdate([1]));
        me.Updated += update => republished = update;

        time.Advance(TimeSpan.FromSeconds(31));
        byte[]? removal = null;
        peer.Updated += update => removal ??= update;
        peer.Heartbeat();
        Assert.AreEqual(0, peer.OnlineCount);

        me.ApplyUpdate(removal!);
        Assert.IsNotNull(republished);

        peer.ApplyUpdate(republished!);
        Assert.AreEqual("me", peer.GetField(1, "user"));
    }

    [TestMethod]
    public void A_value_that_cannot_be_sent_is_rejected_without_breaking_presence()
    {
        using var awareness = new BcrdtAwarenessStore(1);
        int updates = 0;
        awareness.Updated += _ => updates++;
        awareness.SetLocalField("user", "me");

        Assert.ThrowsExactly<NotSupportedException>(() => awareness.SetLocalState(State(("bad", new object()))));
        Assert.AreEqual("me", awareness.GetField(1, "user"));

        // Application types are projected rather than refused.
        awareness.SetLocalField("shape", new Shape("square", 4));
        awareness.Heartbeat();

        Assert.IsInstanceOfType<IReadOnlyDictionary<string, object?>>(awareness.GetField(1, "shape"));
        Assert.AreEqual(3, updates);
    }

    [TestMethod]
    public void A_heartbeat_republishing_unchanged_state_is_not_reported_as_a_change()
    {
        using var a = new BcrdtAwarenessStore(1);
        using var b = new BcrdtAwarenessStore(2);
        b.SetLocalField("user", "bob");
        a.ApplyUpdate(b.EncodeUpdate([2]));

        int localChanges = 0;
        int remoteChanges = 0;
        b.Changed += _ => localChanges++;
        a.Changed += _ => remoteChanges++;

        b.Heartbeat();
        a.ApplyUpdate(b.EncodeUpdate([2]));

        Assert.AreEqual(0, localChanges);
        Assert.AreEqual(0, remoteChanges);
    }

    [TestMethod]
    public void Departed_clients_are_eventually_forgotten_and_not_sent_to_newcomers()
    {
        var time = new FakeTimeProvider();
        using var a = new BcrdtAwarenessStore(1, TimeSpan.FromSeconds(30), time);
        using var b = new BcrdtAwarenessStore(2, TimeSpan.FromSeconds(30), time);

        b.SetLocalField("user", "bob");
        a.ApplyUpdate(b.EncodeUpdate([2]));
        b.SetLocalState(null);
        a.ApplyUpdate(b.EncodeUpdate([2]));

        using var newcomer = new BcrdtAwarenessStore(3);
        newcomer.ApplyUpdate(a.EncodeAll());
        Assert.IsFalse(newcomer.Entries.ContainsKey(2));

        time.Advance(TimeSpan.FromSeconds(31));
        a.Heartbeat();
        Assert.IsFalse(a.Entries.ContainsKey(2));
    }

    [TestMethod]
    public void Presence_can_be_read_while_awareness_changes_on_other_threads()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var awareness = new BcrdtAwarenessStore(1);
        using var presence = new BcrdtPresenceService(awareness, doc);
        var peers = Enumerable.Range(2, 8).Select(id => new BcrdtAwarenessStore((ulong)id)).ToArray();

        Exception? failure = null;
        Parallel.Invoke(
            () => Run(() => { for (int i = 0; i < 2_000; i++) _ = presence.Participants; }),
            () => Run(() =>
            {
                for (int i = 0; i < 2_000; i++)
                {
                    BcrdtAwarenessStore peer = peers[i % peers.Length];
                    peer.SetLocalField("n", i);
                    awareness.ApplyUpdate(peer.EncodeUpdate([peer.LocalClientId]));
                }
            }),
            () => Run(() => { for (int i = 0; i < 2_000; i++) awareness.SetLocalField("n", i); }),
            () => Run(() => { for (int i = 0; i < 200; i++) awareness.Heartbeat(); }));

        foreach (BcrdtAwarenessStore peer in peers) peer.Dispose();
        Assert.IsNull(failure);

        void Run(Action work)
        {
            try { work(); }
            catch (Exception ex) { failure = ex; }
        }
    }

    // ---------------------------------------------------------------- Presence

    [TestMethod]
    public void A_peers_colour_cannot_inject_css()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var mine = new BcrdtAwarenessStore(1);
        using var theirs = new BcrdtAwarenessStore(2);
        using var presence = new BcrdtPresenceService(mine, doc);

        theirs.SetLocalField("user", new Dictionary<string, object?>
        {
            ["id"] = "mallory",
            ["name"] = "Mallory",
            ["color"] = "red;position:fixed;inset:0;background:url(https://attacker.example/beacon)",
        });
        mine.ApplyUpdate(theirs.EncodeAll());

        BcrdtCollaborator mallory = Assert.ContainsSingle(presence.Others);
        Assert.IsTrue(BcrdtPresenceService.IsSafeColor(mallory.User.Color));
        Assert.AreEqual(BcrdtPresenceService.SuggestColor("mallory"), mallory.User.Color);
    }

    [TestMethod]
    public void A_selection_keeps_covering_the_selected_text_whichever_way_it_was_made()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var awareness = new BcrdtAwarenessStore(1);
        using var presence = new BcrdtPresenceService(awareness, doc);
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello world!");

        presence.SetSelection("body", text, 9, 6);   // "wor", selected backwards
        text.Insert(6, "<");                          // typed at the start edge
        text.Insert(10, ">");                         // typed at the end edge

        (int Start, int End) range = presence.Participants[0].Selection!.Resolve(doc)!.Value;
        Assert.AreEqual("wor", text.ToString()[range.Start..range.End]);

        presence.SetSelection("body", text, 3, 3);
        Assert.IsTrue(presence.Participants[0].Selection!.IsCaret);
    }

    [TestMethod]
    public void Suspended_presence_disappears_for_peers_and_comes_back_on_resume()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var peer = new BcrdtAwarenessStore(2);
        using var mine = new BcrdtAwarenessStore(1);
        using var presence = new BcrdtPresenceService(mine, doc);
        mine.Updated += update => peer.ApplyUpdate(update);

        presence.SetUser(new BcrdtCollaboratorInfo("ada", "Ada", "#ff0000"));
        Assert.AreEqual(1, peer.OnlineCount);

        presence.Suspend();
        Assert.AreEqual(0, peer.OnlineCount);

        presence.Resume();
        Assert.AreEqual(1, peer.OnlineCount);
        Assert.Contains(pair => pair.Key == "user", peer.Entries[1].State!);
    }

    // ---------------------------------------------------------------- Persistence

    private static byte[] ItemUpdate(ulong clientId, string value)
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = clientId });
        doc.GetArray("items").Push(value);
        return doc.EncodeStateAsUpdate();
    }

    [TestMethod]
    public async Task Appends_after_a_torn_record_are_not_stranded_behind_it()
    {
        var store = new BcrdtFileSystemDocumentStore(_root, flushToDisk: false);
        await store.AppendUpdateAsync("doc", ItemUpdate(1, "first"));

        // A crash mid-append leaves a header promising more bytes than were written.
        string log = Directory.EnumerateFiles(_root, "*.bcl").Single();
        byte[] header = new byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(header, 100);
        // Appended through a stream rather than File.AppendAllBytesAsync, which net8.0 does not have.
        byte[] torn = [.. header, 1, 2, 3];
        await using (var stream = new FileStream(log, FileMode.Append))
        {
            await stream.WriteAsync(torn);
        }

        // The process restarts and keeps writing.
        var restarted = new BcrdtFileSystemDocumentStore(_root, flushToDisk: false);
        await restarted.AppendUpdateAsync("doc", ItemUpdate(2, "second"));
        await restarted.AppendUpdateAsync("doc", ItemUpdate(3, "third"));

        BcrdtStoredDocument stored = await new BcrdtFileSystemDocumentStore(_root, flushToDisk: false).LoadAsync("doc");
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 9 });
        doc.ApplyUpdate(stored.Update!);

        Assert.AreEqual(3, stored.RecordCount);
        Assert.AreSequenceEqual(new[] { "first", "second", "third" }, doc.GetArray("items").ToArray().Cast<string>().Order());
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Concurrent_appends_and_compactions_lose_nothing(bool useFileSystem)
    {
        IBcrdtDocumentStore store = useFileSystem
            ? new BcrdtFileSystemDocumentStore(_root, flushToDisk: false)
            : new BcrdtInMemoryDocumentStore();

        byte[][] updates = [.. Enumerable.Range(0, 40).Select(i => ItemUpdate((ulong)(i + 1), $"item-{i}"))];

        await Task.WhenAll(Enumerable.Range(0, updates.Length).Select(i => Task.Run(async () =>
        {
            await store.AppendUpdateAsync("doc", updates[i]);
            if (i % 4 == 0) await store.CompactAsync("doc");
        })));

        BcrdtStoredDocument stored = await store.LoadAsync("doc");
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 999 });
        doc.ApplyUpdate(stored.Update!);

        Assert.AreEqual(updates.Length, doc.GetArray("items").Count);
    }

    [TestMethod]
    public async Task Probing_a_document_that_was_never_stored_creates_no_files()
    {
        var store = new BcrdtFileSystemDocumentStore(_root, flushToDisk: false);

        BcrdtStoredDocument stored = await store.LoadAsync("../../not-there");

        Assert.IsTrue(stored.IsEmpty);
        Assert.IsEmpty(Directory.EnumerateFiles(_root));
    }

    [TestMethod]
    public async Task Deleting_a_document_removes_its_identifier_too()
    {
        var store = new BcrdtFileSystemDocumentStore(_root, flushToDisk: false);
        await store.AppendUpdateAsync("doc", ItemUpdate(1, "x"));

        await store.DeleteAsync("doc");

        Assert.IsEmpty(Directory.EnumerateFiles(_root));
        Assert.IsEmpty(await store.ListAsync());
    }

    [TestMethod]
    public async Task Reopening_an_unchanged_document_does_not_grow_its_log()
    {
        var store = new BcrdtInMemoryDocumentStore();
        using (var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 }))
        {
            await using BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(doc, store, "doc");
            doc.GetText("t").Insert(0, "hello");
            doc.GetText("t").Delete(0, 2);
            await persistence.FlushAsync();
        }

        int before = (await store.LoadAsync("doc")).RecordCount;
        for (int i = 0; i < 3; i++)
        {
            using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = (ulong)(10 + i) });
            await using BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(doc, store, "doc");
            await persistence.FlushAsync();
            Assert.AreEqual("llo", doc.GetText("t").ToString());
        }

        Assert.AreEqual(before, (await store.LoadAsync("doc")).RecordCount);
    }

    private sealed class FlakyStore(IBcrdtDocumentStore inner, int failures) : IBcrdtDocumentStore
    {
        private int _remaining = failures;

        public ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default) =>
            inner.LoadAsync(documentId, cancellationToken);

        public ValueTask AppendUpdateAsync(string documentId, ReadOnlyMemory<byte> update, CancellationToken cancellationToken = default) =>
            Interlocked.Decrement(ref _remaining) >= 0
                ? throw new TaskCanceledException("The storage service timed out.")
                : inner.AppendUpdateAsync(documentId, update, cancellationToken);

        public ValueTask CompactAsync(string documentId, CancellationToken cancellationToken = default) =>
            inner.CompactAsync(documentId, cancellationToken);

        public ValueTask DeleteAsync(string documentId, CancellationToken cancellationToken = default) =>
            inner.DeleteAsync(documentId, cancellationToken);

        public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default) =>
            inner.ListAsync(cancellationToken);
    }

    [TestMethod]
    public async Task A_failing_store_delays_writes_but_never_drops_them()
    {
        var backing = new BcrdtInMemoryDocumentStore();
        var store = new FlakyStore(backing, failures: 3);
        int reported = 0;

        using (var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 }))
        {
            await using BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(
                doc, store, "doc", new BcrdtDocumentPersistenceOptions { RetryDelay = TimeSpan.FromMilliseconds(5) });
            persistence.WriteFailed += _ => Interlocked.Increment(ref reported);

            doc.GetText("t").Insert(0, "a");
            doc.GetText("t").Insert(1, "b");
            doc.GetText("t").Insert(2, "c");

            Task flushed = persistence.FlushAsync();
            await WaitUntilAsync(() => flushed.IsCompleted, "the writes have been retried into the store");
            await flushed;
        }

        Assert.AreEqual(3, reported);

        using var reloaded = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        reloaded.ApplyUpdate((await backing.LoadAsync("doc")).Update!);
        Assert.AreEqual("abc", reloaded.GetText("t").ToString());
    }

    // ---------------------------------------------------------------- Collaboration

    private sealed class RoomTransportFactory(BcrdtInMemoryNetwork network, Action? onCreate = null) : IBcrdtTransportFactory
    {
        public IBcrdtTransport Create(string documentId)
        {
            onCreate?.Invoke();
            return network.CreateTransport(documentId);
        }
    }

    private static BcrdtCollaborationService CreateClient(
        BcrdtInMemoryNetwork? network = null,
        IBcrdtDocumentStore? store = null,
        bool undo = false,
        Action? onTransportCreated = null) =>
        new(
            new BcrdtCollaborationOptions
            {
                Sync = new BcrdtSyncProviderOptions { HeartbeatInterval = TimeSpan.Zero },
                CreateUndoManager = undo,
            },
            network is null ? null : new RoomTransportFactory(network, onTransportCreated),
            store);

    [TestMethod]
    public async Task Closing_a_shared_document_once_leaves_it_open_for_the_other_user()
    {
        await using BcrdtCollaborationService service = CreateClient();

        BcrdtCollaborativeDocument first = await service.OpenAsync("doc");
        BcrdtCollaborativeDocument second = await service.OpenAsync("doc");
        Assert.AreSame(first, second);

        await service.CloseAsync("doc");
        second.Text("body").Insert(0, "still open");
        Assert.ContainsSingle(service.OpenDocuments);

        await service.CloseAsync("doc");
        Assert.IsEmpty(service.OpenDocuments);
        Assert.ThrowsExactly<ObjectDisposedException>(() => second.Text("body").Insert(0, "closed"));
    }

    private sealed class GatedStore(IBcrdtDocumentStore inner) : IBcrdtDocumentStore
    {
        public TaskCompletionSource Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool FailNextLoad { get; set; }

        public async ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default)
        {
            await Gate.Task.ConfigureAwait(false);
            if (FailNextLoad)
            {
                FailNextLoad = false;
                throw new IOException("The storage service is unavailable.");
            }
            return await inner.LoadAsync(documentId, cancellationToken).ConfigureAwait(false);
        }

        public ValueTask AppendUpdateAsync(string documentId, ReadOnlyMemory<byte> update, CancellationToken cancellationToken = default) =>
            inner.AppendUpdateAsync(documentId, update, cancellationToken);

        public ValueTask CompactAsync(string documentId, CancellationToken cancellationToken = default) =>
            inner.CompactAsync(documentId, cancellationToken);

        public ValueTask DeleteAsync(string documentId, CancellationToken cancellationToken = default) =>
            inner.DeleteAsync(documentId, cancellationToken);

        public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default) =>
            inner.ListAsync(cancellationToken);
    }

    [TestMethod]
    public async Task A_failed_open_is_retried_rather_than_remembered()
    {
        var store = new GatedStore(new BcrdtInMemoryDocumentStore()) { FailNextLoad = true };
        store.Gate.SetResult();
        await using BcrdtCollaborationService service = CreateClient(store: store);

        await Assert.ThrowsExactlyAsync<IOException>(async () => await service.OpenAsync("doc"));

        BcrdtCollaborativeDocument doc = await service.OpenAsync("doc");
        Assert.AreEqual("doc", doc.Id);
    }

    /// <summary>A network that never answers: a captive portal, a stalled negotiate.</summary>
    private sealed class StalledTransport : IBcrdtTransport
    {
        public BcrdtTransportState State => BcrdtTransportState.Disconnected;

        public string Description => "stalled";

        public event Action<ReadOnlyMemory<byte>>? MessageReceived { add { } remove { } }

        public event Action<BcrdtTransportStateChange>? StateChanged { add { } remove { } }

        public async ValueTask ConnectAsync(CancellationToken cancellationToken = default) =>
            await Task.Delay(Timeout.Infinite, cancellationToken);

        public ValueTask SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask DisconnectAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class StalledTransportFactory : IBcrdtTransportFactory
    {
        public IBcrdtTransport Create(string documentId) => new StalledTransport();
    }

    [TestMethod]
    public async Task Opening_a_document_does_not_wait_for_the_network()
    {
        var store = new BcrdtInMemoryDocumentStore();
        using (var saved = new BcrdtDocument())
        {
            saved.GetText("body").Insert(0, "saved earlier");
            await store.AppendUpdateAsync("doc", saved.EncodeStateAsUpdate());
        }

        await using var service = new BcrdtCollaborationService(
            new BcrdtCollaborationOptions { Sync = new BcrdtSyncProviderOptions { HeartbeatInterval = TimeSpan.Zero } },
            new StalledTransportFactory(),
            store);

        // Loaded from storage, the document is usable at once; the connection carries on behind it.
        BcrdtCollaborativeDocument doc = await service.OpenAsync("doc").AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreEqual("saved earlier", doc.Text("body").ToString());
        Assert.AreEqual(BcrdtConnectionStatus.Connecting, doc.Sync!.Status);
    }

    [TestMethod]
    public void Republishing_a_mutated_state_dictionary_is_announced_as_a_change()
    {
        using var awareness = new BcrdtAwarenessStore(1);
        int changes = 0;
        awareness.Changed += _ => changes++;

        var state = new Dictionary<string, object?> { ["x"] = 1 };
        awareness.SetLocalState(state);
        state["x"] = 2;

        // The store kept its own copy, in the form every peer decodes it as.
        Assert.AreEqual(1L, awareness.LocalState!["x"]);

        awareness.SetLocalState(state);
        Assert.AreEqual(2, changes);
        Assert.AreEqual(2L, awareness.LocalState!["x"]);
    }

    [TestMethod]
    public async Task One_caller_giving_up_does_not_fail_another_waiting_for_the_same_document()
    {
        var store = new GatedStore(new BcrdtInMemoryDocumentStore());
        await using BcrdtCollaborationService service = CreateClient(store: store);

        using var impatient = new CancellationTokenSource();
        ValueTask<BcrdtCollaborativeDocument> first = service.OpenAsync("doc", cancellationToken: impatient.Token);
        ValueTask<BcrdtCollaborativeDocument> second = service.OpenAsync("doc");

        await impatient.CancelAsync();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await first);

        store.Gate.SetResult();
        BcrdtCollaborativeDocument doc = await second;

        doc.Text("body").Insert(0, "usable");
        Assert.ContainsSingle(service.OpenDocuments);
    }

    [TestMethod]
    public async Task Closing_a_document_tells_peers_at_once_that_the_participant_left()
    {
        var network = new BcrdtInMemoryNetwork();
        await using BcrdtCollaborationService clientA = CreateClient(network);
        await using BcrdtCollaborationService clientB = CreateClient(network);

        await clientA.OpenAsync("doc", new BcrdtOpenDocumentOptions { User = new BcrdtCollaboratorInfo("ada", "Ada", "#ff0000") });
        BcrdtCollaborativeDocument b = await clientB.OpenAsync("doc", new BcrdtOpenDocumentOptions { User = new BcrdtCollaboratorInfo("bob", "Bob", "#00ff00") });
        await WaitUntilAsync(() => b.Presence.Others.Any(o => o.User.Name == "Ada"), "B sees A");

        await clientA.CloseAsync("doc");

        // The presence timeout is thirty seconds; only a goodbye explains A disappearing now.
        await WaitUntilAsync(() => b.Presence.Others.Count == 0, "B sees that A left");
    }

    [TestMethod]
    public async Task Reconnecting_restores_who_the_participant_is()
    {
        var network = new BcrdtInMemoryNetwork();
        await using BcrdtCollaborationService clientA = CreateClient(network);
        await using BcrdtCollaborationService clientB = CreateClient(network);

        BcrdtCollaborativeDocument a = await clientA.OpenAsync("doc", new BcrdtOpenDocumentOptions { User = new BcrdtCollaboratorInfo("ada", "Ada", "#ff0000") });
        BcrdtCollaborativeDocument b = await clientB.OpenAsync("doc");
        await WaitUntilAsync(() => b.Presence.Others.Any(o => o.User.Name == "Ada"), "B sees A");

        await a.DisconnectAsync();
        await WaitUntilAsync(() => b.Presence.Others.Count == 0, "B sees A leave");

        await a.ConnectAsync();
        await WaitUntilAsync(() => b.Presence.Others.Any(o => o.User.Name == "Ada"), "B sees Ada again, not an anonymous cursor");
    }

    [TestMethod]
    public async Task The_default_undo_stack_captures_edits_to_text_obtained_from_the_document()
    {
        await using BcrdtCollaborationService service = CreateClient(undo: true);
        BcrdtCollaborativeDocument doc = await service.OpenAsync("doc");

        BcrdtText text = doc.Text("body");
        BcrdtTextSynchronizer.Apply(text, "typed");
        Assert.IsTrue(doc.Undo!.CanUndo);

        doc.Undo.Undo();
        Assert.AreEqual(string.Empty, text.ToString());
    }

    [TestMethod]
    public async Task A_transaction_opened_inside_another_reports_that_it_is_nested()
    {
        await using BcrdtCollaborationService service = CreateClient();
        BcrdtCollaborativeDocument doc = await service.OpenAsync("doc");

        using BcrdtCollaborativeTransaction outer = doc.BeginTransaction();
        using BcrdtCollaborativeTransaction inner = doc.BeginTransaction();

        Assert.IsFalse(outer.IsNested);
        Assert.IsTrue(inner.IsNested);
    }

    private sealed class PumpingContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = [];

        public PumpingContext() => new Thread(Pump) { IsBackground = true, Name = "test-dispatcher" }.Start();

        public override void Post(SendOrPostCallback d, object? state) => _queue.Add((d, state));

        public override void Send(SendOrPostCallback d, object? state) => throw new NotSupportedException();

        public Task<T> RunAsync<T>(Func<Task<T>> work)
        {
            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            Post(async _ =>
            {
                try { completion.SetResult(await work()); }
                catch (Exception ex) { completion.SetException(ex); }
            }, null);
            return completion.Task;
        }

        public void Dispose() => _queue.CompleteAdding();

        private void Pump()
        {
            SetSynchronizationContext(this);
            foreach ((SendOrPostCallback callback, object? state) in _queue.GetConsumingEnumerable()) callback(state);
        }
    }

    private sealed class SlowStore(IBcrdtDocumentStore inner) : IBcrdtDocumentStore
    {
        public async ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            return await inner.LoadAsync(documentId, cancellationToken).ConfigureAwait(false);
        }

        public ValueTask AppendUpdateAsync(string documentId, ReadOnlyMemory<byte> update, CancellationToken cancellationToken = default) =>
            inner.AppendUpdateAsync(documentId, update, cancellationToken);

        public ValueTask CompactAsync(string documentId, CancellationToken cancellationToken = default) =>
            inner.CompactAsync(documentId, cancellationToken);

        public ValueTask DeleteAsync(string documentId, CancellationToken cancellationToken = default) =>
            inner.DeleteAsync(documentId, cancellationToken);

        public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default) =>
            inner.ListAsync(cancellationToken);
    }

    [TestMethod]
    public async Task A_document_is_wired_to_the_network_on_the_context_that_opened_it()
    {
        using var context = new PumpingContext();
        SynchronizationContext? wiredOn = null;
        var network = new BcrdtInMemoryNetwork();
        BcrdtCollaborationService service = CreateClient(
            network, new SlowStore(new BcrdtInMemoryDocumentStore()), onTransportCreated: () => wiredOn = SynchronizationContext.Current);

        // Loading from the store completes on a thread-pool thread; the sync provider must still be
        // created back on the opening context, because that is the context it applies updates on.
        await context.RunAsync(async () => await service.OpenAsync("doc"));
        Assert.AreSame(context, wiredOn);

        await context.RunAsync(async () =>
        {
            await service.DisposeAsync();
            return 0;
        });
    }

    // ---------------------------------------------------------------- Second review

    private const string EmbeddedUrl = "https://example.com/picture.png";

    [TestMethod]
    public void An_embedded_string_occupies_one_position_in_a_projection()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("t");
        text.Insert(0, "hello world");
        text.InsertEmbed(5, EmbeddedUrl);

        BcrdtTextProjection projection = BcrdtTextProjection.Create(text, normalizeLineEndings: true);

        Assert.AreEqual("hello world", projection.Display);
        Assert.AreEqual(12, projection.DocumentLength);
        Assert.AreEqual(6, projection.ToDocument(5));
        Assert.AreEqual(12, projection.ToDocument(11));

        BcrdtTextSynchronizer.Apply(text, "hello world!");
        Assert.AreEqual("hello world!", text.ToString());
        Assert.AreEqual(13, text.Count);
    }

    [TestMethod]
    public void A_binding_tracks_positions_through_an_embedded_string()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("t");
        text.Insert(0, "hello world");
        using var binding = new BcrdtTextInputBinding(text);

        // A collaborator embeds a link; the control has not been updated yet.
        text.InsertEmbed(5, EmbeddedUrl);
        binding.ApplyInput("hello world!", 12, 12);

        Assert.AreEqual("hello world!", text.ToString());
        Assert.AreEqual(13, text.Count);

        BcrdtTextControlUpdate update = binding.CreateUpdate();
        Assert.AreEqual("hello world!", update.Value);
        Assert.AreEqual(update.Expected, update.Value);
        binding.Acknowledge(update);
        Assert.IsFalse(binding.HasPendingChanges);

        binding.ApplyInput("hello, world!", 6, 6);
        Assert.AreEqual("hello, world!", text.ToString());
    }

    [TestMethod]
    public void A_caret_moves_by_one_past_an_embedded_string()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("t");
        text.Insert(0, "hello");

        List<BcrdtDeltaOperation> delta = [];
        using IDisposable subscription = text.Observe((e, _) => delta.AddRange(((BcrdtTextEvent)e).Delta));
        text.InsertEmbed(0, EmbeddedUrl);

        Assert.AreEqual(4, BcrdtTextSynchronizer.MapCaretThroughDelta(3, delta));
    }

    [TestMethod]
    public void Input_after_an_update_whose_confirmation_was_lost_is_read_against_what_the_control_showed()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello");
        using var binding = new BcrdtTextInputBinding(text);

        text.Insert(0, "X");
        BcrdtTextControlUpdate update = binding.CreateUpdate();
        Assert.AreEqual(1, update.Id);
        Assert.ThrowsExactly<InvalidOperationException>(() => binding.CreateUpdate());

        // The control applied the update, but the answer never arrived. The user types; the control
        // reports that the last update it applied was this one.
        binding.Resolve(update.Id);
        binding.ApplyInput("Xhello!", 7, 7);

        Assert.AreEqual("Xhello!", text.ToString());
        Assert.IsFalse(binding.HasOutstandingUpdate);

        // A control that never applied an update reports an older identifier, which rejects it.
        text.Insert(0, ">");
        BcrdtTextControlUpdate unapplied = binding.CreateUpdate();
        binding.Resolve(update.Id);
        Assert.IsFalse(binding.HasOutstandingUpdate);
        binding.ApplyInput("Xhello!?", 8, 8);
        Assert.AreEqual(">Xhello!?", text.ToString());

        // A late answer for an update already settled changes nothing.
        binding.Acknowledge(unapplied);
        Assert.AreEqual("Xhello!?", binding.Value);
    }

    [TestMethod]
    public void Positions_stay_correct_when_change_notifications_are_delivered_later()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello");

        var posted = new Queue<Action>();
        using var binding = new BcrdtTextInputBinding(text, posted.Enqueue);
        int changed = 0;
        binding.Changed += () => changed++;

        text.Insert(0, "X");
        binding.ApplyInput("hello!", 6, 6);   // before the notification has been delivered

        Assert.AreEqual("Xhello!", text.ToString());
        Assert.AreEqual(0, changed);

        while (posted.TryDequeue(out Action? work)) work();
        Assert.AreEqual(1, changed);
    }

    [TestMethod]
    public void A_change_made_in_response_to_the_bindings_own_edit_is_tracked()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "line1\r\nline2\r\nline3");
        using var binding = new BcrdtTextInputBinding(text);

        // A subscriber that edits the text inside the binding's own change: its edit settles straight
        // after the binding's, and must move positions that already include the binding's edit.
        bool trimmed = false;
        using IDisposable trimmer = text.Observe((_, _) =>
        {
            if (trimmed || text.Count < 20) return;
            trimmed = true;
            text.Delete(14, 5);
        });

        binding.ApplyInput("line1\nline2\nline3!", 18, 18);
        Assert.AreEqual("line1\r\nline2\r\n!", text.ToString());
        Assert.IsTrue(binding.HasPendingChanges);

        // The control still shows the deleted run. Typing after it must neither bring it back nor
        // land anywhere but after the "!".
        binding.ApplyInput("line1\nline2\nline3!?", 19, 19);
        Assert.AreEqual("line1\r\nline2\r\n!?", text.ToString());
    }

    [TestMethod]
    public void Resynchronizing_keeps_windows_line_endings_out_of_the_edit()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "line1\r\nline2\r\nline3");
        using var binding = new BcrdtTextInputBinding(text);

        // A subscriber that edits the text as the binding's own transaction opens leaves the edit
        // aimed at a document that no longer exists - which forces the binding's fallback.
        bool trimmed = false;
        doc.BeforeTransaction += _ =>
        {
            if (trimmed) return;
            trimmed = true;
            text.Delete(14, 5);
        };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => binding.ApplyInput("line1\nline2\nline3!", 18, 18));

        // The fallback made the document read what the control shows, diffing against the same
        // normalized view the control has, so the "\r\n" pairs were left alone.
        Assert.AreEqual("line1\r\nline2\r\nline3!", text.ToString());
        Assert.AreEqual("line1\nline2\nline3!", binding.Value);
        Assert.IsFalse(binding.HasPendingChanges);
    }

    [TestMethod]
    public void Input_applied_while_a_transaction_is_open_waits_for_it_rather_than_joining_it()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello world");
        using var binding = new BcrdtTextInputBinding(text);

        using (doc.BeginTransaction())
        {
            // A change already in the batch moves every position the binding tracks...
            text.Insert(0, ">> ");

            Assert.IsTrue(binding.ApplyInput("hello world!", 12, 12));
            Assert.AreEqual(">> hello world", text.ToString());
            Assert.AreEqual("hello world", binding.Value);

            // ...and so does one made after the input.
            text.Insert(text.Count, " <<");
        }

        Assert.AreEqual(">> hello world! <<", text.ToString());
        Assert.AreEqual("hello world!", binding.Value);

        // Positions are still exact: the next keystroke lands where the user typed it.
        binding.ApplyInput("hello world!?", 13, 13);
        Assert.AreEqual(">> hello world!? <<", text.ToString());
    }

    private sealed class SizeLimitedStore(IBcrdtDocumentStore inner, int maxRecord, int transientFailures) : IBcrdtDocumentStore
    {
        private int _transient = transientFailures;

        public int Rejected;

        public ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default) =>
            inner.LoadAsync(documentId, cancellationToken);

        public ValueTask AppendUpdateAsync(string documentId, ReadOnlyMemory<byte> update, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Decrement(ref _transient) >= 0) throw new IOException("The disk is busy.");
            if (update.Length > maxRecord)
            {
                Interlocked.Increment(ref Rejected);
                throw new ArgumentException("The record is larger than this store accepts.", nameof(update));
            }
            return inner.AppendUpdateAsync(documentId, update, cancellationToken);
        }

        public ValueTask CompactAsync(string documentId, CancellationToken cancellationToken = default) =>
            inner.CompactAsync(documentId, cancellationToken);

        public ValueTask DeleteAsync(string documentId, CancellationToken cancellationToken = default) =>
            inner.DeleteAsync(documentId, cancellationToken);

        public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default) =>
            inner.ListAsync(cancellationToken);
    }

    [TestMethod]
    public async Task An_update_the_store_refuses_outright_is_dropped_without_wedging_the_rest()
    {
        var backing = new BcrdtInMemoryDocumentStore();
        var store = new SizeLimitedStore(backing, maxRecord: 300, transientFailures: 1);
        int reported = 0;

        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        await using BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(
            doc, store, "doc", new BcrdtDocumentPersistenceOptions { RetryDelay = TimeSpan.FromMilliseconds(50) });
        persistence.WriteFailed += _ => Interlocked.Increment(ref reported);

        // One small update, one the store will never accept, one small one - queued together behind a
        // transient failure, so they are first tried as a single coalesced record. The oversized one
        // comes from a peer, so dropping it leaves no gap in this client's own history.
        using var peer = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 3 });
        peer.GetText("big").Insert(0, new string('x', 1_000));

        doc.GetArray("small").Push("a");
        doc.ApplyUpdate(peer.EncodeStateAsUpdate(), origin: "peer");
        doc.GetArray("small").Push("b");

        Task flushed = persistence.FlushAsync();
        await WaitUntilAsync(() => flushed.IsCompleted, "every queued update has been dealt with");
        await Assert.ThrowsExactlyAsync<IOException>(() => flushed);
        Assert.IsTrue(store.Rejected >= 1);
        Assert.IsTrue(reported >= 2);

        // Not wedged: later changes are stored and flush cleanly.
        doc.GetArray("small").Push("c");
        Task later = persistence.FlushAsync();
        await WaitUntilAsync(() => later.IsCompleted, "the later change has been stored");
        await later;

        using var reloaded = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        reloaded.ApplyUpdate((await backing.LoadAsync("doc")).Update!);
        Assert.AreSequenceEqual(new[] { "a", "b", "c" }, reloaded.GetArray("small").ToArray().Cast<string>());
        Assert.AreEqual(string.Empty, reloaded.GetText("big").ToString());
    }

    private sealed class HangingStore : IBcrdtDocumentStore
    {
        public bool LoadCancelled { get; private set; }

        public async ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                return default;
            }
            catch (OperationCanceledException)
            {
                LoadCancelled = true;
                throw;
            }
        }

        public ValueTask AppendUpdateAsync(string documentId, ReadOnlyMemory<byte> update, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask CompactAsync(string documentId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask DeleteAsync(string documentId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<string>>([]);
    }

    [TestMethod]
    public async Task Giving_up_on_the_only_open_returns_at_once_and_stops_building_the_document()
    {
        var store = new HangingStore();
        await using BcrdtCollaborationService service = CreateClient(store: store);

        using var impatient = new CancellationTokenSource();
        ValueTask<BcrdtCollaborativeDocument> open = service.OpenAsync("doc", cancellationToken: impatient.Token);
        await impatient.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await open);
        await WaitUntilAsync(() => store.LoadCancelled, "the abandoned creation has been cancelled");
        Assert.IsEmpty(service.OpenDocuments);
    }

    [TestMethod]
    public void A_heartbeat_never_reverts_a_concurrent_change_to_local_state()
    {
        using var awareness = new BcrdtAwarenessStore(1);
        awareness.SetLocalField("n", 0);
        const int last = 3_000;

        Parallel.Invoke(
            () => { for (int i = 1; i <= last; i++) awareness.SetLocalField("n", i); },
            () => { for (int i = 0; i < 3_000; i++) awareness.Heartbeat(); });

        Assert.AreEqual((long)last, awareness.GetField(1, "n"));
    }
}
