using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;

namespace BitChangeSetManager.Api
{
    public class ChangeSetDeliveryRequirementsController : BitChangeSetManagerDtoSetController<ChangeSetDeliveryRequirementDto, ChangeSetDeliveryRequirement>
    {
        public ChangeSetDeliveryRequirementsController(IBitChangeSetManagerRepository<ChangeSetDeliveryRequirement> repository)
            : base(repository)
        {
        }

        protected override bool IsReadOnly()
        {
            return true;
        }
    }
}