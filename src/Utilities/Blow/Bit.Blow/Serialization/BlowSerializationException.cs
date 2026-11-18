namespace Bit.Blow;

/// <summary>A graph could not be written: an element carries data no mapping says how to save.</summary>
public sealed class BlowSerializationException : Exception
{
    /// <summary>Creates the exception.</summary>
    public BlowSerializationException(string message) : base(message) { }

    /// <summary>Creates the exception with the error that caused it.</summary>
    public BlowSerializationException(string message, Exception inner) : base(message, inner) { }
}
