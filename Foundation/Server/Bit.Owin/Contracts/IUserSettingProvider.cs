using System.Threading;
using System.Threading.Tasks;
using Foundation.Model.DomainModels;

namespace Foundation.Api.Contracts
{
    public interface IUserSettingProvider
    {
        Task<UserSetting> GetCurrentUserSettingAsync(CancellationToken cancellationToken);

        UserSetting GetCurrentUserSetting();
    }
}