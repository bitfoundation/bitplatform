using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;

namespace BitChangeSetManager.Api
{
    public class ChangeSetDeliveryRequirementsController : DefaultReadOnlyDtoSetController<ChangeSetDeliveryRequirement, ChangeSetDeliveryRequirementDto>
    {
        public ChangeSetDeliveryRequirementsController(IBitChangeSetManagerRepository<ChangeSetDeliveryRequirement> repository)
            : base(repository)
        {
        }
    }
}