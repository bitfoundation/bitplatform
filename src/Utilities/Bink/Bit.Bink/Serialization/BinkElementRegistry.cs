using System.Text.Json;

namespace Bit.Bink;

/// <summary>
/// Reads and writes the fields of a custom element type, so documents, clipboard payloads and collaboration
/// messages that contain it can be saved and loaded. Register it with <see cref="BinkElementRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// The serializer writes the <c>type</c> discriminator and every field all elements share (id, version, transform,
/// description, metadata, lock and groups) itself, plus the element's local <c>bounds</c>, so a reader without the
/// registration still keeps the element where it is and writes it back unchanged. A converter writes and reads only
/// the type's own fields, and must not use those names.
/// </para>
/// <para>
/// Reading is a parser of untrusted input. Validate every value and bound every size; report malformed input by
/// throwing <see cref="BinkFormatException"/>, <see cref="FormatException"/> or <see cref="ArgumentException"/>.
/// Coordinates are checked against the document's limits after the element is created, as for every element.
/// </para>
/// </remarks>
/// <typeparam name="TElement">The element type. Matched exactly when writing: a type derived from it needs its own converter.</typeparam>
public abstract class BinkElementConverter<TElement> where TElement : BinkElement
{
    /// <summary>The <c>type</c> discriminator written for the element, for example <c>"acme-sticky-note"</c>.</summary>
    public abstract string TypeName { get; }

    /// <summary>Writes the element's own fields into the JSON object the serializer has already opened.</summary>
    /// <param name="writer">The writer, positioned inside the element's object.</param>
    /// <param name="element">The element.</param>
    /// <param name="options">The serializer options in use.</param>
    public abstract void Write(Utf8JsonWriter writer, TElement element, BinkSerializerOptions options);

    /// <summary>Creates the element from its JSON object. The shared fields are applied to the result afterwards.</summary>
    /// <param name="json">The element's JSON object.</param>
    /// <param name="options">The deserializer options in use, including the limits for untrusted input.</param>
    public abstract TElement Read(JsonElement json, BinkDeserializerOptions options);
}

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
