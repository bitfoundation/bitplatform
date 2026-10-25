using System.Security.Cryptography;

namespace Bit.Bcrdt;

/// <summary>
/// Supplies the client identifier a document uses to stamp the content it creates.
/// </summary>
/// <remarks>
/// A client identifier is a <i>session</i> identity, not a user identity and not a device identity.
/// Two browser tabs belonging to the same person must have different client identifiers, or their
/// concurrent edits would appear to come from one replica and the engine's ordering guarantees
/// would not hold. User identity belongs in the awareness layer, which is deliberately separate.
/// </remarks>
public interface IBcrdtClientIdProvider
{
    /// <summary>Produces an identifier for a new document session.</summary>
    ulong CreateClientId();
}

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

/// <summary>
/// A provider that always returns the same identifier.
/// </summary>
/// <remarks>
/// Intended for tests and benchmarks, where reproducibility matters more than uniqueness. Using it
/// in an application would give two concurrent sessions the same identity and break convergence.
/// </remarks>
/// <param name="clientId">The identifier to hand out.</param>
public sealed class BcrdtFixedClientIdProvider(ulong clientId) : IBcrdtClientIdProvider
{
    /// <inheritdoc/>
    public ulong CreateClientId() => clientId;
}
