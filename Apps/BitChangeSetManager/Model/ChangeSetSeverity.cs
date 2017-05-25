using System;
using Bit.Model.Contracts;

namespace BitChangeSetManager.Model
{
    public class ChangeSetSeverity : IEntityWithDefaultGuidKey
    {
        public virtual Guid Id { get; set; }

        public virtual string Title { get; set; }
    }
}