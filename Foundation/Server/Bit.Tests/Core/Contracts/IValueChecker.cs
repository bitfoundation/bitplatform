namespace Bit.Tests.Core.Contracts
{
    public interface IValueChecker
    {
        void CheckValue<T>(T val);
    }
}
