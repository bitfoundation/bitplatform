using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Foundation.Model.Contracts;

namespace Foundation.Test.Model.DomainModels
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
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public virtual long Version { get; set; }
    }
}
