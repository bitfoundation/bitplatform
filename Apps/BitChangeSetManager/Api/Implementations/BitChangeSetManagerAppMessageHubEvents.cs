using Bit.Core.Contracts;
using Bit.Signalr.Middlewares.Signalr;
using Bit.Signalr.Middlewares.Signalr.Implementations;
using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BitChangeSetManager.Api.Implementations
{
    public class BitChangeSetManagerAppMessageHubEvents : DefaultMessageHubEvents
    {
        private readonly IUserInformationProvider _userInformationProvider;
        private readonly IBitChangeSetManagerRepository<User> _usersRepository;

        public BitChangeSetManagerAppMessageHubEvents(IUserInformationProvider userInformationProvider, IBitChangeSetManagerRepository<Model.User> usersRepository)
            : base(userInformationProvider)
        {
            _userInformationProvider = userInformationProvider;
            _usersRepository = usersRepository;
        }

        public override async Task OnConnected(MessagesHub hub)
        {
            User user = await _usersRepository.GetByIdAsync(Guid.Parse(_userInformationProvider.GetCurrentUserId()), default(CancellationToken));

            await hub.Groups.Add(hub.Context.ConnectionId, user.Culture.ToString());

            await base.OnConnected(hub);
        }
    }
}