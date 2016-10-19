using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Foundation.Model.Contracts;

namespace Foundation.Test.Model.DomainModels
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
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public virtual long Version { get; set; }

        [ForeignKey(nameof(ParentEntityId))]
        [InverseProperty(nameof(DomainModels.ParentEntity.ChildEntities))]
        public virtual ParentEntity ParentEntity { get; set; }

        public virtual long ParentEntityId { get; set; }
    }
}
