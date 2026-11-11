namespace Bit.Bdnd;

/// <summary>
/// The keys of a keyboard drag: the ones that pick a source up, and the ones that move, drop and cancel the drag in
/// progress. Each lists <see href="https://developer.mozilla.org/docs/Web/API/KeyboardEvent/key">KeyboardEvent.key</see>
/// values - <c>" "</c> for Space, <c>"Enter"</c>, <c>"ArrowDown"</c>, <c>"j"</c> - pressed without Ctrl, Alt or Meta
/// (and, to pick up, without Shift). The defaults are the WAI-ARIA conventions; set the ones to change and keep the
/// rest, and hand the instance to <see cref="BdndProvider.KeyBindings"/> (or <see cref="BdndDragDropManager.KeyBindings"/>).
/// </summary>
/// <remarks>
/// A key moves, drops or cancels one way only, so no key may be listed for two of <see cref="Drop"/>,
/// <see cref="Cancel"/>, <see cref="Up"/>, <see cref="Down"/>, <see cref="Left"/>, <see cref="Right"/>,
/// <see cref="First"/> and <see cref="Last"/>; <see cref="PickUp"/> may share its keys with <see cref="Drop"/> (Space
/// and Enter do both by default). A drag must be possible from the keyboard, so <see cref="PickUp"/> and
/// <see cref="Drop"/> need a key each. Tab always ends a drag, by moving the focus. The texts screen readers read
/// (<see cref="BdndAnnouncements.Instructions"/>) name the default keys: change them along with the keys.
/// </remarks>
public sealed class BdndKeyBindings
{
    /// <summary>The WAI-ARIA keys every provider uses unless it is given others.</summary>
    public static BdndKeyBindings Default { get; } = new();

    /// <summary>The keys that pick the focused source up. Space and Enter by default.</summary>
    public IReadOnlyList<string> PickUp { get; init; } = [" ", "Enter"];

    /// <summary>The keys that drop the item where it is. Space and Enter by default.</summary>
    public IReadOnlyList<string> Drop { get; init; } = [" ", "Enter"];

    /// <summary>The keys that cancel the drag. Escape by default.</summary>
    public IReadOnlyList<string> Cancel { get; init; } = ["Escape", "Esc"];

    /// <summary>
    /// The keys that go up: a row up in a layout that wraps, else the previous place. The up arrow by default.
    /// </summary>
    public IReadOnlyList<string> Up { get; init; } = ["ArrowUp"];

    /// <summary>
    /// The keys that go down: a row down in a layout that wraps, else the next place. The down arrow by default.
    /// </summary>
    public IReadOnlyList<string> Down { get; init; } = ["ArrowDown"];

    /// <summary>
    /// The keys that go left: the previous place, or the next one in a right-to-left layout. The left arrow by default.
    /// </summary>
    public IReadOnlyList<string> Left { get; init; } = ["ArrowLeft"];

    /// <summary>
    /// The keys that go right: the next place, or the previous one in a right-to-left layout. The right arrow by default.
    /// </summary>
    public IReadOnlyList<string> Right { get; init; } = ["ArrowRight"];

    /// <summary>The keys that go to the first place. Home by default.</summary>
    public IReadOnlyList<string> First { get; init; } = ["Home"];

    /// <summary>The keys that go to the last place. End by default.</summary>
    public IReadOnlyList<string> Last { get; init; } = ["End"];

    /// <summary>
    /// The keys by command, as the module reads them, or null for the default ones (which the module knows, so a
    /// source sends nothing for them). Validates the bindings first.
    /// </summary>
    internal Dictionary<string, string[]>? ToModuleMap()
    {
        Validate();
        if (ReferenceEquals(this, Default)) return null;
        var map = Map();
        var defaults = Default.Map();
        return map.All(pair => pair.Value.SequenceEqual(defaults[pair.Key], StringComparer.Ordinal)) ? null : map;
    }

    /// <summary>Whether two maps of <see cref="ToModuleMap"/> bind the same keys.</summary>
    internal static bool SameMap(Dictionary<string, string[]>? a, Dictionary<string, string[]>? b) =>
        ReferenceEquals(a, b)
        || (a is not null && b is not null && a.Count == b.Count
            && a.All(pair => b.TryGetValue(pair.Key, out var keys) && pair.Value.SequenceEqual(keys, StringComparer.Ordinal)));

    private Dictionary<string, string[]> Map() => new()
    {
        ["pickUp"] = [.. PickUp],
        ["drop"] = [.. Drop],
        ["cancel"] = [.. Cancel],
        ["up"] = [.. Up],
        ["down"] = [.. Down],
        ["left"] = [.. Left],
        ["right"] = [.. Right],
        ["first"] = [.. First],
        ["last"] = [.. Last],
    };

    private void Validate()
    {
        (string Name, IReadOnlyList<string>? Keys)[] all =
        [
            (nameof(PickUp), PickUp), (nameof(Drop), Drop), (nameof(Cancel), Cancel), (nameof(Up), Up),
            (nameof(Down), Down), (nameof(Left), Left), (nameof(Right), Right), (nameof(First), First), (nameof(Last), Last),
        ];
        foreach (var (name, keys) in all)
        {
            if (keys is null) throw new ArgumentException($"{nameof(BdndKeyBindings)}.{name} is null; list no keys with [].");
            if (keys.Any(string.IsNullOrEmpty)) throw new ArgumentException($"{nameof(BdndKeyBindings)}.{name} lists an empty key.");
        }
        if (PickUp.Count == 0) throw new ArgumentException($"{nameof(BdndKeyBindings)}.{nameof(PickUp)} needs a key: a drag must be possible from the keyboard.");
        if (Drop.Count == 0) throw new ArgumentException($"{nameof(BdndKeyBindings)}.{nameof(Drop)} needs a key: a keyboard drag must be droppable.");

        // the keys of a drag in progress each do one thing
        var owners = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, keys) in all.Skip(1))
        {
            foreach (var key in keys!.Distinct(StringComparer.Ordinal))
            {
                if (owners.TryGetValue(key, out var other))
                    throw new ArgumentException($"The key \"{key}\" is bound to both {nameof(BdndKeyBindings)}.{other} and {name}.");
                owners[key] = name;
            }
        }
    }
}
