using Bit.Core.Contracts;

namespace Bit.Core.Implementations
{
    public class DefaultScopeStatusManager : IScopeStatusManager
    {
        private bool _isFailed;

        public virtual bool WasSucceeded()
        {
            return _isFailed == false;
        }

        public virtual void MarkAsFailed()
        {
            _isFailed = true;
        }

        public virtual void MarkAsSucceeded()
        {
            _isFailed = false;
        }
    }
}
