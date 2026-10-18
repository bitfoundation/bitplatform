using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Browser;

/// <summary>
/// AES-GCM backed by the browser's WebCrypto implementation, for use with
/// <see cref="BlazeDbEncryptedStorage"/> in Blazor WebAssembly. The managed
/// <see cref="System.Security.Cryptography.AesGcm"/> throws in the browser sandbox, so encryption
/// at rest in the browser has to go through SubtleCrypto - which is asynchronous, hence the
/// async shape of <see cref="IBlazeDbAeadCipher"/>.
/// </summary>
[SupportedOSPlatform("browser")]
public sealed class BlazeDbWebCryptoCipher : IBlazeDbAeadCipher, IDisposable
{
    private readonly JSObject _key;
    private bool _disposed;

    private BlazeDbWebCryptoCipher(JSObject key) => _key = key;

    /// <summary>
    /// Imports a raw AES key (16, 24 or 32 bytes) as a non-extractable WebCrypto key. Derive one
    /// from a passphrase with <see cref="BlazeDbEncryptedStorage.DeriveKey"/>, which uses PBKDF2 - one of
    /// the few managed primitives that does work in the browser.
    /// </summary>
    public static async ValueTask<BlazeDbWebCryptoCipher> CreateAsync(byte[] rawKey, string? moduleUrl = null)
    {
        if (rawKey is not { Length: 16 or 24 or 32 })
        {
            throw new ArgumentException("Key must be 16, 24 or 32 bytes.", nameof(rawKey));
        }
        await EnsureModuleAsync(moduleUrl).ConfigureAwait(false);
        var key = await BlazeDbCryptoInterop.ImportKey(rawKey).ConfigureAwait(false);
        return new BlazeDbWebCryptoCipher(key);
    }

    private static Task EnsureModuleAsync(string? moduleUrl) => BlazeDbCryptoInterop.EnsureModuleAsync(moduleUrl);

    public int NonceSizeInBytes => 12;

    public int TagSizeInBytes => 16;

    public async ValueTask<byte[]> EncryptAsync(
        ReadOnlyMemory<byte> nonce,
        ReadOnlyMemory<byte> plaintext,
        ReadOnlyMemory<byte> associatedData,
        CancellationToken cancellationToken = default)
    {
        using var result = await BlazeDbCryptoInterop
            .Encrypt(_key, nonce.ToArray(), plaintext.ToArray(), associatedData.ToArray())
            .ConfigureAwait(false);
        return ToBytes(result);
    }

    public async ValueTask<byte[]> DecryptAsync(
        ReadOnlyMemory<byte> nonce,
        ReadOnlyMemory<byte> ciphertextWithTag,
        ReadOnlyMemory<byte> associatedData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var result = await BlazeDbCryptoInterop
                .Decrypt(_key, nonce.ToArray(), ciphertextWithTag.ToArray(), associatedData.ToArray())
                .ConfigureAwait(false);
            return ToBytes(result);
        }
        catch (JSException ex)
        {
            // SubtleCrypto reports a failed tag check as a bare OperationError; translate it into
            // the shape BlazeDbEncryptedStorage expects so it reads as corruption, not an interop bug.
            throw new System.Security.Cryptography.CryptographicException(
                "WebCrypto could not authenticate the ciphertext.", ex);
        }
    }

    private static byte[] ToBytes(JSObject array)
    {
        var bytes = new byte[array.GetPropertyAsInt32("length")];
        BlazeDbCryptoInterop.CopyBytes(array, bytes);
        return bytes;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _key.Dispose();
    }
}
