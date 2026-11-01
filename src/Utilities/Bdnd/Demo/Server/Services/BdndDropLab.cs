using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Bdnd.Demo.Server.Dtos;
using Target = Bit.Bdnd.Demo.Server.Services.BdndDropHost.Target;

namespace Bit.Bdnd.Demo.Server.Services;

/// <summary>
/// Runs a drag scenario - a draggable, a tree of drop targets and where the pointer is - through the
/// library itself, so "which targets light up, which <c>OnDrop</c> runs, in what order, and what does
/// <c>OnEnd</c> get?" is answered by the code that decides it at runtime rather than by a reading of the
/// docs.
/// <para>
/// The work is done by the real <see cref="BdndProvider"/>, <see cref="BdndDraggable{TItem}"/> and
/// <see cref="BdndDropTarget{TItem}"/> (see <see cref="BdndDropHost"/>): each target's
/// <c>CanDrop</c>, <c>IsOver</c> and <c>IsOverShallow</c> are read off the state it hands its own child
/// content, and the drop order, the results and the end context are what its callbacks received. What
/// this class adds is only the reading of the scenario and the notes that say what each finding means
/// for the page being written - the near-miss type, the result an outer target overwrote, the drop that
/// fell through to an ancestor.
/// </para>
/// </summary>
public static partial class BdndDropLab
{
    /// <summary>
    /// The largest scenario this tool reads. A scenario arrives as a tool argument, and one past this
    /// size is past anything a question about one drag needs.
    /// </summary>
    public const int MaxInputBytes = 256 * 1024;

    /// <summary>The most drop targets one scenario may declare.</summary>
    public const int MaxTargets = 500;

    /// <summary>How deep targets may nest; a page nesting them deeper than this is not the page in question.</summary>
    public const int MaxDepth = 32;

    /// <summary>
    /// The longest target id this tool reads. An id is a name a page gives its target, and the report
    /// repeats it as the parent of every child the target has - so one long id under many children would
    /// make the report hundreds of times the size of the scenario.
    /// </summary>
    public const int MaxIdLength = 256;

    /// <summary>The most items one note lists by id before it says how many more there are.</summary>
    private const int MaxListed = 8;

    /// <summary>
    /// The longest name compared for a typo. A name past this is not one anyone misspells by a letter or
    /// two, and the comparison would cost the product of the two lengths.
    /// </summary>
    private const int MaxNearMissLength = 128;

    /// <summary>
    /// The native drags, by the DataTransfer type the browser gives them - the table the README's
    /// "Native drags" section documents, and the one the components match their accept lists against.
    /// </summary>
    private static readonly (string Constant, string Name, string Token, string Source)[] _nativeTypes =
    [
        (BdndNativeTypes.File, nameof(BdndNativeTypes.File), "Files", "files dragged in from the desktop"),
        (BdndNativeTypes.Url, nameof(BdndNativeTypes.Url), "text/uri-list", "a link dragged in from another page"),
        (BdndNativeTypes.Text, nameof(BdndNativeTypes.Text), "text/plain", "selected text dragged in"),
        (BdndNativeTypes.Html, nameof(BdndNativeTypes.Html), "text/html", "HTML dragged in from another page"),
    ];

    /// <summary>The values <c>dataTransfer.effectAllowed</c> takes; the browser ignores anything else, case included.</summary>
    private static readonly string[] _effects = ["none", "copy", "copyLink", "copyMove", "link", "linkMove", "move", "all"];

    private static readonly string[] _rootProperties = ["draggable", "targets", "hover"];
    private static readonly string[] _draggableProperties = ["type", "canDrag", "dropEffect"];
    private static readonly string[] _targetProperties = ["id", "accept", "acceptTypes", "canDrop", "result", "children"];

    private static readonly JsonDocumentOptions _options = new()
    {
        // Each nested target costs two levels - its object and its children array.
        MaxDepth = MaxDepth * 2 + 8,
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    private static readonly Lazy<string> _sample = new(BuildSample);

    /// <summary>
    /// A kanban scenario that shows the rules worth knowing in one drag: nested targets dropped on
    /// innermost first, shallow hover on the innermost only, an outer result overriding an inner one,
    /// a target whose CanDropPredicate says no, and one that only takes files.
    /// </summary>
    public static string Sample => _sample.Value;

    public static async Task<BdndDropReportDto> InspectAsync(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new BdndDropReportDto
            {
                Valid = false,
                InputBytes = 0,
                Sample = Sample,
                Notes =
                [
                    "No scenario was passed, so nothing was run. Sample is a kanban board: a 'card' hovered over a card that is itself a " +
                    "drop target, inside a column, inside the board - plus a column whose CanDropPredicate says no and an attachments zone " +
                    "that only takes files. Pass it, or your own page's draggable and targets in the same shape, as json to see the report. " +
                    "A target takes 'id', 'accept' (a type or an array; BdndNativeTypes.File and friends for native drags), 'acceptTypes', " +
                    "'canDrop' (false emulates a CanDropPredicate answering false), 'result' (what its OnDrop sets as args.Result) and " +
                    "'children'; 'hover' is the id of the target under the pointer, whose ancestors are hovered with it. A draggable whose " +
                    "type is a native one ('BdndNativeTypes.File') is a drag from outside the app."
                ]
            };
        }

        var inputBytes = Encoding.UTF8.GetByteCount(json);

        if (inputBytes > MaxInputBytes)
        {
            return Invalid(inputBytes, $"The scenario is {inputBytes} bytes; this tool reads scenarios of up to {MaxInputBytes} bytes.");
        }

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(json, _options);
        }
        catch (JsonException exception)
        {
            return Invalid(inputBytes, exception.Message);
        }

        using (document)
        {
            var readingNotes = new ReadingNotes();

            var error = ReadScenario(document.RootElement, readingNotes, out var scenario);

            if (error is not null) return Invalid(inputBytes, error);

            BdndDropHost.Outcome outcome;

            try
            {
                outcome = await BdndDropHost.RunAsync(scenario!.Drag, scenario.Roots, scenario.HoverChain);
            }
            catch (TimeoutException)
            {
                return Invalid(inputBytes, "The drag did not settle in time - the components never finished reacting to it. That is a bug in the library or in this tool, not in the scenario.");
            }

            return Report(inputBytes, scenario, outcome, readingNotes);
        }
    }

    private static BdndDropReportDto Invalid(int inputBytes, string error) => new()
    {
        Valid = false,
        Error = error,
        InputBytes = inputBytes,
        Notes = []
    };

    // ---- Reading the scenario ---------------------------------------------------------

    private sealed record Scenario(BdndDropHost.Drag Drag, string RawType, List<Target> Roots, List<Target> All, List<Target> HoverChain);

    /// <summary>
    /// What reading the scenario noticed - mostly properties it ignored. Past <see cref="MaxListed"/> notes
    /// the rest are only counted: a scenario can hold thousands of stray properties, and a note for each
    /// would make the report many times the size of the scenario.
    /// </summary>
    private sealed class ReadingNotes
    {
        public List<string> Listed { get; } = [];

        public int Unlisted { get; private set; }

        public bool Full => Listed.Count >= MaxListed;

        public void Add(string note)
        {
            if (Full) Unlisted++;
            else Listed.Add(note);
        }

        public void Skip() => Unlisted++;
    }

    /// <summary>Reads the scenario, or says - with the JSON path - what is wrong with it.</summary>
    private static string? ReadScenario(JsonElement root, ReadingNotes readingNotes, out Scenario? scenario)
    {
        scenario = null;

        if (root.ValueKind != JsonValueKind.Object)
        {
            return "A scenario is a JSON object with a \"draggable\", a \"targets\" array and a \"hover\" - call InspectBdndDrop with no json for a sample.";
        }

        NoteUnknownProperties(root, _rootProperties, "the scenario", readingNotes);

        if (TryGet(root, "draggable", out var draggable) is false || draggable.ValueKind != JsonValueKind.Object)
        {
            return TryGet(root, "type", out _)
                ? "The draggable goes in a \"draggable\" object: { \"draggable\": { \"type\": \"card\" }, \"targets\": [...], \"hover\": \"...\" }."
                : "The scenario needs a \"draggable\" object with the item's \"type\" - the BdndDraggable's Type, e.g. { \"type\": \"card\" }.";
        }

        NoteUnknownProperties(draggable, _draggableProperties, "draggable", readingNotes);

        if (TryGet(draggable, "type", out var typeElement) is false || typeElement.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(typeElement.GetString()))
        {
            return "draggable.type is required: it is BdndDraggable.Type ([EditorRequired]), the string every drop target matches against its Accept list.";
        }

        var rawType = typeElement.GetString()!;
        var resolvedType = Resolve(rawType);
        var native = Native(resolvedType);

        var canDrag = true;
        if (TryGet(draggable, "canDrag", out var canDragElement) && canDragElement.ValueKind != JsonValueKind.Null)
        {
            if (canDragElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return "draggable.canDrag must be true or false - it is BdndDraggable.CanDrag.";

            canDrag = canDragElement.GetBoolean();
        }

        var dropEffect = "move";
        if (TryGet(draggable, "dropEffect", out var effectElement) && effectElement.ValueKind != JsonValueKind.Null)
        {
            if (effectElement.ValueKind != JsonValueKind.String) return $"draggable.dropEffect must be a string, one of: {string.Join(", ", _effects.Where(effect => effect != "none"))}.";

            dropEffect = effectElement.GetString()!;
        }

        if (native is not null && (TryGet(draggable, "canDrag", out _) || TryGet(draggable, "dropEffect", out _)))
        {
            readingNotes.Add("canDrag and dropEffect were ignored: they configure a BdndDraggable, and a native drag comes from outside the app, where no BdndDraggable is involved.");
        }

        var all = new List<Target>();
        var roots = new List<Target>();

        if (TryGet(root, "targets", out var targets) && targets.ValueKind != JsonValueKind.Null)
        {
            if (targets.ValueKind != JsonValueKind.Array) return "\"targets\" must be an array of drop targets, each { \"id\": ..., \"accept\": [...], \"children\": [...] }.";

            var error = ReadTargets(targets, "targets", null, 0, all, roots, readingNotes);
            if (error is not null) return error;
        }

        var hoverGiven = TryGet(root, "hover", out var hover) && hover.ValueKind != JsonValueKind.Null;
        var hoverError = ResolveHover(hoverGiven ? hover : default, hoverGiven, all, out var hovered);

        if (hoverError is not null) return hoverError;

        var chain = new List<Target>();
        for (var target = hovered; target is not null; target = target.Parent) chain.Insert(0, target);

        scenario = new Scenario(
            new BdndDropHost.Drag(resolvedType, native?.Token, canDrag, dropEffect),
            rawType, roots, all, chain);

        return null;
    }

    private static string? ReadTargets(JsonElement array, string path, Target? parent, int depth, List<Target> all, List<Target> siblings, ReadingNotes readingNotes)
    {
        var position = 0;

        foreach (var element in array.EnumerateArray())
        {
            var at = $"{path}[{position++}]";

            // Only a target that is there nests: an empty children array at the deepest level is fine.
            if (depth >= MaxDepth) return $"{at} nests targets {depth + 1} deep; this tool runs scenarios nested up to {MaxDepth} deep.";

            if (element.ValueKind != JsonValueKind.Object) return $"{at} is not a drop target: a target is an object, {{ \"id\": \"...\", \"accept\": [...] }}.";

            if (all.Count >= MaxTargets) return $"The scenario declares more than {MaxTargets} targets; this tool runs up to {MaxTargets} of them.";

            NoteUnknownProperties(element, _targetProperties, at, readingNotes);

            if (TryGet(element, "id", out var idElement) is false || idElement.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(idElement.GetString()))
            {
                return $"{at}.id is required: every target needs an id - it is what \"hover\" names and what the report lists.";
            }

            var id = idElement.GetString()!;

            if (id.Length > MaxIdLength) return $"{at}.id is {id.Length} characters; this tool runs targets with ids of up to {MaxIdLength} characters.";

            var accept = new List<string>();

            if (TryGet(element, "accept", out var acceptElement))
            {
                var error = ReadTypes(acceptElement, $"{at}.accept", allowSingle: true, accept);
                if (error is not null) return error;
            }

            if (TryGet(element, "acceptTypes", out var acceptTypesElement))
            {
                var error = ReadTypes(acceptTypesElement, $"{at}.acceptTypes", allowSingle: false, accept);
                if (error is not null) return error;
            }

            var canDrop = true;
            if (TryGet(element, "canDrop", out var canDropElement) && canDropElement.ValueKind != JsonValueKind.Null)
            {
                if (canDropElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return $"{at}.canDrop must be true or false; false renders the target with a CanDropPredicate that answers false.";

                canDrop = canDropElement.GetBoolean();
            }

            string? result = null;
            if (TryGet(element, "result", out var resultElement) && resultElement.ValueKind != JsonValueKind.Null)
            {
                result = resultElement.ValueKind == JsonValueKind.String ? resultElement.GetString() : resultElement.GetRawText();
            }

            var target = new Target
            {
                Index = all.Count,
                Id = id,
                Parent = parent,
                Depth = depth,
                // The component combines Accept and AcceptTypes and drops duplicates; so does this.
                Accept = [.. accept.Distinct(StringComparer.Ordinal)],
                CanDrop = canDrop,
                Result = result
            };

            all.Add(target);
            siblings.Add(target);

            if (TryGet(element, "children", out var children) && children.ValueKind != JsonValueKind.Null)
            {
                if (children.ValueKind != JsonValueKind.Array) return $"{at}.children must be an array of drop targets.";

                var error = ReadTargets(children, $"{at}.children", target, depth + 1, all, target.Children, readingNotes);
                if (error is not null) return error;
            }
        }

        return null;
    }

    private static string? ReadTypes(JsonElement element, string path, bool allowSingle, List<string> types)
    {
        if (element.ValueKind == JsonValueKind.Null) return null;

        if (element.ValueKind == JsonValueKind.String && allowSingle)
        {
            if (string.IsNullOrEmpty(element.GetString()) is false) types.Add(Resolve(element.GetString()!));
            return null;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            return allowSingle
                ? $"{path} must be an item type or an array of them, e.g. \"card\" or [\"card\", \"BdndNativeTypes.File\"]."
                : $"{path} must be an array of item types, e.g. [\"card\", \"column\"].";
        }

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String) return $"{path} holds {item.GetRawText()}, which is not an item type: every entry is a string.";

            // The component skips empty entries, and so does this.
            if (string.IsNullOrEmpty(item.GetString()) is false) types.Add(Resolve(item.GetString()!));
        }

        return null;
    }

    /// <summary>The target under the pointer: one id, or a path of ids from an outer target to it.</summary>
    private static string? ResolveHover(JsonElement hover, bool given, List<Target> all, out Target? hovered)
    {
        hovered = null;

        if (given is false) return null;

        if (hover.ValueKind == JsonValueKind.String)
        {
            var id = hover.GetString()!;
            if (id.Length == 0) return null;

            hovered = all.FirstOrDefault(target => target.Id == id);

            return hovered is null ? UnknownId(id, all) : null;
        }

        if (hover.ValueKind != JsonValueKind.Array) return "\"hover\" is the id of the target under the pointer (or an array of ids, from an outer target to it), or null for a drop outside every target.";

        var path = new List<string>();

        foreach (var item in hover.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String) return $"\"hover\" holds {item.GetRawText()}, which is not a target id.";
            path.Add(item.GetString()!);
        }

        if (path.Count == 0) return null;

        foreach (var id in path.Distinct(StringComparer.Ordinal))
        {
            if (all.Any(target => target.Id == id) is false) return UnknownId(id, all);
        }

        // The path may start anywhere above the hovered target; its ancestors further out are hovered too.
        hovered = all.FirstOrDefault(target => target.Id == path[^1] && Matches(target, path));

        return hovered is null
            ? $"\"hover\" [{string.Join(", ", path.Select(id => $"'{id}'"))}] is not a chain of nested targets: each id has to be the parent of the next. Pass just the id of the target under the pointer - its ancestors are hovered with it."
            : null;

        static bool Matches(Target target, List<string> path)
        {
            var current = target;

            for (var i = path.Count - 1; i >= 0; i--, current = current.Parent)
            {
                if (current is null || current.Id != path[i]) return false;
            }

            return true;
        }
    }

    private static string UnknownId(string id, List<Target> all)
    {
        var close = all.Select(target => target.Id).Distinct(StringComparer.Ordinal).Where(other => IsNearMiss(other, id)).Take(MaxListed).ToArray();

        return close.Length > 0
            ? $"\"hover\" names '{id}', which is not the id of any target. Did you mean: {string.Join(", ", close.Select(other => $"'{other}'"))}?"
            : $"\"hover\" names '{id}', which is not the id of any target. The ids are: {List(all.Select(target => target.Id).Distinct(StringComparer.Ordinal).ToArray())}.";
    }

    private static void NoteUnknownProperties(JsonElement element, string[] known, string owner, ReadingNotes readingNotes)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (known.Contains(property.Name, StringComparer.OrdinalIgnoreCase)) continue;

            if (readingNotes.Full)
            {
                readingNotes.Skip();
                continue;
            }

            var close = known.FirstOrDefault(name => IsNearMiss(name, property.Name));

            readingNotes.Add(close is null
                ? $"'{property.Name}' is not a property of {owner} and was ignored; it takes {string.Join(", ", known)}."
                : $"'{property.Name}' is not a property of {owner} and was ignored - did you mean '{close}'?");
        }
    }

    // ---- Native types ---------------------------------------------------------------

    private static (string Constant, string Name, string Token, string Source)? Native(string type)
    {
        foreach (var native in _nativeTypes)
        {
            if (native.Constant == type) return native;
        }

        return null;
    }

    /// <summary>
    /// A type as the scenario spells it, made into what the component is given: <c>BdndNativeTypes.File</c>
    /// (as C# writes it) and <c>native:file</c> become the constant <c>"__NATIVE_FILE__"</c>; anything else is
    /// an in-app type and stays exactly as written - a plain "file" is a type an app may well use for its own items.
    /// </summary>
    private static string Resolve(string type)
    {
        var match = NativeNameRegex().Match(type.Trim());
        if (match.Success is false) return type;

        var name = match.Groups["name"].Value;

        foreach (var native in _nativeTypes)
        {
            if (string.Equals(native.Name, name, StringComparison.OrdinalIgnoreCase)) return native.Constant;
        }

        return type;
    }

    [GeneratedRegex(@"^(?:(?:Bit\.Bdnd\.)?(?:Bdnd)?NativeTypes\.|native:)(?<name>File|Url|Text|Html)$", RegexOptions.IgnoreCase)]
    private static partial Regex NativeNameRegex();

    // ---- The report -----------------------------------------------------------------

    private static BdndDropReportDto Report(int inputBytes, Scenario scenario, BdndDropHost.Outcome outcome, ReadingNotes readingNotes)
    {
        var hovered = scenario.HoverChain.ToHashSet();
        var drops = outcome.Drops.Select((drop, index) => (Drop: drop, Order: index + 1)).ToDictionary(entry => entry.Drop.Target.Index);

        var targets = scenario.All.Select(target =>
        {
            var state = outcome.Targets.GetValueOrDefault(target.Index) ?? new BdndDropHost.TargetOutcome(false, false, false);
            var drop = drops.TryGetValue(target.Index, out var entry) ? entry : default;

            return new BdndDropTargetReportDto
            {
                Id = target.Id,
                Parent = target.Parent?.Id,
                Depth = target.Depth,
                Accept = target.Accept,
                Hovered = hovered.Contains(target),
                CanDrop = state.CanDrop,
                IsOver = state.IsOver,
                IsOverShallow = state.IsOverShallow,
                DropOrder = drop.Drop is null ? null : drop.Order,
                InnerAlreadyDropped = drop.Drop is null || scenario.Drag.NativeToken is not null ? null : drop.Drop.InnerAlreadyDropped,
                Result = drop.Drop?.Result,
                NativeItemType = drop.Drop?.NativeItemType
            };
        }).ToArray();

        var notes = new List<string>();

        Explain(scenario, outcome, targets, notes);

        notes.AddRange(readingNotes.Listed);

        if (readingNotes.Unlisted > 0)
        {
            notes.Add($"{readingNotes.Unlisted} more {(readingNotes.Unlisted == 1 ? "note" : "notes")} like these were left out - fix the properties above and run the scenario again to see the rest.");
        }

        var native = scenario.Drag.NativeToken is not null;

        return new BdndDropReportDto
        {
            Valid = true,
            InputBytes = inputBytes,
            ItemType = scenario.Drag.ItemType,
            Native = native,
            DataTransferTypes = outcome.DataTransferTypes,
            EffectAllowed = outcome.EffectAllowed,
            DragStarted = native ? null : outcome.DragStarted,
            BeginFired = native ? null : outcome.BeginFired,
            HoverPath = [.. scenario.HoverChain.Select(target => target.Id)],
            DropFired = outcome.DropFired,
            DropOrder = [.. outcome.Drops.Select(drop => drop.Target.Id)],
            End = outcome.End is { } end ? new BdndDragEndReportDto { DidDrop = end.DidDrop, DropResult = end.DropResult?.ToString() } : null,
            Targets = targets,
            Notes = [.. notes]
        };
    }

    /// <summary>What the outcome means for the page being written - most important first.</summary>
    private static void Explain(Scenario scenario, BdndDropHost.Outcome outcome, BdndDropTargetReportDto[] reports, List<string> notes)
    {
        var shown = Quote(scenario.RawType);
        var report = scenario.All.Zip(reports).ToDictionary(pair => pair.First, pair => pair.Second);

        foreach (var exception in outcome.Exceptions.Take(MaxListed))
        {
            notes.Add($"Bit.Bdnd threw while running this drag - {exception.GetType().Name}: {exception.Message} That is a bug in the library, not in the scenario.");
        }

        if (scenario.Drag.NativeToken is { } token)
        {
            ExplainNative(scenario, outcome, report, token, notes);
        }
        else if (outcome.DragStarted is false)
        {
            notes.Add(scenario.Drag.CanDrag
                ? "The draggable did not recognise its own dragstart, so the drag never began. That is a bug in the library or in this tool, not in the scenario."
                : "The drag never began: canDrag is false, so BdndDraggable renders draggable=\"false\" and its script registration tags no drag - " +
                  "the browser starts none, and OnBegin, every hover, OnDrop and OnEnd stay silent. A CanDragPredicate that answers false does the same.");
        }
        else
        {
            ExplainInApp(scenario, outcome, report, shown, notes);
        }

        ExplainTypes(scenario, notes);

        foreach (var target in scenario.All.Where(target => target.Accept.Length == 0).Take(MaxListed))
        {
            notes.Add($"'{target.Id}' accepts nothing (no accept or acceptTypes): it never reports IsOver or CanDrop and never gets OnDrop. Give it Accept or AcceptTypes.");
        }

        foreach (var group in scenario.All.GroupBy(target => target.Id, StringComparer.Ordinal).Where(group => group.Count() > 1).Take(MaxListed))
        {
            notes.Add($"The id '{group.Key}' is used by {group.Count()} targets; \"hover\" resolves it to the first one in document order. Give each target its own id.");
        }

        if (scenario.Drag.NativeToken is null && outcome.EffectAllowed is { } effect)
        {
            if (effect == "none")
            {
                notes.Add("dropEffect is 'none', which turns every drop operation off: the browser shows the no-drop cursor everywhere and fires no drop event, so OnEnd gets DidDrop = false. Use move, copy, link, copyMove, copyLink, linkMove or all.");
            }
            else if (_effects.Contains(effect, StringComparer.Ordinal) is false)
            {
                var close = _effects.FirstOrDefault(value => string.Equals(value, effect, StringComparison.OrdinalIgnoreCase));

                notes.Add($"dropEffect '{effect}' is not a value dataTransfer.effectAllowed takes{(close is null ? null : $" - it is case-sensitive: '{close}'")}, so the browser ignores the " +
                          "assignment and leaves effectAllowed 'uninitialized', which allows every effect. Use move, copy, link, copyMove, copyLink, linkMove or all.");
            }
        }
    }

    private static void ExplainInApp(Scenario scenario, BdndDropHost.Outcome outcome, Dictionary<Target, BdndDropTargetReportDto> report, string shown, List<string> notes)
    {
        var type = scenario.Drag.ItemType;
        var chain = scenario.HoverChain;
        var end = outcome.End;

        if (chain.Count == 0)
        {
            notes.Add($"Nothing was hovered: the item was dropped outside every target. OnEnd ran with DidDrop = false and no DropResult - " +
                      "the place to snap the item back or, for 'drag out to remove', to delete it.");
        }
        else
        {
            var innermost = chain[^1];
            var fallback = chain.Take(chain.Count - 1).Reverse().FirstOrDefault(target => report[target].CanDrop);

            if (innermost.Accept.Contains(type, StringComparer.Ordinal) is false)
            {
                notes.Add($"The hovered target '{innermost.Id}' does not accept {shown} ({Accepts(innermost)}), so it stays IsOver = false and never gets OnDrop. " +
                          (fallback is null
                              ? "No target around it can drop the item either, so the browser shows the no-drop cursor and fires no drop event - OnEnd gets DidDrop = false."
                              : outcome.DropFired
                                  ? $"Over it alone the browser would show the no-drop cursor; '{fallback.Id}' around it can drop the item, so the cursor follows the draggable's DropEffect and the drop falls through to '{fallback.Id}'."
                                  : $"'{fallback.Id}' around it could take the item, but the draggable's dropEffect turns every drop off."));
            }

            foreach (var target in chain.Where(target => target.Accept.Contains(type, StringComparer.Ordinal) && report[target].CanDrop is false))
            {
                var rest = outcome.DropFired is false
                    ? " No target in the hovered chain can drop the item, so the browser shows the no-drop cursor and fires no drop event."
                    : ReferenceEquals(target, innermost) ? $" The drop goes on to the targets around it that can drop it: {List(outcome.Drops.Select(drop => drop.Target.Id).ToArray())}." : null;

                notes.Add($"'{target.Id}' accepts {shown} but its CanDropPredicate answered false: it reports IsOver = {Bool(report[target].IsOver)} - hover is tracked by the type alone - " +
                          $"with CanDrop = false, so render the 'not here' state from IsOver && !CanDrop. Its OnDrop does not run.{rest}");
            }
        }

        if (outcome.Drops.Count > 1)
        {
            var after = outcome.Drops.Where(drop => drop.InnerAlreadyDropped).Select(drop => drop.Target.Id).ToArray();

            notes.Add($"OnDrop ran on every target in the hovered chain that can drop, innermost first: {string.Join(", ", outcome.Drops.Select(drop => drop.Target.Id))}. " +
                      $"Inside {(after.Length == 1 ? $"'{after[0]}'" : List(after))} args.Monitor.DidDrop() was already true - check it there when only the innermost target should act.");
        }

        var published = outcome.Drops.Where(drop => drop.Result is not null).ToArray();

        if (published.Length > 1 && published[^1].Result != published[^2].Result)
        {
            notes.Add($"'{published[^1].Target.Id}' published Result '{published[^1].Result}' after '{published[^2].Target.Id}' had published '{published[^2].Result}': a non-null Result " +
                      $"from an outer target replaces an inner one (React DnD's rule), so OnEnd received DropResult '{end?.DropResult}'. To keep the innermost result, leave " +
                      "args.Result unset in an outer OnDrop when args.Monitor.DidDrop() is already true.");
        }
        else if (end is { DidDrop: true, DropResult: null })
        {
            notes.Add("OnEnd got DidDrop = true and DropResult = null: a target accepted the drop, but no OnDrop set args.Result. Set it where the source needs to know where the item went.");
        }

        var overs = chain.Where(target => report[target].IsOver).ToArray();

        if (overs.Length > 1)
        {
            var shallow = overs.Where(target => report[target].IsOverShallow).Select(target => target.Id).ToArray();
            var skipped = chain.Count > 0 && report[chain[^1]].IsOver is false ? $" (the hovered '{chain[^1].Id}' does not accept the type, so it does not count)" : null;

            notes.Add($"IsOver is true on every hovered target that accepts {shown} ({List(overs.Select(target => target.Id).ToArray())}), IsOverShallow only on " +
                      $"{List(shallow)} - the innermost of them{skipped}. Highlight from IsOverShallow when only the target directly under the pointer should light up.");
        }

        var elsewhere = scenario.All.Where(target => chain.Contains(target) is false && report[target].CanDrop).Select(target => target.Id).ToArray();

        if (elsewhere.Length > 0)
        {
            notes.Add($"{List(elsewhere)} {(elsewhere.Length == 1 ? "is" : "are")} not hovered and still report{(elsewhere.Length == 1 ? "s" : null)} CanDrop = true: CanDrop is about the item, " +
                      "not the pointer - it is how a chessboard highlights every legal square while the piece is in the air.");
        }

        // Under effectAllowed "none" the reason is the effect, which has a note of its own.
        if (chain.Count > 0 && end is { DidDrop: false } && outcome.EffectAllowed != "none")
        {
            notes.Add("OnEnd got DidDrop = false: no target in the hovered chain could drop the item, so to the draggable this drag was a cancel.");
        }
    }

    private static void ExplainNative(Scenario scenario, BdndDropHost.Outcome outcome, Dictionary<Target, BdndDropTargetReportDto> report, string token, List<string> notes)
    {
        var native = Native(scenario.Drag.ItemType)!.Value;
        var chain = scenario.HoverChain;

        notes.Add($"{Quote(scenario.RawType)} is a native type (BdndNativeTypes.{native.Name}, DataTransfer type '{token}'): this is {native.Source}, which no BdndDraggable " +
                  "starts - so there is no OnBegin or OnEnd, and the drop arrives through OnNativeDrop, never OnDrop.");

        if (chain.Count == 0)
        {
            notes.Add("Nothing was hovered, so the drag left without a drop: OnNativeDrop ran nowhere.");
            return;
        }

        var lit = chain.Where(target => report[target].IsOver).Select(target => target.Id).ToArray();

        if (lit.Length > 1)
        {
            var shallow = chain.Where(target => report[target].IsOverShallow).Select(target => target.Id).ToArray();

            notes.Add($"While a native drag hovers, every hovered target that accepts it reports IsOver and CanDrop as true ({List(lit)}), IsOverShallow only on " +
                      $"{List(shallow)} - the innermost of them, as for an in-app drag.");
        }

        var waiting = scenario.All.Where(target => chain.Contains(target) is false && target.Accept.Contains(native.Constant, StringComparer.Ordinal)).Select(target => target.Id).ToArray();

        if (waiting.Length > 0)
        {
            notes.Add($"{List(waiting)} accept{(waiting.Length == 1 ? "s" : null)} it too but {(waiting.Length == 1 ? "is" : "are")} not hovered, and report{(waiting.Length == 1 ? "s" : null)} CanDrop = false: for a native " +
                      "drag the state only lights up under the pointer, because the browser does not say what is being dragged until it is over a target.");
        }

        if (outcome.Drops.Count == 0)
        {
            notes.Add($"No hovered target accepts BdndNativeTypes.{native.Name}, so the browser shows the no-drop cursor and no drop happens - OnNativeDrop ran nowhere.");
        }
        else if (outcome.Drops.Count > 1)
        {
            notes.Add($"OnNativeDrop ran on every hovered target that accepts the drag, innermost first: {string.Join(", ", outcome.Drops.Select(drop => drop.Target.Id))}. " +
                      "Unlike OnDrop, a native drop carries no DidDrop telling an outer target that an inner one took it - accept the native type on one target of a chain.");
        }

        if (scenario.All.Any(target => target.Result is not null))
        {
            notes.Add("'result' was not published: it is what OnDrop sets as args.Result, and OnDrop never runs for a native drag.");
        }
    }

    /// <summary>Accept lists that will never match the dragged type although they nearly do.</summary>
    private static void ExplainTypes(Scenario scenario, List<string> notes)
    {
        var type = scenario.Drag.ItemType;

        if (scenario.All.Count > 0 && scenario.All.Any(target => target.Accept.Contains(type, StringComparer.Ordinal)) is false)
        {
            notes.Add($"No target accepts {Quote(type)}: a target reacts only to item types in its Accept/AcceptTypes, compared as strings, case included.");
        }

        var listed = 0;

        foreach (var target in scenario.All)
        {
            foreach (var accepted in target.Accept.Where(accepted => accepted != type))
            {
                string? why = null;

                if (scenario.Drag.NativeToken is not null && Native(type) is { } native && PlainNativeName(accepted, native.Name))
                {
                    why = $"'{target.Id}' accepts '{accepted}' - an in-app type name. A native drag only matches BdndNativeTypes.{native.Name} (\"{native.Constant}\"): write Accept=\"@BdndNativeTypes.{native.Name}\".";
                }
                else if (IsNearMiss(accepted, type))
                {
                    why = $"'{target.Id}' accepts '{accepted}', which is not {Quote(type)}: item types are compared ordinally, case included - a typo here leaves the target dark for this item.";
                }

                if (why is null) continue;

                notes.Add(why);

                if (++listed >= MaxListed) return;
            }
        }
    }

    private static bool PlainNativeName(string accepted, string nativeName)
        => accepted.TrimEnd('s').Equals(nativeName, StringComparison.OrdinalIgnoreCase) || (nativeName == "Url" && accepted.Equals("link", StringComparison.OrdinalIgnoreCase));

    private static string Accepts(Target target) => target.Accept.Length == 0 ? "it accepts nothing" : $"it accepts {List(target.Accept)}";

    private static string Quote(string value) => $"'{value}'";

    private static string Bool(bool value) => value ? "true" : "false";

    private static string List(IReadOnlyCollection<string> items)
    {
        var shown = string.Join(", ", items.Take(MaxListed).Select(item => $"'{item}'"));

        return items.Count > MaxListed ? $"{shown} and {items.Count - MaxListed} more" : shown;
    }

    /// <summary>
    /// Two names close enough to be a typo of each other: the same but for case, or one or two edits
    /// apart (one for short names) - "Card" for "card", "cards" for "card", "colum" for "column", and
    /// two swapped letters count as one edit, "alpah" for "alpha". Past <see cref="MaxNearMissLength"/>
    /// only the case is compared.
    /// </summary>
    private static bool IsNearMiss(string a, string b)
    {
        if (string.Equals(a, b, StringComparison.Ordinal)) return false;
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;

        var shortest = Math.Min(a.Length, b.Length);
        if (shortest < 3 || Math.Max(a.Length, b.Length) > MaxNearMissLength) return false;

        var edits = shortest < 6 ? 1 : 2;

        return Distance(a.ToLowerInvariant(), b.ToLowerInvariant(), edits) <= edits;
    }

    /// <summary>
    /// The optimal string alignment distance: insertions, deletions, substitutions and swaps of two neighbours.
    /// Only a distance up to <paramref name="max"/> is worked out exactly; anything further is
    /// <paramref name="max"/> + 1.
    /// </summary>
    private static int Distance(string a, string b, int max)
    {
        if (Math.Abs(a.Length - b.Length) > max) return max + 1;

        var twoBack = new int[b.Length + 1];
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++) previous[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            var from = Math.Max(1, i - max);
            var to = Math.Min(b.Length, i + max);

            current[from - 1] = from == 1 ? i : max + 1;
            if (to < b.Length) current[to + 1] = max + 1;

            var best = current[from - 1];

            for (var j = from; j <= to; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;

                current[j] = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1), previous[j - 1] + cost);

                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1]) current[j] = Math.Min(current[j], twoBack[j - 2] + 1);

                best = Math.Min(best, current[j]);
            }

            if (best > max) return max + 1;

            (twoBack, previous, current) = (previous, current, twoBack);
        }

        return previous[b.Length];
    }

    private static bool TryGet(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string BuildSample() => """
        {
          "draggable": { "type": "card" },
          "targets": [
            {
              "id": "board",
              "accept": ["card", "column"],
              "result": "board",
              "children": [
                {
                  "id": "todo",
                  "accept": "card",
                  "result": "todo",
                  "children": [
                    { "id": "card-2", "accept": "card" }
                  ]
                },
                { "id": "done", "accept": "card", "canDrop": false },
                { "id": "attachments", "accept": "BdndNativeTypes.File" }
              ]
            }
          ],
          "hover": "card-2"
        }
        """.ReplaceLineEndings("\n");
}
