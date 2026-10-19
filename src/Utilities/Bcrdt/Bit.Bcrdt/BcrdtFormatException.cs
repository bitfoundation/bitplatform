namespace Bit.Bcrdt;

/// <summary>
/// Thrown when binary data that claims to be a bit Bcrdt update, state vector or
/// protocol message cannot be decoded.
/// </summary>
/// <remarks>
/// This exception is the contract that lets a host reject hostile or corrupted input safely: every
/// decoder validates lengths and tags before allocating, so a malformed payload raises this
/// exception instead of crashing, hanging or partially mutating a document. Callers that accept
/// updates from an untrusted peer should catch it and drop the message.
/// </remarks>
public sealed class BcrdtFormatException : Exception
{
    /// <summary>Creates the exception with a description of what failed to decode.</summary>
    public BcrdtFormatException(string message) : base(message) { }

    /// <summary>Creates the exception with a description and the underlying cause.</summary>
    public BcrdtFormatException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>Creates an exception describing a payload that ended sooner than its framing claimed.</summary>
    public static BcrdtFormatException Truncated(string what, int needed, int available) =>
        new($"Truncated {what}: needed {needed} byte(s) but only {available} remain.");

    /// <summary>
    /// Creates an exception describing a discriminator this build does not recognise.
    /// </summary>
    /// <remarks>
    /// Reserved tag values are how the formats stay forward-compatible: a reader that meets a newer
    /// tag reports it clearly instead of guessing, so an old client talking to a new one fails in a
    /// way somebody can act on rather than corrupting data quietly.
    /// </remarks>
    public static BcrdtFormatException UnknownTag(string what, int tag) =>
        new($"Unknown {what} tag 0x{tag:X2}.");

    [DoesNotReturn]
    internal static void Throw(string message) => throw new BcrdtFormatException(message);
}
