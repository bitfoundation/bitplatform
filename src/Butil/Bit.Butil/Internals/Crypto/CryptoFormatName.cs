using System;

namespace Bit.Butil;

/// <summary>
/// The one place <see cref="CryptoKeyFormat"/> becomes the string Web Crypto expects.
/// </summary>
internal static class CryptoFormatName
{
    internal static string Resolve(CryptoKeyFormat format) => format switch
    {
        CryptoKeyFormat.Raw => "raw",
        CryptoKeyFormat.Pkcs8 => "pkcs8",
        CryptoKeyFormat.Spki => "spki",
        CryptoKeyFormat.Jwk => "jwk",
        // Only reachable by casting an invalid int to the enum; substituting "raw" would read a
        // private key as symmetric bytes, so fail instead.
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported key format."),
    };
}
