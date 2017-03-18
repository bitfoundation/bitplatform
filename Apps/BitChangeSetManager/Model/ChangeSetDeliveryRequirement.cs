using Foundation.Model.Contracts;
using System;

namespace BitChangeSetManager.Model
{
    public class ChangeSetDeliveryRequirement : IEntityWithDefaultGuidKey
    {
        public virtual Guid Id { get; set; }

        public virtual string Title { get; set; }

        public virtual string Name { get; set; }
    }
}