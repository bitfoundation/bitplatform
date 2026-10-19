namespace Bit.Bcrdt.Tests;

/// <summary>
/// Reconstructing a document as it was at a point in the past.
/// </summary>
/// <remarks>
/// A snapshot names a moment rather than copying one, which is what makes version history
/// affordable - a hundred versions cost a hundred small records rather than a hundred documents.
/// The price is that the document has to keep the tombstones a snapshot refers to, and these tests
/// pin down both halves of that bargain.
/// </remarks>
[TestClass]
public class SnapshotTests
{
    private static BcrdtDocument History(ulong clientId = 1) =>
        new(new BcrdtDocumentOptions { ClientId = clientId, GarbageCollect = false });

    [TestMethod]
    public void A_snapshot_reconstructs_the_text_as_it_was()
    {
        using BcrdtDocument doc = History();
        BcrdtText text = doc.GetText("body");

        text.Insert(0, "first draft");
        BcrdtSnapshot version1 = BcrdtSnapshot.Capture(doc);

        text.Delete(0, 5);
        text.Insert(0, "final");
        BcrdtSnapshot version2 = BcrdtSnapshot.Capture(doc);

        text.Insert(text.Count, " plus more");

        Assert.AreEqual("final draft plus more", text.ToString());

        using BcrdtDocument restored1 = BcrdtSnapshot.RestoreDocument(doc, version1);
        using BcrdtDocument restored2 = BcrdtSnapshot.RestoreDocument(doc, version2);

        Assert.AreEqual("first draft", restored1.GetText("body").ToString());
        Assert.AreEqual("final draft", restored2.GetText("body").ToString());
    }

    [TestMethod]
    public void A_snapshot_reconstructs_map_and_array_state()
    {
        using BcrdtDocument doc = History();

        doc.GetMap("meta").Set("title", "v1");
        doc.GetArray("items").Push("a", "b");
        BcrdtSnapshot version1 = BcrdtSnapshot.Capture(doc);

        doc.GetMap("meta").Set("title", "v2");
        doc.GetArray("items").Push("c");
        doc.GetArray("items").Delete(0, 1);

        using BcrdtDocument restored = BcrdtSnapshot.RestoreDocument(doc, version1);

        Assert.AreEqual("v1", restored.GetMap("meta").Get("title"));
        Assert.AreSequenceEqual(new object?[] { "a", "b" }, restored.GetArray("items").ToArray());
    }

    [TestMethod]
    public void A_snapshot_is_far_smaller_than_the_document_it_names()
    {
        using BcrdtDocument doc = History();
        doc.GetText("body").Insert(0, new string('x', 50_000));

        BcrdtSnapshot snapshot = BcrdtSnapshot.Capture(doc);

        int snapshotSize = snapshot.Encode().Length;
        int documentSize = doc.EncodeStateAsUpdate().Length;

        Assert.IsTrue(
            snapshotSize < 200,
            $"A snapshot of a {documentSize} byte document took {snapshotSize} bytes; it should name a " +
            "moment rather than copy the content.");
    }

    [TestMethod]
    public void A_snapshot_round_trips_through_its_encoding()
    {
        using BcrdtDocument doc = History();
        doc.GetText("body").Insert(0, "content");
        doc.GetText("body").Delete(0, 3);

        BcrdtSnapshot original = BcrdtSnapshot.Capture(doc);
        BcrdtSnapshot decoded = BcrdtSnapshot.Decode(original.Encode());

        using BcrdtDocument fromOriginal = BcrdtSnapshot.RestoreDocument(doc, original);
        using BcrdtDocument fromDecoded = BcrdtSnapshot.RestoreDocument(doc, decoded);

        BcrdtConvergenceHarness.AssertConverged(fromOriginal, fromDecoded);
    }

    [TestMethod]
    public void Snapshotting_a_collecting_document_fails_loudly_rather_than_lying()
    {
        using var collecting = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1, GarbageCollect = true });
        collecting.GetText("body").Insert(0, "content");

        BcrdtSnapshot snapshot = BcrdtSnapshot.Capture(collecting);

        // The tombstones a snapshot needs have been reclaimed, so the answer would be wrong. Saying
        // so is much better than quietly returning a document that is missing its history.
        InvalidOperationException error = Assert.ThrowsExactly<InvalidOperationException>(
            () => BcrdtSnapshot.RestoreDocument(collecting, snapshot));

        Assert.Contains("GarbageCollect", error.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_restored_document_can_still_be_edited_and_synchronized()
    {
        using BcrdtDocument doc = History();
        doc.GetText("body").Insert(0, "original");
        BcrdtSnapshot snapshot = BcrdtSnapshot.Capture(doc);
        doc.GetText("body").Insert(8, " and more");

        using BcrdtDocument restored = BcrdtSnapshot.RestoreDocument(doc, snapshot);
        restored.GetText("body").Insert(0, "restored: ");

        // Restoring produces an ordinary document, not a frozen artifact.
        Assert.AreEqual("restored: original", restored.GetText("body").ToString());

        using var peer = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 9, GarbageCollect = false });
        peer.ApplyUpdate(restored.EncodeStateAsUpdate());
        Assert.AreEqual("restored: original", peer.GetText("body").ToString());
    }

    [TestMethod]
    public void Visibility_of_specific_content_can_be_queried_directly()
    {
        using BcrdtDocument doc = History();
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "abc");

        BcrdtItemId firstCharacter = doc.Store.GetClientStructs(doc.ClientId)![0].Id;
        BcrdtSnapshot beforeDeletion = BcrdtSnapshot.Capture(doc);

        text.Delete(0, 1);
        BcrdtSnapshot afterDeletion = BcrdtSnapshot.Capture(doc);

        Assert.IsTrue(beforeDeletion.IsVisible(firstCharacter));
        Assert.IsFalse(afterDeletion.IsVisible(firstCharacter));
    }
}
