namespace Bit.Bmage;

/// <summary>Thrown when JSON is not a document, pipeline or object Bmage can read.</summary>
public sealed class BmageImageDocumentFormatException : BmageImageException
{
    public BmageImageDocumentFormatException(string message) : base(message) { }
    public BmageImageDocumentFormatException(string message, Exception? innerException) : base(message, innerException) { }
}
