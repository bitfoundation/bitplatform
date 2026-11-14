using Bunit;

namespace Bit.Bortable.Tests;

/// <summary>
/// <c>ItemKey</c>: items told apart by a key of the app's - an entity's id - instead of by their <c>Equals</c>. A page
/// that loads its list again (new instances of the same records) keeps each item's element, key, focus and selection,
/// and a drag moves the instance the list holds now. Duplicates are recognised by the key, across lists too.
/// </summary>
[TestClass]
public class ItemKeyTests : BortableTestContext
{
    /// <summary>A record as a server sends it: a class compared by reference, identified by its id.</summary>
    private sealed class Card(int id, string title)
    {
        public int Id { get; } = id;
        public string Title { get; } = title;
        public override string ToString() => Title;
    }

    private static List<Card> Load(params string[] titles) => [.. titles.Select((t, i) => new Card(i + 1, t))];

    private static List<Card> Reload(IEnumerable<Card> cards) => [.. cards.Select(c => new Card(c.Id, c.Title))];

    private IRenderedComponent<Bortable<Card>> RenderCards(List<Card> cards, string id = "L1",
        Action<ComponentParameterCollectionBuilder<Bortable<Card>>>? extra = null, bool keyed = true)
        => RenderList(cards, c => c.Title, id, p =>
        {
            if (keyed) p.Add(c => c.ItemKey, c => c.Id);
            extra?.Invoke(p);
        });

    [TestMethod]
    public void ReloadedItemsKeepTheirKeys()
    {
        var cards = Load("A", "B", "C");
        var list = RenderCards(cards);
        var before = new[] { "A", "B", "C" }.Select(l => KeyOf(list, l)).ToList();

        list.Render(p => p.Add(c => c.Items, Reload(cards)));

        Assert.AreSequenceEqual(before, new[] { "A", "B", "C" }.Select(l => KeyOf(list, l)).ToList());
    }

    [TestMethod]
    public void WithoutAnItemKeyReloadedItemsAreNewItems()
    {
        var cards = Load("A", "B");
        var list = RenderCards(cards, keyed: false);
        var before = KeyOf(list, "A");

        list.Render(p => p.Add(c => c.Items, Reload(cards)));

        Assert.AreNotEqual(before, KeyOf(list, "A"));
    }

    [TestMethod]
    public async Task ADragAfterAReloadMovesTheInstanceTheListHoldsNow()
    {
        var cards = Load("A", "B", "C");
        var list = RenderCards(cards);
        var reloaded = Reload(cards);
        list.Render(p => p.Add(c => c.Items, reloaded));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        await DropAsync(list);

        Assert.AreEqual("B,C,A", string.Join(",", reloaded.Select(c => c.Title)));
        Assert.IsTrue(reloaded.All(c => !cards.Contains(c)), "An instance of the first load came back into the list.");
    }

    [TestMethod]
    public async Task AReloadDuringADragKeepsTheNewInstances()
    {
        var cards = Load("A", "B", "C");
        var list = RenderCards(cards);

        await DragStartAsync(list, "A");
        var reloaded = Reload(cards);
        list.Render(p => p.Add(c => c.Items, reloaded));
        await DragOverAsync(list, 3);
        await DropAsync(list);

        Assert.AreEqual("B,C,A", string.Join(",", reloaded.Select(c => c.Title)));
        Assert.IsTrue(reloaded.All(c => !cards.Contains(c)), "The instance the drag started with came back into the list.");
    }

    [TestMethod]
    public async Task AReloadDuringADragIntoAnotherListMovesTheNewInstance()
    {
        var left = Load("A", "B");
        List<Card> right = [new(10, "X")];
        var l1 = RenderCards(left, "L1", p => p.Add(c => c.Group, "g"));
        var l2 = RenderCards(right, "L2", p => p.Add(c => c.Group, "g"));
        Card? reported = null;
        l2.Render(p => p.Add(c => c.OnAdd, e => reported = e.Item));

        await DragStartAsync(l1, "A");
        var reloaded = Reload(left);
        l1.Render(p => p.Add(c => c.Items, reloaded));
        await DragOverAsync(l2, 0);
        await DropAsync(l2);

        Assert.AreEqual("B", string.Join(",", reloaded.Select(c => c.Title)));
        Assert.AreEqual("A,X", string.Join(",", right.Select(c => c.Title)));
        Assert.IsTrue(right.All(c => !left.Contains(c)), "The instance the drag started with went into the list.");
        Assert.AreSame(right[0], reported);
    }

    [TestMethod]
    public async Task AReloadedCopyOfTheDraggedItemIsNotItsOwnSwapPartner()
    {
        var cards = Load("A", "B", "C");
        var list = RenderCards(cards, extra: p => p.Add(c => c.Swap, true));
        var updates = 0;
        list.Render(p => p.Add(c => c.OnUpdate, _ => updates++));

        await DragStartAsync(list, "A");
        var reloaded = Reload(cards);
        list.Render(p => p.Add(c => c.Items, reloaded));
        await DragOverAsync(list, 0);

        Assert.AreEqual("", LabelsWithClass(list, "bit-bortable-swap-highlight"));
        await DropAsync(list);

        Assert.AreEqual("A,B,C", string.Join(",", reloaded.Select(c => c.Title)));
        Assert.IsTrue(reloaded.All(c => !cards.Contains(c)), "The instance the drag started with came back into the list.");
        Assert.AreEqual(0, updates);
    }

    [TestMethod]
    public async Task ASwapAfterAReloadDuringTheDragTradesTheNewInstances()
    {
        var cards = Load("A", "B", "C");
        var list = RenderCards(cards, extra: p => p.Add(c => c.Swap, true));

        await DragStartAsync(list, "A");
        var reloaded = Reload(cards);
        list.Render(p => p.Add(c => c.Items, reloaded));
        await DragOverAsync(list, 2);
        await DropAsync(list);

        Assert.AreEqual("C,B,A", string.Join(",", reloaded.Select(c => c.Title)));
        Assert.IsTrue(reloaded.All(c => !cards.Contains(c)), "The instance the drag started with came back into the list.");
    }

    [TestMethod]
    public async Task AnInlineItemKeyGivenAgainOnEveryRenderKeepsTheSelection()
    {
        // A new delegate of the same lambda on every call, as a page that writes it inline gives on every render.
        static Func<Card, object> KeyBy(string scope) => c => $"{scope}:{c.Id}";

        var cards = Load("A", "B", "C");
        var list = RenderCards(cards, keyed: false, extra: p =>
        {
            p.Add(c => c.MultiDrag, true);
            p.Add(c => c.ItemKey, KeyBy("cards"));
        });
        await ClickAsync(list, "B");
        var keys = new[] { "A", "B", "C" }.Select(l => KeyOf(list, l)).ToList();

        for (int i = 0; i < 3; i++)
            list.Render(p => p.Add(c => c.ItemKey, KeyBy("cards")));
        list.Render(p => p.Add(c => c.Items, Reload(cards)));

        Assert.AreEqual("B", LabelsWithClass(list, "bit-bortable-selected"));
        Assert.AreSequenceEqual(keys, new[] { "A", "B", "C" }.Select(l => KeyOf(list, l)).ToList());
    }

    [TestMethod]
    public async Task TheSelectionSurvivesAReload()
    {
        var cards = Load("A", "B", "C");
        var published = new List<IReadOnlyCollection<Card>>();
        var list = RenderCards(cards, extra: p =>
        {
            p.Add(c => c.MultiDrag, true);
            p.Add(c => c.SelectedItemsChanged, s => published.Add(s));
        });
        await ClickAsync(list, "A");
        await ClickAsync(list, "C");

        list.Render(p => p.Add(c => c.Items, Reload(cards)));

        Assert.AreEqual("A,C", LabelsWithClass(list, "bit-bortable-selected"));
    }

    [TestMethod]
    public async Task AMultiDragAfterAReloadCarriesTheWholeSelection()
    {
        var cards = Load("A", "B", "C", "D");
        var list = RenderCards(cards, extra: p => p.Add(c => c.MultiDrag, true));
        await ClickAsync(list, "A");
        await ClickAsync(list, "B");
        var reloaded = Reload(cards);
        list.Render(p => p.Add(c => c.Items, reloaded));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 4);
        await DropAsync(list);

        Assert.AreEqual("C,D,A,B", string.Join(",", reloaded.Select(c => c.Title)));
    }

    [TestMethod]
    public async Task AnItemWhoseKeyIsInTheTargetIsADuplicate()
    {
        // Another instance of B (same id) is already in the target: dropping B there would key two elements alike.
        List<Card> left = [new(1, "A"), new(2, "B")], right = [new(2, "B"), new(3, "X")];
        var l1 = RenderCards(left, "L1", p => p.Add(c => c.Group, "g"));
        var l2 = RenderCards(right, "L2", p => p.Add(c => c.Group, "g"));

        var started = await DragStartAsync(l1, "B");

        Assert.DoesNotContain("L2", started!.Lists);
        Assert.IsFalse(await DragOverAsync(l2, 0));
    }

    [TestMethod]
    public async Task ACloneNeedsAKeyOfItsOwn()
    {
        int next = 100;
        List<Card> palette = [new(1, "A")], sameId = [], newId = [];
        var source = RenderCards(palette, "P", p =>
        {
            p.Add(c => c.Group, "g");
            p.Add(c => c.Pull, BortablePull.Clone);
        });
        var copiesSameId = RenderCards(sameId, "S", p =>
        {
            p.Add(c => c.Group, "g");
        });
        var copiesNewId = RenderCards(newId, "N", p => p.Add(c => c.Group, "g"));

        // A copy that keeps the original's id cannot go next to an item with that id; one with an id of its own can.
        source.Render(p => p.Add(c => c.Clone, c => new Card(c.Id, c.Title)));
        await DragStartAsync(source, "A");
        await DragOverAsync(copiesSameId, 0);
        await DropAsync(copiesSameId);
        await DragStartAsync(source, "A");
        await DragOverAsync(copiesSameId, 1);
        await DropAsync(copiesSameId);

        source.Render(p => p.Add(c => c.Clone, c => new Card(++next, c.Title)));
        await DragStartAsync(source, "A");
        await DragOverAsync(copiesNewId, 0);
        await DropAsync(copiesNewId);
        await DragStartAsync(source, "A");
        await DragOverAsync(copiesNewId, 1);
        await DropAsync(copiesNewId);

        Assert.HasCount(1, sameId);
        Assert.HasCount(2, newId);
        Assert.AreSequenceEqual([101, 102], newId.Select(c => c.Id).ToList());
    }

    [TestMethod]
    public void TheElementsAreKeyedByTheKey()
    {
        // Two equal-looking titles with distinct ids render side by side: the elements are keyed by id, not by item.
        List<Card> cards = [new(1, "Same"), new(2, "Same")];
        var list = RenderCards(cards);

        Assert.AreEqual("Same,Same", Labels(list));
        var keys = list.FindAll("[data-bortable-key]").Select(e => e.GetAttribute("data-bortable-key")).ToList();
        Assert.HasCount(2, keys.Distinct().ToList());
    }

    [TestMethod]
    public async Task AKeyboardDragFindsTheReloadedItem()
    {
        var cards = Load("A", "B", "C");
        var list = RenderCards(cards);
        var reloaded = Reload(cards);
        list.Render(p => p.Add(c => c.Items, reloaded));

        var picked = await Renderer.Dispatcher.InvokeAsync(() => Zone(list).KeyAsync(KeyOf(list, "B"), "pick", null, -1));
        await Renderer.Dispatcher.InvokeAsync(() => Zone(list).KeyAsync("", "to", null, 0, picked.Drag));
        await Renderer.Dispatcher.InvokeAsync(() => Zone(list).KeyAsync("", "drop", null, -1, picked.Drag));

        Assert.IsTrue(picked.Dragging);
        Assert.AreEqual("B,A,C", string.Join(",", reloaded.Select(c => c.Title)));
    }
}
