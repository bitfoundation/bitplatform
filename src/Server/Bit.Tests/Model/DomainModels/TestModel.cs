using Bit.Model.Contracts;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bit.Tests.Model.DomainModels
{
    [Table("TestModels", Schema = "Test")]
    [Serializable]
    public class TestModel : IEntity, IDto
    {
        [Key]
        public virtual long Id { get; set; }

        [Required]
        [MaxLength(50)]
        public virtual string StringProperty { get; set; }

        public virtual DateTimeOffset DateProperty { get; set; }

        [ConcurrencyCheck]
        public virtual long Version { get; set; }
    }
}
