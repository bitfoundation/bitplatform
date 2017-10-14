using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Model;
using System;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Model.DomainModels;
using Bit.Owin.Contracts;
using Bit.Data.Contracts;
using BitChangeSetManager.DataAccess.Contracts;

namespace BitChangeSetManager.Api.Implementations
{
    public class BitUserSettingProvider : IUserSettingProvider
    {
        public virtual IUserInformationProvider UserInformationProvider { get; set; }
        public virtual IBitChangeSetManagerRepository<User> UsersRepository { get; set; }

        public UserSetting GetCurrentUserSetting()
        {
            User user = UsersRepository.GetById(Guid.Parse(UserInformationProvider.GetCurrentUserId()));

            UserSetting result = new UserSetting
            {
                Culture = user.Culture.ToString()
            };

            return result;
        }

        public async Task<UserSetting> GetCurrentUserSettingAsync(CancellationToken cancellationToken)
        {
            User user = await UsersRepository.GetByIdAsync(cancellationToken, Guid.Parse(UserInformationProvider.GetCurrentUserId()));

            UserSetting result = new UserSetting
            {
                Culture = user.Culture.ToString()
            };

            return result;
        }
    }
}