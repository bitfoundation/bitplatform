using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using System;

namespace BitChangeSetManager.Api
{
    public class ChangeSetSeveritiesController : BitChangeSetManagerDtoSetController<ChangeSetSeverityDto, ChangeSetSeverity, Guid>
    {
        public ChangeSetSeveritiesController(IBitChangeSetManagerRepository<ChangeSetSeverity> repository)
            : base(repository)
        {
        }

        protected override bool IsReadOnly()
        {
            return true;
        }
    }
}