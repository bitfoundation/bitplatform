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

namespace BitChangeSetManager.Api
{
    public class ChangeSetsController : BitChangeSetManagerDtoSetController<ChangeSetDto, ChangeSet, Guid>
    {
        public ChangeSetsController(IChangeSetRepository changeSetRepository)
        {
            Repository = changeSetRepository;
        }

        public virtual IMessageSender MessageSender { get; set; }
        public virtual IUserInformationProvider UserInformationProvider { get; set; }
        public virtual IRepository<User> UsersRepository { get; set; }
        public IRepository<Customer> CustomersRepository { get; set; }

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

        public override async Task<ChangeSetDto> Create(ChangeSetDto dto, CancellationToken cancellationToken)
        {
            ChangeSetDto insertedChangeSet = await base.Create(dto, cancellationToken);

            User user = await UsersRepository.GetByIdAsync(cancellationToken, Guid.Parse(UserInformationProvider.GetCurrentUserId()));

            MessageSender.SendMessageToGroups("ChangeSetHasBeenInsertedByUser", new { userName = user.UserName, title = insertedChangeSet.Title }, groupNames: new[] { user.Culture.ToString() });

            return insertedChangeSet;
        }

        public override Task<ChangeSetDto> Update(Guid key, ChangeSetDto dto, CancellationToken cancellationToken)
        {
            return base.Update(key, dto, cancellationToken);
        }

        public override Task<ChangeSetDto> PartialUpdate(Guid key, Delta<ChangeSetDto> modifiedDtoDelta, CancellationToken cancellationToken)
        {
            return base.PartialUpdate(key, modifiedDtoDelta, cancellationToken);
        }
    }
}