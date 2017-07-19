using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;

namespace BitChangeSetManager.Api
{
    public class ChangeSetSeveritiesController : BitChangeSetManagerDtoSetController<ChangeSetSeverityDto, ChangeSetSeverity>
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