using System;
using Bit.Model.Contracts;

namespace BitChangeSetManager.Dto
{
    public class ChangeSetDeliveryRequirementDto : IDtoWithDefaultGuidKey
    {
        public virtual Guid Id { get; set; }

        public virtual string Title { get; set; }
    }
}