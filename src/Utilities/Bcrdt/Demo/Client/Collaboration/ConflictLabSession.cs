using System.Diagnostics;

namespace Bit.Bcrdt.Demo.Client.Collaboration;

/// <summary>One deliberately provoked conflict: what each replica does while isolated, and what should follow.</summary>
/// <param name="Title">The scenario's heading.</param>
/// <param name="Description">What the two edits are.</param>
/// <param name="Expectation">The outcome the merge must produce, stated before the scenario runs.</param>
/// <param name="EditA">Replica A's edit, made while it cannot see B.</param>
/// <param name="EditB">Replica B's edit, made while it cannot see A.</param>
public sealed record ConflictScenario(
    string Title,
    string Description,
    string Expectation,
    Action<DemoReplica> EditA,
    Action<DemoReplica> EditB);

/// <summary>
/// The conflict laboratory's procedure, independent of the page that displays it.
/// </summary>
/// <remarks>
/// Kept out of the Razor markup so the exact sequence the page runs - seed, isolate, edit both sides,
/// reconnect, compare - can be exercised by a test without a browser. A test that re-implemented the
/// sequence would only prove that its own copy works.
/// </remarks>
public sealed class ConflictLabSession
{
    /// <summary>The text field every text scenario edits.</summary>
    public const string TextKey = "body";

    /// <summary>The sequence every list scenario edits.</summary>
    public const string ListKey = "items";

    /// <summary>The map every key scenario writes.</summary>
    public const string MapKey = "meta";

    /// <summary>The set every tag scenario edits.</summary>
    public const string SetKey = "tags";

    /// <summary>The tag every replica starts from, so a scenario can remove a member both of them hold.</summary>
    public const string SeedTag = "seed tag";

    private readonly List<string> _log = [];

    /// <summary>Creates a session over two replicas of the same document.</summary>
    public ConflictLabSession(DemoReplica a, DemoReplica b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        A = a;
        B = b;

        // Both replicas name all four fields up front, exactly as rendering the page does. A root
        // that one replica has asked for by kind and the other has not would project differently,
        // and the comparison would report a difference that is not in the data.
        foreach (DemoReplica replica in new[] { a, b })
        {
            replica.Document.Text(TextKey);
            replica.Document.Array(ListKey);
            replica.Document.GetMap(MapKey);
            replica.Document.Set(SetKey);
        }
    }

    /// <summary>How long to wait for the replicas to exchange what they hold before reporting.</summary>
    public static TimeSpan SettleTimeout { get; } = TimeSpan.FromSeconds(2);

    /// <summary>Replica A.</summary>
    public DemoReplica A { get; }

    /// <summary>Replica B.</summary>
    public DemoReplica B { get; }

    /// <summary>What happened, in order, since the last reset.</summary>
    public IReadOnlyList<string> Log => _log;

    /// <summary>Whether both replicas hold identical state.</summary>
    public bool Converged => DemoJson.Format(A.Document.ToJson()) == DemoJson.Format(B.Document.ToJson());

    /// <summary>The scenarios the page offers.</summary>
    public static IReadOnlyList<ConflictScenario> Scenarios { get; } =
    [
        new(
            "Concurrent insertion at the same position",
            "Both replicas insert different text at exactly the same index, neither aware of the other. " +
            "This is the case that a naïve merge interleaves character by character into nonsense.",
            "Both insertions survive intact and each stays contiguous - you get one run then the other, " +
            "never letters from the two alternating. Both replicas agree on which comes first.",
            replica => replica.Document.Text(TextKey).Insert(0, "[from A]"),
            replica => replica.Document.Text(TextKey).Insert(0, "[from B]")),

        new(
            "Concurrent deletion of the same item",
            "Both replicas delete the same list element at the same time.",
            "The element disappears exactly once. Deleting twice is not an error and does not remove a " +
            "second element - deletions are idempotent, which is why duplicate delivery is harmless.",
            DeleteFirst,
            DeleteFirst),

        new(
            "Concurrent write to the same map key",
            "Both replicas set the same key to different values.",
            "One value wins and both replicas agree on which. This is last-writer-wins, and it is the " +
            "one place the engine genuinely discards data - model the field as a nested type if that " +
            "is unacceptable.",
            replica => replica.Document.GetMap(MapKey).Set("shared", "written by A"),
            replica => replica.Document.GetMap(MapKey).Set("shared", "written by B")),

        new(
            "Concurrent writes to different map keys",
            "Each replica writes a key the other does not touch.",
            "Both writes survive. Independent fields never conflict, so a form with separate fields is " +
            "safe even though any single field is last-writer-wins.",
            replica => replica.Document.GetMap(MapKey).Set("only-a", "set by A"),
            replica => replica.Document.GetMap(MapKey).Set("only-b", "set by B")),

        new(
            "One replica edits text while the other deletes it",
            "A appends to the text; B deletes the whole thing. Neither sees the other.",
            "B's deletion removes the characters it could actually see, and A's new characters survive - " +
            "they were never part of what B deleted. Deletion applies to observed content, not to a range " +
            "of positions that may since have come to mean something else.",
            replica => replica.Document.Text(TextKey).Insert(replica.Document.Text(TextKey).Count, "[appended by A]"),
            ClearText),

        new(
            "Concurrent insertion into the same list position",
            "Both replicas insert an element at the front of the list.",
            "Both elements are present, in an order both replicas agree on. Neither insertion is lost and " +
            "neither overwrites the other.",
            replica => replica.Document.Array(ListKey).Insert(0, "inserted by A"),
            replica => replica.Document.Array(ListKey).Insert(0, "inserted by B")),

        new(
            "Concurrent addition of the same tag",
            "Both replicas add the tag \"urgent\" to a set, neither aware of the other.",
            "The set holds \"urgent\" once, not twice. A set's members are keyed by their value, so two " +
            "additions of an equal value converge on a single member - which is what tags, labels and " +
            "membership need.",
            replica => replica.Document.Set(SetKey).Add("urgent"),
            replica => replica.Document.Set(SetKey).Add("urgent")),

        new(
            "One replica removes a tag while the other re-adds it",
            $"Both start with the tag \"{SeedTag}\". A removes it; B removes it and adds it back - a label " +
            "unticked and ticked again. Neither sees the other.",
            "The tag stays: an add wins over a concurrent remove. A's removal deleted the member A could " +
            "see, and B's re-addition is a new member A never saw. Adding a tag the set already holds " +
            "writes nothing, though, so a plain add racing a remove would let the remove stand.",
            replica => replica.Document.Set(SetKey).Remove(SeedTag),
            replica =>
            {
                BcrdtSet tags = replica.Document.Set(SetKey);
                tags.Remove(SeedTag);
                tags.Add(SeedTag);
            }),
    ];

    /// <summary>
    /// Runs one scenario: share a starting point, isolate, edit both sides, reconnect, report.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The starting point is established while both replicas are connected, and only by A. Seeding
    /// each replica after isolating it would give each its own, different seed element - two
    /// "seed item"s that merely look alike - so a scenario such as "concurrent deletion of the same
    /// item" would in fact delete two different items, and the merged document would carry both
    /// seeds. Seeding from one replica and letting it arrive at the other is what makes the seed
    /// genuinely shared.
    /// </para>
    /// <para>
    /// Disconnecting is what makes the two edits genuinely concurrent. Without it the first edit
    /// would reach the second replica before the second edit was made, and the second edit would be
    /// causally after it rather than concurrent with it - a different situation entirely, and not
    /// the one anybody is worried about.
    /// </para>
    /// </remarks>
    public async Task RunAsync(ConflictScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        _log.Add($"- {scenario.Title} -");

        await A.GoOnlineAsync();
        await B.GoOnlineAsync();
        if (SeedFromA())
        {
            await WaitUntilAsync(BHoldsEverythingAInserted);
            _log.Add($"Seeded from Replica A while both were connected. Both now hold: {Summarize(B)}");
        }

        await A.GoOfflineAsync();
        await B.GoOfflineAsync();
        _log.Add("Both replicas disconnected, so neither can see what the other is about to do.");

        scenario.EditA(A);
        _log.Add($"Replica A edited. It now holds: {Summarize(A)}");

        scenario.EditB(B);
        _log.Add($"Replica B edited. It now holds: {Summarize(B)}");

        await A.GoOnlineAsync();
        await B.GoOnlineAsync();

        // The handshake runs on reconnect and is what reconciles the divergence. Wait for it to
        // finish rather than for a fixed interval, up to a bound, then report what is there.
        bool converged = await WaitUntilAsync(() => Converged);

        _log.Add($"Reconnected. Replica A holds: {Summarize(A)}");
        _log.Add($"Reconnected. Replica B holds: {Summarize(B)}");
        _log.Add(converged
            ? "The two replicas are identical."
            : "The two replicas still differ - give the exchange another moment.");
    }

    /// <summary>Clears all four fields on both replicas and empties the log.</summary>
    /// <remarks>
    /// Both replicas clear, not just one. A replica can only delete what it can see, so a reset
    /// issued from A alone would leave behind anything B holds that never reached A. B's pass is
    /// usually a no-op, and deleting content the other replica already deleted is idempotent.
    /// </remarks>
    public async Task ResetAsync()
    {
        await A.GoOnlineAsync();
        await B.GoOnlineAsync();

        ClearAll(A);
        ClearAll(B);

        await WaitUntilAsync(() => Converged && IsEmpty(A) && IsEmpty(B));
        _log.Clear();
    }

    /// <summary>A one-line description of what a replica holds.</summary>
    public static string Summarize(DemoReplica replica)
    {
        ArgumentNullException.ThrowIfNull(replica);

        string text = replica.Document.Text(TextKey).ToString();
        int items = replica.Document.Array(ListKey).Count;
        int keys = replica.Document.GetMap(MapKey).Count;
        int tags = replica.Document.Set(SetKey).Count;
        return $"text {Quote(text)}, {items} list item(s), {keys} map key(s), {tags} tag(s)";
    }

    /// <summary>Quotes a value for display, naming the empty string explicitly.</summary>
    public static string Quote(string value) =>
        string.IsNullOrEmpty(value) ? "(empty)" : $"“{value}”";

    /// <summary>Gives the delete, edit-versus-delete and remove-versus-add scenarios something to act on.</summary>
    /// <returns>Whether anything had to be written.</returns>
    private bool SeedFromA()
    {
        BcrdtArray list = A.Document.Array(ListKey);
        BcrdtText text = A.Document.Text(TextKey);
        BcrdtSet tags = A.Document.Set(SetKey);
        if (list.Count > 0 && text.Count > 0 && tags.Contains(SeedTag)) return false;

        // One transaction, so the seed reaches B as a single update.
        A.Document.Transact(_ =>
        {
            if (list.Count == 0) list.Push("seed item");
            if (text.Count == 0) text.Insert(0, "seed text. ");
            tags.Add(SeedTag);
        });
        return true;
    }

    private bool BHoldsEverythingAInserted() =>
        B.Document.Crdt.GetStateVector().IsSupersetOf(A.Document.Crdt.GetStateVector());

    private static void DeleteFirst(DemoReplica replica)
    {
        BcrdtArray list = replica.Document.Array(ListKey);
        if (list.Count > 0) list.Delete(0, 1);
    }

    private static void ClearText(DemoReplica replica)
    {
        BcrdtText text = replica.Document.Text(TextKey);
        if (text.Count > 0) text.Delete(0, text.Count);
    }

    private static void ClearAll(DemoReplica replica)
    {
        // One transaction, so the reset reaches the peer as a single update rather than four.
        using BcrdtCollaborativeTransaction transaction = replica.Document.BeginTransaction();
        ClearText(replica);
        BcrdtArray list = replica.Document.Array(ListKey);
        if (list.Count > 0) list.Delete(0, list.Count);
        replica.Document.GetMap(MapKey).Clear();
        replica.Document.Set(SetKey).Clear();
    }

    private static bool IsEmpty(DemoReplica replica) =>
        replica.Document.Text(TextKey).Count == 0 &&
        replica.Document.Array(ListKey).Count == 0 &&
        replica.Document.GetMap(MapKey).Count == 0 &&
        replica.Document.Set(SetKey).Count == 0;

    private static async Task<bool> WaitUntilAsync(Func<bool> condition)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition())
        {
            if (elapsed.Elapsed >= SettleTimeout) return false;
            await Task.Delay(25);
        }
        return true;
    }
}
