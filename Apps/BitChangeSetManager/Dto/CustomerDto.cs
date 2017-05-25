using System;
using System.ComponentModel.DataAnnotations;
using Bit.Model.Contracts;

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