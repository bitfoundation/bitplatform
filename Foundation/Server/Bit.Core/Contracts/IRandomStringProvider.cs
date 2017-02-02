namespace Foundation.Core.Contracts
{
    public interface IRandomStringProvider
    {
        string GetRandomNonSecureString(int length);
    }
}