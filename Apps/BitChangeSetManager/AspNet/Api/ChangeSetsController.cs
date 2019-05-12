using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Data.Contracts;
using System.Web.OData;
using BitChangeSetManager.DataAccess.Contracts;
using System.Web.Http;

namespace BitChangeSetManager.Api
{
    public class ChangeSetsController : BitChangeSetManagerDtoSetController<ChangeSetDto, ChangeSet, Guid>
    {
        public virtual IMessageSender MessageSender { get; set; }
        public virtual IUserInformationProvider UserInformationProvider { get; set; }
        public virtual IBitChangeSetManagerRepository<User> UsersRepository { get; set; }
        public virtual IBitChangeSetManagerRepository<Customer> CustomersRepository { get; set; }

        public override async Task<IQueryable<ChangeSetDto>> GetAll(CancellationToken cancellationToken)
        {
            // We can declare a view for following sql query in database and then map our dto directly to view using ef db context:
            // select *, (case (((select count(1) from Deliveries as Delivery where ChangeSet.Id = Delivery.ChangeSetId ))) when (select count(1) from Customers) then 1 else 0 end) as IsDeliveredToAll from ChangeSets as ChangeSet
            // or we can use ef core execute sql which returns IQueryable
            // Note: _changeSetsRepository.GetAll(changeSet => new ChangeSetDto { Id = changeSet.Id , ... , IsDeliveredToAll = changeSet.Deliveries.Count() == customersQuery.Count() }); results into problematic sql.

            // The downside of following code are its database round trips.

            int customersCount = await (await CustomersRepository.GetAllAsync(cancellationToken)).CountAsync(cancellationToken);

            return DtoEntityMapper.FromEntityQueryToDtoQuery((await Repository.GetAllAsync(cancellationToken)), parameters: new { customersCount = customersCount });
        }

        public override async Task<SingleResult<ChangeSetDto>> Create(ChangeSetDto dto, CancellationToken cancellationToken)
        {
            SingleResult<ChangeSetDto> insertedChangeSet = await base.Create(dto, cancellationToken);

            User user = await UsersRepository.GetByIdAsync(cancellationToken, Guid.Parse(UserInformationProvider.GetCurrentUserId()));

            MessageSender.SendMessageToGroups("ChangeSetHasBeenInsertedByUser", new { userName = user.UserName, title = dto.Title }, groupNames: new[] { user.Culture.ToString() });

            return insertedChangeSet;
        }
    }
}