using System.Threading;
using System.Threading.Tasks;
using Bit.Model.DomainModels;

namespace Bit.Owin.Contracts
{
    public interface IUserSettingProvider
    {
        Task<UserSetting?> GetCurrentUserSettingAsync(CancellationToken cancellationToken);

        UserSetting? GetCurrentUserSetting();
    }
}