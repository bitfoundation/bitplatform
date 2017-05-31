using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;

namespace BitChangeSetManager.Api
{
    public class ConstantsController : DefaultReadOnlyDtoSetController<Constant, ConstantDto>
    {
        public ConstantsController(IBitChangeSetManagerRepository<Constant> repository)
            : base(repository)
        {
        }
    }
}