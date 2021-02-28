using Bit.Core.Contracts;
using Bit.Data.Contracts;
using Bit.Model.DomainModels;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.OData.ODataControllers
{
    public class UsersSettingsController : DtoController<UserSetting>
    {
        public virtual IUserInformationProvider UserInformationProvider { get; set; } = default!;
        public virtual IRepository<UserSetting> UsersSettingsRepository { get; set; } = default!;

        [Get]
        public virtual async Task<IQueryable<UserSetting>> Get(CancellationToken cancellationToken)
        {
            string userId = UserInformationProvider.GetCurrentUserId()!;

            return (await UsersSettingsRepository
                .GetAllAsync(cancellationToken).ConfigureAwait(false))
                .Where(userSetting => userSetting.UserId == userId);
        }
    }
}
