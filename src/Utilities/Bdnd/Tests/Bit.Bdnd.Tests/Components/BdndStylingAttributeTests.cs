using Bunit;

namespace Bit.Bdnd.Tests.Components;

/// <summary>
/// The <c>data-bit-bdnd-*</c> attributes on the host elements, which let a stylesheet follow a drag without the
/// content rendering classes from the collected state.
/// </summary>
[TestClass]
public class BdndStylingAttributeTests : BdndComponentTestBase
{
    private IRenderedComponent<BdndProvider> RenderDustbin(Func<string?, IBdndMonitor, bool>? canDrop = null)
        => RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.Item, "crate"))
            .AddChildContent<BdndDropTarget<string>>(t => t
                .Add(x => x.Accept, "box")
                .Add(x => x.CanDropPredicate, canDrop)
                .Add(x => x.ChildContent, _ => Fragment<BdndDropTarget<string>>((nameof(BdndDropTarget<string>.Accept), "box")))));

    [TestMethod]
    public void IdleHostsCarryNoDragAttributes()
    {
        var cut = RenderDustbin();

        Assert.IsEmpty(cut.FindAll("[data-bit-bdnd-dragging], [data-bit-bdnd-can-drop], [data-bit-bdnd-over], [data-bit-bdnd-over-shallow]"));
    }

    [TestMethod]
    public async Task TheDraggedSourceAndTheTargetsThatCanTakeItAreMarked()
    {
        var cut = RenderDustbin();

        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());

        Assert.IsTrue(HostOf(cut.FindComponent<BdndDraggable<string>>()).HasAttribute("data-bit-bdnd-dragging"));
        Assert.HasCount(2, cut.FindAll("[data-bit-bdnd-can-drop]"));
        Assert.IsEmpty(cut.FindAll("[data-bit-bdnd-over]"));
    }

    [TestMethod]
    public async Task HoveredTargetsAreMarkedDeepAndShallow()
    {
        var cut = RenderDustbin();
        var targets = cut.FindComponents<BdndDropTarget<string>>();
        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());

        await HoverAsync(targets[0]);
        await HoverAsync(targets[1]);

        var over = cut.FindAll("[data-bit-bdnd-over]");
        Assert.HasCount(2, over);
        var shallow = cut.FindAll("[data-bit-bdnd-over-shallow]");
        Assert.HasCount(1, shallow);
        Assert.AreSame(over[1], shallow[0], "the inner target, which comes second in document order");
    }

    [TestMethod]
    public async Task ATargetThatRefusesTheItemIsNotMarkedAsAbleToDrop()
    {
        var cut = RenderDustbin(canDrop: (_, _) => false);
        var outer = cut.FindComponents<BdndDropTarget<string>>()[0];
        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());

        await HoverAsync(outer);

        Assert.IsFalse(HostOf(outer).HasAttribute("data-bit-bdnd-can-drop"));
        Assert.IsTrue(HostOf(outer).HasAttribute("data-bit-bdnd-over"), "it is over the target all the same");
    }

    [TestMethod]
    public async Task TheMarksGoAwayWhenTheDragEnds()
    {
        var cut = RenderDustbin();
        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());
        await HoverAsync(cut.FindComponents<BdndDropTarget<string>>()[0]);

        await EndDragAsync(cut.FindComponent<BdndDraggable<string>>());

        Assert.IsEmpty(cut.FindAll("[data-bit-bdnd-dragging], [data-bit-bdnd-can-drop], [data-bit-bdnd-over], [data-bit-bdnd-over-shallow]"));
    }
}
