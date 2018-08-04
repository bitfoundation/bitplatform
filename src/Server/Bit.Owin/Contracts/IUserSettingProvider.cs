using Bit.Model.DomainModels;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Owin.Contracts
{
    public interface IUserSettingProvider
    {
        Task<UserSetting> GetCurrentUserSettingAsync(CancellationToken cancellationToken);

        UserSetting GetCurrentUserSetting();
    }
}
