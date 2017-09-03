using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using System;

namespace BitChangeSetManager.Api
{
    public class ChangeSetDeliveryRequirementsController : BitChangeSetManagerDtoSetController<ChangeSetDeliveryRequirementDto, ChangeSetDeliveryRequirement, Guid>
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