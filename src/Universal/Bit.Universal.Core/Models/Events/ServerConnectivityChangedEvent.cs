using Prism.Events;

namespace Bit.Core.Models.Events
{
    public class ServerConnectivityChangedEvent : PubSubEvent<ServerConnectivityChangedEvent>
    {
        public virtual bool IsConnected { get; set; }
    }
}
