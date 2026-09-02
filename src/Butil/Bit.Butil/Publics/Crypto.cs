using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The Crypto interface represents basic cryptography features available in the current context. 
/// It allows access to a cryptographically strong random number generator and to cryptographic primitives.
/// <br />
/// More info: <see href="https://developer.mozilla.org/en-US/docs/Web/API/Crypto">https://developer.mozilla.org/en-US/docs/Web/API/Crypto</see>
/// </summary>
/// <remarks>
/// <b>Security note:</b> the key-handling methods on this type marshal raw key material across the
/// JavaScript&#8596;.NET interop boundary. Generated keys are created as <i>extractable</i> and their
/// bytes (symmetric <c>raw</c> keys, private <c>pkcs8</c> keys, and PBKDF2-derived bits) are exported
/// back to .NET, where they are transferred as base64 over the interop channel and may therefore
/// appear in interop logs, traces, or memory dumps. They are <b>not</b> retained inside the browser's
/// non-extractable key store. Treat returned key bytes as sensitive: avoid logging them, zero/clear
/// buffers when done where practical, and prefer server-side key custody when the threat model
/// requires keys never to leave a hardware/secure boundary.
/// </remarks>
[ButilService(typeof(Crypto))]
public class Crypto(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>crypto.subtle</c>.</summary>
    /// <remarks>
    /// SubtleCrypto is secure-context only, so this is <c>false</c> on a plain <c>http://</c> page
    /// in every engine - the usual reason encryption "isn't supported" on a development box served
    /// over the network rather than over localhost. <see cref="RandomUuid"/> and
    /// <see cref="GetRandomValues"/> do not go through SubtleCrypto and work either way; everything
    /// that encrypts, signs, hashes or derives does.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.crypto.isSupported");

    /// <summary>
    /// Returns a cryptographically strong random Guid (v4 UUID).
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Crypto/randomUUID">https://developer.mozilla.org/en-US/docs/Web/API/Crypto/randomUUID</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async ValueTask<Guid> RandomUuid()
    {
        var raw = await js.Invoke<string>("BitButil.crypto.randomUUID");
        // During prerender/SSR the invoke returns a safe default (empty string), and a genuine
        // call always yields a parseable UUID. Guid.Parse(null/"") would throw, contradicting the
        // documented "returns default rather than throwing" prerender contract - so guard it.
        return Guid.TryParse(raw, out var uuid) ? uuid : default;
    }

    /// <summary>
    /// Fills <paramref name="length"/> bytes with cryptographically strong random values.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Crypto/getRandomValues">https://developer.mozilla.org/en-US/docs/Web/API/Crypto/getRandomValues</see>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="length"/> is negative or above the
    /// browser's per-call limit (65 536).</exception>
    public ValueTask<byte[]> GetRandomValues(int length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "length must be non-negative.");
        if (length > 65536)
            throw new ArgumentOutOfRangeException(nameof(length), "Web Crypto rejects requests larger than 65 536 bytes.");

        return js.Invoke<byte[]>("BitButil.crypto.getRandomValues", length);
    }

    /// <summary>
    /// Computes a digest of <paramref name="data"/> using the requested algorithm.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/digest">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/digest</see>
    /// </summary>
    public ValueTask<byte[]> Digest(CryptoKeyHash algorithm, byte[] data)
    {
        return js.Invoke<byte[]>("BitButil.crypto.digest", HashAlgorithmName(algorithm), data);
    }

    /// <summary>
    /// Produces an HMAC tag for <paramref name="data"/> using the given symmetric key.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/sign">SubtleCrypto.sign()</see>
    /// </summary>
    public ValueTask<byte[]> SignHmac(CryptoKeyHash algorithm, byte[] key, byte[] data)
    {
        var algo = HashAlgorithmName(algorithm);
        return js.Invoke<byte[]>("BitButil.crypto.signHmac", algo, key, data);
    }

    /// <summary>
    /// Verifies an HMAC tag previously produced by <see cref="SignHmac"/> (or any compatible producer).
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/verify">SubtleCrypto.verify()</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> VerifyHmac(CryptoKeyHash algorithm, byte[] key, byte[] signature, byte[] data)
    {
        var algo = HashAlgorithmName(algorithm);
        return js.Invoke<bool>("BitButil.crypto.verifyHmac", algo, key, signature, data);
    }

    private static string HashAlgorithmName(CryptoKeyHash algorithm) => CryptoHashName.Resolve(algorithm);

    // ─── Key generation / import / export ──────────────────────────────────────

    /// <summary>
    /// Generates a fresh AES key as raw bytes.
    /// </summary>
    /// <param name="bits">Key length in bits - 128, 192, or 256.</param>
    /// <remarks>The key is returned as extractable raw bytes - see the security note on <see cref="Crypto"/>.</remarks>
    public ValueTask<byte[]> GenerateAesKey(int bits = 256)
        => js.Invoke<byte[]>("BitButil.crypto.generateAesKey", bits);

    /// <summary>
    /// Generates an HMAC key of the requested length and hash.
    /// </summary>
    /// <remarks>The key is returned as extractable raw bytes - see the security note on <see cref="Crypto"/>.</remarks>
    public ValueTask<byte[]> GenerateHmacKey(CryptoKeyHash algorithm = CryptoKeyHash.Sha256, int? lengthBits = null)
        => js.Invoke<byte[]>("BitButil.crypto.generateHmacKey", HashAlgorithmName(algorithm), lengthBits);

    /// <summary>
    /// Generates an RSA key pair (RSA-OAEP). Returns spki/pkcs8 DER bytes for public/private.
    /// </summary>
    /// <remarks>The private key is returned as extractable pkcs8 bytes - see the security note on <see cref="Crypto"/>.</remarks>
    public ValueTask<RsaKeyPair> GenerateRsaKeyPair(int modulusLengthBits = 2048,
                                                    CryptoKeyHash algorithm = CryptoKeyHash.Sha256)
        => js.Invoke<RsaKeyPair>("BitButil.crypto.generateRsaKeyPair", modulusLengthBits, HashAlgorithmName(algorithm));

    /// <summary>
    /// Generates an ECDSA key pair on the named curve.
    /// </summary>
    /// <param name="curve">One of <c>"P-256"</c>, <c>"P-384"</c>, <c>"P-521"</c>.</param>
    /// <remarks>The private key is returned as extractable pkcs8 bytes - see the security note on <see cref="Crypto"/>.</remarks>
    public ValueTask<EcKeyPair> GenerateEcdsaKeyPair(string curve = "P-256")
        => js.Invoke<EcKeyPair>("BitButil.crypto.generateEcdsaKeyPair", curve);

    // ─── Derivation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Derives raw bytes from a password using PBKDF2.
    /// </summary>
    /// <remarks>The derived bits are returned as raw bytes - see the security note on <see cref="Crypto"/>.</remarks>
    public ValueTask<byte[]> DerivePbkdf2(byte[] password, byte[] salt, int iterations,
                                          int outputLengthBits, CryptoKeyHash algorithm = CryptoKeyHash.Sha256)
        => js.Invoke<byte[]>("BitButil.crypto.derivePbkdf2", password, salt, iterations, outputLengthBits, HashAlgorithmName(algorithm));

    // ─── RSA-PSS sign / verify ─────────────────────────────────────────────────

    /// <summary>
    /// Produces an RSA-PSS signature using a PKCS8 private key.
    /// </summary>
    public ValueTask<byte[]> SignRsaPss(byte[] privateKey, byte[] data, int saltLength = 32,
                                        CryptoKeyHash algorithm = CryptoKeyHash.Sha256)
        => js.Invoke<byte[]>("BitButil.crypto.signRsaPss", privateKey, data, saltLength, HashAlgorithmName(algorithm));

    /// <summary>
    /// Verifies an RSA-PSS signature using an SPKI public key.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> VerifyRsaPss(byte[] publicKey, byte[] signature, byte[] data, int saltLength = 32,
                                        CryptoKeyHash algorithm = CryptoKeyHash.Sha256)
        => js.Invoke<bool>("BitButil.crypto.verifyRsaPss", publicKey, signature, data, saltLength, HashAlgorithmName(algorithm));

    // ─── ECDSA sign / verify ───────────────────────────────────────────────────

    /// <summary>
    /// Produces an ECDSA signature using a PKCS8 private key.
    /// </summary>
    public ValueTask<byte[]> SignEcdsa(byte[] privateKey, byte[] data, string curve = "P-256",
                                       CryptoKeyHash algorithm = CryptoKeyHash.Sha256)
        => js.Invoke<byte[]>("BitButil.crypto.signEcdsa", privateKey, data, curve, HashAlgorithmName(algorithm));

    /// <summary>
    /// Verifies an ECDSA signature using an SPKI public key.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> VerifyEcdsa(byte[] publicKey, byte[] signature, byte[] data, string curve = "P-256",
                                       CryptoKeyHash algorithm = CryptoKeyHash.Sha256)
        => js.Invoke<bool>("BitButil.crypto.verifyEcdsa", publicKey, signature, data, curve, HashAlgorithmName(algorithm));

    // ─── Key import / export ───────────────────────────────────────────────────

    /// <summary>
    /// Re-expresses key material in another format: reads <paramref name="key"/> as
    /// <paramref name="sourceFormat"/> and writes it back out as <paramref name="targetFormat"/>.
    /// This is how a key that came from somewhere else - a server, a file, another library - is
    /// turned into the bytes the rest of this type takes.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/exportKey">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/exportKey</see>
    /// </summary>
    /// <param name="key">The key material, in <paramref name="sourceFormat"/>.</param>
    /// <param name="sourceFormat">How <paramref name="key"/> is encoded. Not <see cref="CryptoKeyFormat.Jwk"/> - use <see cref="ImportJsonWebKey"/>.</param>
    /// <param name="targetFormat">How to encode the result. Not <see cref="CryptoKeyFormat.Jwk"/> - use <see cref="ExportJsonWebKey"/>.</param>
    /// <param name="algorithm">The algorithm the key belongs to. The browser rejects a key imported under the wrong one.</param>
    /// <exception cref="ArgumentException">Either format is <see cref="CryptoKeyFormat.Jwk"/>, which is an object rather than bytes.</exception>
    /// <remarks>
    /// Both directions require the key to be extractable, so this cannot be used with the PBKDF2 and
    /// HKDF derivation keys, which the specification forbids from ever being exported. The exported
    /// bytes cross the interop boundary - see the security note on <see cref="Crypto"/>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CryptoKeyAlgorithmJsOptions))]
    public ValueTask<byte[]> ExportKey(byte[] key, CryptoKeyFormat sourceFormat, CryptoKeyFormat targetFormat, CryptoKeyAlgorithm algorithm)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        RequireByteFormat(sourceFormat, nameof(sourceFormat));
        RequireByteFormat(targetFormat, nameof(targetFormat));

        return js.Invoke<byte[]>("BitButil.crypto.exportKey",
            CryptoFormatName.Resolve(sourceFormat), key, CryptoFormatName.Resolve(targetFormat), new CryptoKeyAlgorithmJsOptions(algorithm));
    }

    /// <summary>
    /// Exports key material as a JSON Web Key - the format a server usually publishes and consumes.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/exportKey">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/exportKey</see>
    /// </summary>
    /// <param name="key">The key material, in <paramref name="sourceFormat"/>.</param>
    /// <param name="sourceFormat">How <paramref name="key"/> is encoded - raw, pkcs8 or spki.</param>
    /// <param name="algorithm">The algorithm the key belongs to.</param>
    /// <exception cref="ArgumentException"><paramref name="sourceFormat"/> is <see cref="CryptoKeyFormat.Jwk"/>.</exception>
    /// <remarks>A JWK exported from a private key contains the private half in the clear - see the security note on <see cref="Crypto"/>.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CryptoKeyAlgorithmJsOptions))]
    public ValueTask<CryptoJsonWebKey> ExportJsonWebKey(byte[] key, CryptoKeyFormat sourceFormat, CryptoKeyAlgorithm algorithm)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        RequireByteFormat(sourceFormat, nameof(sourceFormat));

        return js.Invoke<CryptoJsonWebKey>("BitButil.crypto.exportJwk",
            CryptoFormatName.Resolve(sourceFormat), key, new CryptoKeyAlgorithmJsOptions(algorithm));
    }

    /// <summary>
    /// Imports a JSON Web Key and hands back its bytes in <paramref name="targetFormat"/>, ready for
    /// the encrypt, sign and derive methods on this type.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/importKey">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/importKey</see>
    /// </summary>
    /// <param name="jwk">The key, as published.</param>
    /// <param name="algorithm">The algorithm to import it under. A JWK's own <c>alg</c> member is a hint, not a substitute - the browser reads this one.</param>
    /// <param name="targetFormat">How to encode the result: raw for a symmetric key, spki for a public key, pkcs8 for a private one.</param>
    /// <exception cref="ArgumentException"><paramref name="targetFormat"/> is <see cref="CryptoKeyFormat.Jwk"/>, which would be a no-op.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CryptoKeyAlgorithmJsOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CryptoJsonWebKey))]
    public ValueTask<byte[]> ImportJsonWebKey(CryptoJsonWebKey jwk, CryptoKeyAlgorithm algorithm, CryptoKeyFormat targetFormat = CryptoKeyFormat.Raw)
    {
        ArgumentNullException.ThrowIfNull(jwk);
        ArgumentNullException.ThrowIfNull(algorithm);
        RequireByteFormat(targetFormat, nameof(targetFormat));

        return js.Invoke<byte[]>("BitButil.crypto.importJwk",
            jwk, new CryptoKeyAlgorithmJsOptions(algorithm), CryptoFormatName.Resolve(targetFormat));
    }

    /// <summary>
    /// JWK is a JSON object, not a byte string, so the methods that move bytes take the other three
    /// formats and leave it to <see cref="ExportJsonWebKey"/> / <see cref="ImportJsonWebKey"/>.
    /// Refusing it here names the parameter instead of failing inside the browser.
    /// </summary>
    private static void RequireByteFormat(CryptoKeyFormat format, string parameterName)
    {
        if (format is CryptoKeyFormat.Jwk)
            throw new ArgumentException("JWK is an object rather than bytes; use ExportJsonWebKey / ImportJsonWebKey.", parameterName);
    }

    // ─── Key agreement and derivation ──────────────────────────────────────────

    /// <summary>
    /// Generates an ECDH key pair on the named curve, for deriving a shared secret with someone
    /// else's public key.
    /// </summary>
    /// <param name="curve">One of <c>"P-256"</c>, <c>"P-384"</c>, <c>"P-521"</c>.</param>
    /// <remarks>The private key is returned as extractable pkcs8 bytes - see the security note on <see cref="Crypto"/>.</remarks>
    public ValueTask<EcKeyPair> GenerateEcdhKeyPair(string curve = "P-256")
        => js.Invoke<EcKeyPair>("BitButil.crypto.generateEcdhKeyPair", curve);

    /// <summary>
    /// Derives raw shared-secret bits from your ECDH private key and the other party's public key.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/deriveBits">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/deriveBits</see>
    /// </summary>
    /// <param name="privateKey">Your private key, as PKCS#8 DER bytes.</param>
    /// <param name="publicKey">Their public key, as SPKI DER bytes.</param>
    /// <param name="outputLengthBits">How many bits to derive. Both sides must ask for the same number.</param>
    /// <param name="curve">The curve both keys are on.</param>
    /// <remarks>
    /// The raw output of ECDH is the x coordinate of a point, not a uniformly random key: run it
    /// through <see cref="DeriveHkdfBits"/> (or use <see cref="DeriveEcdhKey"/>, which derives a
    /// usable key directly) rather than encrypting with it as it stands.
    /// </remarks>
    public ValueTask<byte[]> DeriveEcdhBits(byte[] privateKey, byte[] publicKey, int outputLengthBits, string curve = "P-256")
        => js.Invoke<byte[]>("BitButil.crypto.deriveEcdhBits", privateKey, publicKey, curve, outputLengthBits);

    /// <summary>
    /// Derives a usable key from an ECDH agreement, in one step.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/deriveKey">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/deriveKey</see>
    /// </summary>
    /// <param name="privateKey">Your private key, as PKCS#8 DER bytes.</param>
    /// <param name="publicKey">Their public key, as SPKI DER bytes.</param>
    /// <param name="derivedKeyAlgorithm">What the derived key is - e.g. <c>CryptoKeyAlgorithm.AesGcm(256)</c>.</param>
    /// <param name="curve">The curve both keys are on.</param>
    /// <remarks>The derived key is returned as extractable raw bytes - see the security note on <see cref="Crypto"/>.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CryptoKeyAlgorithmJsOptions))]
    public ValueTask<byte[]> DeriveEcdhKey(byte[] privateKey, byte[] publicKey, CryptoKeyAlgorithm derivedKeyAlgorithm, string curve = "P-256")
    {
        ArgumentNullException.ThrowIfNull(derivedKeyAlgorithm);

        return js.Invoke<byte[]>("BitButil.crypto.deriveEcdhKey",
            privateKey, publicKey, curve, new CryptoKeyAlgorithmJsOptions(derivedKeyAlgorithm));
    }

    /// <summary>
    /// Derives raw bytes from existing high-entropy key material using HKDF.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/deriveBits">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/deriveBits</see>
    /// </summary>
    /// <param name="keyMaterial">The input keying material - a shared secret, not a password.</param>
    /// <param name="salt">Optional salt. Non-secret, and may be empty.</param>
    /// <param name="info">Optional context string binding the output to a purpose ("encryption", "signing", a session id).</param>
    /// <param name="outputLengthBits">How many bits to derive.</param>
    /// <param name="algorithm">The digest to extract and expand with.</param>
    /// <remarks>
    /// HKDF is not a password hash: it does no stretching at all, so a low-entropy input stays as
    /// weak as it started. Use <see cref="DerivePbkdf2"/> for passwords.
    /// </remarks>
    public ValueTask<byte[]> DeriveHkdfBits(byte[] keyMaterial, byte[]? salt, byte[]? info, int outputLengthBits,
                                            CryptoKeyHash algorithm = CryptoKeyHash.Sha256)
        => js.Invoke<byte[]>("BitButil.crypto.deriveHkdfBits", keyMaterial, salt, info, outputLengthBits, HashAlgorithmName(algorithm));

    /// <summary>
    /// Derives a usable key from existing high-entropy key material using HKDF.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/deriveKey">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/deriveKey</see>
    /// </summary>
    /// <param name="keyMaterial">The input keying material - a shared secret, not a password.</param>
    /// <param name="salt">Optional salt. Non-secret, and may be empty.</param>
    /// <param name="info">Optional context string binding the output to a purpose.</param>
    /// <param name="derivedKeyAlgorithm">What the derived key is - e.g. <c>CryptoKeyAlgorithm.AesGcm(256)</c>.</param>
    /// <param name="algorithm">The digest to extract and expand with.</param>
    /// <remarks>The derived key is returned as extractable raw bytes - see the security note on <see cref="Crypto"/>.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CryptoKeyAlgorithmJsOptions))]
    public ValueTask<byte[]> DeriveHkdfKey(byte[] keyMaterial, byte[]? salt, byte[]? info, CryptoKeyAlgorithm derivedKeyAlgorithm,
                                           CryptoKeyHash algorithm = CryptoKeyHash.Sha256)
    {
        ArgumentNullException.ThrowIfNull(derivedKeyAlgorithm);

        return js.Invoke<byte[]>("BitButil.crypto.deriveHkdfKey",
            keyMaterial, salt, info, HashAlgorithmName(algorithm), new CryptoKeyAlgorithmJsOptions(derivedKeyAlgorithm));
    }

    /// <summary>
    /// Stretches a password into a usable key with PBKDF2 - <see cref="DerivePbkdf2"/>'s output as a
    /// key of a stated algorithm rather than as loose bits.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/deriveKey">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/deriveKey</see>
    /// </summary>
    /// <param name="password">The password bytes.</param>
    /// <param name="salt">A random per-user salt.</param>
    /// <param name="iterations">The iteration count. Six figures, not four.</param>
    /// <param name="derivedKeyAlgorithm">What the derived key is - e.g. <c>CryptoKeyAlgorithm.AesGcm(256)</c>. Its length decides the output size.</param>
    /// <param name="algorithm">The digest to stretch with.</param>
    /// <remarks>The derived key is returned as extractable raw bytes - see the security note on <see cref="Crypto"/>.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CryptoKeyAlgorithmJsOptions))]
    public ValueTask<byte[]> DerivePbkdf2Key(byte[] password, byte[] salt, int iterations, CryptoKeyAlgorithm derivedKeyAlgorithm,
                                             CryptoKeyHash algorithm = CryptoKeyHash.Sha256)
    {
        ArgumentNullException.ThrowIfNull(derivedKeyAlgorithm);

        return js.Invoke<byte[]>("BitButil.crypto.derivePbkdf2Key",
            password, salt, iterations, HashAlgorithmName(algorithm), new CryptoKeyAlgorithmJsOptions(derivedKeyAlgorithm));
    }

    // ─── Key wrapping ──────────────────────────────────────────────────────────

    /// <summary>
    /// Generates an AES-KW key - the algorithm whose only job is encrypting other keys.
    /// </summary>
    /// <param name="bits">Key length in bits - 128, 192, or 256.</param>
    /// <remarks>The key is returned as extractable raw bytes - see the security note on <see cref="Crypto"/>.</remarks>
    public ValueTask<byte[]> GenerateAesKwKey(int bits = 256)
        => js.Invoke<byte[]>("BitButil.crypto.generateAesKwKey", bits);

    /// <summary>
    /// Encrypts key material with another key, so it can be stored or sent without ever appearing in
    /// the clear.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/wrapKey">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/wrapKey</see>
    /// </summary>
    /// <param name="key">The key material to wrap, in <paramref name="format"/>.</param>
    /// <param name="format">How <paramref name="key"/> is encoded, and how it will be encoded again when unwrapped. Not <see cref="CryptoKeyFormat.Jwk"/>.</param>
    /// <param name="keyAlgorithm">The algorithm the wrapped key belongs to.</param>
    /// <param name="wrappingKey">The key doing the wrapping: raw bytes for the AES algorithms, SPKI bytes for RSA-OAEP.</param>
    /// <param name="wrapAlgorithm">How to wrap - <see cref="AesKwCryptoAlgorithmParams"/>, <see cref="AesGcmCryptoAlgorithmParams"/>, <see cref="AesCbcCryptoAlgorithmParams"/>, <see cref="AesCtrCryptoAlgorithmParams"/> or <see cref="RsaOaepCryptoAlgorithmParams"/>.</param>
    /// <param name="wrappingKeyHash">The digest the RSA-OAEP wrapping key was created with. Ignored by the AES algorithms.</param>
    /// <exception cref="ArgumentException"><paramref name="format"/> is <see cref="CryptoKeyFormat.Jwk"/>.</exception>
    /// <remarks>
    /// Unlike everything else on this type, the result is <i>not</i> sensitive: that is the point of
    /// wrapping. The key going in still is.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CryptoKeyAlgorithmJsOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesKwCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesCtrCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesCbcCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesGcmCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RsaOaepCryptoAlgorithmParams))]
    public ValueTask<byte[]> WrapKey<T>(byte[] key, CryptoKeyFormat format, CryptoKeyAlgorithm keyAlgorithm,
                                        byte[] wrappingKey, T wrapAlgorithm, CryptoKeyHash? wrappingKeyHash = null)
        where T : ICryptoAlgorithmParams
    {
        ArgumentNullException.ThrowIfNull(keyAlgorithm);
        ArgumentNullException.ThrowIfNull(wrapAlgorithm);
        RequireByteFormat(format, nameof(format));

        return js.Invoke<byte[]>("BitButil.crypto.wrapKey",
            CryptoFormatName.Resolve(format), key, new CryptoKeyAlgorithmJsOptions(keyAlgorithm),
            wrappingKey, wrapAlgorithm, wrappingKeyHash is null ? null : HashAlgorithmName(wrappingKeyHash.Value));
    }

    /// <summary>
    /// Reverses <see cref="WrapKey{T}"/>: decrypts wrapped key material and hands back its bytes.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/unwrapKey">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/unwrapKey</see>
    /// </summary>
    /// <param name="wrappedKey">The wrapped bytes.</param>
    /// <param name="format">The format the key was wrapped in, and the format it comes back in. Not <see cref="CryptoKeyFormat.Jwk"/>.</param>
    /// <param name="unwrappedKeyAlgorithm">The algorithm the wrapped key belongs to. The wrapping carries no record of it, so this has to match what <see cref="WrapKey{T}"/> was told.</param>
    /// <param name="unwrappingKey">The key doing the unwrapping: raw bytes for the AES algorithms, PKCS#8 bytes for RSA-OAEP.</param>
    /// <param name="unwrapAlgorithm">How it was wrapped. Must match the wrap, including the IV where one was used.</param>
    /// <param name="unwrappingKeyHash">The digest the RSA-OAEP unwrapping key was created with. Ignored by the AES algorithms.</param>
    /// <exception cref="ArgumentException"><paramref name="format"/> is <see cref="CryptoKeyFormat.Jwk"/>.</exception>
    /// <remarks>The unwrapped key is returned as extractable bytes - see the security note on <see cref="Crypto"/>.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CryptoKeyAlgorithmJsOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesKwCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesCtrCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesCbcCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesGcmCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RsaOaepCryptoAlgorithmParams))]
    public ValueTask<byte[]> UnwrapKey<T>(byte[] wrappedKey, CryptoKeyFormat format, CryptoKeyAlgorithm unwrappedKeyAlgorithm,
                                          byte[] unwrappingKey, T unwrapAlgorithm, CryptoKeyHash? unwrappingKeyHash = null)
        where T : ICryptoAlgorithmParams
    {
        ArgumentNullException.ThrowIfNull(unwrappedKeyAlgorithm);
        ArgumentNullException.ThrowIfNull(unwrapAlgorithm);
        RequireByteFormat(format, nameof(format));

        return js.Invoke<byte[]>("BitButil.crypto.unwrapKey",
            CryptoFormatName.Resolve(format), wrappedKey, new CryptoKeyAlgorithmJsOptions(unwrappedKeyAlgorithm),
            unwrappingKey, unwrapAlgorithm, unwrappingKeyHash is null ? null : HashAlgorithmName(unwrappingKeyHash.Value));
    }

    /// <summary>
    /// The Encrypt method of the Crypto interface that encrypts data.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/encrypt">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/encrypt</see>
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesCtrCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesCbcCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesGcmCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RsaOaepCryptoAlgorithmParams))]
    public ValueTask<byte[]> Encrypt<T>(T algorithm, byte[] key, byte[] data, CryptoKeyHash? keyHash = null) where T : ICryptoAlgorithmParams
    {
        if (algorithm.GetType() == typeof(RsaOaepCryptoAlgorithmParams))
        {
            var keyHashString = keyHash switch
            {
                CryptoKeyHash.Sha384 => "SHA-384",
                CryptoKeyHash.Sha512 => "SHA-512",
                _ => "SHA-256",
            };

            return js.Invoke<byte[]>("BitButil.crypto.encryptRsaOaep", algorithm, key, data, keyHashString);
        }

        if (algorithm.GetType() == typeof(AesCtrCryptoAlgorithmParams))
        {
            return js.Invoke<byte[]>("BitButil.crypto.encryptAesCtr", algorithm, key, data);
        }

        if (algorithm.GetType() == typeof(AesCbcCryptoAlgorithmParams))
        {
            return js.Invoke<byte[]>("BitButil.crypto.encryptAesCbc", algorithm, key, data);
        }


        return js.Invoke<byte[]>("BitButil.crypto.encryptAesGcm", algorithm, key, data);
    }

    /// <summary>
    /// The Encrypt method of the Crypto interface that encrypts data.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/encrypt">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/encrypt</see>
    /// </summary>
    /// <param name="algorithm">Which algorithm to use. Every AES mode requires <paramref name="iv"/>.</param>
    /// <param name="key">The raw key bytes.</param>
    /// <param name="data">The plaintext to encrypt.</param>
    /// <param name="iv">The initialization vector (the counter block for AES-CTR). Required for the AES modes, ignored for RSA-OAEP.</param>
    /// <param name="keyHash">The digest to use with RSA-OAEP. Ignored by the AES modes.</param>
    /// <exception cref="ArgumentNullException">An AES mode was requested without an <paramref name="iv"/>.</exception>
    public ValueTask<byte[]> Encrypt(CryptoAlgorithm algorithm, byte[] key, byte[] data, byte[]? iv = null, CryptoKeyHash? keyHash = null)
        => algorithm switch
        {
            CryptoAlgorithm.AesCtr => Encrypt(new AesCtrCryptoAlgorithmParams { Counter = RequireIv(algorithm, iv) }, key, data, null),
            CryptoAlgorithm.AesCbc => Encrypt(new AesCbcCryptoAlgorithmParams { Iv = RequireIv(algorithm, iv) }, key, data, null),
            CryptoAlgorithm.AesGcm => Encrypt(new AesGcmCryptoAlgorithmParams { Iv = RequireIv(algorithm, iv) }, key, data, null),
            _ => Encrypt(new RsaOaepCryptoAlgorithmParams(), key, data, keyHash),
        };

    /// <summary>
    /// The AES modes have no meaningful default IV, so a null one can only produce a WebCrypto
    /// <c>OperationError</c> from inside the browser. Failing here names the parameter instead.
    /// </summary>
    private static byte[] RequireIv(CryptoAlgorithm algorithm, byte[]? iv)
        => iv ?? throw new ArgumentNullException(nameof(iv),
            $"{algorithm} requires an initialization vector; pass one through the iv parameter.");

    /// <summary>
    /// The Decrypt method of the Crypto interface that decrypts data.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/decrypt">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/decrypt</see>
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesCtrCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesCbcCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesGcmCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RsaOaepCryptoAlgorithmParams))]
    public ValueTask<byte[]> Decrypt<T>(T algorithm, byte[] key, byte[] data, CryptoKeyHash? keyHash = null) where T : ICryptoAlgorithmParams
    {
        if (algorithm.GetType() == typeof(RsaOaepCryptoAlgorithmParams))
        {
            var keyHashString = keyHash switch
            {
                CryptoKeyHash.Sha384 => "SHA-384",
                CryptoKeyHash.Sha512 => "SHA-512",
                _ => "SHA-256",
            };

            return js.Invoke<byte[]>("BitButil.crypto.decryptRsaOaep", algorithm, key, data, keyHashString);
        }

        if (algorithm.GetType() == typeof(AesCtrCryptoAlgorithmParams))
        {
            return js.Invoke<byte[]>("BitButil.crypto.decryptAesCtr", algorithm, key, data);
        }

        if (algorithm.GetType() == typeof(AesCbcCryptoAlgorithmParams))
        {
            return js.Invoke<byte[]>("BitButil.crypto.decryptAesCbc", algorithm, key, data);
        }

        return js.Invoke<byte[]>("BitButil.crypto.decryptAesGcm", algorithm, key, data);
    }
    /// <summary>
    /// The Decrypt method of the Crypto interface that decrypts data.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/decrypt">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/decrypt</see>
    /// </summary>
    /// <param name="algorithm">Which algorithm to use. Every AES mode requires <paramref name="iv"/>.</param>
    /// <param name="key">The raw key bytes.</param>
    /// <param name="data">The ciphertext to decrypt.</param>
    /// <param name="iv">The initialization vector (the counter block for AES-CTR) the data was encrypted with. Required for the AES modes, ignored for RSA-OAEP.</param>
    /// <param name="keyHash">The digest to use with RSA-OAEP. Ignored by the AES modes.</param>
    /// <exception cref="ArgumentNullException">An AES mode was requested without an <paramref name="iv"/>.</exception>
    public ValueTask<byte[]> Decrypt(CryptoAlgorithm algorithm, byte[] key, byte[] data, byte[]? iv = null, CryptoKeyHash? keyHash = null)
        => algorithm switch
        {
            CryptoAlgorithm.AesCtr => Decrypt(new AesCtrCryptoAlgorithmParams { Counter = RequireIv(algorithm, iv) }, key, data, null),
            CryptoAlgorithm.AesCbc => Decrypt(new AesCbcCryptoAlgorithmParams { Iv = RequireIv(algorithm, iv) }, key, data, null),
            CryptoAlgorithm.AesGcm => Decrypt(new AesGcmCryptoAlgorithmParams { Iv = RequireIv(algorithm, iv) }, key, data, null),
            _ => Decrypt(new RsaOaepCryptoAlgorithmParams(), key, data, keyHash),
        };
}
