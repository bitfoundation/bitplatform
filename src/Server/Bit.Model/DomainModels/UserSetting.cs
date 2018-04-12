using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bit.Model.Contracts;

namespace Bit.Model.DomainModels
{
    [Table("UsersSettingsView", Schema = "Bit")]
    public class UserSetting : IEntity, IDto
    {
        [Key]
        public virtual long Id { get; set; }

        [MaxLength(50)]
        public virtual string Theme { get; set; }

        [MaxLength(50)]
        public virtual string Culture { get; set; }

        [MaxLength(50)]
        public virtual string DesiredTimeZone { get; set; }

        [Required]
        public virtual string UserId { get; set; }

        [ConcurrencyCheck]
        public virtual long Version { get; set; }
    }
}