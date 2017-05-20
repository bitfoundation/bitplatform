using Foundation.Model.Contracts;
using System;

namespace BitChangeSetManager.Dto
{
    public class ChangeSetSeverityDto : IDtoWithDefaultGuidKey
    {
        public virtual Guid Id { get; set; }

        public virtual string Title { get; set; }
    }
}