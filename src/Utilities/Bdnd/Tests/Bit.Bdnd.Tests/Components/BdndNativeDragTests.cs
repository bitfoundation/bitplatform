using Bunit;

namespace Bit.Bdnd.Tests.Components;

/// <summary>
/// Drags that start outside the application - files, text, links, HTML. The manager knows nothing about them: the
/// script reports their hover and their drop, and only targets that list a matching native type react.
/// </summary>
[TestClass]
public class BdndNativeDragTests : BdndComponentTestBase
{
    private IRenderedComponent<BdndDropTarget<object>> RenderTarget(StateLog<BdndDropState<object>> log,
        List<BdndNativeDropEventArgs>? drops = null, params string[] accept)
        => Render<BdndDropTarget<object>>(p =>
        {
            p.AddCascadingValue(Manager);
            p.Add(x => x.AcceptTypes, accept);
            p.Add(x => x.ChildContent, log.Content());
            if (drops is not null) p.Add(x => x.OnNativeDrop, drops.Add);
        });

    // ---- hover ----

    [TestMethod]
    public async Task ANativeDragOfAnAcceptedTypeReportsTheTargetAsHovered()
    {
        var log = new StateLog<BdndDropState<object>>();
        var cut = RenderTarget(log, accept: [BdndNativeTypes.File]);

        await HoverAsync(cut, true, "Files");

        Assert.IsTrue(log.Last.IsOver);
        Assert.IsTrue(log.Last.IsOverShallow);
        Assert.IsTrue(log.Last.CanDrop);
        Assert.AreEqual(BdndNativeTypes.File, log.Last.ItemType);
        Assert.IsNull(log.Last.Item);
        Assert.IsFalse(Manager.IsDragging(), "the manager only tracks in-app drags");
    }

    [TestMethod]
    public async Task LeavingClearsTheNativeHover()
    {
        var log = new StateLog<BdndDropState<object>>();
        var cut = RenderTarget(log, accept: [BdndNativeTypes.File]);
        await HoverAsync(cut, true, "Files");

        await HoverAsync(cut, false, "Files");

        Assert.IsFalse(log.Last.IsOver);
        Assert.IsFalse(log.Last.IsOverShallow);
        Assert.IsFalse(log.Last.CanDrop);
        Assert.IsNull(log.Last.ItemType);
    }

    [TestMethod]
    public async Task OnlyTheInnermostHoveredTargetAcceptingANativeDragIsOverShallow()
    {
        var outerLog = new StateLog<BdndDropState<object>>();
        var innerLog = new StateLog<BdndDropState<object>>();
        var inner = Fragment<BdndDropTarget<object>>(
            ("AcceptTypes", new[] { BdndNativeTypes.File }), ("ChildContent", innerLog.Content()));
        var cut = Render<BdndProvider>(p => p
            .Add(x => x.Manager, Manager)
            .AddChildContent<BdndDropTarget<object>>(t => t
                .Add(x => x.AcceptTypes, [BdndNativeTypes.File])
                .Add(x => x.ChildContent, outerLog.Content(_ => inner))));
        var targets = cut.FindComponents<BdndDropTarget<object>>();

        await HoverAsync(targets[0], true, "Files");
        Assert.IsTrue(outerLog.Last.IsOverShallow);

        await HoverAsync(targets[1], true, "Files");
        Assert.IsTrue(outerLog.Last.IsOver);
        Assert.IsFalse(outerLog.Last.IsOverShallow, "the nested target is the one under the pointer");
        Assert.IsTrue(innerLog.Last.IsOver);
        Assert.IsTrue(innerLog.Last.IsOverShallow);

        await HoverAsync(targets[1], false, "Files");
        Assert.IsTrue(outerLog.Last.IsOverShallow);
        Assert.IsFalse(innerLog.Last.IsOver);
    }

    [TestMethod]
    public async Task ANestedTargetThatDoesNotAcceptTheNativeDragLeavesTheShallowHoverToItsParent()
    {
        var outerLog = new StateLog<BdndDropState<object>>();
        var inner = Fragment<BdndDropTarget<object>>(("AcceptTypes", new[] { BdndNativeTypes.Text }));
        var cut = Render<BdndProvider>(p => p
            .Add(x => x.Manager, Manager)
            .AddChildContent<BdndDropTarget<object>>(t => t
                .Add(x => x.AcceptTypes, [BdndNativeTypes.File])
                .Add(x => x.ChildContent, outerLog.Content(_ => inner))));
        var targets = cut.FindComponents<BdndDropTarget<object>>();

        await HoverAsync(targets[0], true, "Files");
        await HoverAsync(targets[1], true, "Files");

        Assert.IsTrue(outerLog.Last.IsOverShallow);
    }

    [TestMethod]
    public async Task ANativeDragOfAnotherTypeIsIgnored()
    {
        var log = new StateLog<BdndDropState<object>>();
        var cut = RenderTarget(log, accept: [BdndNativeTypes.File]);
        var renders = log.Count;

        await HoverAsync(cut, true, "text/plain", "text/html");

        Assert.IsFalse(log.Last.IsOver);
        Assert.AreEqual(renders, log.Count, "nothing changed, so nothing re-rendered");
    }

    [TestMethod]
    public async Task ADragTaggedByADraggableIsNeverNative()
    {
        // e.g. a draggable of another provider dragging a file-like payload over this target
        var log = new StateLog<BdndDropState<object>>();
        var cut = RenderTarget(log, accept: [BdndNativeTypes.File]);

        await HoverAsync(cut, true, "Files", SourceType("elsewhere"));

        Assert.IsFalse(log.Last.IsOver);
        Assert.IsNull(log.Last.ItemType);
    }

    [TestMethod]
    public async Task TheFirstAcceptedTypeInTheTargetsOrderDecidesTheItemType()
    {
        var log = new StateLog<BdndDropState<object>>();
        var cut = RenderTarget(log, accept: [BdndNativeTypes.Html, BdndNativeTypes.Text, BdndNativeTypes.Url]);

        await HoverAsync(cut, true, "text/plain", "text/uri-list", "text/html");

        Assert.AreEqual(BdndNativeTypes.Html, log.Last.ItemType);
    }

    [TestMethod]
    public async Task AnInAppDragReplacesAStaleNativeHover()
    {
        var log = new StateLog<BdndDropState<object>>();
        var cut = RenderTarget(log, accept: [BdndNativeTypes.Text, "box"]);
        await HoverAsync(cut, true, "text/plain");

        await cut.InvokeAsync(() =>
        {
            Manager.BeginDrag("src", "crate", "card", new(0, 0), new(0, 0));
            return Manager.FlushBeginAsync();
        });
        await HoverAsync(cut, true, SourceType("src"));

        Assert.IsFalse(log.Last.IsOver);
        Assert.AreEqual("card", log.Last.ItemType);
        Assert.IsFalse(log.Last.CanDrop);
    }

    // ---- drop ----

    [TestMethod]
    public async Task ANativeFileDropReachesOnNativeDropWithTheFilesAndTheData()
    {
        var log = new StateLog<BdndDropState<object>>();
        var drops = new List<BdndNativeDropEventArgs>();
        var cut = RenderTarget(log, drops, BdndNativeTypes.File);
        await HoverAsync(cut, true, "Files");

        await cut.InvokeAsync(() => cut.Instance.HandleNativeDrop(new BdndNativeDropPayload
        {
            Types = ["Files", "text/plain"],
            Data = new() { ["text/plain"] = "report.pdf" },
            Files = [new BdndNativeFile { Name = "report.pdf", Size = 2048, Type = "application/pdf" }],
        }));

        var drop = Assert.ContainsSingle(drops);
        Assert.AreEqual(BdndNativeTypes.File, drop.ItemType);
        var file = Assert.ContainsSingle(drop.Files);
        Assert.AreEqual("report.pdf", file.Name);
        Assert.AreEqual(2048, file.Size);
        Assert.AreEqual("application/pdf", file.Type);
        Assert.AreEqual("report.pdf", drop.Text);
        Assert.IsFalse(log.Last.IsOver, "no dragleave follows a drop, so the drop clears the hover");
    }

    [TestMethod]
    public async Task ANativeTextDropCarriesEveryStringOfTheDrag()
    {
        var drops = new List<BdndNativeDropEventArgs>();
        var cut = RenderTarget(new(), drops, BdndNativeTypes.Html, BdndNativeTypes.Url);

        await cut.InvokeAsync(() => cut.Instance.HandleNativeDrop(new BdndNativeDropPayload
        {
            Types = ["text/plain", "text/uri-list", "text/html"],
            Data = new()
            {
                ["text/plain"] = "bit platform",
                ["text/uri-list"] = "https://bitplatform.dev",
                ["text/html"] = "<a href=\"https://bitplatform.dev\">bit platform</a>",
            },
        }));

        var drop = Assert.ContainsSingle(drops);
        Assert.AreEqual(BdndNativeTypes.Html, drop.ItemType);
        Assert.AreEqual("bit platform", drop.Text);
        Assert.AreEqual("<a href=\"https://bitplatform.dev\">bit platform</a>", drop.Html);
        Assert.AreSequenceEqual(new[] { "https://bitplatform.dev" }, drop.Urls);
        Assert.IsEmpty(drop.Files);
    }

    [TestMethod]
    public async Task MissingPayloadCollectionsArriveEmpty()
    {
        var drops = new List<BdndNativeDropEventArgs>();
        var cut = RenderTarget(new(), drops, BdndNativeTypes.Text);

        await cut.InvokeAsync(() => cut.Instance.HandleNativeDrop(new BdndNativeDropPayload
        {
            Types = ["text/plain"],
            Data = null!,
            Files = null!,
        }));

        var drop = Assert.ContainsSingle(drops);
        Assert.IsEmpty(drop.Files);
        Assert.IsEmpty(drop.Data);
    }

    [TestMethod]
    public async Task ANativeDropOfAnotherTypeIsIgnored()
    {
        var drops = new List<BdndNativeDropEventArgs>();
        var cut = RenderTarget(new(), drops, BdndNativeTypes.File);

        await cut.InvokeAsync(() => cut.Instance.HandleNativeDrop(new BdndNativeDropPayload { Types = ["text/plain"] }));

        Assert.IsEmpty(drops);
    }

    [TestMethod]
    public async Task ADropTaggedByADraggableIsNotANativeDrop()
    {
        var drops = new List<BdndNativeDropEventArgs>();
        var cut = RenderTarget(new(), drops, BdndNativeTypes.Text);

        await cut.InvokeAsync(() => cut.Instance.HandleNativeDrop(new BdndNativeDropPayload
        {
            Types = ["text/plain", SourceType("elsewhere")],
        }));

        Assert.IsEmpty(drops);
    }

    [TestMethod]
    public async Task NoNativeDropIsDeliveredWhileAnInAppDragRuns()
    {
        var drops = new List<BdndNativeDropEventArgs>();
        var cut = RenderTarget(new(), drops, BdndNativeTypes.Text);
        await cut.InvokeAsync(() =>
        {
            Manager.BeginDrag("src", "crate", "box", new(0, 0), new(0, 0));
            return Manager.FlushBeginAsync();
        });

        await cut.InvokeAsync(() => cut.Instance.HandleNativeDrop(new BdndNativeDropPayload { Types = ["text/plain"] }));

        Assert.IsEmpty(drops);
    }

    [TestMethod]
    public async Task ANativeDropNeverReachesOnDrop()
    {
        var drops = 0;
        var cut = Render<BdndDropTarget<object>>(p => p
            .AddCascadingValue(Manager)
            .Add(x => x.Accept, BdndNativeTypes.File)
            .Add(x => x.OnDrop, _ => drops++));

        // the managed drop event of a native drop: the script handles it, the managed handler stays out
        await HostOf(cut).DropAsync(PointerAt(0, 0, "Files"));
        await cut.InvokeAsync(() => cut.Instance.HandleNativeDrop(new BdndNativeDropPayload { Types = ["Files"] }));

        Assert.AreEqual(0, drops);
    }
}
