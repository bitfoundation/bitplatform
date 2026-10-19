using System.Text.Json;

namespace Bit.Bcrdt.Tests;

/// <summary>
/// Smoke tests over the engine's basic contracts, ahead of the convergence and adversarial suites.
/// </summary>
[TestClass]
public class CoreEngineTests
{
    private static BcrdtDocument Doc(ulong clientId) =>
        new(new BcrdtDocumentOptions { ClientId = clientId });

    private static void Exchange(BcrdtDocument a, BcrdtDocument b)
    {
        byte[] aToB = a.EncodeStateAsUpdate(b.GetStateVector());
        byte[] bToA = b.EncodeStateAsUpdate(a.GetStateVector());
        b.ApplyUpdate(aToB);
        a.ApplyUpdate(bToA);
    }

    private static string Json(BcrdtDocument doc) => JsonSerializer.Serialize(doc.ToJson());

    [TestMethod]
    public void Map_stores_and_reads_primitives()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtMap map = doc.GetMap("root");

        map.Set("title", "Hello");
        map.Set("count", 42);
        map.Set("ratio", 0.5);
        map.Set("flag", true);

        Assert.AreEqual("Hello", map.Get("title"));
        Assert.AreEqual(42L, map.Get("count"));
        Assert.AreEqual(0.5, map.Get("ratio"));
        Assert.AreEqual(true, map.Get("flag"));
        Assert.AreEqual(4, map.Count);

        map.Delete("flag");
        Assert.IsFalse(map.ContainsKey("flag"));
        Assert.AreEqual(3, map.Count);
    }

    [TestMethod]
    public void Array_insert_delete_and_order()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtArray array = doc.GetArray("items");

        array.Push("a", "b", "c");
        array.Insert(1, "x");
        Assert.AreSequenceEqual(new object?[] { "a", "x", "b", "c" }, array.ToArray());

        array.Delete(0, 2);
        Assert.AreSequenceEqual(new object?[] { "b", "c" }, array.ToArray());
    }

    [TestMethod]
    public void Text_insert_and_delete()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");

        text.Insert(0, "Hello world");
        text.Insert(5, ",");
        Assert.AreEqual("Hello, world", text.ToString());
        Assert.AreEqual(12, text.Count);

        text.Delete(5, 1);
        Assert.AreEqual("Hello world", text.ToString());
    }

    [TestMethod]
    public void Two_replicas_converge_on_concurrent_text_insertions()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);

        a.GetText("body").Insert(0, "hello");
        Exchange(a, b);
        Assert.AreEqual("hello", b.GetText("body").ToString());

        // Concurrent insertions at the same position, neither replica aware of the other.
        a.GetText("body").Insert(5, " from A");
        b.GetText("body").Insert(5, " from B");
        Exchange(a, b);

        Assert.AreEqual(a.GetText("body").ToString(), b.GetText("body").ToString());

        // The whole point of the dual-origin rule: each author's run stays contiguous.
        string result = a.GetText("body").ToString();
        Assert.Contains(" from A", result);
        Assert.Contains(" from B", result);
    }

    [TestMethod]
    public void Concurrent_map_writes_resolve_identically_on_both_replicas()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);

        a.GetMap("m").Set("k", "from-a");
        b.GetMap("m").Set("k", "from-b");
        Exchange(a, b);

        Assert.AreEqual(a.GetMap("m").Get("k"), b.GetMap("m").Get("k"));
    }

    [TestMethod]
    public void Updates_are_idempotent()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);

        a.GetArray("items").Push("one", "two", "three");
        byte[] update = a.EncodeStateAsUpdate();

        b.ApplyUpdate(update);
        b.ApplyUpdate(update);
        b.ApplyUpdate(update);

        Assert.AreSequenceEqual(new object?[] { "one", "two", "three" }, b.GetArray("items").ToArray());
    }

    [TestMethod]
    public void Updates_applied_in_reverse_order_still_converge()
    {
        using BcrdtDocument source = Doc(1);
        BcrdtArray array = source.GetArray("items");

        List<byte[]> updates = [];
        source.Updated += args => updates.Add(args.Update);

        for (int i = 0; i < 10; i++) array.Push(i);

        using BcrdtDocument replica = Doc(2);
        for (int i = updates.Count - 1; i >= 0; i--) replica.ApplyUpdate(updates[i]);

        Assert.AreEqual(Json(source), Json(replica));
    }

    [TestMethod]
    public void Incremental_sync_sends_only_what_the_peer_lacks()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);

        a.GetText("body").Insert(0, new string('x', 20_000));
        Exchange(a, b);

        a.GetText("body").Insert(0, "y");
        byte[] delta = a.EncodeStateAsUpdate(b.GetStateVector());

        Assert.IsTrue(delta.Length < 200, $"Incremental update was {delta.Length} bytes; expected a small delta.");
        b.ApplyUpdate(delta);
        Assert.AreEqual(a.GetText("body").ToString(), b.GetText("body").ToString());
    }

    [TestMethod]
    public void Nested_types_round_trip_through_an_update()
    {
        using BcrdtDocument a = Doc(1);
        BcrdtMap root = a.GetMap("root");

        BcrdtMap child = root.EnsureMap("child");
        child.Set("name", "nested");
        BcrdtArray list = child.EnsureArray("list");
        list.Push(1, 2, 3);
        BcrdtText text = root.EnsureText("body");
        text.Insert(0, "inner text");

        using BcrdtDocument b = Doc(2);
        b.ApplyUpdate(a.EncodeStateAsUpdate());

        Assert.AreEqual(Json(a), Json(b));
        Assert.AreEqual("nested", b.GetMap("root").GetMap("child")!.Get("name"));
        Assert.AreEqual("inner text", b.GetMap("root").GetText("body")!.ToString());
    }

    [TestMethod]
    public void Run_length_encoding_keeps_sequential_typing_compact()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");

        for (int i = 0; i < 2000; i++) text.Insert(text.Count, "a");

        int blockCount = doc.Store.GetClientStructs(doc.ClientId)!.Count;
        Assert.IsTrue(blockCount < 20, $"Sequential typing produced {blockCount} blocks; expected them to merge.");
        Assert.AreEqual(2000, text.Count);
    }

    [TestMethod]
    public void Rich_text_formatting_survives_a_round_trip()
    {
        using BcrdtDocument a = Doc(1);
        BcrdtText text = a.GetText("body");

        text.Insert(0, "Hello world");
        text.Format(0, 5, new Dictionary<string, object?> { ["bold"] = true });

        using BcrdtDocument b = Doc(2);
        b.ApplyUpdate(a.EncodeStateAsUpdate());

        var delta = b.GetText("body").ToDelta();
        Assert.AreEqual("Hello world", b.GetText("body").ToString());
        Assert.AreEqual(2, delta.Count);
        Assert.AreEqual("Hello", delta[0].Content);
        Assert.AreEqual(true, delta[0].Attributes!["bold"]);
        Assert.AreEqual(" world", delta[1].Content);
        Assert.IsNull(delta[1].Attributes);
    }

    [TestMethod]
    public void Formatting_marks_do_not_shift_character_indices()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");

        text.Insert(0, "abcdef");
        text.Format(2, 2, new Dictionary<string, object?> { ["italic"] = true });

        Assert.AreEqual(6, text.Count);
        text.Insert(6, "g");
        Assert.AreEqual("abcdefg", text.ToString());
    }

    [TestMethod]
    public void Set_is_add_wins_under_concurrent_add_and_remove()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);

        a.GetSet("tags").Add("urgent");
        Exchange(a, b);

        // B removes the element it can see. A, unaware, replaces it with a fresh one. The removal
        // targets a specific element that A's addition never observed, so the addition survives.
        b.GetSet("tags").Remove("urgent");
        a.GetSet("tags").Remove("urgent");
        a.GetSet("tags").Add("urgent");
        Exchange(a, b);

        Assert.IsTrue(a.GetSet("tags").Contains("urgent"));
        Assert.IsTrue(b.GetSet("tags").Contains("urgent"));
    }

    [TestMethod]
    public void Set_deduplicates_concurrent_additions_of_equal_values()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);

        a.GetSet("tags").Add("shared");
        b.GetSet("tags").Add("shared");
        Exchange(a, b);

        Assert.AreEqual(1, a.GetSet("tags").Count);
        Assert.AreEqual(1, b.GetSet("tags").Count);
    }

    [TestMethod]
    public void Set_removal_sticks_when_it_is_not_concurrent_with_an_addition()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);

        a.GetSet("tags").Add("stale");
        Exchange(a, b);

        b.GetSet("tags").Remove("stale");
        Exchange(a, b);

        Assert.IsFalse(a.GetSet("tags").Contains("stale"));
        Assert.IsFalse(b.GetSet("tags").Contains("stale"));
    }

    [TestMethod]
    public void Transaction_batches_many_changes_into_one_update()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtArray array = doc.GetArray("items");

        int updateCount = 0;
        doc.Updated += _ => updateCount++;

        doc.Transact(_ =>
        {
            for (int i = 0; i < 100; i++) array.Push(i);
        });

        Assert.AreEqual(1, updateCount);
        Assert.AreEqual(100, array.Count);
    }

    [TestMethod]
    public void Events_report_a_usable_delta()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtArray array = doc.GetArray("items");
        array.Push("a", "b", "c");

        List<string> observed = [];
        array.Observe((e, _) =>
        {
            foreach (var op in e.Delta) observed.Add(op.ToString()!);
        });

        doc.Transact(_ =>
        {
            array.Delete(0, 1);
            array.Push("d");
        });

        Assert.Contains("delete 1", observed);
        Assert.Contains(s => s.StartsWith("insert", StringComparison.Ordinal), observed);
    }

    [TestMethod]
    public void Truncated_updates_are_rejected_without_corrupting_the_document()
    {
        using BcrdtDocument source = Doc(1);
        source.GetArray("items").Push("a", "b", "c");
        byte[] update = source.EncodeStateAsUpdate();

        using BcrdtDocument target = Doc(2);
        target.GetArray("items").Push("existing");

        int rejections = 0;
        for (int cut = 1; cut < update.Length; cut++)
        {
            string before = BcrdtConvergenceHarness.Describe(target);
            try
            {
                target.ApplyUpdate(update.AsMemory(0, cut));
            }
            catch (BcrdtFormatException)
            {
                // The payload is incomplete. Rejection must be all-or-nothing: the whole update is
                // decoded before any of it is integrated.
                rejections++;
                Assert.AreEqual(before, BcrdtConvergenceHarness.Describe(target));
            }
        }

        // Most prefixes are malformed; the few that happen to be well-formed may apply.
        Assert.IsTrue(rejections > 0);
        Assert.Contains("existing", target.GetArray("items").ToArray().Select(v => v?.ToString()));
    }

    [TestMethod]
    public void State_vector_round_trips()
    {
        var vector = new BcrdtStateVector();
        vector.Set(1, 10);
        vector.Set(ulong.MaxValue, 999_999);

        BcrdtStateVector decoded = BcrdtStateVector.Decode(vector.Encode());
        Assert.IsTrue(vector.StructurallyEquals(decoded));
        Assert.AreEqual(10, decoded.Get(1));
        Assert.AreEqual(999_999, decoded.Get(ulong.MaxValue));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(true)]
    [DataRow(42)]
    [DataRow(-42)]
    [DataRow(3.5)]
    [DataRow("hello")]
    public void Values_round_trip_through_the_codec(object? value)
    {
        object? decoded = BcrdtAnyCodec.Decode(BcrdtAnyCodec.Encode(value));
        Assert.IsTrue(BcrdtAnyCodec.DeepEquals(value, decoded), $"{value} did not survive a round trip: got {decoded}.");
    }

    [TestMethod]
    public void Nested_values_round_trip_through_the_codec()
    {
        var value = new Dictionary<string, object?>
        {
            ["name"] = "test",
            ["tags"] = new List<object?> { "a", "b" },
            ["nested"] = new Dictionary<string, object?> { ["n"] = 1L },
        };

        object? decoded = BcrdtAnyCodec.Decode(BcrdtAnyCodec.Encode(value));
        Assert.IsTrue(BcrdtAnyCodec.DeepEquals(value, decoded));
    }

    [TestMethod]
    public void Object_encoding_is_canonical_regardless_of_key_order()
    {
        var first = new Dictionary<string, object?> { ["b"] = 1, ["a"] = 2 };
        var second = new Dictionary<string, object?> { ["a"] = 2, ["b"] = 1 };

        Assert.AreSequenceEqual(BcrdtAnyCodec.Encode(first), BcrdtAnyCodec.Encode(second));
    }
}
