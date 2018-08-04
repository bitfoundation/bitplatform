using Microsoft.AspNet.SignalR;

namespace Bit.Signalr.Contracts
{
    public interface ISignalRConfiguration
    {
        void Configure(HubConfiguration signalRConfig);
    }
}
