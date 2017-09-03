using System;
using Bit.Model.Contracts;

namespace BitChangeSetManager.Dto
{
    public class ChangeSetSeverityDto : IDto
    {
        public virtual Guid Id { get; set; }

        public virtual string Title { get; set; }
    }
}