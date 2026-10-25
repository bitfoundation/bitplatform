using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Bdnd.Tests.Components;

/// <summary>
/// Whole drags through the components: a tagged dragstart, hover reports from the script, the managed drop
/// event and the script's end report, in the order the browser produces them.
/// </summary>
[TestClass]
public class BdndDragLifecycleTests : BdndComponentTestBase
{
    /// <summary>How long a test waits for a drop or an end it unblocked, so a regression fails instead of hanging.</summary>
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    private readonly List<string> _log = [];
    private readonly StateLog<BdndDragState> _sourceStates = new();
    private readonly StateLog<BdndDropState<string>> _binStates = new();

    /// <summary>A draggable "crate" of type <c>box</c> and a dustbin accepting <c>box</c>, both logging their callbacks.</summary>
    private IRenderedComponent<BdndProvider> RenderDustbin(
        Action<ComponentParameterCollectionBuilder<BdndDraggable<string>>>? source = null,
        Func<BdndDragEndContext<string>, Task>? onEnd = null,
        Func<BdndDropEventArgs<string>, Task>? onDrop = null)
        => RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d =>
            {
                d.Add(x => x.Type, "box");
                d.Add(x => x.Item, "crate");
                d.Add(x => x.OnBegin, item => _log.Add($"begin:{item}"));
                d.Add(x => x.OnEnd, onEnd ?? (e =>
                {
                    _log.Add($"end:{e.Item}:{e.DidDrop}:{e.DropResult}");
                    return Task.CompletedTask;
                }));
                d.Add(x => x.ChildContent, _sourceStates.Content());
                source?.Invoke(d);
            })
            .AddChildContent<BdndDropTarget<string>>(t => t
                .Add(x => x.Accept, "box")
                .Add(x => x.OnDrop, onDrop ?? (e =>
                {
                    _log.Add($"drop:{e.Item}");
                    e.Result = "archived";
                    return Task.CompletedTask;
                }))
                .Add(x => x.ChildContent, _binStates.Content())));

    private static IRenderedComponent<BdndDraggable<string>> Source(IRenderedComponent<BdndProvider> cut)
        => cut.FindComponent<BdndDraggable<string>>();

    private static IRenderedComponent<BdndDropTarget<string>> Bin(IRenderedComponent<BdndProvider> cut)
        => cut.FindComponent<BdndDropTarget<string>>();

    // ---- a whole drag ----

    [TestMethod]
    public async Task ADragRunsBeginDropAndEndInOrderAndHandsTheResultToOnEnd()
    {
        var cut = RenderDustbin();

        await StartDragAsync(Source(cut), 10, 10);
        Assert.IsTrue(_sourceStates.Last.IsDragging);
        Assert.IsTrue(_binStates.Last.CanDrop);
        Assert.IsFalse(_binStates.Last.IsOver);

        await HoverAsync(Bin(cut));
        Assert.IsTrue(_binStates.Last.IsOver);
        Assert.IsTrue(_binStates.Last.IsOverShallow);

        await DropAsync(Bin(cut), 50, 60);
        Assert.IsTrue(Manager.DidDrop());
        Assert.AreEqual("archived", Manager.GetDropResult());
        Assert.AreEqual(new BdndXYCoord(50, 60), Manager.GetClientOffset());

        await EndDragAsync(Source(cut));

        Assert.AreSequenceEqual(new[] { "begin:crate", "drop:crate", "end:crate:True:archived" }, _log);
        Assert.IsFalse(Manager.IsDragging());
        Assert.IsFalse(_sourceStates.Last.IsDragging);
        Assert.IsFalse(_binStates.Last.IsOver);
        Assert.IsFalse(_binStates.Last.CanDrop);
        Assert.IsNull(_binStates.Last.ItemType);
    }

    [TestMethod]
    public async Task ADragEndingOutsideEveryTargetReportsNoDrop()
    {
        var cut = RenderDustbin();

        await StartDragAsync(Source(cut));
        await EndDragAsync(Source(cut));

        Assert.AreSequenceEqual(new[] { "begin:crate", "end:crate:False:" }, _log);
    }

    [TestMethod]
    public async Task ASecondDragStartsClean()
    {
        var cut = RenderDustbin();
        await StartDragAsync(Source(cut));
        await DropAsync(Bin(cut));
        await EndDragAsync(Source(cut));

        await StartDragAsync(Source(cut));
        Assert.IsFalse(Manager.DidDrop());
        Assert.IsNull(Manager.GetDropResult());
        await EndDragAsync(Source(cut));

        Assert.AreEqual("end:crate:False:", _log[^1]);
    }

    [TestMethod]
    public async Task TheEndContextCarriesTheMonitor()
    {
        BdndDragEndContext<string>? end = null;
        var cut = RenderDustbin(onEnd: e =>
        {
            end = e;
            return Task.CompletedTask;
        });

        await StartDragAsync(Source(cut));
        await EndDragAsync(Source(cut));

        Assert.IsNotNull(end);
        Assert.AreSame(Manager, end.Monitor);
        Assert.AreEqual("crate", end.Item);
    }

    // ---- drag start ----

    [TestMethod]
    public async Task DragStartPublishesTheItemAndThePointer()
    {
        var cut = RenderDustbin();

        await StartDragAsync(Source(cut), clientX: 100, clientY: 60, offsetX: 10, offsetY: 15);

        Assert.IsTrue(Manager.IsDragging(SourceIdOf(Source(cut).Instance)));
        Assert.AreEqual("box", Manager.GetItemType());
        Assert.AreEqual("crate", Manager.GetItem());
        Assert.AreEqual(new BdndXYCoord(100, 60), Manager.GetInitialClientOffset());
        Assert.AreEqual(new BdndXYCoord(90, 45), Manager.GetInitialSourceClientOffset(), "pointer minus its offset in the source");
    }

    [TestMethod]
    public async Task TheSourceRectMeasuredDuringDragStartIsUsedAtOnce()
    {
        // Blazor WebAssembly: the registration is in-process and answers synchronously
        Sources.Setup<BdndDomRect?>("getDragStartRect", i => i.InvocationMethodName == "Invoke")
            .SetResult(new BdndDomRect { Left = 40, Top = 50, Width = 120, Height = 30 });
        var cut = RenderDustbin();

        await StartDragAsync(Source(cut), clientX: 100, clientY: 60, offsetX: 10, offsetY: 15);

        Assert.AreEqual(new BdndXYCoord(40, 50), Manager.GetInitialSourceClientOffset());
        Assert.ContainsSingle(Sources.Invocations["getDragStartRect"]);
    }

    [TestMethod]
    public async Task AnEstimatedSourceOffsetIsCorrectedOnceTheRectArrives()
    {
        // Blazor Server: the rect is fetched after the drag has been published
        Sources.Setup<BdndDomRect?>("getDragStartRect", i => i.InvocationMethodName == "InvokeAsync")
            .SetResult(new BdndDomRect { Left = 40, Top = 50, Width = 120, Height = 30 });
        var cut = RenderDustbin();
        var offsetChanges = 0;
        Manager.OffsetChanged += () => offsetChanges++;

        await StartDragAsync(Source(cut), clientX: 100, clientY: 60, offsetX: 10, offsetY: 15);

        Assert.AreEqual(new BdndXYCoord(40, 50), Manager.GetInitialSourceClientOffset());
        Assert.AreEqual(1, offsetChanges, "drag layers re-render from the corrected corner");
    }

    [TestMethod]
    public async Task ARectArrivingAfterTheDragEndedIsIgnored()
    {
        var rect = Sources.Setup<BdndDomRect?>("getDragStartRect", i => i.InvocationMethodName == "InvokeAsync");
        var cut = RenderDustbin();

        var start = StartDragAsync(Source(cut));
        await EndDragAsync(Source(cut));
        rect.SetResult(new BdndDomRect { Left = 40, Top = 50 });
        await start;

        Assert.IsFalse(Manager.IsDragging());
        Assert.IsNull(Manager.GetInitialSourceClientOffset());
        Assert.AreSequenceEqual(new[] { "begin:crate", "end:crate:False:" }, _log);
    }

    [TestMethod]
    public async Task ItemFactoryProducesTheItemWhenTheDragStarts()
    {
        var produced = 0;
        var cut = RenderDustbin(d => d.Add(x => x.ItemFactory, () => $"lazy-{++produced}"));
        Assert.AreEqual(0, produced);

        await StartDragAsync(Source(cut));
        await EndDragAsync(Source(cut));

        Assert.AreEqual(1, produced);
        Assert.AreSequenceEqual(new[] { "begin:lazy-1", "end:lazy-1:False:" }, _log);
    }

    [TestMethod]
    public async Task AnItemFactoryReturningNullCancelsTheDrag()
    {
        var cut = RenderDustbin(d => d.Add(x => x.ItemFactory, () => null));

        await StartDragAsync(Source(cut));

        Assert.IsFalse(Manager.IsDragging());
        Assert.IsEmpty(_log);
        Assert.IsFalse(_sourceStates.Last.IsDragging);
        Assert.ContainsSingle(Sources.Invocations["cancelDrag"]);
    }

    [TestMethod]
    public async Task ADraggableThatCanNoLongerDragCancelsTheDragWithoutProducingAnItem()
    {
        var allowed = true;
        var produced = 0;
        var cut = RenderDustbin(d => d
            .Add(x => x.CanDragPredicate, () => allowed)
            .Add(x => x.ItemFactory, () => $"lazy-{++produced}"));

        allowed = false;
        await StartDragAsync(Source(cut));

        Assert.IsFalse(Manager.IsDragging());
        Assert.AreEqual(0, produced);
        Assert.ContainsSingle(Sources.Invocations["cancelDrag"]);
    }

    [TestMethod]
    public async Task ADragStartNotTaggedWithThisSourceIsIgnored()
    {
        var cut = RenderDustbin();
        var renders = _sourceStates.Count;

        await HostOf(Source(cut)).DragStartAsync(PointerAt(0, 0, SourceType("someone-else")));
        await HostOf(Source(cut)).DragStartAsync(new DragEventArgs { DataTransfer = null! });

        Assert.IsFalse(Manager.IsDragging());
        Assert.IsEmpty(_log);
        Assert.IsEmpty(Sources.Invocations["cancelDrag"]);
        Assert.AreEqual(renders, _sourceStates.Count, "drag events do not re-render the draggable by themselves");
    }

    [TestMethod]
    public async Task NestedDraggablesDragTheInnermostOne()
    {
        var cut = RenderProvider(p => p.AddChildContent<BdndDraggable<string>>(outer => outer
            .Add(x => x.Type, "box")
            .Add(x => x.Item, "outer")
            .Add<BdndDraggable<string>, BdndDragState>(x => x.ChildContent, _ => inner => inner
                .Add(x => x.Type, "box")
                .Add(x => x.Item, "inner"))));
        var draggables = cut.FindComponents<BdndDraggable<string>>();
        var innerType = SourceType(SourceIdOf(draggables[1].Instance));

        // the script tags the drag with the innermost source; reaching the outer host must not start it
        await HostOf(draggables[0]).DragStartAsync(PointerAt(0, 0, innerType));
        Assert.IsFalse(Manager.IsDragging());

        await StartDragAsync(draggables[1]);

        Assert.AreEqual("inner", Manager.GetItem());
        Assert.IsTrue(Manager.IsDragging(SourceIdOf(draggables[1].Instance)));
        Assert.IsFalse(Manager.IsDragging(SourceIdOf(draggables[0].Instance)));
    }

    // ---- ordering ----

    [TestMethod]
    [DataRow("hover")]
    [DataRow("dragover")]
    [DataRow("drop")]
    [DataRow("move")]
    [DataRow("end")]
    public async Task EveryCallbackPublishesAPendingBeginFirst(string callback)
    {
        // In Blazor WebAssembly the source publishes its drag a moment after dragstart; whatever arrives in
        // that window publishes it first, so OnBegin always precedes the other callbacks of the drag.
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d
                .Add(x => x.Type, "box")
                .Add(x => x.OnEnd, _ => _log.Add("end")))
            .AddChildContent<BdndDropTarget<string>>(t => t
                .Add(x => x.Accept, "box")
                .Add(x => x.OnHover, _ => _log.Add("hover"))
                .Add(x => x.OnDrop, _ => _log.Add("drop"))));
        var source = Source(cut);
        var bin = Bin(cut);
        await cut.InvokeAsync(() => Manager.BeginDrag(SourceIdOf(source.Instance), "crate", "box", new(0, 0), new(0, 0), () =>
        {
            _log.Add("begin");
            return Task.CompletedTask;
        }));

        await (callback switch
        {
            "hover" => HoverAsync(bin),
            "dragover" => HostOf(bin).DragOverAsync(PointerAt(1, 1)),
            "drop" => DropAsync(bin),
            "move" => source.InvokeAsync(() => source.Instance.HandleDragMove(1, 1)),
            _ => EndDragAsync(source),
        });

        Assert.AreEqual("begin", _log[0]);
        Assert.ContainsSingle(_log.Where(entry => entry == "begin"));
    }

    // ---- drop ----

    [TestMethod]
    [DataRow("outer", "outer")]
    [DataRow(null, "inner")]
    public async Task NestedTargetsReceiveTheDropInnermostFirstAndAnOuterResultWins(string? outerResult, string expected)
    {
        BdndDragEndContext<string>? end = null;
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d
                .Add(x => x.Type, "box")
                .Add(x => x.Item, "crate")
                .Add(x => x.OnEnd, e => end = e))
            .AddChildContent<BdndDropTarget<string>>(outer => outer
                .Add(x => x.Accept, "box")
                .Add(x => x.OnDrop, e =>
                {
                    _log.Add($"outer:{e.Monitor.DidDrop()}");
                    e.Result = outerResult;
                })
                .Add<BdndDropTarget<string>, BdndDropState<string>>(x => x.ChildContent, _ => inner => inner
                    .Add(x => x.Accept, "box")
                    .AddUnmatched("id", "inner")
                    .Add(x => x.OnDrop, e =>
                    {
                        _log.Add($"inner:{e.Monitor.DidDrop()}:{e.Item}");
                        e.Result = "inner";
                    }))));

        await StartDragAsync(Source(cut));
        await DropAsync(cut.Find("#inner"));
        await EndDragAsync(Source(cut));

        Assert.AreSequenceEqual(new[] { "inner:False:crate", "outer:True" }, _log);
        Assert.IsNotNull(end);
        Assert.IsTrue(end.DidDrop);
        Assert.AreEqual(expected, end.DropResult);
    }

    [TestMethod]
    public async Task ATargetThatCannotAcceptIsSkippedWhileAnEnclosingOneReceivesTheDrop()
    {
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.Item, "crate"))
            .AddChildContent<BdndDropTarget<string>>(outer => outer
                .Add(x => x.Accept, "box")
                .Add(x => x.OnDrop, e => _log.Add($"outer:{e.Monitor.DidDrop()}"))
                .Add<BdndDropTarget<string>, BdndDropState<string>>(x => x.ChildContent, _ => middle => middle
                    .Add(x => x.Accept, "card")
                    .Add(x => x.OnDrop, _ => _log.Add("middle"))
                    .Add<BdndDropTarget<string>, BdndDropState<string>>(x => x.ChildContent, _ => inner => inner
                        .Add(x => x.Accept, "box")
                        .AddUnmatched("id", "inner")
                        .Add(x => x.CanDropPredicate, (_, _) => false)
                        .Add(x => x.OnDrop, _ => _log.Add("inner"))))));

        await StartDragAsync(Source(cut));
        await DropAsync(cut.Find("#inner"));

        Assert.AreSequenceEqual(new[] { "outer:False" }, _log);
    }

    [TestMethod]
    public async Task OnEndWaitsForAnAsyncOnDropAndGetsTheResultItSetsAfterAnAwait()
    {
        var gate = new TaskCompletionSource();
        var cut = RenderDustbin(onDrop: async e =>
        {
            await gate.Task;
            e.Result = "late";
        });
        await StartDragAsync(Source(cut));

        var drop = DropAsync(Bin(cut));
        Assert.IsTrue(Manager.DidDrop(), "marked before the handler completes");
        // on WebAssembly the browser fires dragend right after drop, while the handler still awaits
        var end = EndDragAsync(Source(cut));
        Assert.IsTrue(Manager.IsDragging(), "the drag lasts until the async OnDrop has completed");
        Assert.AreEqual("begin:crate", _log[^1]);

        gate.SetResult();
        await drop.WaitAsync(WaitLimit);
        await end.WaitAsync(WaitLimit);

        Assert.AreEqual("end:crate:True:late", _log[^1]);
        Assert.IsFalse(Manager.IsDragging());
    }

    [TestMethod]
    public async Task ADragOfTheSourceBeginningWhileItsLastDragEndsHandsEachOnEndItsOwnItem()
    {
        var gate = new TaskCompletionSource();
        var produced = 0;
        var cut = RenderDustbin(
            source: d => d.Add(x => x.ItemFactory, () => $"crate-{++produced}"),
            onDrop: async e =>
            {
                await gate.Task;
                e.Result = "late";
            });
        await StartDragAsync(Source(cut));
        var drop = DropAsync(Bin(cut));
        // the end waits for the async OnDrop, so the drag is still in progress as the source is dragged again
        var end = EndDragAsync(Source(cut));

        await StartDragAsync(Source(cut));

        Assert.AreSequenceEqual(new[] { "begin:crate-1", "end:crate-1:True:", "begin:crate-2" }, _log);
        gate.SetResult();
        await drop.WaitAsync(WaitLimit);
        await end.WaitAsync(WaitLimit);
        await EndDragAsync(Source(cut));
        Assert.AreEqual("end:crate-2:False:", _log[^1]);
    }

    [TestMethod]
    public async Task ADraggableRemovedWhileADragStateChangeIsNotifiedNoLongerRunsItsPredicates()
    {
        var show = true;
        var callsSinceRemoval = 0;
        IRenderedComponent<BdndProvider>? cut = null;
        // subscribed before the draggable is: the notification removes the draggable before its own handler runs
        Manager.StateChanged += () =>
        {
            if (show is false || Manager.IsDragging() is false) return;
            show = false;
            cut!.Render();
        };
        cut = RenderProvider(p => p.Add(x => x.ChildContent, (RenderFragment)(b => b.AddContent(0, show
            ? Fragment<BdndDraggable<string>>(
                (nameof(BdndDraggable<string>.Type), "box"),
                (nameof(BdndDraggable<string>.Item), "crate"),
                (nameof(BdndDraggable<string>.CanDragPredicate), (Func<bool>)(() =>
                {
                    if (show is false) callsSinceRemoval++;
                    return true;
                })))
            : null))));

        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());

        Assert.IsFalse(show, "the drag's start removed the draggable");
        Assert.AreEqual(0, callsSinceRemoval);
    }

    [TestMethod]
    public async Task AnOuterResultStillWinsOverTheResultAnInnerAsyncOnDropSetsLater()
    {
        var gate = new TaskCompletionSource();
        BdndDragEndContext<string>? end = null;
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d
                .Add(x => x.Type, "box")
                .Add(x => x.Item, "crate")
                .Add(x => x.OnEnd, e => end = e))
            .AddChildContent<BdndDropTarget<string>>(outer => outer
                .Add(x => x.Accept, "box")
                .Add(x => x.OnDrop, e =>
                {
                    _log.Add($"outer:{e.Monitor.DidDrop()}");
                    e.Result = "outer";
                })
                .Add<BdndDropTarget<string>, BdndDropState<string>>(x => x.ChildContent, _ => inner => inner
                    .Add(x => x.Accept, "box")
                    .AddUnmatched("id", "inner")
                    .Add(x => x.OnDrop, async e =>
                    {
                        await gate.Task;
                        e.Result = "inner";
                    }))));
        await StartDragAsync(Source(cut));

        var drop = DropAsync(cut.Find("#inner"));
        var ended = EndDragAsync(Source(cut));
        gate.SetResult();
        await drop.WaitAsync(WaitLimit);
        await ended.WaitAsync(WaitLimit);

        Assert.AreSequenceEqual(new[] { "outer:True" }, _log);
        Assert.IsNotNull(end);
        Assert.AreEqual("outer", end.DropResult);
    }

    [TestMethod]
    public async Task AFailingAsyncOnDropDoesNotKeepTheDragGoing()
    {
        var gate = new TaskCompletionSource();
        var cut = RenderDustbin(onDrop: async _ =>
        {
            await gate.Task;
            throw new InvalidOperationException("save failed");
        });
        await StartDragAsync(Source(cut));

        var drop = DropAsync(Bin(cut));
        var end = EndDragAsync(Source(cut));
        gate.SetResult();
        try
        {
            await drop.WaitAsync(WaitLimit);
        }
        catch (InvalidOperationException)
        {
            // the handler's own failure, reported by the drop event
        }
        await end.WaitAsync(WaitLimit);

        Assert.AreEqual("end:crate:True:", _log[^1]);
        Assert.IsFalse(Manager.IsDragging());
    }

    [TestMethod]
    public async Task AnAsyncOnDropThatCompletesInTimePublishesItsResult()
    {
        var gate = new TaskCompletionSource();
        var cut = RenderDustbin(onDrop: async e =>
        {
            await gate.Task;
            e.Result = "done";
        });
        await StartDragAsync(Source(cut));

        var drop = DropAsync(Bin(cut));
        gate.SetResult();
        await drop;
        await EndDragAsync(Source(cut));

        Assert.AreEqual("end:crate:True:done", _log[^1]);
    }

    // ---- end ----

    [TestMethod]
    public async Task TheDragStateIsClearedRightAfterTheSynchronousPartOfOnEnd()
    {
        var gate = new TaskCompletionSource();
        var cut = RenderDustbin(onEnd: async e =>
        {
            _log.Add($"end:{e.Monitor.IsDragging()}");
            await gate.Task;
            _log.Add($"after:{e.Monitor.IsDragging()}");
        });
        await StartDragAsync(Source(cut));
        await HoverAsync(Bin(cut));

        var end = EndDragAsync(Source(cut));

        Assert.IsFalse(Manager.IsDragging());
        Assert.IsFalse(_binStates.Last.IsOver, "an async OnEnd must not keep targets highlighted");
        // the end report completes once the drag is over: the module starts the next drag after it, which an
        // OnEnd still saving something must not hold up
        await end.WaitAsync(WaitLimit);
        gate.SetResult();
        cut.WaitForAssertion(() => Assert.AreSequenceEqual(new[] { "begin:crate", "end:True", "after:False" }, _log));
    }

    [TestMethod]
    public async Task OnEndRunsEvenWhenTheDraggableWasRemovedMidDrag()
    {
        var cut = RenderDustbin();
        var source = Source(cut).Instance;
        var dotNetRef = (DotNetObjectReference<BdndDraggable<string>>)SourceRegistrationCall(source).Arguments[1]!;
        await StartDragAsync(Source(cut));
        await DropAsync(Bin(cut));

        // e.g. the drop moved the item to another list and re-rendered this one
        cut.Render(p => p.AddChildContent("<p>moved</p>"));
        Assert.IsEmpty(cut.FindComponents<BdndDraggable<string>>());
        Assert.IsEmpty(Sources.Invocations["dispose"], "the registration reports the end, so it stays alive");
        Assert.AreSame(source, dotNetRef.Value);

        await cut.InvokeAsync(source.HandleDragEnd);

        Assert.AreEqual("end:crate:True:archived", _log[^1]);
        Assert.IsFalse(Manager.IsDragging());
        Assert.ContainsSingle(Sources.Invocations["dispose"]);
        Assert.ThrowsExactly<ObjectDisposedException>(() => dotNetRef.Value);
    }

    [TestMethod]
    public async Task AnEndReportedForASourceThatIsNotDraggingIsIgnored()
    {
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.OnEnd, _ => _log.Add("first")))
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.OnEnd, _ => _log.Add("second"))));
        var draggables = cut.FindComponents<BdndDraggable<string>>();

        await EndDragAsync(draggables[0]);
        await StartDragAsync(draggables[1]);
        await EndDragAsync(draggables[0]);

        Assert.IsEmpty(_log);
        Assert.IsTrue(Manager.IsDragging());
    }

    [TestMethod]
    public async Task PointerMovesOfTheDraggedSourceUpdateTheOffsets()
    {
        var cut = RenderDustbin();
        var source = Source(cut);

        await source.InvokeAsync(() => source.Instance.HandleDragMove(5, 5));
        Assert.IsNull(Manager.GetClientOffset(), "not dragging yet");

        await StartDragAsync(source, 10, 10);
        await source.InvokeAsync(() => source.Instance.HandleDragMove(150, 90));

        Assert.AreEqual(new BdndXYCoord(150, 90), Manager.GetClientOffset());
        Assert.AreEqual(new BdndXYCoord(140, 80), Manager.GetDifferenceFromInitialOffset());
    }

    // ---- collected state ----

    [TestMethod]
    public async Task IsDraggingOverrideDecidesTheCollectedState()
    {
        // e.g. the dragged card was re-rendered by another component instance in another list
        var originalStates = new StateLog<BdndDragState>();
        var copyStates = new StateLog<BdndDragState>();
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d
                .Add(x => x.Type, "card")
                .Add(x => x.Item, "card-7")
                .Add(x => x.ChildContent, originalStates.Content()))
            .AddChildContent<BdndDraggable<string>>(d => d
                .Add(x => x.Type, "card")
                .Add(x => x.Item, "card-7")
                .Add(x => x.IsDraggingOverride, m => m.GetItem<string>() == "card-7")
                .Add(x => x.ChildContent, copyStates.Content())));

        await StartDragAsync(cut.FindComponents<BdndDraggable<string>>()[0]);

        Assert.IsTrue(originalStates.Last.IsDragging);
        Assert.IsTrue(copyStates.Last.IsDragging);
    }

    [TestMethod]
    public async Task OnlyTheDraggedSourceReportsIsDragging()
    {
        var first = new StateLog<BdndDragState>();
        var second = new StateLog<BdndDragState>();
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.ChildContent, first.Content()))
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.ChildContent, second.Content())));

        await StartDragAsync(cut.FindComponents<BdndDraggable<string>>()[1]);

        Assert.IsFalse(first.Last.IsDragging);
        Assert.IsTrue(second.Last.IsDragging);
        Assert.IsTrue(first.Last.Monitor.IsDragging());
    }

    [TestMethod]
    public async Task AHoverChangeReRendersOnlyTheTargetsWhoseStateItChanged()
    {
        var sourceStates = new StateLog<BdndDragState>();
        var hoveredStates = new StateLog<BdndDropState<string>>();
        var otherStates = new StateLog<BdndDropState<string>>();
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.ChildContent, sourceStates.Content()))
            .AddChildContent<BdndDropTarget<string>>(t => t.Add(x => x.Accept, "box").Add(x => x.ChildContent, hoveredStates.Content()))
            .AddChildContent<BdndDropTarget<string>>(t => t.Add(x => x.Accept, "box").Add(x => x.ChildContent, otherStates.Content())));
        var targets = cut.FindComponents<BdndDropTarget<string>>();
        await StartDragAsync(Source(cut));
        var (sourceRenders, hoveredRenders, otherRenders) = (sourceStates.Count, hoveredStates.Count, otherStates.Count);

        await HoverAsync(targets[0]);
        await HoverAsync(targets[0], isOver: false);

        Assert.AreEqual(hoveredRenders + 2, hoveredStates.Count, "the hovered target renders its enter and its leave");
        Assert.AreEqual(otherRenders, otherStates.Count, "nothing the other target shows changed");
        Assert.AreEqual(sourceRenders, sourceStates.Count, "nor anything the draggable shows");

        await EndDragAsync(Source(cut));

        Assert.AreEqual(otherRenders + 1, otherStates.Count, "every target renders the end of the drag");
        Assert.AreEqual(sourceRenders + 1, sourceStates.Count);
    }

    [TestMethod]
    public async Task PointerMovesAreOnlyTrackedWhileADragLayerFollowsThem()
    {
        var withLayer = false;
        var layer = Fragment<BdndDragLayer<string>>(
            ("ChildContent", (RenderFragment<BdndDragLayerState<string>>)(_ => _ => { })));
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.Item, "crate"))
            .AddChildContent(builder =>
            {
                if (withLayer) builder.AddContent(0, layer);
            }));
        Assert.IsFalse(Assert.ContainsSingle(SourceRegistrations).TrackPointer);

        // a layer added after the draggable rendered: the draggable picks it up when the drag starts
        withLayer = true;
        cut.Render();
        await StartDragAsync(Source(cut));

        Assert.IsTrue(SourceUpdates[^1].TrackPointer, "the drag started with a layer listening");
    }

    [TestMethod]
    public async Task ALayerDrawingOtherTypesNeitherSeesTheDragNorHasItsMovesSent()
    {
        var states = new List<BdndDragLayerState<string>>();
        var renders = 0;
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.Item, "crate"))
            .AddChildContent(Fragment<BdndDragLayer<string>>(
                ("Accept", "card"),
                ("ChildContent", (RenderFragment<BdndDragLayerState<string>>)(state => _ =>
                {
                    renders++;
                    states.Add(state);
                })))));
        var source = Source(cut);

        await StartDragAsync(source, 10, 10);
        var rendersAtStart = renders;
        await source.InvokeAsync(() => source.Instance.HandleDragMove(150, 90));

        Assert.IsFalse(SourceUpdates.Concat(SourceRegistrations).Any(o => o.TrackPointer), "the only layer draws cards: the box's moves stay in the browser");
        Assert.IsTrue(Manager.IsDragging());
        Assert.IsFalse(states[^1].IsDragging, "a box drag is no drag to a layer of cards");
        Assert.IsNull(states[^1].Item);
        Assert.AreEqual(rendersAtStart, renders, "a move of a box drag does not re-render the layer");
    }

    [TestMethod]
    public async Task ALayerDrawingTheDraggedTypeFollowsItsMoves()
    {
        var states = new List<BdndDragLayerState<string>>();
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.Item, "crate"))
            .AddChildContent(Fragment<BdndDragLayer<string>>(
                ("AcceptTypes", new[] { "card", "box" }),
                ("ChildContent", (RenderFragment<BdndDragLayerState<string>>)(state => _ => states.Add(state))))));
        var source = Source(cut);

        await StartDragAsync(source, 10, 10);
        await source.InvokeAsync(() => source.Instance.HandleDragMove(150, 90));

        Assert.IsTrue((SourceUpdates.LastOrDefault() ?? SourceRegistrations[^1]).TrackPointer);
        cut.WaitForAssertion(() => Assert.AreEqual(new BdndXYCoord(150, 90), states[^1].ClientOffset));
        Assert.IsTrue(states[^1].IsDragging);
        Assert.AreEqual("crate", states[^1].Item);
    }

    [TestMethod]
    public async Task NestedTargetsReportDeepAndShallowHover()
    {
        var outerStates = new StateLog<BdndDropState<string>>();
        var innerStates = new StateLog<BdndDropState<string>>();
        var inner = Fragment<BdndDropTarget<string>>(("Accept", "box"), ("ChildContent", innerStates.Content()));
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box"))
            .AddChildContent<BdndDropTarget<string>>(t => t
                .Add(x => x.Accept, "box")
                .Add(x => x.ChildContent, outerStates.Content(_ => inner))));
        var targets = cut.FindComponents<BdndDropTarget<string>>();
        await StartDragAsync(Source(cut));

        await HoverAsync(targets[0]);
        Assert.IsTrue(outerStates.Last.IsOverShallow);

        await HoverAsync(targets[1]);
        Assert.IsTrue(outerStates.Last.IsOver);
        Assert.IsFalse(outerStates.Last.IsOverShallow);
        Assert.IsTrue(innerStates.Last.IsOver);
        Assert.IsTrue(innerStates.Last.IsOverShallow);

        await HoverAsync(targets[1], isOver: false);
        Assert.IsTrue(outerStates.Last.IsOverShallow);
        Assert.IsFalse(innerStates.Last.IsOver);
    }

    // ---- providers ----

    [TestMethod]
    public async Task ATargetOfANestedProviderIgnoresTheOuterProvidersDrag()
    {
        var innerStates = new StateLog<BdndDropState<string>>();
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.Item, "crate"))
            .AddChildContent<BdndProvider>(nested => nested.AddChildContent<BdndDropTarget<string>>(t => t
                .Add(x => x.Accept, "box")
                .Add(x => x.OnDrop, _ => _log.Add("drop"))
                .Add(x => x.ChildContent, innerStates.Content()))));
        var source = Source(cut);
        var target = Bin(cut);

        await StartDragAsync(source);
        await HoverAsync(target, true, SourceType(SourceIdOf(source.Instance)));
        await DropAsync(target);

        Assert.IsFalse(innerStates.Last.CanDrop);
        Assert.IsFalse(innerStates.Last.IsOver);
        Assert.IsNull(innerStates.Last.ItemType);
        Assert.IsEmpty(_log);
        Assert.IsFalse(Manager.DidDrop());
    }

    [TestMethod]
    public async Task ShallowHoverSkipsTheTargetsOfANestedProvider()
    {
        // A (outer manager) > X (nested provider's manager) > B (outer manager again, through a provider sharing it):
        // B's parent is A, so hovering B takes the shallow hover from A.
        var aStates = new StateLog<BdndDropState<string>>();
        var b = Fragment<BdndProvider>(
            ("Manager", Manager),
            ("ChildContent", Fragment<BdndDropTarget<string>>(("Accept", "box"))));
        var nestedX = Fragment<BdndProvider>(
            ("ChildContent", Fragment<BdndDropTarget<string>>(
                ("Accept", "box"),
                ("ChildContent", (RenderFragment<BdndDropState<string>>)(_ => b)))));
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box"))
            .AddChildContent<BdndDropTarget<string>>(t => t
                .Add(x => x.Accept, "box")
                .Add(x => x.ChildContent, aStates.Content(_ => nestedX))));
        var targets = cut.FindComponents<BdndDropTarget<string>>();
        Assert.HasCount(3, targets);
        await StartDragAsync(Source(cut));

        await HoverAsync(targets[0]);
        await HoverAsync(targets[2]);

        Assert.IsTrue(aStates.Last.IsOver);
        Assert.IsFalse(aStates.Last.IsOverShallow);
    }
}
