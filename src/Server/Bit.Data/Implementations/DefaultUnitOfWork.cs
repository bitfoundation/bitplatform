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
        public virtual IScopeStatusManager ScopeStatusManager { get; set; } = default!;

        public IDisposable BeginWork()
        {
            ScopeStatusManager.MarkAsFailed($"As {nameof(DefaultUnitOfWork)} begins it work, scope is failed by default, until {nameof(CommitWork)} is called");
            return new UnitOfWorkManager();
        }

        public void CommitWork()
        {
            ScopeStatusManager.MarkAsSucceeded();
        }

        public void RollbackWork()
        {
            ScopeStatusManager.MarkAsFailed($"{nameof(RollbackWork)} of {nameof(DefaultUnitOfWork)} is called");
        }
    }
}
