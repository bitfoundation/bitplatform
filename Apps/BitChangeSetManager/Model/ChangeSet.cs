using Foundation.Model.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    }
}