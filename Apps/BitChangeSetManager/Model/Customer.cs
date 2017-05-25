using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bit.Model.Contracts;

namespace BitChangeSetManager.Model
{
    public class Customer : IEntityWithDefaultGuidKey
    {
        [Key]
        public virtual Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public virtual string Name { get; set; }

        [InverseProperty(nameof(Delivery.Customer))]
        public virtual List<Delivery> DeliveredChangeSets { get; set; }
    }
}