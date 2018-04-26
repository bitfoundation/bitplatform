namespace Bit.Core.Contracts
{
    public interface IRandomStringProvider
    {
        string GetRandomString(int length);
    }
}