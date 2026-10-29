using System.Collections;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Bit.Borm;

/// <summary>
/// Stores form data in a nested dictionary/list structure. This is the backing store used
/// when a form is driven purely by a JSON schema and there is no compiled C# model.
/// Nested objects are <see cref="Dictionary{TKey,TValue}"/> and arrays are <see cref="List{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// The dictionary passed in <em>is</em> the store: every write lands in it, at the top level and below,
/// so the caller reads the answers back out of the very object it handed over. Whatever it already
/// holds is worked with rather than around — a nested dictionary of any value type, an array, a typed
/// list, and the <see cref="JsonElement"/> values <c>JsonSerializer.Deserialize&lt;Dictionary&lt;string, object?&gt;&gt;</c>
/// produces, which are turned into plain dictionaries, lists and primitives the first time the path
/// through them is read or written, so rules and conditions only ever see CLR values.
/// </para>
/// <para>
/// Writes never throw. A value the existing container cannot hold — text into a
/// <c>Dictionary&lt;string, int&gt;</c>, an object into a <c>List&lt;string&gt;</c> — is refused and
/// reported through <see cref="LastWriteFailed"/>, exactly as the model accessor reports a value its
/// property cannot take.
/// </para>
/// </remarks>
public sealed class BormDictionaryDataAccessor : IBormDataAccessor
{
    private const int MaxPadding = 1_000;

    private readonly IDictionary<string, object?> _root;

    /// <summary>
    /// Keys are matched without regard to case, because every other way a path is resolved already is:
    /// a schema lookup (<see cref="BormDefinition.FindByPath"/>), a property on a typed model,
    /// and the state's own touched/dirty/message tracking. A dictionary-backed form was the one place
    /// where <c>Email</c> and <c>email</c> were different fields — which is not a distinction the
    /// library can honour anywhere else, and <see cref="BormDefinition.Validate"/> already
    /// reports two siblings whose names differ only in case as binding to the same path.
    /// </summary>
    /// <remarks>
    /// Containers the store creates use this comparer. A dictionary the caller supplied keeps its own,
    /// so a key is looked up exactly first and then without regard to case — and a write goes to the
    /// key that is already there, in the spelling it already has, rather than adding a second one.
    /// </remarks>
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    // The value type of a keyed container other than IDictionary<string, object?>, or null when its
    // keys are not text. Looked up once per type: it sits on the path of every read through one.
    private static readonly ConcurrentDictionary<Type, Type?> DictionaryValueTypes = new();

    /// <param name="initial">
    /// The store itself when it is writable — edited in place, so do not hand the same dictionary to two
    /// forms (a shared static "defaults" one included) unless they are meant to edit one set of answers.
    /// A read-only one (<c>ReadOnlyDictionary</c>, <c>ImmutableDictionary</c>, <c>FrozenDictionary</c>)
    /// cannot be the store, or the whole form would be uneditable, so it seeds a copy instead.
    /// </param>
    public BormDictionaryDataAccessor(IDictionary<string, object?>? initial = null)
        => _root = initial is { IsReadOnly: false } ? initial : Copy(initial);

    /// <summary>The dictionary the form reads and writes — the one passed to the constructor, when there was one.</summary>
    public object? Root => _root;

    /// <summary>
    /// True when the most recent <see cref="SetValue"/> could not be applied: the value does not convert
    /// to what an existing typed container holds, the path indexes into the root, or the path itself is
    /// malformed. (A read-only container on the way is replaced by a writable copy rather than refused.)
    /// </summary>
    /// <remarks>
    /// A scalar standing where the path needs a container is not a failure: it is replaced by the
    /// container, since the path is the more recent statement of what the data looks like.
    /// </remarks>
    public bool LastWriteFailed { get; private set; }

    public object? GetValue(string path) => TryGetValue(path, out var value) ? value : null;

    /// <summary>
    /// Reads a path, reporting whether the key is present at all — as distinct from present and null,
    /// which is a value the store can legitimately hold.
    /// </summary>
    public bool TryGetValue(string path, out object? value)
    {
        value = null;
        object? current = _root;

        // Indexed, not foreach: through the interface, every read would box the list's enumerator.
        var segments = BormPath.Parse(path);
        for (var i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg.IsIndex)
            {
                if (current is not IList list || seg.Index < 0 || seg.Index >= list.Count) return false;
                current = Materialise(list, seg.Index, list[seg.Index]);
            }
            else
            {
                if (current is null || !TryReadMember(current, seg.Name!, out var key, out var member)) return false;
                current = Materialise(current, key!, member);
            }
        }

        value = current;
        return true;
    }

    public void SetValue(string path, object? value)
    {
        LastWriteFailed = false;

        IReadOnlyList<BormPathSegment> segments;
        try
        {
            segments = BormPath.Parse(path);
        }
        catch (FormatException)
        {
            LastWriteFailed = true;
            return;
        }

        if (segments.Count == 0) return;

        // The root is the caller's own dictionary and is never swapped out, so a replacement (an array
        // grown into a longer one) can only be asked for further down, where the parent stores it.
        LastWriteFailed = !TryWrite(_root, segments, 0, value, out _);
    }

    /// <summary>
    /// Element types are unknown in a schema-only form, so items are created as loosely-typed
    /// dictionaries and values are stored as they arrive.
    /// </summary>
    public Type? GetElementType(string arrayPath) => typeof(object);

    /// <summary>
    /// A nested object in the store. Public so a caller creating an array item by hand — or the form
    /// state creating one for a repeater — gets a container that resolves keys the same way the root
    /// does, rather than a case-sensitive one nested inside a case-insensitive store.
    /// </summary>
    public static Dictionary<string, object?> NewObject() => new(KeyComparer);

    // ---------------------------------------------------------------- writing

    /// <summary>
    /// Writes <paramref name="value"/> at <c>segments[position..]</c> inside <paramref name="container"/>.
    /// <paramref name="replacement"/> is set when the container itself had to be swapped for a new one
    /// — only ever an array that had to grow — and the caller must store it where the old one was.
    /// </summary>
    private static bool TryWrite(object container, IReadOnlyList<BormPathSegment> segments, int position,
        object? value, out object? replacement)
    {
        replacement = null;
        var seg = segments[position];
        var isLeaf = position == segments.Count - 1;

        if (seg.IsIndex)
        {
            if (container is not IList list || seg.Index < 0) return false;

            var item = value;
            if (!isLeaf)
            {
                var existing = seg.Index < list.Count ? list[seg.Index] : null;
                if (!TryWriteBelow(existing, segments, position, value, out item, out var changed)) return false;
                if (!changed) return true;
            }
            return TryStore(list, seg.Index, item, out replacement);
        }

        if (!IsKeyed(container)) return false;

        var member = value;
        if (!isLeaf)
        {
            TryReadMember(container, seg.Name!, out _, out var existing);
            if (!TryWriteBelow(existing, segments, position, value, out member, out var changed)) return false;
            if (!changed) return true;
        }
        return TryWriteMember(container, seg.Name!, member);
    }

    /// <summary>
    /// Continues a write into the child at <c>segments[position]</c>, which currently holds
    /// <paramref name="existing"/>. <paramref name="child"/> is what the parent should hold afterwards and
    /// <paramref name="changed"/> whether that differs from what it holds now.
    /// </summary>
    /// <remarks>
    /// A container already there is written into in place — so a typed one, a
    /// <c>Dictionary&lt;string, int&gt;</c> of scores, keeps every entry the write did not name, where
    /// it used to be thrown away and replaced by a fresh dictionary holding only the new one. Anything
    /// that is not the container the next segment needs is replaced by one.
    /// </remarks>
    private static bool TryWriteBelow(object? existing, IReadOnlyList<BormPathSegment> segments, int position,
        object? value, out object? child, out bool changed)
    {
        var next = segments[position + 1];
        var materialised = WritableCopyIfReadOnly(FromJson(existing));
        var target = materialised is not null && IsContainerFor(materialised, next) ? materialised : CreateContainer(next);

        if (!TryWrite(target, segments, position + 1, value, out var replaced))
        {
            child = existing;
            changed = false;
            return false;
        }

        child = replaced ?? target;
        changed = !ReferenceEquals(child, existing);
        return true;
    }

    /// <summary>Stores <paramref name="value"/> at <paramref name="index"/>, growing the list as needed.</summary>
    private static bool TryStore(IList list, int index, object? value, out object? replacement)
    {
        replacement = null;

        var elementType = BormFieldTypeResolver.GetEnumerableElementType(list.GetType()) ?? typeof(object);
        if (!BormValueConverter.TryCoerce(value, elementType, out var converted)) return false;

        try
        {
            if (index < list.Count)
            {
                list[index] = converted;
                return true;
            }

            if (index - list.Count > MaxPadding) return false;

            if (list is Array array)
            {
                // An array cannot grow, so a longer copy of it takes its place in whatever holds it.
                var grown = Array.CreateInstance(elementType, index + 1);
                array.CopyTo(grown, 0);
                grown.SetValue(converted, index);
                replacement = grown;
                return true;
            }

            // Padding is the element type's own empty value: a List<int> cannot hold the null a
            // List<object?> is padded with.
            while (list.Count < index) list.Add(DefaultFor(elementType));
            list.Add(converted);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or InvalidCastException)
        {
            return false;
        }
    }

    /// <summary>Writes a key of a keyed container, converting to its value type and keeping the key's existing spelling.</summary>
    private static bool TryWriteMember(object container, string name, object? value)
    {
        try
        {
            switch (container)
            {
                case IDictionary<string, object?> dict:
                    dict[ExistingKey(dict, name) ?? name] = value;
                    return true;

                case IDictionary untyped when ValueTypeOf(untyped) is { } valueType:
                    if (!BormValueConverter.TryCoerce(value, valueType, out var converted)) return false;
                    untyped[ExistingKey(untyped, name) ?? name] = converted;
                    return true;

                default:
                    return false;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or InvalidCastException)
        {
            // A read-only dictionary, or one whose comparer will not take the key.
            return false;
        }
    }

    // ---------------------------------------------------------------- reading

    /// <summary>Reads a key of a keyed container — exactly first, then without regard to case.</summary>
    private static bool TryReadMember(object container, string name, out string? key, out object? value)
    {
        switch (container)
        {
            case IDictionary<string, object?> dict:
                if (ExistingKey(dict, name) is { } found)
                {
                    key = found;
                    value = dict[found];
                    return true;
                }
                break;

            case IDictionary untyped when ValueTypeOf(untyped) is not null:
                if (ExistingKey(untyped, name) is { } match)
                {
                    key = match;
                    value = untyped[match];
                    return true;
                }
                break;
        }

        key = null;
        value = null;
        return false;
    }

    private static string? ExistingKey(IDictionary<string, object?> dict, string name)
    {
        if (dict.ContainsKey(name)) return name;
        // A dictionary that already ignores case has answered the question: a miss is a miss. Scanning
        // its keys for another spelling made every new key cost a walk of all the others — quadratic
        // over a form filled in from nothing, which is every dictionary-backed form when it opens.
        if (dict is Dictionary<string, object?> { Comparer: var comparer } && IgnoresCase(comparer)) return null;
        foreach (var key in dict.Keys)
            if (KeyComparer.Equals(key, name)) return key;
        return null;
    }

    private static bool IgnoresCase(IEqualityComparer<string> comparer)
        => ReferenceEquals(comparer, StringComparer.OrdinalIgnoreCase)
           || ReferenceEquals(comparer, StringComparer.InvariantCultureIgnoreCase)
           || ReferenceEquals(comparer, StringComparer.CurrentCultureIgnoreCase);

    private static string? ExistingKey(IDictionary dict, string name)
    {
        try
        {
            if (dict.Contains(name)) return name;
        }
        catch (ArgumentException)
        {
            // A comparer that refuses the key outright; the scan below still gets a say.
        }

        foreach (var key in dict.Keys)
            if (key is string text && KeyComparer.Equals(text, name)) return text;
        return null;
    }

    /// <summary>
    /// Replaces a <see cref="JsonElement"/> read out of <paramref name="container"/> with the CLR value it
    /// stands for, in place, so the next read (and every rule) sees a dictionary, list or primitive.
    /// </summary>
    private static object? Materialise(object container, string key, object? value)
    {
        if (value is not JsonElement) return value;
        var converted = FromJson(value);
        TryWriteMember(container, key, converted);
        return converted;
    }

    private static object? Materialise(IList list, int index, object? value)
    {
        if (value is not JsonElement) return value;
        var converted = FromJson(value);
        try
        {
            list[index] = converted;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or InvalidCastException)
        {
            // A list typed to hold JsonElement keeps it; this read still answers with the CLR value.
        }
        return converted;
    }

    /// <summary>
    /// The CLR shape of a JSON value: objects become store dictionaries, arrays become lists, numbers
    /// become <see cref="long"/> when whole and <see cref="double"/> otherwise — the same types the
    /// form itself writes for an integer and a number field.
    /// </summary>
    internal static object? FromJson(object? value) => value is JsonElement element ? FromJsonElement(element) : value;

    private static object? FromJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = NewObject();
                foreach (var property in element.EnumerateObject()) obj[property.Name] = FromJsonElement(property.Value);
                return obj;
            case JsonValueKind.Array:
                var list = new List<object?>(element.GetArrayLength());
                foreach (var item in element.EnumerateArray()) list.Add(FromJsonElement(item));
                return list;
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var whole)) return whole;
                return element.TryGetDouble(out var real) ? real : element.GetRawText();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            default:
                return null;
        }
    }

    // ---------------------------------------------------------------- shapes

    /// <summary>
    /// A store dictionary holding <paramref name="source"/>'s entries. When two keys differ only in case
    /// the last one wins, which is what the store would end up with anyway.
    /// </summary>
    private static Dictionary<string, object?> Copy(IEnumerable<KeyValuePair<string, object?>>? source)
    {
        var copy = NewObject();
        if (source is null) return copy;
        foreach (var (key, value) in source) copy[key] = value;
        return copy;
    }

    /// <summary>
    /// A read-only container met on the way to a write is replaced, where it lies, by a writable copy —
    /// the same thing the constructor does for a read-only root. Left in place, it refused every write
    /// beneath it and made that whole part of the form uneditable.
    /// </summary>
    private static object? WritableCopyIfReadOnly(object? value) => value switch
    {
        IDictionary<string, object?> { IsReadOnly: true } dictionary => Copy(dictionary),
        IDictionary { IsReadOnly: true } untyped when ValueTypeOf(untyped) is not null
            => Copy(untyped.Cast<DictionaryEntry>().Select(e =>
                new KeyValuePair<string, object?>(Convert.ToString(e.Key, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty, e.Value))),
        IList { IsReadOnly: true } list => list.Cast<object?>().ToList(),
        _ => value
    };

    private static object CreateContainer(BormPathSegment next)
        => next.IsIndex ? new List<object?>() : NewObject();

    /// <summary>Whether an existing value is already the right kind of container for the next segment.</summary>
    private static bool IsContainerFor(object existing, BormPathSegment next)
        => next.IsIndex ? existing is IList : IsKeyed(existing);

    /// <summary>Whether a value is a dictionary keyed by text, of any value type.</summary>
    private static bool IsKeyed(object value)
        => value is IDictionary<string, object?> || (value is IDictionary untyped && ValueTypeOf(untyped) is not null);

    /// <summary>
    /// The value type of a non-generic view of a dictionary: <c>int</c> for a
    /// <c>Dictionary&lt;string, int&gt;</c>, <see cref="object"/> for a <see cref="Hashtable"/>, and
    /// null when its keys are not text, which makes it no container a path can walk into.
    /// </summary>
    private static Type? ValueTypeOf(IDictionary dictionary)
        => DictionaryValueTypes.GetOrAdd(dictionary.GetType(), static type =>
        {
            var generic = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            if (generic is null) return typeof(object);

            var arguments = generic.GetGenericArguments();
            return arguments[0] == typeof(string) || arguments[0] == typeof(object) ? arguments[1] : null;
        });

    private static object? DefaultFor(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;
}
