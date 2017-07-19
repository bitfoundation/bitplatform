using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Model;
using System;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Model.DomainModels;
using Bit.Owin.Contracts;

namespace BitChangeSetManager.Api.Implementations
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
            User user = await _usersRepository.GetByIdAsync(Guid.Parse(_userInformationProvider.GetCurrentUserId()));

            UserSetting result = new UserSetting
            {
                Culture = user.Culture.ToString()
            };

            return result;
        }
    }
}