using System.Linq;
using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using Foundation.Api.ApiControllers;

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

        public IBitChangeSetManagerRepository<Customer> CustomersRepository { get; set; }

        public override IQueryable<ChangeSetDto> GetAll()
        {
            IQueryable<Customer> customersQuery = CustomersRepository.GetAll();

            return DtoModelMapper.FromModelQueryToDtoQuery(_changeSetsRepository.GetAll(), parameters: new { customersQuery = customersQuery });
        }
    }
}