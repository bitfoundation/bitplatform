using Foundation.Model.Contracts;
using System;
using System.ComponentModel.DataAnnotations;

namespace Foundation.Test.Model.Dto
{
    [Serializable]
    public class ValidationSampleDto : IDto
    {
        [Key]
        public virtual long Id { get; set; }

        [Required]
        public virtual string RequiredByAttributeMember { get; set; }

        public virtual string RequiredByMetadataMember { get; set; }

        public virtual string NotRequiredMember { get; set; }

        public virtual string RequiredByDtoRulesMember { get; set; }
    }
}
