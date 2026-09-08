using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

/// <summary>
/// One message delivered over a structured-messaging channel - a <see cref="MessagePortHandle"/>, a
/// <see cref="WorkerHandle"/>, or a shared worker's port.
/// </summary>
/// <remarks>
/// The browser's own channels carry structured clones, which say far more than JSON does: <c>Map</c>,
/// <c>Set</c>, <c>Date</c>, cyclic graphs, <c>ArrayBuffer</c>s. None of the extra survives the JSON
/// interop between .NET and JavaScript, so Butil draws the line in one place: <b>binary payloads stay
/// binary, and everything else becomes JSON</b>. A message posted as an <c>ArrayBuffer</c> or a typed
/// array arrives in <see cref="Data"/>; anything else arrives as JSON text in <see cref="Json"/>.
/// </remarks>
/// <param name="IsBinary">
/// Which of the two payloads carries this message. Branch on this rather than on a null check -
/// <c>null</c> is a message a sender can legitimately post, and it arrives as the JSON text
/// <c>"null"</c>.
/// </param>
/// <param name="Json">
/// The message as JSON, or null for a binary one. Always valid JSON, including for a payload that
/// was a plain string on the other side - which therefore arrives quoted, and deserializes with
/// <c>Deserialize&lt;string&gt;()</c>.
/// </param>
/// <param name="Data">The message's bytes, or null for a JSON one.</param>
public record ButilMessage(bool IsBinary, string? Json, byte[]? Data)
{
    /// <summary>
    /// Deserializes <see cref="Json"/> into <typeparamref name="T"/>.
    /// </summary>
    /// <returns><c>default</c> for a binary message, or one whose JSON is the literal <c>null</c>.</returns>
    /// <exception cref="JsonException">The payload is not valid JSON for <typeparamref name="T"/>.</exception>
    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public T? Deserialize<[DynamicallyAccessedMembers(JsonSerialized)] T>(JsonSerializerOptions? options = null)
        => Json is null ? default : JsonSerializer.Deserialize<T>(Json, options);
}
