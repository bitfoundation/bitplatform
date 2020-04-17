using System;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Bit.Signalr.Implementations
{
    public class SignalRMessageSender : IMessageSender
    {
        public virtual IMessageContentFormatter ContentFormatter { get; set; } = default!;

        private IConnectionManager _connectionManager = default!;

        public virtual Microsoft.AspNet.SignalR.IDependencyResolver SignalrDependencyResolver
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(SignalrDependencyResolver));

                _connectionManager = value.Resolve<IConnectionManager>();
            }
        }

        public virtual async Task SendMessageToUsersAsync<T>(string messageKey, T? messageArgs, string[] userIds)
            where T : class
        {
            if (userIds == null)
                throw new ArgumentNullException(nameof(userIds));

            if (messageKey == null)
                throw new ArgumentNullException(nameof(messageKey));

            IHubContext hubContext = _connectionManager.GetHubContext<MessagesHub>();

            string objectArgsAsString = messageArgs == null ? string.Empty : ContentFormatter.Serialize(messageArgs);

            foreach (string u in userIds)
            {
                await hubContext.Clients.Group(u).OnMessageReceived(messageKey, objectArgsAsString).ConfigureAwait(false);
            }
        }

        public virtual void SendMessageToUsers<T>(string messageKey, T? messageArgs, string[] userIds)
            where T : class
        {
            if (userIds == null)
                throw new ArgumentNullException(nameof(userIds));

            if (messageKey == null)
                throw new ArgumentNullException(nameof(messageKey));

            IHubContext hubContext = _connectionManager.GetHubContext<MessagesHub>();

            string objectArgsAsString = messageArgs == null ? string.Empty : ContentFormatter.Serialize(messageArgs);

            foreach (string u in userIds)
            {
                hubContext.Clients.Group(u).OnMessageReceived(messageKey, objectArgsAsString);
            }
        }

        public virtual Task SendMessageToAllAsync<T>(string messageKey, T? messageArgs)
            where T : class
        {
            if (messageKey == null)
                throw new ArgumentNullException(nameof(messageKey));

            IHubContext hubContext = _connectionManager.GetHubContext<MessagesHub>();

            string objectArgsAsString = messageArgs == null ? string.Empty : ContentFormatter.Serialize(messageArgs);

            return hubContext.Clients.All.OnMessageReceived(messageKey, objectArgsAsString);
        }

        public virtual void SendMessageToAll<T>(string messageKey, T? messageArgs)
            where T : class
        {
            if (messageKey == null)
                throw new ArgumentNullException(nameof(messageKey));

            IHubContext hubContext = _connectionManager.GetHubContext<MessagesHub>();

            string objectArgsAsString = messageArgs == null ? string.Empty : ContentFormatter.Serialize(messageArgs);

            hubContext.Clients.All.OnMessageReceived(messageKey, objectArgsAsString);
        }

        public virtual Task SendMessageToGroupsAsync<T>(string messageKey, T? messageArgs, string[] groupNames) where T : class
        {
            return SendMessageToUsersAsync(messageKey, messageArgs, groupNames);
        }

        public virtual void SendMessageToGroups<T>(string messageKey, T? messageArgs, string[] groupNames) where T : class
        {
            SendMessageToUsers(messageKey, messageArgs, groupNames);
        }
    }
}