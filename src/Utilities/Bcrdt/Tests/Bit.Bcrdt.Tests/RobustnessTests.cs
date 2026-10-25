using System.Text;

namespace Bit.Bcrdt.Tests;

/// <summary>
/// What happens when the bytes are wrong.
/// </summary>
/// <remarks>
/// Updates arrive from other people's machines, so "the input is well-formed" is not an assumption
/// this code is allowed to make. The contract is narrow and absolute: a bad payload raises
/// <see cref="BcrdtFormatException"/>, the receiving document is unchanged, and nothing
/// crashes, hangs or allocates without bound.
/// </remarks>
[TestClass]
public class RobustnessTests
{
    private static byte[] SampleUpdate()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 7 });
        doc.GetText("body").Insert(0, "hello world");
        doc.GetArray("items").Push(1, "two", 3.0, true, null);
        doc.GetMap("meta").Set("key", "value");
        doc.GetMap("meta").EnsureMap("nested").Set("inner", 42);
        return doc.EncodeStateAsUpdate();
    }

    private static bool IsRejection(Exception ex) =>
        ex is BcrdtFormatException
        || ex is AggregateException aggregate && aggregate.InnerExceptions.All(IsRejection);

    [TestMethod]
    public void Every_truncation_of_a_valid_update_is_rejected_or_ignored()
    {
        byte[] update = SampleUpdate();

        foreach (byte[] truncated in BcrdtUpdateCorrupter.Truncations(update))
        {
            using var target = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
            try
            {
                target.ApplyUpdate(truncated);
            }
            catch (Exception ex) when (IsRejection(ex))
            {
                continue;
            }

            // A prefix that happens to be structurally valid is allowed to succeed. What is not
            // allowed is for it to leave the document unreadable.
            _ = target.ToJson();
        }
    }

    [TestMethod]
    public void Single_byte_corruptions_never_crash_or_hang()
    {
        byte[] update = SampleUpdate();

        foreach (byte[] mutated in BcrdtUpdateCorrupter.SingleByteMutations(update))
        {
            using var target = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
            try
            {
                target.ApplyUpdate(mutated);
                _ = target.ToJson();
            }
            catch (Exception ex) when (IsRejection(ex))
            {
                // Expected for most mutations.
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IndexOutOfRangeException)
            {
                Assert.Fail(
                    $"A corrupted update produced {ex.GetType().Name} instead of a clean rejection: {ex.Message}");
            }
        }
    }

    [TestMethod]
    public void Random_and_hostile_payloads_are_rejected_cleanly()
    {
        foreach (byte[] payload in BcrdtUpdateCorrupter.Garbage())
        {
            using var target = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
            try
            {
                target.ApplyUpdate(payload);
            }
            catch (Exception ex) when (IsRejection(ex))
            {
                continue;
            }
            catch (Exception ex)
            {
                Assert.Fail(
                    $"Payload of {payload.Length} byte(s) produced {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    [TestMethod]
    public void A_rejected_update_leaves_the_document_exactly_as_it_was()
    {
        byte[] update = SampleUpdate();

        using var target = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        target.GetText("body").Insert(0, "existing content");
        target.GetMap("meta").Set("preserved", true);
        string before = BcrdtConvergenceHarness.Describe(target);

        foreach (byte[] payload in BcrdtUpdateCorrupter.Garbage().Concat(BcrdtUpdateCorrupter.Truncations(update)))
        {
            try
            {
                target.ApplyUpdate(payload);
            }
            catch (Exception ex) when (IsRejection(ex))
            {
                Assert.AreEqual(before, BcrdtConvergenceHarness.Describe(target));
            }
        }
    }

    [TestMethod]
    public void A_declared_collection_length_larger_than_the_payload_is_rejected_before_allocating()
    {
        // Five bytes claiming billions of entries. A decoder that sizes a collection from a declared
        // count without checking it can be made to exhaust memory from almost nothing.
        byte[] hostile = [0xFF, 0xFF, 0xFF, 0xFF, 0x7F, 0x00];

        using var target = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        Assert.ThrowsExactly<BcrdtFormatException>(() => target.ApplyUpdate(hostile));
    }

    [TestMethod]
    public void A_varint_of_continuation_bytes_terminates_rather_than_spinning()
    {
        byte[] runaway = new byte[64];
        Array.Fill(runaway, (byte)0x80);

        var reader = new BcrdtBufferReader(runaway);
        Assert.ThrowsExactly<BcrdtFormatException>(() => reader.ReadVarUInt());
    }

    [TestMethod]
    public void Protocol_messages_from_an_unknown_version_are_rejected()
    {
        byte[] future = [99, 0];
        Assert.ThrowsExactly<BcrdtFormatException>(
            () => BcrdtSyncProtocol.Decode(future));
    }

    [TestMethod]
    public void Protocol_messages_of_an_unknown_kind_are_rejected()
    {
        byte[] unknown = [BcrdtSyncProtocol.Version, 200];
        Assert.ThrowsExactly<BcrdtFormatException>(
            () => BcrdtSyncProtocol.Decode(unknown));
    }

    [TestMethod]
    [DataRow("plain ascii")]
    [DataRow("accented \u00e9\u00e0\u00fc")]
    [DataRow("emoji \ud83d\ude00 and more \ud83c\udf89")]
    [DataRow("cjk \u4f60\u597d\u4e16\u754c")]
    [DataRow("rtl \u05e9\u05dc\u05d5\u05dd")]
    public void Text_survives_a_round_trip_through_the_wire(string content)
    {
        using var source = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        source.GetText("body").Insert(0, content);

        using var target = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        target.ApplyUpdate(source.EncodeStateAsUpdate());

        Assert.AreEqual(content, target.GetText("body").ToString());
    }

    [TestMethod]
    public void A_surrogate_pair_survives_being_split_between_two_blocks()
    {
        using var source = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = source.GetText("body");
        text.Insert(0, "a\ud83d\ude00b");

        // Insert exactly between the two halves of the emoji, which forces a split there.
        text.Insert(2, "X");
        Assert.AreEqual("a\ud83dX\ude00b", text.ToString());

        using var target = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        target.ApplyUpdate(source.EncodeStateAsUpdate());

        // The halves must survive the wire exactly, or removing the intruder would not restore the
        // original character.
        Assert.AreEqual(text.ToString(), target.GetText("body").ToString());

        target.GetText("body").Delete(2, 1);
        Assert.AreEqual("a\ud83d\ude00b", target.GetText("body").ToString());
    }

    [TestMethod]
    public void Lone_surrogates_round_trip_through_the_string_encoder()
    {
        string loneHigh = "before\ud83dafter";
        string loneLow = "before\ude00after";

        foreach (string value in new[] { loneHigh, loneLow })
        {
            var writer = new BcrdtBufferWriter(64);
            try
            {
                writer.WriteString(value);
                var reader = new BcrdtBufferReader(writer.ToArray());
                Assert.AreEqual(value, reader.ReadString());
            }
            finally
            {
                writer.Dispose();
            }
        }
    }

    [TestMethod]
    public void Deeply_nested_values_are_rejected_rather_than_overflowing_the_stack()
    {
        // A payload that nests far enough to blow the stack of a naïve recursive decoder.
        var writer = new BcrdtBufferWriter(512);
        try
        {
            for (int i = 0; i < 200; i++)
            {
                writer.WriteByte((byte)BcrdtAnyTag.Array);
                writer.WriteVarUInt(1);
            }
            writer.WriteByte((byte)BcrdtAnyTag.Null);

            byte[] payload = writer.ToArray();
            Assert.ThrowsExactly<BcrdtFormatException>(() => BcrdtAnyCodec.Decode(payload));
        }
        finally
        {
            writer.Dispose();
        }
    }

    [TestMethod]
    public void Merging_updates_produces_the_same_document_as_applying_them_individually()
    {
        using var source = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        List<byte[]> updates = [];
        source.Updated += args => updates.Add(args.Update);

        var generator = new BcrdtOperationGenerator(99);
        for (int i = 0; i < 30; i++) generator.ApplyRandomTransaction(source);

        using var applied = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        foreach (byte[] update in updates) applied.ApplyUpdate(update);

        using var merged = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 3 });
        merged.ApplyUpdate(BcrdtUpdateOperations.Merge([.. updates.Select(u => (ReadOnlyMemory<byte>)u)]));

        BcrdtConvergenceHarness.AssertConverged(applied, merged);
    }

    [TestMethod]
    public void A_merged_update_is_smaller_than_the_updates_it_replaces()
    {
        using var source = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        List<byte[]> updates = [];
        source.Updated += args => updates.Add(args.Update);

        BcrdtText text = source.GetText("body");
        for (int i = 0; i < 500; i++) text.Insert(text.Count, "x");

        int total = updates.Sum(u => u.Length);
        byte[] merged = BcrdtUpdateOperations.Merge([.. updates.Select(u => (ReadOnlyMemory<byte>)u)]);

        Assert.IsTrue(
            merged.Length * 4 < total,
            $"Compaction of {updates.Count} updates went from {total} to {merged.Length} bytes, " +
            "which is not the reduction run-length encoding should give.");
    }

    [TestMethod]
    public void A_state_vector_can_be_read_from_an_update_without_applying_it()
    {
        using var source = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 42 });
        source.GetText("body").Insert(0, "content");

        byte[] update = source.EncodeStateAsUpdate();
        BcrdtStateVector decoded = BcrdtUpdateOperations.DecodeStateVector(update);

        Assert.IsTrue(source.GetStateVector().StructurallyEquals(decoded));
    }

    [TestMethod]
    public void Diffing_an_update_yields_only_what_the_peer_lacks()
    {
        using var source = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = source.GetText("body");
        text.Insert(0, new string('a', 5000));

        BcrdtStateVector midpoint = source.GetStateVector();
        text.Insert(text.Count, "tail");

        byte[] full = source.EncodeStateAsUpdate();
        byte[] diff = BcrdtUpdateOperations.Diff(full, midpoint);

        Assert.IsTrue(diff.Length < 200, $"Diff was {diff.Length} bytes; expected only the tail.");

        using var target = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        target.ApplyUpdate(BcrdtUpdateOperations.Diff(full, new BcrdtStateVector()));
        Assert.AreEqual(text.ToString(), target.GetText("body").ToString());
    }

    [TestMethod]
    public void Crc32_detects_a_single_flipped_bit()
    {
        byte[] payload = Encoding.UTF8.GetBytes("the quick brown fox jumps over the lazy dog");
        uint original = BcrdtCrc32.Compute(payload);

        for (int i = 0; i < payload.Length; i++)
        {
            byte[] flipped = (byte[])payload.Clone();
            flipped[i] ^= 0x01;
            Assert.AreNotEqual(original, BcrdtCrc32.Compute(flipped));
        }
    }
}
