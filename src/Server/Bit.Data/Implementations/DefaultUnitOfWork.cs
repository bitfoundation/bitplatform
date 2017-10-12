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
        public virtual IScopeStatusManager ScopeStatusManager { get; set; }

        public IDisposable BeginWork()
        {
            ScopeStatusManager.MarkAsFailed(); // Failed by default
            return new UnitOfWorkManager();
        }

        public void CommitWork()
        {
            ScopeStatusManager.MarkAsSucceeded();
        }

        public void RollbackWork()
        {
            ScopeStatusManager.MarkAsFailed();
        }
    }
}
