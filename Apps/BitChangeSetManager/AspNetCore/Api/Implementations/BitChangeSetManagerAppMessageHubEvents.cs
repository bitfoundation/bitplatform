using Bit.Signalr;
using Bit.Signalr.Implementations;
using BitChangeSetManager.DataAccess.Contracts;
using BitChangeSetManager.Model;
using Microsoft.Owin;
using System;
using System.Threading.Tasks;

namespace BitChangeSetManager.Api.Implementations
{
    public class BitChangeSetManagerAppMessageHubEvents : DefaultMessageHubEvents
    {
        public virtual IBitChangeSetManagerRepository<User> UsersRepository { get; set; }

        public virtual IOwinContext OwinContext { get; set; }

        public override async Task OnConnected(MessagesHub hub)
        {
            User user = await UsersRepository.GetByIdAsync(OwinContext.Request.CallCancelled, Guid.Parse(UserInformationProvider.GetCurrentUserId()));

            await hub.Groups.Add(hub.Context.ConnectionId, user.Culture.ToString());

            await base.OnConnected(hub);
        }
    }
}