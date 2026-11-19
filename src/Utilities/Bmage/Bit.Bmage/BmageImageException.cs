namespace Bit.Bmage;

/// <summary>Base exception for all Bmage failures.</summary>
public class BmageImageException : Exception
{
    public BmageImageException(string message) : base(message) { }
    public BmageImageException(string message, Exception? innerException) : base(message, innerException) { }
}
