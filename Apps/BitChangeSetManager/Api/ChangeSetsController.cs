using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using Foundation.Api.ApiControllers;
using Foundation.Core.Contracts;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitChangeSetManager.Api
{
    public class ChangeSetsController : DefaultDtoSetController<ChangeSet, ChangeSetDto>
    {
        private readonly IBitChangeSetManagerRepository<ChangeSet> _changeSetsRepository;

        public ChangeSetsController(IBitChangeSetManagerRepository<ChangeSet> changeSetsRepository)
            : base(changeSetsRepository)
        {
            _changeSetsRepository = changeSetsRepository;
        }

        public override Task<ChangeSetDto> Insert(ChangeSetDto dto, CancellationToken cancellationToken)
        {
            dto.CreatedOn = DateTimeProvider.GetCurrentUtcDateTime();

            return base.Insert(dto, cancellationToken);
        }

        public IBitChangeSetManagerRepository<Customer> CustomersRepository { get; set; }

        public IDateTimeProvider DateTimeProvider { get; set; }

        public override async Task<IQueryable<ChangeSetDto>> GetAll(CancellationToken cancellationToken)
        {
            // We can declare a view for following sql query in database and then map our dto directly to view using ef db context:
            // select *, (case (((select count(1) from Deliveries as Delivery where ChangeSet.Id = Delivery.ChangeSetId ))) when (select count(1) from Customers) then 1 else 0 end) as IsDeliveredToAll from ChangeSets as ChangeSet
            // or we can use ef core execute sql which returns IQueryable
            // Note: _changeSetsRepository.GetAll(changeSet => new ChangeSetDto { Id = changeSet.Id , ... , IsDeliveredToAll = changeSet.Deliveries.Count() == customersQuery.Count() }); results into problematic sql.

            // The downside of following code are its database round trips.

            int customersCount = await CustomersRepository.GetAll().CountAsync(cancellationToken);

            return DtoModelMapper.FromModelQueryToDtoQuery(_changeSetsRepository.GetAll(), parameters: new { customersCount = customersCount });
        }
    }
}