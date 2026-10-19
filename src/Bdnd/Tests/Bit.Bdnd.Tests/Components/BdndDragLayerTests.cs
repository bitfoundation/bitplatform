using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bdnd.Tests.Components;

[TestClass]
public class BdndDragLayerTests : BdndComponentTestBase
{
    private IRenderedComponent<BdndProvider> RenderLayer<TItem>(StateLog<BdndDragLayerState<TItem>> log)
        => RenderProvider(p => p.AddChildContent<BdndDragLayer<TItem>>(l => l.Add(x => x.ChildContent, log.Content())));

    private Task BeginDragAsync(IRenderedComponent<IComponent> cut, object item)
        => cut.InvokeAsync(() =>
        {
            Manager.BeginDrag("src", item, "card", new(100, 50), new(80, 40));
            return Manager.FlushBeginAsync();
        });

    [TestMethod]
    public void AnIdleLayerReceivesAnEmptyState()
    {
        var log = new StateLog<BdndDragLayerState<string>>();

        RenderLayer(log);

        var state = log.Last;
        Assert.IsFalse(state.IsDragging);
        Assert.IsNull(state.ItemType);
        Assert.IsNull(state.Item);
        Assert.IsNull(state.InitialClientOffset);
        Assert.IsNull(state.InitialSourceClientOffset);
        Assert.IsNull(state.ClientOffset);
        Assert.IsNull(state.DifferenceFromInitialOffset);
        Assert.IsNull(state.SourceClientOffset);
        Assert.AreSame(Manager, state.Monitor);
    }

    [TestMethod]
    public async Task TheLayerRendersTheDragWithItsOffsets()
    {
        var log = new StateLog<BdndDragLayerState<string>>();
        var cut = RenderLayer(log);

        await BeginDragAsync(cut, "card-7");

        var state = log.Last;
        Assert.IsTrue(state.IsDragging);
        Assert.AreEqual("card", state.ItemType);
        Assert.AreEqual("card-7", state.Item);
        Assert.AreEqual(new BdndXYCoord(100, 50), state.InitialClientOffset);
        Assert.AreEqual(new BdndXYCoord(80, 40), state.InitialSourceClientOffset);
        Assert.AreEqual(new BdndXYCoord(100, 50), state.ClientOffset);
        Assert.AreEqual(new BdndXYCoord(0, 0), state.DifferenceFromInitialOffset);
        Assert.AreEqual(new BdndXYCoord(80, 40), state.SourceClientOffset);
    }

    [TestMethod]
    public async Task TheLayerReRendersAsThePointerMoves()
    {
        var log = new StateLog<BdndDragLayerState<string>>();
        var cut = RenderLayer(log);
        await BeginDragAsync(cut, "card-7");
        var renders = log.Count;

        await cut.InvokeAsync(() => Manager.SetClientOffset(new(130, 20)));

        Assert.AreEqual(renders + 1, log.Count);
        Assert.AreEqual(new BdndXYCoord(130, 20), log.Last.ClientOffset);
        Assert.AreEqual(new BdndXYCoord(30, -30), log.Last.DifferenceFromInitialOffset);
        Assert.AreEqual(new BdndXYCoord(110, 10), log.Last.SourceClientOffset);
    }

    [TestMethod]
    public async Task TheLayerFollowsAPointerMoveReportedForTheDraggedSource()
    {
        var log = new StateLog<BdndDragLayerState<string>>();
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "card").Add(x => x.Item, "card-7").Add(x => x.UseCustomDragLayer, true))
            .AddChildContent<BdndDragLayer<string>>(l => l.Add(x => x.ChildContent, log.Content())));
        var source = cut.FindComponent<BdndDraggable<string>>();
        await StartDragAsync(source, 10, 10, 2, 3);

        await source.InvokeAsync(() => source.Instance.HandleDragMove(60, 45));

        Assert.IsTrue(log.Last.IsDragging);
        Assert.AreEqual("card-7", log.Last.Item);
        Assert.AreEqual(new BdndXYCoord(58, 42), log.Last.SourceClientOffset);
        Assert.IsTrue(Assert.ContainsSingle(SourceRegistrations).UseEmptyPreview);
    }

    [TestMethod]
    public async Task TheLayerClearsWhenTheDragEnds()
    {
        var log = new StateLog<BdndDragLayerState<string>>();
        var cut = RenderLayer(log);
        await BeginDragAsync(cut, "card-7");

        await cut.InvokeAsync(Manager.EndDrag);

        Assert.IsFalse(log.Last.IsDragging);
        Assert.IsNull(log.Last.SourceClientOffset);
    }

    [TestMethod]
    public async Task AnItemOfAnotherTypeIsNotHandedToTheLayer()
    {
        var log = new StateLog<BdndDragLayerState<string>>();
        var cut = RenderLayer(log);

        await BeginDragAsync(cut, 42);

        Assert.IsTrue(log.Last.IsDragging);
        Assert.IsNull(log.Last.Item);
        Assert.AreEqual("card", log.Last.ItemType);
    }

    [TestMethod]
    public async Task ALayerWithoutContentRendersNothing()
    {
        var cut = RenderProvider(p => p.AddChildContent<BdndDragLayer<string>>());

        await BeginDragAsync(cut, "card-7");

        Assert.AreEqual("", cut.Markup.Trim());
    }

    [TestMethod]
    public async Task ARemovedLayerStopsListening()
    {
        var log = new StateLog<BdndDragLayerState<string>>();
        var cut = RenderLayer(log);
        await BeginDragAsync(cut, "card-7");

        cut.Render(p => p.AddChildContent("<p>no layer</p>"));
        var renders = log.Count;
        await cut.InvokeAsync(() => Manager.SetClientOffset(new(1, 1)));
        await cut.InvokeAsync(Manager.EndDrag);

        Assert.AreEqual(renders, log.Count);
    }
}
