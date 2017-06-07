using System;
using System.Linq;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Bit.Signalr.Middlewares.Signalr.Implementations
{
    public class SignalRMessageSender : IMessageSender
    {
        private readonly IMessageContentFormatter _formatter;
        private readonly IConnectionManager _connectionManager;

        protected SignalRMessageSender()
        {
        }

        public SignalRMessageSender(IMessageContentFormatter formatter, Microsoft.AspNet.SignalR.IDependencyResolver signalRDependencyResolver)
        {
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            if (signalRDependencyResolver == null)
                throw new ArgumentNullException(nameof(signalRDependencyResolver));

            _formatter = formatter;
            _connectionManager = signalRDependencyResolver.Resolve<IConnectionManager>();
        }

        public virtual async Task SendMessageToUsersAsync<T>(string messageKey, T messageArgs, params string[] userIds)
            where T : class
        {
            if (userIds == null)
                throw new ArgumentNullException(nameof(userIds));

            if (messageKey == null)
                throw new ArgumentNullException(nameof(messageKey));

            IHubContext hubContext = _connectionManager.GetHubContext<MessagesHub>();

            string objectArgsAsString = messageArgs == null ? string.Empty : _formatter.Serialize(messageArgs);

            userIds.ToList()
                .ForEach(async u =>
                {
                    await hubContext.Clients.Group(u).OnMessageReceived(messageKey, objectArgsAsString);
                });
        }

        public virtual void SendMessageToUsers<T>(string messageKey, T messageArgs, params string[] userIds)
            where T : class
        {
            if (userIds == null)
                throw new ArgumentNullException(nameof(userIds));

            if (messageKey == null)
                throw new ArgumentNullException(nameof(messageKey));

            IHubContext hubContext = _connectionManager.GetHubContext<MessagesHub>();

            string objectArgsAsString = messageArgs == null ? string.Empty : _formatter.Serialize(messageArgs);

            userIds.ToList()
                .ForEach(u =>
                {
                    hubContext.Clients.Group(u).OnMessageReceived(messageKey, objectArgsAsString);
                });
        }

        public async Task SendMessageToAllAsync<T>(string messageKey, T messageArgs)
            where T : class
        {
            if (messageKey == null)
                throw new ArgumentNullException(nameof(messageKey));

            IHubContext hubContext = _connectionManager.GetHubContext<MessagesHub>();

            string objectArgsAsString = messageArgs == null ? string.Empty : _formatter.Serialize(messageArgs);

            await hubContext.Clients.All.OnMessageReceived(messageKey, objectArgsAsString);
        }

        public void SendMessageToAll<T>(string messageKey, T messageArgs)
            where T : class
        {
            if (messageKey == null)
                throw new ArgumentNullException(nameof(messageKey));

            IHubContext hubContext = _connectionManager.GetHubContext<MessagesHub>();

            string objectArgsAsString = messageArgs == null ? string.Empty : _formatter.Serialize(messageArgs);

            hubContext.Clients.All.OnMessageReceived(messageKey, objectArgsAsString);
        }

        public async Task SendMessageToGroupsAsync<T>(string messageKey, T messageArgs, params string[] groupNames) where T : class
        {
            await SendMessageToUsersAsync(messageKey, messageArgs, groupNames);
        }

        public void SendMessageToGroups<T>(string messageKey, T messageArgs, params string[] groupNames) where T : class
        {
            SendMessageToUsers(messageKey, messageArgs, groupNames);
        }
    }
}