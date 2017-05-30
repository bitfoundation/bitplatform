using Bit.Model.Contracts;
using System;

namespace BitChangeSetManager.Dto
{
    public class CityDto : IDtoWithDefaultGuidKey
    {
        public virtual Guid Id { get; set; }

        public virtual string Name { get; set; }

        public virtual Guid ProvinceId { get; set; }

        public virtual bool CanBeSelectedForChangeSet { get; set; }
    }
}