using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using System;

namespace Bit.ViewModel.Implementations
{
    public class DefaultServerSentEventsTransport : ServerSentEventsTransport
    {
        public event Action OnDisconnected;

        public DefaultServerSentEventsTransport(IHttpClient httpClient)
            : base(httpClient)
        {
        }

        public override void Abort(IConnection connection, TimeSpan timeout, string connectionData)
        {
            OnDisconnected?.Invoke();

            base.Abort(connection, timeout, connectionData);
        }

        public override void LostConnection(IConnection connection)
        {
            OnDisconnected?.Invoke();

            base.LostConnection(connection);
        }
    }
}
