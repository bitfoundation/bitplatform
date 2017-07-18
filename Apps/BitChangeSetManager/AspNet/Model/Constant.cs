using Bit.Model.Contracts;
using System;

namespace BitChangeSetManager.Model
{
    public class Constant : IEntityWithDefaultGuidKey
    {
        public virtual Guid Id { get; set; }

        public virtual string Name { get; set; }

        public virtual string Title { get; set; }
    }
}