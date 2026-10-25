using AngleSharp.Dom;
using Bunit;

namespace Bit.Bdnd.Tests.Components;

[TestClass]
public class BdndDragHandleTests : BdndComponentTestBase
{
    /// <summary>A draggable with a handle inside it.</summary>
    private IRenderedComponent<BdndDraggable<string>> RenderWithHandle(
        Action<ComponentParameterCollectionBuilder<BdndDraggable<string>>>? draggable = null,
        Action<ComponentParameterCollectionBuilder<BdndDragHandle>>? handle = null)
        => Render<BdndDraggable<string>>(p =>
        {
            p.AddCascadingValue(Manager);
            p.Add(x => x.Type, "card");
            p.Add<BdndDragHandle, BdndDragState>(x => x.ChildContent, _ => h =>
            {
                h.AddChildContent("<span class=\"grip\">::</span>");
                handle?.Invoke(h);
            });
            draggable?.Invoke(p);
        });

    private static IElement HandleOf(IRenderedComponent<BdndDraggable<string>> cut) => cut.Find("[data-bit-bdnd-handle]");

    [TestMethod]
    public void TheHandleIsTheNativeDraggableOfADraggableUsingHandles()
    {
        var cut = RenderWithHandle(d => d.Add(x => x.UseHandle, true));

        var handle = HandleOf(cut);
        Assert.AreEqual("true", handle.GetAttribute("draggable"));
        Assert.AreEqual("", handle.GetAttribute("data-bit-bdnd-handle"));
        // touch drags it at once from the handle, which does not scroll
        Assert.AreEqual("cursor: move; touch-action: none;", handle.GetAttribute("style"));
        Assert.AreEqual("::", handle.QuerySelector(".grip")!.TextContent);
        Assert.AreEqual("false", HostOf(cut).GetAttribute("draggable"), "the host is not the native draggable");
    }

    [TestMethod]
    public void TheCursorIsPrependedToTheHandlesStyle()
    {
        var cut = RenderWithHandle(d => d.Add(x => x.UseHandle, true), h => h.Add(x => x.Style, "color: red"));

        Assert.AreEqual("cursor: move; touch-action: none; color: red", HandleOf(cut).GetAttribute("style"));
    }

    [TestMethod]
    public void OtherAttributesGoOntoTheHandle()
    {
        var cut = RenderWithHandle(d => d.Add(x => x.UseHandle, true), h => h
            .AddUnmatched("class", "grip-area")
            .AddUnmatched("title", "Drag to reorder"));

        var handle = HandleOf(cut);
        Assert.AreEqual("grip-area", handle.ClassName);
        Assert.AreEqual("Drag to reorder", handle.GetAttribute("title"));
    }

    [TestMethod]
    public void AHandleOfADraggableNotUsingHandlesIsNotDraggable()
    {
        var cut = RenderWithHandle();

        Assert.IsFalse(HandleOf(cut).HasAttribute("draggable"));
        Assert.AreEqual("cursor: move;", HandleOf(cut).GetAttribute("style"), "the whole item still drags");
        Assert.AreEqual("true", HostOf(cut).GetAttribute("draggable"));
    }

    [TestMethod]
    public void AHandleOfADraggableThatCannotDragIsInert()
    {
        var cut = RenderWithHandle(d => d.Add(x => x.UseHandle, true).Add(x => x.CanDrag, false), h => h.Add(x => x.Style, "color: red"));

        Assert.IsFalse(HandleOf(cut).HasAttribute("draggable"));
        Assert.AreEqual("color: red", HandleOf(cut).GetAttribute("style"));
    }

    [TestMethod]
    public void TheHandleFollowsTheDraggablesParameters()
    {
        var cut = RenderWithHandle(d => d.Add(x => x.UseHandle, true));

        cut.Render(p => p.Add(x => x.CanDrag, false));
        Assert.IsFalse(HandleOf(cut).HasAttribute("draggable"));
        Assert.IsFalse(HandleOf(cut).HasAttribute("style"));

        cut.Render(p => p.Add(x => x.CanDrag, true));
        Assert.AreEqual("true", HandleOf(cut).GetAttribute("draggable"));
    }

    [TestMethod]
    public void TheDraggableTellsTheScriptToStartDragsOnlyFromHandles()
    {
        RenderWithHandle(d => d.Add(x => x.UseHandle, true));

        Assert.IsTrue(Assert.ContainsSingle(SourceRegistrations).UseHandle);
    }

    [TestMethod]
    public void AHandleOutsideADraggableRendersInert()
    {
        var cut = Render<BdndDragHandle>(p => p.Add(x => x.Style, "color: red").AddChildContent("::"));

        var handle = cut.Find("[data-bit-bdnd-handle]");
        Assert.IsFalse(handle.HasAttribute("draggable"));
        Assert.AreEqual("color: red", handle.GetAttribute("style"));
        Assert.AreEqual("::", handle.TextContent);
    }

    [TestMethod]
    public void AHandleWithoutStyleOutsideADraggableHasNoStyle()
    {
        var cut = Render<BdndDragHandle>();

        Assert.IsFalse(cut.Find("[data-bit-bdnd-handle]").HasAttribute("style"));
    }
}
