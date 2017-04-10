using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Model;
using Foundation.Api.Contracts;
using Foundation.Core.Contracts;
using Foundation.Model.DomainModels;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BitChangeSetManager.Api
{
    public class BitUserSettingProvider : IUserSettingProvider
    {
        private readonly IUserInformationProvider _userInformationProvider;
        private readonly IBitChangeSetManagerRepository<User> _usersRepository;

        public BitUserSettingProvider(IUserInformationProvider userInformationProvider, IBitChangeSetManagerRepository<User> usersRepository)
        {
            _userInformationProvider = userInformationProvider;
            _usersRepository = usersRepository;
        }

        public UserSetting GetCurrentUserSetting()
        {
            User user = _usersRepository.GetById(Guid.Parse(_userInformationProvider.GetCurrentUserId()));

            UserSetting result = new UserSetting
            {
                Culture = user.Culture.ToString()
            };

            return result;
        }

        public async Task<UserSetting> GetCurrentUserSettingAsync(CancellationToken cancellationToken)
        {
            User user = await _usersRepository.GetByIdAsync(Guid.Parse(_userInformationProvider.GetCurrentUserId()), cancellationToken);

            UserSetting result = new UserSetting
            {
                Culture = user.Culture.ToString()
            };

            return result;
        }
    }
}