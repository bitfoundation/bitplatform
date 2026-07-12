namespace Bit.BlazorUI;

/// <summary>
/// Thrown when a document declares an <c>/Encrypt</c> dictionary that this
/// library cannot handle — an unsupported security handler or revision, or a
/// cryptographic primitive that is unavailable on the current platform (for
/// example MD5/AES in the browser WebAssembly sandbox). Distinct from
/// <see cref="BitPdfFormatException"/> so callers can surface a clear "this
/// encrypted PDF is not supported" message instead of loading ciphertext as if
/// it were valid content.
/// </summary>
public sealed class BitPdfUnsupportedEncryptionException : Exception
{
    public BitPdfUnsupportedEncryptionException(string message) : base(message) { }
    public BitPdfUnsupportedEncryptionException(string message, Exception inner) : base(message, inner) { }
}
