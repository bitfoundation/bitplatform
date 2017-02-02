using System.Threading.Tasks;

namespace Foundation.Core.Contracts
{
    public interface IMessageSender
    {
        Task SendMessageToUsersAsync<T>(string messageKey, T messageArgs, params string[] userIds)
            where T : class;

        void SendMessageToUsers<T>(string messageKey, T messageArgs, params string[] userIds)
            where T : class;

        Task SendMessageToGroupsAsync<T>(string messageKey, T messageArgs, params string[] groupNames)
            where T : class;

        void SendMessageToGroups<T>(string messageKey, T messageArgs, params string[] groupNames)
            where T : class;

        Task SendMessageToAllAsync<T>(string messageKey, T messageArgs)
            where T : class;

        void SendMessageToAll<T>(string messageKey, T messageArgs)
            where T : class;
    }
}