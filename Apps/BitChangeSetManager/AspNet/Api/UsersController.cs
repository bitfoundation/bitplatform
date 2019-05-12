using Bit.Core.Contracts;
using Bit.Model.Contracts;
using Bit.OData.ODataControllers;
using BitChangeSetManager.DataAccess.Contracts;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace BitChangeSetManager.Api
{
    public class UsersController : DtoController<UserDto>
    {
        public IBitChangeSetManagerRepository<User> UsersRepository { get; set; }

        public IUserInformationProvider UserInformationProvider { get; set; }

        public IDtoEntityMapper<UserDto, User> DtoModelMapper { get; set; }

        [Function]
        public async Task<SingleResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken)
        {
            Guid userId = Guid.Parse(UserInformationProvider.GetCurrentUserId());

            return SingleResult(DtoModelMapper.FromEntityQueryToDtoQuery((await UsersRepository.GetAllAsync(cancellationToken)))
                 .Where(u => u.Id == userId));
        }
    }
}