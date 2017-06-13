using System;
using System.ComponentModel.DataAnnotations;
using Bit.Model.Contracts;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

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

        public virtual Guid? CityId { get; set; }

        public virtual string CityName { get; set; }

        public virtual Guid? ProvinceId { get; set; }

        public virtual string ProvinceName { get; set; }

        public virtual Guid? NeedsReviewId { get; set; }

        [InverseProperty(nameof(ChangeSetImagetDto.ChangeSet))]
        public virtual List<ChangeSetImagetDto> Images { get; set; }
    }

    public class ChangeSetImagetDto : IDtoWithDefaultGuidKey
    {
        public virtual Guid Id { get; set; }

        public virtual string Name { get; set; }

        public virtual Guid ChangeSetId { get; set; }

        [ForeignKey(nameof(ChangeSetId))]
        [InverseProperty(nameof(ChangeSetDto.Images))]
        public virtual ChangeSetDto ChangeSet { get; set; }
    }
}