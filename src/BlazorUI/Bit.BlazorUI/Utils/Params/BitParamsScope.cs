using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bit.BlazorUI;

/// <summary>
/// What a <see cref="BitParams"/> hands down: the params objects it cascades itself, each already merged with the
/// one of the same type its ancestors carry, and every params object in effect under it, which is what a nested
/// <see cref="BitParams"/> merges its own ones with in turn.
/// </summary>
internal sealed class BitParamsScope
{
    // How deep a value is taken apart for its snapshot; anything deeper is compared as it is.
    private const int MaxSnapshotDepth = 16;

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]?> _propertiesCache = new();

    private readonly Dictionary<BitParamsKey, object?> _snapshots;



    private BitParamsScope(bool isPassThrough,
                           List<KeyValuePair<BitParamsKey, IBitComponentParams>> own,
                           List<BitParamsKey> hidden,
                           Dictionary<BitParamsKey, IBitComponentParams> all,
                           bool isIsolated)
    {
        IsPassThrough = isPassThrough;
        Own = own;
        Hidden = hidden;
        All = all;
        IsIsolated = isIsolated;

        // A merged copy is only shallow, so a nested object - a ClassStyles, an icon, a list - is the very one the
        // markup holds, and a change made to it in place shows in the previous scope too. What every params object
        // held is therefore taken apart now, while it can still be told apart from what it holds later.
        _snapshots = new(all.Count);

        foreach (var (key, value) in all)
        {
            _snapshots[key] = TakeSnapshot(value, 0);
        }
    }



    /// <summary>
    /// Whether the <see cref="BitParams"/> has nothing to add to what its ancestors carry, so it cascades nothing.
    /// </summary>
    public bool IsPassThrough { get; }

    /// <summary>
    /// The params objects to cascade, in the order they were first listed.
    /// </summary>
    public IReadOnlyList<KeyValuePair<BitParamsKey, IBitComponentParams>> Own { get; }

    /// <summary>
    /// The params objects of the ancestors that an isolated <see cref="BitParams"/> hides from its content.
    /// </summary>
    public IReadOnlyList<BitParamsKey> Hidden { get; }

    /// <summary>
    /// Every params object in effect under the <see cref="BitParams"/>.
    /// </summary>
    public IReadOnlyDictionary<BitParamsKey, IBitComponentParams> All { get; }

    /// <summary>
    /// Whether the <see cref="BitParams"/> ignores the params objects its ancestors provide.
    /// </summary>
    public bool IsIsolated { get; }



    public static BitParamsScope Create(BitParamsScope? parent,
                                        IEnumerable<IBitComponentParams>? parameters,
                                        bool isolated)
    {
        var inherited = isolated ? null : parent?.All;
        var own = new List<KeyValuePair<BitParamsKey, IBitComponentParams>>();

        foreach (var item in parameters ?? [])
        {
            if (item is null) continue;

            var key = BitParamsKey.From(item);
            var index = own.FindIndex(o => o.Key == key);

            if (index >= 0)
            {
                own[index] = new(key, Merge(own[index].Value, item));
            }
            else
            {
                own.Add(new(key, Merge(inherited?.GetValueOrDefault(key), item)));
            }
        }

        var hidden = new List<BitParamsKey>();

        if (isolated && parent is not null)
        {
            foreach (var key in parent.All.Keys)
            {
                if (own.Exists(o => o.Key == key)) continue;

                hidden.Add(key);
            }
        }

        // The ancestors' scope holds everything in effect above, so with nothing to add and nothing to hide the
        // content is left to it.
        var isPassThrough = isolated is false && own.Count == 0;

        var all = inherited is null ? [] : new Dictionary<BitParamsKey, IBitComponentParams>(inherited);

        foreach (var (key, value) in own)
        {
            all[key] = value;
        }

        return new(isPassThrough, own, hidden, all, isolated);
    }

    /// <summary>
    /// Whether this scope carries exactly what the other one does, parameter by parameter and down into every
    /// nested object, as each held it when its scope was created, so that nothing would change for the components
    /// under it.
    /// </summary>
    public bool IsEquivalentTo(BitParamsScope? other)
    {
        if (other is null) return false;
        if (IsPassThrough && other.IsPassThrough) return true;
        if (IsPassThrough != other.IsPassThrough) return false;
        if (IsIsolated != other.IsIsolated) return false;

        if (Own.Count != other.Own.Count) return false;
        if (Hidden.Count != other.Hidden.Count) return false;
        if (All.Count != other.All.Count) return false;

        for (int i = 0; i < Own.Count; i++)
        {
            if (Own[i].Key != other.Own[i].Key) return false;
        }

        for (int i = 0; i < Hidden.Count; i++)
        {
            if (Hidden[i] != other.Hidden[i]) return false;
        }

        foreach (var (key, snapshot) in _snapshots)
        {
            if (other._snapshots.TryGetValue(key, out var otherSnapshot) is false) return false;
            if (AreEqual(snapshot, otherSnapshot) is false) return false;
        }

        return true;
    }



    /// <summary>
    /// Creates a copy of <paramref name="overlay"/> in which every parameter it leaves unset is taken from
    /// <paramref name="basis"/>, a params object of the same type, if there is one. The copy is also what makes a
    /// later change to a property of <paramref name="overlay"/> something that can be told apart from its previous
    /// value. A type that cannot be copied - one without a public parameterless constructor - is used as is.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Every params type is preserved by the DynamicDependency of the component that reads it.")]
    private static IBitComponentParams Merge(IBitComponentParams? basis, IBitComponentParams overlay)
    {
        var type = overlay.GetType();
        var properties = GetProperties(type);

        if (properties is null) return overlay;

        if (basis is not null && basis.GetType() != type)
        {
            basis = null;
        }

        var result = (IBitComponentParams)Activator.CreateInstance(type)!;

        foreach (var property in properties)
        {
            var value = property.GetValue(overlay);
            var basisValue = basis is null ? null : property.GetValue(basis);

            // A dictionary is copied even without one to merge into, so that an entry added to it later is
            // still a change to compare against.
            if (value is IDictionary<string, object> dictionary && property.PropertyType.IsAssignableFrom(typeof(Dictionary<string, object>)))
            {
                var merged = basisValue is IDictionary<string, object> basisDictionary
                                ? new Dictionary<string, object>(basisDictionary)
                                : new Dictionary<string, object>(dictionary.Count);

                foreach (var (name, entry) in dictionary)
                {
                    merged[name] = entry;
                }

                value = merged;
            }
            else if (value is null)
            {
                value = basisValue;
            }

            property.SetValue(result, value);
        }

        return result;
    }

    /// <summary>
    /// Takes a value apart down to its leaves, so that it can be compared with what it holds later even when it is
    /// changed in place: an object with public read-write properties and a public parameterless constructor - a
    /// params object, a ClassStyles, an icon - property by property, a dictionary entry by entry and any other
    /// sequence item by item. Everything else - a string, a value type, a delegate, an object of any other kind, or
    /// one nested too deep - is a leaf, compared as it is. A params type that cannot be copied is cascaded as is, so
    /// it is a leaf too, and the same only when it is the same object.
    /// </summary>
    private static object? TakeSnapshot(object? value, int depth)
    {
        if (value is null or string or Delegate or Type || value.GetType().IsValueType) return value;

        if (depth >= MaxSnapshotDepth) return value;

        if (value is IDictionary dictionary)
        {
            var entries = new List<KeyValuePair<object, object?>>(dictionary.Count);

            foreach (DictionaryEntry entry in dictionary)
            {
                entries.Add(new(entry.Key, TakeSnapshot(entry.Value, depth + 1)));
            }

            return new DictionarySnapshot(entries);
        }

        if (value is IEnumerable sequence)
        {
            var items = new List<object?>();

            foreach (var item in sequence)
            {
                items.Add(TakeSnapshot(item, depth + 1));
            }

            return new SequenceSnapshot(items);
        }

        var type = value.GetType();
        var properties = GetProperties(type);

        if (properties is null || properties.Length == 0) return value;

        var values = new object?[properties.Length];

        for (int i = 0; i < properties.Length; i++)
        {
            values[i] = TakeSnapshot(properties[i].GetValue(value), depth + 1);
        }

        return new ObjectSnapshot(type, values);
    }

    private static bool AreEqual(object? first, object? second)
    {
        if (Equals(first, second)) return true;

        switch (first, second)
        {
            case (ObjectSnapshot firstObject, ObjectSnapshot secondObject):
                return firstObject.Type == secondObject.Type && AreAllEqual(firstObject.Values, secondObject.Values);

            case (SequenceSnapshot firstSequence, SequenceSnapshot secondSequence):
                return AreAllEqual(firstSequence.Items, secondSequence.Items);

            case (DictionarySnapshot firstDictionary, DictionarySnapshot secondDictionary):
                if (firstDictionary.Entries.Count != secondDictionary.Entries.Count) return false;

                foreach (var (key, entry) in firstDictionary.Entries)
                {
                    var index = secondDictionary.Entries.FindIndex(e => Equals(e.Key, key));

                    if (index < 0) return false;
                    if (AreEqual(entry, secondDictionary.Entries[index].Value) is false) return false;
                }

                return true;

            default:
                return false;
        }
    }

    private static bool AreAllEqual(IReadOnlyList<object?> first, IReadOnlyList<object?> second)
    {
        if (first.Count != second.Count) return false;

        for (int i = 0; i < first.Count; i++)
        {
            if (AreEqual(first[i], second[i]) is false) return false;
        }

        return true;
    }

    /// <summary>
    /// The public read-write properties of a params type, or null when the type cannot be copied.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Every params type is preserved by the DynamicDependency of the component that reads it.")]
    private static PropertyInfo[]? GetProperties(Type type)
    {
        return _propertiesCache.GetOrAdd(type, static t =>
        {
            if (t.IsAbstract || t.GetConstructor(Type.EmptyTypes) is null) return null;

            return t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0
                             && p.GetMethod!.IsPublic && p.SetMethod!.IsPublic)
                    .ToArray();
        });
    }



    private sealed record ObjectSnapshot(Type Type, object?[] Values);

    private sealed record SequenceSnapshot(List<object?> Items);

    private sealed record DictionarySnapshot(List<KeyValuePair<object, object?>> Entries);
}



/// <summary>
/// Identifies a params object the way its consumers do: by its type and by its name, which the CascadingValue
/// component matches case-insensitively.
/// </summary>
internal readonly record struct BitParamsKey(Type Type, string? Name)
{
    public static BitParamsKey From(IBitComponentParams parameters)
    {
        var name = parameters.Name;

        return new(parameters.GetType(), string.IsNullOrWhiteSpace(name) ? null : name);
    }

    public bool Equals(BitParamsKey other)
        => Type == other.Type && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode()
        => HashCode.Combine(Type, Name is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Name));
}
