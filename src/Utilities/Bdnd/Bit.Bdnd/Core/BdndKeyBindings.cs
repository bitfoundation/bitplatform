namespace Bit.Bdnd;

/// <summary>
/// The keys of a keyboard drag: the ones that pick a source up, and the ones that move, drop and cancel the drag in
/// progress. Each lists <see href="https://developer.mozilla.org/docs/Web/API/KeyboardEvent/key">KeyboardEvent.key</see>
/// values - <c>" "</c> for Space, <c>"Enter"</c>, <c>"ArrowDown"</c>, <c>"j"</c> - pressed without Ctrl, Alt or Meta
/// (and, to pick up, without Shift). A key of one character matches whatever its case (Caps Lock, or Shift during a
/// drag), and a Latin letter also matches the key in its place on a keyboard whose layout writes other letters
/// (Cyrillic, Greek): <c>"l"</c> is the L key on a Russian layout too. The defaults are the WAI-ARIA conventions; set the ones to change and keep the
/// rest, and hand the instance to <see cref="BdndProvider.KeyBindings"/> (or <see cref="BdndDragDropManager.KeyBindings"/>).
/// </summary>
/// <remarks>
/// A key moves, drops or cancels one way only, so no key may be listed for two of <see cref="Drop"/>,
/// <see cref="Cancel"/>, <see cref="Up"/>, <see cref="Down"/>, <see cref="Left"/>, <see cref="Right"/>,
/// <see cref="First"/> and <see cref="Last"/>; <see cref="PickUp"/> may share its keys with <see cref="Drop"/> (Space
/// and Enter do both by default). A drag must be possible from the keyboard, so <see cref="PickUp"/> and
/// <see cref="Drop"/> need a key each. Tab cannot be bound: it always moves the focus, which ends a drag, so the
/// keyboard never traps the focus (WCAG 2.1.2). The instructions screen readers read
/// (<see cref="BdndAnnouncements.Instructions"/>) name the keys bound, through their placeholders.
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
        if (ReferenceEquals(this, Default)) return null;
        Validate();
        var map = Map();
        return SameMap(map, DefaultMap) ? null : map;
    }

    /// <summary>
    /// The default keys by command - which the module has a copy of (<c>defaultKeys</c> in <c>bit-bdnd.ts</c>, checked
    /// against this by a test), so that a source sends nothing for them. Worked out once.
    /// </summary>
    internal static Dictionary<string, string[]> DefaultMap { get; } = new BdndKeyBindings().Map();

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
            if (keys.Any(key => string.Equals(key, "Tab", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"{nameof(BdndKeyBindings)}.{name} lists Tab, which cannot be bound: it moves the focus on, which ends a drag, so the keyboard never traps the focus.");
        }
        if (PickUp.Count == 0) throw new ArgumentException($"{nameof(BdndKeyBindings)}.{nameof(PickUp)} needs a key: a drag must be possible from the keyboard.");
        if (Drop.Count == 0) throw new ArgumentException($"{nameof(BdndKeyBindings)}.{nameof(Drop)} needs a key: a keyboard drag must be droppable.");

        // the keys of a drag in progress each do one thing
        var owners = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, keys) in all.Skip(1))
        {
            // "k" and "K" are one key: the module matches a character whatever its case
            foreach (var key in keys!.Select(Normalized).Distinct(StringComparer.Ordinal))
            {
                if (owners.TryGetValue(key, out var other))
                    throw new ArgumentException($"The key \"{key}\" is bound to both {nameof(BdndKeyBindings)}.{other} and {name}.");
                owners[key] = name;
            }
        }
    }

    // as the module compares a key: one character whatever its case (isKey in bit-bdnd.ts)
    private static string Normalized(string key) => key.Length == 1 ? key.ToLowerInvariant() : key;
}
