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
        public virtual IUserInformationProvider UserInformationProvider { get; set; }
        public virtual IRepository<UserSetting> UsersSettingsRepository { get; set; }

        [Get]
        public virtual async Task<IQueryable<UserSetting>> Get(CancellationToken cancellationToken)
        {
            string userId = UserInformationProvider.GetCurrentUserId();

            return (await UsersSettingsRepository
                .GetAllAsync(cancellationToken))
                .Where(userSetting => userSetting.UserId == userId);
        }
    }
}
