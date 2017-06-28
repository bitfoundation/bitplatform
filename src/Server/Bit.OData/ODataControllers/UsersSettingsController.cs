using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Data.Contracts;
using Bit.Model.DomainModels;

namespace Bit.OData.ODataControllers
{
    public class UsersSettingsController : DtoController<UserSetting>
    {
        private readonly IUserInformationProvider _userInformationProvider;
        private readonly IRepository<UserSetting> _usersSettingsRepository;

        public UsersSettingsController(IUserInformationProvider userInformationProvider, IRepository<UserSetting> usersSettingsRepository)
        {
            if (userInformationProvider == null)
                throw new ArgumentNullException(nameof(userInformationProvider));

            if (usersSettingsRepository == null)
                throw new ArgumentNullException(nameof(usersSettingsRepository));

            _userInformationProvider = userInformationProvider;
            _usersSettingsRepository = usersSettingsRepository;
        }

        protected UsersSettingsController()
        {

        }

        [Get]
        public virtual async Task<IQueryable<UserSetting>> Get(CancellationToken cancellationToken)
        {
            string userId = _userInformationProvider.GetCurrentUserId();

            return (await _usersSettingsRepository
                .GetAllAsync(cancellationToken))
                .Where(userSetting => userSetting.UserId == userId);
        }
    }
}
