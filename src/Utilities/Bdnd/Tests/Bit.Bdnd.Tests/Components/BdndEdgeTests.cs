using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bdnd.Tests.Components;

/// <summary>
/// Insertion edges: a target with <c>Edges</c> is told by the module which of them the drag is closest to
/// (<c>HandleEdgeChanged</c>), reports it while hovered (<c>ClosestEdge</c>, <c>data-bit-bdnd-edge</c>) and hands it to
/// <c>OnHover</c>, <c>OnDrop</c> and <c>OnNativeDrop</c>; a keyboard drag visits the places before and after it.
/// </summary>
[TestClass]
public class BdndEdgeTests : BdndComponentTestBase
{
    private readonly List<string> _log = [];
    private readonly StateLog<BdndDropState<string>> _states = new();

    private IRenderedComponent<BdndProvider> RenderList(BdndEdges edges = BdndEdges.Vertical)
        => RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "card").Add(x => x.Item, "a").Add(x => x.Label, "A")
                .Add(x => x.OnEnd, e => _log.Add($"end:{e.DidDrop}")))
            .AddChildContent<BdndDropTarget<string>>(t => t
                .Add(x => x.AcceptTypes, ["card", BdndNativeTypes.File])
                .Add(x => x.Edges, edges)
                .Add(x => x.Label, "B")
                .Add(x => x.OnHover, e => _log.Add($"hover:{e.ClosestEdge}"))
                .Add(x => x.OnDrop, e => _log.Add($"drop:{e.ClosestEdge}"))
                .Add(x => x.OnNativeDrop, e => _log.Add($"native:{e.ClosestEdge}"))
                .Add(x => x.ChildContent, _states.Content())));

    private static IRenderedComponent<BdndDropTarget<string>> Target(IRenderedComponent<BdndProvider> cut)
        => cut.FindComponent<BdndDropTarget<string>>();

    private static Task EdgeAsync(IRenderedComponent<BdndDropTarget<string>> target, string? edge)
        => target.InvokeAsync(() => target.Instance.HandleEdgeChanged(edge));

    [TestMethod]
    public void TheTargetTellsTheScriptItsEdges()
    {
        RenderList(BdndEdges.Horizontal);

        Assert.AreEqual((int)BdndEdges.Horizontal, TargetRegistrations.Last().Edges);
    }

    [TestMethod]
    public async Task TheClosestEdgeIsReportedWhileTheTargetIsHovered()
    {
        var cut = RenderList();
        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());
        await HoverAsync(Target(cut));

        await EdgeAsync(Target(cut), "top");
        Assert.AreEqual(BdndEdge.Top, _states.Last.ClosestEdge);
        Assert.AreEqual("top", HostOf(Target(cut)).GetAttribute("data-bit-bdnd-edge"));

        await EdgeAsync(Target(cut), "bottom");
        Assert.AreEqual(BdndEdge.Bottom, _states.Last.ClosestEdge);
        Assert.AreEqual("bottom", HostOf(Target(cut)).GetAttribute("data-bit-bdnd-edge"));
    }

    [TestMethod]
    public async Task AnEdgeReportedBeforeTheHoverShowsOnceTheHoverArrives()
    {
        var cut = RenderList();
        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());

        await EdgeAsync(Target(cut), "bottom");
        Assert.IsNull(_states.Last.ClosestEdge, "not hovered yet");
        await HoverAsync(Target(cut));

        Assert.AreEqual(BdndEdge.Bottom, _states.Last.ClosestEdge);
    }

    [TestMethod]
    public async Task AnEdgeTheTargetDoesNotListIsIgnored()
    {
        var cut = RenderList(BdndEdges.Vertical);
        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());
        await HoverAsync(Target(cut));

        await EdgeAsync(Target(cut), "left");

        Assert.IsNull(_states.Last.ClosestEdge);
    }

    [TestMethod]
    public async Task LeavingTheTargetOrEndingTheDragForgetsTheEdge()
    {
        var cut = RenderList();
        var source = cut.FindComponent<BdndDraggable<string>>();
        await StartDragAsync(source);
        await HoverAsync(Target(cut));
        await EdgeAsync(Target(cut), "top");

        await HoverAsync(Target(cut), isOver: false);
        Assert.IsNull(_states.Last.ClosestEdge);
        Assert.IsFalse(HostOf(Target(cut)).HasAttribute("data-bit-bdnd-edge"));

        await HoverAsync(Target(cut));
        Assert.IsNull(_states.Last.ClosestEdge, "a new hover starts without an edge until one is reported");
        await EdgeAsync(Target(cut), "bottom");
        await EndDragAsync(source);
        Assert.IsNull(_states.Last.ClosestEdge);
    }

    [TestMethod]
    public async Task AnEdgeReportedWithNoDragOfThisProviderIsIgnored()
    {
        var cut = RenderList();

        await EdgeAsync(Target(cut), "top");
        await HoverAsync(Target(cut), true, "Files");

        Assert.IsNull(_states.Last.ClosestEdge, "the edge of a drag of another provider does not show on the next native drag");
    }

    [TestMethod]
    public async Task ALeaveForgetsTheEdgeEvenWhenItOvertookTheHover()
    {
        var cut = RenderList();
        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());

        await EdgeAsync(Target(cut), "top");
        await HoverAsync(Target(cut), isOver: false);
        await HoverAsync(Target(cut));

        Assert.IsNull(_states.Last.ClosestEdge);
    }

    [TestMethod]
    public async Task OnlyTheInnermostHoveredTargetShowsAndDropsWithItsEdge()
    {
        var log = new List<string>();
        var states = new StateLog<BdndDropState<string>>();
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "card").Add(x => x.Item, "a"))
            .AddChildContent<BdndDropTarget<string>>(outer => outer
                .Add(x => x.Accept, "card")
                .Add(x => x.Edges, BdndEdges.Vertical)
                .Add(x => x.OnDrop, e => log.Add($"outer:{e.ClosestEdge}"))
                .Add(x => x.ChildContent, s => b =>
                {
                    states.Content()(s)(b);
                    b.OpenComponent<BdndDropTarget<string>>(0);
                    b.AddComponentParameter(1, nameof(BdndDropTarget<string>.Accept), "card");
                    b.AddComponentParameter(2, nameof(BdndDropTarget<string>.Edges), BdndEdges.Vertical);
                    b.AddComponentParameter(3, nameof(BdndDropTarget<string>.OnDrop),
                        EventCallback.Factory.Create<BdndDropEventArgs<string>>(this, e => log.Add($"inner:{e.ClosestEdge}")));
                    b.CloseComponent();
                })));
        var targets = cut.FindComponents<BdndDropTarget<string>>();
        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());
        await HoverAsync(targets[0]);
        await HoverAsync(targets[1]);

        await EdgeAsync(targets[0], "top");
        await EdgeAsync(targets[1], "bottom");

        Assert.IsNull(states.Last.ClosestEdge, "the outer target is not the shallow one: no second indicator");
        await DropAsync(cut.Find("div div")); // in the root DOM, so the drop bubbles to the outer target
        Assert.AreSequenceEqual(new[] { "inner:Bottom", "outer:" }, log);
    }

    [TestMethod]
    public async Task ATargetThatRefusesTheItemShowsNoEdge()
    {
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "card").Add(x => x.Item, "a"))
            .AddChildContent<BdndDropTarget<string>>(t => t
                .Add(x => x.Accept, "card")
                .Add(x => x.Edges, BdndEdges.Vertical)
                .Add(x => x.CanDropPredicate, (_, _) => false)
                .Add(x => x.ChildContent, _states.Content())));
        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());
        await HoverAsync(Target(cut));

        await EdgeAsync(Target(cut), "top");

        Assert.IsTrue(_states.Last.IsOver);
        Assert.IsNull(_states.Last.ClosestEdge);
    }

    [TestMethod]
    public async Task AnEdgeChangeReRendersOnlyItsTarget()
    {
        var other = new StateLog<BdndDropState<string>>();
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "card").Add(x => x.Item, "a"))
            .AddChildContent<BdndDropTarget<string>>(t => t.Add(x => x.Accept, "card").Add(x => x.Edges, BdndEdges.Vertical)
                .Add(x => x.ChildContent, _states.Content()))
            .AddChildContent<BdndDropTarget<string>>(t => t.Add(x => x.Accept, "card").Add(x => x.Edges, BdndEdges.Vertical)
                .Add(x => x.ChildContent, other.Content())));
        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());
        await HoverAsync(Target(cut));
        var renders = other.Count;

        await EdgeAsync(Target(cut), "top");
        await EdgeAsync(Target(cut), "bottom");

        Assert.AreEqual(BdndEdge.Bottom, _states.Last.ClosestEdge);
        Assert.AreEqual(renders, other.Count, "the other target did not render again");
    }

    [TestMethod]
    public async Task HoverReportsWaitForAnAsyncOnBeginInTheirOrder()
    {
        var gate = new TaskCompletionSource();
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "card").Add(x => x.Item, "a")
                .Add(x => x.OnBegin, async _ => await gate.Task))
            .AddChildContent<BdndDropTarget<string>>(t => t.Add(x => x.Accept, "card").Add(x => x.Edges, BdndEdges.Vertical)
                .Add(x => x.ChildContent, _states.Content())));
        var source = cut.FindComponent<BdndDraggable<string>>();
        Manager.BeginDrag(SourceIdOf(source.Instance), "a", "card", default, default, () => gate.Task,
            source: (IBdndDragSourceHandler)source.Instance);

        var enter = HoverAsync(Target(cut));
        await EdgeAsync(Target(cut), "top");
        var leave = HoverAsync(Target(cut), isOver: false);
        gate.SetResult();
        await Task.WhenAll(enter, leave).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.IsFalse(_states.Last.IsOver, "the leave, reported last, wins");
        Assert.IsNull(_states.Last.ClosestEdge);
    }

    [TestMethod]
    public async Task HoverAndDropCarryTheEdge()
    {
        var cut = RenderList();
        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());
        await HoverAsync(Target(cut));
        await EdgeAsync(Target(cut), "bottom");

        await HostOf(Target(cut)).DragOverAsync(PointerAt(1, 1, SourceType("in-app")));
        await DropAsync(Target(cut));

        Assert.AreSequenceEqual(new[] { "hover:Bottom", "drop:Bottom" }, _log);
    }

    [TestMethod]
    public async Task ANativeDropCarriesTheEdge()
    {
        var cut = RenderList();
        await HoverAsync(Target(cut), true, "Files");
        await EdgeAsync(Target(cut), "top");
        Assert.AreEqual(BdndEdge.Top, _states.Last.ClosestEdge);

        await Target(cut).InvokeAsync(() => Target(cut).Instance.HandleNativeDrop(new BdndNativeDropPayload { Types = ["Files"] }));

        Assert.AreSequenceEqual(new[] { "native:Top" }, _log);
    }

    [TestMethod]
    public async Task ANativeDropWhoseHoverEndedBeforeItStillCarriesTheEdge()
    {
        var cut = RenderList();
        await HoverAsync(Target(cut), true, "Files");
        await EdgeAsync(Target(cut), "bottom");
        // the module reports the leave first while it walks dropped folders
        await HoverAsync(Target(cut), false, "Files");
        Assert.IsNull(_states.Last.ClosestEdge, "no longer shown");

        await Target(cut).InvokeAsync(() => Target(cut).Instance.HandleNativeDrop(new BdndNativeDropPayload { Types = ["Files"] }));

        Assert.AreSequenceEqual(new[] { "native:Bottom" }, _log);
    }

    [TestMethod]
    public async Task ANativeDropCarriesNoEdgeAHoverBeforeItLeftBehind()
    {
        var cut = RenderList();
        await HoverAsync(Target(cut), true, "Files");
        await EdgeAsync(Target(cut), "bottom");
        await HoverAsync(Target(cut), false, "Files");
        // back over the target, with no edge reported yet
        await HoverAsync(Target(cut), true, "Files");

        await Target(cut).InvokeAsync(() => Target(cut).Instance.HandleNativeDrop(new BdndNativeDropPayload { Types = ["Files"] }));

        Assert.AreSequenceEqual(new[] { "native:" }, _log);
    }

    [TestMethod]
    public async Task AKeyboardDragLandsOnAnEdgeAndDropsThere()
    {
        var cut = RenderList();
        var source = cut.FindComponent<BdndDraggable<string>>();
        var targetId = ((BdndDropTargetOptions)TargetRegistrationCall(Target(cut).Instance).Arguments[2]!).Id;
        Sources.Setup<BdndKeyboardTarget?>("keyboardNavigate", i => (string)i.Arguments[4]! == "next")
            .SetResult(new BdndKeyboardTarget
            {
                Id = targetId,
                Edge = "bottom",
                Rect = new BdndDomRect { Left = 0, Top = 100, Width = 200, Height = 40 },
                Position = 1,
                Count = 1,
                Moved = true,
            });
        await source.InvokeAsync(() => source.Instance.HandleKeyboardDragStart(new BdndDomRect(), "A"));

        await source.InvokeAsync(() => source.Instance.HandleKeyboardCommand("next"));
        Assert.AreEqual(BdndEdge.Bottom, _states.Last.ClosestEdge);
        Assert.AreEqual(new BdndXYCoord(100, 130), Manager.GetClientOffset(), "the pointer is in the half next to the edge");
        var announcements = Module.Invocations["announce"].Select(i => (string)i.Arguments[0]!).ToArray();
        Assert.AreEqual("A is after B.", announcements[^1]);

        await source.InvokeAsync(() => source.Instance.HandleKeyboardCommand("drop"));

        Assert.AreSequenceEqual(new[] { "hover:Bottom", "drop:Bottom", "end:True" }, _log);
        announcements = [.. Module.Invocations["announce"].Select(i => (string)i.Arguments[0]!)];
        Assert.AreEqual("A was dropped after B.", announcements[^1]);
    }

    [TestMethod]
    public async Task AKeyboardDragIgnoresEdgeReportsOfTheModule()
    {
        var cut = RenderList();
        var source = cut.FindComponent<BdndDraggable<string>>();
        await source.InvokeAsync(() => source.Instance.HandleKeyboardDragStart(new BdndDomRect(), "A"));

        await EdgeAsync(Target(cut), "top");

        Assert.IsNull(Manager.GetTargetEdge(((BdndDropTargetOptions)TargetRegistrationCall(Target(cut).Instance).Arguments[2]!).Id));
    }

    [TestMethod]
    public async Task TheKeyboardIsToldEachCandidatesParentAndEdgesAndTheTargetsAroundTheSource()
    {
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDropTarget<string>>(list => list
                .Add(x => x.Accept, "card")
                .Add(x => x.ChildContent, _ => b =>
                {
                    b.OpenComponent<BdndDraggable<string>>(0);
                    b.AddComponentParameter(1, nameof(BdndDraggable<string>.Type), "card");
                    b.AddComponentParameter(2, nameof(BdndDraggable<string>.Item), "a");
                    b.CloseComponent();
                    b.OpenComponent<BdndDropTarget<string>>(3);
                    b.AddComponentParameter(4, nameof(BdndDropTarget<string>.Accept), "card");
                    b.AddComponentParameter(5, nameof(BdndDropTarget<string>.Edges), BdndEdges.Vertical);
                    b.CloseComponent();
                })));
        var targets = cut.FindComponents<BdndDropTarget<string>>();
        var listId = ((BdndDropTargetOptions)TargetRegistrationCall(targets[0].Instance).Arguments[2]!).Id;
        var itemId = ((BdndDropTargetOptions)TargetRegistrationCall(targets[1].Instance).Arguments[2]!).Id;
        var source = cut.FindComponent<BdndDraggable<string>>();

        await source.InvokeAsync(() => source.Instance.HandleKeyboardDragStart(new BdndDomRect(), "A"));

        var call = Sources.Invocations["keyboardNavigate"].Last();
        var candidates = (BdndKeyboardCandidate[])call.Arguments[0]!;
        var item = candidates.Single(c => c.Id == itemId);
        Assert.AreEqual(listId, item.ParentId);
        Assert.AreEqual((int)BdndEdges.Vertical, item.Edges);
        Assert.AreSequenceEqual(new[] { listId }, (string[])call.Arguments[1]!);
    }
}
