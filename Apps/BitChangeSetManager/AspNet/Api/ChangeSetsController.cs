using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;

namespace BitChangeSetManager.Api
{
    public class ChangeSetsController : BitChangeSetManagerDtoSetController<ChangeSetDto, ChangeSet>
    {
        private readonly IChangeSetRepository _changeSetsRepository;
        private readonly IMessageSender _messageSender;
        private readonly IUserInformationProvider _userInformationProvider;
        private readonly IBitChangeSetManagerRepository<User> _usersRepository;

        public ChangeSetsController(IChangeSetRepository changeSetsRepository, IMessageSender messageSender, IUserInformationProvider userInformationProvider, IBitChangeSetManagerRepository<User> usersRepository)
            : base(changeSetsRepository)
        {
            _changeSetsRepository = changeSetsRepository;
            _messageSender = messageSender;
            _userInformationProvider = userInformationProvider;
            _usersRepository = usersRepository;
        }

        public IBitChangeSetManagerRepository<Customer> CustomersRepository { get; set; }

        public override async Task<IQueryable<ChangeSetDto>> GetAll(CancellationToken cancellationToken)
        {
            // We can declare a view for following sql query in database and then map our dto directly to view using ef db context:
            // select *, (case (((select count(1) from Deliveries as Delivery where ChangeSet.Id = Delivery.ChangeSetId ))) when (select count(1) from Customers) then 1 else 0 end) as IsDeliveredToAll from ChangeSets as ChangeSet
            // or we can use ef core execute sql which returns IQueryable
            // Note: _changeSetsRepository.GetAll(changeSet => new ChangeSetDto { Id = changeSet.Id , ... , IsDeliveredToAll = changeSet.Deliveries.Count() == customersQuery.Count() }); results into problematic sql.

            // The downside of following code are its database round trips.

            int customersCount = await (await CustomersRepository.GetAllAsync(cancellationToken)).CountAsync(cancellationToken);

            return DtoModelMapper.FromModelQueryToDtoQuery((await _changeSetsRepository.GetAllAsync(cancellationToken)), parameters: new { customersCount = customersCount });
        }

        public override async Task<ChangeSetDto> Create(ChangeSetDto dto, CancellationToken cancellationToken)
        {
            ChangeSetDto insertedChangeSet = await base.Create(dto, cancellationToken);

            User user = await _usersRepository.GetByIdAsync(Guid.Parse(_userInformationProvider.GetCurrentUserId()));

            _messageSender.SendMessageToGroups("ChangeSetHasBeenInsertedByUser", new { userName = user.UserName, title = insertedChangeSet.Title }, groupNames: new[] { user.Culture.ToString() });

            return insertedChangeSet;
        }
    }
}