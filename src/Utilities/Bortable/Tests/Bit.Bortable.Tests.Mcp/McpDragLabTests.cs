using System.Text.Json;
using System.Text.Json.Nodes;
using Bit.Bortable.Demo.Server.Dtos;

namespace Bit.Bortable.Tests.Mcp;

/// <summary>
/// InspectBortableDrag on the drags an application actually meets: a reorder, a move across lists,
/// a clone, a swap and a multi-drag, every refusal the component knows, a release outside any list,
/// and input that is not a drag at all. The lists are built as JSON here, so each test changes
/// exactly the one thing it is about - and every answer checked is one the real component gave.
/// </summary>
[TestClass]
public class McpDragLabTests
{
    // ---- The sample --------------------------------------------------------

    [TestMethod]
    public async Task Without_json_it_runs_a_sample_and_hands_out_its_description()
    {
        var report = await InspectAsync(null);

        Assert.IsTrue(report.Valid, report.Error);
        Assert.IsNotNull(report.Sample);

        var sample = JsonSerializer.Deserialize<BortableDragScenarioDto>(report.Sample, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        Assert.HasCount(2, sample.Lists!);
        Assert.AreEqual("move", report.Kind);
        Assert.IsTrue(report.Changed);
        CollectionAssert.AreEqual(new[] { "Review the PR", "Set up CI" }, List(report, "done").After);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("the sample in Sample was run", StringComparison.Ordinal)));
    }

    // ---- Within a list -------------------------------------------------------

    [TestMethod]
    public async Task A_reorder_moves_the_item_live_and_raises_update_sort_and_end()
    {
        var report = await InspectAsync(Scenario([List("a", ["A", "B", "C", "D"])], Drag("A", index: 2)));

        Assert.AreEqual("reorder", report.Kind);
        CollectionAssert.AreEqual(new[] { "B", "C", "A", "D" }, List(report, "a").After);
        Assert.AreEqual(1, List(report, "a").ItemsChanged);
        Assert.AreEqual("OnStart:a OnChange:a ItemsChanged:a OnUpdate:a OnSort:a OnEnd:a", Sequence(report));

        var update = Event(report, "OnUpdate", "a");

        Assert.AreEqual("A", update.Item);
        Assert.AreEqual(0, update.OldIndex);
        Assert.AreEqual(2, update.NewIndex);
        Assert.IsFalse(update.CrossedLists);

        // dropIndex reports a gap counted before the item leaves its place: landing at 2 is the gap
        // after the third item, 3.
        var dragover = Step(report, "dragover");

        Assert.IsTrue(dragover.Accepted);
        CollectionAssert.Contains(dragover.Browser!, "dropIndex('a') -> 3");
    }

    [TestMethod]
    public async Task A_reorder_upwards_asks_for_the_gap_it_lands_in()
    {
        var report = await InspectAsync(Scenario([List("a", ["A", "B", "C", "D"])], Drag("D", index: 0)));

        CollectionAssert.AreEqual(new[] { "D", "A", "B", "C" }, List(report, "a").After);
        CollectionAssert.Contains(Step(report, "dragover").Browser!, "dropIndex('a') -> 0");
    }

    [TestMethod]
    public async Task The_dragged_item_renders_as_ghost_and_chosen()
    {
        var report = await InspectAsync(Scenario([List("a", ["A", "B"])], Drag("A", index: 1)));

        var picked = Step(report, "dragstart").Rendered!.Single(list => list.List == "a").Items.Single(item => item.Item == "A");

        Assert.AreEqual("bit-bortable-ghost bit-bortable-chosen", picked.Classes);
    }

    [TestMethod]
    public async Task A_drag_released_where_it_started_raises_only_start_and_end()
    {
        var report = await InspectAsync(Scenario([List("a", ["A", "B", "C"])], Drag("B")));

        Assert.IsFalse(report.Changed);
        Assert.AreEqual("OnStart:a OnEnd:a", Sequence(report));
        Assert.IsFalse(report.Steps!.Any(step => step.Event == "dragover"), "The pointer never left the item.");
        Assert.Contains("released where it started", report.Outcome!);
    }

    [TestMethod]
    public async Task A_list_that_does_not_sort_keeps_its_order()
    {
        var report = await InspectAsync(Scenario([List("a", ["A", "B", "C"], sort: false)], Drag("A", index: 2)));

        Assert.IsFalse(report.Changed);
        Assert.IsFalse(Step(report, "dragover").Accepted);
        Assert.Contains("Sort=\"false\"", Step(report, "dragover").Outcome);
    }

    // ---- Across lists -------------------------------------------------------

    [TestMethod]
    public async Task A_move_across_lists_previews_hides_and_commits_on_drop()
    {
        var report = await InspectAsync(Scenario(
            [List("todo", ["A", "B"], group: "board"), List("done", ["X"], group: "board")],
            Drag("A", to: "done", index: 1)));

        Assert.AreEqual("move", report.Kind);
        CollectionAssert.AreEqual(new[] { "B" }, List(report, "todo").After);
        CollectionAssert.AreEqual(new[] { "X", "A" }, List(report, "done").After);
        Assert.AreEqual("OnStart:todo OnChange:done ItemsChanged:todo ItemsChanged:done OnRemove:todo OnAdd:done OnSort:todo OnSort:done OnEnd:todo", Sequence(report));

        // While the pointer is over 'done', the item is only previewed there; its original hides.
        var dragover = Step(report, "dragover");
        var source = dragover.Rendered!.Single(list => list.List == "todo").Items.Single(item => item.Item == "A");
        var placeholder = dragover.Rendered!.Single(list => list.List == "done").Items.Single(item => item.Placeholder is true);

        Assert.AreEqual("bit-bortable-hidden", source.Classes);
        Assert.AreEqual("A", placeholder.Item);
        Assert.AreEqual(1, Array.IndexOf(dragover.Rendered!.Single(list => list.List == "done").Items, placeholder));

        var add = Event(report, "OnAdd", "done");

        Assert.AreEqual("todo", add.FromId);
        Assert.AreEqual("done", add.ToId);
        Assert.AreEqual("board", add.FromGroup);
        Assert.AreEqual(0, add.OldIndex);
        Assert.AreEqual(1, add.NewIndex);
        Assert.IsTrue(add.CrossedLists);
    }

    [TestMethod]
    public async Task Into_another_list_without_an_index_the_item_lands_at_the_end()
    {
        var report = await InspectAsync(Scenario(
            [List("a", ["A"], group: "g"), List("b", ["X", "Y"], group: "g")],
            Drag("A", to: "b")));

        CollectionAssert.AreEqual(new[] { "X", "Y", "A" }, List(report, "b").After);
    }

    [TestMethod]
    public async Task The_trailing_dragend_after_a_drop_finds_nothing_left_to_do()
    {
        var report = await InspectAsync(Scenario([List("a", ["A", "B"])], Drag("A", index: 1)));

        var dragend = report.Steps!.Last();

        Assert.AreEqual("dragend", dragend.Event);
        Assert.IsFalse(dragend.Accepted);
        Assert.Contains("Whichever of drop and dragend comes first completes it", dragend.Outcome);
        Assert.AreEqual(1, report.Events!.Count(e => e.Event == "OnEnd"));
    }

    [TestMethod]
    public async Task Released_outside_any_list_a_previewed_item_still_lands()
    {
        var report = await InspectAsync(Scenario(
            [List("a", ["A", "B"], group: "g"), List("b", ["X"], group: "g")],
            Drag("A", to: "b", index: 0, release: "outside")));

        CollectionAssert.AreEqual(new[] { "A", "X" }, List(report, "b").After);
        Assert.IsFalse(report.Steps!.Any(step => step.Event == "drop"), "A release outside any list fires no drop.");
        Assert.IsTrue(Step(report, "dragend").Accepted);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("as in SortableJS", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Escape_ends_the_drag_like_a_release_outside()
    {
        var report = await InspectAsync(Scenario(
            [List("a", ["A", "B"])],
            Drag("A", index: 1, release: "cancel")));

        CollectionAssert.AreEqual(new[] { "B", "A" }, List(report, "a").After);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("Escape", StringComparison.Ordinal)));
    }

    // ---- Clone ----------------------------------------------------------------

    [TestMethod]
    public async Task A_clone_leaves_the_original_and_adds_the_copy_the_factory_makes()
    {
        var report = await InspectAsync(Scenario(
            [List("palette", ["Text", "Image"], group: "form", pull: "clone", cloneSuffix: " (copy)"), List("form", ["Title"], group: "form")],
            Drag("Text", to: "form", index: 1)));

        Assert.AreEqual("clone", report.Kind);
        Assert.IsFalse(List(report, "palette").Changed);
        CollectionAssert.AreEqual(new[] { "Title", "Text (copy)" }, List(report, "form").After);
        Assert.AreEqual("OnStart:palette OnChange:form ItemsChanged:form OnAdd:form OnSort:form OnEnd:palette", Sequence(report), "A clone raises no OnRemove, and the source does not sort.");
        Assert.AreEqual("Text (copy)", Event(report, "OnAdd", "form").Item, "The events report the copy.");

        // The original stays visible while it is cloned: no bit-bortable-hidden.
        var source = Step(report, "dragover").Rendered?.SingleOrDefault(list => list.List == "palette");
        Assert.IsTrue(source is null || source.Items.All(item => item.Classes?.Contains("bit-bortable-hidden", StringComparison.Ordinal) is not true));
    }

    [TestMethod]
    public async Task A_string_cloned_without_a_factory_is_shared_and_cannot_be_cloned_twice()
    {
        var once = await InspectAsync(Scenario(
            [List("palette", ["Text"], group: "form", pull: "clone"), List("form", [], group: "form")],
            Drag("Text", to: "form")));

        CollectionAssert.AreEqual(new[] { "Text" }, List(once, "form").After);
        Assert.IsTrue(once.Notes.Any(note => note.Contains("ICloneable.Clone()", StringComparison.Ordinal)));

        var twice = await InspectAsync(Scenario(
            [List("palette", ["Text"], group: "form", pull: "clone"), List("form", ["Text"], group: "form")],
            Drag("Text", from: "palette", to: "form")));

        Assert.IsFalse(twice.Changed);
        Assert.Contains("already holds 'Text'", Step(twice, "dragover").Outcome);
    }

    [TestMethod]
    public async Task A_copy_equal_to_an_item_of_the_target_is_refused_at_the_drop()
    {
        var report = await InspectAsync(Scenario(
            [List("palette", ["Text"], group: "form", pull: "clone", cloneSuffix: "!"), List("form", ["Text!"], group: "form")],
            Drag("Text", to: "form")));

        Assert.IsTrue(Step(report, "dragover").Accepted, "The target does not hold the original, so it previews it.");
        Assert.IsFalse(report.Changed);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("cancelled at the last moment", StringComparison.Ordinal) && note.Contains("'Text!'", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_read_only_list_is_only_ever_cloned_from()
    {
        var move = await InspectAsync(Scenario(
            [List("palette", ["Text"], group: "g", readOnly: true), List("form", [], group: "g")],
            Drag("Text", to: "form")));
        var clone = await InspectAsync(Scenario(
            [List("palette", ["Text"], group: "g", readOnly: true, pull: "clone"), List("form", [], group: "g")],
            Drag("Text", to: "form")));

        Assert.IsFalse(move.Changed);
        Assert.Contains("read-only list", Step(move, "dragover").Outcome);
        Assert.IsTrue(clone.Changed);
        Assert.AreEqual(0, List(clone, "palette").ItemsChanged);
        Assert.Contains("Items=\"_palette\"", clone.Markup!, "A read-only list is bound one way: it never changes.");
    }

    // ---- Swap -------------------------------------------------------------------

    [TestMethod]
    public async Task A_swap_highlights_the_target_and_trades_places_on_drop()
    {
        var report = await InspectAsync(Scenario([List("s", ["A", "B", "C"], swap: true)], Drag("A", swapWith: "C")));

        Assert.AreEqual("swap", report.Kind);

        var dragover = Step(report, "dragover");

        Assert.IsTrue(dragover.Accepted);
        CollectionAssert.Contains(dragover.Browser!, "itemIndexAt('s') -> 2");
        Assert.AreEqual("bit-bortable-swap-highlight", dragover.Rendered!.Single().Items.Single(item => item.Item == "C").Classes);

        CollectionAssert.AreEqual(new[] { "C", "B", "A" }, List(report, "s").After);
        Assert.AreEqual("OnStart:s ItemsChanged:s OnUpdate:s OnSort:s OnEnd:s", Sequence(report), "A swap raises no OnChange: nothing moves until the drop.");
    }

    [TestMethod]
    public async Task A_swap_across_lists_moves_an_item_each_way()
    {
        var report = await InspectAsync(Scenario(
            [List("l", ["A", "B"], group: "g", swap: true), List("r", ["X", "Y"], group: "g", swap: true)],
            Drag("A", to: "r", swapWith: "Y")));

        CollectionAssert.AreEqual(new[] { "Y", "B" }, List(report, "l").After);
        CollectionAssert.AreEqual(new[] { "X", "A" }, List(report, "r").After);
        Assert.AreEqual("OnStart:l ItemsChanged:l ItemsChanged:r OnRemove:l OnAdd:r OnSort:l OnSort:r OnEnd:l", Sequence(report));
    }

    [TestMethod]
    public async Task A_swap_needs_both_lists_to_take_the_other_item()
    {
        var report = await InspectAsync(Scenario(
            [List("l", ["A"], group: "g", swap: true, put: false), List("r", ["X"], group: "g", swap: true)],
            Drag("A", to: "r", swapWith: "X")));

        Assert.IsFalse(report.Changed);
        Assert.Contains("A swap sends 'X' into 'l' in exchange", Step(report, "dragover").Outcome);
        Assert.Contains("Put=\"false\"", Step(report, "dragover").Outcome);
    }

    [TestMethod]
    public async Task An_item_is_not_swapped_with_itself()
    {
        var report = await InspectAsync(Scenario([List("s", ["A", "B"], swap: true)], Drag("A", swapWith: "A")));

        Assert.IsFalse(report.Changed);
        Assert.Contains("cannot be swapped with itself", Step(report, "dragover").Outcome);
    }

    // ---- Multi-drag ---------------------------------------------------------------

    [TestMethod]
    public async Task A_multi_drag_selects_by_click_and_moves_the_selection_as_one_block()
    {
        var report = await InspectAsync(Scenario(
            [List("m", ["A", "B", "C", "D", "E"], multiDrag: true)],
            Drag("B", selected: ["B", "D"], index: 4)));

        Assert.AreEqual("multi-drag reorder", report.Kind);
        Assert.IsTrue(report.Steps!.Where(step => step.Event == "click").All(step => step.Accepted));
        CollectionAssert.AreEqual(new[] { "A", "C", "E", "B", "D" }, List(report, "m").After);
        CollectionAssert.AreEqual(new[] { "B", "D" }, Event(report, "OnUpdate", "m").Items);
        Assert.Contains("carries the selection of 2", Step(report, "dragstart").Outcome);
    }

    [TestMethod]
    public async Task A_multi_drag_into_another_list_brings_the_whole_selection()
    {
        var report = await InspectAsync(Scenario(
            [List("m", ["A", "B", "C", "D"], group: "g", multiDrag: true), List("t", ["X", "Y"], group: "g")],
            Drag("C", selected: ["A", "C"], to: "t", index: 1)));

        CollectionAssert.AreEqual(new[] { "B", "D" }, List(report, "m").After);
        CollectionAssert.AreEqual(new[] { "X", "A", "C", "Y" }, List(report, "t").After);
        CollectionAssert.AreEqual(new[] { "A", "C" }, Event(report, "OnAdd", "t").Items);
    }

    [TestMethod]
    public async Task Dragging_an_unselected_item_clears_the_selection_and_moves_it_alone()
    {
        var report = await InspectAsync(Scenario(
            [List("m", ["A", "B", "C", "D"], multiDrag: true)],
            Drag("A", selected: ["C", "D"], index: 3)));

        Assert.AreEqual("reorder", report.Kind);
        CollectionAssert.AreEqual(new[] { "B", "C", "D", "A" }, List(report, "m").After);
        Assert.Contains("cleared the selection", Step(report, "dragstart").Outcome);
    }

    [TestMethod]
    public async Task A_click_selects_nothing_without_multi_drag_or_on_a_filtered_item()
    {
        var plain = await InspectAsync(Scenario([List("m", ["A", "B"])], Drag("A", selected: ["A", "B"], index: 1)));
        var filtered = await InspectAsync(Scenario([List("m", ["A", "B"], multiDrag: true, filter: ["B"])], Drag("A", selected: ["B"], index: 1)));

        Assert.IsTrue(plain.Steps!.Where(step => step.Event == "click").All(step => step.Accepted is false));
        Assert.Contains("does not have MultiDrag", plain.Steps!.First().Outcome);
        Assert.IsFalse(filtered.Steps!.First().Accepted);
        Assert.Contains("excluded by the Filter", filtered.Steps!.First().Outcome);
    }

    // ---- Refusals --------------------------------------------------------------------

    [TestMethod]
    public async Task Every_refusal_of_another_list_is_explained()
    {
        (string Name, object Source, object Target, string Expected)[] cases =
        [
            ("no group", List("a", ["A"]), List("b", []), "no Group"),
            ("different groups", List("a", ["A"], group: "x"), List("b", [], group: "y"), "only lists with the same Group"),
            ("groups differing by case", List("a", ["A"], group: "Board"), List("b", [], group: "board"), "case-sensitive"),
            ("put false", List("a", ["A"], group: "g"), List("b", [], group: "g", put: false), "Put=\"false\""),
            ("pull none", List("a", ["A"], group: "g", pull: "none"), List("b", [], group: "g"), "Pull=\"BortablePull.None\""),
            ("disabled target", List("a", ["A"], group: "g"), List("b", [], group: "g", disabled: true), "'b' is Disabled"),
            ("read-only target", List("a", ["A"], group: "g"), List("b", [], group: "g", readOnly: true), "nothing is ever dropped into it"),
            ("duplicate", List("a", ["A"], group: "g"), List("b", ["A"], group: "g"), "already holds 'A'"),
        ];

        foreach (var (name, source, target, expected) in cases)
        {
            var report = await InspectAsync(Scenario([source, target], Drag("A", from: "a", to: "b")));

            Assert.IsTrue(report.Valid, $"{name}: {report.Error}");
            Assert.IsFalse(report.Changed, name);
            Assert.IsFalse(Step(report, "dragover").Accepted, name);
            Assert.Contains(expected, Step(report, "dragover").Outcome, name);
            Assert.Contains(expected, report.Outcome!, name);
            Assert.AreEqual("OnStart:a OnEnd:a", Sequence(report), name);
        }
    }

    [TestMethod]
    public async Task A_disabled_list_or_a_filtered_item_never_starts_a_drag()
    {
        var disabled = await InspectAsync(Scenario([List("a", ["A", "B"], disabled: true)], Drag("A", index: 1)));
        var filtered = await InspectAsync(Scenario([List("a", ["A", "B"], filter: ["A"])], Drag("A", index: 1)));

        foreach (var report in new[] { disabled, filtered })
        {
            Assert.IsFalse(Step(report, "dragstart").Accepted);
            Assert.Contains("draggable=\"false\"", Step(report, "dragstart").Outcome);
            Assert.IsEmpty(report.Events!);
            Assert.HasCount(1, report.Steps!, "Without a drag there is nothing to carry anywhere.");
        }

        Assert.Contains("Disabled", disabled.Outcome!);
        Assert.Contains("Filter", filtered.Outcome!);
    }

    [TestMethod]
    public async Task A_refused_multi_drag_still_gathers_its_selection_in_its_own_list()
    {
        var report = await InspectAsync(Scenario(
            [List("m", ["A", "B", "C", "D"], group: "g", multiDrag: true), List("t", ["A2", "A"], group: "g")],
            Drag("C", selected: ["A", "C"], to: "t", index: 1)));

        Assert.IsFalse(Step(report, "dragover").Accepted);
        Assert.IsFalse(List(report, "t").Changed);
        CollectionAssert.AreEqual(new[] { "B", "A", "C", "D" }, List(report, "m").After);
        Assert.Contains("refused the selection", report.Outcome!);
    }

    // ---- The report -----------------------------------------------------------------

    [TestMethod]
    public async Task Items_changed_fires_before_the_completion_events_and_start_comes_first_end_last()
    {
        var report = await InspectAsync(Scenario(
            [List("a", ["A", "B"], group: "g"), List("b", ["X"], group: "g")],
            Drag("A", to: "b")));

        var events = report.Events!.Select(e => e.Event).ToList();

        Assert.AreEqual("OnStart", events.First());
        Assert.AreEqual("OnEnd", events.Last());
        Assert.IsLessThan(events.IndexOf("OnRemove"), events.LastIndexOf("ItemsChanged"));
        CollectionAssert.AreEqual(new[] { "B" }, report.Events!.Single(e => e.Event == "ItemsChanged" && e.List == "a").Items);
        Assert.IsTrue(report.Events!.All(e => e.Step > 0));
    }

    [TestMethod]
    public async Task The_markup_spells_the_parameters_the_way_the_component_takes_them()
    {
        // The drag is refused - 'done' is disabled - which is beside the point: the markup is written
        // whatever the drag does.
        var report = await InspectAsync(Scenario(
            [List("todo-list", ["A", "B"], group: "board", pull: "clone", cloneSuffix: " (copy)", filter: ["B"], put: false, sort: false, multiDrag: true),
             List("done", [], group: "board", disabled: true),
             List("trade", ["X"], group: "board", swap: true)],
            Drag("A", to: "done")));

        var markup = report.Markup!;

        Assert.Contains("<Bortable @bind-Items=\"_todoList\" TItem=\"string\" Id=\"todo-list\" Group=\"board\" Pull=\"BortablePull.Clone\" Clone=\"CloneTodoList\" Put=\"false\" Sort=\"false\" MultiDrag=\"true\" Filter=\"_todoListLocked.Contains\">", markup);
        Assert.Contains("Swap=\"true\"", markup);
        Assert.Contains("Disabled=\"true\"", markup);
        Assert.Contains("private IList<string> _todoList = new List<string> { \"A\", \"B\" };", markup);
        Assert.Contains("private static readonly HashSet<string> _todoListLocked = [\"B\"];", markup);
        Assert.Contains("private static string CloneTodoList(string item) => item + \" (copy)\";", markup);
    }

    // ---- Input that is not a drag ------------------------------------------------------

    [TestMethod]
    public async Task Input_that_is_not_json_is_rejected_with_the_parsers_own_message()
    {
        var report = await InspectAsync("{ lists: [");

        Assert.IsFalse(report.Valid);
        Assert.Contains("could not be read", report.Error!);
    }

    [TestMethod]
    public async Task A_misspelt_option_is_refused_rather_than_ignored()
    {
        var report = await InspectAsync("""{ "lists": [{ "id": "a", "items": ["A"], "multi_drag": true }], "drag": { "item": "A" } }""");

        Assert.IsFalse(report.Valid);
        Assert.Contains("Unknown property at $.lists[0].multi_drag", report.Error!);
        Assert.Contains("multiDrag", report.Error!);
    }

    [TestMethod]
    public async Task Every_malformed_description_is_answered_with_what_to_change()
    {
        (string Json, string Expected)[] cases =
        [
            ("""{ "drag": { "item": "A" } }""", "A drag needs lists"),
            ("""{ "lists": [{ "id": "a", "items": ["A", "A"] }], "drag": { "item": "A" } }""", "holds 'A' twice"),
            ("""{ "lists": [{ "items": ["A"] }], "drag": { "item": "A" } }""", "has no \"id\""),
            ("""{ "lists": [{ "id": "a", "items": ["A"] }, { "id": "a", "items": [] }], "drag": { "item": "A" } }""", "Two lists are called 'a'"),
            ("""{ "lists": [{ "id": "a", "items": ["A"] }] }""", "A drag needs \"drag\""),
            ("""{ "lists": [{ "id": "a", "items": ["A"] }], "drag": { "item": "Z" } }""", "No list holds 'Z'"),
            ("""{ "lists": [{ "id": "a", "items": ["A"] }, { "id": "b", "items": ["A"] }], "drag": { "item": "A" } }""", "say which with \"from\""),
            ("""{ "lists": [{ "id": "a", "items": ["A"] }], "drag": { "item": "A", "to": "zz" } }""", "No list is called 'zz'"),
            ("""{ "lists": [{ "id": "a", "items": ["A", "B"] }], "drag": { "item": "A", "index": 5 } }""", "index 5 is outside 'a'"),
            ("""{ "lists": [{ "id": "a", "items": ["A", "B"] }], "drag": { "item": "A", "swapWith": "B" } }""", "\"swapWith\" is for a list with swap: true"),
            ("""{ "lists": [{ "id": "a", "group": "g", "items": ["A"] }, { "id": "b", "group": "g", "swap": true, "items": ["X"] }], "drag": { "item": "A", "to": "b" } }""", "Say which, with \"swapWith\""),
            ("""{ "lists": [{ "id": "a", "items": ["A"], "pull": "copy" }], "drag": { "item": "A" } }""", "has pull 'copy'"),
            ("""{ "lists": [{ "id": "a", "items": ["A"] }], "drag": { "item": "A", "release": "throw" } }""", "\"release\" is 'throw'"),
            ("""{ "lists": [{ "id": "a", "items": ["A"] }], "drag": { "item": "A", "selected": ["Z"] } }""", "\"selected\" names 'Z'"),
        ];

        foreach (var (json, expected) in cases)
        {
            var report = await InspectAsync(json);

            Assert.IsFalse(report.Valid, json);
            Assert.Contains(expected, report.Error!, json);
        }
    }

    [TestMethod]
    public async Task A_description_past_the_tool_limits_is_refused_with_the_reason()
    {
        var huge = await InspectAsync(new string(' ', 64 * 1024 + 1) + """{ "lists": [] }""");

        var tooMany = await InspectAsync(Scenario([.. Enumerable.Range(0, 9).Select(i => List($"l{i}", [$"I{i}"]))], Drag("I0")));

        var tooLong = await InspectAsync(Scenario([List("a", [.. Enumerable.Range(0, 51).Select(i => $"I{i}")])], Drag("I0")));

        Assert.IsFalse(huge.Valid);
        Assert.Contains("65536 bytes", huge.Error!);
        Assert.Contains("up to 8", tooMany.Error!);
        Assert.Contains("up to 50", tooLong.Error!);
    }

    // ---- Helpers ------------------------------------------------------------------------------

    internal static Task<BortableDragReportDto> InspectAsync(string? json)
    {
        var arguments = new Dictionary<string, object?>();
        if (json is not null) arguments["json"] = json;

        return McpCall.StructuredAsync<BortableDragReportDto>("InspectBortableDrag", arguments);
    }

    private static string Scenario(object[] lists, object drag) => new JsonObject
    {
        ["lists"] = new JsonArray([.. lists.Cast<JsonObject>()]),
        ["drag"] = (JsonObject)drag
    }.ToJsonString();

    private static JsonObject List(string id, string[] items, string? group = null, string? pull = null, bool? put = null, bool? sort = null,
        bool? disabled = null, bool? swap = null, bool? multiDrag = null, string[]? filter = null, string? cloneSuffix = null, bool? readOnly = null)
    {
        var list = new JsonObject { ["id"] = id, ["items"] = new JsonArray([.. items.Select(item => (JsonNode?)item)]) };

        if (group is not null) list["group"] = group;
        if (pull is not null) list["pull"] = pull;
        if (put is not null) list["put"] = put;
        if (sort is not null) list["sort"] = sort;
        if (disabled is not null) list["disabled"] = disabled;
        if (swap is not null) list["swap"] = swap;
        if (multiDrag is not null) list["multiDrag"] = multiDrag;
        if (filter is not null) list["filter"] = new JsonArray([.. filter.Select(item => (JsonNode?)item)]);
        if (cloneSuffix is not null) list["cloneSuffix"] = cloneSuffix;
        if (readOnly is not null) list["readOnly"] = readOnly;

        return list;
    }

    private static JsonObject Drag(string item, string? from = null, string? to = null, int? index = null, string? swapWith = null, string[]? selected = null, string? release = null)
    {
        var drag = new JsonObject { ["item"] = item };

        if (from is not null) drag["from"] = from;
        if (to is not null) drag["to"] = to;
        if (index is not null) drag["index"] = index;
        if (swapWith is not null) drag["swapWith"] = swapWith;
        if (selected is not null) drag["selected"] = new JsonArray([.. selected.Select(entry => (JsonNode?)entry)]);
        if (release is not null) drag["release"] = release;

        return drag;
    }

    private static BortableListResultDto List(BortableDragReportDto report, string id)
    {
        Assert.IsTrue(report.Valid, report.Error);

        return report.Lists!.Single(list => list.Id == id);
    }

    private static BortableDragStepDto Step(BortableDragReportDto report, string name)
    {
        Assert.IsTrue(report.Valid, report.Error);

        return report.Steps!.First(step => step.Event == name);
    }

    private static BortableDragEventDto Event(BortableDragReportDto report, string name, string list)
        => report.Events!.Single(e => e.Event == name && e.List == list);

    /// <summary>The callbacks as "Event:List", in the order they were raised.</summary>
    private static string Sequence(BortableDragReportDto report) => string.Join(' ', report.Events!.Select(e => $"{e.Event}:{e.List}"));
}
