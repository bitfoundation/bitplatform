namespace Bit.Core.Contracts
{
    public interface IRandomStringProvider
    {
        string GetRandomNonSecureString(int length);
    }
}