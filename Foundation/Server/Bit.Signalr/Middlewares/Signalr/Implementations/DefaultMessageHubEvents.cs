using Foundation.Api.Middlewares.SignalR.Contracts;
using Foundation.Core.Contracts;
using System;
using System.Threading.Tasks;

namespace Foundation.Api.Middlewares.SignalR.Implementations
{
    public class DefaultMessageHubEvents : IMessagesHubEvents
    {
        private readonly IUserInformationProvider _userInformationProvider;

        public DefaultMessageHubEvents(IUserInformationProvider userInformationProvider)
        {
            if (userInformationProvider == null)
                throw new ArgumentNullException(nameof(userInformationProvider));

            _userInformationProvider = userInformationProvider;
        }

        protected DefaultMessageHubEvents()
        {

        }

        public virtual async Task OnConnected(MessagesHub hub)
        {
            if (_userInformationProvider.IsAuthenticated())
                await hub.Groups.Add(hub.Context.ConnectionId, _userInformationProvider.GetCurrentUserId());
        }

        public virtual async Task OnDisconnected(MessagesHub hub, bool stopCalled)
        {

        }

        public virtual async Task OnReconnected(MessagesHub hub)
        {

        }
    }
}
