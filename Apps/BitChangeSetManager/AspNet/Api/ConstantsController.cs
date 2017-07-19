using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;

namespace BitChangeSetManager.Api
{
    public class ConstantsController : BitChangeSetManagerDtoSetController<ConstantDto, Constant>
    {
        public ConstantsController(IBitChangeSetManagerRepository<Constant> repository)
            : base(repository)
        {
        }

        protected override bool IsReadOnly()
        {
            return true;
        }
    }
}