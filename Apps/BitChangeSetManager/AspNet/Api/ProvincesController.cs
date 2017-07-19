using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;

namespace BitChangeSetManager.Api
{
    public class ProvincesController : BitChangeSetManagerDtoSetController<ProvinceDto, Province>
    {
        public ProvincesController(IBitChangeSetManagerRepository<Province> repository)
            : base(repository)
        {
        }

        protected override bool IsReadOnly()
        {
            return true;
        }
    }
}