using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bit.Model.Contracts;

namespace Bit.Tests.Model.DomainModels
{
    [Table("TestCustomers", Schema = "Test")]
    [Serializable]
    public class TestCustomer : IEntity
    {
        [Key]
        public virtual Guid Id { get; set; }

        public virtual string Name { get; set; }

        [ForeignKey("CityId")]
        public virtual TestCity City { get; set; }

        public virtual Guid CityId { get; set; }
    }
}
