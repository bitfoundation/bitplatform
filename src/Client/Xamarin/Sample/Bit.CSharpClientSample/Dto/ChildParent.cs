using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bit.Model.Contracts;

namespace Bit.CSharpClientSample.Dto
{
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

        public virtual List<ChildEntity> ChildEntities { get; set; } = new List<ChildEntity>();
    }
}
