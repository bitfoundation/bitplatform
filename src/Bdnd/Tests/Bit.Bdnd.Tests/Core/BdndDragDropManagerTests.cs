namespace Bit.Bdnd.Tests.Core;

/// <summary>
/// The manager is the authoritative drag state every component reads. These tests drive it directly,
/// the way the components do, without rendering anything.
/// </summary>
[TestClass]
public class BdndDragDropManagerTests
{
    private static readonly BdndXYCoord Pointer = new(100, 50);
    private static readonly BdndXYCoord SourceCorner = new(80, 40);

    private static BdndDropTargetEntry Target(string id, string? parentId = null,
        string[]? accept = null, Func<object?, IBdndMonitor, bool>? canDrop = null)
        => new() { Id = id, ParentId = parentId, Accept = accept ?? ["box"], CanDrop = canDrop };

    private static BdndDragDropManager Manager(params BdndDropTargetEntry[] targets)
    {
        var manager = new BdndDragDropManager();
        foreach (var target in targets) manager.RegisterTarget(target);
        return manager;
    }

    private static void Begin(BdndDragDropManager manager, string itemType = "box", object? item = null,
        string sourceId = "src", Func<Task>? onBegun = null)
        => manager.BeginDrag(sourceId, item ?? "crate", itemType, Pointer, SourceCorner, onBegun);

    // ---- ids and identity ----

    [TestMethod]
    public void GenerateHandlerIdIsPrefixedAndUniqueWithinAManager()
    {
        var manager = new BdndDragDropManager();

        Assert.AreEqual("card-1", manager.GenerateHandlerId("card"));
        Assert.AreEqual("card-2", manager.GenerateHandlerId("card"));
        Assert.AreEqual("bin-3", manager.GenerateHandlerId("bin"));
    }

    [TestMethod]
    public void GenerateHandlerIdCountsPerManager()
    {
        new BdndDragDropManager().GenerateHandlerId("card");

        Assert.AreEqual("card-1", new BdndDragDropManager().GenerateHandlerId("card"));
    }

    [TestMethod]
    public void TheMonitorIsTheManagerItself()
    {
        var manager = new BdndDragDropManager();

        Assert.AreSame(manager, manager.Monitor);
    }

    [TestMethod]
    public void AnIdleManagerReportsNoDrag()
    {
        var manager = Manager(Target("bin"));
        IBdndMonitor monitor = manager.Monitor;

        Assert.IsFalse(monitor.IsDragging());
        Assert.IsFalse(monitor.IsDragging(null));
        Assert.IsFalse(monitor.IsDragging("src"));
        Assert.IsNull(monitor.GetItemType());
        Assert.IsNull(monitor.GetItem());
        Assert.IsNull(monitor.GetItem<string>());
        Assert.IsNull(monitor.GetDropResult());
        Assert.IsFalse(monitor.DidDrop());
        Assert.IsFalse(monitor.CanDrop("bin"));
        Assert.IsFalse(monitor.IsOver("bin"));
        Assert.IsNull(monitor.GetInitialClientOffset());
        Assert.IsNull(monitor.GetInitialSourceClientOffset());
        Assert.IsNull(monitor.GetClientOffset());
        Assert.IsNull(monitor.GetDifferenceFromInitialOffset());
        Assert.IsNull(monitor.GetSourceClientOffset());
    }

    // ---- begin and publish ----

    [TestMethod]
    public async Task BeginDragSetsTheStateWithoutNotifyingUntilFlushed()
    {
        var manager = Manager();
        var changes = 0;
        manager.StateChanged += () => changes++;

        Begin(manager);

        Assert.IsTrue(manager.IsDragging());
        Assert.AreEqual(0, changes, "a re-render during dragstart can abort the native drag");

        await manager.FlushBeginAsync();

        Assert.AreEqual(1, changes);
    }

    [TestMethod]
    public async Task FlushPublishesTheDragAndRunsTheBeginCallbackOnce()
    {
        var manager = Manager();
        var log = new List<string>();
        manager.StateChanged += () => log.Add("state");
        Begin(manager, onBegun: () =>
        {
            log.Add("begun");
            return Task.CompletedTask;
        });

        await manager.FlushBeginAsync();
        await manager.FlushBeginAsync();

        Assert.AreSequenceEqual(new[] { "state", "begun" }, log);
    }

    [TestMethod]
    public async Task FlushWithoutAPendingBeginDoesNothing()
    {
        var manager = Manager();
        var changes = 0;
        manager.StateChanged += () => changes++;

        await manager.FlushBeginAsync();

        Assert.AreEqual(0, changes);
    }

    [TestMethod]
    public async Task FlushWithoutABeginCallbackStillPublishes()
    {
        var manager = Manager();
        var changes = 0;
        manager.StateChanged += () => changes++;
        Begin(manager);

        await manager.FlushBeginAsync();

        Assert.AreEqual(1, changes);
    }

    [TestMethod]
    public async Task FlushCompletesWithTheBeginCallback()
    {
        var manager = Manager();
        var gate = new TaskCompletionSource();
        Begin(manager, onBegun: () => gate.Task);

        var flush = manager.FlushBeginAsync();

        Assert.IsFalse(flush.IsCompleted);
        gate.SetResult();
        await flush;
    }

    [TestMethod]
    public void BeginDragExposesTheItemAndItsOffsets()
    {
        var manager = Manager();

        manager.BeginDrag("src", "crate", "box", Pointer, SourceCorner);

        Assert.AreEqual("box", manager.GetItemType());
        Assert.AreEqual("crate", manager.GetItem());
        Assert.AreEqual("crate", manager.GetItem<string>());
        Assert.AreEqual(Pointer, manager.GetInitialClientOffset());
        Assert.AreEqual(SourceCorner, manager.GetInitialSourceClientOffset());
        Assert.AreEqual(Pointer, manager.GetClientOffset());
        Assert.AreEqual(new BdndXYCoord(0, 0), manager.GetDifferenceFromInitialOffset());
        Assert.AreEqual(SourceCorner, manager.GetSourceClientOffset());
    }

    [TestMethod]
    public void EveryBeginStartsANewDrag()
    {
        var manager = Manager();
        Begin(manager);
        var first = manager.DragId;

        Begin(manager);

        Assert.AreNotEqual(first, manager.DragId);
    }

    [TestMethod]
    public void IsDraggingBySourceMatchesOnlyTheDraggedSource()
    {
        var manager = Manager();
        Begin(manager, sourceId: "src");

        Assert.IsTrue(manager.IsDragging("src"));
        Assert.IsFalse(manager.IsDragging("other"));
        Assert.IsTrue(manager.IsDragging(null), "null means any source");
    }

    [TestMethod]
    public void TypedAccessorsAnswerDefaultForAnotherType()
    {
        var manager = Manager(Target("bin"));
        Begin(manager, item: "crate");
        manager.RecordDrop(manager.DragId, 42);

        Assert.AreEqual(0, manager.GetItem<int>());
        Assert.AreEqual(42, manager.GetDropResult<int>());
        Assert.IsNull(manager.GetDropResult<string>());
    }

    [TestMethod]
    public void ANewDragForgetsTheDropAndHoverOfAnUnfinishedOne()
    {
        var manager = Manager(Target("bin"));
        Begin(manager);
        manager.SetTargetOver("bin", true);
        manager.RecordDrop(manager.DragId, "result");

        Begin(manager);

        Assert.IsFalse(manager.DidDrop());
        Assert.IsNull(manager.GetDropResult());
        Assert.IsFalse(manager.IsOver("bin"));
    }

    // ---- can drop ----

    [TestMethod]
    public void CanDropRequiresTheItemTypeToBeAccepted()
    {
        var manager = Manager(Target("bin", accept: ["box", "card"]), Target("other", accept: ["card"]));
        Begin(manager, itemType: "box");

        Assert.IsTrue(manager.CanDrop("bin"));
        Assert.IsFalse(manager.CanDrop("other"));
    }

    [TestMethod]
    public void CanDropIsFalseForAnUnknownTarget()
    {
        var manager = Manager();
        Begin(manager);

        Assert.IsFalse(manager.CanDrop("ghost"));
    }

    [TestMethod]
    public void CanDropAsksThePredicateWithTheItemAndTheMonitor()
    {
        object? seenItem = null;
        IBdndMonitor? seenMonitor = null;
        var allow = false;
        var manager = Manager(Target("bin", canDrop: (item, monitor) =>
        {
            seenItem = item;
            seenMonitor = monitor;
            return allow;
        }));
        Begin(manager, item: "crate");

        Assert.IsFalse(manager.CanDrop("bin"));
        allow = true;
        Assert.IsTrue(manager.CanDrop("bin"));
        Assert.AreEqual("crate", seenItem);
        Assert.AreSame(manager, seenMonitor);
    }

    [TestMethod]
    public void ThePredicateIsNotAskedForAnUnacceptedTypeOrWhileIdle()
    {
        var asked = 0;
        var manager = Manager(Target("bin", canDrop: (_, _) => ++asked > 0));

        Assert.IsFalse(manager.CanDrop("bin"));
        Begin(manager, itemType: "card");
        Assert.IsFalse(manager.CanDrop("bin"));

        Assert.AreEqual(0, asked);
    }

    // ---- hover ----

    [TestMethod]
    public void SetTargetOverNotifiesOnlyWhenTheHoverChanges()
    {
        var manager = Manager(Target("bin"));
        Begin(manager);
        var changes = 0;
        manager.StateChanged += () => changes++;

        manager.SetTargetOver("bin", true);
        manager.SetTargetOver("bin", true);
        Assert.AreEqual(1, changes);
        Assert.IsTrue(manager.IsOver("bin"));

        manager.SetTargetOver("bin", false);
        Assert.AreEqual(2, changes);
        Assert.IsFalse(manager.IsOver("bin"));
    }

    [TestMethod]
    public void SetTargetOverIsIgnoredWhileIdleAndForUnknownTargets()
    {
        var manager = Manager(Target("bin"));
        var changes = 0;
        manager.StateChanged += () => changes++;

        manager.SetTargetOver("bin", true);
        Begin(manager);
        manager.SetTargetOver("ghost", true);

        Assert.AreEqual(0, changes);
        Assert.IsFalse(manager.IsOver("bin"));
        Assert.IsFalse(manager.IsOver("ghost"));
    }

    [TestMethod]
    public void DeepHoverIncludesAncestorsAndShallowHoverOnlyTheInnermost()
    {
        // outer > middle > inner, all hovered: the pointer is over inner.
        var manager = Manager(Target("outer"), Target("middle", "outer"), Target("inner", "middle"));
        Begin(manager);
        manager.SetTargetOver("outer", true);
        manager.SetTargetOver("middle", true);
        manager.SetTargetOver("inner", true);

        Assert.IsTrue(manager.IsOver("outer"));
        Assert.IsTrue(manager.IsOver("middle"));
        Assert.IsTrue(manager.IsOver("inner"));
        Assert.IsFalse(manager.IsOver("outer", shallow: true));
        Assert.IsFalse(manager.IsOver("middle", shallow: true));
        Assert.IsTrue(manager.IsOver("inner", shallow: true));

        manager.SetTargetOver("inner", false);

        Assert.IsTrue(manager.IsOver("middle", shallow: true));
        Assert.IsFalse(manager.IsOver("outer", shallow: true));
    }

    [TestMethod]
    public void ShallowHoverLooksThroughAnUnhoveredIntermediateTarget()
    {
        // middle does not accept the item, so only outer and inner are hovered
        var manager = Manager(Target("outer"), Target("middle", "outer", accept: ["card"]), Target("inner", "middle"));
        Begin(manager);
        manager.SetTargetOver("outer", true);
        manager.SetTargetOver("inner", true);

        Assert.IsFalse(manager.IsOver("outer", shallow: true));
        Assert.IsTrue(manager.IsOver("inner", shallow: true));
    }

    [TestMethod]
    public void AHoveredSiblingDoesNotTakeTheShallowHover()
    {
        var manager = Manager(Target("left"), Target("right"));
        Begin(manager);
        manager.SetTargetOver("left", true);
        manager.SetTargetOver("right", true);

        Assert.IsTrue(manager.IsOver("left", shallow: true));
        Assert.IsTrue(manager.IsOver("right", shallow: true));
    }

    [TestMethod]
    public void IsOverIsFalseForUnhoveredAndUnknownTargets()
    {
        var manager = Manager(Target("bin"));
        Begin(manager);

        Assert.IsFalse(manager.IsOver("bin"));
        Assert.IsFalse(manager.IsOver("bin", shallow: true));
        Assert.IsFalse(manager.IsOver("ghost"));
    }

    // ---- registration ----

    [TestMethod]
    public void ReRegisteringATargetKeepsItsHoverAndTakesTheNewAcceptListAndPredicate()
    {
        var manager = Manager(Target("bin"));
        Begin(manager);
        manager.SetTargetOver("bin", true);

        manager.RegisterTarget(Target("bin", accept: ["box"], canDrop: (_, _) => false));

        Assert.IsTrue(manager.IsOver("bin"), "a re-render must not lose an in-flight hover");
        Assert.IsFalse(manager.CanDrop("bin"));

        manager.RegisterTarget(Target("bin", accept: ["card"]));

        Assert.IsFalse(manager.CanDrop("bin"));
        manager.RegisterTarget(Target("bin", accept: ["box"]));
        Assert.IsTrue(manager.CanDrop("bin"));
    }

    [TestMethod]
    public void UnregisteringAHoveredTargetNotifiesAndHandsTheShallowHoverBack()
    {
        var manager = Manager(Target("outer"), Target("inner", "outer"));
        Begin(manager);
        manager.SetTargetOver("outer", true);
        manager.SetTargetOver("inner", true);
        var changes = 0;
        manager.StateChanged += () => changes++;

        manager.UnregisterTarget("inner");

        Assert.AreEqual(1, changes);
        Assert.IsTrue(manager.IsOver("outer", shallow: true));
        Assert.IsFalse(manager.CanDrop("inner"));
    }

    [TestMethod]
    public void UnregisteringAnUnhoveredOrUnknownTargetDoesNotNotify()
    {
        var manager = Manager(Target("bin"), Target("other"));
        Begin(manager);
        manager.SetTargetOver("other", true);
        var changes = 0;
        manager.StateChanged += () => changes++;

        manager.UnregisterTarget("bin");
        manager.UnregisterTarget("ghost");

        Assert.AreEqual(0, changes);
        Assert.IsFalse(manager.CanDrop("bin"));
    }

    // ---- drop ----

    [TestMethod]
    public void RecordDropMarksTheDropAndKeepsItsResult()
    {
        var manager = Manager(Target("bin"));
        Begin(manager);

        manager.RecordDrop(manager.DragId, "archived");

        Assert.IsTrue(manager.DidDrop());
        Assert.AreEqual("archived", manager.GetDropResult());
    }

    [TestMethod]
    public void AnOuterResultOverridesAnInnerOneButNullDoesNotEraseIt()
    {
        var manager = Manager();
        Begin(manager);

        manager.RecordDrop(manager.DragId, "inner");
        manager.RecordDrop(manager.DragId, "outer");
        Assert.AreEqual("outer", manager.GetDropResult());

        manager.RecordDrop(manager.DragId, null);
        Assert.AreEqual("outer", manager.GetDropResult());
        Assert.IsTrue(manager.DidDrop());
    }

    [TestMethod]
    public void ANullResultStillCountsAsADrop()
    {
        var manager = Manager();
        Begin(manager);

        manager.RecordDrop(manager.DragId, null);

        Assert.IsTrue(manager.DidDrop());
        Assert.IsNull(manager.GetDropResult());
    }

    [TestMethod]
    public void ADropRecordedForAnEndedDragIsIgnored()
    {
        // a slow async OnDrop of the previous drag completes during the next one
        var manager = Manager();
        Begin(manager);
        var staleDragId = manager.DragId;
        manager.EndDrag();
        Begin(manager);

        manager.RecordDrop(staleDragId, "late");

        Assert.IsFalse(manager.DidDrop());
        Assert.IsNull(manager.GetDropResult());
    }

    [TestMethod]
    public void ADropRecordedWhileIdleIsIgnored()
    {
        var manager = Manager();
        Begin(manager);
        var dragId = manager.DragId;
        manager.EndDrag();

        manager.RecordDrop(dragId, "late");

        Assert.IsFalse(manager.DidDrop());
        Assert.IsNull(manager.GetDropResult());
    }

    // ---- end ----

    [TestMethod]
    public void EndDragClearsTheWholeStateAndNotifies()
    {
        var manager = Manager(Target("bin"));
        Begin(manager);
        manager.SetTargetOver("bin", true);
        manager.SetClientOffset(new(120, 60));
        manager.RecordDrop(manager.DragId, "archived");
        var changes = 0;
        manager.StateChanged += () => changes++;

        manager.EndDrag();

        Assert.AreEqual(1, changes);
        Assert.IsFalse(manager.IsDragging());
        Assert.IsFalse(manager.IsDragging("src"));
        Assert.IsNull(manager.GetItem());
        Assert.IsNull(manager.GetItemType());
        Assert.IsNull(manager.GetDropResult());
        Assert.IsFalse(manager.DidDrop());
        Assert.IsNull(manager.GetInitialClientOffset());
        Assert.IsNull(manager.GetInitialSourceClientOffset());
        Assert.IsNull(manager.GetClientOffset());
        Assert.IsNull(manager.GetSourceClientOffset());
        Assert.IsFalse(manager.CanDrop("bin"));

        Begin(manager);
        Assert.IsFalse(manager.IsOver("bin"), "the hover flags are reset too");
    }

    [TestMethod]
    public void EndDragWhileIdleDoesNotNotify()
    {
        var manager = Manager();
        var changes = 0;
        manager.StateChanged += () => changes++;

        manager.EndDrag();

        Assert.AreEqual(0, changes);
    }

    [TestMethod]
    public async Task EndingAnUnpublishedDragDiscardsItsBeginCallback()
    {
        var manager = Manager();
        var begun = 0;
        Begin(manager, onBegun: () =>
        {
            begun++;
            return Task.CompletedTask;
        });

        manager.EndDrag();
        await manager.FlushBeginAsync();

        Assert.AreEqual(0, begun);
    }

    // ---- offsets ----

    [TestMethod]
    public void OffsetsFollowThePointer()
    {
        var manager = Manager();
        Begin(manager);

        manager.SetClientOffset(new(130, 20));

        Assert.AreEqual(new BdndXYCoord(130, 20), manager.GetClientOffset());
        Assert.AreEqual(Pointer, manager.GetInitialClientOffset());
        Assert.AreEqual(new BdndXYCoord(30, -30), manager.GetDifferenceFromInitialOffset());
        Assert.AreEqual(new BdndXYCoord(110, 10), manager.GetSourceClientOffset());
    }

    [TestMethod]
    public void SetClientOffsetRaisesOffsetChangedOnlyWhenTheOffsetChanges()
    {
        var manager = Manager();
        Begin(manager);
        var offsets = 0;
        var changes = 0;
        manager.OffsetChanged += () => offsets++;
        manager.StateChanged += () => changes++;

        manager.SetClientOffset(Pointer);
        Assert.AreEqual(0, offsets, "the drag starts at this offset");

        manager.SetClientOffset(new(101, 50));
        manager.SetClientOffset(new(101, 50));
        Assert.AreEqual(1, offsets);
        Assert.AreEqual(0, changes, "pointer moves are not structural changes");
    }

    [TestMethod]
    public void SetClientOffsetIsIgnoredWhileIdle()
    {
        var manager = Manager();
        var offsets = 0;
        manager.OffsetChanged += () => offsets++;

        manager.SetClientOffset(new(1, 2));

        Assert.AreEqual(0, offsets);
        Assert.IsNull(manager.GetClientOffset());
    }

    [TestMethod]
    public void SetInitialSourceClientOffsetCorrectsTheSourceCorner()
    {
        var manager = Manager();
        Begin(manager);
        manager.SetClientOffset(new(110, 60));
        var offsets = 0;
        manager.OffsetChanged += () => offsets++;

        manager.SetInitialSourceClientOffset(new(70, 30));
        manager.SetInitialSourceClientOffset(new(70, 30));

        Assert.AreEqual(1, offsets);
        Assert.AreEqual(new BdndXYCoord(70, 30), manager.GetInitialSourceClientOffset());
        Assert.AreEqual(new BdndXYCoord(80, 40), manager.GetSourceClientOffset());
    }

    [TestMethod]
    public void SetInitialSourceClientOffsetIsIgnoredWhileIdle()
    {
        var manager = Manager();
        var offsets = 0;
        manager.OffsetChanged += () => offsets++;

        manager.SetInitialSourceClientOffset(new(1, 2));

        Assert.AreEqual(0, offsets);
        Assert.IsNull(manager.GetInitialSourceClientOffset());
    }
}
