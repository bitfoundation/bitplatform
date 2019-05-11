using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bit.Model.Contracts;

namespace Bit.Tests.Model.DomainModels
{
    [Table("ChildEntities", Schema = "Test")]
    [Serializable]
    public class ChildEntity : IEntity, IDto
    {
        [Key]
        public virtual long Id { get; set; }

        [Required]
        [MaxLength(50)]
        public virtual string Name { get; set; }

        [ConcurrencyCheck]
        public virtual long Version { get; set; }

        [ForeignKey(nameof(ParentEntityId))]
        public virtual ParentEntity ParentEntity { get; set; }

        public virtual long ParentEntityId { get; set; }
    }
}
