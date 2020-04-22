using Prism.Events;

namespace Bit.Model.Events
{
    public class ServerConnectivityChangedEvent : PubSubEvent<ServerConnectivityChangedEvent>
    {
        public virtual bool IsConnected { get; set; }
    }
}
