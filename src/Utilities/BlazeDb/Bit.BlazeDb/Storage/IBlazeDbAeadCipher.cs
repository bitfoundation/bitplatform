namespace Bit.BlazeDb.Storage;

/// <summary>
/// An authenticated cipher used by <see cref="BlazeDbEncryptedStorage"/>. Kept as an abstraction because
/// the platforms differ: .NET's managed AES is unavailable in the browser sandbox, where the only
/// route to AES-GCM is the asynchronous WebCrypto API. Hence the async signatures - they cost
/// nothing on desktop and are required in the browser.
/// </summary>
public interface IBlazeDbAeadCipher
{
    /// <summary>Nonce length in bytes. 12 for AES-GCM.</summary>
    int NonceSizeInBytes { get; }

    /// <summary>Authentication tag length in bytes, included in the ciphertext. 16 for AES-GCM.</summary>
    int TagSizeInBytes { get; }

    /// <summary>Encrypts <paramref name="plaintext"/>, returning ciphertext with the tag appended.</summary>
    ValueTask<byte[]> EncryptAsync(
        ReadOnlyMemory<byte> nonce,
        ReadOnlyMemory<byte> plaintext,
        ReadOnlyMemory<byte> associatedData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts and verifies. Implementations must throw if authentication fails rather than
    /// returning unverified plaintext.
    /// </summary>
    ValueTask<byte[]> DecryptAsync(
        ReadOnlyMemory<byte> nonce,
        ReadOnlyMemory<byte> ciphertextWithTag,
        ReadOnlyMemory<byte> associatedData,
        CancellationToken cancellationToken = default);
}
