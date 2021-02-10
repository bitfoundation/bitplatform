using Bit.Data.Contracts;
using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using System;

namespace BitChangeSetManager.Api
{
    public class ChangeSetDeliveryRequirementsController : BitChangeSetManagerDtoSetController<ChangeSetDeliveryRequirementDto, ChangeSetDeliveryRequirement, Guid>
    {
        protected override bool IsReadOnly()
        {
            return true;
        }
    }
}