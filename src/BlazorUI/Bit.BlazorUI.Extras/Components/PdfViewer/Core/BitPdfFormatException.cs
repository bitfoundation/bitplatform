namespace Bit.BlazorUI;

/// <summary>
/// Thrown when the PDF byte stream cannot be parsed according to the PDF
/// specification (malformed tokens, structural errors, etc.).
/// </summary>
public sealed class BitPdfFormatException : Exception
{
    public BitPdfFormatException(string message) : base(message) { }
    public BitPdfFormatException(string message, Exception inner) : base(message, inner) { }
}
