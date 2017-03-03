using Foundation.Model.Contracts;
using System;
using System.ComponentModel.DataAnnotations;

namespace BitChangeSetManager.Dto
{
    public class CustomerDto : IDtoWithDefaultGuidKey
    {
        [Key]
        public virtual Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public virtual string Name { get; set; }
    }
}