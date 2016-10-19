using Foundation.Model.Contracts;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foundation.Test.Model.DomainModels
{
    [Table("Cities", Schema = "Test")]
    [Serializable]
    public class TestCity : IEntityWithDefaultGuidKey
    {
        [Key]
        public virtual Guid Id { get; set; }

        public virtual string Name { get; set; }
    }
}
