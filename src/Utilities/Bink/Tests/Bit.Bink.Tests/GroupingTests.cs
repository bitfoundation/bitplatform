using System.Numerics;
using Bunit;

namespace Bit.Bink.Tests;

/// <summary>Grouping, ungrouping, working inside groups, and element locks: in the model, the select tool and the canvas.</summary>
[TestClass]
public sealed class GroupingTests
{
    private static (BinkDocument Document, List<Guid> Ids) Row(int count)
    {
        var document = new BinkDocument();
        var ids = new List<Guid>();
        for (int i = 0; i < count; i++)
        {
            var shape = new BinkRectangleShape(new BinkRect(i * 100, 0, 60, 60)) { Style = new BinkShapeStyle { FillColor = BinkColor.White } };
            document.AddElement(shape);
            ids.Add(shape.Id);
        }
        return (document, ids);
    }

    private static List<Guid> Order(BinkDocument document) => document.Layers[0].Elements.Select(e => e.Id).ToList();

    private static IReadOnlyList<Guid> Groups(BinkDocument document, Guid id) => document.GetElement(id).GroupIds;

    [TestMethod]
    public void Grouping_gives_the_elements_one_shared_group_as_one_undo_step()
    {
        (BinkDocument document, List<Guid> ids) = Row(3);
        using var history = new BinkHistoryManager(document);

        Guid? group = document.GroupElements([ids[0], ids[2]]);

        Assert.IsNotNull(group);
        CollectionAssert.AreEqual(new[] { group.Value }, Groups(document, ids[0]).ToArray());
        CollectionAssert.AreEqual(new[] { group.Value }, Groups(document, ids[2]).ToArray());
        Assert.IsEmpty(Groups(document, ids[1]));
        Assert.AreEqual(1, history.UndoCount);
        Assert.AreEqual("Group", history.UndoLabel);

        Assert.IsTrue(history.Undo());
        Assert.IsTrue(ids.All(id => Groups(document, id).Count == 0));
        CollectionAssert.AreEqual(ids, Order(document), "Undo puts the stacking order back too.");
    }

    [TestMethod]
    public void Grouping_brings_the_members_together_below_the_top_most_one()
    {
        (BinkDocument document, List<Guid> ids) = Row(5);
        document.GroupElements([ids[0], ids[2], ids[4]]);
        // Nothing is sandwiched between members, and members keep their order relative to each other.
        CollectionAssert.AreEqual(new[] { ids[1], ids[3], ids[0], ids[2], ids[4] }, Order(document));
    }

    [TestMethod]
    public void Fewer_than_two_elements_are_not_grouped()
    {
        (BinkDocument document, List<Guid> ids) = Row(2);
        using var history = new BinkHistoryManager(document);
        Assert.IsNull(document.GroupElements([ids[0]]));
        Assert.IsNull(document.GroupElements([ids[0], ids[0], Guid.NewGuid()]));
        Assert.AreEqual(0, history.UndoCount);
    }

    [TestMethod]
    public void Groups_nest_and_ungrouping_takes_them_apart_one_level_at_a_time()
    {
        (BinkDocument document, List<Guid> ids) = Row(3);
        Guid inner = document.GroupElements([ids[0], ids[1]])!.Value;
        Guid outer = document.GroupElements(ids)!.Value;

        CollectionAssert.AreEqual(new[] { inner, outer }, Groups(document, ids[0]).ToArray());
        CollectionAssert.AreEqual(new[] { outer }, Groups(document, ids[2]).ToArray());

        Assert.IsTrue(document.UngroupElements(ids));
        CollectionAssert.AreEqual(new[] { inner }, Groups(document, ids[0]).ToArray(), "Only the outer group is dissolved.");
        Assert.IsEmpty(Groups(document, ids[2]));

        Assert.IsTrue(document.UngroupElements(ids));
        Assert.IsTrue(ids.All(id => Groups(document, id).Count == 0));
        Assert.IsFalse(document.UngroupElements(ids), "Nothing left to ungroup.");
    }

    [TestMethod]
    public void Grouping_part_of_a_group_nests_the_new_group_inside_it()
    {
        (BinkDocument document, List<Guid> ids) = Row(3);
        Guid outer = document.GroupElements(ids)!.Value;
        Guid inner = document.GroupElements([ids[0], ids[1]])!.Value;

        CollectionAssert.AreEqual(new[] { inner, outer }, Groups(document, ids[0]).ToArray());
        CollectionAssert.AreEqual(new[] { outer }, Groups(document, ids[2]).ToArray());
    }

    [TestMethod]
    public void Ungrouping_part_of_a_group_leaves_it_alone()
    {
        (BinkDocument document, List<Guid> ids) = Row(3);
        Guid group = document.GroupElements(ids)!.Value;
        Assert.IsFalse(document.UngroupElements([ids[0], ids[1]]));
        Assert.IsTrue(ids.All(id => Groups(document, id).Single() == group));
    }

    [TestMethod]
    public void An_element_grouped_with_an_outsider_leaves_the_group_it_was_only_partly_picked_from()
    {
        (BinkDocument document, List<Guid> ids) = Row(4);
        Guid first = document.GroupElements([ids[0], ids[1]])!.Value;
        Guid second = document.GroupElements([ids[1], ids[2]])!.Value;

        CollectionAssert.AreEqual(new[] { first }, Groups(document, ids[0]).ToArray());
        CollectionAssert.AreEqual(new[] { second }, Groups(document, ids[1]).ToArray(), "Groups never overlap without nesting.");
        CollectionAssert.AreEqual(new[] { second }, Groups(document, ids[2]).ToArray());
    }

    [TestMethod]
    public void Expanding_a_pick_selects_the_group_at_the_level_being_worked_in()
    {
        (BinkDocument document, List<Guid> ids) = Row(4);
        Guid inner = document.GroupElements([ids[0], ids[1]])!.Value;
        Guid outer = document.GroupElements([ids[0], ids[1], ids[2]])!.Value;

        CollectionAssert.AreEquivalent(new[] { ids[0], ids[1], ids[2] }, BinkDocumentQuery.ExpandToGroups(document, [ids[0]]).ToArray());
        CollectionAssert.AreEquivalent(new[] { ids[0], ids[1] }, BinkDocumentQuery.ExpandToGroups(document, [ids[0]], outer).ToArray());
        CollectionAssert.AreEquivalent(new[] { ids[0] }, BinkDocumentQuery.ExpandToGroups(document, [ids[0]], inner).ToArray());
        CollectionAssert.AreEquivalent(new[] { ids[3] }, BinkDocumentQuery.ExpandToGroups(document, [ids[3]], outer).ToArray());
        Assert.AreEqual(outer, BinkDocumentQuery.GetSelectionGroup(document.GetElement(ids[2])));
        Assert.IsNull(BinkDocumentQuery.GetSelectionGroup(document.GetElement(ids[2]), outer));
    }

    [TestMethod]
    public void Expanding_leaves_out_members_a_tool_may_not_touch()
    {
        var document = new BinkDocument();
        BinkLayer hidden = document.AddLayer("Hidden");
        var a = new BinkRectangleShape(new BinkRect(0, 0, 10, 10));
        var b = new BinkRectangleShape(new BinkRect(20, 0, 10, 10));
        var c = new BinkRectangleShape(new BinkRect(40, 0, 10, 10));
        document.AddElement(a, document.Layers[0].Id);
        document.AddElement(b, document.Layers[0].Id);
        document.AddElement(c, hidden.Id);
        document.GroupElements([a.Id, b.Id, c.Id]);
        document.UpdateLayer(hidden.Id, p => p with { IsVisible = false });
        document.SetLocked([b.Id], true);

        CollectionAssert.AreEqual(new[] { a.Id }, BinkDocumentQuery.ExpandToGroups(document, [a.Id]).ToArray());
    }

    [TestMethod]
    public void Duplicating_a_whole_group_makes_a_new_group_and_duplicating_inside_one_keeps_it()
    {
        (BinkDocument document, List<Guid> ids) = Row(3);
        Guid group = document.GroupElements(ids)!.Value;

        IReadOnlyList<BinkElement> copies = document.DuplicateElements(ids, new Vector2(0, 100));
        Guid copyGroup = copies[0].GroupIds.Single();
        Assert.AreNotEqual(group, copyGroup);
        Assert.IsTrue(copies.All(c => c.GroupIds.Single() == copyGroup), "The copies form one group of their own.");

        IReadOnlyList<BinkElement> single = document.DuplicateElements([ids[0]], new Vector2(0, 200));
        Assert.AreEqual(group, single[0].GroupIds.Single(), "A member copied on its own stays in its group.");
    }

    [TestMethod]
    public void Pasted_groups_are_new_and_meaningless_ones_are_dropped()
    {
        (BinkDocument document, List<Guid> ids) = Row(4);
        Guid inner = document.GroupElements([ids[0], ids[1]])!.Value;
        document.GroupElements([ids[0], ids[1], ids[2]]);

        // Copying the inner group from inside the outer one: among the pasted elements both groups have the same
        // members, so only one survives.
        string payload = BinkClipboard.Serialize([document.GetElement(ids[0]), document.GetElement(ids[1])]);
        Assert.IsTrue(BinkClipboard.TryDeserialize(payload, out IReadOnlyList<BinkElement> first, out _));
        Assert.IsTrue(BinkClipboard.TryDeserialize(payload, out IReadOnlyList<BinkElement> second, out _));
        Guid pasted = first[0].GroupIds.Single();
        Assert.AreEqual(pasted, first[1].GroupIds.Single());
        Assert.AreNotEqual(inner, pasted);
        Assert.AreNotEqual(pasted, second[0].GroupIds.Single(), "Each paste is a group of its own.");

        // A lone member of a group is pasted on its own.
        Assert.IsTrue(BinkClipboard.TryDeserialize(BinkClipboard.Serialize([document.GetElement(ids[2])]), out IReadOnlyList<BinkElement> lone, out _));
        Assert.IsEmpty(lone[0].GroupIds);
    }

    [TestMethod]
    public void Groups_and_locks_survive_saving_and_loading()
    {
        (BinkDocument document, List<Guid> ids) = Row(3);
        Guid inner = document.GroupElements([ids[0], ids[1]])!.Value;
        Guid outer = document.GroupElements(ids)!.Value;
        document.SetLocked([ids[2]], true);

        BinkDocument loaded = BinkSerializer.Deserialize(BinkSerializer.Serialize(document));
        CollectionAssert.AreEqual(new[] { inner, outer }, loaded.GetElement(ids[0]).GroupIds.ToArray());
        CollectionAssert.AreEqual(new[] { outer }, loaded.GetElement(ids[2]).GroupIds.ToArray());
        Assert.IsTrue(loaded.GetElement(ids[2]).IsLocked);
        Assert.IsFalse(loaded.GetElement(ids[0]).IsLocked);
    }

    [TestMethod]
    public void A_malformed_group_list_is_cleaned_and_an_absurd_one_refused()
    {
        Guid id = Guid.NewGuid();
        string Doc(string groups) => $$"""{"format":"bink","version":1,"layers":[{"elements":[{"type":"rect","rect":[0,0,10,10],"groups":{{groups}}}]}]}""";

        BinkDocument loaded = BinkSerializer.Deserialize(Doc($"""["{id}", 5, "nope", "{id}", "{Guid.Empty}"]"""));
        CollectionAssert.AreEqual(new[] { id }, loaded.AllElements.Single().GroupIds.ToArray());

        string deep = "[" + string.Join(",", Enumerable.Range(0, BinkElement.MaxGroupDepth + 1).Select(_ => $"\"{Guid.NewGuid()}\"")) + "]";
        Assert.IsFalse(BinkSerializer.TryDeserialize(Doc(deep), out _, out string? error));
        Assert.Contains("groups", error!);
    }

    [TestMethod]
    public void The_group_list_cannot_be_changed_after_the_element_is_made()
    {
        var groups = new List<Guid> { Guid.NewGuid() };
        var shape = new BinkRectangleShape(new BinkRect(0, 0, 10, 10)) { GroupIds = groups };
        groups.Add(Guid.NewGuid());
        Assert.HasCount(1, shape.GroupIds);
        Assert.ThrowsExactly<ArgumentException>(() => shape with { GroupIds = Enumerable.Range(0, BinkElement.MaxGroupDepth + 1).Select(_ => Guid.NewGuid()).ToArray() });
    }

    // ---- Locking ---------------------------------------------------------------------------------------------------

    [TestMethod]
    public void A_locked_element_is_drawn_but_cannot_be_picked_or_selected()
    {
        (BinkDocument document, List<Guid> ids) = Row(2);
        using var history = new BinkHistoryManager(document);
        using var selection = new BinkSelection(document);
        selection.Set(ids);

        Assert.IsTrue(document.SetLocked([ids[0]], true));
        Assert.AreEqual(1, history.UndoCount);
        CollectionAssert.AreEqual(new[] { ids[1] }, selection.Ids.ToArray(), "Locking drops the element from the selection.");

        Assert.IsNull(BinkDocumentQuery.HitTest(document, new Vector2(30, 30)));
        Assert.AreEqual(ids[0], BinkDocumentQuery.HitTest(document, new Vector2(30, 30), includeLocked: true)?.Id);
        Assert.IsFalse(BinkDocumentQuery.FindInRect(document, new BinkRect(-10, -10, 500, 100)).Any(e => e.Id == ids[0]));
        selection.Set(ids[0]);
        Assert.IsTrue(selection.IsEmpty);
        selection.SelectAll();
        CollectionAssert.AreEqual(new[] { ids[1] }, selection.Ids.ToArray());

        Assert.IsFalse(document.SetLocked([ids[0]], true), "Already locked.");
        Assert.IsTrue(history.Undo());
        Assert.IsFalse(document.GetElement(ids[0]).IsLocked);
    }

    [TestMethod]
    public void The_stroke_and_segment_erasers_leave_locked_ink_alone()
    {
        foreach (BinkEraserMode mode in new[] { BinkEraserMode.Stroke, BinkEraserMode.Segment })
        {
            using var context = new TestToolContext();
            BinkStroke stroke = TestData.Stroke(new Vector2(100, 100), new Vector2(300, 100));
            context.Document.AddElement(stroke);
            context.Document.SetLocked([stroke.Id], true);

            context.Line(new BinkEraserTool { Mode = mode, Radius = 10f }, new Vector2(200, 50), new Vector2(200, 150));

            Assert.AreEqual(1, context.Document.ElementCount, mode.ToString());
            Assert.AreSame(stroke.Points, ((BinkStroke)context.Document.GetElement(stroke.Id)).Points, mode + ": the stroke is untouched.");
        }
    }

    [TestMethod]
    public void A_group_with_a_locked_member_is_whole_to_what_is_done_with_its_selection()
    {
        (BinkDocument document, List<Guid> ids) = Row(3);
        Guid group = document.GroupElements(ids)!.Value;
        document.SetLocked([ids[2]], true);
        // What clicking the group selects: a locked member never is.
        var selection = new BinkSelection(document);
        selection.Set(BinkDocumentQuery.ExpandToGroups(document, [ids[0]]));
        Guid[] selected = [.. selection.Ids];
        CollectionAssert.AreEquivalent(new[] { ids[0], ids[1] }, selected);

        IReadOnlyList<BinkElement> copies = document.DuplicateElements(selected, new Vector2(0, 200));
        Assert.AreEqual(copies[0].GroupIds.Single(), copies[1].GroupIds.Single(), "The copies are a group of their own.");
        Assert.AreNotEqual(group, copies[0].GroupIds.Single(), "Clicking a copy must not select the originals.");

        float before = document.GetElement(ids[1]).Bounds.Left;
        Assert.IsFalse(document.AlignElements(selected, BinkAlignment.Left), "The group is one unit, so there is nothing to line up.");
        Assert.AreEqual(before, document.GetElement(ids[1]).Bounds.Left);

        Assert.IsTrue(document.UngroupElements(selected));
        Assert.IsTrue(ids.All(id => Groups(document, id).Count == 0), "The locked member leaves the dissolved group too.");
        Assert.IsTrue(document.GetElement(ids[2]).IsLocked);
    }

    [TestMethod]
    public void A_group_chain_a_document_could_not_read_back_the_same_is_refused()
    {
        var shape = new BinkRectangleShape(new BinkRect(0, 0, 10, 10));
        var group = Guid.NewGuid();
        Assert.ThrowsExactly<ArgumentException>(() => shape with { GroupIds = [Guid.Empty] });
        Assert.ThrowsExactly<ArgumentException>(() => shape with { GroupIds = [group, Guid.NewGuid(), group] });
        Assert.HasCount(2, (shape with { GroupIds = [group, Guid.NewGuid()] }).GroupIds);
    }

    [TestMethod]
    public void Grouping_counts_every_member_so_a_new_group_never_splits_one()
    {
        (BinkDocument document, List<Guid> ids) = Row(3);
        Guid outer = document.GroupElements(ids)!.Value;
        document.SetLocked([ids[2]], true);

        // The two selectable members, grouped while the third is locked: the new group goes inside the old one.
        Guid inner = document.GroupElements([ids[0], ids[1]])!.Value;

        CollectionAssert.AreEqual(new[] { inner, outer }, Groups(document, ids[0]).ToArray());
        CollectionAssert.AreEqual(new[] { outer }, Groups(document, ids[2]).ToArray());
    }

    [TestMethod]
    public void Elements_of_unknown_types_are_not_locked_or_ungrouped_since_they_save_as_read()
    {
        string json = BinkSerializer.Serialize(new BinkDocument());
        int at = json.IndexOf("\"elements\":[", StringComparison.Ordinal);
        json = json.Insert(at + "\"elements\":[".Length, """{"type":"future-shape","id":"9f1d4d1e-0000-4000-8000-000000000001","locked":true,"groups":["9f1d4d1e-0000-4000-8000-000000000002"]}""");
        BinkDocument document = BinkSerializer.Deserialize(json);
        BinkElement unknown = document.AllElements.Single();
        Assert.IsInstanceOfType<BinkUnknownElement>(unknown);
        Assert.HasCount(1, unknown.GroupIds);

        // Its saved form is the text it was read from, so a change to it would be lost on save and on the wire.
        Assert.IsFalse(document.SetLocked([unknown.Id], false));
        Assert.IsFalse(document.UngroupElements([unknown.Id]));
        Assert.AreSame(unknown, document.GetElement(unknown.Id));
    }
}

/// <summary>Grouping and locking through the select tool.</summary>
[TestClass]
public sealed class GroupSelectToolTests
{
    private static TestToolContext WithGroup(out List<Guid> ids, out Guid inner, out Guid outer)
    {
        var context = new TestToolContext();
        ids = [];
        for (int i = 0; i < 4; i++)
        {
            var shape = new BinkRectangleShape(new BinkRect(100 + i * 100, 100, 60, 60)) { Style = new BinkShapeStyle { FillColor = BinkColor.White } };
            context.Document.AddElement(shape);
            ids.Add(shape.Id);
        }
        inner = context.Document.GroupElements([ids[0], ids[1]])!.Value;
        outer = context.Document.GroupElements([ids[0], ids[1], ids[2]])!.Value;
        context.History.Clear();
        return context;
    }

    private static BinkInputSample Sample(BinkPointerPhase phase, float x, float y, double time) => new()
    {
        PointerId = 1,
        Kind = BinkPointerKind.Mouse,
        Phase = phase,
        X = x,
        Y = y,
        Pressure = 0.5f,
        Timestamp = time,
        Buttons = phase == BinkPointerPhase.Up ? 0 : 1,
        IsPrimary = true,
    };

    private static void Click(TestToolContext context, IBinkTool tool, float x, float y, double time)
    {
        tool.OnPointerDown(context, Sample(BinkPointerPhase.Down, x, y, time));
        tool.OnPointerUp(context, Sample(BinkPointerPhase.Up, x, y, time + 50));
    }

    [TestMethod]
    public void Clicking_a_member_selects_the_whole_group_and_dragging_moves_it()
    {
        using TestToolContext context = WithGroup(out List<Guid> ids, out _, out _);
        var tool = new BinkSelectTool();

        Click(context, tool, 130, 130, 1000);
        CollectionAssert.AreEquivalent(new[] { ids[0], ids[1], ids[2] }, context.Selection.Ids.ToArray());
        Assert.Contains("group of 3 elements", context.Announcements[^1]);

        context.Line(tool, new Vector2(330, 130), new Vector2(330, 230));
        foreach (Guid id in ids.Take(3)) Assert.AreEqual(199f, context.Document.GetElement(id).Bounds.Top, 2f);
        Assert.AreEqual(99f, context.Document.GetElement(ids[3]).Bounds.Top, 2f, "The element outside the group stays.");
    }

    [TestMethod]
    public void Double_clicking_enters_the_group_and_Escape_leaves_it()
    {
        using TestToolContext context = WithGroup(out List<Guid> ids, out Guid inner, out Guid outer);
        var tool = new BinkSelectTool();

        Click(context, tool, 130, 130, 1000);
        Click(context, tool, 130, 130, 1200);
        Assert.AreEqual(outer, tool.FocusedGroup);
        CollectionAssert.AreEquivalent(new[] { ids[0], ids[1] }, context.Selection.Ids.ToArray(), "One level down is the inner group.");

        Click(context, tool, 130, 130, 1400);
        Click(context, tool, 130, 130, 1600);
        Assert.AreEqual(inner, tool.FocusedGroup);
        CollectionAssert.AreEqual(new[] { ids[0] }, context.Selection.Ids.ToArray());

        // Inside the outer group, a click on its other member picks that member alone.
        Assert.IsTrue(tool.OnKeyDown(context, new BinkToolKeyEvent("Escape", false, false, false)));
        Assert.AreEqual(outer, tool.FocusedGroup);
        CollectionAssert.AreEquivalent(new[] { ids[0], ids[1] }, context.Selection.Ids.ToArray());
        Click(context, tool, 330, 130, 3000);
        CollectionAssert.AreEqual(new[] { ids[2] }, context.Selection.Ids.ToArray());

        Assert.IsTrue(tool.OnKeyDown(context, new BinkToolKeyEvent("Escape", false, false, false)));
        Assert.IsNull(tool.FocusedGroup);
        CollectionAssert.AreEquivalent(new[] { ids[0], ids[1], ids[2] }, context.Selection.Ids.ToArray());
    }

    [TestMethod]
    public void Two_slow_clicks_do_not_enter_the_group()
    {
        using TestToolContext context = WithGroup(out List<Guid> ids, out _, out _);
        var tool = new BinkSelectTool();
        Click(context, tool, 130, 130, 1000);
        Click(context, tool, 130, 130, 3000);
        Assert.IsNull(tool.FocusedGroup);
        Assert.AreEqual(3, context.Selection.Count);
    }

    [TestMethod]
    public void Clicking_outside_the_group_being_worked_in_leaves_it()
    {
        using TestToolContext context = WithGroup(out List<Guid> ids, out _, out _);
        var tool = new BinkSelectTool();
        Click(context, tool, 130, 130, 1000);
        Click(context, tool, 130, 130, 1200);
        Assert.IsNotNull(tool.FocusedGroup);

        Click(context, tool, 430, 130, 5000);
        Assert.IsNull(tool.FocusedGroup);
        CollectionAssert.AreEqual(new[] { ids[3] }, context.Selection.Ids.ToArray());
    }

    [TestMethod]
    public void A_marquee_over_part_of_a_group_selects_the_group()
    {
        using TestToolContext context = WithGroup(out List<Guid> ids, out _, out _);
        var tool = new BinkSelectTool();
        context.Line(tool, new Vector2(80, 80), new Vector2(180, 180));
        CollectionAssert.AreEquivalent(new[] { ids[0], ids[1], ids[2] }, context.Selection.Ids.ToArray());
    }

    [TestMethod]
    public void Shift_clicking_a_member_toggles_the_whole_group()
    {
        using TestToolContext context = WithGroup(out List<Guid> ids, out _, out _);
        var tool = new BinkSelectTool();
        context.Selection.Set(ids[3]);
        tool.OnPointerDown(context, Sample(BinkPointerPhase.Down, 230, 130, 1000) with { Shift = true });
        tool.OnPointerUp(context, Sample(BinkPointerPhase.Up, 230, 130, 1050) with { Shift = true });
        Assert.AreEqual(4, context.Selection.Count);

        tool.OnPointerDown(context, Sample(BinkPointerPhase.Down, 230, 130, 5000) with { Shift = true });
        tool.OnPointerUp(context, Sample(BinkPointerPhase.Up, 230, 130, 5050) with { Shift = true });
        CollectionAssert.AreEqual(new[] { ids[3] }, context.Selection.Ids.ToArray());
        Assert.StartsWith("Deselected group of 3 elements", context.Announcements[^1]);
    }

    [TestMethod]
    public void Tab_steps_through_a_group_as_one_stop()
    {
        using TestToolContext context = WithGroup(out List<Guid> ids, out _, out _);
        var tool = new BinkSelectTool();
        Assert.IsTrue(tool.OnKeyDown(context, new BinkToolKeyEvent("Enter", false, false, false)));
        Assert.AreEqual(3, context.Selection.Count);
        Assert.Contains("1 of 2: group of 3 elements", context.Announcements[^1]);
        Assert.IsTrue(tool.OnKeyDown(context, new BinkToolKeyEvent("Tab", false, false, false)));
        CollectionAssert.AreEqual(new[] { ids[3] }, context.Selection.Ids.ToArray());
        Assert.IsTrue(tool.OnKeyDown(context, new BinkToolKeyEvent("Tab", false, false, false)));
        Assert.AreEqual(3, context.Selection.Count, "Wraps around to the group.");
    }

    [TestMethod]
    public void A_locked_element_is_not_picked_by_a_click()
    {
        using TestToolContext context = WithGroup(out List<Guid> ids, out _, out _);
        context.Document.SetLocked([ids[3]], true);
        var tool = new BinkSelectTool();
        Click(context, tool, 430, 130, 1000);
        Assert.IsTrue(context.Selection.IsEmpty);
    }

    [TestMethod]
    public void Alt_dragging_a_group_moves_a_copy_that_is_a_group_of_its_own()
    {
        using TestToolContext context = WithGroup(out List<Guid> ids, out _, out Guid outer);
        var tool = new BinkSelectTool();
        Click(context, tool, 130, 130, 1000);

        tool.OnPointerDown(context, Sample(BinkPointerPhase.Down, 130, 130, 5000) with { Alt = true });
        tool.OnPointerMove(context, Sample(BinkPointerPhase.Move, 130, 330, 5016) with { Alt = true });
        tool.OnPointerUp(context, Sample(BinkPointerPhase.Up, 130, 330, 5032) with { Alt = true });

        Assert.AreEqual(7, context.Document.ElementCount);
        var copies = context.Selection.Elements.ToList();
        Assert.HasCount(3, copies);
        Guid copyGroup = copies[0].GroupIds[^1];
        Assert.AreNotEqual(outer, copyGroup);
        Assert.IsTrue(copies.All(c => c.GroupIds[^1] == copyGroup));
    }
}

/// <summary>The group and lock commands of the canvas, as the browser layer drives them.</summary>
[TestClass]
public sealed class BinkCanvasGroupingTests : BunitContext
{
    public BinkCanvasGroupingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static void Key(IRenderedComponent<BinkCanvas> canvas, string key, bool ctrl = false, bool shift = false, bool alt = false)
        => canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnKey(key, ctrl, shift, alt));

    private (IRenderedComponent<BinkCanvas> Canvas, List<Guid> Ids) WithShapes(int count)
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.Tool, new BinkSelectTool()));
        var ids = new List<Guid>();
        canvas.InvokeAsync(() =>
        {
            for (int i = 0; i < count; i++)
            {
                var shape = new BinkRectangleShape(new BinkRect(20 + i * 40, 20, 30, 30));
                canvas.Instance.CurrentDocument.AddElement(shape);
                ids.Add(shape.Id);
            }
        });
        return (canvas, ids);
    }

    [TestMethod]
    public void Ctrl_G_groups_the_selection_and_Ctrl_Shift_G_ungroups_it()
    {
        (IRenderedComponent<BinkCanvas> canvas, List<Guid> ids) = WithShapes(3);
        BinkDocument document = canvas.Instance.CurrentDocument;
        canvas.InvokeAsync(() => canvas.Instance.Selection.Set(ids.Take(2)));

        Key(canvas, "g", ctrl: true);
        Guid group = document.GetElement(ids[0]).GroupIds.Single();
        Assert.AreEqual(group, document.GetElement(ids[1]).GroupIds.Single());
        Assert.AreEqual(2, canvas.Instance.Selection.Count, "The grouped elements stay selected.");

        Key(canvas, "G", ctrl: true, shift: true);
        Assert.IsTrue(ids.All(id => document.GetElement(id).GroupIds.Count == 0));

        canvas.InvokeAsync(() => canvas.Instance.Undo());
        Assert.AreEqual(group, document.GetElement(ids[0]).GroupIds.Single());
    }

    [TestMethod]
    public void Ctrl_Shift_L_locks_the_selection_and_UnlockAll_unlocks_everything()
    {
        (IRenderedComponent<BinkCanvas> canvas, List<Guid> ids) = WithShapes(3);
        BinkDocument document = canvas.Instance.CurrentDocument;
        canvas.InvokeAsync(() => canvas.Instance.Selection.Set(ids.Take(2)));

        Key(canvas, "L", ctrl: true, shift: true);
        Assert.IsTrue(document.GetElement(ids[0]).IsLocked);
        Assert.IsTrue(document.GetElement(ids[1]).IsLocked);
        Assert.IsTrue(canvas.Instance.Selection.IsEmpty);

        Key(canvas, "l", ctrl: true);
        Key(canvas, "a", ctrl: true);
        CollectionAssert.AreEqual(new[] { ids[2] }, canvas.Instance.Selection.Ids.ToArray(), "Select all skips locked elements.");

        int unlocked = 0;
        canvas.InvokeAsync(() => unlocked = canvas.Instance.UnlockAll());
        Assert.AreEqual(2, unlocked);
        Assert.IsFalse(document.AllElements.Any(e => e.IsLocked));
    }

    [TestMethod]
    public void A_read_only_canvas_neither_groups_nor_locks()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.ReadOnly, true));
        var a = new BinkRectangleShape(new BinkRect(0, 0, 10, 10));
        var b = new BinkRectangleShape(new BinkRect(20, 0, 10, 10));
        canvas.InvokeAsync(() =>
        {
            canvas.Instance.CurrentDocument.AddElement(a);
            canvas.Instance.CurrentDocument.AddElement(b);
            canvas.Instance.Selection.Set([a.Id, b.Id]);
        });
        Guid? group = null;
        int locked = 0;
        canvas.InvokeAsync(() =>
        {
            group = canvas.Instance.GroupSelection();
            locked = canvas.Instance.LockSelection();
        });
        Assert.IsNull(group);
        Assert.AreEqual(0, locked);
    }
}
