using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bortable.Tests;

/// <summary>
/// The markup a list renders before anything is dragged. The browser layer and the stylesheet both key off
/// it - <c>data-bortable-item</c>, <c>data-bortable-disabled</c>, <c>draggable</c> and the state classes - so
/// a change here silently breaks hit testing, drag handles or styling even though no C# logic changed.
/// </summary>
[TestClass]
public class RenderingTests : BortableTestContext
{
    [TestMethod]
    public void RendersOneWrapperPerItemWithTheTemplateInside()
    {
        var list = RenderList(["A", "B", "C"]);

        var wrappers = list.FindAll(".bit-bortable-item");
        Assert.HasCount(3, wrappers);
        Assert.AreEqual("A,B,C", Labels(list));
        Assert.IsTrue(wrappers.All(w => w.GetAttribute("data-bortable-item") == "true"));
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
        Assert.AreEqual("bit-bortable board", container.GetAttribute("class"));
        Assert.AreEqual("min-height: 40px", container.GetAttribute("style"));
    }

    [TestMethod]
    public void ItemsAreDraggableByDefault()
    {
        var list = RenderList(["A", "B"]);

        Assert.IsTrue(list.FindAll(".bit-bortable-item").All(e => e.GetAttribute("draggable") == "true"));
        Assert.IsTrue(list.FindAll(".bit-bortable-item").All(e => !e.HasAttribute("data-bortable-disabled")));
        Assert.IsFalse(Container(list).ClassList.Contains("bit-bortable-disabled"));
        Assert.IsFalse(Container(list).ClassList.Contains("bit-bortable-has-handle"));
    }

    [TestMethod]
    public void ADisabledListIsMarkedAndNoneOfItsItemsIsDraggable()
    {
        var list = RenderList(["A", "B"], extra: p => p.Add(c => c.Disabled, true));

        Assert.IsTrue(Container(list).ClassList.Contains("bit-bortable-disabled"));
        // data-bortable-disabled is what stops the handle listener from re-enabling the drag in the browser.
        Assert.IsTrue(list.FindAll(".bit-bortable-item").All(e =>
            e.GetAttribute("draggable") == "false" && e.GetAttribute("data-bortable-disabled") == "true"));
    }

    [TestMethod]
    public void WithAHandleTheItemsWaitForTheHandleToBePressed()
    {
        var list = RenderList(["A", "B"], extra: p => p.Add(c => c.Handle, ".grip"));

        // The browser layer flips draggable on while the handle is pressed, so the markup starts with it off
        // - otherwise a press anywhere on the item would start a drag.
        Assert.IsTrue(Container(list).ClassList.Contains("bit-bortable-has-handle"));
        Assert.IsTrue(list.FindAll(".bit-bortable-item").All(e => e.GetAttribute("draggable") == "false"));
        // ...but an item behind a handle is still draggable, so it must not look disabled to that layer.
        Assert.IsTrue(list.FindAll(".bit-bortable-item").All(e => !e.HasAttribute("data-bortable-disabled")));
    }

    [TestMethod]
    public void TheHandleIsPushedToTheBrowserLayerAndUpdatedWhenItChanges()
    {
        var list = RenderList(["A"], extra: p => p.Add(c => c.Handle, ".grip"));

        list.Render(p => p.Add(c => c.Handle, ".other"));
        list.Render(p => p.Add(c => c.Class, "unrelated"));
        list.Render(p => p.Add(c => c.Handle, (string?)null));

        // Once per distinct value: an unrelated re-render must not reinstall the listener.
        var calls = Module.Invocations["setHandle"];
        Assert.AreSequenceEqual([".grip", ".other", null], calls.Select(c => c.Arguments[1] as string));
    }

    [TestMethod]
    public void FilteredItemsAreMarkedAndCannotBeDragged()
    {
        var list = RenderList(["A", "Locked", "C"], extra: p => p.Add(c => c.Filter, i => i == "Locked"));

        var locked = ItemElement(list, "Locked");
        Assert.IsTrue(locked.ClassList.Contains("bit-bortable-filtered"));
        Assert.AreEqual("false", locked.GetAttribute("draggable"));
        Assert.AreEqual("true", locked.GetAttribute("data-bortable-disabled"));

        var free = ItemElement(list, "A");
        Assert.IsFalse(free.ClassList.Contains("bit-bortable-filtered"));
        Assert.AreEqual("true", free.GetAttribute("draggable"));
        Assert.IsFalse(free.HasAttribute("data-bortable-disabled"));
    }

    [TestMethod]
    public void ItemClassAndItemClassSelectorAreAppendedToEachWrapper()
    {
        var list = RenderList(["Apple", "Banana"], extra: p =>
        {
            p.Add(c => c.ItemClass, "card");
            p.Add(c => c.ItemClassSelector, i => i.StartsWith('B') ? "tinted" : null);
        });

        Assert.AreEqual("bit-bortable-item card", ItemElement(list, "Apple").GetAttribute("class"));
        Assert.AreEqual("bit-bortable-item card tinted", ItemElement(list, "Banana").GetAttribute("class"));
    }

    [TestMethod]
    public void ChildContentIsRenderedAfterTheItems()
    {
        var list = RenderList(["A", "B"], extra: p => p.AddChildContent("<p class=\"empty\">Drop here</p>"));

        var children = Container(list).Children;
        Assert.HasCount(3, children);
        Assert.IsTrue(children[2].ClassList.Contains("empty"));
    }

    [TestMethod]
    public void WithoutItemsOnlyTheContainerAndItsChildContentAreRendered()
    {
        var list = RenderList(null, extra: p => p.AddChildContent("<p class=\"empty\">Nothing yet</p>"));

        Assert.IsEmpty(list.FindAll("[data-bortable-item]"));
        Assert.ContainsSingle(list.FindAll(".empty"));
    }

    [TestMethod]
    public void AGeneratedIdNamesTheContainerAndIsUniquePerList()
    {
        var first = RenderList(["A"]);
        var second = RenderList(["B"]);

        string id = Container(first).Id!;
        Assert.StartsWith("bit-bortable-", id);
        // The browser layer finds the list an item belongs to through this attribute.
        Assert.AreEqual(id, Container(first).GetAttribute("data-bortable-list"));
        Assert.AreEqual(id, first.Instance.Id);
        Assert.AreNotEqual(id, Container(second).Id);
    }

    [TestMethod]
    public void ACustomIdIsRenderedAsIs()
    {
        var list = RenderList(["A"], "todo");

        Assert.AreEqual("todo", Container(list).Id);
        Assert.AreEqual("todo", Container(list).GetAttribute("data-bortable-list"));
    }

    [TestMethod]
    public void TheModuleIsImportedOnceTheListHasRendered()
    {
        RenderList(["A"]);

        // Every engine decision that needs a measurement goes through this module, so a list that never
        // imports it silently refuses every drag.
        var imports = JSInterop.Invocations["import"];
        Assert.ContainsSingle(imports);
        Assert.AreEqual(ModulePath, imports[0].Arguments[0]);
    }

    [TestMethod]
    public void EveryListOfTheScopeSharesOneImport()
    {
        RenderList(["A"]);
        RenderList(["B"]);
        RenderList(["C"]);

        // The module keeps no per-list state, so a tree with a list per node still imports it once.
        Assert.ContainsSingle(JSInterop.Invocations["import"]);
    }

    [TestMethod]
    public void RenderingWithoutTheServicesThrows()
    {
        using var bare = new BunitContext();
        bare.JSInterop.Mode = JSRuntimeMode.Loose;

        Assert.ThrowsExactly<InvalidOperationException>(() => bare.Render<Bortable<string>>(p => p.Add(c => c.Items, ["A"])));
    }

    [TestMethod]
    public void RegisteringTheServicesTwiceIsHarmless()
    {
        var services = new ServiceCollection();

        services.AddBitBortableServices().AddBitBortableServices();

        // Two coordinators would split a drag in two: lists resolving different instances never see each
        // other's drags.
        Assert.ContainsSingle(d => d.ServiceType == typeof(BortableService), services);
        Assert.AreEqual(ServiceLifetime.Scoped, services.Single(d => d.ServiceType == typeof(BortableService)).Lifetime);
    }

    [TestMethod]
    public void RegisteringOnANullCollectionThrows()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => BitBortable.AddBitBortableServices(null!));
    }
}
