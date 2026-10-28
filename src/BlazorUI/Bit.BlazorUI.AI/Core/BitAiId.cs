using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Bit.BlazorUI;

/// <summary>
/// Generates short, sortable, collision-resistant identifiers for conversations, messages, and parts.
/// </summary>
/// <remarks>
/// <para>
/// Identifiers are used as rendering keys on every message and part, so they are on the hot path of
/// every diff. A <see cref="Guid"/> would cost 36 characters and lose ordering; these are 20-ish
/// characters, sort chronologically as strings, and stay unique across processes.
/// </para>
/// <para>
/// The layout is a millisecond timestamp in base32 followed by 40 bits of randomness, which is the
/// same shape as a ULID and gives roughly a one-in-a-trillion collision chance per millisecond.
/// </para>
/// </remarks>
public static class BitAiId
{
    private const string Alphabet = "0123456789abcdefghjkmnpqrstvwxyz";

    /// <summary>
    /// Creates an identifier with an optional prefix.
    /// </summary>
    /// <param name="prefix">
    /// A short tag such as <c>msg</c> or <c>part</c>, joined with an underscore. Useful when
    /// debugging a stream, since the identifier says what it points at.
    /// </param>
    /// <returns>The identifier.</returns>
    public static string New(string? prefix = null)
    {
        Span<char> buffer = stackalloc char[16];
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        for (var i = 9; i >= 0; i--)
        {
            buffer[i] = Alphabet[(int)(timestamp & 31)];
            timestamp >>= 5;
        }

        Span<byte> random = stackalloc byte[4];
        RandomNumberGenerator.Fill(random);
        var entropy = BinaryPrimitives.ReadUInt32LittleEndian(random);
        for (var i = 15; i >= 10; i--)
        {
            buffer[i] = Alphabet[(int)(entropy & 31)];
            entropy >>= 5;
        }

        return prefix is null ? new string(buffer) : string.Concat(prefix, "_", new string(buffer));
    }

    /// <summary>Creates a conversation identifier.</summary>
    /// <returns>The identifier.</returns>
    public static string NewConversationId() => New("conv");

    /// <summary>Creates a message identifier.</summary>
    /// <returns>The identifier.</returns>
    public static string NewMessageId() => New("msg");

    /// <summary>Creates a message part identifier.</summary>
    /// <returns>The identifier.</returns>
    public static string NewPartId() => New("part");

    /// <summary>Creates a tool call identifier.</summary>
    /// <returns>The identifier.</returns>
    public static string NewToolCallId() => New("call");

    /// <summary>Creates an artifact identifier.</summary>
    /// <returns>The identifier.</returns>
    public static string NewArtifactId() => New("art");

    /// <summary>Creates an agent run identifier.</summary>
    /// <returns>The identifier.</returns>
    public static string NewRunId() => New("run");
}
