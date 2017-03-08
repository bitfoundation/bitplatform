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

        public IBitChangeSetManagerRepository<Delivery> DeliveriesRepository { get; set; }

        public override IQueryable<ChangeSetDto> GetAll()
        {
            IQueryable<Delivery> deliveriesQuery = DeliveriesRepository.GetAll();

            return DtoModelMapper.FromModelQueryToDtoQuery(_changeSetsRepository.GetAll(), parameters: new { deliveriesQuery = deliveriesQuery });
        }
    }
}