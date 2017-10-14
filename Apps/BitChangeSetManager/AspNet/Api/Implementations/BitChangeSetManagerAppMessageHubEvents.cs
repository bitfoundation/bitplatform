using Bit.Signalr;
using Bit.Signalr.Implementations;
using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Model;
using System;
using System.Threading;
using System.Threading.Tasks;
using Bit.Data.Contracts;
using BitChangeSetManager.DataAccess.Contracts;

namespace BitChangeSetManager.Api.Implementations
{
    public class BitChangeSetManagerAppMessageHubEvents : DefaultMessageHubEvents
    {
        public virtual IBitChangeSetManagerRepository<User> UsersRepository { get; set; }

        public override async Task OnConnected(MessagesHub hub)
        {
            User user = await UsersRepository.GetByIdAsync(CancellationToken.None, Guid.Parse(UserInformationProvider.GetCurrentUserId()));

            await hub.Groups.Add(hub.Context.ConnectionId, user.Culture.ToString());

            await base.OnConnected(hub);
        }
    }
}