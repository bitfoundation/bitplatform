using System.Diagnostics;
using Bit.Bcrdt.Demo.Client.Collaboration;

namespace Bit.Bcrdt.Tests;

/// <summary>
/// Exercises the demo's collaboration wiring without a browser.
/// </summary>
/// <remarks>
/// <para>
/// The demo's central claim is that each panel is a genuinely separate replica rather than several
/// views of one document - which is the difference between demonstrating collaboration and merely
/// asserting it. That claim lives in <see cref="DemoWorkspace"/> and <see cref="DemoReplica"/>, not
/// in the markup, so it can be checked directly. So does the conflict laboratory's procedure, in
/// <see cref="ConflictLabSession"/>, which the tests here run exactly as the page does.
/// </para>
/// <para>
/// What is not covered here is the Razor rendering itself, which needs a browser.
/// </para>
/// <para>
/// Nothing here waits a fixed interval and hopes. Every wait is for a stated condition, bounded by
/// a generous timeout, so a slow machine costs time rather than a false failure, and a fast one
/// costs nothing.
/// </para>
/// </remarks>
[TestClass]
public class DemoTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static DemoWorkspace CreateWorkspace() => new(new BcrdtInMemoryNetwork());

    /// <summary>Polls until a condition holds, failing the test with a description if it never does.</summary>
    private static async Task WaitUntilAsync(Func<bool> condition, string description)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition())
        {
            if (elapsed.Elapsed >= Timeout)
            {
                Assert.Fail($"Timed out after {Timeout.TotalSeconds:N0} s waiting until {description}.");
            }
            await Task.Delay(10);
        }
    }

    private static string Text(DemoReplica replica) => replica.Document.Text("body").ToString();

    private static bool AllConverged(IReadOnlyList<DemoReplica> replicas)
    {
        string reference = DemoJson.Format(replicas[0].Document.ToJson());
        return replicas.All(replica => DemoJson.Format(replica.Document.ToJson()) == reference);
    }

    private static async Task DisposeAllAsync(IEnumerable<DemoReplica> replicas)
    {
        foreach (DemoReplica replica in replicas) await replica.DisposeAsync();
    }

    [TestMethod]
    public async Task Each_panel_is_a_separate_replica_with_its_own_identity()
    {
        DemoWorkspace workspace = CreateWorkspace();
        IReadOnlyList<DemoReplica> replicas = await workspace.OpenReplicasAsync("doc", "A", "B", "C");

        try
        {
            Assert.AreEqual(3, replicas.Count);

            // Separate documents, separate client identities, separate presence colours. If any of
            // these were shared, the demo would be showing one object through three windows.
            Assert.AreEqual(3, replicas.Select(r => r.ClientId).Distinct().Count());
            Assert.AreEqual(3, replicas.Select(r => r.Document).Distinct().Count());
            Assert.AreEqual(3, replicas.Select(r => r.Color).Distinct().Count());
        }
        finally
        {
            await DisposeAllAsync(replicas);
        }
    }

    [TestMethod]
    public async Task An_edit_in_one_replica_reaches_the_others()
    {
        DemoWorkspace workspace = CreateWorkspace();
        IReadOnlyList<DemoReplica> replicas = await workspace.OpenReplicasAsync("doc", "A", "B", "C");

        try
        {
            replicas[0].Document.Text("body").Insert(0, "typed into A");

            await WaitUntilAsync(
                () => replicas.All(replica => Text(replica) == "typed into A"),
                "every replica holds A's edit");
        }
        finally
        {
            await DisposeAllAsync(replicas);
        }
    }

    [TestMethod]
    public async Task Going_offline_isolates_a_replica_and_reconnecting_merges_it()
    {
        DemoWorkspace workspace = CreateWorkspace();
        IReadOnlyList<DemoReplica> replicas = await workspace.OpenReplicasAsync("doc", "A", "B", "C");

        try
        {
            await replicas[0].GoOfflineAsync();
            Assert.IsFalse(replicas[0].IsOnline);

            replicas[0].Document.Text("body").Insert(0, "[offline A]");
            replicas[1].Document.Text("body").Insert(0, "[B]");
            replicas[2].Document.Text("body").Insert(0, "[C]");

            // B and C can still see each other; A can see neither, and neither can see A.
            await WaitUntilAsync(
                () => Text(replicas[1]).Contains("[C]", StringComparison.Ordinal) &&
                      Text(replicas[2]).Contains("[B]", StringComparison.Ordinal),
                "B and C hold each other's edits");
            Assert.AreEqual(Text(replicas[1]), Text(replicas[2]));
            Assert.DoesNotContain("[offline A]", Text(replicas[1]));
            Assert.DoesNotContain("[B]", Text(replicas[0]));

            await replicas[0].GoOnlineAsync();

            // Everything survives, and all three agree.
            await WaitUntilAsync(() => AllConverged(replicas), "all three replicas converge");
            string merged = Text(replicas[0]);
            Assert.Contains("[offline A]", merged);
            Assert.Contains("[B]", merged);
            Assert.Contains("[C]", merged);
            BcrdtConvergenceHarness.AssertConverged([.. replicas.Select(r => r.Document.Crdt)]);
        }
        finally
        {
            await DisposeAllAsync(replicas);
        }
    }

    /// <summary>
    /// The collaborative text page's offline story when the offline replica only deletes.
    /// </summary>
    /// <remarks>
    /// A deletion does not advance a state vector, so a reconnecting replica whose only offline work
    /// was deleting looks, by state vector alone, as if it had nothing to contribute. This is the
    /// case the insertion-only test above cannot see.
    /// </remarks>
    [TestMethod]
    public async Task A_replica_that_only_deleted_while_offline_merges_on_reconnect()
    {
        DemoWorkspace workspace = CreateWorkspace();
        IReadOnlyList<DemoReplica> replicas = await workspace.OpenReplicasAsync("doc", "A", "B", "C");

        try
        {
            replicas[1].Document.Text("body").Insert(0, "shared sentence.");
            await WaitUntilAsync(
                () => replicas.All(replica => Text(replica) == "shared sentence."),
                "every replica holds the shared sentence");

            await replicas[0].GoOfflineAsync();
            replicas[0].Document.Text("body").Delete(0, "shared ".Length);
            Assert.AreEqual("sentence.", Text(replicas[0]));

            await replicas[0].GoOnlineAsync();

            await WaitUntilAsync(
                () => replicas.All(replica => Text(replica) == "sentence."),
                "A's offline deletion reaches B and C");
            BcrdtConvergenceHarness.AssertConverged([.. replicas.Select(r => r.Document.Crdt)]);
        }
        finally
        {
            await DisposeAllAsync(replicas);
        }
    }

    /// <summary>The text page's "Clear the document (from Replica A)" button, pressed while A is offline.</summary>
    [TestMethod]
    public async Task Clearing_the_document_while_offline_clears_it_everywhere_on_reconnect()
    {
        DemoWorkspace workspace = CreateWorkspace();
        IReadOnlyList<DemoReplica> replicas = await workspace.OpenReplicasAsync("doc", "A", "B", "C");

        try
        {
            replicas[2].Document.Text("body").Insert(0, "written by C");
            await WaitUntilAsync(
                () => replicas.All(replica => Text(replica) == "written by C"),
                "every replica holds C's text");

            await replicas[0].GoOfflineAsync();
            BcrdtText text = replicas[0].Document.Text("body");
            text.Delete(0, text.Count);

            await replicas[0].GoOnlineAsync();

            await WaitUntilAsync(
                () => replicas.All(replica => Text(replica).Length == 0),
                "the clear issued offline by A reaches B and C");
        }
        finally
        {
            await DisposeAllAsync(replicas);
        }
    }

    [TestMethod]
    public async Task An_abrupt_connection_drop_repairs_itself()
    {
        DemoWorkspace workspace = CreateWorkspace();
        IReadOnlyList<DemoReplica> replicas = await workspace.OpenReplicasAsync("doc", "A", "B");

        try
        {
            // Not a graceful disconnect: the plug is pulled, so the replica still wants a connection
            // and the provider's backoff loop is what brings it back - after a randomized delay, which
            // is why this waits for the outcome rather than for a fixed interval.
            replicas[0].SimulateNetworkDrop();
            replicas[1].Document.Text("body").Insert(0, "written while A was down");

            await WaitUntilAsync(
                () => Text(replicas[0]) == "written while A was down",
                "A reconnects on its own and receives what it missed");
            Assert.IsTrue(replicas[0].IsOnline);
        }
        finally
        {
            await DisposeAllAsync(replicas);
        }
    }

    [TestMethod]
    public async Task Replicas_see_each_other_in_their_presence_lists()
    {
        DemoWorkspace workspace = CreateWorkspace();
        IReadOnlyList<DemoReplica> replicas = await workspace.OpenReplicasAsync("doc", "A", "B", "C");

        try
        {
            await WaitUntilAsync(
                () => replicas.All(replica => replica.Document.Presence.Others.Count == 2),
                "every replica sees the other two");

            foreach (DemoReplica replica in replicas)
            {
                Assert.DoesNotContain(other => other.User.Name == replica.Label, replica.Document.Presence.Others);
            }
        }
        finally
        {
            await DisposeAllAsync(replicas);
        }
    }

    [TestMethod]
    public async Task A_replicas_store_survives_the_page_being_left_and_revisited()
    {
        DemoWorkspace workspace = CreateWorkspace();

        IReadOnlyList<DemoReplica> first = await workspace.OpenReplicasAsync("doc", "A", "B");
        first[0].Document.Text("body").Insert(0, "written before leaving");
        await first[0].Document.FlushAsync();
        await DisposeAllAsync(first);

        // The workspace keeps a store per replica, so revisiting a page finds the document where it
        // was left rather than empty.
        IReadOnlyList<DemoReplica> second = await workspace.OpenReplicasAsync("doc", "A", "B");
        try
        {
            await WaitUntilAsync(
                () => Text(second[0]) == "written before leaving",
                "the revisited replica holds what was written before leaving");
        }
        finally
        {
            await DisposeAllAsync(second);
        }
    }

    public static IEnumerable<object[]> ConflictScenarioIndices
    {
        get
        {
            for (int index = 0; index < ConflictLabSession.Scenarios.Count; index++) yield return [index];
        }
    }

    /// <summary>
    /// Runs one conflict-lab scenario on a fresh, empty document - the state a first visit starts
    /// from - through the same <see cref="ConflictLabSession"/> the page uses, seeding included.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(ConflictScenarioIndices))]
    public async Task Each_conflict_lab_scenario_converges_to_its_stated_outcome(int scenarioIndex)
    {
        DemoWorkspace workspace = CreateWorkspace();
        IReadOnlyList<DemoReplica> replicas = await workspace.OpenReplicasAsync("demo-conflicts", "Replica A", "Replica B");

        try
        {
            var session = new ConflictLabSession(replicas[0], replicas[1]);
            ConflictScenario scenario = ConflictLabSession.Scenarios[scenarioIndex];

            await session.RunAsync(scenario);

            Assert.IsTrue(
                session.Converged,
                $"'{scenario.Title}' left the replicas different:{Environment.NewLine}" +
                string.Join(Environment.NewLine, session.Log));
            BcrdtConvergenceHarness.AssertConverged(session.A.Document.Crdt, session.B.Document.Crdt);

            AssertStatedOutcome(scenario, session.A);
            AssertStatedOutcome(scenario, session.B);
        }
        finally
        {
            await DisposeAllAsync(replicas);
        }
    }

    /// <summary>
    /// The page's real usage: one scenario after another on the same document, then a reset.
    /// </summary>
    [TestMethod]
    public async Task Running_every_conflict_lab_scenario_in_turn_converges_each_time_and_resets_cleanly()
    {
        DemoWorkspace workspace = CreateWorkspace();
        IReadOnlyList<DemoReplica> replicas = await workspace.OpenReplicasAsync("demo-conflicts", "Replica A", "Replica B");

        try
        {
            var session = new ConflictLabSession(replicas[0], replicas[1]);

            foreach (ConflictScenario scenario in ConflictLabSession.Scenarios)
            {
                await session.RunAsync(scenario);
                Assert.IsTrue(
                    session.Converged,
                    $"'{scenario.Title}' left the replicas different:{Environment.NewLine}" +
                    string.Join(Environment.NewLine, session.Log));

                // However many scenarios have run, the seed was shared rather than duplicated.
                Assert.IsTrue(
                    CountOf(Text(session.A), "seed text. ") <= 1,
                    $"The seed text appears more than once after '{scenario.Title}': {Text(session.A)}");
            }

            await session.ResetAsync();

            Assert.IsEmpty(session.Log);
            Assert.IsTrue(session.Converged);
            foreach (DemoReplica replica in replicas)
            {
                Assert.AreEqual(0, replica.Document.Text(ConflictLabSession.TextKey).Count);
                Assert.AreEqual(0, replica.Document.Array(ConflictLabSession.ListKey).Count);
                Assert.AreEqual(0, replica.Document.GetMap(ConflictLabSession.MapKey).Count);
                Assert.AreEqual(0, replica.Document.Set(ConflictLabSession.SetKey).Count);
            }
        }
        finally
        {
            await DisposeAllAsync(replicas);
        }
    }

    /// <summary>
    /// The seed is created once, by A, while both replicas can see it - so both edit the same element.
    /// </summary>
    [TestMethod]
    public async Task The_conflict_lab_seed_is_shared_rather_than_created_on_each_replica()
    {
        DemoWorkspace workspace = CreateWorkspace();
        IReadOnlyList<DemoReplica> replicas = await workspace.OpenReplicasAsync("demo-conflicts", "Replica A", "Replica B");

        try
        {
            var session = new ConflictLabSession(replicas[0], replicas[1]);

            // A scenario that edits neither seeded field, so the seed is all there is to inspect.
            ConflictScenario differentKeys = ConflictLabSession.Scenarios
                .Single(scenario => scenario.Title == "Concurrent writes to different map keys");
            await session.RunAsync(differentKeys);

            foreach (DemoReplica replica in replicas)
            {
                Assert.AreSequenceEqual(new object?[] { "seed item" }, replica.Document.Array(ConflictLabSession.ListKey).ToArray());
                Assert.AreEqual("seed text. ", Text(replica));
                Assert.AreSequenceEqual(new object?[] { ConflictLabSession.SeedTag }, replica.Document.Set(ConflictLabSession.SetKey).ToArray());
            }
        }
        finally
        {
            await DisposeAllAsync(replicas);
        }
    }

    /// <summary>
    /// The version-history page's premise: replicas opened to keep their history can rebuild a version one of them
    /// saved, once they hold everything it names - and not before.
    /// </summary>
    [TestMethod]
    public async Task Replicas_that_keep_history_restore_a_version_saved_by_another()
    {
        DemoWorkspace workspace = CreateWorkspace();
        IReadOnlyList<DemoReplica> replicas = await workspace.OpenReplicasAsync("doc", keepHistory: true, "A", "B");

        try
        {
            Assert.IsTrue(replicas.All(replica => !replica.Document.Crdt.GarbageCollect), "the replicas reclaim deleted content");

            BcrdtText a = replicas[0].Document.Text("body");
            a.Insert(0, "first draft");
            byte[] saved = BcrdtSnapshot.Capture(replicas[0].Document.Crdt).Encode();
            a.Delete(0, 5);
            a.Insert(0, "second");
            await WaitUntilAsync(() => Text(replicas[1]) == "second draft", "B holds A's edits");

            BcrdtSnapshot version = BcrdtSnapshot.Decode(saved);
            Assert.IsTrue(replicas[1].Document.Crdt.GetStateVector().IsSupersetOf(version.StateVector));
            using (BcrdtDocument then = BcrdtSnapshot.RestoreDocument(replicas[1].Document.Crdt, version))
            {
                Assert.AreEqual("first draft", then.GetText("body").ToString());
            }

            // A version saved by A while B is offline names content B has not received.
            await replicas[1].GoOfflineAsync();
            a.Insert(a.Count, " and more");
            BcrdtSnapshot later = BcrdtSnapshot.Capture(replicas[0].Document.Crdt);
            Assert.IsFalse(replicas[1].Document.Crdt.GetStateVector().IsSupersetOf(later.StateVector));
        }
        finally
        {
            await DisposeAllAsync(replicas);
        }
    }

    [TestMethod]
    public void The_json_formatter_renders_a_document_projection_readably()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        doc.GetMap("meta").Set("title", "Example \"quoted\"");
        doc.GetArray("items").Push(1, "two", true, null);
        doc.GetText("body").Insert(0, "line one\nline two");

        string json = DemoJson.Format(doc.ToJson());

        Assert.Contains("\"title\"", json);
        Assert.Contains("\\\"quoted\\\"", json);
        Assert.Contains("\\n", json);
        Assert.Contains("true", json);
        Assert.Contains("null", json);
    }

    /// <summary>Checks, on one replica, the expectation a scenario states on the page.</summary>
    private static void AssertStatedOutcome(ConflictScenario scenario, DemoReplica replica)
    {
        string text = Text(replica);
        List<object?> list = replica.Document.Array(ConflictLabSession.ListKey).ToArray();
        BcrdtMap map = replica.Document.GetMap(ConflictLabSession.MapKey);
        BcrdtSet tags = replica.Document.Set(ConflictLabSession.SetKey);

        switch (scenario.Title)
        {
            case "Concurrent insertion at the same position":
                // Both runs survive, each contiguous, on top of the one shared seed.
                Assert.Contains("[from A]", text);
                Assert.Contains("[from B]", text);
                Assert.AreEqual(1, CountOf(text, "seed text. "));
                break;

            case "Concurrent deletion of the same item":
                // The one shared element disappears once; nothing else is removed.
                Assert.IsEmpty(list);
                Assert.AreEqual("seed text. ", text);
                break;

            case "Concurrent write to the same map key":
                Assert.Contains(map.Get("shared"), new object?[] { "written by A", "written by B" });
                break;

            case "Concurrent writes to different map keys":
                Assert.AreEqual("set by A", map.Get("only-a"));
                Assert.AreEqual("set by B", map.Get("only-b"));
                break;

            case "One replica edits text while the other deletes it":
                // B removed the seed it could see; A's appended run was never part of that.
                Assert.AreEqual("[appended by A]", text);
                break;

            case "Concurrent insertion into the same list position":
                Assert.AreEqual(3, list.Count);
                Assert.Contains("inserted by A", list);
                Assert.Contains("inserted by B", list);
                Assert.ContainsSingle(value => Equals(value, "seed item"), list);
                break;

            case "Concurrent addition of the same tag":
                // Two additions of an equal value are one member, beside the untouched seed.
                Assert.AreEqual(2, tags.Count);
                Assert.IsTrue(tags.Contains("urgent"));
                Assert.IsTrue(tags.Contains(ConflictLabSession.SeedTag));
                break;

            case "One replica removes a tag while the other re-adds it":
                // A removed the member it saw; B's re-addition is a member A never saw, so it stands.
                Assert.AreEqual(1, tags.Count);
                Assert.IsTrue(tags.Contains(ConflictLabSession.SeedTag));
                break;

            default:
                Assert.Fail($"No stated outcome is checked for '{scenario.Title}'; add one.");
                break;
        }
    }

    private static int CountOf(string text, string fragment)
    {
        int count = 0;
        for (int at = text.IndexOf(fragment, StringComparison.Ordinal);
             at >= 0;
             at = text.IndexOf(fragment, at + fragment.Length, StringComparison.Ordinal))
        {
            count++;
        }
        return count;
    }
}
