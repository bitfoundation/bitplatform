namespace Bit.Core.Contracts
{
    public interface IScopeStatusManager
    {
        bool WasSucceeded();

        void MarkAsFailed(string reason);

        void MarkAsSucceeded();

        string FailureReason { get; }
    }
}