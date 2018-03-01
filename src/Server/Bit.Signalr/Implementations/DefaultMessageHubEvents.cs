using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Signalr.Contracts;

namespace Bit.Signalr.Implementations
{
    public class DefaultMessageHubEvents : IMessagesHubEvents
    {
        public virtual IUserInformationProvider UserInformationProvider { get; set; }

        public virtual async Task OnConnected(MessagesHub hub)
        {
            if (UserInformationProvider.IsAuthenticated())
                await hub.Groups.Add(hub.Context.ConnectionId, UserInformationProvider.GetCurrentUserId()).ConfigureAwait(false);
        }

        public virtual async Task OnDisconnected(MessagesHub hub, bool stopCalled)
        {

        }

        public virtual async Task OnReconnected(MessagesHub hub)
        {

        }
    }
}
