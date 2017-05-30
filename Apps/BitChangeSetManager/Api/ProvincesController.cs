using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;

namespace BitChangeSetManager.Api
{
    public class ProvincesController : DefaultReadOnlyDtoSetController<Province, ProvinceDto>
    {
        public ProvincesController(IBitChangeSetManagerRepository<Province> repository)
            : base(repository)
        {
        }
    }
}