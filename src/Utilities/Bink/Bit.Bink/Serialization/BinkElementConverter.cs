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
