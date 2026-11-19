namespace Bit.Bmage;

/// <summary>Thrown when a capability (format, API) is not available in the current environment.</summary>
public sealed class BmageImageCapabilityException : BmageImageException
{
    public BmageImageCapabilityException(string message) : base(message) { }
    public BmageImageCapabilityException(string message, Exception? innerException) : base(message, innerException) { }
}
