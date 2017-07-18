using System;
using Bit.Model.Contracts;

namespace BitChangeSetManager.Dto
{
    public class ChangeSetSeverityDto : IDtoWithDefaultGuidKey
    {
        public virtual Guid Id { get; set; }

        public virtual string Title { get; set; }
    }
}