using Bunit;

namespace Bit.Bortable.Tests;

/// <summary>
/// The state classes a drag puts on the markup. The stylesheet - or a list's own CSS, through
/// <c>GhostClass</c>, <c>ChosenClass</c>, <c>SwapClass</c> and <c>SelectedClass</c> - is the only feedback a user
/// gets while dragging, so each custom class must replace its default rather than be added next to it, and
/// none may outlive the drag.
/// </summary>
[TestClass]
public class DragStateClassTests : BortableTestContext
{
    [TestMethod]
    public async Task CustomGhostAndChosenClassesReplaceTheDefaults()
    {
        var list = RenderList(["A", "B"], extra: p =>
        {
            p.Add(c => c.GhostClass, "is-ghost");
            p.Add(c => c.ChosenClass, "is-chosen");
        });

        await DragStartAsync(list, "A");

        var classes = ItemElement(list, "A").ClassList;
        Assert.IsTrue(classes.Contains("is-ghost"));
        Assert.IsTrue(classes.Contains("is-chosen"));
        Assert.IsFalse(classes.Contains("bit-bortable-ghost"));
        Assert.IsFalse(classes.Contains("bit-bortable-chosen"));
        Assert.IsFalse(ItemElement(list, "B").ClassList.Contains("is-ghost"));
    }

    [TestMethod]
    public async Task ThePlaceholderTakesTheGhostClassesOfTheListShowingIt()
    {
        var source = RenderList(["A"], "L1", p =>
        {
            p.Add(c => c.Group, "g");
            p.Add(c => c.GhostClass, "source-ghost");
        });
        var target = RenderList(["X"], "L2", p =>
        {
            p.Add(c => c.Group, "g");
            p.Add(c => c.GhostClass, "target-ghost");
            p.Add(c => c.ChosenClass, "target-chosen");
        });

        await DragStartAsync(source, "A");
        await DragOverAsync(target, 0);

        var classes = Placeholder(target)!.ClassList;
        Assert.IsTrue(classes.Contains("target-ghost"));
        Assert.IsTrue(classes.Contains("target-chosen"));
        Assert.IsFalse(classes.Contains("source-ghost"));
    }

    [TestMethod]
    public async Task ACustomSwapClassMarksTheSwapTarget()
    {
        var list = RenderList(["A", "B"], extra: p =>
        {
            p.Add(c => c.Swap, true);
            p.Add(c => c.SwapClass, "trade-with-me");
        });

        await DragStartAsync(list, "A");
        await HoverItemAsync(list, 1);

        Assert.AreEqual("B", LabelsWithClass(list, "trade-with-me"));
        Assert.AreEqual("", LabelsWithClass(list, "bit-bortable-swap-highlight"));
    }

    [TestMethod]
    public async Task ACustomSelectedClassMarksTheSelection()
    {
        var list = RenderList(["A", "B"], extra: p =>
        {
            p.Add(c => c.MultiDrag, true);
            p.Add(c => c.SelectedClass, "picked");
        });

        await ClickAsync(list, "B");

        Assert.AreEqual("B", LabelsWithClass(list, "picked"));
        Assert.AreEqual("", LabelsWithClass(list, "bit-bortable-selected"));
    }

    [TestMethod]
    public async Task NoDragStateOutlivesACrossListDrop()
    {
        var source = RenderList(["A", "B"], "L1", p => p.Add(c => c.Group, "g"));
        var target = RenderList(["X"], "L2", p => p.Add(c => c.Group, "g"));

        await DragStartAsync(source, "A");
        await DragOverAsync(target, 0);
        await DropAsync(target);

        foreach (var list in new[] { source, target })
        {
            Assert.IsNull(Placeholder(list));
            foreach (var cssClass in new[] { "bit-bortable-ghost", "bit-bortable-chosen", "bit-bortable-hidden" })
                Assert.AreEqual("", LabelsWithClass(list, cssClass), cssClass);
        }
    }

    [TestMethod]
    public async Task NoDragStateOutlivesASwap()
    {
        var list = RenderList(["A", "B", "C"], extra: p => p.Add(c => c.Swap, true));

        await DragStartAsync(list, "A");
        await HoverItemAsync(list, 1);
        await DropAsync(list);

        Assert.AreEqual("", LabelsWithClass(list, "bit-bortable-swap-highlight"));
        Assert.AreEqual("", LabelsWithClass(list, "bit-bortable-ghost"));
    }
}
