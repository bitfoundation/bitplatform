using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Bdnd.Tests.Components;

/// <summary>
/// Keyboard drags: the module reports Space/Enter on a source (<c>HandleKeyboardDragStart</c>) and every key of the
/// drag after it (<c>HandleKeyboardCommand</c>), and answers where a move lands (<c>keyboardNavigate</c>, planned here
/// per test). The drag runs through the same manager, hover state and OnHover/OnDrop/OnEnd chain as a native one.
/// </summary>
[TestClass]
public class BdndKeyboardDragTests : BdndComponentTestBase
{
    private static readonly BdndDomRect SourceRect = new() { Left = 10, Top = 20, Width = 100, Height = 40 };

    private readonly List<string> _log = [];
    private readonly StateLog<BdndDragState> _sourceStates = new();
    private readonly StateLog<BdndDropState<string>> _binStates = new();

    private IRenderedComponent<BdndProvider> RenderDustbin(
        Action<ComponentParameterCollectionBuilder<BdndDraggable<string>>>? source = null,
        Func<BdndDropEventArgs<string>, Task>? onDrop = null,
        string? binLabel = "Bin",
        Func<string?, IBdndMonitor, bool>? binPredicate = null)
        => RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d =>
            {
                d.Add(x => x.Type, "box");
                d.Add(x => x.Item, "crate");
                d.Add(x => x.OnBegin, item => _log.Add($"begin:{item}"));
                d.Add(x => x.OnEnd, e => _log.Add($"end:{e.Item}:{e.DidDrop}:{e.DropResult}"));
                d.Add(x => x.ChildContent, _sourceStates.Content());
                source?.Invoke(d);
            })
            .AddChildContent<BdndDropTarget<string>>(t =>
            {
                t.Add(x => x.Accept, "box");
                t.Add(x => x.Label, binLabel);
                t.Add(x => x.CanDropPredicate, binPredicate);
                t.Add(x => x.OnHover, e => _log.Add($"hover:{e.Item}:{e.Monitor.GetClientOffset()}"));
                t.Add(x => x.OnDrop, onDrop ?? (e =>
                {
                    _log.Add($"drop:{e.Item}");
                    e.Result = "archived";
                    return Task.CompletedTask;
                }));
                t.Add(x => x.ChildContent, _binStates.Content());
            }));

    private static IRenderedComponent<BdndDraggable<string>> Source(IRenderedComponent<BdndProvider> cut)
        => cut.FindComponent<BdndDraggable<string>>();

    private string TargetIdOf<TItem>(IRenderedComponent<BdndDropTarget<TItem>> target)
        => ((BdndDropTargetOptions)TargetRegistrationCall(target.Instance).Arguments[2]!).Id;

    /// <summary>The module lands a <paramref name="command"/> from <paramref name="from"/> (null: no target yet) on <paramref name="to"/>.</summary>
    private void PlanMove(string? from, string command, string to, int position = 1, int count = 1, bool moved = true, string? label = null,
        BdndDomRect? rect = null)
        => Sources.Setup<BdndKeyboardTarget?>("keyboardNavigate",
                i => (string?)i.Arguments[2] == from && (string)i.Arguments[4]! == command)
            .SetResult(new BdndKeyboardTarget
            {
                Id = to,
                Rect = rect ?? new BdndDomRect { Left = 200, Top = 300, Width = 50, Height = 20 },
                Label = label,
                Position = position,
                Count = count,
                Moved = moved,
            });

    /// <summary>The module counts <paramref name="count"/> candidates before the first move.</summary>
    private void PlanCount(int count)
        => Sources.Setup<BdndKeyboardTarget?>("keyboardNavigate", i => (string)i.Arguments[4]! == "stay")
            .SetResult(new BdndKeyboardTarget { Id = "", Count = count });

    private static Task<bool> StartAsync<TItem>(IRenderedComponent<BdndDraggable<TItem>> draggable, string? text = "Crate")
        => draggable.InvokeAsync(() => draggable.Instance.HandleKeyboardDragStart(SourceRect, text));

    private static Task<bool> CommandAsync<TItem>(IRenderedComponent<BdndDraggable<TItem>> draggable, string command)
        => draggable.InvokeAsync(() => draggable.Instance.HandleKeyboardCommand(command));

    private string[] Announcements => [.. Module.Invocations["announce"].Select(i => (string)i.Arguments[0]!)];

    // ---- a whole keyboard drag ----

    [TestMethod]
    public async Task SpacePicksTheSourceUpAsAKeyboardDragFromItsMiddle()
    {
        var cut = RenderDustbin();
        PlanCount(1);

        Assert.IsTrue(await StartAsync(Source(cut)));

        Assert.IsTrue(Manager.IsDragging());
        Assert.AreEqual(BdndDragInput.Keyboard, Manager.GetDragInput());
        Assert.AreEqual(new BdndXYCoord(60, 40), Manager.GetInitialClientOffset());
        Assert.AreEqual(new BdndXYCoord(10, 20), Manager.GetInitialSourceClientOffset());
        Assert.IsTrue(_sourceStates.Last.IsDragging);
        Assert.IsTrue(_binStates.Last.CanDrop);
        Assert.IsFalse(_binStates.Last.IsOver);
        Assert.AreSequenceEqual(new[] { "begin:crate" }, _log);
        Assert.AreSequenceEqual(new[] { "Picked up Crate. 1 drop target is available." }, Announcements);
    }

    [TestMethod]
    public async Task AnArrowMovesTheDragOntoATargetWhichIsHoveredFromItsMiddle()
    {
        var cut = RenderDustbin();
        var bin = cut.FindComponent<BdndDropTarget<string>>();
        PlanCount(1);
        PlanMove(null, "next", TargetIdOf(bin));
        await StartAsync(Source(cut));

        Assert.IsTrue(await CommandAsync(Source(cut), "next"));

        Assert.IsTrue(_binStates.Last.IsOver);
        Assert.IsTrue(_binStates.Last.IsOverShallow);
        Assert.AreEqual(new BdndXYCoord(225, 310), Manager.GetClientOffset());
        Assert.AreEqual("hover:crate:BdndXYCoord { X = 225, Y = 310 }", _log[^1]);
        Assert.AreEqual("Crate is over Bin.", Announcements[^1]);
    }

    [TestMethod]
    [DataRow("down")]
    [DataRow("up")]
    public async Task DownAndUpMoveTheDragLikeTheOtherArrows(string command)
    {
        var cut = RenderDustbin();
        var bin = cut.FindComponent<BdndDropTarget<string>>();
        PlanCount(1);
        PlanMove(null, command, TargetIdOf(bin));
        await StartAsync(Source(cut));

        Assert.IsTrue(await CommandAsync(Source(cut), command));

        Assert.IsTrue(_binStates.Last.IsOver);
        Assert.AreEqual("Crate is over Bin.", Announcements[^1]);
    }

    [TestMethod]
    public async Task SpaceOverATargetDropsOnItAndEndsTheDrag()
    {
        var cut = RenderDustbin();
        var bin = cut.FindComponent<BdndDropTarget<string>>();
        PlanCount(1);
        PlanMove(null, "next", TargetIdOf(bin));
        await StartAsync(Source(cut));
        await CommandAsync(Source(cut), "next");

        Assert.IsFalse(await CommandAsync(Source(cut), "drop"));

        Assert.AreSequenceEqual(new[] { "begin:crate", "hover:crate:BdndXYCoord { X = 225, Y = 310 }", "drop:crate", "end:crate:True:archived" }, _log);
        Assert.IsFalse(Manager.IsDragging());
        Assert.IsNull(Manager.GetDragInput());
        Assert.IsFalse(_binStates.Last.IsOver);
        Assert.IsFalse(_sourceStates.Last.IsDragging);
        Assert.AreEqual("Crate was dropped on Bin.", Announcements[^1]);
    }

    [TestMethod]
    public async Task EscapeCancelsTheDrag()
    {
        var cut = RenderDustbin();
        var bin = cut.FindComponent<BdndDropTarget<string>>();
        PlanCount(1);
        PlanMove(null, "next", TargetIdOf(bin));
        await StartAsync(Source(cut));
        await CommandAsync(Source(cut), "next");

        Assert.IsFalse(await CommandAsync(Source(cut), "cancel"));

        Assert.AreEqual("end:crate:False:", _log[^1]);
        Assert.IsFalse(_log.Any(l => l.StartsWith("drop:", StringComparison.Ordinal)));
        Assert.IsFalse(Manager.IsDragging());
        Assert.AreEqual("Dragging Crate was cancelled.", Announcements[^1]);
    }

    [TestMethod]
    public async Task DroppingOverNoTargetCancels()
    {
        var cut = RenderDustbin();
        PlanCount(1);
        await StartAsync(Source(cut));

        Assert.IsFalse(await CommandAsync(Source(cut), "drop"));

        Assert.AreSequenceEqual(new[] { "begin:crate", "end:crate:False:" }, _log);
        Assert.AreEqual("Dragging Crate was cancelled.", Announcements[^1]);
    }

    [TestMethod]
    public async Task AMoveWithNoFurtherTargetStaysAndSaysSo()
    {
        var cut = RenderDustbin();
        var binId = TargetIdOf(cut.FindComponent<BdndDropTarget<string>>());
        PlanCount(1);
        PlanMove(null, "next", binId);
        PlanMove(binId, "next", binId, moved: false);
        await StartAsync(Source(cut));
        await CommandAsync(Source(cut), "next");
        var hovers = _log.Count(l => l.StartsWith("hover:", StringComparison.Ordinal));

        Assert.IsTrue(await CommandAsync(Source(cut), "next"));

        Assert.AreEqual(hovers, _log.Count(l => l.StartsWith("hover:", StringComparison.Ordinal)));
        Assert.IsTrue(_binStates.Last.IsOver);
        Assert.AreEqual("Bin is the last drop target in that direction.", Announcements[^1]);
    }

    [TestMethod]
    public async Task OnlyTheTargetsThatCanTakeTheItemAreCandidates()
    {
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.Item, "crate"))
            .AddChildContent<BdndDropTarget<string>>(t => t.Add(x => x.Accept, "box").Add(x => x.Label, "yes"))
            .AddChildContent<BdndDropTarget<string>>(t => t.Add(x => x.Accept, "glass").Add(x => x.Label, "wrong type"))
            .AddChildContent<BdndDropTarget<string>>(t => t.Add(x => x.Accept, "box").Add(x => x.CanDropPredicate, (_, _) => false)));
        var targets = cut.FindComponents<BdndDropTarget<string>>();

        await StartAsync(Source(cut));

        var candidates = ((BdndKeyboardCandidate[])Sources.Invocations["keyboardNavigate"].Last().Arguments[0]!).Select(c => c.Id).ToArray();
        Assert.AreSequenceEqual(new[] { TargetIdOf(targets[0]) }, candidates);
    }

    [TestMethod]
    public async Task ADropOnANestedTargetRunsTheChainInnermostFirst()
    {
        var log = new List<string>();
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.Item, "crate")
                .Add(x => x.OnEnd, e => log.Add($"end:{e.DidDrop}:{e.DropResult}")))
            .AddChildContent<BdndDropTarget<string>>(outer => outer
                .Add(x => x.Accept, "box")
                .Add(x => x.OnDrop, e =>
                {
                    log.Add($"outer:{e.Monitor.DidDrop()}");
                    e.Result = "outer";
                })
                .Add(x => x.ChildContent, _ => Fragment<BdndDropTarget<string>>(
                    (nameof(BdndDropTarget<string>.Accept), "box"),
                    (nameof(BdndDropTarget<string>.OnDrop), EventCallback.Factory.Create<BdndDropEventArgs<string>>(this, e =>
                    {
                        log.Add($"inner:{e.Monitor.DidDrop()}");
                        e.Result = "inner";
                    }))))));
        var targets = cut.FindComponents<BdndDropTarget<string>>();
        var inner = targets[1];
        PlanMove(null, "next", TargetIdOf(inner));
        await StartAsync(Source(cut));
        await CommandAsync(Source(cut), "next");

        Assert.IsTrue(Manager.IsOver(TargetIdOf(targets[0])));
        Assert.IsTrue(Manager.IsOver(TargetIdOf(inner), shallow: true));
        Assert.IsFalse(Manager.IsOver(TargetIdOf(targets[0]), shallow: true));

        await CommandAsync(Source(cut), "drop");

        Assert.AreSequenceEqual(new[] { "inner:False", "outer:True", "end:True:outer" }, log);
    }

    [TestMethod]
    public async Task AnAsyncOnDropIsAwaitedBeforeOnEnd()
    {
        var gate = new TaskCompletionSource();
        var cut = RenderDustbin(onDrop: async e =>
        {
            await gate.Task;
            e.Result = "late";
        });
        var bin = cut.FindComponent<BdndDropTarget<string>>();
        PlanMove(null, "next", TargetIdOf(bin));
        await StartAsync(Source(cut));
        await CommandAsync(Source(cut), "next");

        var drop = CommandAsync(Source(cut), "drop");
        Assert.IsFalse(drop.IsCompleted);
        gate.SetResult();

        Assert.IsFalse(await drop.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.AreEqual("end:crate:True:late", _log[^1]);
    }

    // ---- what starts, and what does not ----

    [TestMethod]
    public async Task WithoutAllowKeyboardNothingStarts()
    {
        var cut = RenderDustbin(source: d => d.Add(x => x.AllowKeyboard, false));

        Assert.IsFalse(await StartAsync(Source(cut)));
        Assert.IsFalse(Manager.IsDragging());
    }

    [TestMethod]
    public async Task ASourceThatCannotDragDoesNotStart()
    {
        var cut = RenderDustbin(source: d => d.Add(x => x.CanDrag, false));

        Assert.IsFalse(await StartAsync(Source(cut)));
        Assert.IsFalse(Manager.IsDragging());
    }

    [TestMethod]
    public async Task AnItemFactoryReturningNullDoesNotStart()
    {
        var cut = RenderDustbin(source: d => d.Add(x => x.ItemFactory, () => null));

        Assert.IsFalse(await StartAsync(Source(cut)));
        Assert.IsFalse(Manager.IsDragging());
    }

    [TestMethod]
    public async Task AKeyboardPickupEndsADragWhoseEndNeverArrived()
    {
        var cut = RenderDustbin();
        await StartDragAsync(Source(cut));

        Assert.IsTrue(await StartAsync(Source(cut)));

        Assert.AreSequenceEqual(new[] { "begin:crate", "end:crate:False:", "begin:crate" }, _log);
        Assert.AreEqual(BdndDragInput.Keyboard, Manager.GetDragInput());
    }

    [TestMethod]
    public async Task ANativeDragStartingDuringAKeyboardDragEndsItFirst()
    {
        var cut = RenderDustbin();
        PlanMove(null, "next", TargetIdOf(cut.FindComponent<BdndDropTarget<string>>()));
        await StartAsync(Source(cut));

        await StartDragAsync(Source(cut));

        Assert.AreSequenceEqual(new[] { "begin:crate", "end:crate:False:", "begin:crate" }, _log);
        Assert.AreEqual(BdndDragInput.Native, Manager.GetDragInput());

        // a key of the keyboard drag that was still on its way does not touch the native drag
        Assert.IsFalse(await CommandAsync(Source(cut), "next"));
        Assert.IsNull(Manager.KeyboardTargetId);
        Assert.IsFalse(_binStates.Last.IsOver);
        Assert.AreEqual(BdndDragInput.Native, Manager.GetDragInput());
    }

    [TestMethod]
    public async Task AFailingPredicateEndsTheKeyboardDragAndIsReported()
    {
        var fail = false;
        var cut = RenderDustbin(binPredicate: (_, _) => fail ? throw new InvalidOperationException("predicate failed") : true);
        await StartAsync(Source(cut));
        fail = true;

        Assert.IsFalse(await CommandAsync(Source(cut), "next"));

        var error = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreEqual("predicate failed", error.Message);
        Assert.IsFalse(Manager.IsDragging());
        Assert.AreEqual("end:crate:False:", _log[^1]);
    }

    [TestMethod]
    [DataRow("button")]
    [DataRow("a")]
    [DataRow("input")]
    public void AnInteractiveHostKeepsItsOwnKeys(string tag)
    {
        var cut = RenderDustbin(source: d => d.Add(x => x.Tag, tag));

        var host = HostOf(Source(cut));
        Assert.IsNull(host.GetAttribute("tabindex"));
        Assert.IsNull(host.GetAttribute("aria-describedby"));
        Assert.IsFalse(SourceRegistrations.Last().Keyboard);
    }

    [TestMethod]
    public void AnInteractiveHostIsDraggedByKeyboardFromItsHandle()
    {
        var cut = RenderDustbin(source: d => d
            .Add(x => x.Tag, "button")
            .Add(x => x.UseHandle, true)
            .Add(x => x.ChildContent, _ => Fragment<BdndDragHandle>()));

        Assert.IsTrue(SourceRegistrations.Last().Keyboard);
        Assert.AreEqual("0", cut.Find("[data-bit-bdnd-handle]").GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task KeyboardCommandsDoNotTouchANativeDrag()
    {
        var cut = RenderDustbin();
        await StartDragAsync(Source(cut));

        Assert.IsFalse(await CommandAsync(Source(cut), "cancel"));
        Assert.IsTrue(Manager.IsDragging());
    }

    // ---- labels and announcements ----

    [TestMethod]
    public async Task TheSourceIsNamedByItsLabelBeforeItsText()
    {
        var cut = RenderDustbin(source: d => d.Add(x => x.Label, "Write the docs"));

        await StartAsync(Source(cut), "ignored text");
        Assert.StartsWith("Picked up Write the docs.", Announcements[^1]);

        await CommandAsync(Source(cut), "cancel");
        Assert.AreEqual("Dragging Write the docs was cancelled.", Announcements[^1]);
    }

    [TestMethod]
    public async Task AnUnnamedSourceWithoutTextIsAnItem()
    {
        var cut = RenderDustbin();

        await StartAsync(Source(cut), "  ");

        Assert.StartsWith("Picked up item.", Announcements[^1]);
    }

    [TestMethod]
    public async Task AnUnlabelledTargetIsNamedByItsAriaLabelElseByItsPlace()
    {
        var cut = RenderDustbin(binLabel: null);
        var binId = TargetIdOf(cut.FindComponent<BdndDropTarget<string>>());
        PlanMove(null, "next", binId, position: 2, count: 3, label: "Archive");
        PlanMove(binId, "first", binId, position: 2, count: 3, moved: true);
        await StartAsync(Source(cut));

        await CommandAsync(Source(cut), "next");
        Assert.AreEqual("Crate is over Archive.", Announcements[^1]);

        await CommandAsync(Source(cut), "first");
        Assert.AreEqual("Crate is over drop target 2 of 3.", Announcements[^1]);
    }

    [TestMethod]
    public async Task TheProvidersAnnouncementsReplaceTheDefaultOnes()
    {
        var announcements = new BdndAnnouncements
        {
            DragStarted = a => $"Aufgenommen: {a.SourceLabel} ({a.TargetCount})",
            Cancelled = _ => null,
        };
        var cut = Render<BdndProvider>(p => p
            .Add(x => x.Manager, Manager)
            .Add(x => x.Announcements, announcements)
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box").Add(x => x.Item, "crate")));
        PlanCount(4);

        await StartAsync(cut.FindComponent<BdndDraggable<string>>());
        await CommandAsync(cut.FindComponent<BdndDraggable<string>>(), "cancel");

        Assert.AreSame(announcements, Manager.Announcements);
        Assert.AreSequenceEqual(new[] { "Aufgenommen: Crate (4)" }, Announcements);
    }

    [TestMethod]
    public async Task AnAnnouncementThatThrowsIsReportedAndTheDragGoesOn()
    {
        Manager.Announcements = new BdndAnnouncements { DragStarted = _ => throw new InvalidOperationException("bad text") };
        var cut = RenderDustbin();

        Assert.IsTrue(await StartAsync(Source(cut)));

        var error = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreEqual("bad text", error.Message);
        Assert.IsTrue(Manager.IsDragging());
    }

    // ---- what reaches the script and the markup ----

    [TestMethod]
    public void AKeyboardDraggableSourceTellsTheScriptAndIsFocusableAndDescribed()
    {
        var cut = RenderDustbin();

        var options = SourceRegistrations.Last();
        Assert.IsTrue(options.Keyboard);
        Assert.AreEqual(Manager.InstructionsId, options.InstructionsId);
        Assert.AreEqual(BdndAnnouncements.Default.Instructions, options.Instructions);

        var host = HostOf(Source(cut));
        Assert.AreEqual("0", host.GetAttribute("tabindex"));
        Assert.AreEqual(Manager.InstructionsId, host.GetAttribute("aria-describedby"));
        Assert.StartsWith("bit-bdnd-instructions-", Manager.InstructionsId);
    }

    [TestMethod]
    public void TheSourceKeepsTheTabIndexAndDescriptionItWasGiven()
    {
        var cut = RenderDustbin(source: d => d.AddUnmatched("tabindex", "-1").AddUnmatched("aria-describedby", "help"));

        var host = HostOf(Source(cut));
        Assert.AreEqual("-1", host.GetAttribute("tabindex"));
        Assert.AreEqual($"help {Manager.InstructionsId}", host.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    [DataRow(false, true)]
    [DataRow(true, false)]
    public void ASourceTheKeyboardCannotDragIsNeitherFocusableNorDescribed(bool allowKeyboard, bool canDrag)
    {
        var cut = RenderDustbin(source: d => d.Add(x => x.AllowKeyboard, allowKeyboard).Add(x => x.CanDrag, canDrag));

        var host = HostOf(Source(cut));
        Assert.IsNull(host.GetAttribute("tabindex"));
        Assert.IsNull(host.GetAttribute("aria-describedby"));
        Assert.IsFalse(SourceRegistrations.Last().Keyboard);
    }

    [TestMethod]
    public void WithAHandleTheHandleIsTheFocusableLabelledButton()
    {
        var cut = RenderDustbin(source: d => d
            .Add(x => x.UseHandle, true)
            .Add(x => x.Label, "Crate")
            .Add(x => x.ChildContent, _ => Fragment<BdndDragHandle>()));

        var host = HostOf(Source(cut));
        Assert.IsNull(host.GetAttribute("tabindex"));
        var handle = cut.Find("[data-bit-bdnd-handle]");
        Assert.AreEqual("0", handle.GetAttribute("tabindex"));
        Assert.AreEqual("button", handle.GetAttribute("role"));
        Assert.AreEqual("Crate", handle.GetAttribute("aria-label"));
        Assert.AreEqual(Manager.InstructionsId, handle.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void AHandleKeepsTheRoleAndLabelItWasGiven()
    {
        var cut = RenderDustbin(source: d => d
            .Add(x => x.UseHandle, true)
            .Add(x => x.Label, "Crate")
            .Add(x => x.ChildContent, _ => Fragment<BdndDragHandle>(("role", "img"), ("aria-label", "Grip"))));

        var handle = cut.Find("[data-bit-bdnd-handle]");
        Assert.AreEqual("img", handle.GetAttribute("role"));
        Assert.AreEqual("Grip", handle.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void AHandleOfASourceTheKeyboardCannotDragIsNotFocusable()
    {
        var cut = RenderDustbin(source: d => d
            .Add(x => x.UseHandle, true)
            .Add(x => x.AllowKeyboard, false)
            .Add(x => x.ChildContent, _ => Fragment<BdndDragHandle>()));

        var handle = cut.Find("[data-bit-bdnd-handle]");
        Assert.IsNull(handle.GetAttribute("tabindex"));
        Assert.IsNull(handle.GetAttribute("role"));
    }

    [TestMethod]
    public void ChangedInstructionsAreWhatTheSourcesAreDescribedBy()
    {
        var cut = RenderDustbin();

        Manager.Announcements = new BdndAnnouncements { Instructions = "Leertaste zum Aufnehmen." };

        cut.WaitForAssertion(() => Assert.AreEqual(Manager.InstructionsId, HostOf(Source(cut)).GetAttribute("aria-describedby")));
        Assert.AreEqual("Leertaste zum Aufnehmen.", SourceRegistrations.Concat(SourceUpdates).Last().Instructions);
    }

    [TestMethod]
    public void ChangedInstructionsGetAnotherId()
    {
        var before = Manager.InstructionsId;

        Manager.Announcements = new BdndAnnouncements { Instructions = "Drücken Sie die Leertaste." };

        Assert.AreNotEqual(before, Manager.InstructionsId);
        Manager.Announcements = new BdndAnnouncements();
        Assert.AreEqual(before, Manager.InstructionsId);
    }

    // ---- the source going away, and the focus ----

    [TestMethod]
    public async Task ASourceRemovedDuringAKeyboardDragStillEndsIt()
    {
        var show = true;
        var cut = RenderProvider(p => p.Add(x => x.ChildContent, (RenderFragment)(b => b.AddContent(0, show
            ? Fragment<BdndDraggable<string>>(
                (nameof(BdndDraggable<string>.Type), "box"),
                (nameof(BdndDraggable<string>.Item), "crate"),
                (nameof(BdndDraggable<string>.OnEnd), EventCallback.Factory.Create<BdndDragEndContext<string>>(this, e => _log.Add($"end:{e.DidDrop}"))))
            : null))));
        var source = cut.FindComponent<BdndDraggable<string>>();
        var dotNetRef = (DotNetObjectReference<BdndDraggable<string>>)SourceRegistrationCall(source.Instance).Arguments[1]!;
        await StartAsync(source);

        show = false;
        cut.Render();
        Assert.AreEqual(0, Sources.Invocations["dispose"].Count, "the registration lives on while the drag does");

        Assert.IsFalse(await source.InvokeAsync(() => dotNetRef.Value.HandleKeyboardCommand("cancel")));

        Assert.AreSequenceEqual(new[] { "end:False" }, _log);
        Assert.AreEqual(1, Sources.Invocations["dispose"].Count);
        Assert.ThrowsExactly<ObjectDisposedException>(() => dotNetRef.Value);
    }

    [TestMethod]
    public async Task ADraggableShowingTheDroppedItemTakesTheFocusOver()
    {
        var inFirst = true;
        var cut = RenderProvider(p => p.Add(x => x.ChildContent, (RenderFragment)(b =>
        {
            b.OpenElement(0, "section");
            if (inFirst) b.AddContent(1, Card("first"));
            b.CloseElement();
            b.OpenElement(2, "section");
            if (inFirst is false) b.AddContent(3, Card("second"));
            b.CloseElement();
            b.OpenComponent<BdndDropTarget<string>>(4);
            b.AddComponentParameter(5, nameof(BdndDropTarget<string>.Accept), "card");
            b.AddComponentParameter(6, nameof(BdndDropTarget<string>.OnDrop),
                EventCallback.Factory.Create<BdndDropEventArgs<string>>(this, () => inFirst = false));
            b.CloseComponent();
        })));
        Sources.Setup<bool>("adoptFocus").SetResult(true);
        PlanMove(null, "next", TargetIdOf(cut.FindComponent<BdndDropTarget<string>>()));
        await StartAsync(cut.FindComponent<BdndDraggable<string>>());
        await CommandAsync(cut.FindComponent<BdndDraggable<string>>(), "next");

        await CommandAsync(cut.FindComponent<BdndDraggable<string>>(), "drop");
        cut.Render();

        // the draggable now showing the card - not the source, which is gone - took the focus over, once
        Assert.AreEqual("second", cut.Find("[data-card]").GetAttribute("data-card"));
        Assert.HasCount(1, Sources.Invocations["adoptFocus"]);
        var newcomer = cut.FindComponent<BdndDraggable<string>>();
        Assert.IsFalse(Manager.WantsFocus("the card", "card", SourceIdOf(newcomer.Instance)), "nothing else takes it once it was taken");

        static RenderFragment Card(string where) => b =>
        {
            b.OpenComponent<BdndDraggable<string>>(0);
            b.AddComponentParameter(1, nameof(BdndDraggable<string>.Type), "card");
            b.AddComponentParameter(2, nameof(BdndDraggable<string>.Item), "the card");
            b.AddComponentParameter(3, "data-card", where);
            b.CloseComponent();
        };
    }

    [TestMethod]
    public async Task ADraggableShowingAnotherItemDoesNotTakeTheFocus()
    {
        var cut = RenderProvider(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "card").Add(x => x.Item, "dragged"))
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "card").Add(x => x.Item, "other")));
        var draggables = cut.FindComponents<BdndDraggable<string>>();
        await StartAsync(draggables[0]);

        draggables[1].Render();

        Assert.IsEmpty(Sources.Invocations["adoptFocus"]);
    }
}
