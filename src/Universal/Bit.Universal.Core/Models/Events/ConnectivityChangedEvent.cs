using Prism.Events;

namespace Bit.Core.Models.Events
{
    public class ConnectivityChangedEvent : PubSubEvent<ConnectivityChangedEvent>
    {
        public virtual bool IsConnected { get; set; }
    }
}
