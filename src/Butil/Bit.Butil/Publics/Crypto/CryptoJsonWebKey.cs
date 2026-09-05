using System.Text.Json.Serialization;

namespace Bit.Butil;

/// <summary>
/// A key in the JSON Web Key format (RFC 7517) - the shape a server hands out and the only one of
/// the four key formats that is text rather than bytes.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/exportKey">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/exportKey</see>
/// </summary>
/// <remarks>
/// Which members are populated depends on the key: a symmetric key carries <see cref="K"/>, an RSA
/// key <see cref="N"/>/<see cref="E"/> (plus the private half), and an EC key
/// <see cref="Crv"/>/<see cref="X"/>/<see cref="Y"/>. Every value is base64url without padding, the
/// encoding the JWK specification uses - not the base64 <c>Convert.ToBase64String</c> produces.
/// <br/>
/// The members that do not apply are omitted rather than sent as null: a JWK's members are typed by
/// the specification, and an explicit <c>"n": null</c> makes the browser reject the whole key.
/// <br/>
/// A JWK with its private members set is key material in the clear: it is as sensitive as the raw
/// bytes, and the security note on <see cref="Crypto"/> applies to it unchanged.
/// </remarks>
public class CryptoJsonWebKey
{
    /// <summary>The key type: <c>"oct"</c> (symmetric), <c>"RSA"</c> or <c>"EC"</c>.</summary>
    [JsonPropertyName("kty")]
    public string Kty { get; set; } = string.Empty;

    /// <summary>The algorithm the key is intended for, e.g. <c>"A256GCM"</c>, <c>"RS256"</c>, <c>"HS256"</c>.</summary>
    [JsonPropertyName("alg")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Alg { get; set; }

    /// <summary>The intended use: <c>"sig"</c> or <c>"enc"</c>.</summary>
    [JsonPropertyName("use")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Use { get; set; }

    /// <summary>
    /// The operations the key may be used for, e.g. <c>["encrypt", "decrypt"]</c>. When set, it is
    /// what the key is imported with - the browser rejects an import asking for more than the JWK
    /// declares - so leave it null to get every usage the algorithm supports.
    /// </summary>
    [JsonPropertyName("key_ops")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? KeyOps { get; set; }

    /// <summary>Whether the key may be exported again once imported.</summary>
    [JsonPropertyName("ext")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Ext { get; set; }

    /// <summary>Symmetric key material (<c>"oct"</c> keys).</summary>
    [JsonPropertyName("k")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? K { get; set; }

    /// <summary>RSA modulus.</summary>
    [JsonPropertyName("n")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? N { get; set; }

    /// <summary>RSA public exponent.</summary>
    [JsonPropertyName("e")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? E { get; set; }

    /// <summary>RSA private exponent, or the EC private key.</summary>
    [JsonPropertyName("d")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? D { get; set; }

    /// <summary>RSA first prime factor.</summary>
    [JsonPropertyName("p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? P { get; set; }

    /// <summary>RSA second prime factor.</summary>
    [JsonPropertyName("q")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Q { get; set; }

    /// <summary>RSA first factor CRT exponent.</summary>
    [JsonPropertyName("dp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Dp { get; set; }

    /// <summary>RSA second factor CRT exponent.</summary>
    [JsonPropertyName("dq")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Dq { get; set; }

    /// <summary>RSA first CRT coefficient.</summary>
    [JsonPropertyName("qi")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Qi { get; set; }

    /// <summary>The named curve of an EC key, e.g. <c>"P-256"</c>.</summary>
    [JsonPropertyName("crv")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Crv { get; set; }

    /// <summary>The x coordinate of an EC public key.</summary>
    [JsonPropertyName("x")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? X { get; set; }

    /// <summary>The y coordinate of an EC public key.</summary>
    [JsonPropertyName("y")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Y { get; set; }
}
