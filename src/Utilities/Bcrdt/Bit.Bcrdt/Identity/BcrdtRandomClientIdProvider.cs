using System.Security.Cryptography;

namespace Bit.Bcrdt;

/// <summary>
/// The default provider: 64 bits from a cryptographic random source, per session.
/// </summary>
/// <remarks>
/// Randomness rather than a counter is deliberate. A counter would need a coordinator to hand out
/// values, which would defeat the point of a CRDT; 64 random bits make a collision within one
/// document's lifetime negligible without any coordination at all.
/// </remarks>
public sealed class BcrdtRandomClientIdProvider : IBcrdtClientIdProvider
{
    /// <summary>A shared instance; the type is stateless and thread-safe.</summary>
    public static readonly BcrdtRandomClientIdProvider Instance = new();

    /// <inheritdoc/>
    public ulong CreateClientId()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        ulong value = BitConverter.ToUInt64(bytes);
        // Zero is reserved as "unassigned" throughout the stack, so nudge it off that value.
        return value == 0 ? 1UL : value;
    }
}
