using System;

namespace Bit.Butil;

/// <summary>
/// The one place <see cref="CryptoKeyHash"/> becomes the string Web Crypto expects. Kept together
/// so the mapping - and its deliberate refusal to default - cannot drift between the call sites
/// that sign, derive, import and wrap.
/// </summary>
internal static class CryptoHashName
{
    internal static string Resolve(CryptoKeyHash algorithm) => algorithm switch
    {
        CryptoKeyHash.Sha256 => "SHA-256",
        CryptoKeyHash.Sha384 => "SHA-384",
        CryptoKeyHash.Sha512 => "SHA-512",
        // An out-of-range value (only reachable by casting an invalid int to the enum) is a caller
        // bug. For crypto, fail loudly rather than silently substituting SHA-256.
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unsupported hash algorithm."),
    };
}
