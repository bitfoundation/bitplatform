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

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]?> _copyablePropertiesCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]?> _snapshotPropertiesCache = new();

    private readonly BitParamsScope? _parent;

    // What each params object the scope was created from held then, in the order they were listed. A merged copy is
    // only shallow, so a nested object - a ClassStyles, an icon, a list - is the very one the markup holds, and a
    // change made to it in place shows in the copy too. What every params object held is therefore taken apart now,
    // while it can still be told apart from what it holds later.
    private readonly List<object?> _inputs;

    // How each params object the scope cascades itself was merged: from what, and the snapshots of the listed ones.
    private readonly Dictionary<BitParamsKey, OwnEntry> _ownEntries;



    private BitParamsScope(BitParamsScope? parent,
                           List<object?> inputs,
                           Dictionary<BitParamsKey, OwnEntry> ownEntries,
                           List<KeyValuePair<BitParamsKey, IBitComponentParams>> own,
                           Dictionary<BitParamsKey, IBitComponentParams> all,
                           bool isIsolated)
    {
        _parent = parent;
        _inputs = inputs;
        _ownEntries = ownEntries;
        Own = own;
        All = all;
        IsIsolated = isIsolated;
    }



    /// <summary>
    /// The params objects to cascade, in the order they were first listed.
    /// </summary>
    public IReadOnlyList<KeyValuePair<BitParamsKey, IBitComponentParams>> Own { get; }

    /// <summary>
    /// Every params object in effect under the <see cref="BitParams"/>.
    /// </summary>
    public IReadOnlyDictionary<BitParamsKey, IBitComponentParams> All { get; }

    /// <summary>
    /// Whether the <see cref="BitParams"/> ignores the params objects its ancestors provide.
    /// </summary>
    public bool IsIsolated { get; }



    /// <summary>
    /// Creates the scope of the given ancestors' scope and params objects.
    /// </summary>
    /// <param name="parent">The scope of the nearest <see cref="BitParams"/> ancestor, if any.</param>
    /// <param name="parameters">The params objects the <see cref="BitParams"/> lists.</param>
    /// <param name="isolated">Whether the <see cref="BitParams"/> ignores what its ancestors carry.</param>
    /// <param name="previous">
    /// The scope the same <see cref="BitParams"/> created before, if any. A params object merged from exactly what
    /// one of it was merged from is taken over from it as the very same object, so that the components it cascades
    /// to can tell it has not changed: changing one params object under a <see cref="BitParams"/> hands a new
    /// object down to the components of that type alone, and not to every one of the others it carries.
    /// </param>
    public static BitParamsScope Create(BitParamsScope? parent,
                                        IEnumerable<IBitComponentParams>? parameters,
                                        bool isolated,
                                        BitParamsScope? previous = null)
    {
        var inherited = isolated ? null : parent?.All;
        var inputs = new List<object?>();
        var keys = new List<BitParamsKey>();
        var ownEntries = new Dictionary<BitParamsKey, OwnEntry>();

        foreach (var item in parameters ?? [])
        {
            if (item is null) continue;

            var snapshot = TakeSnapshot(item, 0);

            inputs.Add(snapshot);

            var key = BitParamsKey.From(item);

            if (ownEntries.TryGetValue(key, out var entry) is false)
            {
                ownEntries.Add(key, entry = new(inherited?.GetValueOrDefault(key)));
                keys.Add(key);
            }

            entry.Items.Add(item);
            entry.Snapshots.Add(snapshot);
        }

        var own = new List<KeyValuePair<BitParamsKey, IBitComponentParams>>(keys.Count);

        foreach (var key in keys)
        {
            var entry = ownEntries[key];

            if (previous is not null
                && previous._ownEntries.TryGetValue(key, out var previousEntry)
                && ReferenceEquals(previousEntry.Basis, entry.Basis)
                && AreAllEqual(previousEntry.Snapshots, entry.Snapshots))
            {
                entry.Value = previousEntry.Value;
            }
            else
            {
                var value = entry.Basis;

                foreach (var item in entry.Items)
                {
                    value = Merge(value, item);
                }

                entry.Value = value!;
            }

            // The listed objects are only needed to merge them, and are not kept beyond it.
            entry.Items.Clear();

            own.Add(new(key, entry.Value));
        }

        var all = inherited is null ? [] : new Dictionary<BitParamsKey, IBitComponentParams>(inherited);

        foreach (var (key, value) in own)
        {
            all[key] = value;
        }

        return new(parent, inputs, ownEntries, own, all, isolated);
    }

    /// <summary>
    /// Whether a scope created from the given ancestors' scope and params objects would carry exactly what this one
    /// does, parameter by parameter and down into every nested object, so that nothing would change for the
    /// components under it. Only the params objects listed are taken apart for it, and nothing is merged: the
    /// ancestors' scope stays the very same object for as long as what it carries stays the same.
    /// </summary>
    public bool IsCreatedFrom(BitParamsScope? parent, IEnumerable<IBitComponentParams>? parameters, bool isolated)
    {
        if (isolated != IsIsolated) return false;

        // An isolated scope takes nothing from its ancestors but the params objects it hides from its content.
        if (ReferenceEquals(parent, _parent) is false && (isolated is false || HaveTheSameKeys(parent, _parent) is false)) return false;

        var index = 0;

        foreach (var item in parameters ?? [])
        {
            if (item is null) continue;

            if (index == _inputs.Count) return false;

            if (AreEqual(TakeSnapshot(item, 0), _inputs[index]) is false) return false;

            index++;
        }

        return index == _inputs.Count;
    }



    private static bool HaveTheSameKeys(BitParamsScope? first, BitParamsScope? second)
    {
        var firstCount = first?.All.Count ?? 0;
        var secondCount = second?.All.Count ?? 0;

        if (firstCount != secondCount) return false;
        if (firstCount == 0) return true;

        foreach (var key in first!.All.Keys)
        {
            if (second!.All.ContainsKey(key) is false) return false;
        }

        return true;
    }

    /// <summary>
    /// Creates a copy of <paramref name="overlay"/> in which every parameter it leaves unset is taken from
    /// <paramref name="basis"/>, a params object of the same type, if there is one. A type that cannot be copied -
    /// one without a public parameterless constructor - is used as is.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Every params type is preserved by the DynamicDependency of the component that reads it.")]
    private static IBitComponentParams Merge(IBitComponentParams? basis, IBitComponentParams overlay)
    {
        var type = overlay.GetType();
        var properties = GetCopyableProperties(type);

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

            // A dictionary is merged entry by entry, so that a nested params object only adds to the attributes of
            // the one around it.
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
    /// changed in place: an object with public properties and a public parameterless constructor - a params object,
    /// a ClassStyles, an icon - property by property, a dictionary entry by entry and a collection that already
    /// holds its items item by item. Everything else - a string, a value type, a delegate, a sequence that is only
    /// produced as it is enumerated, an object of any other kind, or one nested too deep - is a leaf, compared as it
    /// is. A params type that cannot be copied is cascaded as is, so it is a leaf too, and the same only when it is
    /// the same object.
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
            // A LINQ query or an iterator runs again on every enumeration - reading a file or a database again, or
            // using up a sequence that can only be read once - so only a collection that holds its items is read.
            if (IsMaterialized(value) is false) return value;

            var items = new List<object?>();

            foreach (var item in sequence)
            {
                items.Add(TakeSnapshot(item, depth + 1));
            }

            return new SequenceSnapshot(items);
        }

        var type = value.GetType();
        var properties = GetSnapshotProperties(type);

        if (properties is null || properties.Length == 0) return value;

        var values = new object?[properties.Length];

        for (int i = 0; i < properties.Length; i++)
        {
            values[i] = TakeSnapshot(properties[i].GetValue(value), depth + 1);
        }

        return new ObjectSnapshot(type, values);
    }

    private static bool IsMaterialized(object sequence)
    {
        return sequence is Array or ICollection
            || sequence.GetType().Namespace?.StartsWith("System.Collections", StringComparison.Ordinal) is true;
    }

    private static bool AreEqual(object? first, object? second)
    {
        if (BitCascadeExtensions.AreSame(first, second)) return true;

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
    private static PropertyInfo[]? GetCopyableProperties(Type type)
    {
        return _copyablePropertiesCache.GetOrAdd(type, static t =>
        {
            if (t.IsAbstract || t.GetConstructor(Type.EmptyTypes) is null) return null;

            return t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0
                             && p.GetMethod!.IsPublic && p.SetMethod!.IsPublic)
                    .ToArray();
        });
    }

    /// <summary>
    /// The properties a value of a type that can be copied is taken apart by: the read-write ones, and the read-only
    /// ones holding a collection, which is changed through what it holds rather than by being assigned.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Every params type is preserved by the DynamicDependency of the component that reads it.")]
    private static PropertyInfo[]? GetSnapshotProperties(Type type)
    {
        return _snapshotPropertiesCache.GetOrAdd(type, static t =>
        {
            var copyable = GetCopyableProperties(t);

            if (copyable is null) return null;

            var collections = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                               .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && p.GetMethod!.IsPublic
                                        && (p.CanWrite is false || p.SetMethod!.IsPublic is false)
                                        && IsCollectionType(p.PropertyType));

            return [.. copyable, .. collections];
        });

        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Only the interfaces of a property type are read, which the trimmer keeps.")]
        static bool IsCollectionType(Type type)
        {
            if (type.IsArray || typeof(ICollection).IsAssignableFrom(type)) return true;

            return type.GetInterfaces()
                       .Append(type)
                       .Any(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(ICollection<>)
                                                  || i.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>)));
        }
    }



    private sealed class OwnEntry(IBitComponentParams? basis)
    {
        /// <summary>
        /// The params object of the same type the ancestors carry, which the listed ones are merged over.
        /// </summary>
        public IBitComponentParams? Basis { get; } = basis;

        public List<IBitComponentParams> Items { get; } = [];

        public List<object?> Snapshots { get; } = [];

        /// <summary>
        /// The merged params object cascaded for the type.
        /// </summary>
        public IBitComponentParams Value { get; set; } = default!;
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
