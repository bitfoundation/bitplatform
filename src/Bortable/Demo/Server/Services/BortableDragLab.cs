using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bit.Bortable.Demo.Server.Dtos;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bortable.Demo.Server.Services;

/// <summary>
/// Runs one drag - over lists the caller describes - through the library itself, so "will this list
/// take the item, where does it land, and which events fire in which order?" is answered by the code
/// that decides it at runtime rather than by a reading of the README.
/// <para>
/// The work is done by real <see cref="Bortable{TItem}"/> lists (see <see cref="BortableListHost"/>):
/// each list is rendered with the parameters described, the drag is dispatched to the handlers they
/// rendered - a click per item of a multi-drag selection, a dragstart on the item, a dragover on the
/// target list, then a drop and the browser's trailing dragend, or a dragend alone for a release
/// outside any list - and what comes back is what the component did: the lists before and after,
/// ItemsChanged, every lifecycle event with its arguments, and the classes each step left on the
/// markup. What this class adds is only the reading of it: whether each step was accepted, why a list
/// refused, and what a refusal or a surprising outcome means in practice.
/// </para>
/// <para>
/// One thing is stood in for: the browser's hit testing. Where the pointer is over a list is not
/// something a list can be told in C#, so the lab answers <c>dropIndex</c> and <c>itemIndexAt</c> with
/// the position the caller asked for. The thresholds and the direction only shape that answer, which is
/// why they are not among the options - and why the pointer reaches its target in one dragover.
/// </para>
/// </summary>
public static class BortableDragLab
{
    /// <summary>The largest description this tool reads. Lists and one drag are small; past this, it is not a drag.</summary>
    public const int MaxInputBytes = 64 * 1024;

    /// <summary>The most lists one call renders.</summary>
    public const int MaxLists = 8;

    /// <summary>The most items one list holds, and the most a selection clicks.</summary>
    public const int MaxItems = 50;

    /// <summary>The longest item.</summary>
    public const int MaxItemLength = 100;

    /// <summary>How a drag may end: dropped on the target list, released anywhere else, or cancelled with Escape.</summary>
    public static readonly string[] Releases = ["drop", "outside", "cancel"];

    /// <summary>The most items one note lists by name before it says how many more there are.</summary>
    private const int MaxListed = 8;

    /// <summary>
    /// How the description is read: either casing, comments and trailing commas allowed - and a
    /// property no list or drag has refused rather than ignored, because a misspelt "multidrag" that
    /// is silently dropped runs a different drag from the one the caller meant.
    /// </summary>
    private static readonly JsonSerializerOptions _read = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = 16,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private static readonly JsonSerializerOptions _write = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        // The same text on every operating system, rather than one with the server's own line endings.
        NewLine = "\n",
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// The drag-state classes a list renders when they are left at their defaults - read off a list
    /// rather than written down, so a renamed default is followed rather than missed.
    /// </summary>
    private static readonly (string Ghost, string Chosen, string Swap, string Selected) _classes = ReadClasses();

    private static readonly Lazy<string> _sample = new(BuildSample);

    /// <summary>A small board - two lists of a group and a card dragged between them - written as the tool reads it.</summary>
    public static string Sample => _sample.Value;

    public static async Task<BortableDragReportDto> InspectAsync(string? json)
    {
        var sampled = string.IsNullOrWhiteSpace(json);
        var text = sampled ? Sample : json!;

        var inputBytes = Encoding.UTF8.GetByteCount(text);

        if (inputBytes > MaxInputBytes)
        {
            return Invalid(inputBytes, $"The description is {inputBytes} bytes; this tool reads up to {MaxInputBytes} bytes of lists and drag.");
        }

        BortableDragScenarioDto? scenario;

        try
        {
            scenario = JsonSerializer.Deserialize<BortableDragScenarioDto>(text, _read);
        }
        catch (JsonException exception)
        {
            return Invalid(inputBytes, Explain(exception));
        }

        if (scenario is null) return Invalid(inputBytes, "The JSON is null, not a drag. Call InspectBortableDrag with no json for a sample.");

        var error = Read(scenario, out var lists, out var drag);

        if (error is not null) return Invalid(inputBytes, error);

        BortableDragReportDto report;

        try
        {
            report = await RunAsync(lists, drag!, inputBytes);
        }
        catch (Exception exception) when (exception is TimeoutException or InvalidOperationException or ArgumentException)
        {
            // What the component throws while handling an event is collected by the renderer and
            // reported in the notes; this is what is left - a handler that never finished, an event the
            // renderer would not dispatch - and it says nothing about the description, which was valid.
            return Invalid(inputBytes, $"The drag could not be run - {exception.GetType().Name}: {exception.Message} That is a fault in this lab or in the library, not in the description.");
        }

        if (sampled is false) return report;

        return report with
        {
            Sample = Sample,
            Notes =
            [
                "No json was passed, so the sample in Sample was run: a card dragged from one list of a board into the other. Pass your own " +
                "lists and drag in the same shape - a list also takes pull, put, sort, disabled, swap, multiDrag, filter, cloneSuffix and " +
                "readOnly; the drag also takes selected, swapWith and release.",
                .. report.Notes
            ]
        };
    }

    private static BortableDragReportDto Invalid(int inputBytes, string error) => new()
    {
        Valid = false,
        Error = error,
        InputBytes = inputBytes,
        Notes = []
    };

    private static string Explain(JsonException exception)
    {
        if (exception.Message.Contains("could not be mapped", StringComparison.Ordinal))
        {
            return $"Unknown property at {exception.Path}. The top level holds lists and drag; a list takes id, items, group, pull, put, " +
                   "sort, disabled, swap, multiDrag, filter, cloneSuffix and readOnly; the drag takes from, item, selected, to, index, swapWith and release.";
        }

        return $"The description could not be read: {exception.Message}";
    }

    // ---- Reading the description -------------------------------------------------------

    /// <summary>One list, validated: what the host renders it with.</summary>
    private sealed record ListPlan(string Id, string[] Items, string? Group, BortablePull Pull, bool Put, bool Sort, bool Disabled,
        bool Swap, bool MultiDrag, HashSet<string> Locked, string? CloneSuffix, bool ReadOnly)
    {
        /// <summary>The list's own rule: a Move out of a read-only list is no pull at all.</summary>
        public BortablePull EffectivePull => Pull == BortablePull.Move && ReadOnly ? BortablePull.None : Pull;

        public bool AllowSort => Sort && Disabled is false && ReadOnly is false;
    }

    /// <summary>The drag, validated, with the target resolved to an index or a swap partner.</summary>
    private sealed record DragPlan(ListPlan From, string Item, string[] Selected, ListPlan To, int? Index, string? SwapWith, string Release)
    {
        public bool SameList => ReferenceEquals(From, To);

        public bool Outside => Release != "drop";

        /// <summary>Whether the pointer has anywhere to go: another list, another place, or an item to swap with.</summary>
        public bool Hovers => SameList is false || SwapWith is not null || (Index is { } index && index != Array.IndexOf(From.Items, Item));
    }

    /// <summary>Validates the description and resolves its defaults; returns what is wrong with it, or null.</summary>
    private static string? Read(BortableDragScenarioDto scenario, out ListPlan[] lists, out DragPlan? drag)
    {
        lists = [];
        drag = null;

        if (scenario.Lists is not { Length: > 0 })
        {
            return "A drag needs lists: \"lists\" is an array of at least one list, each with an \"id\" and its \"items\". Call InspectBortableDrag with no json for a sample.";
        }

        if (scenario.Lists.Length > MaxLists) return $"{scenario.Lists.Length} lists were passed; this tool renders up to {MaxLists}.";

        var plans = new List<ListPlan>();

        for (var i = 0; i < scenario.Lists.Length; i++)
        {
            var list = scenario.Lists[i];

            if (list is null) return $"lists[{i}] is null; a list is an object with an \"id\" and its \"items\".";
            if (string.IsNullOrWhiteSpace(list.Id)) return $"lists[{i}] has no \"id\" - the list's Id, which the events report as FromId and ToId.";
            if (plans.Any(plan => plan.Id == list.Id)) return $"Two lists are called '{list.Id}'; every list needs an id of its own.";
            if (list.Items is null) return $"List '{list.Id}' has no \"items\" - pass an array, empty if the list starts empty.";
            if (list.Items.Length > MaxItems) return $"List '{list.Id}' holds {list.Items.Length} items; this tool renders lists of up to {MaxItems}.";

            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var item in list.Items)
            {
                if (item is null) return $"List '{list.Id}' holds a null item; items are strings.";
                if (item.Length > MaxItemLength) return $"List '{list.Id}' holds an item of {item.Length} characters; items here are up to {MaxItemLength}.";

                if (seen.Add(item) is false)
                {
                    return $"List '{list.Id}' holds '{item}' twice. Items are rendered with @key, so they must be unique within a list - Blazor " +
                           "refuses to render the same key twice, which is why a Bortable never adds an item equal to one it holds.";
                }
            }

            if (list.Filter?.Any(item => item is null) is true) return $"List '{list.Id}' has a null in \"filter\"; it lists the items the Filter locks.";

            BortablePull pull;

            if (string.IsNullOrWhiteSpace(list.Pull)) pull = BortablePull.Move;
            else if (Enum.TryParse(list.Pull.Trim().Replace("BortablePull.", string.Empty, StringComparison.OrdinalIgnoreCase), ignoreCase: true, out pull) is false || Enum.IsDefined(pull) is false)
            {
                return $"List '{list.Id}' has pull '{list.Pull}'; use \"move\" (the default), \"none\" or \"clone\".";
            }

            plans.Add(new ListPlan(list.Id, list.Items, list.Group, pull, list.Put ?? true, list.Sort ?? true, list.Disabled ?? false,
                list.Swap ?? false, list.MultiDrag ?? false, new HashSet<string>(list.Filter ?? [], StringComparer.Ordinal), list.CloneSuffix,
                list.ReadOnly ?? false));
        }

        lists = [.. plans];

        var gesture = scenario.Drag;

        if (gesture is null) return "A drag needs \"drag\": the item to pick up and where it goes, e.g. { \"item\": \"A\", \"to\": \"done\", \"index\": 0 }.";
        if (string.IsNullOrEmpty(gesture.Item)) return "\"drag\" has no \"item\" - the item to pick up.";

        ListPlan from;

        if (gesture.From is not null)
        {
            var found = Find(plans, gesture.From);
            if (found is null) return $"No list is called '{gesture.From}'. The lists are: {Names(plans.Select(plan => plan.Id))}.";
            if (found.Items.Contains(gesture.Item, StringComparer.Ordinal) is false) return $"'{found.Id}' holds no item '{gesture.Item}'. It holds: {Names(found.Items)}.";

            from = found;
        }
        else
        {
            var holding = plans.Where(plan => plan.Items.Contains(gesture.Item, StringComparer.Ordinal)).ToArray();

            if (holding.Length == 0) return $"No list holds '{gesture.Item}'.";
            if (holding.Length > 1) return $"'{gesture.Item}' is in more than one list ({Names(holding.Select(plan => plan.Id))}); say which with \"from\".";

            from = holding[0];
        }

        var to = from;

        if (gesture.To is not null)
        {
            var found = Find(plans, gesture.To);
            if (found is null) return $"No list is called '{gesture.To}'. The lists are: {Names(plans.Select(plan => plan.Id))}.";

            to = found;
        }

        var release = (gesture.Release ?? "drop").Trim().ToLowerInvariant();
        if (Releases.Contains(release, StringComparer.Ordinal) is false) return $"\"release\" is '{gesture.Release}'; use one of: {string.Join(", ", Releases)}.";

        var selected = gesture.Selected ?? [];

        if (selected.Length > MaxItems) return $"{selected.Length} items are selected; this tool clicks up to {MaxItems}.";

        foreach (var item in selected)
        {
            if (item is null || from.Items.Contains(item, StringComparer.Ordinal) is false)
            {
                return $"\"selected\" names '{item}', which '{from.Id}' does not hold. A multi-drag selection is made of items of the list the drag starts in.";
            }
        }

        if (selected.Distinct(StringComparer.Ordinal).Count() != selected.Length) return "\"selected\" names an item twice; a second click would deselect it again.";

        var index = gesture.Index;
        var swapWith = gesture.SwapWith;

        if (to.Swap)
        {
            if (swapWith is not null && index is not null) return "Give \"swapWith\" or \"index\" for a swap list, not both.";

            if (swapWith is not null && to.Items.Contains(swapWith, StringComparer.Ordinal) is false)
            {
                return $"'{to.Id}' holds no item '{swapWith}' to swap with. It holds: {Names(to.Items)}.";
            }

            if (index is { } position)
            {
                if (position < 0 || position >= to.Items.Length) return $"index {position} is not an item of '{to.Id}', which holds {to.Items.Length}; a swap list takes the index of the item to swap with.";

                swapWith = to.Items[position];
                index = null;
            }

            if (swapWith is null && ReferenceEquals(to, from) is false)
            {
                return $"'{to.Id}' is a swap list (swap: true): an item dropped on it trades places with the item under the pointer. Say which, with \"swapWith\".";
            }
        }
        else
        {
            if (swapWith is not null) return $"\"swapWith\" is for a list with swap: true; '{to.Id}' sorts, so give \"index\" - where the item should land.";

            var last = ReferenceEquals(to, from) ? to.Items.Length - 1 : to.Items.Length;

            if (index is { } position && (position < 0 || position > last))
            {
                return ReferenceEquals(to, from)
                    ? $"index {position} is outside '{to.Id}', whose items are at 0..{last}."
                    : $"index {position} is outside '{to.Id}': an item it receives lands at 0..{last}.";
            }

            // Into another list with no position given: the end, where a pointer below the last item puts it.
            if (index is null && ReferenceEquals(to, from) is false) index = to.Items.Length;
        }

        drag = new DragPlan(from, gesture.Item, selected, to, index, swapWith, release);

        return null;

        static ListPlan? Find(List<ListPlan> plans, string id) => plans.FirstOrDefault(plan => plan.Id == id);
    }

    // ---- Running it -----------------------------------------------------------------------

    private static async Task<BortableDragReportDto> RunAsync(ListPlan[] lists, DragPlan drag, int inputBytes)
    {
        var setups = lists.Select(list => new BortableListHost.ListSetup
        {
            Id = list.Id,
            // A read-only list is an array, as the README says an app's would be.
            Items = list.ReadOnly ? list.Items.ToArray() : new List<string>(list.Items),
            Group = list.Group,
            Pull = list.Pull,
            Put = list.Put,
            Sort = list.Sort,
            Disabled = list.Disabled,
            Swap = list.Swap,
            MultiDrag = list.MultiDrag,
            Filter = list.Locked.Count == 0 ? null : list.Locked.Contains,
            Clone = list.CloneSuffix is { } suffix ? item => item + suffix : null
        }).ToArray();

        var setupOf = setups.ToDictionary(setup => setup.Id, StringComparer.Ordinal);

        await using var host = await BortableListHost.StartAsync(setups);

        var steps = new List<BortableDragStepDto>();
        var notes = new List<string>();
        var view = await host.ReadAsync();

        if (host.ModuleImports < lists.Length)
        {
            notes.Add($"Only {host.ModuleImports} of the {lists.Length} lists imported their browser module, so the others answered every hit test with -1. That is a fault in this lab.");
        }

        // ---- Select (multi-drag): one click per item, as a person would -----------------------

        foreach (var item in drag.Selected)
        {
            var step = ++host.Step;
            var before = Wrapper(view, drag.From.Id, item)!;

            if (before.Click == 0)
            {
                steps.Add(Step(step, "click", $"'{item}' in '{drag.From.Id}'", null, false,
                    $"Nothing to click: '{drag.From.Id}' does not have MultiDrag, so its items render no click handler and a click selects nothing.", null));
                continue;
            }

            await host.DispatchAsync(before.Click, new MouseEventArgs());
            view = await host.ReadAsync();
            host.TakeBrowserCalls();

            // Clicks report no markup: the class that toggled is the whole of what they change, and the
            // dragstart that follows shows the selection it carries.
            var after = Wrapper(view, drag.From.Id, item)!;
            var toggled = HasClass(before.Class, _classes.Selected) != HasClass(after.Class, _classes.Selected);

            steps.Add(Step(step, "click", $"'{item}' in '{drag.From.Id}'", null, toggled,
                toggled
                    ? HasClass(after.Class, _classes.Selected) ? $"'{item}' is selected - it renders with {_classes.Selected}." : $"'{item}' was deselected."
                    : $"The click selected nothing. {Sentences(WhyNotDraggable(drag.From, item))}".Trim(), null));
        }

        var selection = view.Single(list => list.Id == drag.From.Id).Items
                            .Where(item => item.Placeholder is false && HasClass(item.Class, _classes.Selected))
                            .Select(item => item.Item)
                            .ToArray();

        // The block a multi-drag carries: the selection, in list order, when the dragged item is part of it.
        var block = drag.From.MultiDrag && selection.Length > 1 && selection.Contains(drag.Item, StringComparer.Ordinal) ? selection : [drag.Item];

        // ---- Pick the item up ---------------------------------------------------------------------

        var started = false;
        {
            var step = ++host.Step;
            var wrapper = Wrapper(view, drag.From.Id, drag.Item)!;

            await host.DispatchAsync(wrapper.DragStart, new DragEventArgs());
            var previous = view;
            view = await host.ReadAsync();
            host.TakeBrowserCalls();

            var start = host.Callbacks.FirstOrDefault(callback => callback.Step == step && callback.Event == "OnStart");
            started = start is not null;

            string outcome;

            if (started)
            {
                outcome = $"The drag started at index {start!.Args!.OldIndex} of '{drag.From.Id}'.";

                if (block.Length > 1) outcome += $" It carries the selection of {block.Length}: {Names(block)}.";
                else if (selection.Length > 0 && drag.From.MultiDrag && selection.Contains(drag.Item, StringComparer.Ordinal) is false) outcome += " Picking up an unselected item cleared the selection.";
            }
            else
            {
                var reasons = WhyNotDraggable(drag.From, drag.Item);

                outcome = $"No drag started. {Sentences(reasons)}".Trim();

                if (wrapper.Draggable is false) outcome += " In a browser it would not even begin: the item renders draggable=\"false\".";

                notes.AddRange(reasons);
            }

            steps.Add(Step(step, "dragstart", $"'{drag.Item}' in '{drag.From.Id}'", null, started, outcome, Changed(previous, view)));
        }

        var hovered = false;
        var hoverAccepted = false;
        string[] hoverReasons = [];

        if (started)
        {
            // ---- Move the pointer to the target ---------------------------------------------------

            if (drag.Hovers)
            {
                hovered = true;

                var step = ++host.Step;
                var target = view.Single(list => list.Id == drag.To.Id);
                var targetItems = setupOf[drag.To.Id].Items;

                int dropIndex = -1, itemIndex = -1;
                string where;

                if (drag.To.Swap)
                {
                    // The swap partner's index among all of the list's items, hidden ones included -
                    // which is what the real itemIndexAt counts.
                    itemIndex = targetItems.IndexOf(drag.SwapWith!);
                    where = $"over '{drag.SwapWith}' in '{drag.To.Id}'";
                }
                else if (drag.SameList)
                {
                    // dropIndex is a gap counted before the item leaves its place: landing past where
                    // it was means the gap after the item it lands behind.
                    var from = targetItems.IndexOf(drag.Item);
                    dropIndex = drag.Index!.Value > from ? drag.Index.Value + 1 : drag.Index.Value;
                    where = $"to index {drag.Index} of '{drag.To.Id}'";
                }
                else
                {
                    dropIndex = drag.Index!.Value;
                    where = $"to index {drag.Index} of '{drag.To.Id}'";
                }

                host.PointAt(target, dropIndex, itemIndex);

                await host.DispatchAsync(target.DragOver, new DragEventArgs());
                var previous = view;
                view = await host.ReadAsync();

                hoverAccepted = drag.To.Swap
                    ? view.Single(list => list.Id == drag.To.Id).Items.Any(item => HasClass(item.Class, _classes.Swap))
                    : host.Callbacks.Any(callback => callback.Step == step && callback.Event == "OnChange" && callback.List == drag.To.Id);

                string outcome;

                if (hoverAccepted)
                {
                    outcome = drag.To.Swap
                        ? $"'{drag.SwapWith}' is highlighted as the swap target ({_classes.Swap}); nothing moves until the drop."
                        : drag.SameList
                            ? $"'{drag.Item}' moved to index {drag.Index} of '{drag.To.Id}' live, while still dragged."
                            : $"'{drag.To.Id}' shows the drop placeholder at index {drag.Index}; the item itself stays in '{drag.From.Id}' until the drag ends" +
                              (drag.From.EffectivePull == BortablePull.Clone ? "." : ", hidden there.");
                }
                else
                {
                    hoverReasons = [.. HoverRefusal(drag, block)];

                    if (hoverReasons.Length == 0) hoverReasons = [$"'{drag.To.Id}' refused it for a reason this lab does not model."];

                    outcome = $"'{drag.To.Id}' did not take it. {Sentences(hoverReasons)}";
                    notes.AddRange(hoverReasons);
                }

                steps.Add(Step(step, "dragover", $"list '{drag.To.Id}', pointer {where}", host.TakeBrowserCalls(), hoverAccepted, outcome, Changed(previous, view)));
            }

            // ---- Let go ---------------------------------------------------------------------------

            if (drag.Outside is false)
            {
                var step = ++host.Step;
                var target = view.Single(list => list.Id == drag.To.Id);

                await host.DispatchAsync(target.Drop, new DragEventArgs());
                var previous = view;
                view = await host.ReadAsync();
                host.TakeBrowserCalls();

                var ended = host.Callbacks.Any(callback => callback.Step == step && callback.Event == "OnEnd");

                steps.Add(Step(step, "drop", $"list '{drag.To.Id}'", null, ended, Completion(host, step, drag, hovered, hoverAccepted), Changed(previous, view)));

                // A browser follows a drop with a dragend on the element the drag started from - when
                // that element is still on the page. An item that left its list with the drop was
                // removed from it, and a removed element's dragend never reaches the list.
                if (Wrapper(view, drag.From.Id, drag.Item) is { } source)
                {
                    step = ++host.Step;

                    await host.DispatchAsync(source.DragEnd, new DragEventArgs());
                    previous = view;
                    view = await host.ReadAsync();
                    host.TakeBrowserCalls();

                    var completed = host.Callbacks.Any(callback => callback.Step == step && callback.Event == "OnEnd");

                    steps.Add(Step(step, "dragend", $"'{drag.Item}' in '{drag.From.Id}'", null, completed,
                        completed
                            ? Completion(host, step, drag, hovered, hoverAccepted)
                            : "Nothing left to do: the drop already completed the drag. Whichever of drop and dragend comes first completes it.",
                        Changed(previous, view)));
                }
            }
            else
            {
                var step = ++host.Step;
                var source = Wrapper(view, drag.From.Id, drag.Item);

                if (source is null)
                {
                    steps.Add(Step(step, "dragend", $"'{drag.Item}' in '{drag.From.Id}'", null, false, "The dragged item's element is no longer rendered, so its dragend reaches no list.", null));
                }
                else
                {
                    await host.DispatchAsync(source.DragEnd, new DragEventArgs());
                    var previous = view;
                    view = await host.ReadAsync();
                    host.TakeBrowserCalls();

                    var ended = host.Callbacks.Any(callback => callback.Step == step && callback.Event == "OnEnd");

                    steps.Add(Step(step, "dragend", $"'{drag.Item}' in '{drag.From.Id}', released {(drag.Release == "cancel" ? "with Escape" : "outside any list")}",
                        null, ended, Completion(host, step, drag, hovered, hoverAccepted), Changed(previous, view)));
                }
            }
        }
        else
        {
            notes.Add("Without a drag there is nothing to move: in a browser the pointer would carry nothing to the target, so the lab stopped there.");
        }

        // ---- What it came to -----------------------------------------------------------------------

        foreach (var exception in host.Exceptions.Take(MaxListed))
        {
            notes.Insert(0, $"Bortable threw during the drag - {exception.GetType().Name}: {exception.Message} That is a bug in the library, not in the lists.");
        }

        var results = lists.Select(list => new BortableListResultDto
        {
            Id = list.Id,
            Group = list.Group,
            Before = list.Items,
            After = [.. setupOf[list.Id].Items],
            Changed = list.Items.SequenceEqual(setupOf[list.Id].Items, StringComparer.Ordinal) is false,
            ItemsChanged = host.Callbacks.Count(callback => callback.List == list.Id && callback.Event == "ItemsChanged")
        }).ToArray();

        var changed = results.Any(result => result.Changed);

        Explain(lists, drag, block, selection, results, host, hoverAccepted, changed, notes);

        return new BortableDragReportDto
        {
            Valid = true,
            InputBytes = inputBytes,
            Kind = KindOf(drag, block),
            Changed = changed,
            Outcome = Summarize(drag, block, host, started, hovered, hoverAccepted, hoverReasons, changed),
            Lists = results,
            Steps = [.. steps],
            Events = [.. host.Callbacks.Select(callback => new BortableDragEventDto
            {
                Step = callback.Step,
                Event = callback.Event,
                List = callback.List,
                Item = callback.Args?.Item,
                Items = callback.Args is { } args ? [.. args.Items] : callback.Items,
                OldIndex = callback.Args?.OldIndex,
                NewIndex = callback.Args?.NewIndex,
                FromId = callback.Args?.FromId,
                ToId = callback.Args?.ToId,
                FromGroup = callback.Args?.FromGroup,
                ToGroup = callback.Args?.ToGroup,
                CrossedLists = callback.Args?.CrossedLists
            })],
            Markup = Markup(lists),
            Notes = [.. notes.Distinct(StringComparer.Ordinal)]
        };
    }

    private static BortableDragStepDto Step(int step, string name, string target, string[]? browser, bool accepted, string outcome, BortableRenderedListDto[]? rendered) => new()
    {
        Step = step,
        Event = name,
        Target = target,
        Browser = browser is { Length: > 0 } ? browser : null,
        Accepted = accepted,
        Outcome = outcome,
        Rendered = rendered
    };

    private static BortableListHost.RenderedItem? Wrapper(BortableListHost.RenderedList[] view, string list, string item)
        => view.Single(rendered => rendered.Id == list).Items.FirstOrDefault(wrapper => wrapper.Placeholder is false && wrapper.Item == item);

    private static bool HasClass(string classes, string name) => classes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(name, StringComparer.Ordinal);

    /// <summary>The lists whose markup a step changed, as they render after it - the drag states, as the stylesheet sees them.</summary>
    private static BortableRenderedListDto[]? Changed(BortableListHost.RenderedList[] before, BortableListHost.RenderedList[] after)
    {
        var changed = after.Where(list => before.Single(old => old.Id == list.Id).Items.Select(Shape).SequenceEqual(list.Items.Select(Shape)) is false)
                           .Select(list => new BortableRenderedListDto
                           {
                               List = list.Id,
                               Items = [.. list.Items.Select(item => new BortableRenderedItemDto
                               {
                                   Item = item.Item,
                                   Classes = StateClasses(item.Class),
                                   Placeholder = item.Placeholder ? true : null,
                                   // The placeholder is a preview, never a drag source: it has no draggable attribute at all.
                                   Draggable = item.Draggable || item.Placeholder ? null : false
                               })]
                           })
                           .ToArray();

        return changed.Length > 0 ? changed : null;

        static (string, string, bool, bool) Shape(BortableListHost.RenderedItem item) => (item.Item, item.Class, item.Placeholder, item.Draggable);
    }

    /// <summary>The classes on a wrapper beyond the one every wrapper has.</summary>
    private static string? StateClasses(string classes)
    {
        var state = string.Join(' ', classes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(name => name != "bit-bortable-item"));

        return state.Length > 0 ? state : null;
    }

    /// <summary>What the step that ended the drag committed, read off the callbacks it raised.</summary>
    private static string Completion(BortableListHost host, int step, DragPlan drag, bool hovered, bool hoverAccepted)
    {
        var raised = host.Callbacks.Where(callback => callback.Step == step).ToArray();

        if (raised.Any(callback => callback.Event == "OnEnd") is false) return "The drag was not completed by this event.";

        var sequence = string.Join(", ", raised.Select(callback => $"{callback.Event} on '{callback.List}'"));
        var end = raised.Last(callback => callback.Event == "OnEnd").Args!;

        if (drag.To.Swap && raised.Any(callback => callback.Event is "OnUpdate" or "OnAdd"))
        {
            return $"Committed the swap: '{drag.Item}' and '{drag.SwapWith}' traded places. Raised {sequence}.";
        }

        if (raised.FirstOrDefault(callback => callback.Event == "OnAdd") is { } add)
        {
            var moved = raised.Any(callback => callback.Event == "OnRemove");

            return moved
                ? $"Committed the move: '{add.Args!.Item}' is in '{add.List}' at index {add.Args.NewIndex}. Raised {sequence}."
                : $"Committed the clone: '{add.Args!.Item}' is in '{add.List}' at index {add.Args.NewIndex}, and the original stayed. Raised {sequence}.";
        }

        if (raised.Any(callback => callback.Event == "OnUpdate"))
        {
            return $"Committed the new order of '{end.FromId}': '{end.Item}' is at index {end.NewIndex}. Raised {sequence}.";
        }

        var why = !hovered
            ? "the item was released where it started"
            : !hoverAccepted
                ? $"'{drag.To.Id}' had refused it"
                : drag.From.EffectivePull == BortablePull.Clone
                    ? "the copy was refused as a duplicate"
                    : "there was nothing to commit";

        return $"The drag ended with nothing changed - {why}. Raised {sequence}: a drag that leaves the lists as they were raises no completion event but OnEnd.";
    }

    // ---- Why ---------------------------------------------------------------------------------
    // Each of these follows the component's own checks, in the component's own order, and is only
    // asked once the component has refused - it explains a refusal the lab observed, it never decides one.

    private static List<string> WhyNotDraggable(ListPlan list, string item)
    {
        var reasons = new List<string>();

        if (list.Disabled) reasons.Add($"'{list.Id}' is Disabled, which turns every drag behaviour of the list off.");
        if (list.Locked.Contains(item)) reasons.Add($"'{item}' is excluded by the Filter of '{list.Id}', so it cannot be dragged or selected - it renders with bit-bortable-filtered.");

        return reasons;
    }

    private static IEnumerable<string> HoverRefusal(DragPlan drag, string[] block)
    {
        if (drag.SameList)
        {
            foreach (var reason in WhyNotSorted(drag.From)) yield return reason;

            if (drag.To.Swap && drag.SwapWith == drag.Item) yield return "An item cannot be swapped with itself.";

            yield break;
        }

        foreach (var reason in WhyNotAccepted(drag.To, drag.From, drag.Item, block)) yield return reason;

        if (drag.To.Swap is false) yield break;

        // A swap moves an item each way, so the source must be able to take the partner in exchange.
        if (drag.To.EffectivePull == BortablePull.None)
        {
            yield return drag.To.Pull == BortablePull.None
                ? $"A swap sends '{drag.SwapWith}' the other way, into '{drag.From.Id}', and '{drag.To.Id}' has Pull=\"BortablePull.None\": it lets nothing out."
                : $"A swap sends '{drag.SwapWith}' the other way, into '{drag.From.Id}', and '{drag.To.Id}' is a read-only list, which lets nothing out but clones.";
        }

        foreach (var reason in WhyNotReceived(drag.From, drag.To)) yield return $"A swap sends '{drag.SwapWith}' into '{drag.From.Id}' in exchange: {reason}";

        if (drag.From.Items.Contains(drag.SwapWith!, StringComparer.Ordinal))
        {
            yield return $"'{drag.From.Id}' already holds '{drag.SwapWith}', so it could not take it in exchange.";
        }
    }

    private static List<string> WhyNotSorted(ListPlan list)
    {
        var reasons = new List<string>();

        if (list.Sort is false) reasons.Add($"'{list.Id}' has Sort=\"false\": its own order never changes - its items can still be dragged out to other lists of its group.");
        if (list.Disabled) reasons.Add($"'{list.Id}' is Disabled.");
        if (list.ReadOnly) reasons.Add($"'{list.Id}' is a read-only list (an array): it is never sorted, pulled from or dropped into - only cloned from.");

        return reasons;
    }

    /// <summary>Why <paramref name="target"/> may not take an item from <paramref name="source"/> - the component's CanReceiveFrom.</summary>
    private static List<string> WhyNotReceived(ListPlan target, ListPlan source)
    {
        var reasons = new List<string>();

        if (target.Disabled) reasons.Add($"'{target.Id}' is Disabled: nothing can be dropped into it.");
        if (target.Put is false) reasons.Add($"'{target.Id}' has Put=\"false\": it never takes items from other lists.");
        if (target.ReadOnly) reasons.Add($"'{target.Id}' is a read-only list (an array): nothing is ever dropped into it.");

        if (ReferenceEquals(target, source)) return reasons;

        var ungrouped = new[] { source, target }.Where(list => list.Group is null).Select(list => $"'{list.Id}'").ToArray();

        if (ungrouped.Length > 0)
        {
            reasons.Add($"{string.Join(" and ", ungrouped)} {(ungrouped.Length > 1 ? "have" : "has")} no Group: lists exchange items only when both name the same group.");
        }
        else if (string.Equals(source.Group, target.Group, StringComparison.Ordinal) is false)
        {
            reasons.Add($"'{source.Id}' is in group '{source.Group}' and '{target.Id}' in '{target.Group}': only lists with the same Group exchange items" +
                        (string.Equals(source.Group, target.Group, StringComparison.OrdinalIgnoreCase) ? " - and the comparison is case-sensitive." : "."));
        }

        return reasons;
    }

    /// <summary>Why <paramref name="target"/> may not take what is dragged from <paramref name="source"/> - the component's CanAccept.</summary>
    private static List<string> WhyNotAccepted(ListPlan target, ListPlan source, string item, string[] block)
    {
        var reasons = new List<string>();

        if (source.EffectivePull == BortablePull.None)
        {
            reasons.Add(source.Pull == BortablePull.None
                ? $"'{source.Id}' has Pull=\"BortablePull.None\": its items cannot be dragged out."
                : $"'{source.Id}' is a read-only list (an array), on which a Pull of Move acts as None - only Pull=\"BortablePull.Clone\" lets items out of it.");
        }

        reasons.AddRange(WhyNotReceived(target, source));

        // A multi-drag that moves brings its whole block, and every item of it must be new to the target.
        var incoming = block.Length > 1 && source.EffectivePull == BortablePull.Move ? block : [item];

        foreach (var duplicate in incoming.Where(candidate => target.Items.Contains(candidate, StringComparer.Ordinal)))
        {
            reasons.Add($"'{target.Id}' already holds '{duplicate}': items are rendered with @key, so a list never takes an item equal to one it holds.");
        }

        return reasons;
    }

    /// <summary>The notes that say what the outcome means - after the refusals, which are already in.</summary>
    private static void Explain(ListPlan[] lists, DragPlan drag, string[] block, string[] selection, BortableListResultDto[] results, BortableListHost host, bool hoverAccepted, bool changed, List<string> notes)
    {
        var target = results.Single(result => result.Id == drag.To.Id);
        var clone = drag.From.EffectivePull == BortablePull.Clone && drag.SameList is false && drag.To.Swap is false;

        if (drag.Selected.Length > 0 && drag.From.MultiDrag is false)
        {
            notes.Add($"'{drag.From.Id}' does not have MultiDrag, so the clicks selected nothing and '{drag.Item}' was dragged alone. Set MultiDrag=\"true\" to drag a selection.");
        }
        else if (drag.From.MultiDrag && selection.Length > 0 && selection.Contains(drag.Item, StringComparer.Ordinal) is false)
        {
            notes.Add($"'{drag.Item}' was not selected, so picking it up cleared the selection and it moved alone - dragging a selected item carries the whole selection.");
        }

        if (block.Length > 1 && changed && drag.SameList is false && hoverAccepted is false && results.Single(result => result.Id == drag.From.Id).Changed)
        {
            notes.Add($"The drag ended in '{drag.From.Id}', and a multi-drag gathers its selection wherever it ends: the selected items now stand together, in their original order, around '{drag.Item}'. Clear the selection first to leave '{drag.From.Id}' as it was.");
        }
        else if (block.Length > 1 && changed)
        {
            notes.Add(clone
                ? "Multi-drag moves; with a Clone pull only the dragged item is cloned into another list - the rest of the selection stays where it is."
                : $"The selection moved as one block, in its original order ({Names(block)}), gathered around '{drag.Item}' where it was dropped. BortableEventArgs.Items lists all of it.");
        }

        if (clone && hoverAccepted)
        {
            var copy = drag.From.CloneSuffix is { } suffix ? drag.Item + suffix : drag.Item;

            if (target.Changed is false)
            {
                notes.Add($"The drop was cancelled at the last moment: the copy of '{drag.Item}' is '{copy}', which '{drag.To.Id}' already holds - Blazor needs a unique @key per item, so a clone equal to an item of the target is refused. Return a distinct item from the Clone factory.");
            }
            else if (drag.From.CloneSuffix is null)
            {
                notes.Add($"'{drag.To.Id}' received '{drag.Item}' itself: without a Clone factory a string is copied with ICloneable.Clone(), which returns the same string. That works once - a second clone of it into '{drag.To.Id}' is refused as a duplicate. Give the list a cloneSuffix here; in an app, a Clone factory that returns a distinct item.");
            }
            else
            {
                notes.Add($"'{drag.To.Id}' received '{copy}', made by the Clone factory (item + \"{drag.From.CloneSuffix}\"), and '{drag.Item}' stayed in '{drag.From.Id}'. The events report the copy as Item.");
            }
        }

        if (drag.To.Swap && drag.SameList is false && changed && drag.From.EffectivePull == BortablePull.Clone)
        {
            notes.Add($"'{drag.From.Id}' pulls by Clone, but a swap trades the two items outright: nothing was copied, and '{drag.Item}' left '{drag.From.Id}'.");
        }

        foreach (var list in results.Where(result => result.Id != drag.From.Id && result.Id != drag.To.Id && result.Changed))
        {
            notes.Add($"'{list.Id}' changed although the drag neither started nor ended there - that is unexpected.");
        }

        foreach (var list in lists.Where(list => list.CloneSuffix is not null && list.Pull != BortablePull.Clone))
        {
            notes.Add($"'{list.Id}' has a cloneSuffix but its pull is not clone, so its Clone factory is never called.");
        }

        if (drag.Outside && changed)
        {
            notes.Add(drag.To.Swap
                ? "Released outside any list, the highlighted swap still happened: a drag ends with the swap target it last highlighted."
                : drag.SameList
                    ? "Released outside any list, the new order stays: within its own list the item had already moved while it was dragged."
                    : $"Released outside any list, the item still landed in '{drag.To.Id}': as in SortableJS, once an item is previewed in a list, releasing it anywhere drops it there.");
        }

        if (drag.Release == "cancel")
        {
            notes.Add("Escape ends a native drag the way a release outside any list does: the browser fires dragend and no drop.");
        }

        if (host.Callbacks.Any(callback => callback.Event == "ItemsChanged"))
        {
            notes.Add("Each list whose contents changed raised ItemsChanged - the @bind-Items update - once, before OnUpdate, OnRemove, OnAdd, OnSort and OnEnd, so those handlers already see the final lists.");
        }

        if (host.Callbacks.Any(callback => callback.Event == "OnChange"))
        {
            notes.Add("The lab answers the browser layer's hit tests (dropIndex, itemIndexAt) with the position asked for, so SwapThreshold, InvertSwap, InvertedSwapThreshold and Direction - which only shape that hit test - play no part, and the pointer reached its target in one dragover. A real pointer crossing several items raises OnChange once per item it passes.");
        }
    }

    private static string KindOf(DragPlan drag, string[] block)
    {
        var kind = drag.To.Swap ? "swap"
            : drag.SameList ? "reorder"
            : drag.From.EffectivePull == BortablePull.Clone ? "clone"
            : "move";

        return block.Length > 1 ? $"multi-drag {kind}" : kind;
    }

    /// <summary>The drag's result in one sentence, from the event that ended it.</summary>
    private static string Summarize(DragPlan drag, string[] block, BortableListHost host, bool started, bool hovered, bool hoverAccepted, string[] hoverReasons, bool changed)
    {
        if (started is false)
        {
            var reasons = WhyNotDraggable(drag.From, drag.Item);

            return $"No drag started: {(reasons.Count > 0 ? Lower(reasons[0]) : "the list refused it")}";
        }

        var end = host.Callbacks.LastOrDefault(callback => callback.Event == "OnEnd")?.Args;

        if (end is null) return "The drag never ended.";

        if (changed is false)
        {
            return hovered && hoverAccepted is false
                ? $"Nothing changed: {Lower(hoverReasons.FirstOrDefault() ?? $"'{drag.To.Id}' refused the item.")}"
                : hovered && drag.From.EffectivePull == BortablePull.Clone && drag.SameList is false
                    ? $"Nothing changed: the copy of '{drag.Item}' was refused by '{drag.To.Id}' as a duplicate."
                    : $"Nothing changed: '{drag.Item}' was released where it started.";
        }

        var carried = block.Length > 1 ? $", with the rest of its selection ({block.Length} items in all)," : string.Empty;

        if (drag.To.Swap && hoverAccepted)
        {
            return $"'{drag.Item}' and '{drag.SwapWith}' traded places: '{drag.Item}' is now at index {end.NewIndex} of '{end.ToId}'.";
        }

        // Where the item really ended, rather than where it was meant to: a multi-drag another list
        // refused still ends in its own list - and gathers its selection there.
        if (end.CrossedLists is false)
        {
            if (drag.SameList is false && block.Length > 1)
            {
                return $"'{drag.To.Id}' refused the selection, so the drag ended in '{drag.From.Id}' - where the multi-drag still gathered it into one block around '{drag.Item}'.";
            }

            return end.OldIndex == end.NewIndex && block.Length > 1
                ? $"'{drag.Item}' stayed at index {end.NewIndex} of '{drag.From.Id}', and the rest of its selection was gathered next to it."
                : $"'{drag.Item}'{carried} moved within '{drag.From.Id}' from index {end.OldIndex} to {end.NewIndex}.";
        }

        return drag.From.EffectivePull == BortablePull.Clone
            ? $"'{drag.Item}' was cloned from '{drag.From.Id}' into '{end.ToId}' as '{end.Item}', at index {end.NewIndex}; the original stayed at index {end.OldIndex}."
            : $"'{drag.Item}'{carried} moved from '{drag.From.Id}' (index {end.OldIndex}) into '{end.ToId}' at index {end.NewIndex}.";
    }

    private static string Lower(string sentence) => sentence.Length > 0 && sentence[0] != '\'' ? char.ToLowerInvariant(sentence[0]) + sentence[1..] : sentence;

    private static string Sentences(IEnumerable<string> reasons) => string.Join(' ', reasons);

    private static string Names(IEnumerable<string> items)
    {
        var all = items.ToArray();
        var shown = string.Join(", ", all.Take(MaxListed).Select(item => $"'{item}'"));

        return all.Length > MaxListed ? $"{shown} and {all.Length - MaxListed} more" : shown;
    }

    private static (string, string, string, string) ReadClasses()
    {
        var list = new Bortable<string>();

        return (list.GhostClass, list.ChosenClass, list.SwapClass, list.SelectedClass);
    }

    // ---- The markup ----------------------------------------------------------------------------

    /// <summary>
    /// The Razor that declares these lists in an app - the parameters spelled the way the component
    /// takes them, so the lab's JSON is never mistaken for the API: Filter is a predicate there, not a
    /// list of items, and Clone a factory, not a suffix.
    /// </summary>
    private static string Markup(ListPlan[] lists)
    {
        var builder = new StringBuilder();
        var code = new StringBuilder();
        var fields = new HashSet<string>(StringComparer.Ordinal);

        foreach (var list in lists)
        {
            var field = FieldName(list.Id, fields);
            var name = char.ToUpperInvariant(field[1]) + field[2..];

            builder.Append($"<Bortable {(list.ReadOnly ? "Items" : "@bind-Items")}=\"{field}\" TItem=\"string\" Id={Attribute(list.Id)}");

            if (list.Group is not null) builder.Append($" Group={Attribute(list.Group)}");
            if (list.Pull != BortablePull.Move) builder.Append($" Pull=\"BortablePull.{list.Pull}\"");
            if (list.CloneSuffix is not null) builder.Append($" Clone=\"Clone{name}\"");
            if (list.Put is false) builder.Append(" Put=\"false\"");
            if (list.Sort is false) builder.Append(" Sort=\"false\"");
            if (list.Disabled) builder.Append(" Disabled=\"true\"");
            if (list.Swap) builder.Append(" Swap=\"true\"");
            if (list.MultiDrag) builder.Append(" MultiDrag=\"true\"");
            if (list.Locked.Count > 0) builder.Append($" Filter=\"{field}Locked.Contains\"");

            builder.Append('>').Append('\n')
                   .Append("    <ItemTemplate Context=\"item\">@item</ItemTemplate>\n")
                   .Append("</Bortable>\n\n");

            var items = string.Join(", ", list.Items.Select(Literal));

            code.Append(list.ReadOnly
                ? $"    private IList<string> {field} = new[] {{ {items} }};\n"
                : $"    private IList<string> {field} = new List<string> {{ {items} }};\n");

            if (list.Locked.Count > 0) code.Append($"    private static readonly HashSet<string> {field}Locked = [{string.Join(", ", list.Locked.Select(Literal))}];\n");
            if (list.CloneSuffix is not null) code.Append($"    private static string Clone{name}(string item) => item + {Literal(list.CloneSuffix)};\n");
        }

        builder.Append("@code {\n").Append(code).Append("}\n");

        return builder.ToString();
    }

    /// <summary>A field name for a list: "_todo" for "todo", "_doneList" for "done-list", "_list3" when the id has no letters.</summary>
    private static string FieldName(string id, HashSet<string> taken)
    {
        var words = id.Split([.. id.Where(c => char.IsLetterOrDigit(c) is false).Distinct()], StringSplitOptions.RemoveEmptyEntries);
        var name = string.Concat(words.Select((word, index) => index == 0 ? char.ToLowerInvariant(word[0]) + word[1..] : char.ToUpperInvariant(word[0]) + word[1..]));

        if (name.Length == 0 || char.IsLetter(name[0]) is false || name.Any(c => c > 127)) name = $"list{taken.Count + 1}";

        var field = $"_{name}";
        for (var suffix = 2; taken.Add(field) is false; suffix++) field = $"_{name}{suffix}";

        return field;
    }

    /// <summary>A string attribute value: as written when it is plain text, as a C# expression when Razor would read it as more.</summary>
    private static string Attribute(string value)
        => value.IndexOfAny(['"', '@', '<', '>', '\\']) < 0 ? $"\"{value}\"" : $"\"@({Literal(value)})\"";

    private static string Literal(string value)
    {
        var builder = new StringBuilder("\"");

        foreach (var c in value)
        {
            builder.Append(c switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ when char.IsControl(c) => $"\\u{(int)c:x4}",
                _ => c.ToString()
            });
        }

        return builder.Append('"').ToString();
    }

    // ---- The sample ------------------------------------------------------------------------------

    /// <summary>
    /// The sample: a board of two lists sharing a group, and a card dragged from the first into the
    /// second - the drag the library exists for, with every event a move raises. Built from the
    /// description's own types and written without its unset options, so the format reads at a glance.
    /// </summary>
    private static string BuildSample()
    {
        var scenario = new BortableDragScenarioDto
        {
            Lists =
            [
                new BortableListSetupDto { Id = "todo", Group = "board", Items = ["Write the spec", "Review the PR", "Ship it"] },
                new BortableListSetupDto { Id = "done", Group = "board", Items = ["Set up CI"] },
            ],
            Drag = new BortableDragGestureDto { From = "todo", Item = "Review the PR", To = "done", Index = 0 }
        };

        return JsonSerializer.Serialize(scenario, _write);
    }
}
