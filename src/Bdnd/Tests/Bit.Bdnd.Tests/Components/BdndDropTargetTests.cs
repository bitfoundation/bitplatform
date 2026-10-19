using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Bdnd.Tests.Components;

/// <summary>What a drop target renders, what it tells the script, and how it reacts to in-app drags it did not start.</summary>
[TestClass]
public class BdndDropTargetTests : BdndComponentTestBase
{
    private IRenderedComponent<BdndDropTarget<string>> RenderTarget(
        Action<ComponentParameterCollectionBuilder<BdndDropTarget<string>>>? parameters = null,
        StateLog<BdndDropState<string>>? log = null, string accept = "box")
        => Render<BdndDropTarget<string>>(p =>
        {
            p.AddCascadingValue(Manager);
            p.Add(x => x.Accept, accept);
            if (log is not null) p.Add(x => x.ChildContent, log.Content());
            parameters?.Invoke(p);
        });

    /// <summary>A published drag of <paramref name="itemType"/>, as a draggable of the same provider would start it.</summary>
    private Task BeginDragAsync(IRenderedComponent<IComponent> cut, string itemType = "box", object? item = null)
        => cut.InvokeAsync(() =>
        {
            Manager.BeginDrag("src", item ?? "crate", itemType, new(10, 10), new(0, 0));
            return Manager.FlushBeginAsync();
        });

    // ---- markup ----

    [TestMethod]
    public void TheContentReceivesAnIdleStateWhenNothingIsDragged()
    {
        var log = new StateLog<BdndDropState<string>>();

        RenderTarget(log: log);

        Assert.IsFalse(log.Last.IsOver);
        Assert.IsFalse(log.Last.IsOverShallow);
        Assert.IsFalse(log.Last.CanDrop);
        Assert.IsNull(log.Last.ItemType);
        Assert.IsNull(log.Last.Item);
        Assert.AreSame(Manager, log.Last.Monitor);
    }

    [TestMethod]
    public void TagAndOtherAttributesGoOntoTheHostElement()
    {
        var cut = RenderTarget(p => p
            .Add(x => x.Tag, "section")
            .AddUnmatched("id", "bin")
            .AddUnmatched("class", "dustbin")
            .Add(x => x.ChildContent, _ => "<h4>Drop here</h4>"));

        var host = cut.Find("section");
        Assert.AreEqual("bin", host.Id);
        Assert.AreEqual("dustbin", host.ClassName);
        Assert.AreEqual("Drop here", host.QuerySelector("h4")!.TextContent);
    }

    [TestMethod]
    public void TheDragOverListenerIsOnlyAttachedForAnOnHoverHandler()
    {
        // dragover fires continuously; on Blazor Server every one would be a circuit message
        var withoutHover = RenderTarget();
        var withHover = RenderTarget(p => p.Add(x => x.OnHover, _ => { }));

        Assert.IsFalse(HostOf(withoutHover).HasAttribute("blazor:ondragover"));
        Assert.IsTrue(HostOf(withHover).HasAttribute("blazor:ondragover"));
        Assert.IsTrue(HostOf(withoutHover).HasAttribute("blazor:ondrop"));
    }

    // ---- registration with the script ----

    [TestMethod]
    public void TheHostElementIsRegisteredAsADropTarget()
    {
        var cut = RenderTarget();

        var call = TargetRegistrationCall(cut.Instance);
        Assert.IsInstanceOfType<ElementReference>(call.Arguments[0]);
        Assert.AreSame(cut.Instance, ((DotNetObjectReference<BdndDropTarget<string>>)call.Arguments[1]!).Value);
        var options = (BdndDropTargetOptions)call.Arguments[2]!;
        Assert.IsFalse(options.CanDrop);
        Assert.IsEmpty(options.NativeTypes);
    }

    [TestMethod]
    public void AcceptedNativeTypesAreSentAsDataTransferTypesInTheirOrder()
    {
        RenderTarget(p => p.Add(x => x.AcceptTypes, [BdndNativeTypes.File, "box", BdndNativeTypes.Url]), accept: BdndNativeTypes.Text);

        var options = Assert.ContainsSingle(TargetRegistrations);
        Assert.AreSequenceEqual(new[] { "text/plain", "Files", "text/uri-list" }, options.NativeTypes);
    }

    [TestMethod]
    public void DuplicateAndEmptyAcceptedTypesAreIgnored()
    {
        RenderTarget(p => p.Add(x => x.AcceptTypes, ["", BdndNativeTypes.Html, null!, BdndNativeTypes.Html]), accept: BdndNativeTypes.Html);

        Assert.AreSequenceEqual(new[] { "text/html" }, Assert.ContainsSingle(TargetRegistrations).NativeTypes);
    }

    [TestMethod]
    public void AnUnchangedAcceptListIsNotPushedAgain()
    {
        var cut = RenderTarget(p => p.Add(x => x.AcceptTypes, [BdndNativeTypes.File]));

        // a new array with the same types, as a razor collection expression produces on every render
        cut.Render(p => p.Add(x => x.AcceptTypes, [BdndNativeTypes.File]));
        cut.Render();

        Assert.IsEmpty(TargetUpdates);
    }

    [TestMethod]
    public void AChangedAcceptListIsPushedToTheExistingRegistration()
    {
        var cut = RenderTarget(p => p.Add(x => x.AcceptTypes, [BdndNativeTypes.File]));

        cut.Render(p => p.Add(x => x.AcceptTypes, [BdndNativeTypes.File, BdndNativeTypes.Text]));

        Assert.ContainsSingle(TargetRegistrations);
        var update = Assert.ContainsSingle(TargetUpdates);
        Assert.AreSequenceEqual(new[] { "Files", "text/plain" }, update.NativeTypes);
    }

    [TestMethod]
    public async Task CanDropIsPushedToTheScriptWhenACompatibleDragBeginsAndEnds()
    {
        // the script decides the drop effect (and whether drop fires at all) from it
        var cut = RenderTarget();

        await BeginDragAsync(cut);
        await cut.InvokeAsync(Manager.EndDrag);

        Assert.AreSequenceEqual(new[] { true, false }, TargetUpdates.Select(u => u.CanDrop));
    }

    [TestMethod]
    public async Task AnIncompatibleDragIsNotPushed()
    {
        var cut = RenderTarget();

        await BeginDragAsync(cut, itemType: "card");

        Assert.IsEmpty(TargetUpdates);
    }

    // ---- in-app drags ----

    [TestMethod]
    public async Task CanDropIsTrueForEveryCompatibleDragHoveredOrNot()
    {
        var log = new StateLog<BdndDropState<string>>();
        var cut = RenderTarget(log: log);

        await BeginDragAsync(cut, item: "crate");

        Assert.IsTrue(log.Last.CanDrop);
        Assert.IsFalse(log.Last.IsOver);
        Assert.AreEqual("box", log.Last.ItemType);
        Assert.AreEqual("crate", log.Last.Item);
    }

    [TestMethod]
    public async Task AcceptAndAcceptTypesCombine()
    {
        var log = new StateLog<BdndDropState<string>>();
        var cut = RenderTarget(p => p.Add(x => x.AcceptTypes, ["card"]), log);

        await BeginDragAsync(cut, itemType: "card");
        Assert.IsTrue(log.Last.CanDrop);

        await BeginDragAsync(cut, itemType: "box");
        Assert.IsTrue(log.Last.CanDrop);

        await BeginDragAsync(cut, itemType: "knight");
        Assert.IsFalse(log.Last.CanDrop);
    }

    [TestMethod]
    public async Task CanDropPredicateNarrowsTheTypeCheck()
    {
        var log = new StateLog<BdndDropState<string>>();
        string? seenItem = null;
        IBdndMonitor? seenMonitor = null;
        var cut = RenderTarget(p => p.Add(x => x.CanDropPredicate, (item, monitor) =>
        {
            seenItem = item;
            seenMonitor = monitor;
            return item == "crate";
        }), log);

        await BeginDragAsync(cut, item: "crate");
        Assert.IsTrue(log.Last.CanDrop);
        Assert.AreEqual("crate", seenItem);
        Assert.AreSame(Manager, seenMonitor);

        await BeginDragAsync(cut, item: "barrel");
        Assert.IsFalse(log.Last.CanDrop);
    }

    [TestMethod]
    public async Task TheItemsDotNetTypeDoesNotTakePart()
    {
        // the target expects an int, the drag carries a string of an accepted type
        var log = new StateLog<BdndDropState<int>>();
        var cut = Render<BdndDropTarget<int>>(p => p
            .AddCascadingValue(Manager)
            .Add(x => x.Accept, "box")
            .Add(x => x.ChildContent, log.Content()));

        await BeginDragAsync(cut, item: "crate");

        Assert.IsTrue(log.Last.CanDrop);
        Assert.AreEqual(0, log.Last.Item);
    }

    [TestMethod]
    public async Task HoverIsReportedOnlyForAnAcceptedType()
    {
        var log = new StateLog<BdndDropState<string>>();
        var cut = RenderTarget(log: log);

        await BeginDragAsync(cut, itemType: "card");
        await HoverAsync(cut);

        Assert.IsFalse(log.Last.IsOver);
        Assert.IsFalse(log.Last.IsOverShallow);
    }

    [TestMethod]
    public async Task HoverEntersAndLeaves()
    {
        var log = new StateLog<BdndDropState<string>>();
        var cut = RenderTarget(log: log);
        await BeginDragAsync(cut);

        await HoverAsync(cut);
        Assert.IsTrue(log.Last.IsOver);
        Assert.IsTrue(log.Last.IsOverShallow);

        await HoverAsync(cut, isOver: false);
        Assert.IsFalse(log.Last.IsOver);
        Assert.IsFalse(log.Last.IsOverShallow);
    }

    [TestMethod]
    public async Task OnHoverRunsForAnAcceptedTypeWithThePointerRecorded()
    {
        var hovers = new List<BdndDropEventArgs<string>>();
        var cut = RenderTarget(p => p.Add(x => x.OnHover, hovers.Add));
        await BeginDragAsync(cut, item: "crate");

        await HostOf(cut).DragOverAsync(PointerAt(40, 25));

        var hover = Assert.ContainsSingle(hovers);
        Assert.AreEqual("crate", hover.Item);
        Assert.AreSame(Manager, hover.Monitor);
        Assert.AreEqual(new BdndXYCoord(40, 25), Manager.GetClientOffset());
    }

    [TestMethod]
    public async Task OnHoverDoesNotRunForAnotherTypeOrWithoutADrag()
    {
        var hovers = 0;
        var cut = RenderTarget(p => p.Add(x => x.OnHover, _ => hovers++));

        await HostOf(cut).DragOverAsync(PointerAt(1, 1));
        await BeginDragAsync(cut, itemType: "card");
        await HostOf(cut).DragOverAsync(PointerAt(2, 2));

        Assert.AreEqual(0, hovers);
    }

    [TestMethod]
    public async Task DragOverDoesNotReRenderTheTarget()
    {
        // the manager decides when something changed, not every dragover
        var log = new StateLog<BdndDropState<string>>();
        var cut = RenderTarget(p => p.Add(x => x.OnHover, _ => { }), log);
        await BeginDragAsync(cut);
        var renders = log.Count;

        await HostOf(cut).DragOverAsync(PointerAt(5, 5));
        await HostOf(cut).DragOverAsync(PointerAt(6, 6));

        Assert.AreEqual(renders, log.Count);
    }

    [TestMethod]
    public async Task ADropWithoutAnInAppDragIsLeftToTheScript()
    {
        var drops = 0;
        var cut = RenderTarget(p => p.Add(x => x.OnDrop, _ => drops++));

        await DropAsync(cut);

        Assert.AreEqual(0, drops);
        Assert.IsFalse(Manager.DidDrop());
    }

    [TestMethod]
    public async Task ADropOfAnIncompatibleDragIsIgnored()
    {
        var drops = 0;
        var cut = RenderTarget(p => p
            .Add(x => x.CanDropPredicate, (_, _) => false)
            .Add(x => x.OnDrop, _ => drops++));
        await BeginDragAsync(cut);

        await DropAsync(cut);

        Assert.AreEqual(0, drops);
        Assert.IsFalse(Manager.DidDrop());
    }

    [TestMethod]
    public async Task ADropRecordsTheFinalPointerBeforeOnDropRuns()
    {
        BdndXYCoord? moved = null;
        var cut = RenderTarget(p => p.Add(x => x.OnDrop, e => moved = e.Monitor.GetDifferenceFromInitialOffset()));
        await BeginDragAsync(cut);

        await DropAsync(cut, 70, 35);

        Assert.AreEqual(new BdndXYCoord(60, 25), moved);
    }

    // ---- disposal ----

    [TestMethod]
    public async Task RemovingAHoveredTargetHandsTheShallowHoverBackToItsParent()
    {
        var outerLog = new StateLog<BdndDropState<string>>();
        var showInner = true;
        var inner = Fragment<BdndDropTarget<string>>(("Accept", "box"));
        var cut = RenderProvider(p => p.AddChildContent<BdndDropTarget<string>>(t => t
            .Add(x => x.Accept, "box")
            .Add(x => x.ChildContent, outerLog.Content(_ => showInner ? inner : null))));
        var targets = cut.FindComponents<BdndDropTarget<string>>();
        await BeginDragAsync(cut);
        await HoverAsync(targets[0]);
        await HoverAsync(targets[1]);
        Assert.IsFalse(outerLog.Last.IsOverShallow);

        showInner = false;
        cut.Render();

        Assert.ContainsSingle(cut.FindComponents<BdndDropTarget<string>>());
        Assert.IsTrue(outerLog.Last.IsOver);
        Assert.IsTrue(outerLog.Last.IsOverShallow);
    }

    [TestMethod]
    public void RemovingATargetReleasesItsRegistrationAndCallbackObject()
    {
        var cut = Render<BdndProvider>(p => p.AddChildContent<BdndDropTarget<string>>(t => t.Add(x => x.Accept, "box")));
        var dotNetRef = (DotNetObjectReference<BdndDropTarget<string>>)TargetRegistrationCall(cut.FindComponent<BdndDropTarget<string>>().Instance).Arguments[1]!;

        cut.Render(p => p.AddChildContent("<p>gone</p>"));

        Assert.ContainsSingle(Targets.Invocations["dispose"]);
        Assert.ThrowsExactly<ObjectDisposedException>(() => dotNetRef.Value);
    }

    [TestMethod]
    public async Task CallbacksArrivingAfterRemovalAreIgnored()
    {
        // the script may still report a hover or a native drop that was in flight
        var nativeDrops = 0;
        var cut = Render<BdndProvider>(p => p.AddChildContent<BdndDropTarget<string>>(t => t
            .Add(x => x.Accept, BdndNativeTypes.File)
            .Add(x => x.OnNativeDrop, _ => nativeDrops++)));
        var target = cut.FindComponent<BdndDropTarget<string>>().Instance;
        cut.Render(p => p.AddChildContent("<p>gone</p>"));

        await cut.InvokeAsync(() => target.HandleHoverChanged(true, ["Files"]));
        await cut.InvokeAsync(() => target.HandleNativeDrop(new BdndNativeDropPayload { Types = ["Files"] }));

        Assert.AreEqual(0, nativeDrops);
    }
}
