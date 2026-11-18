using System.Text.Json;

namespace Bit.Bink;

/// <summary>
/// Maps custom element types to their <see cref="BinkElementConverter{TElement}"/>. Without a registration the
/// serializer refuses to write a custom element, because nothing could read it back, and a document containing one
/// written elsewhere loads it as a <see cref="BinkUnknownElement"/> that is preserved but not drawn. Registration is
/// thread-safe.
/// </summary>
/// <example>
/// <code>
/// BinkElementRegistry.Default.Register(new StickyNoteConverter());
/// </code>
/// </example>
public sealed class BinkElementRegistry
{
    // The built-in type names, which a custom type cannot take over: documents everywhere already use them.
    private static readonly HashSet<string> Reserved = new(StringComparer.Ordinal)
    {
        "stroke", "rect", "ellipse", "line", "polygon", "path", "text", "image", "highlight", "callout",
    };

    /// <summary>The property names the serializer writes for every element, which a converter must not write.</summary>
    internal static readonly HashSet<string> SharedFields = new(StringComparer.Ordinal)
    {
        "type", "id", "v", "transform", "description", "metadata", "locked", "groups", "bounds",
    };

    private readonly Dictionary<string, Entry> _byName = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, Entry> _byType = new();
    private readonly object _gate = new();

    /// <summary>The shared registry the serializer uses unless the options name another.</summary>
    public static BinkElementRegistry Default { get; } = new();

    /// <summary>Registers, or replaces, the converter for an element type.</summary>
    /// <exception cref="ArgumentException">The type name is empty, too long, or one of the built-in element types.</exception>
    public void Register<TElement>(BinkElementConverter<TElement> converter) where TElement : BinkElement
    {
        ArgumentNullException.ThrowIfNull(converter);
        string name = converter.TypeName;
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(converter));
        if (name.Length > 256) throw new ArgumentException("An element type name can be at most 256 characters long.", nameof(converter));
        if (Reserved.Contains(name)) throw new ArgumentException($"\"{name}\" is a built-in element type.", nameof(converter));

        var entry = new Entry(name, typeof(TElement),
            (writer, element, options) => converter.Write(writer, (TElement)element, options),
            (json, options) => converter.Read(json, options));
        lock (_gate)
        {
            // Replacing a registration under either key drops the old one under the other, so a type never ends up
            // written under one name and read under another.
            if (_byName.TryGetValue(name, out Entry? previousByName)) _byType.Remove(previousByName.ElementType);
            if (_byType.TryGetValue(typeof(TElement), out Entry? previousByType)) _byName.Remove(previousByType.TypeName);
            _byName[name] = entry;
            _byType[typeof(TElement)] = entry;
        }
    }

    /// <summary>Removes the registration for a type name. Returns false when there was none.</summary>
    public bool Unregister(string typeName)
    {
        ArgumentNullException.ThrowIfNull(typeName);
        lock (_gate)
        {
            if (!_byName.Remove(typeName, out Entry? entry)) return false;
            _byType.Remove(entry.ElementType);
            return true;
        }
    }

    /// <summary>True when a converter is registered for the type name.</summary>
    public bool Contains(string typeName)
    {
        lock (_gate) return _byName.ContainsKey(typeName);
    }

    /// <summary>The registered type names.</summary>
    public IReadOnlyCollection<string> TypeNames
    {
        get { lock (_gate) return _byName.Keys.ToArray(); }
    }

    internal bool TryGet(Type elementType, out Entry? entry)
    {
        lock (_gate) return _byType.TryGetValue(elementType, out entry);
    }

    internal bool TryGet(string typeName, out Entry? entry)
    {
        lock (_gate) return _byName.TryGetValue(typeName, out entry);
    }

    internal sealed record Entry(
        string TypeName,
        Type ElementType,
        Action<Utf8JsonWriter, BinkElement, BinkSerializerOptions> Write,
        Func<JsonElement, BinkDeserializerOptions, BinkElement> Read);
}
