using System;

namespace Bit.Hangfire.Contracts
{
    public interface IJobSchedulerBackendConfiguration : IDisposable
    {
        void Init();
    }
}
