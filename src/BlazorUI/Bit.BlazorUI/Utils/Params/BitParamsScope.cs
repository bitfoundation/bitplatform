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
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]?> _propertiesCache = new();



    private BitParamsScope(bool isPassThrough,
                           List<KeyValuePair<BitParamsKey, IBitComponentParams>> own,
                           List<BitParamsKey> hidden,
                           Dictionary<BitParamsKey, IBitComponentParams> all,
                           bool? isEnabled,
                           bool? readOnly,
                           BitDir? dir,
                           bool isIsolated)
    {
        IsPassThrough = isPassThrough;
        Own = own;
        Hidden = hidden;
        All = all;
        IsEnabled = isEnabled;
        ReadOnly = readOnly;
        Dir = dir;
        IsIsolated = isIsolated;
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
    /// Whether every component under the <see cref="BitParams"/> is enabled, when it or an ancestor says so.
    /// </summary>
    public bool? IsEnabled { get; }

    /// <summary>
    /// Whether every input under the <see cref="BitParams"/> is read-only, when it or an ancestor says so.
    /// </summary>
    public bool? ReadOnly { get; }

    /// <summary>
    /// The direction the <see cref="BitParams"/> itself sets for every component under it; an inherited one
    /// already reaches them from the ancestor that set it.
    /// </summary>
    public BitDir? Dir { get; }

    /// <summary>
    /// Whether the <see cref="BitParams"/> ignores everything its ancestors provide.
    /// </summary>
    public bool IsIsolated { get; }



    public static BitParamsScope Create(BitParamsScope? parent,
                                        IEnumerable<IBitComponentParams>? parameters,
                                        bool isolated,
                                        bool? isEnabled,
                                        bool? readOnly,
                                        BitDir? dir)
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

        if (isolated is false)
        {
            isEnabled ??= parent?.IsEnabled;
            readOnly ??= parent?.ReadOnly;
        }

        // The ancestors' scope holds everything in effect above, so with nothing to add and nothing to hide the
        // content is left to it.
        var isPassThrough = isolated is false && own.Count == 0 && dir is null
                         && isEnabled == parent?.IsEnabled && readOnly == parent?.ReadOnly;

        var all = inherited is null ? [] : new Dictionary<BitParamsKey, IBitComponentParams>(inherited);

        foreach (var (key, value) in own)
        {
            all[key] = value;
        }

        return new(isPassThrough, own, hidden, all, isEnabled, readOnly, dir, isolated);
    }

    /// <summary>
    /// Whether this scope carries exactly what the other one does, parameter by parameter, so that nothing
    /// would change for the components under it.
    /// </summary>
    public bool IsEquivalentTo(BitParamsScope? other)
    {
        if (other is null) return false;
        if (IsPassThrough && other.IsPassThrough) return true;
        if (IsPassThrough != other.IsPassThrough) return false;
        if (IsEnabled != other.IsEnabled || ReadOnly != other.ReadOnly) return false;
        if (Dir != other.Dir || IsIsolated != other.IsIsolated) return false;

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

        foreach (var (key, value) in All)
        {
            if (other.All.TryGetValue(key, out var otherValue) is false) return false;
            if (AreEquivalent(value, otherValue) is false) return false;
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

    private static bool AreEquivalent(IBitComponentParams first, IBitComponentParams second)
    {
        if (ReferenceEquals(first, second)) return true;

        var type = first.GetType();

        if (type != second.GetType()) return false;

        var properties = GetProperties(type);

        // A type that is not copied is cascaded as is, so it is the same only when it is the same object.
        if (properties is null) return false;

        foreach (var property in properties)
        {
            if (AreEqual(property.GetValue(first), property.GetValue(second)) is false) return false;
        }

        return true;
    }

    private static bool AreEqual(object? first, object? second)
    {
        if (Equals(first, second)) return true;

        // A merged dictionary is a new object on every render, so it is compared entry by entry.
        if (first is IDictionary firstDictionary && second is IDictionary secondDictionary)
        {
            if (firstDictionary.Count != secondDictionary.Count) return false;

            foreach (DictionaryEntry entry in firstDictionary)
            {
                if (secondDictionary.Contains(entry.Key) is false) return false;
                if (Equals(entry.Value, secondDictionary[entry.Key]) is false) return false;
            }

            return true;
        }

        return false;
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
