namespace Bit.Bmage;

/// <summary>Thrown when an image cannot be encoded in the requested format.</summary>
public sealed class BmageImageEncodeException : BmageImageException
{
    public BmageImageEncodeException(string message) : base(message) { }
    public BmageImageEncodeException(string message, Exception? innerException) : base(message, innerException) { }
}
