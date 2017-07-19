using Bit.Core.Contracts;
using Bit.Data.Contracts;
using System;

namespace Bit.Data.Implementations
{
    internal class UnitOfWorkManager : IDisposable
    {
        public virtual void Dispose()
        {

        }
    }

    public class DefaultUnitOfWork : IUnitOfWork
    {
        private readonly IScopeStatusManager _scopeStatusManager;

        public DefaultUnitOfWork(IScopeStatusManager scopeStatusManager)
        {
            if (scopeStatusManager == null)
                throw new ArgumentNullException(nameof(scopeStatusManager));

            _scopeStatusManager = scopeStatusManager;
        }

#if DEBUG
        protected DefaultUnitOfWork()
        {

        }
#endif

        public IDisposable BeginWork()
        {
            _scopeStatusManager.MarkAsFailed(); // Failed by default
            return new UnitOfWorkManager();
        }

        public void CommitWork()
        {
            _scopeStatusManager.MarkAsSucceeded();
        }

        public void RollbackWork()
        {
            _scopeStatusManager.MarkAsFailed();
        }
    }
}
