using Bit.Model.Contracts;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitChangeSetManager.Model
{
    public class City : IEntityWithDefaultGuidKey
    {
        public virtual Guid Id { get; set; }

        public virtual string Name { get; set; }

        public virtual Guid ProvinceId { get; set; }

        [ForeignKey(nameof(ProvinceId))]
        public virtual Province Province { get; set; }
    }
}