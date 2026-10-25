using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bortable.Tests;

/// <summary>
/// The multi-drag selection as the page sees it: bound with <c>@bind-SelectedItems</c>, extended with Shift+click,
/// toggled from the keyboard with Shift+Space, and published again whenever a drag spends or clears it. And the
/// render cost of a drag step in a long list, with <c>IsolateItems</c>.
/// </summary>
[TestClass]
public class SelectionTests : BortableTestContext
{
    private IRenderedComponent<Bortable<string>> RenderSelectable(List<string> items, List<IReadOnlyCollection<string>> published,
        IReadOnlyCollection<string>? selected = null, string id = "L1", Action<ComponentParameterCollectionBuilder<Bortable<string>>>? extra = null)
        => RenderList(items, id, p =>
        {
            p.Add(c => c.MultiDrag, true);
            p.Add(c => c.Group, "g");
            if (selected is not null) p.Add(c => c.SelectedItems, selected);
            p.Add(c => c.SelectedItemsChanged, s => published.Add(s));
            extra?.Invoke(p);
        });

    private static Task ShiftClickAsync(IRenderedComponent<Bortable<string>> list, string label)
        => ItemElement(list, label).ClickAsync(new MouseEventArgs { ShiftKey = true });

    [TestMethod]
    public async Task AClickPublishesTheSelectionInListOrder()
    {
        var published = new List<IReadOnlyCollection<string>>();
        var list = RenderSelectable(["A", "B", "C"], published);

        await ClickAsync(list, "C");
        await ClickAsync(list, "A");

        Assert.HasCount(2, published);
        Assert.AreSequenceEqual(["A", "C"], published[^1].ToList());
    }

    [TestMethod]
    public async Task ShiftClickSelectsTheRangeFromTheLastClickedItem()
    {
        var published = new List<IReadOnlyCollection<string>>();
        var list = RenderSelectable(["A", "B", "C", "D", "E"], published);

        await ClickAsync(list, "B");
        await ShiftClickAsync(list, "D");

        Assert.AreEqual("B,C,D", LabelsWithClass(list, "bit-bortable-selected"));
        Assert.AreSequenceEqual(["B", "C", "D"], published[^1].ToList());

        // Upwards too, from the last clicked item.
        await ShiftClickAsync(list, "A");
        Assert.AreEqual("A,B,C,D", LabelsWithClass(list, "bit-bortable-selected"));
    }

    [TestMethod]
    public async Task ARangeLeavesOutLockedItems()
    {
        var published = new List<IReadOnlyCollection<string>>();
        var list = RenderSelectable(["A", "Locked", "C"], published, extra: p => p.Add(c => c.Filter, i => i == "Locked"));

        await ClickAsync(list, "A");
        await ShiftClickAsync(list, "C");

        Assert.AreEqual("A,C", LabelsWithClass(list, "bit-bortable-selected"));
    }

    [TestMethod]
    public void TheSelectionCanBeSetByThePage()
    {
        var published = new List<IReadOnlyCollection<string>>();
        var list = RenderSelectable(["A", "B", "C"], published, selected: ["C", "A", "Z"]);

        // Items not in the list are left out.
        Assert.AreEqual("A,C", LabelsWithClass(list, "bit-bortable-selected"));

        list.Render(p => p.Add(c => c.SelectedItems, new List<string>()));
        Assert.AreEqual("", LabelsWithClass(list, "bit-bortable-selected"));
        Assert.IsEmpty(published, "Setting the selection is not a change to report back.");
    }

    [TestMethod]
    public async Task TheSelectionComingBackFromTheBindingIsNotTakenForANewOne()
    {
        IReadOnlyCollection<string>? bound = null;
        List<string> items = ["A", "B", "C"];
        var list = RenderList(items, "L1", p =>
        {
            p.Add(c => c.MultiDrag, true);
            p.Add(c => c.SelectedItemsChanged, s => bound = s);
        });

        await ClickAsync(list, "B");
        list.Render(p => p.Add(c => c.SelectedItems, bound));
        await ClickAsync(list, "C");

        Assert.AreEqual("B,C", LabelsWithClass(list, "bit-bortable-selected"));
        Assert.AreSequenceEqual(["B", "C"], bound!.ToList());
    }

    [TestMethod]
    public async Task AMultiDragThatMovesTheSelectionPublishesItSpent()
    {
        var published = new List<IReadOnlyCollection<string>>();
        List<string> left = ["A", "B", "C"], right = ["X"];
        var l1 = RenderSelectable(left, published);
        var l2 = RenderList(right, "L2", p => p.Add(c => c.Group, "g"));

        await ClickAsync(l1, "A");
        await ClickAsync(l1, "C");
        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 1);
        await DropAsync(l2);

        Assert.AreSequenceEqual(["X", "A", "C"], right);
        Assert.IsEmpty(published[^1]);
    }

    [TestMethod]
    public async Task PickingUpAnUnselectedItemPublishesTheSelectionCleared()
    {
        var published = new List<IReadOnlyCollection<string>>();
        var list = RenderSelectable(["A", "B", "C"], published);

        await ClickAsync(list, "A");
        await DragStartAsync(list, "B");

        Assert.IsEmpty(published[^1]);
    }

    [TestMethod]
    public async Task ShiftSpaceTogglesTheSelectionAndSaysSo()
    {
        var published = new List<IReadOnlyCollection<string>>();
        var list = RenderSelectable(["A", "B", "C"], published);

        var first = await Renderer.Dispatcher.InvokeAsync(() => Zone(list).KeyAsync(KeyOf(list, "B"), "select", null, -1));
        var second = await Renderer.Dispatcher.InvokeAsync(() => Zone(list).KeyAsync(KeyOf(list, "C"), "select", null, -1));
        var third = await Renderer.Dispatcher.InvokeAsync(() => Zone(list).KeyAsync(KeyOf(list, "B"), "select", null, -1));

        Assert.AreEqual("B selected. 1 item selected.", first.Announce);
        Assert.AreEqual("C selected. 2 items selected.", second.Announce);
        Assert.AreEqual("B not selected. 1 item selected.", third.Announce);
        Assert.AreEqual("C", LabelsWithClass(list, "bit-bortable-selected"));
        Assert.IsFalse(first.Dragging);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public void AMultiDragListTellsHowToSelectFromTheKeyboard()
    {
        var published = new List<IReadOnlyCollection<string>>();
        var list = RenderSelectable(["A"], published);

        StringAssert.Contains(list.Find("#L1-instructions").TextContent, "Shift and Space");
        Assert.IsTrue(SentOptions(list)!.MultiDrag);
    }

    // ---- IsolateItems ----

    [TestMethod]
    public async Task WithIsolateItemsADragStepRendersNoItemTemplate()
    {
        int rendered = 0;
        List<string> items = [.. Enumerable.Range(1, 20).Select(i => $"Item {i}")];
        RenderFragment<string> template = item => builder => { rendered++; builder.AddContent(0, item); };
        var list = Render<Bortable<string>>(p => p
            .Add(c => c.Items, items)
            .Add(c => c.ItemTemplate, template)
            .Add(c => c.IsolateItems, true));
        Assert.AreEqual(20, rendered);

        await DragStartAsync(list, "Item 1");
        await DragOverAsync(list, 10);
        await DragOverAsync(list, 20);
        Assert.AreEqual(20, rendered, "The steps of the drag moved the wrappers only.");

        await DropAsync(list);
        Assert.AreEqual("Item 2,Item 3", string.Join(",", items.Take(2)));
        Assert.AreEqual("Item 1", items[^1]);
        Assert.AreEqual("Item 2,Item 3", string.Join(",", Labels(list).Split(',').Take(2)));
    }

    [TestMethod]
    public async Task WithoutIsolateItemsADragStepRendersEveryItemTemplate()
    {
        int rendered = 0;
        List<string> items = [.. Enumerable.Range(1, 20).Select(i => $"Item {i}")];
        RenderFragment<string> template = item => builder => { rendered++; builder.AddContent(0, item); };
        var list = Render<Bortable<string>>(p => p.Add(c => c.Items, items).Add(c => c.ItemTemplate, template));

        await DragStartAsync(list, "Item 1");
        rendered = 0;
        await DragOverAsync(list, 10);

        Assert.AreEqual(20, rendered);
    }

    [TestMethod]
    public void AnIsolatedItemIsRenderedAgainWhenThePageRendersTheList()
    {
        int rendered = 0;
        RenderFragment<string> template = item => builder => { rendered++; builder.AddContent(0, item); };
        var list = Render<Bortable<string>>(p => p
            .Add(c => c.Items, new List<string> { "A", "B" })
            .Add(c => c.ItemTemplate, template)
            .Add(c => c.IsolateItems, true));

        // The page renders again (its own state changed, which the template may show).
        list.Render(p => p.Add(c => c.Class, "again"));

        Assert.AreEqual(4, rendered);
    }
}
