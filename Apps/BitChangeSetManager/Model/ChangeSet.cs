using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bit.Model.Contracts;

namespace BitChangeSetManager.Model
{
    public class ChangeSet : IEntityWithDefaultGuidKey
    {
        [Key]
        public virtual Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public virtual string Title { get; set; }

        [DataType(DataType.Url)]
        [MaxLength(100)]
        public virtual string AssociatedCommitUrl { get; set; }

        public virtual DateTimeOffset CreatedOn { get; set; }

        [MaxLength(200)]
        [DataType(DataType.MultilineText)]
        public virtual string Description { get; set; }

        [InverseProperty(nameof(Delivery.ChangeSet))]
        public virtual List<Delivery> Deliveries { get; set; }

        public virtual Guid SeverityId { get; set; }

        [ForeignKey(nameof(SeverityId))]
        public virtual ChangeSetSeverity Severity { get; set; }

        public virtual Guid DeliveryRequirementId { get; set; }

        [ForeignKey(nameof(DeliveryRequirementId))]
        public virtual ChangeSetDeliveryRequirement DeliveryRequirement { get; set; }

        public virtual Guid? CityId { get; set; }

        [ForeignKey(nameof(CityId))]
        public virtual City City { get; set; }

        public virtual Guid? ProvinceId { get; set; }

        [ForeignKey(nameof(ProvinceId))]
        public virtual Province Province { get; set; }

        public virtual Guid? NeedsReviewId { get; set; }

        public virtual List<ChangeSetImage> Images { get; set; }
    }
}