using System;

namespace Bit.Data.Contracts
{
    public interface IUnitOfWork
    {
        IDisposable BeginWork();

        void CommitWork();

        void RollbackWork();
    }
}
