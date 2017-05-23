using Microsoft.AspNet.SignalR;

namespace Foundation.Api.Middlewares.SignalR.Contracts
{
    public interface ISignalRConfiguration
    {
        void Configure(HubConfiguration signalRConfig);
    }
}