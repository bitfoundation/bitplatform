using System;
using System.ComponentModel.DataAnnotations;
using Bit.Model.Contracts;

namespace BitChangeSetManager.Dto
{
    public class DeliveryDto : IDtoWithDefaultGuidKey
    {
        [Key]
        public virtual Guid Id { get; set; }

        public virtual Guid CustomerId { get; set; }

        public virtual string CustomerName { get; set; }

        public virtual Guid ChangeSetId { get; set; }

        public virtual string ChangeSetTitle { get; set; }

        public virtual DateTimeOffset DeliveredOn { get; set; }
    }
}