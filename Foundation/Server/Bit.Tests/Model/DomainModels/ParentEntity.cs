using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Foundation.Model.Contracts;

namespace Foundation.Test.Model.DomainModels
{
    [Table("ParentEntities", Schema = "Test")]
    [Serializable]
    public class ParentEntity : IEntity, IDto
    {
        [Key]
        public virtual long Id { get; set; }

        [Required]
        [MaxLength(50)]
        public virtual string Name { get; set; }

        [DataType(DataType.Date)]
        public virtual DateTimeOffset? Date { get; set; }

        [ConcurrencyCheck]
        public virtual long Version { get; set; }

        [InverseProperty(nameof(ChildEntity.ParentEntity))]
        public virtual List<ChildEntity> ChildEntities { get; set; } = new List<ChildEntity>();

        public virtual TestModel TestModel { get; set; }
    }
}
