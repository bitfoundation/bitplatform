using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.ViewModel.Contracts
{
    public interface IMessageReceiver : IDisposable
    {
        Task Start(CancellationToken cancellationToken);
        Task Stop(CancellationToken cancellationToken);

        bool IsConnected { get; }

        /// <summary>
        /// Do we expect this to be connected or not? But you know, things might not be as we expect!
        /// </summary>
        bool ShouldBeConnected { get; set; }
    }
}
