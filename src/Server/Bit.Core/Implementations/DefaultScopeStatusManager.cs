using Bit.Core.Contracts;

namespace Bit.Core.Implementations
{
    public class DefaultScopeStatusManager : IScopeStatusManager
    {
        private bool _isFailed;
        private string _failureReason;

        public virtual string FailureReason => _failureReason;

        public virtual bool WasSucceeded()
        {
            return _isFailed == false;
        }

        public virtual void MarkAsFailed(string reason)
        {
            _isFailed = true;
            _failureReason = reason;
        }

        public virtual void MarkAsSucceeded()
        {
            _isFailed = false;
        }

        public override string ToString()
        {
            return $"Scope failed: {_isFailed}, Reason: {_failureReason}";
        }
    }
}
