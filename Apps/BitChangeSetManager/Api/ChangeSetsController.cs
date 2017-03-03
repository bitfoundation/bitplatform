using System.Linq;
using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using Foundation.Api.ApiControllers;
using AutoMapper;
using AutoMapper.QueryableExtensions;

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

        public IMapper Mapper { get; set; }

        public IBitChangeSetManagerRepository<Customer> CustomersRepository { get; set; }

        public override IQueryable<ChangeSetDto> GetAll()
        {
            IQueryable<Customer> customersQuery = CustomersRepository.GetAll();

            return _changeSetsRepository
                .GetAll()
                .ProjectTo<ChangeSetDto>(configuration: Mapper.ConfigurationProvider, parameters: new { customersQuery = customersQuery });
        }
    }
}