namespace Bit.Bmage;

/// <summary>Thrown when an image exceeds a configured <see cref="BmageImageLimits"/> value.</summary>
public sealed class BmageImageLimitExceededException : BmageImageException
{
    public BmageImageLimitExceededException(string message) : base(message) { }
}
