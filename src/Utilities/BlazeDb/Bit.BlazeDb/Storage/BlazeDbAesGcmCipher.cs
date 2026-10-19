using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Bit.BlazeDb.Storage;

/// <summary>
/// AES-GCM via .NET's managed implementation, for desktop hosts and tests.
/// Not usable in Blazor WebAssembly: the browser sandbox ships no AES implementation and
/// <see cref="AesGcm"/> throws there. Use the WebCrypto-backed cipher from Bit.BlazeDb.Browser instead.
/// </summary>
[UnsupportedOSPlatform("browser")]
public sealed class BlazeDbAesGcmCipher : IBlazeDbAeadCipher, IDisposable
{
    private readonly AesGcm _aes;

    /// <param name="key">A 16, 24 or 32 byte key. 32 bytes (AES-256) is recommended.</param>
    public BlazeDbAesGcmCipher(ReadOnlySpan<byte> key)
    {
        _aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
    }

    public int NonceSizeInBytes => AesGcm.NonceByteSizes.MaxSize;

    public int TagSizeInBytes => AesGcm.TagByteSizes.MaxSize;

    public ValueTask<byte[]> EncryptAsync(
        ReadOnlyMemory<byte> nonce,
        ReadOnlyMemory<byte> plaintext,
        ReadOnlyMemory<byte> associatedData,
        CancellationToken cancellationToken = default)
    {
        var output = new byte[plaintext.Length + TagSizeInBytes];
        _aes.Encrypt(
            nonce.Span,
            plaintext.Span,
            output.AsSpan(0, plaintext.Length),
            output.AsSpan(plaintext.Length),
            associatedData.Span);
        return new ValueTask<byte[]>(output);
    }

    public ValueTask<byte[]> DecryptAsync(
        ReadOnlyMemory<byte> nonce,
        ReadOnlyMemory<byte> ciphertextWithTag,
        ReadOnlyMemory<byte> associatedData,
        CancellationToken cancellationToken = default)
    {
        var bodyLength = ciphertextWithTag.Length - TagSizeInBytes;
        if (bodyLength < 0)
        {
            throw new CryptographicException("Ciphertext is shorter than the authentication tag.");
        }
        var output = new byte[bodyLength];
        _aes.Decrypt(
            nonce.Span,
            ciphertextWithTag.Span[..bodyLength],
            ciphertextWithTag.Span[bodyLength..],
            output,
            associatedData.Span);
        return new ValueTask<byte[]>(output);
    }

    public void Dispose() => _aes.Dispose();
}
