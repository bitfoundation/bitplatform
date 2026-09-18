using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

/// <summary>
/// A message posted to this window by another browsing context.
/// </summary>
/// <param name="Origin">
/// Where it came from, as <c>"https://example.com"</c> - and the only thing that identifies the
/// sender. It is set by the browser and cannot be forged, which is what makes it worth checking;
/// nothing inside <paramref name="Json"/> has that property.
/// </param>
/// <param name="IsBinary">
/// Which of the two payloads carries this message. Branch on this rather than on a null check -
/// <c>null</c> is a message a sender can legitimately post, arriving as the JSON text <c>"null"</c>.
/// </param>
/// <param name="Json">The message as JSON, or null for a binary one. Always valid JSON, so a payload that was a plain string arrives quoted.</param>
/// <param name="Data">The message's bytes, or null for a JSON one.</param>
/// <param name="Ports">
/// Ports the sender transferred along with the message, or an empty array. They belong to this
/// document now, and deliver nothing until <see cref="MessagePortHandle.Start"/> is called - which
/// is the usual way a cross-document handshake turns into a private channel.
/// <br/>
/// A port has one owner, so when several listeners accept the same message only the first of them -
/// in registration order - is given the ports; the rest see an empty array. Handing every listener a
/// handle over the same port would mean the first disposal closing it under all the others.
/// </param>
public record WindowMessage(string Origin, bool IsBinary, string? Json, byte[]? Data, MessagePortHandle[] Ports)
{
    /// <summary>
    /// Deserializes <see cref="Json"/> into <typeparamref name="T"/>.
    /// </summary>
    /// <returns><c>default</c> for a binary message, or one whose JSON is the literal <c>null</c>.</returns>
    /// <exception cref="JsonException">The payload is not valid JSON for <typeparamref name="T"/>.</exception>
    /// <remarks>
    /// Whatever comes back is data from another origin: check <see cref="Origin"/> before you act on
    /// it, and validate it as you would a request body.
    /// </remarks>
    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public T? Deserialize<[DynamicallyAccessedMembers(JsonSerialized)] T>(JsonSerializerOptions? options = null)
        => Json is null ? default : JsonSerializer.Deserialize<T>(Json, options);
}
