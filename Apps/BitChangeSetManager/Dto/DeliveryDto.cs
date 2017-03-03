using Foundation.Model.Contracts;
using System;
using System.ComponentModel.DataAnnotations;

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
    }
}