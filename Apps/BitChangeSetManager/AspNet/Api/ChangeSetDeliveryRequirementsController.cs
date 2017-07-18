using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;

namespace BitChangeSetManager.Api
{
    public class ChangeSetDeliveryRequirementsController : DefaultReadOnlyDtoSetController<ChangeSetDeliveryRequirementDto, ChangeSetDeliveryRequirement>
    {
        public ChangeSetDeliveryRequirementsController(IBitChangeSetManagerRepository<ChangeSetDeliveryRequirement> repository)
            : base(repository)
        {
        }
    }
}