using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bdnd.Tests.Components;

/// <summary>
/// Pointer drags (touch, pen, or the mouse with <c>PointerDrag="All"</c>): the module reports the start
/// (<c>HandlePointerDragStart</c>), hover changes (<c>HandleHoverChanged</c>), moves over targets with an OnHover
/// (<c>HandlePointerHover</c>), the drop - innermost hovered target first, until one owns the drag
/// (<c>HandlePointerDrop</c>) - and the end (<c>HandleDragEnd</c>).
/// </summary>
[TestClass]
public class BdndPointerDragTests : BdndComponentTestBase
{
    private static readonly BdndDomRect SourceRect = new() { Left = 10, Top = 20, Width = 100, Height = 40 };

    private readonly List<string> _log = [];
    private readonly StateLog<BdndDropState<string>> _binStates = new();

    private IRenderedComponent<BdndProvider> RenderDustbin(Action<ComponentParameterCollectionBuilder<BdndDraggable<string>>>? source = null,
        Action<string?>? onBegin = null)
        => RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d =>
            {
                d.Add(x => x.Type, "box");
                d.Add(x => x.Item, "crate");
                d.Add(x => x.OnBegin, onBegin ?? (item => _log.Add($"begin:{item}")));
                d.Add(x => x.OnEnd, e => _log.Add($"end:{e.Item}:{e.DidDrop}:{e.DropResult}"));
                source?.Invoke(d);
            })
            .AddChildContent<BdndDropTarget<string>>(t => t
                .Add(x => x.Accept, "box")
                .Add(x => x.OnHover, e => _log.Add($"hover:{e.Monitor.GetClientOffset()}"))
                .Add(x => x.OnDrop, e =>
                {
                    _log.Add($"drop:{e.Item}:{e.Monitor.GetClientOffset()}");
                    e.Result = "archived";
                })
                .Add(x => x.ChildContent, _binStates.Content())));

    private static IRenderedComponent<BdndDraggable<string>> Source(IRenderedComponent<BdndProvider> cut)
        => cut.FindComponent<BdndDraggable<string>>();

    private static Task<bool> StartAsync(IRenderedComponent<BdndDraggable<string>> source, double x = 30, double y = 35)
        => source.InvokeAsync(() => source.Instance.HandlePointerDragStart(x, y, SourceRect));

    private static Task HoverAsync(IRenderedComponent<BdndDropTarget<string>> target, bool over = true)
        => target.InvokeAsync(() => target.Instance.HandleHoverChanged(over, [SourceType("pointer")]));

    [TestMethod]
    public async Task APointerDragRunsBeginHoverDropAndEnd()
    {
        var cut = RenderDustbin();
        var bin = cut.FindComponent<BdndDropTarget<string>>();

        Assert.IsTrue(await StartAsync(Source(cut)));
        Assert.AreEqual(BdndDragInput.Pointer, Manager.GetDragInput());
        Assert.AreEqual(new BdndXYCoord(30, 35), Manager.GetInitialClientOffset());
        Assert.AreEqual(new BdndXYCoord(10, 20), Manager.GetInitialSourceClientOffset());

        await HoverAsync(bin);
        Assert.IsTrue(_binStates.Last.IsOver);

        await bin.InvokeAsync(() => bin.Instance.HandlePointerHover(80, 90));
        Assert.AreEqual(new BdndXYCoord(80, 90), Manager.GetClientOffset());

        Assert.IsTrue(await bin.InvokeAsync(() => bin.Instance.HandlePointerDrop(81, 91)));
        await Source(cut).InvokeAsync(() => Source(cut).Instance.HandleDragEnd());

        Assert.AreSequenceEqual(new[]
        {
            "begin:crate",
            "hover:BdndXYCoord { X = 80, Y = 90 }",
            "drop:crate:BdndXYCoord { X = 81, Y = 91 }",
            "end:crate:True:archived",
        }, _log);
        Assert.IsFalse(Manager.IsDragging());
        Assert.IsFalse(_binStates.Last.IsOver);
    }

    [TestMethod]
    public async Task ADropOnANestedTargetRunsTheChainInnermostFirst()
    {
        var log = new List<string>();
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.Item, "crate"))
            .AddChildContent<BdndDropTarget<string>>(outer => outer
                .Add(x => x.Accept, "box")
                .Add(x => x.OnDrop, e => log.Add($"outer:{e.Monitor.DidDrop()}"))
                .Add(x => x.ChildContent, _ => Fragment<BdndDropTarget<string>>(
                    (nameof(BdndDropTarget<string>.Accept), "box"),
                    (nameof(BdndDropTarget<string>.OnDrop), EventCallback.Factory.Create<BdndDropEventArgs<string>>(this, e => log.Add($"inner:{e.Monitor.DidDrop()}")))))));
        var inner = cut.FindComponents<BdndDropTarget<string>>()[1];
        await StartAsync(cut.FindComponent<BdndDraggable<string>>());

        Assert.IsTrue(await inner.InvokeAsync(() => inner.Instance.HandlePointerDrop(0, 0)));

        Assert.AreSequenceEqual(new[] { "inner:False", "outer:True" }, log);
    }

    [TestMethod]
    public async Task ATargetOfAnotherProviderDoesNotTakeTheDrop()
    {
        var other = new BdndDragDropManager();
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.Item, "crate"))
            .AddChildContent<BdndProvider>(inner => inner
                .Add(x => x.Manager, other)
                .AddChildContent<BdndDropTarget<string>>(t => t.Add(x => x.Accept, "box").Add(x => x.OnDrop, _ => _log.Add("wrong")))));
        await StartAsync(cut.FindComponent<BdndDraggable<string>>());

        var target = cut.FindComponent<BdndDropTarget<string>>();
        Assert.IsFalse(await target.InvokeAsync(() => target.Instance.HandlePointerDrop(0, 0)));
        Assert.IsEmpty(_log);
    }

    [TestMethod]
    public async Task PointerReportsDoNotTouchANativeDrag()
    {
        var cut = RenderDustbin();
        var bin = cut.FindComponent<BdndDropTarget<string>>();
        await StartDragAsync(Source(cut));

        await bin.InvokeAsync(() => bin.Instance.HandlePointerHover(5, 5));
        Assert.IsFalse(await bin.InvokeAsync(() => bin.Instance.HandlePointerDrop(5, 5)));

        Assert.AreSequenceEqual(new[] { "begin:crate" }, _log);
        Assert.IsFalse(Manager.DidDrop());
    }

    [TestMethod]
    public async Task APointerDragStartingDuringAnotherDragEndsThatOneFirst()
    {
        var cut = RenderDustbin();
        await StartDragAsync(Source(cut));

        Assert.IsTrue(await StartAsync(Source(cut)));

        Assert.AreSequenceEqual(new[] { "begin:crate", "end:crate:False:", "begin:crate" }, _log);
        Assert.AreEqual(BdndDragInput.Pointer, Manager.GetDragInput());
    }

    [TestMethod]
    [DataRow("CanDrag")]
    [DataRow("ItemFactory")]
    [DataRow("PointerDrag")]
    public async Task ASourceThatMayNotDragRefusesThePointerDrag(string why)
    {
        var cut = RenderDustbin(d =>
        {
            if (why == "CanDrag") d.Add(x => x.CanDrag, false);
            if (why == "ItemFactory") d.Add(x => x.ItemFactory, () => null);
            if (why == "PointerDrag") d.Add(x => x.PointerDrag, BdndPointerDrag.None);
        });

        Assert.IsFalse(await StartAsync(Source(cut)));
        Assert.IsFalse(Manager.IsDragging());
        Assert.IsEmpty(_log);
    }

    [TestMethod]
    public async Task AnItemFactoryThatThrowsRefusesThePointerDragAndIsReported()
    {
        var cut = RenderDustbin(d => d.Add(x => x.ItemFactory, () => throw new InvalidOperationException("no item")));

        Assert.IsFalse(await StartAsync(Source(cut)));

        var error = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreEqual("no item", error.Message);
        Assert.IsFalse(Manager.IsDragging());
    }

    [TestMethod]
    public async Task AnOnBeginThatThrowsIsReportedAndThePointerDragStillBegins()
    {
        var cut = RenderDustbin(onBegin: _ => throw new InvalidOperationException("begin failed"));

        Assert.IsTrue(await StartAsync(Source(cut)));

        var error = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreEqual("begin failed", error.Message);
    }

    // ---- what reaches the script and the markup ----

    [TestMethod]
    public void TheSourceTellsTheScriptHowPointersDragIt()
    {
        RenderDustbin();

        var options = SourceRegistrations.Last();
        Assert.AreEqual("touch", options.PointerDrag);
        Assert.AreEqual(250, options.TouchDelay);
        Assert.AreEqual(5, options.DragThreshold);
        Assert.IsTrue(options.AutoScroll);
    }

    [TestMethod]
    [DataRow(BdndPointerDrag.All, "all")]
    [DataRow(BdndPointerDrag.None, "none")]
    public void ThePointerModeAndItsSettingsReachTheScript(BdndPointerDrag mode, string expected)
    {
        RenderDustbin(d => d
            .Add(x => x.PointerDrag, mode)
            .Add(x => x.TouchDelay, 400)
            .Add(x => x.DragThreshold, 8)
            .Add(x => x.AutoScroll, false));

        var options = SourceRegistrations.Last();
        Assert.AreEqual(expected, options.PointerDrag);
        Assert.AreEqual(400, options.TouchDelay);
        Assert.AreEqual(8, options.DragThreshold);
        Assert.IsFalse(options.AutoScroll);
    }

    [TestMethod]
    public void NegativeSettingsReachTheScriptAsZero()
    {
        RenderDustbin(d => d.Add(x => x.TouchDelay, -5).Add(x => x.DragThreshold, -1));

        Assert.AreEqual(0, SourceRegistrations.Last().TouchDelay);
        Assert.AreEqual(0, SourceRegistrations.Last().DragThreshold);
    }

    [TestMethod]
    public void WithEveryPointerDraggingThroughPointerEventsNothingIsNativelyDraggable()
    {
        var cut = RenderDustbin(d => d.Add(x => x.PointerDrag, BdndPointerDrag.All));

        Assert.AreEqual("false", HostOf(Source(cut)).GetAttribute("draggable"));
    }

    [TestMethod]
    public void AHandleOfAPointerDraggedSourceTakesNoTouchScrolling()
    {
        var cut = RenderDustbin(d => d.Add(x => x.UseHandle, true).Add(x => x.ChildContent, _ => Fragment<BdndDragHandle>()));

        var handle = cut.Find("[data-bit-bdnd-handle]");
        Assert.AreEqual("cursor: move; touch-action: none;", handle.GetAttribute("style"));
        Assert.AreEqual("true", handle.GetAttribute("draggable"));
    }

    [TestMethod]
    public void AHandleOfASourceDraggedOnlyThroughPointerEventsIsNotNativelyDraggable()
    {
        var cut = RenderDustbin(d => d
            .Add(x => x.UseHandle, true)
            .Add(x => x.PointerDrag, BdndPointerDrag.All)
            .Add(x => x.ChildContent, _ => Fragment<BdndDragHandle>()));

        Assert.IsNull(cut.Find("[data-bit-bdnd-handle]").GetAttribute("draggable"));
    }

    [TestMethod]
    public void AHandleOfASourceDraggedOnlyNativelyScrollsAsUsual()
    {
        var cut = RenderDustbin(d => d
            .Add(x => x.UseHandle, true)
            .Add(x => x.PointerDrag, BdndPointerDrag.None)
            .Add(x => x.ChildContent, _ => Fragment<BdndDragHandle>()));

        Assert.AreEqual("cursor: move;", cut.Find("[data-bit-bdnd-handle]").GetAttribute("style"));
    }

    [TestMethod]
    public void ATargetTellsTheScriptWhetherItHasAnOnHover()
    {
        RenderProvider(p => p
            .AddChildContent<BdndDropTarget<string>>(t => t.Add(x => x.Accept, "box").Add(x => x.OnHover, _ => { }))
            .AddChildContent<BdndDropTarget<string>>(t => t.Add(x => x.Accept, "box")));

        Assert.AreSequenceEqual(new[] { true, false }, TargetRegistrations.Select(o => o.Hover).ToArray());
    }
}
