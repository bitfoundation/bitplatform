namespace Bit.Bcrdt.Tests;

/// <summary>
/// Pins down engine behaviours that were each, at some point, subtly wrong.
/// </summary>
/// <remarks>
/// Every test here describes a contract in terms an application can observe - the text a user sees,
/// the position a cursor resolves to, whether a document can still be synchronised - rather than in
/// terms of internal structure, so that the tests keep meaning the same thing as the engine evolves.
/// </remarks>
[TestClass]
public class EngineRegressionTests
{
    private static BcrdtDocument Doc(ulong clientId, bool gc = true) =>
        new(new BcrdtDocumentOptions { ClientId = clientId, GarbageCollect = gc });

    private static Dictionary<string, object?> Attrs(params (string Key, object? Value)[] pairs) =>
        pairs.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    private static string Render(BcrdtText text) =>
        string.Join("|", text.ToDelta().Select(op =>
        {
            string attributes = op.Attributes is null
                ? ""
                : "{" + string.Join(",", op.Attributes.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}")) + "}";
            return $"{op.Content}{attributes}";
        }));

    // ---------------------------------------------------------------- relative positions

    [TestMethod]
    public void A_caret_at_the_end_of_the_text_resolves_to_the_end()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello");

        BcrdtRelativePosition end = BcrdtRelativePosition.FromIndex(text, 5);
        Assert.AreEqual(5, end.Resolve(doc)!.Value.Index);

        // A caret bound to the end keeps tracking the end as text is appended.
        text.Insert(5, " world");
        Assert.AreEqual(11, end.Resolve(doc)!.Value.Index);
        Assert.AreEqual(11, BcrdtRelativePosition.Decode(end.Encode()).Resolve(doc)!.Value.Index);
    }

    [TestMethod]
    public void A_left_bound_caret_at_the_start_resolves_to_the_start()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");

        BcrdtRelativePosition start = BcrdtRelativePosition.FromIndex(text, 0, association: -1);
        text.Insert(0, "hello");

        Assert.AreEqual(0, start.Resolve(doc)!.Value.Index);
    }

    [TestMethod]
    public void A_relative_position_rejects_a_negative_index()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BcrdtRelativePosition.FromIndex(text, -1));
    }

    // ---------------------------------------------------------------- rich text

    [TestMethod]
    public void Inserting_with_explicit_attributes_inside_formatted_text_is_exact()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "abcdef");
        text.Format(0, 6, Attrs(("bold", true)));

        // Explicitly unformatted text in the middle of a bold run must not inherit bold, and must not
        // switch bold off for what follows.
        text.Insert(3, "X", Attrs());

        Assert.AreEqual("abc{bold=True}|X|def{bold=True}", Render(text));
    }

    [TestMethod]
    public void Deleting_across_a_formatting_boundary_keeps_the_formatting_after_it()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "abcdef");
        text.Format(0, 3, Attrs(("bold", true)));

        // The deletion starts past the opening mark, which an index cache would jump over.
        text.Delete(2, 2);

        Assert.AreEqual("ab{bold=True}|ef", Render(text));
    }

    [TestMethod]
    public void Formatting_stops_at_a_conflicting_mark_instead_of_overwriting_what_follows()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "abcdef");
        text.Format(3, 3, Attrs(("bold", "x")));
        text.Format(0, 3, Attrs(("bold", true)));

        Assert.AreEqual("abc{bold=True}|def{bold=x}", Render(text));
    }

    [TestMethod]
    public void A_binary_embed_in_text_appears_in_the_change_delta()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "ab");

        List<BcrdtDeltaOperation> observed = [];
        text.Observe((e, _) => observed.AddRange(e.Delta));
        text.InsertEmbed(1, new byte[] { 1, 2, 3 });

        Assert.AreEqual(2, observed.Count);
        Assert.AreEqual(BcrdtDeltaKind.Retain, observed[0].Kind);
        Assert.AreEqual(BcrdtDeltaKind.Insert, observed[1].Kind);
        Assert.IsExactInstanceOfType<byte[]>(observed[1].Content);
    }

    [TestMethod]
    public void A_text_event_seen_through_the_base_type_carries_the_text_delta()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello");

        IReadOnlyList<BcrdtDeltaOperation>? viaBase = null;
        doc.GetText("body").Observe((e, _) => viaBase = ((BcrdtEvent)e).Delta);
        text.Insert(5, " world");

        BcrdtDeltaOperation insert = Assert.ContainsSingle(op => op.Kind == BcrdtDeltaKind.Insert, viaBase!);
        Assert.AreEqual(" world", insert.Content);
    }

    // ---------------------------------------------------------------- index cache

    [TestMethod]
    public void The_index_cache_survives_blocks_merging_back_together()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "abcdef");
        text.Insert(3, "X");

        doc.Transact(_ =>
        {
            text.Insert(4, "Y");
            text.Delete(5, 1);
            text.Insert(1, "W");
        });
        Assert.AreEqual("aWbcXYef", text.ToString());

        text.Insert(6, "Z");
        Assert.AreEqual("aWbcXYZef", text.ToString());
    }

    [TestMethod]
    public void A_remote_update_applied_inside_a_local_transaction_does_not_leave_stale_positions()
    {
        using BcrdtDocument local = Doc(1);
        using BcrdtDocument remote = Doc(2);
        BcrdtText text = local.GetText("body");
        text.Insert(0, "hello");
        remote.ApplyUpdate(local.EncodeStateAsUpdate());

        remote.GetText("body").Insert(0, "XX");
        byte[] update = remote.EncodeStateAsUpdate(local.GetStateVector());

        local.Transact(_ =>
        {
            text.Insert(5, "!");
            local.ApplyUpdate(update);
        });
        text.Insert(3, "-");

        Assert.AreEqual("XXh-ello!", text.ToString());
    }

    // ---------------------------------------------------------------- arrays and maps

    [TestMethod]
    public void Array_operations_validate_their_range_before_changing_anything()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtArray array = doc.GetArray("items");
        array.Push("a", "b", "c");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => array.Insert(-1, "x"));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => array.Insert(4, "x"));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => array.Delete(1, 5));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => array.Replace(3, "x"));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => array.Move(0, 4));

        Assert.AreSequenceEqual(new object?[] { "a", "b", "c" }, array.ToArray());
    }

    [TestMethod]
    public void Moving_a_nested_map_keeps_its_contents_on_every_replica()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);
        BcrdtArray items = a.GetArray("items");

        var first = new BcrdtMap();
        first.Set("title", "first");
        var second = new BcrdtMap();
        second.Set("title", "second");
        items.Push(first, second);

        items.Move(1, 0);
        b.ApplyUpdate(a.EncodeStateAsUpdate());

        Assert.AreEqual("second", items.GetMap(0)!.Get("title"));
        Assert.AreEqual("first", items.GetMap(1)!.Get("title"));
        Assert.AreEqual(BcrdtConvergenceHarness.Describe(a), BcrdtConvergenceHarness.Describe(b));
    }

    [TestMethod]
    public void A_detached_array_can_move_elements()
    {
        var array = new BcrdtArray();
        array.Push("a", "b", "c");
        array.Move(0, 3);
        Assert.AreSequenceEqual(new object?[] { "b", "c", "a" }, array.ToArray());
    }

    [TestMethod]
    public void An_unsupported_value_is_rejected_before_it_can_poison_the_document()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtMap map = doc.GetMap("meta");
        BcrdtArray array = doc.GetArray("items");

        Assert.ThrowsExactly<NotSupportedException>(() => map.Set("bad", new object()));
        Assert.ThrowsExactly<NotSupportedException>(() => array.Push("fine", new object()));

        Assert.IsFalse(map.ContainsKey("bad"));
        Assert.AreEqual(0, array.Count);

        using BcrdtDocument peer = Doc(2);
        peer.ApplyUpdate(doc.EncodeStateAsUpdate());
    }

    [TestMethod]
    public void A_stored_collection_is_a_snapshot_not_a_live_reference()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtArray array = doc.GetArray("items");

        var list = new List<object?> { 1, 2 };
        array.Push(list);
        list.Add(3);

        using BcrdtDocument peer = Doc(2);
        peer.ApplyUpdate(doc.EncodeStateAsUpdate());

        Assert.AreEqual(BcrdtConvergenceHarness.Describe(doc), BcrdtConvergenceHarness.Describe(peer));
        Assert.AreEqual(2, ((IReadOnlyList<object?>)array[0]!).Count);
    }

    [TestMethod]
    public void A_type_that_already_belongs_to_a_document_cannot_be_inserted_again()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtMap root = doc.GetMap("root");
        BcrdtMap child = root.EnsureMap("child");

        Assert.ThrowsExactly<InvalidOperationException>(() => root.Set("again", child));
        Assert.ThrowsExactly<InvalidOperationException>(() => doc.GetArray("items").Push(child));
    }

    [TestMethod]
    public void Detached_text_validates_indices_eagerly()
    {
        var text = new BcrdtText("abc");
        Assert.AreEqual(3, text.Count);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => text.Insert(4, "x"));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => text.Delete(2, 2));

        text.Insert(3, "d");
        Assert.AreEqual(4, text.Count);

        using BcrdtDocument doc = Doc(1);
        doc.GetMap("root").Set("text", text);
        Assert.AreEqual("abcd", text.ToString());
    }

    [TestMethod]
    public void Text_delete_rejects_a_length_that_would_overflow()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => text.Delete(1, int.MaxValue));
        Assert.AreEqual("hello", text.ToString());
    }

    [TestMethod]
    public void A_detached_set_and_an_attached_set_agree_on_membership()
    {
        var set = new BcrdtSet();
        set.Add(5L);
        set.Add(5.0);
        Assert.AreEqual(2, set.Count);

        using BcrdtDocument doc = Doc(1);
        doc.GetMap("root").Set("set", set);
        Assert.AreEqual(2, set.Count);
    }

    // ---------------------------------------------------------------- document lifecycle

    [TestMethod]
    public void A_throwing_update_subscriber_does_not_starve_the_others()
    {
        using BcrdtDocument doc = Doc(1);
        int delivered = 0;
        doc.Updated += _ => throw new InvalidOperationException("subscriber failure");
        doc.Updated += _ => delivered++;

        Assert.ThrowsExactly<AggregateException>(() => doc.GetText("body").Insert(0, "x"));
        Assert.AreEqual(1, delivered);
    }

    [TestMethod]
    public void A_throwing_before_transaction_subscriber_does_not_wedge_the_document()
    {
        using BcrdtDocument doc = Doc(1);
        bool fail = true;
        doc.BeforeTransaction += _ =>
        {
            if (fail) throw new InvalidOperationException("subscriber failure");
        };

        Assert.Throws<Exception>(() => doc.GetText("body").Insert(0, "x"));
        fail = false;

        Assert.IsFalse(doc.InTransaction);
        doc.GetText("body").Insert(0, "y");
        Assert.Contains("y", doc.GetText("body").ToString());
    }

    [TestMethod]
    public void An_update_encoded_while_settling_includes_the_transactions_own_deletions()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello");
        _ = doc.EncodeStateAsUpdate();

        byte[]? captured = null;
        doc.AfterTransaction += _ => captured = doc.EncodeStateAsUpdate();
        text.Delete(0, 1);

        using BcrdtDocument peer = Doc(2);
        peer.ApplyUpdate(captured!);
        Assert.AreEqual("ello", peer.GetText("body").ToString());
    }

    [TestMethod]
    public void Deleted_neighbours_merge_back_into_one_block()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "abcdef");
        text.Delete(2, 1);
        text.Delete(2, 1);

        Assert.AreEqual("abef", text.ToString());
        Assert.AreEqual(3, doc.Store.GetClientStructs(1)!.Count);
    }

    [TestMethod]
    public void Reading_an_event_summary_after_the_transaction_settled_fails_loudly()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        BcrdtEvent? captured = null;
        text.Observe((e, _) => captured = e);

        text.Insert(0, "hello");

        Assert.ThrowsExactly<InvalidOperationException>(() => captured!.Delta);
    }

    [TestMethod]
    public void Undo_does_not_open_a_transaction_per_edit_just_to_clear_an_empty_redo_stack()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        using var undo = new BcrdtUndoManager(text);

        int transactions = 0;
        doc.AfterTransaction += _ => transactions++;
        text.Insert(0, "a");

        Assert.AreEqual(1, transactions);
    }

    // ---------------------------------------------------------------- wire format

    [TestMethod]
    public void A_state_vector_read_from_an_update_never_claims_content_it_does_not_hold()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "abc");

        byte[]? delta = null;
        doc.Updated += args => delta = args.Update;
        text.Insert(3, "def");

        // The delta starts at clock 3: a holder of only this update has nothing from clock 0.
        Assert.AreEqual(0, BcrdtUpdateOperations.DecodeStateVector(delta!).Get(1));
        Assert.AreEqual(6, BcrdtUpdateOperations.DecodeStateVector(doc.EncodeStateAsUpdate()).Get(1));
    }

    [TestMethod]
    public void An_update_referring_to_its_own_future_is_rejected_cleanly()
    {
        var writer = new BcrdtBufferWriter(32);
        try
        {
            writer.WriteVarUInt(1);           // one client group
            writer.WriteVarUInt(1);           // one block
            writer.WriteVarUInt(5);           // client
            writer.WriteVarUInt(0);           // first clock
            writer.WriteByte(0x80 | 3);       // a string with an origin
            writer.WriteVarUInt(5);           // origin client: the same client...
            writer.WriteVarUInt(10);          // ...at a clock it has not reached
            writer.WriteString("a");
            writer.WriteVarUInt(0);           // empty delete set

            using BcrdtDocument doc = Doc(1);
            Assert.ThrowsExactly<BcrdtFormatException>(() => doc.ApplyUpdate(writer.ToArray()));
            Assert.AreEqual(0, doc.Store.GetState(5));
        }
        finally
        {
            writer.Dispose();
        }
    }

    [TestMethod]
    public void An_update_with_a_corrupt_delete_set_changes_nothing()
    {
        using BcrdtDocument source = Doc(7);
        source.GetText("body").Insert(0, "hello");
        source.GetText("body").Delete(0, 1);
        byte[] update = source.EncodeStateAsUpdate();

        using BcrdtDocument target = Doc(1);
        string before = BcrdtConvergenceHarness.Describe(target);

        // Chop the final byte, which lies inside the delete set.
        Assert.Throws<BcrdtFormatException>(() => target.ApplyUpdate(update.AsMemory(0, update.Length - 1)));
        Assert.AreEqual(before, BcrdtConvergenceHarness.Describe(target));
        Assert.AreEqual(0, target.Store.GetState(7));
    }

    [TestMethod]
    public void Malformed_primitives_are_rejected_as_format_errors()
    {
        // Invalid UTF-8 without any surrogate-range byte takes the fast path, which must still reject it.
        var invalid = new BcrdtBufferReader(new byte[] { 1, 0xFF });
        Assert.ThrowsExactly<BcrdtFormatException>(() => invalid.ReadString());

        // Ten bytes whose final byte carries bits beyond the 64th.
        byte[] overflow = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F];
        var reader = new BcrdtBufferReader(overflow);
        Assert.ThrowsExactly<BcrdtFormatException>(() => reader.ReadVarUInt());

        // A timestamp whose local time falls outside the representable range.
        var writer = new BcrdtBufferWriter(32);
        try
        {
            writer.WriteByte((byte)BcrdtAnyTag.Timestamp);
            writer.WriteVarInt(DateTime.MaxValue.Ticks);
            writer.WriteVarInt(840);
            byte[] bytes = writer.ToArray();
            Assert.ThrowsExactly<BcrdtFormatException>(() => BcrdtAnyCodec.Decode(bytes));
        }
        finally
        {
            writer.Dispose();
        }
    }

    [TestMethod]
    public void A_delete_set_encoded_before_it_was_normalized_still_round_trips()
    {
        var set = new BcrdtDeleteSet();
        set.Add(1, 10, 5);
        set.Add(1, 0, 3);
        set.Add(1, 2, 4);

        BcrdtDeleteSet decoded = BcrdtDeleteSet.Decode(set.Encode());

        Assert.AreSequenceEqual([new BcrdtDeleteRange(0, 6), new BcrdtDeleteRange(10, 5)], decoded.Clients[1]);
    }

    [TestMethod]
    public void A_delete_range_running_past_the_clock_space_is_rejected()
    {
        var writer = new BcrdtBufferWriter(32);
        try
        {
            writer.WriteVarUInt(1);
            writer.WriteVarUInt(9);
            writer.WriteVarUInt(1);
            writer.WriteVarUInt(long.MaxValue - 1);
            writer.WriteVarUInt(10);
            byte[] bytes = writer.ToArray();
            Assert.ThrowsExactly<BcrdtFormatException>(() => BcrdtDeleteSet.Decode(bytes));
        }
        finally
        {
            writer.Dispose();
        }
    }

    // ---------------------------------------------------------------- cloning and nesting

    [TestMethod]
    public void Cloning_text_keeps_formatting_embeds_and_nested_types()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetMap("root").EnsureText("body");
        text.Insert(0, "hello world");
        text.Format(0, 5, Attrs(("bold", true)));
        text.InsertEmbed(5, "an embedded string");
        var nested = new BcrdtMap();
        nested.Set("k", "v");
        text.InsertEmbed(0, nested);

        BcrdtText clone = text.Clone();
        Assert.AreEqual(text.Count, clone.Count);
        doc.GetMap("root").Set("copy", clone);

        Assert.AreEqual(text.ToString(), clone.ToString());
        Assert.AreEqual(text.Count, clone.Count);
        Assert.AreEqual(Render(text), Render(clone));
        Assert.AreEqual("v", ((BcrdtMap)clone.ToDelta()[0].Content!).Get("k"));
        Assert.AreEqual("an embedded string", clone.ToDelta().Single(op => op.Content as string == "an embedded string" && op.Length == 1).Content);
    }

    [TestMethod]
    public void An_embedded_string_is_marked_as_an_embed_in_deltas_and_round_trips_as_one()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello world");

        List<BcrdtDeltaOperation> observed = [];
        text.Observe((e, _) => observed.AddRange(e.Delta));
        text.InsertEmbed(5, "https://example.com/a.png");

        BcrdtDeltaOperation embed = Assert.ContainsSingle(op => op.Kind == BcrdtDeltaKind.Insert, observed);
        Assert.IsTrue(embed.IsEmbed);
        Assert.AreEqual(1, embed.Length);

        IReadOnlyList<BcrdtDeltaOperation> delta = text.ToDelta();
        Assert.ContainsSingle(op => op.IsEmbed, delta);
        foreach (BcrdtDeltaOperation op in delta.Where(op => !op.IsEmbed)) Assert.AreEqual(((string)op.Content!).Length, op.Length);

        BcrdtText copy = doc.GetText("copy");
        copy.ApplyDelta(delta);
        Assert.AreEqual(text.Count, copy.Count);
        Assert.AreEqual(text.ToString(), copy.ToString());
    }

    [TestMethod]
    public void A_detached_type_cannot_be_placed_in_two_containers_or_inside_itself()
    {
        var child = new BcrdtMap();
        var first = new BcrdtArray();
        first.Push(child);

        var second = new BcrdtMap();
        Assert.ThrowsExactly<InvalidOperationException>(() => second.Set("child", child));
        Assert.ThrowsExactly<InvalidOperationException>(() => first.Push(child));
        Assert.ThrowsExactly<InvalidOperationException>(() => child.Set("parent", first));

        var self = new BcrdtMap();
        Assert.ThrowsExactly<InvalidOperationException>(() => self.Set("self", self));

        // Releasing it from the first container frees it for the second.
        first.Delete(0);
        second.Set("child", child);

        using BcrdtDocument doc = Doc(1);
        doc.GetMap("root").Set("second", second);
        Assert.AreSame(child, second.GetMap("child"));
    }

    [TestMethod]
    public void A_detached_delta_is_validated_when_it_is_given_not_when_it_is_inserted()
    {
        var text = new BcrdtText();
        text.ApplyDelta([BcrdtDeltaOperation.InsertText("abc"), BcrdtDeltaOperation.Insert(new byte[] { 1 }, 1)]);
        Assert.AreEqual(4, text.Count);

        Assert.ThrowsExactly<NotSupportedException>(() => text.ApplyDelta([BcrdtDeltaOperation.Insert(new object(), 1)]));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => text.ApplyDelta([BcrdtDeltaOperation.Delete(-1)]));

        using BcrdtDocument doc = Doc(1);
        doc.GetMap("root").Set("text", text);
        Assert.AreEqual("abc", text.ToString());
        Assert.AreEqual(4, text.Count);
    }

    [TestMethod]
    public void A_set_refuses_nested_types()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtSet set = doc.GetSet("tags");
        Assert.ThrowsExactly<NotSupportedException>(() => set.Add(new BcrdtMap()));
        Assert.ThrowsExactly<NotSupportedException>(() => set.AddRange(["fine", new BcrdtArray()]));
        Assert.AreEqual(0, set.Count);
    }

    // ---------------------------------------------------------------- undo

    [TestMethod]
    public void Two_undo_managers_sharing_options_do_not_capture_each_others_undos()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        var options = new BcrdtUndoManagerOptions { CaptureTimeout = TimeSpan.Zero };
        using var first = new BcrdtUndoManager(text, options);
        using var second = new BcrdtUndoManager(text, options);

        text.Insert(0, "a");
        first.Undo();

        // The first manager's undo is not a tracked edit for the second one.
        Assert.AreEqual(1, second.UndoCount);
        Assert.AreEqual(1, first.RedoCount);
    }

    [TestMethod]
    public void A_collaborators_edit_ends_the_current_undo_step()
    {
        using BcrdtDocument local = Doc(1);
        using BcrdtDocument remote = Doc(2);
        BcrdtText text = local.GetText("body");
        using var undo = new BcrdtUndoManager(text, new BcrdtUndoManagerOptions { CaptureTimeout = TimeSpan.FromHours(1) });

        text.Insert(0, "a");
        remote.ApplyUpdate(local.EncodeStateAsUpdate());
        remote.GetText("body").Insert(1, "b");
        local.ApplyUpdate(remote.EncodeStateAsUpdate(local.GetStateVector()), origin: "remote");
        text.Insert(2, "c");

        Assert.AreEqual(2, undo.UndoCount);
    }

    // ---------------------------------------------------------------- value model

    [TestMethod]
    public void Large_integers_are_compared_exactly()
    {
        Assert.IsFalse(BcrdtAnyCodec.DeepEquals(9007199254740993L, 9007199254740992L));
        Assert.IsTrue(BcrdtAnyCodec.DeepEquals(5, 5L));
        Assert.IsTrue(BcrdtAnyCodec.DeepEquals(5L, 5.0));
        Assert.IsFalse(BcrdtAnyCodec.DeepEquals(0.1m, 0.1000000000000001m));
    }

    [TestMethod]
    public void Equal_integers_produce_equal_canonical_keys_whatever_their_width()
    {
        Assert.AreEqual(BcrdtAnyCodec.CanonicalKey(5L), BcrdtAnyCodec.CanonicalKey(5UL));
        Assert.AreEqual(BcrdtAnyCodec.CanonicalKey(5L), BcrdtAnyCodec.CanonicalKey((ushort)5));
        Assert.AreEqual(BcrdtAnyCodec.CanonicalKey(-5L), BcrdtAnyCodec.CanonicalKey((sbyte)-5));
    }

    [TestMethod]
    public void A_date_time_is_projected_onto_the_value_model_as_a_timestamp()
    {
        var instant = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);
        Assert.AreEqual(new DateTimeOffset(instant), BcrdtAnyCodec.FromObject(instant));
    }

    // ---------------------------------------------------------------- integration edge cases

    [TestMethod]
    public void A_block_overlapping_content_this_replica_collected_is_reduced_to_a_clock_range()
    {
        using BcrdtDocument author = Doc(1);
        BcrdtMap map = author.GetMap("m");
        map.Set("t", new BcrdtText());
        BcrdtText text = map.GetText("t")!;
        text.Insert(0, "abcde");
        byte[] firstHalf = author.EncodeStateAsUpdate();
        text.Insert(5, "fghij");
        byte[] whole = author.EncodeStateAsUpdate();

        // This replica sees the first half; then the container is deleted and its content collected.
        using BcrdtDocument replica = Doc(2);
        replica.ApplyUpdate(firstHalf);
        replica.GetMap("m").Delete("t");

        // A peer still holding the ten characters as one block - one that keeps deleted content, or
        // one the deletion has not reached - sends all of it, overlapping what was collected here.
        replica.ApplyUpdate(whole);

        Assert.AreEqual(author.Store.GetState(1), replica.Store.GetState(1));
        Assert.IsNull(replica.GetMap("m").Get("t"));

        author.ApplyUpdate(replica.EncodeStateAsUpdate());
        Assert.AreEqual(BcrdtConvergenceHarness.Describe(author), BcrdtConvergenceHarness.Describe(replica));
    }

    [TestMethod]
    public void An_embed_without_a_value_is_rejected_as_malformed()
    {
        var writer = new BcrdtBufferWriter(32);
        try
        {
            writer.WriteVarUInt(1);                         // one client group
            writer.WriteVarUInt(1);                         // one block
            writer.WriteVarUInt(5);                         // client
            writer.WriteVarUInt(0);                         // first clock
            writer.WriteByte((byte)BcrdtContentRef.Embed);  // an embed with no neighbours...
            writer.WriteByte(1);                            // ...so its parent follows: a root, by name
            writer.WriteString("body");
            writer.WriteByte((byte)BcrdtAnyTag.Null);       // embedding nothing
            writer.WriteVarUInt(0);                         // empty delete set

            using BcrdtDocument doc = Doc(1);
            Assert.ThrowsExactly<BcrdtFormatException>(() => doc.ApplyUpdate(writer.ToArray()));
            Assert.AreEqual(0, doc.Store.GetState(5));
        }
        finally
        {
            writer.Dispose();
        }

        Assert.ThrowsExactly<ArgumentNullException>(() => new BcrdtContentEmbed(null!));
    }

    [TestMethod]
    public void An_update_applied_inside_a_local_transaction_leaves_that_transaction_local()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        text.Insert(0, new string('a', 100));

        using BcrdtDocument peer = Doc(2);
        peer.ApplyUpdate(doc.EncodeStateAsUpdate());
        peer.GetText("body").Insert(0, "R");
        byte[] update = peer.EncodeStateAsUpdate(doc.GetStateVector());

        bool? local = null;
        doc.Updated += args => local = args.Transaction.Local;

        doc.Transact(_ =>
        {
            text.Insert(50, "X");
            doc.ApplyUpdate(update);
        });

        // The local edit is still the replica's own, so persistence that keeps only local changes
        // keeps it; and the index cache it warmed was still dropped, because the update moved things.
        Assert.IsTrue(local);
        text.Insert(60, "Y");
        string expected = "R" + new string('a', 50) + "X" + new string('a', 8) + "Y" + new string('a', 42);
        Assert.AreEqual(expected, text.ToString());

        // An update applied on its own is still remote.
        peer.GetText("body").Insert(0, "S");
        doc.ApplyUpdate(peer.EncodeStateAsUpdate(doc.GetStateVector()));
        Assert.IsFalse(local);
    }

    [TestMethod]
    public void A_formatted_retain_pads_the_trailing_newline_and_no_more()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "abc");

        // An editor that always ends a document in a newline formats that newline.
        text.ApplyDelta([BcrdtDeltaOperation.Retain(3), BcrdtDeltaOperation.Retain(1, Attrs(("header", 1L)))]);
        Assert.AreEqual("abc\n", text.ToString());

        // Anything further describes a different text, and would have the padding allocated whole.
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => text.ApplyDelta([BcrdtDeltaOperation.Retain(int.MaxValue, Attrs(("bold", true)))]));
        Assert.AreEqual("abc\n", text.ToString());

        var detached = new BcrdtText();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => detached.ApplyDelta([BcrdtDeltaOperation.Retain(5, Attrs(("bold", true)))]));
        Assert.AreEqual(0, detached.Count);
    }
}
