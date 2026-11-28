using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Sortable;

/// <summary>
/// The state classes a drag puts on the markup. The stylesheet - and a list's own CSS, through
/// <c>Classes.Ghost</c>, <c>Classes.Chosen</c>, <c>Classes.SwapTarget</c> and <c>Classes.Selected</c> (and the same parts
/// of <c>Styles</c>) - is the only feedback a user gets while dragging, so each custom class is added next to its
/// built-in one, on the very items the built-in one marks, and none may outlive the drag.
/// </summary>
[TestClass]
public class BitSortableDragStateClassTests : BitSortableTestContext
{
    [TestMethod]
    public async Task CustomGhostAndChosenClassesAreAddedToTheBuiltInOnes()
    {
        var list = RenderList(["A", "B"], extra: p =>
        {
            p.Add(c => c.Classes, new BitSortableClassStyles { Ghost = "is-ghost", Chosen = "is-chosen" });
            p.Add(c => c.Styles, new BitSortableClassStyles { Ghost = "opacity: 0.4", Chosen = "outline: 1px dashed" });
        });

        await DragStartAsync(list, "A");

        var classes = ItemElement(list, "A").ClassList;
        Assert.IsTrue(classes.Contains("is-ghost"));
        Assert.IsTrue(classes.Contains("is-chosen"));
        Assert.IsTrue(classes.Contains("bit-srt-gst"));
        Assert.IsTrue(classes.Contains("bit-srt-chs"));
        var style = ItemElement(list, "A").GetAttribute("style")!;
        StringAssert.Contains(style, "opacity: 0.4");
        StringAssert.Contains(style, "outline: 1px dashed");
        Assert.IsFalse(ItemElement(list, "B").ClassList.Contains("is-ghost"));
        Assert.IsFalse(ItemElement(list, "B").HasAttribute("style"));

        await DropAsync(list);

        Assert.AreEqual("", LabelsWithClass(list, "is-ghost"));
        Assert.AreEqual("", LabelsWithClass(list, "is-chosen"));
        Assert.IsFalse(ItemElement(list, "A").HasAttribute("style"));
    }

    [TestMethod]
    public async Task ThePlaceholderTakesTheGhostClassesOfTheListShowingIt()
    {
        var source = RenderList(["A"], "L1", p =>
        {
            p.Add(c => c.Group, "g");
            p.Add(c => c.Classes, new BitSortableClassStyles { Ghost = "source-ghost" });
            p.Add(c => c.Styles, new BitSortableClassStyles { Ghost = "color: red" });
        });
        var target = RenderList(["X"], "L2", p =>
        {
            p.Add(c => c.Group, "g");
            p.Add(c => c.Classes, new BitSortableClassStyles { Ghost = "target-ghost", Chosen = "target-chosen" });
            p.Add(c => c.Styles, new BitSortableClassStyles { Ghost = "color: blue" });
        });

        await DragStartAsync(source, "A");
        await DragOverAsync(target, 0);

        var classes = Placeholder(target)!.ClassList;
        Assert.IsTrue(classes.Contains("bit-srt-gst"));
        Assert.IsTrue(classes.Contains("target-ghost"));
        Assert.IsTrue(classes.Contains("target-chosen"));
        Assert.IsFalse(classes.Contains("source-ghost"));
        StringAssert.Contains(Placeholder(target)!.GetAttribute("style"), "color: blue");
        Assert.IsFalse(Placeholder(target)!.GetAttribute("style")!.Contains("color: red"));
    }

    [TestMethod]
    public async Task ACustomSwapTargetClassMarksTheSwapTarget()
    {
        var list = RenderList(["A", "B"], extra: p =>
        {
            p.Add(c => c.Swap, true);
            p.Add(c => c.Classes, new BitSortableClassStyles { SwapTarget = "trade-with-me" });
            p.Add(c => c.Styles, new BitSortableClassStyles { SwapTarget = "background: gold" });
        });

        await DragStartAsync(list, "A");
        await HoverItemAsync(list, 1);

        Assert.AreEqual("B", LabelsWithClass(list, "trade-with-me"));
        Assert.AreEqual("B", LabelsWithClass(list, "bit-srt-swp"));
        StringAssert.Contains(ItemElement(list, "B").GetAttribute("style"), "background: gold");
    }

    [TestMethod]
    public async Task ACustomSelectedClassMarksTheSelection()
    {
        var list = RenderList(["A", "B"], extra: p =>
        {
            p.Add(c => c.MultiDrag, true);
            p.Add(c => c.Classes, new BitSortableClassStyles { Selected = "picked" });
        });

        await ClickAsync(list, "B");

        Assert.AreEqual("B", LabelsWithClass(list, "picked"));
        Assert.AreEqual("B", LabelsWithClass(list, "bit-srt-sel"));
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
            foreach (var cssClass in new[] { "bit-srt-gst", "bit-srt-chs", "bit-srt-hid" })
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

        Assert.AreEqual("", LabelsWithClass(list, "bit-srt-swp"));
        Assert.AreEqual("", LabelsWithClass(list, "bit-srt-gst"));
    }
}
