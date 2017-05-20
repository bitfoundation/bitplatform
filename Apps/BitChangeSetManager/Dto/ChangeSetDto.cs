using Foundation.Model.Contracts;
using System;
using System.ComponentModel.DataAnnotations;

namespace BitChangeSetManager.Dto
{
    public class ChangeSetDto : IDtoWithDefaultGuidKey
    {
        [Key]
        public virtual Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public virtual string Title { get; set; }

        [DataType(DataType.Url)]
        [MaxLength(100)]
        public virtual string AssociatedCommitUrl { get; set; }

        public virtual DateTimeOffset? CreatedOn { get; set; }

        [MaxLength(200)]
        [DataType(DataType.MultilineText)]
        public virtual string Description { get; set; }

        public virtual bool IsDeliveredToAll { get; set; }

        public virtual Guid SeverityId { get; set; }

        public virtual string SeverityTitle { get; set; }

        public virtual Guid DeliveryRequirementId { get; set; }

        public virtual string DeliveryRequirementTitle { get; set; }
    }
}