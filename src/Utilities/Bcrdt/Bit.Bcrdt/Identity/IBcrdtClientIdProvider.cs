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
