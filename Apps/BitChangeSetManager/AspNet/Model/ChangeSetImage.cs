using Bit.Model.Contracts;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitChangeSetManager.Model
{
    public class ChangeSetImage : IEntityWithDefaultGuidKey
    {
        public virtual Guid Id { get; set; }

        public virtual string Name { get; set; }

        public virtual byte[] Data { get; set; }

        public virtual Guid ChangeSetId { get; set; }

        [ForeignKey(nameof(ChangeSetId))]
        public virtual ChangeSet ChangeSet { get; set; }
    }
}