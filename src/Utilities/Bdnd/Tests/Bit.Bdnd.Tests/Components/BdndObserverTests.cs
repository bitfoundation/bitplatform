using Bunit;

namespace Bit.Bdnd.Tests.Components;

/// <summary>
/// What watches drags: a target's <c>OnEnter</c> and <c>OnLeave</c>, a provider's <c>OnDragStart</c> and <c>OnDragEnd</c>
/// (and the manager's events behind them) - and a <c>Disabled</c> target, which takes part in none of them.
/// </summary>
[TestClass]
public class BdndObserverTests : BdndComponentTestBase
{
    private readonly List<string> _log = [];

    private IRenderedComponent<BdndProvider> RenderDustbin(bool disabled = false)
        => Render<BdndProvider>(p => p
            .Add(x => x.Manager, Manager)
            .Add(x => x.OnDragStart, e => _log.Add($"start:{e.Item}:{e.ItemType}:{e.Input}"))
            .Add(x => x.OnDragEnd, e => _log.Add($"end:{e.Item}:{e.DidDrop}:{e.DropResult}:{e.Monitor.IsDragging()}"))
            .AddChildContent<BdndDraggable<string>>(d => d
                .Add(x => x.Type, "box")
                .Add(x => x.Item, "crate")
                .Add(x => x.OnBegin, item => _log.Add($"begin:{item}"))
                .Add(x => x.OnEnd, e => _log.Add($"source-end:{e.DidDrop}")))
            .AddChildContent<BdndDropTarget<string>>(t => t
                .Add(x => x.Accept, "box")
                .Add(x => x.Disabled, disabled)
                .Add(x => x.OnEnter, e => _log.Add($"enter:{e.Item}"))
                .Add(x => x.OnLeave, e => _log.Add($"leave:{e.Item}"))
                .Add(x => x.OnDrop, e =>
                {
                    _log.Add("drop");
                    e.Result = "kept";
                })));

    private static IRenderedComponent<BdndDraggable<string>> Source(IRenderedComponent<BdndProvider> cut) => cut.FindComponent<BdndDraggable<string>>();

    private static IRenderedComponent<BdndDropTarget<string>> Bin(IRenderedComponent<BdndProvider> cut) => cut.FindComponent<BdndDropTarget<string>>();

    [TestMethod]
    public async Task ADragRunsStartEnterLeaveEnterDropAndEndInOrder()
    {
        var cut = RenderDustbin();

        await StartDragAsync(Source(cut));
        await HoverAsync(Bin(cut));
        await HoverAsync(Bin(cut), isOver: false);
        await HoverAsync(Bin(cut));
        await DropAsync(Bin(cut));
        await EndDragAsync(Source(cut));

        // OnDragStart and OnDragEnd run once the manager's update is over: after OnBegin started, after OnEnd began
        cut.WaitForAssertion(() => Assert.AreSequenceEqual(new[]
        {
            "begin:crate",
            "start:crate:box:Native",
            "enter:crate",
            "leave:crate",
            "enter:crate",
            "drop",
            "source-end:True",
            "end:crate:True:kept:False",
        }, _log));
    }

    [TestMethod]
    public async Task ADragEndingOverTheTargetDoesNotLeaveIt()
    {
        var cut = RenderDustbin();
        await StartDragAsync(Source(cut));
        await HoverAsync(Bin(cut));

        await EndDragAsync(Source(cut));

        cut.WaitForAssertion(() => Assert.AreEqual("end:crate:False::False", _log[^1]));
        Assert.IsFalse(_log.Any(l => l.StartsWith("leave", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task AKeyboardDragEntersAndLeavesTargetsAndReportsItsInput()
    {
        var cut = RenderDustbin();
        var binId = ((BdndDropTargetOptions)TargetRegistrationCall(Bin(cut).Instance).Arguments[2]!).Id;
        Sources.Setup<BdndKeyboardTarget?>("keyboardNavigate", i => (string)i.Arguments[4]! == "next")
            .SetResult(new BdndKeyboardTarget { Id = binId, Rect = new BdndDomRect(), Position = 1, Count = 1, Moved = true });
        var source = Source(cut);

        await source.InvokeAsync(() => source.Instance.HandleKeyboardDragStart(new BdndDomRect(), "crate"));
        await source.InvokeAsync(() => source.Instance.HandleKeyboardCommand("next"));
        await source.InvokeAsync(() => source.Instance.HandleKeyboardCommand("cancel"));

        cut.WaitForAssertion(() => Assert.AreSequenceEqual(
            new[] { "begin:crate", "start:crate:box:Keyboard", "enter:crate", "source-end:False", "end:crate:False::False" }, _log));
    }

    [TestMethod]
    public async Task ADisabledTargetTakesPartInNothing()
    {
        var states = new StateLog<BdndDropState<string>>();
        var cut = Render<BdndProvider>(p => p
            .Add(x => x.Manager, Manager)
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.Item, "crate"))
            .AddChildContent<BdndDropTarget<string>>(t => t
                .Add(x => x.AcceptTypes, ["box", BdndNativeTypes.File])
                .Add(x => x.Disabled, true)
                .Add(x => x.OnDrop, _ => _log.Add("drop"))
                .Add(x => x.OnNativeDrop, _ => _log.Add("native"))
                .Add(x => x.ChildContent, states.Content())));
        var bin = cut.FindComponent<BdndDropTarget<string>>();
        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());

        await HoverAsync(bin);
        await DropAsync(bin);
        await EndDragAsync(cut.FindComponent<BdndDraggable<string>>());
        await HoverAsync(bin, true, "Files");
        await bin.InvokeAsync(() => bin.Instance.HandleNativeDrop(new BdndNativeDropPayload { Types = ["Files"] }));

        Assert.IsFalse(states.Last.CanDrop);
        Assert.IsFalse(states.Last.IsOver);
        Assert.IsEmpty(Manager.GetDroppableTargets(), "the keyboard skips it");
        Assert.IsEmpty(TargetRegistrations.Last().NativeTypes);
        Assert.IsEmpty(_log);
    }

    [TestMethod]
    public async Task DisablingAHoveredTargetTakesItsHoverAwayAtOnce()
    {
        var states = new StateLog<BdndDropState<string>>();
        var cut = Render<BdndProvider>(p => p
            .Add(x => x.Manager, Manager)
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.Item, "crate"))
            .AddChildContent<BdndDropTarget<string>>(t => t
                .Add(x => x.Accept, "box")
                .Add(x => x.OnLeave, _ => _log.Add("leave"))
                .Add(x => x.ChildContent, states.Content())));
        var bin = cut.FindComponent<BdndDropTarget<string>>();
        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());
        await HoverAsync(bin);
        Assert.IsTrue(states.Last.IsOver);

        bin.Render(p => p.Add(x => x.Disabled, true));

        cut.WaitForAssertion(() => Assert.IsFalse(states.Last.IsOver));
        Assert.IsFalse(HostOf(bin).HasAttribute("data-bit-bdnd-over"));
        cut.WaitForAssertion(() => Assert.AreSequenceEqual(new[] { "leave" }, _log));
    }

    [TestMethod]
    public async Task MovingBetweenNestedTargetsEntersAndLeavesEachOnce()
    {
        var log = new List<string>();
        var cut = Render<BdndProvider>(p => p
            .Add(x => x.Manager, Manager)
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.Item, "crate"))
            .AddChildContent<BdndDropTarget<string>>(outer => outer
                .Add(x => x.Accept, "box")
                .Add(x => x.OnEnter, _ => log.Add("enter:outer"))
                .Add(x => x.OnLeave, _ => log.Add("leave:outer"))
                .Add(x => x.ChildContent, _ => b =>
                {
                    b.OpenComponent<BdndDropTarget<string>>(0);
                    b.AddComponentParameter(1, nameof(BdndDropTarget<string>.Accept), "box");
                    b.AddComponentParameter(2, nameof(BdndDropTarget<string>.OnEnter),
                        Microsoft.AspNetCore.Components.EventCallback.Factory.Create<BdndDropEventArgs<string>>(this, () => log.Add("enter:inner")));
                    b.AddComponentParameter(3, nameof(BdndDropTarget<string>.OnLeave),
                        Microsoft.AspNetCore.Components.EventCallback.Factory.Create<BdndDropEventArgs<string>>(this, () => log.Add("leave:inner")));
                    b.CloseComponent();
                })));
        var targets = cut.FindComponents<BdndDropTarget<string>>();
        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());

        await HoverAsync(targets[0]);
        await HoverAsync(targets[1]);
        await HoverAsync(targets[1], isOver: false);
        await HoverAsync(targets[0], isOver: false);

        // deep hover: over the inner target the outer one is still over - it is left only when the drag leaves it
        Assert.AreSequenceEqual(new[] { "enter:outer", "enter:inner", "leave:inner", "leave:outer" }, log);
    }

    [TestMethod]
    public async Task AThrowingDragEndedHandlerDoesNotStopTheDragFromEnding()
    {
        var cut = RenderDustbin();
        Manager.DragEnded += _ => throw new InvalidOperationException("handler failed");
        await StartDragAsync(Source(cut));

        await EndDragAsync(Source(cut));

        Assert.IsFalse(Manager.IsDragging());
        var error = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreEqual("handler failed", error.Message);
    }

    [TestMethod]
    public async Task AThrowingDragStartedHandlerDoesNotKeepOnBeginFromRunning()
    {
        var cut = RenderDustbin();
        Manager.DragStarted += _ => throw new InvalidOperationException("handler failed");

        // the failure reaches the renderer like an event handler's - bUnit rethrows it from the dispatch
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => StartDragAsync(Source(cut)));

        Assert.AreEqual("handler failed", error.Message);
        Assert.Contains("begin:crate", _log);
        Assert.IsTrue(Manager.IsDragging());
    }

    [TestMethod]
    public async Task ADragEndedBecauseAnotherBeganReportsTheSameOutcomeToOnEndAndOnDragEnd()
    {
        var gate = new TaskCompletionSource();
        var log = new List<string>();
        var cut = Render<BdndProvider>(p => p
            .Add(x => x.Manager, Manager)
            .Add(x => x.OnDragEnd, e => log.Add($"drag-end:{e.DidDrop}:{e.DropResult}"))
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.Item, "crate")
                .Add(x => x.OnEnd, e => log.Add($"on-end:{e.DidDrop}:{e.DropResult}")))
            .AddChildContent<BdndDropTarget<string>>(t => t.Add(x => x.Accept, "box").Add(x => x.OnDrop, async e =>
            {
                await gate.Task;
                e.Result = "late";
            })));
        var source = cut.FindComponent<BdndDraggable<string>>();
        await StartDragAsync(source);
        _ = DropAsync(cut.FindComponent<BdndDropTarget<string>>());

        // the next drag begins while the async OnDrop of the first is still running
        await StartDragAsync(source);
        gate.SetResult();

        cut.WaitForAssertion(() => Assert.AreSequenceEqual(new[] { "on-end:True:", "drag-end:True:" }, log));
    }

    [TestMethod]
    public async Task ADisabledTargetComesBackWhenItIsEnabledAgain()
    {
        var cut = RenderDustbin(disabled: true);
        await StartDragAsync(Source(cut));
        Assert.IsEmpty(Manager.GetDroppableTargets());

        Bin(cut).Render(p => p.Add(x => x.Disabled, false));

        Assert.HasCount(1, Manager.GetDroppableTargets());
    }

    [TestMethod]
    public void TheManagerRaisesItsEventsWithoutAProvider()
    {
        var events = new List<string>();
        var manager = new BdndDragDropManager();
        manager.DragStarted += e => events.Add($"started:{e.Item}");
        manager.DragEnded += e => events.Add($"ended:{e.DidDrop}:{e.DropResult}");

        manager.BeginDrag("source", "crate", "box", default, default);
        _ = manager.FlushBeginAsync();
        manager.RecordDrop(manager.DragId, "result");
        manager.EndDrag();

        Assert.AreSequenceEqual(new[] { "started:crate", "ended:True:result" }, events);
    }

    [TestMethod]
    public void ADragThatEndsBeforeItsBeginWasPublishedReportsNeither()
    {
        var events = new List<string>();
        var manager = new BdndDragDropManager();
        manager.DragStarted += _ => events.Add("started");
        manager.DragEnded += _ => events.Add("ended");

        manager.BeginDrag("source", "crate", "box", default, default);
        manager.EndDrag();

        Assert.IsEmpty(events);
    }

    [TestMethod]
    public async Task AFailingOnDragEndReachesTheRenderer()
    {
        var cut = Render<BdndProvider>(p => p
            .Add(x => x.Manager, Manager)
            .Add(x => x.OnDragEnd, _ => throw new InvalidOperationException("analytics failed"))
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.Item, "crate")));
        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());

        await EndDragAsync(cut.FindComponent<BdndDraggable<string>>());

        var error = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreEqual("analytics failed", error.Message);
    }
}
