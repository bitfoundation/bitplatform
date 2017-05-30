using Bit.Model.Contracts;
using System;
using System.Collections.Generic;

namespace BitChangeSetManager.Model
{
    public class Province : IEntityWithDefaultGuidKey
    {
        public virtual Guid Id { get; set; }

        public virtual string Name { get; set; }

        public virtual ICollection<City> Cities { get; set; }
    }
}