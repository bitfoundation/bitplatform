using Foundation.Model.Contracts;
using System;

namespace BitChangeSetManager.Model
{
    public class ChangeSetSeverity : IEntityWithDefaultGuidKey
    {
        public virtual Guid Id { get; set; }

        public virtual string Title { get; set; }
    }
}