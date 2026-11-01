using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace Bit.Borm;

/// <summary>
/// Read/write access to a strongly-typed POCO via reflection, supporting nested objects and
/// <see cref="IList"/> collections. Used when a form is bound to a compiled C# model.
/// </summary>
/// <remarks>
/// Writes never throw: a value that cannot be converted to the target property type is discarded and
/// reported through <see cref="LastWriteFailed"/> instead. The UI keeps the raw text the user typed
/// (so it can be corrected) while the model keeps its last valid value.
/// </remarks>
public sealed class BormModelDataAccessor : IBormDataAccessor
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance;

    // Property lookup sits on the hot path of every keystroke; reflection results are cached per type.
    private static readonly ConcurrentDictionary<(Type Type, string Name), PropertyInfo?> PropertyCache = new();

    public BormModelDataAccessor(object model) => Root = model ?? throw new ArgumentNullException(nameof(model));

    public object? Root { get; }

    /// <summary>
    /// True when the most recent <see cref="SetValue"/> could not be applied because the value did not
    /// convert to the property's type (e.g. "abc" into an <c>int</c>), or the property has no public
    /// setter.
    /// </summary>
    public bool LastWriteFailed { get; private set; }

    public object? GetValue(string path)
    {
        var segments = BormPath.Parse(path);
        object? current = Root;
        // Indexed, not foreach: through the interface, every read would box the list's enumerator. This
        // is the read beneath every condition, computed value and render.
        for (var i = 0; i < segments.Count; i++)
        {
            if (current is null) return null;
            current = ReadSegment(current, segments[i]);
        }
        return current;
    }

    /// <summary>
    /// Reads a path, reporting whether it exists on the model at all. A property that is null and a
    /// property that does not exist both read as null through <see cref="GetValue"/>; only this can
    /// tell them apart, which is what lets a scoped read know whether a row really owns the field.
    /// </summary>
    public bool TryGetValue(string path, out object? value)
    {
        value = null;
        var segments = BormPath.Parse(path);
        if (segments.Count == 0)
        {
            value = Root;
            return true;
        }

        object? current = Root;
        for (var i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            // The path continues past something that is not there, so it cannot be resolved.
            if (current is null) return false;

            if (seg.IsIndex)
            {
                if (current is not IList list || seg.Index < 0 || seg.Index >= list.Count) return false;
                current = list[seg.Index];
                continue;
            }

            var prop = FindProperty(current.GetType(), seg.Name!);
            if (prop is null || !IsPubliclyReadable(prop)) return false;
            try { current = prop.GetValue(current); }
            catch (TargetInvocationException) { return false; }
        }

        value = current;
        return true;
    }

    /// <remarks>
    /// <para>
    /// A struct on the way to the value — a <c>Point</c> property, an element of a
    /// <c>List&lt;Point&gt;</c>, a <c>Point?</c> — is read out as a boxed <em>copy</em>. Setting the
    /// leaf on that copy changes nothing the model can see, and the write used to end there: the user's
    /// entry vanished on the next render with no message, because nothing had technically failed. So
    /// every struct the path passed through is written back into its owner, innermost first, until an
    /// owner is a class (which already holds the object that was changed).
    /// </para>
    /// <para>
    /// A malformed path is reported through <see cref="LastWriteFailed"/> like any other write that
    /// could not be applied, keeping the promise that writes never throw.
    /// </para>
    /// </remarks>
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

        // Each step taken: the owner, the segment read from it, and what came back.
        var walked = new List<(object Owner, BormPathSegment Segment, object Child)>(segments.Count - 1);

        object current = Root!;
        for (var i = 0; i < segments.Count - 1; i++)
        {
            var seg = segments[i];
            var child = ReadSegment(current, seg);
            if (child is null)
            {
                var created = CreateChild(current, seg);
                if (created is null)
                {
                    LastWriteFailed = true;
                    return;
                }
                WriteSegment(current, seg, created);
                // Re-read: the write may have converted the placeholder or been rejected outright.
                child = ReadSegment(current, seg);
                if (child is null)
                {
                    LastWriteFailed = true;
                    return;
                }
            }
            walked.Add((current, seg, child));
            current = child;
        }

        WriteSegment(current, segments[^1], value);
        if (LastWriteFailed) return;

        for (var i = walked.Count - 1; i >= 0; i--)
        {
            var (owner, seg, child) = walked[i];
            if (!child.GetType().IsValueType) break;

            WriteSegment(owner, seg, child);
            if (LastWriteFailed) return;
        }
    }

    public Type? GetElementType(string arrayPath)
    {
        var type = ResolveType(arrayPath);
        return type is null ? null : BormFieldTypeResolver.GetEnumerableElementType(type);
    }

    /// <summary>Resolves the declared CLR type at a path without reading any values.</summary>
    public Type? ResolveType(string path)
    {
        var segments = BormPath.Parse(path);
        Type? currentType = Root!.GetType();
        for (var i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (currentType is null) return null;
            currentType = seg.IsIndex
                ? BormFieldTypeResolver.GetEnumerableElementType(currentType)
                : FindProperty(currentType, seg.Name!)?.PropertyType;
        }
        return currentType;
    }

    private static PropertyInfo? FindProperty(Type type, string name)
        => PropertyCache.GetOrAdd((type, name), static key =>
        {
            var (declaring, propertyName) = key;
            // GetProperty(..., IgnoreCase) throws AmbiguousMatchException when a derived type shadows a
            // property with `new`, so the lookup is done by hand: an exact match wins, then a
            // case-insensitive one.
            var candidates = declaring.GetProperties(Flags)
                .Where(p => p.GetIndexParameters().Length == 0)
                .ToArray();

            return candidates.FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.Ordinal))
                ?? candidates.FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));
        });

    private static object? ReadSegment(object target, BormPathSegment seg)
    {
        if (seg.IsIndex)
            return target is IList list && seg.Index >= 0 && seg.Index < list.Count ? list[seg.Index] : null;

        var prop = FindProperty(target.GetType(), seg.Name!);
        if (prop is null || !IsPubliclyReadable(prop)) return null;
        try { return prop.GetValue(target); }
        catch (TargetInvocationException) { return null; }
    }

    /// <summary>
    /// Whether a form may write <paramref name="property"/>: it needs a <em>public</em> setter.
    /// <see cref="PropertyInfo.CanWrite"/> is true for a private, protected or internal one too, and
    /// reflection calls those without complaint — so <c>Id { get; private set; }</c> and
    /// <c>IsAdmin { get; internal set; }</c>, which the model's author kept out of reach on purpose,
    /// rendered as ordinary inputs and took whatever was posted to them. An <c>init</c> accessor is
    /// public and counts: a form building up a record is exactly what it is for.
    /// </summary>
    internal static bool IsPubliclyWritable(PropertyInfo property) => property.SetMethod is { IsPublic: true };

    /// <summary>Whether a form may read <paramref name="property"/>: it needs a public getter, for the same reason.</summary>
    internal static bool IsPubliclyReadable(PropertyInfo property) => property.GetMethod is { IsPublic: true };

    /// <summary>
    /// Whether a field bound to <paramref name="property"/> is read-only for want of a public setter.
    /// </summary>
    /// <remarks>
    /// Only where editing actually goes through the setter. A scalar is replaced on every edit, so it
    /// does. A class-typed group or list is not: its children are written on the object it already
    /// holds and its rows are added to and removed from the list in place, which is how
    /// <c>Shipping { get; private set; } = new()</c> and <c>Lines { get; private set; } = new()</c> —
    /// the usual shape of an EF or domain model — are meant to be edited. Locking those would lock
    /// every field beneath them. A struct group and an array do need it: a struct is changed by writing
    /// a modified copy back, and an array cannot grow without being replaced.
    /// </remarks>
    internal static bool IsReadOnlyForForm(PropertyInfo property, BormFieldType fieldType)
    {
        if (IsPubliclyWritable(property)) return false;
        if (fieldType is not (BormFieldType.Object or BormFieldType.Array)) return true;

        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        return type.IsValueType || type.IsArray;
    }

    private void WriteSegment(object target, BormPathSegment seg, object? value)
    {
        if (seg.IsIndex)
        {
            if (target is not IList list || seg.Index - list.Count > BormPath.MaxPadding) { LastWriteFailed = true; return; }

            var elementType = BormFieldTypeResolver.GetEnumerableElementType(target.GetType()) ?? typeof(object);
            if (!BormValueConverter.TryCoerce(value, elementType, out var converted))
            {
                LastWriteFailed = true;
                return;
            }

            try
            {
                while (list.Count <= seg.Index) list.Add(DefaultFor(elementType));
                list[seg.Index] = converted;
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or InvalidCastException)
            {
                LastWriteFailed = true;
            }
            return;
        }

        var prop = FindProperty(target.GetType(), seg.Name!);
        if (prop is null || !IsPubliclyWritable(prop)) { LastWriteFailed = true; return; }

        if (!BormValueConverter.TryCoerce(value, prop.PropertyType, out var result))
        {
            LastWriteFailed = true;
            return;
        }

        try
        {
            prop.SetValue(target, result);
        }
        catch (Exception ex) when (ex is ArgumentException or TargetInvocationException)
        {
            LastWriteFailed = true;
        }
    }

    /// <summary>Materialises the container needed to keep writing through <paramref name="seg"/>.</summary>
    private static object? CreateChild(object parent, BormPathSegment seg)
    {
        if (seg.IsIndex)
        {
            // The parent is the list itself, so create an element of its declared type — adding a bare
            // `new object()` to a List&lt;LineItem&gt; would throw.
            var elementType = BormFieldTypeResolver.GetEnumerableElementType(parent.GetType());
            return elementType is null ? null : Instantiate(elementType);
        }

        var prop = FindProperty(parent.GetType(), seg.Name!);
        return prop is null ? null : Instantiate(Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
    }

    private static object? Instantiate(Type type)
    {
        if (type == typeof(string)) return string.Empty;

        if (type.IsInterface || type.IsAbstract)
        {
            // Commonly the property is declared as IList<T>/ICollection<T>/IEnumerable<T>.
            var element = BormFieldTypeResolver.GetEnumerableElementType(type);
            if (element is not null)
            {
                var listType = typeof(List<>).MakeGenericType(element);
                if (type.IsAssignableFrom(listType)) return Activator.CreateInstance(listType);
            }
            return null;
        }

        try { return Activator.CreateInstance(type); }
        catch (Exception ex) when (ex is MissingMethodException or MemberAccessException or TargetInvocationException)
        {
            return null;
        }
    }

    private static object? DefaultFor(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;
}
