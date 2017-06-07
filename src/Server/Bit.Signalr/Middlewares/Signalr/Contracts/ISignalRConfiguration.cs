using Microsoft.AspNet.SignalR;

namespace Bit.Signalr.Middlewares.Signalr.Contracts
{
    public interface ISignalRConfiguration
    {
        void Configure(HubConfiguration signalRConfig);
    }
}