using System;
using Bit.Model.Contracts;

namespace BitChangeSetManager.Model
{
    public class ChangeSetDeliveryRequirement : IEntityWithDefaultGuidKey
    {
        public virtual Guid Id { get; set; }

        public virtual string Title { get; set; }
    }
}