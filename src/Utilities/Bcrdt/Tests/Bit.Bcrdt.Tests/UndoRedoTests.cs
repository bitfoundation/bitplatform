namespace Bit.Bcrdt.Tests;

/// <summary>
/// Undo has to mean "reverse what I did", not "restore how the document looked".
/// </summary>
/// <remarks>
/// The second reading is what a single-user editor implements and what a collaborative one must
/// not: rolling the document back would discard everyone else's concurrent work. These tests pin
/// down the difference.
/// </remarks>
[TestClass]
public class UndoRedoTests
{
    private static BcrdtDocument Doc(ulong clientId) => new(new BcrdtDocumentOptions { ClientId = clientId });

    private static BcrdtUndoManagerOptions Immediate => new() { CaptureTimeout = TimeSpan.Zero };

    [TestMethod]
    public void Undo_reverses_an_insertion_and_redo_restores_it()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        using var undo = new BcrdtUndoManager(text, Immediate);

        text.Insert(0, "hello");
        text.Insert(5, " world");

        Assert.AreEqual("hello world", text.ToString());

        undo.Undo();
        Assert.AreEqual("hello", text.ToString());

        undo.Redo();
        Assert.AreEqual("hello world", text.ToString());
    }

    [TestMethod]
    public void Undo_restores_deleted_content()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        using var undo = new BcrdtUndoManager(text, Immediate);

        text.Insert(0, "hello world");
        text.Delete(5, 6);
        Assert.AreEqual("hello", text.ToString());

        undo.Undo();
        Assert.AreEqual("hello world", text.ToString());
    }

    [TestMethod]
    public void Undo_leaves_a_collaborator_edit_untouched()
    {
        using BcrdtDocument local = Doc(1);
        using BcrdtDocument remote = Doc(2);

        BcrdtText localText = local.GetText("body");
        using var undo = new BcrdtUndoManager(localText, Immediate);

        localText.Insert(0, "local");
        Sync(local, remote);

        // The collaborator appends while we are not looking.
        remote.GetText("body").Insert(5, "-remote");
        Sync(remote, local);

        Assert.AreEqual("local-remote", localText.ToString());

        // Undoing our own insertion must remove exactly our five characters.
        undo.Undo();
        Assert.AreEqual("-remote", localText.ToString());
    }

    [TestMethod]
    public void Undo_does_not_capture_changes_that_arrived_from_a_peer()
    {
        using BcrdtDocument local = Doc(1);
        using BcrdtDocument remote = Doc(2);

        BcrdtText localText = local.GetText("body");
        using var undo = new BcrdtUndoManager(localText, Immediate);

        remote.GetText("body").Insert(0, "theirs");
        local.ApplyUpdate(remote.EncodeStateAsUpdate(), origin: "sync");

        Assert.AreEqual("theirs", localText.ToString());

        // There is nothing of ours to undo, so undo must be a no-op rather than deleting their text.
        Assert.IsFalse(undo.CanUndo);
        undo.Undo();
        Assert.AreEqual("theirs", localText.ToString());
    }

    [TestMethod]
    public void Changes_within_the_capture_window_collapse_into_one_step()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        using var undo = new BcrdtUndoManager(text, new BcrdtUndoManagerOptions { CaptureTimeout = TimeSpan.FromSeconds(30) });

        // Typing a word should be one undo step, not one per keystroke.
        foreach (char c in "hello") text.Insert(text.Count, c.ToString());

        Assert.AreEqual(1, undo.UndoCount);
        undo.Undo();
        Assert.AreEqual("", text.ToString());
    }

    [TestMethod]
    public void Stopping_capture_starts_a_new_step()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText text = doc.GetText("body");
        using var undo = new BcrdtUndoManager(text, new BcrdtUndoManagerOptions { CaptureTimeout = TimeSpan.FromSeconds(30) });

        text.Insert(0, "first");
        undo.StopCapturing();
        text.Insert(5, "second");

        Assert.AreEqual(2, undo.UndoCount);
        undo.Undo();
        Assert.AreEqual("first", text.ToString());
    }

    [TestMethod]
    public void Undo_works_across_map_writes()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtMap map = doc.GetMap("form");
        using var undo = new BcrdtUndoManager(map, Immediate);

        map.Set("title", "original");
        map.Set("title", "changed");

        undo.Undo();
        Assert.AreEqual("original", map.Get("title"));

        undo.Undo();
        Assert.IsFalse(map.ContainsKey("title"));

        undo.Redo();
        Assert.AreEqual("original", map.Get("title"));
    }

    [TestMethod]
    public void Undo_is_scoped_to_the_types_it_was_given()
    {
        using BcrdtDocument doc = Doc(1);
        BcrdtText tracked = doc.GetText("tracked");
        BcrdtText untracked = doc.GetText("untracked");
        using var undo = new BcrdtUndoManager(tracked, Immediate);

        tracked.Insert(0, "in scope");
        untracked.Insert(0, "out of scope");

        undo.Undo();

        Assert.AreEqual("", tracked.ToString());
        Assert.AreEqual("out of scope", untracked.ToString());
    }

    [TestMethod]
    public void An_undone_change_propagates_to_peers_like_any_other_edit()
    {
        using BcrdtDocument local = Doc(1);
        using BcrdtDocument remote = Doc(2);

        BcrdtText text = local.GetText("body");
        using var undo = new BcrdtUndoManager(text, Immediate);

        text.Insert(0, "hello");
        Sync(local, remote);
        Assert.AreEqual("hello", remote.GetText("body").ToString());

        undo.Undo();
        Sync(local, remote);

        // Undo is not a local rewind; it is an operation, and it replicates.
        Assert.AreEqual("", remote.GetText("body").ToString());
    }

    [TestMethod]
    public void Two_replicas_undoing_concurrently_still_converge()
    {
        using BcrdtDocument a = Doc(1);
        using BcrdtDocument b = Doc(2);

        BcrdtText textA = a.GetText("body");
        BcrdtText textB = b.GetText("body");
        using var undoA = new BcrdtUndoManager(textA, Immediate);
        using var undoB = new BcrdtUndoManager(textB, Immediate);

        textA.Insert(0, "aaa");
        Sync(a, b);
        textB.Insert(3, "bbb");
        Sync(b, a);

        // Each replica reverses its own contribution, unaware of the other doing the same.
        undoA.Undo();
        undoB.Undo();

        Sync(a, b);
        Sync(b, a);

        BcrdtConvergenceHarness.AssertConverged(a, b);
        Assert.AreEqual("", a.GetText("body").ToString());
    }

    [TestMethod]
    public void Garbage_collection_does_not_reclaim_content_an_undo_stack_still_needs()
    {
        using BcrdtDocument doc = Doc(1);
        Assert.IsTrue(doc.GarbageCollect, "This test is only meaningful with collection enabled.");

        BcrdtText text = doc.GetText("body");
        using var undo = new BcrdtUndoManager(text, Immediate);

        text.Insert(0, "recoverable");
        text.Delete(0, 11);

        // The content is deleted and collection has run, but the undo stack pinned it.
        undo.Undo();
        Assert.AreEqual("recoverable", text.ToString());
    }

    private static void Sync(BcrdtDocument from, BcrdtDocument to) =>
        to.ApplyUpdate(from.EncodeStateAsUpdate(to.GetStateVector()), origin: "sync");
}
