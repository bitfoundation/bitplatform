using Prism.Events;

namespace Bit.Model.Events
{
    public class ConnectivityChangedEvent : PubSubEvent<ConnectivityChangedEvent>
    {
        public virtual bool IsConnected { get; set; }
    }
}
