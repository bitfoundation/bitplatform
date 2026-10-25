using System.Text;
using System.Text.Json;
using System.Globalization;
using System.Text.Json.Nodes;
using Bit.Bcrdt.Demo.Server.Dtos;

namespace Bit.Bcrdt.Demo.Server.Services;

/// <summary>
/// Answers "what will the merge decide?" by running it: each replica gets its own <see cref="BcrdtDocument"/>,
/// makes its edits with no connection to the others, and then receives every other replica's updates - in every
/// order the replicas could be heard in, and once with every update delivered newest first - while the lab records
/// what each replica held before, what they all hold after, and the size of every update on the way.
/// <para>
/// Nothing here models the CRDT. The documents, the updates and the merge are Bit.Bcrdt's own, so the outcome
/// reported is the outcome an application gets - including the cases people do not expect: a nested text two
/// replicas both created while apart (one of the two wins, and what was typed into the other is gone), an add
/// to a set that already held the element (a no-op, so a concurrent remove takes it out), two moves of one
/// element (two copies).
/// </para>
/// </summary>
public static class BcrdtMergeLab
{
    public const int MaxReplicas = 4;

    /// <summary>The most statements one run takes; past it the input is a script rather than a scenario.</summary>
    private const int MaxStatements = 200;

    /// <summary>
    /// The longest input the lab reads. Every replica is rebuilt once per delivery order - 25 times at four
    /// replicas - so the input is what bounds the work one anonymous request can cause.
    /// </summary>
    private const int MaxInputLength = 20_000;

    private const string BaseLabel = "base";

    /// <summary>The client id the shared starting state is written under, apart from every replica's.</summary>
    private const ulong BaseClientId = 1000;

    public static readonly string[] Kinds = ["text", "map", "array", "set"];

    /// <summary>What runs when no operations are passed: one contested map key and one contested text.</summary>
    public const string SampleOperations = """
        base: text notes insert 0 "Agenda:"
        A: map meta set title "Q3 planning"
        B: map meta set title "Q3 review"
        A: text notes insert 7 " budget"
        B: text notes insert 7 " hiring"
        """;

    /// <summary>The input format, as every error and the sample's notes spell it out.</summary>
    public const string Grammar =
        "One operation per line (or separated by ';'), as '<replica>: <type> <name> <action> <arguments>' with values in JSON - " +
        "text <name> insert <index> \"text\" [{attributes}] | delete <index> <length> | format <index> <length> {attributes}; " +
        "map <name> set <key> <value> | delete <key>; " +
        "array <name> insert <index> <value>... | push <value>... | delete <index> [<length>] | move <from> <to>; " +
        "set <name> add <value> | remove <value>. " +
        "Replicas are any labels (A, B, ...), two to four of them; 'base:' lines build the state every replica starts from. " +
        "A name 'root/key' is the type nested at that key of the root map 'root', created there if it is not one yet. " +
        "A JSON object { \"base\": [...], \"A\": [...], \"B\": [...] } of operation lists works too.";

    private sealed record Operation(int Number, string Label, string Kind, string Root, string? Key, string Action, IReadOnlyList<object?> Arguments, string Source)
    {
        /// <summary>Where the operation lands: the root's name, or "root/key" for a type nested in a root map.</summary>
        public string Path => Key is null ? Root : $"{Root}/{Key}";

        public string Line => $"{Label}: {Source}";
    }

    /// <summary>
    /// One operation as a replica ran it: the update it produced, if it changed anything at all, and whether it had
    /// to create the nested type it went into - a write to the root map's key like any other.
    /// </summary>
    private sealed record Step(Operation Operation, byte[]? Update, bool Created);

    private sealed record Replica(string Label, ulong ClientId, BcrdtDocument Document, Step[] Steps);

    private sealed class LabException(string message) : Exception(message);

    public static BcrdtMergeReportDto Inspect(string? operations)
    {
        var sample = string.IsNullOrWhiteSpace(operations);
        var input = sample ? SampleOperations : operations!;

        if (input.Length > MaxInputLength)
        {
            return Failed([], $"The operations are {input.Length:N0} characters long; the lab reads at most {MaxInputLength:N0}. A merge scenario is a handful of concurrent edits, not a document.");
        }

        List<Operation> parsed;
        try
        {
            parsed = Parse(input);
        }
        catch (LabException ex)
        {
            return Failed([], ex.Message);
        }

        string[] lines = [.. parsed.Select(op => op.Line)];
        string[] labels = [.. parsed.Where(op => op.Label != BaseLabel).Select(op => op.Label).Distinct(StringComparer.Ordinal)];

        if (labels.Length < 2)
        {
            return Failed(lines, $"A merge needs at least two replicas, and the operations name {(labels.Length == 0 ? "none" : $"only '{labels[0]}'")}. Label each line with the replica that makes the edit - 'A:', 'B:' - and put the shared starting state on 'base:' lines.");
        }

        if (labels.Length > MaxReplicas)
        {
            return Failed(lines, $"The operations name {labels.Length} replicas ({string.Join(", ", labels)}); the lab merges at most {MaxReplicas}.");
        }

        Dictionary<string, string> roots;
        try
        {
            roots = RootKinds(parsed);
        }
        catch (LabException ex)
        {
            return Failed(lines, ex.Message);
        }

        try
        {
            return Run(parsed, lines, labels, roots, sample);
        }
        catch (LabException ex)
        {
            return Failed(lines, ex.Message);
        }
    }

    private static BcrdtMergeReportDto Run(List<Operation> operations, string[] lines, string[] labels, Dictionary<string, string> roots, bool sample)
    {
        // The shared starting state: built once, under a client id of its own, and handed to every replica
        // before any of them goes its own way.
        using var origin = Create(BaseClientId, roots);
        var baseSteps = Execute(origin, operations.Where(op => op.Label == BaseLabel), BaseLabel);
        byte[] baseUpdate = origin.EncodeStateAsUpdate();
        var baseVector = origin.GetStateVector();
        var baseState = Project(origin);

        Replica Build(int index) => BuildReplica(labels[index], (ulong)index + 1, roots, baseUpdate, operations);

        // Filled one by one inside the try, so a replica whose operations fail does not leave the ones before it open.
        var built = new List<Replica>(labels.Length);

        try
        {
            for (int i = 0; i < labels.Length; i++) built.Add(Build(i));

            var replicas = built.ToArray();
            var before = replicas.ToDictionary(replica => replica.Label, replica => Project(replica.Document), StringComparer.Ordinal);
            var formattingBefore = replicas.ToDictionary(replica => replica.Label, replica => Formatting(replica.Document, roots), StringComparer.Ordinal);

            // Every order the replicas' batches could arrive in, then every update newest first - the order that
            // makes the most of them wait in the pending queue for the update they depend on.
            var schedules = Permutations(labels.Length)
                .Select(order => (Name: $"{string.Join(" then ", order.Select(i => labels[i]))}, each replica's updates in order", Order: order, NewestFirst: false))
                .Append((Name: "every update newest first", Order: Enumerable.Range(0, labels.Length).Reverse().ToArray(), NewestFirst: true))
                .ToArray();

            string? reference = null;
            JsonNode? merged = null;
            JsonNode? mergedFormatting = null;
            var mergedBytes = 0;
            var diverged = new List<string>();

            foreach (var schedule in schedules)
            {
                var receivers = Enumerable.Range(0, labels.Length).Select(Build).ToArray();

                try
                {
                    foreach (var receiver in receivers)
                    {
                        foreach (var sender in schedule.Order.Select(i => replicas[i]).Where(sender => sender.Label != receiver.Label))
                        {
                            var updates = sender.Steps.Select(step => step.Update).OfType<byte[]>();

                            foreach (var update in schedule.NewestFirst ? updates.Reverse() : updates) receiver.Document.ApplyUpdate(update);
                        }
                    }

                    foreach (var receiver in receivers)
                    {
                        var state = Project(receiver.Document);
                        var formatting = Formatting(receiver.Document, roots);
                        var snapshot = $"{state?.ToJsonString()}|{formatting?.ToJsonString()}";

                        if (reference is null)
                        {
                            reference = snapshot;
                            merged = state;
                            mergedFormatting = formatting;
                            mergedBytes = receiver.Document.EncodeStateAsUpdate().Length;
                        }
                        else if (snapshot != reference)
                        {
                            diverged.Add($"{schedule.Name}: replica {receiver.Label} differs");
                            break;
                        }
                    }
                }
                finally
                {
                    foreach (var receiver in receivers) receiver.Document.Dispose();
                }
            }

            var decisions = Decide(replicas, before, merged);

            return new BcrdtMergeReportDto
            {
                Operations = lines,
                Sample = sample,
                Base = baseSteps.Length == 0 ? null : baseState,
                Replicas = [.. replicas.Select(replica => new BcrdtMergeReplicaDto
                {
                    Label = replica.Label,
                    ClientId = replica.ClientId,
                    Updates = [.. replica.Steps.Select(step => new BcrdtMergeUpdateDto { Operation = step.Operation.Source, Bytes = step.Update?.Length ?? 0 })],
                    Before = before[replica.Label],
                    FormattingBefore = formattingBefore[replica.Label],
                    DeltaBytes = replica.Document.EncodeStateAsUpdate(baseVector).Length
                })],
                Converged = diverged.Count == 0,
                Schedules = schedules.Length,
                Diverged = diverged.Count == 0 ? null : [.. diverged],
                Merged = merged,
                MergedFormatting = mergedFormatting,
                MergedBytes = mergedBytes,
                Decisions = decisions,
                Notes = Notes(operations, replicas, schedules.Length, diverged.Count == 0, decisions, sample)
            };
        }
        finally
        {
            foreach (var replica in built) replica.Document.Dispose();
        }
    }

    private static BcrdtMergeReportDto Failed(string[] lines, string error) => new()
    {
        Operations = lines,
        Error = error,
        Notes = [Grammar, $"For example: {SampleOperations.ReplaceLineEndings("; ")}"]
    };

    /// <summary>
    /// A document with every root declared by the kind the operations use it as - what an application does when it
    /// asks for its roots. A root that only arrived from a peer would otherwise stay untyped, and project as
    /// whatever its content looks like rather than as what it is.
    /// </summary>
    private static BcrdtDocument Create(ulong clientId, Dictionary<string, string> roots)
    {
        var document = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = clientId });

        foreach (var (name, kind) in roots)
        {
            _ = kind switch
            {
                "text" => (BcrdtAbstractType)document.GetText(name),
                "array" => document.GetArray(name),
                "set" => document.GetSet(name),
                _ => document.GetMap(name)
            };
        }

        return document;
    }

    /// <summary>
    /// One replica, as it stands once it has made its own edits: the shared state, then its operations, each as a
    /// transaction of its own. It is deterministic - the same client id, starting state and operations make the same
    /// updates - which is what lets every delivery order start from a fresh, identical set of replicas.
    /// </summary>
    private static Replica BuildReplica(string label, ulong clientId, Dictionary<string, string> roots, byte[] baseUpdate, List<Operation> operations)
    {
        var document = Create(clientId, roots);

        try
        {
            document.ApplyUpdate(baseUpdate);

            return new Replica(label, clientId, document, Execute(document, operations.Where(op => op.Label == label), label));
        }
        catch
        {
            document.Dispose();
            throw;
        }
    }

    private static Step[] Execute(BcrdtDocument document, IEnumerable<Operation> operations, string label)
    {
        var steps = new List<Step>();
        byte[]? update = null;

        void Capture(BcrdtUpdateEventArgs args) => update = args.Update;

        document.Updated += Capture;
        try
        {
            foreach (var operation in operations)
            {
                update = null;

                // Ensure* creates the nested type - and writes the key - only when the key does not hold one of that
                // kind yet, so whether this operation creates it is a question about the replica as it stands now.
                var created = operation.Key is not null && Holds(document.GetMap(operation.Root).Get(operation.Key), operation.Kind) is false;

                try
                {
                    document.Transact(_ => Apply(document, operation));
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException or InvalidCastException)
                {
                    var who = label == BaseLabel ? "while building the starting state" : $"on replica {label}";
                    var hint = ex is ArgumentOutOfRangeException
                        ? " Indexes count from 0 in what that replica holds at that point - the starting state plus its own earlier lines, never another replica's."
                        : string.Empty;

                    throw new LabException($"Statement {operation.Number} ('{operation.Line}') failed {who}: {ex.Message}{hint}");
                }

                steps.Add(new Step(operation, update, created));
            }
        }
        finally
        {
            document.Updated -= Capture;
        }

        return [.. steps];
    }

    private static bool Holds(object? value, string kind) => kind switch
    {
        "text" => value is BcrdtText,
        "array" => value is BcrdtArray,
        "set" => value is BcrdtSet,
        _ => value is BcrdtMap
    };

    private static void Apply(BcrdtDocument document, Operation op)
    {
        var a = op.Arguments;

        switch (op.Kind, op.Action)
        {
            case ("text", "insert"):
                Text(document, op).Insert((int)a[0]!, (string)a[1]!, a.Count > 2 ? (IReadOnlyDictionary<string, object?>?)a[2] : null);
                break;
            case ("text", "delete"):
                Text(document, op).Delete((int)a[0]!, (int)a[1]!);
                break;
            case ("text", "format"):
                Text(document, op).Format((int)a[0]!, (int)a[1]!, (IReadOnlyDictionary<string, object?>)a[2]!);
                break;
            case ("map", "set"):
                Map(document, op).Set((string)a[0]!, a[1]);
                break;
            case ("map", "delete"):
                Map(document, op).Delete((string)a[0]!);
                break;
            case ("array", "insert"):
                Array(document, op).Insert((int)a[0]!, [.. a.Skip(1)]);
                break;
            case ("array", "push"):
                Array(document, op).Push([.. a]);
                break;
            case ("array", "delete"):
                Array(document, op).Delete((int)a[0]!, a.Count > 1 ? (int)a[1]! : 1);
                break;
            case ("array", "move"):
                Array(document, op).Move((int)a[0]!, (int)a[1]!);
                break;
            case ("set", "add"):
                Set(document, op).Add(a[0]);
                break;
            case ("set", "remove"):
                Set(document, op).Remove(a[0]);
                break;
        }
    }

    private static BcrdtText Text(BcrdtDocument document, Operation op) => op.Key is null ? document.GetText(op.Root) : document.GetMap(op.Root).EnsureText(op.Key);

    private static BcrdtMap Map(BcrdtDocument document, Operation op) => op.Key is null ? document.GetMap(op.Root) : document.GetMap(op.Root).EnsureMap(op.Key);

    private static BcrdtArray Array(BcrdtDocument document, Operation op) => op.Key is null ? document.GetArray(op.Root) : document.GetMap(op.Root).EnsureArray(op.Key);

    private static BcrdtSet Set(BcrdtDocument document, Operation op) => op.Key is null ? document.GetSet(op.Root) : document.GetMap(op.Root).EnsureSet(op.Key);

    // ---- What the merge decided -------------------------------------------------------------------------

    /// <summary>
    /// A decision for everything more than one replica changed: each map key, each text, array and set. A map key
    /// is also contested when a replica created a nested type at it - an operation on 'root/key' while the key held
    /// no such type - because that creation is a write to the key like any other.
    /// </summary>
    private static BcrdtMergeDecisionDto[] Decide(Replica[] replicas, Dictionary<string, JsonNode?> before, JsonNode? merged)
    {
        var decisions = new List<BcrdtMergeDecisionDto>();
        var steps = replicas.SelectMany(replica => replica.Steps).ToArray();

        // Map keys: (map path, key) -> the replicas' writes to it, in order.
        var keys = new Dictionary<(string Map, string Key), List<Step>>();

        foreach (var step in steps)
        {
            var op = step.Operation;

            if (op.Kind == "map") Add(keys, (op.Path, (string)op.Arguments[0]!), step);
            if (step.Created) Add(keys, (op.Root, op.Key!), step);
        }

        foreach (var ((map, key), writes) in keys)
        {
            var writers = writes.Select(step => step.Operation.Label).Distinct(StringComparer.Ordinal).ToArray();
            if (writers.Length < 2) continue;

            decisions.Add(DecideKey(map, key, writers, writes, before, merged));
        }

        foreach (var group in steps.Where(step => step.Operation.Kind != "map").GroupBy(step => (step.Operation.Kind, step.Operation.Path)))
        {
            var editors = group.Select(step => step.Operation.Label).Distinct(StringComparer.Ordinal).ToArray();
            if (editors.Length < 2) continue;

            decisions.Add(group.Key.Kind switch
            {
                "text" => DecideText(group.Key.Path, editors, [.. group], merged),
                "array" => DecideArray(group.Key.Path, editors, [.. group], before, merged),
                _ => DecideSet(group.Key.Path, editors, [.. group], merged)
            });
        }

        return [.. decisions];

        static void Add(Dictionary<(string, string), List<Step>> keys, (string, string) key, Step step)
        {
            if (keys.TryGetValue(key, out var list) is false) keys[key] = list = [];
            list.Add(step);
        }
    }

    private static BcrdtMergeDecisionDto DecideKey(string map, string key, string[] writers, List<Step> writes, Dictionary<string, JsonNode?> before, JsonNode? merged)
    {
        string[] path = [.. map.Split('/'), key];
        var present = TryGet(merged, path, out var value);
        var target = $"map '{map}', key '{key}'";

        // What each writer did to the key last - its final word on it is the write that competes.
        var described = writers.Select(label => $"{label} {Describe(writes.Last(step => step.Operation.Label == label), map, key)}").ToArray();

        // The write that won is the one whose replica held, before the merge, exactly what everyone holds after it.
        var winners = writers.Where(label => TryGet(before[label], path, out var held) == present && (present is false || JsonNode.DeepEquals(held, value))).ToArray();

        if (winners.Length == writers.Length)
        {
            return new BcrdtMergeDecisionDto
            {
                Target = target,
                Replicas = writers,
                Outcome = "one value wins",
                Detail = $"{Join(described)} while apart - to the same result, so every replica holds {(present ? Render(value) : "no such key")} whichever write won."
            };
        }

        var losers = writers.Except(winners, StringComparer.Ordinal).ToArray();
        var created = writes.Where(step => step.Created).ToArray();

        var detail = new StringBuilder($"{Join(described)} while apart. One value wins: ");
        detail.Append(present ? Render(value) : "the delete, so the key is gone");
        if (winners.Length > 0) detail.Append($" ({Join(winners)}'s write)");
        detail.Append($" - every replica now holds that, and {Join(losers)}'s write is gone. A map key holds one value, picked deterministically but arbitrarily - by client id here, by random session ids in an app - and never by time.");

        detail.Append(created.Length > 0
            ? $" A nested {created[0].Operation.Kind} created while apart is a value of '{key}' like any other, so everything written into a losing one went with it. Create the nested type once, before people edit it - a 'base:' line here - and concurrent edits inside it merge."
            : " If both edits must survive, make the value a nested BcrdtText (or map) created once before people edit it, or give each writer a key of its own.");

        return new BcrdtMergeDecisionDto
        {
            Target = target,
            Replicas = writers,
            Outcome = "one value wins",
            Detail = detail.ToString()
        };
    }

    private static BcrdtMergeDecisionDto DecideText(string path, string[] editors, Step[] steps, JsonNode? merged)
    {
        var text = TryGet(merged, path.Split('/'), out var value) && value is JsonValue node && node.TryGetValue<string>(out var s) ? s : null;

        var inserted = steps.Where(step => step.Operation.Action == "insert")
                            .Select(step => (step.Operation.Label, Text: (string)step.Operation.Arguments[1]!))
                            .ToArray();

        var detail = new StringBuilder();
        var described = editors.Select(label => $"{label} {string.Join(", ", steps.Where(step => step.Operation.Label == label).Select(step => Describe(step, null, null)))}").ToArray();

        detail.Append($"{Join(described)} while apart.");

        var missing = inserted.Where(insert => text is null || text.Contains(insert.Text, StringComparison.Ordinal) is false).ToArray();

        if (inserted.Length > 0)
        {
            detail.Append(missing.Length == 0
                ? $" Every insertion survives, each as one contiguous run: every replica now reads {Quote(text)}. Insertions at the same index are ordered by client id, the same way on every replica."
                : $" Every replica now reads {Quote(text)}; {string.Join(", ", missing.Select(m => $"{m.Label}'s {Quote(m.Text)}"))} is not in it - deleted by its own replica later, or written into a nested text that lost a race for its map key (see that decision).");
        }
        else
        {
            detail.Append($" Every replica now reads {Quote(text)}.");
        }

        if (steps.Any(step => step.Operation.Action == "delete"))
        {
            detail.Append(" A deletion removes only what its replica saw: a range deleted on both is deleted once, and text inserted concurrently inside it survives.");
        }

        if (steps.Any(step => step.Operation.Action == "format"))
        {
            detail.Append(" Marks occupy no index space and merge per attribute: different attributes all apply, while one attribute set to different values on the same characters resolves to one of them - see mergedFormatting.");
        }

        return new BcrdtMergeDecisionDto
        {
            Target = $"text '{path}'",
            Replicas = editors,
            Outcome = inserted.Length == 0 ? $"{Both(editors)} apply" : missing.Length > 0 ? "an insertion is lost with its map key" : $"{Both(editors)} survive",
            Detail = detail.ToString()
        };
    }

    private static BcrdtMergeDecisionDto DecideArray(string path, string[] editors, Step[] steps, Dictionary<string, JsonNode?> before, JsonNode? merged)
    {
        TryGet(merged, path.Split('/'), out var value);
        var items = value as JsonArray;

        var described = editors.Select(label => $"{label} {string.Join(", ", steps.Where(step => step.Operation.Label == label).Select(step => Describe(step, null, null)))}").ToArray();
        var detail = new StringBuilder($"{Join(described)} while apart. Every replica now holds {Render(value)}.");

        var movers = steps.Where(step => step.Operation.Action == "move" && step.Update is not null).Select(step => step.Operation.Label).Distinct(StringComparer.Ordinal).ToArray();

        // An element the merge holds more often than any replica held it is a copy a concurrent move left behind.
        var duplicated = items is null ? [] : items
            .GroupBy(item => item?.ToJsonString() ?? "null")
            .Where(group => group.Count() > editors.Max(label => TryGet(before[label], path.Split('/'), out var held) && held is JsonArray list ? list.Count(item => (item?.ToJsonString() ?? "null") == group.Key) : 0))
            .Select(group => group.Key)
            .ToArray();

        if (steps.Any(step => step.Operation.Action is "insert" or "push"))
        {
            detail.Append(" Every insertion survives, in a stable order the same on every replica; elements inserted at the same index are ordered by client id.");
        }

        if (movers.Length > 0)
        {
            detail.Append(" A move is a delete and an insert of a copy, so a concurrent edit to the moved element is lost with the original");
            detail.Append(duplicated.Length > 0
                ? $", and an element moved on two replicas ends up twice: {string.Join(", ", duplicated)} now appears more often than any replica held it."
                : ".");
        }

        if (steps.Any(step => step.Operation.Action == "delete"))
        {
            detail.Append(" An element deleted on more than one replica is deleted once.");
        }

        return new BcrdtMergeDecisionDto
        {
            Target = $"array '{path}'",
            Replicas = editors,
            Outcome = duplicated.Length > 0 ? "a moved element is duplicated" : $"{Both(editors)} survive",
            Detail = detail.ToString()
        };
    }

    private static BcrdtMergeDecisionDto DecideSet(string path, string[] editors, Step[] steps, JsonNode? merged)
    {
        TryGet(merged, path.Split('/'), out var value);
        var members = value as JsonArray;

        var notes = new List<string>();
        var addWins = false;
        var removeStands = false;

        foreach (var element in steps.GroupBy(step => ToNode(step.Operation.Arguments[0])?.ToJsonString() ?? "null"))
        {
            var adders = element.Where(step => step.Operation.Action == "add").ToArray();
            var removers = element.Where(step => step.Operation.Action == "remove").ToArray();
            if (adders.Length == 0 || removers.Length == 0) continue;

            var present = members?.Any(member => (member?.ToJsonString() ?? "null") == element.Key) == true;
            var effectiveAdders = adders.Where(step => step.Update is not null).Select(step => step.Operation.Label).Distinct(StringComparer.Ordinal).ToArray();
            var removing = removers.Select(step => step.Operation.Label).Distinct(StringComparer.Ordinal).ToArray();

            if (present && effectiveAdders.Length > 0)
            {
                addWins = true;
                notes.Add($"{Join(removing)} removed {element.Key} while {Join(effectiveAdders)} added it: add wins - it is in the set on every replica, because a remove deletes the entry its replica saw and the add made a new one.");
            }
            else if (present is false && effectiveAdders.Length == 0)
            {
                removeStands = true;
                notes.Add($"{Join(removing)} removed {element.Key} while {Join(adders.Select(step => step.Operation.Label).Distinct(StringComparer.Ordinal))} added it - but it was already in the set that replica saw, so the add was a no-op and wrote nothing. The remove stands: it is gone on every replica. Add wins only over the removal of an entry the adder had not seen.");
            }
            else
            {
                notes.Add($"{element.Key} was added and removed concurrently, and is {(present ? "in" : "not in")} the set on every replica.");
            }
        }

        if (notes.Count == 0) notes.Add("Their adds and removes touch different elements, or the same one the same way, so all of them apply.");

        var described = editors.Select(label => $"{label} {string.Join(", ", steps.Where(step => step.Operation.Label == label).Select(step => Describe(step, null, null)))}").ToArray();

        return new BcrdtMergeDecisionDto
        {
            Target = $"set '{path}'",
            Replicas = editors,
            Outcome = addWins ? "add wins" : removeStands ? "the remove wins" : $"{Both(editors)} apply",
            Detail = $"{Join(described)} while apart. Every replica now holds {Render(value)}. {string.Join(" ", notes)}"
        };
    }

    /// <summary>
    /// What a step did, in words. Described as a write to a map key - <paramref name="key"/> of the map at
    /// <paramref name="map"/> - a step that created the nested type it went into is that creation, whatever it then
    /// wrote inside it.
    /// </summary>
    private static string Describe(Step step, string? map, string? key)
    {
        var op = step.Operation;
        var a = op.Arguments;

        if (key is not null && step.Created && (op.Kind == "map" && op.Path == map && (string)a[0]! == key) is false)
        {
            return $"created a new {op.Kind} there and wrote into it";
        }

        return (op.Kind, op.Action) switch
        {
            ("map", "set") => key is null ? $"set '{a[0]}' to {Render(ToNode(a[1]))}" : $"set it to {Render(ToNode(a[1]))}",
            ("map", _) => key is null ? $"deleted '{a[0]}'" : "deleted it",
            ("text", "insert") => $"inserted {Quote((string)a[1]!)} at {a[0]}",
            ("text", "delete") => $"deleted {a[1]} at {a[0]}",
            ("text", _) => $"formatted {a[1]} at {a[0]} as {Render(ToNode(a[2]))}",
            ("array", "insert") => $"inserted {string.Join(", ", a.Skip(1).Select(v => Render(ToNode(v))))} at {a[0]}",
            ("array", "push") => $"pushed {string.Join(", ", a.Select(v => Render(ToNode(v))))}",
            ("array", "delete") => $"deleted {(a.Count > 1 ? a[1] : 1)} at {a[0]}",
            ("array", _) => $"moved {a[0]} to {a[1]}",
            ("set", "add") => $"added {Render(ToNode(a[0]))}",
            _ => $"removed {Render(ToNode(a[0]))}"
        };
    }

    private static string[] Notes(List<Operation> operations, Replica[] replicas, int schedules, bool converged, BcrdtMergeDecisionDto[] decisions, bool sample)
    {
        var notes = new List<string>();

        if (sample)
        {
            notes.Add($"No operations were passed, so this ran a sample. {Grammar}");
        }

        notes.Add($"Each replica made its edits with no connection to the others - one transaction, and so one update, per line - and then received every other replica's updates. " +
                  $"{schedules} delivery orders were tried: every order the replicas could be heard in, and every update newest first, which leaves updates waiting in the pending queue for the ones they depend on. " +
                  (converged
                      ? "Every replica ended identical under every one of them: replicas that have seen the same updates hold the same document, whatever the order."
                      : "Not every replica ended identical - see diverged. That is a bug in the engine; please report it with these operations."));

        notes.Add($"The replicas were given client ids {string.Join(", ", replicas.Select(replica => $"{replica.ClientId} ({replica.Label})"))} so the run is repeatable. " +
                  "An application's are random per session, so which concurrent write wins varies between sessions - treat it as arbitrary, never as \"the last one\".");

        var kinds = operations.Where(op => op.Label != BaseLabel).Select(op => op.Kind).ToHashSet(StringComparer.Ordinal);

        if (kinds.Contains("map")) notes.Add("BcrdtMap: a key written concurrently keeps one value and loses the other. Fields a person types prose into belong in a nested BcrdtText - created once, before people edit it - where both edits merge.");
        if (kinds.Contains("text")) notes.Add("BcrdtText: every concurrent insertion survives, each author's run contiguous. Write an input's new value with BcrdtTextSynchronizer.Apply (BcrdtTextArea does), never by replacing the whole text, or every concurrent edit is lost.");
        if (kinds.Contains("array")) notes.Add("BcrdtArray: concurrent insertions all survive. Replace and Move are a delete plus an insert, so a concurrent edit to that element is lost and two concurrent moves of one element leave two copies - mutate a nested type in place when that matters.");
        if (kinds.Contains("set")) notes.Add("BcrdtSet: an add wins over a concurrent remove of an entry the adder had not seen. Adding an element already present is a no-op, so it does not save that element from a concurrent remove.");

        if (decisions.Length == 0)
        {
            notes.Add("No key, text, array or set was changed by more than one replica, so nothing was contested: the merge simply combines the edits.");
        }

        notes.Add("deltaBytes is what each replica sends a peer holding only the starting state - its whole session as one delta, however many updates it made; mergedBytes is the whole document encoded, what a new peer is sent. Decode any update with InspectBcrdtUpdate.");

        return [.. notes];
    }

    // ---- Reading the operations -------------------------------------------------------------------------

    private static List<Operation> Parse(string input)
    {
        var statements = input.TrimStart().StartsWith('{') ? StatementsFromJson(input) : Split(input);

        if (statements.Count == 0) throw new LabException($"The operations hold no statement. {Grammar}");

        if (statements.Count > MaxStatements)
        {
            throw new LabException($"The operations hold {statements.Count} statements; the lab runs at most {MaxStatements}. A merge scenario is a handful of concurrent edits.");
        }

        var operations = new List<Operation>(statements.Count);
        var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < statements.Count; i++)
        {
            var number = i + 1;
            var statement = statements[i];

            var colon = statement.IndexOf(':');
            var label = colon > 0 ? statement[..colon].Trim() : string.Empty;

            if (label.Length == 0 || label.All(c => char.IsLetterOrDigit(c) || c is '_' or '-') is false)
            {
                throw new LabException($"Statement {number} ('{statement}') does not start with a replica label. Start it with the replica that makes the edit - 'A: ', 'B: ' - or 'base: ' for the starting state. {Grammar}");
            }

            // Labels are matched without regard to case, and spelt the way they were first written.
            label = label.Equals(BaseLabel, StringComparison.OrdinalIgnoreCase) ? BaseLabel : labels.TryGetValue(label, out var known) ? known : labels[label] = label;

            operations.Add(ParseOperation(number, label, statement[(colon + 1)..].Trim()));
        }

        return operations;
    }

    /// <summary>The JSON form: an object of replica labels, each holding a list of operations (or a single one).</summary>
    private static List<string> StatementsFromJson(string input)
    {
        try
        {
            using var document = JsonDocument.Parse(input);

            if (document.RootElement.ValueKind != JsonValueKind.Object) throw new LabException($"The JSON form is an object of replica labels. {Grammar}");

            var statements = new List<string>();

            foreach (var replica in document.RootElement.EnumerateObject())
            {
                var values = replica.Value.ValueKind switch
                {
                    JsonValueKind.Array => replica.Value.EnumerateArray().ToArray(),
                    JsonValueKind.String => [replica.Value],
                    _ => throw new LabException($"'{replica.Name}' holds {replica.Value.ValueKind}; each replica holds a list of operation strings.")
                };

                foreach (var value in values)
                {
                    if (value.ValueKind != JsonValueKind.String) throw new LabException($"'{replica.Name}' lists {value.ValueKind}; every operation is a string such as \"map meta set title \\\"Draft\\\"\".");

                    statements.AddRange(Split(value.GetString()!).Select(statement => statement.Contains(':') && LabelOf(statement) is { } own && own.Equals(replica.Name, StringComparison.OrdinalIgnoreCase)
                        ? statement
                        : $"{replica.Name}: {statement}"));
                }
            }

            return statements;
        }
        catch (JsonException ex)
        {
            throw new LabException($"The operations start with '{{' but are not valid JSON: {ex.Message} {Grammar}");
        }
    }

    private static string? LabelOf(string statement)
    {
        var colon = statement.IndexOf(':');
        return colon > 0 ? statement[..colon].Trim() : null;
    }

    /// <summary>
    /// Splits the line form into statements: one per line, or several on one line separated by ';' - outside the
    /// quotes, brackets and braces of a JSON value. Blank lines and lines starting with '#' or '//' are comments.
    /// </summary>
    private static List<string> Split(string input)
    {
        var statements = new List<string>();
        var current = new StringBuilder();
        var depth = 0;
        var quoted = false;

        void End()
        {
            var statement = current.ToString().Trim();
            current.Clear();

            if (statement.Length == 0 || statement.StartsWith('#') || statement.StartsWith("//", StringComparison.Ordinal)) return;

            statements.Add(statement);
        }

        for (int i = 0; i < input.Length; i++)
        {
            var c = input[i];

            if (quoted)
            {
                current.Append(c);
                if (c == '\\' && i + 1 < input.Length) current.Append(input[++i]);
                else if (c == '"') quoted = false;
                continue;
            }

            switch (c)
            {
                case '"':
                    quoted = true;
                    break;
                case '[' or '{':
                    depth++;
                    break;
                case ']' or '}':
                    depth = Math.Max(0, depth - 1);
                    break;
                case '\n' or '\r':
                    End();
                    continue;
                case ';' when depth == 0:
                    End();
                    continue;
            }

            current.Append(c);
        }

        End();

        return statements;
    }

    private static Operation ParseOperation(int number, string label, string text)
    {
        var tokens = Tokenize(number, label, text);

        LabException Error(string problem) => new($"Statement {number} ('{label}: {text}'): {problem} {Grammar}");

        if (tokens.Count < 3) throw Error("an operation is '<type> <name> <action> <arguments>'.");

        var kind = tokens[0].ToLowerInvariant();
        if (Kinds.Contains(kind) is false) throw Error($"'{tokens[0]}' is not a type; use one of {string.Join(", ", Kinds)}.");

        var name = Unquote(tokens[1]);
        var slash = name.IndexOf('/');
        var root = slash < 0 ? name : name[..slash];
        var key = slash < 0 ? null : name[(slash + 1)..];

        if (root.Length == 0 || key is { Length: 0 } || key?.Contains('/') == true)
        {
            throw Error($"'{tokens[1]}' is not a name: use a root's name, or 'root/key' for a type nested in the root map 'root'.");
        }

        var action = tokens[2].ToLowerInvariant();
        var arguments = tokens.Skip(3).ToArray();

        object?[] Args(params Func<string, object?>[] readers)
        {
            if (arguments.Length != readers.Length) throw Error($"{kind} {action} takes {readers.Length} argument{(readers.Length == 1 ? "" : "s")}, not {arguments.Length}.");
            return [.. arguments.Select((argument, i) => readers[i](argument))];
        }

        int Int(string token) => int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw Error($"'{token}' is not a whole number.");

        string Quoted(string token) => Value(token) is string value ? value : throw Error($"{token} is not a JSON string - write text in double quotes, e.g. \"Hello\".");

        IReadOnlyDictionary<string, object?> Attributes(string token) => Value(token) as Dictionary<string, object?>
            ?? throw Error($"{token} is not a JSON object of attributes, e.g. {{\"bold\":true}}.");

        object? Value(string token)
        {
            try
            {
                using var document = JsonDocument.Parse(token);
                return FromJson(document.RootElement);
            }
            catch (JsonException)
            {
                throw Error($"'{token}' is not a JSON value - write strings in double quotes (\"Draft\"), and numbers, true, false, null, [arrays] and {{objects}} as JSON.");
            }
        }

        object?[] parsed = (kind, action) switch
        {
            ("text", "insert") when arguments.Length == 3 => Args(t => Int(t), Quoted, Attributes),
            ("text", "insert") => Args(t => Int(t), Quoted),
            ("text", "delete") => Args(t => Int(t), t => Int(t)),
            ("text", "format") => Args(t => Int(t), t => Int(t), Attributes),
            ("map", "set") => Args(Unquote, Value),
            ("map", "delete") => Args(Unquote),
            ("array", "insert") when arguments.Length >= 2 => [Int(arguments[0]), .. arguments.Skip(1).Select(Value)],
            ("array", "insert") => throw Error("array insert takes an index and at least one value."),
            ("array", "push") when arguments.Length >= 1 => [.. arguments.Select(Value)],
            ("array", "push") => throw Error("array push takes at least one value."),
            ("array", "delete") when arguments.Length == 1 => Args(t => Int(t)),
            ("array", "delete") => Args(t => Int(t), t => Int(t)),
            ("array", "move") => Args(t => Int(t), t => Int(t)),
            ("set", "add") => Args(Value),
            ("set", "remove") => Args(Value),
            _ => throw Error($"'{tokens[2]}' is not something a {kind} does; a {kind} can {Actions(kind)}.")
        };

        return new Operation(number, label, kind, root, key, action, parsed, string.Join(' ', tokens));
    }

    private static string Actions(string kind) => kind switch
    {
        "text" => "insert, delete or format",
        "map" => "set or delete",
        "array" => "insert, push, delete or move",
        _ => "add or remove"
    };

    /// <summary>
    /// Splits an operation into words, keeping a JSON string, array or object whole - "Q3 planning" and
    /// {"bold": true} are one argument each, spaces and all.
    /// </summary>
    private static List<string> Tokenize(int number, string label, string text)
    {
        var tokens = new List<string>();
        var i = 0;

        while (i < text.Length)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                i++;
                continue;
            }

            var start = i;

            if (text[i] is '"' or '[' or '{')
            {
                // A JSON value ends where its outermost string, array or object closes - skipping over escapes,
                // and over brackets that are only characters inside a string.
                var depth = 0;
                var quoted = false;
                var closed = false;

                for (; i < text.Length && closed is false; i++)
                {
                    var c = text[i];

                    if (quoted)
                    {
                        if (c == '\\') i++;
                        else if (c == '"') quoted = false;
                    }
                    else if (c == '"') quoted = true;
                    else if (c is '[' or '{') depth++;
                    else if (c is ']' or '}') depth--;

                    closed = quoted is false && depth == 0;
                }

                if (closed is false) throw new LabException($"Statement {number} ('{label}: {text}'): a quote, bracket or brace is never closed. {Grammar}");
            }
            else
            {
                while (i < text.Length && char.IsWhiteSpace(text[i]) is false) i++;
            }

            tokens.Add(text[start..i]);
        }

        return tokens;
    }

    private static string Unquote(string token)
    {
        if (token.Length < 2 || token[0] != '"') return token;

        try
        {
            using var document = JsonDocument.Parse(token);
            return document.RootElement.GetString() ?? token;
        }
        catch (JsonException)
        {
            return token;
        }
    }

    /// <summary>A JSON value as the collaborative value model holds it: integers as long, objects as string-keyed dictionaries.</summary>
    private static object? FromJson(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        // Boxed separately: as one conditional expression, long and double meet at double, and every integer would
        // be stored - and sized on the wire - as a float.
        JsonValueKind.Number => element.TryGetInt64(out var integer) ? (object)integer : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Array => element.EnumerateArray().Select(FromJson).ToList(),
        JsonValueKind.Object => element.EnumerateObject().ToDictionary(property => property.Name, property => FromJson(property.Value), StringComparer.Ordinal),
        _ => null
    };

    /// <summary>
    /// The kind every root is used as. A root is one kind for good - replicas that disagreed could never converge,
    /// and the engine refuses it - so a name used as two kinds is reported before anything runs. A nested type is
    /// a value under a map key, and may well be contested: two kinds at one key is a write conflict like any other.
    /// </summary>
    private static Dictionary<string, string> RootKinds(List<Operation> operations)
    {
        var roots = new Dictionary<string, (string Kind, Operation First)>(StringComparer.Ordinal);

        foreach (var op in operations)
        {
            var kind = op.Key is null ? op.Kind : "map";

            if (roots.TryGetValue(op.Root, out var known) && known.Kind != kind)
            {
                throw new LabException($"Statement {op.Number} ('{op.Line}') uses '{op.Root}' as a {kind}{(op.Key is null ? "" : $" (it holds the nested '{op.Path}')")}, but statement {known.First.Number} ('{known.First.Line}') uses it as a {known.Kind}. A root is one kind on every replica.");
            }

            roots.TryAdd(op.Root, (kind, op));
        }

        return roots.ToDictionary(root => root.Key, root => root.Value.Kind, StringComparer.Ordinal);
    }

    // ---- Projections ------------------------------------------------------------------------------------

    /// <summary>The document as <see cref="BcrdtDocument.ToJson"/> projects it - what an application reads.</summary>
    private static JsonNode? Project(BcrdtDocument document) => ToNode(document.ToJson());

    /// <summary>
    /// The marks of every root text that carries any, as its <see cref="BcrdtText.ToDelta"/>: <see cref="BcrdtText.ToJson"/>
    /// is the plain text, and a formatting conflict is invisible in it.
    /// </summary>
    private static JsonNode? Formatting(BcrdtDocument document, Dictionary<string, string> roots)
    {
        var result = new JsonObject();

        foreach (var (name, kind) in roots.OrderBy(root => root.Key, StringComparer.Ordinal))
        {
            if (kind != "text") continue;

            var text = document.GetText(name);
            if (text.HasFormatting is false) continue;

            result[name] = new JsonArray([.. text.ToDelta().Select(operation =>
            {
                var node = new JsonObject { ["insert"] = ToNode(operation.Content) };
                if (operation.Attributes is { Count: > 0 } attributes) node["attributes"] = ToNode(attributes);
                return (JsonNode)node;
            })]);
        }

        return result.Count == 0 ? null : result;
    }

    internal static JsonNode? ToNode(object? value) => value switch
    {
        null => null,
        string text => JsonValue.Create(text),
        bool flag => JsonValue.Create(flag),
        long integer => JsonValue.Create(integer),
        int integer => JsonValue.Create(integer),
        double number when double.IsFinite(number) => JsonValue.Create(number),
        float number when float.IsFinite(number) => JsonValue.Create(number),
        double or float => JsonValue.Create(Convert.ToString(value, CultureInfo.InvariantCulture)),
        decimal number => JsonValue.Create(number),
        byte[] bytes => JsonValue.Create(Convert.ToBase64String(bytes)),
        DateTime time => JsonValue.Create(time.ToString("O", CultureInfo.InvariantCulture)),
        DateTimeOffset time => JsonValue.Create(time.ToString("O", CultureInfo.InvariantCulture)),
        Guid id => JsonValue.Create(id.ToString()),
        BcrdtAbstractType type => ToNode(type.ToJson()),
        IEnumerable<KeyValuePair<string, object?>> map => new JsonObject(map.Select(pair => KeyValuePair.Create(pair.Key, ToNode(pair.Value)))),
        IEnumerable<object?> list => new JsonArray([.. list.Select(ToNode)]),
        _ => JsonValue.Create(Convert.ToString(value, CultureInfo.InvariantCulture))
    };

    /// <summary>The value at a path of root, nested keys and key - false when any of it is absent.</summary>
    private static bool TryGet(JsonNode? node, string[] path, out JsonNode? value)
    {
        value = node;

        foreach (var segment in path)
        {
            if (value is not JsonObject map || map.TryGetPropertyValue(segment, out value) is false)
            {
                value = null;
                return false;
            }
        }

        return true;
    }

    private static string Render(JsonNode? node) => node?.ToJsonString() ?? "null";

    /// <summary>"both" for two replicas, "all" for more - the README's phrasing of what survives a merge.</summary>
    private static string Both(string[] editors) => editors.Length == 2 ? "both" : "all";

    private static string Quote(string? text) => text is null ? "nothing" : JsonSerializer.Serialize(text);

    private static string Join(IEnumerable<string> parts)
    {
        var list = parts.ToArray();

        return list.Length switch
        {
            0 => string.Empty,
            1 => list[0],
            2 => $"{list[0]} and {list[1]}",
            _ => $"{string.Join(", ", list[..^1])} and {list[^1]}"
        };
    }

    /// <summary>Every order of 0..count-1 - at most 24, for four replicas.</summary>
    private static IEnumerable<int[]> Permutations(int count)
    {
        if (count == 1)
        {
            yield return [0];
            yield break;
        }

        foreach (var rest in Permutations(count - 1))
        {
            for (int position = 0; position <= rest.Length; position++)
            {
                yield return [.. rest[..position], count - 1, .. rest[position..]];
            }
        }
    }
}
