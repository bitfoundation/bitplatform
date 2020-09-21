using Bit.Core.Contracts;
using Bit.Signalr.Contracts;
using System;
using System.Threading.Tasks;

namespace Bit.Signalr.Implementations
{
    public class DefaultMessageHubEvents : IMessagesHubEvents
    {
        public virtual IUserInformationProvider UserInformationProvider { get; set; } = default!;

        public virtual async Task OnConnected(MessagesHub hub)
        {
            if (hub == null)
                throw new ArgumentNullException(nameof(hub));

            if (UserInformationProvider.IsAuthenticated())
                await hub.Groups.Add(hub.Context.ConnectionId, UserInformationProvider.GetCurrentUserId()).ConfigureAwait(false);
        }

        public virtual Task OnDisconnected(MessagesHub hub, bool stopCalled)
        {
            if (hub == null)
                throw new ArgumentNullException(nameof(hub));

            return Task.CompletedTask;
        }

        public virtual Task OnReconnected(MessagesHub hub)
        {
            if (hub == null)
                throw new ArgumentNullException(nameof(hub));

            return Task.CompletedTask;
        }
    }
}
