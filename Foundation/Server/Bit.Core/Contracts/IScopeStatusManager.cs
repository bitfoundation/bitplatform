namespace Foundation.Core.Contracts
{
    public interface IScopeStatusManager
    {
        bool WasSucceeded();

        void MarkAsFailed();

        void MarkAsSucceeded();
    }
}