using Bunit;

namespace Bit.Bortable.Tests;

/// <summary>
/// <see cref="BortablePull.Clone"/> and read-only lists. A clone leaves the source untouched and puts a copy
/// in the target, which must be a distinct item: Blazor renders items with <c>@key</c>, so a copy equal to an
/// item already in the target would break rendering. These tests pin where the copy comes from (the factory,
/// <see cref="ICloneable"/>, or the item itself), which events a clone raises, and that a read-only list is a
/// valid source to clone from but never changes.
/// </summary>
[TestClass]
public class CloneTests : BortableTestContext
{
    /// <summary>A reference type: two cards are distinct even with the same name.</summary>
    private sealed class Card(string name)
    {
        public string Name { get; } = name;
    }

    /// <summary>Copied through <see cref="ICloneable"/> when the list has no Clone factory.</summary>
    private sealed class Note(string name) : ICloneable
    {
        public string Name { get; } = name;
        public bool IsCopy { get; private init; }
        public object Clone() => new Note(Name) { IsCopy = true };
    }

    /// <summary>A value-equal type: two tags with the same name are the same item as far as @key goes.</summary>
    private sealed record Tag(string Name);

    private (IRenderedComponent<Bortable<T>> Source, IRenderedComponent<Bortable<T>> Target) RenderClonePair<T>(
        IList<T> source, IList<T> target, Func<T, string> label,
        Action<ComponentParameterCollectionBuilder<Bortable<T>>>? sourceExtra = null,
        List<Raised<T>>? log = null)
    {
        var s = RenderList(source, label, "S", p =>
        {
            p.Add(c => c.Group, "g");
            p.Add(c => c.Pull, BortablePull.Clone);
            if (log is not null) Record(p, "S", log);
            sourceExtra?.Invoke(p);
        });
        var t = RenderList(target, label, "T", p =>
        {
            p.Add(c => c.Group, "g");
            if (log is not null) Record(p, "T", log);
        });
        return (s, t);
    }

    [TestMethod]
    public async Task CloningLeavesTheSourceUntouchedAndAddsADistinctCopy()
    {
        var a = new Card("a");
        var b = new Card("b");
        List<Card> source = [a, b], target = [];
        var (s, t) = RenderClonePair(source, target, c => c.Name, p => p.Add(c => c.Clone, c => new Card(c.Name)));

        await DragStartAsync(s, "a");
        await DragOverAsync(t, 0);

        // Unlike a move, the original stays visible while the copy is previewed.
        Assert.IsFalse(HasClass(s, "a", "bit-bortable-hidden"));
        Assert.IsNotNull(Placeholder(t));

        await DropAsync(t);

        Assert.AreSequenceEqual([a, b], source);
        var copy = target.Single();
        Assert.AreNotSame(a, copy);
        Assert.AreEqual("a", copy.Name);
        Assert.AreEqual("a,b", Labels(s));
        Assert.AreEqual("a", Labels(t));
    }

    [TestMethod]
    public async Task ACloneRaisesAddButNoRemoveAndReportsTheCopy()
    {
        var a = new Card("a");
        List<Card> source = [a], target = [];
        var log = new List<Raised<Card>>();
        var (s, t) = RenderClonePair(source, target, c => c.Name, p => p.Add(c => c.Clone, c => new Card(c.Name)), log);

        await DragStartAsync(s, "a");
        await DragOverAsync(t, 0);
        await DropAsync(t);

        Assert.AreEqual("Start:S Change:T ItemsChanged:T Add:T Sort:T End:S", Sequence(log));
        var add = Single(log, "Add", "T");
        Assert.AreSame(target[0], add.Item);
        Assert.AreSequenceEqual([target[0]], add.Items);
        Assert.IsTrue(add.CrossedLists);
        Assert.AreSame(target[0], Single(log, "End", "S").Item);
        // The drag started with the original, before any copy existed.
        Assert.AreSame(a, Single(log, "Start", "S").Item);
    }

    [TestMethod]
    public async Task TheSameItemCanBeClonedAgainAndAgain()
    {
        var a = new Card("a");
        List<Card> source = [a], target = [];
        var (s, t) = RenderClonePair(source, target, c => c.Name, p => p.Add(c => c.Clone, c => new Card(c.Name)));

        for (int i = 0; i < 2; i++)
        {
            await DragStartAsync(s, "a");
            await DragOverAsync(t, 0);
            await DropAsync(t);
        }

        Assert.HasCount(2, target);
        Assert.AreNotSame(target[0], target[1]);
        Assert.AreSequenceEqual([a], source);
    }

    [TestMethod]
    public async Task WithoutAFactoryAnICloneableItemIsCopiedWithItsCloneMethod()
    {
        var original = new Note("n");
        List<Note> source = [original], target = [];
        var (s, t) = RenderClonePair(source, target, n => n.Name);

        await DragStartAsync(s, "n");
        await DragOverAsync(t, 0);
        await DropAsync(t);

        Assert.IsTrue(target.Single().IsCopy);
        Assert.IsFalse(original.IsCopy);
        Assert.AreSame(original, source.Single());
    }

    [TestMethod]
    public async Task WithoutAFactoryAStringIsSharedSoItCanBeClonedOnlyOnce()
    {
        List<string> source = ["A"], target = [];
        var (s, t) = RenderClonePair(source, target, i => i);

        await DragStartAsync(s, "A");
        await DragOverAsync(t, 0);
        await DropAsync(t);
        Assert.AreSequenceEqual(["A"], target);

        // A second "A" would be a duplicate @key in the target, so the target refuses it up front.
        await DragStartAsync(s, "A");
        await DragOverAsync(t, 1);
        Assert.IsNull(Placeholder(t));
        await DropAsync(t);

        Assert.AreSequenceEqual(["A"], target);
        Assert.AreSequenceEqual(["A"], source);
    }

    [TestMethod]
    public async Task ACopyEqualToAnItemAlreadyInTheTargetIsRefusedAtTheDrop()
    {
        List<Tag> source = [new("a")], target = [new("b")];
        var log = new List<Raised<Tag>>();
        // A careless factory: the copy only exists at the drop, so only the drop can catch the clash.
        var (s, t) = RenderClonePair(source, target, x => x.Name, p => p.Add(c => c.Clone, _ => new Tag("b")), log);

        await DragStartAsync(s, "a");
        await DragOverAsync(t, 0);
        await DropAsync(t);

        Assert.AreSequenceEqual([new Tag("b")], target);
        Assert.AreSequenceEqual([new Tag("a")], source);
        Assert.AreEqual("Start:S Change:T End:S", Sequence(log));
        Assert.IsNull(Placeholder(t));
    }

    [TestMethod]
    public async Task MovingBackOverTheSourceCancelsTheClone()
    {
        List<string> source = ["A", "B"], target = [];
        var (s, t) = RenderClonePair(source, target, i => i);

        await DragStartAsync(s, "A");
        await DragOverAsync(t, 0);
        await DragOverAsync(s, 0);
        await DropAsync(s);

        Assert.IsEmpty(target);
        Assert.AreSequenceEqual(["A", "B"], source);
    }

    // ---- read-only lists ----

    [TestMethod]
    public async Task AReadOnlyListCanBeClonedFrom()
    {
        string[] palette = ["Red", "Green"];
        List<string> target = [];
        var (s, t) = RenderClonePair(palette, target, i => i);

        await DragStartAsync(s, "Green");
        await DragOverAsync(t, 0);
        await DropAsync(t);

        Assert.AreSequenceEqual(["Green"], target);
        Assert.AreSequenceEqual(["Red", "Green"], palette.AsEnumerable());
    }

    [TestMethod]
    public async Task AReadOnlyListIsNeverSorted()
    {
        string[] palette = ["Red", "Green", "Blue"];
        var (s, _) = RenderClonePair(palette, new List<string>(), i => i);

        await DragStartAsync(s, "Red");
        await DragOverAsync(s, 3);
        await DropAsync(s);

        // Sorting would write into the array: refused rather than throwing NotSupportedException.
        Assert.AreSequenceEqual(["Red", "Green", "Blue"], palette.AsEnumerable());
        Assert.AreEqual("Red,Green,Blue", Labels(s));
    }

    [TestMethod]
    public async Task AReadOnlyListWithPullMoveActsAsPullNone()
    {
        string[] palette = ["Red"];
        List<string> target = [];
        var s = RenderList(palette, "S", p => p.Add(c => c.Group, "g"));
        var t = RenderList(target, "T", p => p.Add(c => c.Group, "g"));

        await DragStartAsync(s, "Red");
        await DragOverAsync(t, 0);
        Assert.IsNull(Placeholder(t));
        await DropAsync(t);

        Assert.IsEmpty(target);
        Assert.AreSequenceEqual(["Red"], palette.AsEnumerable());
    }

    [TestMethod]
    public async Task AReadOnlyTargetRefusesItems()
    {
        List<string> source = ["A"];
        string[] frozen = ["X"];
        var s = RenderList(source, "S", p => p.Add(c => c.Group, "g"));
        var t = RenderList(frozen, "T", p => p.Add(c => c.Group, "g"));

        await DragStartAsync(s, "A");
        await DragOverAsync(t, 0);
        Assert.IsNull(Placeholder(t));
        await DropAsync(t);

        Assert.AreSequenceEqual(["A"], source);
        Assert.AreSequenceEqual(["X"], frozen.AsEnumerable());
    }
}
