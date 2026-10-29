using System.Text.Json.Nodes;
using Bit.Bdnd.Demo.Server.Dtos;
using Bit.Bdnd.Demo.Server.Services;

namespace Bit.Bdnd.Tests.Mcp;

/// <summary>
/// InspectBdndDrop on the drags a page actually meets: the nested kanban of the sample, an item of a
/// type the hovered target does not take, a target whose predicate says no, a drop outside every
/// target, a draggable that cannot drag, files from the desktop, the mistakes in a scenario that are
/// worth a note, and input that is not a scenario at all. The scenarios are built as JSON, so each test
/// changes exactly the one thing it is about.
/// </summary>
[TestClass]
public class McpDropLabTests
{
    [TestMethod]
    public async Task Without_a_scenario_it_hands_out_a_sample_it_can_run()
    {
        var report = await InspectAsync(null);

        Assert.IsFalse(report.Valid);
        Assert.IsNotNull(report.Sample);
        Assert.IsNull(report.Targets);

        var sample = JsonNode.Parse(report.Sample)!;

        Assert.AreEqual("card", (string?)sample["draggable"]!["type"]);
        Assert.AreEqual("card-2", (string?)sample["hover"]);
    }

    [TestMethod]
    public async Task The_sample_drops_innermost_first_and_the_outer_result_wins()
    {
        var report = await InspectAsync((await InspectAsync(null)).Sample);

        Assert.IsTrue(report.Valid, report.Error);
        Assert.AreEqual("card", report.ItemType);
        Assert.IsFalse(report.Native);
        Assert.IsTrue(report.DragStarted);
        Assert.IsTrue(report.BeginFired);
        Assert.AreEqual("move", report.EffectAllowed);
        CollectionAssert.AreEqual(new[] { "board", "todo", "card-2" }, report.HoverPath);

        // Nested targets get OnDrop innermost first, and each outer one sees that an inner one already dropped.
        Assert.IsTrue(report.DropFired);
        CollectionAssert.AreEqual(new[] { "card-2", "todo", "board" }, report.DropOrder);
        Assert.IsFalse(Target(report, "card-2").InnerAlreadyDropped);
        Assert.IsTrue(Target(report, "todo").InnerAlreadyDropped);
        Assert.IsTrue(Target(report, "board").InnerAlreadyDropped);

        // "todo" published first, "board" last: a non-null outer result overrides the inner one.
        Assert.AreEqual(new BdndDragEndReportDto { DidDrop = true, DropResult = "board" }, report.End);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("replaces an inner one", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task The_sample_hovers_deep_on_the_chain_and_shallow_on_the_innermost_only()
    {
        var report = await InspectAsync((await InspectAsync(null)).Sample);

        foreach (var id in new[] { "board", "todo", "card-2" })
        {
            var target = Target(report, id);

            Assert.IsTrue(target.Hovered, id);
            Assert.IsTrue(target.IsOver, id);
            Assert.IsTrue(target.CanDrop, id);
            Assert.AreEqual(id == "card-2", target.IsOverShallow, id);
        }

        // A CanDropPredicate that says no, and a target that only takes files: neither can drop a card.
        var done = Target(report, "done");
        var attachments = Target(report, "attachments");

        Assert.IsFalse(done.CanDrop);
        Assert.IsFalse(done.IsOver);
        Assert.IsNull(done.DropOrder);
        Assert.IsFalse(attachments.CanDrop);
        CollectionAssert.AreEqual(new[] { BdndNativeTypes.File }, attachments.Accept);
        Assert.AreEqual("board", done.Parent);
        Assert.AreEqual(1, done.Depth);
    }

    [TestMethod]
    public async Task A_target_that_does_not_take_the_type_passes_the_drop_to_the_one_around_it()
    {
        var report = await InspectAsync(Scenario("card", "trash",
            Target("list", ["card"], result: "list", children: [Target("trash", ["bin"])])));

        var trash = Target(report, "trash");
        var list = Target(report, "list");

        Assert.IsTrue(trash.Hovered);
        Assert.IsFalse(trash.IsOver, "IsOver is only set for a target that accepts the type.");
        Assert.IsFalse(trash.CanDrop);
        Assert.IsTrue(list.IsOver);
        Assert.IsTrue(list.IsOverShallow, "With the inner target out of it, the list is the innermost hovered target that counts.");

        CollectionAssert.AreEqual(new[] { "list" }, report.DropOrder);
        Assert.AreEqual(new BdndDragEndReportDto { DidDrop = true, DropResult = "list" }, report.End);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("'trash' does not accept 'card'", StringComparison.Ordinal) && note.Contains("falls through to 'list'", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_wrong_type_with_nothing_around_it_never_drops()
    {
        var report = await InspectAsync(Scenario("card", "trash", Target("trash", ["bin"])));

        Assert.IsFalse(report.DropFired, "With no target able to drop, the browser fires no drop event.");
        Assert.IsEmpty(report.DropOrder!);
        Assert.AreEqual(new BdndDragEndReportDto { DidDrop = false }, report.End);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("no drop event", StringComparison.Ordinal)));
        Assert.IsTrue(report.Notes.Any(note => note.Contains("No target accepts 'card'", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_predicate_that_says_no_keeps_the_hover_and_rejects_the_drop()
    {
        var report = await InspectAsync(Scenario("card", "done",
            Target("board", ["card"], children: [Target("done", ["card"], canDrop: false, result: "done")])));

        var done = Target(report, "done");

        // Hover follows the type; the predicate only decides CanDrop.
        Assert.IsTrue(done.IsOver);
        Assert.IsTrue(done.IsOverShallow);
        Assert.IsFalse(done.CanDrop);
        Assert.IsNull(done.DropOrder);

        CollectionAssert.AreEqual(new[] { "board" }, report.DropOrder);
        Assert.AreEqual(new BdndDragEndReportDto { DidDrop = true }, report.End);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("CanDropPredicate answered false", StringComparison.Ordinal) && note.Contains("'board'", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_lone_target_whose_predicate_says_no_ends_the_drag_as_a_cancel()
    {
        var report = await InspectAsync(Scenario("card", "done", Target("done", ["card"], canDrop: false)));

        Assert.IsFalse(report.DropFired);
        Assert.AreEqual(new BdndDragEndReportDto { DidDrop = false }, report.End);
    }

    [TestMethod]
    public async Task An_inner_result_survives_an_outer_target_that_sets_none()
    {
        var report = await InspectAsync(Scenario("card", "slot",
            Target("column", ["card"], children: [Target("slot", ["card"], result: "slot")])));

        CollectionAssert.AreEqual(new[] { "slot", "column" }, report.DropOrder);
        Assert.AreEqual(new BdndDragEndReportDto { DidDrop = true, DropResult = "slot" }, report.End);
        Assert.IsFalse(report.Notes.Any(note => note.Contains("replaces an inner one", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_drop_outside_every_target_is_a_cancel_and_can_drop_still_lights_up()
    {
        var report = await InspectAsync(Scenario("card", null, Target("left", ["card"]), Target("right", ["card"])));

        Assert.IsEmpty(report.HoverPath!);
        Assert.IsFalse(report.DropFired);
        Assert.AreEqual(new BdndDragEndReportDto { DidDrop = false }, report.End);

        // CanDrop is about the item, not the pointer.
        Assert.IsTrue(report.Targets!.All(target => target.CanDrop && target.IsOver is false));
        Assert.IsTrue(report.Notes.Any(note => note.Contains("dropped outside every target", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_draggable_that_cannot_drag_never_starts()
    {
        var report = await InspectAsync(Scenario(new JsonObject { ["type"] = "card", ["canDrag"] = false }, "list", Target("list", ["card"])));

        Assert.IsTrue(report.Valid);
        Assert.IsFalse(report.DragStarted);
        Assert.IsFalse(report.BeginFired);
        Assert.IsEmpty(report.DataTransferTypes!, "The script tags no drag for a source that cannot drag.");
        Assert.IsNull(report.End);
        Assert.IsFalse(Target(report, "list").CanDrop);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("draggable=\"false\"", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task The_drag_is_tagged_with_the_draggables_own_type_and_effect()
    {
        var report = await InspectAsync(Scenario(new JsonObject { ["type"] = "box", ["dropEffect"] = "copy" }, "bin", Target("bin", ["box"])));

        Assert.AreEqual("copy", report.EffectAllowed);
        Assert.HasCount(1, report.DataTransferTypes!);
        Assert.StartsWith("application/x-bit-bdnd-", report.DataTransferTypes![0]);
    }

    [TestMethod]
    public async Task An_effect_of_none_turns_every_drop_off()
    {
        var report = await InspectAsync(Scenario(new JsonObject { ["type"] = "box", ["dropEffect"] = "none" }, "bin", Target("bin", ["box"])));

        Assert.IsTrue(Target(report, "bin").CanDrop);
        Assert.IsFalse(report.DropFired);
        Assert.AreEqual(new BdndDragEndReportDto { DidDrop = false }, report.End);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("dropEffect is 'none'", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task An_effect_the_browser_does_not_know_is_pointed_out()
    {
        var report = await InspectAsync(Scenario(new JsonObject { ["type"] = "box", ["dropEffect"] = "copymove" }, "bin", Target("bin", ["box"])));

        Assert.IsTrue(report.DropFired);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("'copyMove'", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_native_file_drag_reaches_every_accepting_target_through_OnNativeDrop()
    {
        var report = await InspectAsync(Scenario("BdndNativeTypes.File", "zone",
            Target("page", ["native:file"], result: "ignored", children: [Target("zone", ["BdndNativeTypes.File"]), Target("other", [BdndNativeTypes.File])])));

        Assert.IsTrue(report.Valid, report.Error);
        Assert.IsTrue(report.Native);
        Assert.AreEqual(BdndNativeTypes.File, report.ItemType);
        CollectionAssert.AreEqual(new[] { "Files" }, report.DataTransferTypes);
        Assert.IsNull(report.End, "No BdndDraggable is involved, so there is no OnEnd.");
        Assert.IsNull(report.DragStarted);

        // Both hovered targets take the drop, innermost first; the state lights up only under the pointer.
        CollectionAssert.AreEqual(new[] { "zone", "page" }, report.DropOrder);
        Assert.AreEqual(BdndNativeTypes.File, Target(report, "zone").NativeItemType);

        foreach (var id in new[] { "page", "zone" })
        {
            Assert.IsTrue(Target(report, id).IsOver, id);
            Assert.IsTrue(Target(report, id).CanDrop, id);
        }

        Assert.IsTrue(Target(report, "zone").IsOverShallow, "the innermost hovered target that accepts the drag");
        Assert.IsFalse(Target(report, "page").IsOverShallow, "an accepting target nested in it is hovered");

        Assert.IsFalse(Target(report, "other").CanDrop);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("OnNativeDrop, never OnDrop", StringComparison.Ordinal)));
        Assert.IsTrue(report.Notes.Any(note => note.Contains("'result' was not published", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_plain_file_type_does_not_take_a_native_drag()
    {
        var report = await InspectAsync(Scenario(BdndNativeTypes.File, "zone", Target("zone", ["file"])));

        Assert.IsFalse(report.DropFired);
        Assert.IsEmpty(report.DropOrder!);
        Assert.IsFalse(Target(report, "zone").IsOver);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("'zone' accepts 'file' - an in-app type name", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Every_native_type_reaches_a_target_that_accepts_it()
    {
        foreach (var (name, constant) in new[] { ("File", BdndNativeTypes.File), ("Url", BdndNativeTypes.Url), ("Text", BdndNativeTypes.Text), ("Html", BdndNativeTypes.Html) })
        {
            var report = await InspectAsync(Scenario($"BdndNativeTypes.{name}", "zone", Target("zone", [constant])));

            CollectionAssert.AreEqual(new[] { "zone" }, report.DropOrder, name);
            Assert.AreEqual(constant, Target(report, "zone").NativeItemType, name);
        }
    }

    [TestMethod]
    public async Task A_type_that_differs_only_in_case_is_called_a_near_miss()
    {
        var report = await InspectAsync(Scenario("card", "list", Target("list", ["Card", "cards"])));

        Assert.IsFalse(Target(report, "list").CanDrop);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("'list' accepts 'Card', which is not 'card'", StringComparison.Ordinal)));
        Assert.IsTrue(report.Notes.Any(note => note.Contains("'list' accepts 'cards', which is not 'card'", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Duplicate_ids_and_empty_accept_lists_are_pointed_out()
    {
        var report = await InspectAsync(Scenario("card", "list", Target("list", ["card"]), Target("list", [])));

        Assert.HasCount(2, report.Targets!);
        Assert.IsTrue(Target(report, "list").Hovered, "hover resolves a repeated id to the first target that has it.");
        Assert.IsFalse(report.Targets![1].Hovered);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("used by 2 targets", StringComparison.Ordinal)));
        Assert.IsTrue(report.Notes.Any(note => note.Contains("accepts nothing", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_hover_path_is_read_from_the_outer_target_in()
    {
        var path = await InspectAsync(Scenario("card", new JsonArray("outer", "inner"), Target("outer", ["card"], children: [Target("inner", ["card"])])));
        var broken = await InspectAsync(Scenario("card", new JsonArray("inner", "outer"), Target("outer", ["card"], children: [Target("inner", ["card"])])));

        CollectionAssert.AreEqual(new[] { "outer", "inner" }, path.HoverPath);
        Assert.IsFalse(broken.Valid);
        Assert.Contains("not a chain of nested targets", broken.Error!);
    }

    [TestMethod]
    public async Task An_unknown_hover_id_suggests_the_close_ones()
    {
        var report = await InspectAsync(Scenario("card", "alpah", Target("alpha", ["card"])));

        Assert.IsFalse(report.Valid);
        Assert.Contains("Did you mean: 'alpha'", report.Error!);
    }

    [TestMethod]
    public async Task A_longer_hover_id_two_edits_off_is_suggested_and_three_edits_off_is_not()
    {
        var two = await InspectAsync(Scenario("card", "backlgo-colum", Target("backlog-column", ["card"])));
        var three = await InspectAsync(Scenario("card", "bcklgo-colum", Target("backlog-column", ["card"])));

        Assert.Contains("Did you mean: 'backlog-column'", two.Error!);
        Assert.DoesNotContain("Did you mean", three.Error!);
    }

    [TestMethod]
    public async Task Names_too_long_to_be_a_typo_are_not_compared()
    {
        var name = new string('x', 100_000);

        var hover = await InspectAsync(Scenario("card", name + "a", Target(name + "b", ["card"])));
        var type = await InspectAsync(Scenario(name + "a", "zone", Target("zone", [name + "b"])));

        Assert.IsFalse(hover.Valid);
        Assert.DoesNotContain("Did you mean", hover.Error!);
        Assert.IsTrue(type.Valid, type.Error);
        Assert.IsFalse(type.Notes.Any(note => note.Contains("a typo here", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_near_miss_at_the_length_limit_is_suggested_and_one_past_it_is_not()
    {
        var atLimit = new string('x', 127);
        var pastLimit = new string('x', 128);

        var at = await InspectAsync(Scenario("card", atLimit + "a", Target(atLimit + "b", ["card"])));
        var past = await InspectAsync(Scenario("card", pastLimit + "a", Target(pastLimit + "b", ["card"])));

        Assert.Contains("Did you mean", at.Error!);
        Assert.DoesNotContain("Did you mean", past.Error!);
    }

    [TestMethod]
    public async Task A_misspelt_property_is_named_rather_than_silently_ignored()
    {
        var target = new JsonObject { ["id"] = "list", ["acept"] = "card" };

        var report = await InspectAsync(new JsonObject { ["draggable"] = new JsonObject { ["type"] = "card" }, ["targets"] = new JsonArray(target), ["hover"] = "list" }.ToJsonString());

        Assert.IsTrue(report.Valid);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("'acept'", StringComparison.Ordinal) && note.Contains("did you mean 'accept'", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Input_that_is_not_json_is_rejected_with_the_parsers_own_message()
    {
        var report = await InspectAsync("{ draggable: [");

        Assert.IsFalse(report.Valid);
        Assert.IsNotNull(report.Error);
        Assert.IsNull(report.Targets);
    }

    [TestMethod]
    public async Task Json_that_is_not_a_scenario_says_what_a_scenario_is()
    {
        var array = await InspectAsync("[1, 2, 3]");
        var flat = await InspectAsync("""{ "type": "card" }""");
        var untyped = await InspectAsync("""{ "draggable": { "type": "" } }""");
        var anonymous = await InspectAsync("""{ "draggable": { "type": "card" }, "targets": [ { "accept": "card" } ] }""");

        Assert.Contains("\"draggable\"", array.Error!);
        Assert.Contains("\"draggable\" object", flat.Error!);
        Assert.Contains("draggable.type is required", untyped.Error!);
        Assert.Contains("targets[0].id is required", anonymous.Error!);
    }

    [TestMethod]
    public async Task A_scenario_past_the_tool_limit_is_refused_with_the_reason()
    {
        var huge = new string(' ', BdndDropLab.MaxInputBytes + 1) + """{ "draggable": { "type": "card" } }""";

        var report = await InspectAsync(huge);

        Assert.IsFalse(report.Valid);
        Assert.Contains($"{BdndDropLab.MaxInputBytes} bytes", report.Error!);
    }

    [TestMethod]
    public async Task Targets_nested_past_the_limit_are_refused_with_the_reason()
    {
        var root = Target("t0", ["card"]);
        var current = root;

        for (var depth = 1; depth <= BdndDropLab.MaxDepth; depth++)
        {
            var child = Target($"t{depth}", ["card"]);
            current["children"] = new JsonArray(child);
            current = child;
        }

        var report = await InspectAsync(Scenario("card", null, root));

        Assert.IsFalse(report.Valid);
        Assert.Contains($"nested up to {BdndDropLab.MaxDepth} deep", report.Error!);
    }

    [TestMethod]
    public async Task Targets_nested_to_the_limit_run_even_when_the_innermost_has_an_empty_children_array()
    {
        var root = Target("t0", ["card"]);
        var current = root;

        for (var depth = 1; depth < BdndDropLab.MaxDepth; depth++)
        {
            var child = Target($"t{depth}", ["card"]);
            current["children"] = new JsonArray(child);
            current = child;
        }

        current["children"] = new JsonArray();

        var report = await InspectAsync(Scenario("card", $"t{BdndDropLab.MaxDepth - 1}", root));

        Assert.IsTrue(report.Valid, report.Error);
        Assert.HasCount(BdndDropLab.MaxDepth, report.Targets!);
    }

    internal static Task<BdndDropReportDto> InspectAsync(string? json)
    {
        var arguments = new Dictionary<string, object?>();
        if (json is not null) arguments["json"] = json;

        return McpCall.StructuredAsync<BdndDropReportDto>("InspectBdndDrop", arguments);
    }

    internal static string Scenario(string type, JsonNode? hover, params JsonObject[] targets)
        => Scenario(new JsonObject { ["type"] = type }, hover, targets);

    internal static string Scenario(JsonObject draggable, JsonNode? hover, params JsonObject[] targets) => new JsonObject
    {
        ["draggable"] = draggable,
        ["targets"] = new JsonArray([.. targets]),
        ["hover"] = hover
    }.ToJsonString();

    internal static JsonObject Target(string id, string[] accept, bool? canDrop = null, string? result = null, JsonObject[]? children = null)
    {
        var target = new JsonObject { ["id"] = id, ["accept"] = new JsonArray([.. accept.Select(type => (JsonNode?)type)]) };

        if (canDrop is not null) target["canDrop"] = canDrop;
        if (result is not null) target["result"] = result;
        if (children is not null) target["children"] = new JsonArray([.. children]);

        return target;
    }

    private static BdndDropTargetReportDto Target(BdndDropReportDto report, string id)
    {
        Assert.IsTrue(report.Valid, report.Error);

        return report.Targets!.First(target => target.Id == id);
    }
}
