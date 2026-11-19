namespace Bit.Bink;

/// <summary>Thrown when a document cannot be read because it is malformed, too large or written by a newer version.</summary>
public sealed class BinkFormatException : Exception
{
    /// <summary>Creates the exception.</summary>
    public BinkFormatException() : base("The ink document is not valid.") { }

    /// <summary>Creates the exception with a message.</summary>
    public BinkFormatException(string message) : base(message) { }

    /// <summary>Creates the exception with a message and inner exception.</summary>
    public BinkFormatException(string message, Exception innerException) : base(message, innerException) { }
}
