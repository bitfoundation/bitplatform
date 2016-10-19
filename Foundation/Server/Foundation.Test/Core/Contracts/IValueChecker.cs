namespace Foundation.Test.Core.Contracts
{
    public interface IValueChecker
    {
        void CheckValue<T>(T val);
    }
}
