using Foundation.Model.Contracts;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foundation.Test.Model.DomainModels
{
    [Table("TestCustomers", Schema = "Test")]
    [Serializable]
    public class TestCustomer : IEntityWithDefaultGuidKey
    {
        [Key]
        public virtual Guid Id { get; set; }

        public virtual string Name { get; set; }

        [ForeignKey("CityId")]
        public virtual TestCity City { get; set; }

        public virtual Guid CityId { get; set; }
    }
}
