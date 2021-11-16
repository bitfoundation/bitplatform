using Bit.Model.Contracts;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bit.Tests.Model.DomainModels
{
    [Table("TestCustomers", Schema = "Test")]
    [Serializable]
    public class TestCustomer : IEntity, ISyncableEntity
    {
        [Key]
        public virtual Guid Id { get; set; }

        public virtual string Name { get; set; }

        [ForeignKey("CityId")]
        public virtual TestCity City { get; set; }

        public virtual Guid CityId { get; set; }

        public virtual long Version { get; set; }

        public virtual bool IsArchived { get; set; }

        [ConcurrencyCheck, Timestamp]
        public byte[] Timestamp { get; set; } = Array.Empty<byte>();
    }
}
