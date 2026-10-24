using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bdnd.Tests.Components;

/// <summary>
/// Exceptions thrown by application callbacks the module's JS-invoked methods run (<c>OnEnd</c>, <c>OnNativeDrop</c>,
/// the keyboard's <c>OnDrop</c>) reach the renderer - an error boundary or the app's error UI - as a managed event
/// handler's would, instead of being lost with the module's promise; and the drag still ends.
/// </summary>
[TestClass]
public class BdndCallbackFailureTests : BdndComponentTestBase
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    private IRenderedComponent<BdndProvider> RenderDustbin(
        Action<ComponentParameterCollectionBuilder<BdndDraggable<string>>>? source = null,
        Action<ComponentParameterCollectionBuilder<BdndDropTarget<string>>>? bin = null)
        => RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d =>
            {
                d.Add(x => x.Type, "box");
                d.Add(x => x.Item, "crate");
                source?.Invoke(d);
            })
            .AddChildContent<BdndDropTarget<string>>(t =>
            {
                t.Add(x => x.AcceptTypes, ["box", BdndNativeTypes.File]);
                bin?.Invoke(t);
            }));

    [TestMethod]
    public async Task AnOnEndThatThrowsReachesTheRendererAndTheDragEnds()
    {
        var cut = RenderDustbin(source: d => d.Add(x => x.OnEnd, _ => throw new InvalidOperationException("end failed")));
        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());

        await EndDragAsync(cut.FindComponent<BdndDraggable<string>>());

        var error = await Renderer.UnhandledException.WaitAsync(WaitLimit);
        Assert.AreEqual("end failed", error.Message);
        Assert.IsFalse(Manager.IsDragging());
    }

    [TestMethod]
    public async Task AnOnEndThatThrowsAfterADropRemovedTheSourceReachesTheRendererThroughTheProvider()
    {
        var show = true;
        var cut = RenderProvider(p => p.Add(x => x.ChildContent, (RenderFragment)(b =>
        {
            if (show)
            {
                b.OpenComponent<BdndDraggable<string>>(0);
                b.AddComponentParameter(1, nameof(BdndDraggable<string>.Type), "box");
                b.AddComponentParameter(2, nameof(BdndDraggable<string>.Item), "crate");
                b.AddComponentParameter(3, nameof(BdndDraggable<string>.OnEnd),
                    EventCallback.Factory.Create<BdndDragEndContext<string>>(this, () => throw new InvalidOperationException("end failed")));
                b.CloseComponent();
            }
        })));
        var source = cut.FindComponent<BdndDraggable<string>>();
        var instance = source.Instance;
        await StartDragAsync(source);
        show = false;
        cut.Render();

        await cut.InvokeAsync(() => instance.HandleDragEnd());

        var error = await Renderer.UnhandledException.WaitAsync(WaitLimit);
        Assert.AreEqual("end failed", error.Message);
        Assert.IsFalse(Manager.IsDragging());
    }

    [TestMethod]
    public async Task AnAsyncOnEndThatFailsReachesTheRenderer()
    {
        var gate = new TaskCompletionSource();
        var cut = RenderDustbin(source: d => d.Add(x => x.OnEnd, async _ =>
        {
            await gate.Task;
            throw new InvalidOperationException("late end failure");
        }));
        await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());

        var end = EndDragAsync(cut.FindComponent<BdndDraggable<string>>());
        Assert.IsFalse(Manager.IsDragging(), "the drag ends right after OnEnd's synchronous part");
        gate.SetResult();
        await end.WaitAsync(WaitLimit);

        var error = await Renderer.UnhandledException.WaitAsync(WaitLimit);
        Assert.AreEqual("late end failure", error.Message);
    }

    [TestMethod]
    public async Task AnOnBeginThatThrowsStillLetsTheDragEnd()
    {
        var ended = false;
        var cut = RenderDustbin(source: d => d
            .Add(x => x.OnBegin, _ => throw new InvalidOperationException("begin failed"))
            .Add(x => x.OnEnd, _ => ended = true));
        try
        {
            await StartDragAsync(cut.FindComponent<BdndDraggable<string>>());
        }
        catch (InvalidOperationException)
        {
            // the managed dragstart handler's own failure
        }

        await EndDragAsync(cut.FindComponent<BdndDraggable<string>>());

        Assert.IsTrue(ended);
        Assert.IsFalse(Manager.IsDragging());
    }

    [TestMethod]
    public async Task AnOnBeginThatThrowsWhenTheEndPublishesTheDragStillEndsIt()
    {
        // In Blazor Server the end can arrive before the begin was published (dragstart, then dragend at once): the
        // end publishes it, runs OnBegin - which throws - and must still run OnEnd and end the drag.
        var log = new List<string>();
        var cut = RenderDustbin(source: d => d
            .Add(x => x.OnBegin, _ => throw new InvalidOperationException("begin failed"))
            .Add(x => x.OnEnd, e => log.Add($"end:{e.DidDrop}")));
        var source = cut.FindComponent<BdndDraggable<string>>();
        Manager.BeginDrag(SourceIdOf(source.Instance), "crate", "box", default, default,
            () => throw new InvalidOperationException("begin failed"), source: (IBdndDragSourceHandler)source.Instance);

        await EndDragAsync(source);

        var error = await Renderer.UnhandledException.WaitAsync(WaitLimit);
        Assert.AreEqual("begin failed", error.Message);
        Assert.AreSequenceEqual(new[] { "end:False" }, log);
        Assert.IsFalse(Manager.IsDragging());
    }

    [TestMethod]
    public async Task AnOnNativeDropThatThrowsReachesTheRenderer()
    {
        var cut = RenderDustbin(bin: t => t.Add(x => x.OnNativeDrop, _ => throw new InvalidOperationException("upload failed")));
        var bin = cut.FindComponent<BdndDropTarget<string>>();

        await bin.InvokeAsync(() => bin.Instance.HandleNativeDrop(new BdndNativeDropPayload { Types = ["Files"] }));

        var error = await Renderer.UnhandledException.WaitAsync(WaitLimit);
        Assert.AreEqual("upload failed", error.Message);
    }

    [TestMethod]
    public async Task AKeyboardDropWhoseOnDropThrowsReachesTheRendererAndEndsTheDrag()
    {
        var log = new List<string>();
        var cut = RenderDustbin(
            source: d => d.Add(x => x.OnEnd, e => log.Add($"end:{e.DidDrop}")),
            bin: t => t.Add(x => x.OnDrop, _ => throw new InvalidOperationException("drop failed")));
        var source = cut.FindComponent<BdndDraggable<string>>();
        var bin = cut.FindComponent<BdndDropTarget<string>>();
        var binId = ((BdndDropTargetOptions)TargetRegistrationCall(bin.Instance).Arguments[2]!).Id;
        Sources.Setup<BdndKeyboardTarget?>("keyboardNavigate", i => (string)i.Arguments[4]! == "next")
            .SetResult(new BdndKeyboardTarget { Id = binId, Rect = new BdndDomRect(), Position = 1, Count = 1, Moved = true });
        await source.InvokeAsync(() => source.Instance.HandleKeyboardDragStart(new BdndDomRect(), "crate"));
        await source.InvokeAsync(() => source.Instance.HandleKeyboardCommand("next"));

        Assert.IsFalse(await source.InvokeAsync(() => source.Instance.HandleKeyboardCommand("drop")));

        var error = await Renderer.UnhandledException.WaitAsync(WaitLimit);
        Assert.AreEqual("drop failed", error.Message);
        Assert.AreSequenceEqual(new[] { "end:False" }, log);
        Assert.IsFalse(Manager.IsDragging());
    }
}
