namespace Bit.BlazorUI;

/// <summary>
/// Thrown when an encrypted document requires a password that was not supplied
/// or was incorrect. Callers can catch this to prompt the user and retry
/// <see cref="BitPdfDocument.Load(byte[], string?)"/> with a password.
/// </summary>
public sealed class BitPdfPasswordException : Exception
{
    /// <summary><c>true</c> when a password was supplied but rejected; <c>false</c>
    /// when none was supplied for a document that needs one.</summary>
    public bool WasProvided { get; }

    public BitPdfPasswordException(string message, bool wasProvided) : base(message)
        => WasProvided = wasProvided;
}
