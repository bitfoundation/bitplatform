namespace Bit.Bmage;

/// <summary>Thrown when encoded image data cannot be decoded (corrupt, truncated or unsupported).</summary>
public sealed class BmageImageDecodeException : BmageImageException
{
    public BmageImageDecodeException(string message) : base(message) { }
    public BmageImageDecodeException(string message, Exception? innerException) : base(message, innerException) { }
}
