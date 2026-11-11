using System.Text.Json;
using System.Text.Json.Nodes;
using Bit.Bcrdt.Demo.Server.Dtos;
using Bit.Bcrdt.Demo.Server.Services;

namespace Bit.Bcrdt.Tests.Mcp;

/// <summary>
/// InspectBcrdtMerge on the questions it exists for: which value a concurrently written map key keeps, whether
/// both people's typing survives, what happens to a nested text two replicas both created, an add racing a
/// remove, two moves of one element, concurrent changes to a counter - and what it says about input it cannot run. The library's own tests pin
/// the engine; these pin what the tool reports about it.
/// </summary>
[TestClass]
public class McpMergeLabTests
{
    [TestMethod]
    public async Task Without_operations_it_runs_a_sample_that_shows_the_format()
    {
        var report = await InspectAsync(null);

        Assert.IsTrue(report.Sample);
        Assert.IsNull(report.Error);
        Assert.IsTrue(report.Converged);
        Assert.HasCount(2, report.Replicas!);
        Assert.IsTrue(report.Operations.All(line => line.StartsWith("base:", StringComparison.Ordinal) || line.StartsWith("A:", StringComparison.Ordinal) || line.StartsWith("B:", StringComparison.Ordinal)));
        Assert.Contains(note => note.Contains("<replica>: <type> <name> <action> <arguments>", StringComparison.Ordinal), report.Notes);

        Assert.AreSequenceEqual(new[] { "one value wins", "both survive" }, report.Decisions!.Select(decision => decision.Outcome).ToArray(), global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
    }

    [TestMethod]
    public async Task Concurrent_writes_to_a_map_key_keep_one_value_on_every_replica()
    {
        var report = await InspectAsync("""
            A: map meta set title "Draft"
            B: map meta set title "Final"
            """);

        Assert.IsTrue(report.Converged);

        var title = report.Merged!["meta"]!["title"]!.GetValue<string>();
        Assert.Contains(title, new[] { "Draft", "Final" });

        // Each replica held its own value before; after the merge, exactly one of them is what everyone holds.
        var before = report.Replicas!.ToDictionary(replica => replica.Label, replica => replica.Before!["meta"]!["title"]!.GetValue<string>());
        Assert.AreEqual("Draft", before["A"]);
        Assert.AreEqual("Final", before["B"]);

        var decision = report.Decisions!.Single();
        var winner = before.Single(pair => pair.Value == title).Key;
        var loser = before.Single(pair => pair.Value != title).Key;

        Assert.AreEqual("map 'meta', key 'title'", decision.Target);
        Assert.AreEqual("one value wins", decision.Outcome);
        Assert.Contains($"({winner}'s write)", decision.Detail);
        Assert.Contains($"{loser}'s write is gone", decision.Detail);
        Assert.Contains("never by time", decision.Detail);
        Assert.Contains(note => note.StartsWith("BcrdtMap:", StringComparison.Ordinal), report.Notes);
    }

    [TestMethod]
    public async Task Concurrent_inserts_into_a_text_both_survive_each_as_one_run()
    {
        var report = await InspectAsync("""
            base: text notes insert 0 "Agenda:"
            A: text notes insert 7 " budget"
            B: text notes insert 7 " hiring"
            """);

        Assert.IsTrue(report.Converged);

        var text = report.Merged!["notes"]!.GetValue<string>();

        Assert.StartsWith("Agenda:", text);
        Assert.Contains(" budget", text);
        Assert.Contains(" hiring", text);
        Assert.AreEqual("Agenda:".Length + " budget".Length + " hiring".Length, text.Length);

        var decision = report.Decisions!.Single();
        Assert.AreEqual("text 'notes'", decision.Target);
        Assert.AreEqual("both survive", decision.Outcome);
        Assert.Contains(JsonSerializer.Serialize(text), decision.Detail);
    }

    [TestMethod]
    public async Task A_nested_text_both_replicas_created_while_apart_keeps_only_one()
    {
        var report = await InspectAsync("""
            A: text form/notes insert 0 "alpha"
            B: text form/notes insert 0 "beta"
            """);

        Assert.IsTrue(report.Converged);

        var notes = report.Merged!["form"]!["notes"]!.GetValue<string>();
        Assert.Contains(notes, new[] { "alpha", "beta" });

        var key = report.Decisions!.Single(decision => decision.Target == "map 'form', key 'notes'");
        Assert.AreEqual("one value wins", key.Outcome);
        Assert.Contains("created a new text there", key.Detail);
        Assert.Contains("Create the nested type once", key.Detail);

        var text = report.Decisions!.Single(decision => decision.Target == "text 'form/notes'");
        Assert.AreEqual("an insertion is lost with its map key", text.Outcome);
    }

    [TestMethod]
    public async Task A_nested_text_created_before_the_replicas_parted_keeps_both_edits()
    {
        var report = await InspectAsync("""
            base: text form/notes insert 0 "!"
            A: text form/notes insert 0 "alpha"
            B: text form/notes insert 0 "beta"
            """);

        var notes = report.Merged!["form"]!["notes"]!.GetValue<string>();

        Assert.Contains("alpha", notes);
        Assert.Contains("beta", notes);
        Assert.EndsWith("!", notes);

        // Nobody wrote the key: it holds the text both replicas wrote into.
        Assert.DoesNotContain(decision => decision.Target.StartsWith("map", StringComparison.Ordinal), report.Decisions!);
        Assert.AreEqual("both survive", report.Decisions!.Single().Outcome);
    }

    [TestMethod]
    public async Task An_add_wins_over_a_concurrent_remove_only_when_it_adds_something()
    {
        // B removes and re-adds: its add is a new entry A's remove never saw, so it survives that remove.
        var addWins = await InspectAsync("""
            base: set tags add "urgent"
            A: set tags remove "urgent"
            B: set tags remove "urgent"
            B: set tags add "urgent"
            """);

        Assert.AreEqual("[\"urgent\"]", addWins.Merged!["tags"]!.ToJsonString());
        Assert.AreEqual("add wins", addWins.Decisions!.Single().Outcome);

        // B's add of an element it already holds writes nothing, so A's remove takes the element out everywhere.
        var removeWins = await InspectAsync("""
            base: set tags add "urgent"
            A: set tags remove "urgent"
            B: set tags add "urgent"
            """);

        Assert.AreEqual("[]", removeWins.Merged!["tags"]!.ToJsonString());
        Assert.AreEqual(0, removeWins.Replicas!.Single(replica => replica.Label == "B").Updates.Single().Bytes, "An add of a present element is a no-op and produces no update.");
        Assert.AreEqual("the remove wins", removeWins.Decisions!.Single().Outcome);
        Assert.Contains("no-op", removeWins.Decisions!.Single().Detail);
    }

    [TestMethod]
    public async Task Two_concurrent_moves_of_one_element_leave_two_copies()
    {
        var report = await InspectAsync("""
            base: array todo push "a" "b" "c"
            A: array todo move 0 3
            B: array todo move 0 2
            """);

        Assert.IsTrue(report.Converged);

        var items = report.Merged!["todo"]!.AsArray().Select(item => item!.GetValue<string>()).ToArray();

        Assert.AreEqual(2, items.Count(item => item == "a"), string.Join(",", items));
        Assert.AreEqual("a moved element is duplicated", report.Decisions!.Single().Outcome);
    }

    [TestMethod]
    public void Two_concurrent_moves_in_a_long_array_still_name_the_duplicate()
    {
        var report = BcrdtMergeLab.Inspect($"""
            base: array todo push {string.Join(' ', Enumerable.Range(0, 3_000))}
            A: array todo move 0 3
            B: array todo move 0 2
            """);

        Assert.IsNull(report.Error);
        Assert.IsTrue(report.Converged);
        Assert.AreEqual(3_001, report.Merged!["todo"]!.AsArray().Count);
        Assert.AreEqual("a moved element is duplicated", report.Decisions!.Single().Outcome);
        Assert.Contains("twice: 0 now appears more often", report.Decisions!.Single().Detail);
    }

    [TestMethod]
    public async Task Concurrent_array_inserts_all_survive()
    {
        var report = await InspectAsync("A: array todo push \"x\"; B: array todo push \"y\"; C: array todo insert 0 \"z\"");

        var items = report.Merged!["todo"]!.AsArray().Select(item => item!.GetValue<string>()).ToArray();

        Assert.AreSequenceEqual(new[] { "x", "y", "z" }, items, global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
        Assert.AreEqual("all survive", report.Decisions!.Single().Outcome);
    }

    [TestMethod]
    public async Task Concurrent_changes_to_a_counter_are_all_summed()
    {
        var report = await InspectAsync("""
            base: counter votes add 5
            A: counter votes add 2
            B: counter votes increment
            B: counter votes add 3
            C: counter votes decrement
            """);

        Assert.IsTrue(report.Converged, string.Join(" | ", report.Diverged ?? []));
        Assert.AreEqual(5, report.Base!["votes"]!.GetValue<long>());
        Assert.AreEqual(5 + 2 + 1 + 3 - 1, report.Merged!["votes"]!.GetValue<long>());

        // Each replica held the start plus only its own changes before it heard from the others.
        var before = report.Replicas!.ToDictionary(replica => replica.Label, replica => replica.Before!["votes"]!.GetValue<long>());
        Assert.AreEqual(7, before["A"]);
        Assert.AreEqual(9, before["B"]);
        Assert.AreEqual(4, before["C"]);

        var decision = report.Decisions!.Single();
        Assert.AreEqual("counter 'votes'", decision.Target);
        Assert.AreSequenceEqual(new[] { "A", "B", "C" }, decision.Replicas);
        Assert.AreEqual("every change is summed", decision.Outcome);
        Assert.Contains("A added 2, B incremented it, added 3 and C decremented it while apart", decision.Detail);
        Assert.Contains("the counter started at 5, A +2, B +4 and C -1, and every replica now holds 10", decision.Detail);
        Assert.Contains("count + 1", decision.Detail);
        Assert.Contains(note => note.StartsWith("BcrdtCounter:", StringComparison.Ordinal), report.Notes);
    }

    [TestMethod]
    public async Task A_nested_counter_created_before_the_replicas_parted_sums_every_change()
    {
        var report = await InspectAsync("""
            base: counter post/likes increment
            A: counter post/likes add 4
            B: counter post/likes add -6
            """);

        Assert.IsTrue(report.Converged);
        Assert.AreEqual(-1, report.Merged!["post"]!["likes"]!.GetValue<long>());

        // Nobody wrote the key: it holds the counter both replicas changed.
        var decision = report.Decisions!.Single();
        Assert.AreEqual("counter 'post/likes'", decision.Target);
        Assert.AreEqual("every change is summed", decision.Outcome);
        Assert.Contains("B subtracted 6", decision.Detail);
        Assert.Contains("started at 1, A +4 and B -6", decision.Detail);
    }

    [TestMethod]
    public async Task A_nested_counter_both_replicas_created_while_apart_keeps_only_one_share()
    {
        var report = await InspectAsync("""
            A: counter post/likes add 2
            B: counter post/likes add 3
            """);

        Assert.IsTrue(report.Converged);

        var likes = report.Merged!["post"]!["likes"]!.GetValue<long>();
        Assert.Contains(likes, new long[] { 2, 3 });

        var key = report.Decisions!.Single(decision => decision.Target == "map 'post', key 'likes'");
        Assert.AreEqual("one value wins", key.Outcome);
        Assert.Contains("created a new counter there", key.Detail);

        var counter = report.Decisions!.Single(decision => decision.Target == "counter 'post/likes'");
        Assert.AreEqual("a change is lost with its map key", counter.Outcome);
        Assert.Contains($"add up to 5, but every replica holds {likes}", counter.Detail);
        Assert.Contains("A and B each created the counter while apart", counter.Detail);
        Assert.Contains("Create a nested counter once", counter.Detail);
    }

    [TestMethod]
    public async Task A_nested_counter_whose_key_is_written_over_loses_every_change_made_inside_it()
    {
        var report = await InspectAsync("""
            base: counter post/likes increment
            A: counter post/likes add 2
            B: counter post/likes add 3
            C: map post set likes 0
            """);

        Assert.IsTrue(report.Converged);
        Assert.AreEqual(0, report.Merged!["post"]!["likes"]!.GetValue<long>());

        var counter = report.Decisions!.Single(decision => decision.Target == "counter 'post/likes'");
        Assert.AreEqual("a change is lost with its map key", counter.Outcome);
        Assert.Contains("A +2 and B +3 on a counter that started at 1 - add up to 6, but every replica holds 0", counter.Detail);
        Assert.Contains("was written over concurrently", counter.Detail);
        Assert.DoesNotContain("see that decision", counter.Detail, "Only C wrote the key, so there is no key decision to point at.");
        Assert.DoesNotContain(decision => decision.Target.StartsWith("map", StringComparison.Ordinal), report.Decisions!);
    }

    [TestMethod]
    public async Task A_number_written_over_a_counter_is_not_taken_for_the_sum_when_it_happens_to_equal_it()
    {
        var report = await InspectAsync("""
            base: counter post/likes increment
            A: counter post/likes add 2
            B: counter post/likes add 3
            C: map post set likes 6
            """);

        Assert.IsTrue(report.Converged);
        Assert.AreEqual(6, report.Merged!["post"]!["likes"]!.GetValue<long>());

        var counter = report.Decisions!.Single(decision => decision.Target == "counter 'post/likes'");
        Assert.AreEqual("a change is lost with its map key", counter.Outcome);
        Assert.Contains("add up to 6, but every replica holds 6, and not as a counter", counter.Detail);
        Assert.Contains("was written over concurrently", counter.Detail);
    }

    [TestMethod]
    public async Task A_count_written_into_a_map_key_points_at_a_counter()
    {
        var report = await InspectAsync("""
            base: map post set likes 5
            A: map post set likes 6
            B: map post set likes 6
            C: map post set likes 7
            """);

        var decision = report.Decisions!.Single();

        Assert.AreEqual("one value wins", decision.Outcome);
        Assert.Contains("make it a BcrdtCounter", decision.Detail);

        // A title is not a count: the hint is for numbers only.
        var title = await InspectAsync("A: map meta set title \"Draft\"\nB: map meta set title \"Final\"");
        Assert.DoesNotContain("BcrdtCounter", title.Decisions!.Single().Detail);
    }

    [TestMethod]
    public async Task Formatting_is_reported_where_to_json_cannot_show_it()
    {
        var report = await InspectAsync("""
            base: text notes insert 0 "hello world"
            A: text notes format 0 5 {"bold":true}
            B: text notes format 6 5 {"italic":true}
            """);

        Assert.AreEqual("hello world", report.Merged!["notes"]!.GetValue<string>());

        var delta = report.MergedFormatting!["notes"]!.ToJsonString();
        Assert.Contains("\"bold\":true", delta);
        Assert.Contains("\"italic\":true", delta);
        Assert.Contains("\"bold\":true", report.Replicas!.Single(replica => replica.Label == "A").FormattingBefore!.ToJsonString());
    }

    [TestMethod]
    public async Task Every_update_is_measured_and_a_replicas_session_is_one_delta()
    {
        var report = await InspectAsync("""
            A: text notes insert 0 "a"
            A: text notes insert 1 "b"
            A: text notes insert 2 "c"
            B: text notes insert 0 "x"
            """);

        var a = report.Replicas!.Single(replica => replica.Label == "A");

        Assert.HasCount(3, a.Updates);
        Assert.IsTrue(a.Updates.All(update => update.Bytes > 0));

        // Three keystrokes are three updates on the wire, and one delta on reconnect - a run merged into one block.
        Assert.IsLessThan(a.Updates.Sum(update => update.Bytes), a.DeltaBytes);
        Assert.IsGreaterThan(0, report.MergedBytes);
    }

    [TestMethod]
    public async Task Four_replicas_converge_under_every_delivery_order()
    {
        var report = await InspectAsync("""
            base: text notes insert 0 "."
            A: text notes insert 0 "a"
            B: text notes insert 1 "b"
            C: map meta set k 1
            D: map meta set k 2
            D: text notes delete 0 1
            """);

        Assert.IsTrue(report.Converged, string.Join(" | ", report.Diverged ?? []));
        Assert.AreEqual(4 * 3 * 2 * 1 + 1, report.Schedules);
        Assert.IsNull(report.Diverged);
        Assert.AreSequenceEqual(new ulong[] { 1, 2, 3, 4 }, report.Replicas!.Select(replica => replica.ClientId).ToArray());
    }

    [TestMethod]
    public async Task The_json_form_and_the_line_form_run_the_same_scenario()
    {
        var lines = await InspectAsync("""
            # a comment
            base: map meta set title "Plan"
            A: map meta set title "Draft"; B: map meta delete title
            """);

        var json = await InspectAsync("""
            { "base": ["map meta set title \"Plan\""], "A": ["map meta set title \"Draft\""], "B": "map meta delete title" }
            """);

        Assert.IsNull(lines.Error);
        Assert.IsNull(json.Error);
        Assert.AreSequenceEqual(lines.Operations, json.Operations);
        Assert.AreEqual(lines.Merged!.ToJsonString(), json.Merged!.ToJsonString());
        Assert.AreEqual("one value wins", lines.Decisions!.Single().Outcome);
    }

    [TestMethod]
    public async Task Input_it_cannot_run_is_explained_rather_than_guessed()
    {
        (string Operations, string Expected)[] cases =
        [
            ("A: list todo push 1\nB: list todo push 2", "'list' is not a type"),
            ("map meta set a 1\nB: map meta set a 2", "does not start with a replica label"),
            ("A: map meta set a 1", "at least two replicas"),
            ("A: map m set a 1\nB: map m set a 1\nC: map m set a 1\nD: map m set a 1\nE: map m set a 1", "at most 4"),
            ("A: map meta set title Draft\nB: map meta set title \"x\"", "is not a JSON value"),
            ("B: text notes insert 0 \"x\"\nA: text notes insert 0 \"unclosed", "never closed"),
            ("A: text doc insert 0 \"x\"\nB: map doc set k 1", "A root is one kind on every replica"),
            ("A: text notes insert 5 \"x\"\nB: text notes insert 0 \"y\"", "failed on replica A"),
            ("A: text notes paint 0 1\nB: text notes insert 0 \"y\"", "a text can insert, delete or format"),
            ("A: map meta set k\nB: map meta set k 1", "takes 2 arguments"),
            ("A: counter votes add 1.5\nB: counter votes increment", "'1.5' is not a whole number a counter can add"),
            ("A: counter votes increment 2\nB: counter votes increment", "counter increment takes 0 arguments"),
            ("A: counter votes reset\nB: counter votes increment", "a counter can add, increment or decrement"),
            ("A: counter votes increment\nB: set votes add 1", "A root is one kind on every replica"),
            ("A: counter votes add 9223372036854775807\nA: counter votes increment\nB: counter votes increment", "failed on replica A"),
            ("{ not json", "not valid JSON"),
        ];

        foreach (var (operations, expected) in cases)
        {
            var report = await InspectAsync(operations);

            Assert.IsNotNull(report.Error, operations);
            Assert.Contains(expected, report.Error, operations);
            Assert.IsNull(report.Replicas, operations);
            Assert.Contains(note => note.Contains("<replica>: <type>", StringComparison.Ordinal), report.Notes, $"{operations}: the error should come with the format.");
        }
    }

    [TestMethod]
    public async Task Input_too_large_to_be_a_scenario_is_refused_before_it_runs()
    {
        var operations = string.Join('\n', Enumerable.Range(0, 300).Select(i => $"{(i % 2 == 0 ? "A" : "B")}: array todo push {i}"));

        var report = await InspectAsync(operations);

        Assert.Contains("at most 200", report.Error!);
    }

    internal static Task<BcrdtMergeReportDto> InspectAsync(string? operations)
    {
        var arguments = new Dictionary<string, object?>();
        if (operations is not null) arguments["operations"] = operations;

        return McpCall.StructuredAsync<BcrdtMergeReportDto>("InspectBcrdtMerge", arguments);
    }
}
