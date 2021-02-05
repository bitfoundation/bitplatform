using Bit.Model.Contracts;
using System;

namespace BitChangeSetManager.Dto
{
    public class CityDto : IDto
    {
        public virtual Guid Id { get; set; }

        public virtual string Name { get; set; }

        public virtual Guid ProvinceId { get; set; }
    }
}