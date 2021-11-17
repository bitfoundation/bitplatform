using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Core.Contracts
{
    public interface IPipelineAwareDisposable : IDisposable
    {
        Task WaitForDisposal(CancellationToken cancellationToken);
    }
}
