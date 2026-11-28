using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Sortable;

/// <summary>
/// The markup a list renders before anything is dragged. The browser layer and the stylesheet both key off
/// it - <c>data-bit-srt-item</c>, <c>data-bit-srt-disabled</c>, <c>draggable</c> and the state classes - so
/// a change here silently breaks hit testing, drag handles or styling even though no C# logic changed.
/// </summary>
[TestClass]
public class BitSortableRenderingTests : BitSortableTestContext
{
    [TestMethod]
    public void RendersOneWrapperPerItemWithTheTemplateInside()
    {
        var list = RenderList(["A", "B", "C"]);

        var wrappers = list.FindAll(".bit-srt-itm");
        Assert.HasCount(3, wrappers);
        Assert.AreEqual("A,B,C", Labels(list));
        Assert.IsTrue(wrappers.All(w => w.GetAttribute("data-bit-srt-item") == "true"));
        Assert.AreEqual("<span>B</span>", wrappers[1].InnerHtml.Trim());
    }

    [TestMethod]
    public void TheContainerCarriesTheBaseClassTheCustomClassAndTheStyle()
    {
        var list = RenderList(["A"], extra: p =>
        {
            p.Add(c => c.Class, "board");
            p.Add(c => c.Style, "min-height: 40px");
        });

        var container = Container(list);
        Assert.IsTrue(container.ClassList.Contains("bit-srt"));
        Assert.IsTrue(container.ClassList.Contains("board"));
        StringAssert.Contains(container.GetAttribute("style"), "min-height: 40px");
    }

    [TestMethod]
    public void ItemsAreDraggableByDefault()
    {
        var list = RenderList(["A", "B"]);

        Assert.IsTrue(list.FindAll(".bit-srt-itm").All(e => e.GetAttribute("draggable") == "true"));
        Assert.IsTrue(list.FindAll(".bit-srt-itm").All(e => !e.HasAttribute("data-bit-srt-disabled")));
        Assert.IsFalse(Container(list).ClassList.Contains("bit-dis"));
        Assert.IsFalse(Container(list).ClassList.Contains("bit-srt-hnd"));
    }

    [TestMethod]
    public void ADisabledListIsMarkedAndNoneOfItsItemsIsDraggable()
    {
        var list = RenderList(["A", "B"], extra: p => p.Add(c => c.Disabled, true));

        Assert.IsTrue(Container(list).ClassList.Contains("bit-dis"));
        // data-bit-srt-disabled is what stops the handle listener from re-enabling the drag in the browser.
        Assert.IsTrue(list.FindAll(".bit-srt-itm").All(e =>
            e.GetAttribute("draggable") == "false" && e.GetAttribute("data-bit-srt-disabled") == "true"));
    }

    [TestMethod]
    public void WithAHandleTheItemsWaitForTheHandleToBePressed()
    {
        var list = RenderList(["A", "B"], extra: p => p.Add(c => c.Handle, ".grip"));

        // The browser layer flips draggable on while the handle is pressed, so the markup starts with it off
        // - otherwise a press anywhere on the item would start a drag.
        Assert.IsTrue(Container(list).ClassList.Contains("bit-srt-hnd"));
        Assert.IsTrue(list.FindAll(".bit-srt-itm").All(e => e.GetAttribute("draggable") == "false"));
        // ...but an item behind a handle is still draggable, so it must not look disabled to that layer.
        Assert.IsTrue(list.FindAll(".bit-srt-itm").All(e => !e.HasAttribute("data-bit-srt-disabled")));
    }

    [TestMethod]
    public void TheHandleIsPushedToTheBrowserLayerAndUpdatedWhenItChanges()
    {
        var list = RenderList(["A"], extra: p => p.Add(c => c.Handle, ".grip"));

        list.Render(p => p.Add(c => c.Handle, ".other"));
        list.Render(p => p.Add(c => c.Class, "unrelated"));
        list.Render(p => p.Add(c => c.Handle, (string?)null));

        // Once per distinct value: an unrelated re-render must not send the options again.
        var calls = JSInterop.Invocations["BitBlazorUI.Sortable.register"].Concat(JSInterop.Invocations["BitBlazorUI.Sortable.configure"]);
        Assert.AreSequenceEqual([".grip", ".other", null], calls.Select(c => ((BitSortableJsOptions)c.Arguments[^1]!).Handle));
    }

    [TestMethod]
    public void TheInputOptionsAreSentToTheBrowserLayer()
    {
        var defaults = SentOptions(RenderList(["A"], "L1"))!;
        Assert.AreEqual(0, defaults.Delay);
        Assert.AreEqual(200, defaults.TouchDelay);
        Assert.AreEqual(5, defaults.DelayTolerance);
        Assert.IsFalse(defaults.ForceFallback);
        Assert.AreEqual("bit-srt-drg", defaults.DragClass);
        Assert.IsTrue(defaults.Keyboard);

        var custom = SentOptions(RenderList(["A"], "L2", p =>
        {
            p.Add(c => c.Delay, 100);
            p.Add(c => c.TouchDelay, 0);
            p.Add(c => c.DelayTolerance, -3);
            p.Add(c => c.ForceFallback, true);
            p.Add(c => c.Classes, new BitSortableClassStyles { Drag = "my-preview" });
        }))!;
        Assert.AreEqual(100, custom.Delay);
        Assert.AreEqual(0, custom.TouchDelay);
        Assert.AreEqual(0, custom.DelayTolerance, "A negative tolerance means none.");
        Assert.IsTrue(custom.ForceFallback);
        // The app's class is added to the built-in one, which carries the preview's own look.
        Assert.AreEqual("bit-srt-drg my-preview", custom.DragClass);
    }

    [TestMethod]
    public void EveryItemIsRenderedWithAKeyOfItsOwn()
    {
        var list = RenderList(["A", "B", "C"]);

        var keys = list.FindAll("[data-bit-srt-item]").Select(e => e.GetAttribute("data-bit-srt-key")).ToList();
        Assert.HasCount(3, keys);
        Assert.IsTrue(keys.All(k => !string.IsNullOrEmpty(k)));
        Assert.HasCount(3, keys.Distinct());
    }

    [TestMethod]
    public void TheListRegistersWithTheBrowserLayerThroughItsOwnBridge()
    {
        var list = RenderList(["A"], "L1");

        var register = JSInterop.Invocations["BitBlazorUI.Sortable.register"].Single();
        Assert.AreSame(list.Instance, ((DotNetObjectReference<BitSortable<string>>)register.Arguments[1]!).Value);
        Assert.AreEqual("L1", ((BitSortableJsOptions)register.Arguments[2]!).Id);
    }

    [TestMethod]
    public async Task ADisposedListUnregistersFromTheBrowserLayer()
    {
        var list = RenderList(["A"], "L1");

        await DisposeComponentsAsync();

        Assert.ContainsSingle(JSInterop.Invocations["BitBlazorUI.Sortable.unregister"]);
    }

    [TestMethod]
    public void FilteredItemsAreMarkedAndCannotBeDragged()
    {
        var list = RenderList(["A", "Locked", "C"], extra: p => p.Add(c => c.Filter, i => i == "Locked"));

        var locked = ItemElement(list, "Locked");
        Assert.IsTrue(locked.ClassList.Contains("bit-srt-flt"));
        Assert.AreEqual("false", locked.GetAttribute("draggable"));
        Assert.AreEqual("true", locked.GetAttribute("data-bit-srt-disabled"));

        var free = ItemElement(list, "A");
        Assert.IsFalse(free.ClassList.Contains("bit-srt-flt"));
        Assert.AreEqual("true", free.GetAttribute("draggable"));
        Assert.IsFalse(free.HasAttribute("data-bit-srt-disabled"));
    }

    [TestMethod]
    public void ItemClassAndItemClassSelectorAreAppendedToEachWrapper()
    {
        var list = RenderList(["Apple", "Banana"], extra: p =>
        {
            p.Add(c => c.Classes, new BitSortableClassStyles { Item = "card" });
            p.Add(c => c.ItemClass, i => i.StartsWith('B') ? "tinted" : null);
        });

        Assert.AreEqual("bit-srt-itm card", ItemElement(list, "Apple").GetAttribute("class"));
        Assert.AreEqual("bit-srt-itm card tinted", ItemElement(list, "Banana").GetAttribute("class"));
        Assert.IsTrue(Container(list).ClassList.Contains("bit-srt"), "Classes.Item is not the root's class.");
    }

    [TestMethod]
    public void ChildContentIsRenderedAfterTheItems()
    {
        var list = RenderList(["A", "B"], extra: p => p.AddChildContent("<p class=\"empty\">Drop here</p>"));

        // The items, the child content, then the hidden keyboard instructions.
        var children = Container(list).Children;
        Assert.HasCount(4, children);
        Assert.IsTrue(children[2].ClassList.Contains("empty"));
        Assert.IsTrue(children[3].ClassList.Contains("bit-srt-ins"));
    }

    [TestMethod]
    public void WithoutItemsOnlyTheContainerAndItsChildContentAreRendered()
    {
        var list = RenderList(null, extra: p => p.AddChildContent("<p class=\"empty\">Nothing yet</p>"));

        Assert.IsEmpty(list.FindAll("[data-bit-srt-item]"));
        Assert.ContainsSingle(list.FindAll(".empty"));
    }

    [TestMethod]
    public void AGeneratedIdNamesTheContainerAndIsUniquePerList()
    {
        var first = RenderList(["A"]);
        var second = RenderList(["B"]);

        string id = Container(first).Id!;
        // With no Id the list goes by its UniqueId, as every component does.
        Assert.IsNull(first.Instance.Id);
        Assert.AreEqual(first.Instance.UniqueId, id);
        // The browser layer finds the list an item belongs to through this attribute.
        Assert.AreEqual(id, Container(first).GetAttribute("data-bit-srt-list"));
        Assert.AreEqual(id, Zone(first).Id);
        Assert.AreNotEqual(id, Container(second).Id);
    }

    [TestMethod]
    public void ACustomIdIsRenderedAsIs()
    {
        var list = RenderList(["A"], "todo");

        Assert.AreEqual("todo", Container(list).Id);
        Assert.AreEqual("todo", Container(list).GetAttribute("data-bit-srt-list"));
        Assert.AreEqual("todo", Zone(list).Id);
    }

    [TestMethod]
    public void EveryListOfTheScopeSharesOneCoordinator()
    {
        var first = RenderList(["A"]);
        var second = RenderList(["B"]);
        var third = RenderList(["C"]);

        // Nothing is registered for the lists: they find their scope's coordinator themselves, one for all of them, as
        // two coordinators would split a drag in two - lists holding different ones never see each other's drags.
        Assert.HasCount(3, Coordinator.Zones);
        Assert.IsTrue(new[] { first, second, third }.All(l => Coordinator.Zones.Contains(Zone(l))));
    }

    [TestMethod]
    public void AskingForTheCoordinatorOfNoScopeThrows()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => BitSortableCoordinator.Of(null!));
    }

    // ---- what is new in BitSortable ----

    [TestMethod]
    public void TheRootCarriesTheRootClassAndTheStateClasses()
    {
        var plain = RenderList(["A"]);
        Assert.AreEqual("bit-srt", Container(plain).GetAttribute("class"));

        var disabled = RenderList(["A"], extra: p => p.Add(c => c.Disabled, true));
        Assert.IsTrue(Container(disabled).ClassList.Contains("bit-srt"));
        Assert.IsTrue(Container(disabled).ClassList.Contains("bit-dis"));

        var handled = RenderList(["A"], extra: p => p.Add(c => c.Handle, ".grip"));
        Assert.IsTrue(Container(handled).ClassList.Contains("bit-srt-hnd"));
        Assert.IsFalse(Container(handled).ClassList.Contains("bit-dis"));

        // Taking the Handle away takes its class away.
        handled.Render(p => p.Add(c => c.Handle, (string?)null));
        Assert.IsFalse(Container(handled).ClassList.Contains("bit-srt-hnd"));
    }

    [TestMethod]
    public void TheRootTakesClassesRootAndStylesRoot()
    {
        var list = RenderList(["A"], extra: p =>
        {
            p.Add(c => c.Classes, new BitSortableClassStyles { Root = "board" });
            p.Add(c => c.Styles, new BitSortableClassStyles { Root = "gap: 4px" });
        });

        Assert.IsTrue(Container(list).ClassList.Contains("bit-srt"));
        Assert.IsTrue(Container(list).ClassList.Contains("board"));
        StringAssert.Contains(Container(list).GetAttribute("style"), "gap: 4px");
    }

    [TestMethod]
    public void ItemStyleAndStylesItemAreWrittenOnEachWrapper()
    {
        var list = RenderList(["Apple", "Banana"], extra: p =>
        {
            p.Add(c => c.Styles, new BitSortableClassStyles { Item = "padding: 2px" });
            p.Add(c => c.ItemStyle, i => i.StartsWith('B') ? "color: red" : null);
        });

        var apple = ItemElement(list, "Apple").GetAttribute("style")!;
        var banana = ItemElement(list, "Banana").GetAttribute("style")!;
        StringAssert.Contains(apple, "padding: 2px");
        Assert.IsFalse(apple.Contains("color: red"));
        StringAssert.Contains(banana, "padding: 2px");
        StringAssert.Contains(banana, "color: red");
    }

    [TestMethod]
    public void AnItemWithNoStyleRendersNone()
    {
        var list = RenderList(["A"]);

        Assert.IsFalse(ItemElement(list, "A").HasAttribute("style"));
    }

    [TestMethod]
    public async Task TheFilteredAndSelectedStylesAndClassesAreAddedToTheBuiltInOnes()
    {
        var list = RenderList(["A", "Locked"], extra: p =>
        {
            p.Add(c => c.MultiDrag, true);
            p.Add(c => c.Filter, i => i == "Locked");
            p.Add(c => c.Classes, new BitSortableClassStyles { Filtered = "my-locked", Selected = "my-picked" });
            p.Add(c => c.Styles, new BitSortableClassStyles { Filtered = "opacity: 0.5", Selected = "outline: 1px solid" });
        });

        Assert.IsTrue(HasClass(list, "Locked", "bit-srt-flt"));
        Assert.IsTrue(HasClass(list, "Locked", "my-locked"));
        StringAssert.Contains(ItemElement(list, "Locked").GetAttribute("style"), "opacity: 0.5");

        await ClickAsync(list, "A");

        Assert.IsTrue(HasClass(list, "A", "bit-srt-sel"));
        Assert.IsTrue(HasClass(list, "A", "my-picked"));
        StringAssert.Contains(ItemElement(list, "A").GetAttribute("style"), "outline: 1px solid");
    }

    [TestMethod]
    public void TheDragStyleIsSentWithTheDragClass()
    {
        var plain = SentOptions(RenderList(["A"], "L1"))!;
        Assert.IsNull(plain.DragStyle);

        var styled = SentOptions(RenderList(["A"], "L2", p =>
        {
            p.Add(c => c.Classes, new BitSortableClassStyles { Drag = "tilted" });
            p.Add(c => c.Styles, new BitSortableClassStyles { Drag = "rotate: 3deg" });
        }))!;
        Assert.AreEqual("bit-srt-drg tilted", styled.DragClass);
        Assert.AreEqual("rotate: 3deg", styled.DragStyle);
    }

    [TestMethod]
    public void AnEmptyListSaysSoWithTheLibrarysText()
    {
        var list = RenderList([]);

        var empty = list.Find(".bit-srt-emp");
        Assert.AreEqual(BitStrings.Current.NoItemsFound, empty.TextContent.Trim());
    }

    [TestMethod]
    public void AListWithNoItemsAtAllSaysItIsEmptyToo()
    {
        var list = RenderList(null);

        Assert.ContainsSingle(list.FindAll(".bit-srt-emp"));
    }

    [TestMethod]
    public void EmptyTextAndEmptyTemplateReplaceTheLibrarysText()
    {
        var text = RenderList([], extra: p =>
        {
            p.Add(c => c.EmptyText, "Nothing to do");
            p.Add(c => c.Classes, new BitSortableClassStyles { Empty = "my-empty" });
            p.Add(c => c.Styles, new BitSortableClassStyles { Empty = "padding: 8px" });
        });
        var empty = text.Find(".bit-srt-emp");
        Assert.AreEqual("Nothing to do", empty.TextContent.Trim());
        Assert.IsTrue(empty.ClassList.Contains("my-empty"));
        StringAssert.Contains(empty.GetAttribute("style"), "padding: 8px");

        // The template wins over the text.
        var template = RenderList([], extra: p =>
        {
            p.Add(c => c.EmptyText, "Nothing to do");
            p.Add(c => c.EmptyTemplate, "<em class=\"drop-here\">Drop here</em>");
        });
        Assert.AreEqual("Drop here", template.Find(".bit-srt-emp .drop-here").TextContent);
        Assert.IsFalse(template.Find(".bit-srt-emp").TextContent.Contains("Nothing to do"));
    }

    [TestMethod]
    public void TheEmptyTextIsNotShownWhileThereAreItems()
    {
        List<string> items = ["A"];
        var list = RenderList(items, extra: p => p.Add(c => c.EmptyText, "Nothing to do"));

        Assert.IsEmpty(list.FindAll(".bit-srt-emp"));

        items.Clear();
        list.Render();

        Assert.ContainsSingle(list.FindAll(".bit-srt-emp"));
    }

    [TestMethod]
    public async Task TheEmptyTextMakesWayForTheDropPreview()
    {
        List<string> left = ["A"], right = [];
        var source = RenderList(left, "L1", p => p.Add(c => c.Group, "g"));
        var target = RenderList(right, "L2", p =>
        {
            p.Add(c => c.Group, "g");
            p.Add(c => c.EmptyText, "Nothing to do");
        });

        Assert.ContainsSingle(target.FindAll(".bit-srt-emp"));

        await DragStartAsync(source, "A");
        await DragOverAsync(target, 0);

        // The preview stands where the item will land: the list is not empty to look at any more.
        Assert.IsNotNull(Placeholder(target));
        Assert.IsEmpty(target.FindAll(".bit-srt-emp"));

        await DropAsync(target);

        Assert.AreSequenceEqual(["A"], right);
        Assert.IsEmpty(target.FindAll(".bit-srt-emp"));
        // ...and the list the item left is the empty one now.
        Assert.ContainsSingle(source.FindAll(".bit-srt-emp"));
    }
}
