using Foundation.Model.Contracts;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitChangeSetManager.Model
{
    public class Delivery : IEntityWithDefaultGuidKey
    {
        [Key]
        public virtual Guid Id { get; set; }

        public virtual Guid CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        [InverseProperty(nameof(Model.Customer.DeliveredChangeSets))]
        public virtual Customer Customer { get; set; }

        public virtual Guid ChangeSetId { get; set; }

        [ForeignKey(nameof(ChangeSetId))]
        [InverseProperty(nameof(Model.ChangeSet.Deliveries))]
        public virtual ChangeSet ChangeSet { get; set; }

        public virtual DateTimeOffset DeliveredOn { get; set; }
    }
}